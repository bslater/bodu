---
uid: Bodu.Financial.ExchangeRates
---

![Bodu.Financial.ExchangeRates](~/images/hero-fx.svg)

# Bodu.Financial.ExchangeRates

## Purpose

**Bodu.Financial.ExchangeRates** is the exchange-rate namespace of the [`Bodu.Financial`](Bodu.Financial.md) family. The core FX types — <xref:Bodu.Financial.ExchangeRates.ExchangeRate> values, currency pairs, rate series and stores, and the standard <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> and timeless <xref:Bodu.Financial.ExchangeRates.IRateProvider> contracts — ship in the core `Bodu.Financial` package. The separate **`Bodu.Financial.ExchangeRates`** package layers the web/HTTP provider machinery on top (the abstract <xref:Bodu.Financial.ExchangeRates.WebRateProvider> and <xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1> bases and their supporting types), so the core package carries no HTTP machinery. The same flattened namespace then gathers the concrete providers for the public feeds — the Bank of England, the European Central Bank, the Reserve Bank of Australia, Yahoo Finance, OFX, XE.com, OANDA, Fixer (fixer.io), exchangerate.host, FRED (St. Louis Fed), and the IMF — each shipped as its own package. Every provider downloads and parses its source over a requested date window into a dated series, so they all compose with `Money.ConvertTo`, the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer, and the rest of the FX stack.

The single-base providers (BoE, ECB, RBA, IMF) publish one base currency (GBP, EUR, AUD, USD), so they support direct (`base→X`) and inverse (`X→base`) lookups but not cross pairs; IMF downloads the IMF's monthly Representative Exchange Rates tab-separated report and normalizes its quotation direction to a consistent USD base. The market providers (Yahoo, OFX, XE, OANDA, Fixer, exchangerate.host) fetch a distinct series per currency pair, so they serve **arbitrary pairs**; FRED is per-pair too but maps each pair to a source series identifier (with a built-in map for the major pairs). Fixer, exchangerate.host, and FRED require an API key on their options; IMF is keyless and daily. Every provider is `IDisposable`: the options-only constructor builds and owns its `HttpClient`, while the constructor that accepts an `HttpClient` (the form the dependency-injection registration uses) leaves the client's lifetime to the caller. Downloaded responses are cached on disk by default.

Each provider ships its own dependency-injection registration in the `Bodu.Financial.ExchangeRates` namespace, so a single `using Bodu.Financial.ExchangeRates;` makes the `Add<Source>...` extension methods available — `AddBoeExchangeRates`, `AddEcbExchangeRates`, `AddRbaExchangeRates`, `AddYahooExchangeRates`, `AddOfxExchangeRates`, `AddXeExchangeRates`, `AddOandaExchangeRates`, `AddFixerExchangeRates`, `AddExchangeRateHostExchangeRates`, `AddFredExchangeRates`, and `AddImfExchangeRates`. There is no separate per-provider `*.DependencyInjection` package; the registration lives in the provider's own runtime package over the shared `AddWebRateProvider` machinery.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — construction, warming the store or a pair, the shared lookup surface, dependency injection, and composing a provider with caching and aggregation.

## Key types

**Core exchange types (in the `Bodu.Financial` package)**

