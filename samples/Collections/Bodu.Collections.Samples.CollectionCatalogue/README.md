# Bodu.Collections.Samples.CollectionCatalogue

A tour of the specialized generic collections in `Bodu.Collections.Generic`: the two fixed-capacity
sequential buffers, the bounded evicting cache in both its capacity and its time dimension, the multi-map /
multiset / ordered-set family, the bidirectional and sorted-navigable dictionaries, the sequenced dictionary
alongside the indexed priority queue, the two dictionary decorators, and the two-key table with the segmented
buffer. Eight scenarios, one per collection group.

Everything runs offline with fixed inputs — deterministic output every run. The expiration scenario drives the
cache from a `ManualTimeProvider` rather than the wall clock, so it neither sleeps nor varies between runs.

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

## Scenario 3 — ExpiringCache

**Intent.** Show the *time* dimension of `EvictingDictionary<TKey, TValue>`, which is independent of the
capacity policy from Scenario 2. Supplying an `EvictingDictionaryExpiration` gives entries a time-to-live
measured either absolutely or on a sliding window, and — because that configuration also carries the
`TimeProvider` every clock read goes through — the behaviour is demonstrable without sleeping.

**What it does.** Three walkthroughs against a `ManualTimeProvider` stepped forward by exact amounts.
*Absolute:* adds two entries on a 10-minute TTL, reads one at +8m and again at +12m, then compares `Count`
against a real enumeration before sweeping with `RemoveExpired`. *Sliding:* reads one entry three times at
8-minute intervals on the same 10-minute TTL, then leaves it idle for 11 minutes and calls `Touch`.
*Per-entry:* configures a `null` default TTL and mixes a permanent entry with one added through the
`Add(key, value, TimeSpan)` overload.

**What to expect.** Under `Absolute` the +8m read buys the entry nothing — the countdown started when it was
added — so it is gone by +12m. There is no background timer: that failed read removed `session-a` lazily, but
`session-b` was never touched, so it still occupies a slot and still counts towards `Count` even though no
lookup or enumeration can see it. `RemoveExpired()` is what reconciles the two. Under `Sliding` each read
restarts the countdown, so an actively used session survives 24 minutes on a 10-minute TTL and only dies once
idle; `Touch` promotes for the capacity policy but deliberately does *not* refresh the lifetime, so it cannot
resurrect stale data. With a `null` default TTL, only entries added through the TTL overloads expire at all:

```text
  Absolute (reads do not extend the lifetime):
    +08m read a  : hit  (user-1)
    +12m read a  : miss (expired)
    Count        : 1 (session-b lingers - expired but never accessed)
    enumerated   : 0 live entries (expired entries are skipped)
    RemoveExpired: 1 removed, Count now 0
  Sliding (each read restarts the countdown):
    +08m read    : hit  (user-2)
    +16m read    : hit  (user-2)
    +24m read    : hit  (user-2)
    +35m read    : miss (expired) (idle for 11m)
    Touch()      : False (nothing to promote - it is gone)
  Per-entry TTL overriding the default:
    +06m region  : hit  (ap-southeast-2) (added without a TTL)
    +06m token   : miss (expired) (5m TTL elapsed)
    survivors    : region
```

The `enumerated` line goes through `.Select(...)` on purpose: LINQ's `Count()` would short-circuit to the
`ICollection` `Count` property and report the stored count rather than walking the live entries.

**APIs demonstrated.** `EvictingDictionaryExpiration(TimeSpan?, EvictingDictionaryExpirationKind, TimeProvider)`,
`EvictingDictionaryExpirationKind.Absolute` / `.Sliding`,
`EvictingDictionary<,>(int, EvictingDictionaryExpiration?)`, `.Add(TKey, TValue, TimeSpan)`, `.RemoveExpired`,
`.Touch`, `.TryGetValue`, `.Count`.

## Scenario 4 — MultiMapsAndSets

**Intent.** Cover the collections that relax the one-key-one-value / no-duplicates rules of a plain
dictionary and set: `MultiValueDictionary<,>` (many values per key), `Multiset<T>` (elements with counts),
and the insertion-ordered `OrderedSet<T>` / `IndexedSet<T>`.

**What it does.** Files four values under two keys (including a duplicate, kept by the default `List`
backing), then files the *same* four values into a second dictionary built with `MultiValueBacking.Set` to
contrast the two backings; counts six colour words in a multiset and reads back per-element frequencies; adds
items to an ordered set (rejecting a duplicate) and reads a position by value, then wraps the same elements in
an `IndexedSet<T>` for O(1) positional access.

