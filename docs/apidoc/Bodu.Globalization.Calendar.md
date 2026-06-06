---
uid: Bodu.Globalization.Calendar
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar** resolves culturally and algorithmically significant dates — public holidays, observances, religious festivals, and recurring notable dates — from an authored *rule document* into concrete occurrences for a requested year, date, or range and territory.

A rule describes the *what* and *how* of a notable date — a fixed calendar date, an *n*th-weekday-of-month recurrence, a weekday near a fixed date, a fixed offset from another rule, or a named algorithm (Gregorian / Orthodox Easter, solar equinoxes, lunar phases, lunisolar festivals). Rules are authored on the **v2 cookbook schema** (`urn:bodu:globalization:calendar`) as XML or JSON, loaded eagerly into an immutable <xref:Bodu.Globalization.Calendar.NotableDateResource>, and resolved through <xref:Bodu.Globalization.Calendar.NotableDateService>.

Reach for this library when a `DateTime.DayOfWeek` check is not enough: when you need Easter Sunday in year *N*, when a fixed holiday that lands on a weekend rolls to a substitute weekday, or when you need a culture-aware set of notable dates for a territory and year — optionally extended by external plugin assemblies under a deny-by-default trust policy.

## Static documentation

- **[Bodu.Globalization.Calendar introduction](~/docs/calendar/index.md)** — package family, mental model, headline types, scenarios.
- **[Bodu.Globalization.Calendar getting started](~/docs/calendar/getting-started.md)** — install and minimal samples for loading a resource, resolving dates, and working-day arithmetic.
- **[Bodu.Globalization.Calendar guides](~/guides/calendar/index.md)** — [`NotableDateService`](~/guides/calendar/notable-dates.md), [rule authoring](~/guides/calendar/rule-authoring.md), [date-calculation algorithms](~/guides/calendar/algorithms.md), [companion data packs](~/guides/calendar/data-packs.md), [dependency injection](~/guides/calendar/dependency-injection.md).

## Companion packages

- [`Bodu.Globalization.Calendar.Builder`](Bodu.Globalization.Calendar.Builder.md) — a fluent C# API for authoring notable-date documents on the v2 cookbook schema, with XML / JSON serialization and load/save.
- [`Bodu.Globalization.Calendar.DependencyInjection`](Bodu.Globalization.Calendar.DependencyInjection.md) — `Microsoft.Extensions.DependencyInjection` integration: `services.AddNotableDateService(...)` / `AddReloadableNotableDateService(...)` register `INotableDateService` as a singleton over a loaded `NotableDateResource`.
- [`Bodu.Globalization.Calendar.Plugins`](Bodu.Globalization.Calendar.Plugins.md) — trust-gated loading of external assemblies that contribute custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> implementations.
- [`Bodu.Globalization.Calendar.Data.*`](Bodu.Globalization.Calendar.Data.md) — curated public-holiday resources for the Americas, Asia-Pacific, and Europe territory bundles.

## Key types

**Entry points and results**

- <xref:Bodu.Globalization.Calendar.NotableDateService> — the main resolver (and the primary <xref:Bodu.Globalization.Calendar.INotableDateService> implementation). Built over an immutable <xref:Bodu.Globalization.Calendar.NotableDateResource>, optionally composed with a custom algorithm registry, collision resolver, adjustment handler / trigger registries, and code-first providers.
- <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService> — an `INotableDateService` built over an <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider> that rebuilds itself when the provider's resource reference is swapped.
- <xref:Bodu.Globalization.Calendar.NotableDate> — the materialized result record: the emitted `Date` (the observed date when an adjustment applied), the originally calculated `ActualDate`, `IsObserved`, the rule `Identity`, `DisplayName`, `TerritoryCode`, <xref:Bodu.Globalization.Calendar.NotableDateCategory>, `Priority`, `DurationDays` / `EndDate`, `IsNonWorkingDay`, `Tags`, and the `AdjustmentPolicyId` / `AdjustmentReason` audit pair.
- <xref:Bodu.Globalization.Calendar.NotableDateFilter> — a composable predicate over resolved occurrences, built via static factory methods (`ForCategory`, `ForAnyCategory`, `WithName`, `WithId`, `WithTag`, `WithAnyTag`, `WithAllTags`, `WithMinDuration`, `IsNonWorkingDay`, `WasAdjusted`, `InDateRange`) and combined with `And`, `Or`, `Not`, `AllOf`, `AnyOf`.
- <xref:Bodu.Globalization.Calendar.NotableDateCategory> — categorization: `PublicHoliday`, `BankHoliday`, `Observance`, `Remembrance`, `Cultural`, `Religious`, `Seasonal`, `Civic`, `School`, `Regional`, `Other`, `None`.
- <xref:Bodu.Globalization.Calendar.TerritoryCode> — a strongly-typed ISO 3166 country / subdivision code with parent/child containment (`AU-NSW` is contained by `AU`). Implicitly converts to the `string` territory argument the service accepts.
- <xref:Bodu.Globalization.Calendar.DateRange>, <xref:Bodu.Globalization.Calendar.CalendarSystem> — the inclusive `[StartDate, EndDate]` query range and the calendar a rule's strategy is expressed in (`Gregorian`, `Hijri`, `UmmAlQura`, `Hebrew`, `Persian`, `ChineseLunisolar`).

