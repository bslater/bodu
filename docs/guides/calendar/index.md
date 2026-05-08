---
title: Bodu.Globalization.Calendar guides
---

# Bodu.Globalization.Calendar guides

Recipe-style walk-throughs for **Bodu.Globalization.Calendar** — a rule-driven library for resolving notable dates (public holidays, observances, religious festivals) for any year, territory, or calendar system.

If you're looking for the generated API reference, see the [Bodu.Globalization.Calendar namespace page](../../apidoc/Bodu.Globalization.Calendar.md).

## Start here
Recipe-style walk-throughs for **Bodu.Globalization.Calendar**, organised by namespace.

If you have not yet installed the package or want the high-level shape of the library, start with the [Bodu.Globalization.Calendar introduction](../../docs/calendar/index.md) and the [getting-started page](../../docs/calendar/getting-started.md). For the auto-generated API reference, see the [Bodu.Globalization.Calendar namespace page](../../apidoc/Bodu.Globalization.Calendar.md).

## How the library works

![NotableDateService resolution pipeline](../../images/diagrams/calendar-resolution-pipeline.svg)

A **`NotableDateRule`** is an authored recipe — strategy, category, territory, adjustments, duration. **`NotableDateService`** loads rules from one or more providers, layers optional override providers, resolves each rule for the requested year via the calculator, runs the adjustment pipeline, and caches the resolved set per year in a thread-safe dictionary.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| `Bodu.Globalization.Calendar` | Service, rules, results, providers, adjustments, registries, parsers — the resolution pipeline. | [Using NotableDateService](notable-dates.md) · [Authoring notable date rules](rule-authoring.md) |
| `Bodu.Globalization.Calendar.Algorithms` | Built-in date calculators — Gregorian and Orthodox Easter, Hindu Lunar, Losar, Vesak, Asalha Puja, Qingming, Lunar phase. | [Date calculation algorithms](algorithms.md) |
| `Bodu.Globalization.Calendar.Plugins` | Plugin host with trust policies for loading rules / algorithms from external assemblies. | (no dedicated guide yet — see API reference) |
| `Bodu.Globalization.Calendar.Extensions` | Working-day arithmetic over `DateOnly` and `DateTime` — `IsWorkingDay`, `NextWorkingDay`, `AddWorkingDays`, … | (covered in [Using NotableDateService](notable-dates.md)) |
| `Bodu.Globalization.Calendar.Providers` | Companion-pack rule providers (Americas / Europe / AsiaPacific). | [Calendar data packs](data-packs.md) |

## Guides

### `Bodu.Globalization.Calendar` — Service

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="notable-dates.html">Using NotableDateService</a></h3>
  <p>The main entry point — resolving notable dates for a year, filtering by territory and category, querying a date range, and layering override providers.</p>
</div>

<div class="bodu-card">
  <h3><a href="algorithms.html">Date calculation algorithms</a></h3>
  <p>The built-in algorithm types — Gregorian Easter, Orthodox Easter, Hindu Lunar, Losar, Vesak, Asalha Puja, and Qingming — with algorithm selection guidance.</p>
  <h3><a href="notable-dates.md">Using NotableDateService</a></h3>
  <p>The main entry point — resolving notable dates for a year, filtering by territory and category, querying a date range, layering override providers, and working-day arithmetic over <code>DateOnly</code> / <code>DateTime</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="rule-authoring.md">Authoring notable date rules</a></h3>
  <p>How to add your own rules — as in-code objects, embedded XML / JSON resource files, or companion assemblies — and how to layer runtime overrides.</p>
</div>

</div>

## How the library works

```
Rule (NotableDateRule)
  └─► NotableDateRuleResolver.ResolveAnchorDate(rule, year)
        └─► NotableDate  { Date, Name, Category, EndDate, … }
```

Every notable date starts as a **`NotableDateRule`** — an authored description that captures:

- **Strategy** (`DateResolutionStrategy`) — how to find the anchor date: a fixed month/day, an *n*th weekday-of-month, an offset from another rule's date, or a pluggable algorithm.
- **Category** (`NotableDateCategory`) — `PublicHoliday`, `Observance`, `ReligiousFestival`, `CulturalEvent`, or `Other`.
- **Territory** (`TerritoryCode`) — the ISO 3166-1 alpha-2 or sub-region code the rule applies to.
- **Adjustments** (`ObservanceAdjustment`) — how the anchor moves when it falls on a weekend.
- **Duration** (`DurationDays`) — multi-day spans such as Easter weekend.

**`NotableDateService`** loads rules from one or more providers, layers override providers on top, resolves each rule for the requested year using `NotableDateRuleResolver`, and caches the results per year.

## Key types

| Type | Responsibility |
|---|---|
| `NotableDateService` | Main entry point — resolves, caches, and queries notable dates. |
| `INotableDateService` | Interface for DI registration. |
| `NotableDateRule` | Authored description of a notable date (strategy, category, territory, adjustments). |
| `NotableDate` | Resolved result — the concrete date, name, category, and optional end date. |
| `DateResolutionStrategy` | Describes how to locate the anchor date for a rule. |
| `NotableDateRuleResolver` | Dispatches a rule to the correct resolution logic. |
| `NotableDateFilter` | Builder for territory- and category-scoped queries. |
| `TerritoryCode` | Strongly-typed ISO 3166-1 alpha-2 or sub-region code. |
| `INotableDateAlgorithm` | Extension point for custom astronomical or calendar calculations. |

## Related concepts

- [Using NotableDateService](notable-dates.md) — patterns for querying, filtering, and overriding.
- [Date calculation algorithms](algorithms.md) — selecting the right built-in algorithm and implementing your own.
### `Bodu.Globalization.Calendar.Algorithms`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="algorithms.md">Date calculation algorithms</a></h3>
  <p>The built-in algorithm types — Easter (Gregorian / Orthodox), Hindu Lunar, Losar, Vesak, Asalha Puja, Qingming, Lunar phase — with registration guidance and a custom-algorithm walk-through.</p>
</div>

</div>

### `Bodu.Globalization.Calendar.Data.*` — Data packs

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="data-packs.md">Calendar data packs</a></h3>
  <p>The official <code>Bodu.Globalization.Calendar.Data.*</code> companion assemblies — Americas, Europe, and Asia-Pacific — and how to compose them with the resolution pipeline.</p>
</div>

</div>

## Where to go next

- [Bodu.Globalization.Calendar introduction](../../docs/calendar/index.md) — namespaces, headline types, scenarios.
- [Bodu.Globalization.Calendar getting started](../../docs/calendar/getting-started.md) — install and minimal samples.
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full namespace overview.
