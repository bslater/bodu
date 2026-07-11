---
title: Concurrent collections
---

# Concurrent collections

`Bodu.Collections.Generic.Concurrent` ships three thread-safe collections that pair with their non-concurrent peers in `Bodu.Collections.Generic`: `ConcurrentCircularBuffer<T>` for fixed-capacity FIFO under multi-producer / multi-consumer load, `ConcurrentHashSet<T>` for an unordered set of unique elements under concurrent add / remove / lookup, and `ConcurrentEvictingDictionary<TKey,TValue>` for a bounded cache with policy-driven eviction under concurrent reads and writes. The namespace ships in the **`Bodu.Collections.Concurrent`** package (which depends on `Bodu.Collections`) — install with `dotnet add package Bodu.Collections.Concurrent`.

Reach for them when the same collection is accessed by multiple producers and consumers — external locking around `CircularBuffer<T>`, `HashSet<T>`, or `EvictingDictionary<TKey,TValue>` works, but it serialises every operation behind a single monitor. The concurrent variants coordinate more finely — per-slot sequence numbers (Vyukov MPMC) for the buffer, a lock-free split-ordered list for the set, and independently locked policy segments (lock striping) for the cache — so disjoint operations proceed in parallel.

## `ConcurrentCircularBuffer<T>`

A lock-free MPMC ring buffer using the **Vyukov per-slot sequence protocol**. Each slot carries a sequence number that producers and consumers CAS-update to coordinate without taking a global lock; the per-slot 64-byte cache-line padding prevents false sharing on x86 / x64.

### Construction

```csharp
using Bodu.Collections.Generic.Concurrent;

// Default capacity 16, overwrite enabled.
var ring = new ConcurrentCircularBuffer<Message>();

// Capacity hint; overwrite enabled.
var sized = new ConcurrentCircularBuffer<Message>(capacity: 1024);

// Capacity + overwrite control.
var strict = new ConcurrentCircularBuffer<Message>(capacity: 1024, allowOverwrite: false);

// Pre-fill from an enumerable.
var seeded = new ConcurrentCircularBuffer<Message>(initial, capacity: 1024, allowOverwrite: true);
```

The constraint is `where T : class?` — reference types only. Slot publication relies on `Volatile` reads / writes of object references for atomic visibility.

The capacity minimum is 2; smaller values are rejected at construction because the Vyukov protocol cannot distinguish "data published" from "slot released" on a single-slot ring.

### Core operations

```csharp
ring.Enqueue(message);                      // Throws if full and !AllowOverwrite
ring.TryEnqueue(message);                   // Returns false if full and !AllowOverwrite
Message m = ring.Dequeue();                 // Throws if empty
if (ring.TryDequeue(out Message? m2)) { … } // Safe variant
Message head = ring.Peek();                 // Throws if empty
if (ring.TryPeek(out Message? p)) { … }     // Safe variant
ring.Clear();                                // Acquires all slots; expensive
```

Single-element operations (`Enqueue`, `Dequeue`, `Peek`, and their `Try…` variants) are individually atomic and lock-free on the hot path. The buffer also implements <xref:System.Collections.Concurrent.IProducerConsumerCollection`1> (`TryAdd` / `TryTake` forward to `TryEnqueue` / `TryDequeue`), so it can back a `BlockingCollection<T>`.

> [!IMPORTANT]
> `Count` is **approximate** under concurrency — the head and tail positions are read independently (no lock), so the value is a point-in-time estimate that can momentarily disagree with the true contents. `ToArray` is the way to obtain a *coherent* FIFO snapshot: it uses a per-slot seqlock that restarts on churn, falling back to a sequence-validated best-effort snapshot only after an internal retry budget is exhausted. `Clear` drains at most the count observed at the call (bounded so a continuous producer cannot livelock it) and does **not** raise `ItemEvicted`.

### Overwrite semantics

`AllowOverwrite` is a settable property. When `true`, a producer that finds the buffer full silently overwrites the oldest entry; the `ItemEvicted` event fires with the evicted item:

```csharp
ring.AllowOverwrite = true;
ring.ItemEvicted += evicted => log.Warn("Dropped {Id}", evicted.Id);
ring.Enqueue(message);   // may evict; ItemEvicted fires after
```

`ItemEvicted` exceptions are caught and suppressed — the event handler must not assume it can fail the producer. When `AllowOverwrite` is `false`, `Enqueue` throws `InvalidOperationException` on full; `TryEnqueue` returns `false`.

### Snapshot enumeration

```csharp
foreach (Message m in ring)
    Process(m);

