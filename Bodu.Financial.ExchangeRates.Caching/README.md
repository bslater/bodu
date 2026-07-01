# Bodu.Financial.ExchangeRates.Caching

A caching and composition layer for `Bodu.Financial` exchange-rate providers.

> For the full walkthrough — quickstart, stacking (tiered read-through), aggregation,
> "when to use which", observability, and troubleshooting — see the
> [Caching and aggregating exchange rates guide](../docs/guides/financial/exchange-rate-caching.md).

The provider classes (Yahoo, RBA, ECB, BoE) are pure fetchers — they know nothing
of caching. This package adds two orthogonal pieces that each implement the same
`IDatedExchangeRateProvider` contract (and the timeless `IExchangeRateProvider`), so
they compose anywhere a provider is expected:

```
Caller
  │  IDatedExchangeRateProvider / IExchangeRateProvider
  ▼
AggregatingExchangeRateProvider     ── groups named children; routes per FX pair and
  │                                    combines them with a strategy (priority / average)
  ├── CachingExchangeRateProvider("RBA")  ── read-through cache over ONE source + ONE cache
  │       └── RbaExchangeRateProvider
  └── CachingExchangeRateProvider("ECB")
          └── EcbExchangeRateProvider
```

## Caching (one cache = one provider)

`CachingExchangeRateProvider` wraps exactly **one** inner `IDatedExchangeRateProvider`
over **one** single-provider `IExchangeRateCache`. On a lookup it first tries the cache's
fresh rows (reusing `FixedDatedExchangeRateProvider` for date-resolution, inverse, and
identity handling) and, on a miss, delegates to the inner provider and stores what it
returns. It also exposes the timeless `IExchangeRateProvider.GetRate(from, to)`, which
resolves the current UTC date under `CachingExchangeRateOptions.DefaultLookupOptions`.

The cache is bound to a single provider, so its surface carries no provider argument:

| Type | Role |
|---|---|
| `IExchangeRateCache` | Single-provider cache contract (`Provider`; `GetRates`/`Store` by pair). |
| `ExchangeRateCacheBase<TOptions>` | Storage-agnostic core: freshness filtering + merge/prune. No physical layout. |
| `IFileExchangeRateCache` | File-storage seam (`CacheDirectory`, `ResolveFilePath`, `ResolveDirectory`, `ResolvePartitionPath`). |
| `FileExchangeRateCacheBase<TOptions>` | File plumbing: layout-driven directory + file-name resolution, date partitioning, best-effort IO. |
| `TomlFileExchangeRateCache` | Sealed TOML leaf — `<dir>/<provider>/<from><to>.toml`, decimals quoted for lossless round-trips, self-describing `Provider`/`From`/`To` header. |
| `JsonFileExchangeRateCache` | Sealed JSON leaf — `<dir>/<provider>/<from><to>.json`, decimals as JSON numbers, the same self-describing header. |
| `ExchangeRateCacheFileLayout` | Where a pair's files live and whether they split by date: `SingleFile` (default), `Yearly`, `Monthly`, `Daily`, or `Create(strategy, directoryFunc?, fileNameFunc?)`. |
| `ExchangeRateCachePartitionStrategy` | The date split a layout applies: `Single`, `Yearly`, `Monthly`, `Daily`, or `Custom(...)`. |
| `InMemoryExchangeRateCache` | In-memory cache reusing the same expiry mechanism; nothing persisted. |
| `NullExchangeRateCache` | No-op cache (`NullExchangeRateCache.Create(provider)`). |
| `CachingExchangeRateProvider` | Read-through caching decorator over one source + one cache. |
| `CachingExchangeRateOptions` | Cache location, default + per-provider expiry, log levels, timeless lookup options. |

Craft your own storage by implementing `IExchangeRateCache`, extending
`ExchangeRateCacheBase<TOptions>` (storage-agnostic), or extending
`FileExchangeRateCacheBase<TOptions>` (a new file format).

### File layout and date partitioning

Both file caches store one file per pair by default
(`<dir>/<provider>/<from><to>.toml` or `.json`) and write a self-describing
`Provider`/`From`/`To` header into each file, so a file no longer depends on its
name or folder for identity. Set `FileExchangeRateCacheOptions.Layout` to control
the folder hierarchy, file name, and whether a pair's rows split across files by
date — `ExchangeRateCacheFileLayout.Yearly` / `.Monthly` / `.Daily` write one file
per calendar period under a per-pair folder (for example
`<dir>/RBA/AUDUSD/2023-01.toml`), and `ExchangeRateCacheFileLayout.Create(...)`
builds a custom layout from a partition strategy and optional directory/file-name
delegates:

```csharp
var monthly = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
{
    Provider = "RBA",
    CacheDirectory = "/var/cache/fx",
    Layout = ExchangeRateCacheFileLayout.Monthly,
});

// JSON instead of TOML, same layout/partitioning surface.
var json = new JsonFileExchangeRateCache(new FileExchangeRateCacheOptions
{
    Provider = "RBA",
    CacheDirectory = "/var/cache/fx",
});
```

