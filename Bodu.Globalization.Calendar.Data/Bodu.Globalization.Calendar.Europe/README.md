# Bodu.Globalization.Calendar.Europe

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Holiday and observance data for the EU / EEA territories, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack — national rules plus ISO 3166-2 subdivisions — that imports the shared Western, Orthodox, and Catholic Christian catalogues through the `europe-common` hub.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Europe
```

Targets `net8.0`.

## Supported territories

`AT`, `BE`, `BG`, `CY`, `CZ`, `DE`, `DK`, `EE`, `ES`, `FI`, `FR`, `GB`, `GR`, `HR`, `HU`, `IE`, `IT`, `LT`, `LU`, `LV`, `MT`, `NL`, `PL`, `PT`, `RO`, `SE`, `SI`, `SK` — 28 territories.

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = EuropeCalendarData.CreateService("DE");
var holidays = service.GetNotableDates(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
```

The `EuropeCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Subdivision rules (e.g. German *Land* or Spanish *comunidad autónoma* holidays) resolve automatically when a territory code carries an ISO 3166-2 subdivision.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Data/Bodu.Globalization.Calendar.Europe/test/Bodu.Globalization.Calendar.Europe.Test.csproj --settings bvt.runsettings
```

`EuropeCalendarDataTests` pins every floating or computed holiday (Western and Orthodox Easter offsets, nth-weekday rules) to confirmed published dates, plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test.

## License

MIT. © Bodu Pty. Ltd.
