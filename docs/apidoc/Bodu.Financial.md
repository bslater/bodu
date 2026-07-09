---
uid: Bodu.Financial
---

![Bodu.Financial](~/images/hero-financial.svg)

## Purpose

**Bodu.Financial** is the monetary-primitives package: type-safe money (`Money<TCurrency>`), runtime-tagged money (`Money`), multi-currency portfolios (`MoneyBag`), a shipped catalogue of 184 ISO 4217 currencies (in <xref:Bodu.Financial.Currencies>), an exchange-rate core with both timeless and dated lookup (in <xref:Bodu.Financial.ExchangeRates>), and JSON converters with strict / lenient / compact policy shapes.

Reach for this library when you need monetary arithmetic that the compiler validates — adding USD to JPY should fail the build, not run with the wrong unit — and when you need audit-grade FX conversion that records which date, which provider, and which fallback policy produced a given rate.

## Static documentation

- **[Bodu.Financial introduction](~/docs/financial/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Financial getting started](~/docs/financial/getting-started.md)** — install and minimal samples for typed money, runtime-tagged money, portfolios, FX lookup, and the JSON policies.
- **[Bodu.Financial guides](~/guides/financial/index.md)** — [Working with `Money<TCurrency>`](~/guides/financial/money.md).

## Key types

**Monetary value types**

- <xref:Bodu.Financial.Money`1> — immutable, value-equatable monetary amount whose currency is encoded as the type parameter. Cross-currency arithmetic is a compile error. Provides arithmetic, allocation, conversion, formatting/parsing, cash rounding, minor-unit interop, and `Fraction<BigInteger>` interop.
- <xref:Bodu.Financial.Money> — runtime-tagged sister type with the same surface, where the currency is an ISO 4217 string. Cross-currency arithmetic throws `InvalidOperationException` at runtime. Use for deserialisation and generic invoicing.
- <xref:Bodu.Financial.MoneyBag> — immutable mixed-currency portfolio. Aggregates per-ISO balances, prunes zero balances, enumerates in lexicographic ISO order.

**Currency display**

- <xref:Bodu.Financial.CurrencyDisplay> — currency symbol / display-name formatting for presenting an amount's currency.
- The currency metadata surface — <xref:Bodu.Financial.Currencies.ICurrency>, <xref:Bodu.Financial.Currencies.CurrencyInfo>, <xref:Bodu.Financial.Currencies.CurrencyRegistry>, <xref:Bodu.Financial.Currencies.CurrencyLookupService>, <xref:Bodu.Financial.Currencies.CurrencyCode>, and the 184 sealed tag types — lives in the [`Bodu.Financial.Currencies`](Bodu.Financial.Currencies.md) namespace.

**Rounding, allocation, formatting, and parsing**

- <xref:Bodu.Financial.IRoundingStrategy>, <xref:Bodu.Financial.MidpointRoundingStrategy> — the rounding-strategy contract and the midpoint (banker's / away-from-zero) implementation applied when an amount is reduced to a currency's minor units.
- <xref:Bodu.Financial.ScalePolicy>, <xref:Bodu.Financial.CashRoundingPolicy>, <xref:Bodu.Financial.ConversionRoundingPolicy>, <xref:Bodu.Financial.AllocationPolicy> — policy enums that select scale, cash-rounding increment, conversion-rounding, and allocation-remainder behaviour.
- <xref:Bodu.Financial.MoneyFormatter>, <xref:Bodu.Financial.MoneyFormatterBuilder>, <xref:Bodu.Financial.MoneyFormatOptions>, <xref:Bodu.Financial.Extensions.MoneyCompactFormattingExtensions> — configurable formatting: a formatter, its fluent builder, the options record, and compact (`1.2K`-style) formatting extensions.
- <xref:Bodu.Financial.MoneyParseOptions>, <xref:Bodu.Financial.MoneyParseMode> — parse configuration and the strictness selector for reading money back from text.
- <xref:Bodu.Financial.MoneyConversionResult>, <xref:Bodu.Financial.MoneyBagConversionAudit`1>, <xref:Bodu.Financial.MoneyBagConversionRoundingPolicy> — the runtime-tagged conversion result and the portfolio-conversion audit record plus its rounding policy.
- <xref:Bodu.Financial.MoneyConversionResult`2> — audit record bundling source and target money with the full FX lookup result.
- <xref:Bodu.Financial.Extensions.MoneyOfTCurrencyExchangeRateExtensions> — `Convert`/lookup extension methods on `Money<TCurrency>` over the exchange-rate providers.

**Related namespaces**

- <xref:Bodu.Financial.Currencies> — the runtime currency metadata surface (`ICurrency`, `CurrencyInfo`, `CurrencyRegistry`, `CurrencyLookupService`, `CurrencyCode`) plus 184 sealed ISO 4217 tag types (155 active plus 29 historic / demonetised).
- <xref:Bodu.Financial.ExchangeRates> — the exchange-rate stack: values, series, in-memory tables, the timeless / dated provider contracts, and (via the separate `Bodu.Financial.ExchangeRates` package) the web-provider machinery the per-source feed packages build on.
- <xref:Bodu.Financial.ExchangeRates.Caching> — provider-agnostic read-through caching and aggregation over any provider.
- <xref:Bodu.Financial.Serialization> — JSON converters and the `FinancialJsonPolicy` enum (`Strict`, `Lenient`, `Compact`).

## Example

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;

Money<USD> dinner = new Money<USD>(54.30m);
Money<USD> tip    = dinner * 0.18m;
Money<USD> total  = dinner + tip;        // OK — same currency

Money<JPY> sushi = new Money<JPY>(2500m);
// var oops = dinner + sushi;            // Compile error — cannot mix currencies

// Fair allocation that preserves the original total exactly.
Money<USD>[] shares = new Money<USD>(0.10m).Allocate(3);
// [0.04, 0.03, 0.03]
```

## Notes

- **Currency in the type system.** `Money<USD>` and `Money<JPY>` are different types. The compiler enforces same-currency arithmetic; `Convert<TTarget>(rate)` is the explicit cross-currency boundary.
- **Banker's rounding default.** Construction rounds to `TCurrency.MinorUnits` using `MidpointRounding.ToEven`. Pass an explicit `MidpointRounding` to opt out.
- **Audit-friendly FX.** Dated lookups return <xref:Bodu.Financial.ExchangeRates.RateLookupResult> carrying the provider name, the date actually used, the offset-day distance from the requested date, the resolution policy that fired, and an inversion flag.
- **Sub-minor-unit precision.** `Money<T>.ToFraction()` / `FromFraction()` / `MultiplyExact()` round-trip through `Fraction<BigInteger>` so chained multiplications and divisions do not accumulate rounding error. See [`Fraction<T>`](~/guides/numerics/fraction.md).
- **Zero balances are pruned.** `MoneyBag` removes zero balances on every operation, so equality and enumeration are stable across insertion order and across serialisation round trips.
- **See also:** the [`Money<TCurrency>` guide](~/guides/financial/money.md), the [`Bodu.Financial.Currencies` reference](xref:Bodu.Financial.Currencies), and the [`Bodu.Financial.Serialization` reference](xref:Bodu.Financial.Serialization).
