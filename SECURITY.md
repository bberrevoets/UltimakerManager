# Security Policy

UltimakerManager is a hobby experiment by Bert Berrevoets (Berrevoets Systems). It runs only on a development PC
through the Aspire AppHost and is not deployed or released anywhere.

## Supported versions

Only the latest commit on `main` is maintained. There are no releases, so there are no older versions to patch.

## Reporting a vulnerability

Please do not open a public issue for a security problem.

- **Preferred:** use GitHub's private form,
  [Report a vulnerability](https://github.com/bberrevoets/UltimakerManager/security/advisories/new). Only the
  maintainer can read the report.
- **Or by email:** <bert@berrevoets.net>.

Include what you found, the steps to reproduce it and, if you know it, the package or file involved.

## What to expect

This project is maintained in spare time. You can expect a first reply within about a week. A confirmed problem is
fixed on `main` and noted in [CHANGELOG.md](CHANGELOG.md) under "Security".

## Dependencies

Dependabot alerts and security updates are enabled, so known vulnerabilities in NuGet packages are picked up
automatically. A vulnerability in a dependency is best reported to that project.
