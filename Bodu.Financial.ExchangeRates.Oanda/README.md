# Bodu.Financial.ExchangeRates.Oanda

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by **OANDA's**
Historical Currency Converter, queried from its anonymous JSON endpoint.

`OandaRateProvider` derives from the arbitrary-pair `PairWebRateProvider`
base: it fetches a pair's history over a date range on demand and serves the results as
`Bodu.Financial.ExchangeRates.ExchangeRate` values through the standard `IDatedRateProvider`
and `IRateProvider` contracts — so it composes with `Money.ConvertTo`, the
caching and aggregating providers, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates;

using var provider = new OandaRateProvider(new OandaRateProviderOptions());

RateRangeResult series =
    await provider.GetRatesAsync("EUR", "USD", new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 12));
```

## Behaviour

- **Arbitrary pairs.** Any ISO 4217 pair OANDA publishes is resolvable directly; the reverse
  direction is inverted from the fetched series.
- **Rolling history window.** The anonymous endpoint serves only a rolling recent window —
  roughly the last **180 days**. The provider advertises this through
  `WebRateProvider.HistoryAvailability` (`RateHistoryAvailability.RollingDays(180)`),
  so a caller — or the caching / aggregation layer — can resolve the earliest date worth
  requesting rather than fetching a window that returns nothing. A request for an older date
  resolves against what the window can supply.
- **Configuration.** `OandaRateProviderOptions` carries working defaults (the Historical
  Currency Converter host and the 180-day window) and binds through
  `Microsoft.Extensions.Options`. The package ships its own `AddOandaExchangeRates`
  registration in the `Bodu.Financial.ExchangeRates` namespace.

## HTTP client and lifetime

The provider is `IDisposable` and offers two construction styles: `new
OandaRateProvider(options, ...)` — the provider builds, owns, and disposes its own
`HttpClient` — and `new OandaRateProvider(httpClient, options, ...)` — you supply
the client and own its lifetime. The second form is what the DI registration uses, backed
by `IHttpClientFactory`.

## Logging

The provider logs through `Microsoft.Extensions.Logging`. Pass an `ILogger` to the
constructor, or let the DI registration wire one for you; when no logger is supplied it
defaults to `NullLogger.Instance`, so logging is opt-in and free when unused.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
