---
uid: Bodu.Collections.Generic.Concurrent
---

![Bodu.Collections.Generic.Concurrent](~/images/hero-core.svg)

## Purpose

**Bodu.Collections.Generic.Concurrent** ships the thread-safe / lock-free variants of the `Bodu.Collections.Generic` collections, in the `Bodu.Collections.Concurrent` package (which depends on `Bodu.Collections`, and through it on `Bodu.Core`). Reach for this namespace when the same collection is accessed by multiple producers and consumers and you need predictable concurrent semantics rather than an external lock.

## Key types

- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> — thread-safe variant of <xref:Bodu.Collections.Generic.CircularBuffer`1> implementing `IProducerConsumerCollection<T>` over the Vyukov MPMC algorithm. Same overwrite semantics as the non-concurrent base.
- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> — thread-safe hash set with concurrent add / remove / contains; backed by a partitioned segment design for predictable contention behaviour.

## Example

```csharp
using Bodu.Collections.Generic.Concurrent;

var ring = new ConcurrentCircularBuffer<long>(capacity: 1024, allowOverwrite: true);

// Multi-producer / multi-consumer scenarios — no external lock needed.
Parallel.ForEach(samples, sample => ring.TryAdd(sample));

while (ring.TryTake(out long item))
    Process(item);
```

## Notes

- **Lock-free.** `ConcurrentCircularBuffer<T>` uses the Vyukov bounded MPMC algorithm — no `lock` statements on the hot path.
- **Producer-consumer semantics.** Implements `IProducerConsumerCollection<T>`, so it composes with `BlockingCollection<T>` if you need blocking semantics on top.
- **Related namespaces.** The non-concurrent peers (<xref:Bodu.Collections.Generic.CircularBuffer`1> and the rest of the catalogue) live in <xref:Bodu.Collections.Generic>, shipped by the `Bodu.Collections` package this one depends on.
- **See also:** the [concurrent collections guide](~/guides/core/concurrent-collections.md), the [circular buffer guide](~/guides/core/circular-buffer.md), and the [Bodu.Collections.Concurrent introduction](~/docs/collections-concurrent/index.md).
