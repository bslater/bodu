---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for the financial packages under
[`samples/Financial/`](https://github.com/bslater/bodu/tree/master/samples/Financial). Every
sample runs **fully offline** — exchange-rate samples build their providers from committed
static data files instead of calling live feeds — and each carries a clearly fenced comment
block showing how to switch to a real web provider. The samples are members of `bodu.slnx` and
are built and executed by CI, so the code they show cannot drift from the current API.

Run any sample from the repository root:

```bash
dotnet run --project samples/Financial/<SampleName>
```

## The samples

### Bodu.Financial.Samples.MoneyBasics

The core value types, offline with in-code data only. Covers the three-tier rounding model
(<xref:Bodu.Financial.Money`1> per step, <xref:Bodu.Financial.CalculatedMoney> deferred,
`Fraction<BigInteger>` exact via `MultiplyExact`), the typed↔runtime bridges (`As<T>`,
`TryAs<T>`, casts), sum-preserving allocation and cash rounding, the format-specifier
vocabulary and <xref:Bodu.Financial.MoneyFormatterBuilder>, the four
<xref:Bodu.Financial.MoneyParseMode> levels, <xref:Bodu.Financial.MoneyBag> ledgers with
`ConvertToWithAudit`, and the three <xref:Bodu.Financial.Serialization.FinancialJsonPolicy>
shapes. *Packages: `Bodu.Financial`.*

### Bodu.Financial.Samples.OfflineRates

The flagship **static-rate-file pattern**: a committed CSV poured through
<xref:Bodu.Financial.ExchangeRates.RateTableBuilder> into an immutable
<xref:Bodu.Financial.ExchangeRates.RateBook>, served through
<xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider> — the same contracts the live web
providers implement. Works the four <xref:Bodu.Financial.ExchangeRates.RateLookupOptions>
date-resolution modes over weekend gaps and converts typed and runtime money through dated
rates. *Packages: `Bodu.Financial`.*

### Bodu.Financial.Samples.CachedRates

The caching layer against an offline source: the read-through
<xref:Bodu.Financial.ExchangeRates.Caching.CachingRateProvider>, coverage-based range serving
(including negative caching of empty windows), tiered stacking (in-memory L1 over durable file
L2, surviving a simulated restart), and the
<xref:Bodu.Financial.ExchangeRates.RateHistoryAvailability> clamping model. A small counting
decorator makes hit-vs-fetch behaviour visible in the output. *Packages: `Bodu.Financial`,
`Bodu.Financial.ExchangeRates.Caching`.*

### Bodu.Financial.Samples.AggregatedRates

Multi-provider aggregation over two offline feeds with complementary coverage:
<xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider> with priority fallback,
<xref:Bodu.Financial.ExchangeRates.Caching.AverageStrategy> (and its synthetic provenance
label), per-pair <xref:Bodu.Financial.ExchangeRates.Caching.CurrencyPairRoute> overrides, and
the fluent `AddAggregatedRateProvider` DI builder with keyed per-child resolution. *Packages:
`Bodu.Financial`, `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial.DependencyInjection`.*

### Bodu.Financial.Samples.CurrencyServices

Currency services and host wiring: the ambient
<xref:Bodu.Financial.Currencies.CurrencyResolution> seam (`PushScoped` over a restricted-lookup
decorator), named <xref:Bodu.Financial.MonetaryContext> registrations, and the
`AddFinancialService` composition root with `UseCurrencyResolution`. *Packages:
`Bodu.Financial`, `Bodu.Financial.DependencyInjection`.*

### Bodu.Financial.Samples.CustomProvider (+ .Test)

Consumer extensibility: a custom `CsvFileRateProvider` in the recommended shape (builder →
book → delegated fixed provider), used directly, through the conversion extensions, and under
the caching decorator. Its companion test project derives
`DatedRateProviderContractTests<CsvFileRateProvider>` from
`Bodu.Financial.ExchangeRates.Testing` — see [Testing your own provider](../guides/financial/testing-providers.md).
*Packages: `Bodu.Financial`, `Bodu.Financial.ExchangeRates.Caching`,
`Bodu.Financial.ExchangeRates.Testing` (test).*

### Bodu.Financial.Samples.LiveRates

The one sample that goes **online** (and is therefore excluded from the CI samples run): it
fetches real published rates from a live web provider for a computed historical date — the most
recent Wednesday at least five days old, with a `PreviousWithin(5)` tolerance so a published
fixing is near-certain — plus that date's trailing week as a single range read. The ECB feed is
active by default; RBA, BoE, Yahoo, OFX, OANDA, and XE are comment-switchable blocks, and every
provider package is referenced so the switch is a comment flip. *Packages: one of the
`Bodu.Financial.ExchangeRates.<Source>` provider packages.*

## Offline by default, live by choice

With the exception of `LiveRates` above, the samples never touch the network. Where a live feed
could be used, a fenced comment block shows the exact switch:

```csharp
// --- To use the live Reserve Bank of Australia feed instead -----------------
// 1. dotnet add package Bodu.Financial.ExchangeRates.Rba
// 2. Replace the offline source with:
//
//     using var rba = new RbaRateProvider(new RbaRateProviderOptions());
//     await rba.LoadRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30));
// ----------------------------------------------------------------------------
```

Because every provider in the family serves the same
<xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> /
<xref:Bodu.Financial.ExchangeRates.IRateProvider> contracts, the rest of each sample works
unchanged after the switch.