## Aggregation (group many providers behind one entry point)

`AggregatingExchangeRateProvider` groups several named children and resolves each request
through a pluggable `IExchangeRateAggregationStrategy`, with optional **per-FX-pair routing**.

- `PriorityFallbackStrategy` — first child that resolves wins (the default).
- `AverageStrategy` — arithmetic mean of every child that resolves, tagged `Average`.
- Implement `IExchangeRateAggregationStrategy` for your own (weighted, median, …).
- `ExchangeRateAggregationOptions.Routes` maps a pair to an ordered child list and an
  optional per-pair strategy, so `AUD/USD` can prefer `[RBA, ECB]` while `USD/GBP`
  prefers `[ECB, RBA]`.
- `TryGetProvider(name, out provider)` resolves a specific child directly.

```csharp
// The caching provider is storage-agnostic — you supply the IExchangeRateCache.
var rba = new CachingExchangeRateProvider(
    rbaSource, new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" }), options);
var ecb = new CachingExchangeRateProvider(
    ecbSource, new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = "ECB", CacheDirectory = "/var/cache/fx" }), options);

var agg = new ExchangeRateAggregationOptions();
agg.Routes[new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
agg.Routes[new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.GBP)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });

IDatedExchangeRateProvider provider = new AggregatingExchangeRateProvider(
    new[]
    {
        new NamedDatedExchangeRateProvider("RBA", rba),
        new NamedDatedExchangeRateProvider("ECB", ecb),
    },
    agg);
```

For dependency-injection wiring, see
`Bodu.Financial.ExchangeRates.Caching.DependencyInjection`.

## Logging

Both the caching decorator and the aggregator log through `Microsoft.Extensions.Logging`.
Pass an `ILogger` to the constructor, or let the `*.DependencyInjection` package wire one
for you. When no logger is supplied it defaults to `NullLogger.Instance`, so logging is
entirely opt-in and free when unused.

Single-date lookups happen on the read hot path, so their hit/miss diagnostics default to
`Trace`; coarser range operations default to `Debug`. Every level is individually
configurable on `CachingExchangeRateOptions`:

| Event | Default level | Option property |
|---|---|---|
| A single-date lookup served from the cache | `Trace` | `CacheHitLogLevel` |
| A single-date cache miss resolved from a source and cached | `Trace` | `CacheMissLogLevel` |
| A range lookup served entirely from the cache | `Debug` | `CacheRangeHitLogLevel` |
| A range lookup refetched from a source and re-cached | `Debug` | `CacheRangeRefetchLogLevel` |

The aggregator's route-selected, aggregated, and unresolved diagnostics are configurable on
`ExchangeRateAggregationOptions`.

Persistent backends may add their own diagnostics. The SQLite cache logs best-effort **storage
degradation** at `Warning` under **`EventId 4520`** (first failure immediately, then rate-limited
to one per minute) so a silently-degrading cache is visible — see the
[`Bodu.Financial.ExchangeRates.Caching.Sqlite`](../Bodu.Financial.ExchangeRates.Caching.Sqlite/README.md)
package and the observability section of the
[caching guide](../docs/guides/financial/exchange-rate-caching.md#observability-seeing-hits-misses-and-degradation).

## Served-rate provenance & data age

Every `ExchangeRateLookupResult` carries an `ExchangeRateProvenance` describing where the rate came from:

- `Origin == Live` — the value was resolved directly by a provider (for a cache-fronted provider, a miss the inner
  provider satisfied). `Backend`, `CachedAtUtc`, and `Age` are all `null`.
- `Origin == Cache` — the value was served from a cache without consulting the provider. `Backend` is the cache's
  runtime identity, `CachedAtUtc` is the instant the served data was written to the cache, and `Age` is the elapsed
  time since then, clamped to be never negative (`Age >= 0`, since a row may be written marginally ahead of the lookup
  clock).

`Backend` is a **diagnostic** runtime identity (the cache type's name), not a stable key — do not parse it or branch on
it as if it were part of the contract.

Two ages travel with a cache-served rate and are deliberately distinct:

- **Cache-write age** — `Provenance.Age` (and `Provenance.CachedAtUtc`) is anchored to when the row was written to the
  cache.
- **Data age** — `ExchangeRate.FetchedAtUtc` carries the *upstream* fetch instant end to end. A provider stamps it when
  it loads a rate; the cache persists it (as `CachedExchangeRate.ObservedAtUtc`) and restores it onto the rate it
  serves, so a cache-served rate reports the **original** fetch instant. Data age is `now - ExchangeRate.FetchedAtUtc`,
  independent of how recently the row happened to be (re)written to the cache.

`FetchedAtUtc` is excluded from `ExchangeRate` equality. The TOML and JSON file caches persist it as an optional
`ObservedAtUtc` entry key per row; an entry written before the instant was tracked (or whose source never supplied one)
has no key and reads back `null`. The SQLite and distributed caches persist it the same way through their own additive
fields.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
