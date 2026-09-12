# herald

The herald announces at a fixed hour. This Azure Function publishes approved posts from the Markdown files of a content repository at the time recorded in each file.

The repository is public and contains no post texts, no templates and no real configuration values.

## Status

`v0.1` — the function app skeleton. Three functions that only log that they started, so that per-function scaling on Flex Consumption can be observed.
The publisher itself (front matter parser, scheduler, target clients) follows in later phases.

## Architecture

The content repository is the single source of truth. herald keeps no database and no state store.
A post is published exactly when its Markdown file says `status: approved`, its `publish_at` has passed and it carries no `urn` yet.
After publishing, herald writes `status`, `urn` and `published_at` back to the file as a commit, which is also what prevents a second publish.
Reads and writes go through the GitHub REST API with a fine-grained token scoped to that one repository.
Posting goes through the official Posts API of the configured targets and nothing else.
Without `Herald__Mode=Live` the app is in dry-run: it logs what it would have done, sends nothing and writes nothing.
Secrets live in Azure Key Vault and reach the app as key vault references in its application settings.

The app deliberately hosts three functions with three different triggers, which is what makes the per-function scaling of Flex Consumption visible:

| Function                | Trigger                              | Purpose                                     |
| ----------------------- | ------------------------------------ | --------------------------------------------|
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

## A note for the content repository

herald appends a marker to every commit message it writes back, so that a status change in front matter does not set off a build in the content repository.
The marker is configuration, not a rule: `[skip ci]` is the default because GitHub Actions both honour it. Other systems expect their own token, and an empty value turns the marker off for a repository that does want the build.

Whether a push to a post file should trigger a build at all is a question for the content repository and not for herald.
The usual answer there is a path filter on the push workflow - `paths-ignore` under GitHub Actions, the equivalent elsewhere.
That change lives in the content repository and is not made from here.
