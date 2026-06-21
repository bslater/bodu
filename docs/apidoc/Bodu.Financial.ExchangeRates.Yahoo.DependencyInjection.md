---
uid: Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [`Bodu.Financial.ExchangeRates.Yahoo`](Bodu.Financial.ExchangeRates.Yahoo.md) provider. `AddYahooExchangeRates(…)` on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> binds <xref:Bodu.Financial.ExchangeRates.Yahoo.YahooExchangeRateOptions> (default section `Financial:Yahoo`), configures a named `HttpClient` fitted with the standard Polly resilience handler, and registers the provider as a singleton resolvable on both the dated <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> surfaces.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — see *Registering a provider with dependency injection* and *Adding caching in front*.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection.YahooFinancialServiceBuilderExtensions> — `AddYahooExchangeRates(configuration?, sectionName?, configure?, configureResilience?)` on the financial service builder.
- <xref:Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection.YahooServiceCollectionExtensions> — `AddYahooExchangeRates(…)`, the `IServiceCollection` convenience overload that adds the financial core and the Yahoo provider together.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Yahoo;
using Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialService()
        .AddYahooExchangeRates(builder.Configuration);   // binds the Financial:Yahoo section

var dated = provider.GetRequiredService<IDatedExchangeRateProvider>();
```

To cache the provider, register it first and wrap it through [`AddCachedExchangeRateProvider<YahooExchangeRateProvider>`](Bodu.Financial.ExchangeRates.Caching.DependencyInjection.md). See the [providers guide](~/guides/financial/exchange-rate-providers.md).
