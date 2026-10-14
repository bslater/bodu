---
title: Dependency injection and configuration
---

# Dependency injection and configuration

Bodu's `Microsoft.Extensions.DependencyInjection` registrations come in three families - calendars, money and exchange rates, and configuration sources - and their namespaces do not always match their packages: `AddFinancialService` is in `Bodu.Financial`, not in a `DependencyInjection` namespace, and every exchange-rate provider and cache registers from `Bodu.Financial.ExchangeRates`. This page lists every registration with its package, namespace and lifetime, wires one host that uses all three families, and states the order the registrations must run in.

Every registration adds its services as singletons, because each service it registers is safe for concurrent use; see [thread safety](thread-safety.md).

## Calendars

Every calendar registration is declared in the `Bodu.Globalization.Calendar` namespace, whichever package ships it.

| Method | Package | Registers | Lifetime |
|---|---|---|---|
| `AddNotableDateService` | `Bodu.Globalization.Calendar.DependencyInjection` | `INotableDateService` over a resource or a resource factory; the overloads that take a string key register a keyed service instead. | Singleton; the first registration wins. |
| `AddReloadableNotableDateService` | `Bodu.Globalization.Calendar.DependencyInjection` | `INotableDateService`, and the `INotableDateResourceProvider` whose resource can be swapped at run time. The generic overload rebuilds the resource on every change to its options type. | Singleton; the first registration wins. |
| `AddCachedNotableDateService` | `Bodu.Globalization.Calendar.Caching` | Replaces the registered `INotableDateService` with a `CachingNotableDateService` over it, binding `Calendar:NotableDateCache`. | Singleton. |
| `AddNotableDateCacheWarmup` | `Bodu.Globalization.Calendar.Caching` | A hosted service that fills the cache when the host starts, binding `Calendar:NotableDateCacheWarmup`. | Hosted service. |
| `AddSqliteNotableDateCache` | `Bodu.Globalization.Calendar.Caching.Sqlite` | `SqliteNotableDateCache`, as `INotableDateCache`, binding `Calendar:NotableDateCache:Sqlite`. | Singleton; the first registration wins. |
| `AddDistributedNotableDateCache`, `AddRedisNotableDateCache` | `Bodu.Globalization.Calendar.Caching.Distributed` | `DistributedNotableDateCache`, as `INotableDateCache`, over the registered `IDistributedCache`, binding `Calendar:NotableDateCache:Distributed`. The Redis overload registers the `IDistributedCache` first. | Singleton; the first registration wins. |

## Money and exchange rates

