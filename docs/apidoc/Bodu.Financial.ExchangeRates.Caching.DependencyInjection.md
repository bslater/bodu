---
uid: Bodu.Financial.ExchangeRates.Caching.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Caching.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Caching.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [`Bodu.Financial.ExchangeRates.Caching`](Bodu.Financial.ExchangeRates.Caching.md) layer. A single `AddCachedExchangeRateProvider(...)` call on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> registers the shared on-disk cache and a <xref:Bodu.Financial.ExchangeRates.Caching.CachingDatedExchangeRateProvider> as the <xref:Bodu.Financial.IDatedExchangeRateProvider>, wrapping a set of named sources resolved from the container.

Sources are added through a fluent <xref:Bodu.Financial.ExchangeRates.Caching.DependencyInjection.ICachedExchangeRateSourceBuilder> — `.AddSource<TProvider>(name)` resolves the concrete provider from the container, `.AddSource(name, sp => …)` takes a factory — or through a raw factory overload for full control. <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateOptions> binds from configuration (default section `Financial:ExchangeRateCache`) or a `configure` delegate. Anything resolving `IDatedExchangeRateProvider` then gets cached single-date and range lookups transparently.

## Static documentation

- **[Caching exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — see the *Dependency injection* section for the registration walkthrough.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Caching.DependencyInjection.ExchangeRateCachingServiceBuilderExtensions> — the registration surface:
  - `AddCachedExchangeRateProvider(Action<ICachedExchangeRateSourceBuilder>, …)` — fluent source registration.
  - `AddCachedExchangeRateProvider(Func<IServiceProvider, IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>>>, …)` — raw-factory registration.
- <xref:Bodu.Financial.ExchangeRates.Caching.DependencyInjection.ICachedExchangeRateSourceBuilder> — the fluent builder: `AddSource<TProvider>(name)` and `AddSource(name, factory)`.

## Minimal sample

```csharp
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

services.AddBoduFinancial()
        .AddYahooExchangeRates()
        .AddRbaHistoricalRates()
        .AddCachedExchangeRateProvider(
            sources => sources
                .AddSource<YahooExchangeRateProvider>(YahooExchangeRateProvider.ProviderName)
                .AddSource<RbaExchangeRateProvider>(RbaExchangeRateProvider.ProviderName),
            configure: o =>
            {
                o.CacheDirectory = "/var/cache/fx";
                o.DefaultExpiry = TimeSpan.FromHours(12);
                o.ProviderExpiry[RbaExchangeRateProvider.ProviderName] = TimeSpan.FromDays(7);
            });
```

To bind options from configuration instead, pass the `IConfiguration`:

```csharp
.AddCachedExchangeRateProvider(
    sources => sources.AddSource<YahooExchangeRateProvider>(YahooExchangeRateProvider.ProviderName),
    builder.Configuration);   // binds the "Financial:ExchangeRateCache" section
```

See the [caching guide](~/guides/financial/exchange-rate-caching.md) for the full walkthrough.
