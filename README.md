<div align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/images/herald-logo-dark.svg">
    <img src="docs/images/herald-logo-light.svg" alt="herald" width="420">
  </picture>

  **Publishes approved posts at the time each file names.**

</div>

---

An Azure Function, named after the messenger who announces at a fixed hour.

## Status

`v0.1` is the function app skeleton: three functions that only log that they started, so that per-function scaling on Flex Consumption can be observed.
The publisher itself (front matter parser, scheduler, target clients) follows in later phases.

## Why this exists

Most of what follows is what herald is built to do. The status above says what already runs.

### Everything lives in the post file

**The repository is the only state**  
Status, post URN and timestamp live in the post's own front matter.
There is no database, no table, no second store to reconcile. Whether a post went out is answered by the file that contains it.

**A post can never go out twice**  
The file is committed as published *before* the API call.
A crash between the two leaves a post that is live without its URN recorded, which is recoverable by hand and described in the runbook.
It never leaves a post that gets published again on the next run.

**Own comments live in the post file, with their own schedule**  
Article link, series comment, a follow-up three days later: each one sits in the front matter with a relative time or an absolute one, and is published and recorded like the post itself.
A series comment comes from a template that references other posts by slug.
Each reference renders to that post's URL once that post is published and disappears while it is not, so the list is never wrong and never needs editing.

**A post names its targets**  
Parser, scheduler and escaping deal with posts, comments and times. No network appears in them.
The front matter names the targets a post goes to and carries one result block per target, because two targets return two post ids and two timestamps.
What is due is then decided per target: approved, time reached, no result recorded yet.

### What it refuses to do

**Dry-run is what an unconfigured app does**  
Without `Herald__Mode=Live` nothing is sent and nothing is written.
The dry run logs what it would have done, including the escaped text.

**The official API, and nothing else**  
No scraping, no unofficial endpoints, no browser automation - the three things that get accounts suspended.

**No secret is ever in the repository or in the pipeline**  
Tokens live in Key Vault and reach the app as key vault references.
The app talks to its storage with a managed identity and shared keys are disabled, so no key reaches the Terraform state either.
The pipeline signs in to Azure with OIDC and holds no credential at all.

### Timer trigger against scheduled workflow

A scheduled GitHub Actions workflow would be the obvious host: no Terraform, no Key Vault, no identity, no cost.
Three documented properties of the [`schedule` event](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows) argue against it for publishing at a named time:

- "The `schedule` event can be delayed during periods of high loads of GitHub Actions workflow runs. High load times include the start of every hour."
- "In a public repository, scheduled workflows are automatically disabled when no repository activity has occurred in 60 days."
- "The shortest interval you can run scheduled workflows is once every 5 minutes."

The second one is the reason.
A publisher is finished software: once it works it is not touched for months.
A schedule that switches itself off after 60 quiet days fails silently, and a quiet repository looks like a stable one.

The delay is the weaker argument: the timer polls every ten minutes anyway, so a post goes out somewhere inside a ten-minute window either way.
A ten-minute cron would however fire at `:00`, which is the load peak the documentation names.

The access token never leaves Azure. As a GitHub secret it would be readable by every workflow in the repository and injected into a runner on every run.
The domain logic is testable, because parser, scheduler and escaping are pure functions in `Herald.Core` with no Azure and no HTTP dependency.
Application Insights makes runs queryable across days, which the per-function scaling observation needs and per-run workflow logs cannot give.

The cost of that: more moving parts, Terraform, two tokens to rotate instead of one, and a 30-second app-initialization timeout on Flex Consumption.
Money is not part of that cost: Actions minutes are free for public repositories, and this workload sits inside the Flex Consumption free grant.

## Architecture

A post is published exactly when its Markdown file says `status: approved`, its `publish_at` has passed and it carries no `urn` yet.
After publishing, herald writes `status`, `urn` and `published_at` back to the file as a commit.
Reads and writes go through the GitHub REST API with a fine-grained token scoped to that one repository.

The three functions use three different triggers, which is what makes per-function scaling on Flex Consumption visible:

| Function                | Trigger                              | Purpose                                     |
| ----------------------- | ------------------------------------ | ------------------------------------------- |
| `PublishScheduledPosts` | timer, `0 */10 * * * *`              | publish due posts and their comments        |
| `CheckTokenExpiry`      | timer, `0 0 6 * * *`                 | warn before the access tokens expire        |
| `RunNow`                | HTTP, authorization level `Function` | run the same pass on demand, return its log |

All schedules are UTC: Flex Consumption does not support `WEBSITE_TIME_ZONE` or `TZ`.

## Repository layout

```
src/Herald.Functions   function app: the three triggers, host wiring
src/Herald.Core        domain logic and configuration, no Azure dependencies
build/                 TargetFramework and central package versions
deploy/                Terraform
docs/                  target POST API facts, content format, runbook
scripts/               bootstrap, token exchange
.github/workflows/     ci, infra, deploy
```

`Directory.Build.props` and `Directory.Packages.props` at the root are two-line shims.
The real content is in `build/`, where MSBuild would not find it on its own.

## Running locally

```bash
cp local.settings.template.json src/Herald.Functions/local.settings.json
cd src/Herald.Functions
dotnet run
```

`dotnet run` is the entry point with `Azure.Functions.Sdk`. It starts the Functions host when Core Tools is installed.

