using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace ServicePrincipalManager.Services;

public class EntraIdService(TelemetryClient telemetryClient, IOptions<ServicePrincipalMgrOptions> options)
{
    private readonly ServicePrincipalMgrOptions _spOptions = options.Value;
    private GraphServiceClient _msGraphClient;
    //public async Task<List<Application>> GetAllApplicationsAsync(string logPrefix)
    //{
    //    try
    //    {
    //        CreateGraphCreds(servPrincDto, managedIdentityClientId);
    //        var applications = await _msGraphClient.Applications.GetAsync();

    //        var allAllApplications = new List<Application>();

    //        var pageIterator = PageIterator<Application, ApplicationCollectionResponse>.CreatePageIterator(
    //            _msGraphClient,
    //            applications,

    //            // Callback executed for each item in
    //            // the collection
    //            (m) =>
    //            {
    //                allAllApplications.Add(m);
    //                return true;
    //            }
    //            );

    //        await pageIterator.IterateAsync();

    //        return allAllApplications;
    //    }
    //    catch (Exception ex)
    //    {
    //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(GetAllApplicationsAsync)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
    //        throw;
    //    }
    //}

    //public async Task<PasswordCredential> CreateAppRegistrationSecretAsync(Application app, string logPrefix)
    //{
    //    try
    //    {
    //        var requestBody = new Microsoft.Graph.Applications.Item.AddPassword.AddPasswordPostRequestBody
    //        {
    //            PasswordCredential = new PasswordCredential
    //            {
    //                DisplayName = $"{app.DisplayName}-secret-key",
    //                EndDateTime = DateTime.UtcNow.AddDays(double.Parse(_spOptions.SecretExpOffset)),
    //                StartDateTime = DateTime.UtcNow,
    //            }
    //        };

    //        var isTest = bool.Parse(_spOptions.IsTest);
    //        if (!isTest)
    //        {
    //            CreateGraphCreds();
    //            var newPwdCred = await _msGraphClient.Applications[$"{app.AppId}"].AddPassword.PostAsync(requestBody);
    //            Guard.Against.NullOrEmpty(newPwdCred.KeyId, nameof(newPwdCred.KeyId));

    //            telemetryClient.TrackTrace($"{logPrefix}|{nameof(CreateAppRegistrationSecretAsync)} " +
    //                $"Displayname: {app.DisplayName}, Hint({newPwdCred.Hint}), " +
    //                $"Id:{newPwdCred.KeyId}");

    //            return newPwdCred;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(CreateAppRegistrationSecretAsync)} " +
    //            $"ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
    //        throw;
    //    }
    //}

    public async Task<Application> GetAppRegistrationAsync(string appId, string logPrefix)
    {
        try
        {
            CreateGraphCreds();

            var result = await _msGraphClient.Applications.GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Filter = $"appId eq '{appId}'";
                requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
            });

            if (result == null || result.Value == null)
                throw new NotFoundException(appId, nameof(GetAppRegistrationAsync));

            return result.Value.FirstOrDefault();
        }
        catch (Exception ex)
        {
            telemetryClient.TrackTrace($"{logPrefix}|{nameof(GetAppRegistrationAsync)}: ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            throw;
        }
    }

    //public async Task RemoveExpiredSecretsAsync(ServicePrincipalDto servPrincDto, string managedIdentityClientId, string logPrefix, bool isTest = true)
    //{
    //    Guard.Against.NullOrEmpty(servPrincDto.ServicePrincipal.AppId, nameof(servPrincDto.ServicePrincipal.AppId));
    //    try
    //    {
    //        foreach (PasswordCredential credential in servPrincDto.ServicePrincipal.PasswordCredentials)
    //        {
    //            if (credential.KeyId != servPrincDto.NewPasswordCred.KeyId)
    //                await DeleteApplicationSecretsAsync(servPrincDto.ServicePrincipal, credential.KeyId, servPrincDto, managedIdentityClientId, logPrefix, isTest);

    //            servPrincDto.ServicePrincipal = await GetApplicationByClientIdAsync(servPrincDto, managedIdentityClientId, servPrincDto.ServicePrincipal.AppId, logPrefix);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(RemoveExpiredSecretsAsync)} " +
    //            $"ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
    //    }
    //}
    private void CreateGraphCreds()
    {
        if (_msGraphClient is not null)
            return;

        // The client credentials flow requires that you request the
        // /.default scope, and preconfigure your permissions on the
        // app registration in Azure. An administrator must grant consent
        // to those permissions beforehand.
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        //_msGraphClient = new GraphServiceClient(clientSecretCredential, scopes);

        // https://learn.microsoft.com/en-us/dotnet/api/azure.identity.managedidentitycredential.-ctor?view=azure-dotnet#azure-identity-managedidentitycredential-ctor(system-string-azure-identity-tokencredentialoptions)
        // Remember to set your Envirment var for Azure_Tenant_ID
        var miCred = new ManagedIdentityCredential(_spOptions.ManagedIdentityClientId, new TokenCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud });

        var devCredOption = new DefaultAzureCredentialOptions { VisualStudioTenantId = _spOptions.TenantId };
        var credential = new ChainedTokenCredential(miCred, new DefaultAzureCredential(devCredOption));

        _msGraphClient = new GraphServiceClient(credential, scopes);
    }

    //private async Task DeleteApplicationSecretsAsync(Application application, Guid? removePwdKeyId, ServicePrincipalDto servPrincDto, string managedIdentityClientId, string logPrefix, bool isTest = true)
    //{
    //    Guard.Against.Null(application, nameof(application));
    //    Guard.Against.NullOrEmpty(application.Id, nameof(application.Id));
    //    Guard.Against.Null(removePwdKeyId, nameof(removePwdKeyId));
    //    Guard.Against.NullOrEmpty(removePwdKeyId, nameof(removePwdKeyId));

    //    try
    //    {
    //        var removePwd = application.PasswordCredentials.First(x => x.KeyId == removePwdKeyId);

    //        var requestBody = new RemovePasswordPostRequestBody
    //        {
    //            KeyId = removePwd.KeyId
    //        };

    //        if (!isTest)
    //        {
    //            CreateGraphCreds(servPrincDto, managedIdentityClientId);
    //            telemetryClient.TrackTrace($"{logPrefix}|{nameof(DeleteApplicationSecretsAsync)}: Hint:{removePwd.Hint}, Id: {removePwd.KeyId}");
    //            await _msGraphClient.Applications[application.Id].RemovePassword.PostAsync(requestBody);
    //        }
    //        else
    //        {
    //            telemetryClient.TrackTrace($"{logPrefix}|{nameof(DeleteApplicationSecretsAsync)} (LOG ONLY) Hint:{removePwd.Hint}, Id: {removePwd.KeyId}");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        telemetryClient.TrackTrace($"{logPrefix}|{nameof(DeleteApplicationSecretsAsync)} ERROR - " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
    //        throw;
    //    }
    //}
}