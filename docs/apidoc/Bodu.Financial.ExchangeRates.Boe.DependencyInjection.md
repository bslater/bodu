---
uid: Bodu.Financial.ExchangeRates.Boe.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Boe.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Boe.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [`Bodu.Financial.ExchangeRates.Boe`](Bodu.Financial.ExchangeRates.Boe.md) provider. `AddBoeReferenceRates(…)` on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> binds <xref:Bodu.Financial.ExchangeRates.Boe.BoeExchangeRateOptions> (default section `Financial:Boe`), configures a named `HttpClient` fitted with the standard Polly resilience handler, and registers the provider as a singleton resolvable on both the dated <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> surfaces.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — see *Registering a provider with dependency injection* and *Adding caching in front*.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Boe.DependencyInjection.BoeFinancialServiceBuilderExtensions> — `AddBoeReferenceRates(configuration?, sectionName?, configure?, configureResilience?)` on the financial service builder.
- <xref:Bodu.Financial.ExchangeRates.Boe.DependencyInjection.BoeServiceCollectionExtensions> — `AddBoduBoeReferenceRates(…)`, the `IServiceCollection` convenience overload that adds the financial core and the BoE provider together.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Boe;
using Bodu.Financial.ExchangeRates.Boe.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddBoduFinancial()
        .AddBoeReferenceRates(builder.Configuration);   // binds the Financial:Boe section

var dated = provider.GetRequiredService<IDatedExchangeRateProvider>();
```

To cache the provider, register it first and wrap it through [`AddCachedExchangeRateProvider<BoeExchangeRateProvider>`](Bodu.Financial.ExchangeRates.Caching.DependencyInjection.md). See the [providers guide](~/guides/financial/exchange-rate-providers.md).
