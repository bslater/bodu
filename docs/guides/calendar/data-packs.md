---
title: Calendar data packs
---

# Calendar data packs

Region-specific notable-date rules ship as separate **companion data assemblies** so national public-holiday data can be re-released independently of `Bodu.Globalization.Calendar`. The main library embeds only a minimal default rule (New Year's Day) plus the bundled common catalogues (global, religious, and categorical) that the regional packs import from.

Each pack is a thin static facade: it loads a territory's embedded `region-<cc>.xml`, resolves its `<Imports>` against the bundled catalogues, validates, and hands back an immutable <xref:Bodu.Globalization.Calendar.NotableDateResource> — or a <xref:Bodu.Globalization.Calendar.NotableDateService> pre-wired over it.

## Available packs

All three packs live in the `Bodu.Globalization.Calendar.Data` namespace and expose the same shape.

| Package | Type | Territories |
|---|---|---|
| **Bodu.Globalization.Calendar.Data.Americas** | <xref:Bodu.Globalization.Calendar.AmericasCalendarData> | Canada (`CA`), United States (`US`) |
| **Bodu.Globalization.Calendar.Data.AsiaPacific** | <xref:Bodu.Globalization.Calendar.AsiaPacificCalendarData> | Australia (`AU`), China (`CN`), India (`IN`), Japan (`JP`), South Korea (`KR`), Malaysia (`MY`), New Zealand (`NZ`), Singapore (`SG`) |
| **Bodu.Globalization.Calendar.Data.Europe** | <xref:Bodu.Globalization.Calendar.EuropeCalendarData> | 28 EU/EEA territories: `AT`, `BE`, `BG`, `CY`, `CZ`, `DE`, `DK`, `EE`, `ES`, `FI`, `FR`, `GB`, `GR`, `HR`, `HU`, `IE`, `IT`, `LT`, `LU`, `LV`, `MT`, `NL`, `PL`, `PT`, `RO`, `SE`, `SI`, `SK` |

Each pack depends on `Bodu.Globalization.Calendar` and embeds only the `region-<cc>.xml` files for its territories. National rules are authored at the country level (`AU`, `US`, `GB`); state / province / region variants use the canonical ISO 3166-2 subdivision suffix (`AU-NSW`, `US-CA`, `GB-SCT`). See [Territories and regional composition](territories.md) for the parsing, containment, and composition rules that govern how these codes interact at query time.

## Install

```bash
# Reference the main library for the resolution pipeline:
dotnet add package Bodu.Globalization.Calendar

# Add one or more data packs depending on the regions you need:
dotnet add package Bodu.Globalization.Calendar.Data.Americas
dotnet add package Bodu.Globalization.Calendar.Data.AsiaPacific
dotnet add package Bodu.Globalization.Calendar.Data.Europe
```

## The pack surface

Each `<Region>CalendarData` type is `static` and exposes exactly three members:

| Member | Description |
|---|---|
| `static IReadOnlyList<string> SupportedCountries { get; }` | The country codes the pack carries. |
| `static NotableDateResource LoadResource(string territory)` | Load the immutable resource for a country **or one of its subdivisions** (e.g. `"US"`, `"CA-ON"`, `"AU-WA"`, `"GB-SCT"`). Throws `ArgumentException` when the country is not in the pack. |
| `static NotableDateService CreateService(string territory)` | Equivalent to `new NotableDateService(LoadResource(territory))` — a ready-to-query service. |

A subdivision argument selects its country's resource; because the full territory string is honoured when you *query*, national and subdivision rules compose. Passing `"AU-WA"` to `LoadResource` loads Australia's resource, and querying `"AU-WA"` then returns the national `AU` rules plus the Western Australia (`AU-WA`) rules.

## Wire one pack into a service

Use `CreateService` when you only need a single market — it does the load-and-import wiring for you:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.Americas;

NotableDateService service = AmericasCalendarData.CreateService("US");

// By-year resolution is the NotableDateServiceExtensions.Resolve extension:
IReadOnlyList<NotableDate> y2026 = service.Resolve(2026, "US");

foreach (NotableDate date in y2026)
    Console.WriteLine($"{date.Date:d MMM yyyy}  {date.DisplayName}");
```

Query a single day or an arbitrary range with the instance overloads:

```csharp
IReadOnlyList<NotableDate> july4 = service.Resolve(new DateOnly(2026, 7, 4), "US");
// → Independence Day, 4 July 2026
```

## Compose several territories

A `NotableDateService` is built over a single resource, so a service from one pack resolves every territory that pack carries. Create one service per pack and route the query to the matching service:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.Americas;
using Bodu.Globalization.Calendar.Data.AsiaPacific;
using Bodu.Globalization.Calendar.Data.Europe;

NotableDateService us = AmericasCalendarData.CreateService("US");
NotableDateService gb = EuropeCalendarData.CreateService("GB");
NotableDateService au = AsiaPacificCalendarData.CreateService("AU");

IReadOnlyList<NotableDate> independenceDay = us.Resolve(new DateOnly(2026, 7, 4),  "US");
IReadOnlyList<NotableDate> christmas       = gb.Resolve(new DateOnly(2026, 12, 25), "GB");
IReadOnlyList<NotableDate> australiaDay    = au.Resolve(new DateOnly(2026, 1, 26),  "AU");
```

If you would rather expose all markets through one `INotableDateService`, author (or merge) the territories you need into a single document and load that resource — see [Authoring notable date rules](rule-authoring.md).

## Load the resource only

When you want the resource alone — to register it through dependency injection, or to compose a service with custom collaborators — call `LoadResource` instead of `CreateService`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.AsiaPacific;

NotableDateResource au = AsiaPacificCalendarData.LoadResource("AU-WA");

// Hand it straight to a service with custom collaborators, or register it through DI.
NotableDateService service = new NotableDateService(au);
```

## Compose a pack resource with dependency injection

The `Bodu.Globalization.Calendar.DependencyInjection` companion registers <xref:Bodu.Globalization.Calendar.INotableDateService> as a singleton over a loaded resource. Pass the pack's `LoadResource(...)` result straight into `AddNotableDateService`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.Americas;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));

// or a factory, e.g. when the territory is read from configuration:
builder.Services.AddNotableDateService(sp =>
    AmericasCalendarData.LoadResource(sp.GetRequiredService<IConfiguration>()["Calendar:Territory"]!));
```

See [Calendar dependency injection](dependency-injection.md) for the registration overloads, the reloadable workflow, and lifetime semantics.

## Algorithm-backed rules

Several Asia-Pacific resources resolve through built-in date-calculation algorithms — Lunar New Year and Mid-Autumn Festival (Chinese lunisolar), Vesak and Asalha Puja (lunar), Qingming (solar term), and the Hindu festivals (lunisolar). These keys are part of the base library and are wired automatically by `LoadResource` / `CreateService`, so the packs resolve out of the box with no extra registration.

To extend a pack resource with your own computed date, register a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> and load your own document that imports the pack concepts and references the new `<Algorithm key="…">`. See [Date calculation algorithms](algorithms.md) and [Building and extending the service](building-the-service.md).

## Per-pack reference

### Americas — `AmericasCalendarData` {#americas}

**Package:** `Bodu.Globalization.Calendar.Data.Americas`
**Namespace:** `Bodu.Globalization.Calendar.Data`
**Type:** <xref:Bodu.Globalization.Calendar.AmericasCalendarData>

`SupportedCountries` = `CA`, `US`. National rules cover the federal calendar; subdivision rules (e.g. `US-CA` for California, `CA-ON` for Ontario) follow ISO 3166-2 conventions where present in the resource. All dates resolve through built-in strategies (`Fixed`, `DayOfWeekInMonth`, Easter via `Algorithm`), so no custom algorithm registration is required.

### Asia-Pacific — `AsiaPacificCalendarData` {#asia-pacific}

**Package:** `Bodu.Globalization.Calendar.Data.AsiaPacific`
**Namespace:** `Bodu.Globalization.Calendar.Data`
**Type:** <xref:Bodu.Globalization.Calendar.AsiaPacificCalendarData>

`SupportedCountries` = `AU`, `CN`, `IN`, `JP`, `KR`, `MY`, `NZ`, `SG`. Many of these resources delegate to the built-in lunar, solar-term, and Hindu-festival algorithms described above; those keys ship with the base library, so the pack resolves without additional setup.

### Europe — `EuropeCalendarData` {#europe}

**Package:** `Bodu.Globalization.Calendar.Data.Europe`
**Namespace:** `Bodu.Globalization.Calendar.Data`
**Type:** <xref:Bodu.Globalization.Calendar.EuropeCalendarData>

`SupportedCountries` = the 28 EU/EEA territories listed above (`AT`, `BE`, `BG`, `CY`, `CZ`, `DE`, `DK`, `EE`, `ES`, `FI`, `FR`, `GB`, `GR`, `HR`, `HU`, `IE`, `IT`, `LT`, `LU`, `LV`, `MT`, `NL`, `PL`, `PT`, `RO`, `SE`, `SI`, `SK`). National rules typically cover the federal calendar plus the major regional variants (`GB-SCT`, `GB-NIR`, `DE-BY` for Bavaria, …). All dates resolve through built-in strategies (most commonly Gregorian Easter via `Algorithm`), so no custom algorithm registration is required.

## Where to go next

- [Using NotableDateService](notable-dates.md) — querying patterns, filters, range queries, and overrides.
- [Territories and regional composition](territories.md) — ISO 3166 codes, subdivision patterns, and the containment rules that drive composed queries.
- [Calendar dependency injection](dependency-injection.md) — registering a pack resource through `IServiceCollection`.
- [Authoring notable date rules](rule-authoring.md) — XML / JSON documents, imports, and overrides.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full type reference.
