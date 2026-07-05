---
title: Bodu.Core — Introduction
---

# Bodu.Core

**Bodu.Core** is the foundation package of the Bodu suite and of the **[Core Foundations](../topics/core-foundations.md)** topic — a collection of high-performance, framework-style building blocks for .NET applications. Several other Bodu packages share its primitives: `Bodu.IO.Hashing`, `Bodu.Security.Cryptography`, `Bodu.Globalization.Calendar`, `Bodu.Numerics`, and `Bodu.Financial` all reference `Bodu.Core` for shared types like `ThrowHelper`, `WeekPattern`, the calendar-shape enums, and pooled buffers. See the [package matrix](../package-matrix.md) for the full dependency map.

The library is organized around a family of focused namespaces, each with a clear responsibility.

![Bodu.Core namespace map — focused namespaces and their headline types](../../images/diagrams/core-namespace-map.svg)

## Namespaces and headline types

### `Bodu`
Top-level primitives that don't fit into a sub-namespace.

| Type | Purpose |
|---|---|
| <xref:Bodu.WeekPattern> | Immutable bitmask value type for sets of days of the week. Supports composition (`MTuW`), bitwise operators, parsing, and enumeration. |
| <xref:Bodu.IRandomGenerator> | Abstraction over random number generators — used by collections that need pluggable randomness. |
| <xref:Bodu.XorShiftRandom> | Fast non-cryptographic xor-shift PRNG implementing `IRandomGenerator`. |
| <xref:Bodu.ThrowHelper> | Centralized parameter validation: `ThrowIfNull`, `ThrowIfOutOfRange`, `ThrowIfArrayLengthIsInsufficient`, `ThrowIfEnumValueIsUndefined`, and many more. Uses `[CallerArgumentExpression]` so call sites stay compact. |

### `Bodu.Buffers`
Pooled buffer infrastructure.

