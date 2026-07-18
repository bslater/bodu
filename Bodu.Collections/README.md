# Bodu.Collections

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

The specialized generic-collection catalogue for the Bodu solution and for general .NET 8 use: bounded and ordered collections, bidirectional and layered dictionaries, navigable (order-statistic) sets and maps, interval and range-keyed lookups, graph and tree structures, and probabilistic sketches. The catalogue was split out of `Bodu.Core` with namespaces unchanged — code written against `Bodu.Collections.Generic` and its siblings keeps compiling; only the package reference changes. Every collection ships a struct enumerator for allocation-free iteration and implements the standard BCL interfaces (`IEnumerable<T>`, `ICollection<T>`, `IReadOnlyCollection<T>`, `ISet<T>`, `IList<T>`) so the types drop into existing code. The package references `Bodu.Core` for its shared primitives (`ThrowHelper` argument validation, `IRandomGenerator`, pooled buffers).

## Installation

```shell
dotnet add package Bodu.Collections
```

Targets `net8.0`. Depends on `Bodu.Core`.

## Collections

| Type | Namespace | Summary |
|---|---|---|
| `CircularBuffer<T>` | `Bodu.Collections.Generic` | Fixed-capacity FIFO ring buffer with optional overwrite-on-full |
| `Deque<T>` | `Bodu.Collections.Generic` | Double-ended queue over a circular array; O(1) at either end |
| `EvictingDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Fixed-capacity dictionary with FIFO / LRU / LFU eviction |
| `SequencedDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Insertion- or access-ordered dictionary with O(1) first/last access and removal |
| `BiDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Bidirectional one-to-one dictionary with O(1) lookup in both directions |
| `DefaultingDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Dictionary whose indexer materializes missing entries via a value factory |
| `LayeredDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Live read-through view over ordered dictionary layers; writes go to the first layer |
| `NavigableDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Key-sorted dictionary with floor/ceiling/higher/lower, rank/select, and range queries in O(log n) |
| `NavigableSet<T>` | `Bodu.Collections.Generic` | Sorted set with the same nearest-neighbour, rank/select, and range-counting surface |
| `IndexedSet<T>` | `Bodu.Collections.Generic` | Insertion-ordered unique set with index-addressable `IList<T>` access |
| `OrderedSet<T>` | `Bodu.Collections.Generic` | Insertion-ordered unique set implementing `ISet<T>` |
| `IndexedPriorityQueue<TElement,TPriority>` | `Bodu.Collections.Generic` | Binary-heap priority queue with O(log n) re-prioritization and removal |
| `MultiValueDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Multiple values per key, exposed as `IReadOnlyList<TValue>` |
| `Multiset<T>` | `Bodu.Collections.Generic` | Unordered collection tracking element multiplicities |
| `RangeSet<T>` / `RangeDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Non-overlapping range containment / range-to-value mapping |
| `IntervalTree<T>` / `IntervalTree<TKey,TValue>` | `Bodu.Collections.Generic` | Freely overlapping closed intervals with stabbing and overlap-window queries in O(log n + k) |
| `Table<TRow,TColumn,TValue>` | `Bodu.Collections.Generic` | Two-dimensional map keyed by a row/column pair with live row and column projections |
| `BitSet` | `Bodu.Collections.Generic` | Growable packed bit array with Java `BitSet` semantics and bulk logical operations |
| `SegmentedBuffer<T>` | `Bodu.Collections.Generic` | Append-only chunked buffer that grows without copying existing elements |

## Graphs

The `Bodu.Collections.Generic.Graphs` namespace provides an adjacency-list `Graph<T>` (directed or undirected, optionally weighted), the static `GraphAlgorithms` catalogue (breadth-/depth-first traversal, Dijkstra shortest path, Kahn topological sort, connected components) over the read-only `IReadOnlyGraph<T>` / `IReadOnlyWeightedGraph<TVertex>` interfaces, and the element-keyed union-find structure `DisjointSet<T>`.

## Trees and tries

The `Bodu.Collections.Generic.Trees` namespace provides `Tree<T>` (a mutable n-ary tree node with iterative, stack-safe traversals), the prefix trees `Trie` / `Trie<TValue>` (string set / string-keyed map with prefix queries), their path-compressed siblings `RadixTrie` / `RadixTrie<TValue>` (identical member-for-member surface over PATRICIA-style edges), and `AhoCorasickAutomaton` / `AhoCorasickAutomaton<TValue>` (immutable multi-pattern matchers that report every occurrence of every pattern in a single O(text + matches) pass).

## Probabilistic collections

The `Bodu.Collections.Probabilistic` namespace ships three fixed-footprint approximate sketches, each with a one-sided, quantified error contract:

- `BloomFilter<T>` — approximate set membership with no false negatives; false positives approach the design rate as the fill approaches `ExpectedItems`.
- `CountMinSketch<T>` — approximate frequencies that never underestimate; overestimates by at most `ε · TotalCount` with probability ≥ `1 − δ`.
- `HyperLogLog<T>` — approximate distinct counts with a relative standard error of about `1.04/√m` for `m = 2^precision` one-byte registers.

All three hash through an `IEqualityComparer<T>`, merge with parameter-compatible instances, and round-trip their state through an opaque, version-checked export format. None is thread-safe.

The thread-safe variants (`ConcurrentCircularBuffer<T>`, `ConcurrentHashSet<T>`) ship separately in the `Bodu.Collections.Concurrent` package.

## Testing

Tests live in `test/` as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Collections/test/Bodu.Collections.Test.csproj --settings smoke.runsettings
dotnet test Bodu.Collections/test/Bodu.Collections.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Collections/test/Bodu.Collections.Test.csproj --settings regression.runsettings
```

Collection behaviour is validated through shared contract bases (`CollectionContractTests<>`, `ReadOnlyCollectionContractTests<>`, `SetContractTests<>`, `EnumeratorContractTests<>`, `DebugViewContractTests<>`, `NonGenericCollectionContractTests<>`) so every collection is held to the same interface contract.

## License

MIT. © Bodu Pty. Ltd.
