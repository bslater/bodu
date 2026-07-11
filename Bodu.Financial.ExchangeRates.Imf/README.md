# Bodu.Financial.ExchangeRates.Imf

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **IMF**
(International Monetary Fund) **SDMX Data API**, serving **daily** exchange rates.

It fetches the IMF SDMX data endpoint, parses the SDMX-JSON response, and serves the
results as `Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard
`IDatedRateProvider` and `IRateProvider` contracts — so it composes with `Money.ConvertTo`,
the caching and aggregating providers, and the rest of the Bodu.Financial FX stack. The
same interfaces and DI shape as every other provider, a different data source.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
// The IMF Data API is keyless — no API key is required.
using var provider = new ImfRateProvider(new ImfRateProviderOptions());

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("USD", "GBP", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

RateLookupResult gbp = provider.GetRate("USD", "GBP", new DateOnly(2023, 1, 3));
```

## Behaviour and limitations

- **Daily.** The provider requests the daily (`FREQ = D`) exchange-rate series from the IMF
  Exchange Rates (`ER`) dataflow.
- **Keyless.** The IMF Data API requires no API key; there is no `ApiKey` option.
- **USD/SDR-anchored pairs only.** The seeded series are the domestic-currency-per-USD rates
  (`ENDE_XDC_USD_RATE`); the model is USD/SDR-anchored, so each pair must involve USD (the
  reverse direction is served by the inverse-lookup fallback).
- **Unmapped pairs return no data.** Only pairs present in `SeriesMap` (or their reverse)
  resolve. An unmapped pair yields no observations without issuing a request. Extend
  `SeriesMap` to add more pairs.
- **Series mapping.** A pair is resolved to an SDMX series key
  (`{freq}.{area}.{indicator}`, for example `D.GB.ENDE_XDC_USD_RATE` for `USD/GBP`)
  through `SeriesMap`, keyed `FROM/TO`. The request's date range is sent as `YYYY-MM-DD`
  `startPeriod` / `endPeriod` parameters against
  `{DataPath}/{Dataflow}/{DataVersion}/{seriesKey}`, requesting SDMX-JSON via the `Accept`
  header.
- **No provider-local disk cache.** For durable caching, compose with
  `AddCachedRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package.

## Endpoint configuration

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://api.imf.org/external/sdmx/3.0/` | The SDMX Data API host. |
| `DataPath` | `data/dataflow` | The SDMX data-resource path segment. |
| `Dataflow` | `IMF.STA/ER` | The agency-qualified SDMX dataflow (Exchange Rates). |
| `DataVersion` | `+` | The dataflow version selector (latest). |
| `SeriesMap` | seeded | Maps `FROM/TO` to a daily SDMX series key; extend to add pairs. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand fetches from synchronous lookups. |
| `DefaultLookback` | 7 days | The window fetched around a date for on-demand and latest-rate lookups. |
| `CurrencyAliases` | empty | Maps ISO codes to source symbol components where they differ. |

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
