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
--- CircularBuffer<T> and Deque<T> - fixed-capacity buffers ---
  What   : Pushes five values into a capacity-3 ring that overwrites, reporting each eviction, then builds a bounded
           deque from both ends and overflows it under the EvictOpposite policy.
  Why    : A fixed-capacity buffer never grows, so its whole character is decided by what a write does when it does
           not fit. A ring has one end to drop from, so overwrite-oldest is the only sensible answer and the
           ItemEvicted callback is how you learn what went. A deque has two, so the choice is real: EvictOpposite
           makes an AddLast drop the head, which is what turns a deque into a sliding window over a stream rather
           than a queue that refuses work.
  Expect : The ring evicts 1 then 2 and keeps 3, 4, 5 in FIFO order, so Peek and Dequeue both return 3 - the oldest,
           not the newest. The deque is built as a, b, c and then AddLast("d") evicts a from the far end, leaving b,
           c, d.

  ring evicted : 1  (dropped to make room; without this callback the loss would be silent)
  ring evicted : 2  (dropped to make room; without this callback the loss would be silent)
  ring survivors (cap 3) : 3, 4, 5  (expected 3, 4, 5 - enumeration walks head to tail, so oldest prints first)
  ring peek (oldest)   : 3  (expected 3 - a FIFO ring reads from the oldest end, not the newest)
  ring dequeue (oldest): 3  (expected 3 - same element Peek just reported, now removed)
  deque evicted: a  (expected a - the head, because the write came in at the tail)
  deque contents (cap 3): b, c, d  (expected b, c, d - the window slid by one rather than the write being refused)
  deque first / last   : b / d  (expected b / d - both ends are O(1) to read, which is the point of a deque)
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
--- EvictingDictionary<TKey,TValue> - bounded LRU cache ---
  What   : Fills a capacity-3 LRU cache, reads one key back to promote it, asks which key would go next, then
           inserts a fourth key to force the eviction.
  Why    : A bounded cache has to choose a victim, and the policy is that choice. Least-recently-used bets that what
           you touched lately you will touch again - so under LRU a read is not a passive operation, it is how a key
           earns its place. That surprises people twice: the hot key survives because reads protect it, and an
           innocent-looking diagnostic read reorders the cache. PeekEvictionCandidate is the way to ask the question
           without changing the answer.
  Expect : After reading alpha the recency order is beta, gamma, alpha - so beta is named as the next victim and
           beta is what the fourth insert actually evicts. alpha survives purely because it was read, despite having
           been inserted first.

  policy       : LeastRecentlyUsed  (the victim-selection rule; six are available, and this is the one where reads matter)
  touched alpha via read (now most-recently-used)  - the indexer is not a pure read under LRU
  next victim  : beta  (expected beta - alpha was just promoted past it; asking this way does not itself promote anything)
  evicted: beta=2  (expected beta=2 - fires after the removal commits, so a handler always sees a consistent cache)
  survivors    : alpha=1, delta=4, gamma=3  (expected alpha, delta, gamma - alpha outlived beta only because it was read; printed key-sorted for a stable transcript)
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
--- EvictingDictionary<TKey,TValue> - time-based expiration ---
  What   : Runs the same cache under an absolute time-to-live, a sliding one, and a per-entry override, advancing a
           manual clock rather than sleeping.
  Why    : Expiration is a second, independent dimension from the capacity policy: capacity answers "which key goes
           when I am full", time-to-live answers "which key is too stale to trust". The choice between absolute and
           sliding is the whole decision. Absolute caps how stale a value can ever be, which is what a cached
           credential or price needs. Sliding keeps a value alive as long as it is being used, which is what a
           session needs - and means a busy entry may never expire at all.
  Expect : Under absolute, a read at +8m hits and the same key misses at +12m however often it was read. Under
           sliding, reads at +8m, +16m and +24m all hit because each one restarts the countdown, and only 11 idle
           minutes kill it. Expired entries linger in Count until something touches them - enumeration skips them,
           and RemoveExpired is what actually reclaims the space.

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
--- MultiValueDictionary / Multiset / OrderedSet / IndexedSet ---
  What   : Files values under keys with both a List and a Set backing, counts element frequencies through a
           multiset, then shows insertion-ordered and index-addressable sets.
  Why    : Every one of these is a shape people otherwise build by hand out of a Dictionary and get subtly wrong.
           The multimap's backing choice is a real decision rather than a preference: List keeps duplicates and
           appends in O(1), Set deduplicates per key at the cost of a scan on every add. The ordered sets exist
           because HashSet deliberately has no order, so pairing one with a List to recover insertion order means
           keeping two structures in step - which is exactly the code that drifts.
  Expect : With the List backing, apple appears twice under fruit; with the Set backing the repeat is dropped and
           apple keeps the position of its first occurrence rather than moving to the end. The ordered set reports
           insertion order (gamma, alpha, beta), not sorted order, and answers index queries against that same
           order.

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
--- BiDictionary / NavigableSet / NavigableDictionary ---
  What   : Reads an ISO-code map in both directions and re-maps a value under the Replace policy, then asks
           floor/ceiling/lower/higher neighbour questions against a sorted set and a sorted map.
  Why    : A bidirectional map keeps a one-to-one invariant that two dictionaries maintained by hand do not:
           assigning an already-mapped value to a new key has to do something, and the policy makes that explicit
           rather than leaving a stale reverse entry behind. The navigable containers answer the question a plain
           sorted list cannot without a hand-written binary search - "what is nearest to this value?" - which is the
           shape of every rate lookup, tier boundary and time-series probe.
  Expect : After re-mapping Australia onto OZ, the inverse reports OZ and the old AU key is gone - the Replace
           policy dropped it to preserve one-to-one. Against {10..50}, floor and ceiling of 35 straddle it at 30 and
           40; lower and higher of 30 return 20 and 40, stepping over the exact match that floor and ceiling would
           have returned.

  forward AU        : Australia  (expected Australia - the ordinary key to value direction)
  inverse Australia : AU  (expected AU - a live view, not a copy, so it can never drift from the forward map)
  after re-map, 'Australia' key is: OZ  (expected OZ - Replace rebound the value rather than throwing)
  old key 'AU' still present?      : False  (expected False - one-to-one means rebinding a value must drop its previous key)
  set               : 10, 20, 30, 40, 50  (kept in comparer order, which is what makes the neighbour queries O(log n))
  floor(35)/ceil(35): 30 / 40  (expected 30 / 40 - the elements straddling 35, which is absent)
  lower(30)/high(30): 20 / 40  (expected 20 / 40 - strictly exclusive, so both step over the exact match; floor/ceiling of 30 would both be 30)
  range [20..40]    : 20, 30, 40  (expected 20, 30, 40 - inclusive at both ends, unlike the half-open ranges elsewhere in the library)
  station at/below km 20 : km 12 Junction  (floor on a map returns the whole entry, so the value comes back with the key - the shape of every tier or rate-card lookup)
  stations in [10..30]   : km 12 Junction, km 27 Riverside  (an inclusive sub-map view rather than a filtered copy)
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
--- SequencedDictionary / IndexedPriorityQueue ---
  What   : Enumerates a pipeline definition in insertion order and removes from the front, then lowers an
           already-queued item's priority and drains the queue to show where it landed.
  Why    : Dictionary enumeration order is explicitly unspecified, so anything order-sensitive - a pipeline, a
           migration list, an ordered config - cannot use one without a parallel list to remember the sequence.
           SequencedDictionary makes that order part of the type. IndexedPriorityQueue answers a different gap: a
           standard heap cannot reach an element once queued, so lowering a cost means pushing a duplicate and
           filtering stale pops later. Dijkstra and A* need exactly that operation, which is why the indexed form
           exists.
  Expect : The dictionary enumerates in insertion order rather than hash order, and First and Last are O(1) rather
           than a scan. In the queue, parse starts at priority 40 - behind three others - and after the decrease-key
           to 5 it drains second, having moved without being re-added.

  pipeline order : clone -> build -> test -> ship  (insertion order, guaranteed - a Dictionary would be free to print these in any order at all)
  first / last   : clone / ship  (both O(1); finding the ends of an ordinary dictionary means enumerating it)
  removed first  : clone  (removing from the front is O(1) too, so this doubles as an ordered work queue)
  remaining      : build -> test -> ship  (the surviving entries keep their relative order - removal does not reshuffle)
  peek (min)     : lex @ 10  (a min-queue, so the lowest priority is the front)
  decreased 'parse' priority 40 -> 5  - the element moves within the heap; nothing is re-added and nothing stale is left behind
  drain order    : parse(5), lex(10), render(20), index(30)  (parse comes out second, at its new priority - with a plain heap it would still be sitting at 40)
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
--- LayeredDictionary / DefaultingDictionary ---
  What   : Stacks command-line over file over built-in settings and reads, edits and removes through the merged
           view, then uses a defaulting dictionary to group and to count without any TryGetValue-or-add boilerplate.
  Why    : Layering is how configuration precedence is actually specified - first layer wins, later layers are
           fallbacks - and doing it by copying dictionaries together loses the live link to the sources. The two
           traps are worth stating outright: a write through the view goes to the first layer, shadowing rather than
           editing whichever layer answered the read, and Remove only deletes from that first layer, so removing a
           shadow can reveal a value rather than erase it. A defaulting dictionary trades the opposite way: its
           indexer materializes and stores on a miss, which is what collapses the count-or-create dance into one
           line - and is also why a casual read grows the dictionary.
  Expect : log-level comes from the command line, port from the file, host from the built-in layer, and editing the
           file layer is visible immediately through the view. Removing log-level unshadows the file value rather
           than deleting the key. ContainsKey and TryGetValue leave Count alone; the indexer raises it.

  LayeredDictionary (first layer wins):
    layers       : 3  (expected 3 - searched in order, so position is precedence)
    log-level    : trace  (expected debug - present in all three layers, and the first one wins)
    port         : 8080  (the command line does not set it, so the file layer answers)
    host         : localhost  (only the last layer has it - the fallback doing its job)
    merged Count : 3 distinct keys across 3 layers  (shadowed duplicates are counted once, not per layer)
    port after editing the file layer: 9090  (the view is live - editing a source is visible at once, with nothing to re-merge)
    port after writing through the view: 3000  (the write landed in the FIRST layer, shadowing the file rather than editing it)
      commandLine: [log-level=trace, port=3000]
      configFile : [log-level=info, port=9090]  (untouched - which is the point: a write must not silently rewrite a lower layer)
    Remove("host")     : False  (expected False - Remove only touches the first layer, and host is not in it)
    Remove("log-level"): True then log-level = info  (removing the shadow REVEALS the next layer - the key is not gone)
  DefaultingDictionary (the indexer materializes misses):
    a: [apple, avocado, apricot]
    b: [banana, blueberry]
    c: [cherry]
    ContainsKey("z") : False, Count 0  (expected False and unchanged - the probe answers honestly and creates nothing)
    TryGetValue("z") : False, Count 0  (also non-materializing - use these two when you must not grow the dictionary)
    counters["z"]    : 0, Count 1  (the indexer materialized the default AND stored it, so Count rose on a read)
    counters["z"] += 5 -> 5  (the whole point: compound assignment on a missing key just works, no TryGetValue-or-add)
    widths["alpha"]  : 5  (the factory receives the key, so a default can be computed from it rather than being a constant)
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
--- Table / SegmentedBuffer ---
  What   : Builds a sparse city-by-month rainfall table, reads it by row and by column, removes a whole row and
           column, then appends past several segment boundaries of a segmented buffer.
  Why    : A two-key map done by hand is a Dictionary of Dictionaries, where every read needs two null-checks and a
           column view means walking every row. Table makes the column a first-class view and stores only occupied
           cells, so an absent reading costs nothing rather than a null slot. SegmentedBuffer addresses the other
           classic cost: List<T> grows by allocating a bigger array and copying everything across, which for a large
           log means repeated large-object allocations and a copy pause. Adding a segment does neither - the trade
           is that indexing is a division rather than an offset, and there is no contiguous span to hand out.
  Expect : Only the cells actually recorded are counted, so the column views are ragged - Jan has all three cities
           while Feb has one. Removing a column and a row drops exactly their cells. The buffer indexes across
           segment boundaries as if it were flat, and Clear plus TrimExcess releases the segments.

  Table<TRow, TColumn, TValue> (sparse two-key map):
    cells        : 6 across 3 rows and 3 columns  (fewer than rows x columns - only recorded cells exist, which is what sparse means)
    TryAdd occupied cell : False  (expected False - TryAdd refuses rather than overwriting; the indexer is the way to replace)
    TryGetValue Perth/Feb: False  (value 0 - one call answers both keys, with no intermediate row lookup to null-check)
    Contains Perth/Feb   : False
    ContainsRow/Column   : Perth=True, Feb=True  (a row or column exists exactly when some cell uses it)
      row Darwin : Jan=468
      row Perth  : Jan=9, Mar=19
      row Sydney : Jan=102, Feb=117, Mar=129
      col Jan    : Darwin=468, Perth=9, Sydney=102  (all three cities reported - a column view, not a filtered copy of every row)
      col Feb    : Sydney=117  (ragged by design - the missing cities have no cell, as distinct from having a zero)
    RemoveColumn("Mar"): True -> 4 cells remain  (one call drops the column across every row)
    RemoveRow("Darwin"): True -> 3 cells remain  (and the symmetric operation on a row)
    cells        : (Perth,Jan)=9, (Sydney,Jan)=102, (Sydney,Feb)=117
  SegmentedBuffer<T> (append-only, no reallocation):
    count        : 10  (segment size 4, so three segments are in use - and none of the earlier ones were copied to get here)
    [0] / [3] / [4] / [9]: 1 / 4 / 5 / 10  (indices 3 and 4 sit in different segments, yet the indexer reads as if the storage were flat)
    enumerated   : 1, 2, 3, 4, 5, 6, 7, 8, 9, 10  (enumeration walks the segments in order, so append order is preserved)
    after [9] = 100: last item 100  (append-only refers to length: existing slots are still writable)
    ToArray()    : [1, 2, 3, 4, 5, 6, 7, 8, 9, 100]  (the one operation that does copy - the price of wanting a contiguous array back)
    after Clear + TrimExcess: count 0  (expected 0 - Clear empties, TrimExcess releases the segments rather than holding them for reuse)
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
  SampleConsole.cs                      # the what/why/expect banner every scenario opens with
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
