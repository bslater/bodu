# Bodu.Financial.DependencyInjection

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

`Microsoft.Extensions.DependencyInjection` integration for `Bodu.Financial`. Registers currency lookup, financial options, exchange-rate providers, and named monetary contexts behind a small fluent builder. Financial JSON registration (`AddFinancialJson`) ships in the companion `Bodu.Financial.Serialization.Json` package.

## Installation

```shell
dotnet add package Bodu.Financial.DependencyInjection
```

Targets `net8.0`. The registration extension methods, the `IFinancialServiceBuilder` builder, and `FinancialOptions` live in the `Bodu.Financial` namespace.

## Registration

```csharp
using Bodu.Financial;
using Bodu.Financial.Serialization.Json;   // Bodu.Financial.Serialization.Json package (optional, for AddFinancialJson)

services.AddFinancialService(configuration, sectionName: "Financial")
    .AddExchangeRateProvider<EcbRateProvider>()
    .AddMonetaryContext("au", auContext);

services.AddFinancialJson(FinancialJsonPolicy.Strict);
```

| Entry point | Purpose |
|---|---|
| `AddFinancialService(IConfiguration?, string)` | Register `ICurrencyLookup`, bind `FinancialOptions`, return an `IFinancialServiceBuilder` |
| `AddFinancialService(Action<IFinancialServiceBuilder>)` | Register core services and compose fluently |
| `IFinancialServiceBuilder.AddCurrencyLookup<TLookup>()` | Replace the registered `ICurrencyLookup` |
| `.AddExchangeRateProvider<T>()` / `(instance)` | Register an `IRateProvider` |
| `.AddDatedExchangeRateProvider<T>()` / `(instance)` | Register an `IDatedRateProvider` |
| `.AddMonetaryContext(name, context)` | Register a named `MonetaryContext` as a keyed singleton |
| `IServiceProvider.UseCurrencyResolution()` | Install the container's `ICurrencyLookup` as the process-wide ambient resolver |

`FinancialOptions` is bound from the supplied configuration section (default `"Financial"`); it currently declares no settings of its own and exists as the binding seam for future options. The JSON registration `services.AddFinancialJson(policy)` (a `JsonSerializerOptions` keyed `"Financial"` with the financial converters) lives in the companion `Bodu.Financial.Serialization.Json` package.

## Testing

```bash
dotnet test Bodu.Financial.DependencyInjection/test/Bodu.Financial.DependencyInjection.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
