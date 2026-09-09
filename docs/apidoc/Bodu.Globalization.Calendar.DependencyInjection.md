---
uid: Bodu.Globalization.Calendar.NotableDateServiceCollectionExtensions
---

![Bodu.Globalization.Calendar.DependencyInjection](~/images/hero-calendar-di.svg)

# Bodu.Globalization.Calendar.DependencyInjection (package)

## Purpose

The **Bodu.Globalization.Calendar.DependencyInjection** package provides the `Microsoft.Extensions.DependencyInjection` integration for [`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md). It registers <xref:Bodu.Globalization.Calendar.INotableDateService> as a singleton over a loaded <xref:Bodu.Globalization.Calendar.NotableDateResource> (or a factory that produces one), so an ASP.NET Core app — or any `Microsoft.Extensions.*`-style host — can inject the calendar service rather than composing `new NotableDateService(...)` by hand.

The package is intentionally thin: a resource is an immutable, already-validated value, so registration takes the resource (or a factory for it) directly, optionally with a <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions> carrying the service collaborators (algorithm registry, collision resolver, adjustment handlers, code-first providers). There is no fluent builder — resource-level behaviour is carried by the resource's `<ResolutionPolicy>`. Direct construction continues to work for consoles, libraries, and tests that prefer not to bring in `IServiceCollection`.

There is no `Bodu.Globalization.Calendar.DependencyInjection` namespace: the extension methods live in the `Bodu.Globalization.Calendar` namespace, so add `using Bodu.Globalization.Calendar;` to bring them into scope on `IServiceCollection`.

## Static documentation

- **[Calendar dependency injection guide](~/guides/calendar/dependency-injection.md)** — registration overloads, the reloadable workflow, and lifetime semantics.

## Key types

- <xref:Bodu.Globalization.Calendar.NotableDateServiceCollectionExtensions> — the registration surface:
  - `AddNotableDateService(IServiceCollection, NotableDateResource)` — register a singleton <xref:Bodu.Globalization.Calendar.INotableDateService> over an already-loaded resource.
  - `AddNotableDateService(IServiceCollection, NotableDateResource, NotableDateServiceOptions?)` — the same, composed with the collaborators on <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions>.
  - `AddNotableDateService(IServiceCollection, Func<IServiceProvider, NotableDateResource>)` — the resource is produced from the container (e.g. loaded from configuration or a data pack resolved through DI).
  - `AddNotableDateService(IServiceCollection, Func<IServiceProvider, NotableDateResource>, Func<IServiceProvider, NotableDateServiceOptions?>?)` — factory registration with the collaborators also produced from the container.
  - `AddNotableDateService(IServiceCollection, string serviceKey, NotableDateResource, NotableDateServiceOptions? = null)` and `AddNotableDateService(IServiceCollection, string serviceKey, Func<IServiceProvider, NotableDateResource>, Func<IServiceProvider, NotableDateServiceOptions?>? = null)` — **keyed** singletons, so a multi-tenant process registers one service per jurisdiction and resolves them by key.
  - `AddReloadableNotableDateService(IServiceCollection, NotableDateResource)` — register a singleton <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService> together with a singleton <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider> (also exposed as <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider>). Inject the mutable provider to call `Reload(...)` and the live service picks up the new resource.
  - `AddReloadableNotableDateService(IServiceCollection, NotableDateResource, NotableDateServiceOptions?)` and `AddReloadableNotableDateService(IServiceCollection, Func<IServiceProvider, NotableDateResource>, NotableDateServiceOptions? = null)` — the reloadable registration with collaborators propagated to each rebuilt inner service, and with the initial resource produced from the container.
  - `AddReloadableNotableDateService<TOptions>(IServiceCollection, Func<IServiceProvider, TOptions, NotableDateResource>, NotableDateServiceOptions? = null)` — driven by `IOptionsMonitor<TOptions>`: every options change rebuilds the resource through the factory and swaps it into the live service.

## Minimal sample

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

// From a companion data pack (or NotableDateResourceLoader.Load(...) for your own document):
builder.Services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));

// ... elsewhere, the resolved singleton is injected:
public sealed class HolidayController(INotableDateService calendar)
{
    public IReadOnlyList<NotableDate> Year(int year) => calendar.Resolve(year, "AU-NSW");
}
```

To swap the rule set at runtime, register the reloadable service and inject the mutable provider:

```csharp
builder.Services.AddReloadableNotableDateService(EuropeCalendarData.LoadResource("GB"));

// later, when the rules change:
provider.Reload(NotableDateResourceLoader.Load(updatedXml, CommonNotableDateResources.Resolver));
```

See the [Calendar dependency injection](~/guides/calendar/dependency-injection.md) guide for the full walkthrough, including the reloadable workflow and lifetime semantics.
