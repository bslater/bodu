# Bodu.Collections.Concurrent

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

The thread-safe collection variants of the Bodu collection catalogue: a lock-free bounded MPMC ring buffer, a lock-free split-ordered concurrent set, a lock-striped bounded cache, and a read-optimized bounded cache with lock-free reads. The original two types were split out of `Bodu.Collections` with the namespace unchanged - code written against `Bodu.Collections.Generic.Concurrent` keeps compiling; only the package reference changes. All collections implement the standard BCL interfaces and ship struct enumerators over best-effort snapshots. The package references `Bodu.Collections` (and transitively `Bodu.Core`).

## Installation

```shell
dotnet add package Bodu.Collections.Concurrent
```

Targets `net8.0` and `net10.0`. Depends on `Bodu.Collections`.

## Collections

| Type | Namespace | Summary |
|---|---|---|
| `ConcurrentCircularBuffer<T>` | `Bodu.Collections.Generic.Concurrent` | Lock-free (Vyukov MPMC) fixed-capacity FIFO ring buffer with optional overwrite-on-full and eviction events |
| `ConcurrentHashSet<T>` | `Bodu.Collections.Generic.Concurrent` | Lock-free split-ordered concurrent set implementing `ISet<T>`, with comparer injection and snapshot enumeration |
| `ConcurrentEvictingDictionary<TKey,TValue>` | `Bodu.Collections.Generic.Concurrent` | Lock-striped bounded cache - the thread-safe variant of `EvictingDictionary<TKey,TValue>` with all six eviction policies (FIFO/LRU/LFU/MRU/SecondChance/Random), optional TTL expiry, single-flight `GetOrAdd`, and a post-commit `ItemEvicted` event. Eviction order is exact per segment, approximate globally |
| `ConcurrentLruCache<TKey,TValue>` | `Bodu.Collections.Generic.Concurrent` | Read-optimized bounded cache with lock-free reads over a segmented pseudo-LRU (hot/warm/cold queues), maintenance amortized onto writers, and striped hit/miss telemetry. No TTL and no policy choice; `GetOrAdd` is not single-flight, and `Count` may transiently exceed `Capacity` |

For the single-threaded counterparts (`CircularBuffer<T>`, `EvictingDictionary<TKey,TValue>`, `Deque<T>`, and the wider catalogue), see the `Bodu.Collections` package.

## Building and testing

To build this package from source and run its tests, see [CONTRIBUTING.md](https://github.com/bslater/bodu/blob/master/CONTRIBUTING.md).

## License

MIT - see the repository's license file.
