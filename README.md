# herald

The herald announces at a fixed hour. This Azure Function publishes approved posts from the Markdown files of a content repository at the time recorded in each file.

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

There are two stages, `dev` and `prd`.
They differ only in the value of `environment`, which is a required Terraform variable validated against exactly those two values - it is the stage segment of every resource name and it also selects the state file, so no plan can be run without saying which stage it means.

Infrastructure is Terraform under `deploy/`, applied only by the `infra` workflow.
Locally there is `terraform plan` and nothing else, so the pipeline stays the only thing that writes state.
The backend in `deploy/providers.tf` carries no `key`, so a local plan has to name the stage twice - once for the state and once for the configuration:

```bash
terraform -chdir=deploy init -backend-config="key=herald-dev.tfstate"
terraform -chdir=deploy plan -var="environment=dev" -var="app_version=0.1.0"
```

Terraform needs a state store before it can run, and the pipeline needs an identity before Terraform can create one.
`scripts/bootstrap.ps1 <owner>/<repository> <stage>` creates both once, locally, in a resource group Terraform does not manage - which is also what makes `terraform destroy` safe for the app resource group.
Run it once per stage, with `az login` done first and, for a private repository, `gh auth login` as well.
The state store is shared - one storage account, two state files kept apart by the blob key - but the pipeline identity is per stage, so the `dev` pipeline holds no credential it could sign in as `prd` with.

That is why `AZURE_CLIENT_ID` is a GitHub **environment** secret on `dev` and on `prd`, while `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` and `AZURE_PLAN_CLIENT_ID` are repository **secrets**.
None of the four is confidential. They are secrets because GitHub redacts secrets in workflow logs and leaves variables untouched, and the logs of a public repository are readable by anyone. The documentation calls that redaction "not guaranteed", so it sits behind not printing a value rather than in front of it.

Set the repository variable `DEPLOY_ENABLED` to `true`. Without it `infra` and `deploy` skip every job, which is what a fork that only builds wants.

Create both environments. Give `prd` a required reviewer and leave `dev` without one - a stage that waits for a human is a stage that stops being the place where a broken change is found first.
The script is idempotent, so running it again changes nothing.
Role assignments take a few minutes to become effective, so if the very first pipeline run fails during `terraform init` with an authorization error, repeat it once before looking any further.

The `gh auth login` is there for the federated credential subject. GitHub appends the numeric owner and repository ids to the OIDC subject claim:

```
repo:<owner>@<owner_id>/<repo>@<repo_id>:environment:<stage>
```

### What the pipeline identity is allowed to do

The script grants rights at subscription scope.

| Role                                      | Scope                     | Why                                                         |
| ----------------------------------------- | ------------------------- | ----------------------------------------------------------- |
| `Storage Blob Data Contributor`           | the state storage account | read and write the state file                               |
| `Contributor`                             | the subscription          | create the app resource group and everything in it          |
| `Role Based Access Control Administrator` | the subscription          | assign the storage roles of the function app's own identity |

`Contributor` is not scoped to the app resource group, because that group does not exist until the first apply creates it and `terraform destroy` removes it again together with the role assignment. This assumes one subscription per workload, where subscription scope and workload scope are the same thing. On a shared subscription, let the bootstrap script create the app resource group as well and have Terraform only read it.

`Role Based Access Control Administrator` is the one assignment that could escalate, because a principal allowed to grant roles can grant itself more. It is therefore restricted by an ABAC condition, which Azure calls constrained delegation. The identity may assign and remove exactly two roles, `Storage Blob Data Owner` and `Storage Table Data Contributor`, and only to service principals. It cannot grant `Owner` and it cannot grant anything to a user.

Pull requests plan with a third identity that belongs to no stage.

| Role                       | Scope                     | Why                                     |
| -------------------------- | ------------------------- | --------------------------------------- |
| `Reader`                   | the subscription          | sign in to the subscription and read it |
| `Storage Blob Data Reader` | the state storage account | read the state file                     |

A pull request runs the workflow file from its own branch, so whoever can push a branch decides what this identity does. It can therefore change nothing: a plan writes no state, and it runs with `-lock=false` because taking the lock would need write access. It also runs with `-refresh=false`, because refreshing the function app reads its app settings through a list action that `Reader` does not include. A pull request plan therefore compares the configuration with the state and shows no drift, and the apply on `main` refreshes before it changes anything. Before the first apply of a stage there is no state file, and `terraform init` would have to create one, so the plan then runs against an empty local state instead. It can read both state files, which is unavoidable for a plan, and that is why only collaborators with push access can open a pull request that reaches it. A pull request from a fork gets no OIDC token while "Send write tokens to workflows from pull requests" stays off.

### The workflows

| Workflow | Trigger                      | What it does                                                                                                  |
| -------- | ---------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `ci`     | pull request, push to `main` | build, test, `terraform fmt`/`validate`, `tflint`. On `main` it also publishes the artifact                   |
| `infra`  | changes under `deploy/**`    | plans both stages on a pull request and comments both plans. On `main` applies `dev`, then `prd` after review |
| `deploy` | a green `ci` on `main`       | deploys the artifact to `dev`, smoke-tests it, then does the same for `prd`                                   |

`infra-plan`, `infra-apply` and `deploy-env` are called by those two, once per stage.
They are reusable workflows and not matrix legs.
`needs` is the only thing that actually orders two applies - `max-parallel: 1` limits how many run at once but does not promise which one runs first, which is not good enough for a promotion gate.
A matrix is also a single job, and `environment:` is a property of a job, so both stages would share one environment and one approval. The `prd` reviewer would be gating the `dev` apply, and `dev` would stop being the stage that fails first.

No plan file is stored anywhere. A pull request gets the rendered plan as a comment, and each apply takes its own plan inside the job that applies it.
Logs and artifacts of a public repository are readable by anyone, a saved plan carries a copy of the prior state in cleartext, and an artifact downloads without authentication - so a stored plan would publish the state, including the values the plan text renders as `(sensitive value)`.
The price is that an apply plans again rather than applying a plan a human has read. The gain is that the `prd` plan is taken after the `dev` apply instead of before it.

A called workflow gets the repository secrets through `secrets: inherit`. The per-stage `AZURE_CLIENT_ID` does not travel that way: a job that declares `environment:` reads that environment's secret, and an environment secret wins over anything the caller passes. That is what lets one reusable workflow serve both stages.

The deploy smoke test asserts that `RunNow` reports the commit SHA it just deployed.
The function names would also come back from an app that is still running the previous build, so they prove nothing about the upload. A rollback is `deploy` run manually with the SHA of the last green commit. It replays both stages in the same order.

`deploy` only starts on its own when a commit changes `src/` or `build/`. After the first `infra` apply of a new setup the function apps are therefore empty, and the first code deploy is the same manual run with the SHA of `main`: `gh workflow run deploy.yml -f sha=<sha>`.

`HERALD_MODE_DEV` and `HERALD_MODE_PRD` are repository variables and no workflow writes them. Neither has to exist - the `herald_mode` input defaults to `Dry`, so only a stage that should publish needs its variable set. Switching `prd` to `Live` means setting that variable and running `infra`, with the same review as any other infrastructure change, and `dev` stays on `Dry`.

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

If something here saved you an afternoon, a star is welcome.
