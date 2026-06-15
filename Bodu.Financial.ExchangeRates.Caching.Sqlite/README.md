# Bodu.Financial.ExchangeRates.Caching.Sqlite

A SQLite-backed persistent cache for `Bodu.Financial` exchange-rate providers.

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

## Behaviour

* Expiry is by caching duration: stale and semantically invalid rows are filtered on read and pruned on write; stale
  coverage windows are pruned when coverage is recorded, so the database self-cleans.
* The two halves of a pair's state are written independently — storing rates never drops coverage, and recording
  coverage never drops rows.
* Single-process best-effort: same-pair writes are serialized under a per-pair lock and run in a transaction. A storage
  failure (`SqliteException` / `IOException`) degrades to an empty read or skipped write rather than throwing.

## Usage

```csharp
var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", DatabaseFilePath = "/var/cache/rba.db" };
using var cache = new SqliteExchangeRateCache(options);
IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
```

Or, through dependency injection (see `Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection`):

```csharp
services.AddBoduFinancial()
        .AddSqliteExchangeRateCache("RBA", configure: o => o.DatabaseFilePath = "/var/cache/rba.db");
```

A `SqliteExchangeRateCache` holds one keep-alive connection open for its lifetime so a shared in-memory database
(`Mode=Memory;Cache=Shared`) survives between operations; dispose the cache to release it.
