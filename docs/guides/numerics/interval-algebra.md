---
title: Interval algebra
---

# Interval algebra

This guide works through the set algebra on
[`Interval<T>`](xref:Bodu.Numerics.Interval`1) — chained `Intersect`,
`TryUnion` over adjacent and disjoint operands, containment, gap and
adjacency detection, and a complete "merge overlapping windows"
walk-through. For the single-operation basics — constructing intervals,
membership, formatting, parsing — see
[Working with `Interval<T>`](interval.md); this page builds on those and
goes deeper.

Two conventions thread through everything below.

- **The canonical empty interval.** Every interval that admits no value —
  inverted bounds, or equal bounds with at least one open endpoint —
  compares equal to [`Interval<T>.Empty`](xref:Bodu.Numerics.Interval`1.Empty),
  and `default(Interval<T>)` reads as empty. `Intersect` returns it for
  disjoint operands; `TryUnion` treats it as the identity element.
- **Half-open is the partitioning shape.** Adjacent closed-open intervals
  `[a, b)` and `[b, c)` cover `[a, c)` with no overlap and no gap, which is
  what makes the merge algorithm at the end clean. When endpoint values tie,
  `Intersect` keeps the *stricter* (open) side so the result is a subset of
  both operands, and `TryUnion` keeps the *looser* (closed) side so the
  result is a superset of both.

## Chained intersection

`Intersect` is associative and commutative, so a sequence of constraints
folds cleanly. It is also idempotent (`a.Intersect(a) == a`) and has
`Empty` as its absorbing element (`a.Intersect(Empty) == Empty`), which
together make any fold order valid. The intersection narrows
monotonically — each operand can only shrink the running result — and
collapses to `Empty` the moment two constraints disagree:

```csharp
using Bodu.Numerics;

// Three independent constraints on an allowed value.
var atLeastTen   = Interval<int>.Closed(10, 100);   // [10, 100]
var underNinety  = Interval<int>.ClosedOpen(0, 90);  // [0, 90)
var evenDecade   = Interval<int>.Closed(20, 80);    // [20, 80]

Interval<int> allowed = atLeastTen
    .Intersect(underNinety)   // [10, 90)
    .Intersect(evenDecade);   // [20, 80]

Console.WriteLine(allowed);   // "[20, 80]"
```

Because `Empty` is absorbing under intersection, a single conflicting
constraint short-circuits the whole chain to the empty set:

```csharp
var conflict = atLeastTen
    .Intersect(Interval<int>.Closed(200, 300));  // disjoint with [10, 100]

Console.WriteLine(conflict.IsEmpty);             // True
Console.WriteLine(conflict.Intersect(evenDecade).IsEmpty);  // True — stays empty
```

To fold an arbitrary list of constraints, seed the accumulator with a
range that contains everything you care about (or with the first element)
and `Intersect` across the rest:

```csharp
static Interval<int> IntersectAll(IEnumerable<Interval<int>> constraints)
{
    using var e = constraints.GetEnumerator();
    if (!e.MoveNext())
        return Interval<int>.Empty;   // no constraints — nothing to intersect

    Interval<int> result = e.Current;
    while (e.MoveNext())
        result = result.Intersect(e.Current);

    return result;
}
```

## Union of adjacent versus disjoint intervals

`TryUnion` returns `true` only when the union of the two operands is itself
a single contiguous interval — that is, when they overlap *or* are
adjacent. It never synthesizes a two-piece result; a true gap returns
`false` and leaves `result` as `Empty`.

```csharp
// Adjacent — the half-open seam at 5 belongs to the second interval.
bool a = Interval<int>.ClosedOpen(1, 5)
    .TryUnion(Interval<int>.Closed(5, 10), out var merged);
// a == true, merged == [1, 10]

// Overlapping — shared interior.
bool b = Interval<int>.Closed(1, 6)
    .TryUnion(Interval<int>.Closed(4, 10), out var overlap);
// b == true, overlap == [1, 10]

// Disjoint with a gap — no contiguous representation exists.
bool c = Interval<int>.Closed(1, 5)
    .TryUnion(Interval<int>.Closed(8, 10), out var gapped);
// c == false, gapped == Interval<int>.Empty
```

The adjacency rule is precise: two intervals are adjacent when the upper
endpoint of one equals the lower endpoint of the other **and at least one
of those endpoints is inclusive**. That single-inclusive requirement is
why two half-open intervals that meet at a point still union, while two
intervals that both *exclude* the meeting point do not:

```csharp
// [1, 5) and [5, 10] — 5 is owned by the second interval → contiguous.
Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.Closed(5, 10), out _);   // true

// (1, 5) and (5, 10] — 5 is in neither interval → a one-point gap.
Interval<int>.Open(1, 5).TryUnion(Interval<int>.OpenClosed(5, 10), out _);     // false
```

On endpoint ties the union keeps the looser side, so unioning a closed and
an open interval over the same bounds yields the closed result:

```csharp
Interval<int>.Closed(1, 5).TryUnion(Interval<int>.Open(1, 5), out var u);
// u == [1, 5] — inclusive wins on both ends
```

Unlike `Intersect`, `TryUnion` is *partial*: it is defined only when the
result is a single contiguous interval. `Empty` is its identity (a union
with `Empty` returns the other operand and `true`), but because the
operation can fail on a gap, folding a list of intervals into a minimal
cover requires sorting first — see the worked example below. This is the
structural reason `TryUnion` returns `bool` rather than an
`Interval<T>`: there is no contiguous value to return for a disjoint
pair.

## Containment and overlap

