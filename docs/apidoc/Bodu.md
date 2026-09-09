---
uid: Bodu
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

The root **Bodu** namespace holds the cross-cutting primitives that the rest of `Bodu.Core` — and the wider solution — build on: a day-of-week bitmask value type, the centralized argument-validation helper, and a small pluggable random-number abstraction. These types are namespace-root on purpose: they are used everywhere and carry no sub-domain of their own.

## Static documentation

- **[Bodu.Core introduction](~/docs/core/index.md)** — namespaces, headline types, scenarios.
- **[`WeekPattern` guide](~/guides/core/week-pattern.md)** — composing, parsing, and enumerating day-of-week sets.

## Key types

- <xref:Bodu.WeekPattern> — an immutable bitmask value type for sets of days of the week. Built from `DayOfWeek` values (`new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday)`, `With` / `Without`) or taken from the named presets (`Weekdays`, `Weekend`, `AllDays`, `Empty`, and the regional working weeks such as `SundayToThursday`); supports bitwise composition (`WeekPattern.Weekdays | WeekPattern.Weekend`), parsing, formatting, and enumeration. See the [`WeekPattern` guide](~/guides/core/week-pattern.md).
- <xref:Bodu.WorkingDaysOfWeek> — a `[Flags]` enum naming the conventional working-day sets used by the date and calendar extensions.
- <xref:Bodu.IRandomGenerator> — a minimal abstraction over a random source, so algorithms (shuffles, sampling) can be tested deterministically or swapped between PRNG implementations.
- <xref:Bodu.XorShiftRandom> — a fast xorshift PRNG that derives from `System.Random`; seedable for reproducible sequences. Drop-in where `Random` is expected, faster where throughput matters and cryptographic strength is *not* required.
- <xref:Bodu.ThrowHelper> — the centralized argument-validation surface (`ThrowIfNull`, `ThrowIfLessThan`, `ThrowIfGreaterThan`, span/array offset checks, enum-defined checks, …). Public APIs across the solution validate their parameters through these `ThrowIf…` members rather than hand-rolled checks.

## Example

```csharp
using Bodu;

// Compose and test a day-of-week set.
WeekPattern weekdays = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                       DayOfWeek.Thursday, DayOfWeek.Friday);   // same set as WeekPattern.Weekdays
WeekPattern fourDay  = weekdays.Without(DayOfWeek.Friday);
bool worksSaturday   = weekdays.Contains(DayOfWeek.Saturday);   // false

// Deterministic randomness for reproducible tests.
IRandomGenerator rng = new XorShiftRandom(seed: 12345);
int roll = rng.Next(6) + 1;   // Next(maxValue) yields [0, maxValue) — here a die roll of 1–6
```

## Notes

- **`XorShiftRandom` is not cryptographically secure.** Use `System.Security.Cryptography.RandomNumberGenerator` for security-sensitive randomness.
- **Validate through `ThrowHelper`.** When contributing public APIs, prefer an existing `ThrowIf…` helper over a hand-written check; add a new helper only when the rule is general-purpose.
- **See also:** the [`WeekPattern` guide](~/guides/core/week-pattern.md) and the [Bodu.Collections.Generic overview](xref:Bodu.Collections.Generic).
