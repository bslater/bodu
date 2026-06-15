# Bodu.Financial.ExchangeRates.Caching

A caching and composition layer for `Bodu.Financial` exchange-rate providers.

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
| `IFileExchangeRateCache` | File-storage seam (`CacheDirectory`, `ResolveFilePath`). |
| `FileExchangeRateCacheBase<TOptions>` | File plumbing: per-provider subdirectory, file-name resolution, best-effort IO. |
| `TomlFileExchangeRateCache` | Sealed TOML leaf — `<dir>/<provider>/<from><to>.toml`, decimals quoted for lossless round-trips. |
| `InMemoryExchangeRateCache` | In-memory cache reusing the same expiry mechanism; nothing persisted. |
| `NullExchangeRateCache` | No-op cache (`NullExchangeRateCache.Create(provider)`). |
| `CachingExchangeRateProvider` | Read-through caching decorator over one source + one cache. |
| `CachingExchangeRateOptions` | Cache location, default + per-provider expiry, log levels, timeless lookup options. |

Craft your own storage by implementing `IExchangeRateCache`, extending
`ExchangeRateCacheBase<TOptions>` (storage-agnostic), or extending
`FileExchangeRateCacheBase<TOptions>` (a new file format).

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
var rba = new CachingExchangeRateProvider("RBA", rbaSource, options);
var ecb = new CachingExchangeRateProvider("ECB", ecbSource, options);

var agg = new ExchangeRateAggregationOptions();
agg.Routes[new ExchangeRatePair("AUD", "USD")] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
agg.Routes[new ExchangeRatePair("USD", "GBP")] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });

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

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
