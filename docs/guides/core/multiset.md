---
title: Multiset
---

# Multiset

<xref:Bodu.Collections.Generic.Multiset`1> (a *bag*) is a set that retains duplicates as **multiplicity** rather than discarding them. Adding an element that is already present increments its count instead of being rejected. It is the right tool for frequency counting, inventory-style tallies, and multiset algebra (sum, union, intersection, difference) where the number of copies matters.

Equality is governed by an `IEqualityComparer<T>` supplied at construction, so counting can be case-insensitive or structural. The element type is constrained to `notnull`; storage is a `Dictionary<T, int>` from each distinct element to its count.

## Pattern 1 — counting occurrences

```csharp
using Bodu.Collections.Generic;

var words = new Multiset<string>();

foreach (string token in "the cat sat on the mat".Split(' '))
    words.Add(token);

int the   = words.CountOf("the");   // → 2
int total = words.Count;            // → 6 — includes multiplicity
int kinds = words.DistinctCount;    // → 5 — distinct elements only
```

`Count` reports the total number of elements including duplicates; `DistinctCount` reports the number of *distinct* elements. `CountOf` returns `0` for an absent element — no exception, no `TryGetValue` dance:

```csharp
var histogram = new Multiset<char>("mississippi");

Console.WriteLine(histogram.Count);          // → 11
Console.WriteLine(histogram.DistinctCount);  // → 4
Console.WriteLine(histogram.CountOf('s'));   // → 4
Console.WriteLine(histogram.CountOf('z'));   // → 0 — absent elements report zero
```

The `IEnumerable<T>` constructor seeds the bag with one count per source element, so any sequence becomes a histogram in one line.

## Pattern 2 — adding and removing with explicit multiplicity

```csharp
var inventory = new Multiset<string>();

inventory.Add("widget", count: 10);   // add ten at once
inventory.Add("widget");              // now 11

bool removedOne = inventory.Remove("widget");     // removes a single copy → 10
bool removedAll = inventory.RemoveAll("widget");  // removes every copy → 0, returns true
```

`Remove` removes a single copy and returns `false` when the element is absent; `RemoveAll` removes every copy of the element at once. An element whose count reaches zero leaves the bag entirely — `Contains` becomes `false` and the element no longer appears in `Distinct()` or `Frequencies()`. `Add(item, count)` throws <xref:System.ArgumentOutOfRangeException> when `count` is zero or negative.

## Pattern 3 — enumerating distinct values and frequencies

```csharp
var bag = new Multiset<char> { 'a', 'a', 'b', 'c', 'c', 'c' };

foreach (char distinct in bag.Distinct().OrderBy(c => c))
    Console.Write(distinct);                          // → abc

foreach (KeyValuePair<char, int> freq in bag.Frequencies().OrderBy(p => p.Key))
    Console.WriteLine($"{freq.Key} × {freq.Value}");
// → a × 2
// → b × 1
// → c × 3
```

Enumerating the multiset directly (`foreach (var item in bag)`) yields each element repeated according to its count. `Distinct()` and `Frequencies()` run in O(`DistinctCount`) and do not expand multiplicity; their order is **not guaranteed** (apply `OrderBy` as above when stable output matters). Both are fail-fast: mutating the bag mid-enumeration throws <xref:System.InvalidOperationException>.

A typical reporting shape — top-N most frequent:

```csharp
var top2 = bag.Frequencies()
    .OrderByDescending(p => p.Value)
    .ThenBy(p => p.Key)
    .Take(2);

Console.WriteLine(string.Join(", ", top2.Select(p => $"{p.Key}×{p.Value}")));
// → c×3, a×2
```

## Pattern 4 — multiset algebra

Multiset operations combine counts rather than just membership. Each returns a new `Multiset<T>` (using the left operand's comparer) and mutates neither operand:

```csharp
var a = new Multiset<int> { 1, 1, 2, 3 };
var b = new Multiset<int> { 1, 2, 2, 4 };

static void Dump(Multiset<int> m) =>
    Console.WriteLine(string.Join(", ",
        m.Frequencies().OrderBy(p => p.Key).Select(p => $"{p.Key}×{p.Value}")));

