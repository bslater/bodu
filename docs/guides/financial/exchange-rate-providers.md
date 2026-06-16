---
title: Built-in exchange-rate providers
---

# Built-in exchange-rate providers

Bodu ships four exchange-rate providers, one per published source. Each is a thin
**fetcher** that downloads and parses its source and serves the result through the
same [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
and timeless [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider)
contracts — so any provider drops into the same lookups, the same
[caching and aggregation](exchange-rate-caching.md) layer, and the same
[`Money` conversions](money.md) as every other. None of them knows anything about
caching; that is added in front (see the caching guide).

## The providers at a glance

| Provider | Package | Base | Source format | DI registration |
|---|---|---|---|---|
| Reserve Bank of Australia | `Bodu.Financial.ExchangeRates.Rba` | AUD | published `.xls` workbooks, per **era** | `AddRbaHistoricalRates()` |
| European Central Bank | `Bodu.Financial.ExchangeRates.Ecb` | EUR | the `eurofxref` XML **feed** | `AddEcbReferenceRates()` |
| Bank of England | `Bodu.Financial.ExchangeRates.Boe` | GBP | CSV over a date **window** | `AddBoeReferenceRates()` |
| Yahoo Finance | `Bodu.Financial.ExchangeRates.Yahoo` | *any pair* | per-**ticker** chart JSON | `AddYahooExchangeRates()` |

RBA, ECB, and BoE quote one base currency against many others; direct (`BASE→X`)
and inverse (`X→BASE`) lookups are supported, cross pairs are not. Yahoo fetches a
distinct ticker per pair, so it serves arbitrary pairs directly.

## What every provider shares

Because the surface is uniform, the same code drives any provider — the only
difference is the type you construct and its options.

**Two construction styles.** The options-only constructor builds and **owns** an
[`HttpClient`](xref:System.Net.Http.HttpClient); dispose the provider to release
it. The constructor that takes an `HttpClient` uses the caller's client as-is and
never disposes it — the form the dependency-injection package uses, backed by
`IHttpClientFactory`.

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Rba;

// The provider owns the HttpClient it builds from the options; dispose it to release the client.
using var provider = new RbaExchangeRateProvider(new RbaExchangeRateOptions());
```

**Warm, then look up.** A provider loads its source on demand. Warm the in-memory
store first with `LoadRangeAsync` (or the provider's preload method), then resolve
synchronously:

```csharp
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate, usd.Rate.Provider == "RBA", usd.Provenance.Origin == ExchangeRateOrigin.Live
```

A synchronous lookup that misses an unloaded span blocks to download it when
`AllowSynchronousNetworkAccess` is enabled (the default); set it to `false` to
force callers onto the asynchronous surface or an explicit preload. Concurrent
loads of the same span are coalesced, so a burst of misses triggers at most one
download.

**Lookups behave identically across providers.** Dated and timeless lookups,
`TryGetRate`, range reads, date-resolution policies, and inverse fallback all work
the same way described in [Working with exchange rates](exchange-rates.md):

```csharp
// Dated, with a fallback policy and the resolution metadata.
provider.TryGetRate("AUD", "USD", new DateOnly(2024, 1, 6),
    ExchangeRateLookupOptions.PreviousWithin(7), out ExchangeRateLookupResult prev);

// A whole window at once (AUD-based pairs; the reverse direction is inverted).
ExchangeRateRangeResult series =
    await provider.GetRatesAsync("AUD", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// The timeless surface resolves the most recent rate.
decimal latest = ((IExchangeRateProvider)provider).GetRate("AUD", "USD");
```

**Logging is opt-in and free when unused.** Pass an
[`ILogger`](xref:Microsoft.Extensions.Logging.ILogger) (or let the DI package wire
one); with no logger the provider uses `NullLogger`. Each provider's options expose
per-event `*LogLevel` properties, defaulting to one `Information` line per
completed download, `Debug` for download starts, and `Trace` for per-observation
detail.

**Downloaded payloads are cached on disk.** Each provider keeps a best-effort cache
of the raw bytes it downloaded (configurable, on by default), so immutable history
is not re-fetched. This is distinct from the [rate cache](exchange-rate-caching.md):
the on-disk payload cache avoids re-downloading the source file, while the rate
cache stores parsed, resolved rates in front of the provider.

## Reserve Bank of Australia (AUD)

[`RbaExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Rba.RbaExchangeRateProvider)
serves the RBA's published historical daily rates. The RBA splits its history into
**eras**, each a published `.xls` workbook covering a span of dates; a range load
fetches every era overlapping the request. Configure the eras, base URL, timeout,
user agent, and disk cache through
[`RbaExchangeRateOptions`](xref:Bodu.Financial.ExchangeRates.Rba.RbaExchangeRateOptions);
warm the store with `PreloadAsync`, `LoadEraAsync`, or `LoadRangeAsync`.

```csharp
using Bodu.Financial.ExchangeRates.Rba;

using var rba = new RbaExchangeRateProvider(new RbaExchangeRateOptions());
await rba.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult aud = rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

foreach (RbaSeriesInfo info in rba.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.FromIsoCode}/{info.Pair.ToIsoCode} ({info.SeriesId})");
```

## European Central Bank (EUR)

[`EcbExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateProvider)
serves the ECB euro foreign-exchange reference rates from the `eurofxref` XML feed.
The feed carries the full published history, so one load covers every date it
contains. Options are
[`EcbExchangeRateOptions`](xref:Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateOptions).

```csharp
using Bodu.Financial.ExchangeRates.Ecb;

using var ecb = new EcbExchangeRateProvider(new EcbExchangeRateOptions());
await ecb.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

ExchangeRateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
ExchangeRateLookupResult inverse = ecb.GetRate("USD", "EUR", new DateOnly(2023, 1, 3)); // inverted
```

## Bank of England (GBP)

[`BoeExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Boe.BoeExchangeRateProvider)
serves the Bank of England daily spot rates, downloaded as CSV over a requested
date **window**. A synchronous miss loads a window around the requested date;
`LoadRangeAsync` warms an explicit range. Options are
[`BoeExchangeRateOptions`](xref:Bodu.Financial.ExchangeRates.Boe.BoeExchangeRateOptions).

```csharp
using Bodu.Financial.ExchangeRates.Boe;

using var boe = new BoeExchangeRateProvider(new BoeExchangeRateOptions());
await boe.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

ExchangeRateLookupResult gbp = boe.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));
```

## Yahoo Finance (any pair)

[`YahooExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Yahoo.YahooExchangeRateProvider)
fetches a Yahoo Finance chart per currency pair (the ticker `AUDUSD=X` for AUD/USD),
so unlike the central-bank providers it serves arbitrary pairs rather than one base
currency. Warm a pair over a window with `LoadPairAsync`. Options are
[`YahooExchangeRateOptions`](xref:Bodu.Financial.ExchangeRates.Yahoo.YahooExchangeRateOptions).

```csharp
using Bodu.Financial.ExchangeRates.Yahoo;

using var yahoo = new YahooExchangeRateProvider(new YahooExchangeRateOptions());
await yahoo.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

ExchangeRateLookupResult aud = yahoo.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
```

## Registering a provider with dependency injection

Each provider has a companion `*.DependencyInjection` package whose extension method
registers the provider on the [`IFinancialServiceBuilder`](xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder),
backed by a named `HttpClient` with the standard Polly resilience handler, and
resolvable as both the dated and timeless surfaces:

```csharp
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Rba.DependencyInjection;
using Bodu.Financial.ExchangeRates.Ecb.DependencyInjection;

services.AddBoduFinancial()
        .AddRbaHistoricalRates(builder.Configuration)    // section Financial:Rba
        .AddEcbReferenceRates(builder.Configuration);     // section Financial:Ecb

// AddBoeReferenceRates() and AddYahooExchangeRates() register the other two.
```

## Adding caching in front

A provider is a pure fetcher, so wrap it in the [caching layer](exchange-rate-caching.md)
to serve repeated lookups without re-hitting the source. The source must be
registered first — the cached registration resolves it, it does not build it:

```csharp
using Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

services.AddBoduFinancial()
        .AddRbaHistoricalRates()
        .AddCachedExchangeRateProvider<RbaExchangeRateProvider>("RBA",
            configure: o => o.DefaultExpiry = TimeSpan.FromHours(12));
```

To serve several sources behind one entry point with per-pair routing and a
fallback or averaging strategy, group them with the
[aggregator](exchange-rate-caching.md#grouping-providers-with-the-aggregator).

## Choosing a provider

| Need | Reach for |
|---|---|
| Official AUD rates with deep history | RBA |
| Official EUR reference rates | ECB |
| Official GBP spot rates | BoE |
| An arbitrary pair not quoted by a central bank | Yahoo |
| One pair from several sources, with fallback or an average | the [aggregator](exchange-rate-caching.md) over any mix |

## See also

- [Working with exchange rates](exchange-rates.md) — the provider contracts, lookup
  options, provenance, and series these providers serve.
- [Caching and aggregating exchange rates](exchange-rate-caching.md) — adding a
  read-through cache and grouping providers.
- [Exchange-rate types catalogue](exchange-types.md) — every FX type mapped to a scenario.
- [`RbaExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Rba.RbaExchangeRateProvider),
  [`EcbExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateProvider),
  [`BoeExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Boe.BoeExchangeRateProvider),
  [`YahooExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Yahoo.YahooExchangeRateProvider)
