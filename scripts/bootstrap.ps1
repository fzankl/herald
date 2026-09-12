<#
.SYNOPSIS
Creates what Terraform cannot create for itself: the state store and the pipeline identity.

.DESCRIPTION
Terraform needs a state store before it can run, and the pipeline needs an identity to sign in with
before Terraform can create one. Both therefore live outside Terraform, in a resource group it does
not manage, and they stay outside it. Terraform must never be able to destroy the credentials that
its own pipeline signs in with.

Run it once per stage. The state store is shared, the identity and its federated credential are per
stage, so that the dev pipeline cannot sign in as the prd one.

The GitHub environment has to carry the same name as the stage: GitHub puts that name into the OIDC
token, and Entra ID compares it with the credential subject character for character. The subject
also carries the numeric owner and repository ids, which the script reads from the GitHub API. That
is why a private repository needs an authenticated gh.

The script is idempotent.

.EXAMPLE
az login
az account set --subscription <subscription id or name>
# Only for a private repository. The credential subject needs the numeric owner and repository ids,
# and those are not readable anonymously.
gh auth login
./scripts/bootstrap.ps1 my-account/herald dev
./scripts/bootstrap.ps1 my-account/herald prd
#>

#Requires -Version 7.4

[CmdletBinding()]
param(
    # owner/repository of this repository, used for the federated credential subject. Spell it the
    # way GitHub does: the subject is matched case-sensitively and is not corrected here.
    [Parameter(Mandatory, Position = 0)]
    [ValidatePattern('^[^/\s]+/[^/\s]+$')]
    [string] $GithubRepository,

    # Stage the pipeline is created for. Run the script once per stage. Mandatory and
    # without a default, for the same reason the Terraform variable has none.
    # Nothing should reach prd because a value was left out.
    [Parameter(Mandatory, Position = 1)]
    [ValidateSet('dev', 'prd')]
    [string] $Environment
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Invoke-Az checks the exit code of every az call itself. The default is $false, but a user profile
# can set $PSNativeCommandUseErrorActionPreference to $true, and then a non-zero exit would become a
# terminating error before Invoke-Az sees $LASTEXITCODE - which would break -RetryUntilAuthorized.
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-Az {
    <#
        Every az call in this script goes through here, so that the flags every one of them needs
        are written once: --only-show-errors always, and --output none for the calls whose output
        nobody reads (-Quiet, which also swallows the return value).

        -RetryUntilAuthorized is for a data plane call made right after its role was granted. Role
        assignments in Entra ID are not effective immediately. Propagation regularly takes a few
        minutes, and until it is through the call comes back as AuthorizationPermissionMismatch.
        That has to be tolerated rather than fail the bootstrap.
    #>
    param(
        [Parameter(Mandatory, Position = 0)]
        [string[]] $Arguments,

        [switch] $Quiet,
        [switch] $RetryUntilAuthorized,
        [int] $TimeoutSeconds = 300,
        [int] $DelaySeconds = 15
    )

    $all = $Arguments + @('--only-show-errors')
    if ($Quiet) {
        $all += @('--output', 'none')
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ($true) {
        $output = & az @all
        if ($LASTEXITCODE -eq 0) {
            if ($Quiet) { return }
            return $output
        }

        if (-not $RetryUntilAuthorized -or (Get-Date) -ge $deadline) {
            throw "az $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
        }

        Write-Host "    not authorized yet, retrying in $DelaySeconds s (role assignments take a few minutes to propagate)"
        Start-Sleep -Seconds $DelaySeconds
    }
}

function Assert-RoleAssignment {
    <#
        Grants a role at a scope, once.

        -Condition carries an ABAC condition, which is how a role that may assign other roles is
        kept from assigning any role it likes.
    #>
    param(
        [Parameter(Mandatory)] [string] $PrincipalId,
        [Parameter(Mandatory)] [ValidateSet('ServicePrincipal', 'User')] [string] $PrincipalType,
        [Parameter(Mandatory)] [string] $Role,
        [Parameter(Mandatory)] [string] $Scope,
        [string] $Condition
    )

    $listArguments = @(
        'role', 'assignment', 'list',
        '--assignee-object-id', $PrincipalId,
        '--role', $Role,
        '--scope', $Scope
    )

    $existingId = [string](Invoke-Az ($listArguments + @('--query', '[0].id', '-o', 'tsv')))

    if ($existingId) {
        $existingCondition = [string](Invoke-Az ($listArguments + @('--query', '[0].condition', '-o', 'tsv')))

        # Azure may return the condition reformatted, so compare without whitespace.
        if (($existingCondition -replace '\s', '') -eq ($Condition -replace '\s', '')) {
            Write-Host "    $Role already assigned"
            return
        }

        Write-Host "    $Role assigned without the expected condition, replacing it"
        Invoke-Az -Quiet @('role', 'assignment', 'delete', '--ids', $existingId)
    }

    if ($Condition) {
        Write-Host "    granting $Role at $Scope, constrained by a condition"
    }
    else {
        Write-Host "    granting $Role at $Scope"
    }

    $createArguments = @(
        'role', 'assignment', 'create',
        '--assignee-object-id', $PrincipalId,
        '--assignee-principal-type', $PrincipalType,
        '--role', $Role,
        '--scope', $Scope
    )

    if ($Condition) {
        $createArguments += @('--condition', $Condition, '--condition-version', '2.0')
    }

    Invoke-Az -Quiet $createArguments
}

function Assert-FederatedCredential {
    <#
        Creates a federated credential, or corrects the subject of an existing one.

        The subject decides whether a credential is correct, not its name, 
        so the subject is what gets compared.
    #>
    param(
        [Parameter(Mandatory)] [string] $IdentityName,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Subject
    )

    Write-Host "    federated credential $Name"

    $existingSubject = [string](Invoke-Az @(
            'identity', 'federated-credential', 'list',
            '--identity-name', $IdentityName,
            '--resource-group', $resourceGroup,
            '--query', "[?name=='$Name'] | [0].subject", '-o', 'tsv'
        ))

    $arguments = @(
        '--name', $Name,
        '--identity-name', $IdentityName,
        '--resource-group', $resourceGroup,
        '--issuer', 'https://token.actions.githubusercontent.com',
        '--audiences', 'api://AzureADTokenExchange',
        '--subject', $Subject
    )

    if ($existingSubject -ceq $Subject) {
        Write-Host '        already correct'
        return
    }

    if ($existingSubject) {
        Write-Host "        subject differs, updating it from $existingSubject"
        Invoke-Az -Quiet (@('identity', 'federated-credential', 'update') + $arguments)
        return
    }

    Invoke-Az -Quiet (@('identity', 'federated-credential', 'create') + $arguments)
}

function Register-ResourceProvider {
    <#
        Registers the resource providers this repository needs and waits until they are ready.

        A freshly created subscription has almost nothing registered, and an unregistered provider
        does not say so. ARM answers a call into it with SubscriptionNotFound, naming a
        subscription that plainly exists. The resource group is created through
        Microsoft.Resources, which is always registered, so the failure appears one step later at
        the storage account and points at the wrong thing.
    #>
    param(
        [Parameter(Mandatory, Position = 0)]
        [string[]] $Namespace,

        [int] $TimeoutSeconds = 600,
        [int] $DelaySeconds = 15
    )

    $pending = [System.Collections.Generic.List[string]]::new()

    foreach ($ns in $Namespace) {
        $state = [string](Invoke-Az @('provider', 'show', '-n', $ns, '--query', 'registrationState', '-o', 'tsv'))
        if ($state -eq 'Registered') {
            Write-Host "    $ns already registered"
            continue
        }

        Write-Host "    $ns is $state, registering"
        Invoke-Az -Quiet @('provider', 'register', '--namespace', $ns)
        $pending.Add($ns)
    }

    if ($pending.Count -eq 0) {
        return
    }

    # Registration is asynchronous. Every later step depends on it, so waiting here is cheaper than
    # failing three resources further on.
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ($pending.Count -gt 0) {
        Start-Sleep -Seconds $DelaySeconds

        foreach ($ns in @($pending)) {
            $state = [string](Invoke-Az @('provider', 'show', '-n', $ns, '--query', 'registrationState', '-o', 'tsv'))
            if ($state -eq 'Registered') {
                Write-Host "    $ns registered"
                $pending.Remove($ns) | Out-Null
            }
        }

        if ($pending.Count -gt 0 -and (Get-Date) -ge $deadline) {
            throw "Resource providers still not registered after $TimeoutSeconds s: $($pending -join ', '). Check with 'az provider show -n <namespace> --query registrationState'."
        }
    }
}

function Get-GithubRepositoryId {
    <#
        Returns the canonical owner/repository spelling and the immutable numeric owner and
        repository ids.

        GitHub uses the following format for the OIDC subject claim:

            repo:<owner>@<owner_id>/<repo>@<repo_id>:environment:<stage>

        Names can be renamed, transferred and later reused by someone else. The ids cannot be 
        guessed, which is why this lookup exists. For a private repository the
        call needs an authenticated gh, so run gh auth login first.
    #>
    param(
        [Parameter(Mandatory, Position = 0)]
        [string] $Repository
    )

    $json = $null

    if (Get-Command gh -ErrorAction SilentlyContinue) {
        $output = & gh api "repos/$Repository" 2>$null
        if ($LASTEXITCODE -eq 0 -and $output) {
            $json = ($output -join '') | ConvertFrom-Json
        }
    }

    if (-not $json) {
        try {
            $response = Invoke-WebRequest -Uri "https://api.github.com/repos/$Repository" `
                -Headers @{ Accept = 'application/vnd.github+json' } -TimeoutSec 15 -SkipHttpErrorCheck
            if ($response.StatusCode -eq 200) {
                $json = $response.Content | ConvertFrom-Json
            }
        }
        catch {
            # Falls through to the throw below, which says what to do about it.
        }
    }

    if (-not $json) {
        throw @"
Could not read the owner and repository ids for '$Repository' from GitHub.

The federated credential subject needs them, and they cannot be typed from knowledge:

    repo:<owner>@<owner_id>/<repo>@<repo_id>:environment:<stage>

Run 'gh auth login' and try again. A private repository is not readable anonymously.
"@
    }

    return [pscustomobject]@{
        Owner   = [string]$json.owner.login
        OwnerId = [string]$json.owner.id
        Repo    = [string]$json.name
        RepoId  = [string]$json.id
    }
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'The Azure CLI (az) is not on PATH.'
}

$workload = 'herald'
$location = 'germanywestcentral'
$locationShort = 'gwc'
$instance = '001'

# The shd stage holds everything that belongs to no single stage: the Terraform state store and the
# pipeline identity of every stage. dev and prd are told apart by the blob key that terraform init
# is given, not by the account they live in.
$sharedEnvironment = 'shd'
$statePurpose = 'state'
$resourceGroup = "rg-$workload-$sharedEnvironment-$locationShort-$instance"
$storageAccount = "st$workload$statePurpose$sharedEnvironment$locationShort$instance"
$container = 'tfstate'
$stateKey = "$workload-$Environment.tfstate"

# Per stage, so that the dev pipeline holds no credential it could sign in as the prd one with.
$identityName = "id-$workload-$Environment-$locationShort-$instance"
$credentialName = "github-$Environment"

# Entra ID compares this with the claim in the GitHub token character for character. The immutable
# form carries the numeric ids. See Get-GithubRepositoryId for why the ids have to be looked up.
$repo = Get-GithubRepositoryId $GithubRepository
$immutableRepo = "$($repo.Owner)@$($repo.OwnerId)/$($repo.Repo)@$($repo.RepoId)"
$credentialSubject = "repo:${immutableRepo}:environment:$Environment"

# The plan identity belongs to no stage, so it carries no stage in its name and its credential
# matches the pull_request claim, which carries no stage either.
$planIdentityName = "id-$workload-plan-$locationShort-$instance"
$planCredentialName = 'github-pull-request'
$planCredentialSubject = "repo:${immutableRepo}:pull_request"

$revision = 'unknown'
try {
    $head = & git rev-parse HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and $head) {
        $revision = [string]$head
    }
}
catch {
    # Not a git checkout, or no git on PATH. The version tag then says so.
}

# The same four tag keys Terraform writes, set by hand because these resources are not managed by
# Terraform. tool says which mechanism owns them. The shared resources carry shd, so that a second
# run for the other stage does not retag them. The identity carries the stage it belongs to, which
# is also what Terraform writes once the import blocks adopt it.
$stateTags = @("workload=$workload", "environment=$sharedEnvironment", 'tool=bootstrap', "version=$revision")
$identityTags = @("workload=$workload", "environment=$Environment", 'tool=bootstrap', "version=$revision")

try {
    $subscriptionId = [string](Invoke-Az @('account', 'show', '--query', 'id', '-o', 'tsv'))
}
catch {
    throw "Not signed in. Run 'az login' and 'az account set --subscription <subscription id or name>' first."
}
Write-Host "subscription: $subscriptionId"

Write-Host '==> resource providers'
Register-ResourceProvider @(
    'Microsoft.Storage'
    'Microsoft.Web'
    'Microsoft.ManagedIdentity'
    'Microsoft.OperationalInsights'
    'Microsoft.Insights'
    'Microsoft.KeyVault'
)

Write-Host "==> resource group $resourceGroup"
Invoke-Az -Quiet (@(
        'group', 'create',
        '--name', $resourceGroup,
        '--location', $location,
        '--tags'
    ) + $stateTags)

Write-Host "==> storage account $storageAccount"

Invoke-Az -Quiet (@(
        'storage', 'account', 'create',
        '--name', $storageAccount,
        '--resource-group', $resourceGroup,
        '--location', $location,
        '--sku', 'Standard_LRS',
        '--kind', 'StorageV2',
        '--https-only', 'true',
        '--min-tls-version', 'TLS1_2',
        '--allow-blob-public-access', 'false',
        '--allow-shared-key-access', 'false',
        '--tags'
    ) + $stateTags)

Write-Host '==> blob versioning and soft delete on the state account'

Invoke-Az -Quiet @(
    'storage', 'account', 'blob-service-properties', 'update',
    '--account-name', $storageAccount,
    '--resource-group', $resourceGroup,
    '--enable-versioning', 'true',
    '--enable-delete-retention', 'true', '--delete-retention-days', '30',
    '--enable-container-delete-retention', 'true', '--container-delete-retention-days', '30'
)

$storageAccountId = [string](Invoke-Az @(
        'storage', 'account', 'show',
        '--name', $storageAccount,
        '--resource-group', $resourceGroup,
        '--query', 'id', '-o', 'tsv'
    ))

# This role has to be granted before the container is created. The container is created
# over the data plane, and Owner or Contributor on the subscription grant no data plane access to
# blobs. With shared keys disabled there is no fallback either, so without the role the next call
# fails with AuthorizationPermissionMismatch.
Write-Host '==> role for the current user, so that the container can be created and local terraform plan can read the state'
$currentUserObjectId = [string](Invoke-Az @('ad', 'signed-in-user', 'show', '--query', 'id', '-o', 'tsv'))
Assert-RoleAssignment -PrincipalId $currentUserObjectId -PrincipalType User `
    -Role 'Storage Blob Data Contributor' -Scope $storageAccountId

Write-Host "==> blob container $container"
Invoke-Az -Quiet -RetryUntilAuthorized @(
    'storage', 'container', 'create',
    '--name', $container,
    '--account-name', $storageAccount,
    '--auth-mode', 'login'
)

Write-Host "==> user assigned identity $identityName"
Invoke-Az -Quiet (@(
        'identity', 'create',
        '--name', $identityName,
        '--resource-group', $resourceGroup,
        '--location', $location,
        '--tags'
    ) + $identityTags)

$identityClientId = [string](Invoke-Az @(
        'identity', 'show', '--name', $identityName, '--resource-group', $resourceGroup,
        '--query', 'clientId', '-o', 'tsv'
    ))
$identityPrincipalId = [string](Invoke-Az @(
        'identity', 'show', '--name', $identityName, '--resource-group', $resourceGroup,
        '--query', 'principalId', '-o', 'tsv'
    ))

Write-Host "==> federated credential for stage $Environment"
# The subject binds the token to one repository and one GitHub environment. A pull request from a
# fork gets a different subject claim, so its sign-in fails. That is intentional.
Assert-FederatedCredential -IdentityName $identityName -Name $credentialName `
    -Subject $credentialSubject

Write-Host "==> plan identity $planIdentityName"
# A separate identity for terraform plan, shared by both stages and read-only.
#
# The plan runs on pull requests, and a pull request from this repository runs the workflow file
# from its own branch. Anyone who can push a branch can therefore change what the plan job does. If
# that job could obtain a token for a stage identity, it would obtain Contributor along with it, so
# a branch would be enough to change the subscription.
#
# This identity holds Reader on the subscription and data plane read access to the state storage
# account. A plan writes no state, and infra-plan.yml plans with -lock=false, so there is no lease to
# take either. It also plans with -refresh=false, because refreshing the function app needs a list
# action that Reader does not include and that would return the app settings. The identity can read
# both state files and cannot change a resource or a state file. The apply on main plans again, with
# a refresh, as the stage identity.
Invoke-Az -Quiet (@(
        'identity', 'create',
        '--name', $planIdentityName,
        '--resource-group', $resourceGroup,
        '--location', $location,
        '--tags'
    ) + $stateTags)

$planClientId = [string](Invoke-Az @(
        'identity', 'show', '--name', $planIdentityName, '--resource-group', $resourceGroup,
        '--query', 'clientId', '-o', 'tsv'
    ))
$planPrincipalId = [string](Invoke-Az @(
        'identity', 'show', '--name', $planIdentityName, '--resource-group', $resourceGroup,
        '--query', 'principalId', '-o', 'tsv'
    ))

# The pull_request subject carries no stage, so one credential covers both stages. It also does not
# match a push to main, which is what keeps the apply out of this identity.
Assert-FederatedCredential -IdentityName $planIdentityName -Name $planCredentialName `
    -Subject $planCredentialSubject

Assert-RoleAssignment -PrincipalId $planPrincipalId -PrincipalType ServicePrincipal `
    -Role 'Reader' -Scope "/subscriptions/$subscriptionId"
Assert-RoleAssignment -PrincipalId $planPrincipalId -PrincipalType ServicePrincipal `
    -Role 'Storage Blob Data Reader' -Scope $storageAccountId

Write-Host '==> roles for the pipeline identity'
# Reading and writing the state file.
Assert-RoleAssignment -PrincipalId $identityPrincipalId -PrincipalType ServicePrincipal `
    -Role 'Storage Blob Data Contributor' -Scope $storageAccountId
# Creating the app resource group and everything in it. The scope is the subscription and not the
# resource group because the resource group does not exist until the first apply creates it, and
# because terraform destroy removes it again.
Assert-RoleAssignment -PrincipalId $identityPrincipalId -PrincipalType ServicePrincipal `
    -Role 'Contributor' -Scope "/subscriptions/$subscriptionId"
# function.tf assigns the storage roles of the function app identity, so the pipeline needs the
# right to grant rights, which is close to a privilege escalation. The role therefore carries an
# ABAC condition, which Azure calls constrained delegation: this identity may assign and remove
# exactly the two storage roles function.tf needs, and only to service principals. It cannot grant
# Owner and it cannot grant anything to a user.
#
# GUIDs, because the condition language takes role definition ids and not names:
#   b7e6dc6d-f1e8-4753-8033-0f276bb0955b  Storage Blob Data Owner
#   0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3  Storage Table Data Contributor
$assignableRoles = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b, 0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
$rbacCondition = @"
((!(ActionMatches{'Microsoft.Authorization/roleAssignments/write'})) OR (@Request[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {$assignableRoles} AND @Request[Microsoft.Authorization/roleAssignments:PrincipalType] ForAnyOfAnyValues:StringEqualsIgnoreCase {'ServicePrincipal'})) AND ((!(ActionMatches{'Microsoft.Authorization/roleAssignments/delete'})) OR (@Resource[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {$assignableRoles} AND @Resource[Microsoft.Authorization/roleAssignments:PrincipalType] ForAnyOfAnyValues:StringEqualsIgnoreCase {'ServicePrincipal'}))
"@.Trim()

Assert-RoleAssignment -PrincipalId $identityPrincipalId -PrincipalType ServicePrincipal `
    -Role 'Role Based Access Control Administrator' -Scope "/subscriptions/$subscriptionId" `
    -Condition $rbacCondition

$tenantId = [string](Invoke-Az @('account', 'show', '--query', 'tenantId', '-o', 'tsv'))

Write-Host @"

Done: $Environment

GitHub environment secret on "$Environment"
  AZURE_CLIENT_ID        $identityClientId

GitHub repository secrets
  AZURE_TENANT_ID        $tenantId
  AZURE_SUBSCRIPTION_ID  $subscriptionId
  AZURE_PLAN_CLIENT_ID   $planClientId

None of these four is confidential. They are secrets so that GitHub redacts them in workflow logs.

Federated credentials
  apply, per stage       $credentialSubject
  plan, both stages      $planCredentialSubject

Terraform backend
  resource group         $resourceGroup
  storage account        $storageAccount
  container              $container
  local init             terraform -chdir=deploy init -backend-config="key=$stateKey"

Next
  - create the GitHub environment "$Environment" (prd needs a required reviewer)
  - set the repository variable DEPLOY_ENABLED to true, otherwise infra and deploy skip every job
  - run this script for the other stage
"@
