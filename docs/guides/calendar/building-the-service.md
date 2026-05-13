---
title: Building and extending the service
---

# Building and extending the service

`NotableDateService` is assembled by composing a set of registries, providers, and
extension-point implementations via its constructor. This page describes every constructor
parameter, each registry and factory type, and all the extension interfaces — so you can
wire up exactly the capabilities your application requires.

---

## NotableDateService constructor parameters

| Parameter | Type | Default | Purpose |
|---|---|---|---|
| `ruleProviders` | `IEnumerable<INotableDateRuleProvider>?` | `null` | Ordered list of rule providers. Rules are merged in registration order. When `null` or empty, the service loads the embedded minimal rule set (New Year's Day only). |
| `weekendDefinition` | `CalendarWeekendDefinition` | `SaturdaySunday` | Defines which days of the week constitute the weekend. Affects `IsWeekend`, `IsNonWorkingDay`, `IfWeekend` trigger evaluation, and all working-day extension methods. |
| `overrideProviders` | `IEnumerable<INotableDateRuleOverrideProvider>?` | `null` | Override providers that add or remove rules on top of the base rule set without modifying the source XML. Evaluated after all base providers. |
| `algorithmRegistry` | `INotableDateAlgorithmRegistry?` | `null` | Registry of `INotableDateAlgorithm` instances looked up by string key. Required when any rule uses `Strategy = DateResolutionStrategy.Algorithm`. |
| `adjustmentHandlers` | `IAdjustmentHandlerRegistry?` | `null` | Registry of `IAdjustmentHandler` instances. Required when any adjustment uses `Trigger = Custom` or `Action = Custom`. |
| `collisionResolver` | `INotableDateCollisionResolver?` | `null` | Resolves conflicts when multiple rules produce the same calendar date. Defaults to `DefaultNotableDateCollisionResolver`. |
| `nameLocalizer` | `INotableDateNameLocalizer?` | `null` | Translates `NotableDate.DisplayName` for a given `CultureInfo`. When `null`, `DisplayName` falls back to `Name`. |
| `plugins` | `IEnumerable<INotableDatePlugin>?` | `null` | External plugins loaded via `ExternalPluginLoader`. Each plugin can contribute additional rule providers and algorithm registrations. |

### Minimal construction

```csharp
using Bodu.Globalization.Calendar;

// Loads only the built-in minimal rule set (New Year's Day)
var service = new NotableDateService();
```

### Typical construction with a data pack

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("easter-sunday", new EasterSundayNotableDateAlgorithm());

var provider = new XmlResourceNotableDateRuleProvider(
    "MyApp/Calendar/Resources/holidays.xml",
    new ResourcePathResolver());

var service = new NotableDateService(
    ruleProviders:     new[] { provider },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    algorithmRegistry: registry);
```

---

## NotableDateAlgorithmRegistry

`NotableDateAlgorithmRegistry` is a thread-safe, in-memory registry that maps string keys
to `INotableDateAlgorithm` instances. It supports fluent chaining so all registrations can
be expressed in a single expression.

### Fluent API

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("easter-sunday",          new EasterSundayNotableDateAlgorithm())
    .Register("orthodox-easter-sunday", new OrthodoxEasterSundayNotableDateAlgorithm())
    .Register("qingming",               new QingmingNotableDateAlgorithm())
    .Register("vesak",                  new VesakNotableDateAlgorithm())
    .Register("losar",                  new LosarNotableDateAlgorithm())
    .Register("asalha-puja",            new AsalhaPujaNotableDateAlgorithm());
```

### Key vs type lookup

Rules reference algorithms in two ways:

- **`AlgorithmKey`** — a plain string key registered via `Register`. Preferred. Case-insensitive.
- **`AlgorithmType`** — an assembly-qualified type name. Used as a fallback when `AlgorithmKey`
  is not present in the registry. The type is activated via reflection; the type must have a
  public parameterless constructor.

Prefer key-based lookup: it decouples the rule document from assembly names and makes the
registry the single point of registration.

### Checking registration

```csharp
if (registry.Contains("easter-sunday"))
{
    // Safe to use rules that reference this key
}
```

### Implementing INotableDateAlgorithm

```csharp
using Bodu.Globalization.Calendar;

// Second Sunday in May — Mother's Day
public sealed class MothersDayAlgorithm : INotableDateAlgorithm
{
    public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null)
    {
        DateTime firstOfMay = new DateTime(year, 5, 1);
        int daysToSunday = ((int)DayOfWeek.Sunday - (int)firstOfMay.DayOfWeek + 7) % 7;
        return firstOfMay.AddDays(daysToSunday + 7); // second Sunday
    }
}
```

Register and wire to a rule:

```csharp
NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("mothers-day", new MothersDayAlgorithm());

NotableDateRule mothersDay = new NotableDateRule
{
    Name         = "Mother's Day",
    Strategy     = DateResolutionStrategy.Algorithm,
    Category     = NotableDateCategory.Observance,
    AlgorithmKey = "mothers-day",
};

var service = new NotableDateService(
    ruleProviders:     new[] { new InMemoryRuleProvider(mothersDay) },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    algorithmRegistry: registry);
```

---

## AdjustmentHandlerRegistry

`AdjustmentHandlerRegistry` is a thread-safe registry that maps string keys to
`IAdjustmentHandler` instances. It follows the same fluent pattern as
`NotableDateAlgorithmRegistry`.

### Fluent API

```csharp
using Bodu.Globalization.Calendar;

AdjustmentHandlerRegistry handlers = new AdjustmentHandlerRegistry()
    .Register("corporate-closure",   new CorporateClosureHandler())
    .Register("next-working-day",    new NextWorkingDayHandler());

var service = new NotableDateService(
    ruleProviders:      new[] { provider },
    weekendDefinition:  CalendarWeekendDefinition.SaturdaySunday,
    adjustmentHandlers: handlers);
```

### IAdjustmentHandler contract

```csharp
public interface IAdjustmentHandler
{
    AdjustmentHandlerResult Apply(AdjustmentHandlerContext context);
}
```

`AdjustmentHandlerContext` provides:

| Property | Type | Description |
|---|---|---|
| `CurrentDate` | `DateTime` | The current anchor date (before this adjustment). |
| `Rule` | `NotableDateRule` | The rule being resolved. |
| `Adjustment` | `ObservanceAdjustment` | The adjustment being evaluated. |
| `TerritoryCode` | `TerritoryCode?` | The territory the rule is being resolved for. |
| `Year` | `int` | The year being resolved. |
| `Parameters` | `IReadOnlyDictionary<string,string>` | Contents of `ObservanceAdjustment.HandlerParameters`. |
| `GenerationContext` | `NotableDateGenerationContext` | Access to `IsNonWorkingDay(date, territory)` and `ResolveByName(ruleName)` for working-day and cross-rule dependencies. |

`AdjustmentHandlerResult` factory methods:

```csharp
// Handler handled the adjustment; return the adjusted date
AdjustmentHandlerResult.Handled(DateTime adjustedDate)

// Handler declines; the next adjustment in the chain is evaluated
AdjustmentHandlerResult.NotHandled()
```

### Example handler

```csharp
using Bodu.Globalization.Calendar;

public sealed class NextWorkingDayHandler : IAdjustmentHandler
{
    public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context)
    {
        DateTime candidate = context.CurrentDate.AddDays(1);

        for (int i = 0; i < 7; i++)
        {
            if (!context.GenerationContext.IsNonWorkingDay(candidate, context.TerritoryCode))
                return AdjustmentHandlerResult.Handled(candidate);

            candidate = candidate.AddDays(1);
        }

        return AdjustmentHandlerResult.NotHandled();
    }
}
```

Wire to an adjustment:

```csharp
new ObservanceAdjustment
{
    Key        = "shift-to-next-working-day",
    Trigger    = AdjustmentTrigger.IfNonWorkingDay,
    Action     = AdjustmentAction.Custom,
    HandlerKey = "next-working-day",
}
```

---

## NotableDateFilter — factory methods and composition

`NotableDateFilter` is a composable two-stage predicate. See [The resolution pipeline — Stage 8](resolution-pipeline.md#stage-8--filter-gate-filtered-queries-only) for an explanation of rule-level and date-level gates.

### Factory method reference

| Factory method | Gate | Description |
|---|---|---|
| `ForCategory(category)` | Rule-level | Matches rules whose `Category` equals the given value. |
| `ForAnyCategory(categories)` | Rule-level | Matches rules whose `Category` is in the provided set. |
| `WithTag(tag)` | Rule-level | Matches rules whose `Tags` contains the given tag. |
| `WithAnyTag(tags)` | Rule-level | Matches rules whose `Tags` contains at least one of the given tags. |
| `WithAllTags(tags)` | Rule-level | Matches rules whose `Tags` contains all of the given tags. |
| `WithName(name)` | Rule-level | Matches rules whose `Name` equals the given value (case-insensitive). |
| `WithAnyName(names)` | Rule-level | Matches rules whose `Name` is in the provided set. |
| `IsNonWorkingDay()` | Rule-level | Matches rules where `IsNonWorkingDay = true`. |
| `InDateRange(start, end)` | Date-level | Matches resolved dates within the inclusive range. |
| `WasAdjusted()` | Date-level | Matches dates where `WasAdjusted = true`. |
| `WithMinDuration(days)` | Date-level | Matches dates where `DurationDays >= days`. |

### Composition

```csharp
using Bodu.Globalization.Calendar;

// And: both predicates must match
NotableDateFilter nonWorkingHolidays = NotableDateFilter
    .ForCategory(NotableDateCategory.Holiday)
    .And(NotableDateFilter.IsNonWorkingDay());

// Or: either predicate matches
NotableDateFilter holidayOrObservance = NotableDateFilter
    .ForCategory(NotableDateCategory.Holiday)
    .Or(NotableDateFilter.ForCategory(NotableDateCategory.Observance));

// AllOf: equivalent to chained And
NotableDateFilter federalNonWorking = NotableDateFilter.AllOf(
    NotableDateFilter.ForCategory(NotableDateCategory.Holiday),
    NotableDateFilter.IsNonWorkingDay(),
    NotableDateFilter.WithTag("Federal"));

// AnyOf: equivalent to chained Or
NotableDateFilter anyReligious = NotableDateFilter.AnyOf(
    NotableDateFilter.WithTag("Christian"),
    NotableDateFilter.WithTag("Jewish"),
    NotableDateFilter.WithTag("Muslim"));

// Combined rule-level and date-level
NotableDateFilter easterWeek2026 = NotableDateFilter
    .ForCategory(NotableDateCategory.Holiday)
    .And(NotableDateFilter.InDateRange(
        new DateTime(2026, 3, 30),
        new DateTime(2026, 4, 7)));
```

### Using a filter

```csharp
IReadOnlyList<NotableDate> results = service.GetNotableDates(
    year:          2026,
    filter:        nonWorkingHolidays,
    territoryCode: "GB");
```

---

## INotableDateRuleOverrideProvider

Override providers layer additions and removals on top of the base rule set at runtime.
Implement `INotableDateRuleOverrideProvider` and pass instances via `overrideProviders`.

### Interface

```csharp
public interface INotableDateRuleOverrideProvider
{
    IEnumerable<RuleRemoval> GetRemovals();
    IEnumerable<NotableDateRule> GetAdditions();
}
```

`RuleRemoval` identifies a rule by name with optional year bounds:

```csharp
// Remove "Boxing Day" for 2026 only
new RuleRemoval("Boxing Day", FromYear: 2026, ToYear: 2026)

// Remove "Temporary Holiday" for all years
new RuleRemoval("Temporary Holiday")
```

### Example

```csharp
using Bodu.Globalization.Calendar;

public sealed class CompanyCalendarOverrides : INotableDateRuleOverrideProvider
{
    public IEnumerable<RuleRemoval> GetRemovals()
    {
        // Suppress Boxing Day for the company calendar in 2026
        yield return new RuleRemoval("Boxing Day", FromYear: 2026, ToYear: 2026);
    }

    public IEnumerable<NotableDateRule> GetAdditions()
    {
        // Add a company-specific non-working day
        yield return new NotableDateRule
        {
            Name            = "Company Founding Day",
            Strategy        = DateResolutionStrategy.Fixed,
            Category        = NotableDateCategory.Observance,
            Month           = 6,
            Day             = 15,
            IsNonWorkingDay = true,
        };
    }
}

var service = new NotableDateService(
    ruleProviders:     new[] { baseProvider },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    overrideProviders: new[] { new CompanyCalendarOverrides() });
```

### Cache invalidation after override state changes

The effective rule list is derived once and cached internally. If override provider state
changes at runtime (e.g. a database-driven list of company closures is updated), call
`Invalidate()` to force re-derivation:

```csharp
// After updating the override source
service.Invalidate();       // clear all years
service.Invalidate(2026);   // clear one year only
```

---

## INotableDateNameLocalizer

`INotableDateNameLocalizer` translates `NotableDate.DisplayName` for a given `CultureInfo`.
When registered, `DisplayName` returns the localised name instead of the canonical English
`Name`.

### Interface

```csharp
public interface INotableDateNameLocalizer
{
    string GetDisplayName(NotableDate notableDate, CultureInfo? culture);
}
```

### Example — resource-file-based localiser

```csharp
using System.Globalization;
using System.Resources;
using Bodu.Globalization.Calendar;

public sealed class ResourceFileNameLocalizer : INotableDateNameLocalizer
{
    private readonly ResourceManager _resources;

    public ResourceFileNameLocalizer(ResourceManager resources) =>
        _resources = resources;

    public string GetDisplayName(NotableDate notableDate, CultureInfo? culture)
    {
        string? localised = _resources.GetString(notableDate.Name, culture);
        return localised ?? notableDate.Name;
    }
}

var localizer = new ResourceFileNameLocalizer(
    new ResourceManager("MyApp.Resources.HolidayNames", typeof(Program).Assembly));

var service = new NotableDateService(
    ruleProviders:  new[] { provider },
    nameLocalizer:  localizer);
```

When a caller requests `NotableDate.DisplayName`, the service invokes
`GetDisplayName(date, Thread.CurrentThread.CurrentUICulture)` and returns the result. If the
localiser returns `null` or throws, `DisplayName` falls back to `Name`.

---

## INotableDateCollisionResolver

`INotableDateCollisionResolver` is called when two or more rules resolve to the same
calendar date. The resolver receives the conflicting `NotableDate` instances and returns the
list to include in the output.

### Interface

```csharp
public interface INotableDateCollisionResolver
{
    IReadOnlyList<NotableDate> Resolve(
        DateTime date,
        IReadOnlyList<NotableDate> overlapping);
}
```

### Default behaviour

`DefaultNotableDateCollisionResolver` removes exact duplicates (same name, category,
territory, and date) and preserves all distinct entries, ordered by `Category` then `Name`.
Both entries are returned when two unrelated holidays land on the same date.

### Example — keep highest-priority entry only

```csharp
using Bodu.Globalization.Calendar;

public sealed class HighestPriorityCollisionResolver : INotableDateCollisionResolver
{
    public IReadOnlyList<NotableDate> Resolve(
        DateTime date,
        IReadOnlyList<NotableDate> overlapping)
    {
        if (overlapping.Count <= 1)
            return overlapping;

        // Lower Priority number = higher precedence
        NotableDate winner = overlapping
            .OrderBy(d => d.Rule?.Priority ?? 100)
            .ThenBy(d => d.Name, StringComparer.Ordinal)
            .First();

        return new[] { winner };
    }
}

var service = new NotableDateService(
    ruleProviders:     new[] { provider },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    collisionResolver: new HighestPriorityCollisionResolver());
```

---

## Plugin system

The plugin system allows external assemblies to contribute rule providers and algorithm
registrations without modifying the consuming application. Plugins are loaded via
`ExternalPluginLoader` with a configurable trust policy.

### Plugin interfaces

A plugin assembly must declare the `[NotableDatePlugin]` assembly attribute pointing to its
plugin class. The class implements one or both plugin interfaces:

```csharp
// Contributes additional rule providers
public interface INotableDateRulePlugin : INotableDatePlugin
{
    IEnumerable<INotableDateRuleProvider> GetRuleProviders();
}

// Contributes named algorithm registrations
public interface INotableDateAlgorithmPlugin : INotableDatePlugin
{
    IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms();
}
```

Both interfaces extend `INotableDatePlugin`:

```csharp
public interface INotableDatePlugin
{
    string Name    { get; }
    Version Version { get; }
}
```

### Loading a plugin

```csharp
using Bodu.Globalization.Calendar.Plugins;

// Allow all plugins (development or trusted environment only)
IPluginTrustPolicy trust = new AllowAllPluginTrustPolicy();

ExternalPluginLoader loader = new ExternalPluginLoader(trust);
INotableDatePlugin plugin = loader.Load("/path/to/MyCalendarPlugin.dll");

var service = new NotableDateService(
    ruleProviders: new[] { baseProvider },
    plugins:       new[] { plugin });
```

### Trust policies

| Policy class | Behaviour |
|---|---|
| `AllowAllPluginTrustPolicy` | Trusts every plugin unconditionally. Suitable only for development environments. |
| `FileHashPluginTrustPolicy` | Allowlist by SHA-256 file hash. Rejects assemblies whose hash is not in the allowlist. |
| `StrongNamePluginTrustPolicy` | Allowlist by strong name (public key token + assembly name). |
| `CompositePluginTrustPolicy` | Combines multiple policies with configurable AND / OR semantics. |
| `DelegatingPluginTrustPolicy` | Wraps a custom `Func<PluginTrustContext, PluginTrustResult>` delegate. |

```csharp
using Bodu.Globalization.Calendar.Plugins;

// Only allow plugins signed with a specific key token
IPluginTrustPolicy policy = new StrongNamePluginTrustPolicy(
    allowedPublicKeyTokens: new[] { "aabbccddeeff0011" });

// Or combine policies: must pass both hash check AND strong-name check
IPluginTrustPolicy combined = new CompositePluginTrustPolicy(
    mode:     CompositePluginTrustMode.All,
    policies: new IPluginTrustPolicy[]
    {
        new FileHashPluginTrustPolicy(allowedHashes: new[] { "sha256:abc123..." }),
        new StrongNamePluginTrustPolicy(allowedPublicKeyTokens: new[] { "aabbccddeeff0011" }),
    });
```

Each plugin is loaded into its own `AssemblyLoadContext` for isolation. Untrusted plugins
throw `PluginNotTrustedException`. Plugins that lack the `[NotableDatePlugin]` assembly
attribute throw `PluginMissingAttributeException`.

---

## Putting it all together

The following example assembles a service with two rule providers, a weekend definition, a
full algorithm registry, a custom adjustment handler, an override provider, a name
localiser, and a custom collision resolver:

```csharp
using System.Globalization;
using System.Resources;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

// 1. Algorithm registry
NotableDateAlgorithmRegistry algorithms = new NotableDateAlgorithmRegistry()
    .Register("easter-sunday",          new EasterSundayNotableDateAlgorithm())
    .Register("orthodox-easter-sunday", new OrthodoxEasterSundayNotableDateAlgorithm())
    .Register("qingming",               new QingmingNotableDateAlgorithm())
    .Register("vesak",                  new VesakNotableDateAlgorithm())
    .Register("mothers-day",            new MothersDayAlgorithm());

// 2. Custom adjustment handler registry
AdjustmentHandlerRegistry adjustmentHandlers = new AdjustmentHandlerRegistry()
    .Register("next-working-day", new NextWorkingDayHandler());

// 3. Rule providers — base rules then region-specific pack
XmlResourceNotableDateRuleProvider globalRules = new XmlResourceNotableDateRuleProvider(
    "MyApp/Calendar/Resources/global-core.xml",
    new ResourcePathResolver());

XmlResourceNotableDateRuleProvider apacRules = new XmlResourceNotableDateRuleProvider(
    "MyApp/Calendar/Resources/apac-holidays.xml",
    new ResourcePathResolver());

// 4. Runtime overrides
CompanyCalendarOverrides overrides = new CompanyCalendarOverrides();

// 5. Name localiser (optional)
ResourceFileNameLocalizer localizer = new ResourceFileNameLocalizer(
    new ResourceManager("MyApp.Resources.HolidayNames", typeof(Program).Assembly));

// 6. Collision resolver (optional — replace default)
HighestPriorityCollisionResolver collisionResolver = new HighestPriorityCollisionResolver();

// 7. Assemble the service
NotableDateService service = new NotableDateService(
    ruleProviders:      new[] { globalRules, apacRules },
    weekendDefinition:  CalendarWeekendDefinition.SaturdaySunday,
    overrideProviders:  new[] { overrides },
    algorithmRegistry:  algorithms,
    adjustmentHandlers: adjustmentHandlers,
    collisionResolver:  collisionResolver,
    nameLocalizer:      localizer);

// 8. Query
IReadOnlyList<NotableDate> dates = service.GetNotableDates(2026, "AU");

foreach (NotableDate date in dates)
    Console.WriteLine($"{date.Date:d MMM yyyy}  {date.DisplayName}");
```

---

## Where to go next

- [NotableDateRule and ObservanceAdjustment reference](rule-reference.md) — field definitions for rule and adjustment authoring.
- [Observance adjustment rules](adjustment-rules.md) — trigger and action catalogues, custom handlers.
- [The resolution pipeline](resolution-pipeline.md) — how the service processes the types described here.
- [Holiday patterns and examples](holiday-patterns.md) — end-to-end examples for common holiday types.
- [Authoring notable date rules](rule-authoring.md) — in-code objects, XML resource files, and companion assemblies.
