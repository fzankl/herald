terraform {
  required_version = "~> 1.16"

  # The version is pinned exactly and .terraform.lock.hcl is committed, so every machine and every
  # runner resolves the identical provider binary. After changing the version, record the hashes for
  # every platform in one go. A lock file written on one platform carries only that platform's h1
  # hash, and the next init elsewhere rewrites the file:
  #
  #   terraform providers lock -platform=windows_amd64 -platform=linux_amd64 -platform=darwin_arm64
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "5.5.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "2.12.0"
    }
  }

  # The state lives in a storage account that Terraform does not manage, in a resource group that
  # Terraform does not manage either. scripts/bootstrap.ps1 creates both. If the state lived in the
  # resource group that Terraform manages, terraform destroy would delete its own backend.
  #
  # Both stages share that account - hence shd (shared) in the names - but not the state file.
  # "key" is therefore deliberately absent here and passed per stage instead:
  #
  #   terraform init -backend-config="key=herald-<environment>.tfstate"
  backend "azurerm" {
    resource_group_name  = "rg-herald-shd-gwc-001"
    storage_account_name = "stheraldstateshdgwc001"
    container_name       = "tfstate"
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  # subscription_id comes from ARM_SUBSCRIPTION_ID so that no subscription value is committed.
  features {}
}

# Signs in the same way as azurerm and reads ARM_SUBSCRIPTION_ID as well.
provider "azapi" {}
