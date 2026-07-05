# Bodu.Core roadmap — implementation plan

**Date:** 2026-07-04
**Status:** T0–T4 executed (2026-07-05); T5–T6 proposed
**Relates to:** [`ROADMAP.md`](../../ROADMAP.md) — *Per-project roadmap → `Bodu.Core`*

This plan turns every item in the `Bodu.Core` section of the repository
roadmap into sequenced, scoped work. It covers the two structural
decisions (the `Bodu.Collections` package split and the `WeekPattern`
extraction) and all fourteen feature items, grouped into seven tranches
(T0–T6). Each feature item carries the same uniform template: scope
boundary, API sketch, placement, test plan, documentation obligations,
and an effort/risk note.

None of the proposed items exist in the solution today in any form —
this was verified by search (no natural comparer, bit set, probabilistic
sketch, `BiDictionary`, interval tree, Aho-Corasick automaton, layered
dictionary, skip list, `Table`, TTL expiry, or `Result`/`Option`/`Either`
anywhere in the tree) — so every item below is greenfield inside an
established project.

---

## 0. Ground rules

Every tranche inherits the repository conventions (see
[`CLAUDE.md`](../../CLAUDE.md)); the ones that recur in this plan:

- **File naming.** One public type per file; generic files use the
  `{T}` / `{T,T}` suffix (`BiDictionary{T,T}.cs`); partials are
  `<Base>.<Part>.cs` and nest per `.filenesting.json`; child types
  (enumerators, debug views) split into partial or sibling files, never
  co-located.
- **Namespace–folder alignment.** The namespace is the source of truth;
  folders under `src/` / `test/` are single flat dotted folders
  (`Collections.Generic.Probabilistic`, not nested). Run
  `bld/check-folder-namespace-alignment.sh` after any folder move.
- **Validation.** Public surfaces validate through
  `ThrowHelper.ThrowIf…`; new general-purpose rules become new
  `ThrowHelper` members; all messages come from
  `ResourceStrings.resx` — never string literals.
- **Tests.** MSTest partial classes mirroring the source layout, with a
  member-named backbone per public method/property; new collection
  types plug into the existing contract bases in
  `Bodu.Core/test/Collections.Generic.Contracts/`
  (`CollectionContractTests<>`, `SetContractTests<>`,
  `EnumeratorContractTests<>`, `DebugViewContractTests<>`,
  `NonGenericCollectionContractTests<>`) rather than bespoke harnesses.
  Data-driven rows use the `Bodu.Test.Kat` generics
  (`ValidKat<,>` / `InvalidKat<>` / `BinaryKat<,>`) wired through
  `KatDisplayName`.
- **Tiers.** New tests default to BVT (no category); exhaustive sweeps
  and published vector tables are `[TestCategory("Regression")]`; one
  happy-path `Smoke` test per new primary public type.
- **Documentation.** Every new public type owes full XML docs (build
  breaks on CS1591), a guide page `docs/guides/core/<feature>.md`, and
  — for collection types — a row in
  `docs/guides/core/choosing-a-collection.md`.
- **Commit discipline — incremental, per `CLAUDE.md`.** One branch per
  session; the branch accumulates work across the session with a fresh
  commit per logical step, never one large batch. Applied to this plan:
  - **One work item is never one monolithic commit.** Each lettered
    item below lands as an ordered commit sequence — typically
    *(1)* production type(s) with full XML docs, *(2)* the test
    backbone + contract wiring, *(3)* Regression sweeps / KAT
    catalogues, *(4)* guide + `choosing-a-collection.md` updates.
    Items that touch several types (T1, T3, T5, T6) commit per type,
    not per tranche.
  - **Never mix items in a commit.** Two T2 items in flight on the
    same branch still commit separately, each with a message naming
    the item.
  - **Refactors precede features.** Where an item needs a preparatory
    change to an existing type (e.g. T4's `CacheItem` timestamp
    storage), that lands as its own behaviour-neutral commit before
    the feature commit.
  - Each item's final commit leaves the tree green
    (`dotnet test bodu.slnx --settings bvt.runsettings`).

## 1. Settled decisions

| # | Decision | Position |
| --- | --- | --- |
| D1 | `Bodu.Collections` package split | **Split — execute as T0, before the Wave 1 package cut.** |
| D2 | `WeekPattern` extraction to `Bodu.Globalization.WeekPattern` | **Do not extract — `WeekPattern` stays in `Bodu.Core`.** |
| D3 | Home of `Result<T>` / `Option<T>` / `Either<,>` | **The Core `Functional` seam (namespace `Bodu.Functional`), not a new package.** |
| D4 | Home of the probabilistic sketches | **The new `Bodu.Collections` package, namespace `Bodu.Collections.Probabilistic`.** |

