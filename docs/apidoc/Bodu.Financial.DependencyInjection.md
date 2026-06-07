---
uid: Bodu.Financial.DependencyInjection
---

# Bodu.Financial.DependencyInjection

## Purpose

**Bodu.Financial.DependencyInjection** provides the `Microsoft.Extensions.DependencyInjection` integration for [`Bodu.Financial`](Bodu.Financial.md). A single `AddBoduFinancial(...)` call registers the currency-lookup service and returns an <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> on which you compose the rest of the financial stack — currency lookups, named monetary contexts, exchange-rate providers (timeless and dated), and JSON converters — through chainable `Add*` extension methods.

Bind <xref:Bodu.Financial.DependencyInjection.FinancialOptions> from configuration (JSON policy, unknown-currency policy) by passing an `IConfiguration`, or configure the builder imperatively with a delegate. Direct construction of the underlying `Bodu.Financial` types continues to work for consoles, libraries, and tests that prefer not to bring in `IServiceCollection`.

## Static documentation

- **[Financial dependency injection guide](~/guides/financial/dependency-injection.md)** — the registration surface, builder chaining, options binding, and the currency-resolution activation step.

## Key types

- <xref:Bodu.Financial.DependencyInjection.ServiceCollectionExtensions> — the entry point:
  - `AddBoduFinancial(IServiceCollection, IConfiguration?, string sectionName)` — registers the currency lookup and binds <xref:Bodu.Financial.DependencyInjection.FinancialOptions> from the named configuration section.
  - `AddBoduFinancial(IServiceCollection, Action<IFinancialServiceBuilder>)` — the same, with the builder configured imperatively.
- <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> — the fluent builder; exposes the underlying `IServiceCollection` via `Services`.
- <xref:Bodu.Financial.DependencyInjection.FinancialServiceBuilderExtensions> — the chainable composition surface:
  - `AddCurrencyLookup<TLookup>()` — replace the default <xref:Bodu.Financial.ICurrency> lookup.
  - `AddMonetaryContext(string name, MonetaryContext context)` — register a named monetary context.
  - `AddExchangeRateProvider` / `AddDatedExchangeRateProvider` (type-parameter and instance overloads) — register timeless and dated FX providers.
  - `AddFinancialJson(FinancialJsonPolicy)` — register the `System.Text.Json` converters under the chosen policy.
- <xref:Bodu.Financial.DependencyInjection.ServiceProviderExtensions> — `UseBoduFinancialCurrencyResolution(IServiceProvider)`, the post-build activation step that wires the resolved currency lookup into the static currency-resolution surface.
- <xref:Bodu.Financial.DependencyInjection.FinancialOptions> — bound options: `JsonPolicy` and `UnknownCurrency`.

## Minimal sample

```csharp
using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddBoduFinancial(configure: financial =>
{
    financial.AddFinancialJson(FinancialJsonPolicy.Strict);
    financial.AddDatedExchangeRateProvider<MyRateProvider>();
});

// After the container is built, activate static currency resolution once:
app.Services.UseBoduFinancialCurrencyResolution();
```

To bind options from configuration instead, pass the `IConfiguration`:

```csharp
builder.Services.AddBoduFinancial(builder.Configuration);   // binds the "Financial" section
```

See the [Financial dependency injection](~/guides/financial/dependency-injection.md) guide for the full walkthrough.