Dump(a.Sum(b));        // → 1×3, 2×3, 3×1, 4×1
Dump(a.Union(b));      // → 1×2, 2×2, 3×1, 4×1
Dump(a.Intersect(b));  // → 1×1, 2×1
Dump(a.Except(b));     // → 1×1, 3×1
```

| Operation | Resulting count of each element |
|---|---|
| `Sum` | sum of the two counts |
| `Union` | maximum of the two counts |
| `Intersect` | minimum of the two counts |
| `Except` | left count minus right count (floored at zero) |

The operations are well defined only when both operands use equivalent comparers — combining a case-sensitive bag with a case-insensitive one produces comparer-dependent results.

## Pattern 5 — case-insensitive counting

```csharp
var tally = new Multiset<string>(StringComparer.OrdinalIgnoreCase);
tally.Add("Error");
tally.Add("ERROR");
int errors = tally.CountOf("error");   // → 2
```

## Complexity

| Operation | Cost | Notes |
|---|---|---|
| `Add` / `Remove` / `RemoveAll` | O(1) average | Hash insert / update on the backing `Dictionary<T, int>`. |
| `Contains` / `CountOf` | O(1) average | Single hash lookup. |
| `Count` / `DistinctCount` | O(1) | Maintained incrementally. |
| `Distinct()` / `Frequencies()` | O(d) | d = `DistinctCount`; multiplicity is not expanded. |
| Full enumeration / `CopyTo` | O(n) | n = `Count`; each element yielded once per occurrence. |
| `Sum` / `Union` | O(d₁ + d₂) | Distinct counts of both operands. |
| `Intersect` / `Except` | O(d₁) | Iterates the left operand's distinct elements only. |

## When *not* to use it

- **A hand-rolled `Dictionary<T, int>` is enough** when a single method only increments and reads counts. `Multiset<T>` earns its keep once you also need the total `Count` alongside `DistinctCount`, removal that automatically drops zero-count entries, multiplicity-aware enumeration and `CopyTo`, `ICollection<T>` interop, or the algebra operations — all of which the hand-rolled dictionary forces you to re-implement (and keep consistent) yourself.
- **Duplicates should be rejected, not counted.** Use `HashSet<T>`, or [`IndexedSet<T>` / `OrderedSet<T>`](ordered-sets.md) when insertion order matters.
- **Sorted frequency order is needed continuously.** The bag is hash-ordered; a `SortedDictionary<T, int>` keeps keys sorted on every read instead of paying an `OrderBy` per query.
- **`null` elements.** The element type is constrained to `notnull`; a bag cannot count `null`.
- **Concurrent writers.** The type is not thread-safe; synchronize externally, or use `ConcurrentDictionary<T, int>` with interlocked updates when contention is the dominant concern.

## API summary

| Member | Description |
|---|---|
| `Multiset()` / `Multiset(IEqualityComparer<T>?)` | Empty bag with default or explicit comparer. |
| `Multiset(IEnumerable<T>[, IEqualityComparer<T>?])` | Seeds the bag from a sequence, one count per element. |
| `Add(T)` / `Add(T, int)` | Adds one, or a given count, of an element. |
| `Remove(T)` | Removes a single copy; returns `false` if absent. |
| `RemoveAll(T)` | Removes every copy of an element. |
| `Contains(T)` | Whether the element is present at least once. |
| `CountOf(T)` | The multiplicity of a specific element (`0` if absent). |
| `Count` | Total element count including duplicates. |
| `DistinctCount` | Number of distinct elements. |
| `Distinct()` | Enumerates the distinct elements. |
| `Frequencies()` | Enumerates `(element, count)` pairs. |
| `Sum` / `Union` / `Intersect` / `Except` | Multiset algebra returning a new `Multiset<T>`. |
| `Comparer` | The active `IEqualityComparer<T>`. |
| `Clear()` / `CopyTo(T[], int)` | Standard collection surface. |

## See also

- [Indexed and ordered sets](ordered-sets.md) — when duplicates should be *rejected* rather than counted.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Core Foundations guides](../topics/core-foundations.md) — every guide in this topic.
- [Core Foundations topic overview](../../docs/topics/core-foundations.md) — package map and install command.
- [Bodu.Core introduction](../../docs/core/index.md) — namespaces, headline types, scenarios.
- [`Multiset<T>` API reference](xref:Bodu.Collections.Generic.Multiset`1)
- [`Bodu.Collections.Generic` namespace landing](xref:Bodu.Collections.Generic)
