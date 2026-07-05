---
title: Interval tree
---

# Interval tree

`IntervalTree<T>` stores closed intervals `[low, high]` that may freely overlap and answers the two questions that define the structure: **stabbing** ("which intervals contain point *x*?" — `QueryPoint`) and **overlap-window** ("which intervals intersect `[a, b]`?" — `QueryOverlaps`), each in O(log n + k) for k reported intervals. `IntervalTree<TKey,TValue>` is the value-carrying counterpart, where each stored interval carries a payload — a meeting name, a reservation id, an IP-range owner.

It fits problems where overlap *is* the data: meeting-room and resource scheduling (find every booking that clashes with a proposed slot), genomic and time-series annotation (all features covering a coordinate), version/date applicability ranges (all rules in force on a given day), and network ranges (all CIDR-derived ranges containing an address).

## Overlap-storing vs range-map — which range type do I want?

The Bodu family has three interval-shaped surfaces, and they answer different questions:

| Type | Overlaps | Answers | Reach for it when |
|---|---|---|---|
| `IntervalTree<T>` / `IntervalTree<TKey,TValue>` | **Stored as-is** | "Which of my possibly-overlapping intervals hit this point/window?" | The overlaps themselves are the data (bookings, annotations, applicability windows). |
| [`RangeSet<T>` / `RangeDictionary<TKey,TValue>`](range-dictionary.md) | Merged / rejected | "Which single range does this key fall in, and what value does it map to?" | Ranges partition the key space — tax bands, IP block → owner, grade boundaries. |
| `Bodu.Numerics`' `IntervalSet<T>` | Normalized to disjoint | Interval *algebra* — union/intersect/complement over a set treated as one region | You care about the covered region, not the individual intervals. |

`IntervalTree` is the only member that *stores* overlapping intervals; the other two deliberately refuse to. If an insert failing (or merging) on overlap sounds right, you want the range map; if it sounds like data loss, you want the interval tree.

Contract points worth keeping in mind:

- **Closed endpoints.** `[low, high]` contains both endpoints; `low == high` is a valid single-point interval, and a window touching an interval at one shared endpoint counts as an overlap. `Add`, `Remove`, `Contains`, and the window queries all reject `low > high` (under the active comparer) with `ArgumentException`.
- **Duplicates are permitted.** `IntervalTree<T>` counts occurrences per distinct (low, high) node (`Remove` drops one occurrence per call); `IntervalTree<TKey,TValue>` keeps a per-interval value list in insertion order — the same slot may carry many values, `Remove(low, high)` drops the first stored entry, and `Remove(low, high, value)` drops the first entry matching under `EqualityComparer<TValue>.Default`. `Count` includes every stored entry.
- **Exact-match vs overlap.** `Contains`/`Remove` match the exact (low, high) pair only; `IntersectsPoint(x)` / `Intersects(a, b)` are the O(log n) early-exit "does anything overlap?" forms.
- **Queries are lazy, ascending by (low, high), and fail-fast** — mutating the tree mid-iteration throws `InvalidOperationException` on the next advance, and a fresh iteration re-resolves against the current state, matching the family convention. Full enumeration follows the same order, duplicates repeated.
- Endpoints are ordered by an `IComparer<T>` fixed at construction (default `Comparer<T>.Default`); `null` endpoints are rejected.
- Not thread-safe.

The backing structure — a max-endpoint augmented red-black tree reusing the navigable-collections node machinery — is recorded in the design note at `Bodu.Collections/docs/interval-tree-design.md`.

## Pattern 1 — conflict detection and stabbing

```csharp
using Bodu.Collections.Generic;

var bookings = new IntervalTree<int, string>();
bookings.Add(9, 11, "stand-up");
bookings.Add(10, 12, "design review");   // overlaps stand-up — stored, not merged
bookings.Add(14, 15, "1:1");

// Everything happening at 10:00.
foreach ((int low, int high, string name) in bookings.QueryPoint(10))
    Console.WriteLine($"[{low}, {high}] {name}");   // stand-up, design review

// Cheap yes/no before enumerating: does a proposed 11-13 slot clash at all?
if (bookings.Intersects(11, 13))                    // true — design review reaches 12,
    Console.WriteLine("conflict");                  // and 11 touches stand-up's end (closed)
```

## Pattern 2 — window queries over annotations

```csharp
using Bodu.Collections.Generic;

var features = new IntervalTree<long>();
features.Add(1_000, 5_000);
features.Add(4_500, 4_800);    // nested inside the first — both retained
features.Add(9_000, 12_000);

// All features intersecting the viewport [4_600, 9_000], ascending by (low, high).
foreach ((long low, long high) in features.QueryOverlaps(4_600, 9_000))
    Console.WriteLine($"[{low}, {high}]");   // [1000, 5000], [4500, 4800], [9000, 12000]
```

## Pattern 3 — duplicate slots and targeted removal

```csharp
using Bodu.Collections.Generic;

var shifts = new IntervalTree<int, string>();
shifts.Add(22, 30, "alice");
shifts.Add(22, 30, "bob");       // same slot, different value
shifts.Add(22, 30, "alice");     // same slot, same value — a third distinct entry

shifts.Remove(22, 30, "bob");    // removes exactly bob's entry
shifts.Remove(22, 30);           // removes the first remaining entry (the older "alice")

Console.WriteLine(shifts.Count); // 1
```

## Related pages

- [Choosing a collection](choosing-a-collection.md) — the family-wide decision guide, including the overlap-storing vs range-map row.
- [Range-keyed lookups](range-dictionary.md) — the non-overlapping `RangeSet<T>` / `RangeDictionary<TKey,TValue>` maps.
- [Navigable set](navigable-set.md) — the order-statistic red-black machinery this tree's backing derives from.
