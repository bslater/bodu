---
title: Running and moving statistics
---

# Running and moving statistics

The statistics aggregates summarize a sample stream in a single
forward pass and constant space — the samples themselves are never
stored. Four types cover the two common shapes:

| Type | Window | Reports |
|---|---|---|
| `RunningStatistics<T>` | whole stream | count, min, max, mean, variance, standard deviation |
| `RunningQuantile<T>` | whole stream | one quantile estimate (P² algorithm) |
| `MovingSum<T>` | last N samples | sum and mean |
| `MovingMinMax<T>` | last N samples | minimum and maximum |

All four are generic over `INumber<T>`, so they accept any numeric
sample type — but the contract deliberately splits by result:

- **Exact, in `T`:** extrema (`Minimum` / `Maximum`) and the rolling
  window `Sum`.
- **Approximate, as finite `double`:** every derived statistical
  moment — mean, variance, standard deviation, and the quantile
  estimate. Each sample is widened with `double.CreateChecked`, so
  `decimal` samples take on binary floating-point precision in these
  results, and a value that cannot survive the widening — an unbounded
  `BigInteger` beyond `double`'s range — throws `OverflowException`
  (at `Add` for the running accumulators, at `Mean` for a
  `MovingSum<T>` whose exact sum has outgrown `double`).

For exact decimal or arbitrary-precision statistical moments, compute
them from your own retained samples — these types never store the
stream.

## Samples must be finite

`Add` rejects NaN and infinite samples with `ArgumentException` on
every aggregate. A single NaN would otherwise poison every subsequent
mean, variance, and quantile estimate irrecoverably, and an infinite
sample makes windowed eviction arithmetic undefined. Integer sample
types are always finite, so the guard costs nothing there.

## The accumulators are mutable value types

`RunningStatistics<T>` and `RunningQuantile<T>` are **mutable
structs** (the same design as `System.HashCode`). Value semantics are
deliberate — copying an accumulator snapshots it — but they carry the
usual mutable-struct rules:

- Store the accumulator in a mutable field or local, never a
  `readonly` field you intend to keep adding to.
- Pass it by `ref` when a callee should observe the additions.
- Do not capture it in a lambda or iterator and expect reference
  semantics — the capture is an independent copy from that point.
- To checkpoint, assign to another variable: the copy freezes at that
  point while the original keeps accumulating.

The `default` value of each accumulator is valid and empty:
`default(RunningStatistics<T>)` is the empty accumulator, and
`default(RunningQuantile<T>)` is an empty **median** estimator.

The moving-window types (`MovingSum<T>`, `MovingMinMax<T>`) own a
fixed buffer sized by their constructor, so they are ordinary sealed
classes with reference semantics.

## Whole-stream moments: `RunningStatistics<T>`

```csharp
using Bodu.Numerics;

var stats = new RunningStatistics<double>();
foreach (var latency in latencies)
    stats.Add(latency);

stats.Count;                        // samples absorbed
stats.Mean;                         // Welford running mean
stats.SampleStandardDeviation;      // Bessel-corrected (n − 1)
stats.PopulationStandardDeviation;  // biased (n)
stats.Minimum; stats.Maximum;       // exact, in T
```

The variance moments use Welford's online recurrence, which stays
accurate where the naive sum-of-squares formulation catastrophically
cancels (large, closely clustered samples).

Result properties throw `InvalidOperationException` on an empty
accumulator (`SampleVariance` needs at least two samples;
`PopulationVariance` of a single sample is `0`). This matches LINQ's
`Average`/`Min`/`Max` and avoids the untypeable "NaN minimum" an
integer stream would otherwise need.

### Parallel accumulation with `Combine`

Two independently filled accumulators merge losslessly:

```csharp
var combined = RunningStatistics<double>.Combine(partA, partB);
```

`Combine` uses the Chan et al. parallel-variance merge, so a stream
can be partitioned across workers, accumulated independently, and
recombined — the result equals accumulating the concatenated stream
(up to ordinary floating-point rounding). Floating-point addition is
not associative, so different partitionings or merge orders can
differ in the last bits; when bitwise-reproducible results matter,
use a stable partitioning strategy and a deterministic merge order.

## Streaming quantiles: `RunningQuantile<T>`

`RunningQuantile<T>` estimates a single quantile with the P²
algorithm of Jain and Chlamtac (1985): five markers track the target
quantile and its neighbourhood, adjusted with piecewise-parabolic
interpolation as samples arrive.

```csharp
var p95 = new RunningQuantile<double>(0.95);
var median = RunningQuantile<double>.CreateMedian();

foreach (var latency in latencies)
    p95.Add(latency);

p95.Estimate;   // streaming 95th-percentile estimate
```

Behavioural notes:

- The first five samples are held exactly; below five, `Estimate`
  returns the linearly interpolated *empirical* quantile, and from
  the fifth sample the P² markers take over.
- The estimate is an approximation that improves with stream length.
  For exact quantiles over small data, sort and index instead.
- `Reset()` clears the samples but preserves the probability.
- P² estimators **cannot be merged** — the marker states of two
  partitions do not compose. Keep the mergeable moments in
  `RunningStatistics<T>` for partition-and-combine workloads.

## Rolling windows: `MovingSum<T>` and `MovingMinMax<T>`

The moving aggregates answer the finance / telemetry question "what
is the sum / mean / min / max of the last N samples?" in amortized
O(1) per sample:

```csharp
var window = new MovingSum<double>(60);     // last 60 samples
var extrema = new MovingMinMax<double>(60);

foreach (var price in prices)
{
    window.Add(price);
    extrema.Add(price);

    if (window.IsFull)
        Plot(window.Mean, extrema.Minimum, extrema.Maximum);
}
```

- Until the window fills (`IsFull`), the aggregates describe the
  samples received so far; `Count` reports how many the window
  currently covers.
- `MovingSum<T>.Sum` is maintained in `T` — exact for integer,
  `decimal`, and `BigInteger` samples. The empty window sums to
  `T.Zero` (`Mean` throws instead, like the other empty reads).
- The rolling-sum arithmetic is **checked**: a fixed-width integer
  sum that would overflow throws `OverflowException` and leaves the
  window unchanged, rather than silently wrapping.
- For floating-point samples the subtract-on-evict update
  accumulates rounding drift, so `MovingSum<T>` transparently
  recomputes the sum from the buffered window after every full
  window turnover, bounding drift to one window's worth of rounding.
- `MovingMinMax<T>` uses the classic pair of monotonic deques stored
  in fixed ring arrays: each sample is pushed and popped at most
  once per deque, with no per-sample allocation.

## Serialization is out of scope

The accumulators deliberately ship without JSON converters (the same
choice as the `IntervalPair<T>` result types): their state is
transient in-process progress, and serializing the Welford or P²
internals would freeze the internal representation into a wire
contract. Persist the *results* (mean, variance, quantile) instead.

## See also

- [Generic math constraints](generic-math-constraints.md) — how the
  `INumber<T>` constraint drives the API shapes in this package.
- [Working with BigDecimal](bigdecimal.md) — exact decimal samples
  compose with the aggregates like any other `INumber<T>`.
