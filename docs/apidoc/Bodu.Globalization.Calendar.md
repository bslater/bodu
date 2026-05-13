---
uid: Bodu.Globalization.Calendar
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar** resolves culturally and algorithmically significant dates — holidays, observances, religious festivals, and recurring notable dates — across a mixture of definition styles: fixed dates, *n*th-weekday-of-month recurrences, offsets from other notable dates, and dynamic calculators (e.g. the Gregorian Computus for Easter Sunday, lunisolar dates for Hindu festivals, sun-longitude based dates for Qingming).

Reach for this library when a `DateTime.DayOfWeek` check is not enough: when you need Easter Sunday in year *N*, when a business-day rule shifts a fixed holiday because it fell on a weekend, or when you need a cached, culture-aware calendar of notable dates for a range of years driven from an XML or JSON rule source, optionally extended by external plugin assemblies under a deny-by-default trust policy.

## Static documentation

- **[Bodu.Globalization.Calendar introduction](~/docs/calendar/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Globalization.Calendar getting started](~/docs/calendar/getting-started.md)** — install and minimal samples for algorithms, the service, and working-day arithmetic.
- **[Bodu.Globalization.Calendar guides](~/guides/calendar/index.md)** — [`NotableDateService`](~/guides/calendar/notable-dates.md), [rule authoring](~/guides/calendar/rule-authoring.md), [date-calculation algorithms](~/guides/calendar/algorithms.md), [companion data packs](~/guides/calendar/data-packs.md).

## Key types

**Entry points and results**

- <xref:Bodu.Globalization.Calendar.NotableDateService> — the main entry point (and sole <xref:Bodu.Globalization.Calendar.INotableDateService> implementation). Composes a rule provider chain, an algorithm registry, an adjustment-handler registry, and a collision resolver to materialize <xref:Bodu.Globalization.Calendar.NotableDate> instances for a year or range, with internal per-year caching.
- <xref:Bodu.Globalization.Calendar.NotableDate> — the materialized result record: the resolved occurrence plus metadata (name, <xref:Bodu.Globalization.Calendar.NotableDateCategory>, cultural applicability via <xref:Bodu.Globalization.Calendar.TerritoryCode>, the original pre-adjustment date if a rollover rule moved it, and the inclusive duration for multi-day events).
- <xref:Bodu.Globalization.Calendar.NotableDateCategory> — categorization: `Holiday`, `Observance`, `Remembrance`, `Cultural`, `Religious`, `Seasonal`, `Civic`, `Bank`, `School`, `Regional`, `Other`, `None`.
- <xref:Bodu.Globalization.Calendar.NotableDateFilter> — composable two-stage predicate built via static factory methods (`ForCategory`, `WithTag`, `WithName`, `IsNonWorkingDay`, `InDateRange`, `WasAdjusted`, …) and combined with `And`, `Or`, `AllOf`, `AnyOf`. Rule-level predicates are evaluated as a primary gate *before* the date is resolved.
- <xref:Bodu.Globalization.Calendar.TerritoryCode> — strongly-typed ISO 3166-1 alpha-2 country / subdivision code with containment semantics.

**Rules and resolution**

