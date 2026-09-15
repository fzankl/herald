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

# The function app is an azapi_resource and not azurerm_function_app_flex_consumption. That resource
# writes AzureWebJobsStorage and DEPLOYMENT_STORAGE_CONNECTION_STRING as connection strings with an
# empty account key into the app settings, even with identity-based storage, and the host then takes
# the connection string and cannot read its own storage. With the ARM resource the app settings below
# are the complete list. Provider issue, open as of azurerm 5.5.0:
# https://github.com/hashicorp/terraform-provider-azurerm/issues/29149
#
# AzureWebJobsStorage__accountName is enough for the default storage DNS suffix. Azure/functions-action
# also looks for exactly that setting and warns when it is missing.
resource "azapi_resource" "function_app" {
  type      = "Microsoft.Web/sites@2025-03-01"
  name      = local.function_app_name
  parent_id = azurerm_resource_group.herald.id
  location  = azurerm_resource_group.herald.location
  tags      = local.tags

  # identity_ids = [] matches what the state holds. azapi keeps principal_id from the state only when
  # type and identity_ids both equal it. Left null, principal_id becomes unknown on every update, and
  # both role assignments below would be replaced on every apply.
  identity {
    type         = "SystemAssigned"
    identity_ids = []
  }

  body = {
    kind = "functionapp,linux"
    properties = {
      # ARM returns "serverfarms", azurerm "serverFarms". Without the replace every plan shows a change.
      serverFarmId = replace(azurerm_service_plan.herald.id, "Microsoft.Web/serverFarms", "Microsoft.Web/serverfarms")
      httpsOnly    = true

      functionAppConfig = {
        deployment = {
          storage = {
            type  = "blobContainer"
            value = "${azurerm_storage_account.herald.primary_blob_endpoint}${azurerm_storage_container.deployments.name}"
            authentication = {
              type = "SystemAssignedIdentity"
            }
          }
        }
        runtime = {
          name    = "dotnet-isolated"
          version = "10.0"
        }
        scaleAndConcurrency = {
          maximumInstanceCount = var.maximum_instance_count
          instanceMemoryMB     = var.instance_memory_in_mb
        }
      }

      siteConfig = {
        appSettings = [
          { name = "APPLICATIONINSIGHTS_CONNECTION_STRING", value = azurerm_application_insights.herald.connection_string },
          { name = "AzureWebJobsStorage__accountName", value = azurerm_storage_account.herald.name },
          { name = "AzureWebJobsStorage__credential", value = "managedidentity" },
          { name = "Herald__Mode", value = var.herald_mode },
          { name = "Herald__Content__PostPattern", value = var.content_post_pattern },
          { name = "Herald__Content__TemplateFolder", value = var.content_template_folder },
        ]
      }
    }
  }

  response_export_values = ["properties.defaultHostName"]
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
  principal_id                     = azapi_resource.function_app.identity[0].principal_id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
}

# Storage Table Data Contributor:
# Without table access the host logs warnings about not being able to persist
# diagnostic events, which are exactly the events that help when the app fails to start.
resource "azurerm_role_assignment" "function_app_storage_table" {
  scope                            = azurerm_storage_account.herald.id
  role_definition_name             = "Storage Table Data Contributor"
  principal_id                     = azapi_resource.function_app.identity[0].principal_id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
}
