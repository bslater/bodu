---
title: Configuring providers from appsettings
---

# Configuring providers from appsettings

Every built-in exchange-rate provider ships an `Add<Source>ExchangeRates` registration that binds
its options from a `Financial:<Source>` configuration section, validates them at host start, and
wraps the provider's `HttpClient` in the standard Polly resilience pipeline. This page is the
reference for that surface: the section each provider binds, every key each section accepts and
its default, where an API key should come from, and what the resilience hook actually does. For
what the providers *serve*, see [Built-in exchange-rate providers](exchange-rate-providers.md);
for the cache that sits in front of them, see
[Configuring rate caching from appsettings](caching-configuration.md).

All eleven registrations follow one shape, so the page shows one bulk provider (ECB) and one pair
provider (Fixer) in full and then tabulates only what differs for the other nine.

## Pattern 1 — the two registration forms

Each provider package ships two extension methods with the same name. The
<xref:Bodu.Financial.IFinancialServiceBuilder> form composes on the builder that
`AddFinancialService` returns; the `IServiceCollection` form is a one-call convenience that
registers the core financial services (through `AddFinancialService(configuration)`) and then
calls the builder form. Both live in the `Bodu.Financial.ExchangeRates` namespace.

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Builder form: compose on the IFinancialServiceBuilder that AddFinancialService returns.
builder.Services
    .AddFinancialService(builder.Configuration)          // binds "Financial"
    .AddEcbExchangeRates(builder.Configuration)          // binds "Financial:Ecb"
    .AddFixerExchangeRates(builder.Configuration);       // binds "Financial:Fixer"
```

```csharp
// IServiceCollection form: one call registers the core financial services and the provider.
builder.Services.AddEcbExchangeRates(builder.Configuration);
```

The builder form takes five parameters — `configuration`, `sectionName`, `configure`, and
`configureResilience` after the builder itself — and is the only one that exposes the
`configureResilience` hook. The `IServiceCollection` form stops at `configure`. Pass a
`sectionName` to bind from somewhere other than the default:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddEcbExchangeRates(builder.Configuration, sectionName: "Rates:EuropeanCentralBank");
```