**What to expect.** Under the default `List` backing `fruit` keeps its duplicate `apple` — a list multimap.
Switching the same type to `MultiValueBacking.Set` makes it an order-preserving *set* multimap: the repeat is
dropped, and each surviving value keeps the position of its first occurrence (the trade-off is a linear scan of
the key's values on every add). The multiset reports `red` three times; the ordered set preserves
first-insertion order (`gamma, alpha, beta`) and reports `beta` at index 2, which the indexed set confirms
positionally:

```text
  backing List:
  fruit : [apple, banana, apple]
  veg   : [carrot]
  backing Set:
  fruit : [apple, banana]
  veg   : [carrot]
  multiset total items : 6
  count of 'red'       : 3
  frequencies          : bluex1, greenx2, redx3
  ordered set (insertion order): gamma, alpha, beta
  ordered index of 'beta'      : 2
  indexed[0] / indexed[2]      : gamma / beta
```

**APIs demonstrated.** `MultiValueDictionary<,>.Add` and the value-list indexer / `.Keys` / `.Backing`,
`MultiValueDictionary<,>(MultiValueBacking, IEqualityComparer<TKey>)`, `MultiValueBacking.List` / `.Set`;
`Multiset<T>.Count` / `.CountOf` / `.Frequencies`; `OrderedSet<T>.Add` / `.IndexOf`;
`IndexedSet<T>(IEnumerable<T>)` and the integer indexer.

## Scenario 5 — BiDirectionalAndNavigable

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

## Scenario 6 — SequencedAndPriority

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

## Scenario 7 — ChainedAndDefaulting

**Intent.** Show the two dictionary *decorators*, each the .NET analogue of a Python type that C# has no
built-in answer for. `LayeredDictionary<,>` is `collections.ChainMap`: a live first-wins view over several
dictionaries, which is the precedence model every configuration cascade needs and nobody should hand-roll.
`DefaultingDictionary<,>` is `collections.defaultdict`: the indexer materializes a missing entry, which removes
the check-then-add dance from every grouping and counting loop.

**What it does.** *Layered:* builds a command-line / config-file / built-in cascade (highest precedence first)
and resolves three keys with different depths of shadowing. It then edits the underlying file layer directly to
prove the layers stay live, writes through the view to show the write landing in the first layer only, and runs
two `Remove` calls — one for a key that exists only deeper, one that unshadows the value beneath it.
*Defaulting:* groups six words by first letter using nothing but `groups[word[0]].Add(word)`, then probes a
counter dictionary with `ContainsKey` and `TryGetValue` before reading it through the indexer, and finishes with
a key-dependent factory.

**What to expect.** Each key resolves from the first layer that has it, and the merged `Count` is 3 distinct
keys across 3 layers (note it walks every layer, so it is *not* a cached O(1) property). Editing `configFile`
is visible through the view at once; writing `port` through the view puts it in `commandLine` and leaves
`configFile` untouched. `Remove("host")` returns `false` because `host` lives only in the built-in layer, while
removing the shadowing `log-level` makes the file layer's `info` visible again. On the defaulting side, only the
indexer getter invokes the factory — `ContainsKey` and `TryGetValue` leave `Count` at 0 — and because the miss
is *stored* rather than recomputed, `+=` works on a first sighting:

```text
  LayeredDictionary (first layer wins):
    layers       : 3
    log-level    : trace (command line shadows file and built-in)
    port         : 8080 (file shadows built-in)
    host         : localhost (only the built-in layer has it)
    merged Count : 3 distinct keys across 3 layers
    port after editing the file layer: 9090
    port after writing through the view: 3000 (command-line layer now has it)
      commandLine: [log-level=trace, port=3000]
      configFile : [log-level=info, port=9090] (untouched)
    Remove("host")     : False (it lives in the built-in layer)
    Remove("log-level"): True then log-level = info (unshadowed)
  DefaultingDictionary (the indexer materializes misses):
    a: [apple, avocado, apricot]
    b: [banana, blueberry]
    c: [cherry]
    ContainsKey("z") : False, Count 0 (no materialization)
    TryGetValue("z") : False, Count 0 (still none)
    counters["z"]    : 0, Count 1 (materialized and stored)
    counters["z"] += 5 -> 5
    widths["alpha"]  : 5 (factory saw the key)
```

**APIs demonstrated.** `LayeredDictionary<,>(params IDictionary<TKey, TValue>[])`, `.Layers`, the indexer
(get and set), `.Count`, `.Remove`; `DefaultingDictionary<,>(Func<TKey, TValue>)`, the indexer getter,
`.ContainsKey`, `.TryGetValue`, `.Count`, `.Keys`.

## Scenario 8 — TableAndSegments

**Intent.** Show two structures that replace a habitually hand-rolled shape. `Table<TRow, TColumn, TValue>` is
a sparse two-key map, so callers stop writing `Dictionary<string, Dictionary<string, int>>` and stop forgetting
to create the inner level. `SegmentedBuffer<T>` is an append-only list that grows by adding a fixed-size segment
instead of doubling and copying — no large contiguous allocations, and existing items never move.

**What it does.** *Table:* builds a deliberately sparse (city, month) rainfall table — Perth has no February
reading — then probes it with `TryAdd`, `TryGetValue`, `Contains`, `ContainsRow`, and `ContainsColumn`, prints
each row view and two column views, and drops a whole column and a whole row in one call each.
*SegmentedBuffer:* appends ten integers into a buffer with `segmentSize: 4`, reads across a segment boundary,
enumerates, writes through the indexer, flattens with `ToArray`, and finishes with `Clear` plus `TrimExcess`.

**What to expect.** Six cells over 3 rows × 3 columns — sparse, so the absent Perth/February cell costs nothing
and reports `false` from every probe. `Row` and `Column` are read-only *views* over one slice, so a caller can
hand out one city's series or one month's cross-section without materializing a copy. `RemoveColumn("Mar")`
drops the month across every row in a single call (6 → 4 cells), which a nested-dictionary layout would make the
caller loop for. On the buffer, `[4]` reads the first item of the second segment, and indexed access stays O(1)
because the index divides into a segment number and an offset:

```text
  Table<TRow, TColumn, TValue> (sparse two-key map):
    cells        : 6 across 3 rows and 3 columns
    TryAdd occupied cell : False (Sydney/Jan is taken)
    TryGetValue Perth/Feb: False (value 0)
    Contains Perth/Feb   : False
    ContainsRow/Column   : Perth=True, Feb=True
      row Darwin : Jan=468
      row Perth  : Jan=9, Mar=19
      row Sydney : Jan=102, Feb=117, Mar=129
      col Jan    : Darwin=468, Perth=9, Sydney=102 (all three cities)
      col Feb    : Sydney=117 (Perth and Darwin never reported)
    RemoveColumn("Mar"): True -> 4 cells remain
    RemoveRow("Darwin"): True -> 3 cells remain
    cells        : (Perth,Jan)=9, (Sydney,Jan)=102, (Sydney,Feb)=117
  SegmentedBuffer<T> (append-only, no reallocation):
    count        : 10 (segment size 4, so three segments are in use)
    [0] / [3] / [4] / [9]: 1 / 4 / 5 / 10 (4 -> 5 crosses a boundary)
    enumerated   : 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
    after [9] = 100: last item 100
    ToArray()    : [1, 2, 3, 4, 5, 6, 7, 8, 9, 100]
    after Clear + TrimExcess: count 0
```

Note `ContainsColumn("Feb")` is `true` while `Contains("Perth", "Feb")` is `false`: the column exists in the
table because Sydney reported it, but that particular cell does not.

**APIs demonstrated.** `Table<,,>(IEqualityComparer<TRow>, IEqualityComparer<TColumn>)`, the `[row, column]`
indexer, `.Add`, `.TryAdd`, `.TryGetValue`, `.Contains`, `.ContainsRow`, `.ContainsColumn`, `.Count`,
`.RowKeys`, `.ColumnKeys`, `.Row`, `.Column`, `.RemoveRow`, `.RemoveColumn`, and enumeration;
`SegmentedBuffer<T>(int segmentSize)`, `.Add`, the indexer (get and set), `.Count`, `.ToArray`, `.Clear`,
`.TrimExcess`, and enumeration.

## Layout

```text
Bodu.Collections.Samples.CollectionCatalogue/
  Program.cs                            # runs the scenarios in order
  ManualTimeProvider.cs                 # hand-advanced clock for deterministic expiry
  Scenarios/RingAndDeque.cs
  Scenarios/EvictingCache.cs
  Scenarios/ExpiringCache.cs
  Scenarios/MultiMapsAndSets.cs
  Scenarios/BiDirectionalAndNavigable.cs
  Scenarios/SequencedAndPriority.cs
  Scenarios/ChainedAndDefaulting.cs
  Scenarios/TableAndSegments.cs
```

## Related

- `Bodu.Collections.Samples.RangesGraphsTrees` — coalescing range sets, the interval tree, graph algorithms,
  disjoint-set union-find, the tree/trie family, and Aho-Corasick multi-pattern search.
- `Bodu.Collections.Samples.ProbabilisticSketches` — the Bloom filter, count-min sketch, and HyperLogLog
  approximate sketches.
- `Bodu.Collections.Samples.BitSets` — the packed `BitSet` from `Bodu.Collections.Specialized`.
- `Bodu.Collections.Concurrent.Samples.ThreadSafeCollections` — the thread-safe counterparts of the ring,
  the hash set, and this scenario's evicting cache.
