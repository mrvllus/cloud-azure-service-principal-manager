using Azure.Security.KeyVault.Secrets;

namespace ServicePrincipalManager.Services;

public class KeyVaultService(TelemetryClient telemetryClient, IOptions<ServicePrincipalMgrOptions> options)
{
    private readonly ServicePrincipalMgrOptions _spOptions = options.Value;

    public async Task<KeyVaultSecret> GetSecretAsync(string keyVaultUrl, string keyVaultSecretName, string logPrefix)
    {
        try
        {
            var secretClient = CreateSecretClient(keyVaultUrl, logPrefix);
            return await secretClient.GetSecretAsync(keyVaultSecretName);
        }
        catch (Exception ex)
        {
            telemetryClient.TrackTrace($"{nameof(GetSecretAsync)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }

    //public async Task UpsertSecretAsync(ServicePrincipalDto servPrincDto, KeyVaultDto keyVaultDto,
    //    string kvSecretName, string logPrefix, bool isTest = true)
    //{
    //    try
    //    {
    //        // Backup Tags on Secret Version
    //        var kvTags = await BackupTags(keyVaultDto, servPrincDto.ClientId, kvSecretName, logPrefix);

    //        // Disable Existing Secret
    //        try
    //        {
    //            await DisableSecretVersion(keyVaultDto, kvSecretName, logPrefix, isTest);
    //        }
    //        catch (Azure.RequestFailedException ex)
    //        {
    //            // If the secret doesn't exist, a Request failed will be thrown. If the message says "was not found ... ", we will ignore and create
    //            // If not, we will throw it.
    //            if (!ex.Message.Contains("was not found in this key vault"))
    //                throw;
    //        }

    //        if (!isTest)
    //        {
    //            // Create New Secret
    //            var updatedSecret = new KeyVaultSecret(kvSecretName, servPrincDto.NewPasswordCred.SecretText);

    //            foreach (var tag in kvTags)
    //            {
    //                telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} Adding KeyVault Tag Key:({tag.Key}) - " +
    //                    $"Val:({tag.Value}), Secret:{updatedSecret.Name}");
    //                updatedSecret.Properties.Tags[tag.Key] = tag.Value;
    //            }

    //            updatedSecret.Properties.ExpiresOn = DateTimeOffset.UtcNow.AddDays(double.Parse(servPrincDto.SecretExpOffset));
    //            updatedSecret.Properties.Enabled = true;

    //            // UPSERT: This SetSecretAsync does the upsert, read the definition
    //            var secResult = await _secretClient.SetSecretAsync(updatedSecret);
    //            var newSecretVer = secResult.Value;

    //            telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} " +
    //                $"KeyVaultSecretName: {kvSecretName}, vers: {newSecretVer.Properties.Version}, " +
    //                $"createdOn: {newSecretVer.Properties.CreatedOn}, expiresOn: {newSecretVer.Properties.ExpiresOn}");
    //        }
    //        else
    //        {
    //            telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} (LOG ONLY) " +
    //                $"KeyVaultSecretName: {kvSecretName}, hint: {servPrincDto.NewPasswordCred.Hint}");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(UpsertSecretAsync)} " +
    //            $"ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
    //        throw;
    //    }
    //}

    //private async Task DisableSecretVersion(KeyVaultDto keyVaultDto, string kvSecretName, string logPrefix, bool isTest = true)
    //{
    //    if (_secretClient is null)
    //        CreateSecretClient(keyVaultDto, logPrefix);

    //    var secretResponse = await _secretClient.GetSecretAsync(kvSecretName);
    //    var existingSecret = secretResponse?.Value;

    //    // Disable Existing Secret Version
    //    existingSecret.Properties.Enabled = false;

    //    if (!isTest)
    //    {
    //        var updatedSecretProperties = await _secretClient.UpdateSecretPropertiesAsync(existingSecret.Properties);
    //        Guard.Against.Null(updatedSecretProperties, nameof(updatedSecretProperties));

    //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(DisableSecretVersion)}: " +
    //            $"KeyVault {keyVaultDto.Uri}, SecretName:{existingSecret.Properties.Name}, Vers: {existingSecret.Properties.Version}");
    //    }
    //    else
    //    {
    //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(DisableSecretVersion)}: " +
    //            $"(LOG ONLY) KeyVault {keyVaultDto.Uri}, SecretName:{existingSecret.Properties.Name}, Vers: {existingSecret.Properties.Version}");
    //    }
    //}

    //private async Task<Dictionary<string, string>> BackupTags(KeyVaultDto keyVaultDto, string appId, string kvSecretName, string logPrefix)
    //{
    //    var datePattern = @"M/d/yyyy hh:mm:ss tt";
    //    var kvTags = new Dictionary<string, string>
    //    {
    //        { keyVaultDto.KrsApprovedTag, appId },
    //        { "client-id", appId },
    //        { "krs-rotated", DateTime.UtcNow.ToString(datePattern)},
    //    };

    //    if (_secretClient is null)
    //        CreateSecretClient(keyVaultDto, logPrefix);

    //    var secretResponse = await _secretClient.GetSecretAsync(kvSecretName);
    //    var existingSecret = secretResponse?.Value;

    //    // Save existing tags, remove krs-approved and KeyRotationCreationTmstmp
    //    foreach (var existingTags in existingSecret.Properties.Tags)
    //    {
    //        try
    //        {
    //            // This Dictionary will throw exception if tag already exists, easy way to remove dups
    //            kvTags.Add(existingTags.Key, existingTags.Value);
    //        }
    //        catch
    //        {
    //            // do nothing, tag already exists
    //        }
    //    }

    //    return kvTags;
    //}

    private SecretClient CreateSecretClient(string keyVaultUrl, string logPrefix)
    {
        try
        {
            // https://learn.microsoft.com/en-us/dotnet/api/azure.identity.managedidentitycredential.-ctor?view=azure-dotnet#azure-identity-managedidentitycredential-ctor(system-string-azure-identity-tokencredentialoptions)
            // Remember to set your Envirment var for Azure_Tenant_ID
            var miCred = new ManagedIdentityCredential(_spOptions.ManagedIdentityClientId,
            new TokenCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud });

            var devCredOption = new DefaultAzureCredentialOptions { VisualStudioTenantId = "9f914513-dda8-4d64-98a0-2d902b23352f" };
            var chainTken = new ChainedTokenCredential(miCred, new DefaultAzureCredential(devCredOption));

            return new SecretClient(new Uri($"https://{keyVaultUrl}.vault.azure.net"), chainTken);
        }
        catch (Exception ex)
        {
            telemetryClient.TrackTrace($"{logPrefix}|{nameof(CreateSecretClient)}: ERROR - " 
                + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }

    public async Task<List<SecretProperties>> GetSecretPropertiesByTagAsync(string keyVaultUrl, string keyTag, string logPrefix)
    {
        try
        {
            var secretClient = CreateSecretClient(keyVaultUrl, logPrefix);

            var allSecretProps = secretClient.GetPropertiesOfSecretsAsync();

            List<SecretProperties> secretProps = new();

            await foreach (SecretProperties secretProperties in allSecretProps)
            {
                if (secretProperties.Tags.ContainsKey(keyTag))
                    secretProps.Add(secretProperties);
            }

            return secretProps;
        }
        catch (Exception ex)
        {
            telemetryClient.TrackTrace($"{logPrefix}|{nameof(GetSecretPropertiesByTagAsync)}: ERROR - " 
                + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }
}