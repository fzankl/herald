variable "workload" {
  description = "Workload segment of every resource name."
  type        = string
  default     = "herald"
}

variable "environment" {
  description = "Stage segment of every resource name. dev and prd are the two stages. There is deliberately no default: the stage also selects the state file, so every plan has to name the one it means."
  type        = string

  validation {
    condition     = contains(["dev", "prd"], var.environment)
    error_message = "environment must be either dev or prd."
  }
}

variable "location" {
  description = "Azure region every resource is created in."
  type        = string
  default     = "germanywestcentral"
}

variable "location_short" {
  description = "Short form of the region used in resource names."
  type        = string
  default     = "gwc"
}

variable "instance" {
  description = "Instance segment of every resource name."
  type        = string
  default     = "001"
}

variable "app_version" {
  description = "Release version from build/Directory.Build.props, written to the version tag of every resource."
  type        = string

  validation {
    condition     = can(regex("^[0-9]+\\.[0-9]+\\.[0-9]+", var.app_version))
    error_message = "app_version must start with a semantic version such as 0.1.0."
  }
}

variable "herald_mode" {
  description = "Value of the Herald__Mode application setting. Dry publishes nothing."
  type        = string
  default     = "Dry"

  validation {
    condition     = contains(["Dry", "Live"], var.herald_mode)
    error_message = "herald_mode must be either Dry or Live."
  }
}

variable "maximum_instance_count" {
  description = "Upper bound for on-demand instances per scale group. Flex Consumption allows 1 to 1000."
  type        = number
  default     = 40

  validation {
    condition     = var.maximum_instance_count >= 1 && var.maximum_instance_count <= 1000
    error_message = "maximum_instance_count must be between 1 and 1000."
  }
}

variable "instance_memory_in_mb" {
  description = "Instance size. Flex Consumption offers 512, 2048 and 4096 MB."
  type        = number
  default     = 2048

  validation {
    condition     = contains([512, 2048, 4096], var.instance_memory_in_mb)
    error_message = "instance_memory_in_mb must be 512, 2048 or 4096."
  }
}

variable "log_retention_in_days" {
  description = "Retention of the Log Analytics workspace. The PerGB2018 SKU accepts 30 to 730 days."
  type        = number
  default     = 30

  validation {
    condition     = var.log_retention_in_days >= 30 && var.log_retention_in_days <= 730
    error_message = "log_retention_in_days must be between 30 and 730."
  }
}
