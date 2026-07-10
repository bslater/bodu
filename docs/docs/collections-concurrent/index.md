---
title: Bodu.Collections.Concurrent — Introduction
---

# Bodu.Collections.Concurrent

![Bodu.Collections.Concurrent](../../images/hero-collections-concurrent.svg)

**Bodu.Collections.Concurrent** ships the thread-safe members of the Bodu collection catalogue — the lock-free `ConcurrentCircularBuffer<T>`, the lock-striped `ConcurrentHashSet<T>`, and the lock-striped `ConcurrentEvictingDictionary<TKey,TValue>` bounded cache — as a focused companion package in the **[Core Foundations](../topics/core-foundations.md)** topic. The dependency chain is `Bodu.Core` ← [`Bodu.Collections`](../collections/index.md) ← `Bodu.Collections.Concurrent`: this package references `Bodu.Collections` (and through it `Bodu.Core`), so installing it brings the whole family. The namespace is unchanged from the original split — every type lives in `Bodu.Collections.Generic.Concurrent`.

Reach for this package when the same collection is accessed by multiple producers and consumers and you need predictable concurrent semantics rather than an external lock. External locking around `CircularBuffer<T>`, `HashSet<T>`, or `EvictingDictionary<TKey,TValue>` works, but it serialises every operation behind a single monitor; the concurrent variants split contention across slots (Vyukov MPMC for the buffer) or bucket / segment regions (lock striping for the set and the cache), so disjoint operations proceed in parallel.

![Bodu.Collections.Concurrent namespace map — the thread-safe types over the Bodu.Collections and Bodu.Core dependency chain](../../images/diagrams/collections-concurrent-namespace-map.svg)

## Namespaces and headline types

### `Bodu.Collections.Generic.Concurrent`

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> | Thread-safe variant of <xref:Bodu.Collections.Generic.CircularBuffer`1>: a lock-free multi-producer / multi-consumer ring over the Vyukov per-slot sequence protocol, implementing <xref:System.Collections.Concurrent.IProducerConsumerCollection`1> with the same overwrite-on-full semantics as its single-threaded peer. Reference types only (`T : class?`). |
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> | Thread-safe unordered set of unique elements backed by lock striping: the bucket array is partitioned into regions each guarded by its own monitor, so disjoint writers proceed in parallel and `Contains` is lock-free. Implements `ISet<T>`; elements must be non-null (`T : notnull`). |
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2> | Thread-safe variant of <xref:Bodu.Collections.Generic.EvictingDictionary`2>: a fixed-capacity bounded cache over lock-striped segments supporting all six eviction policies (FIFO / LRU / LFU / MRU / SecondChance / Random), optional TTL expiry, single-flight `GetOrAdd`, and a post-commit `ItemEvicted` event. Eviction order is exact per segment, approximate globally; keys must be non-null (`TKey : notnull`). |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Fixed-capacity FIFO ring buffer shared by multiple threads | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> |
| Producer-consumer queue (optionally behind `BlockingCollection<T>`) | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> via <xref:System.Collections.Concurrent.IProducerConsumerCollection`1> |
| Bounded telemetry / recent-items window under concurrent writers | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> with `AllowOverwrite = true` |
| Thread-safe set of active correlation ids, dedup of a concurrent stream | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> |
| Read-heavy membership tests that must never block writers | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> (`Contains` is lock-free) |
| Bounded in-process cache (LRU / LFU / TTL) shared by request threads | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2> |
| Cache-stampede protection — expensive value built at most once per key | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2> (`GetOrAdd` runs the factory single-flight per key) |

## Design notes

- **Lock-free ring, lock-striped set and cache.** The buffer coordinates producers and consumers with per-slot sequence numbers (CAS updates, 64-byte cache-line padding against false sharing) — no `lock` on the hot path. The set stripes its buckets across independently locked regions; the cache stripes its capacity across independently locked policy segments; only same-region writers contend.
- **Snapshot enumeration, never fail-fast.** Every type enumerates a coherent snapshot and never throws on concurrent modification — the deliberate inverse of the fail-fast contract on the single-threaded catalogue, because a fail-fast version token cannot be maintained without a lock.
- **Approximate reads are named as such.** `Count` on the buffer and `ApproximateCount` / `IsEmptyApproximate` on the set and cache are point-in-time estimates that never block; the exact `Count` on the set and cache acquires every region lock for a coherent answer. Choose by need, not by habit.
- **Eviction under contention.** With `AllowOverwrite = true`, a producer that finds the buffer full silently overwrites the oldest entry and raises `ItemEvicted` afterwards; the evicting dictionary raises the same post-commit `ItemEvicted` after releasing its segment lock. In both cases handler exceptions are swallowed and there is no pre-eviction veto — a committed concurrent eviction cannot be unwound. See [concepts](concepts.md) for the contrast with the single-threaded event contracts.
- **Exact per segment, approximate globally.** The evicting dictionary runs its policy exactly within each segment over a fixed slice of the capacity (slices sum to `Capacity` exactly), so global eviction order is approximate while the capacity bound stays strict — the standard trade of production concurrent caches.
- **`SyncRoot` throws.** Matching the BCL `ConcurrentQueue<T>`, the explicit `ICollection.SyncRoot` on every type throws <xref:System.NotSupportedException> — there is nothing meaningful to lock on.

## Where to go next

- **[Core concepts](concepts.md)** — the concurrency vocabulary: MPMC rings, lock striping, snapshot enumeration, approximate counts.
- **[Getting started](getting-started.md)** — install the package and run a minimal sample for each type.
- **[Concurrent collections guide](../../guides/core/concurrent-collections.md)** — the full walk-through: construction, consistency table, bulk operations, when *not* to use these types.
- **[Bodu.Collections.Generic.Concurrent API reference](xref:Bodu.Collections.Generic.Concurrent)** — full namespace overview.
- **[Bodu.Collections introduction](../collections/index.md)** — the single-threaded catalogue this package extends.
- **[Core Foundations topic](../topics/core-foundations.md)** — how the three packages fit together.
