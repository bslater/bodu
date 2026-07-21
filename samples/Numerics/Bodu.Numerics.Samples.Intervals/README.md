# Bodu.Numerics.Samples.Intervals

The interval algebra from `Bodu.Numerics`: `Interval<T>` over a continuous domain,
`DiscreteInterval<T>` over the integers, and the normalized `IntervalSet<T>` — with the two-piece
result types `IntervalPair<T>` / `DiscreteIntervalPair<T>` that a subtraction or symmetric difference
can produce. Four scenarios cover boundary-aware membership, the set operations on a pair of
intervals, the discrete-only merge-on-adjacency behaviour, and normalized interval sets.

Everything runs offline with fixed inputs — deterministic output every run.

```bash
dotnet run --project samples/Numerics/Bodu.Numerics.Samples.Intervals
```

## Scenario 1 — IntervalBasics

**Intent.** Show `Interval<T>` over a continuous domain: the closed / open / half-open factories, the
empty interval, and the boundary-aware `Contains` and `Overlaps` predicates that respect endpoint
inclusivity.

**What it does.** Builds `[0, 10]`, `(0, 10)`, and `[0, 10)` from the factories, probes `Contains`
at both endpoints (where the three sets disagree), tests `Overlaps` against `[10, 20]` (a shared
excluded endpoint is not an overlap), and shows the empty interval containing nothing.

**What to expect.** `10` is a member only of the closed set; `0` is a member of the closed and
closed-open sets; `[0,10]` overlaps `[10,20]` at the shared point `10` but `[0,10)` does not; and the
empty interval prints as `∅`:

```text
--- Interval<T>: closed, open, half-open ---
closed    : [0, 10]
open      : (0, 10)
half-open : [0, 10)
Contains(10): closed=True, open=False, half-open=False
Contains(0) : closed=True, open=False, half-open=True
[0,10] overlaps [10,20] : True
[0,10) overlaps [10,20] : False
empty     : ∅ (IsEmpty=True, Contains(0)=False)
```

**APIs demonstrated.** `Interval<T>.Closed` / `.Open` / `.ClosedOpen`, `Interval<T>.Empty`,
`Interval<T>.Contains(T)`, `Interval<T>.Overlaps`, `Interval<T>.IsEmpty`.

## Scenario 2 — SetAlgebra

**Intent.** Show the two-interval set operations. Intersection is always a single interval, but
subtracting or symmetric-differencing two intervals can leave *two* disjoint pieces — which is
exactly what `IntervalPair<T>` carries, ready to bridge to an `IntervalSet<T>`.

**What it does.** Takes `a = [0, 10]` and `b = [4, 20]`, computes `Intersect` (one interval), the
convex-hull union via `operator |` (one interval), `Difference` (`a` minus `b`, one piece here), and
`SymmetricDifference` (two pieces). It then calls `ToIntervalSet()` on the pair and queries
membership across the resulting set.

**What to expect.** Intersection is `[4, 10]`; the union hull is `[0, 20]`; `a` minus `b` is the
single left piece `[0, 4)`; and the symmetric difference is the genuine two-piece pair `[0, 4) ∪
(10, 20]`, which membership then confirms:

```text
--- Interval<T>: intersection, difference, union ---
a = [0, 10], b = [4, 20]
a intersect b        : [4, 10]
a union b (|)         : [0, 20]
a minus b            : [0, 4) (Count=1)
a symmetric-diff b   : [0, 4) ∪ (10, 20] (Count=2)
...ToIntervalSet()   : [0, 4) ∪ (10, 20]
set.Contains(2)      : True, set.Contains(12): True, set.Contains(15): True
```

**APIs demonstrated.** `Interval<T>.Intersect`, `operator |`, `Interval<T>.Difference`,
`Interval<T>.SymmetricDifference`, `IntervalPair<T>.Count`, `IntervalPair<T>.ToIntervalSet`,
`IntervalSet<T>.Contains`.

## Scenario 3 — DiscreteIntervals

