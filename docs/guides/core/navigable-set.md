---
title: Navigable set
---

# Navigable set

`NavigableSet<T>` is a sorted set augmented with order statistics — a comparer-ordered `ISet<T>` backed by a red-black tree whose nodes carry subtree sizes. That one augmentation turns the whole positional query family into O(log n) operations: nearest-neighbour lookups (`TryGetFloor` / `TryGetCeiling` / `TryGetHigher` / `TryGetLower`), rank and select (`IndexOf` / `GetAt`), and range counting (`CountInRange`), plus cheap `Ascending()` / `Descending()` / `Range(low, high)` views.

It fits problems where "sorted" is not enough and the *position* of an element matters: order books and price ladders (nearest quote at or below a limit), percentile and median tracking over a mutating population (k-th smallest in O(log n)), scheduling (next free slot after *t*), leaderboards (rank of a player, player at rank), and windowed counting (how many events between two timestamps) without iterating the window.

The BCL's sorted collections each miss part of this contract:

- `SortedSet<T>` sorts and offers `GetViewBetween`, but has no floor/ceiling/higher/lower queries, no rank/select, and no O(log n) range count (`GetViewBetween(...).Count` is O(k)).
- `SortedList<TKey,TValue>` gives positional access but pays O(n) per insert and remove.
- `SortedDictionary<TKey,TValue>` is a key-value map with neither positional access nor navigation queries.

Contract points worth keeping in mind:

- **Set semantics.** Comparer-equal duplicates are rejected — `Add` returns `false` and keeps the stored element, exactly like `SortedSet<T>`.
- **Null elements are rejected** with `ArgumentNullException`, consistent with the rest of the Bodu collection family. This deliberately diverges from `SortedSet<T>`, which permits `null` for reference types; a null-permitting sorted set would make the Try-pattern navigation results ambiguous.
- **Views are live and fail-fast.** `Ascending()`, `Descending()`, and `Range(low, high)` re-resolve against the current tree each time they are iterated; mutating the set *during* an iteration throws `InvalidOperationException` on the next advance, matching the family convention.
- `Range` bounds are **inclusive on both ends**, need not be present in the set, and the walk costs O(log n + k) — it descends straight to the first in-range element.
- `Min` / `Max` throw `InvalidOperationException` on an empty set; `TryGetMin` / `TryGetMax` are the non-throwing forms.
- Not thread-safe.

The backing-structure decision (order-statistic red-black tree with parent pointers, versus a skip list) is recorded in the design note at `Bodu.Collections/docs/navigable-collections-design.md`. [`NavigableDictionary<TKey,TValue>`](navigable-dictionary.md) is the key-value counterpart over the same node machinery.

## Pattern 1 — nearest-neighbour queries

```csharp
using Bodu.Collections.Generic;

var ladder = new NavigableSet<decimal>(new[] { 99.50m, 100.00m, 100.25m, 101.00m });

// Best bid at or below a limit price.
if (ladder.TryGetFloor(100.10m, out decimal bid))
    Console.WriteLine(bid);              // 100.00

// Best ask strictly above the last trade.
if (ladder.TryGetHigher(100.25m, out decimal ask))
    Console.WriteLine(ask);              // 101.00

ladder.TryGetCeiling(100.10m, out decimal atOrAbove);  // 100.25 — least >= value
ladder.TryGetLower(99.50m, out _);                     // false — nothing strictly below the minimum
```

## Pattern 2 — rank, select, and range counting

`IndexOf` is the zero-based rank (elements smaller than the value); `GetAt` is the inverse (the k-th smallest); `CountInRange` subtracts two rank walks, so none of the three iterates the set:

```csharp
using Bodu.Collections.Generic;

var latencies = new NavigableSet<int>();
foreach (int sample in new[] { 12, 5, 210, 47, 8, 33, 90, 61 })
    latencies.Add(sample);

int median = latencies.GetAt(latencies.Count / 2);   // k-th smallest — 47
int p90 = latencies.GetAt((int)(latencies.Count * 0.9) - 1);

int rank = latencies.IndexOf(33);                    // 3 — three samples are smaller
int fastPath = latencies.CountInRange(0, 50);        // 5 — O(log n), no iteration
```

## Pattern 3 — directional and range views

```csharp
using Bodu.Collections.Generic;

var schedule = new NavigableSet<TimeOnly>(new[]
{
    new TimeOnly(9, 0), new TimeOnly(10, 30), new TimeOnly(13, 0), new TimeOnly(15, 45),
});

// Ascending() == enumeration order; Descending() walks the other way without materializing.
foreach (TimeOnly slot in schedule.Descending())
    Console.WriteLine(slot);             // 15:45, 13:00, 10:30, 09:00

// Inclusive-bounds sub-range; the bounds need not be booked slots.
foreach (TimeOnly slot in schedule.Range(new TimeOnly(10, 0), new TimeOnly(14, 0)))
    Console.WriteLine(slot);             // 10:30, 13:00

// Views are live: a view captured before a mutation reflects it on the next fresh iteration.
IEnumerable<TimeOnly> morning = schedule.Range(new TimeOnly(0, 0), new TimeOnly(12, 0));
schedule.Add(new TimeOnly(11, 15));
Console.WriteLine(morning.Count());      // 3 — 09:00, 10:30, 11:15
```

## Choosing against the alternatives

| Requirement | Reach for |
|---|---|
| Sorted uniqueness only, no positional queries | `SortedSet<T>` (BCL) |
| Sorted + floor/ceiling/rank/select/range-count | `NavigableSet<T>` |
| Sorted key-value with positional access, rare writes | `SortedList<TKey,TValue>` (BCL) — accepts O(n) inserts |
| Membership over dense integer universe | [`BitSet`](bit-set.md) |
| Disjoint interval membership | [`RangeSet<T>`](range-dictionary.md) |

## See also

- [Navigable dictionary](navigable-dictionary.md) — the key-value counterpart with the same query family.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Indexed and ordered sets](ordered-sets.md) — insertion-ordered (not comparer-ordered) unique collections.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
