# Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection

Dependency-injection extensions for the distributed (Redis-capable) `Bodu.Financial` exchange-rate cache.

`AddDistributedRateCache` registers a per-provider `DistributedExchangeRateCache` as an `IExchangeRateCache`
(and as a keyed `IExchangeRateCache` under the provider name) over whatever `IDistributedCache` is already registered in
the container, binding and validating `DistributedExchangeRateCacheOptions` from configuration and an optional callback.

`AddRedisRateCache` is a convenience that first registers a Redis `IDistributedCache` (via
`Microsoft.Extensions.Caching.StackExchangeRedis`) and then registers the exchange-rate cache over it.

## Usage

Over an already-registered `IDistributedCache`:

```csharp
services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
services.AddBoduFinancial()
        .AddDistributedRateCache("RBA");

// Resolve the cache, or wrap a source provider with a CachingExchangeRateProvider over it.
var cache = provider.GetRequiredService<IExchangeRateCache>();
```

Or register the Redis cache and the exchange-rate cache together:

```csharp
services.AddBoduFinancial()
        .AddRedisRateCache("RBA", redis => redis.Configuration = "localhost:6379");
```

An optional key prefix may also be bound from configuration (default section
`Financial:ExchangeRateCache:Distributed`):

```json
{
  "Financial": {
    "ExchangeRateCache": {
      "Distributed": { "KeyPrefix": "fx:" }
    }
  }
}
```

The cache is registered as a singleton; both the default and the keyed `IExchangeRateCache` resolve to the same
instance. Options are validated lazily when the cache is first resolved, matching the other caching registrations.
