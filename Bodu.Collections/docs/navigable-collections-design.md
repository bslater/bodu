# Navigable collections — design note

**Date:** 2026-07-05
**Status:** Executed alongside implementation — `NavigableSet<T>` ships with this note; `NavigableDictionary<TKey,TValue>` has shipped as the follow-on (see §7 for the shared-core decision).
**Relates to:** [`roadmap-implementation-plan.md`](../../Bodu.Core/docs/roadmap-implementation-plan.md) §8 T6.a; [`ROADMAP.md`](../../ROADMAP.md) — *Per-project roadmap → `Bodu.Core`*

This note records the backing-structure decision for the navigable / order-statistic sorted
collections and the contracts that flow from it. The plan deliberately left the decision open
between an order-statistic balanced BST and a skip list; this note closes it.

## 1. Backing structure: order-statistic red-black tree

**Decision: a red-black tree with subtree-size augmentation and parent pointers.** The skip list
alternative is rejected for the sequential types.

| Criterion | Order-statistic red-black tree | Skip list |
|---|---|---|
| Floor / ceiling / higher / lower | O(log n), single descent | O(log n) expected |
| Rank / select (`IndexOf` / `GetAt`) | O(log n) **naturally** — read the size fields accumulated on the search path | Requires a second augmentation (per-level width counters); rarely implemented correctly first try |
| Memory | Deterministic: one node per element, fixed five-field layout | Randomized tower heights; expected ~2 pointers/element but with variance, plus width counters for rank |
| Worst case | Guaranteed O(log n) | Probabilistic only — adversarial or unlucky sequences degrade |
| Concurrency path | Poor (rebalancing touches many nodes) | Excellent (lock-free insert/remove is well known) |

Rank/select is a headline API of this item, not an afterthought — the structure that answers it by
construction wins. The skip list's one decisive advantage is lock-free concurrency, which is
irrelevant to a single-threaded collection.

**This decision does not foreclose the concurrent follow-on.** The planned
`ConcurrentSkipListMap` analogue (plan §10) is a *separate implementation* regardless of what backs
the sequential types — it shares no node machinery, exactly as `ConcurrentCircularBuffer<T>`
(Vyukov ring) shares nothing with `CircularBuffer<T>` (plain array ring). The concurrent variant
should use a skip list; the sequential variants should not.

## 2. Node layout

```
Node { T Item; Node? Left; Node? Right; Node? Parent; bool IsRed; int Size; }
```

- **`Size`** is the count of nodes in the subtree rooted at the node (self included). It is
  maintained incrementally: ancestor-chain increments/decrements on insert/remove, and a two-field
  recomputation inside each rotation (`y.Size = x.Size; x.Size = SizeOf(x.Left) + SizeOf(x.Right) + 1`).
  Every rank/select/count-in-range query reads only `Size` fields on a root-to-node path — O(log n).
- **Parent pointers are kept.** The alternative (explicit-stack in-order iteration) saves one
  reference per node but makes every enumerator carry an O(log n) stack, makes descending iteration
  awkward, and makes `Successor`/`Predecessor` unavailable as O(1)-amortized primitives. With parent
  pointers, ascending, descending, and range views are all the same trivial walk. The cost — one
  reference field per node (8 bytes on 64-bit) — is accepted and documented here.
- Rebalancing follows the classic bottom-up red-black insert/delete fixups (CLRS / `java.util.TreeMap`
  shape), using the deleted node as a phantom during the leaf-delete fixup so size recomputation
  inside rotations always sees consistent subtrees.
- The bulk-load constructor sorts, deduplicates, and builds a balanced tree directly in O(n) after
  the O(n log n) sort, coloring only the spill-over level red (the `SortedSet<T>`
  `ConstructRootFromSortedArray` scheme).

## 3. Iteration and range views

- The struct `Enumerator` walks in ascending order via parent-pointer successor steps; it is
  fail-fast, capturing the structural version and throwing `InvalidOperationException`
  (`Op_Invalid_CollectionModified`) on any mutation, matching the family convention.
