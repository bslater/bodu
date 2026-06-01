---
title: Bodu.Numerics — Introduction
---

# Bodu.Numerics

**Bodu.Numerics** is the numeric-primitives package of the Bodu suite. It ships two value types — `Fraction<T>` for exact rational arithmetic and `Interval<T>` for bounded intervals — both built on the generic-math interfaces (`INumber<T>`, `ISignedNumber<T>`) so they compose with anything that targets the .NET 7+ numeric abstractions.

`Bodu.Numerics` is the dependency that `Bodu.Financial` reaches for when an accounting workflow needs sub-minor-unit precision: `Money<TCurrency>.ToFraction()` round-trips through `Fraction<BigInteger>` for compound interest, percentage-of-percentage, and other chains where deferred rounding matters.

![Bodu.Numerics type map — Fraction<T> and Interval<T> over the generic-math abstractions](../../images/diagrams/numerics-type-map.svg)

## Namespaces and headline types

### `Bodu.Numerics`

| Type | Purpose |
|---|---|
| <xref:Bodu.Numerics.Fraction`1> | Immutable canonical rational over any `IBinaryInteger<T>` backing type. Auto-reduces to GCD-normalised form on construction, raises overflow to `BigInteger` precision internally, and implements the full `INumber<T>` / `ISignedNumber<T>` surface. |
| <xref:Bodu.Numerics.Interval`1> | Immutable bounded interval over any `INumber<T>` endpoint type. Endpoint inclusivity is independent on each side so a single type expresses closed-closed, open-open, closed-open, and open-closed forms. |
| <xref:Bodu.Numerics.Interval> | Non-generic helper class with factory methods (`Closed`, `Open`, `ClosedOpen`, `OpenClosed`) that infer the endpoint type from the arguments. |
| <xref:Bodu.Numerics.Serialization.FractionJsonConverter`1>, <xref:Bodu.Numerics.Serialization.FractionJsonConverterFactory> | `System.Text.Json` converters auto-registered via `[JsonConverter]` on `Fraction<T>` — wire shape is the string `"numerator/denominator"`. |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Exact rational arithmetic across arbitrary backing integer types | <xref:Bodu.Numerics.Fraction`1> |
| Arbitrary-precision rational arithmetic (no overflow) | `Fraction<BigInteger>` |
| Best rational approximation to a `double` or `decimal` within a denominator bound | `Fraction<T>.Approximate(value, maxDenominator)` |
| Closed / open / half-open numeric intervals | <xref:Bodu.Numerics.Interval`1> |
| Membership tests, intersection, union, adjacency over numeric intervals | `Interval<T>.Contains`, `Intersect`, `TryUnion`, `Overlaps` |
| Mixed-number and Unicode-vulgar-fraction formatting | `Fraction<T>.ToString("M")` / `.ToString("U")` |
| Sub-minor-unit-precise monetary calculations | <xref:Bodu.Numerics.Fraction`1> via [`Money<TCurrency>.ToFraction()`](xref:Bodu.Financial.Money`1) |

## Design choices

- **Canonical form on construction.** Every `Fraction<T>` is GCD-reduced with the sign on the numerator and the denominator strictly positive. There is no unreduced form, and `2/4` and `1/2` are indistinguishable after construction.
- **`BigInteger` intermediates.** Arithmetic operations promote operands to `BigInteger`, evaluate exactly, then narrow back to `T`. Overflow on narrowing raises `OverflowException`. `Fraction<BigInteger>` eliminates the narrowing step entirely.
- **One empty interval.** `Interval<T>` honours the mathematical fact that there is one empty set: any inverted-bounds or equal-bounds-with-open-endpoint interval compares equal to `Interval<T>.Empty` and shares its hash code.
- **Generic-math first.** Both types implement the relevant `INumber`-style interfaces so they slot into algorithms written against the generic-math abstractions without bespoke wrappers.

## Where to go next

- **[Core concepts](concepts.md)** — glossary the rest of the documentation assumes.
- **[Getting started](getting-started.md)** — install the package and run minimal samples for `Fraction<T>` and `Interval<T>`.
- **[Working with `Fraction<T>`](../../guides/numerics/fraction.md)** — construction, arithmetic, parsing, formatting, continued fractions, rational approximation.
- **[Working with `Interval<T>`](../../guides/numerics/interval.md)** — endpoint inclusivity, membership, intersection, union, adjacency.
- **[Bodu.Financial introduction](../financial/index.md)** — the monetary library that uses `Fraction<BigInteger>` as its precision escape hatch.
- **[Bodu.Numerics API reference](xref:Bodu.Numerics)** — full type-by-type docs.
