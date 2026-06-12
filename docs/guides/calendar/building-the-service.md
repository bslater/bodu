---
title: Building and extending the service
---

# Building and extending the service

A <xref:Bodu.Globalization.Calendar.NotableDateService> is built over a single immutable, already-validated <xref:Bodu.Globalization.Calendar.NotableDateResource>. The simplest construction takes just the resource; richer scenarios supply optional collaborators through the constructor's overloads. This page walks the constructor surface, the runtime-swap pair, code-first providers, display-name localization, and the trust-gated plugin system.

For the vocabulary used below (resource vs. document, rule vs. resolved date, nominal vs. observed) see [Core concepts](../../docs/calendar/concepts.md).

## Constructing the service

The base constructor takes the loaded resource:

```csharp
using Bodu.Globalization.Calendar;

NotableDateResource resource = NotableDateResourceLoader.Load(xml, CommonNotableDateResources.Resolver);
NotableDateService  service  = new NotableDateService(resource);
```

There is no options object: resolution behaviour is carried by the resource itself — its `<ResolutionPolicy>` decides duplicate handling, same-day collisions, the priority direction, observed-date inclusion, and the working week. To change those, edit the document or build the resource differently; see [Identity and resolution](identity-and-resolution.md). The constructor overloads add optional collaborators, in this fixed order:

| Parameter | Type | Purpose |
|---|---|---|
| `resource` | `NotableDateResource` | The loaded, validated resource the service draws occurrences from. Required. |
| `algorithms` | `INotableDateAlgorithmRegistry?` | A custom algorithm registry for `<Algorithm key="…">` rules. `null` uses the built-in keys only. |
| `collisionResolver` | `INotableDateCollisionResolver?` | Consulted only when the resource's same-day collision policy is `CollisionPolicy.Custom`. |
| `handlers` | `IAdjustmentHandlerRegistry?` | Consulted when an adjustment **action** is `AdjustmentAction.Custom`. |
| `triggerHandlers` | `IAdjustmentTriggerHandlerRegistry?` | Consulted when an adjustment **trigger** is `AdjustmentTrigger.Custom`. |
| `providers` | `IEnumerable<INotableDateProvider>?` | Code-first providers that contribute finished occurrences. |

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

// A custom algorithm backs a <Algorithm key="pi-day"> rule in the resource.
var algorithms = new NotableDateAlgorithmRegistry()
    .Register("pi-day", new PiDayAlgorithm());

NotableDateService service = new NotableDateService(resource, algorithms);
```

To supply a later collaborator while leaving an earlier one at its default, pass `null` for the ones you do not need:

```csharp
NotableDateService service = new NotableDateService(
    resource,
    algorithms:        algorithms,
    collisionResolver: null,                       // use the resource's policy as-authored
    handlers:          customActions,
    triggerHandlers:   null,
    providers:         new[] { new CompanyEventsProvider() });
```

### Custom algorithm registry

A <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> maps string keys to <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> instances and chains fluently. The same registry instance should be handed to the loader (so the document validates) and to the service (so resolution can look the key up):

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

public sealed class PiDayAlgorithm : INotableDateAlgorithm
{
    public DateOnly? Calculate(int year) => new DateOnly(year, 3, 14);
}

var registry = new NotableDateAlgorithmRegistry()
    .Register("pi-day", new PiDayAlgorithm());

NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null, registry);
NotableDateService  service  = new NotableDateService(resource, registry);
```

Built-in keys (`western-easter`, `orthodox-easter`, `qingming`, `vesak`, `losar`, `matariki`, the Hindu-festival keys, …) need no registration. See [Date calculation algorithms](algorithms.md).

### Custom collision resolver

When the resource declares `<ResolutionPolicy sameDayCollisionPolicy="Custom">`, the service delegates same-day reconciliation to your <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver>. It receives the day and the colliding occurrences and returns the set to keep:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.RangeResolution;

public sealed class HighestPriorityResolver : INotableDateCollisionResolver
{
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)
    {
        if (colliding.Count <= 1)
            return colliding;

        NotableDate winner = colliding.OrderByDescending(d => d.Priority).First();
        return new[] { winner };
    }
}

NotableDateService service = new NotableDateService(resource, algorithms: null, collisionResolver: new HighestPriorityResolver());
```

The built-in policies (`KeepAll`, `HighestPriorityOnly`, `CategoryPriority`) cover most needs and require no resolver; reach for `Custom` only for bespoke precedence. See [Identity and resolution](identity-and-resolution.md).

### Custom adjustment handlers

Adjustment policies normally use built-in triggers and actions. When a policy declares `<Trigger type="Custom" handlerKey="…">` or `<Action type="Custom" handlerKey="…">`, the service looks the key up in the handler registries you pass:

```csharp
using Bodu.Globalization.Calendar;

