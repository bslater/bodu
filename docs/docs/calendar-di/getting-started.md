---
title: Bodu.Globalization.Calendar.DependencyInjection — Getting started
---

# Bodu.Globalization.Calendar.DependencyInjection — Getting started

Unfamiliar with terms like *resource*, *territory*, or *collaborators*? Read the runtime's
[core concepts](../calendar/concepts.md) first; the [introduction](index.md) explains the registration surface
this page exercises.

## Install

```bash
dotnet add package Bodu.Globalization.Calendar.DependencyInjection

# One or more regional data packs supply the resources the samples register:
dotnet add package Bodu.Globalization.Calendar.Americas
dotnet add package Bodu.Globalization.Calendar.AsiaPacific

# Optional read-through cache in front of the registered service:
dotnet add package Bodu.Globalization.Calendar.Caching
```

Targets `net8.0`. Depends on `Bodu.Globalization.Calendar` (the runtime) and, at the .NET 8.0 LTS line,
`Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, and
`Microsoft.Extensions.Options` (for the options-monitor overload). It brings no container of its own — any
`IServiceCollection` host works, and the samples use the ASP.NET Core `WebApplication` builder.

Every extension method is in the `Bodu.Globalization.Calendar` namespace; there is no
`Bodu.Globalization.Calendar.DependencyInjection` namespace to import.

## Minimal samples

### Register and inject a service

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
```

`INotableDateService` is now a singleton; inject it anywhere. By-year resolution is the
<xref:Bodu.Globalization.Calendar.NotableDateServiceExtensions> extension; the single-day and range overloads are on
the interface itself:

```csharp
using Bodu.Globalization.Calendar;

public sealed class HolidayService
{
    private readonly INotableDateService _calendar;

    public HolidayService(INotableDateService calendar) =>
        _calendar = calendar;

    public IReadOnlyList<NotableDate> Year(int year) =>
        _calendar.Resolve(year, "AU-NSW");

    public bool IsClosure(DateOnly date) =>
        _calendar.Resolve(date, "AU-NSW", NotableDateFilter.IsNonWorkingDay()).Count > 0;
}
```

Use the factory overload when the resource depends on other registered services — for example, the territory comes
from configuration:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService(sp =>
{
    string territory = sp.GetRequiredService<IConfiguration>()["Calendar:Territory"] ?? "AU";
    return AsiaPacificCalendarData.LoadResource(territory);
});
```

```json
{
  "Calendar": {
    "Territory": "AU"
  }
}
```

The factory runs when the service is first resolved, not at registration.

### Register one service per jurisdiction (keyed)

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService("US", AmericasCalendarData.LoadResource("US"));
builder.Services.AddNotableDateService("AU", AsiaPacificCalendarData.LoadResource("AU"));
```

Resolve by key through the standard .NET 8 keyed-service surface — a `[FromKeyedServices]` constructor parameter or
`GetRequiredKeyedService`:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

public sealed class PayrollCalendar
{
    private readonly INotableDateService _calendar;

    public PayrollCalendar([FromKeyedServices("AU")] INotableDateService calendar) =>
        _calendar = calendar;

    public bool IsBankHoliday(DateOnly date) =>
        _calendar.Resolve(date, "AU", NotableDateFilter.ForCategory(NotableDateCategory.BankHoliday)).Count > 0;
}

// Or, from a resolver:
INotableDateService us = app.Services.GetRequiredKeyedService<INotableDateService>("US");
```

Keyed registrations are independent of the unkeyed one — register both when most consumers want a default calendar
and a few want a specific jurisdiction. The keyed overloads accept a `NotableDateServiceOptions` (or an options
factory) for collaborators, exactly like the unkeyed forms.

### Swap the rule set at run time (reloadable)

`AddReloadableNotableDateService` registers the singleton service over a singleton
<xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider>. Inject the provider to reload:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddReloadableNotableDateService(EuropeCalendarData.LoadResource("GB"));
```

```csharp
using Bodu.Globalization.Calendar;

public sealed class CalendarReloader
{
    private readonly MutableNotableDateResourceProvider _provider;

    public CalendarReloader(MutableNotableDateResourceProvider provider) =>
        _provider = provider;

    public void Apply(string updatedXml) =>
        _provider.Reload(NotableDateResourceLoader.Load(updatedXml, CommonNotableDateResources.Resolver));
}
```

