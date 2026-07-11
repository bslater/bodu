# Bodu.Financial.ExchangeRates.Imf

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **IMF**
(International Monetary Fund) **Representative Exchange Rates** — the daily rates each
issuing central bank reports to the Fund, published as a monthly report.

It downloads the IMF's monthly tab-separated report (every reported currency across each
business day of the month), parses it, and serves the results as
`Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard `IDatedRateProvider`
and `IRateProvider` contracts — so it composes with `Money.ConvertTo`, the caching and
aggregating providers, and the rest of the Bodu.Financial FX stack. The same interfaces and
DI shape as every other provider, a different data source.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
// The IMF report is keyless — no API key is required.
using var provider = new ImfRateProvider(new ImfRateProviderOptions());

// Warm the in-memory store for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

RateLookupResult jpy = provider.GetRate("USD", "JPY", new DateOnly(2026, 4, 1));
// jpy.Rate.Rate is the number of Japanese yen per US dollar on that date.

RateLookupResult usd = provider.GetRate("JPY", "USD", new DateOnly(2026, 4, 1)); // inverted

// Discover what pairs the loaded data supports.
foreach (ImfSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.From}/{info.Pair.To}");
```

## Behaviour and limitations

- **USD-anchored.** Every rate the report publishes quotes a currency against the US
  dollar, so only `USD→X` and `X→USD` lookups are serviceable; a cross-currency pair (for
  example `GBP/EUR`) is rejected with `RateSeriesNotFoundException`. The reverse (`X→USD`)
  direction is served by the inverse-lookup fallback.
- **Bulk, month-based loading.** One download covers every reported currency for every
  business day of a month, so a request for any date in a month loads the whole month at
  once. `LoadRangeAsync`, `LoadMonthAsync`, and `PreloadAsync` (current month) warm the
  store; a range spanning several months downloads each covering month.
- **Direction auto-normalized.** The report quotes most currencies as units per US dollar,
  but marks a few (labelled `(1)`, for example `Euro(1)`, `U.K. pound(1)`) as US dollars
  per unit. Both are normalized on ingest, so the stored rate is always units of the quote
  currency per one US dollar.
- **Keyless.** The report requires no API key; there is no `ApiKey` option.
- **Currency-name mapping.** The report labels currencies by name (`Japanese yen`,
  `Chinese yuan`, …). `CurrencyNames` maps each label to its ISO 4217 code; a label absent
  from the map is resolved directly against the ISO catalogue, and one that resolves
  neither way is skipped. Extend `CurrencyNames` to add labels not seeded by default.
- **Monthly cache.** Downloaded reports are cached on disk (configurable). A closed
  month's report is immutable and is served from cache indefinitely; only the current,
  still-growing month is refreshed on a TTL (`RefreshInterval`). For a durable,
  cross-provider cache, compose with `AddCachedRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package.

## Endpoint configuration

`ImfRateProviderOptions` carries working defaults and binds through
`Microsoft.Extensions.Options`.

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://www.imf.org/external/np/fin/data/` | The IMF report host. |
| `ReportPath` | `rms_mth.aspx` | The monthly report resource path. |
| `ReportType` | `REP` | The report-type selector (representative rates). |
| `EnableDiskCache` | `true` | Persist downloaded reports to an on-disk cache. |
| `CacheDirectory` | `null` (temp `bodu-imf`) | The on-disk cache directory. |
| `RefreshInterval` | 12 h | How long the current month's cached report stays fresh (closed months never expire). |
| `CurrencyNames` | seeded | Maps IMF currency labels to ISO 4217 codes; extend to add labels. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand fetches from synchronous lookups. |

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new ImfRateProvider(options, ...)` — the provider builds, owns, and disposes its own
  `HttpClient`, created via `RateProviderHttpClientFactory.Create` from the configured user
  agent and timeout. Dispose the provider (for example with `using`) to release the client.
- `new ImfRateProvider(httpClient, options, ...)` — you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the DI
  registration uses, backed by `IHttpClientFactory`.

## Dependency injection

The package ships its own `AddImfExchangeRates` registration in the
`Bodu.Financial.ExchangeRates` namespace — there is no separate `*.DependencyInjection`
package.

```csharp
services
    .AddFinancialService(configuration)
    .AddImfExchangeRates(configuration);
```

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
