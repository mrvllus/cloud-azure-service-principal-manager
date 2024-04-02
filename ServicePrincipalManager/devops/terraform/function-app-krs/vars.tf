variable "sn_tags" {  
  type = map(string)
  default = {}
}

variable "TF_AZ_APP_INSIGHTS_LOG_ANALYTICS_NAME" {
  type = string
}

variable "TF_AZ_APP_SERVICE_PLAN_NAME" {
  type = string
}

variable "TF_AZ_FUNC_NAME" {
  type = string
}

variable "TF_AZ_LOG_SUBSCRIPTION_ID" {
  type = string
}

variable "TF_AZ_REGION_NAME" {
  type = string
}

variable "TF_AZ_RESOURCE_GROUP_NAME" {
  type = string
}

variable "TF_AZ_TENANT_ID" {
  type = string
}

variable "TF_AZURECREATION_KV_NAME" {
  type = string
}

variable "TF_SNOWFLAKE_KV_NAME" {
  type = string
}

variable "TF_DEVOPS_PAT_KV_SECRET_NAME" {
  type = string
}

variable "TF_DEVOPS_API_URL" {
  type = string
}

variable "TF_IS_TEST" {
  type = string
}

variable "TF_KRS_AUTO_TAKE" {
  type = string
}

variable "TF_KRS_AUTO_BATCH" {
  type = string
}

variable "TF_REQ_STORAGE_ACCOUNT" {
  type = string
}

variable "TF_MANAGED_IDENTITY_CLIENT_ID" {
  type = string
}

variable "TF_MANAGED_IDENTITY_NAME" {
  type = string
}

variable "TF_SECRET_EXP_OFFSET" {
  type = string
}

variable "TF_KRS_SRV_PRINC_KV_SECRET_NAME" {
  type = string
}

variable "TF_KRS_SRV_PRINC_CLIENT_ID" {
  type = string
}

variable "TF_KRS_APPROVED_TAG" {
  type = string
}