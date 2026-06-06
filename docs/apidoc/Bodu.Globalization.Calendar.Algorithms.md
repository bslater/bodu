---
uid: Bodu.Globalization.Calendar.Algorithms
---

# Bodu.Globalization.Calendar.Algorithms

## Purpose

**Bodu.Globalization.Calendar.Algorithms** is the date-calculation layer of [`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md). It defines the strategy a rule uses to find its *nominal* date for a year, and the contract and registry for plugging in custom calculators.

Every <xref:Bodu.Globalization.Calendar.NotableDateRule> carries exactly one <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy>. The loader maps each `<Strategy>` element in a rule document to one of the built-in strategies below; you rarely construct them by hand, but they are the public vocabulary the engine resolves against.

## Static documentation

- **[Date calculation algorithms](~/guides/calendar/algorithms.md)** — the strategy kinds, the built-in algorithm keys, and how to register a custom algorithm.
- **[Working with non-Gregorian calendars](~/guides/calendar/non-gregorian-calendars.md)** — fixed dates expressed in Hijri / Hebrew / Persian / Chinese lunisolar calendars.

## Key types

**The strategy contract and built-in strategies**

- <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy> — `DateOnly? Calculate(int year, StrategyResolutionContext context)`. Implemented by every strategy below.
- <xref:Bodu.Globalization.Calendar.Algorithms.FixedDateStrategy> — a fixed month / day, optionally expressed in a non-Gregorian <xref:Bodu.Globalization.Calendar.CalendarSystem> (a short Hijri month can recur twice in a Gregorian year, so it also exposes `CalculateAll`).
- <xref:Bodu.Globalization.Calendar.Algorithms.DayOfWeekInMonthStrategy> — the *n*th or last weekday in a month (e.g. fourth Thursday in November), driven by <xref:Bodu.Extensions.WeekOrdinal>.
- <xref:Bodu.Globalization.Calendar.Algorithms.RelativeWeekdayInMonthStrategy> — a weekday relative to a weekday-in-month anchor (e.g. the Tuesday after the first Monday).
- <xref:Bodu.Globalization.Calendar.Algorithms.WeekdayNearDateStrategy> — a weekday on / before / after / nearest a fixed date (e.g. the Monday nearest 24 May), driven by <xref:Bodu.Globalization.Calendar.WeekdayProximity>.
- <xref:Bodu.Globalization.Calendar.Algorithms.OffsetFromRuleStrategy> — a fixed day offset from another rule's occurrence (e.g. Good Friday = Easter Sunday − 2).
- <xref:Bodu.Globalization.Calendar.Algorithms.AlgorithmDateStrategy> — dispatch to a named algorithm by key.

**Algorithm keys**

<xref:Bodu.Globalization.Calendar.Algorithms.AlgorithmDateStrategy> resolves a string key to a bundled astronomical / gazetted calculator. Built-in keys include `western-easter` and `orthodox-easter` (exposed as the `WesternEasterKey` / `OrthodoxEasterKey` constants), `vernal-equinox`, `autumnal-equinox`, `jp-vernal-equinox`, `jp-autumnal-equinox`, `qingming`, `vesak`, `asalha-puja`, `losar`, `matariki`, and the Hindu-festival keys (`diwali`, `holi`, `maha-shivaratri`, `ganesh-chaturthi`, …). `AlgorithmDateStrategy.IsKnownKey(key)` reports whether a key is built in. The calculators that back these keys are an internal implementation detail reached only through the key.

**Custom algorithms**

- <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> — `DateOnly? Calculate(int year)`. Implement this to add a calculator (returning `null` for years you do not support).
- <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithmRegistry>, <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> — the lookup and its chainable, mutable implementation (`Register(key, algorithm)`). Keys not recognised by `AlgorithmDateStrategy` fall through to this registry, so a custom key registered here can be referenced from a rule as `<Algorithm key="my-key" />`. Pass the registry to `NotableDateResourceLoader.Load(xml, resolver, registry)` (to whitelist the key during validation) and to the `NotableDateService` constructor.
- <xref:Bodu.Globalization.Calendar.Algorithms.StrategyResolutionContext> — the per-resolution context passed to strategies; resolves referenced rules (`ResolveReference`) cycle-safely and carries the custom algorithm registry.

## Minimal sample

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

// A custom calculator …
public sealed class PiDayAlgorithm : INotableDateAlgorithm
{
    public DateOnly? Calculate(int year) => new DateOnly(year, 3, 14);
}

// … registered under a key the rule document references via <Algorithm key="pi-day" />.
var registry = new NotableDateAlgorithmRegistry().Register("pi-day", new PiDayAlgorithm());

NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null, registry);
NotableDateService service   = new NotableDateService(resource, registry);
```
