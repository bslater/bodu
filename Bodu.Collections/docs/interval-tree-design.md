# Interval tree — design note

**Date:** 2026-07-05
**Status:** Executed alongside implementation — `IntervalTree<T>` and `IntervalTree<TKey,TValue>` ship with this note.
**Relates to:** [`roadmap-implementation-plan.md`](../../Bodu.Core/docs/roadmap-implementation-plan.md) §8 T6.b; [`ROADMAP.md`](../../ROADMAP.md) — *Per-project roadmap → `Bodu.Core`*

This note records the endpoint model, backing structure, duplicate policy, and query contracts for the
overlap-storing interval trees, and fixes their boundary against the existing range family.

## 1. Endpoint model and family boundary

**Intervals are closed on both ends**: `[low, high]` contains every point `x` with `low <= x <= high` under the
active comparer, and `low == high` is a valid degenerate interval. Endpoints are `T : notnull` values ordered by
an `IComparer<T>` injected at construction (defaulting to `Comparer<T>.Default`) — the same comparer policy as
the navigable collections. This is deliberately simpler than `Bodu.Numerics`' interval algebra, which carries
per-endpoint open/closed metadata: a collection whose job is indexing does not need boundary-kind bookkeeping,
and closed-closed is the natural reading for scheduling, calendar, and version-range workloads.

**Distinctness within the family** (restating the plan document): `RangeSet<T>` / `RangeDictionary<TKey,TValue>`
are sorted **non-overlapping** maps — they reject or merge overlapping inserts — and `Bodu.Numerics`'
`IntervalSet<T>` normalizes its contents to disjoint ranges. `IntervalTree<T>` is the **only member of the
family that stores overlaps**: every added interval is retained as-is, and the point of the type is answering
"which of the stored, freely overlapping intervals hit this point/window?".

## 2. Backing structure: max-endpoint augmented red-black tree

The backing is the proven navigable-collections red-black machinery (parent pointers, bottom-up CLRS fixups),
**duplicated** per the shared-core decision recorded in
[`navigable-collections-design.md`](navigable-collections-design.md) §7, with two changes:

- The order-statistic `Size` field is replaced by a **`Max` field** — the greatest high endpoint in the node's
  subtree (self included). `Max` is the augmentation that prunes query descents.
- The ordering key is the **(low, high) lexicographic pair**, so enumeration and query results are deterministic
  even among intervals sharing a low endpoint.

`Max` maintenance is the correctness crux:

- **Rotations** recompute both participants locally — the demoted node first (its children are final), then the
  promoted node (which reads the demoted node's fresh value). With exact children, a rotation preserves exactness.
- **Insert** folds the new high endpoint into the ancestor chain *before* the insertion fixup runs (stopping at
  the first ancestor whose `Max` already covers it, since `parent.Max >= child.Max` is invariant), so every
  rotation during the fixup reads exact values.
- **Delete** finishes with a bottom-up `RecomputeMaxUpward` walk from the splice point (or the unlinked phantom's
  parent) to the root. Any stale value produced mid-fixup lives on that ancestor chain, so the single O(log n)
  walk restores exactness for every case: leaf, one-child splice, and the two-children successor-copy reduction.

## 3. Duplicate policy

Both types **permit duplicates of the same (low, high) interval** — a scheduler genuinely holds two meetings in
the same slot. The storage differs per type, and the asymmetry is deliberate:

- **`IntervalTree<T>`** keeps one node per distinct (low, high) pair with a per-node **multiplicity count**
  (the `Multiset<T>` approach). `Add` of an existing interval increments the count; `Remove` removes **one
  occurrence**, decrementing the count and deleting the node only at zero. Equal-key sibling nodes are never
  created, so tree shape is independent of duplicate volume.
- **`IntervalTree<TKey,TValue>`** keeps a per-node **value list** in insertion order. The same (low, high) with
  different — or equal — values is permitted; `Remove(low, high)` removes the first stored entry, and
  `Remove(low, high, value)` removes the first entry whose value matches under
  `EqualityComparer<TValue>.Default`. The node is deleted when its list empties.

`Count` on both types is the total number of stored entries, duplicates included.

## 4. Query semantics

- **Stabbing** — `QueryPoint(x)`: every stored interval containing `x`, i.e. `low <= x <= high` (both ends
  inclusive).
- **Overlap window** — `QueryOverlaps(a, b)`: every stored interval intersecting the closed window `[a, b]`,
  i.e. `low <= b && high >= a`; touching at a single shared endpoint counts. The window requires `a <= b`
  (`ArgumentException`, `Arg_Invalid_RangeLowerBoundExceedsUpperBound`) — the same guard `Add` applies to
  stored intervals.
- Both queries are **lazy, ascending by (low, high), and fail-fast** on structural mutation mid-iteration
  (`Op_Invalid_CollectionModified`), matching the family's live-view convention. Enumeration is a pruned
  in-order walk: a left subtree is skipped when its `Max` falls short of the window's low edge, and the walk
  stops outright at the first node whose low endpoint passes the window's high edge (in-order lows are
  non-decreasing). Cost is O(log n + k) for k reported entries in the common case, O((k + 1) log n) worst case.
- **`IntersectsPoint(x)` / `Intersects(a, b)`** are the early-exit boolean forms — the classic single-descent
  CLRS interval search, O(log n) regardless of how many intervals match.

## 5. Enumeration order

Full enumeration yields all stored entries ascending by (low, high) — duplicates repeated per multiplicity
(unkeyed) or per value in insertion order (keyed) — via the family's non-allocating struct enumerator with
version fail-fast.

## 6. Differential test strategy

The correctness gate for the `Max` augmentation is a seeded Regression sweep per type
(`IntervalTreeTests.DifferentialSweep.cs` / `IntervalTreeGenericTests.DifferentialSweep.cs`): thousands of
weighted add/remove operations over integer endpoints mirrored against a brute-force `List` oracle, with
checkpoints comparing `Count`, full enumeration order, and batteries of `QueryPoint` / `QueryOverlaps` /
`Intersects` probes against linear scans. The linear scan cannot be wrong, so any `Max` value corrupted by a
rotation or fixup path surfaces as a missed (or phantom) query hit at the next checkpoint. BVT-tier member
tests pin the deterministic cases: the window-relation matrix (disjoint/touching/contained/containing on each
side), boundary stabs, duplicate accounting, and the targeted delete shapes.