var actions = new AdjustmentHandlerRegistry()
    .Register("skip-to-payday", new SkipToPaydayHandler());

var triggers = new AdjustmentTriggerHandlerRegistry()
    .Register("if-school-term", new IfSchoolTermTrigger());

NotableDateService service = new NotableDateService(
    resource, algorithms: null, collisionResolver: null, handlers: actions, triggerHandlers: triggers);
```

An <xref:Bodu.Globalization.Calendar.IAdjustmentHandler> implements `DateOnly? Adjust(AdjustmentHandlerContext)`; an <xref:Bodu.Globalization.Calendar.IAdjustmentTriggerHandler> implements `bool ShouldAdjust(AdjustmentTriggerContext)`. See [Observance adjustment rules](adjustment-rules.md) for the trigger / action catalogues and the context members.

## Code-first providers

When a source cannot be expressed as an authored rule — occurrences pulled from a database, an HR system, or computed by bespoke logic — implement <xref:Bodu.Globalization.Calendar.INotableDateProvider> and register it through the `providers` parameter. A provider returns finished <xref:Bodu.Globalization.Calendar.NotableDate> occurrences for a requested range and territory:

```csharp
using Bodu.Globalization.Calendar;

public sealed class CompanyEventsProvider : INotableDateProvider
{
    public IEnumerable<NotableDate> GetNotableDates(DateRange range, string territory)
    {
        var foundingDay = new DateOnly(range.StartDate.Year, 6, 15);
        if (range.StartDate <= foundingDay && foundingDay <= range.EndDate)
            yield return new NotableDate(
                Date:        foundingDay,
                ActualDate:  foundingDay,
                IsObserved:  false,
                Identity:    new NotableDateRuleIdentity("company-events", "company-founding-day", "default"),
                DisplayName: "Company Founding Day",
                TerritoryCode: territory,
                Category:    NotableDateCategory.Civic,
                Priority:    0,
                DurationDays: 1,
                IsNonWorkingDay: true,
                Tags:        Array.Empty<string>(),
                AdjustmentPolicyId: null,
                AdjustmentReason:   null);
    }
}

NotableDateService service = new NotableDateService(
    resource, algorithms: null, collisionResolver: null, handlers: null, triggerHandlers: null,
    providers: new[] { new CompanyEventsProvider() });
```

Provider occurrences are *terminal*: the service intersects them with the requested range and applies any query filter, but they do **not** pass through adjustment policies or declarative overrides — a provider that needs an observed-date shift must compute it itself. They do take part in the final ordering and the resource's same-day collision policy alongside resource occurrences.

## Swapping the rule set at runtime

A resource is immutable, so a *live* change means loading a new resource and swapping it in. Build the service over a <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider> via <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService>; the reloadable service rereads the provider's `Current` on each query:

```csharp
using Bodu.Globalization.Calendar;

var provider = new MutableNotableDateResourceProvider(NotableDateResourceLoader.Load(initialXml));
INotableDateService service = new ReloadableNotableDateService(provider);

// later, when the rules change — the live service picks it up atomically on the next query:
provider.Reload(NotableDateResourceLoader.Load(updatedXml, CommonNotableDateResources.Resolver));
```

`ReloadableNotableDateService` accepts the same optional collaborators as `NotableDateService` (custom algorithm registry, collision resolver, adjustment-handler registries) after the provider argument. The pairing is what the DI companion's `AddReloadableNotableDateService` registers for you — see [Calendar dependency injection](dependency-injection.md).

## Localizing display names

Resolution stays culture-agnostic: each <xref:Bodu.Globalization.Calendar.NotableDate> carries the invariant `DisplayName` authored in the resource. To present culture-specific names, implement <xref:Bodu.Globalization.Calendar.INotableDateNameLocalizer> and apply it to resolved occurrences with the `Localize` extensions on <xref:Bodu.Globalization.Calendar.NotableDateLocalizationExtensions>:

```csharp
using System.Globalization;
using System.Resources;
using Bodu.Globalization.Calendar;

public sealed class ResxNameLocalizer : INotableDateNameLocalizer
{
    private readonly ResourceManager _resources;

    public ResxNameLocalizer(ResourceManager resources) =>
        _resources = resources;

    // Return null to fall back to the occurrence's existing display name.
    public string? GetDisplayName(NotableDate notableDate, CultureInfo culture) =>
        _resources.GetString(notableDate.NotableDateId, culture);
}

var localizer = new ResxNameLocalizer(
    new ResourceManager("MyApp.Resources.HolidayNames", typeof(Program).Assembly));

IReadOnlyList<NotableDate> dates = service
    .Resolve(2026, "FR")
    .Localize(localizer, CultureInfo.GetCultureInfo("fr-FR"));   // display names now French
