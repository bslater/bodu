# Numerics Samples

Console applications demonstrating the `Bodu.Numerics` package and its
`Bodu.Numerics.Serialization.Json` companion. Each sample is a standalone project; run one
with:

```bash
dotnet run --project samples/Numerics/<SampleName>
```

Every sample is offline and deterministic: fixed inputs formatted with the invariant culture,
so output does not vary by machine.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Numerics.Samples.Fractions` | `Fraction<T>` exact rational arithmetic with auto-reduction, parse/format, `Fraction<T>` as a first-class `INumber<T>` in generic algorithms, and the continued-fraction surface (`ToContinuedFraction`, `Approximate`, `LimitDenominator`) | `Bodu.Numerics` |
| `Bodu.Numerics.Samples.Intervals` | The `Interval<T>` factories and `Contains`/`Overlaps`, the convex-hull union and the two-piece `Difference`/`SymmetricDifference` `IntervalPair<T>` results, the integer-aware `DiscreteInterval<T>` with adjacency-merge, and the normalizing `IntervalSet<T>` with union/intersect/except/complement | `Bodu.Numerics` |
| `Bodu.Numerics.Samples.StreamingStatistics` | The single-pass statistics types — `RunningStatistics<T>` (Welford mean, population/sample variance and standard deviation, min/max), the sliding-window `MovingSum<T>`/`MovingMinMax<T>`, the streaming `RunningQuantile<T>`, and `BigDecimal` exact scaled arithmetic with rounding modes | `Bodu.Numerics` |
| `Bodu.Numerics.Samples.JsonConverters` | `System.Text.Json` integration — `AddNumericsJsonConverters()` round-tripping `Fraction`/`Interval`/`DiscreteInterval`/`IntervalSet`, the `NumericsJsonPolicy` string-vs-object shapes, the `FractionJsonExtensions` helpers, and a nested numeric-typed POCO | `Bodu.Numerics.Serialization.Json` |

Each sample project has its own README with the four-part per-scenario breakdown (Intent /
What it does / What to expect / APIs demonstrated).
