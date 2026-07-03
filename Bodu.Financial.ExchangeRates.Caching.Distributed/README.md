# Bodu.Financial.ExchangeRates.Caching.Distributed

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A distributed (Redis-capable) cache for `Bodu.Financial` exchange-rate providers.

> One `IExchangeRateCache` backend among several. For the composition model, the
> [`SqliteExchangeRateCache`](../Bodu.Financial.ExchangeRates.Caching.Sqlite/README.md)
> alternative, and "when to use which", see the
> [Caching and aggregating exchange rates guide](../docs/guides/financial/exchange-rate-caching.md).

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
* Each rate row carries an additive optional `observedAtUtc` JSON property holding the upstream fetch instant
  (`ExchangeRate.FetchedAtUtc`), distinct from the row's cache-write instant. It is omitted from the JSON when `null`; a
  legacy blob written before the property existed (or a row whose source supplied no fetch instant) reads back `null`.

## Behaviour

* Expiry is by caching duration: stale and semantically invalid rows are filtered on read and pruned on write; stale
  coverage windows are pruned when coverage is recorded, so the entry self-cleans.
* The two **independent** half-writes preserve the other half: `Store` writes rate rows without dropping coverage, and
  `RecordCoverage` writes coverage windows without dropping rows — each by read-modify-writing the per-pair blob.
* `StoreFetchedRange` — the path the `CachingExchangeRateProvider` decorator uses after a range fetch — writes **both**
  halves together as one atomic blob set: the pair's rate rows and the fetched coverage window are merged and persisted
  in a single `Set`, all-or-nothing. A reader (even in another process) therefore never observes coverage without its
  rows, so a range lookup cannot report a false hit and return incomplete data as if complete. The write returns an
  `ExchangeRateCacheWriteStatus` (`Stored` when both halves were persisted, `Failed` when a backing-store error was
  swallowed and nothing was persisted, `Skipped` for a no-op cache), which the decorator logs and, on `Failed`, treats
  as a miss so the next lookup refetches rather than trusting partial coverage.
* An empty-but-fetched range still records coverage: a successful fetch that returned no observation (a weekend, a
  holiday, a true gap) marks the window covered, so it is served from the cache on a later lookup rather than being
  perpetually re-fetched.
* Consistency is best-effort. An `IDistributedCache` offers no cross-process atomic read-modify-write, so each write
  reads the per-pair blob, modifies it, and writes it back. Same-process races are prevented by a per-pair in-process
  lock. **Cross-process**, the independent `Store` / `RecordCoverage` half-writes are last-write-wins, while the
  decorator's `StoreFetchedRange` blob set is atomic per write (each writer persists a self-consistent rows-plus-coverage
  blob, so a concurrent overwrite loses an update but never tears a half into another writer's blob). A backing-store
  failure or a corrupt blob degrades to an empty read or skipped write rather than throwing.

Because the persisted `observedAtUtc` is restored onto a served rate's `ExchangeRate.FetchedAtUtc`, a cache-served rate
reports its **original** upstream fetch instant (data age), distinct from the cache-write age surfaced through
`ExchangeRateLookupResult.Provenance` (`CachedAtUtc` / `Age`). See the served-rate provenance notes in the
`Bodu.Financial.ExchangeRates.Caching` README.

## When to use

Reach for this distributed (Redis-backed) cache when several application instances or processes share one rate cache,
so a fetch by one instance warms the others. For a single process, prefer the SQLite, file (TOML), or in-memory caches
in `Bodu.Financial.ExchangeRates.Caching` (and `…Caching.Sqlite`): they offer stronger local atomicity — the per-pair
lock for the in-memory and file caches, and one transaction for SQLite — for every write path, including the independent
`Store` / `RecordCoverage` halves.

## Usage

```csharp
var options = new DistributedExchangeRateCacheOptions { Provider = "RBA" };
var cache = new DistributedExchangeRateCache(distributedCache, options);
IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
```

Or, through dependency injection (the package ships its own `AddDistributedRateCache` / `AddRedisRateCache` registration in the `Bodu.Financial.ExchangeRates` namespace):

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;

// Over an already-registered IDistributedCache:
services.AddFinancialService()
        .AddDistributedRateCache("RBA");

// Or register a Redis IDistributedCache and the cache together:
services.AddFinancialService()
        .AddRedisRateCache("RBA", redis => redis.Configuration = "localhost:6379");
```
