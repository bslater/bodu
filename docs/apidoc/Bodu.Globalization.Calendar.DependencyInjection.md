---
uid: Microsoft.Extensions.DependencyInjection.NotableDateServiceCollectionExtensions
---

# Bodu.Globalization.Calendar.DependencyInjection

## Purpose

**Bodu.Globalization.Calendar.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` integration for [`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md). It registers <xref:Bodu.Globalization.Calendar.INotableDateService> as a singleton over a loaded <xref:Bodu.Globalization.Calendar.NotableDateResource> (or a factory that produces one), so an ASP.NET Core app — or any `Microsoft.Extensions.*`-style host — can inject the calendar service rather than composing `new NotableDateService(...)` by hand.

The package is intentionally thin: a resource is an immutable, already-validated value, so registration takes the resource (or a provider for it) directly. There is no options object and no fluent builder — the service's behaviour is carried by the resource's `<ResolutionPolicy>` and by the optional collaborators passed when the resource/service is built. Direct construction continues to work for consoles, libraries, and tests that prefer not to bring in `IServiceCollection`.

The extension methods live in the `Microsoft.Extensions.DependencyInjection` namespace, so they light up on `IServiceCollection` without an extra `using`.

## Static documentation

- **[Calendar dependency injection guide](~/guides/calendar/dependency-injection.md)** — registration overloads, the reloadable workflow, and lifetime semantics.

## Key types

- <xref:Microsoft.Extensions.DependencyInjection.NotableDateServiceCollectionExtensions> — the registration surface:
  - `AddNotableDateService(IServiceCollection, NotableDateResource)` — register a singleton <xref:Bodu.Globalization.Calendar.INotableDateService> over an already-loaded resource.
  - `AddNotableDateService(IServiceCollection, Func<IServiceProvider, NotableDateResource>)` — the same, but the resource is produced from the container (e.g. loaded from configuration or a data pack resolved through DI).
  - `AddReloadableNotableDateService(IServiceCollection, NotableDateResource)` — register a singleton <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService> together with a singleton <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider> (also exposed as <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider>). Inject the mutable provider to call `Reload(...)` and the live service picks up the new resource.

## Minimal sample

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.AsiaPacific;
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
