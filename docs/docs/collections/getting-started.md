---
title: Bodu.Collections — Getting started
---

# Bodu.Collections — Getting started

## Install

```bash
dotnet add package Bodu.Collections
```

Targets `net8.0`. No external runtime dependencies — `Bodu.Collections` references only `Bodu.Core` (pulled in automatically) for shared argument validation and the random-generator abstraction. The thread-safe variants (`ConcurrentCircularBuffer<T>`, `ConcurrentHashSet<T>`) ship in the companion [`Bodu.Collections.Concurrent`](../collections-concurrent/getting-started.md) package.

## Minimal samples

### Circular buffer (`CircularBuffer<T>`)

```csharp
using Bodu.Collections.Generic;

var buffer = new CircularBuffer<int>(capacity: 4, allowOverwrite: true);

buffer.Enqueue(1);
buffer.Enqueue(2);
buffer.Enqueue(3);
buffer.Enqueue(4);
buffer.Enqueue(5); // 1 is evicted; buffer holds [2, 3, 4, 5]

int oldest = buffer.Dequeue(); // 2
```

`allowOverwrite: false` throws `InvalidOperationException` when full. For thread safety, use `Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer<T>` from the [`Bodu.Collections.Concurrent`](../collections-concurrent/getting-started.md) package.

### Evicting dictionary (`EvictingDictionary<TKey, TValue>`)

```csharp
using Bodu.Collections.Generic;

var cache = new EvictingDictionary<string, byte[]>(
    capacity: 100,
    policy: EvictingDictionaryPolicy.LeastRecentlyUsed);

cache["alpha"] = LoadFromDisk("alpha");
_ = cache.TryGetValue("alpha", out byte[]? value); // touches LRU order
```

Policies: `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, `SecondChance`.

### Time-based expiry (`EvictingDictionaryExpiration`)

Layer a time-to-live on top of the capacity policy — sliding renewal on read, a testable `TimeProvider`, and lazy reclamation via `RemoveExpired()`:

```csharp
using Bodu.Collections.Generic;

var sessions = new EvictingDictionary<string, Session>(
    capacity: 10_000,
    expiration: new EvictingDictionaryExpiration(
        TimeSpan.FromMinutes(20),
        EvictingDictionaryExpirationKind.Sliding));

sessions["user-42"] = session;                    // 20-minute sliding lifetime
sessions.Add("one-shot", token, TimeSpan.FromSeconds(30)); // per-entry override

int reclaimed = sessions.RemoveExpired();          // no background timer — reconcile on demand
```

### Deque (`Deque<T>`)

```csharp
using Bodu.Collections.Generic;

var deque = new Deque<string>(allowGrow: true);

deque.AddFirst("a");
deque.AddLast("b");
deque.AddLast("c");

string first = deque.RemoveFirst(); // "a"
string last  = deque.RemoveLast();  // "c"
```

### Sequenced dictionary (`SequencedDictionary<TKey, TValue>`)

An insertion- (or access-) ordered map with O(1) access to and removal of either end — the .NET analogue of Java's `LinkedHashMap`:

```csharp
using Bodu.Collections.Generic;

var lru = new SequencedDictionary<string, byte[]>(accessOrder: true);

lru["a"] = LoadFromDisk("a");
_ = lru.TryGetValue("a", out _);   // access-order: moves "a" to the most-recent end

// Trim the least-recently-used entry in O(1).
if (lru.Count > 100)
    lru.TryRemoveFirst(out _);
```

### Indexed priority queue (`IndexedPriorityQueue<TElement, TPriority>`)

A min-heap that supports O(log n) priority updates by element identity — the shape Dijkstra and A* need:

```csharp
using Bodu.Collections.Generic;

var pq = new IndexedPriorityQueue<string, double>();
pq.Enqueue("source", 0);
pq.EnqueueOrUpdate("a", 7);    // add, or update if already queued
pq.EnqueueOrUpdate("a", 3);    // O(log n) decrease-key, no duplicate

var (element, priority) = pq.Dequeue();   // ("source", 0) — smallest priority first
```

### Navigable set (`NavigableSet<T>`)

Nearest-neighbour, rank, and range queries over sorted data in O(log n):

```csharp
using Bodu.Collections.Generic;

var prices = new NavigableSet<decimal> { 9.99m, 24.50m, 49.00m, 99.00m };

_ = prices.TryGetFloor(30.00m, out decimal floor);     // 24.50 — greatest ≤ probe
_ = prices.TryGetCeiling(30.00m, out decimal ceiling); // 49.00 — least ≥ probe

int rank = prices.IndexOf(49.00m);                     // 2 — elements before it in sort order
decimal cheapest = prices.GetAt(0);                    // 9.99 — select by rank
int midRange = prices.CountInRange(10.00m, 50.00m);    // 2 — counted without enumerating
```

`NavigableDictionary<TKey, TValue>` offers the same query families over key-sorted entries.

### Bloom filter (`BloomFilter<T>`)

Approximate membership in fixed memory — no false negatives, tunable false-positive rate:

```csharp
using Bodu.Collections.Probabilistic;

var seen = new BloomFilter<string>(expectedItems: 1_000_000, falsePositiveRate: 0.01);

seen.Add("user:42");

if (!seen.MightContain(candidate))
{
    // Definitively new — a Bloom filter never reports false negatives.
    Process(candidate);
}
```

## Where to go next

- **[Bodu.Collections introduction](index.md)** — namespaces, headline types, scenarios.
- **[Core concepts](concepts.md)** — the collection vocabulary (overflow policies, eviction, navigation, sketches).
- **[Choosing a collection](../../guides/core/choosing-a-collection.md)** — the decision guide across the catalogue.
- **[Collections guides](../../guides/core/index.md)** — recipe-style walk-throughs for every headline type.
- **[Bodu.Collections.Concurrent getting started](../collections-concurrent/getting-started.md)** — the thread-safe companion package.
- **[Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic)** — full type-by-type docs.
