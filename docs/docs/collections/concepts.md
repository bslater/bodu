---
title: Bodu.Collections — Core concepts
---

# Bodu.Collections — Core concepts

This page is the vocabulary the rest of the collection documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/core/index.md), and refer back whenever a term feels imprecise.

Part of the **[Core Foundations](../topics/core-foundations.md)** topic.

For the high-level shape of the library and the namespace map, start with the [introduction](index.md). Concurrency-specific vocabulary — lock-free rings, split-ordered hashing, snapshot enumeration — lives on the [Bodu.Collections.Concurrent concepts page](../collections-concurrent/concepts.md).

## Fixed-capacity collection

A **fixed-capacity** collection sizes its backing storage once and never grows. Once `Count` reaches `Capacity`, the collection has to choose between two behaviours: reject the next add, or evict an existing element to make room. Bodu's ring-backed types expose that choice as a single toggle rather than two separate classes.

| Type | Toggle | Add when full |
|---|---|---|
| <xref:Bodu.Collections.Generic.CircularBuffer`1> | `AllowOverwrite` | `true` evicts the head; `false` throws <xref:System.InvalidOperationException>. |
| <xref:Bodu.Collections.Generic.Deque`1> | `AllowGrow` + `OverflowPolicy` | `AllowGrow = true` doubles the backing array; `false` consults <xref:Bodu.Collections.Generic.DequeOverflowPolicy> — `Reject` throws, `EvictOpposite` discards the element at the opposite end. |
| <xref:Bodu.Collections.Generic.EvictingDictionary`2> | (always evicting) | Removes the entry selected by the configured <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy>. |

The `Try…` variants (`TryEnqueue`, `TryAddFirst`, `TryAddLast`) substitute a `false` return for the throw, so callers can prefer non-throwing code paths without changing the toggle.

## Overflow policies

The `Deque<T>` / `CircularBuffer<T>` kinship runs deeper than the shared ring: both express "what happens on overflow" as data, not as a subclass. `CircularBuffer<T>` has one axis (`AllowOverwrite` — overwrite the oldest, or refuse) because a FIFO has only one natural victim. `Deque<T>` is double-ended, so its fixed-capacity mode needs a policy: <xref:Bodu.Collections.Generic.DequeOverflowPolicy.Reject> preserves the historical throw-on-full contract, while <xref:Bodu.Collections.Generic.DequeOverflowPolicy.EvictOpposite> discards the element at the *opposite* end of the add — the tail for `AddFirst`, the head for `AddLast` — mirroring Python's `collections.deque(maxlen: N)` bounded-deque semantics. The policy is consulted only when `AllowGrow` is `false`; a growable deque never overflows.

## Ring-backed collection

A **ring-backed** collection stores its elements in a single contiguous array with `head` and `tail` indices that wrap modulo `Capacity`. This gives O(1) add and remove at either end without shifting elements, at the cost of a fixed capacity (or, for growable variants, an occasional array-doubling pass).

<xref:Bodu.Collections.Generic.RingBackedCollection`1> is the shared abstract base. It owns the storage, the wrap arithmetic, the structural-version counter that powers fail-fast enumeration, and the protected primitives (`AddTail`, `AddHead`, `RemoveHead`, `RemoveTail`, `PeekHead`, `PeekTail`, `Resize`) that the concrete types build on. <xref:Bodu.Collections.Generic.CircularBuffer`1> layers a single-ended FIFO surface on top; <xref:Bodu.Collections.Generic.Deque`1> layers a double-ended surface. Both share enumeration, copy, indexer, and trim behaviour through the base.

Derived types skip capacity and emptiness checks on the protected mutators — the public surface enforces those contracts before calling — so the hot path stays branch-free. Because elements are never shifted, every add, remove, and peek is O(1), and the head-relative indexer `this[i]` is an O(1) random read (index `0` is the oldest element).

Enumeration over the ring-backed types is **fail-fast**: the base owns a structural-version counter that every mutator bumps, and the `struct` enumerator captures that token at creation. A structural change while an enumerator is live — including `Clear` and `TrimExcess` — makes the next `MoveNext` (or `Reset`) throw <xref:System.InvalidOperationException>. This is the BCL collection contract; the concurrent peer, <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> (in [Bodu.Collections.Concurrent](../collections-concurrent/index.md)), deliberately drops it in favour of snapshot enumeration that never throws.

## Bounded vs. growing

