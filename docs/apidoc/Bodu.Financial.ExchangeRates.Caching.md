---
uid: Bodu.Financial.ExchangeRates.Caching
---

# Bodu.Financial.ExchangeRates.Caching

## Purpose

**Bodu.Financial.ExchangeRates.Caching** is the caching and composition layer for the [`Bodu.Financial`](Bodu.Financial.md) exchange-rate provider stack. Rather than building caching or grouping into each provider, it ships two orthogonal pieces that each implement the same <xref:Bodu.Financial.IDatedExchangeRateProvider> contract (and the timeless <xref:Bodu.Financial.IExchangeRateProvider>), so they drop in anywhere a provider is expected:

- **A read-through cache, one cache per provider.** <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider> wraps exactly one inner source over one single-provider cache. It serves fresh cached rates and delegates to the source only on a miss, then caches what the source returns.
- **An aggregator that groups many providers.** <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider> groups named children behind one entry point, combining them through a pluggable strategy with optional per-currency-pair routing.

The cache owns expiry: each provider has its own caching duration with a global default, the cache returns only fresh rows, and it prunes stale rows on write. Both single-date lookups and range lookups (`GetRatesAsync`) flow through the cache.

Alongside the in-memory and TOML-file caches, two persistent backends now live in this same namespace: <xref:Bodu.Financial.ExchangeRates.Caching.SqliteExchangeRateCache> (a SQLite database) and <xref:Bodu.Financial.ExchangeRates.Caching.DistributedExchangeRateCache> (any `Microsoft.Extensions.Caching.Distributed.IDistributedCache`, including Redis). Both are behaviourally identical to the built-in caches — the same freshness, merge, coverage, and validation semantics, asserted against the same shared cache contract tests — so each drops in anywhere an `IExchangeRateCache` is expected, behind a <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider>.

All dependency-injection registration lives in the `Microsoft.Extensions.DependencyInjection` namespace, so a single `using Microsoft.Extensions.DependencyInjection;` makes `AddCachedExchangeRateProvider`, `AddAggregatedExchangeRateProvider`, `AddSqliteRateCache`, `AddDistributedRateCache`, and `AddRedisRateCache` available. The SQLite and distributed backends ship their registration inside their own runtime packages; there are no separate `*.DependencyInjection` packages.

## Static documentation

- **[Caching and aggregating exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — the cache cascade, one-cache-per-provider read-through, the aggregator's strategies and per-pair routing, the on-disk TOML format, custom storage, and dependency injection.

## Key types

**Storage (a cache is bound to one provider)**

- <xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache> — the single-provider cache contract: a bound `Provider`, and `GetRates`/`Store` keyed by currency pair.
- <xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheBase`1> — the storage-agnostic core: read-time freshness filtering and write-time merge-and-prune, prescribing no physical layout. Extend it for a non-file store.
- <xref:Bodu.Financial.ExchangeRates.Caching.IFileExchangeRateCache>, <xref:Bodu.Financial.ExchangeRates.Caching.FileExchangeRateCacheBase`1> — the file-storage seam and its plumbing: per-provider subdirectory layout, file-name resolution, and best-effort IO. Extend the base for a new file format.
- <xref:Bodu.Financial.ExchangeRates.Caching.TomlFileExchangeRateCache> — the sealed TOML leaf (`<directory>/<provider>/<from><to>.toml`; decimals quoted for lossless round-trips).
- <xref:Bodu.Financial.ExchangeRates.Caching.InMemoryExchangeRateCache> — an in-memory cache reusing the same expiry mechanism; nothing is persisted.
- <xref:Bodu.Financial.ExchangeRates.Caching.SqliteExchangeRateCache>, <xref:Bodu.Financial.ExchangeRates.Caching.SqliteExchangeRateCacheOptions> — a persistent SQLite-backed cache (one provider's rates and coverage windows in a SQLite database) and its options (bound `Provider`, `DatabaseFilePath`). Registered with `AddSqliteRateCache`.
- <xref:Bodu.Financial.ExchangeRates.Caching.DistributedExchangeRateCache>, <xref:Bodu.Financial.ExchangeRates.Caching.DistributedExchangeRateCacheOptions> — a shared cache over any `IDistributedCache` (Redis-capable), so several application instances share one warm cache, and its options (bound `Provider`, key-prefix settings). Registered with `AddDistributedRateCache` or the Redis convenience `AddRedisRateCache`.
- <xref:Bodu.Financial.ExchangeRates.Caching.NullExchangeRateCache> — the no-op cache (`NullExchangeRateCache.Create(provider)`), for when caching is disabled.
- <xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheOptions>, <xref:Bodu.Financial.ExchangeRates.Caching.FileExchangeRateCacheOptions> — the storage-agnostic options (bound `Provider`) and the file options (adds `CacheDirectory`).
- <xref:Bodu.Financial.ExchangeRates.Caching.CachedExchangeRate> — one cached row: observation `Date`, `Rate`, and the `CachedAtUtc` instant that drives expiry.

**Read-through caching decorator**

- <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider> — wraps one inner source over one cache; implements both the dated and timeless surfaces.
- <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProviderBase> — the abstract base holding the read-through, staleness, and range logic; derived types supply the wrapped inner provider.
- <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateOptions> — cache location (`CacheDirectory`), `DefaultExpiry`, the per-provider `ProviderExpiry` overrides, the per-event log levels, and `DefaultLookupOptions` for the timeless surface.

**Aggregation (group many providers)**

- <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider> — groups named children, applies a strategy and per-pair routing, and exposes `TryGetProvider` for direct access to a named child.
- <xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateAggregationStrategy> — the strategy seam; implement it for weighted, median, or first-non-stale combination.
- <xref:Bodu.Financial.ExchangeRates.Caching.PriorityFallbackStrategy> — first child that resolves wins (the default; successor to the former composite provider).
- <xref:Bodu.Financial.ExchangeRates.Caching.AverageStrategy> — arithmetic mean of every child that resolves, tagged with a synthetic provider label.
- <xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRatePairRoute>, <xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateAggregationOptions> — a per-pair ordered child list (with an optional pair-specific strategy) and the aggregator options that hold the default strategy, default order, and the route map.
- <xref:Bodu.Financial.ExchangeRates.Caching.NamedDatedExchangeRateProvider> — pairs a child provider with the name it is referenced by in routing and diagnostics.

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

// One cache per provider.
var rbaCached = new CachingExchangeRateProvider("RBA", rba, options);
var ecbCached = new CachingExchangeRateProvider("ECB", ecb, options);

// Group them with per-FX-pair routing.
var aggregation = new ExchangeRateAggregationOptions();
aggregation.Routes[new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
aggregation.Routes[new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.GBP)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });

IDatedExchangeRateProvider provider = new AggregatingExchangeRateProvider(
    new[]
    {
        new NamedDatedExchangeRateProvider("RBA", rbaCached),
        new NamedDatedExchangeRateProvider("ECB", ecbCached),
    },
    aggregation);

ExchangeRateLookupResult today = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));
```

For dependency-injection wiring, add `using Microsoft.Extensions.DependencyInjection;` and call `AddCachedExchangeRateProvider`, `AddAggregatedExchangeRateProvider`, `AddSqliteRateCache`, `AddDistributedRateCache`, or `AddRedisRateCache`. See the [caching guide](~/guides/financial/exchange-rate-caching.md) for the full walkthrough.