Message[] snap = ring.ToArray();
```

The enumerator is a true snapshot captured at the moment `GetEnumerator()` runs — concurrent mutation does not throw `InvalidOperationException` and the snapshot is consistent. `ToArray()` materialises the same snapshot; concurrent writers are not blocked beyond the point-in-time scan.

### Observable consistency

| Method | Consistency | Cost |
|---|---|---|
| `Enqueue`, `TryEnqueue` | Per-element atomic | Lock-free hot path |
| `Dequeue`, `TryDequeue` | Per-element atomic | Lock-free hot path |
| `Peek`, `TryPeek` | Per-element atomic | Lock-free |
| `Count` | **Approximate** | Two independent position reads |
| `this[int]` | Single-slot, sequence-validated | Lock-free; two index reads are *not* jointly atomic |
| `Clear` | Bounded drain at observed count | No `ItemEvicted` raised |
| `ToArray`, enumeration | Coherent snapshot (seqlock) | Restarts on churn; best-effort after retry budget |

For lightweight throughput sampling, prefer `TryDequeue` / `TryPeek` over reading `Count`. The explicit `ICollection.SyncRoot` **throws `NotSupportedException`** — matching the BCL `ConcurrentQueue<T>`, there is no lock object to expose — so do not attempt to `lock` on the buffer.

## `ConcurrentHashSet<T>`

A thread-safe unordered set of unique elements backed by a **lock-free split-ordered list** (Shalev & Shavit): all elements live in one Harris–Michael lock-free ordered linked list keyed by bit-reversed hash codes, and the bucket array holds lazily created shortcut pointers into it. Every operation — `Add`, `Remove`, `Contains`, `Count`, `Clear`, `ToArray` — is lock-free; growing the table copies shortcuts but never rehashes or moves a node.

### Construction

```csharp
using Bodu.Collections.Generic.Concurrent;

var set = new ConcurrentHashSet<string>();
var caseInsensitive = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
var sized = new ConcurrentHashSet<string>(capacity: 1024);
var both  = new ConcurrentHashSet<string>(capacity: 1024, StringComparer.Ordinal);
var seeded = new ConcurrentHashSet<string>(initial);
var seededInsensitive = new ConcurrentHashSet<string>(initial, StringComparer.OrdinalIgnoreCase);
```

The constraint is `where T : notnull` — null elements are rejected, matching the BCL `HashSet<T>` contract.

### Core operations

```csharp
set.Add("alpha");        // True if added, false if already present
set.Remove("alpha");     // True if removed, false if absent
set.Contains("alpha");   // Pure lock-free read — never blocks a writer
set.Clear();             // Atomic lock-free swap of the whole backing structure
```

Single-element operations (`Add`, `Remove`, `Contains`) are atomic per element and lock-free — a preempted thread can never stall the others, and readers never block writers, which makes the set well suited to read-heavy workloads (membership tests on a working set, deduplication of a stream).

### Snapshot enumeration

```csharp
foreach (string item in set)
    Process(item);

string[] snap = set.ToArray();
```

`ToArray` and the enumerator capture a **weakly consistent** snapshot via a lock-free traversal: every element present for the entire duration of the capture is included and no element appears twice, but concurrent additions and removals may or may not be observed. Enumeration is safe under concurrent mutation, never throws, and does not see writes that occur after the snapshot is taken.

### Counting

```csharp
int count = set.Count;                 // Lock-free interlocked counter read; exact at quiescence
bool empty = set.IsEmpty;
int alias = set.ApproximateCount;      // Alias of Count, retained for compatibility
bool aliasEmpty = set.IsEmptyApproximate;
```

`Count` is a single lock-free counter read — exact whenever no mutation is in flight, and never off by more than the operations currently executing. `ApproximateCount` and `IsEmptyApproximate` are retained as aliases from the earlier lock-striped design; prefer `Count` and `IsEmpty` in new code.

### Bulk set operations

`ConcurrentHashSet<T>` implements `ISet<T>`, so the standard `UnionWith`, `IntersectWith`, `ExceptWith`, `SymmetricExceptWith`, `IsSubsetOf`, etc. are available. They are *not* atomic — each is a sequence of per-element atomic operations over a snapshot, so concurrent mutation may interleave. For atomic snapshot-and-mutate, capture `ToArray()`, mutate the array, and bulk-update.

## `ConcurrentEvictingDictionary<TKey,TValue>`

The thread-safe variant of [`EvictingDictionary<TKey,TValue>`](evicting-dictionary.md): a fixed-capacity dictionary that evicts per a configurable policy, backed by **lock-striped segments**. The key comparer's hash routes each key to one of up to 32 segments; each segment is a small, exact policy cache over its own slice of the capacity, guarded by its own monitor. In an evicting cache *reads are writes* — a lookup repositions the key for recency-tracked policies — so even `TryGetValue` takes its segment's lock; the striping keeps that contention local instead of global.

### Construction

```csharp
using Bodu.Collections.Generic.Concurrent;

