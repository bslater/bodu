---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for
`Bodu.Collections.Concurrent` under
[`samples/Collections.Concurrent/`](https://github.com/bslater/bodu/tree/master/samples/Collections.Concurrent).
The sample is **offline and deterministic** — even its parallel section prints only
order-independent aggregates so output is byte-identical across runs — and is a member of
`bodu.slnx`, built and executed by CI, so the code it shows cannot drift from the current API.
Its README documents every scenario individually: intent, what the code does, the output to
expect, and the APIs demonstrated.

Run the sample from the repository root:

```bash
dotnet run --project samples/Collections.Concurrent/Bodu.Collections.Concurrent.Samples.ThreadSafeCollections
```

## The samples

### Bodu.Collections.Concurrent.Samples.ThreadSafeCollections

The thread-safe collection variants: `ConcurrentCircularBuffer<T>` as a bounded FIFO ring
through `IProducerConsumerCollection<T>`, `ConcurrentHashSet<T>` lock-free membership and set
operations, and `ConcurrentEvictingDictionary<TKey, TValue>` demonstrating single-flight
`GetOrAdd` (a counted factory that runs exactly once for a repeated key), the `ItemEvicted`
post-commit callback, and eviction order. A final scenario runs a bounded `Parallel.For`
workload against the collections and asserts only the deterministic final aggregates (count,
sum, single-flight invocation count), so concurrency is exercised without nondeterministic
output. *Package: `Bodu.Collections.Concurrent`.*

## Related

- [Collections samples](collections.md) — the single-threaded collection catalogue these are
  built from.
