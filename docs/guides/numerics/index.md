---
title: Bodu.Numerics
---

# Bodu.Numerics

`Bodu.Numerics` is a small numeric-primitives library that ships value
types covering common but missing gaps in the .NET BCL:

- **[`Fraction<T>`](xref:Bodu.Numerics.Fraction`1)** — an immutable
  exact-rational number type generic over any
  `IBinaryInteger<T>` backing component. Use it for accounting,
  precise decimal arithmetic, or anywhere floating-point rounding is
  unacceptable.
- **[`Interval<T>`](xref:Bodu.Numerics.Interval`1)** — an immutable
  bounded interval generic over any `INumber<T>` endpoint type, with
  independent open or closed endpoints on each side and full set
  algebra. Use it for guarded numeric ranges, validation predicates,
  bucketing, and reservation-style overlap checks.
- **[`BigDecimal`](xref:Bodu.Numerics.BigDecimal)** — an immutable
  arbitrary-precision decimal (`BigInteger` unscaled value plus an
  `int` scale). Use it for exact decimal values that exceed
  `System.Decimal`'s 28–29 digit precision or its exponent range, or
  that must preserve trailing-zero scale.
- **[`RunningStatistics<T>`](xref:Bodu.Numerics.RunningStatistics`1)**
  / **[`RunningQuantile<T>`](xref:Bodu.Numerics.RunningQuantile`1)** —
  single-pass, constant-space stream accumulators: Welford
  count/min/max/mean/variance with a parallel `Combine` merge, and a
  P² streaming quantile estimator.
- **[`MovingSum<T>`](xref:Bodu.Numerics.MovingSum`1)** /
  **[`MovingMinMax<T>`](xref:Bodu.Numerics.MovingMinMax`1)** —
  rolling-window companions reporting the sum/mean and min/max of the
  most recent N samples in amortized O(1).

The value types (`Fraction<T>`, `Interval<T>`, `BigDecimal`) are
`readonly struct`, value-equatable, and allocation-free in their
common paths; the statistics accumulators are *mutable* structs (with
class-based rolling windows). Everything integrates with the
generic-math interfaces that ship in .NET 8+.

> **Looking for `Money<TCurrency>`, currencies, or FX?** Those now
> live in the companion **[`Bodu.Financial`](../financial/index.md)**
> package. `Bodu.Financial` depends on `Bodu.Numerics` so
> `Money<T>` can hand off to `Fraction<BigInteger>` for exact
> mid-chain arithmetic.

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="fraction.md">Working with <code>Fraction&lt;T&gt;</code></a></h3>
  <p>Construction, canonical form, arithmetic, continued fractions, and rational approximation over any <code>IBinaryInteger&lt;T&gt;</code> backing type.</p>
</div>

<div class="bodu-card">
  <h3><a href="formatting-and-parsing.md">Formatting and parsing <code>Fraction&lt;T&gt;</code></a></h3>
  <p>General, mixed-number, Unicode vulgar-fraction, and percentage specifiers; what the parser accepts; culture and span surfaces.</p>
</div>

<div class="bodu-card">
  <h3><a href="interval.md">Working with <code>Interval&lt;T&gt;</code></a></h3>
  <p>Endpoint inclusivity, the empty interval, membership and overlap, intersection and union, ISO 31-11 parsing and formatting.</p>
</div>

<div class="bodu-card">
  <h3><a href="bigdecimal.md">Working with <code>BigDecimal</code></a></h3>
  <p>Arbitrary-precision decimal arithmetic — the unscaled-value/scale model, exact add/subtract/multiply, division precision, rounding, and generic-math composition.</p>
</div>

<div class="bodu-card">
  <h3><a href="json-serialization.md">JSON serialization</a></h3>
  <p>Round-tripping <code>Fraction&lt;T&gt;</code> and <code>Interval&lt;T&gt;</code> through <code>System.Text.Json</code> — the <code>Strict</code>, <code>Lenient</code>, and <code>Compact</code> wire shapes and how to register them.</p>
</div>

<div class="bodu-card">
  <h3><a href="interval-algebra.md">Interval algebra</a></h3>
  <p>The set-algebra surface of <code>Interval&lt;T&gt;</code> — intersection, union, difference, symmetric difference, unbounded bounds, the <code>&amp;</code> / <code>|</code> operators, and the empty-interval rules that make the operations total.</p>
</div>

<div class="bodu-card">
  <h3><a href="discrete-intervals.md">Discrete integer intervals</a></h3>
  <p><code>DiscreteInterval&lt;T&gt;</code> — the integer-domain interval with successor-aware emptiness and adjacency, distinct from the continuous <code>Interval&lt;T&gt;</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="interval-algebra.md#disconnected-sets-with-intervalsett">Disconnected sets</a></h3>
  <p><code>IntervalSet&lt;T&gt;</code> — a normalized union of disjoint intervals with N-ary union, intersection, difference, and complement over the whole line.</p>
</div>

<div class="bodu-card">
  <h3><a href="running-statistics.md">Running and moving statistics</a></h3>
  <p><code>RunningStatistics&lt;T&gt;</code>, <code>RunningQuantile&lt;T&gt;</code>, and the rolling-window <code>MovingSum&lt;T&gt;</code> / <code>MovingMinMax&lt;T&gt;</code> — single-pass stream summaries, the mutable-struct usage rules, and the P² estimator's behaviour.</p>
</div>

<div class="bodu-card">
  <h3><a href="generic-math-constraints.md">Generic math constraints</a></h3>
  <p>Writing code generic over <code>Fraction&lt;T&gt;</code> and <code>Interval&lt;T&gt;</code> through the .NET <code>INumber&lt;T&gt;</code> / <code>IBinaryInteger&lt;T&gt;</code> abstractions.</p>
</div>

</div>

## Reading path

1. **[Working with `Fraction<T>`](fraction.md)** — the rational type and its arithmetic surface.
2. **[Formatting and parsing `Fraction<T>`](formatting-and-parsing.md)** — once values are flowing, control how they render and what text round-trips.
3. **[Working with `Interval<T>`](interval.md)** — the interval type, independent of fractions; read in any order.
4. **[Running and moving statistics](running-statistics.md)** — the stream accumulators and rolling windows; independent of the other types.
5. **[JSON serialization](json-serialization.md)** — persist the value types; read last, after the value semantics are familiar.

## See also

- [Bodu.Numerics introduction](../../docs/numerics/index.md) — namespaces, headline types, scenarios.
- [Bodu.Numerics getting started](../../docs/numerics/getting-started.md) — install + minimal samples.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic on one page.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.
- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1)
- [`Interval<T>` API reference](xref:Bodu.Numerics.Interval`1)
- [`BigDecimal` API reference](xref:Bodu.Numerics.BigDecimal)
- [`RunningStatistics<T>` API reference](xref:Bodu.Numerics.RunningStatistics`1)
- [`RunningQuantile<T>` API reference](xref:Bodu.Numerics.RunningQuantile`1)
- [`Interval` static factory helpers](xref:Bodu.Numerics.Interval)
- [`Bodu.Financial` overview](../financial/index.md) — money,
  currency, FX.
