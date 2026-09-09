---
uid: Bodu.Collections.Generic.Concurrent
---

![Bodu.Collections.Generic.Concurrent](~/images/hero-collections-concurrent.svg)

## Purpose

**Bodu.Collections.Generic.Concurrent** ships the thread-safe / lock-free variants of the `Bodu.Collections.Generic` collections, in the `Bodu.Collections.Concurrent` package (which depends on `Bodu.Collections`, and through it on `Bodu.Core`). Reach for this namespace when the same collection is accessed by multiple producers and consumers and you need predictable concurrent semantics rather than an external lock.

## Key types

- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> — thread-safe variant of <xref:Bodu.Collections.Generic.CircularBuffer`1> implementing `IProducerConsumerCollection<T>` over the Vyukov MPMC algorithm. Same overwrite semantics as the non-concurrent base.
- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> — thread-safe hash set with concurrent add / remove / contains; backed by a lock-free split-ordered list, so every operation completes without taking a lock.
- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2> — thread-safe variant of <xref:Bodu.Collections.Generic.EvictingDictionary`2>: a fixed-capacity bounded cache over lock-striped segments supporting all six <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> values, optional TTL expiry (<xref:Bodu.Collections.Generic.EvictingDictionaryExpiration>), single-flight `GetOrAdd`, and a post-commit `ItemEvicted` event. Eviction order is exact per segment and approximate globally; `TKey : notnull`.

## Example

```csharp
using Bodu.Collections.Generic.Concurrent;

// T is constrained to reference types (where T : class?) — wrap value types in a record or class.
var ring = new ConcurrentCircularBuffer<Sample>(capacity: 1024, allowOverwrite: true);

// Multi-producer / multi-consumer scenarios — no external lock needed.
Parallel.ForEach(samples, sample => ring.TryAdd(sample));

while (ring.TryTake(out Sample? item))
    Process(item);

// Bounded, policy-driven cache shared by request threads.
var cache = new ConcurrentEvictingDictionary<string, Payload>(
    capacity: 10_000,
    policy: EvictingDictionaryPolicy.LeastRecentlyUsed);
Payload payload = cache.GetOrAdd(key, k => Load(k));   // single-flight: one loader per key at a time
```

## Notes

- **Lock-free ring and set; lock-striped cache.** `ConcurrentCircularBuffer<T>` uses the Vyukov bounded MPMC algorithm and `ConcurrentHashSet<T>` a split-ordered list — neither takes a lock, so a preempted thread can never stall the others. `ConcurrentEvictingDictionary<TKey,TValue>` is different by design: it partitions the key space into independently locked segments, so contention is bounded by the stripe count rather than eliminated, and eviction order is exact within a segment but approximate across the whole cache.
- **Reference types only in the ring.** `ConcurrentCircularBuffer<T>` is constrained to `where T : class?` because the Vyukov slots exchange references atomically; box or wrap value types.
- **Producer-consumer semantics.** The ring implements `IProducerConsumerCollection<T>`, so it composes with `BlockingCollection<T>` if you need blocking semantics on top.
- **Related namespaces.** The non-concurrent peers (<xref:Bodu.Collections.Generic.CircularBuffer`1> and the rest of the catalogue) live in <xref:Bodu.Collections.Generic>, shipped by the `Bodu.Collections` package this one depends on.
- **See also:** the [concurrent collections guide](~/guides/core/concurrent-collections.md), the [circular buffer guide](~/guides/core/circular-buffer.md), and the [Bodu.Collections.Concurrent introduction](~/docs/collections-concurrent/index.md).
