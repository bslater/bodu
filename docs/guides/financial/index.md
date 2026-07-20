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
- **[`CurrencyRegistry`](xref:Bodu.Financial.Currencies.CurrencyRegistry)** —
  runtime ISO-to-metadata lookup, populated from the source-generated
  catalogue.
- **FX provider stack:**
  [`IRateProvider`](xref:Bodu.Financial.ExchangeRates.IRateProvider)
  for timeless rates plus
  [`IDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.IDatedRateProvider)
  for dated lookups with full audit metadata
  ([`ExchangeRate`](xref:Bodu.Financial.ExchangeRates.ExchangeRate),
  [`RateSeries`](xref:Bodu.Financial.ExchangeRates.RateSeries),
  [`FixedDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider); grouping via
  [`AggregatingRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider)).
- **FX editing surface:**
  [`RateSeriesBuilder`](xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder)
  as the mutable companion to `RateSeries`, plus
  [`RateTableBuilder`](xref:Bodu.Financial.ExchangeRates.RateTableBuilder) for
  multi-pair / multi-provider import workflows that produce
  immutable snapshots. Live providers export those snapshots back out —
  see [Snapshotting and exporting rates](exchange-rate-providers.md#snapshotting-and-exporting-rates).

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="money.md">Working with <code>Money&lt;TCurrency&gt;</code></a></h3>
  <p>Type-parameter currency, allocation, conversion, exact-arithmetic chains, formatting and parsing, cash rounding, and the runtime-tagged <code>Money</code> / <code>MoneyBag</code> companions.</p>
</div>

<div class="bodu-card">
  <h3><a href="monetary-precision.md">Monetary precision &amp; unit pricing</a></h3>
  <p>Sub-minor-unit prices — a 6-dp share price in 2-dp USD — via explicit-scale <code>Money</code> and unrounded <code>CalculatedMoney</code>, with the scale preserved through arithmetic and every JSON wire shape.</p>
</div>

<div class="bodu-card">
  <h3><a href="exchange-rates.md">Working with exchange rates</a></h3>
  <p>The FX provider stack — timeless vs. dated contracts, the audit-grade <code>RateLookupResult</code>, provider grouping via the aggregator, and the <code>RateSeriesBuilder</code> + <code>RateTableBuilder</code> editing surface.</p>
</div>

<div class="bodu-card">
  <h3><a href="exchange-types.md">Exchange-rate types — a usage-scenario catalogue</a></h3>
  <p>Every FX type mapped to the scenario it was defined for, with a one-line "reach for this when…" map and a decision walk-through.</p>
</div>

<div class="bodu-card">
  <h3><a href="exchange-rate-lookups.md">Exchange-rate lookups on a known dataset</a></h3>
  <p>One fixed dataset run through every <code>RateDateResolution</code> policy, tolerance window, and the inverse / identity switches, with a results matrix.</p>
</div>

<div class="bodu-card">
  <h3><a href="exchange-rate-caching.md">Caching and aggregating exchange rates</a></h3>
  <p>Read-through caching one provider per cache (<code>CachingRateProvider</code>, TOML or in-memory, per-provider expiry), and grouping many providers with <code>AggregatingRateProvider</code> — priority fallback, averaging, and per-FX-pair routing.</p>
</div>

<div class="bodu-card">
  <h3><a href="dependency-injection.md">Dependency injection</a></h3>
  <p>Register the financial stack with <code>AddFinancialService(...)</code> — currency lookups, named monetary contexts, FX providers, JSON converters, and options binding.</p>
</div>

</div>

## Reading path

1. **[Working with `Money<TCurrency>`](money.md)** — the monetary core; everything else builds on it.
2. **[Monetary precision & unit pricing](monetary-precision.md)** — when a price is finer than the currency settles at: explicit scale, deferred rounding, and precision-preserving JSON.
3. **[Working with exchange rates](exchange-rates.md)** — the provider contracts and editing surface for crossing currencies.
4. **[Exchange-rate types](exchange-types.md)** and **[lookups on a known dataset](exchange-rate-lookups.md)** — reference material; dip in when choosing a type or tuning a lookup policy.
5. **[Caching exchange rates](exchange-rate-caching.md)** — add a TOML disk cache in front of any provider, with per-provider expiry.
6. **[Dependency injection](dependency-injection.md)** — last, once you know which services your application composes.
7. **[Testing your own provider](testing-providers.md)** and **[runnable samples](../../samples/financial.md)** — the contract-test bases for consumer-written providers, and the offline sample projects under `samples/Financial/` that compose everything above end to end.

## See also

- [Bodu.Financial introduction](../../docs/financial/index.md) — namespaces, headline types, scenarios.
- [Bodu.Financial getting started](../../docs/financial/getting-started.md) — install + minimal samples.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic on one page.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.

- [`Money<TCurrency>` API reference](xref:Bodu.Financial.Money`1)
- [`Money` API reference](xref:Bodu.Financial.Money)
- [`CalculatedMoney` API reference](xref:Bodu.Financial.CalculatedMoney)
- [`MoneyBag` API reference](xref:Bodu.Financial.MoneyBag)
- [`CurrencyRegistry`](xref:Bodu.Financial.Currencies.CurrencyRegistry)
- [`IRateProvider`](xref:Bodu.Financial.ExchangeRates.IRateProvider)
- [`IDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.IDatedRateProvider)
- [`ICurrency` interface](xref:Bodu.Financial.Currencies.ICurrency)
- [`Bodu.Numerics` overview](../numerics/index.md) — for the
  underlying `Fraction<T>` escape hatch.
