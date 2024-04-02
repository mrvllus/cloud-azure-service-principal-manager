namespace AzRbFuncs.Models;

public class ServicePrincipalMgrOptions
{
    public string AzureCreationKeyVaultName { get; set; }
    
    public string ClientIdTag { get; set; }
    
    public string CustomerKeyVaultNameTag { get; set; }
    
    public string CustomServicePrincipalKeyVaultName { get; set; }
    
    public string IsTest { get; set; }
    
    public string ManagedIdentityClientId { get; set; }
 
    public string SecretExpOffset { get; set; } = "90";

    public string TenantId { get; set; } = "9f914513-dda8-4d64-98a0-2d902b23352f"; // default BETA Tenant
}