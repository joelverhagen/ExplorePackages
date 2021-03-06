// Parameters
param appInsightsName string
param appInsightsDailyCapGb int
param actionGroupName string
param actionGroupShortName string
param alertEmail string
param alertPrefix string
param storageAccountName string
param keyVaultName string
param deploymentContainerName string
param leaseContainerName string

param tableSasDefinitionName string

param websiteName string
param websitePlanId string = 'new'
param websitePlanName string = 'default'
param websiteAadClientId string
param websiteConfig array
@secure()
param websiteZipUrl string

param workerPlanNamePrefix string
param workerUserManagedIdentityName string
param workerNamePrefix string
@minValue(1)
param workerPlanCount int
@minValue(1)
param workerCountPerPlan int
param workerConfig array
param workerLogLevel string = 'Warning'
param workerMinInstances int
param workerSku string = 'Y1'
@secure()
param workerZipUrl string

var sakConnectionString = 'AccountName=${storageAccountName};AccountKey=${listkeys(storageAccount.id, storageAccount.apiVersion).keys[0].value};DefaultEndpointsProtocol=https;EndpointSuffix=${environment().suffixes.storage}'
var isConsumptionPlan = workerSku == 'Y1'
var isPremiumPlan = startsWith(workerSku, 'P')
var workerMaxInstances = isPremiumPlan ? 30 : 10
var workerCount = workerPlanCount * workerCountPerPlan

var sharedConfig = [
  {
    name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
    value: appInsights.properties.InstrumentationKey
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsights.properties.ConnectionString
  }
  {
    name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
    value: '~2'
  }
  {
    name: 'NuGet.Insights:LeaseContainerName'
    value: leaseContainerName
  }
  {
    name: 'NuGet.Insights:KeyVaultName'
    value: keyVaultName
  }
  {
    name: 'NuGet.Insights:StorageAccountName'
    value: storageAccountName
  }
  {
    name: 'NuGet.Insights:TableSharedAccessSignatureSecretName'
    value: '${storageAccountName}-${tableSasDefinitionName}'
  }
  {
    // See: https://github.com/projectkudu/kudu/wiki/Configurable-settings#ensure-update-site-and-update-siteconfig-to-take-effect-synchronously 
    name: 'WEBSITE_ENABLE_SYNC_UPDATE_SITE'
    value: '1'
  }
  {
    name: 'WEBSITE_RUN_FROM_PACKAGE'
    value: '1'
  }
]

// Shared resources
var storageAndKvLongName = '${deployment().name}-storage-and-kv'
var storageAndKvName = length(storageAndKvLongName) > 64 ? '${guid(deployment().name)}-storage-and-kv' : storageAndKvLongName
module storageAndKv './storage-and-kv.bicep' = {
  name: storageAndKvName
  params: {
    storageAccountName: storageAccountName
    keyVaultName: keyVaultName
    identities: [for i in range(0, workerCount + 1): {
      tenantId: i == 0 ? website.identity.tenantId : workers[i - 1].identity.tenantId
      objectId: i == 0 ? website.identity.principalId : workers[i - 1].identity.principalId
    }]
    deploymentContainerName: deploymentContainerName
    leaseContainerName: leaseContainerName
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2019-06-01' existing = {
  name: storageAccountName
}

// Application Insights and alerts
resource appInsights 'Microsoft.Insights/components@2015-05-01' = {
  name: appInsightsName
  location: resourceGroup().location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }

  // This produces a warning due to limited type definitions, but works.
  // See: https://github.com/Azure/bicep/issues/784#issuecomment-830997209
  resource billing 'CurrentBillingFeatures' = {
    name: 'Basic'
    properties: {
      CurrentBillingFeatures: 'Basic'
      DataVolumeCap: {
        Cap: appInsightsDailyCapGb
        WarningThreshold: 90
      }
    }
  }
}

