---
title: Bodu.Financial.ExchangeRates — Getting started
---

# Bodu.Financial.ExchangeRates — Getting started

Unfamiliar with terms like *warm-then-lookup*, *bulk vs pair provider*, *history availability*, *payload cache*, or *single-flight*? Read [Core concepts](concepts.md) first.

## Install

The infrastructure package is a dependency of every feed package, so in practice you install a feed and get the infrastructure transitively. Reference it directly only when you write your own provider:

```bash
dotnet add package Bodu.Financial.ExchangeRates
dotnet add package Bodu.Financial.ExchangeRates.Ecb
```

Targets `net8.0`. `Bodu.Financial.ExchangeRates` depends on `Bodu.Financial`, `Bodu.Core`, and `Microsoft.Extensions.Logging.Abstractions`. A feed package such as `Bodu.Financial.ExchangeRates.Ecb` additionally references `Bodu.Financial.DependencyInjection` and `Bodu.Financial.ExchangeRates.DependencyInjection` (for its `AddEcbExchangeRates` registration) plus the `Microsoft.Extensions` configuration, options, HTTP, and `Http.Resilience` packages at the .NET 8.0 LTS line.

## Minimal samples

### Construct a provider and look up a rate

The options-only constructor builds and owns its `HttpClient`; dispose the provider to release it. Warm the store, then resolve synchronously:

```csharp
using Bodu.Financial.ExchangeRates;

using var ecb = new EcbRateProvider(new EcbRateProviderOptions());

await ecb.LoadRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31));

RateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2024, 1, 3));
Console.WriteLine($"{usd.Rate.Rate} from {usd.Rate.Provider} on {usd.Rate.Date}");

// The reverse direction resolves through inverse fallback.
RateLookupResult eur = ecb.GetRate("USD", "EUR", new DateOnly(2024, 1, 3));
Console.WriteLine(eur.Rate.IsInverted);   // True
```

`AllowSynchronousNetworkAccess` is `false` by default, so a synchronous lookup for a date outside the warmed window reports a miss (`TryGetRate` returns `false`; `GetRate` throws `KeyNotFoundException`) rather than downloading.

### Let the asynchronous surface warm on demand

The asynchronous getters fetch whatever is missing before resolving, so no explicit warm-up is required:

```csharp
using Bodu.Financial.ExchangeRates;

using var yahoo = new YahooRateProvider(new YahooRateProviderOptions());

RateLookupResult aud = await yahoo.GetRateAsync(
    "AUD", "USD",
    new DateOnly(2024, 6, 14),
    RateLookupOptions.PreviousWithin(7));
```

A single-date lookup on a pair provider fetches the window `[date − DefaultLookback, date]` (seven days by default), which is why `PreviousWithin(7)` has observations to fall back to when the requested date is a weekend.

### Read a range

```csharp
RateRangeResult window = await ecb.GetRatesAsync(
    "EUR", "GBP",
    new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

Console.WriteLine($"{window.Count} observations, {window.FirstObservedDate}–{window.LastObservedDate}");

foreach (ExchangeRate rate in window)
    Console.WriteLine($"{rate.Date:yyyy-MM-dd} {rate.Rate}");

if (window.IsEmpty)
    Console.WriteLine("no observations in the requested window");
```

The result records the requested window (`RequestedStartDate` / `RequestedEndDate`) separately from the observed span, and is itself an `IReadOnlyList<ExchangeRate>`.

### Warm a pair uniformly, whatever the feed

Every provider implements <xref:Bodu.Financial.ExchangeRates.IPairRateLoader>, so cache-warming code need not know whether the feed fetches by pair, era, feed, or range:

```csharp
IPairRateLoader loader = ecb;
await loader.LoadPairAsync("EUR", "JPY", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

foreach (CurrencyPair pair in loader.GetLoadedPairs())
    Console.WriteLine($"{pair.From}/{pair.To}");
```

On a single-base feed the pair must involve the base currency (EUR for the ECB provider); a cross pair is rejected with `RateSeriesNotFoundException` before any download.

### Register through dependency injection