**Resources and rule model**

- <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> — the entry point that parses XML / JSON (string or `Stream`), resolves `<Imports>`, applies ID-targeted overrides, and returns a validated <xref:Bodu.Globalization.Calendar.NotableDateResource>; throws <xref:Bodu.Globalization.Calendar.NotableDateValidationException> carrying the <xref:Bodu.Globalization.Calendar.NotableDateValidationDiagnostic> list on any error-severity diagnostic.
- <xref:Bodu.Globalization.Calendar.NotableDateResource>, <xref:Bodu.Globalization.Calendar.NotableDateDefinition>, <xref:Bodu.Globalization.Calendar.NotableDateRule>, <xref:Bodu.Globalization.Calendar.RuleApplicability>, <xref:Bodu.Globalization.Calendar.NotableDateRuleIdentity> — the immutable loaded document: a resource of notable-date definitions, each carrying one or more rules (applicability + one strategy + adjustment references + tags).
- <xref:Bodu.Globalization.Calendar.CommonNotableDateResources> — the resolver delegate over the bundled common catalogues (`global-core`, `christian-western`, `global-islamic`, …) that authored documents import by name.
- <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider>, <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider> — supply the current resource for reload; the mutable provider swaps it in via `Reload(...)` so a `ReloadableNotableDateService` picks up the change.
- <xref:Bodu.Globalization.Calendar.INotableDateProvider>, <xref:Bodu.Globalization.Calendar.INotableDateNameLocalizer>, <xref:Bodu.Globalization.Calendar.NotableDateNameLocalizer> — code-first contribution of finished occurrences, and culture-specific display-name localization (applied through <xref:Bodu.Globalization.Calendar.NotableDateLocalizationExtensions>).

**Date-calculation strategies and algorithms — `Bodu.Globalization.Calendar.Algorithms`**

- <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy> and the strategies a rule maps to: <xref:Bodu.Globalization.Calendar.Algorithms.FixedDateStrategy>, <xref:Bodu.Globalization.Calendar.Algorithms.DayOfWeekInMonthStrategy>, <xref:Bodu.Globalization.Calendar.Algorithms.RelativeWeekdayInMonthStrategy>, <xref:Bodu.Globalization.Calendar.Algorithms.WeekdayNearDateStrategy>, <xref:Bodu.Globalization.Calendar.Algorithms.OffsetFromRuleStrategy>, <xref:Bodu.Globalization.Calendar.Algorithms.AlgorithmDateStrategy>.
- <xref:Bodu.Globalization.Calendar.Algorithms.AlgorithmDateStrategy> dispatches a named key (`western-easter`, `orthodox-easter`, `vernal-equinox`, `autumnal-equinox`, `qingming`, `vesak`, `asalha-puja`, `losar`, `matariki`, and the Hindu-festival keys) to the bundled astronomical calculators, falling through to a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithmRegistry> for unknown keys.
- <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm>, <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithmRegistry>, <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> — the custom-algorithm contract (`DateOnly? Calculate(int year)`) and its chainable registry. <xref:Bodu.Globalization.Calendar.Algorithms.StrategyResolutionContext> is the per-resolution context passed to strategies.

**Range resolution and observed-date policy — `Bodu.Globalization.Calendar.RangeResolution`**

- <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> — the resource-level policy bundle (`<ResolutionPolicy>` in the document) governing duplicates, same-day / span collisions, priority direction, range-inclusion of observed dates, and the working week.
- <xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy>, <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy>, <xref:Bodu.Globalization.Calendar.RangeResolution.PriorityDirection>, <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode>, <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> — the policy vocabulary.
- <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver> — a custom resolver consulted when two rules land on the same day under `CollisionPolicy.Custom`.

**Observance adjustments**

- <xref:Bodu.Globalization.Calendar.AdjustmentPolicy> — a reusable, named shift policy (scope + trigger + action + emission) declared once at the top of a document and referenced by rules via `policyRef`. <xref:Bodu.Globalization.Calendar.AdjustmentScope> constrains where it applies.
- <xref:Bodu.Globalization.Calendar.AdjustmentTrigger>, <xref:Bodu.Globalization.Calendar.AdjustmentAction> — the trigger / action vocabulary (e.g. `IfWeekend` → `MoveToNextWorkingDay`).
- <xref:Bodu.Globalization.Calendar.IAdjustmentHandler>, <xref:Bodu.Globalization.Calendar.IAdjustmentTriggerHandler> and their registries (<xref:Bodu.Globalization.Calendar.AdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.AdjustmentTriggerHandlerRegistry>) plus <xref:Bodu.Globalization.Calendar.AdjustmentHandlerContext> / <xref:Bodu.Globalization.Calendar.AdjustmentTriggerContext> — the custom-handler model for `AdjustmentAction.Custom` / `AdjustmentTrigger.Custom`.

