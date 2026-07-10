---
title: Bodu.Globalization.Calendar — Getting started
---

# Bodu.Globalization.Calendar — Getting started

Unfamiliar with terms like *document*, *resource*, *rule*, *nominal date*, or *territory*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Globalization.Calendar

# Optional region-specific data packs (rules ship out-of-band on independent schedules):
dotnet add package Bodu.Globalization.Calendar.Americas
dotnet add package Bodu.Globalization.Calendar.Europe
dotnet add package Bodu.Globalization.Calendar.AsiaPacific

# Optional Microsoft.Extensions.DependencyInjection integration:
dotnet add package Bodu.Globalization.Calendar.DependencyInjection

# Optional trust-gated external algorithm plugins:
dotnet add package Bodu.Globalization.Calendar.Plugins

# Optional fluent C# document-authoring API:
dotnet add package Bodu.Globalization.Calendar.Builder
```

See the [package matrix](../package-matrix.md) for the full taxonomy and the [Calendar package family diagram](index.md#calendar-package-family) for how the runtime and companions compose.

Targets `net8.0`. The base package contains the resolution engine, the built-in algorithms, and a set of bundled common catalogues; the data packs contain region-specific rule sets.

## Minimal samples

### Load a document and resolve

A rule document is XML (or JSON) on the notable-date schema. Load it into an immutable resource, build a service, and resolve:

```csharp
using Bodu.Globalization.Calendar;

const string xml = """
<NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="demo">
  <NotableDates>
    <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
      <Rules>
        <Rule id="default"><Strategy><Fixed month="January" day="1" /></Strategy></Rule>
      </Rules>
    </NotableDate>
  </NotableDates>
</NotableDateResource>
""";

NotableDateResource resource = NotableDateResourceLoader.Load(xml);   // parsed + validated; throws NotableDateValidationException on error
NotableDateService  service  = new NotableDateService(resource);

IReadOnlyList<NotableDate> jan = service.Resolve(new DateOnly(2026, 1, 1), "US");
Console.WriteLine(jan[0].DisplayName);                                // New Year's Day
```

A document that uses `<Imports>` must be loaded with a resolver so import names can be fetched — pass `CommonNotableDateResources.Resolver` to pull from the bundled catalogues:

```csharp
NotableDateResource resource =
    NotableDateResourceLoader.Load(xml, CommonNotableDateResources.Resolver);
```

### Resolve all notable dates for a year and territory

The companion data packs do the load-and-import wiring for you:

```csharp
using Bodu.Globalization.Calendar;

NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

// By-year resolution is an extension method (NotableDateServiceExtensions):
IReadOnlyList<NotableDate> nsw2026 = service.Resolve(2026, "AU-NSW");

foreach (NotableDate d in nsw2026.Where(x => x.Category == NotableDateCategory.PublicHoliday))
    Console.WriteLine($"{d.Date:yyyy-MM-dd}  {d.DisplayName}");
```

Resolve a single day or an arbitrary range with the instance methods:

```csharp
IReadOnlyList<NotableDate> onDay = service.Resolve(new DateOnly(2026, 1, 26), "AU-NSW");
IReadOnlyList<NotableDate> q1    = service.Resolve(
    new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), "AU-NSW");
```

### Filter by category and date range

```csharp
using Bodu.Globalization.Calendar;

NotableDateFilter filter = NotableDateFilter
    .ForAnyCategory(NotableDateCategory.PublicHoliday, NotableDateCategory.Cultural)
    .And(NotableDateFilter.InDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));

IReadOnlyList<NotableDate> firstHalf = service.Resolve(2026, "AU-NSW", filter);
```

`NotableDateFilter` is built via static factory methods (`ForCategory`, `ForAnyCategory`, `WithName`, `WithId`, `WithTag`, `WithMinDuration`, `IsNonWorkingDay`, `WasAdjusted`, `InDateRange`, …) and combined with `And`, `Or`, `Not`, `AllOf`, `AnyOf`.

### Working-day arithmetic over a `DateOnly`

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Extensions;                       // NotableDateOnlyExtensions — not auto-imported

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

bool     isHoliday = today.IsNotableDate(service, "AU-NSW");
bool     isOpen    = today.IsWorkingDay(service, "AU-NSW");
DateOnly nextOpen  = today.NextWorkingDay(service, "AU-NSW");
DateOnly inFive    = today.AddWorkingDays(5, service, "AU-NSW");
int      between   = today.WorkingDaysBetween(inFive, service, "AU-NSW");
```

The same operations exist over `DateTime` and `DateTimeOffset` (`NotableDateTimeExtensions`, `NotableDateTimeOffsetExtensions`, also in `Bodu.Extensions`). Every method accepts an optional `Bodu.Core` `WeekPattern` to override the default Monday–Friday working week — e.g. `today.NextWorkingDay(service, "AE", WeekPattern.SundayToThursday)`.

### Register a custom algorithm

When a date is computed rather than fixed, implement <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> and reference it from the rule by key:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

public sealed class PiDayAlgorithm : INotableDateAlgorithm
{
    public DateOnly? Calculate(int year) => new DateOnly(year, 3, 14);
}

var registry = new NotableDateAlgorithmRegistry().Register("pi-day", new PiDayAlgorithm());
NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null, registry);  // xml has <Algorithm key="pi-day" />
NotableDateService  service  = new NotableDateService(
    resource, new NotableDateServiceOptions { Algorithms = registry });
```

Custom collaborators — algorithm registry, collision resolver, adjustment / trigger handlers, and code-first providers — are supplied through <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions> (an object with `init`-only properties); there is no positional-collaborator constructor. The single-argument `new NotableDateService(resource)` covers the built-in path.

Built-in keys (`western-easter`, `orthodox-easter`, `qingming`, `vesak`, `losar`, `matariki`, the Hindu-festival keys, …) need no registration. See [Date calculation algorithms](../../guides/calendar/algorithms.md).

### Swap the rule set at runtime

A resource is immutable, so runtime change means loading a new resource and swapping it in:

```csharp
using Bodu.Globalization.Calendar;

var provider = new MutableNotableDateResourceProvider(NotableDateResourceLoader.Load(initialXml));
INotableDateService service = new ReloadableNotableDateService(provider);

// later, when the rules change:
provider.Reload(NotableDateResourceLoader.Load(updatedXml));   // the live service picks it up
```

### Register through dependency injection

When the host is an ASP.NET Core application (or any `IServiceCollection`-based composition root), install `Bodu.Globalization.Calendar.DependencyInjection` and register the service:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
// or a factory: builder.Services.AddNotableDateService(sp => AsiaPacificCalendarData.LoadResource("AU"));
// or reloadable:  builder.Services.AddReloadableNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
```

`INotableDateService` is registered as a singleton. See the [Calendar dependency injection guide](../../guides/calendar/dependency-injection.md) for the reloadable workflow and lifetime semantics.

## Where to go next

- **[Bodu.Globalization.Calendar introduction](index.md)** — mental model, headline types, scenarios.
- **[Core concepts](concepts.md)** — vocabulary used across the rest of the documentation.
- **[Bodu.Globalization.Calendar guides](../../guides/calendar/index.md)** — `NotableDateService` patterns, algorithms, rule authoring, working-day arithmetic, territories, data packs.
- **[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)** — full type-by-type docs.
- **[Calendar data packs guide](../../guides/calendar/data-packs.md)** — composing `AmericasCalendarData` / `EuropeCalendarData` / `AsiaPacificCalendarData` resources.
- **[Runnable samples](../../guides/calendar/samples.md)** — offline sample projects under `samples/Globalization.Calendar/` you can `dotnet run` and copy from.
