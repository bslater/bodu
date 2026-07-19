---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Collections` under
[`samples/Collections/`](https://github.com/bslater/bodu/tree/master/samples/Collections). All
three samples are **offline and deterministic** — string-keyed scenarios supply a stable ordinal
comparer so that per-process string-hash randomization cannot vary the output — and are members
of `bodu.slnx`, built and executed by CI, so the code they show cannot drift from the current
API. Each sample's README documents every scenario individually: its intent, what the code does,
the output to expect, and the APIs demonstrated.

Run any sample from the repository root:

```bash
dotnet run --project samples/Collections/<SampleName>
```

## The samples

### Bodu.Collections.Samples.CollectionCatalogue

The general-purpose collections: <xref:Bodu.Collections.Generic.CircularBuffer`1> (overwrite on
full) and <xref:Bodu.Collections.Generic.Deque`1> with its overflow policy, the
<xref:Bodu.Collections.Generic.EvictingDictionary`2> bounded cache with its eviction policies
and `ItemEvicted` order, the `MultiValueDictionary` / `Multiset` / `OrderedSet` / `IndexedSet`
family, the `BiDictionary` forward/inverse map, the `NavigableSet` / `NavigableDictionary`
floor/ceiling/lower/higher navigation, and `SequencedDictionary` plus the update-priority
`IndexedPriorityQueue`. *Package: `Bodu.Collections`.*

### Bodu.Collections.Samples.RangesGraphsTrees

The structural collections: `RangeSet` / `RangeDictionary` coalescing and the `IntervalTree`
point-stab and overlap queries; the `Graph<T>` with `GraphAlgorithms` breadth-first search,
topological sort, and shortest path; `DisjointSet<T>` union-find connectivity; the `Tree<T>`,
`Trie` / `Trie<TValue>`, and `RadixTrie` prefix structures; and the `AhoCorasickAutomaton`
multi-pattern text scan. *Package: `Bodu.Collections`.*

### Bodu.Collections.Samples.ProbabilisticSketches

The `Bodu.Collections.Probabilistic` approximate sketches over fixed inputs with a stable
comparer so the observed error is reproducible: `BloomFilter<T>` (no false negatives across the
added set, with one demonstrated false positive), `CountMinSketch<T>` (estimates never fall
below the true count), and `HyperLogLog<T>` (a distinct-count estimate reported against the true
cardinality and the ~1.04/√m standard error). *Package: `Bodu.Collections`.*

## Related

- [Collections.Concurrent samples](collections-concurrent.md) — the thread-safe variants.
- [Core samples](core.md) — the foundational library the collections build on.
