# Bodu.Globalization.Calendar.AsiaPacific

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

Holiday and observance data for the Asia-Pacific region, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack - national rules plus ISO 3166-2 subdivisions - that imports the shared faith and civil catalogues (including the lunar, Hindu, Buddhist, and Islamic calendars).

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
```

Targets `net8.0` and `net10.0`.

## Supported territories

`AU` (Australia), `CN` (China), `HK` (Hong Kong), `ID` (Indonesia), `IN` (India), `JP` (Japan), `KR` (South Korea), `MY` (Malaysia), `NZ` (New Zealand), `PH` (Philippines), `SG` (Singapore), `TH` (Thailand), `TW` (Taiwan), `VN` (Vietnam).

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = AsiaPacificCalendarData.CreateService("JP");
IReadOnlyList<NotableDate> holidays = service.Resolve(2026, "JP");   // every occurrence in 2026
```

The `AsiaPacificCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Subdivision rules (state / provincial holidays) resolve automatically when a territory code carries an ISO 3166-2 subdivision.

## Building and testing

To build this package from source and run its tests, see [CONTRIBUTING.md](https://github.com/bslater/bodu/blob/master/CONTRIBUTING.md). The published vectors and corpora the tests hold it to are listed under [conformance and test vectors](https://bslater.github.io/bodu/docs/topics/conformance.html).

## License

MIT. © Bodu Pty. Ltd.
