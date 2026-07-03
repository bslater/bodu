# Bodu.Globalization.Calendar.Africa

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Holiday and observance data for Africa, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack — national rules plus ISO 3166-2 subdivisions — that imports the shared Christian (Western and Orthodox) and Islamic catalogues through the `africa-common` hub. Working-week definitions are territory-specific.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Africa
```

Targets `net8.0`.

## Supported territories

`EG` (Egypt), `ET` (Ethiopia), `GH` (Ghana), `KE` (Kenya), `MA` (Morocco), `NG` (Nigeria), `ZA` (South Africa).

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = AfricaCalendarData.CreateService("ZA");
var holidays = service.GetNotableDates(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
```

The `AfricaCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Each country's working-week definition (and any weekend-substitution rules) is encoded in its pack and applied automatically by the working-day extensions.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Data/Bodu.Globalization.Calendar.Africa/test/Bodu.Globalization.Calendar.Africa.Test.csproj --settings bvt.runsettings
```

`AfricaCalendarDataTests` pins every floating or computed holiday (Western and Orthodox Easter offsets, Hijri festivals, the Ethiopian calendar, weekend-substitution shifts) to confirmed published dates — exact where deterministic, with a ±2-day tolerance for moon-sighting festivals — plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test.

## License

MIT. © Bodu Pty. Ltd.
