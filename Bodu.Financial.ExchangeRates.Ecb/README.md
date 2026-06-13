# Bodu.Financial.ExchangeRates.Ecb

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **European
Central Bank's** published euro foreign-exchange reference rates.

It downloads the ECB `eurofxref` XML feeds, parses them, and serves the results as
`Bodu.Financial.ExchangeRate` values through the standard `IDatedExchangeRateProvider`
and `IExchangeRateProvider` contracts — so it composes with `Money.ConvertTo`,
`CompositeDatedExchangeRateProvider`, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates.Ecb;

var provider = new EcbExchangeRateProvider(httpClient, new EcbExchangeRateOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate is the number of US dollars per euro on that date.

// Read a whole range at once (EUR-based pairs; the reverse direction is inverted).
IReadOnlyList<ExchangeRate> series =
    await provider.GetRatesAsync("EUR", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (EcbSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.FromIsoCode}/{info.Pair.ToIsoCode}");
```

## Behaviour

- **EUR-based.** The ECB quotes the euro against each currency. Direct (`EUR→X`) and
  inverse (`X→EUR`) lookups are supported; cross pairs are not.
- **Feeds.** The ECB publishes overlapping `eurofxref` files that each end at the most
  recent business day and reach back a different distance: a rolling 90-day file and the
  full history since 1999 (a latest-day file is also available via
  `EcbExchangeRateFeed.Daily`). The provider loads the narrowest feed that covers the
  dates you ask for, minimizing bandwidth.
- **Loading.** Call `PreloadAsync` / `LoadRangeAsync` to warm the in-memory store. A
  synchronous lookup that misses an unloaded date will block to download its covering feed
  when `AllowSynchronousNetworkAccess` is enabled (the default).
- **Caching.** Downloaded files are cached on disk (configurable); because every feed
  extends to the latest business day, each is refreshed on a TTL.
- **Configuration.** `EcbExchangeRateOptions` carries working defaults and binds through
  `Microsoft.Extensions.Options`. The provider's connection to the ECB is grouped under
  its `Endpoint` (`EcbEndpointOptions`) — base URL, HTTP timeout, and user-agent — so the
  feeds can be pointed at a mirror or proxy without touching caching or feed selection. See
  the `*.DependencyInjection` package for `AddEcbReferenceRates`.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
