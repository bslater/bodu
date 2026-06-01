---
title: Bodu.Financial
---

# Bodu.Financial

`Bodu.Financial` is the financial-primitives library that pairs with
`Bodu.Numerics`. It ships the money, currency, and foreign-exchange
types that an enterprise / accounting workload needs — kept separate
from the generic numeric primitives so consumers of `Fraction<T>`
don't pull in the 185-currency catalogue and the FX provider stack
they don't need.

The package references `Bodu.Numerics` so `Money<TCurrency>` can
hand off to `Fraction<BigInteger>` for exact-arithmetic chains via
`ToFraction()` / `FromFraction(…)` / `MultiplyExact(…)`.

## What's included

- **[`Money<TCurrency>`](xref:Bodu.Financial.Money`1)** — an
  immutable monetary amount whose currency is encoded at the type
  level via an `ICurrency` tag, so cross-currency arithmetic fails
  the build rather than running with the wrong unit at runtime.
- **[`MoneyValue`](xref:Bodu.Financial.MoneyValue)** — the
  runtime-tagged sister type for "currency unknown until
  deserialisation" scenarios.
- **[`MoneyBag`](xref:Bodu.Financial.MoneyBag)** — immutable
  mixed-currency portfolio with aggregate-then-round and
  round-each-then-sum policies.
- **[`CurrencyRegistry`](xref:Bodu.Financial.CurrencyRegistry)** —
  runtime ISO-to-metadata lookup, populated from the source-generated
  catalogue.
- **FX provider stack:**
  [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider)
  for timeless rates plus
  [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
  for dated lookups with full audit metadata
  ([`ExchangeRate`](xref:Bodu.Financial.ExchangeRate),
  [`ExchangeRateSeries`](xref:Bodu.Financial.ExchangeRateSeries),
  [`FixedDatedExchangeRateProvider`](xref:Bodu.Financial.FixedDatedExchangeRateProvider),
  [`CompositeDatedExchangeRateProvider`](xref:Bodu.Financial.CompositeDatedExchangeRateProvider)).

## Articles

- [Working with `Money<TCurrency>`](money.md) — type-parameter
  currency, allocation, conversion, exact-arithmetic chains,
  formatting and parsing.
- [Working with exchange rates](exchange-rates.md) — the FX provider
  stack: timeless vs. dated contracts, the audit-grade
  `ExchangeRateLookupResult`, the composite fallback stack, and the
  `MoneyConversionResult<,>` audit record.

## See also

- [`Money<TCurrency>` API reference](xref:Bodu.Financial.Money`1)
- [`MoneyValue` API reference](xref:Bodu.Financial.MoneyValue)
- [`MoneyBag` API reference](xref:Bodu.Financial.MoneyBag)
- [`CurrencyRegistry`](xref:Bodu.Financial.CurrencyRegistry)
- [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider)
- [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
- [`ICurrency` interface](xref:Bodu.Financial.ICurrency)
- [`Bodu.Numerics` overview](../numerics/index.md) — for the
  underlying `Fraction<T>` escape hatch.
