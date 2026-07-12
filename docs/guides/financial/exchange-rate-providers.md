---
title: Built-in exchange-rate providers
---

# Built-in exchange-rate providers

Bodu ships eleven exchange-rate providers, one per published source. Each is a thin
**fetcher** that downloads and parses its source and serves the result through the
same [`IDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.IDatedRateProvider)
and timeless [`IRateProvider`](xref:Bodu.Financial.ExchangeRates.IRateProvider)
contracts — so any provider drops into the same lookups, the same
[caching and aggregation](exchange-rate-caching.md) layer, and the same
[`Money` conversions](money.md) as every other. None of them knows anything about
caching; that is added in front (see the caching guide).

## The providers at a glance

| Provider | Package | Base | Source format | History depth | DI registration |
|---|---|---|---|---|---|
| Reserve Bank of Australia | `Bodu.Financial.ExchangeRates.Rba` | AUD | published `.xls` workbooks, per **era** | since 1983‑01‑01 (first era) | `AddRbaExchangeRates()` |
| European Central Bank | `Bodu.Financial.ExchangeRates.Ecb` | EUR | the `eurofxref` XML **feed** | since 1999‑01‑04 (euro epoch) | `AddEcbExchangeRates()` |
| Bank of England | `Bodu.Financial.ExchangeRates.Boe` | GBP | CSV over a date **window** | since 1975‑01‑02 (daily spot inception) | `AddBoeExchangeRates()` |
| Yahoo Finance | `Bodu.Financial.ExchangeRates.Yahoo` | *any pair* | per-**ticker** chart JSON | since 2003‑12‑01 (chart inception) | `AddYahooExchangeRates()` |
| OFX (ofx.com) | `Bodu.Financial.ExchangeRates.Ofx` | *any pair* | per-**pair** spot-rate-history JSON | unbounded (multi-decade, no published floor) | `AddOfxExchangeRates()` |
| XE.com | `Bodu.Financial.ExchangeRates.Xe` | *any pair* | per-**pair** charting-rates JSON | rolling ~10 years (server-determined, estimated) | `AddXeExchangeRates()` |
| OANDA | `Bodu.Financial.ExchangeRates.Oanda` | *any pair* | per-**pair** rate-history JSON | rolling ~180 days | `AddOandaExchangeRates()` |
| Fixer (fixer.io) | `Bodu.Financial.ExchangeRates.Fixer` | *any pair* | per-**pair** time-series / single-date JSON (`access_key`) | since 1999‑01‑01 | `AddFixerExchangeRates()` |
| exchangerate.host | `Bodu.Financial.ExchangeRates.ExchangeRateHost` | *any pair* | per-**pair** time-series / single-date JSON (`access_key`) | since 1999‑01‑01 | `AddExchangeRateHostExchangeRates()` |
| FRED (St. Louis Fed) | `Bodu.Financial.ExchangeRates.Fred` | *mapped pairs* | per-**pair** `series_id` observations JSON (`api_key`) | unbounded (per series) | `AddFredExchangeRates()` |
| IMF | `Bodu.Financial.ExchangeRates.Imf` | USD | monthly representative-rates **TSV report** (keyless, daily) | unbounded | `AddImfExchangeRates()` |

RBA, ECB, BoE, and IMF quote one base currency against many others (AUD, EUR, GBP,
and USD respectively); direct (`BASE→X`) and inverse (`X→BASE`) lookups are supported,
cross pairs are not. Yahoo, OFX, XE, OANDA, Fixer, and exchangerate.host fetch a
distinct series per pair, so they serve arbitrary pairs directly (subject to their
plan's base-currency rules). FRED is per-pair too, but each pair must be mapped to a
FRED series identifier — it ships a built-in map for the major pairs and accepts more
through its options. Fixer, exchangerate.host, and FRED require an API key on their
options; IMF is keyless.

Every provider advertises its history depth through
[`HistoryAvailability`](xref:Bodu.Financial.ExchangeRates.WebRateProvider.HistoryAvailability),
so a caller can resolve the earliest date worth requesting before issuing a
lookup. The value is advisory — it describes the source's published coverage,
not a per-day or per-series guarantee: BoE and Yahoo floors reflect their
longest-running series (later-inception series exist), ECB's floor follows the
configured feeds (rolling when the full-history feed is excluded), RBA's
follows the configured era catalogue, and XE's window is an estimate of a
server-determined range. The pair providers expose the value as a settable
option; BoE adds its own options property for the same purpose.

The value is also discoverable at runtime through the
[`IHistoricalRateProvider`](xref:Bodu.Financial.ExchangeRates.IHistoricalRateProvider)
capability interface, and the caching and aggregation decorators consume it by
default: fetches for declared-unavailable dates are skipped or clamped rather
than issued. See
[Respecting advertised history](exchange-rate-caching.md#respecting-advertised-history)
in the caching guide.

## What every provider shares

Because the surface is uniform, the same code drives any provider — the only
difference is the type you construct and its options.

**Two construction styles.** The options-only constructor builds and **owns** an
[`HttpClient`](xref:System.Net.Http.HttpClient); dispose the provider to release
it. The constructor that takes an `HttpClient` uses the caller's client as-is and
never disposes it — the form the dependency-injection registration uses, backed by
`IHttpClientFactory`.

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;

// The provider owns the HttpClient it builds from the options; dispose it to release the client.
using var provider = new RbaRateProvider(new RbaRateProviderOptions());
```

**Warm, then look up.** A provider loads its source on demand. Warm the in-memory
store first with `LoadRangeAsync` (or the provider's preload method), then resolve
synchronously:

```csharp
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

RateLookupResult usd = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate, usd.Rate.Provider == "RBA", usd.Provenance.Origin == RateOrigin.Live
```

A synchronous lookup that misses an unloaded span blocks to download it when
`AllowSynchronousNetworkAccess` is enabled (the default); set it to `false` to
force callers onto the asynchronous surface or an explicit preload. Concurrent
loads of the same span are coalesced, so a burst of misses triggers at most one
download.

**A common warm-up surface across every provider.** Each provider also carries
source-specific warm-up methods shaped to its feed (`LoadRangeAsync` and
`PreloadAsync` for the bulk feeds; `LoadPairAsync` for the pair feeds). On top of
those, every provider implements
<xref:Bodu.Financial.ExchangeRates.IPairRateLoader> — `LoadPairAsync(from, to, start, end)`
and `GetLoadedPairs()` — so a consumer can warm a pair's window and enumerate the
loaded pairs uniformly without knowing whether the source fetches by pair, era,
feed, or range. On a single-base feed such as RBA the pair must involve its base
currency (for example AUD); an unsupported pair is rejected before any download.

```csharp
IPairRateLoader loader = provider;
await loader.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

foreach (CurrencyPair pair in loader.GetLoadedPairs())
{
    // pair.From, pair.To
}
```

**Lookups behave identically across providers.** Dated and timeless lookups,
`TryGetRate`, range reads, date-resolution policies, and inverse fallback all work
the same way described in [Working with exchange rates](exchange-rates.md):

```csharp
// Dated, with a fallback policy and the resolution metadata.
provider.TryGetRate("AUD", "USD", new DateOnly(2024, 1, 6),
    RateLookupOptions.PreviousWithin(7), out RateLookupResult prev);

// A whole window at once (AUD-based pairs; the reverse direction is inverted).
RateRangeResult series =
    await provider.GetRatesAsync("AUD", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// The timeless surface resolves the most recent rate.
decimal latest = ((IRateProvider)provider).GetRate("AUD", "USD");
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

**Shared options.** The pair-provider options — Yahoo, OFX, XE, OANDA, Fixer,
exchangerate.host, and FRED — plus the IMF options derive from the abstract
<xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions>, so they share its surface — the
`BaseAddress`, `HttpTimeout` (default 30s), `UserAgent`, `AllowSynchronousNetworkAccess`,
`DefaultLookback` (default 7 days), a `CurrencyAliases` map for non-ISO source symbols,
and the per-event `*LogLevel` knobs. RBA, ECB, and BoE carry their own option types
(`RbaRateProviderOptions` and friends) with source-specific settings such as the RBA's
era list. The DI registration's `configureResilience` parameter tunes the standard
Polly handler (`HttpStandardResilienceOptions`) wrapped around the named `HttpClient`.

## Reserve Bank of Australia (AUD)

[`RbaRateProvider`](xref:Bodu.Financial.ExchangeRates.RbaRateProvider)
serves the RBA's published historical daily rates. The RBA splits its history into
**eras**, each a published `.xls` workbook covering a span of dates; a range load
fetches every era overlapping the request. Configure the eras, base URL, timeout,
user agent, and disk cache through
[`RbaRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.RbaRateProviderOptions);
warm the store with `PreloadAsync`, `LoadEraAsync`, or `LoadRangeAsync`.

```csharp
using Bodu.Financial.ExchangeRates;

using var rba = new RbaRateProvider(new RbaRateProviderOptions());
await rba.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

RateLookupResult aud = rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

foreach (RbaSeriesInfo info in rba.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.From}/{info.Pair.To} ({info.SeriesId})");
```

## European Central Bank (EUR)

[`EcbRateProvider`](xref:Bodu.Financial.ExchangeRates.EcbRateProvider)
serves the ECB euro foreign-exchange reference rates from the `eurofxref` XML feed.
The feed carries the full published history, so one load covers every date it
contains. Options are
[`EcbRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.EcbRateProviderOptions).

```csharp
using Bodu.Financial.ExchangeRates;

using var ecb = new EcbRateProvider(new EcbRateProviderOptions());
await ecb.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

RateLookupResult usd = ecb.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
RateLookupResult inverse = ecb.GetRate("USD", "EUR", new DateOnly(2023, 1, 3)); // inverted
```

## Bank of England (GBP)

[`BoeRateProvider`](xref:Bodu.Financial.ExchangeRates.BoeRateProvider)
serves the Bank of England daily spot rates, downloaded as CSV over a requested
date **window**. A synchronous miss loads a window around the requested date;
`LoadRangeAsync` warms an explicit range. Options are
[`BoeRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.BoeRateProviderOptions).

```csharp
using Bodu.Financial.ExchangeRates;

using var boe = new BoeRateProvider(new BoeRateProviderOptions());
await boe.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

RateLookupResult gbp = boe.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));
```

## Yahoo Finance (any pair)

[`YahooRateProvider`](xref:Bodu.Financial.ExchangeRates.YahooRateProvider)
fetches a Yahoo Finance chart per currency pair (the ticker `AUDUSD=X` for AUD/USD),
so unlike the central-bank providers it serves arbitrary pairs rather than one base
currency. Warm a pair over a window with `LoadPairAsync`. Options are
[`YahooRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.YahooRateProviderOptions).

```csharp
using Bodu.Financial.ExchangeRates;

using var yahoo = new YahooRateProvider(new YahooRateProviderOptions());
await yahoo.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

RateLookupResult aud = yahoo.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
```

## OFX (any pair)

[`OfxRateProvider`](xref:Bodu.Financial.ExchangeRates.OfxRateProvider)
fetches the OFX (ofx.com) public spot-rate-history JSON service per currency pair,
so like Yahoo it serves arbitrary pairs rather than one base currency. Warm a pair
over a window with `LoadPairAsync`. Options are
[`OfxRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.OfxRateProviderOptions).

```csharp
using Bodu.Financial.ExchangeRates;

using var ofx = new OfxRateProvider(new OfxRateProviderOptions());
await ofx.LoadPairAsync("USD", "AUD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

RateLookupResult aud = ofx.GetRate("USD", "AUD", new DateOnly(2023, 1, 3));
```

## XE.com (any pair)

[`XeRateProvider`](xref:Bodu.Financial.ExchangeRates.XeRateProvider)
fetches the XE.com charting-rates JSON service per currency pair, so like Yahoo and
OFX it serves arbitrary pairs rather than one base currency. Warm a pair over a
window with `LoadPairAsync`. The authorization token the endpoint requires is
acquired automatically from the XE website, so no API key or manual setup is needed.
Options are
[`XeRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.XeRateProviderOptions).

> [!WARNING]
> This package is **Experimental**. The authorization token is recovered by scraping
> an unversioned public XE page, so a change to the site's markup or bundling can
> silently reduce the provider to empty results — a broken scraper looks the same as
> "no rate for this pair". Treat it as best-effort: do not rely on it as your sole
> rate source in production, and pair it with a stable primary feed (for example the
> ECB, Bank of England, or RBA providers).

```csharp
using Bodu.Financial.ExchangeRates;

using var xe = new XeRateProvider(new XeRateProviderOptions());
await xe.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

RateLookupResult usd = xe.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
```

## OANDA (any pair)

[`OandaRateProvider`](xref:Bodu.Financial.ExchangeRates.OandaRateProvider)
fetches the OANDA Historical Currency Converter rate-history JSON service per
currency pair, so like Yahoo and OFX it serves arbitrary pairs rather than one base
currency. Warm a pair over a window with `LoadPairAsync`. Options are
[`OandaRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.OandaRateProviderOptions).

The anonymous endpoint serves only a rolling recent window — roughly the last 180
days — so a request for an earlier start date returns just what the feed publishes.
The provider advertises this through
[`HistoryAvailability`](xref:Bodu.Financial.ExchangeRates.WebRateProvider.HistoryAvailability),
so a caller can resolve the earliest date worth requesting before issuing a lookup.

```csharp
using Bodu.Financial.ExchangeRates;

using var oanda = new OandaRateProvider(new OandaRateProviderOptions());
var today = DateOnly.FromDateTime(DateTime.UtcNow);
await oanda.LoadPairAsync("AUD", "USD", today.AddDays(-30), today);

RateLookupResult usd = oanda.GetRate("AUD", "USD", today.AddDays(-1));
```

## Fixer (any pair, API key)

[`FixerRateProvider`](xref:Bodu.Financial.ExchangeRates.FixerRateProvider)
fetches the Fixer (fixer.io) time-series and single-date JSON endpoints per currency
pair. It denominates the response against the source currency and requests the
destination currency as the quote symbol. Set the `access_key` through
[`FixerRateProviderOptions.ApiKey`](xref:Bodu.Financial.ExchangeRates.FixerRateProviderOptions).

> [!NOTE]
> Fixer's free plan is locked to a EUR base and to the latest and single-date
> endpoints; changing the base currency and the time-series endpoint require a paid
> plan. A request the plan does not permit surfaces as a fetch failure, so on the free
> plan request pairs whose source currency is EUR (or rely on the inverse fallback).

```csharp
using Bodu.Financial.ExchangeRates;

using var fixer = new FixerRateProvider(new FixerRateProviderOptions { ApiKey = "…" });
await fixer.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

RateLookupResult usd = fixer.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

## exchangerate.host (any pair, API key)

[`ExchangeRateHostRateProvider`](xref:Bodu.Financial.ExchangeRates.ExchangeRateHostRateProvider)
fetches the exchangerate.host time-series and single-date JSON endpoints per currency
pair. The response keys quotes by the concatenated source+quote code (for example
`EURUSD`). Set the `access_key` through
[`ExchangeRateHostRateProviderOptions.ApiKey`](xref:Bodu.Financial.ExchangeRates.ExchangeRateHostRateProviderOptions);
the free plan is locked to a USD source currency.

```csharp
using Bodu.Financial.ExchangeRates;

using var host = new ExchangeRateHostRateProvider(new ExchangeRateHostRateProviderOptions { ApiKey = "…" });
await host.LoadPairAsync("USD", "EUR", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

RateLookupResult eur = host.GetRate("USD", "EUR", new DateOnly(2023, 1, 3));
```

## FRED (mapped pairs, API key)

[`FredRateProvider`](xref:Bodu.Financial.ExchangeRates.FredRateProvider)
serves the St. Louis Fed FRED `series/observations` endpoint. FRED publishes one
directional series per pair (for example `DEXUSEU` for EUR/USD), so each pair is
mapped to its series identifier through
[`FredRateProviderOptions.SeriesMap`](xref:Bodu.Financial.ExchangeRates.FredRateProviderOptions).
The options ship a built-in map for the major USD pairs and accept more; a pair with
no mapping returns no data. Missing values (FRED's `"."`) are skipped. Set the
`api_key` through `FredRateProviderOptions.ApiKey`.

```csharp
using Bodu.Financial.ExchangeRates;

using var fred = new FredRateProvider(new FredRateProviderOptions { ApiKey = "…" });
await fred.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

RateLookupResult usd = fred.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

## IMF (USD base, keyless, daily)

[`ImfRateProvider`](xref:Bodu.Financial.ExchangeRates.ImfRateProvider)
serves the IMF **Representative Exchange Rates** — daily rates reported by member central
banks — downloaded as the IMF's published **monthly tab-separated report**. Like the
central-bank providers it is a single-base source (base **USD**): `USD→X` and `X→USD`
resolve, cross pairs do not. It is keyless. The report quotes most currencies as units per
USD and a few (for example AUD, GBP, EUR) as USD per unit; the provider normalizes the
quotation direction on ingest, so consumers always see a consistent `USD→X` rate. Loading is
month-based — one download covers every currency across a month's business days — and closed
months are cached permanently. Options are
[`ImfRateProviderOptions`](xref:Bodu.Financial.ExchangeRates.ImfRateProviderOptions).

```csharp
using Bodu.Financial.ExchangeRates;

using var imf = new ImfRateProvider(new ImfRateProviderOptions());
await imf.LoadRangeAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

RateLookupResult jpy = imf.GetRate("USD", "JPY", new DateOnly(2026, 4, 1));
RateLookupResult usd = imf.GetRate("JPY", "USD", new DateOnly(2026, 4, 1)); // inverted
```

## Registering a provider with dependency injection

Each provider package ships its own DI registration — there is no separate
`*.DependencyInjection` package. The `Add<Source>...` extension method registers the
provider on the [`IFinancialServiceBuilder`](xref:Bodu.Financial.IFinancialServiceBuilder),
backed by a named `HttpClient` with the standard Polly resilience handler, and
resolvable as both the dated and timeless surfaces. The `Add<Source>...` extension
methods live in the `Bodu.Financial.ExchangeRates` namespace (`AddFinancialService`
lives in `Bodu.Financial`), so both `using` directives bring
the chain into scope:

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialService()
        .AddRbaExchangeRates(builder.Configuration)    // section Financial:Rba
        .AddEcbExchangeRates(builder.Configuration);     // section Financial:Ecb

// AddBoeExchangeRates(), AddYahooExchangeRates(), AddOfxExchangeRates(),
// AddXeExchangeRates(), AddOandaExchangeRates(), AddFixerExchangeRates(),
// AddExchangeRateHostExchangeRates(), AddFredExchangeRates(), and
// AddImfExchangeRates() register the others.
```

## Adding caching in front

A provider is a pure fetcher, so wrap it in the [caching layer](exchange-rate-caching.md)
to serve repeated lookups without re-hitting the source. The source must be
registered first — the cached registration resolves it, it does not build it:

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialService()
        .AddRbaExchangeRates()
        .AddCachedRateProvider<RbaRateProvider>("RBA",
            configure: o => o.DefaultExpiry = TimeSpan.FromHours(12));
```

To serve several sources behind one entry point with per-pair routing and a
fallback or averaging strategy, group them with the
[aggregator](exchange-rate-caching.md#grouping-providers-with-the-aggregator).

## Snapshotting and exporting rates

Every web provider maintains its accumulated observations as an immutable
[`RateBook`](xref:Bodu.Financial.ExchangeRates.RateBook) plus a ready-to-query
[`FixedDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider),
and both are exposed directly:
[`GetLoadedBook()`](xref:Bodu.Financial.ExchangeRates.WebRateProvider.GetLoadedBook) and
[`GetLoadedSnapshot()`](xref:Bodu.Financial.ExchangeRates.WebRateProvider.GetLoadedSnapshot)
return the current instances without copying or locking. The results are pinned
at call time — later fetches swap the provider's internal references and never
mutate an instance already handed out — so a snapshot is deterministic, works
offline, and survives disposing the source provider. Call again after further
loads to observe newly accumulated data.

To pin a window of history from *any*
[`IDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.IDatedRateProvider) —
including a cached or aggregated one — materialize it with
[`ToFixedProviderAsync`](xref:Bodu.Financial.Extensions.DatedRateProviderExtensions):

```csharp
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;

using var rba = new RbaRateProvider(new RbaRateProviderOptions());

// Fetch and freeze AUD/USD and AUD/EUR for Q1, decoupled from the live provider.
FixedDatedRateProvider q1 = await rba.ToFixedProviderAsync(
    new[]
    {
        new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD),
        new CurrencyPair(CurrencyCode.AUD, CurrencyCode.EUR),
    },
    new DateOnly(2024, 1, 1),
    new DateOnly(2024, 3, 31));
```

The conversion surface composes in both directions. A rate sequence — for
example the rows a range lookup returned — materializes into a book with
[`ToBook()`](xref:Bodu.Financial.Extensions.ExchangeRateEnumerableExtensions),
which keeps one series per (pair, provider) and stores inverse-resolved rows
under their natively quoted direction; a book wraps into a provider with
[`ToFixedProvider()`](xref:Bodu.Financial.Extensions.RateBookExtensions)
(optionally with a provider-priority list); and
[`RateBook.ToBuilder()`](xref:Bodu.Financial.ExchangeRates.RateBook.ToBuilder)
round-trips a book into a mutable
[`RateTableBuilder`](xref:Bodu.Financial.ExchangeRates.RateTableBuilder) for
editing — `book.ToBuilder()` … edit … `ToBook().ToFixedProvider()`. Each series'
fetch instant (`FetchedAtUtc`) is preserved through every step, so provenance
survives a web → fixed round trip losslessly.

## Choosing a provider

| Need | Reach for |
|---|---|
| Official AUD rates with deep history | RBA |
| Official EUR reference rates | ECB |
| Official GBP spot rates | BoE |
| An arbitrary pair not quoted by a central bank | Yahoo, OFX, XE, OANDA, Fixer, or exchangerate.host |
| Recent rates for an arbitrary pair (rolling ~180-day window) | OANDA |
| A commercial API you already hold a key for | Fixer or exchangerate.host |
| Official US-published FX series (per mapped pair) | FRED |
| Official daily USD representative rates (keyless) | IMF |
| One pair from several sources, with fallback or an average | the [aggregator](exchange-rate-caching.md) over any mix |

## See also

- [Working with exchange rates](exchange-rates.md) — the provider contracts, lookup
  options, provenance, and series these providers serve.
- [Caching and aggregating exchange rates](exchange-rate-caching.md) — adding a
  read-through cache and grouping providers.
- [Exchange-rate types catalogue](exchange-types.md) — every FX type mapped to a scenario.
- [`RbaRateProvider`](xref:Bodu.Financial.ExchangeRates.RbaRateProvider),
  [`EcbRateProvider`](xref:Bodu.Financial.ExchangeRates.EcbRateProvider),
  [`BoeRateProvider`](xref:Bodu.Financial.ExchangeRates.BoeRateProvider),
  [`YahooRateProvider`](xref:Bodu.Financial.ExchangeRates.YahooRateProvider),
  [`OfxRateProvider`](xref:Bodu.Financial.ExchangeRates.OfxRateProvider),
  [`XeRateProvider`](xref:Bodu.Financial.ExchangeRates.XeRateProvider),
  [`OandaRateProvider`](xref:Bodu.Financial.ExchangeRates.OandaRateProvider),
  [`FixerRateProvider`](xref:Bodu.Financial.ExchangeRates.FixerRateProvider),
  [`ExchangeRateHostRateProvider`](xref:Bodu.Financial.ExchangeRates.ExchangeRateHostRateProvider),
  [`FredRateProvider`](xref:Bodu.Financial.ExchangeRates.FredRateProvider),
  [`ImfRateProvider`](xref:Bodu.Financial.ExchangeRates.ImfRateProvider)
