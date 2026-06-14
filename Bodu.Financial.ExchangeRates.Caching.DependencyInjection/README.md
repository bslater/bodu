# Bodu.Financial.ExchangeRates.Caching.DependencyInjection

Dependency-injection wiring for the `Bodu.Financial` exchange-rate caching layer.

## Usage

Register the concrete providers, then register a caching provider that wraps a set of named
sources. The caching provider is registered as `IDatedExchangeRateProvider`, so consumers resolve
the same interface and transparently get cached lookups:

```csharp
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

The names are supplied at the composition root — the provider classes themselves know nothing of
caching. Sources are consulted in the supplied order; the first to satisfy a lookup wins. Both
single-date lookups (`GetRate`/`TryGetRate`) and range lookups (`GetRatesAsync`) are served through
the cache by the resolved `IDatedExchangeRateProvider`.

## Method

`AddCachedExchangeRateProvider(...)` registers the shared `TomlFileSystemExchangeRateCache` and a
`CachingDatedExchangeRateProvider` (as `IDatedExchangeRateProvider`) wrapping the named sources. Two
overloads add the sources:

- **Fluent** — `configureSources` with `.AddSource<TProvider>(name)` (resolved from the container) or
  `.AddSource(name, sp => …)` (a factory).
- **Raw factory** — `Func<IServiceProvider, IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>>>`
  for full control.

Both bind `CachingExchangeRateOptions` from configuration (default section
`Financial:ExchangeRateCache`) and an optional `configure` delegate. Only the dated
`IDatedExchangeRateProvider` surface is registered through the cache; any undated
`IExchangeRateProvider` registration continues to resolve its concrete provider.
