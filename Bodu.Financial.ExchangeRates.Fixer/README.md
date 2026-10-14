# Bodu.Financial.ExchangeRates.Fixer

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **Fixer**
(`fixer.io`) foreign-exchange REST service.

It fetches the Fixer time-series and single-date endpoints, parses the JSON response, and
serves the results as `Bodu.Financial.ExchangeRates.ExchangeRate` values through the
standard `IDatedRateProvider` and `IRateProvider` contracts - so it composes with
`Money.ConvertTo`, the caching and aggregating providers, and the rest of the
Bodu.Financial FX stack. The same interfaces and DI shape as every other provider, a
different data source.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new FixerRateProvider(new FixerRateProviderOptions { ApiKey = "…" });

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 6, 30));

RateLookupResult usd = provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

## Behaviour

- **Base + quote.** Fixer denominates its response against a base currency and returns the
  requested quote symbols. A pair is fetched by denominating against the source currency
  and requesting the destination currency as the quote symbol.
- **Endpoint selection.** A one-day request uses the single-date endpoint; a multi-day
  range uses the time-series endpoint.
- **Plan limits.** The free plan is locked to a EUR base and to the latest and single-date
  endpoints; changing the base currency and the time-series endpoint require a paid plan. A
  request the account's plan does not permit surfaces as a fetch failure, not a pre-empted
  request - on the free plan, request pairs whose source currency is EUR. A request for the
  reverse pair sends that pair's own source currency as the base, so to read `USD/EUR` warm
  `EUR/USD` first; its reverse then resolves by inversion from the loaded series.
- **API key required.** Set `ApiKey`; it is presented as the `access_key` query parameter.
- **No provider-local disk cache.** For durable caching, compose with
  `AddCachedRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package.

## Endpoint configuration

| Option | Default | Purpose |
|---|---|---|
| `ApiKey` | *(required)* | The Fixer access key, sent as `access_key`. |
| `BaseAddress` | `https://data.fixer.io/api/` | The API host. |
| `TimeSeriesPath` | `timeseries` | The multi-day time-series endpoint. |
| `HistoricalPath` | `{date}` | The single-date endpoint template (`{date}` placeholder). |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand fetches from synchronous lookups. |
| `DefaultLookback` | 7 days | The look-back window, ending on the requested date, fetched for on-demand and latest-rate lookups. |
| `CurrencyAliases` | empty | Maps ISO codes to Fixer symbol components where they differ. |

## Dependency injection

The package ships its own `AddFixerExchangeRates` registration in the
`Bodu.Financial.ExchangeRates` namespace - there is no separate `*.DependencyInjection`
package.

```csharp
services
    .AddFinancialService(configuration)
    .AddFixerExchangeRates(configuration, configure: o => o.ApiKey = "…");
```

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