| Type | Purpose |
|---|---|
| <xref:Bodu.Buffers.PooledBufferBuilder`1> | `ArrayPool<T>`-backed builder for assembling byte or character spans without allocation. |

### `Bodu.Collections.Generic`
Bounded and ordered collections built around a shared ring-backed primitive.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Generic.CircularBuffer`1> | Fixed-capacity FIFO ring. Configurable to either silently overwrite or throw when full. |
| <xref:Bodu.Collections.Generic.Deque`1> | Double-ended queue with O(1) `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast`. The `AllowGrow` flag toggles between auto-resize and fixed-capacity-throw modes. |
| <xref:Bodu.Collections.Generic.SegmentedBuffer`1> | Segmented buffer for streaming scenarios where total length is not known up front. |
| <xref:Bodu.Collections.Generic.RingBackedCollection`1> | Abstract base shared by `CircularBuffer<T>` and `Deque<T>`. Extension point for new ring-backed collections. |
| <xref:Bodu.Collections.Generic.EvictingDictionary`2> | Capacity-bounded dictionary with FIFO, LRU, LFU, MRU, Random, or Second-Chance eviction. Drop-in cache primitive with standard dictionary semantics. |
| <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> | Enum selecting the eviction policy: `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, `SecondChance`. |
| <xref:Bodu.Collections.Generic.SequencedDictionary`2> | Unbounded insertion- or access-ordered dictionary (Java `LinkedHashMap` shape) with O(1) access to and removal of the first and last entries. Optional access-order mode underpins hand-built LRU caches. |
| <xref:Bodu.Collections.Generic.BiDictionary`2> | Bidirectional one-to-one map (Guava `BiMap` shape) with O(1) lookup in both directions and a live `Inverse` view sharing the same storage. Duplicate-value conflicts resolve by a construction-time `Throw` / `Replace` policy. |
| <xref:Bodu.Collections.Generic.LayeredDictionary`2> | Live read-through view over an ordered list of dictionaries (Python `ChainMap` shape): the first layer containing a key wins on read, all writes go to the first layer only, and removing a shadowing entry unshadows the deeper value. |
| <xref:Bodu.Collections.Generic.DefaultingDictionary`2> | Dictionary with a construction-time value factory (Python `defaultdict` shape): the indexer getter materializes, stores, and returns a default for a missing key; every other member sees only actually-stored entries. |
| <xref:Bodu.Collections.Generic.Table`3> | Two-key row/column map (Guava `Table` shape) whose point is the projections: live `Row` / `Column` read-only dictionary views, `RowKeys` / `ColumnKeys`, and per-row `RowMap()` iteration over a row-major store. Column-axis operations scan every row (O(rows)); empty rows are pruned automatically. |
| <xref:Bodu.Collections.Generic.IndexedSet`1>, <xref:Bodu.Collections.Generic.OrderedSet`1>, <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> | Index-aware set and priority-queue variants for lookup-by-position and key-based priority updates. |
| <xref:Bodu.Collections.Generic.MultiValueDictionary`2>, <xref:Bodu.Collections.Generic.Multiset`1> | Multi-map and multi-set semantics over `IEqualityComparer<TKey>`. |
| <xref:Bodu.Collections.Generic.BitSet> | Growable packed bit set with Java `BitSet` semantics: auto-grow on `Set`/`Flip`, reads beyond capacity return `false`, `NextSetBit` / `NextClearBit` / `Cardinality` queries, in-place `And` / `Or` / `Xor` / `AndNot`, and a non-boxing enumerator over set-bit indices. |
| <xref:Bodu.Collections.Generic.NavigableSet`1> | Comparer-ordered set over an order-statistic red-black tree: O(log n) nearest-neighbour queries (`TryGetFloor` / `TryGetCeiling` / `TryGetHigher` / `TryGetLower`), rank/select (`IndexOf` / `GetAt`), `CountInRange`, `Min`/`Max`, and live fail-fast `Ascending` / `Descending` / `Range` views. |
| <xref:Bodu.Collections.Generic.NavigableDictionary`2> | Key-sorted dictionary over the same order-statistic red-black tree: O(log n) nearest-neighbour entry queries (`TryGetFloorEntry` / `TryGetCeilingEntry` / `TryGetHigherEntry` / `TryGetLowerEntry`, plus key-only variants), rank/select (`IndexOfKey` / `GetAt`), `CountInRange`, `MinEntry`/`MaxEntry`, and live fail-fast `Ascending` / `Descending` / `Range` entry views. Null keys rejected; null values allowed. |
| <xref:Bodu.Collections.Generic.Range`1>, <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1> | Range-keyed lookups for ordered or interval-valued keys. |

### `Bodu.Collections.Generic.Concurrent`
Lock-free / thread-safe variants.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> | Thread-safe variant of `CircularBuffer<T>`; implements `IProducerConsumerCollection<T>` over the Vyukov MPMC algorithm. |

### `Bodu.Collections.Probabilistic`
Approximate "sketch" structures that trade exactness for a fixed memory footprint — each is sized once from its constructor arguments and carries a quantified, one-sided error bound. See the [Probabilistic collections](../../guides/core/probabilistic-collections.md) guide and the <xref:Bodu.Collections.Probabilistic> overview.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Probabilistic.BloomFilter`1> | Approximate set membership sized from an expected item count and target false-positive rate. No false negatives — added elements are always reported present; never-added elements are misreported at roughly the design rate. Supports `UnionWith` merging and version-checked export/import. |
| <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> | Approximate per-element frequency counting sized from `epsilon` / `delta`. Never underestimates; with probability at least `1 − δ` an estimate is at most the true count plus `ε · TotalCount`. Supports `MergeWith` (cell-wise sum) and export/import. |
| <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> | Approximate distinct-element (cardinality) counting in `2^precision` one-byte registers with ~`1.04/√m` relative standard error. `MergeWith` (register-wise max) is lossless and never double-counts shared elements. |

