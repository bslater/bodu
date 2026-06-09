# Bodu.Globalization.Calendar.Americas

Holiday and observance data for the Americas, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack — national rules plus ISO 3166-2 subdivisions — that imports the shared faith and civil catalogues through the `americas-common` hub.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Americas
```

Targets `net8.0`.

## Supported territories

`AR` (Argentina), `BR` (Brazil), `CA` (Canada), `CL` (Chile), `CO` (Colombia), `MX` (Mexico), `PE` (Peru), `US` (United States).

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = AmericasCalendarData.CreateService("US");
var holidays = service.GetNotableDates(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
```

The `AmericasCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Subdivision rules (state / provincial holidays) resolve automatically when a territory code carries an ISO 3166-2 subdivision.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Data/Bodu.Globalization.Calendar.Americas/test/Bodu.Globalization.Calendar.Americas.Test.csproj --settings bvt.runsettings
```

`AmericasCalendarDataTests` pins every floating or computed holiday (Easter offsets, nth-weekday rules, weekend-substitution shifts) to confirmed published dates, plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test.

## License

MIT. © Bodu Pty. Ltd.
