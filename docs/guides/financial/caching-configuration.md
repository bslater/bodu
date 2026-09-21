---
title: Configuring rate caching from appsettings
---

# Configuring rate caching from appsettings

The caching layer binds four option types from one configuration subtree rooted at
`Financial:RateCache`. This page lays out that tree: which registration binds which section into
which type, every key with its default, a complete JSON example per backend, and the lifetimes
and thread-safety guarantees of the singletons those registrations create. For what the cache
*does* — expiry, coverage, stacking, aggregation strategies — see
[Caching and aggregating exchange rates](exchange-rate-caching.md); for the providers underneath,
see [Configuring providers from appsettings](provider-configuration.md).

## The section tree at a glance

| Section | Bound into | Bound by | Options kind |
|---|---|---|---|
| `Financial:RateCache` | <xref:Bodu.Financial.ExchangeRates.Caching.CachingRateOptions> | `AddCachedRateProvider` **and** `AddAggregatedRateProvider` | one unnamed instance shared by every cached provider and every aggregation child |
| `Financial:RateCache:Sqlite` | <xref:Bodu.Financial.ExchangeRates.Caching.SqliteRateCacheOptions> | `AddSqliteRateCache` | one **named** instance per provider name, all bound from the same section |
| `Financial:RateCache:Distributed` | <xref:Bodu.Financial.ExchangeRates.Caching.DistributedRateCacheOptions> | `AddDistributedRateCache` / `AddRedisRateCache` | one named instance per provider name, same section |
| `Financial:RateCacheWarmup` | <xref:Bodu.Financial.ExchangeRates.Caching.RateCacheWarmupOptions> | `AddRateCacheWarmup` | one unnamed instance |

Every registration takes the same trailing parameters — `IConfiguration? configuration`,
`string sectionName` (the default above), and an `Action<TOptions>? configure` that runs after
binding — and validates the bound options on start through the type's `TryValidate`. Pass the
same `builder.Configuration` to each call and the whole tree is read from one place:

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddFinancialService(builder.Configuration)
    .AddRbaExchangeRates(builder.Configuration)                              // Financial:Rba
    .AddCachedRateProvider<RbaRateProvider>("RBA", builder.Configuration);    // Financial:RateCache
