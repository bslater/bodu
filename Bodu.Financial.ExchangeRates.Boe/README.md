# Bodu.Financial.ExchangeRates.Boe

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **Bank of
England's** daily spot exchange rates, queried from the Bank's Interactive Statistical
Database (IADB).

It builds an IADB CSV query for the configured series over a date range, parses the
response with the `Bodu.Text.Delimited` RFC 4180 reader, and serves the results as
`Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard `IDatedRateProvider`
and `IRateProvider` contracts - so it composes with `Money.ConvertTo`,
the caching and aggregating providers, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new BoeRateProvider(new BoeRateProviderOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

RateLookupResult usd = provider.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate is the number of US dollars per pound on that date.

// Read a whole range at once (GBP-based pairs; the reverse direction is inverted). The result is
// an IReadOnlyList<ExchangeRate> that also reports the requested window and the observed span.
RateRangeResult series =
    await provider.GetRatesAsync("GBP", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (BoeSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.From}/{info.Pair.To} ({info.SeriesCode})");
```

## Behaviour

- **GBP-based.** The Bank of England quotes each currency's daily spot rate against the
  pound (one IADB series per currency, for example `XUDLUSS` for the US dollar). Direct
  (`GBP→X`) and inverse (`X→GBP`) lookups are supported; cross pairs are not.
- **Range queries.** The IADB is queried by date range rather than by fixed file, so
  loading is range-based. `LoadRangeAsync` fetches an inclusive range; a synchronous lookup
  that misses an unloaded date blocks to download a bounded look-back window ending on it
  (`DefaultLookback`, ten days by default) only when `AllowSynchronousNetworkAccess` is enabled (it is
  `false` by default, so the provider serves a snapshot of already-loaded data and a
  synchronous miss does not reach the network). Only the part of a range not already loaded,
  from its first missing day to its last, is downloaded, so a look-back window that slides
  forward one day downloads that day alone.
- **Caching.** Downloaded range responses are cached on disk (configurable) and refreshed
  on a TTL, since a range ending near today can gain an observation each business day. A
  response for a range that had ended before the day it was downloaded never expires.
  In memory, a loaded range keeps its past days for good, while today (and any later day
  it returned no rate for) is downloaded again by a request that reaches it once
  `RefreshInterval` has passed, so a rate published after the first download is picked up.
- **Configuration.** `BoeRateProviderOptions` derives from `WebRateProviderOptions`, so it
  shares the base address, HTTP, look-back, refresh, alias and log-level members every web
  provider's options carry, carries working defaults, and binds through
  `Microsoft.Extensions.Options`. Point `BaseAddress` (with `QueryPath`) at a mirror or proxy
  of the IADB without touching caching or series configuration. The package ships its own
  `AddBoeExchangeRates` registration in the `Bodu.Financial.ExchangeRates` namespace.

## Options

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://www.bankofengland.co.uk/boeapps/database/` | The IADB host. |
| `QueryPath` | `_iadb-fromshowcolumns.asp` | The CSV column query, resolved against `BaseAddress`. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `UserAgent` | `Bodu.Financial.ExchangeRates.Boe` | The `User-Agent` header, applied as `HttpTimeout` is. |
| `MaxResponseContentBufferSize` | 64 MiB | The response-size cap on the `HttpClient` the provider creates. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand downloads from synchronous lookups. |
| `DefaultLookback` | 10 days | The window, ending on the requested date, an on-demand lookup downloads. |
| `RefreshInterval` | 12 h | How long a cached response, and a loaded day the bank could still publish, is served before it is downloaded again. |
| `HistoryAvailability` | since 2 January 1975 | The history depth the provider advertises (set in code). |
| `Series` | `BoeSeries.Default` | The currency series to request (set in code). |
| `CurrencyAliases` | empty | Maps a series' quote label to the ISO code it is served under. |
| `EnableDiskCache` | `true` | Persist downloaded range responses on disk. |
| `CacheDirectory` | `null` | The cache folder; `null` uses `bodu-boe` under the system temporary path. |

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new BoeRateProvider(options, ...)` - the provider builds, owns, and disposes its own
  `HttpClient`, created via `RateProviderHttpClientFactory.Create` from the configured user agent
  and timeout. Dispose the provider (for example with `using`) to release the client.
- `new BoeRateProvider(httpClient, options, ...)` - you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the
  package's `AddBoeExchangeRates` registration uses, backed by `IHttpClientFactory`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the package's `AddBoeExchangeRates` registration wire one for you (category
`Bodu.Financial.ExchangeRates.BoeRateProvider`). When no logger is supplied it
defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

The levels follow the conventions used by `Microsoft.Extensions.Http`, EF Core, and the
Azure SDK - the completed download is the one `Information` line per range loaded, payload
detail is `Trace`, and degraded paths are `Warning`. Every level is individually
configurable on `BoeRateProviderOptions`:

| Event | Default level | Option property |
|---|---|---|
| A range download is starting | `Debug` | `DownloadStartingLogLevel` |
| A range loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| A range download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |
| A synchronous lookup triggered a blocking network fetch | `Warning` | `SynchronousNetworkFetchLogLevel` |

```csharp
// Quieten the per-range line and turn off per-observation tracing entirely.
var options = new BoeRateProviderOptions
{
    DownloadCompletedLogLevel = LogLevel.Debug,
    ObservationIngestedLogLevel = LogLevel.None,
};
```

The default verbosity is deliberately low: at `Information` you see one line per range
loaded; at `Debug` you additionally see when downloads start; only at `Trace` do you get a
line per rate observation (which can be thousands per range - keep it for targeted
debugging).

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
