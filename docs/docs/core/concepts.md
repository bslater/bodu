---
title: Bodu.Core — Core concepts
---

# Bodu.Core — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/core/index.md), and refer back whenever a term feels imprecise.

For the high-level shape of the library and the namespace map, start with the [introduction](index.md).

## Fixed-capacity collection

A **fixed-capacity** collection sizes its backing storage once and never grows. Once `Count` reaches `Capacity`, the collection has to choose between two behaviours: reject the next add, or evict an existing element to make room. Bodu's ring-backed types expose that choice as a single boolean toggle rather than two separate classes.

| Type | Toggle | Add when full |
|---|---|---|
| <xref:Bodu.Collections.Generic.CircularBuffer`1> | `AllowOverwrite` | `true` evicts the head; `false` throws <xref:System.InvalidOperationException>. |
| <xref:Bodu.Collections.Generic.Deque`1> | `AllowGrow` | `true` doubles the backing array; `false` throws <xref:System.InvalidOperationException>. |
| <xref:Bodu.Collections.Generic.EvictingDictionary`2> | (always evicting) | Removes the entry selected by the configured <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy>. |

The `Try…` variants (`TryEnqueue`, `TryAddFirst`, `TryAddLast`) substitute a `false` return for the throw, so callers can prefer non-throwing code paths without changing the toggle.

## Ring-backed collection

A **ring-backed** collection stores its elements in a single contiguous array with `head` and `tail` indices that wrap modulo `Capacity`. This gives O(1) add and remove at either end without shifting elements, at the cost of a fixed capacity (or, for growable variants, an occasional array-doubling pass).

<xref:Bodu.Collections.Generic.RingBackedCollection`1> is the shared abstract base. It owns the storage, the wrap arithmetic, the structural-version counter that powers fail-fast enumeration, and the protected primitives (`AddTail`, `AddHead`, `RemoveHead`, `RemoveTail`, `PeekHead`, `PeekTail`, `Resize`) that the concrete types build on. <xref:Bodu.Collections.Generic.CircularBuffer`1> layers a single-ended FIFO surface on top; <xref:Bodu.Collections.Generic.Deque`1> layers a double-ended surface. Both share enumeration, copy, indexer, and trim behaviour through the base.

Derived types skip capacity and emptiness checks on the protected mutators — the public surface enforces those contracts before calling — so the hot path stays branch-free.

## Bounded vs. growing

The `AllowGrow` flag on <xref:Bodu.Collections.Generic.Deque`1> picks between two modes at runtime:

- **Growing** (`AllowGrow = true`, the default) — the backing array doubles on overflow, capped at <xref:System.Array.MaxLength>. The deque behaves like a `List<T>` with O(1) ends.
- **Bounded** (`AllowGrow = false`) — the deque is fixed at its current capacity. Overflow throws <xref:System.InvalidOperationException>; the `Try…` variants return `false` instead.

The toggle can be flipped at runtime — useful when a deque starts in growable mode during warm-up and is then locked down for steady-state. Switching to `false` does not shrink the backing array; call `TrimExcess` afterwards if a smaller footprint is wanted. `EnsureCapacity(int)` can pre-grow even when `AllowGrow` is `false`.

## Eviction policy

An <xref:Bodu.Collections.Generic.EvictingDictionary`2> is bounded by a capacity and an <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> that decides which entry leaves when a new key triggers overflow:

| Policy | Evicts |
|---|---|
| `FirstInFirstOut` | The entry that was added earliest, regardless of access. |
| `LeastRecentlyUsed` | The entry with the oldest last-access timestamp. |
| `LeastFrequentlyUsed` | The entry with the lowest cumulative access count. |
| `MostRecentlyUsed` | The entry with the newest last-access timestamp. |
| `RandomReplacement` | A uniformly randomly chosen entry. |
| `SecondChance` | A FIFO scan that skips entries flagged as recently accessed (the flag clears on skip), evicting the first unflagged entry. |

All policies share the same `IDictionary<TKey, TValue>` surface and the same overflow trigger; the policy only changes the selection.

## WeekPattern

<xref:Bodu.WeekPattern> is an immutable `readonly struct` that represents a set of days within a seven-day week, packed into a 7-bit bitmask. It supports:

- **Composition** — `With(DayOfWeek)` and `Without(DayOfWeek)` return new patterns; the bitwise operators `|`, `&`, `^`, `~` compose set union, intersection, symmetric difference, and complement.
- **Parsing** — `WeekPattern.Parse("MTuWThF")` produces the weekday set; the format uses two-letter abbreviations for Tuesday, Thursday, and Sunday to disambiguate. Presets `WeekPattern.Empty`, `WeekPattern.Weekdays`, `WeekPattern.Weekend`, and `WeekPattern.AllDays` cover the common cases.
- **Enumeration** — implements `IEnumerable<DayOfWeek>`, yielding selected days in `DayOfWeek` order.

