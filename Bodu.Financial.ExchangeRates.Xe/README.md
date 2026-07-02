# Bodu.Financial.ExchangeRates.Xe

> **API stability — Experimental.** The public API surface and behaviour are still evolving and may change or be removed without a major-version bump.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **XE.com**
charting-rates JSON service.

It fetches the XE `api/protected/charting-rates` endpoint, decodes the delta-encoded
series, and serves the results as `Bodu.Financial.ExchangeRate` values through the
standard `IDatedExchangeRateProvider` and `IExchangeRateProvider` contracts — so it
composes with `Money.ConvertTo`, the caching and aggregating providers, and the rest of
the Bodu.Financial FX stack. It is a logical sister to
[`Bodu.Financial.ExchangeRates.Yahoo`](../Bodu.Financial.ExchangeRates.Yahoo): the same
interfaces and DI shape, a different data source.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new XeExchangeRateProvider(new XeExchangeRateOptions());

// Warm a pair for a range (recommended), then look rates up synchronously.
await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

ExchangeRateLookupResult usd = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));

// Read a whole range at once.
ExchangeRateRangeResult series =
    await provider.GetRatesAsync("AUD", "JPY", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
```

## Authorization

The XE charting-rates endpoint requires an `Authorization: Basic` token that XE does not
publish as a stable credential. The provider acquires it automatically (an
`IXeAuthTokenProvider`): it fetches the bootstrap page, scans the `_next` script chunks it
references for a credential built by a `btoa(...)` call next to a `Basic ` literal — for
example `e.set("Authorization", `` `Basic ${btoa("user:secret")}` `` `)` — and base64-encodes
it. When no referenced chunk matches, lazily-loaded chunk URLs reconstructed from the
webpack runtime's chunk map are scanned as a fallback. The token is cached and refreshed
once when the endpoint rejects it (`401`/`403`). This depends on the XE website's current
structure and is therefore **inherently brittle**; the package carries no affiliation with
or endorsement by XE.

## Behaviour

- **Arbitrary pairs.** XE serves any pair directly through the `fromCurrency` /
  `toCurrency` query parameters, so there is no base-currency restriction. The inverse
  direction is served directly or, if only the reverse series is loaded, inverted.
- **Server-determined window.** XE returns a fixed extended window per request rather than
  honouring an explicit date range; the response is range-filtered to the dates you ask
  for, so a request for dates outside the returned window resolves to no rate.
- **Delta decoding.** XE encodes the series as deltas from a baseline carried in the first
  element of the `rates` array; the provider decodes each point with the same rounding the
  XE site applies. When the returned window has sub-daily granularity, the last point of
  each calendar day is the one retained.
- **Loading.** Call `LoadPairAsync` to warm the in-memory store. A synchronous lookup that
  misses an un-fetched pair will block to fetch a window around the requested date only
  when `AllowSynchronousNetworkAccess` is enabled (it is `false` by default).
- **No provider-local disk cache.** The provider keeps only an in-memory store of the pairs
  and windows it has fetched this session. For durable caching across processes, compose it
  with the generic caching provider — `AddCachedExchangeRateProvider<…>` from the
  [`Bodu.Financial.ExchangeRates.Caching`](../Bodu.Financial.ExchangeRates.Caching) package.

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles:

- `new XeExchangeRateProvider(options, ...)` — the provider builds, owns, and disposes its
  own `HttpClient`, created via `ExchangeRateHttpClientFactory.Create` from the configured
  user agent and timeout. Dispose the provider (for example with `using`) to release the
  client.
- `new XeExchangeRateProvider(httpClient, options, ...)` — you supply the client and own its
  lifetime; the provider never disposes a client it did not create. This is the form the
  `*.DependencyInjection` package uses, backed by `IHttpClientFactory`.

## Endpoint configuration

`XeExchangeRateOptions` is centred on configuring the REST endpoint and token acquisition:

| Option | Default | Purpose |
|---|---|---|
| `BaseAddress` | `https://www.xe.com/` | The API host. |
| `ChartingRatesPath` | `api/protected/charting-rates/` | The charting-rates endpoint path. |
| `AuthBootstrapUrl` | `https://www.xe.com/currencycharts` | Page whose referenced script chunks are scanned for the token. |
| `AuthScriptBaseUrl` | `https://www.xe.com/_next/` | Base URL for reconstructed lazily-loaded chunk names. |
| `UserAgent` | browser-like | XE rejects requests without a recognizable user agent. |
| `HttpTimeout` | 30 s | Applied to the `HttpClient` the provider creates, or by the DI registration. |
| `AllowSynchronousNetworkAccess` | `false` | Opt in to blocking on-demand fetches from synchronous lookups. |
| `DefaultLookback` | 7 days | The window fetched around a date for on-demand and latest-rate lookups. |
| `CurrencyAliases` | empty | Maps ISO codes to XE currency codes where they differ. |

## Dependency injection

The package ships its own `AddXeExchangeRates` registration in the
`Bodu.Financial.ExchangeRates` namespace — there is no separate `*.DependencyInjection`
package.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the `*.DependencyInjection` package wire one for you (category
`Bodu.Financial.ExchangeRates.XeExchangeRateProvider`). When no logger is supplied it
defaults to `NullLogger.Instance`, so logging is entirely opt-in and free when unused.

| Event | Default level | Option property |
|---|---|---|
| A pair download is starting | `Debug` | `DownloadStartingLogLevel` |
| A pair loaded (with its observation count) | `Information` | `DownloadCompletedLogLevel` |
| Each individual rate observation ingested | `Trace` | `ObservationIngestedLogLevel` |
| A pair download failed (logged, then re-thrown) | `Warning` | `DownloadFailedLogLevel` |
| A synchronous lookup triggered a blocking network fetch | `Warning` | `SynchronousNetworkFetchLogLevel` |

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
