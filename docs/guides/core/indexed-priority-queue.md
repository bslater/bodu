---
title: Indexed priority queue
---

# Indexed priority queue

`IndexedPriorityQueue<TElement, TPriority>` is a binary min-heap priority queue keyed by element identity. It maintains an auxiliary element-to-index map alongside the heap, so containment and re-prioritisation are O(1) and O(log n) respectively — the operations that turn the BCL `PriorityQueue<TElement, TPriority>` into a clumsy fit for Dijkstra's algorithm, Prim's algorithm, A* search, and other graph algorithms that need to *update* an element's priority rather than re-enqueue it.

The standard `PriorityQueue<TElement, TPriority>` does not enforce element uniqueness and does not support priority updates; the canonical workaround is to enqueue a duplicate at the new priority and tombstone the stale entry on dequeue. That works, but burns memory and lookup time for every relaxation step. `IndexedPriorityQueue<TElement, TPriority>` is the alternative: each element appears at most once; updating a priority moves the element through the heap in O(log n) without enqueuing a duplicate.

## Construction

```csharp
using Bodu.Collections.Generic;

// Empty queue.
var pq = new IndexedPriorityQueue<string, int>();

// With a capacity hint.
var sized = new IndexedPriorityQueue<string, int>(capacity: 1024);

// With a custom priority comparer (descending).
var desc = new IndexedPriorityQueue<string, int>(
    Comparer<int>.Create((a, b) => b.CompareTo(a)));

// With both an element-equality comparer (for case-insensitive keys) and a priority comparer.
var both = new IndexedPriorityQueue<string, int>(
    capacity: 1024,
    priorityComparer: Comparer<int>.Default,
    elementComparer: StringComparer.OrdinalIgnoreCase);

// Heapified initialisation in O(n) instead of n × O(log n).
var fromPairs = new IndexedPriorityQueue<int, double>(
    new[] { new KeyValuePair<int, double>(7, 1.5), new KeyValuePair<int, double>(3, 0.5) });
```

The constraint is `where TElement : notnull` — the auxiliary map cannot accept a null key. Strings, value-type identifiers, and class instances with proper equality all work.

## Core operations

```csharp
pq.Enqueue("source", priority: 0);
pq.Enqueue("a", priority: 7);
pq.Enqueue("b", priority: 3);
pq.Enqueue("c", priority: 12);

pq.Count;                           // 4
pq.Contains("a");                   // True
pq.GetPriority("a");                // 7

var (element, priority) = pq.Peek();   // ("source", 0) — smallest priority
pq.Dequeue();                          // ("source", 0)
```

Element uniqueness is enforced. Calling `Enqueue("a", 5)` when `"a"` is already present throws `ArgumentException`. Use `TryEnqueue` for a safe non-throwing add, or `EnqueueOrUpdate` — the canonical "relax this edge" primitive — to add or update in one call:

```csharp
// True if added, false if already present (and priority updated).
bool added = pq.EnqueueOrUpdate("b", newPriority: 2);
```

## Priority updates

```csharp
pq.Update("a", newPriority: 0);     // O(log n) — re-position in the heap
pq.TryUpdate("missing", 0);         // False — no exception
```

`Update` throws `KeyNotFoundException` when the element is absent; `TryUpdate` returns `false`. This is the operation that makes the type useful for graph algorithms — when a shorter path to `a` is discovered, `Update("a", newDistance)` re-positions it in O(log n) without enqueueing a duplicate.

## Removal

```csharp
pq.Remove("a");                     // True if removed, false if absent
pq.Clear();                         // Empty the queue; capacity preserved
```

`Remove(TElement)` removes by element identity in O(log n). For the head-only removal, prefer `Dequeue` / `TryDequeue` — they avoid the auxiliary lookup.

## Capacity management

```csharp
pq.Capacity;                         // Current heap-array length
pq.EnsureCapacity(10_000);          // Grow if needed; returns new capacity
pq.TrimExcess();                    // Shrink to fit
```

`TrimExcess` does *not* bump the enumeration version, so an enumerator outstanding at the time of the call continues to walk the original snapshot.

## Enumeration

```csharp
foreach (KeyValuePair<string, int> entry in pq)
    Console.WriteLine($"{entry.Key} = {entry.Value}");
```

The enumerator walks the underlying heap array in storage order, not priority order. For a sorted result, drain the queue with `Dequeue`:

```csharp
var sorted = new List<KeyValuePair<string, int>>();
while (pq.TryDequeue(out string? element, out int priority))
    sorted.Add(new(element!, priority));
```

The enumerator is a struct, so a `foreach` loop allocates nothing.

## Worked example — Dijkstra's algorithm

```csharp
using Bodu.Collections.Generic;

Dictionary<string, double> ShortestPaths(
    string source,
    Func<string, IEnumerable<(string To, double Weight)>> edges)
{
    var distances = new Dictionary<string, double> { [source] = 0 };
    var pq = new IndexedPriorityQueue<string, double>();
    pq.Enqueue(source, 0);

    while (pq.TryDequeue(out string? u, out double du))
    {
        foreach ((string v, double w) in edges(u!))
        {
            double alt = du + w;
            if (!distances.TryGetValue(v, out double dv) || alt < dv)
            {
                distances[v] = alt;
                pq.EnqueueOrUpdate(v, alt);   // O(log n), no duplicates
            }
        }
    }

    return distances;
}
```

This is the textbook Dijkstra without the standard duplicate-and-tombstone workaround — `EnqueueOrUpdate` keeps the queue at most |V| in size, with each node updated O(deg) times in O(log n).

## Performance

| Operation | Complexity |
|---|---|
| `Contains`, `GetPriority`, `TryGetPriority` | O(1) |
| `Peek`, `TryPeek` | O(1) |
| `Enqueue`, `TryEnqueue`, `EnqueueOrUpdate` | O(log n) |
| `Dequeue`, `TryDequeue` | O(log n) |
| `Update`, `TryUpdate` | O(log n) |
| `Remove(TElement)` | O(log n) |
| Heapified construction from `IEnumerable` | O(n) |
| Enumeration (storage order) | O(n) |
| Drain to sorted order | O(n log n) |

The auxiliary element-to-index map doubles memory cost compared to a plain heap — the trade is worth it for any workload that needs containment, retrieval, or update by element identity.

## When *not* to use `IndexedPriorityQueue`

- **No update or containment queries.** If you only need enqueue + dequeue and elements can appear more than once, the BCL `PriorityQueue<TElement, TPriority>` is simpler and avoids the auxiliary map overhead.
- **Unbounded priority types.** The heap re-positions on `Update` by comparing priorities — if your priority type has comparison cost proportional to length (e.g. arbitrary-precision rationals), the constant factor dominates.
- **Multi-element priority changes per call.** The type is one-at-a-time. For bulk re-prioritisation, drain and rebuild.

## See also

- [`IndexedPriorityQueue<TElement, TPriority>` API reference](xref:Bodu.Collections.Generic.IndexedPriorityQueue`2)
- [`Bodu.Collections.Generic` namespace landing](~/apidoc/Bodu.Collections.Generic.md)
- [Range-keyed lookups](range-dictionary.md) — the other "lookup-by-key" collection family.
- [Concurrent collections](concurrent-collections.md) — for thread-safe queue scenarios.
