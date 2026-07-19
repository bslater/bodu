# Collections.Concurrent Samples

Console applications demonstrating the `Bodu.Collections.Concurrent` package. Run the sample
with:

```bash
dotnet run --project samples/Collections.Concurrent/<SampleName>
```

The sample is offline and deterministic. The descriptive scenarios exercise the API on a single
thread; the parallel-safety scenario runs a bounded `Parallel.For` workload and asserts only
order-independent aggregates (final count, sum, single-flight invocation count), so output is
byte-identical across runs despite the concurrency.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Collections.Concurrent.Samples.ThreadSafeCollections` | `ConcurrentCircularBuffer<T>` as a bounded FIFO ring through `IProducerConsumerCollection<T>`, `ConcurrentHashSet<T>` lock-free membership and set operations, and `ConcurrentEvictingDictionary<,>` single-flight `GetOrAdd` (a counted factory that runs exactly once for a repeated key), `ItemEvicted`, and eviction order — closed by a parallel workload verified through deterministic aggregates | `Bodu.Collections.Concurrent` |

The sample project has its own README with the four-part per-scenario breakdown (Intent /
What it does / What to expect / APIs demonstrated).
