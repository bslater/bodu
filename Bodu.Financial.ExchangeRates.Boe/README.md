# Bodu.Financial.ExchangeRates.Boe

A [Bodu.Financial](../Bodu.Financial) exchange-rate provider backed by the **Bank of
England's** daily spot exchange rates, queried from the Bank's Interactive Statistical
Database (IADB).

It builds an IADB CSV query for the configured series over a date range, parses the
response with the `Bodu.Text.Formats` RFC 4180 reader, and serves the results as
`Bodu.Financial.ExchangeRate` values through the standard `IDatedExchangeRateProvider`
and `IExchangeRateProvider` contracts — so it composes with `Money.ConvertTo`,
`CompositeDatedExchangeRateProvider`, and the rest of the Bodu.Financial FX stack.

```csharp
using Bodu.Financial.ExchangeRates.Boe;

var provider = new BoeExchangeRateProvider(httpClient, new BoeExchangeRateOptions());

// Warm the cache for a range (recommended), then look rates up synchronously.
await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2026, 6, 30));

ExchangeRateLookupResult usd = provider.GetRate("GBP", "USD", new DateOnly(2023, 1, 3));
// usd.Rate.Rate is the number of US dollars per pound on that date.

// Read a whole range at once (GBP-based pairs; the reverse direction is inverted).
IReadOnlyList<ExchangeRate> series =
    await provider.GetRatesAsync("GBP", "JPY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 12));

// Discover what pairs the loaded data supports.
foreach (BoeSeriesInfo info in provider.GetAvailablePairs())
    Console.WriteLine($"{info.Pair.FromIsoCode}/{info.Pair.ToIsoCode} ({info.SeriesCode})");
```

## Behaviour

- **GBP-based.** The Bank of England quotes each currency's daily spot rate against the
  pound (one IADB series per currency, for example `XUDLUSS` for the US dollar). Direct
  (`GBP→X`) and inverse (`X→GBP`) lookups are supported; cross pairs are not.
- **Range queries.** The IADB is queried by date range rather than by fixed file, so
  loading is range-based. `LoadRangeAsync` fetches an inclusive range; a synchronous lookup
  that misses an unloaded date blocks to download a bounded window around it (configurable
  via `OnDemandWindowDays`) when `AllowSynchronousNetworkAccess` is enabled (the default).
- **Caching.** Downloaded range responses are cached on disk (configurable) and refreshed
  on a TTL, since a range ending near today can gain an observation each business day.
- **Configuration.** `BoeExchangeRateOptions` carries working defaults and binds through
  `Microsoft.Extensions.Options`. The provider's connection to the IADB is grouped under
  its `Endpoint` (`BoeEndpointOptions`) — base URL, query path, HTTP timeout, and
  user-agent — so the query can be pointed at a mirror or proxy without touching caching or
  series configuration. See the `*.DependencyInjection` package for `AddBoeReferenceRates`.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
