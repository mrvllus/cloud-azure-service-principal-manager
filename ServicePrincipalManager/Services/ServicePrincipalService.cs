using Ardalis.GuardClauses;
using Microsoft.ApplicationInsights;

namespace ServicePrincipalManager.Services;

public class ServicePrincipalService
{
    private readonly KeyVaultService _keyVaultService;
    private readonly TelemetryClient _telemetryClient;
    private readonly EntraIdService _entraIdService;

    public ServicePrincipalService(KeyVaultService keyVaultService,
        TelemetryClient telemetryClient, EntraIdService entraIdService)
    {
        _keyVaultService = keyVaultService;
        _telemetryClient = telemetryClient;
        _entraIdService = entraIdService;
    }

    public async Task CreateSecretAsync(string logPrefix)
    {
        try
        {
            _telemetryClient.TrackTrace($"{logPrefix}|{nameof(CreateSecretAsync)} Started Snowflake - ServicePrinc: " +
              $"{entraIdDto.ServicePrincipal.DisplayName}");

            var datePattern = @"M/d/yyyy hh:mm:ss tt";
            keyVaultDto.Tags = new Dictionary<string, string>
            {
                { "client-id", entraIdDto.ServicePrincipal.AppId },
                { "krs-added", DateTime.UtcNow.ToString(datePattern)},
                { "kv-name", keyVaultDto.CustomerKeyVaultName}
            };

            // #######################################
            // ### Create Service Principal Secret ###
            await _entraIdService.CreateApplicationSecretAsync(entraIdDto, keyVaultDto.ManagedIdentityClientId, logPrefix, isTest);

            // #######################################################
            // ### Add Principal Secret to Snowmageddon Key Vault  ###
            await _keyVaultService.UpsertSecretAsync(entraIdDto, keyVaultDto, entraIdDto.ServicePrincipal.DisplayName, logPrefix, isTest);

            // ###################################################
            // ### Add Principal Secret to Customer Key Vault  ###
            keyVaultDto.Uri = keyVaultDto.CustomerKeyVaultName;
            await _keyVaultService.UpsertSecretAsync(entraIdDto, keyVaultDto, entraIdDto.ServicePrincipal.DisplayName, logPrefix, isTest);
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackTrace($"{logPrefix}|{nameof(CreateSecretAsync)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }
}