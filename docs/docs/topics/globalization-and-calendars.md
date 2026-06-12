---
title: Globalization & Calendars — Overview
---

# Globalization & Calendars

The **Globalization & Calendars** topic groups the packages that resolve authored calendar rules into concrete notable dates — public holidays, observances, religious festivals, and regional events — and that make those dates usable for filtering, querying, and working-day-aware date arithmetic. At its center is a single resource-driven engine: rules are authored on the notable-date schema, loaded into an immutable, validated <xref:Bodu.Globalization.Calendar.NotableDateResource>, and queried through <xref:Bodu.Globalization.Calendar.NotableDateService> by year, date, or range and territory.

The runtime is intentionally small. Everything beyond the resolution engine — fluent rule authoring, dependency-injection registration, trust-gated plugin loading, and the curated per-region holiday data — ships as opt-in companion packages that release on their own cadence. Consumers pull in only the pieces they need: most applications reference the runtime plus one or two regional data packs and never touch the rest.

## Packages in this topic

| Package | Status | What it provides | Docs |
|---|---|---|---|
| `Bodu.Globalization.Calendar` | Stable | The runtime — rule engine, resolution pipeline, built-in date-calculation algorithms, bundled common catalogues, working-day extensions. Required by every other package in this topic. | [Introduction](../calendar/index.md) |
| `Bodu.Globalization.Calendar.Builder` | Stable | Fluent, chainable C# API for authoring notable-date documents in code, with XML / JSON serialization and load/save. | [Builder guide](../../guides/calendar/notable-date-builder.md) |
| `Bodu.Globalization.Calendar.DependencyInjection` | Stable | `IServiceCollection` extensions for registering `INotableDateService` over a loaded resource. | [DI guide](../../guides/calendar/dependency-injection.md) |
| `Bodu.Globalization.Calendar.Plugins` | Stable | Trust-gated loading of external assemblies that contribute custom `INotableDateAlgorithm` implementations. | [Building and extending the service](../../guides/calendar/building-the-service.md) |
| `Bodu.Globalization.Calendar.Americas` | Stable | Curated public-holiday rules for the Americas bundle (e.g. `US`, `CA`). | [Data packs guide](../../guides/calendar/data-packs.md) |
| `Bodu.Globalization.Calendar.AsiaPacific` | Stable | Asia-Pacific bundle (e.g. `AU` with subdivisions, `CN`, `IN`, `JP`, `KR`, `MY`, `NZ`, `SG`). | [Data packs guide](../../guides/calendar/data-packs.md) |
| `Bodu.Globalization.Calendar.Europe` | Stable | Europe bundle (e.g. `DE`, `ES`, `FR`, `GB`, `IT`, `NL`). | [Data packs guide](../../guides/calendar/data-packs.md) |
| `Bodu.Globalization.Calendar.Africa` | Stable | Africa bundle (e.g. `ZA`, `NG`, `KE`, `GH`, `ET`, `EG`, `MA`). | [Data packs guide](../../guides/calendar/data-packs.md) |
| `Bodu.Globalization.Calendar.MiddleEast` | Stable | Middle East bundle (e.g. `AE`, `SA`, `IL`, `TR`, `QA`, `JO`). | [Data packs guide](../../guides/calendar/data-packs.md) |

The authoritative dependency and status rows live in the [package matrix](../package-matrix.md).

## How the pieces fit

![Bodu.Globalization.Calendar package family — runtime, companions, and data packs](../../images/diagrams/calendar-package-family.svg)

A notable date flows through the topic's packages in a fixed order:

1. **A data pack supplies rules.** Each regional pack embeds per-country rule documents that import the shared common catalogues, and exposes a `<Region>CalendarData` factory (`SupportedCountries`, `LoadResource(territory)`, `CreateService(territory)`). Alternatively, you author your own document — as XML / JSON text, or fluently in C# with the Builder's <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder>.
2. **The runtime loads a resource.** <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> parses the document, resolves its imports against the bundled catalogues, applies overrides, validates, and produces an immutable <xref:Bodu.Globalization.Calendar.NotableDateResource>.
3. **`NotableDateService` resolves dates.** Built over the resource, the service computes each rule's nominal date via its strategy (fixed date, *n*th weekday, weekday-near-date, offset from another rule, or a named algorithm), applies observance adjustments, settles same-day collisions, and emits resolved <xref:Bodu.Globalization.Calendar.NotableDate> occurrences for the requested year, date, or range and territory.
4. **Consumers query and compute.** Results are filtered with <xref:Bodu.Globalization.Calendar.NotableDateFilter> and fed into the working-day extensions (`IsWorkingDay`, `AddWorkingDays`, `NextWorkingDay`, …) in `Bodu.Extensions`.

The companions attach at well-defined seams. **Builder** authors documents in step 1 without hand-writing XML. **Plugins** extends step 3 with custom astronomical or ecclesiastical algorithms discovered from external assemblies, admitted only under an explicit, deny-by-default trust policy. **DependencyInjection** registers the assembled service in a `Microsoft.Extensions.DependencyInjection` container, including the reloadable runtime-swap workflow.

## Which package do I need?

