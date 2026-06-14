---
uid: Bodu.Financial.ExchangeRates.Caching
---

# Bodu.Financial.ExchangeRates.Caching

## Purpose

**Bodu.Financial.ExchangeRates.Caching** is the caching layer for the [`Bodu.Financial`](Bodu.Financial.md) exchange-rate provider stack. Rather than building caching into each provider, it adds a caching provider that sits *in front of* the fetch-only providers: <xref:Bodu.Financial.ExchangeRates.Caching.CachingDatedExchangeRateProvider> implements the same <xref:Bodu.Financial.IDatedExchangeRateProvider> contract the caller already resolves, wraps one or more named sources supplied at construction, serves fresh rates from a cache, and delegates downstream only on a miss.

Rates are persisted as **TOML**, one file per `(provider, currency pair)` (for example `Yahoo_AUDUSD.toml`), so a fresh rate survives process restarts. The cache owns expiry: each source has its own caching duration with a global default, the cache returns only fresh rows, and it prunes stale rows on write. Both single-date lookups and range lookups (`GetRatesAsync`) flow through the cache.

## Static documentation

- **[Caching exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — wrapping providers, per-provider expiry, single-date and range serving, the on-disk TOML format, and custom cache stores.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Caching.CachingDatedExchangeRateProvider> — the caching provider; wraps one or more named <xref:Bodu.Financial.IDatedExchangeRateProvider> sources (a caching composite when several are supplied) over a shared cache.
- <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProviderBase> — the abstract base holding the caching, staleness, and range logic; derived types supply the wrapped sources.
- <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateOptions> — cache location (`CacheDirectory`), `DefaultExpiry`, and the per-provider `ProviderExpiry` overrides; `GetExpiry(name)` resolves the effective duration.
- <xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache> — the cache contract: `GetRates` returns fresh rows; `Store` merges and prunes.
- <xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheBase> — shared merge / prune / freshness mechanism; derive and implement only raw entry persistence.
- <xref:Bodu.Financial.ExchangeRates.Caching.TomlFileSystemExchangeRateCache> — the TOML-on-disk cache (one file per provider and pair; decimals as quoted strings for lossless round-trips).
- <xref:Bodu.Financial.ExchangeRates.Caching.NullExchangeRateCache> — the no-op cache, for when on-disk caching is disabled.
- <xref:Bodu.Financial.ExchangeRates.Caching.CachedExchangeRate> — one cached row: observation `Date`, `Rate`, and the `CachedAtUtc` instant that drives expiry.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Caching;

var options = new CachingExchangeRateOptions
{
    CacheDirectory = "/var/cache/fx",
    DefaultExpiry = TimeSpan.FromHours(12),
};
options.ProviderExpiry["RBA"] = TimeSpan.FromDays(7);

var caching = new CachingDatedExchangeRateProvider(
    new[]
    {
        new KeyValuePair<string, IDatedExchangeRateProvider>("Yahoo", yahoo),
        new KeyValuePair<string, IDatedExchangeRateProvider>("RBA", rba),
    },
    options);

ExchangeRateLookupResult today = caching.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));
IReadOnlyList<ExchangeRate> january =
    await caching.GetRatesAsync("AUD", "USD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
```

For dependency-injection wiring, see [`Bodu.Financial.ExchangeRates.Caching.DependencyInjection`](Bodu.Financial.ExchangeRates.Caching.DependencyInjection.md) and the [caching guide](~/guides/financial/exchange-rate-caching.md).
