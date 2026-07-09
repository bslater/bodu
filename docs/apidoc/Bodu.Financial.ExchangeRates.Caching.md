---
uid: Bodu.Financial.ExchangeRates.Caching
---

![Bodu.Financial.ExchangeRates.Caching](~/images/hero-fx-caching.svg)

# Bodu.Financial.ExchangeRates.Caching

## Purpose

**Bodu.Financial.ExchangeRates.Caching** is the caching and composition layer for the [`Bodu.Financial`](Bodu.Financial.md) exchange-rate provider stack. Rather than building caching or grouping into each provider, it ships two orthogonal pieces that each implement the same <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> contract (and the timeless <xref:Bodu.Financial.ExchangeRates.IRateProvider>), so they drop in anywhere a provider is expected:

- **A read-through cache, one cache per provider.** <xref:Bodu.Financial.ExchangeRates.Caching.CachingRateProvider> wraps exactly one inner source over one single-provider cache. It serves fresh cached rates and delegates to the source only on a miss, then caches what the source returns.
- **An aggregator that groups many providers.** <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider> groups named children behind one entry point, combining them through a pluggable strategy with optional per-currency-pair routing.

![The caller talks to an AggregatingRateProvider, which routes each FX pair to a CachingRateProvider; each caching provider reads through its own cache (SQLite, in-memory, …) and calls its concrete source only on a miss.](../images/exchange-rate-caching-architecture.svg)

The cache owns expiry: each provider has its own caching duration with a global default, the cache returns only fresh rows, and it prunes stale rows on write. Both single-date lookups and range lookups (`GetRatesAsync`) flow through the cache.

Alongside the in-memory, TOML-file, and JSON-file caches, two persistent backends now live in this same namespace: <xref:Bodu.Financial.ExchangeRates.Caching.SqliteRateCache> (a SQLite database) and <xref:Bodu.Financial.ExchangeRates.Caching.DistributedRateCache> (any `Microsoft.Extensions.Caching.Distributed.IDistributedCache`, including Redis). Both are behaviourally identical to the built-in caches — the same freshness, merge, coverage, and validation semantics, asserted against the same shared cache contract tests — so each drops in anywhere an `IRateCache` is expected, behind a <xref:Bodu.Financial.ExchangeRates.Caching.CachingRateProvider>. Each cache stays bound to one provider, but several single-provider SQLite caches may share one database file — the leading `provider` key column keeps their series partitioned — and caching providers can be stacked (a fast in-memory tier over a durable SQLite one); see the guide.

All dependency-injection registration lives in the `Bodu.Financial.ExchangeRates` namespace, so a single `using Bodu.Financial.ExchangeRates;` makes `AddCachedRateProvider`, `AddAggregatedRateProvider`, `AddSqliteRateCache`, `AddDistributedRateCache`, and `AddRedisRateCache` available. The SQLite and distributed backends ship their registration inside their own runtime packages; there are no separate `*.DependencyInjection` packages.

## Static documentation

