using Ardalis.GuardClauses;
using AzRbFuncs.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using ServicePrincipalManager.Services;

namespace ServicePrincipalManager;

public class ServicePrincipalMgrFunc(IConfiguration config,
    TelemetryClient telemetryClient, KeyVaultService keyVaultService,
    ServicePrincipalService spService, EntraIdService entraService,
    IOptions<ServicePrincipalMgrOptions> options)
{
    private readonly ServicePrincipalMgrOptions _spOptions = options.Value;

    [Function(nameof(RotateSnowflakeSingle))]
    public async Task<IActionResult> RotateSnowflakeSingle(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "keyrotation/snowflake/single")] HttpRequest req)
    {
        if (string.IsNullOrEmpty(req.Query["id"]))
            return new BadRequestResult();

        var logPrefix = $"KRS_Snowflake_Single|{nameof(RotateSnowflakeSingle)} -";

        telemetryClient.TrackTrace($"{logPrefix} |#|-|#| Requested |#|-|#|");


        var spAppReg = await entraService.GetAppRegistrationAsync(req.Query["id"], logPrefix);

        // Get Snowmageddon KV secret tags
        var kvSecret = await keyVaultService.GetSecretAsync(keyVaultDto, servPrincDto.ServicePrincipal.DisplayName, logPrefix);
        Guard.Against.NullOrEmpty(kvSecret.Value);

        var clientIdTag = kvSecret.Properties.Tags.FirstOrDefault(x => x.Key == config["ClientIdTag"]);
        Guard.Against.NullOrEmpty(clientIdTag.Value, nameof(clientIdTag));

        if (servPrincDto.ServicePrincipal.AppId != clientIdTag.Value)
        {
            telemetryClient.TrackTrace($"{logPrefix} BadRequest - App ID do not match: AppId: {req.Query["id"]}");
            return new BadRequestResult();
        }

        var customerKeyVaultNameTag = kvSecret.Properties.Tags.FirstOrDefault(x => x.Key == config["CustomerKeyVaultNameTag"]);
        Guard.Against.NullOrEmpty(customerKeyVaultNameTag.Value, nameof(customerKeyVaultNameTag));

        keyVaultDto.CustomerKeyVaultName = customerKeyVaultNameTag.Value;

        await spService.CreateSecretAsync(servPrincDto, keyVaultDto, logPrefix, isTest);

        return new OkObjectResult($"{logPrefix}|{nameof(RotateSnowflakeSingle)} - Complete");
    }
}