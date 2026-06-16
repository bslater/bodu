---
uid: Bodu.Financial.ExchangeRates.Ecb
---

# Bodu.Financial.ExchangeRates.Ecb

## Purpose

**Bodu.Financial.ExchangeRates.Ecb** serves the European Central Bank euro foreign-exchange reference rates as <xref:Bodu.Financial.ExchangeRate> values through the standard <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> contracts. It downloads and parses the ECB's `eurofxref` XML **feed**, which carries the full published history, into EUR-based series — so it composes with `Money.ConvertTo`, the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer, and the rest of the FX stack. Direct (`EUR→X`) and inverse (`X→EUR`) lookups are supported; cross pairs are not.

The provider is `IDisposable`: the options-only constructor builds and owns its `HttpClient`, while the constructor that accepts an `HttpClient` (the form the dependency-injection package uses) leaves the client's lifetime to the caller. The downloaded feed is cached on disk by default.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — construction, warming the store, the shared lookup surface, and composing a provider with caching and aggregation.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateProvider> — the provider; warm it with `LoadRangeAsync`, then resolve through the dated or timeless surface.
- <xref:Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateOptions> — the endpoint configuration, the HTTP timeout, the user agent, on-demand synchronous access, and the on-disk feed cache.
- <xref:Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateFeed> — identifies the `eurofxref` feed variant a load fetches.
- <xref:Bodu.Financial.ExchangeRates.Ecb.EcbSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.Ecb.IEcbFeedCache> — the raw-feed cache seam; <xref:Bodu.Financial.ExchangeRates.Ecb.FileSystemEcbFeedCache> is the on-disk implementation.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Ecb;

using var ecb = new EcbExchangeRateProvider(new EcbExchangeRateOptions());
await ecb.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

ExchangeRateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

For dependency-injection wiring, see [`Bodu.Financial.ExchangeRates.Ecb.DependencyInjection`](Bodu.Financial.ExchangeRates.Ecb.DependencyInjection.md) and the [providers guide](~/guides/financial/exchange-rate-providers.md).
