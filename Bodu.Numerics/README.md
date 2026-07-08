# Bodu.Numerics

> **API stability — Preview / Release Candidate.** The rational and interval algebra is functionally complete and suitable for trial use. `Fraction<T>` is a stable candidate; the interval-set and discrete-interval APIs may still receive source-breaking refinement before the 1.0 stable release. Breaking changes are otherwise reserved for a major-version bump per [SemVer](https://semver.org).

Numeric value primitives for .NET. The public model is exact rational numbers plus an interval algebra:

- **`Fraction<T>`** — an immutable, exact-rational value type generic over any `IBinaryInteger<T>` backing component.
- **`Interval<T>`** — an immutable connected interval over any `INumber<T>` endpoint type, with independent open/closed (and unbounded) endpoints and set algebra.
- **`DiscreteInterval<T>`** — a connected integer-domain interval over `IBinaryInteger<T>`, with successor/predecessor-aware emptiness and adjacency.
- **`IntervalSet<T>`** — a normalized set of disconnected `Interval<T>` pieces, with N-ary union / intersection / difference / complement.
- **`IntervalPair<T>`** / **`DiscreteIntervalPair<T>`** — allocation-conscious results of a binary difference / symmetric-difference (zero, one, or two disjoint pieces), each convertible to an `IntervalSet<T>`.
- **`BigDecimal`** — an immutable arbitrary-precision decimal (a `BigInteger` unscaled value plus an `int` scale), for exact decimal values beyond `System.Decimal`'s precision or exponent range.
- **`RunningStatistics<T>`** / **`RunningQuantile<T>`** — single-pass, constant-space accumulators over a sample stream: Welford count/min/max/mean/variance with a parallel `Combine` merge, and a P² streaming quantile estimator.
- **`MovingSum<T>`** / **`MovingMinMax<T>`** — rolling-window companions that report the sum/mean and min/max of the most recent N samples in amortized O(1).

> Money, currency, and foreign-exchange types ship in the companion **[Bodu.Financial](https://www.nuget.org/packages/Bodu.Financial)** package. Keeping them separate means a consumer of just `Fraction<T>` does not pull in the ~185-currency ISO 4217 catalogue and FX provider stack.

## Installation

```shell
dotnet add package Bodu.Numerics
```

Targets `net8.0`.

## `Fraction<T>`

`Fraction<T>` is always held in canonical form — strictly positive denominator, sign carried on the numerator, fully reduced. Arithmetic is exact: intermediate results are evaluated with `BigInteger` precision and narrowed back to `T`, throwing `OverflowException` when a fixed-width component cannot represent the canonical result.

```csharp
using Bodu.Numerics;

Fraction<int> a = Fraction<int>.Parse("1/3");
Fraction<int> b = Fraction<int>.Parse("1/6");

Fraction<int> sum = a + b;          // 1/2 (canonical, fully reduced)
string mixed     = (a + 1).ToString("M");   // "1 1/3"
decimal asDecimal = (decimal)sum;            // 0.5m
```

Highlights:

- Arithmetic and comparison operators, named methods (`Add`, `Negate`, `Abs`, `Reciprocal`, `Pow`, `Remainder`), and `GreatestCommonDivisor` / `LeastCommonMultiple`.
- Exact conversions to/from `decimal` and `double` (`FromDecimal` / `FromDouble`), plus `As<TOther>()` to retype the backing component.
- Continued-fraction expansion (`ToContinuedFraction` / `FromContinuedFraction`) and bounded best-rational approximation (`LimitDenominator`).
- Parsing of integer, ratio, mixed-number, Unicode vulgar-fraction, and percent forms across `string`, `ReadOnlySpan<char>`, and UTF-8 (`IParsable`, `ISpanParsable`, `IUtf8SpanParsable`); formatting with general, mixed (`M`), Unicode (`U`), and percent (`P`) specifiers.
- The full generic-math surface — `INumber<Fraction<T>>`, `INumberBase<Fraction<T>>`, `ISignedNumber<Fraction<T>>` — so `Fraction<T>` composes with `INumber<T>`-constrained code.
- XML serialization (`IXmlSerializable`) and `System.Text.Json` support.

## `Interval<T>`

```csharp
using Bodu.Numerics;

Interval<int> a = Interval.Closed(1, 5);      // [1, 5]
Interval<int> b = Interval.OpenClosed(4, 8);  // (4, 8]

bool overlaps          = a.Overlaps(b);       // true
Interval<int> meet     = a.Intersect(b);      // (4, 5]
bool joined            = a.TryUnion(b, out Interval<int> u); // true -> [1, 8]
bool contains          = a.Contains(3);       // true
```

Highlights:

- Independent endpoint inclusivity: `[a, b]`, `(a, b)`, `[a, b)`, `(a, b]`.
- Factory methods (`Closed`, `Open`, `ClosedOpen`, `OpenClosed`, `Singleton`, `Empty`) plus a non-generic `Interval` helper that infers `T` (`Interval.Closed(1, 5)`).
- Set algebra: `Contains(T)`, `Contains(Interval<T>)`, `Overlaps`, `Intersect`, and `TryUnion` (which succeeds only when the result is a single contiguous interval).
- `IsEmpty`, `IsDegenerate`, and `Length`. All empty intervals compare equal to `Empty`.
- ISO 31-11 bracket-notation formatting and parsing (`IFormattable` / `ISpanFormattable` / `IUtf8SpanFormattable` / `IParsable` / `ISpanParsable`). Empty intervals render as the U+2205 EMPTY SET glyph.

### When to use `Interval<T>` vs `DiscreteInterval<T>`

Use **`Interval<T>`** when the endpoints are coordinates in an ordered numeric *continuum* — even when `T` is an integer coordinate type — so the values between the bounds matter. Use **`DiscreteInterval<T>`** (over `IBinaryInteger<T>`) when the interval represents the *set of integers* between its bounds. The distinction is observable:

```csharp
Interval<int>.Open(1, 2).IsEmpty;          // False — the real coordinates between 1 and 2
DiscreteInterval<int>.Open(1, 2).IsEmpty;  // True  — no integer lies strictly between 1 and 2
```

`DiscreteInterval<T>` is integer-only; it is not a general discrete-domain abstraction over `DateOnly`, `char`, or enum ranges. Reach for `IntervalSet<T>` when a set operation can produce a disconnected result.

## `BigDecimal`

`BigDecimal` is an unbounded decimal: a `BigInteger` unscaled value paired with a non-negative `int` scale, so the value is `unscaledValue × 10^-scale`. It grows to whatever precision a value needs — there is no `MinValue`/`MaxValue` and arithmetic never overflows. Add, subtract, and multiply are exact; division computes to a default 50-digit working precision (half-to-even) unless you pass an explicit scale and rounding mode.

```csharp
using Bodu.Numerics;

BigDecimal a = BigDecimal.Add(0.1m, 0.2m);     // 0.3 exactly
BigDecimal b = BigDecimal.Divide(10m, 3m, scale: 2, MidpointRounding.ToEven); // 3.33
BigDecimal big = BigDecimal.Parse("123456789012345678901234567890.123456789",
    System.Globalization.CultureInfo.InvariantCulture);
```

Highlights:

- Exact `Add` / `Subtract` / `Multiply` / `Negate` / `Abs` / `Pow`; `Divide` with a default precision or an explicit scale and `MidpointRounding`; `Remainder`.
- Value-based equality and ordering (`1.0` equals `1.00`), scale-preserving formatting until you `Round` / `Floor` / `Ceiling` / `Truncate`.
- Implicit lifts from `int`, `long`, `BigInteger`, and `decimal`; explicit conversions to/from `double` and to `BigInteger` / `decimal`.
- Parsing of plain and scientific decimal text across `string`, `ReadOnlySpan<char>`, and UTF-8; `G` and `F` formatting through `IFormattable` / `ISpanFormattable` / `IUtf8SpanFormattable`.
- The full generic-math surface — `INumber<BigDecimal>`, `ISignedNumber<BigDecimal>` (`Radix` 10) — so it composes with `INumber<T>`-constrained code.

## Running and moving statistics

The statistics aggregates summarize a sample stream in one forward pass without storing it. `RunningStatistics<T>` (Welford) and `RunningQuantile<T>` (the P² algorithm) are mutable struct accumulators over the whole stream; `MovingSum<T>` and `MovingMinMax<T>` are class-based rolling windows over the most recent N samples. All four accept any `INumber<T>` sample type: extrema and window sums stay exact in `T`, while means, variances, and quantile estimates are computed in `double`.

```csharp
using Bodu.Numerics;

var stats = new RunningStatistics<double>();
var p95 = new RunningQuantile<double>(0.95);
var window = new MovingMinMax<double>(60);

foreach (var latency in latencies)
{
    stats.Add(latency);
    p95.Add(latency);
    window.Add(latency);
}

// stats.Mean, stats.SampleStandardDeviation, stats.Minimum, stats.Maximum
// p95.Estimate                     — streaming 95th-percentile estimate
// window.Minimum, window.Maximum  — extrema of the last 60 samples
```

Highlights:

- O(1) per sample and constant space; the samples themselves are never stored (the moving types buffer at most one window).
- `RunningStatistics<T>.Combine` merges independently filled accumulators losslessly (Chan et al.), so streams can be partitioned and accumulated in parallel; P² estimators are deliberately not mergeable.
- Non-finite samples (NaN, ±∞) are rejected at `Add`, so an estimate can never be silently poisoned, and the rolling-sum arithmetic is checked — fixed-width integer overflow throws instead of silently wrapping.
- The running accumulators are mutable value types: copying one snapshots it, which is also the supported checkpoint mechanism — see the guide for the usage rules.
- No JSON converters are provided for the accumulators: their state is transient in-process progress, not a wire contract.

## Documentation

See the [Bodu.Numerics guide](https://github.com/bslater/bodu/blob/master/docs/guides/numerics/index.md), including the dedicated [`Interval<T>` article](https://github.com/bslater/bodu/blob/master/docs/guides/numerics/interval.md).

## License

MIT. © Bodu Pty. Ltd.
