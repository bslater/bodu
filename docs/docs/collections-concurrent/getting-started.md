---
title: Bodu.Collections.Concurrent — Getting started
---

# Bodu.Collections.Concurrent — Getting started

## Install

```bash
dotnet add package Bodu.Collections.Concurrent
```

Targets `net8.0`. No external runtime dependencies — the package references `Bodu.Collections` (which in turn references `Bodu.Core`), so both are pulled in automatically. The types live in the `Bodu.Collections.Generic.Concurrent` namespace.

## Minimal samples

### Concurrent circular buffer (`ConcurrentCircularBuffer<T>`)

A lock-free multi-producer / multi-consumer FIFO ring — no external lock needed:

```csharp
using Bodu.Collections.Generic.Concurrent;

var ring = new ConcurrentCircularBuffer<Message>(capacity: 1024, allowOverwrite: true);

// Producers — may run on many threads concurrently.
ring.Enqueue(message);            // overwrites the oldest entry when full
ring.ItemEvicted += dropped => log.Warn("Dropped {Id}", dropped.Id);

// Consumers — also concurrent.
while (ring.TryDequeue(out Message? item))
    Process(item);
```

With `allowOverwrite: false`, `Enqueue` throws when full and `TryEnqueue` returns `false`. The buffer implements `IProducerConsumerCollection<T>` (`TryAdd` / `TryTake`), so wrap it in a `BlockingCollection<T>` when consumers should block for work. `T` must be a reference type, and the minimum capacity is 2 — see [concepts](concepts.md) for why.

### Concurrent hash set (`ConcurrentHashSet<T>`)

A lock-free split-ordered set of unique elements — every operation is lock-free, so writers never block each other or readers:

```csharp
using Bodu.Collections.Generic.Concurrent;

var seen = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

// Dedup a concurrent stream: Add returns true only for the first arrival.
Parallel.ForEach(events, e =>
{
    if (seen.Add(e.CorrelationId))
        ProcessFirstOccurrence(e);
});

bool active = seen.Contains("req-42");   // lock-free — never blocks a writer
int hot     = seen.ApproximateCount;     // lock-free estimate for hot paths
int exact   = seen.Count;                // coherent — acquires every region lock
```

Enumeration and `ToArray()` observe a coherent snapshot and never throw on concurrent modification.

## Where to go next

- **[Bodu.Collections.Concurrent introduction](index.md)** — headline types, scenarios, and design notes.
- **[Core concepts](concepts.md)** — MPMC rings, split-ordered hashing, snapshot enumeration, counting under concurrency.
- **[Concurrent collections guide](../../guides/core/concurrent-collections.md)** — the full walk-through, including the consistency table and when *not* to use these types.
- **[Bodu.Collections getting started](../collections/getting-started.md)** — the single-threaded catalogue.
- **[Bodu.Collections.Generic.Concurrent API reference](xref:Bodu.Collections.Generic.Concurrent)** — full type-by-type docs.