**D1 — do the split.** The collections pillar (`Collections.Generic` +
`.Concurrent` + `.Graphs` + `.Trees`) is already the bulk of Core, and
this plan adds roughly ten more collection types to it (T3, T5, T6).
Meanwhile every other Bodu package depends on Core mainly for
`ThrowHelper`, the buffers, and the extension surfaces. Extracting the
pillar leaves `Bodu.Core` as the small always-referenced primitive
layer and makes the data-structure catalogue an opt-in dependency.
Nothing has been published, so the split is free today and a breaking
change the day after `Bodu.Core/v1.0.0` is tagged — hence T0 gates the
Wave 1 cut. Namespaces do **not** change (`Bodu.Collections.*`
throughout); only the assembly/package boundary moves. Naming inside
the pillar stays BCL-style (`BiDictionary`, `MultiValueDictionary`,
`RangeDictionary`, `Multiset`, `EvictingDictionary`) rather than the
Java synonyms (`BiMap`, `MultiMap`, `RangeMap`, `MultiSet`,
`LruCache`). *Reversal cost:* re-merging after first publish is a
package-identity break for every `Bodu.Collections` consumer — treat
D1 as final once Wave 1 tags.

**D2 — keep `WeekPattern` in Core.** The extraction's stated motive was
letting globalization-adjacent packages take the pattern without
pulling all of Core. D1 dissolves that motive: post-split Core *is* the
small primitive layer, and every Bodu package already references it.
Recording the decision explicitly matters because it is equally one-way
— moving `WeekPattern` after `Bodu.Core/v1.0.0` is breaking. Revisit
only if a concrete external consumer appears that cannot take Core at
all.

**D3 — `Functional` stays a Core seam for now.** The roadmap's *New
library candidates* section reserves a `Bodu.Functional` extraction "if
the seam grows beyond a couple of types". The T1 scope (the trio plus
combinators) is deliberately held inside that boundary — the same
restraint that kept HOTP/TOTP inside `Bodu.Security.Cryptography`
instead of a premature `Bodu.Security.Otp` sibling. The extraction
trigger is documented in §10.

**D4 — sketches live in `Bodu.Collections`.** The roadmap left
"extend the Core pillar vs. a focused `Bodu.Collections.Probabilistic`
package" open. With D1 done, the answer is both: the types ship inside
the `Bodu.Collections` **package** under their own
`Bodu.Collections.Probabilistic` **namespace** (flat folder
`Collections.Probabilistic/`), keeping the catalogue in one assembly
without a third package.

---

## 2. T0 — Structural gate (must land before Wave 1)

> **Executed 2026-07-05** across the commit sequence beginning
> "Scaffold Bodu.Collections src and test projects". Two deviations
> from the sketch below, both discovered during the move:
>
> 1. **`ShuffleHelpers` (and `SequenceUtility`) stayed in Core.** The
>    staying `IEnumerableExtensions` partials (`Randomize`,
>    `ContainsAll`, `ContainsAny`) depend on them, and a partial class
>    cannot span assemblies. Core therefore retains small
>    `Collections.Generic/` and `Collections.Generic.Internal/` folders
>    (a namespace legally spans assemblies).
> 2. **Move order was inverted and partially merged.** Trees and Graphs
>    moved first (their code depends on `Deque<T>` /
>    `IndexedPriorityQueue<,>`, still resolvable in Core through the
>    project reference), while `Collections.Generic` and `.Concurrent`
>    moved together in one commit — their XML-doc crefs reference each
>    other in both directions, so splitting them would have broken doc
>    resolution mid-sequence.
>
> The resx question was resolved as its own `CollectionsResourceStrings`
> pair (28 keys; 24 pruned from Core, 4 retained as shared), no
> cross-package `InternalsVisibleTo` was needed, and combined BVT
> counts match the pre-split baseline exactly (25,679).

### T0.a Execute the `Bodu.Collections` split

**Scope.** Create sibling projects `Bodu.Collections/src/Bodu.Collections.csproj`
and `Bodu.Collections/test/Bodu.Collections.Test.csproj` (standard
`src`/`test` layout, `RootNamespace` `Bodu`, wired into `bodu.slnx`,
`bld/Bodu.props`, and `Directory.Packages.props` exactly like the other
projects). **Move** from `Bodu.Core/src` the concrete data-structure
namespaces and their debug views:

- `Collections.Generic/` (all types — `CircularBuffer{T}`, `Deque{T}`,
  `EvictingDictionary{T,T}`, `IndexedPriorityQueue{T,T}`,
  `IndexedSet{T}`, `MultiValueDictionary{T,T}`, `Multiset{T}`,
  `OrderedSet{T}`, `RangeDictionary{T,T}` / `RangeSet{T}`,
  `SequencedDictionary{T,T}`, `SegmentedBuffer{T}`,
  `RingBackedCollection{T}`, helpers)
- `Collections.Generic.Concurrent/`
- `Collections.Generic.Graphs/`
- `Collections.Generic.Trees/`
- `Collections.Generic.Internal/`

and the matching test folders (including
`Collections.Generic.Contracts/` and
`Collections.Generic.Concurrent.Contracts/`) into the new projects.

**Keep in Core:** the BCL-interface extension surfaces
(`Collections.Generic.Extensions/`, `Collections.Extensions/`), the
buffers, `Text/`, `Threading/`, `Sequences/`, `Functional/`,
`Extensions/`, `ThrowHelper`, `WeekPattern`. `Bodu.Collections`
references `Bodu.Core` (for `ThrowHelper` and `ResourceStrings`
patterns; the new project gets its own resx if any moved message keys
are collection-specific — prefer moving the keys with the types).

