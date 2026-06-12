---
title: Range-keyed lookups
---

# Range-keyed lookups

`Bodu.Collections.Generic` ships a small family of types for non-overlapping range storage: `Range<T>` and `ValueRange<TKey, TValue>` for the value-type building blocks, `RangeDictionary<TKey, TValue>` for half-open key ranges mapping to values, and `RangeSet<T>` for half-open key ranges as a set with union / intersect / except.

Reach for these when point lookups need to *find the range that contains a key* rather than match the key exactly — version-band lookups, tariff brackets, time bucketing, address-range routing, tier classification.

## `Range<T>` — the half-open interval value type

```csharp
using Bodu.Collections.Generic;

Range<int> band = new(startInclusive: 100, endExclusive: 200);
band.Contains(99);    // False
band.Contains(100);   // True  — lower bound included
band.Contains(199);   // True
band.Contains(200);   // False — upper bound excluded
band.ToString();      // "[100, 200)"
```

`Range<T>` is a `readonly struct` over any `IComparable<T>`. The constructor validates `start < end`; equal-bound ranges are rejected at construction, since the half-open form `[x, x)` is empty.

For the closed-closed / open-open / open-closed / closed-open form factor, reach for [`Interval<T>`](../../guides/numerics/interval.md) in `Bodu.Numerics`. `Range<T>` is intentionally narrower — half-open only, no empty-set canonicalisation — to match the typical storage-key semantics where `RangeDictionary` and `RangeSet` need adjacent ranges to coexist without overlap.

## `ValueRange<TKey, TValue>` — range paired with a value

```csharp
using Bodu.Collections.Generic;

ValueRange<int, string> band = new(100, 200, "tier-2");
band.Contains(150);   // True
band.Value;           // "tier-2"
band.ToString();      // "[100, 200) = tier-2"
```

`ValueRange<TKey, TValue>` is the projection returned by `RangeDictionary` enumeration. It is a `readonly struct` and supports the same `Contains` test as `Range<T>`.

## `RangeDictionary<TKey, TValue>` — range-to-value lookup

```csharp
using Bodu.Collections.Generic;

var brackets = new RangeDictionary<decimal, decimal>
{
    { 0m,        18_200m, 0.00m   },   // tax-free
    { 18_200m,   45_000m, 0.19m   },   // 19%
    { 45_000m,  120_000m, 0.325m },    // 32.5%
    { 120_000m, 180_000m, 0.37m  },    // 37%
    { 180_000m, decimal.MaxValue, 0.45m },
};

decimal rate = brackets[37_500m];   // 0.19m — point lookup; throws on miss
```

The indexer is a *point lookup*: it walks the sorted range list (binary-searched on the endpoint comparer) to find the range that contains the queried key, and returns its value. `KeyNotFoundException` is thrown when the key falls outside every range.

Use `TryGetValue` or `TryGetEntry` when a miss is expected:

```csharp
if (brackets.TryGetValue(37_500m, out decimal rate))
    Console.WriteLine($"Rate: {rate:P}");

if (brackets.TryGetEntry(37_500m, out ValueRange<decimal, decimal> entry))
    Console.WriteLine($"In range {entry.StartInclusive}-{entry.EndExclusive}, rate {entry.Value:P}");
```

### Non-overlap invariant

Overlapping ranges throw `ArgumentException` on `Add`. Adjacent ranges are allowed — `[0, 5)` and `[5, 10)` coexist because they share an endpoint but not a value.

```csharp
brackets.Overlaps(40_000m, 50_000m);   // True — straddles the [18_200, 45_000) and [45_000, 120_000) seams
brackets.Add(40_000m, 50_000m, "oops"); // throws ArgumentException
```

`Overlaps` lets you test before adding so the failure path is `return false` rather than `catch`.

### Capacity and enumeration

```csharp
brackets.Count;                                    // Number of stored ranges
brackets.Capacity;                                 // Allocated slot count
brackets.EnsureCapacity(64);                       // Pre-size
brackets.GetEntryAt(0);                            // First range in sorted order
ValueRange<decimal, decimal>[] snap = brackets.ToArray(); // Snapshot

foreach (ValueRange<decimal, decimal> band in brackets)
    Console.WriteLine($"{band.StartInclusive}-{band.EndExclusive}: {band.Value:P}");
```

Enumeration is in ascending endpoint order, regardless of insertion order.

## `RangeSet<T>` — non-overlapping range membership

