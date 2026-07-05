---
title: Choosing a collection
---

# Choosing a collection

Bodu.Core ships more than a dozen collection types. This page is the decision guide — it answers "which collection should I reach for?" without making the reader walk every namespace. For the full namespace map, start with the [Bodu.Core introduction](../../docs/core/index.md); for vocabulary, read [Core concepts](../../docs/core/concepts.md).

## Quick decision tree

1. **Do you need a key-value store?**
   - Bounded with eviction (cache) → <xref:Bodu.Collections.Generic.EvictingDictionary`2>.
   - Unbounded but iterate in insertion (or access) order, with O(1) first/last → <xref:Bodu.Collections.Generic.SequencedDictionary`2>.
   - Keys are ranges (`[start, end)`) → <xref:Bodu.Collections.Generic.RangeDictionary`2>.
   - One key maps to many values → <xref:Bodu.Collections.Generic.MultiValueDictionary`2>.
   - One-to-one in both directions, with O(1) value-to-key lookup → <xref:Bodu.Collections.Generic.BiDictionary`2>.
   - Overrides layered over defaults, first layer wins, writes to the first layer → <xref:Bodu.Collections.Generic.LayeredDictionary`2>.
   - Missing keys should materialize a stored default on indexer read → <xref:Bodu.Collections.Generic.DefaultingDictionary`2>.
2. **Do you need a sequence (FIFO / LIFO / two-ended)?**
   - Fixed capacity, single-threaded, overwrite-or-throw on full → <xref:Bodu.Collections.Generic.CircularBuffer`1>.
   - Fixed capacity, multi-threaded → <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1>.
   - Both ends, O(1) push / pop on either end → <xref:Bodu.Collections.Generic.Deque`1>.
   - Append-only, unknown final length → <xref:Bodu.Collections.Generic.SegmentedBuffer`1>.
3. **Do you need set semantics?**
   - Insertion-ordered, unique, indexable like a list → <xref:Bodu.Collections.Generic.IndexedSet`1>.
   - Insertion-ordered, unique, read-only index view → <xref:Bodu.Collections.Generic.OrderedSet`1>.
   - Unordered, unique, multi-threaded → <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1>.
   - Duplicates retained as multiplicity → <xref:Bodu.Collections.Generic.Multiset`1>.
   - Set of disjoint half-open intervals → <xref:Bodu.Collections.Generic.RangeSet`1>.
   - Dense set of non-negative integers as packed bits → <xref:Bodu.Collections.Generic.BitSet>.