**Open sub-question to resolve during the move:** which
`ResourceStrings` keys are used only by moved types (they move to a new
`Bodu.Collections` resx) versus shared (they stay in Core). Resolve by
usage grep, not duplication.

**Tests.** The moved test suite must pass unchanged
(`dotnet test bodu.slnx --settings bvt.runsettings`); no test bodies
change, only project membership. Run
`bld/check-folder-namespace-alignment.sh` across both projects.

**Docs.** Update the project table in `CLAUDE.md` and the `Bodu.Core`
entries in `ROADMAP.md` in a follow-up PR (roadmap edits stay
directional); `docs/guides/core/` pages keep their URLs — add a note to
`docs/guides/core/index.md` that the collection types ship in the
`Bodu.Collections` package.

**Commit sequence.** The move is wide, so it lands as ordered
incremental commits, each leaving the solution building and green:
*(1)* scaffold the two new projects (csproj, slnx, `bld/Bodu.props` /
`Directory.Packages.props` wiring) with empty content; *(2)–(5)* move
one namespace folder pair (src + test) per commit —
`Collections.Generic` **together with** `.Internal` (internal helpers
are not visible across the assembly boundary, so they must travel with
their consumers), then `.Concurrent`, `.Graphs`, `.Trees` — updating
project references as each moves; *(6)* resolve
the resx key split; *(7)* run the alignment script and fix any
fallout; *(8)* doc updates (`CLAUDE.md` table, guides index note).

**Effort & risk.** Medium effort, mechanical but wide (hundreds of
files move). Risk is low — namespaces are untouched, so the only
breakage surface is project wiring (csproj globs for debug-view
partials, `InternalsVisibleTo` if any, embedded resx).

### T0.b Record the `WeekPattern` decision

**Scope.** No code. Add the D2 outcome to `ROADMAP.md`'s `Bodu.Core`
section (retire the extraction bullet, note the rationale) in the same
roadmap-update PR as T0.a's doc changes.

---

## 3. T1 — Functional seam: `Result<T>` / `Option<T>` / `Either<TLeft,TRight>`

