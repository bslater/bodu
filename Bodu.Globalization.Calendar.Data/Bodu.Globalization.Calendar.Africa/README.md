# Bodu.Globalization.Calendar.Africa

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

Holiday and observance data for Africa, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack - national rules plus ISO 3166-2 subdivisions - that imports the shared Christian (Western and Orthodox) and Islamic catalogues through the `africa-common` hub. Working-week definitions are territory-specific.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Africa
```

Targets `net8.0` and `net10.0`.

## Supported territories

`EG` (Egypt), `ET` (Ethiopia), `GH` (Ghana), `KE` (Kenya), `MA` (Morocco), `NG` (Nigeria), `ZA` (South Africa).

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = AfricaCalendarData.CreateService("ZA");
IReadOnlyList<NotableDate> holidays = service.Resolve(2026, "ZA");   // every occurrence in 2026
```

The `AfricaCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Each country's working-week definition (and any weekend-substitution rules) is encoded in its pack and applied automatically by the working-day extensions.

## Building and testing

To build this package from source and run its tests, see [CONTRIBUTING.md](https://github.com/bslater/bodu/blob/master/CONTRIBUTING.md). The published vectors and corpora the tests hold it to are listed under [conformance and test vectors](https://bslater.github.io/bodu/docs/topics/conformance.html).

## License

MIT. © Bodu Pty. Ltd.
