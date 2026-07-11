# Bodu.Financial.ExchangeRates.Imf

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **IMF**
(International Monetary Fund) SDMX-JSON CompactData REST service.

It fetches the IMF CompactData endpoint, parses the SDMX-JSON response, and serves the
results as `Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard
`IDatedRateProvider` and `IRateProvider` contracts — so it composes with `Money.ConvertTo`,
the caching and aggregating providers, and the rest of the Bodu.Financial FX stack. The
same interfaces and DI shape as every other provider, a different data source.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
// The IMF service is keyless — no API key is required.
using var provider = new ImfRateProvider(new ImfRateProviderOptions());

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("USD", "GBP", new DateOnly(2023, 1, 1), new DateOnly(2023, 6, 1));

RateLookupResult gbp = provider.GetRate("USD", "GBP", new DateOnly(2023, 1, 1));
```

## Limitations

- **Keyless.** The IMF SDMX-JSON service requires no API key; there is no `ApiKey` option.
- **Monthly, not daily.** IMF observations are **monthly** (the IFS end-of-period rate).
  Each observation maps to the **first day of its month**, so request and look up rates on
  month boundaries. This provider is not a source of daily fixings.
- **USD/SDR-anchored pairs only.** The seeded series are the IFS
  domestic-currency-per-USD rates (`ENDE_XDC_USD_RATE`); the model is USD/SDR-anchored.
  Arbitrary daily cross pairs are **not serviceable** from this feed.
- **Unmapped pairs return no data.** Only pairs present in `SeriesMap` (or their reverse,
  served by the inverse-lookup fallback) resolve. An unmapped pair yields no observations
  without issuing a request. Extend `SeriesMap` to add more pairs.
- **No provider-local disk cache.** For durable caching, compose with
  `AddCachedRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package.

## Behaviour

- **Series mapping.** A pair is resolved to an SDMX series key
  (`{freq}.{area}.{indicator}`, for example `M.GB.ENDE_XDC_USD_RATE` for `USD/GBP`)
  through `SeriesMap`, keyed `FROM/TO`.
- **Request window.** The request's date range is projected to `YYYY-MM` `startPeriod` and
  `endPeriod` query parameters against `{CompactDataPath}/{Dataflow}/{seriesKey}`.
- **Response shape.** The parser handles the SDMX-JSON nesting
  `CompactData` → `DataSet` → `Series` → `Obs`, where `Series` and `Obs` may each be a
  single object or an array.

## Endpoint configuration

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://dataservices.imf.org/REST/SDMX_JSON.svc/` | The API host. |
| `CompactDataPath` | `CompactData` | The CompactData resource path segment. |
| `Dataflow` | `IFS` | The SDMX dataflow (International Financial Statistics). |
| `SeriesMap` | seeded | Maps `FROM/TO` to an SDMX series key; extend to add pairs. |
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