- <xref:Bodu.Financial.ExchangeRates.IRateProvider>, <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> — timeless and dated provider contracts.
- <xref:Bodu.Financial.ExchangeRates.ExchangeRate>, <xref:Bodu.Financial.ExchangeRates.CurrencyPair>, <xref:Bodu.Financial.ExchangeRates.RateObservation>, <xref:Bodu.Financial.ExchangeRates.RateSeries> — observation record, strongly-typed (from, to) key, single dated observation value, and an O(log n) read-optimised time series.
- <xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder>, <xref:Bodu.Financial.ExchangeRates.RateSeriesKey>, <xref:Bodu.Financial.ExchangeRates.RateTableBuilder> — mutable companion for building or editing a series, the (pair, provider) key, and a higher-level multi-series editor for import workflows.
- <xref:Bodu.Financial.ExchangeRates.RateLookupOptions>, <xref:Bodu.Financial.ExchangeRates.RateLookupResult>, <xref:Bodu.Financial.ExchangeRates.RateDateResolution> — resolution policy options and the audit-grade lookup result.
- <xref:Bodu.Financial.ExchangeRates.RateProvenance> — readonly-record-struct recording where a rate came from (provider, optional backend, cached-at / as-of instants), with `Live` and `FromCache` factories.
- <xref:Bodu.Financial.ExchangeRates.FixedRateTable>, <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider>, <xref:Bodu.Financial.ExchangeRates.DatedRateProviderAdapter> — in-memory provider implementations and an adapter that pins a date to a dated provider for codebases that don't need the dated surface. Grouping several providers (prioritised fallback, averaging, per-FX-pair routing) and read-through caching live in [`Bodu.Financial.ExchangeRates.Caching`](Bodu.Financial.ExchangeRates.Caching.md).
- <xref:Bodu.Financial.ExchangeRates.RateHistoryAvailability>, <xref:Bodu.Financial.ExchangeRates.RateRangeResult>, <xref:Bodu.Financial.ExchangeRates.DateRangeCoverage> — how far back a source serves rates (unbounded, fixed earliest date, or rolling window), the whole-range read result, and the coverage record the caching layer stores.
- <xref:Bodu.Financial.ExchangeRates.RateSeriesNotFoundException> (a `KeyNotFoundException`) — the missing-series failure raised by the provider stack.

**Web-provider machinery (in the `Bodu.Financial.ExchangeRates` package)**

- <xref:Bodu.Financial.ExchangeRates.WebRateProvider>, <xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions> — the abstract HTTP-backed dated-provider base that every provider here derives from (it accumulates fetched observations into an immutable book / snapshot, coalesces concurrent loads, and owns or borrows its `HttpClient`) and the abstract options carrying `BaseAddress`, `HttpTimeout`, `UserAgent`, `DefaultLookback`, `CurrencyAliases`, and per-stage log levels.
- <xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1> — the specialisation the pair-serving providers (Yahoo, OFX, XE, OANDA, Fixer, exchangerate.host, FRED) build on. IMF, like the central-bank providers, extends <xref:Bodu.Financial.ExchangeRates.WebRateProvider> directly.
- <xref:Bodu.Financial.ExchangeRates.IPairRateSource`1>, <xref:Bodu.Financial.ExchangeRates.IPairRateLoader>, <xref:Bodu.Financial.ExchangeRates.CurrencyPairRequest>, <xref:Bodu.Financial.ExchangeRates.PairRateData`1> — the pair-based fetch contracts (`GetPairAsync`), the request struct (pair + inclusive date range), and the result record (pair, observations, source-specific series metadata).
- <xref:Bodu.Financial.ExchangeRates.SingleFlightCoordinator`1> — keyed single-flight coordinator that coalesces concurrent loads of the same key onto one in-flight operation (`RunAsync` / `RunAsync<TResult>`), used internally by `WebRateProvider` to deduplicate endpoint fetches.
- <xref:Bodu.Financial.ExchangeRates.FileSystemByteCache`1> — the abstract base for the file-feed providers' on-disk raw-response caches (best-effort `TryGetCore` / `StoreCore` keyed by a download unit); a derived cache supplies only the file name and, optionally, a freshness rule. The BoE / ECB / RBA `FileSystem*Cache` implementations derive from it.
- <xref:Bodu.Financial.ExchangeRates.RateProviderHttpClientFactory> — builds the owned `HttpClient` for the options-only constructor form.
- <xref:Bodu.Financial.ExchangeRates.ExchangeRateFormatException> (a `FormatException`) — the feed-parse failure raised by the provider stack.

**Registration machinery (in the `Bodu.Financial.ExchangeRates.DependencyInjection` package)**

- <xref:Bodu.Financial.ExchangeRates.WebRateProviderExtensions> — the shared `AddWebRateProvider<TProvider, TOptions>(...)` registration machinery (named `HttpClient` plus Polly resilience) that every provider's `Add<Source>...` method delegates to, exposed in the same flattened `Bodu.Financial.ExchangeRates` namespace.

