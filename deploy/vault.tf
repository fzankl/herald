data "azurerm_client_config" "current" {}

# The access token of the content repository lives here and nowhere else. Terraform creates the
# vault and the access to it and never the secret value: a value in the configuration would end up
# in the state, which is the same reason shared keys are disabled on the storage account. The value
# is set once with az, and the app reads it through the key vault reference in function.tf.
resource "azurerm_key_vault" "herald" {
  name                = local.key_vault_name
  location            = var.location
  resource_group_name = azurerm_resource_group.herald.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  rbac_authorization_enabled = true

  # Off on purpose: purge protection cannot be switched back off, it blocks terraform destroy for
  # the whole retention period and keeps the globally unique name reserved. A token can be written
  # again, so the protection buys nothing that is worth losing a destroy over.
  purge_protection_enabled   = false
  soft_delete_retention_days = 7

  tags = local.tags
}

# Key Vault Secrets User reads secret values and nothing else. It is the data plane role: the
# pipeline identity holds Contributor, which creates the vault but never reads from it.
#
# This assignment is the reason scripts/bootstrap.ps1 lists a third role definition id in the
# condition on the pipeline identity. Without it Azure answers 403 and says nothing about why.
resource "azurerm_role_assignment" "function_app_key_vault_secrets" {
  scope                            = azurerm_key_vault.herald.id
  role_definition_name             = "Key Vault Secrets User"
  principal_id                     = azapi_resource.function_app.identity[0].principal_id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
}
