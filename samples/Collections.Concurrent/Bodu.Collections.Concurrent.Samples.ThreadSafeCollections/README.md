# Bodu.Collections.Concurrent.Samples.ThreadSafeCollections

The thread-safe collection variants from `Bodu.Collections.Concurrent` (namespace
`Bodu.Collections.Generic.Concurrent`): a lock-free bounded ring, a lock-free set, and a
lock-striped bounded cache. Four scenarios cover the FIFO ring and its two overflow modes, the
set's idempotent-add contract and algebra operators, the cache's single-flight `GetOrAdd` and
`ItemEvicted` callback, and a bounded parallel workload that prints only order-independent
aggregates.

Everything runs offline with fixed inputs. The descriptive scenarios run on a single thread; the
parallel scenario mutates the collections from many threads but reports only aggregates that are
invariant under scheduling, so the output is deterministic — byte-identical every run.

```bash
dotnet run --project samples/Collections.Concurrent/Bodu.Collections.Concurrent.Samples.ThreadSafeCollections
```

NuGet consumers would add:

```bash
dotnet add package Bodu.Collections.Concurrent
```

## Scenario 1 — BoundedRingBuffer

**Intent.** Show `ConcurrentCircularBuffer<T>` as a fixed-capacity FIFO ring with two distinct
overflow policies: reject-when-full (the standard `IProducerConsumerCollection<T>` contract that
lets the buffer back a `BlockingCollection<T>`) and overwrite-oldest with an eviction callback.

**What it does.** Part 1 builds a capacity-3 ring with `allowOverwrite: false` and drives it through
the `IProducerConsumerCollection<T>` surface: three `TryAdd`s succeed, the fourth is rejected because
the ring is full, and two `TryTake`s drain the two oldest elements in arrival order. Part 2 builds a
capacity-3 ring with `allowOverwrite: true`, subscribes to `ItemEvicted`, and enqueues five elements
so the two oldest are pushed out as the newest arrive.

**What to expect.** `TryAdd D` returns `False` (the ring is full and will not overwrite); `TryTake`
returns `A` then `B` (FIFO); the overwrite ring evicts `A` and `B`, leaving `C, D, E` as the live
snapshot:

```text
--- ConcurrentCircularBuffer<T> - bounded FIFO ring ---
  What   : Fills a capacity-3 ring past its limit twice: once with allowOverwrite false, driven through the
           IProducerConsumerCollection<T> surface, and once with allowOverwrite true, collecting the ItemEvicted
           callbacks.
  Why    : A bounded buffer has to do something when it fills, and the two sensible answers suit opposite jobs.
           Reject-on-full applies back-pressure - the producer learns it is outrunning the consumer and can slow
           down or shed load. Overwrite-oldest never blocks a producer and silently discards history, which is what
           you want for a telemetry ring or a crash buffer holding the last N events. Choosing the wrong one is how
           a queue either deadlocks or quietly loses data.
  Expect : The first ring accepts A, B, C and refuses D, so TryAdd returns False and Count stays at the capacity of
           3. The second ring accepts all five, evicting A and B - the two oldest - and keeps C, D, E in arrival
           order.

  Reject-on-full (allowOverwrite: false) - the back-pressure personality:
    TryAdd A       : True  (expected True - one of three slots)
    TryAdd B       : True  (expected True)
    TryAdd C       : True  (expected True - the ring is now exactly full)
    TryAdd D (full): False  (expected False - full, and no element is displaced)
    Count/Capacity : 3 / 3  (expected 3 / 3 - the refusal left the contents untouched)
    TryTake x2     : A, B  (expected A, B - FIFO, oldest first)

  Overwrite-oldest (allowOverwrite: true) - the never-block personality:
    evicted        : [A, B]  (expected [A, B] - the two oldest, in the order they were displaced)
    survivors      : [C, D, E]  (expected [C, D, E] - the newest three, oldest first)
    Count/Capacity : 3 / 3  (expected 3 / 3 - a full ring stays full; overwriting is not growth)
```

**APIs demonstrated.** `ConcurrentCircularBuffer<T>(int, bool)`, `IProducerConsumerCollection<T>.TryAdd` /
`.TryTake`, `ConcurrentCircularBuffer<T>.Enqueue`, `.ItemEvicted`, `.ToArray`, `.Count`, `.Capacity`.

## Scenario 2 — LockFreeSet

**Intent.** Show `ConcurrentHashSet<T>`'s idempotent-add contract — `Add` returns whether the element
was newly inserted, so it doubles as the type's try-add — together with membership, removal, and the
in-place set-algebra operators.

**What it does.** Adds two new elements (each `Add` returns `True`), re-adds a present element (returns
`False` without mutating), checks `Contains`, removes an element (returns `True`) and removes it again
(returns `False`). It then runs `UnionWith` / `ExceptWith` / `IntersectWith` over fresh copies of
`{1, 2, 3, 4}`, and finally the non-mutating predicates `IsSupersetOf` / `Overlaps` / `SetEquals`.
Snapshots are sorted before printing because iteration order is unspecified.