### `Bodu.Collections.Generic.Graphs`
Graphs and graph algorithms. See the [Graphs and graph algorithms](../../guides/core/graphs.md) guide and the <xref:Bodu.Collections.Generic.Graphs> overview.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Generic.Graphs.Graph`1> | Directed or undirected graph with optional non-negative edge weights. |
| <xref:Bodu.Collections.Generic.Graphs.GraphAlgorithms> | BFS / DFS traversal, shortest path, topological sort, and connected components over the read-only graph views. |
| <xref:Bodu.Collections.Generic.Graphs.DisjointSet`1> | Union-find (disjoint-set) with path compression for connectivity and components. |

### `Bodu.Collections.Generic.Trees`
Prefix trees and an n-ary tree. See the [Trie (prefix tree)](../../guides/core/trie.md) guide and the <xref:Bodu.Collections.Generic.Trees> overview.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Generic.Trees.Trie>, <xref:Bodu.Collections.Generic.Trees.Trie`1> | A string set and a string-keyed map with prefix queries (`StartsWith`, `KeysWithPrefix`). |
| <xref:Bodu.Collections.Generic.Trees.Tree`1> | A mutable n-ary tree node with stack-safe pre-/post-/level-order traversals. |

### `Bodu.Threading`
Async coordination primitives — the async-friendly peers of the BCL synchronization types. See the [Async coordination primitives](../../guides/core/async-primitives.md) guide and the <xref:Bodu.Threading> overview.

| Type | Purpose |
|---|---|
| <xref:Bodu.Threading.AsyncLock>, <xref:Bodu.Threading.AsyncSemaphore>, <xref:Bodu.Threading.AsyncReaderWriterLock> | Awaitable mutual-exclusion and bounded-concurrency gates. |
| <xref:Bodu.Threading.AsyncManualResetEvent>, <xref:Bodu.Threading.AsyncAutoResetEvent>, <xref:Bodu.Threading.AsyncCountdownEvent> | Awaitable signalling events. |
| <xref:Bodu.Threading.AsyncLazy`1>, <xref:Bodu.Threading.AsyncDebouncer>, <xref:Bodu.Threading.RateGate> | One-time async initialization, trailing-edge debouncing, and rate limiting. |

### `Bodu.Functional`
Functional helpers and railway primitives. See the [Memoization](../../guides/core/memoization.md) and [Options, results, and eithers](../../guides/core/functional-results.md) guides and the <xref:Bodu.Functional> overview.

| Type | Purpose |
|---|---|
| <xref:Bodu.Functional.Memoizer> | Wraps a pure function in a thread-safe caching delegate (single- and multi-argument). |
| <xref:Bodu.Functional.Option`1> | An optional value — `Some(value)` or `None` — with `Map` / `Bind` / `Filter` / `Match` combinators; `default` equals `None`. |
| <xref:Bodu.Functional.Result>, <xref:Bodu.Functional.Result`1> | Success-or-failure outcomes carrying a value or a <xref:Bodu.Functional.ResultError>; `default` is a failure with an empty error. |
| <xref:Bodu.Functional.ResultError> | The failure descriptor — optional code, never-null message, optional captured exception. |
| <xref:Bodu.Functional.Either`2> | A symmetric disjoint union with `MapLeft` / `MapRight` / `Match` / `Swap`; `default` is an explicit uninitialized state. |
| <xref:Bodu.Functional.OptionAsyncExtensions>, <xref:Bodu.Functional.ResultAsyncExtensions> | Task-based `MapAsync` / `BindAsync` / `MatchAsync` (and `TapAsync`) companions for async pipelines. |

### `Bodu.Collections.Extensions` and `Bodu.Collections.Generic.Extensions`
Sequence-shaping helpers that compose on top of `IEnumerable<T>` and `IList<T>`.

