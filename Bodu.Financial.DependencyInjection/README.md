# Bodu.Financial.DependencyInjection

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

`Microsoft.Extensions.DependencyInjection` integration for `Bodu.Financial`. Registers currency lookup, financial options, exchange-rate providers, named monetary contexts, and the JSON converter set behind a small fluent builder.

## Installation

```shell
dotnet add package Bodu.Financial.DependencyInjection
```

Targets `net8.0`. The registration extension methods, the `IFinancialServiceBuilder` builder, and `FinancialOptions` live in the `Bodu.Financial` namespace.

## Registration

```csharp
using Bodu.Financial;
using Bodu.Financial.Serialization;

services.AddFinancialService(configuration, sectionName: "Financial")
    .AddExchangeRateProvider<EcbRateProvider>()
    .AddMonetaryContext("au", auContext)
    .AddFinancialJson(FinancialJsonPolicy.Strict);
```

| Entry point | Purpose |
|---|---|
| `AddFinancialService(IConfiguration?, string)` | Register `ICurrencyLookup`, bind `FinancialOptions`, return an `IFinancialServiceBuilder` |
| `AddFinancialService(Action<IFinancialServiceBuilder>)` | Register core services and compose fluently |
| `IFinancialServiceBuilder.AddCurrencyLookup<TLookup>()` | Replace the registered `ICurrencyLookup` |
| `.AddExchangeRateProvider<T>()` / `(instance)` | Register an `IExchangeRateProvider` |
| `.AddDatedExchangeRateProvider<T>()` / `(instance)` | Register an `IDatedExchangeRateProvider` |
| `.AddMonetaryContext(name, context)` | Register a named `MonetaryContext` as a keyed singleton |
| `.AddFinancialJson(policy)` | Register a `JsonSerializerOptions` (keyed `"Financial"`) with the financial converters |
| `IServiceProvider.UseCurrencyResolution()` | Install the container's `ICurrencyLookup` as the process-wide ambient resolver |

`FinancialOptions` carries the `JsonPolicy` (default `Strict`) and `UnknownCurrency` (default `Reject`) settings, bound from the supplied configuration section (default `"Financial"`).

## Testing

```bash
dotnet test Bodu.Financial.DependencyInjection/test/Bodu.Financial.DependencyInjection.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