`Contains(Interval<T>)` tests the subset relation; `Overlaps` tests for a
shared value. The two answer different questions and a worked comparison
makes the distinction concrete:

```csharp
var outer = Interval<int>.Closed(0, 10);
var inner = Interval<int>.Closed(2, 8);
var straddle = Interval<int>.Closed(8, 20);

outer.Contains(inner);    // True  — every value of inner is in outer
outer.Overlaps(inner);    // True  — they obviously share values

outer.Contains(straddle); // False — straddle reaches 20, outside outer
outer.Overlaps(straddle); // True  — they share [8, 10]

outer.Contains(Interval<int>.Empty);  // True — ∅ ⊆ every set
outer.Overlaps(Interval<int>.Empty);  // False — ∅ shares no value
```

Note that touching at a boundary is *not* overlap. `[1, 5)` and `[5, 10]`
are adjacent — they union — but they share no value, so `Overlaps` returns
`false`. Use `Overlaps` to detect double-booking and `TryUnion` (or the
adjacency test below) to detect mergeable neighbours.

## Detecting gaps and adjacency

`Interval<T>` exposes overlap, intersection, and union directly. Adjacency
and the gap between two disjoint intervals are derived from the public
endpoint properties. The following helpers express both as small,
self-documenting functions:

```csharp
// Two intervals are adjacent when they do not overlap yet still union.
static bool AreAdjacent(Interval<int> a, Interval<int> b) =>
    !a.Overlaps(b) && a.TryUnion(b, out _);

// The gap between two disjoint, non-adjacent intervals, as an open interval.
// Returns Empty when the intervals touch, overlap, or are out of order.
static Interval<int> GapBetween(Interval<int> a, Interval<int> b)
{
    if (a.IsEmpty || b.IsEmpty || a.Overlaps(b) || a.TryUnion(b, out _))
        return Interval<int>.Empty;

    // Order the operands so the lower one is first.
    (Interval<int> lo, Interval<int> hi) = a.Upper <= b.Lower ? (a, b) : (b, a);

    // The gap excludes whichever endpoints the neighbours already claim.
    return new Interval<int>(
        lo.Upper, hi.Lower,
        lowerInclusive: !lo.UpperInclusive,
        upperInclusive: !hi.LowerInclusive);
}
```

```csharp
AreAdjacent(Interval<int>.ClosedOpen(1, 5), Interval<int>.Closed(5, 10)); // True
AreAdjacent(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 10));     // False

GapBetween(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 10));
// [5, 8] excluded on the claimed ends → (5, 8) → "(5, 8)"
```

## Worked example — merging overlapping windows

The canonical interval-set task is *normalising* an unsorted list of
ranges into the smallest set of disjoint intervals that covers the same
values. This is the algorithm behind merging calendar bookings,
coalescing byte ranges in a download, or collapsing overlapping log
windows.

The recipe is the classic sweep: sort by lower endpoint, then walk the
sorted list folding each interval into the current run with `TryUnion`.
Because `TryUnion` returns `false` exactly when the next interval neither
overlaps nor abuts the current run, a `false` is the signal to close the
run and start a new one:

```csharp
using Bodu.Numerics;

static List<Interval<int>> Merge(IEnumerable<Interval<int>> windows)
{
    // Drop empties; they contribute nothing and would not sort meaningfully.
    var sorted = windows
        .Where(w => !w.IsEmpty)
        .OrderBy(w => w.Lower)
        .ThenBy(w => w.Upper)
        .ToList();

    var merged = new List<Interval<int>>();
    if (sorted.Count == 0)
        return merged;

    Interval<int> current = sorted[0];
    for (int i = 1; i < sorted.Count; i++)
    {
        if (current.TryUnion(sorted[i], out Interval<int> union))
        {
            current = union;          // overlaps or abuts — extend the run
        }
        else
        {
            merged.Add(current);      // a real gap — close the run
            current = sorted[i];
        }
    }

    merged.Add(current);
    return merged;
}
```

Running it over a deliberately messy input — out of order, overlapping,
adjacent, and disjoint — produces the normalised cover:

```csharp
var input = new[]
{
    Interval<int>.Closed(10, 14),     // disjoint tail
    Interval<int>.ClosedOpen(1, 5),   // [1, 5)
    Interval<int>.Closed(5, 8),       // adjacent to [1, 5) at 5
    Interval<int>.Closed(3, 6),       // overlaps both above
    Interval<int>.Closed(20, 22),     // disjoint
};

foreach (var window in Merge(input))
    Console.WriteLine(window);

// [1, 8]
// [10, 14]
// [20, 22]
```

The first three inputs collapse into `[1, 8]`: `[1, 5)` and `[3, 6]`
overlap into `[1, 6]`, which abuts `[5, 8]` into `[1, 8]`. `[10, 14]` and
`[20, 22]` survive as separate runs because a genuine gap separates them —
exactly the `false` return from `TryUnion` that closes a run.

Sorting by lower endpoint is what guarantees a single forward pass is
enough: once the list is ordered, any interval that can merge with the
current run will appear before any interval that cannot, so the algorithm
never has to look back.

## See also

- [Working with `Interval<T>`](interval.md) — single-operation basics: construction, membership, formatting, parsing.
- [Generic math with `Fraction<T>` and `Interval<T>`](generic-math-constraints.md) — writing range and ratio code against the `INumber<T>` abstractions.
- [`Interval<T>` API reference](xref:Bodu.Numerics.Interval`1) and the [`Interval` static factory helpers](xref:Bodu.Numerics.Interval).
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