resource actionGroup 'microsoft.insights/actionGroups@2019-06-01' = {
  name: actionGroupName
  location: 'Global'
  properties: empty(alertEmail) ? {
    groupShortName: actionGroupShortName
    enabled: true
  } : {
    groupShortName: actionGroupShortName
    enabled: true
    emailReceivers: [
      {
        name: 'recipient_-EmailAction-'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

resource expandDLQAlert 'microsoft.insights/metricAlerts@2018-03-01' = {
  name: '${alertPrefix}NuGet.Insights dead-letter queue "expand-poison" is not empty'
  location: 'global'
  properties: {
    description: 'The Azure Queue Storage queue "expand-poison" for NuGet.Insights deployed to resource group "${resourceGroup().name}" has at least one message in it. This may be blocking the NuGet.Insights workflow or other regular operations from continuing. Check the "expand-poison" queue in the "${storageAccount.name}" storage account to see the message or look at logs in the "${appInsights.name}" Application Insights to investigate.'
    severity: 3
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 0
          name: 'ExpandDLQMax'
          metricNamespace: 'Azure.ApplicationInsights'
          metricName: 'StorageQueueSize.Expand.Poison'
          operator: 'GreaterThan'
          timeAggregation: 'Maximum'
          criterionType: 'StaticThresholdCriterion'
          skipMetricValidation: true
        }
      ]
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
    }
    autoMitigate: true
    targetResourceType: 'microsoft.insights/components'
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

resource workDLQAlert 'microsoft.insights/metricAlerts@2018-03-01' = {
  name: '${alertPrefix}NuGet.Insights dead-letter queue "work-poison" is not empty'
  location: 'global'
  properties: {
    description: 'The Azure Queue Storage queue "work-poison" for NuGet.Insights deployed to resource group "${resourceGroup().name}" has at least one message in it. This may be blocking the NuGet.Insights workflow or other regular operations from continuing. Check the "work-poison" queue in the "${storageAccount.name}" storage account to see the message or look at logs in the "${appInsights.name}" Application Insights to investigate.'
    severity: 3
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 0
          name: 'WorkDLQMax'
          metricNamespace: 'Azure.ApplicationInsights'
          metricName: 'StorageQueueSize.Work.Poison'
          operator: 'GreaterThan'
          timeAggregation: 'Maximum'
          criterionType: 'StaticThresholdCriterion'
          skipMetricValidation: true
        }
      ]
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
    }
    autoMitigate: true
    targetResourceType: 'microsoft.insights/components'
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

resource recentWorkflowAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${alertPrefix}NuGet.Insights workflow has not completed in the past 48 hours'
  location: 'global'
  properties: {
    description: 'The NuGet.Insights workflow (catalog scan, Kusto import, etc) for NuGet.Insights deployed to resource group "${resourceGroup().name}" has not completed for at least the past 48 hours. It should complete every 24 hours. Check https://${website.properties.defaultHostName}/admin and logs in the "${appInsights.name}" Application Insights to investigate.'
    severity: 3
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 48
          name: 'HoursSinceWorkflowCompletedMax'
          metricNamespace: 'Azure.ApplicationInsights'
          metricName: 'SinceLastWorkflowCompletedHours'
          operator: 'GreaterThan'
          timeAggregation: 'Maximum'
          criterionType: 'StaticThresholdCriterion'
          skipMetricValidation: true
        }
      ]
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
    }
    autoMitigate: true
    targetResourceType: 'microsoft.insights/components'
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Website
resource websitePlan 'Microsoft.Web/serverfarms@2020-09-01' = if (websitePlanId == 'new') {
  name: websitePlanName == 'default' ? '${websiteName}-WebsitePlan' : websitePlanName
  location: resourceGroup().location
  sku: {
    name: 'B1'
  }
}

