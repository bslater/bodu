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

## Articles

- [Working with `Interval<T>`](interval.md) — endpoint inclusivity,
  set operations, parsing and formatting.

## See also

- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1)
- [`Interval<T>` API reference](xref:Bodu.Numerics.Interval`1)
- [`Interval` static factory helpers](xref:Bodu.Numerics.Interval)
