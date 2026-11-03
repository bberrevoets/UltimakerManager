# AGENTS.md

Instructions for AI coding agents (Claude Code, Codex and others) working in this repository. `CLAUDE.md` only imports
this file, so edit this one. [README.md](README.md) describes the project for people; this file lists what the code
alone does not tell you.

<!-- repo-init:start -->
## Repository Info

- **Company:** Berrevoets Systems
- **Author:** Bert Berrevoets
- **Contact:** <bert@berrevoets.net>
- **Remote:** <https://github.com/bberrevoets/UltimakerManager> (public)

## Workflow Rules

- **GitHub Flow:** `main` is the default and only long-lived branch. It is protected by the ruleset "Protect main":
  no direct pushes, no deletion, changes only through a pull request, and all PR conversations must be resolved
  before merging. Merged head branches are deleted automatically.
- After cloning, run `git config pull.rebase true` once, so `git pull` rebases instead of creating merge commits.
- Work on a branch per topic, push it, and open a PR to `main` only when Bert asks for it, assigned to him
  (`gh pr create --assignee bberrevoets`). PRs are squash-merged. After a merge: switch to `main` and pull, then delete the
  local branch with `git branch -D <branch>` (`-d` refuses after a squash merge) once
  `gh pr view <branch> --json state,headRefOid` shows `MERGED` and the same commit as `git rev-parse <branch>`.
- **"Update the documentation"** means `AGENTS.md`, the project's own `README.md` files (not vendored third-party ones)
  and `CHANGELOG.md`; then commit and push.
- [CHANGELOG.md](CHANGELOG.md) follows Keep a Changelog: the Attributors list at the top, and one `Author: *Name*`
  line under each section heading (`### Added`, `### Changed`, ...).
- Markdown must pass `markdownlint-cli2 "**/*.md"` with [.markdownlint.json](.markdownlint.json) (180-character
  lines) before every commit.
- Binaries go through Git LFS: every `filter=lfs` pattern in `.gitattributes` — the standard set (PDF, ZIP, PNG,
  JPEG, STEP/STP, STL, 3MF, F3D/F3Z) plus any pattern this repository adds. Add a pattern there before committing
  another binary type (GIF, BMP, ...). The patterns are lowercase and match `X.PNG` only where `core.ignorecase` is
  true (Windows, macOS); on a case-sensitive system check a new binary with `git check-attr filter -- <file>` before
  committing it.
- Commit messages use the conventional style (`feat:`, `fix:`, `docs:`, `chore:`).
<!-- repo-init:end -->

## Project Overview

Bert's own experiment (April 2025) in distributed .NET services: find the Ultimaker 3D printers on the home LAN and
keep their state up to date. It runs only on a development PC through the Aspire AppHost; nothing is deployed. Revived
on 2026-04-30 (upgrade to .NET 10 and Aspire 13.2.4); a run that day found Bert's Ultimaker 3 (firmware 5.3.0).

[bberrevoets/UltimakerMonitor](https://github.com/bberrevoets/UltimakerMonitor) is a separate, later monitor for the
same printers (Aspire with a web front end). The two repositories share no code.

## Common commands

Run from the solution root.

- Build everything: `dotnet build UltimakerManager.sln`
- Run the full distributed app (Aspire orchestrates Seq + RabbitMQ containers and both worker
  services, exposes the Aspire dashboard): `dotnet run --project UltimakerManager.AppHost`
- Run a single worker outside Aspire (loses service discovery / config wiring; rarely useful):
  `dotnet run --project PrinterDiscoveryService` or
  `dotnet run --project UltimakerPrinterDeviceManager`

There is currently no test project and no CI. Docker Desktop must be running because AppHost
provisions Seq and RabbitMQ as persistent-lifetime containers.

### RabbitMQ credentials

`UltimakerManager.AppHost/Program.cs` declares two Aspire parameters, `rmq-username` and
`rmq-password`, used to bring up the RabbitMQ container. Set them via user-secrets on the
AppHost project before first run:

```bash
dotnet user-secrets --project UltimakerManager.AppHost set Parameters:rmq-username <user>
dotnet user-secrets --project UltimakerManager.AppHost set Parameters:rmq-password <pass>
```

## Architecture

.NET 10 + .NET Aspire 13.x. Four projects in one solution:

- [UltimakerManager.AppHost](UltimakerManager.AppHost/) — Aspire orchestrator. Composes the
  whole system: starts Seq (logs) and RabbitMQ (with management plugin + persistent data
  volume), then both worker services, wiring them with `WithReference` so connection strings
  are injected automatically.
- [UltimakerManager.ServiceDefaults](UltimakerManager.ServiceDefaults/) — shared library
  consumed by both workers. `Extensions.AddServiceDefaults` wires OpenTelemetry (logging,
  metrics, tracing), health checks, service discovery, and HttpClient resilience. Also holds
  the cross-service DTOs in [Models/](UltimakerManager.ServiceDefaults/Models/):
  `DiscoveredPrinter` (the message on the bus) and `UltimakerPrinter` (the enriched record
  the device manager owns).
- [PrinterDiscoveryService](PrinterDiscoveryService/) — `BackgroundService` using
  `Makaretu.Dns` to send a multicast DNS PTR query for `_ultimaker._tcp.local` every 30 s.
  Parses the SRV/TXT records and remote endpoint into a `DiscoveredPrinter`, then publishes
  JSON onto the RabbitMQ queue `discoveredPrinterEvents`.
- [UltimakerPrinterDeviceManager](UltimakerPrinterDeviceManager/) — consumes
  `discoveredPrinterEvents`, deduplicates by `UniqueIdentifier` (`{Port}:{Name}`), then
  enriches each entry by hitting the Ultimaker REST API at `http://{ip}:{port}/api/v1/...`
  (firmware, variant, guid, status, print progress, hotend/bed temperatures via
  [PrinterData](UltimakerPrinterDeviceManager/Models/PrinterData.cs)). State is persisted to
  `printers.json` under `LocalApplicationData/UltimakerPrinterDeviceManager` (override with
  config key `PrinterRepository:DataPath`). A `PeriodicTimer` re-checks every 10 s and flips
  online/offline.

### Data flow

```text
mDNS (LAN) → PrinterDiscoveryService → RabbitMQ "discoveredPrinterEvents"
                                            ↓
                              UltimakerPrinterDeviceManager
                                  ↓                ↓
                       Ultimaker HTTP API     printers.json
```

The bus contract is the `DiscoveredPrinter` POCO serialized with `System.Text.Json`. Both
producer and consumer reference the same class from `ServiceDefaults`, so changes to that
shape are a breaking wire change — keep producer and consumer in sync.

### Things that look surprising but are intentional

- The consumer `BasicAck`s even on validation failure to drain poison messages rather than
  loop on them (see [PrinterManagerWorker.ProcessMessageAsync](UltimakerPrinterDeviceManager/PrinterManagerWorker.cs)).
- Status-check loop only re-queries printers currently marked online; offline printers come
  back via a fresh mDNS event, not via polling.
- `UltimakerPrinter.UniqueIdentifier` is `Port:Name`, not the API-reported serial. Serial is
  populated *after* the first successful API call, so it cannot be used as the primary key.

## Conventions

- `UltimakerManager.code-workspace` is tracked on purpose: a `!UltimakerManager.code-workspace` line after the
  repo-init block in `.gitignore` overrides its `*.code-workspace` rule.
- `.markdownlint.json` also turns off MD060 (table pipe alignment): padded and unpadded GFM tables render the same.
