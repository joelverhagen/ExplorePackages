[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $true)]
    [string]$StorageAccountName,
    
    [Parameter(Mandatory = $true)]
    [string]$TableSasDefinitionName,
    
    [Parameter(Mandatory = $true)]
    [switch]$AutoRegenerateKey,

    [Parameter(Mandatory = $true)]
    [TimeSpan]$SasValidityPeriod
)

Import-Module (Join-Path $PSScriptRoot "NuGet.Insights.psm1")

# The application ID for Key Vault managed storage:
# Source: https://docs.microsoft.com/en-us/azure/key-vault/secrets/overview-storage-keys-powershell
$keyVaultSpAppId = "cfa8b339-82a2-471a-a3c9-0fc0be7a4093"

# This is the key for Azure Storage Emulator, just for creating a template SAS.
# Source: https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator
$storageEmulatorKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="

# This is how frequently the active storage key is swapped. Twice this value is how long a given storage key is value.
# We round up to the nearest 2 weeks.
$regenerationPeriod = New-TimeSpan -Days ([Math]::Ceiling($SasValidityPeriod.TotalDays / 7) * 14)

$maxRetries = 30

# Get the current user
Write-Status "Determining the current user for Key Vault operations..."
$graphToken = Get-AzAccessToken -Resource "https://graph.microsoft.com/"
$graphHeaders = @{ Authorization = "Bearer $($graphToken.Token)" }
$currentUser = Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me" -Headers $graphHeaders

Write-Status "Adding Key Vault role assignment for '$($currentUser.userPrincipalName)' (object ID $($currentUser.id))..."
$existingRoleAssignment = Get-AzRoleAssignment `
    -ResourceGroupName $ResourceGroupName `
    -RoleDefinitionName "Key Vault Administrator" `
| Where-Object { $_.ObjectId -eq $currentUser.id }
if (!$existingRoleAssignment) {
    New-AzRoleAssignment `
        -ObjectId $currentUser.id `
        -ResourceGroupName $ResourceGroupName `
        -RoleDefinitionName "Key Vault Administrator" | Out-Default
}

Write-Status "Getting the resource ID for storage account '$StorageAccountName'..."
$storageAccount = Get-AzStorageAccount `
    -ResourceGroupName $ResourceGroupName `
    -Name $StorageAccountName

Write-Status "Checking if Key Vault '$KeyVaultName' already manages storage account '$StorageAccountName'..."
$attempt = 0
while ($true) {
    try {
        $attempt++
        $matchingStorage = Get-AzKeyVaultManagedStorageAccount `
            -VaultName $KeyVaultName `
            -ErrorAction Stop `
        | Where-Object { $_.AccountResourceId -eq $storageAccount.Id }
        break
    }
    catch {
        if ($attempt -lt $maxRetries -and $_.Exception.Response.StatusCode -eq 403) {
            Write-Warning "Attempt $($attempt) - HTTP 403 Forbidden. Trying again in 10 seconds."
            Start-Sleep 10
            continue
        }
        throw
    }
}

if (!$matchingStorage) {   
    Write-Status "Giving Key Vault the operator role on storage account '$StorageAccountName'..."
    $roleAssignement = Get-AzRoleAssignment `
        -RoleDefinitionName 'Storage Account Key Operator Service Role' `
        -Scope $storageAccount.Id
    if (!$roleAssignement) {
        New-AzRoleAssignment `
            -ApplicationId $keyVaultSpAppId `
            -RoleDefinitionName 'Storage Account Key Operator Service Role' `
            -Scope $storageAccount.Id | Out-Default
    }

    Write-Status "Making Key Vault '$KeyVaultName' manage storage account '$StorageAccountName'..."
    $attempt = 0;
    while ($true) {
        try {
            $attempt++

            if ($AutoRegenerateKey) {
                $parameters = @{ RegenerationPeriod = $regenerationPeriod }
            }
            else {
                $parameters = @{ DisableAutoRegenerateKey = $true }
            }

            Add-AzKeyVaultManagedStorageAccount `
                -VaultName $KeyVaultName `
                -AccountName $StorageAccountName `
                -ActiveKeyName key1 `
                -AccountResourceId $storageAccount.Id `
                -ErrorAction Stop `
                @parameters | Out-Default
            break
        }
        catch {
            if ($_.Exception.Body.Error.Code -eq "Forbidden" -and ($attempt -lt 5)) {
                $sleep = 30
                Write-Warning "HTTP 403 Forbidden returned. Trying again in $sleep seconds."
                Start-Sleep -Seconds $sleep
            }
            else {
                throw
            }
        }
    }
}

Write-Status "Generating a template SAS for '$TableSasDefinitionName'..."
$storageContext = New-AzStorageContext `
    -StorageAccountName $StorageAccountName `
    -Protocol Https `
    -StorageAccountKey $storageEmulatorKey
$tableSasTemplate = New-AzStorageAccountSASToken `
    -ExpiryTime (Get-Date "2010-01-01Z").ToUniversalTime() `
    -Permission "rwdlacu" `
    -ResourceType Service, Container, Object `
    -Service Table `
    -Protocol HttpsOnly `
    -Context $storageContext
    
Write-Status "Creating SAS definition '$TableSasDefinitionName'..."
Set-AzKeyVaultManagedStorageSasDefinition `
    -VaultName $KeyVaultName `
    -AccountName $StorageAccountName `
    -Name $TableSasDefinitionName `
    -ValidityPeriod $SasValidityPeriod `
    -SasType 'account' `
    -TemplateUri $tableSasTemplate | Out-Default

Write-Status "Removing Key Vault role assignment for '$($currentUser.userPrincipalName)' (object ID $($currentUser.id))..."
$attempt = 0
while ($true) {
    try {
        $attempt++
        Remove-AzRoleAssignment `
            -ObjectId $currentUser.id `
            -ResourceGroupName $ResourceGroupName `
            -RoleDefinitionName "Key Vault Administrator" `
            -ErrorAction Stop
        break
    }
    catch {
        if ($attempt -lt $maxRetries -and $_.Exception.Response.StatusCode -eq 204) {
            Write-Warning "Attempt $($attempt) - HTTP 204 No Content. Trying again in 10 seconds."
            Start-Sleep 10
            continue
        }
        elseif ($attempt -lt $maxRetries -and $_.Exception.Message -eq "The provided information does not map to a role assignment.") {
            Write-Warning "Attempt $($attempt) - transient duplicate role assignments. Trying again in 10 seconds."
            Start-Sleep 10
            continue
        } 
        throw
    }
}
