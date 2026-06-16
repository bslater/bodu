---
uid: Bodu.Financial.ExchangeRates.Boe
---

# Bodu.Financial.ExchangeRates.Boe

## Purpose

**Bodu.Financial.ExchangeRates.Boe** serves the Bank of England daily spot exchange rates as <xref:Bodu.Financial.ExchangeRate> values through the standard <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> contracts. It downloads and parses the Bank's CSV export over a requested date **window** into GBP-based series — so it composes with `Money.ConvertTo`, the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer, and the rest of the FX stack. Direct (`GBP→X`) and inverse (`X→GBP`) lookups are supported; cross pairs are not.

The provider is `IDisposable`: the options-only constructor builds and owns its `HttpClient`, while the constructor that accepts an `HttpClient` (the form the dependency-injection package uses) leaves the client's lifetime to the caller. Downloaded responses are cached on disk by default, and a synchronous miss loads a window around the requested date.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — construction, warming the store, the shared lookup surface, and composing a provider with caching and aggregation.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Boe.BoeExchangeRateProvider> — the provider; warm it with `LoadRangeAsync`, then resolve through the dated or timeless surface.
- <xref:Bodu.Financial.ExchangeRates.Boe.BoeExchangeRateOptions> — the endpoint configuration, the HTTP timeout, the user agent, on-demand synchronous access, and the on-disk response cache.
- <xref:Bodu.Financial.ExchangeRates.Boe.BoeEndpointOptions> — the series-to-endpoint configuration the provider downloads from.
- <xref:Bodu.Financial.ExchangeRates.Boe.BoeSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.Boe.IBoeResponseCache> — the raw-response cache seam; <xref:Bodu.Financial.ExchangeRates.Boe.FileSystemBoeResponseCache> is the on-disk implementation.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Boe;

using var boe = new BoeExchangeRateProvider(new BoeExchangeRateOptions());
await boe.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

ExchangeRateLookupResult gbp = boe.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));
```

For dependency-injection wiring, see [`Bodu.Financial.ExchangeRates.Boe.DependencyInjection`](Bodu.Financial.ExchangeRates.Boe.DependencyInjection.md) and the [providers guide](~/guides/financial/exchange-rate-providers.md).
