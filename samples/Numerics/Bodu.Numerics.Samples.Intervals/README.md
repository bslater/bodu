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
--- Interval<T> - closed, open, half-open ---
  What   : Builds the same 0-to-10 span three ways, asks each whether it contains its own endpoints, then tests two
           adjacent spans for overlap under two different boundary conventions.
  Why    : Whether an endpoint is included is not a detail - it decides whether adjacent ranges touch or overlap,
           and getting it wrong double-counts the boundary. That is the bug behind a reading filed in two buckets,
           an event billed to two periods, or a schedule that claims a conflict it does not have. Making the
           convention part of the type means it travels with the value instead of living in a comment beside a pair
           of comparisons.
  Expect : The three spans differ only at the endpoints, and that is enough to change the answers: [0,10] and
           [10,20] overlap at 10, while [0,10) and [10,20] tile without touching. The empty interval contains
           nothing at all, including the values its bounds name.

  closed    : [0, 10]
  open      : (0, 10)
  half-open : [0, 10)
  Contains(10): closed=True, open=False, half-open=False  (the upper endpoint: in for closed, out for the other two - one character of syntax, three different answers)
  Contains(0) : closed=True, open=False, half-open=True  (and the lower endpoint, where half-open sides with closed rather than open)
  [0,10] overlaps [10,20] : True  (expected True - both claim 10, so these two ranges double-count their shared boundary)
  [0,10) overlaps [10,20] : False  (expected False - the half-open form tiles cleanly, which is why it is the right default for buckets and billing periods)
  empty     : ∅ (IsEmpty=True, Contains(0)=False)  (an empty interval contains nothing at all, including the values its own bounds name)
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
--- Interval<T> - intersection, difference, union ---
  What   : Takes two overlapping spans and computes their intersection, union, difference and symmetric difference,
           then converts the multi-part results into an IntervalSet.
  Why    : Only intersection is guaranteed to yield a single interval. A union of disjoint spans, and any difference
           that removes a middle section, produce two pieces - which is why these operations return a pair type
           rather than one interval, and why hand-written range subtraction so often quietly drops a fragment. The
           boundary bookkeeping is the other half: removing [4,20] from [0,10] must leave 4 itself outside the
           result, so the remainder is half-open.
  Expect : a minus b is [0, 4) - note the open end, because 4 belongs to b. The symmetric difference has two parts
           and its Count says so, and ToIntervalSet turns that pair into a set that answers Contains across both
           pieces.

  a = [0, 10], b = [4, 20]
  a intersect b        : [4, 10]  (expected [4, 10] - the only one of these operations that always yields a single interval)
  a union b (|)         : [0, 20]  (one piece here only because a and b overlap; disjoint inputs would give two)
  a minus b            : [0, 4) (Count=1)  (expected [0, 4) - OPEN at 4, because 4 belongs to b; this is the boundary bookkeeping hand-written subtraction gets wrong)
  a symmetric-diff b   : [0, 4) ∪ (10, 20] (Count=2)  (two pieces, and Count says so - which is why these return a pair type rather than one interval)
  ...ToIntervalSet()   : [0, 4) ∪ (10, 20]
  set.Contains(2)      : True, set.Contains(12): True, set.Contains(15): True  (the set answers across both pieces without the caller checking each one)
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
--- DiscreteInterval<T> - countable ranges ---
  What   : Enumerates a small integer range, merges two adjacent ranges and two separated ones, then removes a
           middle section.
  Why    : Over a countable domain, adjacency means something it cannot mean over the reals: [1,5] and [6,10] have
           nothing between them, so they merge into [1,10] even though they do not overlap. A continuous interval
           type cannot make that call, because 5.5 exists. This is what makes the discrete form the right one for
           day numbers, record ids, ports and version numbers - and it is also why such a range has a Count and can
           be enumerated at all.
  Expect : [1,5] and [6,10] merge because they are adjacent in the integers; [1,5] and [7,10] do not, because 6 is
           missing. Removing [4,6] from [1,10] leaves two closed pieces rather than the half-open ones a continuous
           domain would produce.

  range        : [3, 8] (First=3, Last=8, Count=6)  (Count is 6, not 5 - a closed integer range includes both ends, the classic fencepost)
  members      : 3, 4, 5, 6, 7, 8  (enumerable at all only because the domain is countable)
  [1,5] u [6,10] merged : True -> [1, 10]  (expected True -> [1, 10]: adjacent in the integers with nothing between them, so they merge without overlapping)
  [1,5] u [7,10] merged : False  (expected False - 6 is missing, so these stay separate; over the reals neither pair could ever merge)
  [1,10] minus [4,6]    : [1, 3] ∪ [7, 10] (Count=2)  (two CLOSED pieces - a continuous domain would have to leave half-open ends instead)
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
--- IntervalSet<T> - normalized unions ---
  What   : Builds a set from overlapping and adjacent spans, then unions, intersects, subtracts and complements it.
  Why    : A set keeps itself normalized: overlapping and touching pieces are coalesced on every operation, so the
           representation is canonical and two sets covering the same values are equal regardless of how they were
           built. A plain list of intervals gives none of that - it grows fragments with each edit, and answering
           Contains means scanning all of them. The complement is the operation that needs the type most, since it
           has to invent unbounded pieces at both ends.
  Expect : The input spans collapse into two pieces on construction. Complementing produces three, including the two
           infinite tails, which is exactly what a list-of-ranges implementation cannot represent without a special
           case at each end.

  normalized set       : [0, 8] ∪ [12, 15] (Count=2)  (the input spans coalesced on construction, so the representation is canonical and equality is meaningful)
  Contains(6)          : True, Contains(10): False, Contains(13): True  (10 falls in the gap between the two pieces; the set checks them without the caller scanning)
  union [9,12]         : [0, 8] ∪ [9, 15]  (adding a span that bridges the gap re-normalizes rather than appending a third fragment)
  intersect [4,13]     : [4, 8] ∪ [12, 13]  (clipping can leave more pieces than it started with, which is why the result is a set)
  except [2,4]         : [0, 2) ∪ (4, 8] ∪ [12, 15]  (removing an interior span splits a piece in two and opens both new edges)
  complement           : (-∞, 0) ∪ (8, 12) ∪ (15, +∞)  (three pieces including two infinite tails - what a plain list of ranges cannot represent without a special case at each end)
```

**APIs demonstrated.** `IntervalSet<T>.Of`, `IntervalSet<T>.Count`, `IntervalSet<T>.Contains`,
`IntervalSet<T>.Union`, `IntervalSet<T>.Intersect`, `IntervalSet<T>.Except`,
`IntervalSet<T>.Complement`.

## Layout

```text
Bodu.Numerics.Samples.Intervals/
  Program.cs                     # runs the scenarios in order
  SampleConsole.cs               # the what/why/expect banner every scenario opens with
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
