# Bodu.Financial.ExchangeRates.Rba

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **Reserve
Bank of Australia's** published historical daily exchange-rate files.

It downloads the RBA `.xls` files, parses them (via
[`Bodu.Formats.Excel.Binary`](../Bodu.Formats.Excel.Binary) →
[`Bodu.IO.Compound`](../Bodu.IO.Compound)), and serves the results as
`Bodu.Financial.ExchangeRate` values through the standard `IDatedExchangeRateProvider`
and `IExchangeRateProvider` contracts — so it composes with `Money.ConvertTo`,
`CompositeDatedExchangeRateProvider`, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates.Rba;

var provider = new RbaExchangeRateProvider(httpClient, new RbaExchangeRateOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
// usd.Rate == 0.6828m

// Read a whole range at once (AUD-based pairs; the reverse direction is inverted).
IReadOnlyList<ExchangeRate> series =
    await provider.GetRatesAsync("AUD", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (RbaSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.FromIsoCode}/{info.Pair.ToIsoCode} ({info.SeriesId})");
```

## Behaviour

- **AUD-based.** RBA quotes the Australian dollar against each currency. Direct
  (`AUD→X`) and inverse (`X→AUD`) lookups are supported; cross pairs are not.
- **Loading.** Call `PreloadAsync` / `LoadRangeAsync` to warm the in-memory store. A
  synchronous lookup that misses an unloaded era will block to download it when
  `AllowSynchronousNetworkAccess` is enabled (the default).
- **Caching.** Downloaded files are cached on disk (configurable); immutable historical
  eras are cached indefinitely and the open-ended current era refreshes on a TTL.
- **Configuration.** `RbaExchangeRateOptions` carries working defaults and binds through
  `Microsoft.Extensions.Options`; see the `*.DependencyInjection` package for
  `AddRbaHistoricalRates`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the `*.DependencyInjection` package wire one for you (category
`Bodu.Financial.ExchangeRates.Rba.RbaExchangeRateProvider`). When no logger is supplied it
defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

The levels follow the conventions used by `Microsoft.Extensions.Http`, EF Core, and the
Azure SDK — the completed download is the one `Information` line per fetch, payload detail
is `Trace`, and degraded paths are `Warning`. Every level is individually configurable on
`RbaExchangeRateOptions`:

| Event | Default level | Option property |
|---|---|---|
| An era download is starting | `Debug` | `DownloadStartingLogLevel` |
| An era loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| An era download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |

```csharp
// Quieten the per-fetch line and turn off per-observation tracing entirely.
var options = new RbaExchangeRateOptions
{
    DownloadCompletedLogLevel = LogLevel.Debug,
    ObservationIngestedLogLevel = LogLevel.None,
};
```

The default verbosity is deliberately low: at `Information` you see one line per era
loaded; at `Debug` you additionally see when downloads start; only at `Trace` do you get a
line per rate observation (which can be thousands per era — keep it for targeted
debugging).

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
