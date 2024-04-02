variable "sn_tags" {  
  type        = map(string)
  default = {}
  description = "service now tags that map to service now fields"
}

variable "TF_APP_PLAN_NAME" {
  type = string
  description = "The name of app plan"
}