The `AllowGrow` flag on <xref:Bodu.Collections.Generic.Deque`1> picks between two modes at runtime:

- **Growing** (`AllowGrow = true`, the default) — the backing array doubles on overflow, capped at <xref:System.Array.MaxLength>. The deque behaves like a `List<T>` with O(1) ends.
- **Bounded** (`AllowGrow = false`) — the deque is fixed at its current capacity. Overflow behaviour follows the configured <xref:Bodu.Collections.Generic.DequeOverflowPolicy>: `Reject` throws <xref:System.InvalidOperationException> (the `Try…` variants return `false` instead); `EvictOpposite` silently discards from the opposite end.

The toggle can be flipped at runtime — useful when a deque starts in growable mode during warm-up and is then locked down for steady-state. Switching to `false` does not shrink the backing array; call `TrimExcess` afterwards if a smaller footprint is wanted. `EnsureCapacity(int)` can pre-grow even when `AllowGrow` is `false`.

## Eviction policy

An <xref:Bodu.Collections.Generic.EvictingDictionary`2> is bounded by a capacity and an <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> that decides which entry leaves when a new key triggers overflow:

| Policy | Evicts |
|---|---|
| `FirstInFirstOut` | The entry that was added earliest, regardless of access. |
| `LeastRecentlyUsed` | The entry with the oldest last-access timestamp. |
| `LeastFrequentlyUsed` | The entry with the lowest cumulative access count. |
| `MostRecentlyUsed` | The entry with the newest last-access timestamp. |
| `RandomReplacement` | A uniformly randomly chosen entry. |
| `SecondChance` | A FIFO scan that skips entries flagged as recently accessed (the flag clears on skip), evicting the first unflagged entry. |

All policies share the same `IDictionary<TKey, TValue>` surface and the same overflow trigger; the policy only changes the selection. `SecondChance` is the *clock* algorithm — a low-overhead LRU approximation that swaps the per-access list-splice for a single reference bit — and `MostRecentlyUsed` is the scan-resistant inverse of LRU, evicting the just-touched entry to preserve the older working set.

Two `Action<TKey, TValue>` events bracket each eviction — `ItemEvicting` (before) and `ItemEvicted` (after) — so a backing store can be flushed or a resource counter decremented as entries leave. Handlers must not mutate the dictionary; a re-entrant `Add`, `Remove`, `Clear`, or indexer set from inside a handler throws <xref:System.InvalidOperationException>. `PeekEvictionCandidate()` reports the next victim without disturbing the policy metadata, and the running `EvictionCount` / `TotalTouches` totals support cache-effectiveness telemetry.

> [!NOTE]
> A successful read through the `this[key]` getter or `TryGetValue` **counts as an access** and updates the recency / frequency metadata for LRU, MRU, LFU, and SecondChance — so even concurrent read-read is unsafe without external synchronisation. `ContainsKey` and `PeekEvictionCandidate` are pure reads. `Add(key, value)` is add-*or-replace* and does not throw on a duplicate key (there is no `GetOrAdd`).

## Time-based expiry

