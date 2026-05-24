---
title: Calendar dependency injection
---

# Calendar dependency injection

The optional `Bodu.Globalization.Calendar.DependencyInjection` companion package wires `INotableDateService` into a `Microsoft.Extensions.DependencyInjection` container so ASP.NET Core, generic-host, or any `IServiceCollection`-based application can resolve the service like any other framework-registered dependency.

If you are constructing the service by hand — for example in a console app or a test — keep using `new NotableDateService(...)`; this page is only relevant when you want the host to compose the service for you.

---

## Install

```bash
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
```

The package depends on:

- `Bodu.Globalization.Calendar`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Options` (+ `Options.ConfigurationExtensions` for binding)

## AddNotableDates — two entry shapes

Both overloads return an [`INotableDateServiceBuilder`](xref:Bodu.Globalization.Calendar.DependencyInjection.INotableDateServiceBuilder) for further chaining.

### Configuration-driven

```csharp
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

builder.Services
    .AddNotableDates(builder.Configuration)           // binds "NotableDates" section
    .AddRuleProviders(AsiaPacificCalendarData.CreateProviders());
```

`AddNotableDates(IConfiguration?, string sectionName = "NotableDates")` binds the named section into [`NotableDateOptions`](xref:Bodu.Globalization.Calendar.DependencyInjection.NotableDateOptions). Pass `configuration: null` if you do not want any configuration binding.

### Builder-callback driven

```csharp
builder.Services.AddNotableDates(notable =>
{
    notable
        .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
        .UseWorkingDays(WorkingDaysOfWeek.MondayToFriday)
        .RegisterAsAmbientDefault();
});
```

The callback receives the same `INotableDateServiceBuilder` instance the caller would otherwise receive as a return value, so both shapes are equivalent.

---

## NotableDateOptions

[`NotableDateOptions`](xref:Bodu.Globalization.Calendar.DependencyInjection.NotableDateOptions) is the bindable POCO. It is intentionally composed of bindable primitives so the entire surface can be populated from `appsettings.json`. Interface-typed collaborators (algorithm registries, collision resolvers, plugins) are configured through the builder instead.

| Property | Default | Purpose |
|---|---|---|
| `WorkingDays` | `WorkingDaysOfWeek.MondayToFriday` | Named working-week preset converted to a `WeekPattern` before the service is constructed. |
| `DefaultTerritoryCode` | `null` | Optional default territory code exposed to consumers via `IOptions<NotableDateOptions>`. Not pushed into the service itself — pass it explicitly to per-call queries. |
| `DefaultCalendarTypeName` | `null` | Assembly-qualified name of the default calendar type. Stored as a string for bindability; resolve with `Type.GetType(name)`. |
| `RegisterAsAmbientDefault` | `false` | When `true`, the resolved singleton is also assigned to <xref:Bodu.Globalization.Calendar.NotableDateContext.Default> the first time it is resolved. |

```json
{
  "NotableDates": {
    "WorkingDays": "MondayToSaturday",
    "DefaultTerritoryCode": "AU-NSW",
    "RegisterAsAmbientDefault": true
  }
}
```

---

## Builder reference

Every fluent extension method lives on [`NotableDateServiceBuilderExtensions`](xref:Bodu.Globalization.Calendar.DependencyInjection.NotableDateServiceBuilderExtensions). The method shapes mirror conventional `Microsoft.Extensions.*` builders — chains return the builder, registrations accumulate via `IEnumerable<T>` injection, and overlapping options follow the standard `IConfiguration` → `IConfigureOptions` → `IPostConfigureOptions` ordering.

### Rule and override providers

```csharp
notable
    .AddRuleProvider(new XmlResourceNotableDateRuleProvider(
        "MyApp/Calendar/Resources/holidays.xml",
        new ResourcePathResolver()))
    .AddRuleProvider<MyCustomProvider>()             // typed registration, container-instantiated
    .AddRuleProvider(sp => sp.GetRequiredService<DatabaseRuleProvider>())
    .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
    .AddOverrideProvider(new MutableNotableDateRuleOverrideProvider());
```

Multiple calls accumulate — a host can compose contributions from several data packs without overwriting earlier registrations.

### Single-instance collaborators

```csharp
notable
    .UseAlgorithmRegistry(new NotableDateAlgorithmRegistry()
        .Register("easter-sunday", new EasterSundayNotableDateAlgorithm()))
    .UseCollisionResolver(new MyCollisionResolver())
    .UseNameLocalizer(new MyResxNameLocalizer())
    .UseAdjustmentHandlers(new AdjustmentHandlerRegistry())
    .UseResourcePathResolver(new ResourcePathResolver());
```

Later calls replace earlier registrations, so you can override a default supplied elsewhere in your composition root.

### Options shaping

```csharp
notable
    .UseWorkingDays(WorkingDaysOfWeek.MondayToFriday)   // sugar over Configure
    .Configure(opts => opts.DefaultTerritoryCode = "AU-NSW")
    .RegisterAsAmbientDefault();
```

---

## The PostConfigure hook — projecting from custom POCOs

If your application already has its own bindable settings class — perhaps `MyAppCalendarSettings` bound from `appsettings.json:MyApp:Calendar` — you do not need to abandon it. Register it through the standard `services.Configure<TOptions>(...)` call and project values into `NotableDateOptions` via `PostConfigure`:

```csharp
public sealed class MyAppCalendarSettings
{
    public string?            RegionCode { get; set; }
    public WorkingDaysOfWeek  WorkWeek   { get; set; } = WorkingDaysOfWeek.MondayToFriday;
}

