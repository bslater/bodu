---
title: Date calculation algorithms
---

# Date calculation algorithms

Several notable dates cannot be expressed as a fixed month/day or an *n*th weekday-of-month — their position in the calendar depends on astronomical or ecclesiastical calculations. **Bodu.Globalization.Calendar** ships built-in implementations for the most common cases.

## Built-in algorithms

| Algorithm class | Date it computes | Standard / basis |
|---|---|---|
| `GregorianEasterSundayNotableDateProvider` | Easter Sunday (Western) | Anonymous Gregorian algorithm (Computus) |
| `OrthodoxEasterSundayNotableDateProvider` | Easter Sunday (Eastern Orthodox) | Julian calendar Computus, projected to Gregorian |
| `HinduLunarNotableDateAlgorithm` | Hindu lunar calendar dates (Diwali, Holi, …) | Astronomical Hindu calendar |
| `LosarNotableDateAlgorithm` | Losar (Tibetan New Year) | Tibetan lunisolar calendar |
| `VesakNotableDateAlgorithm` | Vesak (Buddha's birthday) | Sri Lanka / Theravāda full-moon calculation |
| `AsalhaPujaNotableDateAlgorithm` | Asalha Puja (Dhamma Day) | Full moon of the 8th lunar month |
| `QingmingNotableDateAlgorithm` | Qingming (Tomb-Sweeping Day) | Solar term — 15° after Spring Equinox |

## Using an algorithm in a rule

Rules that depend on a calculation algorithm set `DateResolutionStrategy.Algorithm`:
The following classes implement `INotableDateAlgorithm` and can be registered in a `NotableDateAlgorithmRegistry` for use by rules with `Strategy = DateResolutionStrategy.Algorithm`:

| Class | Date it computes | Notes |
|---|---|---|
| `EasterSundayNotableDateAlgorithm` | Easter Sunday | Gregorian computus for years ≥ 1583; Julian computus otherwise. |
| `HinduLunarNotableDateAlgorithm` | Hindu lunar festivals (Diwali, Holi, …) | Approximate Gregorian projection of a Hindu panchanga date. |
| `LosarNotableDateAlgorithm` | Losar (Tibetan New Year) | Tibetan lunisolar calculation. |
| `VesakNotableDateAlgorithm` | Vesak (Buddha's birthday) | Full-moon calculation per Theravāda tradition. |
| `AsalhaPujaNotableDateAlgorithm` | Asalha Puja (Dhamma Day) | Full moon of the 8th lunar month. |
| `QingmingNotableDateAlgorithm` | Qingming (Tomb-Sweeping Day) | Solar term 15° after the Spring Equinox (typically 4–5 April). |

The algorithm namespace also contains two `INotableDateProvider` implementations — `GregorianEasterSundayNotableDateProvider` and `OrthodoxEasterSundayNotableDateProvider` — which can be used directly when you want to compute Easter dates outside the rule-resolution pipeline.

## Registering an algorithm

Rules reference algorithms by string key via `NotableDateRule.AlgorithmKey`. Register the corresponding instance in a `NotableDateAlgorithmRegistry` and supply it to `NotableDateService`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

// Register the Easter algorithm explicitly.
var registry = new NotableDateAlgorithmRegistry();
registry.Register<EasterSundayNotableDateAlgorithm>("EasterSunday");

// Build a rule that uses it.
var goodFriday = new NotableDateRule
{
    Name     = "Good Friday",
    Category = NotableDateCategory.PublicHoliday,
    Strategy = new DateResolutionStrategy
    {
        Kind           = DateResolutionKind.OffsetFromAnchor,
        AnchorRuleName = "Easter Sunday",
        DayOffset      = -2,                // 2 days before Easter
    },
};
```

Most built-in algorithm rules are already defined in the embedded global XML rule set and do not need to be registered manually.

## Easter Sunday

Gregorian Easter (Western):

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var easter = new GregorianEasterSundayNotableDateProvider();
DateOnly easterDate = easter.ComputeEasterSunday(2025);
Console.WriteLine(easterDate); // 2025-04-20
```

Orthodox Easter (Julian projection):

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var orthodox = new OrthodoxEasterSundayNotableDateProvider();
DateOnly orthDate = orthodox.ComputeEasterSunday(2025);
Console.WriteLine(orthDate); // 2025-04-20 (coincides in 2025)
// Build a registry with fluent chaining:
NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("easter-sunday", new EasterSundayNotableDateAlgorithm())
    .Register("qingming",      new QingmingNotableDateAlgorithm());

var service = new NotableDateService(
    ruleProviders:     new[] { new XmlResourceNotableDateRuleProvider(
                           "MyApp/Calendar/Resources/rules.xml",
                           new ResourcePathResolver()) },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    algorithmRegistry: registry);
```

Most built-in algorithm rules are already defined in the embedded global XML rule set and do not need to be registered manually when using the default `NotableDateService()` constructor.

## Using an algorithm in a rule

Rules that require an algorithm set `Strategy = DateResolutionStrategy.Algorithm` and identify the algorithm via `AlgorithmKey`:

```csharp
using Bodu.Globalization.Calendar;

// Easter Sunday — resolved via the registered "easter-sunday" algorithm.
NotableDateRule easterSunday = new NotableDateRule
{
    Name         = "Easter Sunday",
    Strategy     = DateResolutionStrategy.Algorithm,
    Category     = NotableDateCategory.Holiday,
    AlgorithmKey = "easter-sunday",
    IsNonWorkingDay = true,
};

// Easter Monday — offset 1 day from Easter Sunday.
NotableDateRule easterMonday = new NotableDateRule
{
    Name           = "Easter Monday",
    Strategy       = DateResolutionStrategy.OffsetFromAnchor,
    Category       = NotableDateCategory.Holiday,
    AnchorRuleName = "Easter Sunday",
    OffsetDays     = 1,
    IsNonWorkingDay = true,
};

// Good Friday — 2 days before Easter Sunday.
NotableDateRule goodFriday = new NotableDateRule
{
    Name           = "Good Friday",
    Strategy       = DateResolutionStrategy.OffsetFromAnchor,
    Category       = NotableDateCategory.Holiday,
    AnchorRuleName = "Easter Sunday",
    OffsetDays     = -2,
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

Qingming falls on the solar term 15° after the Spring Equinox — typically 4 or 5 April:
`QingmingNotableDateAlgorithm` implements `INotableDateAlgorithm` and can be called directly:

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var qingming = new QingmingNotableDateAlgorithm();
DateOnly date = qingming.Compute(2025);
Console.WriteLine(date); // 2025-04-04
DateTime? date = qingming.GetDate(2026);
Console.WriteLine(date); // 2026-04-04
```

## Implementing a custom algorithm

Implement `INotableDateAlgorithm` to add a calculation not covered by the built-in set:
Implement `INotableDateAlgorithm` to add a calculation not covered by the built-in set. The single method `GetDate` receives the target year and an optional calendar system, and returns a `DateTime?` (`null` when the date cannot be determined for that year):

```csharp
using Bodu.Globalization.Calendar;

public sealed class MothersDay : INotableDateAlgorithm
{
    // Second Sunday in May.
    public DateOnly Compute(int year)
    {
        DateOnly firstOfMay = new DateOnly(year, 5, 1);
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

Register it with the service:
Register it and wire up a rule:

```csharp
using Bodu.Globalization.Calendar;

var registry = new NotableDateAlgorithmRegistry();
registry.Register<MothersDay>("MothersDay");

var service = new NotableDateService(
    ruleProviders:     [myProvider],
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
    ruleProviders:     new[] { new InMemoryRuleProvider(new[] { mothersDay }) },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    algorithmRegistry: registry);
```

## Where to go next

- [Using NotableDateService](notable-dates.md) — loading rules, override layers, and caching.
- [Authoring notable date rules](rule-authoring.md) — in-code objects, XML resource files, satellite assemblies, and runtime overrides.
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full type reference.