| Method | Package | Namespace | Registers | Lifetime |
|---|---|---|---|---|
| `AddFinancialService` | `Bodu.Financial.DependencyInjection` | `Bodu.Financial` | `ICurrencyLookup` and the `FinancialOptions` binding (`Financial`), and returns the `IFinancialServiceBuilder` the rest of this table chains from. | Singleton; the first registration wins. |
| `AddCurrencyLookup<TLookup>` | `Bodu.Financial.DependencyInjection` | `Bodu.Financial` | Your `ICurrencyLookup`, in place of the default. | Singleton; replaces the existing one. |
| `AddMonetaryContext` | `Bodu.Financial.DependencyInjection` | `Bodu.Financial` | A `MonetaryContext`, keyed by name. | Keyed singleton. |
| `AddExchangeRateProvider`, `AddDatedExchangeRateProvider` | `Bodu.Financial.DependencyInjection` | `Bodu.Financial` | Your `IRateProvider` or `IDatedRateProvider`, by type or instance. | Singleton; the first registration wins. |
| `Add<Source>ExchangeRates`, such as `AddEcbExchangeRates` | `Bodu.Financial.ExchangeRates.<Source>` | `Bodu.Financial.ExchangeRates` | The provider, as itself, `IDatedRateProvider` and `IRateProvider`, with a named `HttpClient` fitted with the standard resilience handler, binding `Financial:<Source>`. The overload on `IServiceCollection` calls `AddFinancialService` first. | Singleton; the first registration wins. |
| `AddWebRateProvider` | `Bodu.Financial.ExchangeRates.DependencyInjection` | `Bodu.Financial.ExchangeRates` | The same plumbing, for a provider of your own. | Singleton; the first registration wins. |
| `AddCachedRateProvider<TProvider>` | `Bodu.Financial.ExchangeRates.Caching` | `Bodu.Financial.ExchangeRates` | A `CachingRateProvider` over the registered `TProvider`, as `IDatedRateProvider` and `IRateProvider`, binding `Financial:RateCache`. | Singleton; overrides earlier registrations. |
| `AddAggregatedRateProvider` | `Bodu.Financial.ExchangeRates.Caching` | `Bodu.Financial.ExchangeRates` | An `AggregatingRateProvider` over cached children, as `IDatedRateProvider` and `IRateProvider`, with each child also a keyed `IDatedRateProvider`. | Singleton; overrides earlier registrations. |
| `AddRateCacheWarmup` | `Bodu.Financial.ExchangeRates.Caching` | `Bodu.Financial.ExchangeRates` | A hosted service that fills the rate caches when the host starts, binding `Financial:RateCacheWarmup`. | Hosted service. |
| `AddSqliteRateCache` | `Bodu.Financial.ExchangeRates.Caching.Sqlite` | `Bodu.Financial.ExchangeRates` | `SqliteRateCache` for one provider, as a keyed `IRateCache` under the provider's name, binding `Financial:RateCache:Sqlite`. | Keyed singleton; the first one registered is also the unkeyed `IRateCache`. |
| `AddDistributedRateCache`, `AddRedisRateCache` | `Bodu.Financial.ExchangeRates.Caching.Distributed` | `Bodu.Financial.ExchangeRates` | `DistributedRateCache` over the registered `IDistributedCache`, likewise, binding `Financial:RateCache:Distributed`. The Redis overload registers the `IDistributedCache` first. | Keyed singleton; the first one registered is also the unkeyed `IRateCache`. |
| `AddFinancialJson` | `Bodu.Financial.Serialization.Json` | `Bodu.Financial.Serialization.Json` | A `JsonSerializerOptions` holding the financial converters, keyed `"Financial"`. | Keyed singleton. |

After building the provider, `UseCurrencyResolution` (in `Bodu.Financial`) makes the registered `ICurrencyLookup` the one the static currency resolution uses.

## Configuration sources

The `Bodu.Extensions.Configuration.Text` package adds configuration sources, all in the `Bodu.Extensions.Configuration.Text` namespace.

| Method | Extends | Adds |
|---|---|---|
| `AddTextConfigurationFile`, `AddTextConfiguration` | `IConfigurationBuilder` | A file in the Bodu text configuration format, which can reload when the file changes. `AddTextConfiguration` looks for `.boduconfig`, then `bodu.config`. |
| `AddTextConfigurationStream`, `AddTextConfigurationDocument` | `IConfigurationBuilder` | The same format from a stream or an already parsed document. |
| `AddTomlFile`, `AddTomlStream` | `IConfigurationBuilder` | A TOML file or stream, read once. A table path becomes a colon-delimited section, so `[Financial.Ecb]` is the `Financial:Ecb` section. |
| `AddBencodeFile`, `AddBencodeStream` | `IConfigurationBuilder` | A Bencode dictionary, read once. |
| `AddConfigurationOptions<TOptions>` | `IServiceCollection` | Binds a section to `IOptions<TOptions>`. |

## One host

This host reads its settings from TOML, registers a calendar service with a cache in front of it, and registers the core financial services with the ECB provider and a cache in front of that. Every registration that takes an `IConfiguration` reads its default section from it.

