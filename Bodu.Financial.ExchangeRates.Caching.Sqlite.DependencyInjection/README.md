# Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection

Dependency-injection extensions for the SQLite-backed `Bodu.Financial` exchange-rate cache.

`AddSqliteRateCache` registers a per-provider `SqliteExchangeRateCache` as an `IExchangeRateCache` (and as a
keyed `IExchangeRateCache` under the provider name), binding and validating `SqliteExchangeRateCacheOptions` from
configuration and an optional callback.

## Usage

```csharp
services.AddFinancialService()
        .AddSqliteRateCache("RBA", configure: o => o.DatabaseFilePath = "/var/cache/rba.db");

// Resolve the cache, or wrap a source provider with a CachingExchangeRateProvider over it.
var cache = provider.GetRequiredService<IExchangeRateCache>();
```

The database location may also be bound from configuration (default section `Financial:ExchangeRateCache:Sqlite`):

```json
{
  "Financial": {
    "ExchangeRateCache": {
      "Sqlite": { "DatabaseFilePath": "/var/cache/rba.db" }
    }
  }
}
```

The cache is registered as a singleton; the container disposes it (and its keep-alive connection) on shutdown.
Options are validated lazily when the cache is first resolved, matching the other caching registrations.
