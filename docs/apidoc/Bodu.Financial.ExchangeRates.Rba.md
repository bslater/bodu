---
uid: Bodu.Financial.ExchangeRates.Rba
---

# Bodu.Financial.ExchangeRates.Rba

## Purpose

**Bodu.Financial.ExchangeRates.Rba** serves Reserve Bank of Australia historical daily exchange rates as <xref:Bodu.Financial.ExchangeRate> values through the standard <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> contracts. It downloads the RBA's published `.xls` workbooks, splits history into **eras** (one workbook per span of dates), and parses them into AUD-based series — so it composes with `Money.ConvertTo`, the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer, and the rest of the FX stack. Direct (`AUD→X`) and inverse (`X→AUD`) lookups are supported; cross pairs are not.

The provider is `IDisposable`: the options-only constructor builds and owns its `HttpClient`, while the constructor that accepts an `HttpClient` (the form the dependency-injection package uses) leaves the client's lifetime to the caller. Downloaded workbooks are cached on disk by default, so immutable eras are not re-fetched.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — construction, warming the store, the shared lookup surface, and composing a provider with caching and aggregation.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Rba.RbaExchangeRateProvider> — the provider; warm it with `PreloadAsync`, `LoadEraAsync`, or `LoadRangeAsync`, then resolve through the dated or timeless surface.
- <xref:Bodu.Financial.ExchangeRates.Rba.RbaExchangeRateOptions> — the base URL, the era list, the HTTP timeout, the user agent, on-demand synchronous access, and the on-disk workbook cache.
- <xref:Bodu.Financial.ExchangeRates.Rba.RbaEra> — one published workbook era: a date span and its source file.
- <xref:Bodu.Financial.ExchangeRates.Rba.RbaSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.Rba.IRbaWorkbookCache> — the raw-workbook byte-cache seam; <xref:Bodu.Financial.ExchangeRates.Rba.FileSystemRbaWorkbookCache> is the on-disk implementation.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Rba;

// The provider owns the HttpClient it builds from the options; dispose it to release the client.
using var rba = new RbaExchangeRateProvider(new RbaExchangeRateOptions());
await rba.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult aud = rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
```

For dependency-injection wiring, see [`Bodu.Financial.ExchangeRates.Rba.DependencyInjection`](Bodu.Financial.ExchangeRates.Rba.DependencyInjection.md) and the [providers guide](~/guides/financial/exchange-rate-providers.md).
