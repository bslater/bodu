---
title: Bodu.Core — Introduction
---

# Bodu.Core

**Bodu.Core** is the foundation package of the Bodu suite — a collection of high-performance, framework-style building blocks for .NET applications. Several other Bodu packages share its primitives: `Bodu.IO.Hashing`, `Bodu.Security.Cryptography`, `Bodu.Globalization.Calendar`, `Bodu.Numerics`, and `Bodu.Financial` all reference `Bodu.Core` for shared types like `ThrowHelper`, `WeekPattern`, the calendar-shape enums, and pooled buffers. See the [package matrix](../package-matrix.md) for the full dependency map.

The library is organized around eight focused namespaces, each with a clear responsibility.

![Bodu.Core namespace map — eight focused namespaces and their headline types](../../images/diagrams/core-namespace-map.svg)

## Namespaces and headline types

### `Bodu`
Top-level primitives that don't fit into a sub-namespace.

| Type | Purpose |
|---|---|
| <xref:Bodu.WeekPattern> | Immutable bitmask value type for sets of days of the week. Supports composition (`MTuW`), bitwise operators, parsing, and enumeration. |
| <xref:Bodu.IRandomGenerator> | Abstraction over random number generators — used by collections that need pluggable randomness. |
| <xref:Bodu.XorShiftRandom> | Fast non-cryptographic xor-shift PRNG implementing `IRandomGenerator`. |
| <xref:Bodu.ThrowHelper> | Centralized parameter validation: `ThrowIfNull`, `ThrowIfOutOfRange`, `ThrowIfArrayLengthIsInsufficient`, `ThrowIfEnumValueIsUndefined`, and many more. Uses `[CallerArgumentExpression]` so call sites stay compact. |

### `Bodu.Buffers`
Pooled buffer infrastructure.

| Type | Purpose |
|---|---|
| <xref:Bodu.Buffers.PooledBufferBuilder`1> | `ArrayPool<T>`-backed builder for assembling byte or character spans without allocation. |

### `Bodu.Collections.Generic`
Bounded and ordered collections built around a shared ring-backed primitive.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Generic.CircularBuffer`1> | Fixed-capacity FIFO ring. Configurable to either silently overwrite or throw when full. |
| <xref:Bodu.Collections.Generic.Deque`1> | Double-ended queue with O(1) `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast`. The `AllowGrow` flag toggles between auto-resize and fixed-capacity-throw modes. |
| <xref:Bodu.Collections.Generic.SegmentedBuffer`1> | Segmented buffer for streaming scenarios where total length is not known up front. |
| <xref:Bodu.Collections.Generic.RingBackedCollection`1> | Abstract base shared by `CircularBuffer<T>` and `Deque<T>`. Extension point for new ring-backed collections. |
| <xref:Bodu.Collections.Generic.EvictingDictionary`2> | Capacity-bounded dictionary with FIFO, LRU, LFU, MRU, Random, or Second-Chance eviction. Drop-in cache primitive with standard dictionary semantics. |
| <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> | Enum selecting the eviction policy: `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, `SecondChance`. |
| <xref:Bodu.Collections.Generic.IndexedSet`1>, <xref:Bodu.Collections.Generic.OrderedSet`1>, <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> | Index-aware set and priority-queue variants for lookup-by-position and key-based priority updates. |
| <xref:Bodu.Collections.Generic.MultiValueDictionary`2>, <xref:Bodu.Collections.Generic.Multiset`1> | Multi-map and multi-set semantics over `IEqualityComparer<TKey>`. |
| <xref:Bodu.Collections.Generic.Range`1>, <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1> | Range-keyed lookups for ordered or interval-valued keys. |

### `Bodu.Collections.Generic.Concurrent`
Lock-free / thread-safe variants.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> | Thread-safe variant of `CircularBuffer<T>`; implements `IProducerConsumerCollection<T>` over the Vyukov MPMC algorithm. |

### `Bodu.Collections.Extensions` and `Bodu.Collections.Generic.Extensions`
Sequence-shaping helpers that compose on top of `IEnumerable<T>` and `IList<T>`.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Extensions.IEnumerableExtensions>, <xref:Bodu.Collections.Generic.Extensions.IEnumerableExtensions> | Recursive selection, sliding windows, batched enumeration, and other sequence helpers. |
| <xref:Bodu.Collections.Generic.Extensions.IListExtensions>, <xref:Bodu.Collections.Generic.Extensions.SystemRandomAdapter>, <xref:Bodu.Collections.Generic.Extensions.RandomizationMode> | Pluggable randomness-driven shuffles backed by `IRandomGenerator`. |

