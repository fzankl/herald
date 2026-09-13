output "function_app_name" {
  description = "Name of the function app, used by deploy.yml as the deployment target."
  value       = azapi_resource.function_app.name
}

output "function_app_default_hostname" {
  description = "Default hostname of the function app, used by the smoke test."
  value       = azapi_resource.function_app.output.properties.defaultHostName
}

output "resource_group_name" {
  description = "Resource group holding the function app."
  value       = azurerm_resource_group.herald.name
}

output "application_insights_name" {
  description = "Application Insights instance the host and worker traces are written to."
  value       = azurerm_application_insights.herald.name
}

output "function_app_principal_id" {
  description = "Principal ID of the function app identity, granted access to Key Vault from phase 6."
  value       = azapi_resource.function_app.identity[0].principal_id
}
