# Bodu.Financial.ExchangeRates.Ofx

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by **OFX's**
(ofx.com) spot-rate history, queried from their public JSON endpoint.

Unlike the central-bank providers, OFX serves arbitrary ISO currency pairs, so
`OfxExchangeRateProvider` derives from the arbitrary-pair `PairWebExchangeRateProvider`
base: it fetches a pair's history over a date range on demand and serves the results as
`Bodu.Financial.ExchangeRate` values through the standard `IDatedExchangeRateProvider`
and `IExchangeRateProvider` contracts — so it composes with `Money.ConvertTo`, the
caching and aggregating providers, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates;

// The provider builds and owns its HttpClient from the options; dispose it to release the client.
using var provider = new OfxExchangeRateProvider(new OfxExchangeRateOptions());

ExchangeRateRangeResult series =
    await provider.GetRatesAsync("AUD", "USD", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

ExchangeRateLookupResult latest = provider.GetRate("AUD", "USD", new DateOnly(2026, 6, 12));
```

## Behaviour

- **Arbitrary pairs.** Any ISO 4217 pair OFX publishes is resolvable directly; the reverse
  direction is inverted from the fetched series.
- **Range queries.** History is fetched per pair over a date range and accumulated, with
  per-pair coverage tracking and single-flight coalescing inherited from the pair base, so
  concurrent lookups for the same pair share one download.
- **Configuration.** `OfxExchangeRateOptions` carries working defaults and binds through
  `Microsoft.Extensions.Options`. The package ships its own `AddOfxExchangeRates`
  registration in the `Bodu.Financial.ExchangeRates` namespace.

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles: `new
OfxExchangeRateProvider(options, ...)` — the provider builds, owns, and disposes its own
`HttpClient` — and `new OfxExchangeRateProvider(httpClient, options, ...)` — you supply the
client and own its lifetime. The second form is what the DI registration uses, backed by
`IHttpClientFactory`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the DI registration wire one for you; when no logger is supplied it
defaults to `NullLogger.Instance`, so logging is opt-in and free when unused.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