> **Executed 2026-07-05** as five incremental commits (Option → Result
> family → Either → async extensions → docs). Deviations from the
> sketch below, each a deliberate design-review outcome:
>
> 1. **`ValueTask` combinator variants deferred** — the async surface
>    is Task-only in v1 (`OptionAsyncExtensions` /
>    `ResultAsyncExtensions`), with no `CancellationToken` parameters
>    (the combinators perform no I/O; cancellation belongs inside the
>    caller's delegates).
> 2. **`Either<TLeft,TRight>` default is an uninitialized-throwing
>    state**, not a value: with no privileged side, `default` reports
>    neither `IsLeft` nor `IsRight` and side-requiring operations throw
>    (the `ImmutableArray<T>.IsDefault` precedent). Option and Result
>    keep total defaults (`None`; failure with empty error).
> 3. **No right-biased `Map`/`Bind` on `Either`** — it exposes only the
>    explicit `MapLeft` / `MapRight`, keeping the railway bias on
>    `Result<T>`.
> 4. **Additions over the sketch:** a non-generic `Result` (void
>    success/failure carrying the factories), the `Option` companion
>    class with `FromNullable`, and a strict non-null rule for all
>    present values with explicitly-documented lenient lifts.

**Scope.** Railway-oriented primitives as `readonly struct`s in
namespace `Bodu.Functional`. In scope: the three value types, their
core combinators, and async (`Task`/`ValueTask`) variants of the
combinators. Out of scope: applicative/monad-transformer stacks,
`Validation`-style error accumulation, and any LanguageExt-scale
surface (§10).

**API sketch.**

- `Option<T>` — `Some(T)` / `None` factories, `IsSome` / `IsNone`,
  `TryGetValue(out T)`, `Map`, `Bind`, `Filter`, `Match(onSome, onNone)`,
  `GetValueOrDefault(...)`, `ToResult(error)`, implicit conversion from
  `T`, `IEquatable<Option<T>>`.
- `Result<T>` — success-or-`Exception`-free error model:
  `Result<T>.Success(T)` / `Result<T>.Failure(ResultError)` (a small
  `ResultError` readonly struct: code + message + optional exception),
  `IsSuccess` / `IsFailure`, `Map`, `Bind`, `MapError`, `Tap` /
  `TapError`, `Match`, `GetValueOrThrow()`, `ToOption()`.
- `Either<TLeft,TRight>` — `Left` / `Right` factories, `IsLeft` /
  `IsRight`, `Map` / `MapLeft`, `Bind`, `Match`, `Swap()`.
- Async companions as extension classes
  (`ResultAsyncExtensions.BindAsync(...)` etc.) so the structs stay
  allocation-free.

**Placement.** `Bodu.Core/src/Functional/` — `Option{T}.cs`,
`Result{T}.cs`, `ResultError.cs`, `Either{T,T}.cs`, plus
`*Extensions.cs` companions; partial splits for operators/equality per
the `WeekPattern` precedent.

**Tests.** `Bodu.Core/test/Functional/` — member backbone
(`OptionTests.Map.cs`, `.Bind.cs`, `.Match.cs`, …) plus subject
partials `OptionTests.Equality.cs` and `ResultTests.Nulls.cs` (the seam
must define `Some(null)` / null-error semantics explicitly). Law-style
sweeps (functor/monad identity + associativity over representative
inputs) as `[DynamicData]` KATs in BVT; one `Smoke` test per type.

**Docs.** `docs/guides/core/functional-results.md` (joins the existing
`memoization.md`).

**Effort & risk.** Medium. The risk is API-taste, not implementation —
the surface should be reviewed against `CSharpFunctionalExtensions`
naming before landing, since this is the most-consumed shape in the
ecosystem and gratuitous divergence will hurt adoption.

---

## 4. T2 — Small additive wins on existing types

> **Executed 2026-07-05** as four incremental commits (one per item).
> Deviations from the sketches below:
>
> 1. **T2.a** additionally mirrors `CircularBuffer<T>`'s `ItemEvicting`
>    (veto-by-throwing) / `ItemEvicted` event pair on the eviction
>    path, keeping the ring-backed family consistent, and refactors the
>    four add members onto a shared `TryAddInternal` core.
> 2. **T2.b** keeps the single `List<TValue>` bucket with a
>    comparer-aware O(n) duplicate scan under `Set` backing — the
>    documented trade-off that preserves the `IReadOnlyList<TValue>`
>    live-view contract, insertion order, and a bit-identical `List`
>    path.
> 3. **T2.c** scopes to ASCII digit runs with a deferred
>    leading-zero tiebreak keeping the order total; culture instances
>    read `CultureInfo.CurrentCulture` per comparison (the
>    `StringComparer` semantic).
> 4. **T2.d** ships `Interleave` only (skip-exhausted round-robin) —
>    no `RoundRobin` alias, since the repo has no alias precedent —
>    and the size parameter is named `size` for consistency with
>    `Windowed`. The BCL-overlap gate ran against the installed
>    .NET 10 ref assembly and is recorded in the commit message.

Four independent items; land in any order after T0.

### T2.a `Deque<T>` overflow policy

**Scope.** Close the Python `deque(maxlen=N)` gap: when fixed-capacity
(`AllowGrow == false`) and full, optionally evict from the *opposite*
end instead of rejecting. No new type.

**API sketch.** `DequeOverflowPolicy` enum (`Reject`,
`EvictOpposite`); a `Deque<T>.OverflowPolicy` property defaulting to
`Reject`, preserving today's behaviour exactly
(`InvalidOperationException` / `Op_Invalid_CapacityExhausted` from
`AddFirst`/`AddLast`; `TryAddFirst`/`TryAddLast` return `false`). Under
`EvictOpposite`, `AddFirst` on a full deque discards the last element
and vice versa; `TryAdd*` always succeed.

**Placement.** `Bodu.Collections/src/Collections.Generic/` (post-T0):
`DequeOverflowPolicy.cs` + edits to `Deque{T}.cs`.

**Tests.** Extend the existing `DequeTests` partials: new backbone file
`DequeTests.OverflowPolicy.cs` covering both policies at both ends,
`Try*` semantics, and enumeration order after eviction.

**Docs.** Update `docs/guides/core/deque.md`.

**Effort & risk.** Small; behaviour-preserving by default.

### T2.b Set-backed values for `MultiValueDictionary<TKey,TValue>`

**Scope.** The type is hard-wired list-backed today
(`Dictionary<TKey, ValueBucket>` with `List<TValue>` buckets; ctors
take only `IEqualityComparer<TKey>?`) — Guava's `ListMultimap`. Add the
`SetMultimap` half as a construction-time backing option; no second
type.

**API sketch.** `MultiValueBacking` enum (`List`, `Set`); new ctor
overloads `MultiValueDictionary(MultiValueBacking backing,
IEqualityComparer<TKey>? keyComparer = null,
IEqualityComparer<TValue>? valueComparer = null)`. Under `Set`,
per-key duplicates (by `valueComparer`) are ignored on `Add` (return
`bool` where the surface allows), and the per-key view is exposed
through the existing `IReadOnlyCollection<TValue>`-compatible surface.
The backing choice is immutable after construction.

**Placement.** Edits to `MultiValueDictionary{T,T}.cs` (+ a
`MultiValueBacking.cs` sibling), `Bodu.Collections` post-T0.

**Tests.** New subject partial
`MultiValueDictionaryTests.Backing.cs` (duplicate suppression, value
comparer plumbing, view behaviour under both backings) plus a
`MultiValueDictionaryTests.Comparer.cs` sweep; existing tests must pass
unchanged (list remains the default).

**Docs.** Update `docs/guides/core/multi-value-dictionary.md`.

**Effort & risk.** Small–medium; the main design check is whether the
per-key view type currently promises `IReadOnlyList<TValue>` publicly —
if so, the set backing exposes an order-preserving set bucket so the
list-typed view contract can be kept.

### T2.c `NaturalStringComparer`

**Scope.** Numeric-aware string ordering (`file2` < `file10`; the
Explorer `StrCmpLogicalW` / Python `natsort` behaviour) as a stateless
comparer. In scope: digit-run comparison (arbitrary length, no numeric
overflow), case-sensitivity options, ordinal and culture-aware letter
comparison. Out of scope: sign/decimal/thousands parsing, version-sort
dotted-tuple semantics.

**API sketch.** `sealed class NaturalStringComparer : IComparer<string?>,
IEqualityComparer<string?>` with static instances
(`NaturalStringComparer.Ordinal`, `.OrdinalIgnoreCase`,
`.CurrentCulture`, `.CurrentCultureIgnoreCase`, plus
`Create(CultureInfo, bool ignoreCase)`), matching the
`StringComparer` factory shape.

**Placement.** `Bodu.Core/src/Extensions/NaturalStringComparer.cs`
(beside `ComparableHelper`; it extends the BCL comparison surface, so
it stays in Core, not `Bodu.Collections`). Namespace `Bodu` root or
`Bodu.Extensions` — follow whichever namespace `ComparableHelper`
declares.

**Tests.** `NaturalStringComparerTests` with a `ValidKat<(string,string),int>`-
driven ordering table (BVT) and a Regression sweep over a published
natsort-style corpus; subject partial `.Nulls.cs` (null ordering
contract) per convention.

**Docs.** `docs/guides/core/natural-string-comparer.md`.

**Effort & risk.** Small; the digit-run-without-overflow comparison is
the only subtle part.

### T2.d Sequence-operator extras

**Scope.** Strictly additive to LINQ and to the operators Core already
ships (`Pairwise`, `Windowed`, `RunLengthEncode`, `ZipLongest`,
`Batch`, `Scan`, `SplitWhen` in
`Collections.Generic.Extensions/IEnumerableExtensions.*` — the
roadmap's "windowed / pairwise projection" and "run-length encoding"
bullets are **already done**). Remaining candidates: `CartesianProduct`
(binary + params overloads), `Permutations` / `Combinations` (k-subset
and full), `Interleave` / `RoundRobin`. **Gate:** each operator is
re-checked against the current BCL (net8/net10 LINQ additions —
`Chunk`, `CountBy`, `AggregateBy`, `Index` are already excluded) before
landing, and the check result recorded in the PR description.

**API sketch.** New `IEnumerableExtensions.<Operator>.cs` partials
following the existing deferred-execution + argument-validation shape
(guards up front, `private static Iterator` core).

**Placement.** `Bodu.Core/src/Collections.Generic.Extensions/` (stays
in Core per T0's split boundary).

**Tests.** One backbone partial per operator under
`Bodu.Core/test/Collections.Generic.Extensions/`, using the local
`EnumerableKat<,>` record; deferred-execution and
multiple-enumeration tests per the existing operator tests' pattern.

**Docs.** Extend the extensions coverage in `docs/guides/core/`.

**Effort & risk.** Small per operator; combinatorial operators need
explicit documented behaviour for `k > n`, empty sources, and result
ordering.

---

## 5. T3 — Probabilistic sketches

> **Executed 2026-07-05** as three incremental commits (one per type:
> BloomFilter → CountMinSketch → HyperLogLog). Hashing landed as the
> shared internal `ProbabilisticHashing` helper — a SplitMix64-style
> avalanche of the comparer's 32-bit hash into the two 64-bit values
> consumed by Kirsch–Mitzenmacher double hashing (HyperLogLog uses the
> first value only). Deviations from the sketch below:
>
> 1. **BloomFilter** ships `EstimatedFalsePositiveRate` (computed from
>    the current bit density) rather than the sketched
>    `ExpectedFalsePositiveRate` name, and an `ApproximateCount` /
>    element-count estimator was deliberately omitted from the surface.
> 2. **HyperLogLog** deliberately omits the original paper's
>    large-range correction — it compensates for collisions in a
>    32-bit hash space, whereas the register pipeline here ranks a
>    64-bit hash (the practical ceiling is instead the 32-bit comparer
>    entropy, documented on the type).
> 3. **HyperLogLog `Import`** additionally validates each register
>    against the rank ceiling for the recorded precision
>    (`64 − b + 1`), rejecting corrupt snapshots that would silently
>    skew every subsequent estimate.

**Scope.** Three approximate-membership/frequency/cardinality
structures: `BloomFilter<T>`, `CountMinSketch<T>`, `HyperLogLog<T>`.
In scope: capacity/error-rate parameterisation, `IEqualityComparer<T>`-
or hash-delegate-based hashing with a documented default, merge/union
of compatible instances, and (Bloom) clear. Out of scope: counting /
deletable Bloom variants, sliding-window sketches, serialization
formats (a `byte[]` export/import round-trip is in scope; wire formats
are not).

**API sketch.**

- `BloomFilter<T>(int expectedItems, double falsePositiveRate, …)` —
  `Add`, `MightContain`, `Clear`, `UnionWith(BloomFilter<T>)`,
  `ExpectedFalsePositiveRate`, `TryExport(Span<byte>)` / `Import`.
- `CountMinSketch<T>(double epsilon, double delta, …)` — `Add(T, long)`,
  `EstimateCount(T)`, `MergeWith`.
- `HyperLogLog<T>(int precision, …)` — `Add`, `EstimateCardinality()`,
  `MergeWith`.

Hashing: double hashing over two 64-bit seeds derived from the
element's hash, so the types take an optional
`IEqualityComparer<T>` and do not depend on `Bodu.IO.Hashing`
(`Bodu.Collections` keeps its single reference to Core).

**Placement.** `Bodu.Collections/src/Collections.Probabilistic/` —
`BloomFilter{T}.cs`, `CountMinSketch{T}.cs`, `HyperLogLog{T}.cs` +
debug-view siblings. Namespace `Bodu.Collections.Probabilistic` (D4).

**Tests.** `Bodu.Collections/test/Collections.Probabilistic/` — member
backbones for the mutation/query members; statistical accuracy sweeps
(observed false-positive rate within tolerance of configured rate;
HyperLogLog error within the standard 1.04/√m bound over seeded random
corpora) as `[TestCategory("Regression")]`; deterministic seeded KATs
in BVT; export/import round-trip via `RoundTripKat<,>`.

**Docs.** `docs/guides/core/probabilistic-collections.md` +
`choosing-a-collection.md` rows (with an explicit
"approximate — do not use for exact membership" caveat).

**Effort & risk.** Medium. The statistical tests must be seeded and
tolerance-based or they will flake; the accuracy bounds go in the
Regression tier only.

---

## 6. T4 — Time-based expiry for `EvictingDictionary<TKey,TValue>`

> **Executed 2026-07-05** as a single commit. Deviations from the
> sketch below:
>
> 1. **The separate behaviour-neutral `CacheItem` refactor commit was
>    collapsed into the feature commit** — a timestamp field with no
>    consumer is dead code the analyzers reject, so no independently
>    buildable neutral precursor existed.
> 2. **`RemoveExpired()` returns `int`** (entries removed), not the
>    sketched `bool`.
> 3. **`Remove(TKey)` stays physical** — it removes an expired-but-
>    unpurged entry and returns `true` (consistent with the raw-`Count`
>    model); the sliding set is exactly `TryGetValue` / indexer get /
>    `ContainsKey`, and `Touch` deliberately does not slide.
> 4. **Expiry removals raise the capacity path's `ItemEvicting` /
>    `ItemEvicted` events** and honour its re-entrancy protocol.

**Scope.** The policy enum already covers the capacity-triggered family
(FIFO / LRU / LFU / MRU / Random / SecondChance); this adds the
*time* dimension: per-entry TTL and sliding expiration, composable with
any existing policy. In scope: a default TTL per dictionary, per-entry
TTL overrides on `Add`/indexer overloads, sliding vs absolute mode,
`TimeProvider`-driven time for testability, lazy purge on access plus
an explicit `RemoveExpired()`. Out of scope: background timer threads
(consumers can call `RemoveExpired()` on their own cadence) and the
W-TinyLFU admission policy (§10).

**API sketch.** `EvictingDictionaryExpiration` options type
(`TimeSpan? TimeToLive`, `ExpirationKind { Absolute, Sliding }`,
`TimeProvider TimeProvider`); ctor overloads accepting it;
`Add(key, value, TimeSpan ttl)` / `TryAdd(...)` overloads;
`bool RemoveExpired()` returning whether anything was purged; expired
entries are invisible to `ContainsKey` / `TryGetValue` / enumeration
even before purge. `Count` semantics (pre- vs post-purge) must be
pinned explicitly in XML docs and tests.

**Placement.** `Bodu.Collections/src/Collections.Generic/` — new
partial `EvictingDictionary{T,T}.Expiration.cs` +
`EvictingDictionaryExpiration.cs`; timestamp storage extends the
existing `CacheItem` partial.

**Tests.** New subject partial
`EvictingDictionaryTests.Expiration.cs` driven by a fake
`TimeProvider` (no real sleeps): absolute vs sliding, per-entry
override beats default, interaction with each capacity policy
(expired entries are preferred victims), enumeration invisibility,
`RemoveExpired` return contract. Existing tests unchanged (no
expiration configured ⇒ behaviour identical).

**Docs.** Update `docs/guides/core/evicting-dictionary.md`.

**Effort & risk.** Medium. The policy-interaction matrix is the risk;
keep expiry orthogonal (a pre-filter before victim selection) rather
than a seventh policy value.

---

## 7. T5 — New map / table / bit types

Four independent items; each is a standalone type (or pair) in
`Bodu.Collections/src/Collections.Generic/` with the standard
debug-view sibling, contract-base test wiring, guide page, and
`choosing-a-collection.md` row. Only the item-specific notes are given
below.

### T5.a `BiDictionary<TKey,TValue>`

Bidirectional one-to-one map. API: forward `IDictionary<TKey,TValue>`
surface; `Inverse` property exposing the reversed live view
(`BiDictionary<TValue,TKey>`-shaped, sharing storage); duplicate-value
policy on construction (`Throw` — default, matching Guava `BiMap` —
vs `ForcePut`-style `Replace`); both comparers injectable. Tests:
member backbone + `BiDictionaryTests.Inverse.cs` (mutations through
either view stay consistent) + `NonGenericCollectionContractTests<>`
wiring. Risk: the inverse-view aliasing contract — document and test
that `Inverse.Inverse` returns the original.

### T5.b `BitSet`

Growable bit set with Java `BitSet` semantics: auto-growing on `Set`;
`Get`/`Set`/`Clear`/`Flip` (single index + range overloads);
`NextSetBit` / `NextClearBit`; `Cardinality`; in-place `And` / `Or` /
`Xor` / `AndNot`; a non-boxing `struct` enumerator over set-bit
indices; `Length` (logical) vs `Capacity` (allocated) distinction.
Backing `ulong[]` with `BitOperations.PopCount`/`TrailingZeroCount`.
Non-generic, so the file is `BitSet.cs` + `BitSet.Enumerator.cs`.
Tests: backbone per member; Regression sweep of randomized
logical-op equivalence against `IEnumerable<bool>` oracle semantics.
Risk: low; well-trodden algorithmics.

### T5.c Layered and defaulting dictionary utilities

Two small types. (1) `LayeredDictionary<TKey,TValue>` — a *read-through
view* over an ordered `IReadOnlyList<IReadOnlyDictionary<TKey,TValue>>`
(first layer wins), implementing `IReadOnlyDictionary<,>`; writes, if
supported at all, go to the first layer only (Python `ChainMap`
semantics) — decide write-through vs read-only during API review and
document the precedence rule with a cross-reference to
`Bodu.Text.Configuration`'s resolver precedence so the two describe
layering the same way. (2) `DefaultingDictionary<TKey,TValue>` — a
wrapper (or subclass-free decorator) whose indexer miss invokes a
`Func<TKey,TValue>` value factory and stores the result (`defaultdict`
— distinct from the existing non-storing `GetOrAdd` extension in
`IDictionaryExtensions`, which stays the lightweight option). Tests:
precedence/enumeration-dedup backbone for the layered view;
factory-invocation counting for the defaulting wrapper. Risk: low;
the design question is only the write surface of the layered view.

### T5.d `Table<TRow,TColumn,TValue>`

Two-key map whose *reason to exist* is the projections — adopt only
with them (roadmap caveat restated): `Row(TRow)` / `Column(TColumn)`
live `IReadOnlyDictionary` views, `RowKeys` / `ColumnKeys` /
`RowMap()` per-row iteration, plus the flat
`this[TRow, TColumn]` / `TryGetValue` / `Add` / `Remove` surface.
Backing: row-major `Dictionary<TRow, Dictionary<TColumn, TValue>>`
with a documented column-view cost (O(rows) per column enumeration —
do not maintain a second index in v1). File
`Table{T,T,T}.cs` + view partials. Tests: backbone + a
`TableTests.Views.cs` subject partial (view liveness, mutation
through views if allowed). Risk: medium-low; the column-view
performance contract must be documented honestly.

---

## 8. T6 — Algorithmic heavyweights

The three largest items. Each warrants its own short design note
(assessment-style, following this document's precedent) before
implementation; the scopes below bound those notes.

### T6.a Navigable / order-statistic sorted collections

**Scope.** `NavigableSet<T>` and `NavigableDictionary<TKey,TValue>`:
nearest-neighbour queries (`TryGetFloor` / `TryGetCeiling` /
`TryGetHigher` / `TryGetLower`), rank/select (`IndexOf` /
`GetAt(int rank)` — k-th smallest), min/max, and cheap ascending /
descending / sub-range views. The **backing-structure choice is the
item's own design decision** (to be made in its design note):
an order-statistic balanced BST gives O(log n) rank/select naturally;
a skip list trades that for a simpler path to the concurrent variant.
The concurrent sorted map (`ConcurrentSkipListMap` analogue) is an
explicit follow-on (§10), but the backing decision should not
foreclose it.

**Placement.** `Bodu.Collections/src/Collections.Generic/` —
`NavigableSet{T}.cs`, `NavigableDictionary{T,T}.cs` + enumerator /
view / debug-view partials.

**Tests.** Contract-base wiring (`SetContractTests<>`,
`EnumeratorContractTests<>`) plus a Regression differential sweep:
randomized operation sequences mirrored against `SortedSet<T>` /
`SortedDictionary<,>` for the overlapping surface and against a naive
sorted-list oracle for floor/ceiling/rank/select.

**Docs.** `docs/guides/core/navigable-collections.md`; the
`choosing-a-collection.md` row must position it against
`SortedSet<T>` (`GetViewBetween` only) and `SortedList<,>` (O(n)
insert).

**Effort & risk.** Large — the biggest single item in this plan.
Ship `NavigableSet<T>` first, then the dictionary over the same node
machinery.

### T6.b `IntervalTree<T>` / `IntervalTree<T,TValue>`

**Scope.** *Overlapping* intervals with stabbing queries ("all
intervals containing x") and overlap-window queries ("all intervals
intersecting [a,b]") in O(log n + k). Distinctness restated:
`RangeSet<T>` / `RangeDictionary<,>` are sorted **non-overlapping**
maps that reject overlapping inserts, and `Bodu.Numerics`'
`IntervalSet<T>` normalizes to disjoint ranges — this type is the
only member of the family that *stores* overlaps. Endpoint model:
closed `[low, high]` over `IComparable<T>`-constrained endpoints
(or an injected `IComparer<T>`), kept deliberately simpler than
Numerics' open/closed metadata — the design note confirms this
boundary. API: `Add`, `Remove`, `QueryPoint(T)`,
`QueryOverlaps(T low, T high)` (both lazy), `Count`, enumeration in
low-endpoint order. Backing: augmented interval tree (max-endpoint
augmented BST).

**Placement.** `Bodu.Collections/src/Collections.Generic/` —
`IntervalTree{T}.cs`, `IntervalTree{T,T}.cs` (value-carrying) +
partials.

**Tests.** Backbone per member; Regression differential sweep against
a brute-force scan oracle over randomized interval corpora;
scheduling-conflict style KATs in BVT.

**Docs.** `docs/guides/core/interval-tree.md`; `choosing-a-collection.md`
must carry the "overlap-storing vs range-map" disambiguation row.

**Effort & risk.** Medium-large; the algorithmics are standard, the
API boundary against the existing range family is the review point.

### T6.c Trie-family extensions: Aho-Corasick + radix compression

**Scope.** Two siblings of the existing `Trie` / `Trie<TValue>` in
`Collections.Generic.Trees`. (1) `AhoCorasickAutomaton` (+
`AhoCorasickAutomaton<TValue>`): build from a pattern set (builder or
`Freeze()`-style two-phase — construction is separate from matching),
then `EnumerateMatches(ReadOnlySpan<char> text)` yielding
(pattern, position) for all patterns simultaneously; immutable once
built. (2) `RadixTrie` / `RadixTrie<TValue>` (PATRICIA-style
path compression): same lookup/prefix surface as `Trie` with
compressed edges — API mirrors `Trie<TValue>` so consumers can swap.

**Placement.** `Bodu.Collections/src/Collections.Generic.Trees/` —
`AhoCorasickAutomaton.cs`, `AhoCorasickAutomaton{T}.cs`,
`RadixTrie.cs`, `RadixTrie{T}.cs` + enumerator/debug-view partials.

**Tests.** Aho-Corasick: overlap-heavy published example sets
(`he/she/his/hers`) as BVT KATs; Regression differential sweep against
per-pattern `string.IndexOf` scanning over randomized corpora.
RadixTrie: reuse/mirror the existing `Trie` test suites plus
compression-specific structure tests (edge splitting on insert).

**Docs.** Extend `docs/guides/core/trie.md` to cover the family;
`choosing-a-collection.md` rows.

**Effort & risk.** Medium each. Aho-Corasick's failure-link
construction is the classic subtle spot — the differential oracle
sweep covers it.

---

## 9. Sequencing summary

| Tranche | Items | Depends on | Notes |
| --- | --- | --- | --- |
| **T0** | Collections split; WeekPattern decision | — | **Gates the Wave 1 package cut.** Nothing else may tag before this lands. |
| **T1** | `Result` / `Option` / `Either` | T0 (lands in Core; order vs T0 is soft — Core files don't move) | Highest external demand; API-taste review first. |
| **T2** | Deque overflow; MVD set backing; natural comparer; sequence extras | T0 (a, b move with the split; c, d stay in Core) | Four independent small items — good parallel/fill-in work. |
| **T3** | Bloom / Count-Min / HyperLogLog | T0 (new namespace in `Bodu.Collections`) | Seeded, tolerance-based Regression accuracy tests. |
| **T4** | EvictingDictionary TTL | T0 | Keep expiry orthogonal to the policy enum. |
| **T5** | BiDictionary; BitSet; layered/defaulting; Table | T0 | Four independent items. |
| **T6** | Navigable collections; IntervalTree; Aho-Corasick + RadixTrie | T0; each gets a short design note first | Largest; T6.a ships set-before-dictionary. |

T1–T2 are the natural next session-sized units after T0; T3–T6 are
ordered by leverage but are mutually independent and reorderable on
demand.

## 10. Follow-ups and explicitly out of scope

- **W-TinyLFU admission policy** for `EvictingDictionary<,>` — stretch
  follow-up to T4, only if benchmarks against BitFaster.Caching show
  the demand.
- **Concurrent sorted map** (`ConcurrentSkipListMap` analogue) —
  follow-on to T6.a; the backing-structure decision there must not
  foreclose it.
- **`Bodu.Functional` package extraction** — triggered only if the T1
  seam grows beyond the trio + combinators (per D3 and the roadmap's
  *New library candidates* entry).
- **Bloom/sketch wire formats** — only the opaque export/import
  round-trip ships in T3.
- **Sqids / identifier encodings, `Bodu.Text.Similarity`, and the
  other net-new library candidates** — different projects' roadmaps;
  not `Bodu.Core` work.
- **`ROADMAP.md` upkeep** — as tranches land, retire the corresponding
  bullets in the `Bodu.Core` section (directional edits, per that
  file's contribution note).
