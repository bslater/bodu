---
title: Thread-safety contracts across Core Foundations
---

# Thread-safety contracts across Core Foundations

This page is the one-table answer to "can I share this object between threads?" for every public type in **Bodu.Core**, **Bodu.Collections**, and **Bodu.Collections.Concurrent**. Each row is derived from the type's XML documentation and source — the presence or absence of internal synchronization, whether reads mutate state, and how enumeration behaves under concurrent modification — not from a general assumption.

The columns use these terms:

| Term | Meaning |
|---|---|
| **Safe** | Designed for concurrent use from any number of threads without external locking. |
| **Immutable** | Cannot change after construction; sharing is trivially safe. |
| **Read-only safe** | Concurrent *reads* are safe once no thread is writing; any write (including a *read that mutates* — see the caveat column) needs exclusive access. |
| **Unsafe** | No synchronization at all; every access needs an external lock, or the object must stay confined to one thread. |
| **Fail-fast** | Enumerators capture a structural-version counter and throw <xref:System.InvalidOperationException> on the next `MoveNext` after a structural mutation. Fail-fast detection is a *debugging aid on one thread*, not a concurrency mechanism. |
| **Snapshot** | Enumeration runs over a point-in-time copy and never throws for concurrent modification. |
| **Undefined** | No version counter; mutating during enumeration produces unspecified results. |

