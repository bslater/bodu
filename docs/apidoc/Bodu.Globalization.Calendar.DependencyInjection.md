---
uid: Bodu.Globalization.Calendar.DependencyInjection
---

# Bodu.Globalization.Calendar.DependencyInjection

## Purpose

**Bodu.Globalization.Calendar.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` integration for [`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md). It registers <xref:Bodu.Globalization.Calendar.INotableDateService> as a singleton, binds [`NotableDateOptions`](xref:Bodu.Globalization.Calendar.DependencyInjection.NotableDateOptions) from `IConfiguration`, and exposes a fluent builder for layering rule providers, override providers, plugins, algorithm registries, collision resolvers, and name localizers from across a host's composition root.

Reach for this package when you want ASP.NET Core (or any `Microsoft.Extensions.*`-style host) to construct and inject the calendar service for you rather than composing `new NotableDateService(...)` by hand. The package is optional — direct construction continues to work for consoles, libraries, and tests that prefer not to bring in `IServiceCollection`.

## Static documentation

- **[Calendar dependency injection guide](~/guides/calendar/dependency-injection.md)** — `AddNotableDates`, fluent builder, `IConfiguration` binding, ambient default wiring, and the `PostConfigure` consumer-options projection hook.
- **[Building and extending the service](~/guides/calendar/building-the-service.md)** — explains every collaborator interface that the DI package wires up.

## Key types

**Entry points**

- <xref:Bodu.Globalization.Calendar.DependencyInjection.ServiceCollectionExtensions> — the `AddNotableDates(IServiceCollection, IConfiguration?, string)` and `AddNotableDates(IServiceCollection, Action<INotableDateServiceBuilder>)` extension methods that register the service singleton and return an <xref:Bodu.Globalization.Calendar.DependencyInjection.INotableDateServiceBuilder>.
- <xref:Bodu.Globalization.Calendar.DependencyInjection.INotableDateServiceBuilder> — the fluent registration surface returned by `AddNotableDates`. Exposes the host's <xref:Microsoft.Extensions.DependencyInjection.IServiceCollection> so extension methods can register additional collaborators.

**Bindable options**

- <xref:Bodu.Globalization.Calendar.DependencyInjection.NotableDateOptions> — the POCO bound from an `IConfiguration` section. Composed of bindable primitives (`WorkingDays`, `DefaultTerritoryCode`, `DefaultCalendarTypeName`, `RegisterAsAmbientDefault`) and distinct from <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions>, which carries non-bindable interface-typed dependencies.

**Builder extension methods**

- <xref:Bodu.Globalization.Calendar.DependencyInjection.NotableDateServiceBuilderExtensions> — the fluent surface exposed on <xref:Bodu.Globalization.Calendar.DependencyInjection.INotableDateServiceBuilder>:
  - `AddRuleProvider` / `AddRuleProviders` / `AddPlugin` — register collaborators resolved via `IEnumerable<T>` injection.
  - `AddOverrideProvider` — register an <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider>; when the supplied instance is a <xref:Bodu.Globalization.Calendar.MutableNotableDateRuleOverrideProvider>, the service is auto-wired to call <xref:Bodu.Globalization.Calendar.INotableDateService.Reload> on every change.
  - `UseAlgorithmRegistry` / `UseCollisionResolver` / `UseNameLocalizer` / `UseAdjustmentHandlers` / `UseResourcePathResolver` — register single-instance collaborators.
  - `Configure(Action<NotableDateOptions>)` — overlay programmatic settings onto any value bound from configuration.
  - `PostConfigure(Action<IServiceProvider, NotableDateOptions>)` — the consumer-defined-options hook for projecting from custom POCOs registered through the standard `Configure<TOptions>` API. Backed by `IPostConfigureOptions<NotableDateOptions>`.
  - `UseWorkingDays(WorkingDaysOfWeek)` / `UseWorkingWeek(WeekPattern)` — sugar over `Configure`.
  - `RegisterAsAmbientDefault()` — wire the resolved singleton into <xref:Bodu.Globalization.Calendar.NotableDateContext.Default> on first resolution so that extension-method overloads without an explicit service argument resolve through the DI-registered instance.

## Minimal sample

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.AsiaPacific;
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

builder.Services
    .AddNotableDates(builder.Configuration)
    .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
    .AddOverrideProvider(new MutableNotableDateRuleOverrideProvider())
    .RegisterAsAmbientDefault();
```

With `appsettings.json`:

```json
{
  "NotableDates": {
    "WorkingDays": "MondayToFriday",
    "DefaultTerritoryCode": "AU-NSW",
    "RegisterAsAmbientDefault": true
  }
}
```

The full guide — including the `PostConfigure` consumer-options projection pattern, runtime-mutable overrides, and the resolved `INotableDateService` lifetime semantics — is in [Calendar dependency injection](~/guides/calendar/dependency-injection.md).
