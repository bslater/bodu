# Bodu.Financial.ExchangeRates.Caching.Sqlite

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A SQLite-backed persistent cache for `Bodu.Financial` exchange-rate providers.

> For the full composition walkthrough (quickstart, stacking, aggregation,
> observability, troubleshooting) see the
> [Caching and aggregating exchange rates guide](../docs/guides/financial/exchange-rate-caching.md); for a
> cross-host cache, see the
> [`Bodu.Financial.ExchangeRates.Caching.Distributed`](../Bodu.Financial.ExchangeRates.Caching.Distributed/README.md)
> backend.

`SqliteExchangeRateCache` implements the `IExchangeRateCache` contract over a SQLite database, persisting one
provider's dated rates and fetch-coverage windows so they need not be re-fetched while fresh. It is behaviourally
identical to the in-memory and TOML caches in `Bodu.Financial.ExchangeRates.Caching` — the same freshness, merge,
coverage, and validation semantics — and is validated against the same shared `ExchangeRateCacheContractTests`.

## Storage

* A `rates` table keyed by `(provider, from_code, to_code, obs_date)`, one row per dated observation (UPSERT on store).
* A `coverage` table of `(provider, from_code, to_code, start_date, end_date, fetched_at)` allowing multiple fetch
  windows per pair.
* Decimal rates are stored as invariant strings and all dates and instants as invariant ISO text (`yyyy-MM-dd` for
  dates, round-trip `"O"` for instants) so precision and scale round-trip losslessly.
* The `rates` table carries an additive `observed_at TEXT NULL` column holding the upstream fetch instant
  (`ExchangeRate.FetchedAtUtc`), distinct from the `cached_at` cache-write instant. A pre-existing database created
  before the column was added is migrated idempotently on open (`ALTER TABLE ... ADD COLUMN`, a no-op when already
  present); rows from before the migration, or whose source supplied no fetch instant, store and read back `null`.

## Behaviour

* Expiry is by caching duration: stale and semantically invalid rows are filtered on read and pruned on write; stale
  coverage windows are pruned when coverage is recorded, so the database self-cleans.
* The independent half-writes preserve the other half — `Store` never drops coverage, and `RecordCoverage` never drops
  rows — while `StoreFetchedRange` (the path the `CachingExchangeRateProvider` decorator uses) rewrites both the `rates`
  and `coverage` tables for the pair in **one transaction**, so a reader never observes coverage without its rows. An
  empty-but-fetched range still records its coverage window so it is not perpetually re-fetched. The write reports an
  `ExchangeRateCacheWriteStatus` (`Stored` / `Failed` / `Skipped`).
* Single-process best-effort: same-pair writes are serialized under a per-pair lock and run in a transaction. A storage
  failure (`SqliteException` / `IOException`) degrades to an empty read or skipped write rather than throwing.

Because the persisted `observed_at` is restored onto a served rate's `ExchangeRate.FetchedAtUtc`, a cache-served rate
reports its **original** upstream fetch instant (data age), distinct from the cache-write age surfaced through
`ExchangeRateLookupResult.Provenance` (`CachedAtUtc` / `Age`). See the served-rate provenance notes in the
`Bodu.Financial.ExchangeRates.Caching` README.

## Usage

```csharp
var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", DatabaseFilePath = "/var/cache/rba.db" };
using var cache = new SqliteExchangeRateCache(options);
IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
```

One cache instance serves **every currency pair** for its provider — the store is keyed by
`(provider, from_code, to_code, obs_date)`, so a single `SqliteExchangeRateCache` holds `AUD/USD`, `GBP/USD`, and any
other pair the provider returns. There is never a cache per pair.

### Several providers in one database (without DI)

Because `provider` is the leading key column, several single-provider caches can share **one** database file with no
collisions — each provider's series stays partitioned. Construct one cache per provider over the same
`DatabaseFilePath` and wrap each in its own `CachingExchangeRateProvider`:

```csharp
using Bodu.Financial.ExchangeRates.Caching;

var options = new CachingExchangeRateOptions { DefaultExpiry = TimeSpan.FromHours(24) };

// One shared .db file, one cache per provider; each cache covers all of that provider's pairs.
using var rbaCache = new SqliteExchangeRateCache("RBA", "/var/cache/fx.db");
using var ofxCache = new SqliteExchangeRateCache("OFX", "/var/cache/fx.db");

IDatedExchangeRateProvider rba = new CachingExchangeRateProvider(rbaSource, rbaCache, options);
IDatedExchangeRateProvider ofx = new CachingExchangeRateProvider(ofxSource, ofxCache, options);
```

Each cache holds its own keep-alive connection and per-pair locks, so dispose every cache you create. To group several
sources behind one entry point with fallback / averaging / per-pair routing, or to stack a fast in-memory tier in front
of the SQLite tier, see the
[exchange-rate caching guide](../docs/guides/financial/exchange-rate-caching.md) (*Stacking providers* and *Grouping
providers with the aggregator*).

### Through dependency injection

The package ships its own `AddSqliteRateCache` registration in the `Bodu.Financial.ExchangeRates` namespace; call it
once per provider, pointing each at the same file to share one database:

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;

services.AddFinancialService()
        .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = "/var/cache/fx.db")
        .AddSqliteRateCache("OFX", configure: o => o.DatabaseFilePath = "/var/cache/fx.db");
```

A `SqliteExchangeRateCache` holds one keep-alive connection open for its lifetime so a shared in-memory database
(`Mode=Memory;Cache=Shared`) survives between operations; dispose the cache to release it.
