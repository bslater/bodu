# Bodu.Financial.ExchangeRates.Boe.DependencyInjection

Dependency-injection extensions for the
[Bodu.Financial.ExchangeRates.Boe](../Bodu.Financial.ExchangeRates.Boe) provider.

Registers `BoeExchangeRateProvider` as a singleton backed by a named, factory-managed
`HttpClient`, binds `BoeExchangeRateOptions` through `Microsoft.Extensions.Options`, and
exposes the provider through `IDatedExchangeRateProvider` and `IExchangeRateProvider`.

```csharp
using Bodu.Financial.ExchangeRates.Boe.DependencyInjection;

// One-call entry point: core Bodu.Financial services + the BoE provider.
services.AddBoduBoeReferenceRates(configuration);

// Or onto an existing IFinancialServiceBuilder:
services.AddBoduFinancial(configuration)
        .AddBoeReferenceRates(configuration, configure: o => o.EnableDiskCache = false);
```

Options bind from the `Financial:Boe` configuration section by default.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
