---
uid: Bodu.Financial.ExchangeRates.Rba.DependencyInjection
---

# Bodu.Financial.ExchangeRates.Rba.DependencyInjection

## Purpose

**Bodu.Financial.ExchangeRates.Rba.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` wiring for the [`Bodu.Financial.ExchangeRates.Rba`](Bodu.Financial.ExchangeRates.Rba.md) provider. `AddRbaHistoricalRates(…)` on the <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> binds <xref:Bodu.Financial.ExchangeRates.Rba.RbaExchangeRateOptions> (default section `Financial:Rba`), configures a named `HttpClient` fitted with the standard Polly resilience handler (retry with backoff and jitter, per-attempt and total timeouts, a circuit breaker), and registers the provider as a singleton resolvable on both the dated <xref:Bodu.Financial.IDatedExchangeRateProvider> and timeless <xref:Bodu.Financial.IExchangeRateProvider> surfaces.

## Static documentation

- **[Built-in exchange-rate providers guide](~/guides/financial/exchange-rate-providers.md)** — see *Registering a provider with dependency injection* and *Adding caching in front*.

## Key types

- <xref:Bodu.Financial.ExchangeRates.Rba.DependencyInjection.RbaFinancialServiceBuilderExtensions> — `AddRbaHistoricalRates(configuration?, sectionName?, configure?, configureResilience?)` on the financial service builder.
- <xref:Bodu.Financial.ExchangeRates.Rba.DependencyInjection.RbaServiceCollectionExtensions> — `AddRbaHistoricalRates(…)`, the `IServiceCollection` convenience overload that adds the financial core and the RBA provider together.

## Minimal sample

```csharp
using Bodu.Financial;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Rba;
using Bodu.Financial.ExchangeRates.Rba.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialService()
        .AddRbaHistoricalRates(builder.Configuration);   // binds the Financial:Rba section

// Later: resolve on either surface.
var dated = provider.GetRequiredService<IDatedExchangeRateProvider>();
```

To cache the provider, register it first and wrap it through [`AddCachedExchangeRateProvider<RbaExchangeRateProvider>`](Bodu.Financial.ExchangeRates.Caching.DependencyInjection.md). See the [providers guide](~/guides/financial/exchange-rate-providers.md).
