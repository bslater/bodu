---
uid: Bodu.Financial.ExchangeRates
---

# Bodu.Financial.ExchangeRates

## Purpose

**Bodu.Financial.ExchangeRates** is the built-in exchange-rate provider namespace for the [`Bodu.Financial`](Bodu.Financial.md) FX stack. It gathers the concrete providers for the public feeds — the Bank of England, the European Central Bank, the Reserve Bank of Australia, Yahoo Finance, and OFX — each serving rates as <xref:Bodu.Financial.ExchangeRate> values through the standard <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> contracts. Every provider downloads and parses its source over a requested date window into a dated series, so they all compose with `Money.ConvertTo`, the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer, and the rest of the FX stack.

The central-bank providers (BoE, ECB, RBA) publish a single base currency (GBP, EUR, AUD), so they support direct (`base→X`) and inverse (`X→base`) lookups but not cross pairs. The market providers (Yahoo, OFX) fetch a distinct series per currency pair, so they serve **arbitrary pairs**. Every provider is `IDisposable`: the options-only constructor builds and owns its `HttpClient`, while the constructor that accepts an `HttpClient` (the form the dependency-injection registration uses) leaves the client's lifetime to the caller. Downloaded responses are cached on disk by default.

Each provider ships its own dependency-injection registration in the `Microsoft.Extensions.DependencyInjection` namespace, so a single `using Microsoft.Extensions.DependencyInjection;` makes the `Add<Source>...` extension methods available — `AddBoeReferenceRates`, `AddEcbReferenceRates`, `AddRbaHistoricalRates`, `AddYahooExchangeRates`, and `AddOfxExchangeRates`. There is no separate per-provider `*.DependencyInjection` package; the registration lives in the provider's own runtime package over the shared `AddWebExchangeRateProvider` machinery.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — construction, warming the store or a pair, the shared lookup surface, dependency injection, and composing a provider with caching and aggregation.

## Key types

**Bank of England (GBP base; daily spot, CSV export)**

- <xref:Bodu.Financial.ExchangeRates.BoeExchangeRateProvider> — the provider; warm it with `LoadRangeAsync`, then resolve through the dated or timeless surface. Registered with `AddBoeReferenceRates`.
- <xref:Bodu.Financial.ExchangeRates.BoeExchangeRateOptions>, <xref:Bodu.Financial.ExchangeRates.BoeEndpointOptions> — the endpoint, HTTP, on-demand-access, and on-disk response-cache configuration, and the series-to-endpoint map.
- <xref:Bodu.Financial.ExchangeRates.BoeSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.IBoeResponseCache>, <xref:Bodu.Financial.ExchangeRates.FileSystemBoeResponseCache> — the raw-response cache seam and its on-disk implementation.

**European Central Bank (EUR base; `eurofxref` XML feed)**

- <xref:Bodu.Financial.ExchangeRates.EcbExchangeRateProvider> — the provider; warm it with `LoadRangeAsync`. Registered with `AddEcbReferenceRates`.
- <xref:Bodu.Financial.ExchangeRates.EcbExchangeRateOptions>, <xref:Bodu.Financial.ExchangeRates.EcbExchangeRateFeed> — the options (endpoint, HTTP, on-demand access, on-disk feed cache) and the `eurofxref` feed variant a load fetches.
- <xref:Bodu.Financial.ExchangeRates.EcbSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.IEcbFeedCache>, <xref:Bodu.Financial.ExchangeRates.FileSystemEcbFeedCache> — the raw-feed cache seam and its on-disk implementation.

**Reserve Bank of Australia (AUD base; published `.xls` workbooks, split into eras)**

- <xref:Bodu.Financial.ExchangeRates.RbaExchangeRateProvider> — the provider; warm it with `PreloadAsync`, `LoadEraAsync`, or `LoadRangeAsync`. Registered with `AddRbaHistoricalRates`.
- <xref:Bodu.Financial.ExchangeRates.RbaExchangeRateOptions>, <xref:Bodu.Financial.ExchangeRates.RbaEra> — the options (base URL, era list, HTTP, on-demand access, on-disk workbook cache) and one published workbook era.
- <xref:Bodu.Financial.ExchangeRates.RbaSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.IRbaWorkbookCache>, <xref:Bodu.Financial.ExchangeRates.FileSystemRbaWorkbookCache> — the raw-workbook byte-cache seam and its on-disk implementation.

**Yahoo Finance (arbitrary pairs; chart per ticker)**

- <xref:Bodu.Financial.ExchangeRates.YahooExchangeRateProvider> — the provider; warm a pair with `LoadPairAsync`. Registered with `AddYahooExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.YahooExchangeRateOptions> — the endpoint, HTTP, user-agent, and on-demand-access configuration.
- <xref:Bodu.Financial.ExchangeRates.YahooSeriesInfo> — a discovered pair and its ticker symbol, surfaced by `GetAvailablePairs`.

**OFX (arbitrary pairs; spot-rate-history JSON service)**

- <xref:Bodu.Financial.ExchangeRates.OfxExchangeRateProvider> — the provider built on the shared `PairWebExchangeRateProvider<TSeries>` base; warm a pair with `LoadPairAsync`. Registered with `AddOfxExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.OfxExchangeRateOptions> — the endpoint, reporting interval, decimal precision, HTTP timeout, and user agent.
- <xref:Bodu.Financial.ExchangeRates.OfxSeriesInfo> — a discovered pair and its quote-currency ISO code, surfaced by `GetAvailablePairs`.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;

using var ecb = new EcbExchangeRateProvider(new EcbExchangeRateOptions());
await ecb.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

ExchangeRateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

To register a provider in the container, add `using Microsoft.Extensions.DependencyInjection;` and call the source's `Add<Source>...` method — for example `services.AddEcbReferenceRates();`. See the [providers guide](~/guides/financial/exchange-rate-providers.md) for construction, warming, dependency injection, and composing with the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer.
