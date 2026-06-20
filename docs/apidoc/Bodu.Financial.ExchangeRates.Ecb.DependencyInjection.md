---
uid: Bodu.Financial.ExchangeRates.Ecb.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Ecb.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Ecb.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [`Bodu.Financial.ExchangeRates.Ecb`](Bodu.Financial.ExchangeRates.Ecb.md) provider. `AddEcbReferenceRates(…)` on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> binds <xref:Bodu.Financial.ExchangeRates.Ecb.EcbExchangeRateOptions> (default section `Financial:Ecb`), configures a named `HttpClient` fitted with the standard Polly resilience handler, and registers the provider as a singleton resolvable on both the dated <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> surfaces.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — see *Registering a provider with dependency injection* and *Adding caching in front*.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Ecb.DependencyInjection.EcbFinancialServiceBuilderExtensions> — `AddEcbReferenceRates(configuration?, sectionName?, configure?, configureResilience?)` on the financial service builder.
- <xref:Bodu.Financial.ExchangeRates.Ecb.DependencyInjection.EcbServiceCollectionExtensions> — `AddEcbReferenceRates(…)`, the `IServiceCollection` convenience overload that adds the financial core and the ECB provider together.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Ecb;
using Bodu.Financial.ExchangeRates.Ecb.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialService()
        .AddEcbReferenceRates(builder.Configuration);   // binds the Financial:Ecb section

var dated = provider.GetRequiredService<IDatedExchangeRateProvider>();
```

To cache the provider, register it first and wrap it through [`AddCachedExchangeRateProvider<EcbExchangeRateProvider>`](Bodu.Financial.ExchangeRates.Caching.DependencyInjection.md). See the [providers guide](~/guides/financial/exchange-rate-providers.md).
