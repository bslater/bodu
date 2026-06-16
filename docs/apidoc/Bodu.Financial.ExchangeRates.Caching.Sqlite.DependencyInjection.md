---
uid: Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [SQLite exchange-rate cache](Bodu.Financial.ExchangeRates.Caching.Sqlite.md). `AddSqliteExchangeRateCache(name, …)` on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> binds <xref:Bodu.Financial.ExchangeRates.Caching.Sqlite.SqliteExchangeRateCacheOptions> (default section `Financial:ExchangeRateCache:Sqlite`) and registers a singleton <xref:Bodu.Financial.ExchangeRates.Caching.Sqlite.SqliteExchangeRateCache> bound to the provider, resolvable as <xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache> and as a keyed `IExchangeRateCache` under the provider name. The single instance (and its keep-alive connection) is disposed by the container on shutdown, and options validate on start.

## Static documentation

- **[Caching and aggregating exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — see *Persistent and shared backends*.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection.SqliteExchangeRateCacheServiceBuilderExtensions> — `AddSqliteExchangeRateCache(providerName, configuration?, sectionName?, configure?)` on the financial service builder.

## Minimal sample

```csharp
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching;
using Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddBoduFinancial()
        .AddSqliteExchangeRateCache("RBA", configure: o => o.DatabaseFilePath = "/var/cache/rba.db");

// Resolve the cache (or the keyed cache for "RBA") and wrap a source with a CachingExchangeRateProvider over it.
var cache = provider.GetRequiredService<IExchangeRateCache>();
```

See the [caching guide](~/guides/financial/exchange-rate-caching.md) for the read-through decorator the cache backs.
