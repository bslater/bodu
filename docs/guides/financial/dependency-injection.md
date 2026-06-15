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

The delegate overload is sugar over the first form — `AddBoduFinancial()` followed by calls on the returned builder produces the same registrations, so chain directly when that reads better:

```csharp
services
    .AddBoduFinancial()
    .AddFinancialJson()
    .AddDatedExchangeRateProvider<HistoricalRateProvider>();
```

## Named monetary contexts

`AddMonetaryContext(name, context)` registers a <xref:Bodu.Financial.MonetaryContext> as a **keyed singleton**, so an application can carry several rounding regimes side by side — for example a settlement context that follows banker's rounding and a cash-desk context that snaps to the currency's cash increment:

```csharp
services.AddBoduFinancial(financial =>
{
    financial
        .AddMonetaryContext("Settlement", MonetaryContext.Default)
        .AddMonetaryContext("CashDesk", new MonetaryContext
        {
            Rounding     = MidpointRoundingStrategy.AwayFromZero,
            CashRounding = CashRoundingPolicy.CurrencyCashIncrement,
        });
});
```

Resolve a named context with the standard keyed-service surface:

```csharp
public sealed class CashDeskService
{
    private readonly MonetaryContext _context;

    public CashDeskService([FromKeyedServices("CashDesk")] MonetaryContext context) =>
        _context = context;
}

// …or imperatively from a built provider:
MonetaryContext cashDesk = provider.GetRequiredKeyedService<MonetaryContext>("CashDesk");
```

The name must be non-empty; `AddMonetaryContext` throws `ArgumentException` for a blank name and `ArgumentNullException` for a null context.

## Registering exchange-rate providers

The generic overloads register an implementation *type*; the instance overloads accept a pre-built provider. Both use `TryAdd` semantics, so the first registration for each contract wins:

```csharp
using Bodu.Financial;

services.AddBoduFinancial(financial =>
{
    financial.AddDatedExchangeRateProvider(new FixedDatedExchangeRateProvider(ecbObservations));
});
```

To group several providers behind one registration — prioritised fallback, averaging, or per-FX-pair routing — and add read-through caching, use the `Bodu.Financial.ExchangeRates.Caching.DependencyInjection` package's `AddAggregatedExchangeRateProvider(...)`, which registers an <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider> as the application's single <xref:Bodu.Financial.IDatedExchangeRateProvider>. `ExchangeRateLookupResult.Rate.Provider` records which source answered, so the audit trail survives the composition. See the [caching and aggregating guide](exchange-rate-caching.md#dependency-injection) for the full walkthrough.

Neither `AddBoduFinancial` overload registers an FX provider by default — an application that never crosses currencies pays nothing for the contract.

## Consuming the financial JSON options

`AddFinancialJson(policy)` registers a configured `JsonSerializerOptions` as a keyed singleton under <xref:Bodu.Financial.DependencyInjection.FinancialServiceBuilderExtensions>`.JsonOptionsKey` (`"Financial"`), with the financial converters applied for the chosen <xref:Bodu.Financial.Serialization.FinancialJsonPolicy>:

```csharp
JsonSerializerOptions financialJson =
    provider.GetRequiredKeyedService<JsonSerializerOptions>(
        FinancialServiceBuilderExtensions.JsonOptionsKey);

string payload = JsonSerializer.Serialize(new Money<USD>(19.99m), financialJson);
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

This is a composition-root operation — it installs the container's lookup as the process-wide ambient default via `CurrencyResolution.SetDefault`. Omitting the call leaves the registry-backed default in place, so existing applications behave identically without it. Only the runtime-tagged <xref:Bodu.Financial.Money> consults the ambient lookup; `Money<TCurrency>` reads its precision from the currency tag and is unaffected.

## End-to-end with the Generic Host

A complete wiring — host builder, financial registration, and a service that consumes the dated provider through constructor injection:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddBoduFinancial(builder.Configuration)   // binds the "Financial" section
    .AddFinancialJson()
    .AddMonetaryContext("Settlement", MonetaryContext.Default)
    .AddDatedExchangeRateProvider(new FixedDatedExchangeRateProvider(observations));

builder.Services.AddSingleton<SettlementService>();

IHost host = builder.Build();
host.Services.UseBoduFinancialCurrencyResolution();        // ambient lookup = the DI lookup

SettlementService settlement = host.Services.GetRequiredService<SettlementService>();
```

The consuming service depends only on the contract:

```csharp
public sealed class SettlementService
{
    private readonly IDatedExchangeRateProvider _rates;

    public SettlementService(IDatedExchangeRateProvider rates) =>
        _rates = rates;

    public Money<EUR> Settle(Money<USD> amount, DateOnly postingDate)
    {
        ExchangeRateLookupResult lookup = _rates.GetRate(
            "USD", "EUR", postingDate,
            ExchangeRateLookupOptions.PreviousWithin(3));

        return amount.Convert<EUR>(lookup.Rate.Rate);
    }
}
```

Swapping the fixed table for a live feed later means changing one registration; `SettlementService` never sees the difference.

## Swapping in a test double

Because consumers depend on <xref:Bodu.Financial.IDatedExchangeRateProvider> rather than a concrete feed, tests substitute a deterministic table — <xref:Bodu.Financial.FixedDatedExchangeRateProvider> over hand-written observations is usually all the fake you need:

```csharp
ServiceCollection services = new();
services.AddBoduFinancial(financial =>
{
    financial.AddDatedExchangeRateProvider(new FixedDatedExchangeRateProvider(new ExchangeRate[]
    {
        new("USD", "EUR", new DateOnly(2024, 6, 14), 0.928m, "Test"),
    }));
});
services.AddSingleton<SettlementService>();

using ServiceProvider provider = services.BuildServiceProvider();
SettlementService sut = provider.GetRequiredService<SettlementService>();
// sut.Settle(new Money<USD>(100m), new DateOnly(2024, 6, 14))  →  EUR 92.80
```

Two registration details matter for tests:

- The provider registrations use `TryAdd` semantics — the *first* registration for a contract wins. Register the fake before any production wiring runs, or use `services.Replace(ServiceDescriptor.Singleton<IDatedExchangeRateProvider>(fake))` (from `Microsoft.Extensions.DependencyInjection.Extensions`) to override an existing registration.
- `UseBoduFinancialCurrencyResolution` mutates *process-wide* ambient state. Avoid calling it in unit tests; if a test must exercise a custom ambient lookup, prefer the flow-scoped `CurrencyResolution.PushScoped(...)` from `Bodu.Financial`, which restores the previous lookup on dispose and isolates parallel tests.

## See also

- [Working with `Money<TCurrency>`](money.md) — the monetary type that the resolved services back.
- [Working with exchange rates](exchange-rates.md) — the FX provider contracts you register above.
- [Exchange-rate types — a usage-scenario catalogue](exchange-types.md) — choosing between the provider implementations.
- [Bodu.Financial guides](index.md) — the member overview for this package.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.
- [`IFinancialServiceBuilder`](xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder) · [`FinancialServiceBuilderExtensions`](xref:Bodu.Financial.DependencyInjection.FinancialServiceBuilderExtensions) · [`FinancialOptions`](xref:Bodu.Financial.DependencyInjection.FinancialOptions) · [`ServiceProviderExtensions`](xref:Bodu.Financial.DependencyInjection.ServiceProviderExtensions)
- [Bodu.Financial.DependencyInjection API reference](xref:Bodu.Financial.DependencyInjection) — full namespace overview.
