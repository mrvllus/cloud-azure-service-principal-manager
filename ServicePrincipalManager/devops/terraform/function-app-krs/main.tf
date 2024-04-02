module "krs_func_app_module" {
  source = "app.terraform.io/ICS/function-app-linux/azure"
  version = "~>1.0"

  function_app_name = var.TF_AZ_FUNC_NAME
  app_service_plan_name = var.TF_AZ_APP_SERVICE_PLAN_NAME
  sn_tags = var.sn_tags
  upgrade_from_old_module = false

  add_to_vnet = true
  create_app_insights = true

  region_name = "eastus"

  storage_account = {
    name            = "${var.TF_REQ_STORAGE_ACCOUNT}"
    type            = "ExistingStandalone"
    resource_group  = "${var.TF_AZ_RESOURCE_GROUP_NAME}"
  }

  user_assigned_identities = [{
    "managed_identity_name"   = "${var.TF_MANAGED_IDENTITY_NAME}",
    "resource_group_name"     = "${var.TF_AZ_RESOURCE_GROUP_NAME}"
  }]
  
  app_settings = { 
    "AzureTenantId" = "${var.TF_AZ_TENANT_ID}",
    "KrsApprovedTag" = "${var.TF_KRS_APPROVED_TAG}",
    "IsTest": "${var.TF_IS_TEST}",
    "SecretExpOffset": "${var.TF_SECRET_EXP_OFFSET}",
    "ManagedIdentityClientId": "${var.TF_MANAGED_IDENTITY_CLIENT_ID}",
    "KrsServicePrincipalClientId": "${var.TF_KRS_SRV_PRINC_CLIENT_ID}",
    "KrsServicePrincipalName": "${var.TF_KRS_SRV_PRINC_KV_SECRET_NAME}",
    "DevOpsApiUrl": "${var.TF_DEVOPS_API_URL}",
    "DevOpsPatKvSecretName": "${var.TF_DEVOPS_PAT_KV_SECRET_NAME}",
    "KrsAutoTake": "${var.TF_KRS_AUTO_TAKE}",
    "KrsAutoBatchSize": "${var.TF_KRS_AUTO_BATCH}",
    "AzureCreationKeyVaultName": "${var.TF_AZURECREATION_KV_NAME}",
    "SnowflakeKeyVaultName": "${var.TF_SNOWFLAKE_KV_NAME}",
  }

  providers = {
	azurerm = azurerm
	azurerm.log_subscription = azurerm.log_subscription
  }
}

provider "azurerm" {
  features {}
}

provider "azurerm" {
  alias				= "log_subscription"
  subscription_id = var.TF_AZ_LOG_SUBSCRIPTION_ID  #beta "3097a4ad-a247-4769-8328-0982ed928431" #global "b0d46910-956d-4f5e-afe5-c79784ac5af5"
  //todo removed once fix is out
  skip_provider_registration = true
  features {}
}

terraform {
  backend "azurerm" {
    #Please leave this empty
  }
}

# Random String Resource
resource "random_string" "myrandom" {
  length = 6
  upper = false 
  special = false
  number = false   
}