---
uid: Bodu.Numerics
---

![Bodu.Numerics](~/images/hero-numerics.svg)

## Purpose

**Bodu.Numerics** provides exact rational arithmetic (`Fraction<T>`), arbitrary-precision decimals (`BigDecimal`), generic complex numbers (`Complex<T>`), and bounded numeric intervals (`Interval<T>` with its discrete, pair, and set companions) on top of the .NET generic-math interfaces, plus a small family of single-pass statistics aggregates. Every value type is immutable, value-equatable, and composable with any algorithm written against `INumberBase<T>` / `INumber<T>` / `ISignedNumber<T>`.

Reach for this library when you need rational or decimal arithmetic that does not lose precision to floating-point drift, complex arithmetic over `float` / `double` / `Half`, a single representation for closed / open / half-open intervals (and unions of them), a precision escape hatch for chained calculations that would otherwise accumulate rounding error, or constant-space summaries of a stream of samples.

## Static documentation

- **[Bodu.Numerics introduction](~/docs/numerics/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Numerics getting started](~/docs/numerics/getting-started.md)** — install and minimal samples for `Fraction<T>` and `Interval<T>`.
- **[Bodu.Numerics guides](~/guides/numerics/index.md)** — recipe-style walk-throughs: [`Fraction<T>`](~/guides/numerics/fraction.md), [`BigDecimal`](~/guides/numerics/bigdecimal.md), [`Interval<T>`](~/guides/numerics/interval.md), [interval algebra](~/guides/numerics/interval-algebra.md), [discrete intervals](~/guides/numerics/discrete-intervals.md), [running statistics](~/guides/numerics/running-statistics.md), [generic-math constraints](~/guides/numerics/generic-math-constraints.md), [JSON serialization](~/guides/numerics/json-serialization.md).

## Key types

**Rational arithmetic**

- <xref:Bodu.Numerics.Fraction`1> — immutable rational with auto-reduction to canonical form, `BigInteger`-promoted intermediates for safe arithmetic, and the full `INumber<T>` / `ISignedNumber<T>` surface. Backed by any `IBinaryInteger<T>` — `int`, `long`, `BigInteger`, or a custom type.

**Arbitrary-precision decimals**

- <xref:Bodu.Numerics.BigDecimal> — immutable `BigInteger` unscaled value paired with an `int` scale. Unbounded (no overflow), exact add / subtract / multiply, precision-controlled division, value-based equality across scales, and the full `INumber<BigDecimal>` / `ISignedNumber<BigDecimal>` surface.

**Complex numbers**

- <xref:Bodu.Numerics.Complex`1> — immutable complex number over any `IFloatingPointIeee754<T>` component type; the generic counterpart of the `double`-only `System.Numerics.Complex`, with arithmetic, `Conjugate` / `Reciprocal`, the elementary functions (`Sqrt`, `Exp`, `Log`, `Pow`, trigonometric and hyperbolic), parsing / formatting, and the `INumberBase<T>` / `ISignedNumber<T>` surface (complex numbers do not order, so not `INumber<T>`).

> JSON support is serialization-agnostic in this package; the converters ship in the companion <xref:Bodu.Numerics.Serialization.Json> package.

**Bounded intervals**

- <xref:Bodu.Numerics.Interval`1> — immutable interval over any `INumber<T>` endpoint type. Independent endpoint inclusivity on each side expresses all four conventional shapes; one canonical `Empty` instance covers every degenerate / inverted-bound case.
- <xref:Bodu.Numerics.Interval> — non-generic helper class with factory methods that infer the endpoint type from arguments (`Interval.Closed(1.5, 2.5)` → `Interval<double>`).
- <xref:Bodu.Numerics.DiscreteInterval`1>, <xref:Bodu.Numerics.DiscreteInterval> — the integer-domain counterpart over any `IBinaryInteger<T>`: every shape canonicalizes to closed integer bounds, so successor-adjacent runs merge and an open interval over consecutive integers is empty. See the [discrete intervals guide](~/guides/numerics/discrete-intervals.md).
- <xref:Bodu.Numerics.IntervalPair`1>, <xref:Bodu.Numerics.DiscreteIntervalPair`1> — allocation-free results of a binary `Difference` / `SymmetricDifference`: zero, one, or two disjoint pieces, each with a `ToIntervalSet()` bridge.
- <xref:Bodu.Numerics.IntervalSet`1> — immutable normalized union of disjoint, non-adjacent intervals; the N-ary home for `Union` / `Intersect` / `Except` / `Complement`. See the [interval algebra guide](~/guides/numerics/interval-algebra.md).

**Statistics aggregates**

- <xref:Bodu.Numerics.RunningStatistics`1>, <xref:Bodu.Numerics.RunningQuantile`1> — single-pass, constant-space stream accumulators (Welford count / min / max / mean / variance with a parallel `Combine` merge; a P² streaming quantile estimator).
- <xref:Bodu.Numerics.MovingSum`1>, <xref:Bodu.Numerics.MovingMinMax`1> — rolling-window sum / mean and min / max of the most recent N samples in amortized O(1). See the [running statistics guide](~/guides/numerics/running-statistics.md).

## Example

```csharp
using System.Numerics;
using Bodu.Numerics;

// Exact rational arithmetic — no floating-point drift.
Fraction<int> sum = Fraction<int>.Create(1, 3) + Fraction<int>.Create(1, 5);
Console.WriteLine(sum);        // "8/15"

// Best rational approximation to a real number.
Fraction<int> piApprox = Fraction<int>.Approximate(Math.PI, maxDenominator: 1000);
Console.WriteLine(piApprox);   // "355/113"

// Bounded intervals with independent endpoint inclusivity.
var window = Interval<int>.ClosedOpen(0, 100);
Console.WriteLine(window.Contains(99));   // True
Console.WriteLine(window.Contains(100));  // False — upper exclusive
```

## Notes

- **Canonical form.** Every `Fraction<T>` is GCD-reduced on construction, with the sign on the numerator and the denominator strictly positive. `Fraction<int>.Create(2, 4)` and `Fraction<int>.Create(1, 2)` are indistinguishable; there is no unreduced form.
- **Overflow handling.** Arithmetic operations promote operands to `BigInteger`, evaluate exactly, then narrow back to `T`. Overflow on narrowing raises `OverflowException`. Use `Fraction<BigInteger>` to eliminate the narrowing step entirely.
- **The empty interval is unique.** Any `Interval<T>` constructed with inverted bounds, or with equal bounds and at least one open endpoint, compares equal to `Interval<T>.Empty` and shares its hash code.
- **Generic-math first.** The value types implement the relevant `INumber`-style interfaces — including `IParsable<T>`, `ISpanParsable<T>`, `ISpanFormattable`, and `IUtf8SpanFormattable` — so they slot into algorithms written against the generic-math abstractions without bespoke wrappers.
- **Cross-package.** `Bodu.Financial.Money<TCurrency>` round-trips through `Fraction<BigInteger>` via `ToFraction()` / `FromFraction()` / `MultiplyExact()` for sub-minor-unit-precise monetary chains; see the [Bodu.Financial overview](xref:Bodu.Financial).
- **See also:** the [`Fraction<T>` guide](~/guides/numerics/fraction.md), the [`BigDecimal` guide](~/guides/numerics/bigdecimal.md), the [`Interval<T>` guide](~/guides/numerics/interval.md), the [interval algebra](~/guides/numerics/interval-algebra.md) and [discrete intervals](~/guides/numerics/discrete-intervals.md) guides, and the [running statistics guide](~/guides/numerics/running-statistics.md).
