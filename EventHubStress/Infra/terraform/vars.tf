variable "region" {
  type = string
  default = "eastus"
}

variable "imgVersion" {
  type = string
  default = "latest"
}

variable "rgname" {
  type = string
  default = "EhThroughputTest"
}

variable "ehnsname" {
  type = string
  default = "EhThroughputTest"
}

variable "zoneredundant" {
  type = bool
  default = "true"
}

variable "containers" {
  type        = list(string)
  default     = ["ehstress1", "ehstress2"]
}