**Intent.** Show `DiscreteInterval<T>` over the integers. Because the domain is countable, an
interval has a first and last member and an exact `Count`, and — crucially — adjacent intervals with
no integer between them merge into one, unlike the continuous `Interval<T>`.

**What it does.** Builds `[3, 8]`, reads its endpoints and count, walks every member from `First` to
`Last`, then merges `[1, 5]` with the adjacent `[6, 10]` via `TryUnion` (there is no integer
between 5 and 6, so they fuse) and shows a gapped pair `[1, 5]` / `[7, 10]` staying separate. It ends
with a `Difference` that leaves two pieces in a `DiscreteIntervalPair<T>`.

**What to expect.** `[3, 8]` has `Count = 6` and members `3..8`; `[1,5]` and `[6,10]` merge to `[1,
10]`; the gapped union reports `False`; and `[1, 10]` minus `[4, 6]` splits into `[1, 3] ∪ [7, 10]`:

```text
--- DiscreteInterval<T>: countable ranges ---
range        : [3, 8] (First=3, Last=8, Count=6)
members      : 3, 4, 5, 6, 7, 8
[1,5] u [6,10] merged : True -> [1, 10]
[1,5] u [7,10] merged : False
[1,10] minus [4,6]    : [1, 3] ∪ [7, 10] (Count=2)
```

**APIs demonstrated.** `DiscreteInterval<T>.Closed`, `DiscreteInterval<T>.First` / `.Last` /
`.Count`, `DiscreteInterval<T>.TryUnion`, `DiscreteInterval<T>.Difference`,
`DiscreteIntervalPair<T>.Count`.

## Scenario 4 — IntervalSets

**Intent.** Show `IntervalSet<T>` as a normalized union of disjoint intervals: overlapping or
touching pieces coalesce automatically, membership is one query across the whole set, and the
set-algebra operators return new normalized sets.

**What it does.** Builds a set from the overlapping `[0, 5]` and `[3, 8]` (which coalesce into
`[0, 8]`) plus the disjoint `[12, 15]`, queries membership, then applies `Union`, `Intersect`,
`Except`, and `Complement`.

**What to expect.** Construction normalizes to `[0, 8] ∪ [12, 15]`; `Union` folds `[9, 12]` in
(it touches `[12, 15]` and coalesces to `[9, 15]`); `Intersect` masks to `[4, 8] ∪ [12, 13]`;
`Except` punches a hole to give `[0, 2) ∪ (4, 8] ∪ [12, 15]`; and `Complement` inverts the set over
the reals with unbounded end pieces:

```text
--- IntervalSet<T>: normalized unions ---
normalized set       : [0, 8] ∪ [12, 15] (Count=2)
Contains(6)          : True, Contains(10): False, Contains(13): True
union [9,12]         : [0, 8] ∪ [9, 15]
intersect [4,13]     : [4, 8] ∪ [12, 13]
except [2,4]         : [0, 2) ∪ (4, 8] ∪ [12, 15]
complement           : (-∞, 0) ∪ (8, 12) ∪ (15, +∞)
```

**APIs demonstrated.** `IntervalSet<T>.Of`, `IntervalSet<T>.Count`, `IntervalSet<T>.Contains`,
`IntervalSet<T>.Union`, `IntervalSet<T>.Intersect`, `IntervalSet<T>.Except`,
`IntervalSet<T>.Complement`.

## Layout

```text
Bodu.Numerics.Samples.Intervals/
  Program.cs                     # runs the scenarios in order
  Scenarios/IntervalBasics.cs
  Scenarios/SetAlgebra.cs
  Scenarios/DiscreteIntervals.cs
  Scenarios/IntervalSets.cs
```

## Related

- `Bodu.Numerics.Samples.Fractions` — the exact-rational `Fraction<T>` over the same numeric surface.
- `Bodu.Numerics.Samples.JsonConverters` — round-tripping `Interval<T>` / `DiscreteInterval<T>` /
  `IntervalSet<T>` through `System.Text.Json` with the companion serialization package.
```