The template sets `Herald__Mode` to `Dry`. Removing the setting, or giving it any other value, makes the app fail at start-up with a message naming the setting.
That is deliberate, because a failed start on Flex Consumption offers no other diagnosis.

`local.settings.json` is in `.gitignore` and never holds a real token in a committed file.

Call the HTTP trigger:

```bash
curl http://localhost:7071/api/RunNow
```

## Building

```bash
dotnet restore src/Herald.Functions/Herald.Functions.csproj
dotnet build Herald.slnx --no-restore -warnaserror
```

Restore the function project itself, not only the solution:
Only a direct project restore runs the post-restore hook that generates the extension project `obj/azure_functions/azure_functions.g.csproj` (`AZFW0108`).

## Configuration

| Setting        | Meaning                                                                           |
| -------------- | --------------------------------------------------------------------------------- |
| `Herald__Mode` | `Dry` (default in the template) or `Live`, matched case-insensitively. (Required) |

## Deployment

There are two stages, `dev` and `prd`: the same Terraform configuration with a different value for the required variable `environment`, which also selects the state file.
Only the `infra` workflow applies it. Locally there is `terraform plan` and nothing else:

```bash
terraform -chdir=deploy init -backend-config="key=herald-dev.tfstate"
terraform -chdir=deploy plan -var="environment=dev" -var="app_version=0.1.0"
```

### Setting up

1. Sign in with `az login`, and for a private repository also with `gh auth login`.
2. Run `scripts/bootstrap.ps1 <owner>/<repository> dev`, then the same with `prd`. It creates the state store, one pipeline identity per stage and a plan identity, each with its federated credential, in a resource group Terraform does not manage. It is idempotent and prints every value the next steps need.
3. Create the GitHub environments `dev` and `prd`, and give `prd` a required reviewer.
4. Add `AZURE_CLIENT_ID` as an environment secret on each stage, and `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` and `AZURE_PLAN_CLIENT_ID` as repository secrets. None of them is confidential, but secrets are redacted in workflow logs.
5. Set the repository variable `DEPLOY_ENABLED` to `true`. Without it `infra` and `deploy` skip every job.
6. Merge to `main`. `infra` applies `dev`, then `prd`.
7. Deploy the code once by hand, because `deploy` starts on its own only when `src/` or `build/` changes: `gh workflow run deploy.yml -f sha=<sha of main>`.

Role assignments take a few minutes to become effective. If the first run fails in `terraform init` with an authorization error, run it again.

### Who may do what

| Identity                | Role                                      | Scope                 | Why                                                |
| ----------------------- | ----------------------------------------- | --------------------- | -------------------------------------------------- |
| Pipeline, one per stage | `Contributor`                             | subscription          | create the app resource group and everything in it |
|                         | `Role Based Access Control Administrator` | subscription          | give the function app identity its storage roles   |
|                         | `Storage Blob Data Contributor`           | state storage account | write the state                                    |
| Plan, shared            | `Reader`                                  | subscription          | sign in and read the subscription                  |
|                         | `Storage Blob Data Reader`                | state storage account | read the state                                     |
| Function app            | `Storage Blob Data Owner`                 | app storage account   | host storage and deployment package                |
|                         | `Storage Table Data Contributor`          | app storage account   | host diagnostic events                             |

An ABAC condition limits the right to assign roles: the pipeline identity may assign and remove exactly those two storage roles, and only to service principals.
`Contributor` sits on the subscription because the app resource group does not exist before the first apply, which assumes one subscription per workload.
A pull request plan runs without a lock and without a refresh, so it shows no drift. The apply on `main` refreshes first.

### The workflows

| Workflow | Trigger                                                             | What it does                                                         |
| -------- | ------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `ci`     | pull request, push to `main`                                        | build, test, check Terraform, publish the artifact on `main`         |
| `infra`  | `deploy/`, the infra workflows, `build/Directory.Build.props`       | plan both stages on a pull request, apply `dev` then `prd` on `main` |
| `deploy` | green `ci` on `main` with changes in `src/` or `build/`, manual run | deploy to `dev`, smoke-test, then the same for `prd`                 |

The smoke test checks that `RunNow` reports the commit it just deployed. A rollback is a manual `deploy` run with the SHA of the last green commit.
Every resource carries a `version` tag with the release version from `build/Directory.Build.props`.
Both stages run with `Herald__Mode` set to `Dry`. To publish from `prd`, set the repository variable `HERALD_MODE_PRD` to `Live` and run `infra`.

## A note for the content repository

herald appends a marker to every commit message it writes back, so that a status change in front matter does not set off a build in the content repository.
The marker is configurable. `[skip ci]` is the default because GitHub Actions honours it. Other systems expect their own token, and an empty value turns the marker off for a repository that does want the build.

Whether a push to a post file should trigger a build at all is a question for the content repository and not for herald.
The usual answer there is a path filter on the push workflow - `paths-ignore` under GitHub Actions, the equivalent elsewhere.
That change lives in the content repository and is not made from here.

## Licence

MIT, see [LICENSE](LICENSE).
Use it, fork it, take pieces out of it.
The only obligation is the one MIT states: keep the copyright notice in copies or substantial portions.

The name herald and the logo files under `docs/images/` are reserved.
MIT conveys no trademark rights, and the logo is how the project is recognised, so a fork should carry its own name and its own artwork.

If something here saved you an afternoon, a star is welcome.
