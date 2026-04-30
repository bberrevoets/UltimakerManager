# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common commands

Run from the solution root.

- Build everything: `dotnet build UltimakerManager.sln`
- Run the full distributed app (Aspire orchestrates Seq + RabbitMQ containers and both worker
  services, exposes the Aspire dashboard): `dotnet run --project UltimakerManager.AppHost`
- Run a single worker outside Aspire (loses service discovery / config wiring; rarely useful):
  `dotnet run --project PrinterDiscoveryService` or
  `dotnet run --project UltimakerPrinterDeviceManager`

There is currently no test project, no linter config, and no CI. Docker Desktop must be
running because AppHost provisions Seq and RabbitMQ as persistent-lifetime containers.

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