```csharp
using Bodu.Collections.Generic;

var weekdays = new RangeSet<DateTime>();
weekdays.Add(new(2026, 1, 5),  new(2026, 1, 10));   // Mon-Fri week 1
weekdays.Add(new(2026, 1, 10), new(2026, 1, 17));   // adjacent; coalesces

weekdays.Count;                                      // 1 — adjacent merged
weekdays.Contains(new DateTime(2026, 1, 12));        // True
```

`RangeSet<T>` differs from `RangeDictionary` in two ways: there is no value associated with each range, and *adjacent ranges automatically merge* on `Add`. Insertion-order is preserved internally only as far as merging permits.

### Set algebra

```csharp
using Bodu.Collections.Generic;

var a = new RangeSet<int>(new[] { new Range<int>(0, 50), new Range<int>(100, 150) });
var b = new RangeSet<int>(new[] { new Range<int>(30, 120) });

RangeSet<int> union = a.Union(b);          // [0, 150)
RangeSet<int> inter = a.Intersect(b);      // [30, 50), [100, 120)
RangeSet<int> diff  = a.Except(b);         // [0, 30)
```

Each set operation returns a new `RangeSet<T>`; the operands are not mutated. The implementation is O(n + m) on the sizes of the operands — a single linear merge over the sorted range arrays.

### Other operations

```csharp
set.Add(new Range<int>(20, 40));     // convenience overload
set.Remove(10, 25);                   // remove or split intersecting range
set.Contains(15);                     // point membership
set.Contains(0, 10);                  // exact range membership
set.Overlaps(0, 100);                 // overlap test
set.ToArray();                        // snapshot in ascending order
```

`Remove(start, end)` is destructive on overlapping ranges — it carves the requested interval out, splitting an existing range into two if the removed interval is fully contained.

## Performance

| Operation | `RangeDictionary` | `RangeSet` |
|---|---|---|
| Point lookup / containment | O(log n) | O(log n) |
| `Add(range)` | O(n) worst-case (insert + validate non-overlap) | O(n) worst-case (insert + merge adjacent) |
| `Remove` | O(n) worst-case | O(n) worst-case |
| `Union` / `Intersect` / `Except` | — | O(n + m) |
| Enumeration | O(n) | O(n) |

For workloads that are dominated by point lookups, the binary search is the headline cost; insertion is rare. For workloads dominated by set algebra over many ranges, the linear merge sets the bound.

## Worked example — version banding

```csharp
using Bodu.Collections.Generic;

// Map (numeric) build numbers to symbolic versions.
var versions = new RangeDictionary<int, string>
{
    { 1000, 1100, "v1.0" },
    { 1100, 1200, "v1.1" },
    { 1200, 1300, "v1.2 LTS" },
    { 1300, 1310, "v1.3 (yanked)" },
    { 1310, 1400, "v1.3.1" },
};

string Symbolic(int build) => versions.TryGetValue(build, out string v) ? v : "unknown";

Symbolic(1175);   // "v1.1"
Symbolic(1250);   // "v1.2 LTS"
Symbolic(1305);   // "v1.3 (yanked)"
Symbolic(999);    // "unknown"
```

## When *not* to use this family

- **Exact-key lookups.** Use `Dictionary<TKey, TValue>` or `SortedDictionary<TKey, TValue>` — both are simpler and faster when the key matches a stored value verbatim.
- **Overlapping ranges.** Both `RangeDictionary` and `RangeSet` enforce non-overlap. For interval trees that admit overlapping intervals (e.g. event-scheduling conflict resolution), reach for a dedicated interval-tree implementation.
- **Closed-closed or open-open intervals.** Use [`Interval<T>`](../../guides/numerics/interval.md) for the full four-form bounded-interval surface; these types are half-open by design.

## See also

- [`Range<T>` API reference](xref:Bodu.Collections.Generic.Range`1)
- [`RangeDictionary<TKey, TValue>` API reference](xref:Bodu.Collections.Generic.RangeDictionary`2)
- [`RangeSet<T>` API reference](xref:Bodu.Collections.Generic.RangeSet`1)
- [`ValueRange<TKey, TValue>` API reference](xref:Bodu.Collections.Generic.ValueRange`2)
- [`Interval<T>` guide](../../guides/numerics/interval.md) — the closed-closed / open-open / half-open numeric interval type.
- [`Bodu.Collections.Generic` namespace landing](xref:Bodu.Collections.Generic)
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
