# Bodu.Financial.ExchangeRates.Rba.DependencyInjection

Dependency-injection extensions for
[`Bodu.Financial.ExchangeRates.Rba`](../Bodu.Financial.ExchangeRates.Rba).

```csharp
using Bodu.Financial.ExchangeRates.Rba.DependencyInjection;

// One-call registration (core financial services + the RBA provider):
services.AddRbaHistoricalRates(configuration);

// Or compose onto an existing Bodu.Financial registration:
services.AddFinancialService(configuration)
        .AddRbaHistoricalRates(configuration, configure: o => o.CurrentEraRefreshInterval = TimeSpan.FromHours(6));
```

The provider is resolvable as `RbaExchangeRateProvider` (for the async load/range API),
`IDatedExchangeRateProvider`, and `IExchangeRateProvider`. It is registered as a
**singleton** so its in-memory store of loaded eras is shared, and is backed by a named
`HttpClient` from `IHttpClientFactory`. `RbaExchangeRateOptions` binds from the
`Financial:Rba` configuration section (override the section name if needed) and from the
optional `configure` callback.

```jsonc
// appsettings.json
{
  "Financial": {
    "Rba": {
      "EnableDiskCache": true,
      "CacheDirectory": "/var/cache/bodu-rba",
      "CurrentEraRefreshInterval": "12:00:00",
      "AllowSynchronousNetworkAccess": true
    }
  }
}
```

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
