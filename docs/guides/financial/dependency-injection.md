---
title: Financial dependency injection
---

# Financial dependency injection

The optional `Bodu.Financial.DependencyInjection` companion package wires the [`Bodu.Financial`](index.md) stack into a `Microsoft.Extensions.DependencyInjection` container. A single `AddBoduFinancial(...)` call registers the currency-lookup service and hands back a fluent <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> on which you compose currency lookups, named monetary contexts, exchange-rate providers, and JSON converters.

If you are constructing the financial types by hand — in a console app or a test — keep using the `Bodu.Financial` constructors directly; this page is only relevant when you want the host to compose the stack for you.

## Install

```bash
dotnet add package Bodu.Financial.DependencyInjection
```

The package depends on `Bodu.Financial` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

## The registration surface

The entry point is <xref:Bodu.Financial.DependencyInjection.ServiceCollectionExtensions>, with two `AddBoduFinancial` overloads. Both register the default <xref:Bodu.Financial.ICurrency> lookup and return an <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder>.

| Method | Registers |
|---|---|
| `AddBoduFinancial(IServiceCollection, IConfiguration?, string sectionName = "Financial")` | The currency lookup, and binds <xref:Bodu.Financial.DependencyInjection.FinancialOptions> from the named configuration section. |
| `AddBoduFinancial(IServiceCollection, Action<IFinancialServiceBuilder> configure)` | The same, with the builder configured imperatively by the delegate. |

## Composing the builder

The chainable methods on <xref:Bodu.Financial.DependencyInjection.FinancialServiceBuilderExtensions> add the rest of the stack:

| Builder method | Effect |
|---|---|
| `AddCurrencyLookup<TLookup>()` | Replaces the default currency lookup with your `ICurrencyLookup` implementation. |
| `AddMonetaryContext(string name, MonetaryContext context)` | Registers a named monetary context (rounding, minor units, formatting). |
| `AddExchangeRateProvider<TProvider>()` / `AddExchangeRateProvider(provider)` | Registers a timeless <xref:Bodu.Financial.IExchangeRateProvider>, by type or by instance. |
| `AddDatedExchangeRateProvider<TProvider>()` / `AddDatedExchangeRateProvider(provider)` | Registers a dated <xref:Bodu.Financial.IDatedExchangeRateProvider>. |
| `AddFinancialJson(FinancialJsonPolicy policy = FinancialJsonPolicy.Strict)` | Registers the `System.Text.Json` converters under the chosen policy. |

```csharp
using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddBoduFinancial(configure: financial =>
{
    financial
        .AddFinancialJson(FinancialJsonPolicy.Strict)
        .AddExchangeRateProvider<MyRateProvider>()
        .AddDatedExchangeRateProvider<HistoricalRateProvider>();
});
```

## Binding options from configuration

Passing an `IConfiguration` binds <xref:Bodu.Financial.DependencyInjection.FinancialOptions> — `JsonPolicy` and `UnknownCurrency` — from the named section (default `"Financial"`):

```jsonc
// appsettings.json
{
  "Financial": {
    "JsonPolicy": "Strict",
    "UnknownCurrency": "Throw"
  }
}
```

```csharp
builder.Services.AddBoduFinancial(builder.Configuration);
```

## Activating static currency resolution

`Bodu.Financial` exposes a static currency-resolution surface used by parsing and formatting. After the container is built, call <xref:Bodu.Financial.DependencyInjection.ServiceProviderExtensions> `UseBoduFinancialCurrencyResolution` once so the resolved `ICurrencyLookup` backs that static surface:

```csharp
var app = builder.Build();
app.Services.UseBoduFinancialCurrencyResolution();
```

## Where to go next

- [Working with `Money<TCurrency>`](money.md) — the monetary type that the resolved services back.
- [Working with exchange rates](exchange-rates.md) — the FX provider contracts you register above.
- [Bodu.Financial.DependencyInjection API reference](xref:Bodu.Financial.DependencyInjection) — full namespace overview.
