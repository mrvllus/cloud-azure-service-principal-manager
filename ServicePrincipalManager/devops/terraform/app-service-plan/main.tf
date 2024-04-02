module "app_service_plan_windows" {
  source  = "app.terraform.io/ICS/app-service-plan/azure"
  version = "~>1.0.1"
  
  app_service_plan_name = var.TF_APP_PLAN_NAME
  sn_tags               = var.sn_tags
  region_name			= "eastus"
}

provider "azurerm" {
  features {}
}

terraform {
  backend "azurerm" {
    #Please leave this empty
  }
}