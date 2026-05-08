---
title: Bodu.Core — Introduction
---

# Bodu.Core

**Bodu.Core** is the foundation package of the Bodu suite — a collection of high-performance, framework-style building blocks for .NET applications. It is also the only Bodu package depended on by the others (`Bodu.IO.Hashing` and `Bodu.Security.Cryptography` use its `ThrowHelper` for shared argument validation).

The library is organised around six focused namespaces, each with a clear responsibility.

## Namespaces and headline types

### `Bodu`
Top-level primitives that don't fit into a sub-namespace.

| Type | Purpose |
|---|---|
| `WeekPattern` | Immutable bitmask value type for sets of days of the week. Supports composition (`MTuW`), bitwise operators, parsing, and enumeration. |
| `IRandomGenerator` | Abstraction over random number generators — used by collections that need pluggable randomness. |
| `XorShiftRandom` | Fast non-cryptographic xor-shift PRNG implementing `IRandomGenerator`. |
| `ThrowHelper` | Centralised parameter validation: `ThrowIfNull`, `ThrowIfOutOfRange`, `ThrowIfArrayLengthIsInsufficient`, `ThrowIfEnumValueIsUndefined`, and many more. Uses `[CallerArgumentExpression]` so call sites stay compact. |

### `Bodu.Buffers`
Pooled buffer infrastructure.

| Type | Purpose |
|---|---|
| `PooledBufferBuilder<T>` | `ArrayPool<T>`-backed builder for assembling byte or character spans without allocation. |

### `Bodu.Collections.Generic`
Bounded, fixed-capacity collections built around a shared ring-backed primitive.

| Type | Purpose |
|---|---|
| `CircularBuffer<T>` | Fixed-capacity FIFO ring. Configurable to either silently overwrite or throw when full. |
| `Deque<T>` | Double-ended queue with O(1) `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast`. The `AllowGrow` flag toggles between auto-resize and fixed-capacity-throw modes. |
| `RingBackedCollection<T>` | Abstract base shared by `CircularBuffer<T>` and `Deque<T>`. Extension point for new ring-backed collections. |
| `EvictingDictionary<TKey, TValue>` | Capacity-bounded dictionary with FIFO, LRU, or LFU eviction. Drop-in cache primitive with standard dictionary semantics. |
| `EvictingDictionaryPolicy` | Enum selecting the eviction policy. |

### `Bodu.Collections.Generic.Concurrent`
Lock-free / thread-safe variants.

| Type | Purpose |
|---|---|
| `ConcurrentCircularBuffer<T>` | Thread-safe variant of `CircularBuffer<T>`; implements `IProducerConsumerCollection<T>`. |

### `Bodu.Extensions`
Date, numeric, span, and array extension methods. Larger surface than the others; the highlights:

| Type | Purpose |
|---|---|
| `DateTimeExtensions` | First / last / next / previous day-of-week within month / quarter / year, ISO week-of-year, day name, weekday tests, midday, end-of-day, truncation. |
| `DateOnlyExtensions` | `DateOnly`-specific equivalents plus `Age` calculation. |
| `NumericExtensions` | `ReverseBits`, `RotateBitsLeft` / `Right`, `ReverseBytes`, `GetBytes` for unsigned integer types. |
| `ArrayExtensions` | `Reverse`, `Clear` overloads. |
| `BufferConverter` | Byte / structure conversion helpers. |
| `SpanExtensions` | Span-friendly helpers. |
| `IComparableExtensions` / `ComparableHelper` | `Min`, `Max`, `Clamp`, `IsGreaterThan` / `IsGreaterThanOrEqual`. |
| `CalendarQuarterDefinition` | Enum selecting how the calendar year is partitioned into quarters. |

### `Bodu.Text` and `Bodu.Xml.Linq`
Small text and XML helpers used internally by the other Bodu packages; available publicly when you need them.

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Fixed-capacity FIFO ring buffer (single-threaded) | `CircularBuffer<T>` |
| Fixed-capacity FIFO ring buffer (multi-threaded) | `ConcurrentCircularBuffer<T>` |
| Double-ended queue with O(1) ends | `Deque<T>` |
| LRU / LFU / FIFO cache with dictionary semantics | `EvictingDictionary<TKey, TValue>` |
| Day-of-week set you can union / intersect / parse | `WeekPattern` |
| Pooled byte / char buffer for zero-allocation building | `PooledBufferBuilder<T>` |
| Date arithmetic — first Monday, ISO week-of-year, age | `DateTimeExtensions`, `DateOnlyExtensions` |
| Bit / byte rotation and reversal | `NumericExtensions` |
| Centralised argument validation in your own code | `ThrowHelper.ThrowIf…` |

## Where to go next

- **[Getting started](getting-started.md)** — install the package and run a minimal sample for each scenario above.
- **[Bodu.Core guides](../../guides/core/index.md)** — recipe-style walk-throughs for the headline types.
- **[Bodu.Collections.Generic API reference](../../apidoc/Bodu.Collections.Generic.md)** — full namespace overview.
- **[Algorithm families](../algorithm-families.md)** — how Bodu.Core relates to the hashing and cryptography libraries (it doesn't directly — but its `ThrowHelper` is used everywhere).
