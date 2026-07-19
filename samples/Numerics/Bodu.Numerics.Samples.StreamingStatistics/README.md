# Bodu.Numerics.Samples.StreamingStatistics

The single-pass numeric accumulators from `Bodu.Numerics`: `RunningStatistics<T>` (count, extremes,
mean, variance in one pass), the fixed-window `MovingSum<T>` / `MovingMinMax<T>`, the streaming
`RunningQuantile<T>` percentile estimator, and the arbitrary-precision `BigDecimal`. Four scenarios
cover each in turn.

Everything runs offline over fixed input streams — deterministic output every run. Double formatting
uses `CultureInfo.InvariantCulture` so the output never varies by machine culture.

```bash
dotnet run --project samples/Numerics/Bodu.Numerics.Samples.StreamingStatistics
```

## Scenario 1 — RunningStats

**Intent.** Show `RunningStatistics<T>` as a single-pass accumulator that tracks count, min/max,
mean, and variance as values arrive — never storing the stream — using Welford's numerically stable
update.

**What it does.** Folds the fixed stream `{2, 4, 4, 4, 5, 5, 7, 9}` in with `Add`, reads back every
statistic, and then accumulates the same data as two independent halves and merges them with
`Combine` to prove sharded aggregation gives the identical mean.

**What to expect.** Mean `5`, population variance `4` (÷N) versus sample variance `4.5714` (÷N−1,
Bessel's correction), and the combined two-halves mean matching the single-pass mean:

```text
--- RunningStatistics<T>: single-pass summary ---
count                       : 8
min / max                   : 2.0000 / 9.0000
mean                        : 5.0000
population variance / stddev: 4.0000 / 2.0000
sample variance / stddev    : 4.5714 / 2.1381
combined mean (2 halves)    : 5.0000 (count 8)
```

> **Note on member names.** The type exposes `Minimum`/`Maximum` (not `Min`/`Max`) and splits
> variance/standard deviation into `Population*` and `Sample*` variants rather than a single
> `Variance`/`StandardDeviation` member — this sample shows both variants.

**APIs demonstrated.** `RunningStatistics<T>.Add`, `.Count`, `.Minimum`, `.Maximum`, `.Mean`,
`.PopulationVariance`, `.PopulationStandardDeviation`, `.SampleVariance`,
`.SampleStandardDeviation`, `RunningStatistics<T>.Combine`.

## Scenario 2 — SlidingWindows

**Intent.** Show the fixed-window accumulators `MovingSum<T>` and `MovingMinMax<T>`: each keeps only
the most recent `Capacity` values, so as new values arrive the oldest drop out and the running sum /
extremes update in O(1).

**What it does.** Streams the series `{10, 12, 8, 20, 6, 6}` through a capacity-3 `MovingSum` and a
capacity-3 `MovingMinMax`, printing the window sum, mean, min, max, and `IsFull` at every step.

**What to expect.** The window fills after three values (`full` flips to `True`); once full each new
value evicts the oldest, so by the last row the window holds only `{20, 6, 6}` summing to `32`:

```text
--- MovingSum / MovingMinMax: fixed windows ---
value  windowSum  windowMean  windowMin  windowMax  full
10.00      10.00       10.00      10.00      10.00  False
12.00      22.00       11.00      10.00      12.00  False
 8.00      30.00       10.00       8.00      12.00  True
20.00      40.00       13.33       8.00      20.00  True
 6.00      34.00       11.33       6.00      20.00  True
 6.00      32.00       10.67       6.00      20.00  True
final window sum            : 32.00 over last 3 of capacity 3
```

**APIs demonstrated.** `new MovingSum<T>(int)`, `MovingSum<T>.Add`, `.Sum`, `.Mean`, `.Count`,
`.Capacity`, `.IsFull`; `new MovingMinMax<T>(int)`, `MovingMinMax<T>.Add`, `.Minimum`, `.Maximum`.

## Scenario 3 — Quantiles

**Intent.** Show `RunningQuantile<T>` as a streaming percentile estimator that approximates a chosen
quantile from a single pass, holding only a handful of markers instead of the whole stream — the
classic technique for tracking a median or a p95 latency online.

**What it does.** Creates a median estimator with `CreateMedian()` and a p95 estimator with `new
RunningQuantile<double>(0.95)`, then feeds all 100 integers `0..99` (in a fixed non-sorted stride
order) through both.

**What to expect.** After 100 samples the estimates track the true values closely — the true median
of `0..99` is `49.5` and the true p95 is about `94`; the single-pass estimates land at `50.51` and
`95.43` (an estimator, not an exact quantile — the small offset is expected):

```text
--- RunningQuantile<T>: streaming percentiles ---
samples observed            : 100
median (p=0.50) estimate    : 50.51
p95    (p=0.95) estimate    : 95.43
```

**APIs demonstrated.** `RunningQuantile<T>.CreateMedian`, `new RunningQuantile<double>(double)`,
`RunningQuantile<T>.Add`, `.Estimate`, `.Probability`, `.Count`.

## Scenario 4 — BigDecimalArithmetic

**Intent.** Show `BigDecimal` as an arbitrary-precision decimal built from an unscaled `BigInteger`
and a base-10 scale: arithmetic is exact and scale-preserving, while division and rounding are
explicit about the scale and midpoint mode they use.

**What it does.** Parses `0.10` and `0.20` and adds them (exactly `0.3`), builds `123.45` directly
from an unscaled integer and a scale, multiplies (scales add), divides `1 / 3` to an explicit
10-place scale, and rounds `2.5` under two midpoint modes plus `123.45` to one place.

**What to expect.** `0.10 + 0.20` is exactly `0.3` (the redundant trailing zero trimmed to scale 1);
multiplication produces a scale-3 product; the explicit-scale division terminates at 10 places; and
`2.5` rounds to `2` under banker's rounding but `3` away-from-zero:

```text
--- BigDecimal: exact scaled arithmetic ---
0.10 + 0.20             : 0.3 (scale 1)
unscaled 12345, scale 2 : 123.45 (precision 5)
123.45 * 1.10           : 135.795 (scale 3)
1 / 3 to 10 places      : 0.3333333333
Round(2.5, ToEven)      : 2
Round(2.5, AwayFromZero): 3
Round(123.45, 1 place)  : 123.4
```

**APIs demonstrated.** `BigDecimal.Parse`, `new BigDecimal(BigInteger, int)`, `operator +`,
`operator *`, `.Scale`, `.Precision`, `BigDecimal.One`, `BigDecimal.Divide(BigDecimal, BigDecimal,
int, MidpointRounding)`, `BigDecimal.Round(BigDecimal, int, MidpointRounding)`.

## Layout

```text
Bodu.Numerics.Samples.StreamingStatistics/
  Program.cs                     # runs the scenarios in order
  Scenarios/RunningStats.cs
  Scenarios/SlidingWindows.cs
  Scenarios/Quantiles.cs
  Scenarios/BigDecimalArithmetic.cs
```

## Related

- `Bodu.Numerics.Samples.Fractions` — the exact-rational `Fraction<T>`.
- `Bodu.Numerics.Samples.Intervals` — the interval algebra over the same numeric surface.
```
