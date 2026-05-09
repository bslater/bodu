---
title: Bodu.Globalization.Calendar — Getting started
---

# Bodu.Globalization.Calendar — Getting started

## Install

```bash
dotnet add package Bodu.Globalization.Calendar

# Optional region-specific data packs (rules ship out-of-band on independent schedules):
dotnet add package Bodu.Globalization.Calendar.Data.Americas
dotnet add package Bodu.Globalization.Calendar.Data.Europe
dotnet add package Bodu.Globalization.Calendar.Data.AsiaPacific
```

Targets `net8.0`. The base package contains the resolution engine and the built-in algorithms; the data packs contain region-specific rule sets.

## Minimal samples

### Resolve Easter Sunday

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var algorithm = new EasterSundayNotableDateAlgorithm();
DateTime easter2026 = algorithm.Calculate(2026);
// 2026-04-05

DateTime goodFriday2026 = easter2026.AddDays(-2);
```

The Gregorian Computus is used for years from 1583 onward; earlier years fall back to the Julian algorithm.

### Resolve Lunar New Year (Losar)

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var algorithm = new LosarNotableDateAlgorithm();
DateTime losar2026 = algorithm.Calculate(2026);
```

Other built-in algorithms: `HinduLunarNotableDateAlgorithm`, `VesakNotableDateAlgorithm`, `AsalhaPujaNotableDateAlgorithm`, `QingmingNotableDateAlgorithm`, `LunarPhaseAlgorithm`.

### Resolve all notable dates for a year and territory

```csharp
using Bodu.Globalization.Calendar;

INotableDateService service = NotableDateService.CreateDefault();

IReadOnlyList<NotableDate> nsw2026 =
    service.GetNotableDates(year: 2026, territory: "AU-NSW");

foreach (NotableDate date in nsw2026.Where(d => d.Category == NotableDateCategory.Holiday))
{
    Console.WriteLine($"{date.Date:yyyy-MM-dd}  {date.Name}");
}
```

`CreateDefault()` wires up the built-in providers and algorithm registry. Pass a custom rule provider, override provider, or algorithm registry for non-default behaviour.

### Working-day arithmetic over a `DateOnly`

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Extensions;

INotableDateService service = NotableDateService.CreateDefault();

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

bool       isHoliday = today.IsNotableDate(service, "AU-NSW");
DateOnly   nextOpen  = today.NextWorkingDay(service, "AU-NSW");
DateOnly   inFive    = today.AddWorkingDays(service, 5, "AU-NSW");
int        between   = today.WorkingDaysBetween(inFive, service, "AU-NSW");
```

The same operations exist over `DateTime` via `NotableDateTimeExtensions`.

### Filter by category and date range

```csharp
using Bodu.Globalization.Calendar;

var filter = new NotableDateFilter
{
    Categories = NotableDateCategory.Holiday | NotableDateCategory.Cultural,
    Territory  = "GB-ENG",
};

IReadOnlyList<NotableDate> dates = service.GetNotableDates(
    from: new DateOnly(2026, 1, 1),
    to:   new DateOnly(2026, 12, 31),
    filter: filter);
```

### Layer runtime overrides

```csharp
using Bodu.Globalization.Calendar;

var overrides = new InMemoryRuleOverrideProvider();
overrides.AddOverride(new NotableDateRule(...));            // add a corporate-specific date
overrides.RemoveRule("HolidayName", territory: "AU-NSW");   // suppress a default rule

INotableDateService service = NotableDateService.Create(
    ruleProviders: new[] { defaultProvider },
    overrideProviders: new[] { overrides });
```

## Where to go next

- **[Bodu.Globalization.Calendar introduction](index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Globalization.Calendar guides](../../guides/calendar/)** — `NotableDateService` patterns, algorithms, rule authoring, data packs.
- **[Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md)** — full type-by-type docs.
