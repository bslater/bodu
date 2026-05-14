---
title: Calendar data packs
---

# Calendar data packs

Region-specific notable-date rules ship as separate **companion data assemblies** so national public-holiday data can be re-released without recompiling `Bodu.Globalization.Calendar`. The main library embeds only a minimal default rule (New Year's Day) plus the global, religious, and categorical rule sources that regional packs cherry-pick from.

## Available packs

| Package | Countries |
|---|---|
| **Bodu.Globalization.Calendar.Data.Americas** | United States (`US`), Canada (`CA`) |
| **Bodu.Globalization.Calendar.Data.Europe** | Germany (`DE`), Spain (`ES`), France (`FR`), United Kingdom (`GB`), Ireland (`IE`), Italy (`IT`), Netherlands (`NL`), Sweden (`SE`) |
| **Bodu.Globalization.Calendar.Data.AsiaPacific** | Australia (`AU`), China (`CN`), India (`IN`), Japan (`JP`), South Korea (`KR`), Malaysia (`MY`), New Zealand (`NZ`), Singapore (`SG`) |

Each pack depends on `Bodu.Globalization.Calendar` and embeds only the `region-XX.xml` files for its countries. Cross-pack references are not used — every region XML cherry-picks its global anchors from the main library, which the provider's assembly chain resolves automatically.

National rules are authored at the country level (`AU`, `US`, `GB`); state / province / region variants use the canonical ISO 3166-2 subdivision suffix (`AU-NSW`, `US-CA`, `GB-SCT`). See [Territories and regional composition](territories.md) for the parsing, containment, and composition rules that govern how these codes interact at query time.

## Install

```bash
# Reference the main library for the resolution pipeline:
dotnet add package Bodu.Globalization.Calendar

# Add one or more data packs depending on the regions you need:
dotnet add package Bodu.Globalization.Calendar.Data.Americas
dotnet add package Bodu.Globalization.Calendar.Data.Europe
dotnet add package Bodu.Globalization.Calendar.Data.AsiaPacific
```

## Wire one pack into a service

Each pack exposes a static `<Pack>CalendarData` factory. Use a per-country helper when you only need a single market:

```csharp
using Bodu.Extensions;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.Americas;

var service = new NotableDateService(
    ruleProviders:     new[] { AmericasCalendarData.CreateUnitedStatesProvider() },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

IReadOnlyList<NotableDate> july4 = service.GetNotableDates(new DateTime(2026, 7, 4), "US");
// → Independence Day, 4 July 2026
```

## Wire several packs into a service

`CreateProviders()` returns one provider per country in the pack. Concatenate across packs to ship a single service instance that knows about every market you care about:

```csharp
using System.Linq;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.Americas;
using Bodu.Globalization.Calendar.Data.AsiaPacific;
using Bodu.Globalization.Calendar.Data.Europe;

var providers = AmericasCalendarData.CreateProviders()
    .Concat(EuropeCalendarData.CreateProviders())
    .Concat(AsiaPacificCalendarData.CreateProviders());

var service = new NotableDateService(
    ruleProviders:     providers,
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);
```

A single query then resolves cleanly across every loaded pack:

```csharp
IReadOnlyList<NotableDate> christmas = service.GetNotableDates(new DateTime(2026, 12, 25), "GB");
IReadOnlyList<NotableDate> bastille  = service.GetNotableDates(new DateTime(2026, 7, 14),  "FR");
IReadOnlyList<NotableDate> auDay     = service.GetNotableDates(new DateTime(2026, 1, 26),  "AU");
```

## How the assembly chain works

Each pack helper constructs an `XmlResourceNotableDateRuleProvider` with an ordered chain of assemblies:

```csharp
new XmlResourceNotableDateRuleProvider(
    resourceName,
    new ResourcePathResolver(),
    new[] { typeof(AmericasCalendarData).Assembly, typeof(NotableDateService).Assembly });
```

When the provider flattens the rule graph it walks the chain in order:

1. The pack assembly is searched first — it carries `region-us.xml`, `region-ca.xml`, etc. under their original `Bodu.Globalization.Calendar.Resources.region-XX.xml` manifest names.
2. The main library is searched as a fallback — it carries every `global-*.xml`, `christian-*.xml`, the schema, and `default-minimal.xml`.

That fallback is what lets a region XML cherry-pick global anchors with `<UseFrom resource="./global-all.xml">` even though that file lives in a different DLL.

## Using a pack alongside an algorithm registry

Several Asia-Pacific calendars resolve through pluggable algorithms (lunar, Hindu, Islamic, Buddhist). Without those algorithms registered, the affected rules silently produce no occurrences; everything else still resolves.

```csharp
using Bodu.Extensions;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.AsiaPacific;

var registry = new NotableDateAlgorithmRegistry()
    .Register("hindu-lunar",  new HinduLunarAlgorithm())
    .Register("vesak",        new VesakAlgorithm());

var service = new NotableDateService(
    ruleProviders:     AsiaPacificCalendarData.CreateProviders(),
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
```

See [Date calculation algorithms](algorithms.md) for the full list of algorithm keys each pack expects.

## Mixing in a custom pack

Each `<Pack>CalendarData.CreateProvider(string)` method also accepts a logical resource name, so you can build a provider over a custom XML file you've embedded in your own assembly **without rebuilding a chain by hand**. For complete control — for example, to layer your own DLL ahead of the official pack — call the multi-assembly constructor directly:

```csharp
var provider = new XmlResourceNotableDateRuleProvider(
    "MyApp/Calendar/Resources/region-us.xml",
    new ResourcePathResolver(),
    new[] {
        typeof(MyAppRules).Assembly,           // overrides win first
        typeof(AmericasCalendarData).Assembly, // standard US pack
        typeof(NotableDateService).Assembly,   // global anchors
    });
```

The provider streams the first match it finds, so anything you place in the leading assembly under the same logical name supersedes the pack version.

## Where to go next

- [Using NotableDateService](notable-dates.md) — querying patterns, filters, range queries, and overrides.
- [Authoring notable date rules](rule-authoring.md) — in-code, XML, and companion-assembly authoring patterns.
- [Territories and regional composition](territories.md) — ISO 3166 codes, subdivision patterns, and the containment rules that drive cross-pack queries.
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full type reference.
