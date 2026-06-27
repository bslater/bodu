---
uid: Bodu.Financial
---

![Bodu.Financial](~/images/hero-financial.svg)

## Purpose

**Bodu.Financial** is the monetary-primitives package: type-safe money (`Money<TCurrency>`), runtime-tagged money (`Money`), multi-currency portfolios (`MoneyBag`), a shipped catalogue of ~185 ISO 4217 currencies, an exchange-rate provider stack with both timeless and dated lookup, and JSON converters with strict / lenient / compact policy shapes.

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

**Currency catalogue and registry**

- <xref:Bodu.Financial.ICurrency> — static-abstract interface with required `IsoCode` and `MinorUnits` plus optional `CashRoundingIncrement`, `IsHistoric`, `DemonetizedOn`, `SuccessorIsoCode`.
- <xref:Bodu.Financial.CurrencyInfo> — runtime metadata record carrying the same fields.
- <xref:Bodu.Financial.CurrencyRegistry> — static, read-only catalogue over the shipped ISO 4217 currencies (active and historic).
- <xref:Bodu.Financial.CurrencyLookupService> — the `ICurrencyLookup` implementation that resolves ISO codes to metadata (the service registered by `AddFinancialService`).
- <xref:Bodu.Financial.CurrencyDisplay> — currency symbol / display-name formatting for presenting an amount's currency.
- <xref:Bodu.Financial.Currencies.CurrencyCode> — the closed enum that identifies a currency on <xref:Bodu.Financial.Money> and the exchange types; one member per shipped ISO 4217 code, valued by its ISO numeric code.
- <xref:Bodu.Financial.Currencies.CurrencyCodeExtensions> — catalogue helpers over `CurrencyCode`: `GetStatus`, `IsActive`, `IsHistoric`, resolving each member's lifecycle status from its declarative attribute (cached at type initialization).
- <xref:Bodu.Financial.Currencies.CurrencyStatusAttribute> — the `[CurrencyStatus(...)]` annotation on each `CurrencyCode` member that is the declarative source of truth for a currency's lifecycle status.
- The shipped tag types live in <xref:Bodu.Financial.Currencies> (one sealed class per ISO code).

**Rounding, allocation, formatting, and parsing**

