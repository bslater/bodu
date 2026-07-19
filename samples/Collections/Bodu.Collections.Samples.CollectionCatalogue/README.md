# Bodu.Collections.Samples.CollectionCatalogue

A tour of the specialized generic collections in `Bodu.Collections.Generic`: the two fixed-capacity
sequential buffers, the bounded evicting cache, the multi-map / multiset / ordered-set family, the
bidirectional and sorted-navigable dictionaries, and the sequenced dictionary alongside the indexed
priority queue. Five scenarios, one per collection group.

Everything runs offline with fixed inputs — deterministic output every run.

```bash
dotnet run --project samples/Collections/Bodu.Collections.Samples.CollectionCatalogue
```

## Scenario 1 — RingAndDeque

**Intent.** Show the two fixed-capacity buffers side by side: `CircularBuffer<T>` as an overwrite-on-full
FIFO ring, and `Deque<T>` as a double-ended queue whose behaviour when full is governed by a
`DequeOverflowPolicy`.

**What it does.** Fills a capacity-3 ring with five values (`allowOverwrite: true`), so the two oldest are
dropped and reported through `ItemEvicted`. It then builds a bounded (`allowGrow: false`) deque with the
`EvictOpposite` policy, pushes onto both ends until full, and adds one more at the tail to force the head out.

**What to expect.** The ring keeps only its three newest values in FIFO order (`3, 4, 5`) and reports the two
evictions; the deque, once full at `a, b, c`, drops the opposite end (`a`) when `d` is added at the tail:

```text
  ring evicted : 1
  ring evicted : 2
  ring survivors (cap 3) : 3, 4, 5
  ring peek (oldest)   : 3
  ring dequeue (oldest): 3
  deque evicted: a
  deque contents (cap 3): b, c, d
  deque first / last   : b / d
```

**APIs demonstrated.** `CircularBuffer<T>(int, bool)`, `CircularBuffer<T>.Enqueue` / `.Peek` / `.Dequeue` /
`.ItemEvicted`; `Deque<T>(int, bool)`, `Deque<T>.OverflowPolicy` (`DequeOverflowPolicy.EvictOpposite`),
`.AddFirst` / `.AddLast` / `.PeekFirst` / `.PeekLast` / `.ItemEvicted`.

## Scenario 2 — EvictingCache

**Intent.** Show `EvictingDictionary<TKey, TValue>` as a bounded cache whose eviction victim is chosen by an
`EvictingDictionaryPolicy`. Under the least-recently-used policy, reading a key protects it from the next
eviction — the core LRU contract.

**What it does.** Fills a capacity-3 LRU cache with `alpha`, `beta`, `gamma`, then reads `alpha` through the
indexer to promote it to most-recently-used. It asks `PeekEvictionCandidate` who the next victim is, then
inserts a fourth key to force the eviction and prints the survivors sorted by key.

**What to expect.** The read moves `alpha` off the chopping block, so `PeekEvictionCandidate` names `beta`;
inserting `delta` then evicts `beta`, leaving `alpha`, `delta`, `gamma`:

```text
  policy       : LeastRecentlyUsed
  touched alpha via read (now most-recently-used)
  next victim  : beta
  evicted: beta=2
  survivors    : alpha=1, delta=4, gamma=3
```

**APIs demonstrated.** `EvictingDictionary<TKey, TValue>(int, EvictingDictionaryPolicy)`, the indexer
(set and get), `.Policy`, `.PeekEvictionCandidate`, `.ItemEvicted`.

## Scenario 3 — MultiMapsAndSets

**Intent.** Cover the collections that relax the one-key-one-value / no-duplicates rules of a plain
dictionary and set: `MultiValueDictionary<,>` (many values per key), `Multiset<T>` (elements with counts),
and the insertion-ordered `OrderedSet<T>` / `IndexedSet<T>`.

**What it does.** Files four values under two keys (including a duplicate, kept by the default `List`
backing); counts six colour words in a multiset and reads back per-element frequencies; adds items to an
ordered set (rejecting a duplicate) and reads a position by value, then wraps the same elements in an
`IndexedSet<T>` for O(1) positional access.

**What to expect.** `fruit` keeps its duplicate `apple`; the multiset reports `red` three times; the ordered
set preserves first-insertion order (`gamma, alpha, beta`) and reports `beta` at index 2, which the indexed
set confirms positionally:

```text
  fruit : [apple, banana, apple]
  veg   : [carrot]
  multiset total items : 6
  count of 'red'       : 3
  frequencies          : bluex1, greenx2, redx3
  ordered set (insertion order): gamma, alpha, beta
  ordered index of 'beta'      : 2
  indexed[0] / indexed[2]      : gamma / beta
```

