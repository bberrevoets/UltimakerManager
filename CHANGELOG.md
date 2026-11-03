# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Attributors

- Bert Berrevoets <bert@berrevoets.net>
- @dependabot
- Claude Opus 4.7 -- co-author of the 2026-04 docs and .NET 10 upgrade
- Claude Opus 5.5 -- co-author of the 2026-11 AI agent setup

## [Unreleased]

### Added

Author: *Bert Berrevoets*

- `CLAUDE.md` with architecture and command guidance for AI assistants working in the repo (moved to `AGENTS.md`
  in the 2026-11 AI agent setup, see below).
- `CHANGELOG.md` (this file).
- Expanded `README.md` covering architecture, prerequisites, run instructions, and repo layout.
- `UltimakerManager.code-workspace` checked into source control.

### Changed

Author: *Bert Berrevoets*

- Replaced `.gitignore` with the canonical
  [GitHub VisualStudio.gitignore](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore)
  template plus project-specific entries for JetBrains Rider (`.idea/`, `*.iml`, `riderModule.iml`)
  and the local `.claude/` agent workspace.
- Untracked the previously committed `.idea/` files; they are now correctly ignored.
- Added `.markdownlint.jsonc` enforcing a 120-character line limit, with relaxations for
  Keep-a-Changelog duplicate sub-headings (`MD024 siblings_only`) and GFM table pipe alignment
  (`MD060` disabled).
- Upgraded all four projects from `net9.0` to `net10.0`.
- Upgraded the Aspire stack from 9.x to 13.2.4 (`Aspire.AppHost.Sdk`, `Aspire.Hosting.AppHost`,
  `Aspire.Hosting.RabbitMQ`, `Aspire.Hosting.Seq`, `Aspire.RabbitMQ.Client`, `Aspire.Seq`).
- Bumped `Microsoft.Extensions.Hosting` 9.0.4 → 10.0.7, `Microsoft.Extensions.Http.Resilience`
  and `Microsoft.Extensions.ServiceDiscovery` 9.0.0 → 10.5.0, and the OpenTelemetry
  instrumentation packages 1.9.0 → 1.15.x to match the already-bumped exporter.
- Migrated both worker services to `RabbitMQ.Client` 7.x: `IModel` → `IChannel`, all queue and
  publish/consume operations now async (`CreateChannelAsync`, `QueueDeclareAsync`,
  `BasicPublishAsync`, `BasicConsumeAsync`, `BasicAckAsync`), and `EventingBasicConsumer` →
  `AsyncEventingBasicConsumer` with the new `ReceivedAsync` event handler.
- AI agent setup (2026-11): the instructions moved from `CLAUDE.md` to `AGENTS.md` (single source for Claude Code,
  Codex and other agents) with a `CLAUDE.md` stub that imports it, a Project Overview and Conventions section;
  `.markdownlint.jsonc` became `.markdownlint.json` with a 180-character line limit (same rules otherwise);
  `.gitattributes` with the standard Git LFS patterns; repo-init blocks in `.gitignore` (the tracked
  `UltimakerManager.code-workspace` stays tracked through an exception line) and a Conventions section in
  `README.md`.

### Fixed

Author: *Bert Berrevoets*

- The workers start again after the Aspire 13.6.4 upgrade (found in a test run on 2026-11-03):
  - the Seq endpoint is proxied (`IsProxied = true`): Aspire 13.6 maps container ports to `127.0.0.1` only, the Seq
    health check on `localhost` timed out on `::1`, and `WaitFor(seq)` kept both workers waiting;
  - the queue `discoveredPrinterEvents` is declared durable in both workers: RabbitMQ 4.3, the new default image,
    closed the connection with `541 INTERNAL_ERROR - Feature transient_nonexcl_queues is deprecated`.

### Security

Author: *Bert Berrevoets*

- Upgraded the Aspire stack from 13.2.4 to 13.6.4 (`Aspire.AppHost.Sdk` and all six `Aspire.*` packages). The
  AppHost pulled `MessagePack` 2.5.192 in transitively (`Aspire.Hosting` → `StreamJsonRpc`), which has 2 high and
  10 moderate advisories (fixed from 2.5.303); 13.6.4 brings 2.5.307. Dependabot did not report it because the
  package is transitive only.

## [2026-04-30]

### Changed

Author: *@dependabot*

- Bumped `OpenTelemetry.Exporter.OpenTelemetryProtocol` from 1.9.0 to 1.15.3
  in `UltimakerManager.ServiceDefaults` ([#1](https://github.com/bberrevoets/UltimakerManager/pull/1)).

## [2025-04-27]

### Added

Author: *Bert Berrevoets*

- `LICENSE.txt`.
- Seq logging endpoint wired through Aspire in `UltimakerManager.AppHost` and consumed by both worker services.

### Changed

Author: *Bert Berrevoets*

- Refactored printer management: introduced `IPrinterRepository` / `PrinterRepository` (JSON-file persistence
  under `%LOCALAPPDATA%`) and `IPrinterApiClient` / `UltimakerPrinterApiClient` (Ultimaker REST polling).
- Added `PrinterManagerWorker` consuming the `discoveredPrinterEvents` RabbitMQ queue, enriching printer
  state from the Ultimaker `/api/v1` endpoints, and re-checking online printers on a 10-second timer.
- Added `PrinterData` model mirroring the Ultimaker printer API response shape.
- Improved logging across all services.

## [2025-04-26]

### Added

Author: *Bert Berrevoets*

- Initial .NET Aspire solution: `UltimakerManager.AppHost`, `UltimakerManager.ServiceDefaults`,
  `PrinterDiscoveryService`, `UltimakerPrinterDeviceManager`.
- mDNS-based printer discovery in `PrinterDiscoveryService` using `Makaretu.Dns`, querying for
  `_ultimaker._tcp.local` and publishing `DiscoveredPrinter` messages to RabbitMQ.
- Shared DTOs `DiscoveredPrinter` and `UltimakerPrinter` in `UltimakerManager.ServiceDefaults.Models`.
- Default OpenTelemetry, health-check, and service-discovery wiring via `AddServiceDefaults`.