**Working-day arithmetic — `Bodu.Extensions`**

- <xref:Bodu.Extensions.NotableDateOnlyExtensions> (the authoritative `DateOnly` surface), <xref:Bodu.Extensions.NotableDateTimeExtensions>, <xref:Bodu.Extensions.NotableDateTimeOffsetExtensions> — `IsWorkingDay`, `IsNonWorkingDay`, `IsNotableDate`, `NextWorkingDay`, `PreviousWorkingDay`, `SnapToWorkingDay`, `AddWorkingDays`, `WorkingDaysBetween`, `EnumerateWorkingDays`, `GetNotableDates`, … Each takes an `INotableDateService`, a `string territory`, and an optional `Bodu.Core` `WeekPattern` working week (defaults to Monday–Friday).
- <xref:Bodu.Extensions.NotableDateFiscalExtensions> — first / last working day of a fiscal year or quarter for a configurable fiscal-year start month.

> [!NOTE]
> The `Bodu.Extensions` namespace is **not** auto-imported — add `using Bodu.Extensions;` to reach the working-day extension methods. By-year resolution (`service.Resolve(year, territory)`) and `Localize(...)` are extension methods in the core `Bodu.Globalization.Calendar` namespace (<xref:Bodu.Globalization.Calendar.NotableDateServiceExtensions>, <xref:Bodu.Globalization.Calendar.NotableDateLocalizationExtensions>).

## Companion data packs

National public-holiday resources ship in three companion assemblies (namespace `Bodu.Globalization.Calendar.Data`) so the data can be re-released independently of the runtime:

- **Bodu.Globalization.Calendar.Americas** — `CA`, `US` (and subdivisions).
- **Bodu.Globalization.Calendar.Europe** — 28 EU/EEA territories including `DE`, `ES`, `FR`, `GB`, `IE`, `IT`, `NL`, `SE`.
- **Bodu.Globalization.Calendar.AsiaPacific** — `AU` (with subdivisions), `CN`, `IN`, `JP`, `KR`, `MY`, `NZ`, `SG`.

Each pack exposes a static factory — <xref:Bodu.Globalization.Calendar.AmericasCalendarData>, <xref:Bodu.Globalization.Calendar.EuropeCalendarData>, <xref:Bodu.Globalization.Calendar.AsiaPacificCalendarData> — with `SupportedCountries`, `LoadResource(territory)` (returns a `NotableDateResource` with imports resolved against the bundled common catalogues), and `CreateService(territory)` (the resource pre-wired into a `NotableDateService`). See the [Calendar data packs](~/guides/calendar/data-packs.md) guide.

## Example

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Extensions;                       // working-day extension methods

// 1. Build a service from a companion data pack (loads + validates the resource, resolving imports).
NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

// 2. Resolve every notable date for a year and territory (by-year resolution is an extension method).
foreach (NotableDate d in service.Resolve(2026, "AU-NSW"))
    Console.WriteLine($"{d.Date:yyyy-MM-dd}  {d.DisplayName}  ({d.Category})");

// 3. Resolve a single day or an arbitrary range, optionally filtered.
IReadOnlyList<NotableDate> publicHolidays = service.Resolve(
    new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
    "AU-NSW",
    NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));

// 4. Working-day arithmetic honours the resolved non-working dates (needs `using Bodu.Extensions;`).
DateOnly today    = DateOnly.FromDateTime(DateTime.Today);
DateOnly nextOpen = today.NextWorkingDay(service, "AU-NSW");
DateOnly inFive   = today.AddWorkingDays(5, service, "AU-NSW");
```

## Notes

- **Immutable resource, configured service.** Loading produces an immutable, fully validated <xref:Bodu.Globalization.Calendar.NotableDateResource>; the service is configured purely through constructor collaborators. There is no mutable options object — resource-level behaviour (duplicate / collision / observed-date policy, working week) lives in the document's `<ResolutionPolicy>`.
- **Nominal vs. observed.** A <xref:Bodu.Globalization.Calendar.NotableDate> tracks both its calculated `ActualDate` and its emitted `Date`, with `IsObserved` and the `AdjustmentPolicyId` / `AdjustmentReason` pair recording why they differ — so a rule like "if a fixed holiday falls on a weekend, observe it on the next working day" is applied transparently while preserving the original for audit and display.
- **Thread safety.** A `NotableDateService` built from an immutable resource is safe for concurrent reads after construction; `ReloadableNotableDateService` reads its provider's `Current` resource per query and rebuilds atomically when it changes.
- **Territory containment.** Territory is a plain `string` argument (`"AU"`, `"AU-NSW"`). A query for a subdivision includes rules authored for its parent country, so national and regional rules compose naturally.
- **Target framework.** `net8.0`.
- **Extensibility.** Implement <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> and register it through <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> to back an `<Algorithm key="…">` rule; implement <xref:Bodu.Globalization.Calendar.INotableDateProvider> to contribute finished occurrences from code; layer runtime changes through <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider> + <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService>.
