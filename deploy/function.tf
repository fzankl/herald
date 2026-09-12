resource "azurerm_log_analytics_workspace" "herald" {
  name                = local.log_analytics_name
  location            = azurerm_resource_group.herald.location
  resource_group_name = azurerm_resource_group.herald.name
  sku                 = "PerGB2018"
  retention_in_days   = var.log_retention_in_days
  tags                = local.tags
}

resource "azurerm_application_insights" "herald" {
  name                = local.application_insights_name
  location            = azurerm_resource_group.herald.location
  resource_group_name = azurerm_resource_group.herald.name
  workspace_id        = azurerm_log_analytics_workspace.herald.id
  application_type    = "web"
  tags                = local.tags
}

resource "azurerm_storage_account" "herald" {
  name                = local.storage_account_name
  location            = azurerm_resource_group.herald.location
  resource_group_name = azurerm_resource_group.herald.name

  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"

  shared_access_key_enabled       = false
  https_traffic_only_enabled      = true
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false

  tags = local.tags
}

resource "azurerm_storage_container" "deployments" {
  name                  = local.deployment_container_name
  storage_account_id    = azurerm_storage_account.herald.id
  container_access_type = "private"
}

resource "azurerm_service_plan" "herald" {
  name                = local.service_plan_name
  location            = azurerm_resource_group.herald.location
  resource_group_name = azurerm_resource_group.herald.name
  os_type             = "Linux"
  sku_name            = "FC1"
  tags                = local.tags
}

resource "azurerm_function_app_flex_consumption" "herald" {
  name                = local.function_app_name
  location            = azurerm_resource_group.herald.location
  resource_group_name = azurerm_resource_group.herald.name
  service_plan_id     = azurerm_service_plan.herald.id

  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.herald.primary_blob_endpoint}${azurerm_storage_container.deployments.name}"
  storage_authentication_type = "SystemAssignedIdentity"

  runtime_name    = "dotnet-isolated"
  runtime_version = "10.0"

  maximum_instance_count = var.maximum_instance_count
  instance_memory_in_mb  = var.instance_memory_in_mb

  https_only = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.herald.connection_string
  }

  app_settings = {
    "Herald__Mode" = var.herald_mode

    "AzureWebJobsStorage__blobServiceUri"  = azurerm_storage_account.herald.primary_blob_endpoint
    "AzureWebJobsStorage__queueServiceUri" = azurerm_storage_account.herald.primary_queue_endpoint
    "AzureWebJobsStorage__tableServiceUri" = azurerm_storage_account.herald.primary_table_endpoint
    "AzureWebJobsStorage__credential"      = "managedidentity"
  }

  tags = local.tags
}

# Both role assignments set principal_type. The pipeline identity may assign roles only under an ABAC
# condition that checks the principal type in the request, and azurerm sends that type only when
# principal_type is set. Without it Azure answers 403 AuthorizationFailed.
# skip_service_principal_aad_check avoids a failure while the app identity, created moments earlier
# in the same apply, is not yet replicated in Entra ID.

# Storage Blob Data Owner:
# Documented minimum for the AzureWebJobsStorage connection and it
# also covers the deployment container, which needs Storage Blob Data Contributor.
resource "azurerm_role_assignment" "function_app_storage_blob" {
  scope                            = azurerm_storage_account.herald.id
  role_definition_name             = "Storage Blob Data Owner"
  principal_id                     = azurerm_function_app_flex_consumption.herald.identity[0].principal_id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
}

# Storage Table Data Contributor:
# Without table access the host logs warnings about not being able to persist
# diagnostic events, which are exactly the events that help when the app fails to start.
resource "azurerm_role_assignment" "function_app_storage_table" {
  scope                            = azurerm_storage_account.herald.id
  role_definition_name             = "Storage Table Data Contributor"
  principal_id                     = azurerm_function_app_flex_consumption.herald.identity[0].principal_id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
}
