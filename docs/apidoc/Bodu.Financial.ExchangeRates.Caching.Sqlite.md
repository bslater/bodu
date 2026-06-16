---
uid: Bodu.Financial.ExchangeRates.Caching.Sqlite
---

# Bodu.Financial.ExchangeRates.Caching.Sqlite

## Purpose

**Bodu.Financial.ExchangeRates.Caching.Sqlite** is a SQLite-backed persistent <xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache> for the [`Bodu.Financial`](Bodu.Financial.md) exchange-rate stack. <xref:Bodu.Financial.ExchangeRates.Caching.Sqlite.SqliteExchangeRateCache> persists one provider's dated rates and fetch-coverage windows in a SQLite database, so they survive process restarts and need not be re-fetched while fresh. It is behaviourally identical to the in-memory, TOML, and distributed caches in [`Bodu.Financial.ExchangeRates.Caching`](Bodu.Financial.ExchangeRates.Caching.md) — the same freshness, merge, coverage, and validation semantics, asserted against the same shared cache contract tests — so it drops in anywhere an `IExchangeRateCache` is expected, behind a <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider>.

Decimal rates are stored as invariant strings and dates and instants as invariant ISO text for lossless round-trips; same-pair writes are serialized under a per-pair lock and run in a transaction (so a reader never observes coverage without its rows), and a storage failure degrades to an empty read or skipped write rather than throwing. The cache holds one keep-alive connection for its lifetime and is `IDisposable`.

## Static documentation

- **[Caching and aggregating exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — the cache contract, the read-through decorator, and how a backend plugs in (see *Persistent and shared backends*).

## Key types

- <xref:Bodu.Financial.ExchangeRates.Caching.Sqlite.SqliteExchangeRateCache> — the SQLite `IExchangeRateCache`; dispose it to release the keep-alive connection.
- <xref:Bodu.Financial.ExchangeRates.Caching.Sqlite.SqliteExchangeRateCacheOptions> — the bound `Provider` and the SQLite connection (for example `DatabaseFilePath`).

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Caching;
using Bodu.Financial.ExchangeRates.Caching.Sqlite;

var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", DatabaseFilePath = "/var/cache/rba.db" };
using var cache = new SqliteExchangeRateCache(options);

// Front a source provider with read-through caching backed by SQLite.
IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
```

For dependency-injection wiring, see [`Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection`](Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection.md).
