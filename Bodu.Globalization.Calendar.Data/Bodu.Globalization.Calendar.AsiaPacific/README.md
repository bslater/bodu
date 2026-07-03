# Bodu.Globalization.Calendar.AsiaPacific

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Holiday and observance data for the Asia-Pacific region, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack — national rules plus ISO 3166-2 subdivisions — that imports the shared faith and civil catalogues (including the lunar, Hindu, Buddhist, and Islamic calendars).

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
```

Targets `net8.0`.

## Supported territories

`AU` (Australia), `CN` (China), `HK` (Hong Kong), `ID` (Indonesia), `IN` (India), `JP` (Japan), `KR` (South Korea), `MY` (Malaysia), `NZ` (New Zealand), `PH` (Philippines), `SG` (Singapore), `TH` (Thailand), `TW` (Taiwan), `VN` (Vietnam).

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = AsiaPacificCalendarData.CreateService("JP");
var holidays = service.GetNotableDates(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
```

The `AsiaPacificCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Subdivision rules (state / provincial holidays) resolve automatically when a territory code carries an ISO 3166-2 subdivision.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Data/Bodu.Globalization.Calendar.AsiaPacific/test/Bodu.Globalization.Calendar.AsiaPacific.Test.csproj --settings bvt.runsettings
```

`AsiaPacificCalendarDataTests` pins every floating or computed holiday (lunar-calendar festivals, Hijri / Hindu / Buddhist dates, nth-weekday rules, weekend-substitution shifts) to confirmed published dates — exact where deterministic, with a ±2-day tolerance for moon-sighting / astronomical festivals — plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test.

## License

MIT. © Bodu Pty. Ltd.
