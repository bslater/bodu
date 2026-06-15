# Bodu.Financial.ExchangeRates.Yahoo

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

var provider = new YahooExchangeRateProvider(httpClient, new YahooExchangeRateOptions());

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

// Read a whole range at once.
IReadOnlyList<ExchangeRate> series =
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
  when `AllowSynchronousNetworkAccess` is enabled (the default).
- **Caching.** When `EnableDiskCache` is on (the default), fetched rates are persisted
  in a provider- and pair-keyed format where each dated rate carries its retrieval
  time. A cached rate is served until `CacheExpiry` elapses after retrieval, after which
  the provider re-fetches.

## Endpoint configuration

`YahooExchangeRateOptions` is centred on configuring the REST endpoint:

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://query1.finance.yahoo.com/` | The API host. |
| `ChartPath` | `v8/finance/chart/{symbol}` | The chart path template (`{symbol}` placeholder). |
| `SymbolFormat` | `{from}{to}=X` | The FX ticker template (`{from}` / `{to}` placeholders). |
| `UserAgent` | browser-like | Yahoo rejects requests without a recognizable user agent. |
| `HttpTimeout` | 30 s | Applied to the configured `HttpClient` by the DI registration. |
| `AllowSynchronousNetworkAccess` | `true` | Allow blocking on-demand fetches from synchronous lookups. |
| `DefaultLookback` | 7 days | The window fetched around a date for on-demand and latest-rate lookups. |
| `EnableDiskCache` | `true` | Persist fetched rates to disk in the pair/provider-keyed cache format. |
| `CacheDirectory` | temp `bodu-yahoo` | Where cached rate files are written. |
| `CacheExpiry` | 24 hours | How long a cached rate stays fresh after its retrieval time. |
| `CurrencyAliases` | empty | Maps ISO codes to Yahoo symbol components where they differ. |

## Dependency injection

See [`Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection`](../Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection)
for `AddYahooExchangeRates` / `AddBoduYahooExchangeRates`.

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
