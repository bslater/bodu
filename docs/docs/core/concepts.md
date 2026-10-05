---
title: Bodu.Core - Core concepts
---

# Bodu.Core - Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/core/index.md), and refer back whenever a term feels imprecise.

Part of the **[Core Foundations](../topics/core-foundations.md)** topic.

For the high-level shape of the library and the namespace map, start with the [introduction](index.md). The collection vocabulary - fixed capacity, ring backing, eviction policies, navigation, sketches - lives on the [Bodu.Collections concepts page](../collections/concepts.md), and the concurrency vocabulary on the [Bodu.Collections.Concurrent concepts page](../collections-concurrent/concepts.md).

## Calendar value sets

A **calendar value set** is an immutable `readonly struct` holding the values a schedule selects from one calendar field, one bit per value. <xref:Bodu.DayOfWeekSet> holds days of the week; <xref:Bodu.MonthSet>, <xref:Bodu.DayOfMonthSet>, <xref:Bodu.HourSet>, <xref:Bodu.MinuteSet>, and <xref:Bodu.SecondSet> hold months, days of the month, hours, minutes, and seconds. Each supports:

- **Composition** - `With` and `Without` return new sets; the operators `|`, `&`, `^`, `~` compose union, intersection, symmetric difference, and the complement within the field's range.
- **Membership** - `Contains` answers `false` for a value outside the field's range rather than throwing, so testing a `DateTime` component is a comparison and a bit test.
- **Text** - `DayOfWeekSet.Parse("_MTWTF_")` produces the weekday set; its text form is a seven-character mask of day initials in Sunday-first order (Monday-first is auto-detected, or forced with `ParseExact(input, "M")`), with `_` for absent days, or seven `0`/`1` bits. The numeric sets read and write a list of values and ranges, such as `"1-3,12"`.
- **Enumeration** - each implements `IEnumerable<T>`, yielding the selected values in ascending order (`DayOfWeek` order for days) through a struct enumerator that does not allocate.