**Bank of England (GBP base; daily spot, CSV export)**

- <xref:Bodu.Financial.ExchangeRates.BoeRateProvider> — the provider; warm it with `LoadRangeAsync`, then resolve through the dated or timeless surface. Registered with `AddBoeExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.BoeRateProviderOptions>, <xref:Bodu.Financial.ExchangeRates.BoeEndpointOptions> — the endpoint, HTTP, on-demand-access, and on-disk response-cache configuration, and the series-to-endpoint map.
- <xref:Bodu.Financial.ExchangeRates.BoeSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.IByteCache`1>, <xref:Bodu.Financial.ExchangeRates.FileSystemBoeResponseCache> — the shared raw-byte cache seam and its on-disk Bank of England implementation.

**European Central Bank (EUR base; `eurofxref` XML feed)**

- <xref:Bodu.Financial.ExchangeRates.EcbRateProvider> — the provider; warm it with `LoadRangeAsync`. Registered with `AddEcbExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.EcbRateProviderOptions>, <xref:Bodu.Financial.ExchangeRates.EcbRateFeed> — the options (endpoint, HTTP, on-demand access, on-disk feed cache) and the `eurofxref` feed variant a load fetches.
- <xref:Bodu.Financial.ExchangeRates.EcbSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.IByteCache`1>, <xref:Bodu.Financial.ExchangeRates.FileSystemEcbFeedCache> — the shared raw-byte cache seam and its on-disk `eurofxref` feed implementation.

**Reserve Bank of Australia (AUD base; published `.xls` workbooks, split into eras)**

- <xref:Bodu.Financial.ExchangeRates.RbaRateProvider> — the provider; warm it with `PreloadAsync`, `LoadEraAsync`, or `LoadRangeAsync`. Registered with `AddRbaExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.RbaRateProviderOptions>, <xref:Bodu.Financial.ExchangeRates.RbaEraWorkbook> — the options (base URL, era list, HTTP, on-demand access, on-disk workbook cache) and one published workbook era.
- <xref:Bodu.Financial.ExchangeRates.RbaSeriesInfo> — a discovered currency series, surfaced by `GetAvailablePairs`.
- <xref:Bodu.Financial.ExchangeRates.IByteCache`1>, <xref:Bodu.Financial.ExchangeRates.FileSystemRbaWorkbookCache> — the shared raw-byte cache seam and its on-disk workbook implementation.

**Yahoo Finance (arbitrary pairs; chart per ticker)**

- <xref:Bodu.Financial.ExchangeRates.YahooRateProvider> — the provider; warm a pair with `LoadPairAsync`. Registered with `AddYahooExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.YahooRateProviderOptions> — the endpoint, HTTP, user-agent, and on-demand-access configuration.
- <xref:Bodu.Financial.ExchangeRates.YahooSeriesInfo> — a discovered pair and its ticker symbol, surfaced by `GetAvailablePairs`.

**OFX (arbitrary pairs; spot-rate-history JSON service)**

- <xref:Bodu.Financial.ExchangeRates.OfxRateProvider> — the provider built on the shared `PairWebRateProvider<TSeries>` base; warm a pair with `LoadPairAsync`. Registered with `AddOfxExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.OfxRateProviderOptions> — the endpoint, reporting interval, decimal precision, HTTP timeout, and user agent.
- <xref:Bodu.Financial.ExchangeRates.OfxSeriesInfo> — a discovered pair and its quote-currency ISO code, surfaced by `GetAvailablePairs`.

**XE.com (arbitrary pairs; charting-rates JSON service)**

