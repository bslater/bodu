# Bodu.Financial.ExchangeRates.Yahoo

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **Yahoo
Finance** chart REST service.

It fetches the Yahoo Finance `v8/finance/chart/{symbol}` endpoint, parses the JSON
response, and serves the results as `Bodu.Financial.ExchangeRate` values through the
standard `IDatedExchangeRateProvider` and `IExchangeRateProvider` contracts — so it
composes with `Money.ConvertTo`, the caching and aggregating providers, and the rest of
the Bodu.Financial FX stack. It is a logical sister to
[`Bodu.Financial.ExchangeRates.Rba`](../Bodu.Financial.ExchangeRates.Rba): the same
interfaces and DI shape, a different data source.

```csharp
using Bodu.Financial.ExchangeRates.Yahoo;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new YahooExchangeRateProvider(new YahooExchangeRateOptions());

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

// Read a whole range at once. The result is an IReadOnlyList<ExchangeRate> that also reports
// the requested window (RequestedStartDate/RequestedEndDate) and the observed span.
ExchangeRateRangeResult series =
    await provider.GetRatesAsync("AUD", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// The latest available spot rate.
decimal latest = provider.GetRate("EUR", "GBP");
```

## Behaviour

- **Arbitrary pairs.** Yahoo serves any pair through the `{FROM}{TO}=X` ticker
  convention (for example, `AUDUSD=X`, `EURGBP=X`), so there is no base-currency
  restriction. The inverse direction is served directly or, if only the reverse series
  is loaded, inverted.
- **Daily bars.** The chart interval is fixed at one day; the date range is supplied
  per call through `LoadPairAsync` / `GetRatesAsync`.
- **Loading.** Call `LoadPairAsync` to warm the in-memory store. A synchronous lookup
  that misses an un-fetched pair will block to fetch a window around the requested date
  only when `AllowSynchronousNetworkAccess` is enabled (it is `false` by default, so the
  provider serves a snapshot of already-loaded data and a synchronous miss does not reach
  the network).
- **No provider-local disk cache.** The Yahoo provider fetches over HTTP and keeps only an
  in-memory store of the pairs and windows it has fetched this session; it does not persist
  anything to disk. For durable caching across processes, compose it with the generic
  caching provider — `AddCachedExchangeRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package
  — rather than a provider-local cache.

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new YahooExchangeRateProvider(options, ...)` — the provider builds, owns, and disposes its
  own `HttpClient`, created via `ExchangeRateHttpClientFactory.Create` from the configured user
  agent and timeout. Dispose the provider (for example with `using`) to release the client.
- `new YahooExchangeRateProvider(httpClient, options, ...)` — you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the
  `*.DependencyInjection` package uses, backed by `IHttpClientFactory`.

## Endpoint configuration

`YahooExchangeRateOptions` is centred on configuring the REST endpoint:

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://query1.finance.yahoo.com/` | The API host. |
| `ChartPath` | `v8/finance/chart/{symbol}` | The chart path template (`{symbol}` placeholder). |
| `SymbolFormat` | `{from}{to}=X` | The FX ticker template (`{from}` / `{to}` placeholders). |
| `UserAgent` | browser-like | Yahoo rejects requests without a recognizable user agent. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates from these options, or by the DI registration when you supply your own client. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand fetches from synchronous lookups. |
| `DefaultLookback` | 7 days | The window fetched around a date for on-demand and latest-rate lookups. |
| `CurrencyAliases` | empty | Maps ISO codes to Yahoo symbol components where they differ. |

The Yahoo provider has no `EnableDiskCache` / `CacheDirectory` / `CacheExpiry` options: it
fetches over HTTP with no provider-local disk cache. Use the generic
[`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package
(`AddCachedExchangeRateProvider<…>`) when you need caching.

## Dependency injection

The package ships its own `AddYahooExchangeRates` registration in the
`Bodu.Financial.ExchangeRates` namespace — there is no separate `*.DependencyInjection`
package.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the `*.DependencyInjection` package wire one for you (category
`Bodu.Financial.ExchangeRates.Yahoo.YahooExchangeRateProvider`). When no logger is supplied
it defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

The levels follow the conventions used by `Microsoft.Extensions.Http`, EF Core, and the
Azure SDK — the completed download is the one `Information` line per fetch, payload detail
is `Trace`, and degraded paths are `Warning`. Every level is individually configurable on
`YahooExchangeRateOptions`:

| Event | Default level | Option property |
|---|---|---|
| A pair/chart download is starting | `Debug` | `DownloadStartingLogLevel` |
| A pair/chart loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| A pair/chart download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |
| A synchronous lookup triggered a blocking network fetch | `Warning` | `SynchronousNetworkFetchLogLevel` |

```csharp
// Quieten the per-fetch line and turn off per-observation tracing entirely.
var options = new YahooExchangeRateOptions
{
    DownloadCompletedLogLevel = LogLevel.Debug,
    ObservationIngestedLogLevel = LogLevel.None,
};
```

The default verbosity is deliberately low: at `Information` you see one line per pair/chart
loaded; at `Debug` you additionally see when downloads start; only at `Trace` do you get a
line per rate observation (which can be hundreds per chart — keep it for targeted
debugging).

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
