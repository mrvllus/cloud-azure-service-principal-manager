using ServicePrincipalManager.Services;

namespace ServicePrincipalManager;

public class ServicePrincipalMgrFunc(TelemetryClient telemetryClient,
    ServicePrincipalService spService, 
    EntraIdService entraService,
    IOptions<ServicePrincipalMgrOptions> options)
{
    private readonly ServicePrincipalMgrOptions _spOptions = options.Value;

    [Function(nameof(ManageSingleServicePrincipal))]
    public async Task<IActionResult> ManageSingleServicePrincipal(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "sp/single")] HttpRequest req)
    {
        if (string.IsNullOrEmpty(req.Query["id"]))
            return new BadRequestResult();

        var logPrefix = $"KRS_Snowflake_Single|{nameof(ManageSingleServicePrincipal)} -";
        telemetryClient.TrackTrace($"{logPrefix} |#|-|#| Requested |#|-|#|");

        var appRegistration = await entraService.GetAppRegistrationAsync(req.Query["id"], logPrefix);
        Guard.Against.Null(appRegistration, nameof(appRegistration));
        Guard.Against.NullOrEmpty(appRegistration.AppId, nameof(appRegistration.AppId));

        await spService.ManageSecretsAsync(appRegistration, logPrefix);

        return new OkObjectResult($"{logPrefix}|{nameof(ManageSingleServicePrincipal)} - Complete");
    }
}