---
uid: Bodu.Collections.Generic
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Core** provides the foundational collection and utility types that the rest of the Bodu solution builds on. This namespace is the headline home of `Bodu.Core`: fixed-capacity collections that behave predictably under memory pressure, with companions for buffer conversion, text encoding, and argument validation in the adjacent namespaces.

Reach for this library when you need a bounded FIFO queue, a size-limited key/value cache with a real eviction policy (not just an ad-hoc `Dictionary` plus a bolted-on timer), or helpers that keep ceremony out of hot paths.

## Key types

- <xref:Bodu.Collections.Generic.CircularBuffer`1> — a fixed-capacity FIFO collection. With `allowOverwrite: true` it silently drops the oldest element when full; with `allowOverwrite: false` it throws on overflow.
- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> — a lock-free multi-producer / multi-consumer circular buffer using the Vyukov MPMC algorithm, with the same overwrite semantics.
- <xref:Bodu.Collections.Generic.EvictingDictionary`2> — a fixed-capacity dictionary that evicts entries automatically when it fills up, under a policy of your choice.
- <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> — the policy enum: FIFO, LRU, LFU, MRU, Random, Second Chance.

Related namespaces in the same library:

- <xref:Bodu.Buffers> — low-overhead conversion helpers between raw byte arrays and arrays of unmanaged types (`BufferConverter`).
- <xref:Bodu.Extensions> — array helpers (`ArrayExtensions`) for slicing, copying, clearing, and reversing.
- `Bodu.Text` — `BaseEncoding` entry points for Base16, Base24, Base32, and Base64.
- <xref:Bodu> — `ThrowHelper` centralises argument validation; `IRandomGenerator` abstracts random generation for testability.

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

- **Thread safety.** `CircularBuffer<T>` and `EvictingDictionary<TKey, TValue>` are **not** thread-safe; external synchronisation is required if accessed concurrently. For a concurrent FIFO, use <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1>, which is designed for multi-producer / multi-consumer scenarios under the Vyukov algorithm.
- **Capacity is fixed.** Both `CircularBuffer<T>` and `EvictingDictionary<TKey, TValue>` reject a non-positive capacity at construction time. Allocation happens once, up front, not incrementally — this is a deliberate choice for predictable memory behaviour in long-running services.
- **Eviction policies differ in cost.** `FirstInFirstOut` and `RandomReplacement` are O(1); `LeastRecentlyUsed` and `MostRecentlyUsed` maintain a linked recency list and are O(1) per access; `LeastFrequentlyUsed` and `SecondChance` carry a small bookkeeping overhead on access. Pick the policy that matches your workload rather than defaulting to LRU.
- **Enumeration is snapshot-stable** for non-concurrent types — iterating while mutating throws, per the usual .NET contract.
