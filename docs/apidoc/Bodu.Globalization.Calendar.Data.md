---
uid: Bodu.Globalization.Calendar.Data
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar.Data** is the namespace shared by the three regional calendar data bundles — `Bodu.Globalization.Calendar.Data.Americas`, `Bodu.Globalization.Calendar.Data.AsiaPacific`, and `Bodu.Globalization.Calendar.Data.Europe`. Each bundle ships embedded XML resources containing curated, per-country notable-date rules, exposed through a static factory class with identical shape: per-country `Create<Country>Provider()` methods, a `CreateProviders()` enumeration, and an advanced `CreateProvider(resourceName)` escape hatch.

The bundles are independent NuGet packages so consumers pull in only the regions they need. They are otherwise drop-in: register their providers with `NotableDateService` and the rules flow through the resolution pipeline like any other.

## Static documentation

- **[`Bodu.Globalization.Calendar` introduction](~/docs/calendar/index.md)** — how the bundles fit into the broader surface.
- **[Calendar data packs guide](~/guides/calendar/data-packs.md)** — installation, registration, and per-bundle coverage.

## Bundle factories

Each bundle exposes a single static factory class with parallel shape:

| Bundle | Factory class | Per-country providers |
|---|---|---|
| Americas | <xref:Bodu.Globalization.Calendar.Data.AmericasCalendarData> | United States, Canada |
| AsiaPacific | <xref:Bodu.Globalization.Calendar.Data.AsiaPacificCalendarData> | Australia, China, India, Japan, South Korea, Malaysia, New Zealand, Singapore |
| Europe | <xref:Bodu.Globalization.Calendar.Data.EuropeCalendarData> | Germany, Spain, France, United Kingdom, Ireland, Italy, Netherlands, Sweden |

Each factory class follows the same shape:

- `static Assembly DataAssembly` — the bundle's host assembly.
- `static INotableDateRuleProvider Create<Country>Provider()` — one method per supported country, returning an <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider> configured against the bundle's embedded XML resource.
- `static IEnumerable<INotableDateRuleProvider> CreateProviders()` — yields every supported country's provider in a stable order.
- `static INotableDateRuleProvider CreateProvider(string resourceName)` — advanced escape hatch for loading a named resource directly. Use the public `<Country>ResourceName` constants to pass the canonical resource path.
- `static string <Country>ResourceName` — public constant naming each embedded resource (e.g. `AmericasCalendarData.UnitedStatesResourceName` = `"Bodu/Globalization/Calendar/Resources/region-us.xml"`).

## Example

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data;

// Single country.
var service = new NotableDateService(
    ruleProviders: new[] { AmericasCalendarData.CreateUnitedStatesProvider() });

// Multiple regions composed.
var multi = new NotableDateService(
    ruleProviders: AmericasCalendarData.CreateProviders()
        .Concat(EuropeCalendarData.CreateProviders())
        .Concat(AsiaPacificCalendarData.CreateProviders()));

// Selective subset.
var selective = new NotableDateService(
    ruleProviders: new[]
    {
        AmericasCalendarData.CreateUnitedStatesProvider(),
        EuropeCalendarData.CreateUnitedKingdomProvider(),
        AsiaPacificCalendarData.CreateAustraliaProvider(),
    });
```

## Notes

- **Assembly-chain resolution.** Each provider is configured with an assembly chain `[DataAssembly, typeof(NotableDateService).Assembly]`, so `<UseFrom>` directives in region-specific rule files resolve their dependencies from the bundle first and the main library second.
- **Algorithm dependencies.** The Asia-Pacific bundle declares several rules that delegate to `INotableDateAlgorithm` implementations (Hindu festivals, Vesak, Asalha Puja, Losar, Qingming, Lunar New Year). The algorithms ship with the main `Bodu.Globalization.Calendar` package; missing algorithms silently produce no occurrences. Use <xref:Bodu.Globalization.Calendar.INotableDateAlgorithmRegistry> to register custom or replacement algorithms.
- **Per-country resources are independent.** Each `Create<Country>Provider()` returns its own provider instance — order them appropriately in your service constructor; providers are merged in registration order.
- **Independent release cadence.** Each bundle is its own NuGet package and ships independently of the main `Bodu.Globalization.Calendar` library — a refresh of Australian public holiday rules does not require a main-library re-release.
- **See also:** the [Calendar data packs guide](~/guides/calendar/data-packs.md), the [territories guide](~/guides/calendar/territories.md), the [`NotableDateService` reference](~/guides/calendar/notable-dates.md).
