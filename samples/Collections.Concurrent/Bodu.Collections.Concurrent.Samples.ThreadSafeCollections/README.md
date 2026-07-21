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
--- ConcurrentCircularBuffer<T>: bounded FIFO ring ---
TryAdd A         : True
TryAdd B         : True
TryAdd C         : True
TryAdd D (full)  : False
Count / Capacity : 3 / 3
TryTake x2 (FIFO): A, B

overwrite evicted: [A, B]
survivors (FIFO) : [C, D, E]
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
--- ConcurrentHashSet<T>: lock-free set ---
Add 1 (new)      : True
Add 2 (new)      : True
Add 1 (repeat)   : False
Contains 2       : True
Remove 2         : True
Remove 2 (again) : False
Count            : 1

union   {1,2,3,4} | {4,5,6} : {1,2,3,4,5,6}
except  {1,2,3,4} - {2,4}   : {1,3}
inter   {1,2,3,4} & {2,4,8} : {2,4}
IsSupersetOf {2,3}       : True
Overlaps     {9,4}       : True
SetEquals    {4,3,2,1}   : True
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
--- ConcurrentEvictingDictionary<TKey,TValue>: single-flight cache ---
GetOrAdd(42) x5   : factory invoked 1 time(s)

cap 1, add 1..4   : evicted in order [1, 2, 3], resident 4

cap 8, add 20     : survivors 8, evictions 12
accounting        : survivors + evictions == inserted -> True
event fires match : ItemEvicted fired == EvictionCount -> True
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
--- Parallel safety: deterministic aggregates only ---
set add 0..999 in parallel:
  Count            : 1000 (expected 1000)
  sum of elements  : 499500 (expected 499500)

single-flight GetOrAdd, 64 concurrent callers, one key:
  factory invoked  : 1 (expected 1)
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
