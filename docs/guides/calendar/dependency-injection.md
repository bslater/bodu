---
title: Calendar dependency injection
---

# Calendar dependency injection

The optional `Bodu.Globalization.Calendar.DependencyInjection` companion package wires <xref:Bodu.Globalization.Calendar.INotableDateService> into a `Microsoft.Extensions.DependencyInjection` container, so ASP.NET Core, generic-host, or any `IServiceCollection`-based application can resolve the calendar service like any other framework-registered dependency.

The package is intentionally thin. A resource is an immutable, already-validated value, so registration takes the resource (or a factory for it) directly — there is no options object and no fluent builder. The service's behaviour is carried by the resource's `<ResolutionPolicy>` and by the optional collaborators supplied when the resource or service is built (see [Building and extending the service](building-the-service.md)). If you are constructing the service by hand — in a console app or a test — keep using `new NotableDateService(...)`; this page is only relevant when you want the host to compose the service for you.

## Install

```bash
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
```

The package depends on `Bodu.Globalization.Calendar` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

## The registration surface

Every extension method lives on <xref:Bodu.Globalization.Calendar.NotableDateServiceCollectionExtensions>, in the `Bodu.Globalization.Calendar` namespace, so add `using Bodu.Globalization.Calendar;` to bring them into scope on `IServiceCollection`.

| Method | Registers |
|---|---|
| `AddNotableDateService(IServiceCollection, NotableDateResource)` | A singleton `INotableDateService` over an already-loaded resource. |
| `AddNotableDateService(IServiceCollection, Func<IServiceProvider, NotableDateResource>)` | The same, but the resource is produced from the container — e.g. loaded from configuration or a data pack resolved through DI. |
| `AddReloadableNotableDateService(IServiceCollection, NotableDateResource)` | A singleton `INotableDateService` (a `ReloadableNotableDateService`) **and** a singleton `MutableNotableDateResourceProvider` you inject to call `Reload(...)`. |

`INotableDateService` is always registered as a singleton. The fixed registration (`AddNotableDateService`) offers both a resource and a `Func<IServiceProvider, NotableDateResource>` factory overload; the reloadable registration takes the initial resource only — load it eagerly (typically from a data pack) and swap it later through the provider.

## Register a resource

Pass a loaded resource — typically from a companion data pack, or from `NotableDateResourceLoader.Load(...)` for your own document:

<!-- compile -->
```csharp
IServiceCollection services = new ServiceCollection();   // or builder.Services in ASP.NET Core

services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));
```

## Register via a factory

The factory overload defers loading until the container builds the service, so the resource can depend on other registered services — for example reading the territory from `IConfiguration`:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService(sp =>
{
    string territory = sp.GetRequiredService<IConfiguration>()["Calendar:Territory"] ?? "GB";
    return EuropeCalendarData.LoadResource(territory);
});
```

## Inject the service

Once registered, inject `INotableDateService` like any singleton. By-year resolution is the `NotableDateServiceExtensions.Resolve` extension; the single-day and range overloads are on the interface itself:

```csharp
using Bodu.Globalization.Calendar;

public sealed class HolidayController
{
    private readonly INotableDateService _calendar;

    public HolidayController(INotableDateService calendar) =>
        _calendar = calendar;

    public IReadOnlyList<NotableDate> Year(int year) =>
        _calendar.Resolve(year, "AU-NSW");

    public IReadOnlyList<NotableDate> OnDay(DateOnly date) =>
        _calendar.Resolve(date, "AU-NSW");
}
```

## Swap the rule set at runtime

When the rule set must change while the host is running, register the reloadable service instead. `AddReloadableNotableDateService` registers the singleton service over a singleton <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider>; inject the provider and call `Reload(...)` to swap the resource. The live service reflects the new data on its next query:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddReloadableNotableDateService(EuropeCalendarData.LoadResource("GB"));

// elsewhere, a component that refreshes the rules injects the provider:
public sealed class CalendarReloader
{
    private readonly MutableNotableDateResourceProvider _provider;

    public CalendarReloader(MutableNotableDateResourceProvider provider) =>
        _provider = provider;

    public void Apply(string updatedXml) =>
        _provider.Reload(NotableDateResourceLoader.Load(updatedXml, CommonNotableDateResources.Resolver));
}
```

The provider is also registered as <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider>, so a component that only needs to read the current resource can inject the interface instead of the concrete provider.

## Registering a service with custom collaborators

`AddNotableDateService` builds a plain `NotableDateService` over the resource — it does not take an algorithm registry, collision resolver, or adjustment handlers. When the service needs those collaborators (see [Building and extending the service](building-the-service.md)), construct the service yourself in the factory overload and register the instance as the interface:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddSingleton<INotableDateService>(sp =>
{
    var registry = new NotableDateAlgorithmRegistry()
        .Register("pi-day", new PiDayAlgorithm());

    NotableDateResource resource = NotableDateResourceLoader.Load(
        sp.GetRequiredService<IConfiguration>()["Calendar:Document"]!,
        CommonNotableDateResources.Resolver,
        registry);

    return new NotableDateService(resource, new NotableDateServiceOptions { Algorithms = registry });
});
```

This is the standard escape hatch: anything `AddNotableDateService` cannot express is just a hand-built `NotableDateService` registered as a singleton `INotableDateService`.

## Service lifetime

- `INotableDateService` is registered as a **singleton**, so it is shared across the application and is safe to resolve from any scope.
- For the reloadable registration, the `MutableNotableDateResourceProvider` is a singleton too; a `Reload(...)` on it is observed by every consumer of the singleton service, atomically, on the next query.

## Where to go next

- **[Building and extending the service](building-the-service.md)** — the collaborators (`NotableDateAlgorithmRegistry`, collision resolver, adjustment handlers, code-first providers) you can compose into the resource/service before registering it.
- **[Calendar data packs](data-packs.md)** — composing an Americas / Asia-Pacific / Europe pack resource through `AddNotableDateService`.
- **[Using NotableDateService](notable-dates.md)** — query patterns and working-day arithmetic.
- **[Bodu.Globalization.Calendar.DependencyInjection API reference](xref:Bodu.Globalization.Calendar.NotableDateServiceCollectionExtensions)** — the registration surface.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