| Type | Purpose |
|---|---|
| <xref:Bodu.Collections.Extensions.IEnumerableExtensions>, <xref:Bodu.Collections.Generic.Extensions.IEnumerableExtensions> | Recursive selection, sliding windows, batched enumeration, and other sequence helpers. |
| <xref:Bodu.Collections.Generic.Extensions.IListExtensions>, <xref:Bodu.Collections.Generic.Extensions.SystemRandomAdapter>, <xref:Bodu.Collections.Generic.Extensions.RandomizationMode> | Pluggable randomness-driven shuffles backed by `IRandomGenerator`. |

### `Bodu.Extensions`
Date, numeric, span, and array extension methods. Larger surface than the others; the highlights:

| Type | Purpose |
|---|---|
| <xref:Bodu.Extensions.DateTimeExtensions> | First / last / next / previous day-of-week within month / quarter / year, ISO week-of-year, day name, weekday tests, midday, end-of-day, truncation. |
| <xref:Bodu.Extensions.DateOnlyExtensions> | `DateOnly`-specific equivalents plus `Age` calculation. |
| <xref:Bodu.Extensions.NumericExtensions> | `ReverseBits`, `RotateBitsLeft` / `Right`, `ReverseBytes`, `GetBytes` for unsigned integer types. |
| <xref:Bodu.Extensions.ArrayExtensions> | `Reverse`, `Clear`, and other in-place array helpers. |
| <xref:Bodu.Extensions.BufferConverter> | Byte / structure conversion helpers. |
| <xref:Bodu.Extensions.SpanExtensions> | Span-friendly helpers. |
| <xref:Bodu.Extensions.IComparableExtensions>, <xref:Bodu.Extensions.ComparableHelper> | `Min`, `Max`, `Clamp`, `IsGreaterThan` / `IsGreaterThanOrEqual`. |
| <xref:Bodu.Extensions.NaturalStringComparer> | Numeric-aware ("natural") string comparer — `file2` sorts before `file10` — with ordinal, case-insensitive, and culture-aware modes. See the [Natural string comparer](../../guides/core/natural-string-comparer.md) guide. |
| <xref:Bodu.Extensions.CalendarQuarterDefinition>, <xref:Bodu.WorkingDaysOfWeek>, <xref:Bodu.Extensions.IWeekendDefinitionProvider>, <xref:Bodu.Extensions.FiscalWeekPattern>, <xref:Bodu.Extensions.WeekOrdinal> | Calendar-shape enums and injection seams for quarter, weekend, fiscal-week, and week-ordinal computations. |

### `Bodu.Globalization.Extensions`
Culture-aware date / calendar helpers built on top of <xref:System.Globalization.DateTimeFormatInfo>.

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Extensions.DateTimeFormatInfoExtensions> | `FirstDayOfWeek`, `LastDayOfWeek`, weekend-aware helpers over `DateTimeFormatInfo`. |