| Scenario | Reach for | Notes |
|---|---|---|
| Resolve US / AU / GB public holidays for a year | `Bodu.Globalization.Calendar` + the matching regional data pack | `AmericasCalendarData.CreateService("US")` is a one-liner; query with `service.Resolve(2026, "US")`. |
| Working-day arithmetic ("add 5 business days") | `Bodu.Globalization.Calendar` + a data pack | The `Bodu.Extensions` working-day surface ships in the runtime; see the [working-days guide](../../guides/calendar/working-days.md). |
| Author custom company dates (closures, fiscal events) in C# | `Bodu.Globalization.Calendar.Builder` | Build, serialize to XML / JSON, save, or materialize a resource directly; see the [builder guide](../../guides/calendar/notable-date-builder.md). |
| Author rules as XML / JSON documents | `Bodu.Globalization.Calendar` alone | `NotableDateResourceLoader.Load(xml)`; see [rule authoring](../../guides/calendar/rule-authoring.md). |
| Host the service in ASP.NET Core / generic-host DI | `Bodu.Globalization.Calendar.DependencyInjection` | `services.AddNotableDateService(resource)` or `AddReloadableNotableDateService(...)`. |
| Load a custom astronomical algorithm from an external assembly | `Bodu.Globalization.Calendar.Plugins` | Trust-gated and default-deny; in-process custom algorithms need only the runtime's `NotableDateAlgorithmRegistry`. |
| Browse what dates the shipped data actually contains | (documentation) | The [notable-date catalogue](../../guides/calendar/catalogue/index.md) lists every concept by theme and region. |

## Install

The runtime plus one regional data pack covers the most common case:

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
```

Regional packs are independent — install only the regions you need:

```bash
dotnet add package Bodu.Globalization.Calendar.Americas
dotnet add package Bodu.Globalization.Calendar.Europe
dotnet add package Bodu.Globalization.Calendar.Africa
dotnet add package Bodu.Globalization.Calendar.MiddleEast
```

The companions are opt-in:

```bash
dotnet add package Bodu.Globalization.Calendar.Builder
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
dotnet add package Bodu.Globalization.Calendar.Plugins
```

## A taste of the surface

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Extensions;                      // working-day arithmetic — not auto-imported

NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

// All NSW public holidays for 2026:
IReadOnlyList<NotableDate> holidays = service.Resolve(
    2026, "AU-NSW", NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));

// Working-day arithmetic that skips weekends and resolved holidays:
DateOnly today  = DateOnly.FromDateTime(DateTime.Today);
DateOnly inFive = today.AddWorkingDays(5, service, "AU-NSW");
```

Territory codes are hierarchical — a query for `AU-NSW` returns rules authored for `AU` as well as rules specific to `AU-NSW`, so national and regional rules compose naturally.

The companions follow the same grain. Hosting the service in a DI container is one registration:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
// or reloadable, for runtime rule swaps:
// builder.Services.AddReloadableNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
```

And authoring a custom date with the Builder produces the same kind of document the data packs embed — see the [builder guide](../../guides/calendar/notable-date-builder.md) for the fluent surface and its XML / JSON round-trip.

## Key types across the family

| Type | Package | Role |
|---|---|---|
| <xref:Bodu.Globalization.Calendar.NotableDateService> / <xref:Bodu.Globalization.Calendar.INotableDateService> | Runtime | Main entry point — resolves and queries notable dates for a date, range, or year. |
| <xref:Bodu.Globalization.Calendar.NotableDate> | Runtime | Resolved output — observed date, calculated date, name, category, territory, optional multi-day span. |
| <xref:Bodu.Globalization.Calendar.NotableDateResource> / <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> | Runtime | The immutable loaded document, and the loader that parses, imports, validates, and produces it. |
| <xref:Bodu.Globalization.Calendar.NotableDateFilter> | Runtime | Composable query predicate — `ForCategory`, `WithTag`, `InDateRange`, combined with `And` / `Or` / `Not`. |
| <xref:Bodu.Globalization.Calendar.TerritoryCode> | Runtime | Strongly-typed ISO 3166 country / subdivision code with containment semantics. |
| <xref:Bodu.Extensions.NotableDateOnlyExtensions> | Runtime | Working-day arithmetic over `DateOnly` — `IsWorkingDay`, `AddWorkingDays`, `NextWorkingDay`, … |
| <xref:Bodu.Globalization.Calendar.AmericasCalendarData> · <xref:Bodu.Globalization.Calendar.AsiaPacificCalendarData> · <xref:Bodu.Globalization.Calendar.EuropeCalendarData> | Data packs | Static per-region factories over the embedded country packs. |
| <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> | Builder | Fluent C# authoring of a document — build, serialize (XML / JSON), save, or materialize a resource. |
| <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginLoader> | Plugins | Trust-gated discovery of external algorithm assemblies. |
| <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> / <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> | Runtime | The pluggable algorithm contract behind `<Algorithm key="…">` rules, and its registry. |

## Where to go next

- **[Topic concepts](globalization-and-calendars-concepts.md)** — the cross-package vocabulary: rule, document, resource, territory, adjustment, algorithm, data pack, trust policy.
- **[Bodu.Globalization.Calendar introduction](../calendar/index.md)** — the runtime's mental model, headline types, and scenarios.
- **[Getting started](../calendar/getting-started.md)** — install plus runnable minimal samples for loading, resolving, and working-day arithmetic.
- **[Globalization & Calendars guides](../../guides/topics/globalization-and-calendars.md)** — the topic's guide landing page.
- **[Notable-date catalogue](../../guides/calendar/catalogue/index.md)** — what dates the shipped data contains, by theme and by region.
- **[Package matrix](../package-matrix.md)** — status, dependencies, and install commands for every package.
