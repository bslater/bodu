# Bodu.Collections.Concurrent

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

The thread-safe collection variants of the Bodu collection catalogue: a lock-free bounded MPMC ring buffer and a lock-striped concurrent set. The types were split out of `Bodu.Collections` with the namespace unchanged — code written against `Bodu.Collections.Generic.Concurrent` keeps compiling; only the package reference changes. Both collections implement the standard BCL interfaces and ship struct enumerators over best-effort snapshots. The package references `Bodu.Collections` (and transitively `Bodu.Core`).

## Installation

```shell
dotnet add package Bodu.Collections.Concurrent
```

Targets `net8.0`. Depends on `Bodu.Collections`.

## Collections

| Type | Namespace | Summary |
|---|---|---|
| `ConcurrentCircularBuffer<T>` | `Bodu.Collections.Generic.Concurrent` | Lock-free (Vyukov MPMC) fixed-capacity FIFO ring buffer with optional overwrite-on-full and eviction events |
| `ConcurrentHashSet<T>` | `Bodu.Collections.Generic.Concurrent` | Lock-striped concurrent set implementing `ISet<T>`, with comparer injection and snapshot enumeration |

For the single-threaded counterparts (`CircularBuffer<T>`, `Deque<T>`, and the wider catalogue), see the `Bodu.Collections` package.

## Testing

The types are verified through the shared contract-test bases in `Bodu.Test` (collection, set, enumerator, non-generic, and debug-view contracts) plus concurrency-specific suites: multi-producer/multi-consumer stress runs, overwrite contention, slot-layout and snapshot-stability checks.

## License

MIT — see the repository's license file.