```

The nested sections are deliberate: `Financial:RateCache` describes the *policy* (expiry,
logging, stampede protection) that every backend shares, while `Financial:RateCache:Sqlite` and
`Financial:RateCache:Distributed` describe the *storage* — so a configuration file that names a
SQLite path does not have to repeat the policy, and switching backends changes one subsection.

## Pattern 1 — `Financial:RateCache` and `CachingRateOptions`

Every key below binds, shown at its default. Durations are `TimeSpan` strings
(`d.hh:mm:ss`), log levels are `Microsoft.Extensions.Logging.LogLevel` names:

```json
{
  "Financial": {
    "RateCache": {
      "CacheDirectory": null,
      "DefaultExpiry": "1.00:00:00",
      "ProviderExpiry": {},
      "RespectHistoryAvailability": true,
      "SkipInverseRangeProbeWhenDirectCovered": false,
      "ExpiryJitter": 0.0,
      "RefreshAheadFraction": 0.0,
      "CacheHitLogLevel": "Information",
      "CacheMissLogLevel": "Information",
      "CacheRangeHitLogLevel": "Debug",
      "CacheRangeRefetchLogLevel": "Debug",
      "HistoryClampLogLevel": "Debug",
      "RateProvenanceLogLevel": "Debug"
    }
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `CacheDirectory` | `null` | Root of the default single-file TOML cache that `AddCachedRateProvider` builds when no `cacheFactory` is supplied; `null` resolves to `bodu-exchange-rates` under the system temporary path. Ignored by the SQLite and distributed backends. |
| `DefaultExpiry` | 24 h | How long a cached row (and a recorded coverage window) is served before it is re-fetched. Must be positive. |
| `ProviderExpiry` | `{}` | Per-provider overrides keyed by provider name (`"RBA": "7.00:00:00"`); entries merge into the map. `GetExpiry(name)` resolves the effective value. |
| `RespectHistoryAvailability` | `true` | Skip or clamp fetches for dates the inner provider has declared unavailable. |
| `SkipInverseRangeProbeWhenDirectCovered` | `false` | Skip the second backend read that probes the inverse pair on a range miss when the direct pair already has coverage. |
| `ExpiryJitter` | `0` | Fraction in `[0, 1)` shaved deterministically off each pair's expiry so warmed pairs do not all expire together. |
| `RefreshAheadFraction` | `0` | Fraction in `[0, 1)` of the expiry after which a hit also schedules one background refresh (stale-while-revalidate). |
| `*LogLevel` | see block | The level of each cache event; `None` suppresses it. |

`DefaultLookupOptions` — the <xref:Bodu.Financial.ExchangeRates.RateLookupOptions> the
*timeless* surface applies — is not configuration-bindable (its members are read-only) and
stays at `RateLookupOptions.Exact` whatever the section says. Set it, like anything the binder
cannot express, in the `configure` callback:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddRbaExchangeRates(builder.Configuration)
    .AddCachedRateProvider<RbaRateProvider>(
        "RBA",
        builder.Configuration,
        configure: cache =>
        {
            cache.DefaultLookupOptions = RateLookupOptions.PreviousWithin(3);
            cache.ProviderExpiry["RBA"] = TimeSpan.FromDays(7);
        });
```

> [!NOTE]
> `AddCachedRateProvider` and `AddAggregatedRateProvider` both bind the same unnamed
> `CachingRateOptions`, so every cached provider and every aggregation child in a host shares
> one policy object. Use `ProviderExpiry` for per-provider expiry; there is no per-provider
> section.

## Pattern 2 — `Financial:RateCache:Sqlite` and `SqliteRateCacheOptions`

`AddSqliteRateCache("RBA", configuration)` binds the section into a **named** options instance
(`"RBA"`), sets `Provider` to that name after binding, validates, and registers a
<xref:Bodu.Financial.ExchangeRates.Caching.SqliteRateCache> keyed by the name — exposed as the
keyed `IRateCache` for `"RBA"` and, for the first cache registered, as the default `IRateCache`.
The cached provider then picks it up through `cacheFactory`:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddRbaExchangeRates(builder.Configuration)
    .AddSqliteRateCache("RBA", builder.Configuration)                        // Financial:RateCache:Sqlite
    .AddCachedRateProvider<RbaRateProvider>(
        "RBA",
        builder.Configuration,
        cacheFactory: (sp, name) => sp.GetRequiredKeyedService<IRateCache>(name));
```

```json
{
  "Financial": {
    "RateCache": {
      "DefaultExpiry": "1.00:00:00",
      "Sqlite": {
        "DatabaseFilePath": "/var/cache/myapp/fx.db",
        "ConnectionString": null,
        "UseWriteAheadLogging": true,
        "BusyTimeout": "00:00:05",
        "ThrowOnStorageFailure": false,
        "ValidateStorageOnStart": false
      }
    }
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `DatabaseFilePath` | `null` | The database file; created with its schema on first use. **One of** `DatabaseFilePath` or `ConnectionString` is required. |
| `ConnectionString` | `null` | Takes precedence over the path (shared in-memory databases, custom flags). Presence only is validated; a bad string surfaces at connect time as a best-effort degradation. |
| `UseWriteAheadLogging` | `true` | WAL journal mode, so readers run alongside a writer; creates `.db-wal` / `.db-shm` sidecars. Disable on file systems that cannot honour WAL (NFS/SMB). |
| `BusyTimeout` | 5 s | How long a connection waits for a peer's write lock before giving up (and, being best-effort, dropping the write). Non-negative. |
| `ThrowOnStorageFailure` | `false` | Surface a storage failure as an exception instead of an empty read / skipped write. |
| `ValidateStorageOnStart` | `false` | Probe the database during `ValidateOnStart`, so an unwritable path fails the host rather than the first lookup. |
| `Provider` | set by the registration | Do not put it in the section: whatever is bound is overwritten with the registration's provider name. |

Because every name binds the *same* section, several providers naturally share one database
file — the intended layout: rows are keyed by provider, so `AddSqliteRateCache("RBA", …)` and
`AddSqliteRateCache("ECB", …)` partition cleanly inside `fx.db`. Give one provider a different
file through its `configure` callback, which runs after binding for that name only.

## Pattern 3 — `Financial:RateCache:Distributed` and `DistributedRateCacheOptions`

The distributed backend stores each pair as one blob in whatever `IDistributedCache` the host
has registered. `AddRedisRateCache` registers the Redis `IDistributedCache` first and then
delegates to `AddDistributedRateCache`; note the Redis configurator is the *first* parameter and
the provider name the second:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddEcbExchangeRates(builder.Configuration)
    .AddRedisRateCache(
        redis => redis.Configuration = builder.Configuration.GetConnectionString("RateCache"),
        "ECB",
        builder.Configuration)                                               // Financial:RateCache:Distributed
    .AddCachedRateProvider<EcbRateProvider>(
        "ECB",
        builder.Configuration,
        cacheFactory: (sp, name) => sp.GetRequiredKeyedService<IRateCache>(name));
```

With any other `IDistributedCache` (SQL Server, the in-memory one for tests), register it
yourself and call `AddDistributedRateCache`:

```csharp
builder.Services.AddDistributedMemoryCache();                                // any IDistributedCache

builder.Services
    .AddFinancialService(builder.Configuration)
    .AddEcbExchangeRates(builder.Configuration)
    .AddDistributedRateCache("ECB", builder.Configuration, configure: o => o.KeyPrefix = "fx:")
    .AddCachedRateProvider<EcbRateProvider>(
        "ECB",
        builder.Configuration,
        cacheFactory: (sp, name) => sp.GetRequiredKeyedService<IRateCache>(name));
```

```json
{
  "Financial": {
    "RateCache": {
      "DefaultExpiry": "12:00:00",
      "Distributed": {
        "EntryExpirationMargin": "01:00:00",
        "ThrowOnStorageFailure": false,
        "ValidateStorageOnStart": false
      }
    }
  },
  "ConnectionStrings": {
    "RateCache": "redis.internal:6379,abortConnect=false"
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `KeyPrefix` | `null` (omit the key) | Prepended verbatim to every key so unrelated tenants of one store cannot collide; unset, keys begin with the provider name. A non-null, all-white-space value fails validation — and a JSON `null` binds as an **empty string**, which is exactly that, so leave the key out of the section rather than writing `"KeyPrefix": null`. |
| `EntryExpirationMargin` | 1 h | Every blob is stamped with a server-side lifetime of the caching duration plus this margin, so an idle pair self-evicts. `null` disables server-side expiry. Non-negative. |
| `ThrowOnStorageFailure` / `ValidateStorageOnStart` | `false` | As for SQLite. |
| `Provider` | set by the registration | As for SQLite. |

> [!IMPORTANT]
> `AddDistributedRateCache` registers a **single** `DistributedRateCache` instance per host:
> the first call creates it (bound to that call's provider name), and later calls for other
> names only add keyed aliases to the same instance. Register it for one cached provider, or use
> it as the shared L1 tier of a [stack](exchange-rate-caching.md#stacking-providers-tiered-read-through);
> give each source its own SQLite or file cache when you need one cache per provider.

## Pattern 4 — warming the cache at startup

`AddRateCacheWarmup` binds `Financial:RateCacheWarmup` and registers a hosted service that runs
once when the host starts. It warms every provider registered through `AddCachedRateProvider`
automatically and any aggregation child you name in `Providers`; the run never blocks or fails
the host, and a pair that fails is logged and skipped.

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddRbaExchangeRates(builder.Configuration)
    .AddCachedRateProvider<RbaRateProvider>("RBA", builder.Configuration)
    .AddRateCacheWarmup(builder.Configuration);                              // Financial:RateCacheWarmup
```

```json
{
  "Financial": {
    "RateCacheWarmup": {
      "Pairs": [ "AUD/USD", "AUD/EUR" ],
      "LookbackDays": 30,
      "StartDate": null,
      "EndDate": null,
      "Providers": []
    }
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `Pairs` | `[]` | **Required, non-empty.** `FROM/TO` ISO pairs, exactly seven characters each. |
| `LookbackDays` | 30 | The rolling window ends today and reaches back this many days. Non-negative. |
| `StartDate` / `EndDate` | `null` | Fixed overrides for either end of the window (`yyyy-MM-dd`); `EndDate` must not precede `StartDate`. |
| `Providers` | `[]` | Names of aggregation children (keyed `IDatedRateProvider` registrations) to warm in addition to the unkeyed cached providers. |

## Pattern 5 — aggregation

Aggregation is configured in code, not from a section: `AddAggregatedRateProvider` builds the
<xref:Bodu.Financial.ExchangeRates.Caching.RateAggregationOptions> from the builder calls, and
the `configuration` it accepts is for the children's shared `Financial:RateCache` policy.

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddRbaExchangeRates(builder.Configuration)
    .AddEcbExchangeRates(builder.Configuration)
    .AddSqliteRateCache("RBA", builder.Configuration)
    .AddSqliteRateCache("ECB", builder.Configuration)                        // same section, same file: keyed by provider
    .AddAggregatedRateProvider(
        agg => agg
            .AddCachedChild<RbaRateProvider>("RBA", (sp, name) => sp.GetRequiredKeyedService<IRateCache>(name))
            .AddCachedChild<EcbRateProvider>("ECB", (sp, name) => sp.GetRequiredKeyedService<IRateCache>(name))
            .UseDefaultStrategy(PriorityFallbackStrategy.Instance)
            .MapPair(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), "RBA", "ECB")
            .MapPair(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD), new AverageStrategy(), "ECB", "RBA"),
        builder.Configuration);                                              // Financial:RateCache for the children
```

The DI builder sets only `DefaultStrategy` and `Routes`. The remaining members — the default
provider order, `RespectHistoryAvailability`, the timeless surface's `DefaultLookupOptions`, and
the three log levels — keep their defaults under DI. To tune them, construct the aggregator
yourself and register the instance with `AddDatedExchangeRateProvider`:

```csharp
var options = new RateAggregationOptions
{
    DefaultStrategy = PriorityFallbackStrategy.Instance,
    DefaultProviderOrder = new[] { "RBA", "ECB" },
    RespectHistoryAvailability = true,
    DefaultLookupOptions = RateLookupOptions.PreviousWithin(3),   // the timeless surface's policy
    RouteSelectedLogLevel = LogLevel.Debug,                        // default Information
    ResolvedLogLevel = LogLevel.Debug,                             // default Information
    UnresolvedLogLevel = LogLevel.Warning,                         // default Debug
};
options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "RBA", "ECB" });

return new AggregatingRateProvider(
    new[] { new NamedDatedRateProvider("RBA", rba), new NamedDatedRateProvider("ECB", ecb) },
    options,
    timeProvider: null,
    logger: loggerFactory.CreateLogger<AggregatingRateProvider>());
```

| Member | Default | Meaning |
|---|---|---|
| `DefaultStrategy` | `PriorityFallbackStrategy.Instance` | How unrouted pairs combine the children (first to resolve; or `AverageStrategy`, or your own). |
| `DefaultProviderOrder` | `null` (registration order) | The child order for unrouted pairs. |
| `Routes` | `{}` | Per-pair child order and optional strategy. |
| `RespectHistoryAvailability` | `true` | Drop children whose advertised history cannot cover the request before the strategy runs. |
| `DefaultLookupOptions` | `Exact` | The policy the timeless `IRateProvider` surface applies. |
| `RouteSelectedLogLevel` | `Information` | A route (or the default order) was chosen for a pair. |
| `ResolvedLogLevel` | `Information` | A child answered. |
| `UnresolvedLogLevel` | `Debug` | No child answered. |

## File layout is a code decision

The default cache `AddCachedRateProvider` builds is a single-file TOML cache under
`CacheDirectory`. The on-disk layout (<xref:Bodu.Financial.ExchangeRates.Caching.RateCacheFileLayout>:
`SingleFile`, `Yearly`, `Monthly`, `Daily`, or `Create(...)` over a
<xref:Bodu.Financial.ExchangeRates.Caching.RateCachePartitionStrategy>) and the format (TOML or
JSON) are not configuration keys; choose them by supplying the cache through `cacheFactory`:

```csharp
builder.Services
    .AddFinancialService(builder.Configuration)
    .AddRbaExchangeRates(builder.Configuration)
    .AddCachedRateProvider<RbaRateProvider>(
        "RBA",
        builder.Configuration,
        cacheFactory: (sp, name) => new JsonFileRateCache(
            new FileRateCacheOptions
            {
                Provider = name,
                CacheDirectory = "/var/cache/fx",
                Layout = RateCacheFileLayout.Monthly,          // RBA/AUDUSD/2024-01.json, …
            },
            sp.GetService<TimeProvider>(),
            sp.GetService<ILoggerFactory>()?.CreateLogger<JsonFileRateCache>()));
```

See [File layouts and date partitioning](exchange-rate-caching.md#file-layouts-and-date-partitioning)
for what each layout writes.

## Lifetimes and thread safety

Everything the caching registrations create is a **singleton**, resolved once and shared for the
host's lifetime:

| Registration | Service | Lifetime and identity |
|---|---|---|
| `AddCachedRateProvider<TProvider>` | `TProvider` | singleton (`TryAdd`, so a provider already registered by `Add<Source>ExchangeRates` is reused) |
| | <xref:Bodu.Financial.ExchangeRates.Caching.CachingRateProvider>, `IDatedRateProvider`, `IRateProvider` | one singleton exposed under all three; a second `AddCachedRateProvider` adds a second `CachingRateProvider` and the *last* registration answers the unkeyed contracts |
| `AddAggregatedRateProvider` | keyed `IDatedRateProvider` per child name | singleton per child (`TryAddKeyedSingleton`) |
| | <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider>, `IDatedRateProvider`, `IRateProvider` | one singleton under all three |
| `AddSqliteRateCache(name)` | keyed `SqliteRateCache` / `IRateCache` per name; default `IRateCache` | one instance per provider name, each with its own keep-alive connection; the first registered is also the unkeyed default |
| `AddDistributedRateCache(name)` / `AddRedisRateCache` | `DistributedRateCache`, `IRateCache`, keyed `IRateCache` | one instance per host (see the note in Pattern 3) |
| `AddRateCacheWarmup` | `IHostedService` | one hosted service, runs once at start |

The container disposes each singleton on shutdown; a cached provider does not dispose the inner
provider it wraps, because the container owns that too.

`CachingRateProvider` is safe for concurrent use. Lookups carry no per-request mutable state on
the decorator; every backend serializes writes to a pair under a per-pair lock (SQLite adds a
transaction per write, so the guarantee holds across processes sharing the file), so two threads
missing the same pair never interleave a half-written row with its coverage. Request coalescing
is deliberately left to the inner provider — every shipped web provider already single-flights
its downloads — so concurrent misses collapse onto one fetch at the origin rather than at the
cache. Refresh-ahead bookkeeping is shared: concurrent aged hits join one pending background
refresh instead of each scheduling their own. The only caveat is the distributed backend, whose
read-merge-write cycle is atomic per process but last-write-wins *across* processes, as the
[caching guide](exchange-rate-caching.md#persistent-and-shared-backends) explains.

## API summary

| Member | Binds | Description |
|---|---|---|
| `AddCachedRateProvider<TProvider>(string providerName, IConfiguration?, string sectionName = "Financial:RateCache", Action<CachingRateOptions>?, Func<IServiceProvider, string, IRateCache>? cacheFactory)` | `CachingRateOptions` | Wraps an already-registered provider in a `CachingRateProvider` over the cache the factory returns (default: single-file TOML under `CacheDirectory`). |
| `AddAggregatedRateProvider(Action<IAggregatedRateBuilder>, IConfiguration?, string sectionName = "Financial:RateCache", Action<CachingRateOptions>? configureCache)` | `CachingRateOptions` | Registers keyed cached children and one `AggregatingRateProvider`. |
| `AddSqliteRateCache(string providerName, IConfiguration?, string sectionName = "Financial:RateCache:Sqlite", Action<SqliteRateCacheOptions>?)` | named `SqliteRateCacheOptions` | A keyed `SqliteRateCache` for the provider. |
| `AddDistributedRateCache(string providerName, IConfiguration?, string sectionName = "Financial:RateCache:Distributed", Action<DistributedRateCacheOptions>?)` | named `DistributedRateCacheOptions` | The host-wide `DistributedRateCache` over the registered `IDistributedCache`. |
| `AddRedisRateCache(Action<RedisCacheOptions>, string providerName, IConfiguration?, string sectionName, Action<DistributedRateCacheOptions>?)` | named `DistributedRateCacheOptions` | `AddStackExchangeRedisCache` followed by `AddDistributedRateCache`. |
| `AddRateCacheWarmup(IConfiguration?, string sectionName = "Financial:RateCacheWarmup", Action<RateCacheWarmupOptions>?)` | `RateCacheWarmupOptions` | The startup warm-up hosted service. |

All six live in the `Bodu.Financial.ExchangeRates` namespace, on `IFinancialServiceBuilder`.

## Where to go next

- [Caching and aggregating exchange rates](exchange-rate-caching.md) — expiry, coverage, stacking, strategies, observability.
- [Configuring providers from appsettings](provider-configuration.md) — the `Financial:<Source>` sections underneath the cache.
- [Financial dependency injection](dependency-injection.md) — the builder these registrations compose on.
- [Testing your own provider](testing-providers.md) — `NullRateCache` and fixed providers for tests that must not cache.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic.
