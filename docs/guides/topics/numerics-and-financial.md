---
title: Numerics & Financial guides
---

# Numerics & Financial guides

Recipe-style walk-throughs for the **Numerics & Financial** topic — [`Bodu.Numerics`](../numerics/index.md), the exact-arithmetic value types `Fraction<T>` and `Interval<T>` (with its set-algebra family: `DiscreteInterval<T>`, `IntervalPair<T>`, and `IntervalSet<T>`), and [`Bodu.Financial`](../financial/index.md), the money, currency, and exchange-rate stack built on top of them.

If you are new to the topic, start with the [Numerics & Financial overview](../../docs/topics/numerics-and-financial.md) for the package boundaries and decision table, and the [Numerics & Financial concepts](../../docs/topics/numerics-and-financial-concepts.md) glossary for the shared vocabulary (canonical form, deferred rounding, `BigInteger` promotion, endpoint inclusivity, minor unit, allocation, provenance).

## Bodu.Numerics

Exact rational arithmetic and first-class numeric ranges over the .NET generic-math abstractions.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../numerics/index.md">Overview</a></h3>
  <p>The two value types, what each is for, and the boundary with <code>Bodu.Financial</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../numerics/fraction.md">Working with Fraction&lt;T&gt;</a></h3>
  <p>Construction, arithmetic, continued fractions, and best rational approximation within a denominator bound.</p>
</div>

<div class="bodu-card">
  <h3><a href="../numerics/interval.md">Working with Interval&lt;T&gt;</a></h3>
  <p>Endpoint inclusivity, membership, intersection, union, adjacency, parsing and formatting.</p>
</div>

<div class="bodu-card">
  <h3><a href="../numerics/formatting-and-parsing.md">Formatting and parsing Fraction&lt;T&gt;</a></h3>
  <p>General, mixed-number, Unicode vulgar-fraction, and percentage specifiers; what the parser accepts; culture and span surfaces.</p>
</div>

<div class="bodu-card">
  <h3><a href="../numerics/json-serialization.md">JSON serialization</a></h3>
  <p>Round-tripping <code>Fraction&lt;T&gt;</code> and <code>Interval&lt;T&gt;</code> through <code>System.Text.Json</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../numerics/interval-algebra.md">Interval algebra</a></h3>
  <p>The set-algebra surface of <code>Interval&lt;T&gt;</code> — intersection, union, difference and symmetric difference, unbounded endpoints, the <code>&amp;</code> / <code>|</code> operators, and the N-ary <code>IntervalSet&lt;T&gt;</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../numerics/discrete-intervals.md">Discrete integer intervals</a></h3>
  <p><code>DiscreteInterval&lt;T&gt;</code> — the integer-domain interval with successor-aware emptiness and adjacency, distinct from the continuous <code>Interval&lt;T&gt;</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../numerics/generic-math-constraints.md">Generic math constraints</a></h3>
  <p>Writing code generic over <code>Fraction&lt;T&gt;</code> and <code>Interval&lt;T&gt;</code> through the .NET <code>INumber&lt;T&gt;</code> / <code>IBinaryInteger&lt;T&gt;</code> abstractions.</p>
</div>

</div>

[Bodu.Numerics API reference](xref:Bodu.Numerics)

## Bodu.Financial

Money with the currency in the type system, the ISO 4217 catalogue, and dated FX with audit-grade provenance.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../financial/index.md">Overview</a></h3>
  <p>What ships in the package and how it pairs with <code>Bodu.Numerics</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../financial/money.md">Working with Money&lt;TCurrency&gt;</a></h3>
  <p>Type-parameter currency, allocation, conversion, exact-arithmetic chains, formatting and parsing, cash rounding.</p>
</div>

<div class="bodu-card">
  <h3><a href="../financial/exchange-rates.md">Working with exchange rates</a></h3>
  <p>Timeless vs. dated provider contracts, the audit-grade lookup result, the composite fallback stack, and the series / table builders.</p>
</div>

<div class="bodu-card">
  <h3><a href="../financial/exchange-rate-lookups.md">Exchange-rate lookups on a known dataset</a></h3>
  <p>One fixed dataset run through every date-resolution policy, tolerance window, and the inverse / identity switches.</p>
</div>

<div class="bodu-card">
  <h3><a href="../financial/dependency-injection.md">Dependency injection</a></h3>
  <p>Register the stack with <code>AddFinancialService(...)</code> — currency lookups, monetary contexts, FX providers, JSON converters, options binding.</p>
</div>

</div>

[Bodu.Financial API reference](xref:Bodu.Financial)

## Suggested reading path

1. **[Working with `Fraction<T>`](../numerics/fraction.md)** — the exact-arithmetic foundation everything else leans on.
2. **[Working with `Interval<T>`](../numerics/interval.md)** — ranges as first-class values.
3. **[Working with `Money<TCurrency>`](../financial/money.md)** — typed money, allocation, and the `ToFraction()` bridge back to exact rationals.
4. **[Working with exchange rates](../financial/exchange-rates.md)** — the FX provider stack and provenance model.
5. **[Dependency injection](../financial/dependency-injection.md)** — let the host compose the stack when you run under `Microsoft.Extensions`.

## See also

- **[Numerics & Financial overview](../../docs/topics/numerics-and-financial.md)** — the topic landing page: package table, decision table, install commands.
- **[Numerics & Financial concepts](../../docs/topics/numerics-and-financial-concepts.md)** — the cross-package vocabulary.
- **[Bodu.Numerics getting started](../../docs/numerics/getting-started.md)** and **[Bodu.Financial getting started](../../docs/financial/getting-started.md)** — install + minimal runnable samples.
