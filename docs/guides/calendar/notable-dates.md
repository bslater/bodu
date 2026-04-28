---
title: Using NotableDateService
---

# Using NotableDateService

`NotableDateService` is the main entry point for resolving notable dates (public holidays, observances, religious festivals) for a given year and territory. It loads rules from one or more `INotableDateRuleProvider` sources, merges optional override providers on top, and caches resolved `NotableDate` instances per year in a thread-safe `ConcurrentDictionary`.

## Pattern 1 — resolve the default global rule set

The default constructor loads the embedded global XML rule set, which covers public holidays and major observances for hundreds of territories:

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

// All notable dates for the current year, globally.
IReadOnlyList<NotableDate> dates = service.GetNotableDates(DateTime.Today.Year);

foreach (NotableDate date in dates)
    Console.WriteLine($"{date.Date:d MMM yyyy}  [{date.TerritoryCode}]  {date.Name}");
```

## Pattern 2 — filter by territory

Pass a territory code string to restrict results to a specific country or sub-region:

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

// All notable dates for Australia in 2026.
IReadOnlyList<NotableDate> auDates = service.GetNotableDates(2026, territoryCode: "AU");

// New South Wales only.
IReadOnlyList<NotableDate> nswDates = service.GetNotableDates(2026, territoryCode: "AU-NSW");

foreach (NotableDate date in nswDates)
    Console.WriteLine($"{date.Date:d MMM yyyy}  {date.Name}");
```

`TerritoryCode` containment applies: a query for `"AU"` returns both country-level dates and all `"AU-XXX"` subdivision dates. A query for `"AU-NSW"` returns dates scoped to `AU-NSW` and unscoped (global) dates, but not other states.

## Pattern 3 — filter by category

`NotableDateFilter` is a composable predicate built from static factory methods. Pass it to the filtered overload of `GetNotableDates`:

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

// Only public holidays for Great Britain.
NotableDateFilter publicFilter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
IReadOnlyList<NotableDate> holidays = service.GetNotableDates(2026, publicFilter, "GB");

// Non-working public holidays — combine predicates with And:
NotableDateFilter nonWorkingFilter = NotableDateFilter
    .ForCategory(NotableDateCategory.Holiday)
    .And(NotableDateFilter.IsNonWorkingDay());

IReadOnlyList<NotableDate> nonWorking = service.GetNotableDates(2026, nonWorkingFilter, "AU");

// Multiple categories with Or:
NotableDateFilter culturalOrObservance = NotableDateFilter
    .ForCategory(NotableDateCategory.Cultural)
    .Or(NotableDateFilter.ForCategory(NotableDateCategory.Observance));
```

## Pattern 4 — query a date range

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

DateTime from = new DateTime(2026, 3, 1);
DateTime to   = new DateTime(2026, 4, 30);

IReadOnlyList<NotableDate> spring = service.GetNotableDates(from, to, "AU");
```

Multi-day events (e.g. Easter — `DurationDays > 1`) are included when *any* day of their span intersects the query window.

## Pattern 5 — query a single day

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

DateTime anzacDay = new DateTime(2026, 4, 25);
IReadOnlyList<NotableDate> onDay = service.GetNotableDates(anzacDay, "AU");
```

This overload also returns multi-day spans whose anchor lies on a preceding day but whose span covers the queried date.

## Pattern 6 — check non-working days and weekends

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

DateTime christmas = new DateTime(2026, 12, 25);

// True when the date is Saturday or Sunday under the configured weekend definition.
bool isWeekend = service.IsWeekend(christmas);

// True when the date is a weekend or a notable date flagged IsNonWorkingDay for the territory.
bool isNonWorking = service.IsNonWorkingDay(christmas, "AU");
bool isNonWorkingNSW = service.IsNonWorkingDay(christmas, "AU-NSW");
```

## Pattern 7 — layer custom override rules

Override providers add, remove, or patch rules on top of the base rule set without modifying the source XML. Implement `INotableDateRuleOverrideProvider` and pass instances via the `overrideProviders` constructor parameter:

```csharp
using Bodu.Globalization.Calendar;

// Suppress Boxing Day for 2026 and inject a company event.
public sealed class CompanyCalendarOverrides : INotableDateRuleOverrideProvider
{
    public IEnumerable<RuleRemoval> GetRemovals()
    {
        yield return new RuleRemoval("Boxing Day", FromYear: 2026, ToYear: 2026);
    }

    public IEnumerable<NotableDateRule> GetAdditions()
    {
        yield return new NotableDateRule
        {
            Name = "Company Founding Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Observance,
            Month = 6,
            Day = 15,
            IsNonWorkingDay = true,
        };
    }
}

// Wire it up:
var provider = new XmlResourceNotableDateRuleProvider(
    "MyApp/Calendar/Resources/holidays-base.xml",
    new ResourcePathResolver());

var service = new NotableDateService(
    ruleProviders:     new[] { provider },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    overrideProviders: new[] { new CompanyCalendarOverrides() });
```

