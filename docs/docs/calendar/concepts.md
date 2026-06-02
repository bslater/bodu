---
title: Bodu.Globalization.Calendar — Core concepts
---

# Bodu.Globalization.Calendar — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/calendar/index.md), and refer back to it whenever a term feels imprecise.

For the high-level shape of the library and the resolution pipeline diagram, start with the [introduction](index.md).

## The pipeline in one line

![Notable date flow — six stages from authored rule to consumer query](../../images/diagrams/calendar-notable-date-flow.svg)

The pipeline reads left to right. A **rule source** loads authored recipes; each recipe is a **`NotableDateRule`**; the **resolution strategy** turns the rule into a *nominal date* for the requested year; the **adjustment pipeline** may shift it to an *observed date*; the result is cached as a **resolved `NotableDate`**; consumers query the cache via **`GetNotableDates`**, working-day arithmetic, and filters.

Every term below corresponds to a stage or an input of that pipeline.

> **Conceptual vs. implementation view.** This diagram collapses the resolver into the six conceptual stages that matter to consumers. The implementation expands these into eight discrete stages — *rule loading*, *override merging*, *effective rule list assembly*, *anchor resolution*, *adjustment chain evaluation*, *collision resolution*, *per-year cache*, and (for filtered queries) *filter gate*. The [resolution pipeline guide](../../guides/calendar/resolution-pipeline.md) walks through each one with traces and worked examples.

## Rule vs. resolved date

A **rule** (<xref:Bodu.Globalization.Calendar.NotableDateRule>) is the authored recipe — *what* the date represents and *how* to compute it for any year. It is immutable, year-independent, and lives in a rule source.

A **resolved date** (<xref:Bodu.Globalization.Calendar.NotableDate>) is the year-specific concrete output — a single occurrence, with a `Date`, `Name`, `Category`, `TerritoryCode`, and optional adjustment metadata. Resolved dates are produced by `NotableDateService.GetNotableDates` and cached per year.

A rule produces zero, one, or many resolved dates per year, depending on its strategy, scope, and any year bounds.

## Nominal date vs. observed date

![Nominal date vs. observed date — Christmas Day 2027 worked example](../../images/diagrams/calendar-nominal-vs-observed.svg)

The **nominal date** is what the resolution strategy computes from the rule before any adjustment runs — e.g. *25 December* for Christmas Day.

The **observed date** is what the adjustment pipeline emits — e.g. *Monday 27 December 2027* when Christmas falls on a Saturday and a weekend-rollover adjustment relocates it.

