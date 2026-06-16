---
uid: Bodu.Financial.ExchangeRates.Yahoo
---

# Bodu.Financial.ExchangeRates.Yahoo

## Purpose

**Bodu.Financial.ExchangeRates.Yahoo** serves Yahoo Finance exchange rates as <xref:Bodu.Financial.ExchangeRate> values through the standard <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> contracts. It fetches a Yahoo Finance chart per currency pair (the ticker `AUDUSD=X` for AUD/USD) and parses it into a dated series — so it composes with `Money.ConvertTo`, the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer, and the rest of the FX stack. Unlike the central-bank providers it fetches a distinct ticker per direction, so it serves **arbitrary pairs** rather than a single base currency.

The provider is `IDisposable`: the options-only constructor builds and owns its `HttpClient`, while the constructor that accepts an `HttpClient` (the form the dependency-injection package uses) leaves the client's lifetime to the caller.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — construction, warming a pair, the shared lookup surface, and composing a provider with caching and aggregation.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Yahoo.YahooExchangeRateProvider> — the provider; warm a pair with `LoadPairAsync`, then resolve through the dated or timeless surface.
- <xref:Bodu.Financial.ExchangeRates.Yahoo.YahooExchangeRateOptions> — the endpoint configuration, the HTTP timeout, the user agent, and on-demand synchronous access.
- <xref:Bodu.Financial.ExchangeRates.Yahoo.YahooSeriesInfo> — a discovered pair and its ticker symbol, surfaced by `GetAvailablePairs`.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Yahoo;

using var yahoo = new YahooExchangeRateProvider(new YahooExchangeRateOptions());
await yahoo.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

ExchangeRateLookupResult aud = yahoo.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
```

For dependency-injection wiring, see [`Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection`](Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection.md) and the [providers guide](~/guides/financial/exchange-rate-providers.md).