- **[Caching and aggregating exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — the cache cascade, one-cache-per-provider read-through, the aggregator's strategies and per-pair routing, the on-disk TOML and JSON formats, file layouts and date partitioning, custom storage, and dependency injection.

## Key types

**Storage (a cache is bound to one provider)**

- <xref:Bodu.Financial.ExchangeRates.Caching.IRateCache> — the single-provider cache contract: a bound `Provider`, and `GetRates`/`Store` keyed by currency pair.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheBase`1> — the storage-agnostic core: read-time freshness filtering and write-time merge-and-prune, prescribing no physical layout. Extend it for a non-file store.
- <xref:Bodu.Financial.ExchangeRates.Caching.IFileRateCache>, <xref:Bodu.Financial.ExchangeRates.Caching.FileRateCacheBase`1> — the file-storage seam and its plumbing: layout-driven directory and file-name resolution, optional date partitioning, and best-effort IO. Extend the base for a new file format; it exposes `ResolveFilePath`, `ResolveDirectory`, and `ResolvePartitionPath`.
- <xref:Bodu.Financial.ExchangeRates.Caching.TomlFileRateCache> — the sealed TOML leaf (`<directory>/<provider>/<from><to>.toml` by default; decimals quoted for lossless round-trips; a self-describing `Provider`/`From`/`To` header).
- <xref:Bodu.Financial.ExchangeRates.Caching.JsonFileRateCache> — the sealed JSON leaf (`<directory>/<provider>/<from><to>.json` by default; decimals as JSON numbers; the same self-describing header). Both file leaves honour the configured layout, including date partitioning.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheFileLayout> — describes where a pair's rows are stored: the folder hierarchy, the file name, and (through its partition strategy) whether the rows split across files by date. Built-ins `SingleFile` (default), `Yearly`, `Monthly`, `Daily`, plus `Create(strategy, directoryFunc?, fileNameFunc?)` for custom folder and file-name rules.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCachePartitionStrategy> — decides how a pair's rows split across files by date: `Single`, `Yearly`, `Monthly`, `Daily`, or `Custom(keySelector, rangeSelector)` for arbitrary periods.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheDirectoryContext>, <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheFileContext> — the inputs passed to a custom layout's directory and file-name delegates.
- <xref:Bodu.Financial.ExchangeRates.Caching.InMemoryRateCache> — an in-memory cache reusing the same expiry mechanism; nothing is persisted.
- <xref:Bodu.Financial.ExchangeRates.Caching.SqliteRateCache>, <xref:Bodu.Financial.ExchangeRates.Caching.SqliteRateCacheOptions> — a persistent SQLite-backed cache (one provider's rates and coverage windows in a SQLite database) and its options (bound `Provider`, `DatabaseFilePath`). Registered with `AddSqliteRateCache`.
- <xref:Bodu.Financial.ExchangeRates.Caching.DistributedRateCache>, <xref:Bodu.Financial.ExchangeRates.Caching.DistributedRateCacheOptions> — a shared cache over any `IDistributedCache` (Redis-capable), so several application instances share one warm cache, and its options (bound `Provider`, key-prefix settings). Registered with `AddDistributedRateCache` or the Redis convenience `AddRedisRateCache`.
- <xref:Bodu.Financial.ExchangeRates.Caching.NullRateCache> — the no-op cache (`NullRateCache.Create(provider)`), for when caching is disabled.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheOptions>, <xref:Bodu.Financial.ExchangeRates.Caching.FileRateCacheOptions> — the storage-agnostic options (bound `Provider`) and the file options (adds `CacheDirectory` and the `Layout`).
- <xref:Bodu.Financial.ExchangeRates.Caching.CachedRate> — one cached row: observation `Date`, `Rate`, and the `CachedAtUtc` instant that drives expiry.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheEntry> — the serializable on-disk row model: observation `Date`, `Rate`, `CachedAtUtc`, and optional `ObservedAtUtc`.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheCoverageEntry> — a persisted coverage window: the inclusive `Start` / `End` dates fetched and the `FetchedAtUtc` instant they were retrieved.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheFile> — the file document the file backends serialize: a self-describing `Provider`/`From`/`To` header plus an `Entries` list of rows and a `Coverage` list of fetched windows.
- <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheWriteStatus> — the outcome of a cache write (`Stored`, `Skipped`, `Failed`).

**Read-through caching decorator**

- <xref:Bodu.Financial.ExchangeRates.Caching.CachingRateProvider> — wraps one inner source over one cache; implements both the dated and timeless surfaces.
- <xref:Bodu.Financial.ExchangeRates.Caching.CachingRateProviderBase> — the abstract base holding the read-through, staleness, and range logic; derived types supply the wrapped inner provider.
- <xref:Bodu.Financial.ExchangeRates.Caching.CachingRateOptions> — cache location (`CacheDirectory`), `DefaultExpiry`, the per-provider `ProviderExpiry` overrides, the per-event log levels, and `DefaultLookupOptions` for the timeless surface.

**Aggregation (group many providers)**

- <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider> — groups named children, applies a strategy and per-pair routing, and exposes `TryGetProvider` for direct access to a named child.
- <xref:Bodu.Financial.ExchangeRates.Caching.IRateAggregationStrategy> — the strategy seam; implement it for weighted, median, or first-non-stale combination.
- <xref:Bodu.Financial.ExchangeRates.Caching.PriorityFallbackStrategy> — first child that resolves wins (the default; successor to the former composite provider).
- <xref:Bodu.Financial.ExchangeRates.Caching.AverageStrategy> — arithmetic mean of every child that resolves, tagged with a synthetic provider label.
- <xref:Bodu.Financial.ExchangeRates.Caching.CurrencyPairRoute>, <xref:Bodu.Financial.ExchangeRates.Caching.RateAggregationOptions> — a per-pair ordered child list (with an optional pair-specific strategy) and the aggregator options that hold the default strategy, default order, and the route map.
- <xref:Bodu.Financial.ExchangeRates.Caching.NamedDatedRateProvider> — pairs a child provider with the name it is referenced by in routing and diagnostics.
- <xref:Bodu.Financial.ExchangeRates.Caching.IAggregatedRateBuilder> — the fluent builder surface for composing an aggregator in DI: `AddCachedChild` (by provider type or factory), `UseDefaultStrategy`, and `MapPair` for per-pair provider ordering and optional pair-specific strategies.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Caching;

var options = new CachingRateOptions
{
    CacheDirectory = "/var/cache/fx",
    DefaultExpiry = TimeSpan.FromHours(12),
};
options.ProviderExpiry["RBA"] = TimeSpan.FromDays(7);

// One cache per provider — the provider is storage-agnostic, so you pick the cache.
var rbaCached = new CachingRateProvider(
    rba, new TomlFileRateCache(new FileRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" }), options);
var ecbCached = new CachingRateProvider(
    ecb, new TomlFileRateCache(new FileRateCacheOptions { Provider = "ECB", CacheDirectory = "/var/cache/fx" }), options);

// Group them with per-FX-pair routing.
var aggregation = new RateAggregationOptions();
aggregation.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "RBA", "ECB" });
aggregation.Routes[new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP)] = new CurrencyPairRoute(new[] { "ECB", "RBA" });

IDatedRateProvider provider = new AggregatingRateProvider(
    new[]
    {
        new NamedDatedRateProvider("RBA", rbaCached),
        new NamedDatedRateProvider("ECB", ecbCached),
    },
    aggregation);

RateLookupResult today = provider.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));
```

For dependency-injection wiring, add `using Bodu.Financial.ExchangeRates;` and call `AddCachedRateProvider`, `AddAggregatedRateProvider`, `AddSqliteRateCache`, `AddDistributedRateCache`, or `AddRedisRateCache`. See the [caching guide](~/guides/financial/exchange-rate-caching.md) for the full walkthrough.