> [!IMPORTANT]
> "Read-only safe" only holds when *no* thread mutates. For the collections whose lookups reposition entries (`EvictingDictionary` under a recency policy, `SequencedDictionary` in access order, `DefaultingDictionary`'s indexer, `DisjointSet.Find`), even a lookup is a write — those rows are marked **Unsafe** rather than read-only safe.

## `Bodu.Core`

| Type | Contract | Enumeration | Notes |
|---|---|---|---|
| <xref:Bodu.WeekPattern> | Immutable | Snapshot (value type) | Every operator returns a new value. [Guide](week-pattern.md) |
| <xref:Bodu.WorkingDaysOfWeek>, <xref:Bodu.Extensions.CalendarQuarterDefinition>, <xref:Bodu.Extensions.WeekOrdinal>, <xref:Bodu.Extensions.DateTimeResolution>, <xref:Bodu.Extensions.FiscalWeekPattern>, <xref:Bodu.Extensions.IdentifierCase>, <xref:Bodu.Extensions.TitleCaseOptions>, <xref:Bodu.Extensions.SentenceCaseOptions>, <xref:Bodu.Collections.Generic.Extensions.RandomizationMode>, <xref:Bodu.Collections.Extensions.RecursiveSelectControl>, <xref:Bodu.Threading.AsyncDebouncerExecutionPolicy> | Immutable | — | Enums. |
| <xref:Bodu.Functional.Option`1>, <xref:Bodu.Functional.Result>, <xref:Bodu.Functional.Result`1>, <xref:Bodu.Functional.ResultError>, <xref:Bodu.Functional.Either`2> | Immutable | — | `readonly struct` railway values. [Guide](functional-results.md) |
| <xref:Bodu.Functional.Memoizer> | Safe | — | The cache is thread-safe; under concurrent *first* calls for the same argument the wrapped function may run more than once, but only one result is published. [Guide](memoization.md) |
| <xref:Bodu.Threading.AsyncLock>, <xref:Bodu.Threading.AsyncSemaphore>, <xref:Bodu.Threading.AsyncReaderWriterLock>, <xref:Bodu.Threading.AsyncAutoResetEvent>, <xref:Bodu.Threading.AsyncManualResetEvent>, <xref:Bodu.Threading.AsyncCountdownEvent>, <xref:Bodu.Threading.AsyncLazy`1>, <xref:Bodu.Threading.AsyncDebouncer>, <xref:Bodu.Threading.RateGate> | Safe | — | Coordination primitives — concurrent use is their purpose. `AsyncLazy<T>` runs its factory exactly once under contention; `RateGate` is leading-edge only. [Guide](async-primitives.md) |
| <xref:Bodu.Buffers.PooledBufferBuilder`1> | Unsafe | — | Single-owner builder over a rented array; no synchronization, and `Dispose` returns the buffer. [Guide](pooled-buffer-builder.md) |
| <xref:Bodu.Extensions.NaturalStringComparer> | Safe | — | Stateless; the static `Ordinal` / `OrdinalIgnoreCase` instances may be shared freely. [Guide](natural-string-comparer.md) |
| <xref:Bodu.Extensions.WordCasingOptions>, <xref:Bodu.Extensions.SlugOptions> | Immutable | — | `init`-only properties; build once, share. [Guide](string-extensions.md) |
| <xref:Bodu.Extensions.FiscalWeekQuarterProvider> | Immutable | — | All configuration is captured in `readonly` fields at construction. [Guide](calendar-shapes-and-providers.md) |
| <xref:Bodu.Extensions.IQuarterDefinitionProvider>, <xref:Bodu.Extensions.IWeekendDefinitionProvider> | Implementation-defined | — | The date extensions call them from whatever thread calls the extension; keep implementations stateless. |
| <xref:Bodu.IRandomGenerator> | Implementation-defined | — | Implementations are *not* required to be thread-safe; helpers that draw from one generator on several threads must pick a safe implementation or lock. |
| <xref:Bodu.XorShiftRandom> | Unsafe | — | Non-atomic state updates; concurrent draws corrupt the generator. Use one per thread. |
| <xref:Bodu.Collections.Generic.Extensions.SystemRandomAdapter> | As the wrapped `Random` | — | Safe over `Random.Shared`; unsafe over a `new Random()` shared across threads. |
| <xref:Bodu.Xml.Linq.XmlNamespaceResolver> | Immutable | — | Holds only the root's default namespace. |
| <xref:Bodu.Extensions.StringExtensions>, <xref:Bodu.Extensions.DateTimeExtensions>, <xref:Bodu.Extensions.DateOnlyExtensions>, <xref:Bodu.Globalization.Extensions.DateTimeFormatInfoExtensions>, <xref:Bodu.Extensions.NumericExtensions>, <xref:Bodu.Extensions.ComparableExtensions>, <xref:Bodu.Extensions.ComparableHelper>, <xref:Bodu.Extensions.EnumExtensions>, <xref:Bodu.Extensions.Enums>, <xref:Bodu.Extensions.ArrayExtensions>, <xref:Bodu.Extensions.SpanExtensions>, <xref:Bodu.Extensions.StreamExtensions>, <xref:Bodu.Extensions.BufferConverter>, <xref:Bodu.Extensions.WorkingDaysOfWeekExtensions>, <xref:Bodu.Extensions.IWeekendDefinitionProviderExtensions>, <xref:Bodu.Text.EncodingDetection>, <xref:Bodu.Text.EncodingExtensions>, <xref:Bodu.Text.StringEncodingExtensions>, <xref:Bodu.ThrowHelper>, <xref:Bodu.Functional.Option>, <xref:Bodu.Functional.OptionAsyncExtensions>, <xref:Bodu.Functional.ResultAsyncExtensions>, <xref:Bodu.Functional.EitherAsyncExtensions> | Safe (stateless) | — | Pure static helpers. `Enums` fills a per-`TEnum` static cache in a type initializer and hands out copies. They are only as safe as the *arguments* you pass — an array or stream shared between threads is your problem, not theirs. |
| <xref:Bodu.Collections.Generic.Extensions.IEnumerableExtensions> | Safe (stateless); results Unsafe | Per operator | The returned sequences are single-consumer iterators — except `Cache()`, whose buffer is explicitly safe for concurrent enumeration from multiple threads. `BatchPooled` windows alias one rented buffer and are valid only until the enumerator advances. [Guide](sequence-operators.md) |
| <xref:Bodu.Collections.Generic.Extensions.IListExtensions>, <xref:Bodu.Collections.Generic.Extensions.IDictionaryExtensions> | Safe (stateless); no synchronization of the target | — | `GetOrAdd` / `AddOrUpdate` are a lookup then a write — not atomic; use `ConcurrentDictionary<TKey,TValue>` for shared maps. |
| <xref:Bodu.Collections.Generic.ShuffleHelpers>, <xref:Bodu.Sequences.SequenceGenerator> | Safe (stateless); sequences Unsafe | — | Generated sequences carry per-enumerator state only; `ShuffleHelpers` is as safe as the `IRandomGenerator` it is given. |

## `Bodu.Collections`

Every mutable collection in this package is **single-threaded by design**: none takes locks, and the non-generic `ICollection.IsSynchronized` is always `false`. Their enumerators are **fail-fast** unless the table says otherwise.

| Type | Contract | Enumeration | Read-mutates caveat / notes |
|---|---|---|---|
| <xref:Bodu.Collections.Generic.RingBackedCollection`1> | Unsafe | Fail-fast (struct enumerator) | Abstract base; the version counter is bumped inside every protected mutator. [Guide](ring-backed-collections.md) |
| <xref:Bodu.Collections.Generic.CircularBuffer`1> | Unsafe | Fail-fast | Eviction events run inline; mutating from a handler throws. Use `ConcurrentCircularBuffer<T>` for MPMC. [Guide](circular-buffer.md) |
| <xref:Bodu.Collections.Generic.Deque`1> | Unsafe | Fail-fast | Same eviction-handler rule under `EvictOpposite`. [Guide](deque.md) |
| <xref:Bodu.Collections.Generic.EvictingDictionary`2> | **Unsafe — reads mutate** | Fail-fast; expired entries are filtered against a clock snapshot taken at enumerator creation | Every lookup (`this[key]`, `TryGetValue`, `ContainsKey`, …) counts as a *touch*: LRU / LFU-style policies reposition the entry, and with an expiration configured an access may purge expired keys. Even concurrent readers race. [Guide](evicting-dictionary.md) |
| <xref:Bodu.Collections.Generic.EvictingDictionaryExpiration> | Immutable | — | Expiration configuration value. |
| <xref:Bodu.Collections.Generic.SequencedDictionary`2> | Read-only safe in insertion order; **Unsafe in access order** | Fail-fast | In access-order mode a lookup moves the entry to the end. [Guide](sequenced-dictionary.md) |
| <xref:Bodu.Collections.Generic.BiDictionary`2> | Read-only safe | Fail-fast (BCL-dictionary rules, through either view) | The `Inverse` view shares storage — a write through one view is a write to both. [Guide](bi-dictionary.md) |
| <xref:Bodu.Collections.Generic.LayeredDictionary`2> | Read-only safe | `Keys` / `Values` are snapshots; enumeration follows each layer's own rules | Writes go to the first layer only; the layers themselves are ordinary dictionaries you also must not mutate concurrently. [Guide](layered-and-defaulting-dictionaries.md) |
| <xref:Bodu.Collections.Generic.DefaultingDictionary`2> | **Unsafe — indexer reads mutate** | Fail-fast (wrapped dictionary); enumeration never materializes defaults | `this[key]` on a missing key invokes the factory and *stores* the result; the check-invoke-store sequence is not atomic. [Guide](layered-and-defaulting-dictionaries.md) |
| <xref:Bodu.Collections.Generic.Table`3> | Read-only safe | Fail-fast (dictionary rules) via the live `Row` / `Column` views; `ColumnKeys` is a snapshot | [Guide](table.md) |
| <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> | Read-only safe | Fail-fast (also bumped by re-prioritization) | [Guide](indexed-priority-queue.md) |
| <xref:Bodu.Collections.Generic.IndexedSet`1>, <xref:Bodu.Collections.Generic.OrderedSet`1> | Read-only safe | Fail-fast | The set-algebra members (`UnionWith`, …) mutate in place. [Guide](ordered-sets.md) |
| <xref:Bodu.Collections.Generic.NavigableSet`1>, <xref:Bodu.Collections.Generic.NavigableDictionary`2> | Read-only safe | Fail-fast, including every ascending / descending / range view and the key and value collections | Views are live: they reflect later mutations on their *next* iteration. [Guides](navigable-set.md) · [dictionary](navigable-dictionary.md) |
| <xref:Bodu.Collections.Generic.Multiset`1> | Read-only safe | Fail-fast | [Guide](multiset.md) |
| <xref:Bodu.Collections.Generic.MultiValueDictionary`2> | Read-only safe | Fail-fast | [Guide](multi-value-dictionary.md) |
| <xref:Bodu.Collections.Generic.Range`1>, <xref:Bodu.Collections.Generic.ValueRange`2> | Immutable | — | `readonly struct` keys and entries. [Guide](range-dictionary.md) |
| <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1> | Read-only safe | Fail-fast | [Guide](range-dictionary.md) |
| <xref:Bodu.Collections.Generic.IntervalTree`1>, <xref:Bodu.Collections.Generic.IntervalTree`2> | Read-only safe | Fail-fast, including `QueryPoint` / `QueryOverlaps` results while they are being iterated | [Guide](interval-tree.md) |
| <xref:Bodu.Collections.Generic.SegmentedBuffer`1> | Read-only safe | Fail-fast | Append-only. [Guide](segmented-buffer.md) |
| <xref:Bodu.Collections.Generic.BitSet> | Read-only safe | Fail-fast | The in-place logical operators (`And`, `Or`, …) are writes. [Guide](bit-set.md) |
| <xref:Bodu.Collections.Probabilistic.BloomFilter`1>, <xref:Bodu.Collections.Probabilistic.CountMinSketch`1>, <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> | Read-only safe | — (no element enumeration) | `Add` / `Merge` / `Import` are writes; `Export` produces a versioned snapshot. [Guide](probabilistic-collections.md) |
| <xref:Bodu.Collections.Generic.Graphs.Graph`1> | Read-only safe | **Undefined** — no version counter; do not mutate while iterating `Vertices` or `Neighbors` | Algorithms in `GraphAlgorithms` only read. [Guide](graphs.md) |
| <xref:Bodu.Collections.Generic.Graphs.IReadOnlyGraph`1>, <xref:Bodu.Collections.Generic.Graphs.IReadOnlyWeightedGraph`1> | As the implementation | — | Read-only *interfaces*, not immutable views — the underlying graph may still be mutated by its owner. |
| <xref:Bodu.Collections.Generic.Graphs.GraphAlgorithms> | Safe (stateless) | — | Operates on the graph it is given; the graph must not be mutated during a run. |
| <xref:Bodu.Collections.Generic.Graphs.ShortestPathResult`1> | Immutable | — | `readonly record struct` result. |
| <xref:Bodu.Collections.Generic.Graphs.DisjointSet`1> | **Unsafe — `Find` mutates** | — (no public enumeration) | Path-halving compression rewrites parent links on every `Find` / `TryFind` / `AreConnected`, so even concurrent lookups race. [Guide](graphs.md) |
| <xref:Bodu.Collections.Generic.Trees.Trie>, <xref:Bodu.Collections.Generic.Trees.Trie`1>, <xref:Bodu.Collections.Generic.Trees.RadixTrie>, <xref:Bodu.Collections.Generic.Trees.RadixTrie`1> | Read-only safe | Fail-fast, including prefix-query sequences | [Guide](trie.md) |
| <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton>, <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton`1> | Immutable | — | Built once from the full pattern set (a new set means a new automaton); matching is a pure read and is safe to run concurrently. [Guide](trie.md) |
| <xref:Bodu.Collections.Generic.Trees.AhoCorasickMatch>, <xref:Bodu.Collections.Generic.Trees.AhoCorasickMatch`1> | Immutable | — | `readonly record struct` results. |
| <xref:Bodu.Collections.Generic.Trees.Tree`1> | Read-only safe | **Undefined** — traversals carry no version; snapshot with `ToList()` before mutating | `Depth` / `Height` are computed reads. [Guide](tree.md) |
| <xref:Bodu.Collections.Generic.DequeOverflowPolicy>, <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy>, <xref:Bodu.Collections.Generic.EvictingDictionaryExpirationKind>, <xref:Bodu.Collections.Generic.BiDictionaryDuplicateValuePolicy>, <xref:Bodu.Collections.Generic.MultiValueBacking>, <xref:Bodu.Collections.Generic.Graphs.GraphKind> | Immutable | — | Enums. |

## `Bodu.Collections.Concurrent`

| Type | Contract | Enumeration | Notes |
|---|---|---|---|
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> | **Safe** — lock-free MPMC (Vyukov ring) | Snapshot (atomic copy at `GetEnumerator`); never throws for concurrent modification | `Count` may be transiently stale under contention. Like its two siblings it reports `ICollection.IsSynchronized == false` — there is no lock to expose because the type manages its own synchronization, so do not try to coordinate access through `SyncRoot`. [Guide](concurrent-collections.md) |
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> | **Safe** — lock-free split-ordered list | Weakly consistent snapshot: elements added or removed after the snapshot completes are not seen; elements changing *during* capture may or may not appear | Resizes are lock-free and never invalidate readers. The `ISet<T>` bulk operations (`UnionWith`, `IntersectWith`, …) are sequences of individual atomic steps, not one atomic operation. [Guide](concurrent-collections.md) |
| <xref:Bodu.Collections.Generic.Concurrent.ConcurrentEvictingDictionary`2> | **Safe** — lock-striped segments | Point-in-time snapshot; `Keys` / `Values` are snapshots too (each read allocates) | `GetOrAdd` is single-flight per key (the factory runs under the owning segment's lock, at most once per key even under concurrent misses); `ItemEvicted` is raised *after* the lock is released. Reads still touch eviction state, but the striping makes that safe. [Guide](concurrent-collections.md) |

## Rules of thumb

1. **Confine or lock every `Bodu.Collections` type.** Wrap it in your own lock, or hand it to one thread. A `ReaderWriterLockSlim` is only valid for the rows marked *read-only safe* — never for `EvictingDictionary`, access-ordered `SequencedDictionary`, `DefaultingDictionary`, or `DisjointSet`, whose reads write.
2. **Do not rely on fail-fast to detect races.** The version check runs on the enumerating thread and can miss interleavings; it exists to turn a same-thread bug into an exception.
3. **Prefer the `Bodu.Collections.Concurrent` type when one exists** — it will beat a locked single-threaded collection under contention and its enumerators are snapshots rather than exceptions.
4. **Share immutable values, not builders.** `WeekPattern`, the option classes, the providers, and the `readonly struct` results are free to share; `PooledBufferBuilder<T>` and `XorShiftRandom` are not.
5. **Stateless helpers are safe; their arguments may not be.** Every extension class is pure, so thread safety collapses to the thread safety of the object you pass in.

## Where to go next

- [Concurrent collections](concurrent-collections.md) — the three safe collections in depth.
- [Async coordination primitives](async-primitives.md) — the locks and gates for coordinating the rest.
- [Choosing a collection](choosing-a-collection.md) — picking by requirement, including "shared between threads".
- [Extending `RingBackedCollection<T>`](ring-backed-collections.md) — the version counter and fail-fast contract from the implementer's side.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
