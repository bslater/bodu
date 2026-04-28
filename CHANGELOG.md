# Changelog

All notable changes to this repository are documented here. Version numbers refer to NuGet package releases.

The format is loosely based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this repository follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Bodu.Globalization.Calendar — 2.0.0

#### Added

- `XmlResourceNotableDateRuleProvider` gains a new constructor accepting an ordered `IEnumerable<Assembly>`. The provider walks the chain when resolving manifest resources, so a `<UseFrom>` directive declared in one assembly can cherry-pick rules from another. The legacy single-assembly constructor is preserved as a thin shim and remains source-compatible.
- A new embedded `default-minimal.xml` resource carrying a single rule for New Year's Day, so the parameterless `new NotableDateService()` constructor still produces a usable service when no companion data pack is referenced.

#### Changed

- **Breaking.** The parameterless `new NotableDateService()` constructor no longer loads the embedded global rule set. It now loads only `default-minimal.xml` (currently New Year's Day). National public-holiday data must be supplied by referencing one of the new `Bodu.Globalization.Calendar.Data.*` companion packages and passing its provider(s) to the full constructor.
- The `FileNotFoundException_EmbeddedXmlResourceNotFound` diagnostic message now reads `…not found in any of the searched assemblies: {1}.` to remain grammatical when the provider's assembly chain has more than one entry.

#### Removed

- All 17 `region-*.xml` resources (au, ca, cn, de, es, fr, gb, ie, in, it, jp, kr, my, nl, nz, se, sg, us) are no longer embedded in `Bodu.Globalization.Calendar.dll`. They ship in the new region-specific data packs listed below.

#### Migration

If your code constructs `new NotableDateService()` and queries regional holidays, install the relevant data pack(s) and pass providers from the matching `<Pack>CalendarData` factory to the full constructor:

```csharp
// Before — relied on the main library shipping every region's rules:
var service = new NotableDateService();
var auDates = service.GetNotableDates(2026, "AU");

// After — install Bodu.Globalization.Calendar.Data.AsiaPacific and pass its provider:
using Bodu.Globalization.Calendar.Data.AsiaPacific;

var service = new NotableDateService(
    ruleProviders:     new[] { AsiaPacificCalendarData.CreateAustraliaProvider() },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);
var auDates = service.GetNotableDates(2026, "AU");
```

Use `<Pack>CalendarData.CreateProviders()` to enumerate every country in a pack at once. See the [Calendar data packs](docs/guides/calendar/data-packs.md) guide for full composition patterns.

### Bodu.Globalization.Calendar.Data.Americas — 1.0.0

Initial release. Ships embedded notable-date rules for the United States (`US`) and Canada (`CA`). Exposes a static `AmericasCalendarData` factory with per-country `CreateUnitedStatesProvider()` / `CreateCanadaProvider()` and bulk `CreateProviders()` helpers that pre-wire the `[pack, main library]` assembly chain.

### Bodu.Globalization.Calendar.Data.Europe — 1.0.0

Initial release. Ships embedded notable-date rules for Germany (`DE`), Spain (`ES`), France (`FR`), the United Kingdom (`GB`), Ireland (`IE`), Italy (`IT`), the Netherlands (`NL`), and Sweden (`SE`). Exposes a static `EuropeCalendarData` factory with per-country and bulk `CreateProviders()` helpers.

### Bodu.Globalization.Calendar.Data.AsiaPacific — 1.0.0

Initial release. Ships embedded notable-date rules for Australia (`AU`), China (`CN`), India (`IN`), Japan (`JP`), South Korea (`KR`), Malaysia (`MY`), New Zealand (`NZ`), and Singapore (`SG`). Exposes a static `AsiaPacificCalendarData` factory with per-country and bulk `CreateProviders()` helpers.

> **Note.** Several rules in the Asia-Pacific pack (lunar, Hindu, Islamic, Buddhist) require corresponding `INotableDateAlgorithm` implementations registered with the algorithm registry. Without those registrations, the affected rules silently produce no occurrences; the remaining Western/Gregorian rules behave unaffected.
