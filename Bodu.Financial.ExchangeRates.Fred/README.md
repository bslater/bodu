# Bodu.Financial.ExchangeRates.Fred

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by **FRED**
(Federal Reserve Bank of St. Louis), served through its foreign-exchange REST series.

It resolves each currency pair to a single FRED series identifier, fetches that series'
observations, parses the JSON response, and serves the results as
`Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard
`IDatedRateProvider` and `IRateProvider` contracts - so it composes with
`Money.ConvertTo`, the caching and aggregating providers, and the rest of the
Bodu.Financial FX stack. The same interfaces and DI shape as every other provider, a
different data source.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new FredRateProvider(new FredRateProviderOptions { ApiKey = "…" });

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 6, 30));

RateLookupResult usd = provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
```

## Behaviour

- **Per-series mapping.** FRED publishes each foreign-exchange rate as an independent time
  series quoted in a fixed direction - for example `DEXUSEU` quotes US dollars per euro
  (`EUR/USD`). The `SeriesMap` on the options maps each currency pair to its FRED series id,
  keyed `FROM/TO` in the exact direction the series is quoted, so no inversion is required.
- **Unmapped pairs.** A pair with no entry in `SeriesMap` returns no data - without issuing a
  request. The reverse direction of a mapped pair resolves by inverting the mapped series only
  once that series is loaded: a request for the reverse pair itself is unmapped and fetches
  nothing, so warm the quoted direction first (for example `LoadPairAsync("EUR", "USD", ...)`
  before reading `USD/EUR`). To support additional pairs, add mappings via `SeriesMap`.
- **Built-in defaults.** The options seed a table of common USD pairs (EUR/USD, GBP/USD,
  AUD/USD, NZD/USD, USD/CAD, USD/JPY, USD/CHF, USD/CNY, USD/MXN, USD/INR, USD/SEK, USD/NOK,
  USD/DKK, USD/ZAR, USD/BRL, USD/KRW, USD/HKD, USD/SGD, USD/TWD).
- **Missing observations.** FRED encodes a day with no published value as the string `"."`;
  those observations are skipped.
- **API key required.** Set `ApiKey`; it is presented as the `api_key` query parameter.
- **No provider-local disk cache.** For durable caching, compose with
  `AddCachedRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package.

## FRED terms of use

FRED's [API terms of use](https://fred.stlouisfed.org/docs/api/terms_of_use.html) require an
application that uses the FRED API to place this notice prominently on the application:

> This product uses the FRED&reg; API but is not endorsed or certified by the Federal Reserve
> Bank of St. Louis.

The terms also forbid stating or implying that the application is endorsed by the Federal
Reserve Bank of St. Louis. Displaying the notice is the application's obligation; this package
cannot do it for you.

## Endpoint configuration

| Option | Default | Purpose |
|---|---|---|
| `ApiKey` | *(required)* | The FRED API key, sent as `api_key`. |
| `BaseAddress` | `https://api.stlouisfed.org/fred/` | The API host. |
| `ObservationsPath` | `series/observations` | The series-observations endpoint. |
| `SeriesMap` | built-in USD pairs | Maps each `FROM/TO` pair to a FRED series id. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand fetches from synchronous lookups. |
| `DefaultLookback` | 7 days | The look-back window, ending on the requested date, fetched for on-demand and latest-rate lookups. |
| `CurrencyAliases` | empty | Maps ISO codes to symbol components where they differ. |

## Dependency injection

The package ships its own `AddFredExchangeRates` registration in the
`Bodu.Financial.ExchangeRates` namespace - there is no separate `*.DependencyInjection`
package.

```csharp
services
    .AddFinancialService(configuration)
    .AddFredExchangeRates(configuration, configure: o => o.ApiKey = "…");
```

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
