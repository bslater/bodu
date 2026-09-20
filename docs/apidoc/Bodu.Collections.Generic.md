---
uid: Bodu.Collections.Generic
---

![Bodu.Collections.Generic](~/images/hero-collections.svg)

## Purpose

**Bodu.Collections.Generic** is the headline namespace of the **`Bodu.Collections`** package (which depends on `Bodu.Core`): bounded, ordered, navigable, and range-keyed collections that behave predictably under memory pressure, with companions for pooled buffers, day-of-week patterns, encoding helpers, and argument validation in the adjacent `Bodu.Core` namespaces.

Reach for this library when you need a fixed-capacity FIFO queue, a deque with O(1) ends, a size-limited key/value cache with a real eviction policy (not just an ad-hoc `Dictionary` plus a bolted-on timer), a range-keyed lookup, or helpers that keep ceremony out of hot paths.

## Static documentation

- **[Bodu.Collections introduction](~/docs/collections/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Collections getting started](~/docs/collections/getting-started.md)** — install and minimal samples for the headline types.
- **[Core Foundations guides](~/guides/core/index.md)** — recipe-style walk-throughs: [choosing a collection](~/guides/core/choosing-a-collection.md), [circular buffer](~/guides/core/circular-buffer.md), [deque](~/guides/core/deque.md), [evicting dictionary](~/guides/core/evicting-dictionary.md), [sequenced dictionary](~/guides/core/sequenced-dictionary.md), [indexed priority queue](~/guides/core/indexed-priority-queue.md), [indexed and ordered sets](~/guides/core/ordered-sets.md), [multiset](~/guides/core/multiset.md), [multi-value dictionary](~/guides/core/multi-value-dictionary.md), [bidirectional dictionary](~/guides/core/bi-dictionary.md), [layered and defaulting dictionaries](~/guides/core/layered-and-defaulting-dictionaries.md), [table](~/guides/core/table.md), [navigable set](~/guides/core/navigable-set.md), [navigable dictionary](~/guides/core/navigable-dictionary.md), [bit set](~/guides/core/bit-set.md), [range-keyed lookups](~/guides/core/range-dictionary.md), [interval tree](~/guides/core/interval-tree.md), [segmented buffer](~/guides/core/segmented-buffer.md), [concurrent collections](~/guides/core/concurrent-collections.md), [RFC 6962 Merkle trees and proofs](~/guides/core/rfc6962-merkle-trees.md), [`WeekPattern`](~/guides/core/week-pattern.md).

## Key types

**Ring-backed collections**

- <xref:Bodu.Collections.Generic.CircularBuffer`1> — a fixed-capacity FIFO collection. With `allowOverwrite: true` it silently drops the oldest element when full; with `allowOverwrite: false` it throws on overflow.
- <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> — a lock-free multi-producer / multi-consumer circular buffer using the Vyukov MPMC algorithm, with the same overwrite semantics (ships in the companion `Bodu.Collections.Concurrent` package).
- <xref:Bodu.Collections.Generic.Deque`1> — double-ended queue with O(1) `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast`; growable or fixed-capacity, with <xref:Bodu.Collections.Generic.DequeOverflowPolicy> (`Reject` / `EvictOpposite`) selecting what a full fixed-capacity deque does on overflow.
- <xref:Bodu.Collections.Generic.RingBackedCollection`1> — abstract base shared by `CircularBuffer<T>` and `Deque<T>` (extension point for new ring-backed collections).
- <xref:Bodu.Collections.Generic.SegmentedBuffer`1> — segmented backing buffer for streaming scenarios where the total length is not known up front.

**Capacity-bounded dictionaries**

- <xref:Bodu.Collections.Generic.EvictingDictionary`2> — a fixed-capacity dictionary that evicts entries automatically when it fills up, under a policy of your choice.
- <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> — the policy enum: `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, `SecondChance`.
- <xref:Bodu.Collections.Generic.EvictingDictionaryExpiration>, <xref:Bodu.Collections.Generic.EvictingDictionaryExpirationKind> — optional time-to-live expiry layered on the capacity bound: `Absolute` (from insertion) or `Sliding` (reset on access).

**Ordered dictionaries**

- <xref:Bodu.Collections.Generic.SequencedDictionary`2> — an unbounded dictionary that preserves a stable encounter order (Java `LinkedHashMap` shape), with O(1) access to and removal of the first and last entries (`First` / `Last` / `TryRemoveFirst` / `TryRemoveLast`). Defaults to insertion order; an opt-in access-order mode moves an entry to the tail on read, the building block for a hand-rolled LRU.
- <xref:Bodu.Collections.Generic.NavigableDictionary`2> — key-sorted dictionary over an order-statistic red-black tree: O(log n) floor / ceiling / higher / lower entry queries, rank / select, `CountInRange`, and live `Ascending` / `Descending` / `Range` views. See the [navigable dictionary guide](~/guides/core/navigable-dictionary.md).