**What to expect.** The repeat add and the second remove both report `False`; the algebra operators
produce the expected sets, and all three predicates hold:

```text
--- ConcurrentHashSet<T> - lock-free set ---
  What   : Adds a duplicate and removes a missing element to show what the return values mean, then runs union,
           except and intersect in place over a fresh {1,2,3,4}, and finishes with the predicates that answer
           relationship questions without mutating.
  Why    : Add returning a bool is what makes this set usable as a concurrency primitive rather than just a
           container. The return value is the atomic answer to "did I win the race to insert this?", so it serves as
           a lock-free claim check - exactly once semantics for a de-duplicating worker, for instance - which a
           fire-and-forget Add followed by a separate Contains cannot give you: between those two calls another
           thread may act.
  Expect : Every first operation on an element succeeds and every repeat fails: Add 1 is True then False, Remove 2
           is True then False. The algebra results are the arithmetic ones, and all three predicates are True. Sets
           print sorted, so the order shown is the sort, not the storage order.

  Add / Contains / Remove - the return value is the claim check:
    Add 1 (new)    : True  (expected True - this caller inserted it)
    Add 2 (new)    : True  (expected True)
    Add 1 (repeat) : False  (expected False - already present, set unchanged)
    Contains 2     : True  (expected True)
    Remove 2       : True  (expected True - this caller removed it)
    Remove 2 again : False  (expected False - already gone, not an error)
    Count          : 1  (expected 1 - only element 1 survives)

  Set algebra (in place, so each line starts from a fresh {1,2,3,4}):
    | {4,5,6}      : {1,2,3,4,5,6}  (expected {1,2,3,4,5,6} - 4 was already present and is not duplicated)
    - {2,4}       : {1,3}  (expected {1,3})
    & {2,4,8}     : {2,4}  (expected {2,4} - 8 is absent, so it contributes nothing)

  Predicates (non-mutating, all against {1,2,3,4}):
    IsSupersetOf {2,3}   : True  (expected True - both are present)
    Overlaps {9,4}       : True  (expected True - one shared element is enough)
    SetEquals {4,3,2,1}  : True  (expected True - a set has no order, so the sequence is irrelevant)
```

**APIs demonstrated.** `ConcurrentHashSet<T>.Add` / `.Contains` / `.Remove` / `.Count` / `.ToArray`,
`.UnionWith` / `.ExceptWith` / `.IntersectWith`, `.IsSupersetOf` / `.Overlaps` / `.SetEquals`, and the
`ConcurrentHashSet<T>(IEnumerable<T>)` constructor.

## Scenario 3 — SingleFlightCache

**Intent.** Show `ConcurrentEvictingDictionary<TKey, TValue>` as a bounded cache: the single-flight
`GetOrAdd(key, factory)` that runs a value factory at most once per key, the first-in-first-out
eviction order under capacity pressure, and the `ItemEvicted` callback that fires as entries are
displaced.

**What it does.** Part 1 calls `GetOrAdd(42, factory)` five times against a capacity-8 FIFO cache with
a counted factory — the load-bearing evidence that the factory runs once, not per call. Part 2 uses a
**capacity-1** cache: because the dictionary partitions its capacity across
`min(concurrencyLevel, capacity)` lock-striped segments, capacity 1 guarantees a single segment, so the
FIFO eviction sequence is exact — adding keys `1, 2, 3, 4` displaces `1, 2, 3` in order, leaving `4`
resident. Part 3 inserts 20 distinct keys into a capacity-8 cache and reports only the accounting
invariant: every inserted key is either still resident or was evicted exactly once, so
`survivors + evictions == inserted` holds no matter how keys route across segments, and the
`ItemEvicted` firing count matches `EvictionCount`.

**What to expect.** The factory runs exactly once; the capacity-1 cache evicts `1, 2, 3` in arrival
order; the capacity-8 cache keeps 8 survivors and evicts 12, and both invariants report `True`:

```text
--- ConcurrentEvictingDictionary<TKey,TValue> - single-flight bounded cache ---
  What   : Calls GetOrAdd for one key five times while counting factory invocations, watches a capacity-1 cache
           report each eviction as it happens, then overflows a capacity-8 cache with 20 keys and checks the books
           balance.
  Why    : A cache without single-flight turns a miss into a thundering herd: every caller that misses runs the
           expensive load, so the moment an entry expires the backing store takes N identical queries instead of
           one. Running the factory inside the owning segment's lock collapses those to one. The ItemEvicted
           callback matters for the opposite reason - a bounded cache discards silently by design, and the callback
           is the only way to learn what it dropped.
  Expect : The factory runs exactly once for five GetOrAdd calls. The capacity-1 cache evicts 1, 2, 3 in arrival
           order and holds 4. After 20 inserts into 8 slots, survivors + evictions == 20 and the callback fired
           exactly EvictionCount times - both True.

  Single-flight GetOrAdd - five lookups of one cold key:
    factory calls  : 1  (expected 1 - four lookups were served from the stored value)

  Eviction order at capacity 1 (FIFO, so oldest goes first):
    evicted        : [1, 2, 3]  (expected [1, 2, 3] - each displaced by its successor)
    resident       : 4  (expected 4 - the last one in is the only survivor)

  Accounting after 20 inserts into 8 slots:
    survivors      : 8  (never exceeds the capacity of 8 - that is what bounded means)
    evictions      : 12  (EvictionCount, the cache's own tally)
    books balance  : True  (expected True - every key is resident or evicted exactly once, never both and never neither)
    callback count : True  (expected True - ItemEvicted fired once per eviction, so no drop went unreported)
```

The `survivors 8, evictions 12` split is the deterministic result of even key distribution across the
segments on this machine; the two invariant lines below it hold on every machine regardless of that
split, which is why they are the assertions the scenario headlines.

**APIs demonstrated.** `ConcurrentEvictingDictionary<TKey, TValue>(int, EvictingDictionaryPolicy)`,
`EvictingDictionaryPolicy.FirstInFirstOut`, `.GetOrAdd(TKey, Func<TKey, TValue>)`, `.Add`, `.ItemEvicted`,
`.ToArray`, `.Count`, `.EvictionCount`.

## Scenario 4 — ParallelSafety

**Intent.** Show that the concurrent collections stay correct under genuine parallelism, while keeping
the sample deterministic by asserting and printing only order-independent aggregates — never per-item
results, whose arrival order is nondeterministic.

**What it does.** A 4-way `Parallel.For` adds `0..999` into a `ConcurrentHashSet<int>`; the scenario
then checks the final `Count` is 1000 and that the elements sum to `1000 * 999 / 2`. Next, 64 tasks
race to `GetOrAdd` the *same* missing key against a `ConcurrentEvictingDictionary<int, string>`; because
the factory runs inside the owning segment's lock, it fires exactly once despite the stampede.

**What to expect.** The set holds all 1000 elements summing to 499500, and the single-flight factory
count is a deterministic `1`:

```text
--- Parallel safety - invariants that hold for any interleaving ---
  What   : Adds 0..999 to a ConcurrentHashSet from a parallel loop and checks the count and the sum, then has 64
           concurrent callers race to GetOrAdd the same missing key and counts factory invocations.
  Why    : Lock-free is a claim about correctness under contention, and the only honest way to show it is to create
           the contention and then assert something that cannot accidentally be true. A count proves no add was lost
           to a torn update; a sum proves no element was corrupted or duplicated into the wrong slot. The
           single-flight count is the sharpest of the three: the stampede is the exact condition a naive cache
           fails, and the factory still runs once.
  Expect : Count 1000 and sum 499500 - the closed form of 0+1+...+999, so a lost or duplicated element would show up
           even if the count happened to look right. The factory is invoked exactly once across 64 racing callers.
           These values are fixed; only the timing varies between runs.

  Parallel add of 0..999 into one set:
    Count          : 1000  (expected 1000 - a lost add under contention would show here)
    sum            : 499500  (expected 499500 - catches a swap that the count alone would miss)

  64 concurrent callers racing to load one missing key:
    factory calls  : 1  (expected 1 - the other 63 waited and took the loaded value)
```

The `Count`, the `sum`, and the factory count are invariant under thread scheduling, so the output is
byte-identical on every run even though the underlying operations interleave differently each time.

**APIs demonstrated.** `ConcurrentHashSet<T>.Add` / `.Count` / `.ToArray` under `Parallel.For`,
`ConcurrentEvictingDictionary<TKey, TValue>.GetOrAdd(TKey, Func<TKey, TValue>)` under contention,
`EvictingDictionaryPolicy.LeastRecentlyUsed`.

## Layout

```text
Bodu.Collections.Concurrent.Samples.ThreadSafeCollections/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the what/why/expect banner every scenario opens with
  Scenarios/BoundedRingBuffer.cs
  Scenarios/LockFreeSet.cs
  Scenarios/SingleFlightCache.cs
  Scenarios/ParallelSafety.cs
```

## Related

- `Bodu.Collections.Concurrent` — the library under demonstration: `ConcurrentCircularBuffer<T>`
  (lock-free Vyukov MPMC ring), `ConcurrentHashSet<T>` (lock-free split-ordered set), and
  `ConcurrentEvictingDictionary<TKey, TValue>` (lock-striped bounded cache).
- `Bodu.Collections` — the single-threaded counterparts (`CircularBuffer<T>`, `EvictingDictionary<TKey, TValue>`,
  and the wider specialized-collection catalogue).
