# Bodu.Globalization.Calendar.MiddleEast

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

Holiday and observance data for the Middle East, packaged for the `Bodu.Globalization.Calendar` engine. Each supported country ships as a self-contained embedded notable-date pack - national rules plus ISO 3166-2 subdivisions - that draws on the shared catalogues: the UAE, Jordan, Qatar, and Turkey packs import the tabular Islamic catalogue through the `middleeast-common` hub, Saudi Arabia imports the Umm al-Qura variant directly, and Israel imports the Jewish catalogue directly. Working-week definitions are territory-specific (Israel, Jordan, Qatar, and Saudi Arabia observe a Friday-Saturday weekend).

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.MiddleEast
```

Targets `net8.0` and `net10.0`.

## Supported territories

`AE` (United Arab Emirates), `IL` (Israel), `JO` (Jordan), `QA` (Qatar), `SA` (Saudi Arabia), `TR` (Turkey).

## Usage

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = MiddleEastCalendarData.CreateService("AE");
IReadOnlyList<NotableDate> holidays = service.Resolve(2026, "AE");   // every occurrence in 2026
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

`MiddleEastCalendarDataTests` pins every floating or computed holiday (Hijri and Hebrew festivals, weekend-substitution shifts) to confirmed published dates - exact where deterministic, with a ±2-day tolerance for moon-sighting festivals - plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test.

## License

MIT. © Bodu Pty. Ltd.
