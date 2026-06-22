# Bodu.Financial.ExchangeRates.Ecb

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **European
Central Bank's** published euro foreign-exchange reference rates.

It downloads the ECB `eurofxref` XML feeds, parses them, and serves the results as
`Bodu.Financial.ExchangeRate` values through the standard `IDatedExchangeRateProvider`
and `IExchangeRateProvider` contracts — so it composes with `Money.ConvertTo`,
the caching and aggregating providers, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates.Ecb;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new EcbExchangeRateProvider(new EcbExchangeRateOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate is the number of US dollars per euro on that date.

// Read a whole range at once (EUR-based pairs; the reverse direction is inverted). The result is
// an IReadOnlyList<ExchangeRate> that also reports the requested window and the observed span.
ExchangeRateRangeResult series =
    await provider.GetRatesAsync("EUR", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (EcbSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.FromIsoCode}/{info.Pair.ToIsoCode}");
```

## Behaviour

- **EUR-based.** The ECB quotes the euro against each currency. Direct (`EUR→X`) and
  inverse (`X→EUR`) lookups are supported; cross pairs are not.
- **Feeds.** The ECB publishes overlapping `eurofxref` files that each end at the most
  recent business day and reach back a different distance: a rolling 90-day file and the
  full history since 1999 (a latest-day file is also available via
  `EcbExchangeRateFeed.Daily`). The provider loads the narrowest feed that covers the
  dates you ask for, minimizing bandwidth.
- **Loading.** Call `PreloadAsync` / `LoadRangeAsync` to warm the in-memory store. A
  synchronous lookup that misses an unloaded date will block to download its covering feed
  only when `AllowSynchronousNetworkAccess` is enabled (it is `false` by default, so the
  provider serves a snapshot of already-loaded data and a synchronous miss does not reach
  the network).
- **Caching.** Downloaded files are cached on disk (configurable); because every feed
  extends to the latest business day, each is refreshed on a TTL.
- **Configuration.** `EcbExchangeRateOptions` carries working defaults and binds through
  `Microsoft.Extensions.Options`. The provider's connection to the ECB is grouped under
  its `Endpoint` (`EcbEndpointOptions`) — base URL, HTTP timeout, and user-agent — so the
  feeds can be pointed at a mirror or proxy without touching caching or feed selection. See
  the package's own `AddEcbReferenceRates` registration in the `Bodu.Financial.ExchangeRates` namespace.

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new EcbExchangeRateProvider(options, ...)` — the provider builds, owns, and disposes its own
  `HttpClient`, created via `ExchangeRateHttpClientFactory.Create` from the configured user agent
  and timeout. Dispose the provider (for example with `using`) to release the client.
- `new EcbExchangeRateProvider(httpClient, options, ...)` — you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the
  `*.DependencyInjection` package uses, backed by `IHttpClientFactory`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the `*.DependencyInjection` package wire one for you (category
`Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateProvider`). When no logger is supplied it
defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

The levels follow the conventions used by `Microsoft.Extensions.Http`, EF Core, and the
Azure SDK — the completed download is the one `Information` line per fetch, payload detail
is `Trace`, and degraded paths are `Warning`. Every level is individually configurable on
`EcbExchangeRateOptions`:

| Event | Default level | Option property |
|---|---|---|
| A feed download is starting | `Debug` | `DownloadStartingLogLevel` |
| A feed loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| A feed download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |
| A synchronous lookup triggered a blocking network fetch | `Warning` | `SynchronousNetworkFetchLogLevel` |

```csharp
// Quieten the per-fetch line and turn off per-observation tracing entirely.
var options = new EcbExchangeRateOptions
{
    DownloadCompletedLogLevel = LogLevel.Debug,
    ObservationIngestedLogLevel = LogLevel.None,
};
```

The default verbosity is deliberately low: at `Information` you see one line per feed
loaded; at `Debug` you additionally see when downloads start; only at `Trace` do you get a
line per rate observation (which can be thousands per feed — keep it for targeted
debugging).

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
