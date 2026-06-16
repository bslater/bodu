---
uid: Bodu.Financial.ExchangeRates.Caching.Distributed
---

# Bodu.Financial.ExchangeRates.Caching.Distributed

## Purpose

**Bodu.Financial.ExchangeRates.Caching.Distributed** is a distributed (Redis-capable) <xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache> for the [`Bodu.Financial`](Bodu.Financial.md) exchange-rate stack. <xref:Bodu.Financial.ExchangeRates.Caching.Distributed.DistributedExchangeRateCache> persists one provider's dated rates and fetch-coverage windows over a `Microsoft.Extensions.Caching.Distributed.IDistributedCache`, so several application instances can share one warm cache — a fetch by one instance serves the others. It is behaviourally identical to the in-memory, TOML, and SQLite caches in [`Bodu.Financial.ExchangeRates.Caching`](Bodu.Financial.ExchangeRates.Caching.md) — the same freshness, merge, coverage, and validation semantics, asserted against the same shared cache contract tests — so it drops in behind a <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider>.

Each currency pair is stored as one JSON blob under a stable key; decimals are serialized as invariant strings and dates and instants as invariant ISO text for lossless round-trips. Because it depends only on the `IDistributedCache` abstraction it is unit-testable against `MemoryDistributedCache` and, in production, backed by Redis or any other implementation. Consistency is best-effort: same-process races are guarded by a per-pair lock, and the decorator's range write persists rows and coverage as one atomic blob, so a reader never observes coverage without its rows.

## Static documentation

- **[Caching and aggregating exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — the cache contract, the read-through decorator, and how a backend plugs in (see *Persistent and shared backends*).

## Key types

- <xref:Bodu.Financial.ExchangeRates.Caching.Distributed.DistributedExchangeRateCache> — the `IExchangeRateCache` over an `IDistributedCache`.
- <xref:Bodu.Financial.ExchangeRates.Caching.Distributed.DistributedExchangeRateCacheOptions> — the bound `Provider` and the key-prefix settings for the per-pair entries in the distributed store.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Caching;
using Bodu.Financial.ExchangeRates.Caching.Distributed;

var options = new DistributedExchangeRateCacheOptions { Provider = "RBA" };
var cache = new DistributedExchangeRateCache(distributedCache, options);   // any IDistributedCache

// Front a source provider with read-through caching shared across processes.
IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
```

For dependency-injection wiring (including a Redis convenience overload), see [`Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection`](Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection.md).