- <xref:Bodu.Globalization.Calendar.NotableDateRule> — an immutable rule record describing how a notable date is defined (fixed, day-of-week-in-month, offset, or delegated to a named algorithm).
- <xref:Bodu.Globalization.Calendar.NotableDateRuleParser>, <xref:Bodu.Globalization.Calendar.NotableDateRuleJsonParser>, <xref:Bodu.Globalization.Calendar.ParsedNotableDateDocument>, <xref:Bodu.Globalization.Calendar.NotableDateRuleUseGroup>, <xref:Bodu.Globalization.Calendar.NotableDateRuleUseDirective>, <xref:Bodu.Globalization.Calendar.NotableDateRuleOverrideBody>, <xref:Bodu.Globalization.Calendar.RuleRemoval> — XML / JSON rule-document model.
- <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider>, <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.NotableDateRuleResourceProviderBase> — plug-points for the rule source (built-in XML / JSON, overlays, custom).
- <xref:Bodu.Globalization.Calendar.INotableDateProvider>, <xref:Bodu.Globalization.Calendar.INotableDateNameLocalizer> — extension surfaces for custom date sources and culture-specific naming.
- <xref:Bodu.Globalization.Calendar.INotableDateCollisionResolver>, <xref:Bodu.Globalization.Calendar.DefaultNotableDateCollisionResolver> — decide what happens when two rules resolve to the same date.
- <xref:Bodu.Globalization.Calendar.IResourcePathResolver>, <xref:Bodu.Globalization.Calendar.ResourcePathResolver>, <xref:Bodu.Globalization.Calendar.ResourcePathResolverOptions> — resource-path resolution for embedded providers.

**Dynamic calculators — `Bodu.Globalization.Calendar.Algorithms`**

- <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm>, <xref:Bodu.Globalization.Calendar.INotableDateAlgorithmRegistry>, <xref:Bodu.Globalization.Calendar.NotableDateAlgorithmRegistry> — the contract and registry for year-keyed date computation.
- <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateAlgorithm>, <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateProviderBase> — Easter Sunday, Gregorian or Orthodox via the matching provider in `Bodu.Globalization.Calendar.Providers`.
- <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarNotableDateAlgorithm> + <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarMonth> + <xref:Bodu.Globalization.Calendar.Algorithms.HinduPaksha> — Hindu lunisolar dates (Diwali, Holi, …).
- <xref:Bodu.Globalization.Calendar.Algorithms.LosarNotableDateAlgorithm> — Tibetan New Year.
- <xref:Bodu.Globalization.Calendar.Algorithms.VesakNotableDateAlgorithm> — Buddhist Vesak.
- <xref:Bodu.Globalization.Calendar.Algorithms.AsalhaPujaNotableDateAlgorithm> — Asalha Puja.
- <xref:Bodu.Globalization.Calendar.Algorithms.QingmingNotableDateAlgorithm> — Qingming festival.

**Bundled Easter providers — `Bodu.Globalization.Calendar.Providers`**

- <xref:Bodu.Globalization.Calendar.Providers.GregorianEasterSundayNotableDateProvider>, <xref:Bodu.Globalization.Calendar.Providers.OrthodoxEasterSundayNotableDateProvider> — Western (Gregorian) and Eastern (Julian-anchored) Easter providers.

**Adjustment pipeline**

- <xref:Bodu.Globalization.Calendar.ObservanceAdjustment> — conditional date-shift specification (trigger + action + scope).
- <xref:Bodu.Globalization.Calendar.AdjustmentTrigger>, <xref:Bodu.Globalization.Calendar.AdjustmentAction>, <xref:Bodu.Globalization.Calendar.AdjustmentReason> — the adjustment-rule vocabulary.
- <xref:Bodu.Globalization.Calendar.IAdjustmentHandler>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.IAdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerContext>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerResult> — handler model for adjustments.

**Plugin host — `Bodu.Globalization.Calendar.Plugins`**

- <xref:Bodu.Globalization.Calendar.Plugins.INotableDatePlugin>, <xref:Bodu.Globalization.Calendar.Plugins.INotableDateRulePlugin>, <xref:Bodu.Globalization.Calendar.Plugins.INotableDateAlgorithmPlugin>, <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginAttribute>, <xref:Bodu.Globalization.Calendar.Plugins.ExternalPluginLoader> — plugin contracts and host.
- <xref:Bodu.Globalization.Calendar.Plugins.IPluginTrustPolicy> with deny-by-default policies <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.StrongNamePluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.FileHashPluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.CompositePluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.DelegatingPluginTrustPolicy>; trust evaluation via <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustContext> / <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustResult>.
- Exceptions: <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginActivationException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginMissingAttributeException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginNotTrustedException>.

**Working-day arithmetic — `Bodu.Extensions`**

