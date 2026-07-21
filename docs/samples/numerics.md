---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Numerics` (and its
`Bodu.Numerics.Serialization.Json` companion) under
[`samples/Numerics/`](https://github.com/bslater/bodu/tree/master/samples/Numerics). All four
samples are **offline and deterministic** — they format with the invariant culture so output
does not vary by machine — and are members of `bodu.slnx`, built and executed by CI, so the code
they show cannot drift from the current API. Each sample's README documents every scenario
individually: its intent, what the code does, the output to expect, and the APIs demonstrated.

Run any sample from the repository root:

```bash
dotnet run --project samples/Numerics/<SampleName>
```

## The samples

### Bodu.Numerics.Samples.Fractions

Exact rational arithmetic with <xref:Bodu.Numerics.Fraction`1>: construction and auto-reduction,
`+ − × ÷` over `Fraction<int>` and `Fraction<BigInteger>`, parse/format, `Fraction<T>` as a
first-class <xref:System.Numerics.INumber`1> in generic algorithms, and the continued-fraction
surface (`ToContinuedFraction`, `Approximate`, `LimitDenominator` — e.g. approximating π to
`355/113`). *Package: `Bodu.Numerics`.*

### Bodu.Numerics.Samples.Intervals

The interval algebra: <xref:Bodu.Numerics.Interval`1> closed/open/half-open factories with
`Contains`/`Overlaps`; the convex-hull union and the two-piece `Difference` /
`SymmetricDifference` results as `IntervalPair<T>`; the integer-aware
<xref:Bodu.Numerics.DiscreteInterval`1> with adjacency-merge and `DiscreteIntervalPair<T>`; and
the normalizing <xref:Bodu.Numerics.IntervalSet`1> with membership, union/intersect/except, and
complement. *Package: `Bodu.Numerics`.*

### Bodu.Numerics.Samples.StreamingStatistics

The single-pass statistics types: `RunningStatistics<T>` (Welford mean, population/sample
variance and standard deviation, min/max), the sliding-window `MovingSum<T>` and
`MovingMinMax<T>`, the streaming `RunningQuantile<T>` (median and p95 estimators), and
`BigDecimal` exact scaled arithmetic with rounding modes. *Package: `Bodu.Numerics`.*

### Bodu.Numerics.Samples.JsonConverters

`System.Text.Json` integration from the companion package: `AddNumericsJsonConverters()`
registration round-tripping `Fraction<T>` / `Interval<T>` / `DiscreteInterval<T>` /
`IntervalSet<T>`, the `NumericsJsonPolicy` string-vs-object wire shapes, the
`FractionJsonExtensions` read/write helpers, and a nested POCO whose numeric-typed properties
round-trip in one document. *Package: `Bodu.Numerics.Serialization.Json`.*

## Related

- [Financial samples](financial.md) — money and exchange-rate types, including a dedicated JSON
  sample built on the same companion-package pattern.
