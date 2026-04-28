---
title: Bodu.Globalization.Calendar guides
---

# Bodu.Globalization.Calendar guides

Recipe-style walk-throughs for **Bodu.Globalization.Calendar** — a rule-driven library for resolving notable dates (public holidays, observances, religious festivals) for any year, territory, or calendar system.

If you're looking for the generated API reference, see the [Bodu.Globalization.Calendar namespace page](../../apidoc/Bodu.Globalization.Calendar.md).

## Start here

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="notable-dates.html">Using NotableDateService</a></h3>
  <p>The main entry point — resolving notable dates for a year, filtering by territory and category, querying a date range, and layering override providers.</p>
</div>

<div class="bodu-card">
  <h3><a href="algorithms.html">Date calculation algorithms</a></h3>
  <p>The built-in algorithm types — Gregorian Easter, Orthodox Easter, Hindu Lunar, Losar, Vesak, Asalha Puja, and Qingming — with algorithm selection guidance.</p>
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
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full namespace overview.