- <xref:Bodu.Financial.ExchangeRates.XeRateProvider> — the provider built on the shared `PairWebRateProvider<TSeries>` base; warm a pair with `LoadPairAsync`. Registered with `AddXeExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.XeRateProviderOptions> — the endpoint, HTTP, and token-acquisition configuration.
- `IXeAuthTokenProvider`, `XeScrapingAuthTokenProvider` (internal) — the authorization-token seam and the default implementation that acquires XE's `Basic` credential automatically.
- <xref:Bodu.Financial.ExchangeRates.XeSeriesInfo> — a discovered pair, surfaced by `GetAvailablePairs`.

**OANDA (arbitrary pairs; anonymous rolling ~180-day history window)**

- <xref:Bodu.Financial.ExchangeRates.OandaRateProvider> — the provider built on the shared `PairWebRateProvider<TSeries>` base; advertises its rolling window through `WebRateProvider.HistoryAvailability`. Registered with `AddOandaExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.OandaRateProviderOptions> — the Historical Currency Converter endpoint, HTTP, and window configuration.
- <xref:Bodu.Financial.ExchangeRates.OandaSeriesInfo> — a discovered pair, surfaced by `GetAvailablePairs`.

**Fixer (fixer.io; arbitrary pairs; time-series / single-date JSON, API key)**

- <xref:Bodu.Financial.ExchangeRates.FixerRateProvider> — the provider built on the shared `PairWebRateProvider<TSeries>` base; warm a pair with `LoadPairAsync`. Registered with `AddFixerExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.FixerRateProviderOptions> — the endpoint, `ApiKey` (`access_key`), time-series/single-date paths, HTTP, and on-demand-access configuration.
- <xref:Bodu.Financial.ExchangeRates.FixerSeriesInfo> — a discovered pair with its base and quote currencies, surfaced by `GetAvailablePairs`.

**exchangerate.host (arbitrary pairs; time-series / single-date JSON, API key)**

- <xref:Bodu.Financial.ExchangeRates.ExchangeRateHostRateProvider> — the provider built on the shared `PairWebRateProvider<TSeries>` base; warm a pair with `LoadPairAsync`. Registered with `AddExchangeRateHostExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.ExchangeRateHostRateProviderOptions> — the endpoint, `ApiKey` (`access_key`), source/currencies paths, HTTP, and on-demand-access configuration.
- <xref:Bodu.Financial.ExchangeRates.ExchangeRateHostSeriesInfo> — a discovered pair with its source and quote currencies, surfaced by `GetAvailablePairs`.

**FRED (St. Louis Fed; mapped pairs; `series/observations` JSON, API key)**

- <xref:Bodu.Financial.ExchangeRates.FredRateProvider> — the provider built on the shared `PairWebRateProvider<TSeries>` base; warm a pair with `LoadPairAsync`. Registered with `AddFredExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.FredRateProviderOptions> — the endpoint, `ApiKey` (`api_key`), and the `SeriesMap` mapping each pair to a FRED `series_id` (built-in map for the major USD pairs).
- <xref:Bodu.Financial.ExchangeRates.FredSeriesInfo> — a discovered pair and its FRED series identifier, surfaced by `GetAvailablePairs`.

**IMF (base USD; monthly Representative Exchange Rates TSV report; keyless, daily)**

- <xref:Bodu.Financial.ExchangeRates.ImfRateProvider> — the single-base (USD) provider built on the shared `WebRateProvider` base; warm the store with `LoadRangeAsync`. Registered with `AddImfExchangeRates`.
- <xref:Bodu.Financial.ExchangeRates.ImfRateProviderOptions> — the report endpoint (`ReportPath`/`ReportType`), the on-disk month cache settings, and the `CurrencyNames` map from IMF currency label to ISO 4217 code.
- <xref:Bodu.Financial.ExchangeRates.ImfSeriesInfo> — a discovered currency series (always quoted against USD), surfaced by `GetAvailablePairs`.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;

using var ecb = new EcbRateProvider(new EcbRateProviderOptions());
await ecb.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

RateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

To register a provider in the container, add `using Bodu.Financial.ExchangeRates;` and call the source's `Add<Source>...` method — for example `services.AddEcbExchangeRates();`. See the [providers guide](~/guides/financial/exchange-rate-providers.md) for construction, warming, dependency injection, and composing with the [caching and aggregating](~/guides/financial/exchange-rate-caching.md) layer.