Most rules have a single occurrence per year and `nominal == observed`. When an adjustment fires, `NotableDate.WasAdjusted` is set to `true` and `NotableDate.AdjustmentReason` records the original nominal date, trigger, and action. Whether the service emits the observed day alone, the nominal day alone, or *both* is governed by <xref:Bodu.Globalization.Calendar.ObservedDateMode>: `ObservedOnly` (the default) supersedes the nominal date with its substitute (e.g. AU New Year's Day moving off the weekend), `ActualAndObserved` emits the nominal day *and* its substitute (e.g. UK bank holidays), and `ActualOnly` suppresses the substitute entirely. The mode is applied consistently regardless of query-window width. See [Identity, priority, and observed dates](../../guides/calendar/identity-and-resolution.md) for the design choice and [Observance adjustment rules](../../guides/calendar/adjustment-rules.md) for the trigger/action catalogue.

## Resolution strategy

A rule's <xref:Bodu.Globalization.Calendar.DateResolutionStrategy> determines how the nominal date is computed:

| Strategy | What it does |
|---|---|
| `Fixed` | A specific month + day every year (e.g. 1 January). |
| `DayOfWeekInMonth` | The *n*th occurrence of a weekday in a month (e.g. third Monday in January). |
| `OffsetFromAnchor` | A signed day-offset from another resolved rule (e.g. Easter Sunday − 2 = Good Friday). |
| `WeekdayNearDate` | A weekday on or after, on or before, or nearest to a fixed reference date (e.g. the Saturday on or after 20 June = Nordic Midsummer Day). |
| `RelativeWeekdayInMonth` | A weekday positioned relative to the *n*th anchor weekday of a month (e.g. the Tuesday after the first Monday in November = US Election Day). |
| `Algorithm` | Delegated to a registered `INotableDateAlgorithm` for astronomical or ecclesiastical computations (Easter, Vesak, …). |

See [Choosing a strategy](../../guides/calendar/rule-reference.md#choosing-a-strategy) for guidance on which strategy to pick, and the [NotableDateRule and ObservanceAdjustment reference](../../guides/calendar/rule-reference.md) for the per-field contracts and worked examples.

## Anchor

An **anchor** is a rule whose resolved date is consumed by `OffsetFromAnchor` rules. Anchors are typically algorithmic — Easter Sunday is the most common — but any resolved rule with a `Name` can serve as one.

A rule names its anchor via `NotableDateRule.AnchorRuleName` and a signed `OffsetDays`. The anchor must be loaded by the same service instance and must resolve before the dependent rule (priority and pipeline order govern this).

See [Date calculation algorithms](../../guides/calendar/algorithms.md) for the algorithm + anchor pattern.

## Adjustment

An **adjustment** (<xref:Bodu.Globalization.Calendar.ObservanceAdjustment>) is a post-resolution shift evaluated against the nominal date. It pairs a `Trigger` (when it fires — `IfWeekend`, `IfNonWorkingDay`, `IfDayOfWeek`, …) with an `Action` (what it does — `MoveToNextWeekday`, `MoveToNextWorkingDay`, `AddDays`, `ReplaceWithNamedDate`, …) and an optional scope (territory, year range).

A rule can carry multiple adjustments. The pipeline sorts them by priority, fires the first whose trigger matches, and stops. See [Observance adjustment rules](../../guides/calendar/adjustment-rules.md) for the full trigger / action catalogue and pattern examples.

## Collision resolution

A **collision** occurs when two rules resolve to the same date for the same territory — for example, when an adjusted Christmas Day lands on Boxing Day. The configured <xref:Bodu.Globalization.Calendar.INotableDateCollisionResolver> decides whether to merge them, keep the higher-priority rule, or surface both. The built-in <xref:Bodu.Globalization.Calendar.DefaultNotableDateCollisionResolver> implements a sensible default; plug in a custom resolver for bespoke merge logic.

See [Building and extending the service](../../guides/calendar/building-the-service.md) for the registry contract.

## Territory

![TerritoryCode containment hierarchy](../../images/diagrams/calendar-territory-containment.svg)

A <xref:Bodu.Globalization.Calendar.TerritoryCode> is an ISO 3166-1 alpha-2 country code with an optional ISO 3166-2 subdivision — `AU`, `AU-NSW`, `GB-ENG`, `US-CA`. Territory codes are **hierarchical**: a parent contains all of its subdivisions.

- A rule authored for `AU` applies to every Australian subdivision.
- A query for `AU-NSW` returns rules authored at `AU` *and* at `AU-NSW`.
- A rule with no territory code applies globally — useful for genuinely global dates like the Gregorian New Year.

See [Territories and regional composition](../../guides/calendar/territories.md) for the parsing, containment, and composition rules.

## Category vs. tag

<xref:Bodu.Globalization.Calendar.NotableDateCategory> is the well-known enum — `Holiday`, `Observance`, `Remembrance`, `Cultural`, `Religious`, `Seasonal`, `Civic`, `Bank`, `School`, `Regional`, `Other`, `None`. Every resolved date has exactly one category. Use it for coarse-grained filtering (e.g. "show me holidays").

**Tags** are free-form strings on the rule (`NotableDateRule.Tags`). They survive into the resolved `NotableDate.Tags` and are intended for fine-grained, app-specific filtering — `"NationalHoliday"`, `"BankClosed"`, `"SchoolHoliday"`, `"Christian"`. Combine `NotableDateFilter.WithTag(...)` with `NotableDateFilter.ForCategory(...)` for compound queries.

## Provider

A **provider** is a source of rules. The base contract is <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider> — a single `LoadRules` method returning `NotableDateRule` instances. Built-in providers cover the common cases:

- <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider> — load rules from an embedded XML resource.
- <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider> — load rules from an embedded JSON resource (same document model as XML).
- The `Bodu.Globalization.Calendar.Data.*` companion packages — `AmericasCalendarData`, `EuropeCalendarData`, `AsiaPacificCalendarData` — ship per-country providers backed by curated rule files.

A service can be constructed from one or many providers; their rule sets are merged in declaration order.

See [Authoring notable date rules](../../guides/calendar/rule-authoring.md) and [Calendar data packs](../../guides/calendar/data-packs.md).

## Override

An **override** is a runtime modification of the base rule set, layered on top of all providers. The contract is <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider>: `GetAdditions` returns new rules; `GetRemovals` returns <xref:Bodu.Globalization.Calendar.RuleRemoval> records that suppress base rules by name (optionally scoped to year ranges).

Use overrides for organisation-specific closures, ad-hoc emergency holidays, suppression of a base rule in a single territory, or short-lived experiments. Overrides do not modify provider XML / JSON — they layer on top at resolution time, and `service.Invalidate()` clears the cache when the override state changes.

## Algorithm vs. fixed rule

When a date cannot be expressed as a calendar formula (Easter Sunday, Vesak, Diwali, Qingming), the calculation lives behind <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm>. The rule references it by key:

```csharp
new NotableDateRule
{
    Name         = "Easter Sunday",
    Strategy     = DateResolutionStrategy.Algorithm,
    AlgorithmKey = "easter-sunday",
};
```

Algorithms are registered on <xref:Bodu.Globalization.Calendar.NotableDateAlgorithmRegistry> and resolved during the pipeline. See [Date calculation algorithms](../../guides/calendar/algorithms.md).

## Resolved notable date

A `NotableDate` is the immutable output of one rule for one occurrence in one year. Its key fields:

| Field | Meaning |
|---|---|
| `Date` | The observed date (after any adjustment). |
| `Name` | The display name (subject to optional localisation). |
| `Category` | `NotableDateCategory` value. |
| `TerritoryCode` | The territory the occurrence applies to. |
| `Tags` | Free-form classification strings from the rule. |
| `DurationDays`, `EndDate` | Multi-day spans (e.g. Hanukkah, Easter weekend). |
| `IsNonWorkingDay` | Whether working-day arithmetic should skip this date. |
| `WasAdjusted`, `AdjustmentReason` | Whether and how the adjustment pipeline shifted the nominal date. |

## Working day vs. non-working day

A **working day** is any day that is neither a weekend (per the service's `CalendarWeekendDefinition`) nor a resolved notable date with `IsNonWorkingDay = true` for the queried territory.

Not every notable date is non-working: Mother's Day, ANZAC commemorations in certain territories, and most cultural observances are notable but not closures. Working-day arithmetic relies entirely on the rule's `IsNonWorkingDay` flag — authors decide which categories of date count as closures.

See [Working-day arithmetic](../../guides/calendar/working-days.md) for the operations (`IsWorkingDay`, `AddWorkingDays`, `NextWorkingDay`, `WorkingDaysBetween`, `SnapToWorkingDay`, …).

## Where to go next

- **[Introduction](index.md)** — the high-level shape of the library.
- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.Globalization.Calendar guides](../../guides/calendar/index.md)** — deep-dive walk-throughs for every concept above.
