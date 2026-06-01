---
title: Concurrent collections
---

# Concurrent collections

`Bodu.Collections.Generic.Concurrent` ships two thread-safe collections that pair with their non-concurrent peers in `Bodu.Collections.Generic`: `ConcurrentCircularBuffer<T>` for fixed-capacity FIFO under multi-producer / multi-consumer load, and `ConcurrentHashSet<T>` for an unordered set of unique elements under concurrent add / remove / lookup.

Reach for them when the same collection is accessed by multiple producers and consumers — external locking around `CircularBuffer<T>` or `HashSet<T>` works, but it serialises every operation behind a single monitor. The concurrent variants split contention across slots (Vyukov MPMC for the buffer) or bucket regions (lock striping for the set), so disjoint operations proceed in parallel.

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

Single-element operations (`Enqueue`, `Dequeue`, `Peek`, and their `Try…` variants) are individually atomic and lock-free on the hot path. Multi-element operations (`Count`, `Clear`, `ToArray`) acquire every slot and observe a coherent point-in-time state — useful for diagnostics or shutdown drain, but expensive enough to keep off the hot path.

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

| Method | Atomic? | Locks? |
|---|---|---|
| `Enqueue`, `TryEnqueue` | Per-element atomic | None on the hot path |
| `Dequeue`, `TryDequeue` | Per-element atomic | None on the hot path |
| `Peek`, `TryPeek` | Per-element atomic | None |
| `Count` | Point-in-time | Acquires all slots |
| `Clear` | Point-in-time | Acquires all slots |
| `ToArray`, enumeration | Point-in-time | Acquires all slots (once) |

For lightweight throughput sampling, prefer `TryDequeue` / `TryPeek` over reading `Count`.

## `ConcurrentHashSet<T>`

A thread-safe unordered set of unique elements backed by **lock striping**. The internal bucket array is partitioned into regions, each guarded by its own monitor; disjoint writers proceed in parallel, and `Contains` is lock-free.

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
set.Contains("alpha");   // Lock-free — never blocks a writer
set.Clear();             // Acquires all locks
```

Single-element operations (`Add`, `Remove`, `Contains`) are atomic per element. `Contains` is *lock-free* — readers never block writers, which makes the set well suited to read-heavy workloads (membership tests on a working set, deduplication of a stream).

### Snapshot enumeration

```csharp
foreach (string item in set)
    Process(item);

string[] snap = set.ToArray();
```

`ToArray` and the enumerator both acquire every region lock once to capture a snapshot, then release. Enumeration is safe under concurrent mutation but does not see writes that occur after the snapshot is taken.

### Approximate vs. coherent counts

```csharp
int hot   = set.ApproximateCount;     // Lock-free; may slightly under- or over-count under concurrent mutation
int exact = set.Count;                 // Acquires all locks; coherent
bool empty = set.IsEmptyApproximate;
bool reallyEmpty = set.IsEmpty;
```

Reach for the approximate properties when reading the count on a hot path; reach for the exact `Count` only when you genuinely need a coherent view (logging on shutdown, capacity-based throttling).

### Bulk set operations

`ConcurrentHashSet<T>` implements `ISet<T>`, so the standard `UnionWith`, `IntersectWith`, `ExceptWith`, `SymmetricExceptWith`, `IsSubsetOf`, etc. are available. They are *not* atomic — each is a sequence of per-element atomic operations over a snapshot, so concurrent mutation may interleave. For atomic snapshot-and-mutate, capture `ToArray()`, mutate the array, and bulk-update.

## When *not* to use these collections

- **Single-threaded scenarios.** The Vyukov coordination and lock-striping overhead is a tax that single-threaded code pays for nothing. Use the non-concurrent peers in [`Bodu.Collections.Generic`](xref:Bodu.Collections.Generic).
- **Value types.** `ConcurrentCircularBuffer<T>` constrains `T : class?` because slot publication relies on `Volatile` reference reads. For a concurrent queue of value types, use the BCL `ConcurrentQueue<T>`.
- **Bounded waiting.** Neither collection has a blocking dequeue / blocking add. Compose with `BlockingCollection<T>` if you need consumer threads to block until an item is available.
- **Ordered set semantics.** `ConcurrentHashSet<T>` does not maintain insertion order. For ordered concurrent semantics, an external lock around `SortedSet<T>` is usually clearer than a custom striped implementation.

## See also

- [`ConcurrentCircularBuffer<T>` API reference](xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1)
- [`ConcurrentHashSet<T>` API reference](xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1)
- [Circular buffer guide](circular-buffer.md) — the non-concurrent peer.
- [`Bodu.Collections.Generic.Concurrent` namespace landing](xref:Bodu.Collections.Generic.Concurrent)