Whichever form you use, the provider is registered once as a singleton and exposed as
<xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> and
<xref:Bodu.Financial.ExchangeRates.IRateProvider> through idempotent `TryAdd` registrations, so
the first provider registered wins each contract. Register several providers and the later ones
are still resolvable by their concrete type (`EcbRateProvider`, `FixerRateProvider`, …) — and the
[aggregator](exchange-rate-caching.md#grouping-providers-with-the-aggregator) is the way to put
them behind one contract.

> [!NOTE]
> Options are validated at startup. Every `Add<Source>ExchangeRates` wires the options type's
> `TryValidate` into `ValidateOnStart`, so a missing API key, a null base address, or a
> non-positive timeout fails the host before the first request rather than on the first lookup.
> The message names the provider ("Fixer exchange-rate options are invalid.").

## Pattern 2 — a bulk provider in full: ECB

The bulk providers (ECB, BoE, RBA, IMF) download one file that covers many pairs, so their options
describe the feed: an endpoint, a payload-cache location, and a refresh interval. Every key below
binds to <xref:Bodu.Financial.ExchangeRates.EcbRateProviderOptions> (and its nested
<xref:Bodu.Financial.ExchangeRates.EcbEndpointOptions>), shown at its default:

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

| Key | Default | Meaning |
|---|---|---|
| `Endpoint:BaseUrl` | `https://www.ecb.europa.eu/stats/eurofxref/` | The directory the feed files are resolved against. |
| `Endpoint:HttpTimeout` | 30 s | The per-attempt timeout; with DI it drives the resilience pipeline (Pattern 5). |
| `Endpoint:UserAgent` | `Bodu.Financial.ExchangeRates.Ecb` | The `User-Agent` header on every request. |
| `AllowSynchronousNetworkAccess` | `false` | Whether a *synchronous* lookup that misses may block to download. Off, a synchronous miss is reported as a miss and only the asynchronous surface or an explicit warm-up fetches. |
| `EnableDiskCache` | `true` | Whether downloaded feed files are kept on disk so immutable history is not re-fetched. |
| `CacheDirectory` | `null` | Where the payload cache lives. `null` or blank resolves to `bodu-ecb` under `Path.GetTempPath()`. |
| `RefreshInterval` | 12 h | A cached file older than this is re-downloaded. |
| `CurrencyAliases` | `{}` | ISO code → feed symbol overrides; entries **merge into** the defaults rather than replacing them. |
| `*LogLevel` | see block | The level each diagnostic is logged at; `None` suppresses it. |

One member does **not** bind: `Feeds`, an `IReadOnlyList<EcbRateFeed>` whose elements carry a
constructor. The configuration binder leaves it at `EcbRateFeed.Default` (the 90-day feed, then
the full history) even when the section supplies a `Feeds` array. Set it — and anything else
that is not a plain value — through the `configure` callback, which runs after binding:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddEcbExchangeRates(
        builder.Configuration,
        configure: options =>
        {
            options.Feeds = new[] { EcbRateFeed.Full };                        // constructor-bearing element type
            options.AllowSynchronousNetworkAccess = true;                       // opt in to blocking sync misses
        });
```

## Pattern 3 — a pair provider in full: Fixer

The pair providers (Yahoo, OFX, XE, OANDA, Fixer, exchangerate.host, FRED) fetch one currency
pair per request, and their options all derive from
<xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions>, so the first twelve keys below are
shared by every one of them (and by IMF, whose options derive from the same base). The last
three are Fixer's own, from <xref:Bodu.Financial.ExchangeRates.FixerRateProviderOptions>:

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
      "ApiKey": "",
      "TimeSeriesPath": "timeseries",
      "HistoricalPath": "{date}"
    }
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `BaseAddress` | per provider | The API host; keep the trailing slash so relative paths resolve. |
| `HttpTimeout` | 30 s | Per-attempt timeout (drives the resilience pipeline under DI). |
| `MaxResponseContentBufferSize` | 64 MiB | Response-size ceiling on a provider-owned client; not applied to a client the host supplies. |
| `UserAgent` | a browser-like string | Several public endpoints reject requests without a recognizable user agent. |
| `AllowSynchronousNetworkAccess` | `false` | As for the bulk providers. |
| `DefaultLookback` | 7 days | The window a synchronous or undated lookup fetches on demand: it ends on the requested date and spans this duration. |
| `CurrencyAliases` | `{}` | ISO code → source symbol; values must be alphanumeric (they are substituted into the request URL). |
| `*LogLevel` | see block | As for the bulk providers. |
| `ApiKey` | `""` | **Required** — a blank key fails validation. Sent as `access_key`. |
| `TimeSeriesPath` / `HistoricalPath` | `timeseries` / `{date}` | The relative endpoints for a window and for a single day. |

`HistoryAvailability` is the one `WebRateProviderOptions` member that does not bind (a record
struct with factory members). Each provider's constructor presets it — Fixer to
`RateHistoryAvailability.Since(1999-01-01)` — and the `configure` callback can override it.

## Per-provider differences

Every provider binds `Financial:<Source>` by default, names its `HttpClient`
`Bodu.Financial.ExchangeRates.<Source>` (the `HttpClientName` constant on each
`<Source>FinancialServiceBuilderExtensions` class), and validates on start. The table lists what
is specific to each: which shared surface it uses, the keys it adds, and how its API key (if any)
reaches the wire.

| Provider | Section | Options type | Shape | Provider-specific keys (defaults) | Key |
|---|---|---|---|---|---|
| ECB | `Financial:Ecb` | `EcbRateProviderOptions` | bulk, nested `Endpoint` | `Endpoint` (`BaseUrl`, `HttpTimeout`, `UserAgent`), `EnableDiskCache` (`true`), `CacheDirectory` (`null` → `bodu-ecb`), `RefreshInterval` (12 h), `CurrencyAliases` (`{}`); `Feeds` code-only | none |
| Bank of England | `Financial:Boe` | `BoeRateProviderOptions` | bulk, nested `Endpoint` | `Endpoint` (`BaseUrl` `https://www.bankofengland.co.uk/boeapps/database/`, `QueryPath` `_iadb-fromshowcolumns.asp`, `HttpTimeout` 30 s, `UserAgent` `Bodu.Financial.ExchangeRates.Boe`), `OnDemandWindowDays` (10 — replaces `DefaultLookback`), `EnableDiskCache` (`true`), `CacheDirectory` (`null` → `bodu-boe`), `RefreshInterval` (12 h); `Series` and `HistoryAvailability` code-only; no `CurrencyAliases`, no `SynchronousNetworkFetchLogLevel` | none |
| RBA | `Financial:Rba` | `RbaRateProviderOptions` | bulk, flat | `BaseUrl` (`https://www.rba.gov.au/statistics/tables/xls-hist/`), `HttpTimeout` (30 s), `UserAgent` (`Bodu.Financial.ExchangeRates.Rba`), `EnableDiskCache` (**`false`**), `CacheDirectory` (`null` → `bodu-rba`), `CurrentEraRefreshInterval` (12 h), `CurrencyAliases` (`{ "SDR": "XDR" }`); `Eras` code-only; no `SynchronousNetworkFetchLogLevel` | none |
| IMF | `Financial:Imf` | `ImfRateProviderOptions` | `WebRateProviderOptions` + report keys | `BaseAddress` (`https://www.imf.org/external/np/fin/data/`), `ReportPath` (`rms_mth.aspx`), `ReportType` (`REP`), `EnableDiskCache` (`true`), `CacheDirectory` (`null` → `bodu-imf`), `RefreshInterval` (12 h), `CurrencyNames` (report label → ISO code, ~35 defaults, merges) | none |
| Yahoo Finance | `Financial:Yahoo` | `YahooRateProviderOptions` | pair | `BaseAddress` (`https://query1.finance.yahoo.com/`), `ChartPath` (`v8/finance/chart/{symbol}`), `SymbolFormat` (`{from}{to}=X`) | none |
| OFX | `Financial:Ofx` | `OfxRateProviderOptions` | pair | `BaseAddress` (`https://api.ofx.com/`), `HistoryPath` (`PublicSite.ApiService/SpotRateHistory/{from}/{to}/{start}/{end}`), `DecimalPlaces` (6), `ReportingInterval` (`daily`) | none |
| XE.com | `Financial:Xe` | `XeRateProviderOptions` | pair | `BaseAddress` (`https://www.xe.com/`), `ChartingRatesPath` (`api/protected/charting-rates/`), `AuthBootstrapUrl` (`https://www.xe.com/currencycharts`), `AuthScriptBaseUrl` (`https://www.xe.com/_next/`) | none (token scraped at runtime) |
| OANDA | `Financial:Oanda` | `OandaRateProviderOptions` | pair | `BaseAddress` (`https://fxds-hcc.oanda.com/`), `UpdatePath` (`api/data/update/`), `PrimePath` (`""`), `Source` (`OANDA`), `Price` (`mid`; `bid`/`ask`), `Period` (`daily`), `Adjustment` (0) | none |
| Fixer | `Financial:Fixer` | `FixerRateProviderOptions` | pair | `BaseAddress` (`https://data.fixer.io/api/`), `TimeSeriesPath` (`timeseries`), `HistoricalPath` (`{date}`) | `ApiKey` → `access_key` query parameter |
| exchangerate.host | `Financial:ExchangeRateHost` | `ExchangeRateHostRateProviderOptions` | pair | `BaseAddress` (`https://api.exchangerate.host/`), `TimeSeriesPath` (`timeseries`), `HistoricalPath` (`historical`) | `ApiKey` → `access_key` query parameter |
| FRED | `Financial:Fred` | `FredRateProviderOptions` | pair | `BaseAddress` (`https://api.stlouisfed.org/fred/`), `ObservationsPath` (`series/observations`), `SeriesMap` (`"EUR/USD": "DEXUSEU"` and 18 more; merges) | `ApiKey` → `api_key` query parameter |

Three shapes, then, and two of them are already shown in full above. RBA's is the flat one:

```json
{
  "Financial": {
    "Rba": {
      "BaseUrl": "https://www.rba.gov.au/statistics/tables/xls-hist/",
      "HttpTimeout": "00:00:30",
      "UserAgent": "Bodu.Financial.ExchangeRates.Rba",
      "AllowSynchronousNetworkAccess": false,
      "EnableDiskCache": false,
      "CacheDirectory": null,
      "CurrentEraRefreshInterval": "12:00:00",
      "CurrencyAliases": { "SDR": "XDR" },
      "DownloadStartingLogLevel": "Debug",
      "DownloadCompletedLogLevel": "Information",
      "DownloadFailedLogLevel": "Warning",
      "ObservationIngestedLogLevel": "Information"
    }
  }
}
```

And the Bank of England's adds a query path and an on-demand window to the nested endpoint shape:

```json
{
  "Financial": {
    "Boe": {
      "Endpoint": {
        "BaseUrl": "https://www.bankofengland.co.uk/boeapps/database/",
        "QueryPath": "_iadb-fromshowcolumns.asp",
        "HttpTimeout": "00:00:30",
        "UserAgent": "Bodu.Financial.ExchangeRates.Boe"
      },
      "AllowSynchronousNetworkAccess": false,
      "OnDemandWindowDays": 10,
      "EnableDiskCache": true,
      "CacheDirectory": null,
      "RefreshInterval": "12:00:00",
      "DownloadStartingLogLevel": "Debug",
      "DownloadCompletedLogLevel": "Information",
      "DownloadFailedLogLevel": "Warning",
      "ObservationIngestedLogLevel": "Information"
    }
  }
}
```

For the remaining pair providers, take the Fixer block, drop the three Fixer keys, and add the
provider-specific keys from the table. FRED's `SeriesMap` and IMF's `CurrencyNames` are
dictionaries, so — like `CurrencyAliases` — a section entry is **added to** the built-in map,
never replacing it:

```json
{
  "Financial": {
    "Fred": {
      "ApiKey": "",
      "ObservationsPath": "series/observations",
      "SeriesMap": { "USD/AUD": "DEXUSAL" }
    }
  }
}
```

> [!IMPORTANT]
> The binder is silent about members it cannot bind. A `Feeds`, `Series`, `Eras`, or
> `HistoryAvailability` entry in a section is neither applied nor reported; the option keeps its
> default. Set those through `configure`.

## Pattern 4 — sourcing an API key

Fixer, exchangerate.host, and FRED refuse to start without a key, and the key must not live in
`appsettings.json`. `Host.CreateApplicationBuilder` already layers the standard configuration
sources — `appsettings.json`, `appsettings.{Environment}.json`, user secrets (in the
`Development` environment), environment variables, then command-line arguments — so the
registration needs no extra code: whichever layer defines `Financial:Fixer:ApiKey` wins.

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddFixerExchangeRates(builder.Configuration);     // ApiKey arrives from whichever layer sets Financial:Fixer:ApiKey
```

On a developer machine, store the key with the user-secrets tool; the colon-separated path is
the same one the JSON section spells out:

```bash
dotnet user-secrets init
dotnet user-secrets set "Financial:Fixer:ApiKey" "your-access-key"
```

In a container or CI environment, use an environment variable. The configuration provider maps
a double underscore to a section separator, so the following three are equivalent:

```text
Financial__Fixer__ApiKey=your-access-key
Financial__ExchangeRateHost__ApiKey=your-access-key
Financial__Fred__ApiKey=your-api-key
```

When the key is only known at runtime — a vault client, a rotated secret — set it in the
`configure` callback, which runs after binding and before validation, so the startup check still
sees the final value:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddFredExchangeRates(
        builder.Configuration,
        configure: options =>
        {
            options.ApiKey = secrets("fred-api-key");        // runs after configuration binding
            options.SeriesMap["USD/AUD"] = "DEXUSAL";        // add to the built-in map, not replace it
        });
```

The key goes on the wire as a query parameter (`access_key` for Fixer and exchangerate.host,
`api_key` for FRED), which is the endpoints' own convention — so avoid logging request URIs at
`Information` in production. XE needs no key: its provider recovers the endpoint's bearer token
from the public site at runtime.

## Pattern 5 — resilience and the `HttpClient`

`AddWebRateProvider` — the shared machinery every `Add<Source>ExchangeRates` delegates to in
<xref:Bodu.Financial.ExchangeRates.WebRateProviderExtensions> — registers a named `HttpClient`
through `IHttpClientFactory` and fits it with the standard resilience handler
(`AddStandardResilienceHandler`). The pipeline is configured from the provider's options:

| Setting | Value | Why |
|---|---|---|
| `HttpClient.Timeout` | infinite | So the client's own timer never competes with the pipeline's timeouts. |
| `User-Agent` header | the options' `UserAgent` | Applied only when non-blank. |
| Attempt timeout | `HttpTimeout` (30 s by default) | One try. |
| Total request timeout | 3 × `HttpTimeout` | Leaves room for the default retries. |
| Circuit-breaker sampling duration | at least 2 × `HttpTimeout` | Widened only when the handler's default is smaller, to satisfy its validation. |
| Everything else | the handler's defaults | Retry with exponential backoff and jitter (three attempts), rate limiter, circuit breaker. |

The `configureResilience` callback receives the `HttpStandardResilienceOptions` *after* those
defaults have been applied, so it can adjust any of them:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddRbaExchangeRates(
        builder.Configuration,
        configureResilience: resilience =>
        {
            resilience.Retry.MaxRetryAttempts = 5;                       // default 3
            resilience.Retry.Delay = TimeSpan.FromSeconds(1);            // base for exponential backoff
            resilience.CircuitBreaker.FailureRatio = 0.5;                // open after half the sampled calls fail
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
        });
```

A transport failure surfaces as `HttpRequestException` only after the pipeline has exhausted
its retries; a payload the parser rejects is never retried (it is not a transport failure) and
surfaces at once as <xref:Bodu.Financial.ExchangeRates.ExchangeRateFormatException>. See
[Failure modes and exceptions](exchange-rate-providers.md#failure-modes-and-exceptions).

None of this applies to a provider you construct by hand from its options: that provider builds
and owns its own client through
<xref:Bodu.Financial.ExchangeRates.RateProviderHttpClientFactory>, with `HttpTimeout` applied as
`HttpClient.Timeout`, the `UserAgent` header, the response-size cap — and **no retry**.

## Two settings worth a second look

**`AllowSynchronousNetworkAccess`.** Off by default on every provider. Off, a synchronous
`GetRate` / `TryGetRate` / `GetRates` that finds nothing loaded reports a miss (`false`, or
`KeyNotFoundException` from the throwing form) without touching the network; the asynchronous
surface and the warm-up methods (`LoadRangeAsync`, `LoadPairAsync`) are the paths that fetch.
On, a synchronous miss blocks to download the missing window — `DefaultLookback` (or BoE's
`OnDemandWindowDays`) ending on the requested date — which is convenient in a worker but can
deadlock on a thread with a captured `SynchronizationContext`; the provider converts that case
into an `InvalidOperationException` rather than a hang. Prefer warming at startup: the
[warm-up hosted service](caching-configuration.md#pattern-4--warming-the-cache-at-startup)
does exactly that.

**`EnableDiskCache` / `CacheDirectory`.** The bulk providers keep the raw bytes they downloaded
on disk (a *payload* cache, distinct from the [rate cache](exchange-rate-caching.md)) so that a
restart does not re-download a multi-decade file. With `CacheDirectory` unset it lands in a
provider-named folder — `bodu-ecb`, `bodu-boe`, `bodu-rba`, `bodu-imf` — under the system
temporary path, which is fine for a workstation and wrong for a fleet: point it at a persistent,
writable directory in production, or set `EnableDiskCache` to `false` (the in-memory snapshot
still prevents duplicate downloads within the process). RBA defaults it off because its era
workbooks are large and change only for the current era.

## API summary

| Member | Where | Description |
|---|---|---|
| `Add<Source>ExchangeRates(IConfiguration?, string sectionName, Action<TOptions>?, Action<HttpStandardResilienceOptions>?)` | `IFinancialServiceBuilder`, `Bodu.Financial.ExchangeRates` | Binds `Financial:<Source>`, validates on start, registers the named `HttpClient` + resilience pipeline and the provider singleton as `IDatedRateProvider` / `IRateProvider`. |
| `Add<Source>ExchangeRates(IConfiguration?, string sectionName, Action<TOptions>?)` | `IServiceCollection`, `Bodu.Financial.ExchangeRates` | Calls `AddFinancialService(configuration)` and then the builder form; no resilience hook. |
| `<Source>FinancialServiceBuilderExtensions.HttpClientName` | each provider package | The named-client key (`Bodu.Financial.ExchangeRates.<Source>`), for `IHttpClientFactory.CreateClient` or a logging filter. |
| <xref:Bodu.Financial.ExchangeRates.WebRateProviderExtensions.AddWebRateProvider*> | `Bodu.Financial.ExchangeRates.DependencyInjection` | The shared registration every provider (and [your own](custom-web-provider.md)) delegates to. |
| <xref:Bodu.Financial.ExchangeRates.WebRateProviderOptions> | `Bodu.Financial.ExchangeRates` | The shared option surface of the pair providers and IMF; `TryValidate` / `Validate` run the shared invariants then the provider's `TryValidateCore`. |
| `TryValidate(out string? error)` | every options type | The predicate `ValidateOnStart` runs; call it yourself when constructing options by hand. |

## Where to go next

- [Configuring rate caching from appsettings](caching-configuration.md) — the `Financial:RateCache` tree that sits in front of these providers.
- [Built-in exchange-rate providers](exchange-rate-providers.md) — what each provider serves, its history depth, and its failure modes.
- [Financial dependency injection](dependency-injection.md) — the `AddFinancialService` builder these registrations compose on.
- [Writing your own web provider](custom-web-provider.md) — give a custom feed the same `Financial:<Source>` registration.
- [Bodu.Financial.ExchangeRates getting started](../../docs/exchange-rates/getting-started.md) — the shortest path to a first live lookup.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic.
