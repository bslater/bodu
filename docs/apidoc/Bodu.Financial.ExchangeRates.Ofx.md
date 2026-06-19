---
uid: Bodu.Financial.ExchangeRates.Ofx
---

# Bodu.Financial.ExchangeRates.Ofx

## Purpose

**Bodu.Financial.ExchangeRates.Ofx** serves OFX (ofx.com) exchange rates as <xref:Bodu.Financial.ExchangeRate> values through the standard <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> contracts. It fetches the OFX public spot-rate-history JSON service per currency pair and parses it into a dated series — so it composes with `Money.ConvertTo`, the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer, and the rest of the FX stack. Like the Yahoo provider, and unlike the central-bank providers, it fetches a distinct series per pair, so it serves **arbitrary pairs** rather than a single base currency.

The provider is built on the shared `PairWebExchangeRateProvider<TSeries>` base, which supplies per-pair coverage tracking, single-flight coalescing, and the fetch-and-accumulate orchestration common to every pair-based web source. It is `IDisposable`: the options-only constructor builds and owns its `HttpClient`, while the constructor that accepts an `HttpClient` (the form the dependency-injection package uses) leaves the client's lifetime to the caller.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — construction, warming a pair, the shared lookup surface, and composing a provider with caching and aggregation.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Ofx.OfxExchangeRateProvider> — the provider; warm a pair with `LoadPairAsync`, then resolve through the dated or timeless surface.
- <xref:Bodu.Financial.ExchangeRates.Ofx.OfxExchangeRateOptions> — the endpoint configuration, the reporting interval, decimal precision, the HTTP timeout, and the user agent.
- <xref:Bodu.Financial.ExchangeRates.Ofx.OfxSeriesInfo> — a discovered pair and its quote-currency ISO code, surfaced by `GetAvailablePairs`.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Ofx;

using var ofx = new OfxExchangeRateProvider(new OfxExchangeRateOptions());
await ofx.LoadPairAsync("USD", "AUD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

ExchangeRateLookupResult aud = ofx.GetRate("USD", "AUD", new DateOnly(2023, 1, 3));
```

For dependency-injection wiring, see [`Bodu.Financial.ExchangeRates.Ofx.DependencyInjection`](Bodu.Financial.ExchangeRates.Ofx.DependencyInjection.md) and the [providers guide](~/guides/financial/exchange-rate-providers.md).