Because the receiver is a value type, every mutation returns a new instance. <xref:Bodu.WorkingDaysOfWeek> is the companion enum naming the common working-week patterns; conversion to and from `WeekPattern` is via the extension methods on <xref:Bodu.Extensions.WorkingDaysOfWeekExtensions>.

## Pooled buffer

A **pooled buffer** is a writer-style builder backed by <xref:System.Buffers.ArrayPool`1> rather than `new T[]`. <xref:Bodu.Buffers.PooledBufferBuilder`1> rents an array on construction, grows it by re-renting when needed, exposes `WrittenSpan` for zero-copy reads, and returns the array to the pool on `Dispose`.

Ownership semantics:

- The builder owns the rented array; callers must call `Dispose` (or use `using`) to return it.
- `WrittenSpan` and `WrittenMemory` are valid only while the builder is alive. Copy out before disposal if the data must outlive the `using` block.
- For reference-typed `T`, the live portion is cleared before the array is returned, preventing unintended object retention.
- `Reset` discards the accumulated content and reuses the current rented buffer without a pool round-trip.

The builder implements <xref:System.Buffers.IBufferWriter`1> and <xref:System.Buffers.IMemoryOwner`1>, so it slots into standard `Span<T>` / `Memory<T>` pipelines.

## ThrowHelper

<xref:Bodu.ThrowHelper> centralises the `ArgumentException` family for the entire Bodu suite — every Bodu library calls into it rather than rolling its own checks. The pattern is:

```csharp
public static double Average(IReadOnlyList<int> values)
{
    ThrowHelper.ThrowIfNull(values);
    ThrowHelper.ThrowIfZero(values.Count);
    return values.Average();
}
```

The helpers are partitioned by concern across partial files: `Null`, `Numeric`, `Comparison`, `Equality`, `Array`, `Collection`, `Span`, `Type`, `String`, `Ascii`. Each helper accepts a `[CallerArgumentExpression(nameof(value))] string? paramName = null` parameter, so the call site does not need to repeat the argument name. Two parallel partial-class sets target the modern (.NET 8) and netstandard runtimes; the public surface is identical across both.

Centralising the guards means the exception messages, parameter-name capture, and `[StackTraceHidden]` behaviour stay consistent across every Bodu library — and `ThrowHelper` is the sole dependency the other Bodu packages take on `Bodu.Core`.

## Random generator abstraction

Several collection and extension helpers — shuffles, sampling, randomised access — depend on a pluggable random source rather than a hard-coded <xref:System.Random>. The seam is <xref:Bodu.IRandomGenerator>, a one-method interface (`int Next(int maxValue)`).

Two implementations ship in `Bodu.Core`:

| Implementation | Use when |
|---|---|
| <xref:Bodu.XorShiftRandom> | Tight inner loops, reproducible tests, or shuffle code where the BCL's `Random` is the measured bottleneck. Subclasses `System.Random` and implements `IRandomGenerator`. |
| <xref:Bodu.Collections.Generic.Extensions.SystemRandomAdapter> | An existing `System.Random` instance must be reused — for example a seeded `Random` shared across multiple helpers. Wraps the `Random` and forwards calls. |

Neither implementation is cryptographically secure; both are deterministic given a seed. Callers that need cryptographic randomness should use <xref:System.Security.Cryptography.RandomNumberGenerator> directly. Helpers that accept an `IRandomGenerator` make the dependency explicit so tests can supply a deterministic generator without monkey-patching globals.

## Multi-value and multi-set semantics

A **multi-** prefix in this library means *duplicate-aware*:

- <xref:Bodu.Collections.Generic.MultiValueDictionary`2> — sometimes called a multimap. A single key maps to zero or more values; values for the same key are retained in insertion order. `Count` is the total number of key-value entries; `KeyCount` is the number of distinct keys. The indexer returns a live read-only view that reflects later mutations to the same key, and an empty list (not `null`) when the key is absent.
- <xref:Bodu.Collections.Generic.Multiset`1> — a set that tracks the *multiplicity* of each element. `Count` includes multiplicity (`{a, a, b}` has count 3); `DistinctCount` does not. Set-theoretic operations (`Union`, `Intersect`, `Except`, `Sum`) return new multisets and follow multiset algebra — `Union` is element-wise `max(a, b)`, `Intersect` is `min(a, b)`, `Except` is `max(0, a − b)`, and `Sum` is `a + b`.

Both types are not thread-safe and require external synchronisation under concurrent mutation.

## Range-keyed lookup

