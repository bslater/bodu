---
title: Multiset
---

# Multiset

<xref:Bodu.Collections.Generic.Multiset`1> (a *bag*) is a set that retains duplicates as **multiplicity** rather than discarding them. Adding an element that is already present increments its count instead of being rejected. It is the right tool for frequency counting, inventory-style tallies, and multiset algebra (sum, union, intersection, difference) where the number of copies matters.

Equality is governed by an `IEqualityComparer<T>` supplied at construction, so counting can be case-insensitive or structural.

## Pattern 1 — counting occurrences

```csharp
using Bodu.Collections.Generic;

var words = new Multiset<string>();

foreach (string token in "the cat sat on the mat".Split(' '))
    words.Add(token);

int the   = words.CountOf("the");   // 2
int total = words.Count;            // 6 — includes multiplicity
int kinds = words.DistinctCount;    // 5 — distinct elements only
```

`Count` reports the total number of elements including duplicates; `DistinctCount` reports the number of *distinct* elements.

## Pattern 2 — adding and removing with explicit multiplicity

```csharp
var inventory = new Multiset<string>();

inventory.Add("widget", count: 10);   // add ten at once
inventory.Add("widget");              // now 11

bool removedOne = inventory.Remove("widget");     // removes a single copy → 10
bool removedAll = inventory.RemoveAll("widget");  // removes every copy → 0, returns true
```

`Remove` removes a single copy and returns `false` when the element is absent; `RemoveAll` removes every copy of the element at once.

## Pattern 3 — enumerating distinct values and frequencies

```csharp
var bag = new Multiset<char> { 'a', 'a', 'b', 'c', 'c', 'c' };

foreach (char distinct in bag.Distinct())
    Console.WriteLine(distinct);                  // a, b, c

foreach (KeyValuePair<char, int> freq in bag.Frequencies())
    Console.WriteLine($"{freq.Key} × {freq.Value}");  // a × 2, b × 1, c × 3
```

Enumerating the multiset directly (`foreach (var item in bag)`) yields each element repeated according to its count.

## Pattern 4 — multiset algebra

Multiset operations combine counts rather than just membership. Each returns a new `Multiset<T>`:

```csharp
var a = new Multiset<int> { 1, 1, 2, 3 };
var b = new Multiset<int> { 1, 2, 2, 4 };

Multiset<int> sum       = a.Sum(b);       // counts added:   1×3, 2×3, 3×1, 4×1
Multiset<int> union     = a.Union(b);     // counts max'd:    1×2, 2×2, 3×1, 4×1
Multiset<int> intersect = a.Intersect(b); // counts min'd:    1×1, 2×1
Multiset<int> except    = a.Except(b);    // counts subtracted: 1×1, 3×1
```

| Operation | Resulting count of each element |
|---|---|
| `Sum` | sum of the two counts |
| `Union` | maximum of the two counts |
| `Intersect` | minimum of the two counts |
| `Except` | left count minus right count (floored at zero) |

## Pattern 5 — case-insensitive counting

```csharp
var tally = new Multiset<string>(StringComparer.OrdinalIgnoreCase);
tally.Add("Error");
tally.Add("ERROR");
int errors = tally.CountOf("error");   // 2
```

## API summary

| Member | Description |
|---|---|
| `Add(T)` / `Add(T, int)` | Adds one, or a given count, of an element. |
| `Remove(T)` | Removes a single copy; returns `false` if absent. |
| `RemoveAll(T)` | Removes every copy of an element. |
| `Contains(T)` | Whether the element is present at least once. |
| `CountOf(T)` | The multiplicity of a specific element. |
| `Count` | Total element count including duplicates. |
| `DistinctCount` | Number of distinct elements. |
| `Distinct()` | Enumerates the distinct elements. |
| `Frequencies()` | Enumerates `(element, count)` pairs. |
| `Sum` / `Union` / `Intersect` / `Except` | Multiset algebra returning a new `Multiset<T>`. |
| `Comparer` | The active `IEqualityComparer<T>`. |
| `Clear()` / `CopyTo(T[], int)` | Standard collection surface. |

## Where to go next

- [Indexed and ordered sets](ordered-sets.md) — when duplicates should be *rejected* rather than counted.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
