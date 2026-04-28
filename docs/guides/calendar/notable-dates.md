---
title: Using NotableDateService
---

# Using NotableDateService

`NotableDateService` is the main entry point for resolving notable dates (public holidays, observances, religious festivals) for a given year and territory. It loads rules from one or more `INotableDateRuleProvider` sources, layers optional override providers on top, and caches resolved `NotableDate` instances per year.

## Pattern 1 — resolve the default global rule set

The default constructor loads the embedded global XML rule set, which covers public holidays and major observances for hundreds of territories:

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

// Resolve all notable dates for the current year.
IReadOnlyList<NotableDate> dates = service.GetDates(DateTime.Today.Year);

foreach (NotableDate date in dates)
    Console.WriteLine($"{date.Date:d MMM yyyy}  [{date.TerritoryCode}]  {date.Name}");
```

## Pattern 2 — filter by territory

Pass a `TerritoryCode` to restrict results to a specific country or sub-region:

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

TerritoryCode au = TerritoryCode.From("AU");
IReadOnlyList<NotableDate> auDates = service.GetDates(2025, au);

foreach (NotableDate date in auDates)
    Console.WriteLine($"{date.Date:d MMM yyyy}  {date.Name}");
```

## Pattern 3 — filter by category

`NotableDateFilter` lets you combine territory and category criteria in one call:

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

var filter = new NotableDateFilter
{
    TerritoryCode = TerritoryCode.From("GB"),
    Category      = NotableDateCategory.PublicHoliday,
};

IReadOnlyList<NotableDate> holidays = service.GetDates(2025, filter);
```

## Pattern 4 — query a date range

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

DateOnly from = new DateOnly(2025, 3, 1);
DateOnly to   = new DateOnly(2025, 4, 30);

IReadOnlyList<NotableDate> spring = service.GetDates(from, to);
```

Multi-day events (e.g. Easter — `DurationDays > 1`) are returned when *any* day of their span intersects the query window.

## Pattern 5 — check whether a date is a public holiday

```csharp
using Bodu.Globalization.Calendar;

var service = new NotableDateService();

DateOnly anzac = new DateOnly(2025, 4, 25);
TerritoryCode au = TerritoryCode.From("AU");

bool isHoliday = service.IsPublicHoliday(anzac, au);
Console.WriteLine(isHoliday); // True
```

## Pattern 6 — layer custom override rules

Override providers let you add, remove, or modify rules on top of the base rule set without touching the source XML. This is useful for organisation-specific closures or territory-specific corrections.

```csharp
using Bodu.Globalization.Calendar;

INotableDateRuleProvider baseProvider = new XmlResourceNotableDateRuleProvider(
    "MyApp.Resources.holidays-base.xml",
    new ResourcePathResolver());

INotableDateRuleOverrideProvider orgOverrides = new XmlResourceNotableDateRuleProvider(
    "MyApp.Resources.holidays-org.xml",
    new ResourcePathResolver());

var service = new NotableDateService(
    ruleProviders:     [baseProvider],
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    overrideProviders: [orgOverrides]);
```

## Pattern 7 — cache invalidation

The service caches resolved dates per year. Call `Invalidate` when override providers change:

```csharp
// Clear the cached result for 2025 only.
service.Invalidate(2025);

// Clear the entire cache.
service.Invalidate();
```

## Understanding the resolution pipeline

```
INotableDateRuleProvider(s)         → base rules
  + INotableDateRuleOverrideProvider(s) → merged effective rules
      → NotableDateRuleResolver.ResolveAnchorDate(rule, year)
          → NotableDateAdjuster (weekday adjustments)
              → NotableDate(s) for the year  [cached]
```

1. **Rule loading** — base providers supply the initial rule list; override providers add, remove, or patch rules.
2. **Anchor resolution** — `NotableDateRuleResolver` dispatches each rule's `DateResolutionStrategy` to the correct logic (fixed date, *n*th weekday, offset, or algorithm).
3. **Adjustment** — `NotableDateAdjuster` shifts the anchor when it falls on a weekend, applying the `ObservanceAdjustment` policy on the rule.
4. **Caching** — the resolved list for a year is stored in a thread-safe `ConcurrentDictionary` and reused on subsequent calls.

## Where to go next

- [Date calculation algorithms](algorithms.md) — the built-in algorithm types and how to implement a custom one.
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full type reference.
