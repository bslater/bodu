---
title: Bodu.Globalization.Calendar — Getting started
---

# Bodu.Globalization.Calendar — Getting started

Unfamiliar with terms like *rule*, *nominal date*, *anchor*, or *territory*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Globalization.Calendar

# Optional region-specific data packs (rules ship out-of-band on independent schedules):
dotnet add package Bodu.Globalization.Calendar.Data.Americas
dotnet add package Bodu.Globalization.Calendar.Data.Europe
dotnet add package Bodu.Globalization.Calendar.Data.AsiaPacific

# Optional Microsoft.Extensions.DependencyInjection integration:
dotnet add package Bodu.Globalization.Calendar.DependencyInjection

# Optional fluent rule-authoring API:
dotnet add package Bodu.Globalization.Calendar.Builder
```

See the [package matrix](../package-matrix.md) for the full taxonomy and the [Calendar package family diagram](index.md#calendar-package-family) for how the runtime and companions compose.

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

Other built-in algorithms: `HinduLunarNotableDateAlgorithm`, `VesakNotableDateAlgorithm`, `AsalhaPujaNotableDateAlgorithm`, `QingmingNotableDateAlgorithm`.

### Resolve all notable dates for a year and territory

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.AsiaPacific;

INotableDateRuleProvider auRules = AsiaPacificCalendarData.CreateAustraliaProvider();

INotableDateService service = new NotableDateService(
    ruleProviders:    [ auRules ],
    workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

IReadOnlyList<NotableDate> nsw2026 =
    service.GetNotableDates(year: 2026, territoryCode: "AU-NSW");

foreach (NotableDate date in nsw2026.Where(d => d.Category == NotableDateCategory.Holiday))
{
    Console.WriteLine($"{date.Date:yyyy-MM-dd}  {date.Name}");
}
```

The parameterless `new NotableDateService()` constructor loads only the embedded `default-minimal.xml` rule set (currently New Year's Day). For region-specific holidays, pass one of the `AmericasCalendarData` / `EuropeCalendarData` / `AsiaPacificCalendarData` providers from the companion data packs.

### Working-day arithmetic over a `DateOnly`

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Extensions;                       // NotableDateOnlyExtensions

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

bool       isHoliday = today.IsNotableDate(service, "AU-NSW");
DateOnly   nextOpen  = today.NextWorkingDay(service, "AU-NSW");
DateOnly   inFive    = today.AddWorkingDays(service, 5, "AU-NSW");
int        between   = today.WorkingDaysBetween(inFive, service, "AU-NSW");
```

The same operations exist over `DateTime` via `NotableDateTimeExtensions` (also in `Bodu.Extensions`).

### Filter by category and date range

```csharp
using Bodu.Globalization.Calendar;

NotableDateFilter filter = NotableDateFilter
    .ForCategory(NotableDateCategory.Holiday)
    .Or(NotableDateFilter.ForCategory(NotableDateCategory.Cultural))
    .And(NotableDateFilter.InDateRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));

IReadOnlyList<NotableDate> dates = service.GetNotableDates(
    startDate:    new DateTime(2026, 1, 1),
    endDate:      new DateTime(2026, 12, 31),
    filter:       filter,
    territoryCode: "GB-ENG");
```

`NotableDateFilter` is built via static factory methods (`ForCategory`, `WithTag`, `WithName`, `InDateRange`, `IsNonWorkingDay`, `WasAdjusted`, …) and combined with `And`, `Or`, `AllOf`, `AnyOf`. Predicates that depend only on the rule (`ForCategory`, `WithTag`, `WithName`, `IsNonWorkingDay`) are evaluated as a primary gate *before* the date is resolved.

### Layer runtime overrides

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.AsiaPacific;

INotableDateRuleProvider          auRules   = AsiaPacificCalendarData.CreateAustraliaProvider();
INotableDateRuleOverrideProvider  overrides = new MyCorporateOverrideProvider(); // implements INotableDateRuleOverrideProvider

INotableDateService service = new NotableDateService(
    ruleProviders:     [ auRules ],
    workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
    options: new NotableDateServiceOptions { OverrideProviders = [ overrides ] });
```

Override providers can add new rules (via `INotableDateRuleProvider.LoadRules`), remove a base rule by name and territory (via `GetRemovals` returning `RuleRemoval` values), or layer adjustment overrides. Implement <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider> directly, store overrides in your own XML / JSON file and load them through <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider> / <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider>, or use the runtime-mutable <xref:Bodu.Globalization.Calendar.MutableNotableDateRuleOverrideProvider> when rules need to be added or removed while the service is alive — paired with `service.Reload()` to take effect.

### Register through dependency injection

When the host is an ASP.NET Core application (or any `IServiceCollection`-based composition root), install the optional `Bodu.Globalization.Calendar.DependencyInjection` package and register the service idiomatically:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.AsiaPacific;
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

builder.Services
    .AddNotableDates(builder.Configuration)                       // binds "NotableDates" section
    .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
    .AddOverrideProvider(new MutableNotableDateRuleOverrideProvider())
    .RegisterAsAmbientDefault();                                  // also assigns NotableDateContext.Default
```

`appsettings.json`:

```json
{
  "NotableDates": {
    "WorkingDays": "MondayToFriday",
    "DefaultTerritoryCode": "AU-NSW",
    "RegisterAsAmbientDefault": true
  }
}
```

See the [Calendar dependency injection guide](../../guides/calendar/dependency-injection.md) for the full builder reference, the `PostConfigure` projection hook for projecting from consumer-defined POCOs, and the runtime-mutable override workflow.

## Where to go next

- **[Bodu.Globalization.Calendar introduction](index.md)** — mental model, headline types, scenarios.
- **[Core concepts](concepts.md)** — vocabulary used across the rest of the documentation.
- **[Bodu.Globalization.Calendar guides](../../guides/calendar/index.md)** — `NotableDateService` patterns, algorithms, rule authoring, working-day arithmetic, territories, data packs.
- **[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)** — full type-by-type docs.
- **[Calendar data packs guide](../../guides/calendar/data-packs.md)** — composing `AmericasCalendarData` / `EuropeCalendarData` / `AsiaPacificCalendarData` providers.
