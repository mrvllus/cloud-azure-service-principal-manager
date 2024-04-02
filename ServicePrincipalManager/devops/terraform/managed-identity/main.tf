# this isn't in a module yet
resource "azurerm_user_assigned_identity" "mgd_id" {
  name                = "krs-${random_string.myrandom.id}-mi"
  location            = var.TF_AZ_REGION_NAME
  resource_group_name = var.TF_AZ_RESOURCE_GROUP_NAME
  tags                = var.sn_tags
}

provider "azurerm" {
  features {}
}

provider "azurerm" {
  alias				= "log_subscription"
  subscription_id	= var.TF_AZ_LOG_SUBSCRIPTION_ID  // ics-beta:"3097a4ad-a247-4769-8328-0982ed928431"  global:"b0d46910-956d-4f5e-afe5-c79784ac5af5"
  features {}
}

terraform {
  backend "azurerm" {
  #Please leave this empty
  }
}

resource "random_string" "myrandom" {
  length = 6
  upper = false 
  special = false
  number = false   
}