Each feed package ships its own `Add<Source>ExchangeRates` extension in the `Bodu.Financial.ExchangeRates` namespace. The `IFinancialServiceBuilder` overload composes with `AddFinancialService` (from `Bodu.Financial`); the `IServiceCollection` overload does both in one call:

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;

// Composed on the financial builder — binds the Financial:Ecb section when configuration is supplied.
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddEcbExchangeRates(builder.Configuration);

// Or the one-call form, which registers the core financial services too.
builder.Services.AddEcbExchangeRates(builder.Configuration);
```

The provider is registered as a singleton, backed by a named `HttpClient` fitted with the standard Polly resilience handler, and resolvable as `EcbRateProvider`, <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider>, and <xref:Bodu.Financial.ExchangeRates.IRateProvider>:

```csharp
IDatedRateProvider rates = app.Services.GetRequiredService<IDatedRateProvider>();
RateLookupResult result = await rates.GetRateAsync("EUR", "USD", new DateOnly(2024, 1, 3));
```

The matching `appsettings.json` section — every key below binds to <xref:Bodu.Financial.ExchangeRates.EcbRateProviderOptions> and its nested <xref:Bodu.Financial.ExchangeRates.EcbEndpointOptions>, shown at their defaults:

```json
{
  "Financial": {
    "Ecb": {
      "Endpoint": {
        "BaseUrl": "https://www.ecb.europa.eu/stats/eurofxref/",
        "HttpTimeout": "00:00:30",
        "UserAgent": "Bodu.Financial.ExchangeRates.Ecb"
      },
      "AllowSynchronousNetworkAccess": false,
      "EnableDiskCache": true,
      "CacheDirectory": null,
      "RefreshInterval": "12:00:00",
      "CurrencyAliases": {},
      "DownloadStartingLogLevel": "Debug",
      "DownloadCompletedLogLevel": "Information",
      "DownloadFailedLogLevel": "Warning",
      "ObservationIngestedLogLevel": "Information",
      "SynchronousNetworkFetchLogLevel": "Warning"
    }
  }
}
```

The one member that does not bind from configuration is `Feeds` — an `IReadOnlyList<EcbRateFeed>` whose elements carry a constructor, defaulting to `EcbRateFeed.Default` (the 90-day feed, then the full history). Set it through the `configure` callback, which runs after binding:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddEcbExchangeRates(
        builder.Configuration,
        sectionName: "Financial:Ecb",
        configure: options => options.Feeds = new[] { EcbRateFeed.Full },
        configureResilience: resilience => resilience.Retry.MaxRetryAttempts = 5);
```

Options are validated at startup (`ValidateOnStart`), so a null `BaseUrl` or a non-positive `RefreshInterval` fails the host before the first request.

### Supply an API key for a pair provider

Fixer, exchangerate.host, and FRED require a key on their options. `FixerRateProviderOptions` derives from <xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions>, so it validates the shared members and rejects a blank `ApiKey`. In code:

```csharp
using Bodu.Financial.ExchangeRates;

using var fixer = new FixerRateProvider(new FixerRateProviderOptions { ApiKey = "…" });

await fixer.LoadPairAsync("EUR", "USD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
RateLookupResult usd = fixer.GetRate("EUR", "USD", new DateOnly(2024, 1, 3));
```

Through DI, keep the key out of source and bind it from configuration (user secrets, environment variables, or a vault-backed provider all feed the same section). Every key below binds; the shared `WebRateProviderOptions` members are shown at their defaults and the Fixer-specific ones follow:

```json
{
  "Financial": {
    "Fixer": {
      "BaseAddress": "https://data.fixer.io/api/",
      "HttpTimeout": "00:00:30",
      "MaxResponseContentBufferSize": 67108864,
      "UserAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
      "AllowSynchronousNetworkAccess": false,
      "DefaultLookback": "7.00:00:00",
      "CurrencyAliases": {},
      "DownloadStartingLogLevel": "Debug",
      "DownloadCompletedLogLevel": "Information",
      "DownloadFailedLogLevel": "Warning",
      "ObservationIngestedLogLevel": "Information",
      "SynchronousNetworkFetchLogLevel": "Warning",
      "ApiKey": "<from user secrets>",
      "TimeSeriesPath": "timeseries",
      "HistoricalPath": "{date}"
    }
  }
}
```

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddFixerExchangeRates(builder.Configuration);          // section Financial:Fixer
```

`HistoryAvailability` is the one `WebRateProviderOptions` member that is not configuration-bindable (it is a record struct with factory members); Fixer's options preset it to `RateHistoryAvailability.Since(1999-01-01)`, and a `configure` callback can override it. The `configure` callback is also the place to inject a key resolved at runtime:

```csharp
builder.Services
    .AddFinancialService()
    .AddFixerExchangeRates(configure: options => options.ApiKey = keyVault.GetSecret("fixer-access-key"));
