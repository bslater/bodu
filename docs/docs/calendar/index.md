---
title: Bodu.Globalization.Calendar — Introduction
---

# Bodu.Globalization.Calendar

**Bodu.Globalization.Calendar** is the rule-driven notable-date library of the Bodu suite. It resolves public holidays, observances, and religious festivals for any year, territory, or calendar system, with a pluggable algorithm registry, observance-adjustment pipeline, and a trust-policy-driven plugin host for loading rules from external assemblies.

## How the library works

![NotableDateService resolution pipeline](../../images/diagrams/calendar-resolution-pipeline.svg)

A **`NotableDateRule`** is an authored recipe — a strategy (fixed, *n*th weekday, offset, algorithm), a category (holiday, observance, …), a territory (`AU`, `AU-NSW`), and an optional adjustment chain. **`NotableDateService`** loads rules from one or more providers, layers optional override providers, resolves each rule for a requested year via the calculator, runs the adjustment pipeline, and caches the resolved set per year in a thread-safe dictionary.

## Namespaces and headline types

### `Bodu.Globalization.Calendar` — Core types

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.NotableDateService> / <xref:Bodu.Globalization.Calendar.INotableDateService> | Main entry point — resolves, caches, and queries notable dates. |
| <xref:Bodu.Globalization.Calendar.NotableDateRule> | Authored recipe (strategy, category, territory, adjustments, duration). |
| <xref:Bodu.Globalization.Calendar.NotableDate> | Resolved output — concrete date, name, category, territory, optional multi-day span. |
| <xref:Bodu.Globalization.Calendar.NotableDateCategory> | `Holiday` / `Observance` / `Remembrance` / `Cultural` / `Religious` / `Seasonal` / `Civic` / `Bank` / `School` / `Regional` / `Other` / `None`. |
| <xref:Bodu.Globalization.Calendar.NotableDateFilter> | Composable two-stage predicate for territory- and category-scoped queries, built via static factory methods (`ForCategory`, `WithTag`, `InDateRange`, `WithName`, `IsNonWorkingDay`, …). |
| <xref:Bodu.Globalization.Calendar.TerritoryCode> | Strongly-typed ISO 3166-1 alpha-2 country / subdivision code with containment semantics. |
| <xref:Bodu.Globalization.Calendar.DateResolutionStrategy> | Enum: `Fixed`, `DayOfWeekInMonth`, `OffsetFromAnchor`, `Algorithm`. |
| <xref:Bodu.Globalization.Calendar.ObservanceAdjustment> | Conditional date-shift specification (trigger + action + scope). |
| <xref:Bodu.Globalization.Calendar.AdjustmentTrigger>, <xref:Bodu.Globalization.Calendar.AdjustmentAction>, <xref:Bodu.Globalization.Calendar.AdjustmentReason> | Adjustment-pipeline primitives. |
| <xref:Bodu.Globalization.Calendar.IAdjustmentHandler>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.IAdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerContext>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerResult> | Handler model for adjustments. |
| <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm>, <xref:Bodu.Globalization.Calendar.INotableDateAlgorithmRegistry>, <xref:Bodu.Globalization.Calendar.NotableDateAlgorithmRegistry> | Pluggable algorithm contract and registry. |
| <xref:Bodu.Globalization.Calendar.INotableDateProvider>, <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider> | Rule-source contracts. |
| <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.NotableDateRuleResourceProviderBase> | Built-in resource-backed rule providers (and their base class). |
| <xref:Bodu.Globalization.Calendar.INotableDateNameLocalizer> | Pluggable display-name localisation. |
| <xref:Bodu.Globalization.Calendar.INotableDateCollisionResolver>, <xref:Bodu.Globalization.Calendar.DefaultNotableDateCollisionResolver> | Behaviour when multiple rules resolve to the same date. |
| <xref:Bodu.Globalization.Calendar.IResourcePathResolver>, <xref:Bodu.Globalization.Calendar.ResourcePathResolver>, <xref:Bodu.Globalization.Calendar.ResourcePathResolverOptions> | Resource-path resolution for embedded providers. |
| <xref:Bodu.Globalization.Calendar.NotableDateRuleParser>, <xref:Bodu.Globalization.Calendar.NotableDateRuleJsonParser>, <xref:Bodu.Globalization.Calendar.ParsedNotableDateDocument>, <xref:Bodu.Globalization.Calendar.NotableDateRuleUseGroup>, <xref:Bodu.Globalization.Calendar.NotableDateRuleUseDirective>, <xref:Bodu.Globalization.Calendar.NotableDateRuleOverrideBody>, <xref:Bodu.Globalization.Calendar.RuleRemoval> | XML / JSON rule-document model. |

### `Bodu.Globalization.Calendar.Algorithms` — Built-in algorithms

| Type | Computes |
|---|---|
| <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateAlgorithm>, <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateProviderBase> | Easter Sunday using the Gregorian or Orthodox computus. |
| <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarNotableDateAlgorithm> + <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarMonth> + <xref:Bodu.Globalization.Calendar.Algorithms.HinduPaksha> | Hindu lunisolar dates (Diwali, Holi, …). |
| <xref:Bodu.Globalization.Calendar.Algorithms.LosarNotableDateAlgorithm> | Tibetan New Year. |
| <xref:Bodu.Globalization.Calendar.Algorithms.VesakNotableDateAlgorithm> | Buddhist Vesak / Buddha Day. |
| <xref:Bodu.Globalization.Calendar.Algorithms.AsalhaPujaNotableDateAlgorithm> | Asalha Puja. |
| <xref:Bodu.Globalization.Calendar.Algorithms.QingmingNotableDateAlgorithm> | Qingming festival (sun-longitude based). |

