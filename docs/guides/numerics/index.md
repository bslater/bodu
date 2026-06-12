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

Both types are `readonly struct`, value-equatable, allocation-free in
their common paths, and integrate with the generic-math interfaces
that ship in .NET 8+.

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
  <h3><a href="json-serialization.md">JSON serialization</a></h3>
  <p>Round-tripping <code>Fraction&lt;T&gt;</code> and <code>Interval&lt;T&gt;</code> through <code>System.Text.Json</code> — the <code>Strict</code>, <code>Lenient</code>, and <code>Compact</code> wire shapes and how to register them.</p>
</div>

</div>

## Reading path

1. **[Working with `Fraction<T>`](fraction.md)** — the rational type and its arithmetic surface.
2. **[Formatting and parsing `Fraction<T>`](formatting-and-parsing.md)** — once values are flowing, control how they render and what text round-trips.
3. **[Working with `Interval<T>`](interval.md)** — the interval type, independent of fractions; read in any order.
4. **[JSON serialization](json-serialization.md)** — persist either type; read last, after the value semantics are familiar.

## See also

- [Bodu.Numerics introduction](../../docs/numerics/index.md) — namespaces, headline types, scenarios.
- [Bodu.Numerics getting started](../../docs/numerics/getting-started.md) — install + minimal samples.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic on one page.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.
- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1)
- [`Interval<T>` API reference](xref:Bodu.Numerics.Interval`1)
- [`Interval` static factory helpers](xref:Bodu.Numerics.Interval)
- [`Bodu.Financial` overview](../financial/index.md) — money,
  currency, FX.