// Capacity + policy; all six EvictingDictionaryPolicy values are supported.
var cache = new ConcurrentEvictingDictionary<string, Payload>(
    capacity: 1024, EvictingDictionaryPolicy.LeastRecentlyUsed);

// Optional TTL layer — same EvictingDictionaryExpiration as the non-concurrent type.
var expiring = new ConcurrentEvictingDictionary<string, Payload>(
    capacity: 1024,
    new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5), EvictingDictionaryExpirationKind.Sliding));
```

### Core operations

```csharp
cache.Add("k", payload);                    // Add-or-replace; may evict when the segment is full
cache.TryAdd("k", payload);                 // Add only when absent
cache.TryGetValue("k", out var value);      // Counts as a policy access; slides a sliding TTL
cache.Touch("k");                           // Policy access without reading or sliding
cache.TryRemove("k", out var removed);      // Explicit removal — not an eviction, no event
Payload p = cache.GetOrAdd("k", key => Load(key)); // Single-flight: factory runs at most once per key
```

`GetOrAdd` runs its factory *inside* the owning segment's lock, so concurrent misses on the same key invoke the factory exactly once — the cache-stampede guard. Keep factories short and never call back into the dictionary from one.

### Approximate global eviction order

Eviction order is **exact within a segment, approximate globally**: each segment runs the configured policy over its own capacity slice (the slices sum to `Capacity` exactly), so a hot segment may evict while a cold one has free slots. The global capacity bound is strict — the dictionary never stores more than `Capacity` entries. This is the same trade every production concurrent cache makes; a cache that maintained one global recency order would serialise all reads behind a single lock.

### Eviction notifications

The `ItemEvicted` event is raised **after** an eviction has been committed and after the segment lock has been released, so handlers can safely call back into the dictionary. Handler exceptions are suppressed (except `OutOfMemoryException`) because the eviction cannot be undone. There is no pre-removal `ItemEvicting` event — under concurrency it could not be honored. Explicit `TryRemove` and `Clear` are not evictions and raise no event.

### Snapshots and counts

`ToArray`, `Keys`, `Values`, enumeration, `Count`, and `IsEmpty` acquire every segment lock for a coherent point-in-time view; `ApproximateCount` is lock-free. Enumeration iterates a detached snapshot and never throws on concurrent modification; its order is unspecified. With TTL configured, `Count` reports the raw stored count *including* expired-but-unpurged entries — call `RemoveExpired()` to reconcile, exactly as with the non-concurrent type.

## When *not* to use these collections

- **Single-threaded scenarios.** The Vyukov and split-ordered CAS coordination is a tax that single-threaded code pays for nothing. Use the non-concurrent peers in [`Bodu.Collections.Generic`](xref:Bodu.Collections.Generic).
- **Value types.** `ConcurrentCircularBuffer<T>` constrains `T : class?` because slot publication relies on `Volatile` reference reads. For a concurrent queue of value types, use the BCL `ConcurrentQueue<T>`.
- **Bounded waiting.** None of the collections has a blocking dequeue / blocking add. Compose with `BlockingCollection<T>` if you need consumer threads to block until an item is available.
- **Ordered set semantics.** `ConcurrentHashSet<T>` does not maintain insertion order. For ordered concurrent semantics, an external lock around `SortedSet<T>` is usually clearer than a custom concurrent implementation.

## See also

- [`ConcurrentCircularBuffer<T>` API reference](xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1)
- [`ConcurrentHashSet<T>` API reference](xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1)
- [`ConcurrentEvictingDictionary<TKey,TValue>` API reference](xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2)
- [Circular buffer guide](circular-buffer.md) — the non-concurrent peer.
- [Evicting dictionary guide](evicting-dictionary.md) — the non-concurrent peer of the evicting cache.
- [`Bodu.Collections.Generic.Concurrent` namespace landing](xref:Bodu.Collections.Generic.Concurrent)
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