## Pattern 8 — cache invalidation

The service caches resolved dates per year. Call `Invalidate` when override providers change:

```csharp
// Clear the cached result for 2026 only.
service.Invalidate(2026);

// Clear the entire cache.
service.Invalidate();
```

## Working with NotableDate results

`NotableDate` is an immutable record. Key properties:

| Property | Description |
|---|---|
| `Date` | The resolved anchor date. |
| `EndDate` | The inclusive last day (`Date + DurationDays - 1`). |
| `DurationDays` | Span in days (1 for single-day events). |
| `Name` | Canonical English name. |
| `DisplayName` | Name qualified with territory and calendar suffix when scoped. |
| `Category` | `NotableDateCategory` value. |
| `TerritoryCode` | Territory the date applies to, or `null` for global. |
| `IsNonWorkingDay` | Whether the date is flagged as a non-working day. |
| `WasAdjusted` | Whether the date was shifted by an `ObservanceAdjustment`. |
| `AdjustmentReason` | Original date and adjustment details when `WasAdjusted` is `true`. |
| `Tags` | Optional non-exclusive classification tags (e.g. `"Christian"`, `"Federal"`). |

```csharp
foreach (NotableDate date in service.GetNotableDates(2026, "AU"))
{
    Console.WriteLine($"{date.Date:d}  {date.DisplayName}");

    if (date.DurationDays > 1)
        Console.WriteLine($"  Multi-day: ends {date.EndDate:d}");

    if (date.WasAdjusted)
        Console.WriteLine($"  Shifted from {date.AdjustmentReason!.OriginalDate:d}");
}
```

## How the filter works

![NotableDateFilter two-stage gate evaluation](../../images/diagrams/calendar-filter-gates.svg)

Filtered queries apply the predicate in two stages:

1. **Primary gate (rule-level)** — evaluated against each `NotableDateRule` *before* the date is resolved. Rules that fail are skipped entirely, avoiding the cost of algorithm invocation and adjustment evaluation. Factory methods such as `ForCategory`, `WithTag`, `WithName`, and `IsNonWorkingDay` operate at this stage.

2. **Secondary gate (date-level)** — evaluated against the materialised `NotableDate` *after* resolution. Factory methods such as `InDateRange`, `WasAdjusted`, and `WithMinDuration` operate here because their result is not known until the date is computed.

Filtered queries bypass the per-year cache so that unfiltered queries continue to return complete cached results.

```csharp
// Efficient: ForCategory is rule-level, so non-matching rules are never resolved.
NotableDateFilter filter = NotableDateFilter
    .ForCategory(NotableDateCategory.Holiday)
    .And(NotableDateFilter.IsNonWorkingDay());

// Date-level: every rule is resolved; the gate acts after resolution.
NotableDateFilter adjusted = NotableDateFilter.WasAdjusted();

// Combined: category pre-screens rules, date-range post-screens resolved dates.
NotableDateFilter easterWeek = NotableDateFilter
    .ForCategory(NotableDateCategory.Holiday)
    .And(NotableDateFilter.InDateRange(
        new DateTime(2026, 4, 1),
        new DateTime(2026, 4, 14)));

// AllOf / AnyOf for multi-filter composition:
NotableDateFilter combined = NotableDateFilter.AllOf(
    NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
    NotableDateFilter.IsNonWorkingDay(),
    NotableDateFilter.WithTag("Federal"));
```

## Understanding the resolution pipeline

```
INotableDateRuleProvider(s)              → base rules
  + INotableDateRuleOverrideProvider(s)  → merged effective rules
      → NotableDateRuleResolver.ResolveAnchorDate(rule, year)
          → NotableDateAdjuster (ObservanceAdjustment rules)
              → NotableDate(s) for the year  [cached per year]
```

1. **Rule loading** — base providers supply the initial rule list; override providers add, remove, or patch rules on top.
2. **Anchor resolution** — `NotableDateRuleResolver` dispatches each rule's `DateResolutionStrategy` to the correct logic: fixed date, *n*th weekday-of-month, algorithm, or offset from another rule.
3. **Adjustment** — `NotableDateAdjuster` applies `ObservanceAdjustment` specs to the anchor, shifting it when the trigger condition is met (e.g. falls on a weekend).
4. **Caching** — the resolved list for each year is stored in a thread-safe `ConcurrentDictionary` and reused on subsequent unfiltered calls. `Invalidate()` clears the cache.

## Where to go next

- [Date calculation algorithms](algorithms.md) — the built-in algorithm types and how to implement a custom one.
- [Authoring notable date rules](rule-authoring.md) — in-code objects, XML resource files, satellite assemblies, and runtime overrides.
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full type reference.
