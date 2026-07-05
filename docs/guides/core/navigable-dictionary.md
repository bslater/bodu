---
title: Navigable dictionary
---

# Navigable dictionary

`NavigableDictionary<TKey,TValue>` is the key-value counterpart of [`NavigableSet<T>`](navigable-set.md) — a key-sorted `IDictionary<TKey,TValue>` backed by the same order-statistic red-black tree, with the navigation surface transposed to keys. Every positional query is O(log n): nearest-neighbour lookups (`TryGetFloorEntry` / `TryGetCeilingEntry` / `TryGetHigherEntry` / `TryGetLowerEntry`, plus the key-only `TryGetFloorKey` family), rank and select (`IndexOfKey` / `GetAt`), and range counting (`CountInRange`), plus cheap `Ascending()` / `Descending()` / `Range(low, high)` entry views.

It fits problems where a sorted map's *position* matters as much as its lookups: time-series stores (the sample at or before a timestamp), tiered pricing and tax brackets (the bracket whose threshold floors the amount), versioned configuration (the newest entry at or below a version), and windowed aggregation (how many readings between two keys) without iterating the window.

The BCL's `SortedDictionary<TKey,TValue>` sorts by key but answers none of this — no floor/ceiling/higher/lower, no rank/select, no O(log n) range count; `SortedList<TKey,TValue>` gives positional access but pays O(n) per insert and remove.

Contract points worth keeping in mind:

- **Dictionary semantics.** `Add` throws `ArgumentException` on a comparer-equal duplicate key (the strict `Dictionary<TKey,TValue>.Add` contract); `TryAdd` returns `false` instead; the indexer upserts. The bulk-load constructor also rejects duplicate keys in the source, matching the `Dictionary` collection-constructor contract.
- **Null keys are rejected** with `ArgumentNullException`, consistent with the rest of the Bodu collection family. **Null values are allowed** — values are unconstrained, and `ContainsValue(null)` finds a stored `null`.
- `ContainsValue` is an honest **O(n)** walk — values are not indexed; `ContainsKey` is the O(log n) lookup.
- **Views are live and fail-fast.** `Ascending()`, `Descending()`, and `Range(low, high)` re-resolve against the current tree each time they are iterated; mutating the dictionary *during* an iteration throws `InvalidOperationException` on the next advance. `Keys` and `Values` are live, read-only, key-sorted views with the same fail-fast behaviour.
- **Overwriting a value is not a structural mutation.** Assigning an existing key through the indexer does not invalidate in-flight enumerators; adds and removes do.
- `Range` bounds are **inclusive on both ends**, need not be present, and the walk costs O(log n + k).
- `MinEntry` / `MaxEntry` throw `InvalidOperationException` on an empty dictionary; `TryGetMinEntry` / `TryGetMaxEntry` are the non-throwing forms.
- Not thread-safe.

The backing structure is the key-value adaptation of the set's node machinery; the decision record — including the anticipated dictionary follow-on — lives in the design note at `Bodu.Collections/docs/navigable-collections-design.md`.

## Pattern 1 — nearest entry at or before a key

```csharp
using Bodu.Collections.Generic;

var samples = new NavigableDictionary<DateTime, double>();
samples.Add(new DateTime(2026, 7, 5, 9, 0, 0), 21.4);
samples.Add(new DateTime(2026, 7, 5, 9, 5, 0), 21.9);
samples.Add(new DateTime(2026, 7, 5, 9, 10, 0), 22.3);

// The reading in effect at 09:07 — greatest key <= the probe.
if (samples.TryGetFloorEntry(new DateTime(2026, 7, 5, 9, 7, 0), out var reading))
    Console.WriteLine($"{reading.Key:HH:mm} -> {reading.Value}");   // 09:05 -> 21.9

// Key-only variants skip materializing the pair.
samples.TryGetHigherKey(reading.Key, out DateTime nextSample);      // 09:10
```

## Pattern 2 — rank, select, and range counting over keys

```csharp
using Bodu.Collections.Generic;

var scores = new NavigableDictionary<int, string>(new[]
{
    KeyValuePair.Create(2210, "dana"), KeyValuePair.Create(1830, "alex"),
    KeyValuePair.Create(2475, "sam"),  KeyValuePair.Create(1990, "kim"),
});

int rank = scores.IndexOfKey(2210);                 // 2 — two lower scores
var median = scores.GetAt(scores.Count / 2);        // entry with the k-th smallest key
int inBand = scores.CountInRange(1900, 2300);       // 2 — O(log n), no iteration
```

## Pattern 3 — directional and range entry views

```csharp
using Bodu.Collections.Generic;

var brackets = new NavigableDictionary<decimal, decimal>();   // threshold -> rate
brackets.Add(0m, 0.00m);
brackets.Add(18_200m, 0.19m);
brackets.Add(45_000m, 0.30m);
brackets.Add(135_000m, 0.37m);

// Highest-first walk without materializing.
foreach (var bracket in brackets.Descending())
    Console.WriteLine($"{bracket.Key} -> {bracket.Value}");

// Inclusive-bounds sub-range; the bounds need not be stored keys.
foreach (var bracket in brackets.Range(10_000m, 100_000m))
    Console.WriteLine(bracket.Key);                  // 18200, 45000

// The rate that applies to an income is a floor query, not a scan.
brackets.TryGetFloorEntry(52_000m, out var applicable);   // (45000, 0.30)
```

## Choosing against the alternatives

| Requirement | Reach for |
|---|---|
| Sorted key-value only, no positional queries | `SortedDictionary<TKey,TValue>` (BCL) |
| Sorted key-value + floor/ceiling/rank/select/range-count | `NavigableDictionary<TKey,TValue>` |
| Sorted unique elements with the same query family | [`NavigableSet<T>`](navigable-set.md) |
| Sorted key-value with positional access, rare writes | `SortedList<TKey,TValue>` (BCL) — accepts O(n) inserts |
| Range-keyed lookup (a key maps a whole interval) | [`RangeDictionary<TKey,TValue>`](range-dictionary.md) |

## See also

- [Navigable set](navigable-set.md) — the element-only counterpart over the same tree machinery.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
