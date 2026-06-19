---
uid: Bodu.Collections.Generic
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Core** provides the foundational collection and utility types that the rest of the Bodu solution builds on. This namespace is the headline home of `Bodu.Core`: bounded and ordered collections that behave predictably under memory pressure, with companions for buffer conversion, base encoding, day-of-week patterns, and argument validation in the adjacent namespaces.

Reach for this library when you need a fixed-capacity FIFO queue, a deque with O(1) ends, a size-limited key/value cache with a real eviction policy (not just an ad-hoc `Dictionary` plus a bolted-on timer), a range-keyed lookup, or helpers that keep ceremony out of hot paths.

## Static documentation

- **[Bodu.Core introduction](~/docs/core/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Core getting started](~/docs/core/getting-started.md)** — install and minimal samples for the headline types.
- **[Bodu.Core guides](~/guides/core/index.md)** — recipe-style walk-throughs: [choosing a collection](~/guides/core/choosing-a-collection.md), [circular buffer](~/guides/core/circular-buffer.md), [deque](~/guides/core/deque.md), [evicting dictionary](~/guides/core/evicting-dictionary.md), [indexed priority queue](~/guides/core/indexed-priority-queue.md), [indexed and ordered sets](~/guides/core/ordered-sets.md), [multiset](~/guides/core/multiset.md), [multi-value dictionary](~/guides/core/multi-value-dictionary.md), [range-keyed lookups](~/guides/core/range-dictionary.md), [segmented buffer](~/guides/core/segmented-buffer.md), [concurrent collections](~/guides/core/concurrent-collections.md), [`WeekPattern`](~/guides/core/week-pattern.md).

## Key types

**Ring-backed collections**

- <xref:Bodu.Collections.Generic.CircularBuffer`1> — a fixed-capacity FIFO collection. With `allowOverwrite: true` it silently drops the oldest element when full; with `allowOverwrite: false` it throws on overflow.
- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> — a lock-free multi-producer / multi-consumer circular buffer using the Vyukov MPMC algorithm, with the same overwrite semantics.
- <xref:Bodu.Collections.Generic.Deque`1> — double-ended queue with O(1) `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast`; growable or fixed-capacity.
- <xref:Bodu.Collections.Generic.RingBackedCollection`1> — abstract base shared by `CircularBuffer<T>` and `Deque<T>` (extension point for new ring-backed collections).
- <xref:Bodu.Collections.Generic.SegmentedBuffer`1> — segmented backing buffer for streaming scenarios where the total length is not known up front.

**Capacity-bounded dictionaries**

- <xref:Bodu.Collections.Generic.EvictingDictionary`2> — a fixed-capacity dictionary that evicts entries automatically when it fills up, under a policy of your choice.
- <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> — the policy enum: `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, `SecondChance`.

**Sets, multisets, range-keyed collections**

- <xref:Bodu.Collections.Generic.IndexedSet`1>, <xref:Bodu.Collections.Generic.OrderedSet`1>, <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> — index-aware set and priority-queue variants for lookup-by-position and key-based priority updates.
- <xref:Bodu.Collections.Generic.MultiValueDictionary`2>, <xref:Bodu.Collections.Generic.Multiset`1> — multi-map and multi-set semantics over `IEqualityComparer<TKey>`.
- <xref:Bodu.Collections.Generic.Range`1>, <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1>, <xref:Bodu.Collections.Generic.ValueRange`2> — range-keyed lookups for ordered or interval-valued keys.

**Related namespaces in `Bodu.Core`**

- <xref:Bodu> — `WeekPattern` (day-of-week bitmask), `IRandomGenerator` / `XorShiftRandom`, and `ThrowHelper` centralized argument validation.
- <xref:Bodu.Buffers> — `PooledBufferBuilder<T>` for `ArrayPool<T>`-backed zero-allocation building.
- <xref:Bodu.Extensions> — date / numeric / span / array extensions and the calendar-shape enums.
- <xref:Bodu.Collections.Extensions>, <xref:Bodu.Collections.Generic.Extensions> — sequence-shaping helpers (recursive selection, sliding windows, batched enumeration, pluggable random shuffles).
- <xref:Bodu.Sequences> — `SequenceGenerator` lazy sequence factories (`Range`, `Repeat`, `NextWhile`, `Factory`) and named mathematical series (Fibonacci, Farey, Leibniz, look-and-say, Thue–Morse).
- <xref:Bodu.Text> — `BaseEncoding` entry points for Base16, Base24, Base32, and Base64.

## Example

```csharp
using Bodu.Collections.Generic;

// Bounded FIFO with overwrite: the four most recent samples win.
var recent = new CircularBuffer<double>(capacity: 4, allowOverwrite: true);
foreach (double sample in stream) recent.Enqueue(sample);

// LRU cache for expensive lookups.
var cache = new EvictingDictionary<string, User>(
    capacity: 1024,
    policy: EvictingDictionaryPolicy.LeastRecentlyUsed);

if (!cache.TryGetValue(id, out User user))
{
    user = Load(id);
    cache[id] = user; // oldest unused entry is evicted automatically when full.
}
```

## Notes

- **Thread safety.** `CircularBuffer<T>`, `Deque<T>`, and `EvictingDictionary<TKey, TValue>` are **not** thread-safe; external synchronization is required if accessed concurrently. For a concurrent FIFO, use <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1>, which is designed for multi-producer / multi-consumer scenarios under the Vyukov algorithm.
- **Capacity is fixed.** Both `CircularBuffer<T>` and `EvictingDictionary<TKey, TValue>` reject a non-positive capacity at construction time. Allocation happens once, up front, not incrementally — this is a deliberate choice for predictable memory behavior in long-running services.
- **Eviction policies differ in cost.** `FirstInFirstOut` and `RandomReplacement` are O(1); `LeastRecentlyUsed` and `MostRecentlyUsed` maintain a linked recency list and are O(1) per access; `LeastFrequentlyUsed` and `SecondChance` carry a small bookkeeping overhead on access. Pick the policy that matches your workload rather than defaulting to LRU.
- **Enumeration is snapshot-stable** for non-concurrent types — iterating while mutating throws, per the usual .NET contract.
- **See also:** the [circular buffer guide](~/guides/core/circular-buffer.md), the [evicting dictionary guide](~/guides/core/evicting-dictionary.md), and the [Bodu.Core introduction](~/docs/core/index.md) for the full scenario table.
