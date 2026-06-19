---
uid: Bodu.Financial.ExchangeRates.Ofx.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Ofx.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Ofx.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [`Bodu.Financial.ExchangeRates.Ofx`](Bodu.Financial.ExchangeRates.Ofx.md) provider. `AddOfxExchangeRates(…)` on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> binds <xref:Bodu.Financial.ExchangeRates.Ofx.OfxExchangeRateOptions> (default section `Financial:Ofx`), configures a named `HttpClient` fitted with the standard Polly resilience handler, and registers the provider as a singleton resolvable on both the dated <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> surfaces.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — see *Registering a provider with dependency injection* and *Adding caching in front*.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Ofx.DependencyInjection.OfxFinancialServiceBuilderExtensions> — `AddOfxExchangeRates(configuration?, sectionName?, configure?, configureResilience?)` on the financial service builder.
- <xref:Bodu.Financial.ExchangeRates.Ofx.DependencyInjection.OfxServiceCollectionExtensions> — `AddBoduOfxExchangeRates(…)`, the `IServiceCollection` convenience overload that adds the financial core and the OFX provider together.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Ofx;
using Bodu.Financial.ExchangeRates.Ofx.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddBoduFinancial()
        .AddOfxExchangeRates(builder.Configuration);   // binds the Financial:Ofx section

var dated = provider.GetRequiredService<IDatedExchangeRateProvider>();
```

To cache the provider, register it first and wrap it through [`AddCachedExchangeRateProvider<OfxExchangeRateProvider>`](Bodu.Financial.ExchangeRates.Caching.DependencyInjection.md). See the [providers guide](~/guides/financial/exchange-rate-providers.md).
