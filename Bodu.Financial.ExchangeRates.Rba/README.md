# Bodu.Financial.ExchangeRates.Rba

> **API stability - Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **Reserve
Bank of Australia's** published historical daily exchange-rate files.

It downloads the RBA `.xls` files, parses them (via
[`Bodu.Formats.Excel.Binary`](../Bodu.Formats.Excel.Binary) →
[`Bodu.IO.Compound`](../Bodu.IO.Compound)), and serves the results as
`Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard `IDatedRateProvider`
and `IRateProvider` contracts - so it composes with `Money.ConvertTo`,
the caching and aggregating providers, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new RbaRateProvider(new RbaRateProviderOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

RateLookupResult usd = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate is the number of US dollars per Australian dollar on that date.

// Read a whole range at once (AUD-based pairs; the reverse direction is inverted). The result is
// an IReadOnlyList<ExchangeRate> that also reports the requested window and the observed span.
RateRangeResult series =
    await provider.GetRatesAsync("AUD", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (RbaSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.From}/{info.Pair.To} ({info.SeriesId})");
```

## Behaviour

- **AUD-based.** RBA quotes the Australian dollar against each currency. Direct
  (`AUD→X`) and inverse (`X→AUD`) lookups are supported; cross pairs are not.
- **Loading.** Call `PreloadAsync` / `LoadRangeAsync` to warm the in-memory store. Up to
  four eras download at once, and each is published only after the eras before it. An
  on-demand lookup loads every era its `DefaultLookback` window (seven days by default)
  reaches, so a date in the first days of an era also loads the era before it. A
  synchronous lookup that misses an unloaded era will block to download it only when
  `AllowSynchronousNetworkAccess` is enabled (it is `false` by default, so the provider
  serves a snapshot of already-loaded data and a synchronous miss does not reach the
  network).
- **Caching.** Downloaded files can be cached on disk by setting `EnableDiskCache` (off by
  default); immutable historical eras are then cached indefinitely and the open-ended current
  era refreshes on `RefreshInterval`. In memory, a closed era is loaded once, while
  a request that reaches a day the RBA had not yet published when the current era was
  loaded downloads it again once `RefreshInterval` has passed.
- **Configuration.** `RbaRateProviderOptions` derives from `WebRateProviderOptions`, so it
  shares the base address, HTTP, look-back, refresh, alias and log-level members every web
  provider's options carry, carries working defaults, and binds through
  `Microsoft.Extensions.Options`. The package ships its own `AddRbaExchangeRates`
  registration in the `Bodu.Financial.ExchangeRates` namespace.

## Options

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://www.rba.gov.au/statistics/tables/xls-hist/` | The directory the era workbooks are resolved against. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `UserAgent` | `Bodu.Financial.ExchangeRates.Rba` | The `User-Agent` header, applied as `HttpTimeout` is. |
| `MaxResponseContentBufferSize` | 64 MiB | The response-size cap on the `HttpClient` the provider creates. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand downloads from synchronous lookups. |
| `DefaultLookback` | 7 days | The window, ending on the requested date, whose eras an on-demand lookup loads. |
| `RefreshInterval` | 12 h | How long the cached current era, and a loaded day the RBA could still publish, is served before it is downloaded again. |
| `HistoryAvailability` | `Unbounded` | Left unbounded, the provider advertises the depth of `Eras`; any other value is advertised as set (set in code). |
| `Eras` | `RbaEraWorkbook.Default` | The era workbooks to draw from (set in code). |
| `CurrencyAliases` | `{ "SDR": "XDR" }` | Maps a units label in a workbook to an ISO code. |
| `EnableDiskCache` | `false` | Persist downloaded era workbooks on disk. |
| `CacheDirectory` | `null` | The cache folder; `null` uses `bodu-rba` under the system temporary path. |

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new RbaRateProvider(options, ...)` - the provider builds, owns, and disposes its own
  `HttpClient`, created via `RateProviderHttpClientFactory.Create` from the configured user agent
  and timeout. Dispose the provider (for example with `using`) to release the client.
- `new RbaRateProvider(httpClient, options, ...)` - you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the
  package's `AddRbaExchangeRates` registration uses, backed by `IHttpClientFactory`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the package's `AddRbaExchangeRates` registration wire one for you (category
`Bodu.Financial.ExchangeRates.RbaRateProvider`). When no logger is supplied it
defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

The levels follow the conventions used by `Microsoft.Extensions.Http`, EF Core, and the
Azure SDK - the completed download is the one `Information` line per fetch, payload detail
is `Trace`, and degraded paths are `Warning`. Every level is individually configurable on
`RbaRateProviderOptions`:

| Event | Default level | Option property |
|---|---|---|
| An era download is starting | `Debug` | `DownloadStartingLogLevel` |
| An era loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| An era download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |
| A synchronous lookup triggered a blocking network fetch | `Warning` | `SynchronousNetworkFetchLogLevel` |

```csharp
// Quieten the per-fetch line and turn off per-observation tracing entirely.
var options = new RbaRateProviderOptions
{
    DownloadCompletedLogLevel = LogLevel.Debug,
    ObservationIngestedLogLevel = LogLevel.None,
};
```

The default verbosity is deliberately low: at `Information` you see one line per era
loaded; at `Debug` you additionally see when downloads start; only at `Trace` do you get a
line per rate observation (which can be thousands per era - keep it for targeted
debugging).

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
