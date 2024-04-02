using AzRbFuncs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServicePrincipalManager.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddScoped<EntraIdService>();
        services.AddScoped<KeyVaultService>();
        services.AddScoped<ServicePrincipalService>();

        services.AddOptions<ServicePrincipalMgrOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("AzServPrincipalOptions").Bind(settings);
        });
    })
    .Build();

host.Run();