resource website 'Microsoft.Web/sites@2020-09-01' = {
  name: websiteName
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: websitePlanId == 'new' ? websitePlan.id : websitePlanId
    clientAffinityEnabled: false
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v5.0'
      appSettings: concat([
        {
          name: 'AzureAd:Instance'
          value: environment().authentication.loginEndpoint
        }
        {
          name: 'AzureAd:ClientId'
          value: websiteAadClientId
        }
        {
          name: 'AzureAd:TenantId'
          value: 'common'
        }
      ], sharedConfig, websiteConfig)
    }
  }

  resource deploy 'extensions' = {
    name: any('ZipDeploy') // Workaround per: https://github.com/Azure/bicep/issues/784#issuecomment-817260643
    properties: {
      packageUri: websiteZipUrl
    }
  }
}

// Workers
resource workerPlans 'Microsoft.Web/serverfarms@2020-09-01' = [for i in range(0, workerPlanCount): {
  name: '${workerPlanNamePrefix}${i}'
  location: resourceGroup().location
  sku: {
    name: workerSku
  }
}]

resource workerPlanAutoScale 'microsoft.insights/autoscalesettings@2015-04-01' = [for i in range(0, workerPlanCount): if (!isConsumptionPlan) {
  name: '${workerPlanNamePrefix}${i}'
  location: resourceGroup().location
  dependsOn: [
    workerPlans[i]
  ]
  properties: {
    enabled: true
    targetResourceUri: workerPlans[i].id
    profiles: [
      {
        name: 'Scale based on CPU'
        capacity: {
          default: string(workerMinInstances)
          minimum: string(workerMinInstances)
          maximum: string(workerMaxInstances)
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricNamespace: 'microsoft.web/serverfarms'
              metricResourceUri: workerPlans[i].id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 15
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '5'
              cooldown: 'PT3M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricNamespace: 'microsoft.web/serverfarms'
              metricResourceUri: workerPlans[i].id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 5
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '5'
              cooldown: 'PT1M'
            }
          }
        ]
      }
    ]
  }
}]

var workerConfigWithStorage = concat(workerConfig, isConsumptionPlan ? [
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    // SAS-based connection strings don't work for this property
    value: sakConnectionString
  }
] : [])

resource workerUserManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: workerUserManagedIdentityName
  location: resourceGroup().location
}

resource workers 'Microsoft.Web/sites@2020-09-01' = [for i in range(0, workerCount): {
  name: '${workerNamePrefix}${i}'
  location: resourceGroup().location
  dependsOn: [
    workerUserManagedIdentity
  ]
  kind: 'FunctionApp'
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${workerUserManagedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: workerPlans[i / workerCountPerPlan].id
    clientAffinityEnabled: false
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: !isConsumptionPlan
      appSettings: concat([
        {
          name: 'AzureFunctionsJobHost__logging__LogLevel__Default'
          value: workerLogLevel
        }
        {
          name: 'AzureWebJobsFeatureFlags'
          value: 'EnableEnhancedScopes'
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'NuGet.Insights:UserManagedIdentityClientId'
          value: workerUserManagedIdentity.properties.clientId
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
        {
          name: 'StorageConnection__queueServiceUri'
          value: storageAccount.properties.primaryEndpoints.queue
        }
      ], sharedConfig, workerConfigWithStorage)
    }
  }
}]

resource blobPermissions 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = [for i in range(0, workerCount + 1): {
  name: guid('FunctionsCanAccessBlob-${i == 0 ? website.id : workers[max(0, i - 1)].id}')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: i == 0 ? website.identity.principalId : workers[max(0, i - 1)].identity.principalId
    principalType: 'ServicePrincipal'
  }
}]

resource queuePermissions 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = [for i in range(0, workerCount + 1): {
  name: guid('FunctionsCanAccessQueue-${i == 0 ? website.id : workers[max(0, i - 1)].id}')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: i == 0 ? website.identity.principalId : workers[max(0, i - 1)].identity.principalId
    principalType: 'ServicePrincipal'
  }
}]

resource workerDeployments 'Microsoft.Web/sites/extensions@2020-09-01' = [for i in range(0, workerCount): {
  name: 'ZipDeploy'
  parent: workers[i]
  properties: {
    packageUri: workerZipUrl
  }
}]
