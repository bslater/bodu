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
- **[`Money`](xref:Bodu.Financial.Money)** — the
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
- **FX editing surface:**
  [`ExchangeRateSeriesBuilder`](xref:Bodu.Financial.ExchangeRateSeriesBuilder)
  as the mutable companion to `ExchangeRateSeries`, plus
  [`ExchangeRateTableBuilder`](xref:Bodu.Financial.ExchangeRateTableBuilder) for
  multi-pair / multi-provider import workflows that produce
  immutable snapshots.

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="money.md">Working with <code>Money&lt;TCurrency&gt;</code></a></h3>
  <p>Type-parameter currency, allocation, conversion, exact-arithmetic chains, formatting and parsing, cash rounding, and the runtime-tagged <code>Money</code> / <code>MoneyBag</code> companions.</p>
</div>

<div class="bodu-card">
  <h3><a href="exchange-rates.md">Working with exchange rates</a></h3>
  <p>The FX provider stack — timeless vs. dated contracts, the audit-grade <code>ExchangeRateLookupResult</code>, the composite fallback stack, and the <code>ExchangeRateSeriesBuilder</code> + <code>ExchangeRateTableBuilder</code> editing surface.</p>
</div>

<div class="bodu-card">
  <h3><a href="exchange-types.md">Exchange-rate types — a usage-scenario catalogue</a></h3>
  <p>Every FX type mapped to the scenario it was defined for, with a one-line "reach for this when…" map and a decision walk-through.</p>
</div>

<div class="bodu-card">
  <h3><a href="exchange-rate-lookups.md">Exchange-rate lookups on a known dataset</a></h3>
  <p>One fixed dataset run through every <code>ExchangeRateDateResolution</code> policy, tolerance window, and the inverse / identity switches, with a results matrix.</p>
</div>

<div class="bodu-card">
  <h3><a href="dependency-injection.md">Dependency injection</a></h3>
  <p>Register the financial stack with <code>AddBoduFinancial(...)</code> — currency lookups, named monetary contexts, FX providers, JSON converters, and options binding.</p>
</div>

</div>

## Reading path

1. **[Working with `Money<TCurrency>`](money.md)** — the monetary core; everything else builds on it.
2. **[Working with exchange rates](exchange-rates.md)** — the provider contracts and editing surface for crossing currencies.
3. **[Exchange-rate types](exchange-types.md)** and **[lookups on a known dataset](exchange-rate-lookups.md)** — reference material; dip in when choosing a type or tuning a lookup policy.
4. **[Dependency injection](dependency-injection.md)** — last, once you know which services your application composes.

## See also

- [Bodu.Financial introduction](../../docs/financial/index.md) — namespaces, headline types, scenarios.
- [Bodu.Financial getting started](../../docs/financial/getting-started.md) — install + minimal samples.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic on one page.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.

- [`Money<TCurrency>` API reference](xref:Bodu.Financial.Money`1)
- [`Money` API reference](xref:Bodu.Financial.Money)
- [`MoneyBag` API reference](xref:Bodu.Financial.MoneyBag)
- [`CurrencyRegistry`](xref:Bodu.Financial.CurrencyRegistry)
- [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider)
- [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
- [`ICurrency` interface](xref:Bodu.Financial.ICurrency)
- [`Bodu.Numerics` overview](../numerics/index.md) — for the
  underlying `Fraction<T>` escape hatch.
