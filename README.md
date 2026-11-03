# UltimakerManager

Distributed background service that discovers Ultimaker 3D printers on the local
network via mDNS and continuously enriches their state by polling the Ultimaker
HTTP API. Built on .NET 10 and orchestrated with [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/).

## Architecture

```text
mDNS (LAN) → PrinterDiscoveryService → RabbitMQ "discoveredPrinterEvents"
                                              ↓
                                UltimakerPrinterDeviceManager
                                    ↓                ↓
                         Ultimaker HTTP API     printers.json
```

| Project                            | Role                                                                        |
|------------------------------------|-----------------------------------------------------------------------------|
| `UltimakerManager.AppHost`         | Aspire orchestrator — starts Seq, RabbitMQ, and both worker services.       |
| `UltimakerManager.ServiceDefaults` | Shared library: OpenTelemetry, health checks, cross-service DTOs.           |
| `PrinterDiscoveryService`          | Worker — multicast-DNS scan for `_ultimaker._tcp.local`, publishes to RMQ.  |
| `UltimakerPrinterDeviceManager`    | Worker — consumes RMQ, queries the printer REST API, persists state.       |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop (Aspire provisions Seq and RabbitMQ as containers)
- An Ultimaker printer reachable on the local network (for end-to-end testing)

## Running

RabbitMQ requires credentials. Set them once via user-secrets on the AppHost:

```bash
dotnet user-secrets --project UltimakerManager.AppHost set Parameters:rmq-username <user>
dotnet user-secrets --project UltimakerManager.AppHost set Parameters:rmq-password <pass>
```

Then launch the whole system through Aspire:

```bash
dotnet run --project UltimakerManager.AppHost
```

The Aspire dashboard URL is printed on stdout. Seq and RabbitMQ data is persisted
in named Docker volumes, so logs and queues survive restarts.

## Persistence

`UltimakerPrinterDeviceManager` writes discovered printers to `printers.json` under
`%LOCALAPPDATA%\UltimakerPrinterDeviceManager`. Override the directory by setting the
configuration key `PrinterRepository:DataPath`.

## Repository layout

- `UltimakerManager.sln` — solution containing all four projects
- `UltimakerManager.AppHost/` — Aspire host (entry point for development)
- `UltimakerManager.ServiceDefaults/` — shared infrastructure code and DTOs
- `PrinterDiscoveryService/` — mDNS discovery worker
- `UltimakerPrinterDeviceManager/` — RMQ consumer + REST poller worker
- `CHANGELOG.md` — release history
- `AGENTS.md` — instructions for AI coding agents (`CLAUDE.md` only imports it)

For deeper guidance aimed at AI coding agents, see [AGENTS.md](AGENTS.md).

## License

See [LICENSE.txt](LICENSE.txt).

## Conventions

- **Branching:** GitHub Flow. `main` is protected and accepts changes only through pull requests.
- **Markdown:** linted with `markdownlint-cli2`, 180 character limit.

  ```sh
  npx markdownlint-cli2 "**/*.md"
  ```

- **Agent instructions:** in [AGENTS.md](AGENTS.md), read by Claude Code, Codex and any other coding agent.
  `CLAUDE.md` only imports it, so edit `AGENTS.md`.
