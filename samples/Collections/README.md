# Collections Samples

Console applications demonstrating the `Bodu.Collections` package. Each sample is a standalone
project; run one with:

```bash
dotnet run --project samples/Collections/<SampleName>
```

Every sample is offline and deterministic: fixed inputs, unordered enumerations sorted before
printing, and — for the sketches and the string-keyed graph — a stable ordinal comparer so that
per-process string-hash randomization cannot vary the output.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Collections.Samples.CollectionCatalogue` | The general-purpose collections — `CircularBuffer<T>` (overwrite on full) and `Deque<T>` with its overflow policy, the `EvictingDictionary<,>` bounded cache and its policies/`ItemEvicted` order, the `MultiValueDictionary`/`Multiset`/`OrderedSet`/`IndexedSet` family, `BiDictionary` forward/inverse lookup, `NavigableSet`/`NavigableDictionary` floor/ceiling navigation, and `SequencedDictionary` plus the update-priority `IndexedPriorityQueue` | `Bodu.Collections` |
| `Bodu.Collections.Samples.RangesGraphsTrees` | The structural collections — `RangeSet`/`RangeDictionary` coalescing and `IntervalTree` point/overlap queries, `Graph<T>` with `GraphAlgorithms` BFS/topological-sort/shortest-path, `DisjointSet<T>` union-find, `Tree<T>`/`Trie`/`RadixTrie`, and the `AhoCorasickAutomaton` multi-pattern scan | `Bodu.Collections` |
| `Bodu.Collections.Samples.ProbabilisticSketches` | The `Bodu.Collections.Probabilistic` sketches — `BloomFilter<T>` (no false negatives, one demonstrated false positive), `CountMinSketch<T>` (never underestimates), and `HyperLogLog<T>` (a distinct-count estimate reported against the true cardinality and the ~1.04/√m standard error) | `Bodu.Collections` |

Each sample project has its own README with the four-part per-scenario breakdown (Intent /
What it does / What to expect / APIs demonstrated).