```

`Localize` returns a copy with the localized `DisplayName` (or the original occurrence when the localizer returns `null`), so the localization step is opt-in and never mutates resolution output. A single-occurrence overload (`notableDate.Localize(localizer, culture)`) is also available.

## Plugin system

The optional `Bodu.Globalization.Calendar.Plugins` package loads **external assemblies** that contribute custom date-calculation algorithms, behind an explicit, **deny-by-default** trust gate. A plugin assembly advertises itself with an assembly-level attribute; the host evaluates it against a trust policy *before* any plugin type is activated, then registers the plugin's algorithms into a <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> for use by `<Algorithm key="…">` rules.

### Authoring a plugin

A plugin implements <xref:Bodu.Globalization.Calendar.Plugins.INotableDateAlgorithmPlugin> and is named by an assembly-level <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginAttribute>:

```csharp
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Plugins;

[assembly: NotableDatePlugin(typeof(Contoso.Holidays.ContosoPlugin))]

namespace Contoso.Holidays;

public sealed class ContosoPlugin : INotableDateAlgorithmPlugin
{
    public string  Name    => "Contoso.Holidays";
    public Version Version => new(1, 0, 0);

    public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms()
    {
        yield return new("contoso-founders-day", new FoundersDayAlgorithm());
    }
}
```

### Loading and registering

<xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginLoader> evaluates trust, activates the plugin, and registers its algorithms. `LoadFrom(Assembly, …)` loads from an already-loaded assembly; `LoadFrom(string assemblyPath, …)` loads the file into a dedicated `AssemblyLoadContext`:

```csharp
using System.Reflection;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Plugins;

// Trust only assemblies whose strong-name public-key token is on the allow-list.
IPluginTrustPolicy trust = new StrongNamePluginTrustPolicy(allowedPublicKeyTokens);

Assembly assembly = Assembly.LoadFrom("Contoso.Holidays.dll");
INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(assembly, trust);   // throws if untrusted

var registry = new NotableDateAlgorithmRegistry();
int registered = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);

// The registry now backs <Algorithm key="contoso-founders-day"> rules:
NotableDateResource resource = NotableDateResourceLoader.Load(xml, CommonNotableDateResources.Resolver, registry);
NotableDateService  service  = new NotableDateService(resource, registry);
```

### Trust policies

Trust is decided by an <xref:Bodu.Globalization.Calendar.Plugins.IPluginTrustPolicy>, whose `Evaluate(PluginTrustContext)` returns a <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustResult> (`PluginTrustResult.Trusted()` or `PluginTrustResult.Rejected(reason)`). The bundled policies:

| Policy | Behaviour |
|---|---|
| <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy> | Trusts every assembly. Development / tests only. |
| <xref:Bodu.Globalization.Calendar.Plugins.StrongNamePluginTrustPolicy> | Allow-list by strong-name public-key token. |
| <xref:Bodu.Globalization.Calendar.Plugins.FileHashPluginTrustPolicy> | Allow-list by SHA-256 file hash. |
| <xref:Bodu.Globalization.Calendar.Plugins.CompositePluginTrustPolicy> | Combines policies with AND / short-circuit semantics. |
| <xref:Bodu.Globalization.Calendar.Plugins.DelegatingPluginTrustPolicy> | Decides with a `Func<PluginTrustContext, PluginTrustResult>` delegate. |

```csharp
using Bodu.Globalization.Calendar.Plugins;

// Must pass both a hash check AND a strong-name check.
IPluginTrustPolicy policy = new CompositePluginTrustPolicy(
    new FileHashPluginTrustPolicy(allowedHashes),
    new StrongNamePluginTrustPolicy(allowedPublicKeyTokens));
```

> [!WARNING]
> <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy> trusts every assembly and is intended for development and tests only. Use a strong-name, file-hash, or composite policy in production.

### Plugin exceptions

The loader signals failure with the <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginException> hierarchy:

- <xref:Bodu.Globalization.Calendar.Plugins.PluginNotTrustedException> — the trust policy rejected the assembly; it is never activated.
- <xref:Bodu.Globalization.Calendar.Plugins.PluginMissingAttributeException> — the assembly lacks a `[assembly: NotableDatePlugin(…)]` attribute.
- <xref:Bodu.Globalization.Calendar.Plugins.PluginActivationException> — the named plugin type could not be instantiated.

See the [Plugins package reference](xref:Bodu.Globalization.Calendar.Plugins) for the full type list.

## Where to go next

- [Using NotableDateService](notable-dates.md) — query patterns, filters, and range queries.
- [Calendar dependency injection](dependency-injection.md) — registering the service (and the reloadable pair) through `IServiceCollection`.
- [Date calculation algorithms](algorithms.md) — built-in keys and implementing `INotableDateAlgorithm`.
- [Observance adjustment rules](adjustment-rules.md) — triggers, actions, and custom adjustment handlers.
- [Authoring notable date rules](rule-authoring.md) — XML / JSON documents, imports, and overrides.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full type reference.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
