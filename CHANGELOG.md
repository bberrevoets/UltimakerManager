# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Attributors

- Bert Berrevoets <bert@berrevoets.net>
- @dependabot

## [Unreleased]

### Added

Author: *Bert Berrevoets*

- `CLAUDE.md` with architecture and command guidance for AI assistants working in the repo.
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
