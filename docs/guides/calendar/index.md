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
  <p>The built-in algorithm types — Easter, Hindu Lunar, Losar, Vesak, Asalha Puja, and Qingming — with registration guidance and a custom-algorithm walk-through.</p>
</div>

<div class="bodu-card">
  <h3><a href="rule-authoring.html">Authoring notable date rules</a></h3>
  <p>How to add your own rules — as in-code objects, embedded XML resource files, or companion assemblies — and how to layer runtime overrides on top of the base rule set.</p>
</div>

<div class="bodu-card">
  <h3><a href="data-packs.html">Calendar data packs</a></h3>
  <p>The official <code>Bodu.Globalization.Calendar.Data.*</code> companion assemblies — Americas, Europe, and Asia-Pacific — and how to compose them with the resolution pipeline.</p>
</div>

</div>

## How the library works

![NotableDateService resolution pipeline](../../images/diagrams/calendar-resolution-pipeline.svg)

Every notable date starts as a **`NotableDateRule`** — an authored recipe that the resolver turns into a concrete `NotableDate` instance per year. The rule captures:

- **Strategy** (`DateResolutionStrategy`) — how to locate the anchor date each year: a fixed month/day, an *n*th weekday-of-month, an offset from another rule's date, or a pluggable algorithm.
- **Category** (`NotableDateCategory`) — `Holiday`, `Observance`, `Remembrance`, `Cultural`, `Seasonal`, or `Other`.
- **Territory** (`TerritoryCode`) — the ISO 3166-1 alpha-2 country or subdivision code the rule applies to (for example `AU`, `AU-NSW`).
- **Adjustments** (`ObservanceAdjustment`) — how the anchor shifts when it falls on a weekend or other trigger condition.
- **Duration** (`DurationDays`) — multi-day spans such as Easter weekend or Hanukkah.

**`NotableDateService`** loads rules from one or more providers, merges optional override providers on top, resolves each rule for the requested year, and caches results per year in a thread-safe `ConcurrentDictionary`.

## Key types

| Type | Responsibility |
|---|---|
| `NotableDateService` | Main entry point — resolves, caches, and queries notable dates. |
| `INotableDateService` | Interface for dependency-injection registration. |
| `NotableDateRule` | Authored recipe describing a notable date (strategy, category, territory, adjustments). |
| `NotableDate` | Resolved output — the concrete date, name, category, territory, and optional multi-day span. |
| `DateResolutionStrategy` | Enum selecting how the anchor date is calculated (`Fixed`, `DayOfWeekInMonth`, `Algorithm`, `OffsetFromAnchor`). |
| `NotableDateCategory` | Coarse-grained classification (`Holiday`, `Observance`, `Remembrance`, `Cultural`, `Seasonal`, `Other`). |
| `NotableDateFilter` | Composable two-stage predicate for territory- and category-scoped queries. |
| `ObservanceAdjustment` | Conditional date-shift specification (trigger + action + optional territory/year scope). |
| `TerritoryCode` | Strongly-typed ISO 3166-1 alpha-2 country or subdivision code with containment semantics. |
| `INotableDateAlgorithm` | Extension point for custom astronomical or calendar calculations. |
| `NotableDateAlgorithmRegistry` | Key-based registry wiring named algorithms into the resolver. |
| `INotableDateRuleProvider` | Contract for rule authoring sources (XML, database, in-memory). |
| `INotableDateRuleOverrideProvider` | Contract for runtime rule additions and removals. |

## Related concepts

- [Using NotableDateService](notable-dates.md) — patterns for querying, filtering, and overriding.
- [Calendar data packs](data-packs.md) — the official Americas / Europe / Asia-Pacific companion assemblies.
- [Date calculation algorithms](algorithms.md) — selecting the right built-in algorithm and implementing your own.
- [Authoring notable date rules](rule-authoring.md) — in-code objects, XML resource files, companion assemblies, and runtime overrides.
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full namespace overview.
