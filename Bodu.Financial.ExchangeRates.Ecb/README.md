# Bodu.Financial.ExchangeRates.Ecb

> **API stability - Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **European
Central Bank's** published euro foreign-exchange reference rates.

It downloads the ECB `eurofxref` XML feeds, parses them, and serves the results as
`Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard `IDatedRateProvider`
and `IRateProvider` contracts - so it composes with `Money.ConvertTo`,
the caching and aggregating providers, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new EcbRateProvider(new EcbRateProviderOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

RateLookupResult usd = provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate is the number of US dollars per euro on that date.

// Read a whole range at once (EUR-based pairs; the reverse direction is inverted). The result is
// an IReadOnlyList<ExchangeRate> that also reports the requested window and the observed span.
RateRangeResult series =
    await provider.GetRatesAsync("EUR", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (EcbSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.From}/{info.Pair.To}");
```

## Behaviour

- **EUR-based.** The ECB quotes the euro against each currency. Direct (`EUR→X`) and
  inverse (`X→EUR`) lookups are supported; cross pairs are not.
- **Feeds.** The ECB publishes overlapping `eurofxref` files that each end at the most
  recent business day and reach back a different distance: a rolling 90-day file and the
  full history since 1999 (a latest-day file is also available via
  `EcbRateFeed.Daily`). The provider loads the narrowest feed that covers the
  dates you ask for, minimizing bandwidth; an on-demand lookup asks for the
  `DefaultLookback` window (seven days by default) ending on the requested date.
- **Loading.** Call `PreloadAsync` / `LoadRangeAsync` to warm the in-memory store. A
  synchronous lookup that misses an unloaded date will block to download its covering feed
  only when `AllowSynchronousNetworkAccess` is enabled (it is `false` by default, so the
  provider serves a snapshot of already-loaded data and a synchronous miss does not reach
  the network).
- **Caching.** Downloaded files are cached on disk (configurable); because every feed
  extends to the latest business day, each is refreshed on a TTL. In memory, a loaded
  feed serves past days for good, while a request that reaches a day the ECB had not yet
  published when the feed was loaded downloads it again once `RefreshInterval` has passed.
- **Configuration.** `EcbRateProviderOptions` derives from `WebRateProviderOptions`, so it
  shares the base address, HTTP, look-back, refresh, alias and log-level members every web
  provider's options carry, carries working defaults, and binds through
  `Microsoft.Extensions.Options`. Point `BaseAddress` at a mirror or proxy of the ECB feeds
  without touching caching or feed selection. See the package's own `AddEcbExchangeRates`
  registration in the `Bodu.Financial.ExchangeRates` namespace.

## Options

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://www.ecb.europa.eu/stats/eurofxref/` | The directory the feed files are resolved against. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `UserAgent` | `Bodu.Financial.ExchangeRates.Ecb` | The `User-Agent` header, applied as `HttpTimeout` is. |
| `MaxResponseContentBufferSize` | 64 MiB | The response-size cap on the `HttpClient` the provider creates. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand downloads from synchronous lookups. |
| `DefaultLookback` | 7 days | The window, ending on the requested date, whose covering feed an on-demand lookup loads. |
| `RefreshInterval` | 12 h | How long a cached feed, and a loaded day the ECB could still publish, is served before it is downloaded again. |
| `HistoryAvailability` | `Unbounded` | Left unbounded, the provider advertises the depth of `Feeds`; any other value is advertised as set (set in code). |
| `Feeds` | `EcbRateFeed.Default` | The feeds to draw from, narrowest first (set in code). |
| `CurrencyAliases` | empty | Maps a currency label in a feed to an ISO code. |
| `EnableDiskCache` | `true` | Persist downloaded feed files on disk. |
| `CacheDirectory` | `null` | The cache folder; `null` uses `bodu-ecb` under the system temporary path. |

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new EcbRateProvider(options, ...)` - the provider builds, owns, and disposes its own
  `HttpClient`, created via `RateProviderHttpClientFactory.Create` from the configured user agent
  and timeout. Dispose the provider (for example with `using`) to release the client.
- `new EcbRateProvider(httpClient, options, ...)` - you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the
  package's `AddEcbExchangeRates` registration uses, backed by `IHttpClientFactory`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the package's `AddEcbExchangeRates` registration wire one for you (category
`Bodu.Financial.ExchangeRates.EcbRateProvider`). When no logger is supplied it
defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

The levels follow the conventions used by `Microsoft.Extensions.Http`, EF Core, and the
Azure SDK - the completed download is the one `Information` line per fetch, payload detail
is `Trace`, and degraded paths are `Warning`. Every level is individually configurable on
`EcbRateProviderOptions`:

| Event | Default level | Option property |
|---|---|---|
| A feed download is starting | `Debug` | `DownloadStartingLogLevel` |
| A feed loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| A feed download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |
| A synchronous lookup triggered a blocking network fetch | `Warning` | `SynchronousNetworkFetchLogLevel` |

```csharp
// Quieten the per-fetch line and turn off per-observation tracing entirely.
var options = new EcbRateProviderOptions
{
    DownloadCompletedLogLevel = LogLevel.Debug,
    ObservationIngestedLogLevel = LogLevel.None,
};
```

The default verbosity is deliberately low: at `Information` you see one line per feed
loaded; at `Debug` you additionally see when downloads start; only at `Trace` do you get a
line per rate observation (which can be thousands per feed - keep it for targeted
debugging).

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