Every consumer of the singleton `INotableDateService` sees the new resource on its next query. A component that only
needs to *read* the current resource can inject the
<xref:Bodu.Globalization.Calendar.INotableDateResourceProvider> interface instead of the concrete provider.

#### Rebuild from configuration automatically

When the rule set is a function of configuration, bind an options class and use the `IOptionsMonitor<TOptions>`
overload. Every change the options infrastructure observes — an edited `appsettings.json` with `reloadOnChange`,
for example — reruns the factory and swaps the result into the live service:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

public sealed class CalendarOptions
{
    public string Territory { get; set; } = "AU";
}

builder.Services.AddOptions<CalendarOptions>().Bind(builder.Configuration.GetSection("Calendar"));
builder.Services.AddReloadableNotableDateService<CalendarOptions>((sp, options) =>
    AsiaPacificCalendarData.LoadResource(options.Territory));
```

```json
{
  "Calendar": {
    "Territory": "AU-NSW"
  }
}
```

A factory that throws during a change is logged and leaves the previously loaded resource in effect, so a broken
configuration edit never takes the calendar offline. (The `Calendar` section can hold other children — such as the
caching package's `NotableDateCache` — without affecting the binding; the binder maps only the properties
`CalendarOptions` declares.)

### Compose with custom collaborators

Pass a <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions> to any overload that accepts one — here a custom
algorithm registry shared by the loader and the service, both produced from the container:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var registry = new NotableDateAlgorithmRegistry()
    .Register("company-founders-day", new FoundersDayAlgorithm());

builder.Services.AddNotableDateService(
    sp => NotableDateResourceLoader.Load(
        sp.GetRequiredService<IConfiguration>()["Calendar:Document"]!,
        CommonNotableDateResources.Resolver,
        registry),
    _ => new NotableDateServiceOptions { Algorithms = registry });
```

The founders-day algorithm stands for your own <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm>
implementation; see [Building and extending the service](../../guides/calendar/building-the-service.md).

## Adding the caching decorator

The [`Bodu.Globalization.Calendar.Caching`](../calendar-caching/index.md) package layers a read-through cache over
whichever registration you chose. `AddCachedNotableDateService` removes the registered `INotableDateService`
descriptor, keeps it as the inner service, and registers the
<xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService> decorator in its place — so it must come
**after** the registration it wraps, and consumers keep injecting `INotableDateService`:

```csharp
using Bodu.Globalization.Calendar;

builder.Services.AddReloadableNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
builder.Services.AddCachedNotableDateService(builder.Configuration);   // binds Calendar:NotableDateCache
```

```json
{
  "Calendar": {
    "NotableDateCache": {
      "Ttl": "7.00:00:00",
      "CacheDirectory": "/var/cache/notable-dates"
    }
  }
}
```

With the reloadable registration, the decorator observes the container's `INotableDateResourceProvider` and derives
its resource-version token from it, so a `Reload(...)` invalidates every cached year on the next query with no extra
wiring. With a plain `AddNotableDateService`, set `ResourceVersion` in the section and bump it after a data update.

The decorator wraps only the **unkeyed** `INotableDateService`; keyed registrations are left untouched. To cache a
keyed service, construct the decorator in that key's factory instead:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

builder.Services.AddKeyedSingleton<INotableDateService>("AU", (sp, _) =>
    new CachingNotableDateService(
        AsiaPacificCalendarData.CreateService("AU"),
        new InMemoryNotableDateCache(),
        new NotableDateCachingOptions { ResourceVersion = "au-2026.1" },
        loggerFactory: sp.GetService<ILoggerFactory>()));
```

The caching package's [getting-started page](../calendar-caching/getting-started.md) covers the SQLite and Redis
backends and the startup warm-up.

## Where to go next

- **[Introduction](index.md)** — the full overload table, lifetimes, and idempotency.
- **[Calendar dependency injection guide](../../guides/calendar/dependency-injection.md)** — the complete walkthrough with collaborators and the reloadable workflow.
- **[Bodu.Globalization.Calendar.Caching](../calendar-caching/index.md)** — the caching decorator, its backends, and its registrations.
- **[Calendar data packs](../../guides/calendar/data-packs.md)** — the `<Region>CalendarData` factories the samples load resources from.
- **[API reference](xref:Bodu.Globalization.Calendar.NotableDateServiceCollectionExtensions)** — the registration surface, member by member.
- **[Runnable samples](../../samples/calendar.md)** — offline calendar sample projects you can `dotnet run`.
