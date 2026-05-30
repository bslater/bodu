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
- **[`Money<TCurrency>`](xref:Bodu.Numerics.Money`1)** — an immutable
  monetary amount whose currency is encoded at the type level via an
  `ICurrency` tag, so cross-currency arithmetic fails the build rather
  than running with the wrong unit at runtime. Pair it with
  **[`MoneyValue`](xref:Bodu.Numerics.MoneyValue)** for the
  runtime-tagged equivalent and
  **[`MoneyBag`](xref:Bodu.Numerics.MoneyBag)** for mixed-currency
  portfolios. Ships with ~180 active and historic ISO 4217 currency
  tags, cash rounding, allocation, exact-arithmetic round-trip,
  JSON, strict parsing, and an `IExchangeRateProvider` abstraction
  for FX conversion.

Both types are `readonly struct`, value-equatable, allocation-free in
their common paths, and integrate with the generic-math interfaces
that ship in .NET 8+.

## Articles

- [Working with `Interval<T>`](interval.md) — endpoint inclusivity,
  set operations, parsing and formatting.
- [Working with `Money<TCurrency>`](money.md) — type-parameter
  currency, allocation, conversion, exact-arithmetic chains,
  formatting and parsing.

## See also

- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1)
- [`Interval<T>` API reference](xref:Bodu.Numerics.Interval`1)
- [`Interval` static factory helpers](xref:Bodu.Numerics.Interval)
- [`Money<TCurrency>` API reference](xref:Bodu.Numerics.Money`1)
- [`MoneyValue` API reference](xref:Bodu.Numerics.MoneyValue)
- [`MoneyBag` API reference](xref:Bodu.Numerics.MoneyBag)
- [`CurrencyRegistry`](xref:Bodu.Numerics.CurrencyRegistry)
- [`IExchangeRateProvider`](xref:Bodu.Numerics.IExchangeRateProvider)
- [`Money` static factory helpers](xref:Bodu.Numerics.Money)
- [`ICurrency` interface](xref:Bodu.Numerics.ICurrency)