**Shaped dictionaries**

- <xref:Bodu.Collections.Generic.BiDictionary`2> — bidirectional one-to-one map with O(1) lookup in both directions and a live `Inverse` view; <xref:Bodu.Collections.Generic.BiDictionaryDuplicateValuePolicy> (`Throw` / `Replace`) decides what a duplicate value does. See the [bidirectional dictionary guide](~/guides/core/bi-dictionary.md).
- <xref:Bodu.Collections.Generic.LayeredDictionary`2>, <xref:Bodu.Collections.Generic.DefaultingDictionary`2> — a read-through view over an ordered list of dictionaries (Python `ChainMap` shape) and a dictionary whose indexer materializes a factory-supplied default for a missing key (Python `defaultdict` shape). See the [layered and defaulting dictionaries guide](~/guides/core/layered-and-defaulting-dictionaries.md).
- <xref:Bodu.Collections.Generic.Table`3> — two-key row / column map (Guava `Table` shape) with live `Row` / `Column` projections over a row-major store. See the [table guide](~/guides/core/table.md).

**Sets, multisets, range-keyed collections**

- <xref:Bodu.Collections.Generic.IndexedSet`1>, <xref:Bodu.Collections.Generic.OrderedSet`1>, <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> — index-aware set and priority-queue variants for lookup-by-position and key-based priority updates.
- <xref:Bodu.Collections.Generic.NavigableSet`1> — comparer-ordered set over the same order-statistic red-black tree: O(log n) `TryGetFloor` / `TryGetCeiling` / `TryGetHigher` / `TryGetLower`, rank / select, `CountInRange`, and live `Ascending` / `Descending` / `Range` views. See the [navigable set guide](~/guides/core/navigable-set.md).
- <xref:Bodu.Collections.Generic.MultiValueDictionary`2>, <xref:Bodu.Collections.Generic.Multiset`1> — multi-map and multi-set semantics over `IEqualityComparer<TKey>`; <xref:Bodu.Collections.Generic.MultiValueBacking> (`List` / `Set`) chooses whether each key's values are an ordered list or a de-duplicated set.
- <xref:Bodu.Collections.Generic.BitSet> — growable packed bit set with Java `BitSet` semantics (`NextSetBit` / `NextClearBit` / `Cardinality`, in-place `And` / `Or` / `Xor` / `AndNot`). See the [bit set guide](~/guides/core/bit-set.md).
- <xref:Bodu.Collections.Generic.Range`1>, <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1>, <xref:Bodu.Collections.Generic.ValueRange`2> — range-keyed lookups for ordered or interval-valued keys (non-overlapping ranges).
- <xref:Bodu.Collections.Generic.IntervalTree`1>, <xref:Bodu.Collections.Generic.IntervalTree`2> — overlap-storing interval trees over a max-endpoint augmented red-black tree: O(log n + k) stabbing (`QueryPoint`) and window (`QueryOverlaps`) queries over closed intervals that may freely overlap. See the [interval tree guide](~/guides/core/interval-tree.md).

**Merkle trees and proofs**

- <xref:Bodu.Collections.Generic.Rfc6962MerkleTree> — the [RFC 6962](https://www.rfc-editor.org/rfc/rfc6962#section-2.1) Merkle Tree Hash over any <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> the caller supplies, as one immutable, stateless facade. Root computation comes in three shapes — `ComputeRoot` / `ComputeRootOfLeafHashes` over a list of entries, `ComputeBlocked` / `ComputeRootOfBlocks` over a stream or span cut into fixed-size blocks, and `ComputeBlockedParallel` / `ComputeRootParallel`, which spread leaf hashing across threads without changing the tree. Proofs are `AuthenticationPath` and `ConsistencyProof` on the prover side and `VerifyInclusion`, `VerifyInclusionOfLeafHash`, `VerifyInclusionBound`, `VerifyBlockInclusion`, and `VerifyConsistency` on the verifier side; every verifier returns `false` for malformed input rather than throwing. `HashLeaf`, `HashNode`, and `BindRoot` expose the three domain-separated primitives directly.
- <xref:Bodu.Collections.Generic.MerkleComputation> — the result of a block-mode pass: the `Root`, the `InputLength` and `BlockSize` it was computed over, and the ordered `LeafHashes`. Returning the leaf hashes from the same pass is the point — an authentication path needs them, and without them a large object would have to be streamed twice.
- <xref:Bodu.Collections.Generic.MerkleBlocks> — the block arithmetic as standalone 64-bit helpers: `BlockCount`, `BlockOffset`, and `BlockLength`. A zero-length input has **no** blocks rather than one empty block, and a final short block is hashed at its actual length rather than padded.

**Related namespaces** (these ship in the `Bodu.Core` package, which `Bodu.Collections` depends on)

- <xref:Bodu> — `WeekPattern` (day-of-week bitmask), `IRandomGenerator` / `XorShiftRandom`, and `ThrowHelper` centralized argument validation.
- <xref:Bodu.Buffers> — `PooledBufferBuilder<T>` for `ArrayPool<T>`-backed zero-allocation building.
- <xref:Bodu.Extensions> — date / numeric / span / array extensions and the calendar-shape enums.
- <xref:Bodu.Collections.Extensions>, <xref:Bodu.Collections.Generic.Extensions> — sequence-shaping helpers (recursive selection, sliding windows, batched enumeration, pluggable random shuffles).
- <xref:Bodu.Sequences> — `SequenceGenerator` lazy sequence factories (`Range`, `NextWhile`, `Factory`) and named mathematical series (Fibonacci, Farey, Leibniz, look-and-say, Thue–Morse).
- <xref:Bodu.Text> — `EncodingDetection`, `EncodingExtensions`, and `StringEncodingExtensions`: BOM detection and span / UTF-8 / pooled-buffer helpers over `System.Text.Encoding`.

## Example

```csharp
using Bodu.Collections.Generic;

