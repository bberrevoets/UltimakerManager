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

- `.gitignore` now excludes the local `.claude/` directory.

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
