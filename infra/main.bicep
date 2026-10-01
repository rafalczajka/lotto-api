//*************************************
// Parameters
//*************************************

@description('A unique token used for resource name generation.')
@minLength(3)
@maxLength(21)
param resourceToken string = toLower(uniqueString(subscription().id, location))

@description('A unique token used for resource name generation without dashes.')
@minLength(1)
param resourceTokenWithoutDashes string = replace(resourceToken, '-', '')

@description('Name of the secret in Key Vault storing Lotto API key.')
param lottoApiKeySecretName string

@description('Base URL for the original Lotto API.')
param lottoBaseUrl string

@description('GitHub Service Principal Object ID.')
param githubSpObjectId string

@description('Data sync schedule in cron format.')
param dataSyncSchedule string

@description('Time zone for scheduling data sync.')
param timeZone string

@description('Name of the Azure Table to store draw results.')
param drawResultsTableName string

@description('List of function names to be disabled in the Function App.')
param disabledFunctions array

@description('Primary region for all Azure resources.')
@minLength(1)
param location string = resourceGroup().location


//*************************************
// Resources
//*************************************

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2026-03-01' = {
  name: 'log-${resourceToken}'
  location: location
  properties: any({
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  })
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceToken}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2026-04-01' = {
  name: 'st${resourceTokenWithoutDashes}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource tableServices 'Microsoft.Storage/storageAccounts/tableServices@2026-04-01' = {
  name: 'default'
  parent: storageAccount

  resource drawResultsTable 'tables' = {
    name: drawResultsTableName
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2026-04-01' = {
  name: 'default'
  parent: storageAccount
}

resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2026-04-01' = {
  name: 'default'
  parent: storageAccount
}

resource queueServices 'Microsoft.Storage/storageAccounts/queueServices@2026-04-01' = {
  name: 'default'
  parent: storageAccount
}

resource appServicePlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: 'asp-${resourceToken}'
  kind: 'functionapp'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2026-02-01' = {
  name: 'kv-${resourceToken}'
  location: location
  properties: {
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: false
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: githubSpObjectId
        permissions: {
          secrets: ['set', 'get', 'list']
        }
      }
    ]
  }

  resource lottoApiKeySecret 'secrets' = {
    name: lottoApiKeySecretName
    properties: {
      value: 'PLACEHOLDER' // Set through GitHub Actions
    }
  }
}

var disabledFunctionsSettings = [for f in disabledFunctions: {
  name: 'AzureWebJobs.${f}.Disabled'
  value: 'true'
}]

resource functionApp 'Microsoft.Web/sites@2025-03-01' = {
  name: 'func-${resourceToken}'
  kind: 'functionapp'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
        supportCredentials: true
      }
      appSettings: concat([
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsights.properties.InstrumentationKey}'
        }
        {
          name: 'WEBSITE_TIME_ZONE'
          value: timeZone
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'DataSyncSchedule'
          value: dataSyncSchedule
        }
        {
          name: 'LottoBaseUrl'
          value: lottoBaseUrl
        }
        {
          name: 'LottoApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${lottoApiKeySecretName})'
        }
        {
          name: 'TableNames__DrawResults'
          value: drawResultsTableName
        }
      ], disabledFunctionsSettings)
    }
    httpsOnly: true
  }
}

resource functionAppMcp 'Microsoft.Web/sites@2025-03-01' = {
  name: 'func-${resourceToken}-mcp'
  kind: 'functionapp'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsights.properties.InstrumentationKey}'
        }
        {
          name: 'WEBSITE_TIME_ZONE'
          value: timeZone
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
      ]
    }
    httpsOnly: true
  }
}

resource keyVaultAccessPolicies 'Microsoft.KeyVault/vaults/accessPolicies@2026-02-01' = {
  name: 'add'
  parent: keyVault
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
        permissions: {
          secrets: ['get']
        }
      }
    ]
  }
}

var defaultStorageDiagnosticSettings = {
  workspaceId: logAnalytics.id
  logs: [
    {
      categoryGroup: 'allLogs'
      enabled: true
    }
  ]
  metrics: [
    {
      category: 'Transaction'
      enabled: true
    }
  ]
}

resource tableStorageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'table-storage-logs'
  scope: tableServices
  properties: defaultStorageDiagnosticSettings
}

resource blobStorageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'blob-storage-logs'
  scope: blobServices
  properties: defaultStorageDiagnosticSettings
}

resource queueStorageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'queue-storage-logs'
  scope: queueServices
  properties: defaultStorageDiagnosticSettings
}

resource fileStorageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'file-storage-logs'
  scope: fileServices
  properties: defaultStorageDiagnosticSettings
}

resource keyVaultDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'key-vault-logs'
  scope: keyVault
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}


//*************************************
// Outputs
//*************************************

output appName string = functionApp.name
output keyVaultName string = keyVault.name
output secretName string = lottoApiKeySecretName