Capacity bounds *how many* entries a cache holds; **time-based expiry** bounds *how long* an entry stays useful. An <xref:Bodu.Collections.Generic.EvictingDictionary`2> constructed with an <xref:Bodu.Collections.Generic.EvictingDictionaryExpiration> configuration layers a time-to-live on top of its capacity policy:

- **Absolute vs. sliding.** Under <xref:Bodu.Collections.Generic.EvictingDictionaryExpirationKind.Absolute> the clock runs from the write; under <xref:Bodu.Collections.Generic.EvictingDictionaryExpirationKind.Sliding> every successful read access renews the deadline. A per-entry `Add(key, value, timeToLive)` / `TryAdd(key, value, timeToLive)` override trumps the dictionary-wide default for that write.
- **Testable time.** The configuration accepts a <xref:System.TimeProvider>, so tests drive expiry with a fake clock instead of `Thread.Sleep`. Without an expiration configuration the dictionary performs no clock reads at all and behaves as a capacity-only cache.
- **Lazy reclamation and the raw-`Count` contract.** There is no background timer. An expired entry is removed lazily when an access touches its key; until then it still counts toward `Count` and `Capacity`. `RemoveExpired()` purges every expired entry on demand — call it periodically for caches that can sit idle — and each expiry-driven removal raises `ItemEvicting` / `ItemEvicted` and increments `EvictionCount` exactly like a capacity-triggered eviction. When a new key overflows a full dictionary, expired entries are purged *first*; the policy evicts a live victim only when none had expired.

## Navigation and rank/select

A **navigable** collection answers *nearest-neighbour* questions about ordered data, not just exact lookups. <xref:Bodu.Collections.Generic.NavigableSet`1> and <xref:Bodu.Collections.Generic.NavigableDictionary`2> store their elements in an order-statistic red-black tree, which makes four query families O(log n):

- **Floor / ceiling / higher / lower** — the greatest element ≤ a probe, the least ≥ it, and the strict variants (`TryGetFloor`, `TryGetCeiling`, `TryGetHigher`, `TryGetLower`; entry-valued equivalents on the dictionary). This is the query `SortedSet<T>` makes awkward and a hash set cannot answer at all.
- **Rank** — `IndexOf` reports how many elements precede a value in sort order.
- **Select** — `GetAt(rank)` returns the element at a given rank, turning the sorted collection into a virtual sorted array without materializing one.
- **Range counting and views** — `CountInRange(low, high)` counts without enumerating, and the live `Ascending` / `Descending` / `Range` views enumerate a slice fail-fast without copying.

Rank and select are what the *order-statistic* augmentation buys: each tree node carries its subtree size, so position arithmetic rides along every rebalance at no extra asymptotic cost.

## Overlap-storing intervals vs. range maps

The range family splits on one question: **may stored ranges overlap?**

- <xref:Bodu.Collections.Generic.RangeDictionary`2> and <xref:Bodu.Collections.Generic.RangeSet`1> keep *disjoint* half-open `[start, end)` ranges over sorted parallel arrays — the dictionary rejects overlapping insertions with <xref:System.ArgumentException>, the set coalesces them. Lookup is a binary search: O(log n) to find *the one* range containing a point. Reach for these when ranges partition a domain (tax brackets, IP blocks, tiered pricing).
- <xref:Bodu.Collections.Generic.IntervalTree`1> / <xref:Bodu.Collections.Generic.IntervalTree`2> store *closed* `[low, high]` intervals that may freely overlap — duplicates included — over a max-endpoint augmented red-black tree. Queries return *every* match: `QueryPoint` (stabbing) and `QueryOverlaps` (window) run in O(log n + k) where `k` is the number of results, and `Intersects` / `IntersectsPoint` answer existence in O(log n). Reach for it when overlap is the data (bookings, effective-dated records, genomic ranges).

The two are complements, not competitors: a range map answers "which bucket?", an interval tree answers "which of these things cover this point?".

## Approximate sketches

The `Bodu.Collections.Probabilistic` types are **sketches**: fixed-size summaries that answer queries about unbounded streams by accepting a quantified, *one-sided* error. Each is sized once from its accuracy parameters and never grows, and each states which direction it can be wrong in:

| Sketch | Question | Error contract |
|---|---|---|
| <xref:Bodu.Collections.Probabilistic.BloomFilter`1> | "Have I seen this element?" | **No false negatives.** Added elements always report present; never-added elements misreport at roughly the design false-positive rate. |
| <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> | "How many times has this occurred?" | **Never underestimates.** With probability ≥ `1 − δ`, an estimate exceeds the truth by at most `ε · TotalCount`. |
| <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> | "How many distinct elements?" | Symmetric relative error ~`1.04/√m` for `m = 2^precision` registers — a few percent in a few kilobytes. |

The one-sided contracts are what make sketches composable into exact systems: a Bloom filter can safely *skip* work (a "no" is definitive), a count-min sketch can safely *throttle* (it never lets a heavy hitter hide). All three support parameter-compatible merging (`UnionWith` / `MergeWith`) and version-checked export/import for cross-process aggregation.

## Bidirectional one-to-one mapping

A <xref:Bodu.Collections.Generic.BiDictionary`2> maintains a strict **one-to-one** correspondence: keys are unique *and values are unique*, so lookup is O(1) in both directions. The `Inverse` property is a live view sharing the same storage — writes through either face are visible through the other, with no copy and no synchronisation step. Because a value insert can collide with an existing value, the construction-time <xref:Bodu.Collections.Generic.BiDictionaryDuplicateValuePolicy> decides the outcome: `Throw` treats a duplicate value as a bug; `Replace` evicts the pair that previously held the value. Use it wherever two identifier spaces alias the same entities (id ↔ code, name ↔ handle) and a second reverse dictionary would inevitably drift.

## Layered first-wins views

A <xref:Bodu.Collections.Generic.LayeredDictionary`2> composes an ordered list of dictionaries into a single **first-wins** read-through view (Python's `ChainMap`): a read walks the layers in order and returns the first hit, all writes go to the first layer only, and removing a first-layer entry *unshadows* the value beneath it. The layers are held by reference — mutate an underlying dictionary and the view reflects it immediately. This is the configuration-override shape (defaults ← environment ← per-request) without flattening or copying. Its sibling <xref:Bodu.Collections.Generic.DefaultingDictionary`2> solves the adjacent problem — a *missing* key on the indexer getter materializes, stores, and returns a factory default (Python's `defaultdict`), so grouping and counting loops lose their `TryGetValue`-else-add boilerplate.

## Path compression and multi-pattern matching

The trie family trades on the same idea at two scales:

