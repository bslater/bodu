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

## Per-bundle reference

Each bundle exposes a single static factory class with the same shape: per-country `Create<Country>Provider()` methods, a `CreateProviders()` enumeration that yields every supported country in a stable order, an advanced `CreateProvider(resourceName)` escape hatch, public `<Country>ResourceName` constants, and a `DataAssembly` property naming the host assembly.

### Americas — `AmericasCalendarData` {#americas}

**Package:** `Bodu.Globalization.Calendar.Data.Americas`
**Namespace:** `Bodu.Globalization.Calendar.Data`
**Factory:** <xref:Bodu.Globalization.Calendar.Data.AmericasCalendarData>

| Country | Factory | Embedded resource |
|---|---|---|
| United States (`US`) | `AmericasCalendarData.CreateUnitedStatesProvider()` | `Bodu/Globalization/Calendar/Resources/region-us.xml` |
| Canada (`CA`) | `AmericasCalendarData.CreateCanadaProvider()` | `Bodu/Globalization/Calendar/Resources/region-ca.xml` |

`CreateProviders()` yields the providers in the order **US, CA**. National rules cover the federal calendar; subdivision rules (e.g. `US-CA` for California, `CA-ON` for Ontario) follow ISO 3166-2 conventions where present in the resource. No external algorithms are required — all rules resolve through `Fixed`, `DayOfWeekInMonth`, or `OffsetFromAnchor` (typically Easter, supplied by the main library).

### Asia-Pacific — `AsiaPacificCalendarData` {#asia-pacific}

**Package:** `Bodu.Globalization.Calendar.Data.AsiaPacific`
**Namespace:** `Bodu.Globalization.Calendar.Data`
**Factory:** <xref:Bodu.Globalization.Calendar.Data.AsiaPacificCalendarData>

| Country | Factory | Embedded resource |
|---|---|---|
| Australia (`AU`) | `AsiaPacificCalendarData.CreateAustraliaProvider()` | `Bodu/Globalization/Calendar/Resources/region-au.xml` |
| China (`CN`) | `AsiaPacificCalendarData.CreateChinaProvider()` | `Bodu/Globalization/Calendar/Resources/region-cn.xml` |
| India (`IN`) | `AsiaPacificCalendarData.CreateIndiaProvider()` | `Bodu/Globalization/Calendar/Resources/region-in.xml` |
| Japan (`JP`) | `AsiaPacificCalendarData.CreateJapanProvider()` | `Bodu/Globalization/Calendar/Resources/region-jp.xml` |
| South Korea (`KR`) | `AsiaPacificCalendarData.CreateSouthKoreaProvider()` | `Bodu/Globalization/Calendar/Resources/region-kr.xml` |
| Malaysia (`MY`) | `AsiaPacificCalendarData.CreateMalaysiaProvider()` | `Bodu/Globalization/Calendar/Resources/region-my.xml` |
| New Zealand (`NZ`) | `AsiaPacificCalendarData.CreateNewZealandProvider()` | `Bodu/Globalization/Calendar/Resources/region-nz.xml` |
| Singapore (`SG`) | `AsiaPacificCalendarData.CreateSingaporeProvider()` | `Bodu/Globalization/Calendar/Resources/region-sg.xml` |

`CreateProviders()` yields the providers in the order **AU, CN, IN, JP, KR, MY, NZ, SG**.

**Algorithm dependencies.** Many Asia-Pacific rules delegate to `INotableDateAlgorithm` implementations from `Bodu.Globalization.Calendar.Algorithms` — Lunar New Year and Mid-Autumn Festival (Chinese lunisolar), Vesak and Asalha Puja (lunar), Qingming (solar term), Hindu festivals (lunisolar), Buddhist observances. Register the algorithms before constructing the service; missing algorithms silently produce no occurrences without raising an error. See [Date calculation algorithms](algorithms.md) for the full algorithm-key table.

### Europe — `EuropeCalendarData` {#europe}

**Package:** `Bodu.Globalization.Calendar.Data.Europe`
**Namespace:** `Bodu.Globalization.Calendar.Data`
**Factory:** <xref:Bodu.Globalization.Calendar.Data.EuropeCalendarData>

| Country | Factory | Embedded resource |
|---|---|---|
| Germany (`DE`) | `EuropeCalendarData.CreateGermanyProvider()` | `Bodu/Globalization/Calendar/Resources/region-de.xml` |
| Spain (`ES`) | `EuropeCalendarData.CreateSpainProvider()` | `Bodu/Globalization/Calendar/Resources/region-es.xml` |
| France (`FR`) | `EuropeCalendarData.CreateFranceProvider()` | `Bodu/Globalization/Calendar/Resources/region-fr.xml` |
| United Kingdom (`GB`) | `EuropeCalendarData.CreateUnitedKingdomProvider()` | `Bodu/Globalization/Calendar/Resources/region-gb.xml` |
| Ireland (`IE`) | `EuropeCalendarData.CreateIrelandProvider()` | `Bodu/Globalization/Calendar/Resources/region-ie.xml` |
| Italy (`IT`) | `EuropeCalendarData.CreateItalyProvider()` | `Bodu/Globalization/Calendar/Resources/region-it.xml` |
| Netherlands (`NL`) | `EuropeCalendarData.CreateNetherlandsProvider()` | `Bodu/Globalization/Calendar/Resources/region-nl.xml` |
| Sweden (`SE`) | `EuropeCalendarData.CreateSwedenProvider()` | `Bodu/Globalization/Calendar/Resources/region-se.xml` |

`CreateProviders()` yields the providers in the order **DE, ES, FR, GB, IE, IT, NL, SE**.

European national rules typically cover the federal calendar plus the major regional variants (`GB-ENG`, `GB-SCT`, `GB-NIR`, `DE-BY` for Bavaria, etc.). All rules resolve through `Fixed`, `DayOfWeekInMonth`, or `OffsetFromAnchor` (most commonly Gregorian Easter, supplied by the main library) — no external algorithms are required for the shipped European resources.

## Where to go next

- [Using NotableDateService](notable-dates.md) — querying patterns, filters, range queries, and overrides.
- [Authoring notable date rules](rule-authoring.md) — in-code, XML, and companion-assembly authoring patterns.
- [Territories and regional composition](territories.md) — ISO 3166 codes, subdivision patterns, and the containment rules that drive cross-pack queries.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full type reference.
