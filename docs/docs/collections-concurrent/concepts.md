---
title: Bodu.Collections.Concurrent — Core concepts
---

# Bodu.Collections.Concurrent — Core concepts

This page is the concurrency vocabulary the package documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [concurrent collections guide](../../guides/core/concurrent-collections.md).

Part of the **[Core Foundations](../topics/core-foundations.md)** topic. The general collection vocabulary — fixed capacity, ring backing, overflow policies — lives on the [Bodu.Collections concepts page](../collections/concepts.md); this page covers only what changes when multiple threads are involved.

## Lock-free MPMC ring

<xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> is a **multi-producer / multi-consumer (MPMC)** ring using the **Vyukov per-slot sequence protocol**: each slot carries a sequence number that producers and consumers advance with compare-and-swap operations, so threads coordinate through the slots themselves rather than a global lock. Per-slot 64-byte cache-line padding prevents *false sharing* — two threads touching adjacent slots never invalidate each other's cache lines on x86 / x64.

Two consequences of the protocol surface in the API:

- **Reference types only.** The constraint is `T : class?` because slot publication relies on `Volatile` reads and writes of object references for atomic visibility. For a concurrent queue of value types, use the BCL [`ConcurrentQueue<T>`](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentqueue-1).
- **Capacity minimum of 2.** On a single-slot ring the protocol cannot distinguish "data published" from "slot released", so smaller capacities are rejected at construction.

Single-element operations (`Enqueue` / `Dequeue` / `Peek` and their `Try…` variants) are individually atomic and lock-free on the hot path. The buffer implements <xref:System.Collections.Concurrent.IProducerConsumerCollection`1> (`TryAdd` / `TryTake`), so it composes with [`BlockingCollection<T>`](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.blockingcollection-1) when consumers should block until an item arrives — neither type has blocking operations of its own.

## Lock-free split-ordered hashing

<xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> is a **lock-free split-ordered list** (Shalev & Shavit): every element lives in one Harris–Michael lock-free ordered linked list, sorted by the bit-reversed hash code, and the hash-table part is just an array of lazily created *shortcut* pointers into that list. Mutations are single compare-and-swap operations; deletion marks the node first and unlinks it cooperatively. Growing the table copies shortcut pointers but never rehashes or moves a node.

No operation on the set takes a lock — `Add`, `Remove`, `Contains`, `Count`, `Clear`, and `ToArray` are all lock-free, so a preempted thread can never stall the others. `Clear` atomically swaps the entire backing structure in one compare-and-swap; `ToArray` and enumeration are *weakly consistent* traversals (see below).

## Lock-striped evicting cache

<xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2> takes a different route, because in an evicting cache *reads are writes*: a lookup repositions the key for recency-tracked policies, so even `TryGetValue` mutates shared order structures and a lock-free design is impractical. Instead the cache uses **lock striping** — each internal segment is an exact policy cache over a fixed slice of the total capacity, guarded by its own monitor. The slices sum to `Capacity` exactly, so the global bound is strict while eviction order is exact per segment and approximate globally. That trade is what every production concurrent cache makes; a single global recency order would serialise all reads behind one lock.

The stripe count is the cache's concurrency budget: operations that need a globally coherent view (`Count`, `ToArray`, enumeration, `Clear`) acquire *every* segment lock once, which is exactly why the approximate alternatives below exist.

## Snapshot enumeration

The single-threaded catalogue enumerates **fail-fast**: a version counter detects structural mutation and the enumerator throws <xref:System.InvalidOperationException>. A lock-free structure cannot maintain that token, so both concurrent types substitute **snapshot enumeration** — `foreach` and `ToArray` capture the contents once, iterate over that fixed copy, and *never throw* on concurrent modification. Writes that land after the snapshot are simply not seen.

The types capture their snapshots differently:

- The **buffer** uses a per-slot *seqlock*: the scan validates slot sequence numbers and restarts on churn, falling back to a sequence-validated best-effort snapshot only after an internal retry budget is exhausted. The result is a coherent point-in-time capture.
- The **set** performs a lock-free traversal of its split-ordered list. The capture is **weakly consistent** rather than point-in-time: it contains every element present for the entire duration of the call and never contains an element twice, but concurrent additions and removals may or may not be observed.
- The **evicting dictionary** acquires every segment lock once, copies, and releases — a brief globally consistent pause rather than an optimistic retry loop.

## Approximate vs. coherent counts

Under concurrency, "how many elements?" has two honest answers, and the API names them:

| Read | Consistency | Cost |
|---|---|---|
| Buffer `Count` | **Approximate** — head and tail positions are read independently | Two volatile reads; never blocks |
| Set `Count`, `IsEmpty` | **Exact at quiescence** — an interlocked counter that may transiently lag operations still in flight | One volatile read; lock-free |
| Dictionary `ApproximateCount` | **Approximate** — per-segment counters summed lock-free | Lock-free |
| Dictionary `Count`, `IsEmpty` | **Coherent** — a true point-in-time value (raw count incl. expired-unpurged) | Acquires every segment lock |
| Buffer `ToArray` / enumeration | **Coherent snapshot** (seqlock) | Restarts on churn |
| Set `ToArray` / enumeration | **Weakly consistent** traversal | Lock-free; never blocks writers |

Prefer `TryPeek` / `TryDequeue` over reading the buffer's `Count` for lightweight sampling of a busy buffer, and treat any count read under active mutation as already stale by the time it is inspected.

## Eviction events under contention

With `AllowOverwrite = true`, a producer that finds the buffer full silently overwrites the oldest entry and raises `ItemEvicted` with the displaced item. The event contract deliberately differs from the single-threaded <xref:Bodu.Collections.Generic.CircularBuffer`1>:

| | `CircularBuffer<T>` | `ConcurrentCircularBuffer<T>` |
|---|---|---|
| Pre-eviction event | `ItemEvicting` (can veto) | *none* |
| Handler exceptions | Propagate to the caller | **Swallowed** |
| `Clear` raises events | Yes | No — bounded drain, no `ItemEvicted` |

The lock-free path cannot safely unwind a half-completed slot handoff, so there is no veto and a throwing handler must not be able to fail the producer. Do not port veto logic between the two types; treat the concurrent event as telemetry, not control flow.

<xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2> follows the same concurrent contract relative to its non-concurrent peer: only the post-commit `ItemEvicted` survives (no `ItemEvicting`), handlers run *after* the owning segment's lock is released — so they may safely call back into the dictionary — and handler exceptions are swallowed. Capacity evictions and TTL expiries raise the event; explicit `TryRemove` and `Clear` do not.

## Bulk operations are not transactions

`ConcurrentHashSet<T>`'s `ISet<T>` operations (`UnionWith`, `IntersectWith`, `ExceptWith`, `SymmetricExceptWith`, and the subset/superset predicates) are sequences of per-element atomic operations over a snapshot — they are **not** atomic as a whole, so concurrent mutation may interleave with them. When a mutation must be all-or-nothing, capture `ToArray()`, compute outside the set, and apply the result — or use an external lock for that operation only.

## Where to go next

- **[Introduction](index.md)** — the package map and headline types.
- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Concurrent collections guide](../../guides/core/concurrent-collections.md)** — construction patterns, the consistency table, and when *not* to use these types.
- **[Bodu.Collections concepts](../collections/concepts.md)** — the single-threaded collection vocabulary this page builds on.
- **[Bodu.Collections.Generic.Concurrent API reference](xref:Bodu.Collections.Generic.Concurrent)** — full type-by-type docs.
