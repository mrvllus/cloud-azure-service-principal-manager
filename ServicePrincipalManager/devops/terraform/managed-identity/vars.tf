variable "sn_tags" {  
  type        = map(string)
  default = {}
  description = "service now tags that map to service now fields"
}

variable "TF_AZ_RESOURCE_GROUP_NAME" {
  type = string
}

variable "TF_AZ_REGION_NAME" {
  type = string
}

variable "TF_AZ_LOG_SUBSCRIPTION_ID" {
  type = string
}