- **Path compression.** A plain <xref:Bodu.Collections.Generic.Trees.Trie> spends one node per character, which is wasteful when keys are long and branching is sparse. <xref:Bodu.Collections.Generic.Trees.RadixTrie> (PATRICIA-style) fuses each single-child run into one string-labelled edge — edges split on insert and re-fuse on remove — so node count tracks *key* count rather than character count. The public surface is member-for-member identical to `Trie`; choose by key shape (URLs, file paths, and hierarchical identifiers favour the radix form).
- **Multi-pattern matching.** An <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton> extends the trie with failure links, turning a *set of patterns* into a single automaton that scans a text once and reports **every** occurrence of **every** pattern — overlapping and nested matches included — in O(text + matches), independent of pattern count. The automaton is immutable after construction: build once from the pattern set, reuse across scans and threads. The keyed variant carries a caller value per pattern onto each match, which is what content-filtering and token-routing pipelines need.

## Multi-value and multi-set semantics

A **multi-** prefix in this library means *duplicate-aware*:

- <xref:Bodu.Collections.Generic.MultiValueDictionary`2> — sometimes called a multimap. A single key maps to zero or more values; values for the same key are retained in insertion order. `Count` is the total number of key-value entries; `KeyCount` is the number of distinct keys. The indexer returns a live read-only view that reflects later mutations to the same key, and an empty list (not `null`) when the key is absent.
- <xref:Bodu.Collections.Generic.Multiset`1> — a set that tracks the *multiplicity* of each element. `Count` includes multiplicity (`{a, a, b}` has count 3); `DistinctCount` does not. Set-theoretic operations (`Union`, `Intersect`, `Except`, `Sum`) return new multisets and follow multiset algebra — `Union` is element-wise `max(a, b)`, `Intersect` is `min(a, b)`, `Except` is `max(0, a − b)`, and `Sum` is `a + b`.

Both types are not thread-safe and require external synchronisation under concurrent mutation.

## Range-keyed lookup

A <xref:Bodu.Collections.Generic.Range`1> is an immutable half-open interval `[StartInclusive, EndExclusive)` over any `IComparable<T>` endpoint. The half-open convention matches .NET span slicing and <xref:System.Range>: adjacent ranges (`[0, 5)` followed by `[5, 10)`) abut without overlapping, which is the property the collection types rely on for internal consistency.

Two range-keyed collections build on it:

| Type | Backing | Behaviour on overlap |
|---|---|---|
| <xref:Bodu.Collections.Generic.RangeDictionary`2> | Sorted parallel arrays of start, end, and value | Rejects overlapping insertions with <xref:System.ArgumentException>. |
| <xref:Bodu.Collections.Generic.RangeSet`1> | Sorted parallel arrays of start and end | Merges adjacent and overlapping ranges on insertion. |

Both use binary search across the start endpoints for O(log n) lookup. The constructor of `Range<T>` validates that `start < end` and rejects degenerate or inverted ranges.

## Index-aware collections

An **index-aware** collection exposes positional access alongside its primary semantic. Two types in `Bodu.Collections.Generic` carry the prefix:

- <xref:Bodu.Collections.Generic.IndexedSet`1> — an insertion-ordered set that implements the full `IList<T>` contract. Duplicates are rejected on add (`Add` returns `false`), positional mutation works through `Insert`, `RemoveAt`, `Move`, and the indexer setter. Backed by a contiguous element array plus an open-addressing hash table, giving O(1) `Contains`, `IndexOf`, and indexed read. <xref:Bodu.Collections.Generic.OrderedSet`1> is the conceptually-a-set sibling that shares the same engine but exposes indices only as a read-only view.
- <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> — a binary min-heap that maintains an element-to-slot map alongside the heap. The map turns `Contains`, `TryGetPriority`, `Update`, `Remove`, and `EnqueueOrUpdate` into O(1) lookup plus O(log n) heap repair — the operations Dijkstra's algorithm, Prim's algorithm, and A* require. Elements are unique; `Enqueue` of an existing element throws.

## Where to go next

- **[Introduction](index.md)** — the namespace map and headline types.
- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Choosing a collection](../../guides/core/choosing-a-collection.md)** — the decision guide across the catalogue.
- **[Collections guides](../../guides/core/index.md)** — recipe-style walk-throughs for every headline type.
- **[Bodu.Collections.Concurrent concepts](../collections-concurrent/concepts.md)** — the concurrency vocabulary for the thread-safe companion package.
- **[Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic)** — full type-by-type docs.
- **[Core Foundations topic](../topics/core-foundations.md)** — the package family; the [topic concepts](../topics/core-foundations-concepts.md) page collects the shared vocabulary.