### `Bodu.Extensions`
Date, numeric, span, and array extension methods. Larger surface than the others; the highlights:

| Type | Purpose |
|---|---|
| <xref:Bodu.Extensions.DateTimeExtensions> | First / last / next / previous day-of-week within month / quarter / year, ISO week-of-year, day name, weekday tests, midday, end-of-day, truncation. |
| <xref:Bodu.Extensions.DateOnlyExtensions> | `DateOnly`-specific equivalents plus `Age` calculation. |
| <xref:Bodu.Extensions.NumericExtensions> | `ReverseBits`, `RotateBitsLeft` / `Right`, `ReverseBytes`, `GetBytes` for unsigned integer types. |
| <xref:Bodu.Extensions.ArrayExtensions> | `Reverse`, `Clear`, and other in-place array helpers. |
| <xref:Bodu.Extensions.BufferConverter> | Byte / structure conversion helpers. |
| <xref:Bodu.Extensions.SpanExtensions> | Span-friendly helpers. |
| <xref:Bodu.Extensions.IComparableExtensions>, <xref:Bodu.Extensions.ComparableHelper> | `Min`, `Max`, `Clamp`, `IsGreaterThan` / `IsGreaterThanOrEqual`. |
| <xref:Bodu.Extensions.CalendarQuarterDefinition>, <xref:Bodu.WorkingDaysOfWeek>, <xref:Bodu.Extensions.IWeekendDefinitionProvider>, <xref:Bodu.Extensions.FiscalWeekPattern>, <xref:Bodu.Extensions.WeekOfMonthOrdinal> | Calendar-shape enums and injection seams for quarter, weekend, fiscal-week, and week-ordinal computations. |

### `Bodu.Text` and `Bodu.Xml.Linq`
Text and XML helpers used internally by the other Bodu packages; available publicly when you need them.

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.BaseEncoding> | Entry points for Base16, Base24, Base32, and Base64 over text or binary input. |
| <xref:Bodu.Text.BaseFormatStyles>, <xref:Bodu.Text.BaseFormattingOptions> | Formatting-style and option flags consumed by `BaseEncoding`. |
| <xref:Bodu.Xml.Linq.XmlNamespaceResolver> | `IXmlNamespaceResolver` helper used by the calendar rule parsers. |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Fixed-capacity FIFO ring buffer (single-threaded) | <xref:Bodu.Collections.Generic.CircularBuffer`1> |
| Fixed-capacity FIFO ring buffer (multi-threaded) | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> |
| Double-ended queue with O(1) ends | <xref:Bodu.Collections.Generic.Deque`1> |
| LRU / LFU / FIFO / MRU / Random / Second-Chance cache | <xref:Bodu.Collections.Generic.EvictingDictionary`2> + <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> |
| Index-aware set with O(1) lookup-by-position | <xref:Bodu.Collections.Generic.IndexedSet`1> |
| Range-keyed lookup table | <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1> |
| Multi-map / multi-set semantics | <xref:Bodu.Collections.Generic.MultiValueDictionary`2>, <xref:Bodu.Collections.Generic.Multiset`1> |
| Day-of-week set you can union / intersect / parse | <xref:Bodu.WeekPattern> |
| Pooled byte / char buffer for zero-allocation building | <xref:Bodu.Buffers.PooledBufferBuilder`1> |
| Date arithmetic — first Monday, ISO week-of-year, age | <xref:Bodu.Extensions.DateTimeExtensions>, <xref:Bodu.Extensions.DateOnlyExtensions> |
| Bit / byte rotation and reversal | <xref:Bodu.Extensions.NumericExtensions> |
| Base16 / Base24 / Base32 / Base64 encoding | <xref:Bodu.Text.BaseEncoding> |
| Centralized argument validation in your own code | <xref:Bodu.ThrowHelper> |

## Where to go next

- **[Core concepts](concepts.md)** — glossary the rest of the documentation assumes.
- **[Getting started](getting-started.md)** — install the package and run a minimal sample for each scenario above.
- **[Bodu.Core guides](../../guides/core/index.md)** — recipe-style walk-throughs for the headline types.
- **[Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic)** — full namespace overview.
- **[Project introduction](../introduction.md)** — how Bodu.Core relates to the hashing, cryptography, calendar, and text libraries (its `ThrowHelper` underpins them all).
