---
uid: Bodu.Financial.ExchangeRates.Caching.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Caching.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Caching.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [`Bodu.Financial.ExchangeRates.Caching`](Bodu.Financial.ExchangeRates.Caching.md) layer. Two extension methods on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> register caching and aggregating providers, both resolvable on the dated <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> surfaces:

- `AddCachedExchangeRateProvider<TProvider>(name, …)` registers a single <xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider> over one source's own on-disk cache.
- `AddAggregatedExchangeRateProvider(…)` registers an <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider> that groups several cached children, configured through a fluent builder with a strategy and per-FX-pair routing. Each child is **also** registered as a keyed `IDatedExchangeRateProvider`, so a specific source is resolvable by name with `GetRequiredKeyedService<IDatedExchangeRateProvider>(name)`.

<xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateOptions> binds from configuration (default section `Financial:ExchangeRateCache`) or a `configure` delegate.

## Static documentation

- **[Caching and aggregating exchange rates guide](~/guides/financial/exchange-rate-caching.md)** — see the *Dependency injection* section for the registration walkthrough.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Caching.DependencyInjection.RateCachingExtensions> — the registration surface:
  - `AddCachedExchangeRateProvider<TProvider>(name, …)` — one cache per provider.
  - `AddAggregatedExchangeRateProvider(Action<IAggregatedExchangeRateBuilder>, …)` — a group of cached children with routing and a strategy.
- <xref:Bodu.Financial.ExchangeRates.Caching.DependencyInjection.IAggregatedExchangeRateBuilder> — the fluent builder: `AddCachedChild<TProvider>(name)` / `AddCachedChild(name, factory)`, `UseDefaultStrategy(strategy)`, and `MapPair(pair, …, order)`.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching;
using Bodu.Financial.ExchangeRates.Caching.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

// One cached provider.
services.AddBoduFinancial()
        .AddRbaHistoricalRates()
        .AddCachedExchangeRateProvider<RbaExchangeRateProvider>("RBA",
            configure: o => o.DefaultExpiry = TimeSpan.FromHours(12));

// A group of cached providers with per-FX-pair routing.
services.AddBoduFinancial()
        .AddRbaHistoricalRates()
        .AddEcbReferenceRates()
        .AddAggregatedExchangeRateProvider(agg => agg
            .AddCachedChild<RbaExchangeRateProvider>("RBA")
            .AddCachedChild<EcbExchangeRateProvider>("ECB")
            .MapPair(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), "RBA", "ECB")
            .MapPair(new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.GBP), "ECB", "RBA"));
```

Resolve the aggregate, or a specific source by name:

```csharp
var aggregate = provider.GetRequiredService<IDatedExchangeRateProvider>();
var rbaOnly = provider.GetRequiredKeyedService<IDatedExchangeRateProvider>("RBA");
```

To bind options from configuration, pass the `IConfiguration` (binds the `Financial:ExchangeRateCache` section):

```csharp
.AddCachedExchangeRateProvider<RbaExchangeRateProvider>("RBA", builder.Configuration);
```

See the [caching guide](~/guides/financial/exchange-rate-caching.md) for the full walkthrough.
