output "evt-hub-connection-string" {
  value = azurerm_eventhub_authorization_rule.tp.primary_connection_string
}

output "instrumentation_key" {
  value = azurerm_application_insights.tp.instrumentation_key
}