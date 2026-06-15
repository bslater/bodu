# Bodu.Financial.ExchangeRates.Caching.Distributed

A distributed (Redis-capable) cache for `Bodu.Financial` exchange-rate providers.

`DistributedExchangeRateCache` implements the `IExchangeRateCache` contract over a
`Microsoft.Extensions.Caching.Distributed.IDistributedCache`, persisting one provider's dated rates and fetch-coverage
windows so they need not be re-fetched while fresh. It is behaviourally identical to the in-memory, TOML, and SQLite
caches in `Bodu.Financial.ExchangeRates.Caching` — the same freshness, merge, coverage, and validation semantics — and
is validated against the same shared `ExchangeRateCacheContractTests`.

Because it depends only on the `IDistributedCache` abstraction it is fully unit-testable in-memory (against
`MemoryDistributedCache`) and, in production, backed by Redis (via
`Microsoft.Extensions.Caching.StackExchangeRedis`) or any other `IDistributedCache` implementation.

## Storage

* One entry per currency pair, under a stable, collision-free key `{prefix}{provider}:{from}{to}`.
* The value is a single JSON blob carrying both the cached rate rows and the recorded coverage windows.
* Decimal rates are serialized as invariant strings and all dates and instants as invariant ISO text (`yyyy-MM-dd` for
  dates, round-trip `"O"` for instants) so precision and scale round-trip losslessly.

## Behaviour

* Expiry is by caching duration: stale and semantically invalid rows are filtered on read and pruned on write; stale
  coverage windows are pruned when coverage is recorded, so the entry self-cleans.
* The two halves of a pair's state are written independently — storing rates never drops coverage, and recording
  coverage never drops rows — by read-modify-writing the per-pair blob.
* Best-effort: an `IDistributedCache` offers no atomic read-modify-write, so the per-pair blob is read, modified, and
  written back. Same-process races are prevented by a per-pair in-process lock; **cross-process** concurrent writes to
  the same pair are last-write-wins. A backing-store failure or a corrupt blob degrades to an empty read or skipped
  write rather than throwing.

## Usage

```csharp
var options = new DistributedExchangeRateCacheOptions { Provider = "RBA" };
var cache = new DistributedExchangeRateCache(distributedCache, options);
IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
```

Or, through dependency injection (see `Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection`):

```csharp
// Over an already-registered IDistributedCache:
services.AddBoduFinancial()
        .AddDistributedExchangeRateCache("RBA");

// Or register a Redis IDistributedCache and the cache together:
services.AddBoduFinancial()
        .AddRedisExchangeRateCache("RBA", redis => redis.Configuration = "localhost:6379");
```