A <xref:Bodu.Collections.Generic.Range`1> is an immutable half-open interval `[StartInclusive, EndExclusive)` over any `IComparable<T>` endpoint. The half-open convention matches .NET span slicing and <xref:System.Range>: adjacent ranges (`[0, 5)` followed by `[5, 10)`) abut without overlapping, which is the property the collection types rely on for internal consistency.

Two range-keyed collections build on it:

| Type | Backing | Behaviour on overlap |
|---|---|---|
| <xref:Bodu.Collections.Generic.RangeDictionary`2> | Sorted parallel arrays of start, end, and value | Rejects overlapping insertions with <xref:System.ArgumentException>. |
| <xref:Bodu.Collections.Generic.RangeSet`1> | Sorted parallel arrays of start and end | Merges adjacent and overlapping ranges on insertion. |

Both use binary search across the start endpoints for O(log n) lookup. The constructor of `Range<T>` validates that `start < end` and rejects degenerate or inverted ranges.

## Index-aware collections

An **index-aware** collection exposes positional access alongside its primary semantic. Two types in `Bodu.Collections.Generic` carry the prefix:

- <xref:Bodu.Collections.Generic.IndexedSet`1> — an insertion-ordered set that implements the full `IList<T>` contract. Duplicates are rejected on add (`Add` returns `false`), positional mutation works through `Insert`, `RemoveAt`, `Move`, and the indexer setter. Backed by a contiguous element array plus an open-addressing hash table, giving O(1) `Contains`, `IndexOf`, and indexed read. <xref:Bodu.Collections.Generic.OrderedSet`1> is the conceptually-a-set sibling that shares the same engine but exposes indices only as a read-only view.
- <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> — a binary min-heap that maintains an element-to-slot map alongside the heap. The map turns `Contains`, `TryGetPriority`, `Update`, `Remove`, and `EnqueueOrUpdate` into O(1) lookup plus O(log n) heap repair — the operations Dijkstra's algorithm, Prim's algorithm, and A* require. Elements are unique; `Enqueue` of an existing element throws.

## Calendar-shape extensions

The `Bodu.Extensions` namespace ships several enums that encode the *shape* of a calendar without implementing one — they parameterise the date arithmetic on <xref:Bodu.Extensions.DateTimeExtensions> and <xref:Bodu.Extensions.DateOnlyExtensions> without pulling in `Bodu.Globalization.Calendar`:

| Type | Encodes |
|---|---|
| <xref:Bodu.Extensions.CalendarQuarterDefinition> | The start month of Q1 — `JanuaryToDecember`, `JulyToJune`, `AprilToMarch`, `April6ToApril5` (UK tax year), `March25ToMarch24` (Lady Day), `OctoberToSeptember`, `FebruaryToJanuary`, plus `Custom` for externally defined rules. Drives `FirstDateOfQuarter`, `LastDateOfQuarter`, `Quarter`, `FiscalYear`. |
| <xref:Bodu.Extensions.WeekOrdinal> | The ordinal position of a weekday in a month — `First`, `Second`, `Third`, `Fourth`, `Fifth`, `Last`. Drives `NthDateOfWeekInMonth` and the recurrence-rule patterns. |
| <xref:Bodu.Extensions.FiscalWeekPattern> | The 4-4-5 / 4-5-4 / 5-4-4 split for retail-style 13-week fiscal quarters. Each quarter is always 13 weeks; the pattern only affects fiscal-period boundaries inside the quarter. The extra week in a 53-week fiscal year is always appended to the final period of Q4. |
| <xref:Bodu.WorkingDaysOfWeek> | Named working-week presets — `MondayToFriday`, `MondayToSaturday`, `SaturdayToThursday`, `SaturdayToWednesday`, and others — plus `Custom` for caller-supplied `WeekPattern` schedules. Drives `IsWeekday`, `IsWeekend`, `IsInWorkingWeek`, `NextWeekday`. |
| <xref:Bodu.Extensions.IWeekendDefinitionProvider> | The injection seam for non-enumerable weekend rules — pass an implementation to override the built-in `WorkingDaysOfWeek` presets when an application needs hybrid, rotating, or domain-specific weekend logic. |

These types are pure data carriers. The actual algorithms live on the extension classes (`DateTimeExtensions.*`, `DateOnlyExtensions.*`) so the same shapes can be passed to a `DateTime`, `DateOnly`, or any future date type without duplicating the enums.

## Where to go next

- **[Introduction](index.md)** — the namespace map and headline types.
- **[Getting started](getting-started.md)** — install + runnable minimal samples for each scenario.
- **[Bodu.Core guides](../../guides/core/index.md)** — recipe-style walk-throughs for circular buffers, deques, evicting dictionaries, and `WeekPattern`.
- **[Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic)** — full type-by-type docs.
