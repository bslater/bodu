---
uid: Bodu
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

The root **Bodu** namespace holds the cross-cutting primitives that the rest of `Bodu.Core` - and the wider solution - build on: the calendar value sets, the centralized argument-validation helper, and a small pluggable random-number abstraction. These types are namespace-root on purpose: they are used everywhere and carry no sub-domain of their own.

## Static documentation

- **[Bodu.Core introduction](~/docs/core/index.md)** - namespaces, headline types, scenarios.
- **[`DayOfWeekSet` guide](~/guides/core/day-of-week-set.md)** - composing, parsing, and enumerating sets of days of the week, and migrating from `WeekPattern`.
- **[Calendar value sets guide](~/guides/core/calendar-value-sets.md)** - the sets of months, days of the month, hours, minutes, and seconds.

## Key types

- <xref:Bodu.DayOfWeekSet> - an immutable set of days of the week, held in one byte. Built from `DayOfWeek` values (`new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday)`, `With` / `Without`) or taken from the named presets (`Weekdays`, `Weekend`, `All`, `Empty`, and the regional working weeks such as `SundayToThursday`); supports the set operators (`DayOfWeekSet.Weekdays | DayOfWeekSet.Weekend`), parsing, formatting, and enumeration. It replaces `WeekPattern`, which Bodu.Core 1.3.0 removed. See the [`DayOfWeekSet` guide](~/guides/core/day-of-week-set.md).
- <xref:Bodu.MonthSet>, <xref:Bodu.DayOfMonthSet>, <xref:Bodu.HourSet>, <xref:Bodu.MinuteSet>, <xref:Bodu.SecondSet> - the same shape over months 1-12, days of the month 1-31, hours 0-23, minutes 0-59, and seconds 0-59, written as a list of values and ranges (`"1-3,12"`). See the [calendar value sets guide](~/guides/core/calendar-value-sets.md).
- <xref:Bodu.WorkingDaysOfWeek> - an enum naming the common working weeks used by the date and calendar extensions, one member per week (`MondayToFriday`, `SundayToThursday`, ...), with `Custom` for a week supplied some other way. It is not a flags enum: its members are not combined, and a set of days is a <xref:Bodu.DayOfWeekSet>.
- <xref:Bodu.IRandomGenerator> - a minimal abstraction over a random source, so algorithms (shuffles, sampling) can be tested deterministically or swapped between PRNG implementations.
- <xref:Bodu.XorShiftRandom> - a fast xorshift PRNG that derives from `System.Random`; seedable for reproducible sequences. Drop-in where `Random` is expected, faster where throughput matters and cryptographic strength is *not* required.
- <xref:Bodu.ThrowHelper> - the centralized argument-validation surface (`ThrowIfNull`, `ThrowIfLessThan`, `ThrowIfGreaterThan`, span/array offset checks, enum-defined checks, …). Public APIs across the solution validate their parameters through these `ThrowIf…` members rather than hand-rolled checks.

## Example

```csharp
using Bodu;

// Compose and test a day-of-week set.
DayOfWeekSet weekdays = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                         DayOfWeek.Thursday, DayOfWeek.Friday);   // same set as DayOfWeekSet.Weekdays
DayOfWeekSet fourDay  = weekdays.Without(DayOfWeek.Friday);
bool worksSaturday    = weekdays.Contains(DayOfWeek.Saturday);   // false

// The other calendar fields have sets of their own.
MonthSet quarterEnds  = MonthSet.Parse("3,6,9,12");
bool isQuarterEnd     = quarterEnds.Contains(DateTime.Today.Month);

// Deterministic randomness for reproducible tests.
IRandomGenerator rng = new XorShiftRandom(seed: 12345);
int roll = rng.Next(6) + 1;   // Next(maxValue) yields [0, maxValue) - here a die roll of 1-6
```

## Notes

- **`XorShiftRandom` is not cryptographically secure.** Use `System.Security.Cryptography.RandomNumberGenerator` for security-sensitive randomness.
- **Validate through `ThrowHelper`.** When contributing public APIs, prefer an existing `ThrowIf…` helper over a hand-written check; add a new helper only when the rule is general-purpose.
- **See also:** the [`DayOfWeekSet` guide](~/guides/core/day-of-week-set.md), the [calendar value sets guide](~/guides/core/calendar-value-sets.md), and the [Bodu.Collections.Generic overview](xref:Bodu.Collections.Generic).