### `Bodu.Globalization.Calendar.Providers` — Bundled Easter providers

| Type | Provides |
|---|---|
| <xref:Bodu.Globalization.Calendar.Providers.GregorianEasterSundayNotableDateProvider> | Western (Gregorian) Easter Sunday. |
| <xref:Bodu.Globalization.Calendar.Providers.OrthodoxEasterSundayNotableDateProvider> | Eastern (Julian-anchored) Easter Sunday. |

Region-specific holiday rule providers ship separately in `Bodu.Globalization.Calendar.Data.*` companion packages on independent release schedules — see [Calendar data packs](../../guides/calendar/data-packs.md).

### `Bodu.Globalization.Calendar.Plugins` — Plugin host with trust policies

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.Plugins.INotableDatePlugin>, <xref:Bodu.Globalization.Calendar.Plugins.INotableDateRulePlugin>, <xref:Bodu.Globalization.Calendar.Plugins.INotableDateAlgorithmPlugin> | Plugin contracts for external rule / algorithm packages. |
| <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginAttribute> | Marker attribute identifying a plugin entry point. |
| <xref:Bodu.Globalization.Calendar.Plugins.ExternalPluginLoader> | Loads plugins from a directory or assembly with trust-policy gating. |
| <xref:Bodu.Globalization.Calendar.Plugins.IPluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.CompositePluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.DelegatingPluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.StrongNamePluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.FileHashPluginTrustPolicy> | Trust policies (deny by default; opt in by strong name, file hash, composition, or custom delegate). |
| <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustContext>, <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustResult> | Trust-evaluation inputs and outputs (record structs). |
| <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginActivationException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginMissingAttributeException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginNotTrustedException> | Plugin-host exceptions. |

### `Bodu.Extensions` — Working-day arithmetic

Two parallel extension surfaces — <xref:Bodu.Extensions.NotableDateOnlyExtensions> (over `DateOnly`) and <xref:Bodu.Extensions.NotableDateTimeExtensions> (over `DateTime`) — hang off an `INotableDateService` to give working-day-aware date arithmetic.

| Operation | Methods |
|---|---|
| Lookup | `IsNotableDate`, `IsWorkingDay`, `IsNonWorkingDay` |
| Enumerate | `EnumerateNotableDates`, `EnumerateWorkingDays`, `EnumerateNonWorkingDays`, `GetNotableDates`, `GetNotableDatesInMonth`, `GetNotableDatesInYear` |
| Navigate | `NextWorkingDay`, `PreviousWorkingDay`, `NextNonWorkingDay`, `PreviousNonWorkingDay`, `NextNotableDate`, `PreviousNotableDate` |
| Snap / arithmetic | `SnapToWorkingDay`, `SnapToWorkingDayBackward`, `SnapToNearestWorkingDay`, `AddWorkingDays`, `WorkingDaysBetween` |
| Batched lookup | <xref:Bodu.Globalization.Calendar.NotableDateContext> (ambient service for chained extension calls). |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Resolve all notable dates in a country for a year | `service.GetNotableDates(year, territoryCode: "AU")` |
| "Is today a public holiday in NSW?" | `dateOnly.IsNotableDate(service, territoryCode: "AU-NSW")` |
| "Add 5 working days to today (skipping weekends and holidays)" | `dateOnly.AddWorkingDays(service, 5, territoryCode: "AU-NSW")` |
| Compute Easter Sunday for any year | <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateAlgorithm>`.Calculate(year)` |
| Compute Tibetan Losar (Lunar New Year) | <xref:Bodu.Globalization.Calendar.Algorithms.LosarNotableDateAlgorithm>`.Calculate(year)` |
| Author rules in XML / JSON and load them | <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider> / <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider> |
| Layer runtime overrides on top of a base rule set | <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider> |
| Load rules / algorithms from external assemblies safely | <xref:Bodu.Globalization.Calendar.Plugins.ExternalPluginLoader> + a trust policy |
| Apply observance adjustments (e.g. holiday-falls-on-weekend → next Monday) | <xref:Bodu.Globalization.Calendar.ObservanceAdjustment> + <xref:Bodu.Globalization.Calendar.AdjustmentHandlerRegistry> |
| Filter resolved notable dates by category, tag, or date range | <xref:Bodu.Globalization.Calendar.NotableDateFilter>`.ForCategory(...)`, `.WithTag(...)`, `.InDateRange(...)` (combine with `.And` / `.Or`). |

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal samples for the algorithm, the service, and working-day arithmetic.
- **[Bodu.Globalization.Calendar guides](../../guides/calendar/index.md)** — using `NotableDateService`, algorithms, rule authoring, data packs.
- **[Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md)** — full type-by-type docs.
- **[Calendar data packs](../../guides/calendar/data-packs.md)** — region-specific public-holiday rule providers (`AmericasCalendarData`, `EuropeCalendarData`, `AsiaPacificCalendarData`).