- <xref:Bodu.Financial.IRoundingStrategy>, <xref:Bodu.Financial.MidpointRoundingStrategy> — the rounding-strategy contract and the midpoint (banker's / away-from-zero) implementation applied when an amount is reduced to a currency's minor units.
- <xref:Bodu.Financial.ScalePolicy>, <xref:Bodu.Financial.CashRoundingPolicy>, <xref:Bodu.Financial.ConversionRoundingPolicy>, <xref:Bodu.Financial.AllocationPolicy> — policy enums that select scale, cash-rounding increment, conversion-rounding, and allocation-remainder behaviour.
- <xref:Bodu.Financial.MoneyFormatter>, <xref:Bodu.Financial.MoneyFormatterBuilder>, <xref:Bodu.Financial.MoneyFormatOptions>, <xref:Bodu.Financial.Extensions.MoneyCompactFormattingExtensions> — configurable formatting: a formatter, its fluent builder, the options record, and compact (`1.2K`-style) formatting extensions.
- <xref:Bodu.Financial.MoneyParseOptions>, <xref:Bodu.Financial.MoneyParseMode> — parse configuration and the strictness selector for reading money back from text.
- <xref:Bodu.Financial.MoneyConversionResult>, <xref:Bodu.Financial.MoneyBagConversionAudit`1>, <xref:Bodu.Financial.MoneyBagConversionRoundingPolicy> — the runtime-tagged conversion result and the portfolio-conversion audit record plus its rounding policy.
- <xref:Bodu.Financial.Extensions.MoneyOfTCurrencyExchangeRateExtensions> — `Convert`/lookup extension methods on `Money<TCurrency>` over the exchange-rate providers.

**Exchange rate stack**

- <xref:Bodu.Financial.IExchangeRateProvider>, <xref:Bodu.Financial.IDatedExchangeRateProvider> — timeless and dated contracts.
- <xref:Bodu.Financial.ExchangeRate>, <xref:Bodu.Financial.ExchangeRatePair>, <xref:Bodu.Financial.ExchangeRateObservation>, <xref:Bodu.Financial.ExchangeRateSeries> — observation record, strongly-typed (from, to) key, single dated observation value, and an O(log n) read-optimised time series.
- <xref:Bodu.Financial.ExchangeRateSeriesBuilder>, <xref:Bodu.Financial.ExchangeRateSeriesKey>, <xref:Bodu.Financial.ExchangeRateTableBuilder> — mutable companion for building or editing a series, the (pair, provider) key, and a higher-level multi-series editor for import workflows.
- <xref:Bodu.Financial.ExchangeRateLookupOptions>, <xref:Bodu.Financial.ExchangeRateLookupResult>, <xref:Bodu.Financial.ExchangeRateDateResolution> — resolution policy options and the audit-grade lookup result.
- <xref:Bodu.Financial.FixedExchangeRateTable>, <xref:Bodu.Financial.FixedDatedExchangeRateProvider>, <xref:Bodu.Financial.DatedExchangeRateProviderAdapter> — in-memory provider implementations and an adapter that pins a date to a dated provider for codebases that don't need the dated surface. Grouping several providers (prioritised fallback, averaging, per-FX-pair routing) and read-through caching live in [`Bodu.Financial.ExchangeRates.Caching`](Bodu.Financial.ExchangeRates.Caching.md).
- <xref:Bodu.Financial.MoneyConversionResult`2> — audit record bundling source and target money with the full lookup result.
- <xref:Bodu.Financial.WebExchangeRateProvider>, <xref:Bodu.Financial.WebExchangeRateProviderOptions> — the abstract base for HTTP-backed dated providers (accumulates fetched observations into an immutable book / snapshot, coalesces concurrent loads, owns its `HttpClient`) and the abstract options carrying `BaseAddress`, `HttpTimeout`, `UserAgent`, `DefaultLookback`, `CurrencyAliases`, and per-stage log levels. The per-source provider packages live in [`Bodu.Financial.ExchangeRates`](Bodu.Financial.ExchangeRates.md).
- <xref:Bodu.Financial.SingleFlightCoordinator`1> — keyed single-flight coordinator that coalesces concurrent loads of the same key onto one in-flight operation (`RunAsync` / `RunAsync<TResult>`), used internally by `WebExchangeRateProvider` to deduplicate endpoint fetches.
- <xref:Bodu.Financial.ExchangeRateProvenance> — readonly-record-struct recording where a rate came from (provider, optional backend, cached-at / as-of instants), with `Live` and `FromCache` factories.
- <xref:Bodu.Financial.IExchangeRatePairSource`1>, <xref:Bodu.Financial.ExchangeRatePairRequest>, <xref:Bodu.Financial.PairRateData`1> — the pair-based fetch contract (`GetPairAsync`), the request struct (pair + inclusive date range), and the result record (pair, observations, source-specific series metadata).
- <xref:Bodu.Financial.ExchangeRateFormatException> (a `FormatException`), <xref:Bodu.Financial.ExchangeRateSeriesNotFoundException> (a `KeyNotFoundException`) — feed-parse and missing-series failures raised by the provider stack.

**Related namespaces**

- <xref:Bodu.Financial.Currencies> — ~185 sealed ISO 4217 tag types (active plus ~30 historic / demonetised).
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
- **Audit-friendly FX.** Dated lookups return <xref:Bodu.Financial.ExchangeRateLookupResult> carrying the provider name, the date actually used, the offset-day distance from the requested date, the resolution policy that fired, and an inversion flag.
- **Sub-minor-unit precision.** `Money<T>.ToFraction()` / `FromFraction()` / `MultiplyExact()` round-trip through `Fraction<BigInteger>` so chained multiplications and divisions do not accumulate rounding error. See [`Fraction<T>`](~/guides/numerics/fraction.md).
- **Zero balances are pruned.** `MoneyBag` removes zero balances on every operation, so equality and enumeration are stable across insertion order and across serialisation round trips.
- **See also:** the [`Money<TCurrency>` guide](~/guides/financial/money.md), the [`Bodu.Financial.Currencies` reference](xref:Bodu.Financial.Currencies), and the [`Bodu.Financial.Serialization` reference](xref:Bodu.Financial.Serialization).