services.Configure<MyAppCalendarSettings>(configuration.GetSection("MyApp:Calendar"));

services.AddNotableDates()
    .PostConfigure((sp, opts) =>
    {
        MyAppCalendarSettings custom = sp.GetRequiredService<IOptions<MyAppCalendarSettings>>().Value;
        opts.DefaultTerritoryCode = custom.RegionCode;
        opts.WorkingDays          = custom.WorkWeek;
    });
```

`PostConfigure` is the consumer-defined-options hook. It is backed by `IPostConfigureOptions<NotableDateOptions>`, runs after `IConfiguration` binding and any `Configure(...)` callbacks, and is given the host's `IServiceProvider` so the callback can resolve arbitrary services. The same pattern is used by EF Core's `AddDbContext((sp, builder) => ...)` and ASP.NET Core's authentication post-configuration.

The effective precedence is:

1. Defaults on `NotableDateOptions`
2. `IConfiguration` binding (when supplied)
3. `Configure(...)` callbacks in registration order
4. `PostConfigure(...)` callbacks last — these always win on overlapping properties

---

## Runtime-mutable overrides

`Bodu.Globalization.Calendar` ships [`MutableNotableDateRuleOverrideProvider`](xref:Bodu.Globalization.Calendar.MutableNotableDateRuleOverrideProvider) for hosts that need to add or remove notable-date rules after the service has been constructed (for example, "company closed for stocktake on Friday" or a CMS-authored one-off observance). When you register it via `AddOverrideProvider`, the DI factory subscribes <xref:Bodu.Globalization.Calendar.INotableDateService.Reload> to the provider's `Changed` event so runtime mutations propagate without further intervention:

```csharp
var overrides = new MutableNotableDateRuleOverrideProvider();

services.AddNotableDates()
    .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
    .AddOverrideProvider(overrides);

// Later, anywhere in the host:
overrides.AddRule(new NotableDateRule
{
    Name            = "Stocktake Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Observance,
    Month           = 6,
    Day             = 30,
    IsNonWorkingDay = true,
    TerritoryCode   = "AU-NSW",
});
// Auto-reload fires; the new rule is visible on the next query.
```

`AddRule` / `RemoveRule` / `Clear` each raise `Changed` exactly once. The provider is thread-safe: concurrent mutations preserve insertion order, and snapshots returned from `GetAdditions` / `GetRemovals` remain stable for the duration of any enumeration.

For details on how `Reload` integrates with the cache, see [Cache invalidation and reload](building-the-service.md#cache-invalidation-and-reload).

---

## Ambient default wiring

When `RegisterAsAmbientDefault` is set (either through configuration binding, the `Configure` callback, or the `RegisterAsAmbientDefault()` sugar), the DI-resolved singleton is also assigned to <xref:Bodu.Globalization.Calendar.NotableDateContext.Default> on first resolution. This lets the parameterless `DateOnly` / `DateTime` extension overloads (`date.NextWorkingDay()`, `date.IsNotableDate()`) resolve through the same instance:

```csharp
services.AddNotableDates(configuration)
    .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
    .RegisterAsAmbientDefault();

// Later, anywhere:
DateTime nextWorking = DateTime.Today.NextWorkingDay(territoryCode: "AU-NSW");
```

Without `RegisterAsAmbientDefault`, the ambient context falls back to its lazy default (an internal service backed by the embedded minimal rule set) — the DI-registered service is still resolvable via `IServiceProvider`, but the extension methods will not see it.

---

## Discovering supported territories and calendars

<xref:Bodu.Globalization.Calendar.INotableDateService.GetSupportedTerritories> and <xref:Bodu.Globalization.Calendar.INotableDateService.GetSupportedCalendars> enumerate what the loaded providers cover. They are useful for driving UI pickers or for sanity-checking deployment:

```csharp
INotableDateService service = host.Services.GetRequiredService<INotableDateService>();

foreach (string code in service.GetSupportedTerritories())
{
    Console.WriteLine($"{code}: {service.GetNotableDates(2026, code).Count} dates in 2026");
}
```

The returned set reflects the *effective* rule set — base rules plus any active override additions. Calls to <xref:Bodu.Globalization.Calendar.INotableDateService.Reload> refresh the set when override providers contribute new territories or calendars.

---

## Service lifetime and re-entrancy

- `INotableDateService` is registered as a singleton via `TryAddSingleton`, so repeated `AddNotableDates(...)` calls do not create duplicate registrations.
- The factory is invoked lazily on first resolution. `RegisterAsAmbientDefault` performs its assignment at that point.
- `Reload()` rebuilds the effective rule set under the service's internal gate; concurrent `GetNotableDates(...)` calls during a reload either see the old state or the new state, never a torn read.

---

## Where to go next

- **[Building and extending the service](building-the-service.md)** — every collaborator interface the DI builder wires up.
- **[Using NotableDateService](notable-dates.md)** — query patterns and working-day arithmetic.
- **[Calendar data packs](data-packs.md)** — composing the Asia-Pacific, Americas, and Europe data packs through `AddRuleProviders`.
