resource "azurerm_resource_group" "herald" {
  name     = local.resource_group_name
  location = var.location
  tags     = local.tags
}