- `Ascending()`, `Descending()`, and `Range(lowInclusive, highInclusive)` return **live views over
  bound pairs**: each call to `GetEnumerator()` re-resolves against the tree's current state, so a
  view created before a mutation reflects that mutation when iterated afresh. Within a single
  iteration the same fail-fast versioning applies. Bounds are inclusive on both ends; `Range`
  rejects `low > high` (per the set's comparer) eagerly, before any deferred iterator is created.
- `Range` iterates only the in-range subwalk — O(log n + k) for k yielded elements, not O(n).

## 4. Duplicates, null, and comparer policy

- **Set semantics.** Comparer-equal duplicates are rejected: `Add` returns `false` and leaves the
  stored element in place, exactly like `SortedSet<T>`. Elements the comparer orders equal are the
  same element.
- **Null elements are rejected** (`ArgumentNullException` via `ThrowHelper.ThrowIfNull` on every
  element-taking member; `Arg_Invalid_NullCollectionElement` for a null inside a bulk source).
  This **diverges from `SortedSet<T>`**, which permits `null` for reference types when the comparer
  handles it. The divergence is deliberate: the Bodu collection family uniformly constrains
  `T : notnull` and rejects null elements (`OrderedSet<T>`, `IndexedSet<T>`, `Multiset<T>`), and a
  null-permitting sorted set would make floor/ceiling results ambiguous (`TryGetFloor` returning
  `true` with a `null` out value). Consistency and an unambiguous Try-pattern win.
- The comparer is fixed at construction (`IComparer<T>`, defaulting to `Comparer<T>.Default`) and
  exposed via `Comparer`. All membership, navigation, rank, and set-algebra operations use it —
  including the projections built from `other` arguments, so mixed-comparer algebra behaves like
  `SortedSet<T>`'s (the receiver's comparer governs).

## 5. Set algebra

The `ISet<T>` operations are deliberately straightforward: `O(m log n)` element-at-a-time loops, with
the intersection/symmetric-difference/relation operations building a one-shot `NavigableSet<T>`
projection of `other` (O(m log m)) for membership tests under the receiver's comparer. No merge-based
bulk algorithms in v1 — the complexity is honest in the XML docs, and the differential sweep keeps
the semantics pinned should a faster implementation land later.

## 6. Differential test strategy

The correctness gate for the augmented tree is a seeded, mirrored Regression sweep
(`NavigableSetTests.DifferentialSweep.cs`):

- 20,000 mixed operations (add / remove / contains, weighted so the tree grows and shrinks through
  many rebalancing paths) applied simultaneously to the `NavigableSet<T>` and a `SortedSet<T>` mirror.
- At fixed checkpoints: full ordered-content equality, `Count`, `Min`/`Max` against the mirror.
- At each checkpoint additionally: a floor/ceiling/higher/lower/rank/select/count-in-range validation
  pass for a probe battery against a **sorted-array oracle** (binary search) — this is what actually
  verifies the `Size` augmentation survives every rotation and fixup path, which the `SortedSet<T>`
  mirror alone cannot see.

BVT-tier member tests cover the deterministic cases (targeted delete shapes: leaf, one-child, root,
two-children-via-successor; boundary matrices for the four navigation queries; guard sweeps).

## 7. `NavigableDictionary<TKey,TValue>` follow-on

The dictionary reuses the same node machinery with a `KeyValuePair`-shaped payload (or a key/value
pair of fields on the node); nothing in this design is set-specific except the element-equals-key
identity. The navigation surface transposes to keys (`TryGetFloorEntry` etc.); rank/select transposes
to `IndexOfKey` / `GetAt(rank)`. It ships separately, second, per the plan's
"set-before-dictionary" sequencing.

**Shared-core decision (executed with the dictionary):** the rotation/fixup/bulk-build core is
**duplicated** into `NavigableDictionary<TKey,TValue>` with a key/value pair of fields on the node,
rather than extracting a shared generic tree the two types would parameterize. The set's `Node` is a
private nested class whose payload is accessed directly throughout the fixups; genericizing it would
either box the payload access behind an abstraction the JIT must see through or force the committed
set through a churny refactor for two consumers with a stable, ~500-line, differentially-swept core.
The duplication is deliberate and bounded: both cores are pinned by their own 20,000-operation
differential sweeps (`NavigableSetTests.DifferentialSweep` / `NavigableDictionaryTests.DifferentialSweep`),
so a fix to one core that misses the other is caught by the mirrored oracle, and the committed set's
implementation stays byte-for-byte untouched. Revisit extraction only if a third consumer of the
order-statistic core emerges.

Dictionary-specific contracts layered on the shared design:

- **Duplicate keys throw.** `Add` and the bulk-load constructor follow the strict
  `Dictionary<TKey,TValue>` contract (`ArgumentException`, reusing `Arg_Invalid_DuplicateDictionaryKey`);
  `TryAdd` is the non-throwing form and the indexer upserts.
- **Null keys are rejected; null values are allowed** (`ContainsValue` handles `null` via
  `EqualityComparer<TValue>.Default`). `ContainsValue` is an honest, documented O(n) walk.
- **Value overwrite is not a structural mutation.** Assigning an existing key through the indexer
  updates the node's value without bumping the version, matching `Dictionary<TKey,TValue>` —
  in-flight enumerators survive an overwrite but fail fast on add/remove/clear.
- `Keys` / `Values` are cached, live, read-only, key-sorted views (the `SequencedDictionary`
  key/value-collection shape minus the non-generic `ICollection`, which the dictionary — like the
  set — does not implement).
