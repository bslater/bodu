# Bodu.Financial.ExchangeRates.Boe

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **Bank of
England's** daily spot exchange rates, queried from the Bank's Interactive Statistical
Database (IADB).

It builds an IADB CSV query for the configured series over a date range, parses the
response with the `Bodu.Text.Formats` RFC 4180 reader, and serves the results as
`Bodu.Financial.ExchangeRate` values through the standard `IDatedExchangeRateProvider`
and `IExchangeRateProvider` contracts — so it composes with `Money.ConvertTo`,
the caching and aggregating providers, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates.Boe;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new BoeExchangeRateProvider(new BoeExchangeRateOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate is the number of US dollars per pound on that date.

// Read a whole range at once (GBP-based pairs; the reverse direction is inverted). The result is
// an IReadOnlyList<ExchangeRate> that also reports the requested window and the observed span.
ExchangeRateRangeResult series =
    await provider.GetRatesAsync("GBP", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (BoeSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.FromIsoCode}/{info.Pair.ToIsoCode} ({info.SeriesCode})");
```

## Behaviour

- **GBP-based.** The Bank of England quotes each currency's daily spot rate against the
  pound (one IADB series per currency, for example `XUDLUSS` for the US dollar). Direct
  (`GBP→X`) and inverse (`X→GBP`) lookups are supported; cross pairs are not.
- **Range queries.** The IADB is queried by date range rather than by fixed file, so
  loading is range-based. `LoadRangeAsync` fetches an inclusive range; a synchronous lookup
  that misses an unloaded date blocks to download a bounded window around it (configurable
  via `OnDemandWindowDays`) only when `AllowSynchronousNetworkAccess` is enabled (it is
  `false` by default, so the provider serves a snapshot of already-loaded data and a
  synchronous miss does not reach the network).
- **Caching.** Downloaded range responses are cached on disk (configurable) and refreshed
  on a TTL, since a range ending near today can gain an observation each business day.
- **Configuration.** `BoeExchangeRateOptions` carries working defaults and binds through
  `Microsoft.Extensions.Options`. The provider's connection to the IADB is grouped under
  its `Endpoint` (`BoeEndpointOptions`) — base URL, query path, HTTP timeout, and
  user-agent — so the query can be pointed at a mirror or proxy without touching caching or
  series configuration. The package ships its own `AddBoeReferenceRates` registration in the `Bodu.Financial.ExchangeRates` namespace.

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new BoeExchangeRateProvider(options, ...)` — the provider builds, owns, and disposes its own
  `HttpClient`, created via `ExchangeRateHttpClientFactory.Create` from the configured user agent
  and timeout. Dispose the provider (for example with `using`) to release the client.
- `new BoeExchangeRateProvider(httpClient, options, ...)` — you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the
  `*.DependencyInjection` package uses, backed by `IHttpClientFactory`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the `*.DependencyInjection` package wire one for you (category
`Bodu.Financial.ExchangeRates.Boe.BoeExchangeRateProvider`). When no logger is supplied it
defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

The levels follow the conventions used by `Microsoft.Extensions.Http`, EF Core, and the
Azure SDK — the completed download is the one `Information` line per range loaded, payload
detail is `Trace`, and degraded paths are `Warning`. Every level is individually
configurable on `BoeExchangeRateOptions`:

| Event | Default level | Option property |
|---|---|---|
| A range download is starting | `Debug` | `DownloadStartingLogLevel` |
| A range loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| A range download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |

```csharp
// Quieten the per-range line and turn off per-observation tracing entirely.
var options = new BoeExchangeRateOptions
{
    DownloadCompletedLogLevel = LogLevel.Debug,
    ObservationIngestedLogLevel = LogLevel.None,
};
```

The default verbosity is deliberately low: at `Information` you see one line per range
loaded; at `Debug` you additionally see when downloads start; only at `Trace` do you get a
line per rate observation (which can be thousands per range — keep it for targeted
debugging).

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
