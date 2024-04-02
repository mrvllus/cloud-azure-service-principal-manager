using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ServicePrincipalManager.Services;

public class KeyVaultService
{
    private static DefaultAzureCredentialOptions _defaultCredsOptions;
    private readonly TelemetryClient _telemetryClient;
    private SecretClient _secretClient;

    public KeyVaultService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public async Task<KeyVaultSecret> GetSecretAsync(KeyVaultDto keyVaultDto, string secretName, string logPrefix)
    {
        try
        {
            if (_secretClient is null)
                CreateSecretClient(keyVaultDto, logPrefix);

            var kvResult = await _secretClient.GetSecretAsync(secretName);
            return kvResult;
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackTrace($"{nameof(GetSecretAsync)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }

    public async Task UpsertSecretAsync(ServicePrincipalDto servPrincDto, KeyVaultDto keyVaultDto,
        string kvSecretName, string logPrefix, bool isTest = true)
    {
        try
        {
            // Backup Tags on Secret Version
            var kvTags = await BackupTags(keyVaultDto, servPrincDto.ClientId, kvSecretName, logPrefix);

            // Disable Existing Secret
            try
            {
                await DisableSecretVersion(keyVaultDto, kvSecretName, logPrefix, isTest);
            }
            catch (Azure.RequestFailedException ex)
            {
                // If the secret doesn't exist, a Request failed will be thrown. If the message says "was not found ... ", we will ignore and create
                // If not, we will throw it.
                if (!ex.Message.Contains("was not found in this key vault"))
                    throw;
            }

            if (!isTest)
            {
                // Create New Secret
                var updatedSecret = new KeyVaultSecret(kvSecretName, servPrincDto.NewPasswordCred.SecretText);

                foreach (var tag in kvTags)
                {
                    _telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} Adding KeyVault Tag Key:({tag.Key}) - " +
                        $"Val:({tag.Value}), Secret:{updatedSecret.Name}");
                    updatedSecret.Properties.Tags[tag.Key] = tag.Value;
                }

                updatedSecret.Properties.ExpiresOn = DateTimeOffset.UtcNow.AddDays(double.Parse(servPrincDto.SecretExpOffset));
                updatedSecret.Properties.Enabled = true;

                // UPSERT: This SetSecretAsync does the upsert, read the definition
                var secResult = await _secretClient.SetSecretAsync(updatedSecret);
                var newSecretVer = secResult.Value;

                _telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} " +
                    $"KeyVaultSecretName: {kvSecretName}, vers: {newSecretVer.Properties.Version}, " +
                    $"createdOn: {newSecretVer.Properties.CreatedOn}, expiresOn: {newSecretVer.Properties.ExpiresOn}");
            }
            else
            {
                _telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} (LOG ONLY) " +
                    $"KeyVaultSecretName: {kvSecretName}, hint: {servPrincDto.NewPasswordCred.Hint}");
            }
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} " +
                $"ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }

    private async Task DisableSecretVersion(KeyVaultDto keyVaultDto, string kvSecretName, string logPrefix, bool isTest = true)
    {
        if (_secretClient is null)
            CreateSecretClient(keyVaultDto, logPrefix);

        var secretResponse = await _secretClient.GetSecretAsync(kvSecretName);
        var existingSecret = secretResponse?.Value;

        // Disable Existing Secret Version
        existingSecret.Properties.Enabled = false;

        if (!isTest)
        {
            var updatedSecretProperties = await _secretClient.UpdateSecretPropertiesAsync(existingSecret.Properties);
            Guard.Against.Null(updatedSecretProperties, nameof(updatedSecretProperties));

            _telemetryClient.TrackTrace($"{logPrefix}|{nameof(DisableSecretVersion)}: " +
                $"KeyVault {keyVaultDto.Uri}, SecretName:{existingSecret.Properties.Name}, Vers: {existingSecret.Properties.Version}");
        }
        else
        {
            _telemetryClient.TrackTrace($"{logPrefix}|{nameof(DisableSecretVersion)}: " +
                $"(LOG ONLY) KeyVault {keyVaultDto.Uri}, SecretName:{existingSecret.Properties.Name}, Vers: {existingSecret.Properties.Version}");
        }
    }

    private async Task<Dictionary<string, string>> BackupTags(KeyVaultDto keyVaultDto, string appId, string kvSecretName, string logPrefix)
    {
        var datePattern = @"M/d/yyyy hh:mm:ss tt";
        var kvTags = new Dictionary<string, string>
        {
            { keyVaultDto.KrsApprovedTag, appId },
            { "client-id", appId },
            { "krs-rotated", DateTime.UtcNow.ToString(datePattern)},
        };

        if (_secretClient is null)
            CreateSecretClient(keyVaultDto, logPrefix);

        var secretResponse = await _secretClient.GetSecretAsync(kvSecretName);
        var existingSecret = secretResponse?.Value;

        // Save existing tags, remove krs-approved and KeyRotationCreationTmstmp
        foreach (var existingTags in existingSecret.Properties.Tags)
        {
            try
            {
                // This Dictionary will throw exception if tag already exists, easy way to remove dups
                kvTags.Add(existingTags.Key, existingTags.Value);
            }
            catch
            {
                // do nothing, tag already exists
            }
        }

        return kvTags;
    }

    private void CreateSecretClient(KeyVaultDto keyVaultDto, string logPrefix)
    {
        try
        {
            // https://learn.microsoft.com/en-us/dotnet/api/azure.identity.managedidentitycredential.-ctor?view=azure-dotnet#azure-identity-managedidentitycredential-ctor(system-string-azure-identity-tokencredentialoptions)
            // Remember to set your Envirment var for Azure_Tenant_ID
            var miCred = new ManagedIdentityCredential(keyVaultDto.ManagedIdentityClientId,
                new TokenCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud });

            var devCredOption = new DefaultAzureCredentialOptions { VisualStudioTenantId = "9f914513-dda8-4d64-98a0-2d902b23352f" };
            var chainTken = new ChainedTokenCredential(miCred, new DefaultAzureCredential(devCredOption));

            SetDefaultAzureCredentialOptions(keyVaultDto.ManagedIdentityClientId);
            _secretClient = new SecretClient(new Uri($"https://{keyVaultDto.Uri}.vault.azure.net"), chainTken);
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackTrace($"{logPrefix}|{nameof(CreateSecretClient)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }

    public async Task<List<SecretProperties>> GetSecretPropertiesByTagAsync(KeyVaultDto keyVaultDto, string logPrefix)
    {
        try
        {
            if (_secretClient is null)
                CreateSecretClient(keyVaultDto, logPrefix);

            var allSecretProps = _secretClient.GetPropertiesOfSecretsAsync();

            List<SecretProperties> secretProps = new();

            await foreach (SecretProperties secretProperties in allSecretProps)
            {
                if (secretProperties.Tags.ContainsKey(keyVaultDto.KrsApprovedTag))
                    secretProps.Add(secretProperties);
            }

            return secretProps;
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackTrace($"{logPrefix}|{nameof(GetSecretPropertiesByTagAsync)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }

    private static void SetDefaultAzureCredentialOptions(string managedIdentityClientId)
    {
#if DEBUG
        _defaultCredsOptions = new DefaultAzureCredentialOptions { VisualStudioTenantId = "9f914513-dda8-4d64-98a0-2d902b23352f" }; // beta for testing
#else
        _defaultCredsOptions = new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId };
#endif
    }
}