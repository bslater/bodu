# Bodu.Globalization.Calendar.Europe

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

Holiday and observance data for the 27 EU member states and the United Kingdom, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack - national rules plus, where authored, ISO 3166-2 subdivisions - that imports the shared core, Western Christian, family, and cultural catalogues through the `europe-common` hub. The Orthodox countries (Bulgaria, Cyprus, Greece, Romania) define their Orthodox Easter cycle in their own packs.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Europe
```

Targets `net8.0` and `net10.0`.

## Supported territories

`AT`, `BE`, `BG`, `CY`, `CZ`, `DE`, `DK`, `EE`, `ES`, `FI`, `FR`, `GB`, `GR`, `HR`, `HU`, `IE`, `IT`, `LT`, `LU`, `LV`, `MT`, `NL`, `PL`, `PT`, `RO`, `SE`, `SI`, `SK` - 28 territories.

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = EuropeCalendarData.CreateService("DE");
IReadOnlyList<NotableDate> holidays = service.Resolve(2026, "DE");   // every occurrence in 2026
```

The `EuropeCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Subdivision rules (the German *Länder*, the UK nations, and the Alsace-Moselle *départements*) resolve automatically when a territory code carries an ISO 3166-2 subdivision.

## Building and testing

To build this package from source and run its tests, see [CONTRIBUTING.md](https://github.com/bslater/bodu/blob/master/CONTRIBUTING.md). The published vectors and corpora the tests hold it to are listed under [conformance and test vectors](https://bslater.github.io/bodu/docs/topics/conformance.html).

## License

MIT. © Bodu Pty. Ltd.
