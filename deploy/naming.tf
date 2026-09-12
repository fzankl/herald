locals {
  # <abbreviation>-<workload>-<stage>-<region>-<instance>, per the Azure Cloud Adoption Framework
  # abbreviation list. dev and prd differ in nothing but var.environment, so every name below is
  # distinct per stage and the two stages can live in the same subscription.
  suffix = "${var.workload}-${var.environment}-${var.location_short}-${var.instance}"

  # Storage account names allow only lower-case letters and digits, and are limited to 24 characters.
  suffix_compact = "${var.workload}${var.environment}${var.location_short}${var.instance}"

  resource_group_name       = "rg-${local.suffix}"
  service_plan_name         = "asp-${local.suffix}"
  function_app_name         = "func-${local.suffix}"
  application_insights_name = "appi-${local.suffix}"
  log_analytics_name        = "log-${local.suffix}"
  storage_account_name      = "st${local.suffix_compact}"

  deployment_container_name = "deployments"

  tags = {
    workload    = var.workload
    environment = var.environment
    tool        = "terraform"
    version     = var.revision
  }
}