// Bounded FIFO with overwrite: the four most recent samples win.
var recent = new CircularBuffer<double>(capacity: 4, allowOverwrite: true);
foreach (double sample in stream) recent.Enqueue(sample);

// LRU cache for expensive lookups.
var cache = new EvictingDictionary<string, User>(
    capacity: 1024,
    policy: EvictingDictionaryPolicy.LeastRecentlyUsed);

if (!cache.TryGetValue(id, out User user))
{
    user = Load(id);
    cache[id] = user; // oldest unused entry is evicted automatically when full.
}
```

## Notes

- **Thread safety.** `CircularBuffer<T>`, `Deque<T>`, and `EvictingDictionary<TKey, TValue>` are **not** thread-safe; external synchronization is required if accessed concurrently. For a concurrent FIFO, use <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> from the companion `Bodu.Collections.Concurrent` package, which is designed for multi-producer / multi-consumer scenarios under the Vyukov algorithm.
- **Capacity is fixed.** Both `CircularBuffer<T>` and `EvictingDictionary<TKey, TValue>` reject a non-positive capacity at construction time. Allocation happens once, up front, not incrementally — this is a deliberate choice for predictable memory behavior in long-running services.
- **Eviction policies differ in cost.** `FirstInFirstOut` and `RandomReplacement` are O(1); `LeastRecentlyUsed` and `MostRecentlyUsed` maintain a linked recency list and are O(1) per access; `LeastFrequentlyUsed` and `SecondChance` carry a small bookkeeping overhead on access. Pick the policy that matches your workload rather than defaulting to LRU.
- **Enumeration is snapshot-stable** for non-concurrent types — iterating while mutating throws, per the usual .NET contract.
- **The Merkle tree is RFC 6962's, exactly.** A tree of *n* leaves splits at `k`, the largest power of two **strictly below** *n*, and a lone subtree root is promoted **unchanged** rather than re-hashed. Leaves are `H(0x00 ‖ entry)` and internal nodes `H(0x01 ‖ left ‖ right)`, so a leaf hash can never be mistaken for a node hash — the defence against the second-preimage attack. The empty tree's root is `H()`, not an exception. `Bodu.Security.Cryptography` also ships `MerkleTreeHash` and `ParallelMerkleTreeHash`, which borrow that domain separation but **not** the tree shape: they reduce level by level with a configurable fan-out, so their roots agree with this one's only when the leaf count is a power of two, and they produce no proofs.
- **`VerifyInclusion`'s `treeSize` is trusted input.** This is RFC 6962 as specified, not a defect: a four-entry tree's path for entry 0 walks to the same head a three-entry tree's first path does, so both verify. When the size comes from the party being examined, they can understate it and exempt their last entries from challenge. `BindRoot` closes the gap by folding the length into the published root as `H(0x02 ‖ uint64_be(length) ‖ root)`, and `VerifyInclusionBound` / `VerifyBlockInclusion` derive the size from it instead of accepting one.
- **`Rfc6962MerkleTree` is not `IDisposable`, deliberately.** It owns no digest state — it creates, uses, and disposes a `HashAlgorithm` within each call — so one instance is safe to share across threads, provided the supplied factory returns a fresh instance each time (as `SHA256.Create` does).
- **See also:** the [circular buffer guide](~/guides/core/circular-buffer.md), the [evicting dictionary guide](~/guides/core/evicting-dictionary.md), the [RFC 6962 Merkle trees and proofs guide](~/guides/core/rfc6962-merkle-trees.md), and the [Bodu.Collections introduction](~/docs/collections/index.md) for the full scenario table.