- <xref:Bodu.Extensions.NotableDateOnlyExtensions>, <xref:Bodu.Extensions.NotableDateTimeExtensions> — parallel surfaces over `DateOnly` and `DateTime`: `IsWorkingDay`, `IsNotableDate`, `NextWorkingDay`, `PreviousWorkingDay`, `SnapToWorkingDay`, `AddWorkingDays`, `WorkingDaysBetween`, `EnumerateNotableDates`, `GetNotableDatesInMonth`, `GetNotableDatesInYear`, …
- <xref:Bodu.Globalization.Calendar.NotableDateContext> — ambient `INotableDateService` for chained extension calls.

## Companion data packs

National public-holiday rule providers ship in three companion assemblies so the data can be re-released independently of the main library:

- **Bodu.Globalization.Calendar.Data.Americas** — United States, Canada.
- **Bodu.Globalization.Calendar.Data.Europe** — Germany, Spain, France, United Kingdom, Ireland, Italy, Netherlands, Sweden.
- **Bodu.Globalization.Calendar.Data.AsiaPacific** — Australia, China, India, Japan, South Korea, Malaysia, New Zealand, Singapore.

Each pack exposes a static `<Pack>CalendarData` factory (`AmericasCalendarData.CreateUnitedStatesProvider()`, `EuropeCalendarData.CreateGermanyProvider()`, `AsiaPacificCalendarData.CreateAustraliaProvider()`, etc.) that constructs an <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider> with the `[pack, main library]` assembly chain pre-wired. The parameterless `new NotableDateService()` constructor loads only the embedded `default-minimal.xml` (currently New Year's Day) — region-specific rules must come from one of the packs above (or your own provider).

See the [Calendar data packs](~/guides/calendar/data-packs.md) guide for composition patterns.

## Example

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Data.AsiaPacific;

// 1. Compute a single notable date directly.
var algorithm = new EasterSundayNotableDateAlgorithm();
DateTime easter2026 = algorithm.Calculate(2026);          // 2026-04-05
DateTime goodFriday2026 = easter2026.AddDays(-2);

// 2. Or resolve every notable date for a year and territory through the service.
INotableDateRuleProvider auRules = AsiaPacificCalendarData.CreateAustraliaProvider();
INotableDateService service = new NotableDateService(
    ruleProviders:     [ auRules ],
    weekendDefinition:  CalendarWeekendDefinition.SaturdaySunday);

foreach (NotableDate date in service.GetNotableDates(year: 2026, territoryCode: "AU-NSW"))
    Console.WriteLine($"{date.Date:yyyy-MM-dd}  {date.DisplayName}");
```

## Notes

- **Thread safety.** A `NotableDateService` built from immutable providers and a stable algorithm registry is **safe for concurrent reads** after construction. The internal per-year cache is a `ConcurrentDictionary`; `Invalidate()` and `Invalidate(year)` clear it cooperatively.
- **Culture and adjustment.** A <xref:Bodu.Globalization.Calendar.NotableDate> tracks both its calculated anchor (`AdjustmentReason.OriginalDate`) and its adjusted date (`Date`) — so a rule like "if a fixed holiday falls on a Saturday, observe it on the preceding Friday" is applied transparently while still preserving the original for audit and display.
- **Filter optimization.** `NotableDateFilter` evaluates rule-level predicates (`ForCategory`, `WithTag`, `WithName`, `IsNonWorkingDay`) *before* date resolution — non-matching rules skip the algorithm invocation and adjustment pipeline entirely. Date-level predicates (`InDateRange`, `WasAdjusted`, `WithMinDuration`) act after resolution.
- **Target framework.** `net8.0`.
- **Extensibility.** Implement <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm> to add a custom dynamic calculator and register it through <xref:Bodu.Globalization.Calendar.NotableDateAlgorithmRegistry>; implement <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider> to source rules from somewhere other than the embedded XML / JSON; implement <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider> to layer corporate or regional overrides on top.
