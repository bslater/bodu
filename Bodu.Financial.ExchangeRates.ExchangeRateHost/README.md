# Bodu.Financial.ExchangeRates.ExchangeRateHost

> **API stability - Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **exchangerate.host**
(`api.exchangerate.host`) foreign-exchange REST service.

It fetches the exchangerate.host `timeframe` and single-date endpoints, parses the JSON
response, and serves the results as `Bodu.Financial.ExchangeRates.ExchangeRate` values
through the standard `IDatedRateProvider` and `IRateProvider` contracts - so it composes
with `Money.ConvertTo`, the caching and aggregating providers, and the rest of the
Bodu.Financial FX stack. The same interfaces and DI shape as every other provider, a
different data source.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new ExchangeRateHostRateProvider(new ExchangeRateHostRateProviderOptions { ApiKey = "…" });

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 6, 30));

RateLookupResult usd = provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

## Behaviour

- **Source + quote.** exchangerate.host denominates its response against a source currency
  and returns the requested quote currencies, keyed by the concatenated `{SOURCE}{QUOTE}`
  currency code (for example `USDEUR`). A pair is fetched by denominating against the source
  currency and requesting the destination currency as the quote currency.
- **Endpoint selection.** A one-day request uses the single-date endpoint (the date is
  carried in the `date` query parameter); a multi-day range uses the `timeframe` endpoint
  (the bounds are carried in `start_date` and `end_date`).
- **Plan limits.** The free plan supports a USD source currency and single-date requests;
  changing the source currency and the `timeframe` (multi-day) endpoint require a paid plan
  (source-currency switching from the Basic plan, time-frame queries from the Professional
  Plus plan). A request the account's plan does not permit surfaces as a fetch failure, not a
  pre-empted request. On a plan without time-frame queries, warm one day at a time
  (`LoadPairAsync` with equal start and end dates): the on-demand and latest-rate lookups
  fetch the `DefaultLookback` window, a multi-day request. On the free plan, request pairs
  whose source currency is USD; a request for the reverse pair sends that pair's own source
  currency, so to read `EUR/USD` warm `USD/EUR` first, and its reverse then resolves by
  inversion from the loaded series.
- **API key required.** Set `ApiKey`; it is presented as the `access_key` query parameter.
- **No provider-local disk cache.** For durable caching, compose with
  `AddCachedRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package.

## Endpoint configuration

| Option | Default | Purpose |
|---|---|---|
| `ApiKey` | *(required)* | The exchangerate.host access key, sent as `access_key`. |
| `BaseAddress` | `https://api.exchangerate.host/` | The API host. |
| `TimeSeriesPath` | `timeframe` | The multi-day time-frame endpoint. |
| `HistoricalPath` | `historical` | The single-date endpoint (date sent as `date`). |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand fetches from synchronous lookups. |
| `DefaultLookback` | 7 days | The look-back window, ending on the requested date, fetched for on-demand and latest-rate lookups. |
| `CurrencyAliases` | empty | Maps ISO codes to exchangerate.host symbol components where they differ. |

## Dependency injection

The package ships its own `AddExchangeRateHostExchangeRates` registration in the
`Bodu.Financial.ExchangeRates` namespace - there is no separate `*.DependencyInjection`
package.

```csharp
services
    .AddFinancialService(configuration)
    .AddExchangeRateHostExchangeRates(configuration, configure: o => o.ApiKey = "…");
```

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