### `Bodu.Text` and `Bodu.Xml.Linq`
Text and XML helpers used internally by the other Bodu packages; available publicly when you need them.

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Encoding.Base16>, <xref:Bodu.Text.Encoding.Base32>, <xref:Bodu.Text.Encoding.Base58>, <xref:Bodu.Text.Encoding.Base64>, <xref:Bodu.Text.Encoding.Base85> | Per-radix codec entry points over text or binary input. Ship in the companion `Bodu.Text.Encoding` package. |
| <xref:Bodu.Text.Encoding.BaseFormatStyles>, <xref:Bodu.Text.Encoding.BaseFormattingOptions> | Formatting-style and option flags consumed by every per-radix codec. |
| <xref:Bodu.Xml.Linq.XmlNamespaceResolver> | `IXmlNamespaceResolver` helper used by the calendar rule parsers. |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Fixed-capacity FIFO ring buffer (single-threaded) | <xref:Bodu.Collections.Generic.CircularBuffer`1> |
| Fixed-capacity FIFO ring buffer (multi-threaded) | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> |
| Double-ended queue with O(1) ends | <xref:Bodu.Collections.Generic.Deque`1> |
| LRU / LFU / FIFO / MRU / Random / Second-Chance cache | <xref:Bodu.Collections.Generic.EvictingDictionary`2> + <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> |
| Index-aware set with O(1) lookup-by-position | <xref:Bodu.Collections.Generic.IndexedSet`1> |
| Range-keyed lookup table | <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1> |
| Multi-map / multi-set semantics | <xref:Bodu.Collections.Generic.MultiValueDictionary`2>, <xref:Bodu.Collections.Generic.Multiset`1> |
| Day-of-week set you can union / intersect / parse | <xref:Bodu.WeekPattern> |
| Pooled byte / char buffer for zero-allocation building | <xref:Bodu.Buffers.PooledBufferBuilder`1> |
| Date arithmetic — first Monday, ISO week-of-year, age | <xref:Bodu.Extensions.DateTimeExtensions>, <xref:Bodu.Extensions.DateOnlyExtensions> |
| Bit / byte rotation and reversal | <xref:Bodu.Extensions.NumericExtensions> |
| Base16 / Base32 / Base58 / Base64 / Base85 encoding | <xref:Bodu.Text.Encoding.Base16>, <xref:Bodu.Text.Encoding.Base32>, <xref:Bodu.Text.Encoding.Base58>, <xref:Bodu.Text.Encoding.Base64>, <xref:Bodu.Text.Encoding.Base85> (in `Bodu.Text.Encoding`) |
| Centralized argument validation in your own code | <xref:Bodu.ThrowHelper> |

## Design principles

A handful of conventions run through the whole package; knowing them up front explains why the types look the way they do.

- **One toggle, not two classes.** Where a collection has to choose between *reject* and *make room* on overflow, that choice is a single settable property — `AllowOverwrite` on <xref:Bodu.Collections.Generic.CircularBuffer`1>, `AllowGrow` on <xref:Bodu.Collections.Generic.Deque`1> — rather than two parallel types. The toggle can be flipped at runtime (grow during warm-up, lock down for steady state), and every throwing operation has a `Try…` peer that substitutes a `false` return.
- **Fail-fast where it is cheap, snapshot where it is not.** The single-threaded collections detect concurrent structural mutation with a version counter and throw <xref:System.InvalidOperationException> from the enumerator — the BCL contract. The lock-free <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> instead enumerates a coherent snapshot and never throws, because a fail-fast token cannot be maintained without a lock.
- **Struct enumerators.** Every collection's `GetEnumerator()` returns a `struct`, so a `foreach` over a concrete-typed variable allocates nothing; enumerating through an `IEnumerable<T>` reference boxes as usual.
- **Reads can mutate.** Recency-based caches (<xref:Bodu.Collections.Generic.EvictingDictionary`2> under LRU/MRU/LFU/SecondChance, <xref:Bodu.Collections.Generic.SequencedDictionary`2> in access-order mode) update ordering metadata on a successful lookup. That is why even concurrent read-read on these types needs external synchronisation.
- **Validation flows through one helper.** Every public entry point validates its arguments through <xref:Bodu.ThrowHelper>, so exception type, message, and parameter-name capture stay uniform across the suite. `ThrowHelper` is also the only dependency the other Bodu packages take on `Bodu.Core`.
- **Pluggable randomness, never a global.** Helpers that need randomness accept an <xref:Bodu.IRandomGenerator> rather than reaching for a static <xref:System.Random>, so tests can inject a deterministic source. Neither shipped implementation is cryptographically secure.

## Where to go next

- **[Core concepts](concepts.md)** — glossary the rest of the documentation assumes.
- **[Getting started](getting-started.md)** — install the package and run a minimal sample for each scenario above.
- **[Bodu.Core guides](../../guides/core/index.md)** — recipe-style walk-throughs for the headline types.
- **[Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic)** — full namespace overview.
- **[Project introduction](../introduction.md)** — how Bodu.Core relates to the hashing, cryptography, calendar, and text libraries (its `ThrowHelper` underpins them all).
- **[Core Foundations topic](../topics/core-foundations.md)** — Bodu.Core alongside its sibling member, the `Bodu.Text` namespace utilities.
