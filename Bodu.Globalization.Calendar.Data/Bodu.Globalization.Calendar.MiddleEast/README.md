# Bodu.Globalization.Calendar.MiddleEast

Holiday and observance data for the Middle East, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack — national rules plus ISO 3166-2 subdivisions — that imports the shared Islamic (including the Umm al-Qura variant) and Jewish catalogues through the `middleeast-common` hub. Working-week definitions are territory-specific (several territories observe a Friday–Saturday weekend).

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.MiddleEast
```

Targets `net8.0`.

## Supported territories

`AE` (United Arab Emirates), `IL` (Israel), `JO` (Jordan), `QA` (Qatar), `SA` (Saudi Arabia), `TR` (Turkey).

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = MiddleEastCalendarData.CreateService("AE");
var holidays = service.GetNotableDates(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
```

The `MiddleEastCalendarData` factory exposes:

| Member | Purpose |
|---|---|
| `SupportedCountries` | The ISO 3166-1 codes this pack resolves |
| `LoadResource(territory)` | The validated `NotableDateResource` for a country (and its subdivisions) |
| `CreateService(territory)` | A ready-to-use `INotableDateService` for the territory |

Each country's working-week definition (and any weekend-substitution rules) is encoded in its pack and applied automatically by the working-day extensions.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Data/Bodu.Globalization.Calendar.MiddleEast/test/Bodu.Globalization.Calendar.MiddleEast.Test.csproj --settings bvt.runsettings
```

`MiddleEastCalendarDataTests` pins every floating or computed holiday (Hijri and Hebrew festivals, weekend-substitution shifts) to confirmed published dates — exact where deterministic, with a ±2-day tolerance for moon-sighting festivals — plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test.

## License

MIT. © Bodu Pty. Ltd.