**APIs demonstrated.** `MultiValueDictionary<,>.Add` and the value-list indexer / `.Keys`;
`Multiset<T>.Count` / `.CountOf` / `.Frequencies`; `OrderedSet<T>.Add` / `.IndexOf`;
`IndexedSet<T>(IEnumerable<T>)` and the integer indexer.

## Scenario 4 — BiDirectionalAndNavigable

**Intent.** Contrast a bidirectional map with the sorted-navigable containers. `BiDictionary<,>` keeps a
value→key inverse and enforces a one-to-one invariant via a duplicate-value policy; `NavigableSet<T>` /
`NavigableDictionary<,>` answer floor/ceiling/lower/higher neighbour queries and inclusive range views over
comparer-sorted data.

**What it does.** Builds an ISO-code map and reads it forward and through `Inverse`; re-maps the value
`Australia` onto a new key `OZ` under the `Replace` policy, dropping the old key. It then runs floor/ceiling
(inclusive) and lower/higher (exclusive) queries and a `Range` view over a navigable set, and a floor-entry
plus range query over a navigable dictionary of kilometre-marked stations.

**What to expect.** `Replace` rebinds `Australia` to `OZ` and removes `AU`; floor/ceiling of 35 are the
straddling `30`/`40`, lower/higher of 30 exclude 30 itself (`20`/`40`), and the range views return exactly the
in-window elements:

```text
  forward AU        : Australia
  inverse Australia : AU
  after re-map, 'Australia' key is: OZ
  old key 'AU' still present?      : False
  set               : 10, 20, 30, 40, 50
  floor(35)/ceil(35): 30 / 40
  lower(30)/high(30): 20 / 40
  range [20..40]    : 20, 30, 40
  station at/below km 20 : km 12 Junction
  stations in [10..30]   : km 12 Junction, km 27 Riverside
```

**APIs demonstrated.** `BiDictionary<,>(BiDictionaryDuplicateValuePolicy)`, the indexer, `.Inverse`,
`.ContainsKey`; `NavigableSet<T>.TryGetFloor` / `.TryGetCeiling` / `.TryGetLower` / `.TryGetHigher` /
`.Range`; `NavigableDictionary<,>.TryGetFloorEntry` / `.Range`.

## Scenario 5 — SequencedAndPriority

**Intent.** Show two order-aware structures: `SequencedDictionary<,>` preserves *insertion* order (not key
order) with cheap first/last access, and `IndexedPriorityQueue<TElement, TPriority>` is a min-heap that also
supports lowering an already-queued element's priority — the decrease-key operation a Dijkstra loop needs.

**What it does.** Inserts four pipeline steps out of alphabetical order and enumerates them, reads `First` /
`Last`, and pops the head with `TryRemoveFirst`. It then enqueues four tasks by cost, lowers `parse` from 40
to 5 in place with `Update`, and drains the queue to show ascending-priority output.

**What to expect.** The sequenced dictionary enumerates in insertion order (`clone → build → test → ship`);
after the decrease-key, `parse` leads the drain even though it was enqueued last with the highest cost:

```text
  pipeline order : clone -> build -> test -> ship
  first / last   : clone / ship
  removed first  : clone
  remaining      : build -> test -> ship
  peek (min)     : lex @ 10
  decreased 'parse' priority 40 -> 5
  drain order    : parse(5), lex(10), render(20), index(30)
```

**APIs demonstrated.** `SequencedDictionary<,>` collection initializer, `.Keys`, `.First` / `.Last`,
`.TryRemoveFirst`; `IndexedPriorityQueue<,>.Enqueue`, `.Peek`, `.Update` (decrease-key), `.TryDequeue`.

## Layout

```text
Bodu.Collections.Samples.CollectionCatalogue/
  Program.cs                            # runs the scenarios in order
  Scenarios/RingAndDeque.cs
  Scenarios/EvictingCache.cs
  Scenarios/MultiMapsAndSets.cs
  Scenarios/BiDirectionalAndNavigable.cs
  Scenarios/SequencedAndPriority.cs
```

## Related

- `Bodu.Collections.Samples.RangesGraphsTrees` — coalescing range sets, the interval tree, graph algorithms,
  disjoint-set union-find, the tree/trie family, and Aho-Corasick multi-pattern search.
- `Bodu.Collections.Samples.ProbabilisticSketches` — the Bloom filter, count-min sketch, and HyperLogLog
  approximate sketches.