```

The same shape applies to `AddExchangeRateHostExchangeRates` (`Financial:ExchangeRateHost`, `ApiKey` sent as `access_key`) and `AddFredExchangeRates` (`Financial:Fred`, `ApiKey` sent as `api_key`, plus the `SeriesMap` from pair to FRED series identifier).

### Control the payload cache

The bulk providers (ECB, BoE, RBA, IMF) keep a best-effort on-disk cache of the raw files they download so immutable history is not re-fetched across process restarts. It is controlled by three options members; the ECB names are shown, the others match apart from RBA's `CurrentEraRefreshInterval` and `EnableDiskCache` default of `false`:

| Member | Default | Effect |
|---|---|---|
| `EnableDiskCache` | `true` (ECB, BoE, IMF); `false` (RBA) | `false` substitutes the no-op <xref:Bodu.Financial.ExchangeRates.NullByteCache`1> — nothing is written or read. |
| `CacheDirectory` | `null` | The folder that holds one file per download unit. `null` or blank resolves to a provider-named folder under the system temporary path — `bodu-ecb`, `bodu-boe`, `bodu-rba`, `bodu-imf` beneath `Path.GetTempPath()`. |
| `RefreshInterval` | 12 hours | A cached file older than this is re-downloaded, so a feed that gains a new observation is refreshed. |

```csharp
var options = new EcbRateProviderOptions
{
    CacheDirectory = "/var/cache/myapp/ecb",     // explicit, shared location
    RefreshInterval = TimeSpan.FromHours(6),
};

// Or disable it entirely (in-memory coverage still avoids duplicate downloads within a process).
var uncached = new EcbRateProviderOptions { EnableDiskCache = false };
```

Pair providers (Yahoo, OFX, XE, OANDA, Fixer, exchangerate.host, FRED) have no payload cache; to persist their rates across restarts, put a [rate cache](../../guides/financial/exchange-rate-caching.md) in front of them.

### Ask how far back a feed goes

```csharp
using var oanda = new OandaRateProvider(new OandaRateProviderOptions());

RateHistoryAvailability history = oanda.HistoryAvailability;
DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
DateOnly? earliest = history.GetEarliestAvailable(today);   // roughly today − 180 days

if (history.IsAvailable(new DateOnly(2019, 1, 1), today))
{
    // safe to request
}
```

### Export what has been fetched

```csharp
using Bodu.Financial.ExchangeRates;

FixedDatedRateProvider offline = ecb.GetLoadedSnapshot();   // immutable, network-free, disposal-independent
RateBook book = ecb.GetLoadedBook();                         // the same data as a composable book
```

Hand the snapshot to code that must never touch the network, or serialize the book's observations with the [financial JSON converters](../financial-serialization-json/index.md).

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — the provider family and the "which provider" table.
- **[Built-in exchange-rate providers](../../guides/financial/exchange-rate-providers.md)** — every feed's warm-up methods and options in detail.
- **[Caching and aggregating exchange rates](../../guides/financial/exchange-rate-caching.md)** — a read-through rate cache and multi-source aggregation in front of these providers.
- **[Financial dependency injection](../../guides/financial/dependency-injection.md)** — `AddFinancialService`, the fluent builder, and swapping in a test double.
- **[Testing your own provider](../../guides/financial/testing-providers.md)** — the contract-test bases and the offline stub handler.
- **[Bodu.Financial.ExchangeRates API reference](xref:Bodu.Financial.ExchangeRates)** — full type-by-type docs.
- **[Runnable samples](../../samples/financial.md)** — `LiveRates` (opt-in network), `CachedRates`, `AggregatedRates`, and `CustomProvider`.
