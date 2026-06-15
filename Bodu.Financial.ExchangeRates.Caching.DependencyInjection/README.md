# Bodu.Financial.ExchangeRates.Caching.DependencyInjection

Dependency-injection wiring for the `Bodu.Financial` exchange-rate caching and composition layer.

## One cached provider

`AddCachedExchangeRateProvider<TProvider>(name, …)` registers a `CachingExchangeRateProvider`
that wraps a single source over its own on-disk cache. It resolves as both
`IDatedExchangeRateProvider` and the timeless `IExchangeRateProvider`:

```csharp
services.AddBoduFinancial()
        .AddRbaHistoricalRates()
        .AddCachedExchangeRateProvider<RbaExchangeRateProvider>("RBA", configuration,
            configure: o =>
            {
                o.CacheDirectory = "/var/cache/fx";
                o.DefaultExpiry = TimeSpan.FromHours(12);
            });
```

## A group of cached providers

`AddAggregatedExchangeRateProvider(…)` registers an `AggregatingExchangeRateProvider` that groups
several cached children behind one entry point, with a strategy and optional per-FX-pair routing.
Each child is also registered as a **keyed** `IDatedExchangeRateProvider`, so a specific source is
resolvable by name through the service catalog:

```csharp
services.AddBoduFinancial()
        .AddRbaHistoricalRates()
        .AddEcbHistoricalRates()
        .AddAggregatedExchangeRateProvider(agg => agg
            .AddCachedChild<RbaExchangeRateProvider>("RBA")
            .AddCachedChild<EcbExchangeRateProvider>("ECB")
            .MapPair(new ExchangeRatePair("AUD", "USD"), "RBA", "ECB")
            .MapPair(new ExchangeRatePair("USD", "GBP"), "ECB", "RBA"));

// Later: the aggregate, or a specific source.
var aggregate = provider.GetRequiredService<IDatedExchangeRateProvider>();
var rbaOnly = provider.GetRequiredKeyedService<IDatedExchangeRateProvider>("RBA");
```

`UseDefaultStrategy(...)` overrides the default `PriorityFallbackStrategy` (for example with an
`AverageStrategy`), and `MapPair(pair, strategy, order)` overrides the strategy for a single pair.

## Configuration

Both methods bind the shared `CachingExchangeRateOptions` from configuration (default section
`Financial:ExchangeRateCache`) and an optional callback. Routing and strategy are configured
through the builder. The provider classes themselves know nothing of caching — names are supplied
at the composition root.
