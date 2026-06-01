---
title: Bodu.Globalization.Calendar — Introduction
---

# Bodu.Globalization.Calendar

**Bodu.Globalization.Calendar** resolves authored calendar rules into concrete notable dates such as public holidays, observances, religious festivals, and regional events. Consumers can query dates by year, territory, category, tag, or date range, and use the resolved dates for working-day-aware arithmetic.

Rules can be supplied from embedded XML / JSON resources, companion data packs, runtime override providers, or external plugins. More advanced scenarios can extend the library with custom algorithms, adjustment handlers, collision resolvers, localizers, and plugin trust policies.

## Calendar package family

The calendar runtime is intentionally small. Region-specific holiday data, fluent rule authoring, and dependency-injection registration ship as separate companion packages so they can release on their own cadence without forcing a main-library rebuild.

![Bodu.Globalization.Calendar package family — runtime, companions, and data packs](../../images/diagrams/calendar-package-family.svg)

| Package | Role |
|---|---|
| **`Bodu.Globalization.Calendar`** | The runtime — rule engine, resolution pipeline, working-day extensions. Required by every other calendar package. |
| `Bodu.Globalization.Calendar.Builder` | Fluent, chainable C# API for authoring `NotableDateRule` documents. Round-trips to and from the XML / JSON forms consumed by the resource providers. See the [builder guide](../../guides/calendar/notable-date-builder.md). |
| `Bodu.Globalization.Calendar.DependencyInjection` | `IServiceCollection` extensions for registering `NotableDateService`, rule providers, and adjustment handlers in a `Microsoft.Extensions.DependencyInjection` container. See the [DI guide](../../guides/calendar/dependency-injection.md). |
| `Bodu.Globalization.Calendar.Data.Americas` | Curated public-holiday rules for `US`, `CA`. |
| `Bodu.Globalization.Calendar.Data.Europe` | Curated rules for eight European countries including `DE`, `ES`, `FR`, `GB`, `IT`, `NL`. |
| `Bodu.Globalization.Calendar.Data.AsiaPacific` | Curated rules for eight Asia-Pacific countries including `AU` (with subdivisions), `CN`, `IN`, `JP`, `KR`, `NZ`. |

The data packs are independent NuGet packages, so consumers pull in only the regions they need. See the [Calendar data packs guide](../../guides/calendar/data-packs.md) for per-pack install commands, territory coverage, and registration patterns.

## Core mental model

![NotableDateService resolution pipeline](../../images/diagrams/calendar-resolution-pipeline.svg)

A single notable date flows through the library in this order:

![Notable date flow — six stages from authored rule to consumer query](../../images/diagrams/calendar-notable-date-flow.svg)

A **`NotableDateRule`** is an authored recipe — the *what* and *how* of a notable date, never the date itself. **`NotableDateService`** loads rules from one or more providers, layers optional override providers, resolves each rule for a requested year using the strategy on the rule (fixed date, *n*th weekday, offset from an anchor, or algorithm), runs the adjustment pipeline against the *nominal* date, and caches the resulting `NotableDate` set per year. Consumers then query that resolved set by territory, category, tag, or date range, or feed it into the working-day extensions.

## Key concepts

| Concept | Plain-language meaning |
|---|---|
| **Rule** | The authored definition of a notable date, not the date itself. |
| **Resolution strategy** | How the rule finds its nominal date: fixed date, *n*th weekday-in-month, offset from an anchor, or algorithm. |
| **Anchor** | A base date that other rules reference (typically algorithmic — e.g. Easter Sunday). |
| **Adjustment** | A post-resolution shift that moves or substitutes the date — e.g. weekend rollover, substitute Monday. |
| **Territory** | Geographic scope where the rule applies — `AU` for Australia, `AU-NSW` for New South Wales. |
| **Category / tag** | Classification used for filtering and display. `NotableDateCategory` is the well-known enum; tags are free-form strings. |
| **Provider** | A source of rules — embedded XML / JSON, a companion data pack, or a custom implementation. |
| **Override** | A way to add, replace, or remove rules at runtime, layered over a base source. |
| **Resolved notable date** | The concrete `NotableDate` returned to consumers — date, name, category, territory, optional multi-day span. |

For the full glossary, see [Core concepts](concepts.md).

### Notable date vs. non-working day

Not every notable date is necessarily a non-working day. A rule can describe a public holiday, observance, remembrance day, religious festival, or regional event. Working-day operations use category, territory, weekend rules, and any configured non-working-day semantics to decide whether a date should be skipped.

### Territory containment

Territory codes are hierarchical. A query for `AU-NSW` can include rules authored for `AU` as well as rules specific to `AU-NSW`, allowing national and regional rules to compose naturally. The same applies to `GB-ENG` inheriting `GB`, `US-CA` inheriting `US`, and so on.

## Worked example — New Year's Day in NSW

A single rule traces the pipeline end-to-end:

1. A rule defines January 1 for territory `AU`, category `Holiday`, `IsNonWorkingDay = true`, with a weekend-rollover adjustment.
2. The service resolves the fixed date for the requested year — the *nominal* date is `2027-01-01` (a Friday).
3. The adjustment evaluates the nominal date. In 2027 it falls on a Friday, so no adjustment fires and the *observed* date stays `2027-01-01`. (If New Year's Day had landed on a weekend, `MoveToNextWeekday` would emit a substitute Monday instead.)
4. A query for territory `AU-NSW` returns the resolved date because `AU-NSW` is contained by `AU`.
5. Working-day arithmetic — `someDate.AddWorkingDays(service, 1, "AU-NSW")` — skips the resolved date because its rule was authored as non-working.

The same flow applies to every other rule — only the strategy in step 2 and the adjustment outcome in step 3 differ.

## Common scenarios

| Scenario | Reach for |
|---|---|
| Resolve all notable dates in a country for a year | `service.GetNotableDates(year, territoryCode: "AU")` |
| "Is today a public holiday in NSW?" | `dateOnly.IsNotableDate(service, territoryCode: "AU-NSW")` |
| "Add 5 working days to today (skipping weekends and holidays)" | `dateOnly.AddWorkingDays(service, 5, territoryCode: "AU-NSW")` |
| Compute Easter Sunday for any year | <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateAlgorithm>`.Calculate(year)` |
| Compute Tibetan Losar (Lunar New Year) | <xref:Bodu.Globalization.Calendar.Algorithms.LosarNotableDateAlgorithm>`.Calculate(year)` |
| Author rules in XML / JSON and load them | <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider> / <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider> |
| Layer runtime overrides on top of a base rule set | <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider> / <xref:Bodu.Globalization.Calendar.MutableNotableDateRuleOverrideProvider> |
| Register the service in an `IServiceCollection`-based host | `services.AddNotableDates(...)` from `Bodu.Globalization.Calendar.DependencyInjection` — see the [dependency-injection guide](../../guides/calendar/dependency-injection.md). |
| Enumerate the territories / calendar systems covered by the loaded rules | `service.GetSupportedTerritories()` / `service.GetSupportedCalendars()` |
| Pick up runtime override mutations on a live service | `service.Reload()` |
| Load rules / algorithms from external assemblies safely | <xref:Bodu.Globalization.Calendar.Plugins.ExternalPluginLoader> + a trust policy |
| Apply observance adjustments (e.g. holiday-falls-on-weekend → next Monday) | <xref:Bodu.Globalization.Calendar.ObservanceAdjustment> + <xref:Bodu.Globalization.Calendar.AdjustmentHandlerRegistry> |
| Filter resolved notable dates by category, tag, or date range | <xref:Bodu.Globalization.Calendar.NotableDateFilter>`.ForCategory(...)`, `.WithTag(...)`, `.InDateRange(...)` (combine with `.And` / `.Or`). |

## Main types

The same surface, grouped by what role you're playing rather than by namespace.

### Types most consumers use

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.NotableDateService> / <xref:Bodu.Globalization.Calendar.INotableDateService> | Main entry point — resolves, caches, and queries notable dates. |
| <xref:Bodu.Globalization.Calendar.NotableDate> | Resolved output — concrete date, name, category, territory, optional multi-day span. |
| <xref:Bodu.Globalization.Calendar.NotableDateFilter> | Composable two-stage predicate for territory- and category-scoped queries, built via static factory methods (`ForCategory`, `WithTag`, `InDateRange`, `WithName`, `IsNonWorkingDay`, …). |
| <xref:Bodu.Globalization.Calendar.TerritoryCode> | Strongly-typed ISO 3166-1 alpha-2 country / subdivision code with containment semantics. |
| <xref:Bodu.Globalization.Calendar.NotableDateCategory> | `Holiday` / `Observance` / `Remembrance` / `Cultural` / `Religious` / `Seasonal` / `Civic` / `Bank` / `School` / `Regional` / `Other` / `None`. |
| <xref:Bodu.Extensions.NotableDateOnlyExtensions>, <xref:Bodu.Extensions.NotableDateTimeExtensions> | Working-day arithmetic over `DateOnly` / `DateTime` — `IsWorkingDay`, `NextWorkingDay`, `AddWorkingDays`, `WorkingDaysBetween`, `SnapToWorkingDay`, … See [Working-day arithmetic](../../guides/calendar/working-days.md). |

### Types used when authoring rules

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.NotableDateRule> | Authored recipe — strategy, category, territory, adjustments, duration. |
| <xref:Bodu.Globalization.Calendar.DateResolutionStrategy> | Enum: `Fixed`, `DayOfWeekInMonth`, `OffsetFromAnchor`, `Algorithm`. |
| <xref:Bodu.Globalization.Calendar.ObservanceAdjustment> + <xref:Bodu.Globalization.Calendar.AdjustmentTrigger>, <xref:Bodu.Globalization.Calendar.AdjustmentAction>, <xref:Bodu.Globalization.Calendar.AdjustmentReason> | Conditional date-shift specification (trigger + action + scope). |
| <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider> | Built-in resource-backed rule providers for embedded XML / JSON. |
| <xref:Bodu.Globalization.Calendar.NotableDateRuleParser>, <xref:Bodu.Globalization.Calendar.NotableDateRuleJsonParser>, <xref:Bodu.Globalization.Calendar.ParsedNotableDateDocument>, <xref:Bodu.Globalization.Calendar.NotableDateRuleUseGroup>, <xref:Bodu.Globalization.Calendar.NotableDateRuleUseDirective>, <xref:Bodu.Globalization.Calendar.NotableDateRuleOverrideBody>, <xref:Bodu.Globalization.Calendar.RuleRemoval> | XML / JSON rule-document model. |

### Built-in algorithms

The `Bodu.Globalization.Calendar.Algorithms` namespace ships concrete algorithm classes that consumers can call directly or that rules can reference via `AlgorithmKey`:

| Type | Computes |
|---|---|
| <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateAlgorithm> | Easter Sunday using the Gregorian or Orthodox computus. |
| <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarNotableDateAlgorithm> + <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarMonth> + <xref:Bodu.Globalization.Calendar.Algorithms.HinduPaksha> | Hindu lunisolar dates (Diwali, Holi, …). |
| <xref:Bodu.Globalization.Calendar.Algorithms.LosarNotableDateAlgorithm> | Tibetan New Year. |
| <xref:Bodu.Globalization.Calendar.Algorithms.VesakNotableDateAlgorithm> | Buddhist Vesak / Buddha Day. |
| <xref:Bodu.Globalization.Calendar.Algorithms.AsalhaPujaNotableDateAlgorithm> | Asalha Puja. |
| <xref:Bodu.Globalization.Calendar.Algorithms.QingmingNotableDateAlgorithm> | Qingming festival (sun-longitude based). |
| <xref:Bodu.Globalization.Calendar.Algorithms.GregorianEasterSundayNotableDateProvider>, <xref:Bodu.Globalization.Calendar.Algorithms.OrthodoxEasterSundayNotableDateProvider> | Bundled Easter providers — Western (Gregorian) and Eastern (Julian-anchored). |

Region-specific holiday rule providers ship separately in `Bodu.Globalization.Calendar.Data.*` companion packages on independent release schedules — see [Calendar data packs](../../guides/calendar/data-packs.md).

### Types used when extending the library

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm>, <xref:Bodu.Globalization.Calendar.INotableDateAlgorithmRegistry>, <xref:Bodu.Globalization.Calendar.NotableDateAlgorithmRegistry> | Pluggable algorithm contract and registry. |
| <xref:Bodu.Globalization.Calendar.INotableDateProvider>, <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider>, <xref:Bodu.Globalization.Calendar.MutableNotableDateRuleOverrideProvider>, <xref:Bodu.Globalization.Calendar.NotableDateRuleResourceProviderBase> | Rule-source contracts, the runtime-mutable override provider, and the base class shared by the built-in resource providers. |
| <xref:Bodu.Globalization.Calendar.IAdjustmentHandler>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.IAdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerContext>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerResult> | Handler model for adjustments. |
| <xref:Bodu.Globalization.Calendar.INotableDateCollisionResolver>, <xref:Bodu.Globalization.Calendar.DefaultNotableDateCollisionResolver> | Behavior when multiple rules resolve to the same date. |
| <xref:Bodu.Globalization.Calendar.INotableDateNameLocalizer> | Pluggable display-name localization. |
| <xref:Bodu.Globalization.Calendar.IResourcePathResolver>, <xref:Bodu.Globalization.Calendar.ResourcePathResolver>, <xref:Bodu.Globalization.Calendar.ResourcePathResolverOptions> | Resource-path resolution for embedded providers. |

## Dependency injection

The optional `Bodu.Globalization.Calendar.DependencyInjection` companion package wires `INotableDateService` into a `Microsoft.Extensions.DependencyInjection` container via `services.AddNotableDates(...)`, binds [`NotableDateOptions`](xref:Bodu.Globalization.Calendar.DependencyInjection.NotableDateOptions) from `IConfiguration`, exposes the fluent <xref:Bodu.Globalization.Calendar.DependencyInjection.INotableDateServiceBuilder> for layering rule providers and collaborators, and provides a `PostConfigure` hook for projecting from consumer-defined POCOs. See the [dependency-injection guide](../../guides/calendar/dependency-injection.md) for the full walkthrough.

## Advanced extensibility

External plugin hosting and trust policies for loading rules / algorithms from external assemblies (strong-name, file-hash, composite, or delegating policies) are documented in [Building and extending the service](../../guides/calendar/building-the-service.md). A first read of this introduction does not need them.

## Where to go next

- **[Core concepts](concepts.md)** — full vocabulary: rule vs. date, nominal vs. observed, provider vs. override, anchor, category vs. tag, working day vs. non-working day.
- **[Getting started](getting-started.md)** — install + minimal samples for the algorithm, the service, and working-day arithmetic.
- **[Bodu.Globalization.Calendar guides](../../guides/calendar/index.md)** — using `NotableDateService`, algorithms, rule authoring, working-day arithmetic, territories, data packs.
- **[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)** — full type-by-type docs.
- **[Calendar data packs](../../guides/calendar/data-packs.md)** — region-specific public-holiday rule providers (`AmericasCalendarData`, `EuropeCalendarData`, `AsiaPacificCalendarData`).
