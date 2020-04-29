provider "azurerm" {
  # The "feature" block is required for AzureRM provider 2.x. 
  # If you are using version 1.x, the "features" block is not allowed.
  version = "~>2.0"
  features {}
}

resource "azurerm_resource_group" "rg" {
  name = var.rgname
  location = var.region
}

resource "azurerm_template_deployment" "rg" {
  name                = var.ehnsname
  resource_group_name = azurerm_resource_group.rg.name
  
  template_body   = <<DEPLOY
  {
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
    },
    "variables": {
    },
    "resources": [
      {
        "name": "${var.ehnsname}",
        "type": "Microsoft.EventHub/namespaces",
        "apiVersion": "2018-01-01-preview",
        "location": "${azurerm_resource_group.rg.location}",
        "tags": {},
        "sku": {
          "name": "Standard",
          "capacity": "4"
        },
        "properties": {
          "isAutoInflateEnabled": "false",
          "maximumThroughputUnits": "0",
          "kafkaEnabled": "true",
          "zoneRedundant": "${var.zoneredundant}"
        },
        "resources": []
      }
    ]
  }
DEPLOY
  deployment_mode = "Incremental"
}

resource "azurerm_eventhub" "tp" {
  depends_on          = [azurerm_template_deployment.rg]
  name                = "ThroughputTestEventHub"
  namespace_name      = var.ehnsname
  resource_group_name = azurerm_resource_group.rg.name
  partition_count     = 4
  message_retention   = 1
}

resource "azurerm_eventhub_authorization_rule" "tp" {
  name                = "managewrite"
  namespace_name      = var.ehnsname
  eventhub_name       = azurerm_eventhub.tp.name
  resource_group_name = azurerm_resource_group.rg.name
  listen              = true
  send                = true
  manage              = true
}

resource "azurerm_application_insights" "tp" {
  name                = "ThroughputAppInsights"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

resource "azurerm_container_group" "aci-containers" {
  for_each = toset(var.containers)

  name                = each.key
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  ip_address_type     = "public"
  os_type             = "linux"

  container {
    name   = each.key
    image  = "pschuler/eventhubstress:${var.imgVersion}"
    cpu    ="2.0"
    memory =  "1.5"

    ports {
      port     = 80
      protocol = "TCP"
    }

    environment_variables = {
      "ApplicationInsights__InstrumentationKey"="${azurerm_application_insights.tp.instrumentation_key}",
      "EventHub__ConnectionString"="${azurerm_eventhub_authorization_rule.tp.primary_connection_string}",
      "GCDisableSustainedLatency"="true"
    }
  }
}
