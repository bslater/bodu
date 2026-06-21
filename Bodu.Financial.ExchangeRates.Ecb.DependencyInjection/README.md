# Bodu.Financial.ExchangeRates.Ecb.DependencyInjection

Dependency-injection extensions for the
[Bodu.Financial.ExchangeRates.Ecb](../Bodu.Financial.ExchangeRates.Ecb) provider.

Registers `EcbExchangeRateProvider` as a singleton backed by a named, factory-managed
`HttpClient`, binds `EcbExchangeRateOptions` through `Microsoft.Extensions.Options`, and
exposes the provider through `IDatedExchangeRateProvider` and `IExchangeRateProvider`.

```csharp
using Bodu.Financial.ExchangeRates.Ecb.DependencyInjection;

// One-call entry point: core Bodu.Financial services + the ECB provider.
services.AddEcbReferenceRates(configuration);

// Or onto an existing IFinancialServiceBuilder:
services.AddFinancialService(configuration)
        .AddEcbReferenceRates(configuration, configure: o => o.EnableDiskCache = false);
```

Options bind from the `Financial:Ecb` configuration section by default.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