4. **Do you need a priority queue with key-based updates?** → <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2>.
5. **Can the answer be approximate?** When the exact structure no longer fits in memory and a quantified error is acceptable → the `Bodu.Collections.Probabilistic` sketches; see [Approximate (probabilistic) collections](#approximate-probabilistic-collections) below.

If none of the above fit, the BCL types (`List<T>`, `Dictionary<TKey,TValue>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`) are the right choice. Bodu.Core does not duplicate BCL primitives — every type below adds a contract the BCL does not provide.

The remainder of this page deepens that tree into per-axis tables, real-world scenarios, and a list of anti-patterns that come up most often when picking between similar types.

## Decision tables

### By access pattern

| Requirement | Reach for | Notes |
|---|---|---|
| Single-ended FIFO ring | <xref:Bodu.Collections.Generic.CircularBuffer`1> | `AllowOverwrite` toggles between sliding-window and bounded-throw modes. |
| Double-ended ring | <xref:Bodu.Collections.Generic.Deque`1> | O(1) `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast`. |
| Append-only stream of unknown length | <xref:Bodu.Collections.Generic.SegmentedBuffer`1> | Grows by fixed-size chunks; avoids the array-doubling copy. |
| Min-heap priority queue with O(1) lookup-by-element | <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> | Required by Dijkstra, Prim, A* — the `Update` / `EnqueueOrUpdate` calls the BCL `PriorityQueue<TElement,TPriority>` cannot perform. |
| Range-keyed lookup (interval → value) | <xref:Bodu.Collections.Generic.RangeDictionary`2> | O(log n) lookup; rejects overlapping inserts. |
| Range membership (in any interval?) | <xref:Bodu.Collections.Generic.RangeSet`1> | Merges adjacent and overlapping intervals on insertion. |
| Cache with policy-driven eviction | <xref:Bodu.Collections.Generic.EvictingDictionary`2> | FIFO, LRU, LFU, MRU, Random, or Second-Chance. |
| Ordered key-value store with O(1) first/last access | <xref:Bodu.Collections.Generic.SequencedDictionary`2> | Insertion order by default; opt into access order for LRU-style reordering. O(1) `First` / `Last` / `TryRemoveFirst` / `TryRemoveLast`. |
| One key → many values | <xref:Bodu.Collections.Generic.MultiValueDictionary`2> | Indexer returns an empty live view, never `null`. |
| One-to-one map, O(1) lookup in both directions | <xref:Bodu.Collections.Generic.BiDictionary`2> | Live `Inverse` view shares storage; duplicate-value conflicts follow the `Throw` / `Replace` policy. |
| Layered lookup with first-wins precedence | <xref:Bodu.Collections.Generic.LayeredDictionary`2> | Python `ChainMap` semantics: a live view over ordered layers, writes to the first layer only; removing a shadowing entry unshadows the deeper value. `Count`/enumeration walk every layer. |
| Auto-materializing defaults on indexer read | <xref:Bodu.Collections.Generic.DefaultingDictionary`2> | Python `defaultdict` semantics: only the indexer getter invokes the value factory and stores the result — `TryGetValue`/`ContainsKey` never materialize. The `GetOrAdd` extension stays the per-call-site option. |
| Dense integer membership as packed bits | <xref:Bodu.Collections.Generic.BitSet> | Java `BitSet` semantics. Prefer over the BCL `BitArray`, which is fixed-size, has no set-bit query surface (`NextSetBit` / `NextClearBit` / `Cardinality`), and enumerates boxed `bool` values instead of set-bit indices. |

### By capacity and lifecycle

| Requirement | Reach for | Notes |
|---|---|---|
| Fixed capacity, never grows, reject on full | <xref:Bodu.Collections.Generic.CircularBuffer`1> with `AllowOverwrite = false` | Or <xref:Bodu.Collections.Generic.Deque`1> with `AllowGrow = false` for two-ended access. |
| Fixed capacity, overwrite on full | <xref:Bodu.Collections.Generic.CircularBuffer`1> with `AllowOverwrite = true` | Sliding-window semantics — the default. |
| Fixed capacity, evict by policy on full | <xref:Bodu.Collections.Generic.EvictingDictionary`2> | The only collection in the namespace that evicts a non-end element. |
| Growable with O(1) ends | <xref:Bodu.Collections.Generic.Deque`1> with `AllowGrow = true` | Backing array doubles on overflow; capped at <xref:System.Array.MaxLength>. |
| Growable append-only without per-doubling copy | <xref:Bodu.Collections.Generic.SegmentedBuffer`1> | New segments allocate without rehoming existing elements. |
| Runtime toggle between growable and fixed | <xref:Bodu.Collections.Generic.Deque`1> | `AllowGrow` is a settable property; switching to `false` does not shrink the array. |
| Pre-grow before a known burst | <xref:Bodu.Collections.Generic.Deque`1>.EnsureCapacity | Honoured even when `AllowGrow = false`. |

### By concurrency

| Requirement | Reach for | Notes |
|---|---|---|
| Multi-threaded FIFO ring | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> | Implements <xref:System.Collections.Concurrent.IProducerConsumerCollection`1> over a Vyukov MPMC algorithm. |
| Multi-threaded unique set | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> | Lock-striped hash table; disjoint writers proceed in parallel. |
| Single-threaded, every other scenario | All non-concurrent types in <xref:Bodu.Collections.Generic> | Wrap with external synchronisation if shared across threads. |

The non-concurrent types are **not** thread-safe even for concurrent reads — <xref:Bodu.Collections.Generic.EvictingDictionary`2> mutates LRU and LFU metadata on read, and <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> mutates the element-to-slot map on every heap operation. Wrap with a lock or `ReaderWriterLockSlim` when sharing a single instance.

### By ordering and uniqueness

| Requirement | Reach for | Notes |
|---|---|---|
| Unique elements, no order | <xref:System.Collections.Generic.HashSet`1> (BCL) | Bodu does not duplicate this. |
| Unique elements, insertion-ordered, indexable | <xref:Bodu.Collections.Generic.IndexedSet`1> | Implements `IList<T>` over an open-addressing hash table; O(1) `Contains`, `IndexOf`, indexed read. |
| Unique elements, insertion-ordered, set surface | <xref:Bodu.Collections.Generic.OrderedSet`1> | Same engine as `IndexedSet<T>`; exposes indices only as a read-only view. |
| Duplicates retained with count | <xref:Bodu.Collections.Generic.Multiset`1> | `Count` includes multiplicity; `DistinctCount` does not. |
| Sorted by priority, unique elements, mutable priorities | <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> | `Enqueue` of an existing element throws — use `EnqueueOrUpdate`. |
| Sorted by interval | <xref:Bodu.Collections.Generic.RangeSet`1> | Half-open intervals over any `IComparable<T>`. |
| Key-value pairs, insertion- or access-ordered | <xref:Bodu.Collections.Generic.SequencedDictionary`2> | Preserves a stable encounter order; access-order mode moves an entry to the tail on read. Unbounded — does not evict. |

### By failure mode on overflow

| Add when full does… | Reach for |
|---|---|
| Throws <xref:System.InvalidOperationException>. | <xref:Bodu.Collections.Generic.CircularBuffer`1> with `AllowOverwrite = false`; <xref:Bodu.Collections.Generic.Deque`1> with `AllowGrow = false`. |
| Overwrites the oldest element. | <xref:Bodu.Collections.Generic.CircularBuffer`1> with `AllowOverwrite = true`. |
| Doubles the backing array. | <xref:Bodu.Collections.Generic.Deque`1> with `AllowGrow = true`. |
| Evicts a policy-selected entry. | <xref:Bodu.Collections.Generic.EvictingDictionary`2>. |
| Cannot happen (collection always grows). | <xref:Bodu.Collections.Generic.LayeredDictionary`2>, <xref:Bodu.Collections.Generic.DefaultingDictionary`2>, <xref:Bodu.Collections.Generic.SegmentedBuffer`1>, <xref:Bodu.Collections.Generic.IndexedSet`1>, <xref:Bodu.Collections.Generic.OrderedSet`1>, <xref:Bodu.Collections.Generic.SequencedDictionary`2>, <xref:Bodu.Collections.Generic.MultiValueDictionary`2>, <xref:Bodu.Collections.Generic.Multiset`1>, <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1>, <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2>, <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1>. |

The `Try…` overloads on the bounded ring-backed types substitute a `false` return for the throw, so callers can stay non-throwing without changing the toggle.

## Approximate (probabilistic) collections

The `Bodu.Collections.Probabilistic` namespace trades exactness for a fixed memory footprint: each sketch is sized once at construction and answers queries over arbitrarily long streams in O(1) space, with an error bound you choose up front.

> [!WARNING]
> These types are **approximate — do not use them for exact membership or exact counting.** A Bloom filter can report a never-added element as present, a count-min estimate can exceed the true count, and a HyperLogLog cardinality is a statistical estimate. When the answer must be exact, stay with the exact types above.

| Reach for | When… | Error contract |
|---|---|---|
| <xref:Bodu.Collections.Probabilistic.BloomFilter`1> | You need "have I seen this?" over a stream too large for a `HashSet<T>`, and a definitive *no* plus a probabilistic *yes* is enough. | No false negatives; false positives at the design rate `p` when filled to `ExpectedItems` (`EstimatedFalsePositiveRate` tracks the current fill). |
| <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> | You need per-element frequencies (heavy hitters, rate estimates) over high-cardinality streams where a counting dictionary would grow without bound. | Never underestimates; overestimates by at most `ε · TotalCount` with probability at least `1 − δ`. |
| <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> | You need a distinct-element count (unique visitors, distinct keys) in kilobytes rather than one entry per element. | Relative standard error ≈ `1.04/√m` for `m = 2^precision` one-byte registers (~0.81% at precision 14). |

All three hash through the element's <xref:System.Collections.Generic.IEqualityComparer`1>, merge with parameter-compatible instances (`UnionWith` / `MergeWith`), and round-trip state through an opaque, version-checked export/import. None is thread-safe. See the [Probabilistic collections guide](probabilistic-collections.md) for the full contracts, including the comparer-entropy and randomized-string-hash caveats.

## Common scenarios

| I want to… | Reach for |
|---|---|
| Track the last *N* sensor readings, dropping the oldest. | <xref:Bodu.Collections.Generic.CircularBuffer`1> with `AllowOverwrite = true`. |
| Implement a rate limiter that rejects bursts. | <xref:Bodu.Collections.Generic.CircularBuffer`1> with `AllowOverwrite = false`. |
| Build a producer-consumer queue between threads. | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1>. |
| Build an undo / redo history with capped size. | <xref:Bodu.Collections.Generic.Deque`1> with `AllowGrow = false`. |
| Cache compiled artifacts by name with LRU eviction. | <xref:Bodu.Collections.Generic.EvictingDictionary`2> + `EvictingDictionaryPolicy.LeastRecentlyUsed`. |
| Track session liveness without reading the value. | <xref:Bodu.Collections.Generic.EvictingDictionary`2>.Touch. |
| Build a lookup from IP ranges to country codes. | <xref:Bodu.Collections.Generic.RangeDictionary`2>. |
| Maintain a set of free disk extents that merges on insert. | <xref:Bodu.Collections.Generic.RangeSet`1>. |
| Run Dijkstra's algorithm on a weighted graph. | <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2>. |
| Group log entries by correlation id. | <xref:Bodu.Collections.Generic.MultiValueDictionary`2>. |
| Keep a dictionary you can iterate in insertion order. | <xref:Bodu.Collections.Generic.SequencedDictionary`2>. |
| Layer request-scoped overrides over shared defaults. | <xref:Bodu.Collections.Generic.LayeredDictionary`2> — overrides first, defaults behind. |
| Group items into lists without seeding empty lists. | <xref:Bodu.Collections.Generic.DefaultingDictionary`2> with `_ => new List<T>()`, or <xref:Bodu.Collections.Generic.MultiValueDictionary`2> for a dedicated multi-map surface. |
| Build an unbounded LRU and evict the oldest yourself. | <xref:Bodu.Collections.Generic.SequencedDictionary`2> with `accessOrder: true` + `TryRemoveFirst`. |
| Count occurrences of tokens in a corpus. | <xref:Bodu.Collections.Generic.Multiset`1>. |
| Maintain a list of items in entry order while ensuring uniqueness. | <xref:Bodu.Collections.Generic.IndexedSet`1>. |
| Track a thread-safe set of active correlation ids. | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1>. |
| Stream-build a payload whose total length is unknown. | <xref:Bodu.Collections.Generic.SegmentedBuffer`1>, or <xref:Bodu.Buffers.PooledBufferBuilder`1> for an `ArrayPool<T>`-backed builder. |
| Skip re-crawling URLs already visited, tolerating rare false skips. | <xref:Bodu.Collections.Probabilistic.BloomFilter`1> — approximate; never misses a visited URL. |
| Find the most frequent requests in a high-cardinality stream. | <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> — approximate; never undercounts. |
| Count unique visitors without storing every id. | <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> — approximate; ~1.04/√m standard error. |

## Anti-patterns

- **Do not use <xref:Bodu.Collections.Generic.Deque`1> when you only need single-ended FIFO.** A `CircularBuffer<T>` with `AllowOverwrite = false` expresses the constraint more clearly and is the same shape under the hood — both inherit from <xref:Bodu.Collections.Generic.RingBackedCollection`1>.
- **Do not use <xref:Bodu.Collections.Generic.EvictingDictionary`2> as a general dictionary.** It evicts on overflow even when you would prefer growth — choose the BCL `Dictionary<TKey,TValue>` when the working set is unbounded, or <xref:Bodu.Collections.Generic.SequencedDictionary`2> when you also need a stable iteration order.
- **Do not confuse <xref:Bodu.Collections.Generic.SequencedDictionary`2> with the BCL `OrderedDictionary<TKey,TValue>` (.NET 9+).** The BCL type is *positional* — index-addressable with `Insert`/`RemoveAt`. `SequencedDictionary<TKey,TValue>` has no positional surface; it gives O(1) ends and O(1) keyed removal instead, and adds an optional access-order (LRU) mode. (On `net8.0` the BCL type is unavailable regardless.)
- **Do not assume the non-concurrent types are safe under concurrent reads.** Reads on <xref:Bodu.Collections.Generic.EvictingDictionary`2> mutate eviction metadata; reads on every collection rely on a structural-version counter that is not interlocked. Wrap with external synchronisation or pick the explicit concurrent variant.
- **Do not pair <xref:Bodu.Collections.Generic.IndexedSet`1> with <xref:System.Collections.Generic.List`1> "to also enforce uniqueness".** `IndexedSet<T>` already implements `IList<T>` with O(1) `Contains` and `IndexOf` — keeping two structures in sync introduces drift bugs.
- **Do not implement an LRU cache by hand around <xref:System.Collections.Generic.Dictionary`2> + <xref:Bodu.Collections.Generic.Deque`1>.** <xref:Bodu.Collections.Generic.EvictingDictionary`2> already provides LRU, LFU, FIFO, MRU, Random, and Second-Chance through a single `EvictingDictionaryPolicy` selector.
- **Do not allocate a fresh <xref:Bodu.Buffers.PooledBufferBuilder`1> per call to "reuse the pool".** The pool is global; the builder is the rental handle. For repeated rebuilds, call `Reset` to keep the current rented buffer.

## See also

- [Bodu.Core introduction](../../docs/core/index.md) — namespace map and headline types.
- [Bodu.Core concepts](../../docs/core/concepts.md) — vocabulary: fixed-capacity, ring-backed, eviction policy, range-keyed.
- [Circular buffer](circular-buffer.md), [Deque](deque.md), [Evicting dictionary](evicting-dictionary.md), [Range dictionary](range-dictionary.md), [Indexed priority queue](indexed-priority-queue.md) — per-type walk-throughs.
- [Concurrent collections](concurrent-collections.md) — the thread-safe variants in detail.
- [Probabilistic collections (sketches)](probabilistic-collections.md) — the approximate `BloomFilter<T>` / `CountMinSketch<T>` / `HyperLogLog<T>` trio.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
