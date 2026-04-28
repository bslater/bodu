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
```

## Qingming (solar term)

Qingming falls on the solar term 15° after the Spring Equinox — typically 4 or 5 April:

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var qingming = new QingmingNotableDateAlgorithm();
DateOnly date = qingming.Compute(2025);
Console.WriteLine(date); // 2025-04-04
```

## Implementing a custom algorithm

Implement `INotableDateAlgorithm` to add a calculation not covered by the built-in set:

```csharp
using Bodu.Globalization.Calendar;

public sealed class MothersDay : INotableDateAlgorithm
{
    // Second Sunday in May.
    public DateOnly Compute(int year)
    {
        DateOnly firstOfMay = new DateOnly(year, 5, 1);
        int daysToFirstSunday = ((int)DayOfWeek.Sunday - (int)firstOfMay.DayOfWeek + 7) % 7;
        return firstOfMay.AddDays(daysToFirstSunday + 7);
    }
}
```

Register it with the service:

```csharp
using Bodu.Globalization.Calendar;

var registry = new NotableDateAlgorithmRegistry();
registry.Register<MothersDay>("MothersDay");

var service = new NotableDateService(
    ruleProviders:     [myProvider],
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    algorithmRegistry: registry);
```

## Where to go next

- [Using NotableDateService](notable-dates.md) — loading rules, override layers, and caching.
- [Bodu.Globalization.Calendar API reference](../../apidoc/Bodu.Globalization.Calendar.md) — full type reference.
