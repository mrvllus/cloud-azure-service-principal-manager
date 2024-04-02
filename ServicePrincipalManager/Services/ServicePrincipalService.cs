using Ardalis.GuardClauses;
using Microsoft.ApplicationInsights;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Abstractions;

namespace ServicePrincipalManager.Services;

public class ServicePrincipalService(KeyVaultService keyVaultService,
    TelemetryClient telemetryClient, EntraIdService entraIdService,
    IOptions<ServicePrincipalMgrOptions> options)
{
    private readonly ServicePrincipalMgrOptions _spOptions = options.Value;
    public async Task ManageSecretsAsync(Application appRegistration, string logPrefix)
    {
        try
        {
            Guard.Against.Null(appRegistration, nameof(appRegistration));

            /*
             * 1. Check clientId exists in snowmageddon
             * 2. if exists, check appReg secret is close to expiration if within 7 days, add new secret to appReg
             *      2a. update snowmageddon secret to new version
             *          2a.1 - update correct tags (client-id and target-kv-name)
             *      2b. expire any snowmageddon secrets expired versions
             *      2c. update customer-kv with new secret
             *          2c.1 - update correct tags (client-id)
             *      
             *      
             * 3. else done
             * 
             * 
             */

            var keyVaultSecretProps = await keyVaultService.GetSecretPropertiesByTagAsync(_spOptions.CustomServicePrincipalKeyVaultName, 
                _spOptions.ClientIdTag, logPrefix);


            //keyVaultSecretProps.Where(x => x.Tags.ContainsKey)
            ////var approvedClientId = string.Empty;
            //foreach (var kvSecretProp in keyVaultSecretProps)
            //{
            //    var clientId = kvSecretProp.Tags.First(x => x.Key == _spOptions.ClientIdTag);

            //    var spApp = allServicePrincs.FirstOrDefault(x => x.AppId == clientId.Value);
            //    if (spApp is null)
            //    {
            //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(ManageSecretsAsync)}:" +
            //            $"### WARNING ### - Unable to find Service Principal from KeyVault Secret Name: {kvSecretProp.Name}");
            //        continue;
            //    }
            //}

            //    var appRegistration = await entraIdService.GetAppRegistrationAsync(appId, logPrefix);




            //var datePattern = @"M/d/yyyy hh:mm:ss tt";
            //var kvTags = new Dictionary<string, string>
            //{
            //    { "client-id", appId },
            //    { "krs-added", DateTime.UtcNow.ToString(datePattern)},
            //    { "target-kv-name", keyVaultDto.CustomerKeyVaultName}
            //};

            //// #######################################
            //// ### Create Service Principal Secret ###
            //await entraIdService.CreateApplicationSecretAsync(entraIdDto, keyVaultDto.ManagedIdentityClientId, logPrefix, isTest);

            //// #######################################################
            //// ### Add Principal Secret to Snowmageddon Key Vault  ###
            //await keyVaultService.UpsertSecretAsync(entraIdDto, keyVaultDto, entraIdDto.ServicePrincipal.DisplayName, logPrefix, isTest);

            //// ###################################################
            //// ### Add Principal Secret to Customer Key Vault  ###
            //keyVaultDto.Uri = keyVaultDto.CustomerKeyVaultName;
            //await keyVaultService.UpsertSecretAsync(entraIdDto, keyVaultDto, entraIdDto.ServicePrincipal.DisplayName, logPrefix, isTest);
        }
        catch (Exception ex)
        {
            telemetryClient.TrackTrace($"{logPrefix}|{nameof(ManageSecretsAsync)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }
}