Presets `DayOfWeekSet.Empty`, `DayOfWeekSet.Weekdays`, `DayOfWeekSet.Weekend`, and `DayOfWeekSet.All` cover the common cases, and the regional working-week presets (`MondayToFriday`, `MondayToSaturday`, `SundayToThursday`, `SaturdayToWednesday`, …) mirror the members of <xref:Bodu.WorkingDaysOfWeek>, the companion enum naming the common working weeks; conversion to and from `DayOfWeekSet` is via the extension methods on <xref:Bodu.Extensions.WorkingDaysOfWeekExtensions>. `DayOfWeekSet` replaced `WeekPattern` in Bodu.Core 1.3.0; the [DayOfWeekSet guide](../../guides/core/day-of-week-set.md#migrating-from-weekpattern) maps each removed member to its replacement.

## Pooled buffer

A **pooled buffer** is a writer-style builder backed by <xref:System.Buffers.ArrayPool`1> rather than `new T[]`. <xref:Bodu.Buffers.PooledBufferBuilder`1> rents an array on construction, grows it by re-renting when needed, exposes `WrittenSpan` for zero-copy reads, and returns the array to the pool on `Dispose`.

Ownership semantics:

- The builder owns the rented array; callers must call `Dispose` (or use `using`) to return it.
- `WrittenSpan` and `WrittenMemory` are valid only while the builder is alive. Copy out before disposal if the data must outlive the `using` block.
- For reference-typed `T`, the live portion is cleared before the array is returned, preventing unintended object retention.
- `Reset` discards the accumulated content and reuses the current rented buffer without a pool round-trip.

The builder implements <xref:System.Buffers.IBufferWriter`1> and <xref:System.Buffers.IMemoryOwner`1>, so it slots into standard `Span<T>` / `Memory<T>` pipelines.

## Railway-oriented outcomes

The `Bodu.Functional` namespace models "this might not produce a value" as data rather than control flow. Three value types cover the three shapes of the problem:

- <xref:Bodu.Functional.Option`1> - a value that may be absent: `Some(value)` or `None`. Reach for it when absence is *normal* and carries no explanation (a cache miss, an optional setting).
- <xref:Bodu.Functional.Result> / <xref:Bodu.Functional.Result`1> - success or failure, where failure carries a <xref:Bodu.Functional.ResultError> (optional code, never-null message, optional captured exception). Reach for it when the caller needs to know *why* an operation failed without paying for a thrown exception.
- <xref:Bodu.Functional.Either`2> - a symmetric disjoint union of two equally valid alternatives, with no success/failure bias.

The **railway** style chains these through combinators instead of `if`/`throw` ladders: `Map` transforms the value on the success track, `Bind` sequences a fallible step, `Filter` demotes a `Some` to `None`, and `Match` forces both tracks to be handled at the exit. Task-based companions (`MapAsync` / `BindAsync` / `MatchAsync` / `TapAsync` on <xref:Bodu.Functional.OptionAsyncExtensions> and <xref:Bodu.Functional.ResultAsyncExtensions>) keep the chain flowing through async steps.

Because all three are `readonly struct`s, each defines its **default contract** - what an unassigned field means:

| Type | `default` means |
|---|---|
| `Option<T>` | `None` - absence, safely. |
| `Result<T>` | A **failure** carrying an empty error - never a phantom success. |
| `Either<TLeft,TRight>` | An explicit **uninitialized** state - neither side is fabricated. |

## Natural ordering

Ordinal string comparison sorts `file10` before `file2` because `'1' < '2'` - correct for code, wrong for humans. **Natural ordering** compares embedded digit runs *numerically* while comparing the surrounding text as text, so `file2` sorts before `file10`.

<xref:Bodu.Extensions.NaturalStringComparer> implements this as a standard `IComparer<string>` with ordinal, case-insensitive, and culture-aware modes, so it drops into `OrderBy`, `SortedSet<string>`, `Array.Sort`, or any API that accepts a comparer. Its companions <xref:Bodu.Extensions.ComparableExtensions> and <xref:Bodu.Extensions.ComparableHelper> round out ordering ergonomics for any `IComparable<T>` - `Min`, `Max`, `Clamp`, and readable comparison predicates (`IsGreaterThan`, `IsGreaterThanOrEqual`). See the [Natural string comparer](../../guides/core/natural-string-comparer.md) guide.

## Async coordination

The `Bodu.Threading` namespace provides the **async-friendly peers of the BCL synchronization types**: where `lock`, `Monitor`, and `ManualResetEvent` block a thread, these primitives return awaitables, so a waiting caller yields its thread back to the pool.

- **Mutual exclusion and gating** - <xref:Bodu.Threading.AsyncLock> (an awaitable mutex whose `LockAsync` result releases on dispose), <xref:Bodu.Threading.AsyncSemaphore> (bounded concurrency), and <xref:Bodu.Threading.AsyncReaderWriterLock> (many readers / one writer).
- **Signalling** - <xref:Bodu.Threading.AsyncManualResetEvent>, <xref:Bodu.Threading.AsyncAutoResetEvent>, and <xref:Bodu.Threading.AsyncCountdownEvent> re-express the BCL event types as awaitables.
- **Flow shaping** - <xref:Bodu.Threading.AsyncLazy`1> (one-time async initialization with a single shared invocation), <xref:Bodu.Threading.AsyncDebouncer> (trailing-edge coalescing of bursts), and <xref:Bodu.Threading.RateGate> (N-operations-per-window rate limiting).

The common contract: waiting never blocks a thread, cancellation is honoured through `CancellationToken` parameters, and lock releases are scoped by `IDisposable` so a `using` guarantees the release path. See the [Async coordination primitives](../../guides/core/async-primitives.md) guide.

## ThrowHelper

<xref:Bodu.ThrowHelper> centralises the `ArgumentException` family for the entire Bodu suite - every Bodu library calls into it rather than rolling its own checks. The pattern is:

```csharp
public static double Average(IReadOnlyList<int> values)
{
    ThrowHelper.ThrowIfNull(values);
    ThrowHelper.ThrowIfZero(values.Count);
    return values.Average();
}
```

The helpers are partitioned by concern across partial files: `Null`, `Numeric`, `Comparison`, `Equality`, `Array`, `Collection`, `Span`, `Type`, `String`, `Ascii`. Each helper accepts a `[CallerArgumentExpression(nameof(value))] string? paramName = null` parameter, so the call site does not need to repeat the argument name. Two parallel partial-class sets target the modern (.NET 8) and netstandard runtimes; the public surface is identical across both.

Centralising the guards means the exception messages, parameter-name capture, and `[StackTraceHidden]` behaviour stay consistent across every Bodu library - and `ThrowHelper` is the primary dependency the other Bodu packages take on `Bodu.Core`.

## Random generator abstraction

Several extension helpers - shuffles, sampling, randomised access - and the `Bodu.Collections` catalogue depend on a pluggable random source rather than a hard-coded <xref:System.Random>. The seam is <xref:Bodu.IRandomGenerator>, a one-method interface (`int Next(int maxValue)`).

Two implementations ship in `Bodu.Core`:

| Implementation | Use when |
|---|---|
| <xref:Bodu.XorShiftRandom> | Tight inner loops, reproducible tests, or shuffle code where the BCL's `Random` is the measured bottleneck. Subclasses `System.Random` and implements `IRandomGenerator`. |
| <xref:Bodu.Collections.Generic.Extensions.SystemRandomAdapter> | An existing `System.Random` instance must be reused - for example a seeded `Random` shared across multiple helpers. Wraps the `Random` and forwards calls. |

Neither implementation is cryptographically secure; both are deterministic given a seed. Callers that need cryptographic randomness should use <xref:System.Security.Cryptography.RandomNumberGenerator> directly. Helpers that accept an `IRandomGenerator` make the dependency explicit so tests can supply a deterministic generator without monkey-patching globals.

## Calendar-shape extensions

The `Bodu.Extensions` namespace ships several enums that encode the *shape* of a calendar without implementing one - they parameterise the date arithmetic on <xref:Bodu.Extensions.DateTimeExtensions> and <xref:Bodu.Extensions.DateOnlyExtensions> without pulling in `Bodu.Globalization.Calendar`:

| Type | Encodes |
|---|---|
| <xref:Bodu.Extensions.CalendarQuarterDefinition> | The start month of Q1 - `JanuaryToDecember`, `JulyToJune`, `AprilToMarch`, `April6ToApril5` (UK tax year), `March25ToMarch24` (Lady Day), `OctoberToSeptember`, `FebruaryToJanuary`, plus `Custom` for externally defined rules. Drives `FirstDateOfQuarter`, `LastDateOfQuarter`, `Quarter`, `FiscalYear`. |
| <xref:Bodu.Extensions.WeekOrdinal> | The ordinal position of a weekday in a month - `First`, `Second`, `Third`, `Fourth`, `Fifth`, `Last`. Drives `NthDateOfWeekInMonth` and the recurrence-rule patterns. |
| <xref:Bodu.Extensions.FiscalWeekPattern> | The 4-4-5 / 4-5-4 / 5-4-4 split for retail-style 13-week fiscal quarters. Each quarter is always 13 weeks; the pattern only affects fiscal-period boundaries inside the quarter. The extra week in a 53-week fiscal year is always appended to the final period of Q4. |
| <xref:Bodu.WorkingDaysOfWeek> | Named working-week presets - `MondayToFriday`, `MondayToSaturday`, `SaturdayToThursday`, `SaturdayToWednesday`, and others - plus `Custom` for caller-supplied `DayOfWeekSet` schedules. Drives `IsWeekday`, `IsWeekend`, `IsInWorkingWeek`, `NextWeekday`. |
| <xref:Bodu.Extensions.IWeekendDefinitionProvider> | The injection seam for non-enumerable weekend rules - pass an implementation to override the built-in `WorkingDaysOfWeek` presets when an application needs hybrid, rotating, or domain-specific weekend logic. |

These types are pure data carriers. The actual algorithms live on the extension classes (`DateTimeExtensions.*`, `DateOnlyExtensions.*`) so the same shapes can be passed to a `DateTime`, `DateOnly`, or any future date type without duplicating the enums.

## Where to go next

- **[Introduction](index.md)** - the namespace map and headline types.
- **[Getting started](getting-started.md)** - install + runnable minimal samples for each scenario.
- **[Core Foundations guides](../../guides/core/index.md)** - recipe-style walk-throughs for the headline types.
- **[Bodu.Collections concepts](../collections/concepts.md)** - the collection vocabulary (fixed capacity, eviction, navigation, sketches).
- **[Bodu.Collections.Concurrent concepts](../collections-concurrent/concepts.md)** - the concurrency vocabulary for the thread-safe collections.
- **[Core Foundations topic](../topics/core-foundations.md)** - Bodu.Core alongside the collection packages and the `Bodu.Text` namespace utilities; the [topic concepts](../topics/core-foundations-concepts.md) page collects the shared vocabulary.
