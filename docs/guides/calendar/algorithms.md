---
title: Date calculation algorithms
---

# Date calculation algorithms

Several notable dates cannot be expressed as a fixed month / day or an *n*th weekday-of-month — their position in the calendar depends on astronomical or ecclesiastical calculations. **Bodu.Globalization.Calendar** ships built-in implementations for the most common cases.

For the conceptual distinction between *algorithm* and *fixed rule*, see [Core concepts — Algorithm vs. fixed rule](../../docs/calendar/concepts.md#algorithm-vs-fixed-rule).

## Built-in algorithms

The following classes implement <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm> and can be registered in a `NotableDateAlgorithmRegistry` for use by rules with `Strategy = DateResolutionStrategy.Algorithm`:

| Class | Date it computes | Notes |
|---|---|---|
| `EasterSundayNotableDateAlgorithm` | Easter Sunday | Gregorian computus for years ≥ 1583; Julian computus otherwise. |
| `HinduLunarNotableDateAlgorithm` | Hindu lunar festivals (Diwali, Holi, …) | Approximate Gregorian projection of a Hindu panchanga date. |
| `LosarNotableDateAlgorithm` | Losar (Tibetan New Year) | Tibetan lunisolar calculation. |
| `VesakNotableDateAlgorithm` | Vesak (Buddha's birthday) | Full-moon calculation per Theravāda tradition. |
| `AsalhaPujaNotableDateAlgorithm` | Asalha Puja (Dhamma Day) | Full moon of the 8th lunar month. |
| `QingmingNotableDateAlgorithm` | Qingming (Tomb-Sweeping Day) | Solar term 15° after the Spring Equinox (typically 4–5 April). |

The `Bodu.Globalization.Calendar.Providers` namespace contains two `INotableDateProvider` implementations — `GregorianEasterSundayNotableDateProvider` and `OrthodoxEasterSundayNotableDateProvider` — which can be used directly when you want to compute Easter dates outside the rule-resolution pipeline.

## Registering algorithms

Rules reference algorithms by string key via `NotableDateRule.AlgorithmKey`. Register the corresponding instance in a `NotableDateAlgorithmRegistry` and supply it to `NotableDateService`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

// Build a registry with fluent chaining:
NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("easter-sunday", new EasterSundayNotableDateAlgorithm())
    .Register("qingming",      new QingmingNotableDateAlgorithm());

var service = new NotableDateService(
    ruleProviders:     new[] { new XmlResourceNotableDateRuleProvider(
                           "MyApp/Calendar/Resources/rules.xml",
                           new ResourcePathResolver()) },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
```

Most built-in algorithm rules are already defined in the embedded global XML rule set and do not need to be registered manually when using the default `NotableDateService()` constructor.

## Using an algorithm in a rule

Rules that require an algorithm set `Strategy = DateResolutionStrategy.Algorithm` and identify the algorithm via `AlgorithmKey`. Rules that derive from an algorithmic anchor use `DateResolutionStrategy.OffsetFromAnchor` and reference the anchor's `Name`:

```csharp
using Bodu.Globalization.Calendar;

// Easter Sunday — resolved via the registered "easter-sunday" algorithm.
NotableDateRule easterSunday = new NotableDateRule
{
    Name            = "Easter Sunday",
    Strategy        = DateResolutionStrategy.Algorithm,
    Category        = NotableDateCategory.Holiday,
    AlgorithmKey    = "easter-sunday",
    IsNonWorkingDay = true,
};

// Easter Monday — offset 1 day from Easter Sunday.
NotableDateRule easterMonday = new NotableDateRule
{
    Name            = "Easter Monday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = 1,
    IsNonWorkingDay = true,
};

// Good Friday — 2 days before Easter Sunday.
NotableDateRule goodFriday = new NotableDateRule
{
    Name            = "Good Friday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = -2,
    IsNonWorkingDay = true,
};
```

## Computing Easter directly

Use the provider classes when you want Easter dates without setting up a full `NotableDateService`:

```csharp
using Bodu.Globalization.Calendar.Providers;

// Gregorian (Western) Easter:
var gregorian = new GregorianEasterSundayNotableDateProvider();
IReadOnlyList<NotableDate> easterDates = gregorian.GetDates(2026);
Console.WriteLine(easterDates[0].Date); // 2026-04-05

// Orthodox Easter (Julian projection to Gregorian):
var orthodox = new OrthodoxEasterSundayNotableDateProvider();
IReadOnlyList<NotableDate> orthDates = orthodox.GetDates(2026);
Console.WriteLine(orthDates[0].Date); // 2026-04-12
```

## Qingming (solar term)

Qingming falls on the solar term 15° after the Spring Equinox — typically 4 or 5 April. `QingmingNotableDateAlgorithm` implements `INotableDateAlgorithm` and can be called directly:

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var qingming = new QingmingNotableDateAlgorithm();
DateTime? date = qingming.GetDate(2026);
Console.WriteLine(date); // 2026-04-04
```

## Implementing a custom algorithm

Implement `INotableDateAlgorithm` to add a calculation not covered by the built-in set. The single method `GetDate` receives the target year and an optional calendar system, and returns a `DateTime?` (`null` when the date cannot be determined for that year):

```csharp
using Bodu.Globalization.Calendar;

// Mother's Day: second Sunday in May.
public sealed class MothersDayAlgorithm : INotableDateAlgorithm
{
    public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null)
    {
        DateTime firstOfMay = new DateTime(year, 5, 1);
        int daysToFirstSunday = ((int)DayOfWeek.Sunday - (int)firstOfMay.DayOfWeek + 7) % 7;
        return firstOfMay.AddDays(daysToFirstSunday + 7);
    }
}
```

Register it and wire up a rule:

```csharp
using Bodu.Globalization.Calendar;

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
    options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
```

## Algorithms as anchors

Many holidays are not themselves algorithmic but are defined as a fixed offset from one. The Easter cluster is the canonical case — Good Friday, Easter Monday, Whit Monday, and Pentecost are all *Easter Sunday plus or minus N days* — and the library models this with the **algorithm-as-anchor** pattern:

1. Define one rule whose strategy is `Algorithm` (or any other strategy that produces a useful base date). Give it a `Name`.
2. Define each dependent rule with strategy `OffsetFromAnchor`, set `AnchorRuleName` to the algorithm rule's name, and set `OffsetDays` to the signed day-offset.

The pipeline resolves the algorithm rule first, then feeds its resolved date into each offset rule. The algorithm runs only once per year per service, regardless of how many offset rules consume it.

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

// Anchor — Easter Sunday, computed by the algorithm registry.
NotableDateRule easterSunday = new NotableDateRule
{
    Name            = "Easter Sunday",
    Strategy        = DateResolutionStrategy.Algorithm,
    Category        = NotableDateCategory.Holiday,
    AlgorithmKey    = "easter-sunday",
    IsNonWorkingDay = true,
};

// Dependents — fixed offsets from the anchor.
NotableDateRule goodFriday = new NotableDateRule
{
    Name            = "Good Friday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = -2,
    IsNonWorkingDay = true,
};

NotableDateRule whitMonday = new NotableDateRule
{
    Name            = "Whit Monday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = 50,
    IsNonWorkingDay = true,
};
```

Anchors are not limited to algorithms — any resolved rule with a `Name` can serve as one. Use this when a date naturally derives from another (e.g. a custom *Founders Week Friday* defined as *Founders Day + 2*) or when authoring tests that compose related dates from a single fixture rule.

For the strategy contract see [NotableDateRule and ObservanceAdjustment reference](rule-reference.md); for resolution ordering and cycle detection see [The resolution pipeline](resolution-pipeline.md).

## Where to go next

- [Core concepts](../../docs/calendar/concepts.md) — vocabulary used across this guide.
- [Using NotableDateService](notable-dates.md) — loading rules, override layers, and caching.
- [Authoring notable date rules](rule-authoring.md) — in-code objects, XML / JSON resource files, satellite assemblies, and runtime overrides.
- [NotableDateRule and ObservanceAdjustment reference](rule-reference.md) — field-by-field reference, including the strategy contract.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full type reference.
