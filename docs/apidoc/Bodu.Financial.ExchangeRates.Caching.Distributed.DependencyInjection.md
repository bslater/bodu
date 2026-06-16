---
uid: Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [distributed exchange-rate cache](Bodu.Financial.ExchangeRates.Caching.Distributed.md). Both methods on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> bind <xref:Bodu.Financial.ExchangeRates.Caching.Distributed.DistributedExchangeRateCacheOptions> (default section `Financial:ExchangeRateCache:Distributed`) and register a singleton <xref:Bodu.Financial.ExchangeRates.Caching.Distributed.DistributedExchangeRateCache> bound to the provider, resolvable as <xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache> and as a keyed `IExchangeRateCache` under the provider name; options validate on start:

- `AddDistributedExchangeRateCache(name, …)` binds over the `IDistributedCache` already registered in the container (Redis, in-memory, SQL Server, …).
- `AddRedisExchangeRateCache(name, configureRedis, …)` is a convenience that first registers a Redis `IDistributedCache` (via `AddStackExchangeRedisCache`) and then the exchange-rate cache over it.

## Static documentation

- **[Caching and aggregating exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — see *Persistent and shared backends*.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection.DistributedExchangeRateCacheServiceBuilderExtensions> — the registration surface:
  - `AddDistributedExchangeRateCache(providerName, configuration?, sectionName?, configure?)` — over an existing `IDistributedCache`.
  - `AddRedisExchangeRateCache(configureRedis, providerName, configuration?, sectionName?, configure?)` — registers Redis and the cache together.

## Minimal sample

```csharp
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching;
using Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

// Over an already-registered IDistributedCache.
services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
services.AddBoduFinancial()
        .AddDistributedExchangeRateCache("RBA");

// Or register Redis and the cache together.
services.AddBoduFinancial()
        .AddRedisExchangeRateCache("RBA", redis => redis.Configuration = "localhost:6379");

var cache = provider.GetRequiredService<IExchangeRateCache>();
```

See the [caching guide](~/guides/financial/exchange-rate-caching.md) for the read-through decorator the cache backs.