```csharp
using System.Text;
using Bodu.Extensions.Configuration.Text;
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

const string settings = """
    [Calendar.NotableDateCache]
    CacheDirectory = "cache/holidays"
    Ttl = "7.00:00:00"

    [Financial.Ecb]
    MaxResponseContentBufferSize = 16777216

    [Financial.RateCache]
    CacheDirectory = "cache/rates"
    DefaultExpiry = "12:00:00"
    """;

using var source = new MemoryStream(Encoding.UTF8.GetBytes(settings));
IConfiguration configuration = new ConfigurationBuilder()
    .AddTomlStream(source)
    .Build();

var services = new ServiceCollection();

// Calendars: the service first, then the cache in front of it.
services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
services.AddCachedNotableDateService(configuration);

// Money and exchange rates: the core services, the provider, then the cache in front of it. The provider's
// named HttpClient takes its response cap, 16 MiB here, from the [Financial.Ecb] options.
services.AddFinancialService(configuration)
    .AddEcbExchangeRates(configuration)
    .AddCachedRateProvider<EcbRateProvider>("ECB", configuration);

using ServiceProvider provider = services.BuildServiceProvider();
provider.UseCurrencyResolution();

INotableDateService holidays = provider.GetRequiredService<INotableDateService>();
IDatedRateProvider rates = provider.GetRequiredService<IDatedRateProvider>();
Console.WriteLine($"{holidays.GetType().Name}, {rates.GetType().Name}");
```

The last line prints `CachingNotableDateService, CachingRateProvider`: each consumer asks for the contract and gets the cached service. Under the Generic Host the same registrations go on `builder.Services`, and the options they validate with `ValidateOnStart` are checked when the host starts; with a plain `ServiceProvider`, as here, they are checked when the service is first resolved.

## Ordering rules

1. **Register a service before the decorator in front of it.** `AddCachedNotableDateService` wraps the `INotableDateService` registered before it, and throws `InvalidOperationException` if there is none. `AddCachedRateProvider<TProvider>` wraps the `TProvider` registered before it; registered first, it would register `TProvider` itself, without the `HttpClient` the provider's own registration supplies.
2. **Keyed calendar services are not cached.** `AddCachedNotableDateService` decorates the most recent unkeyed `INotableDateService` registration and leaves keyed ones, registered before or after it, in place and undecorated. With only keyed registrations it throws `InvalidOperationException`.
3. **Register warm-ups after the caches they warm.** `AddNotableDateCacheWarmup` follows `AddCachedNotableDateService`; `AddRateCacheWarmup` warms every provider registered through `AddCachedRateProvider`, and an aggregation child only when its name is listed in the warm-up options.
4. **Compose a cache backend through the cache factory.** The SQLite and distributed backends register a cache; the decorators use it only when you pass a factory that resolves it: `cacheFactory: sp => sp.GetRequiredService<INotableDateCache>()` for calendars, and `cacheFactory: (sp, name) => sp.GetRequiredKeyedService<IRateCache>(name)` for rates. Without one, each decorator uses a TOML file cache under its `CacheDirectory`.
5. **Know which registrations win.** Most registrations use `TryAdd`, so when two register the same service the first wins: two calendar services, two rate providers, two cache backends. `AddCachedRateProvider` and `AddAggregatedRateProvider` add their providers unconditionally, so the most recent wins, and `AddCurrencyLookup` replaces the lookup outright.
6. **Call `UseCurrencyResolution` once, after the provider is built.** It sets a process-wide default, so it belongs in the composition root.

## Where to go next

- **[Calendar dependency injection](../../guides/calendar/dependency-injection.md)** - every calendar registration in depth, with keyed and reloadable services.
- **[Financial dependency injection](../../guides/financial/dependency-injection.md)** - the financial builder, monetary contexts, and the Generic Host.
- **[Exchange-rate caching](../../guides/financial/exchange-rate-caching.md)** - the rate caches and the aggregating provider.
- **[Configuration sources](../../guides/extensions-configuration-text/configuration-sources.md)** - every configuration source, reloading, and layering.
