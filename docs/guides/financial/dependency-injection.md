---
title: Financial dependency injection
---

# Financial dependency injection

The optional `Bodu.Financial.DependencyInjection` companion package wires the [`Bodu.Financial`](index.md) stack into a `Microsoft.Extensions.DependencyInjection` container. A single `AddFinancialService(...)` call registers the currency-lookup service and hands back a fluent <xref:Bodu.Financial.IFinancialServiceBuilder> on which you compose currency lookups, named monetary contexts, exchange-rate providers, and JSON converters. The registration extension methods live in the `Bodu.Financial` namespace, so a single `using Bodu.Financial;` brings them into scope.

If you are constructing the financial types by hand — in a console app or a test — keep using the `Bodu.Financial` constructors directly; this page is only relevant when you want the host to compose the stack for you.

## Install

```bash
dotnet add package Bodu.Financial.DependencyInjection
```

The package depends on `Bodu.Financial` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

## The registration surface

The entry point is the `AddFinancialService` `IServiceCollection` extension (in the `Bodu.Financial` namespace), with two overloads. Both register the default <xref:Bodu.Financial.ICurrency> lookup and return an <xref:Bodu.Financial.IFinancialServiceBuilder>.

| Method | Registers |
|---|---|
| `AddFinancialService(IServiceCollection, IConfiguration?, string sectionName = "Financial")` | The currency lookup, and binds <xref:Bodu.Financial.FinancialOptions> (its single `JsonPolicy` property) from the named configuration section. The `sectionName` constant is `ServiceCollectionExtensions.DefaultConfigurationSection`. |
| `AddFinancialService(IServiceCollection, Action<IFinancialServiceBuilder> configure)` | The same, with the builder configured imperatively by the delegate. |

## Composing the builder

The chainable `IFinancialServiceBuilder` extension methods (in the `Bodu.Financial` namespace) add the rest of the stack:

| Builder method | Effect |
|---|---|
| `AddCurrencyLookup<TLookup>()` | Replaces the default currency lookup with your `ICurrencyLookup` implementation. |
| `AddMonetaryContext(string name, MonetaryContext context)` | Registers a named monetary context (rounding, minor units, formatting). |
| `AddExchangeRateProvider<TProvider>()` / `AddExchangeRateProvider(provider)` | Registers a timeless <xref:Bodu.Financial.IExchangeRateProvider>, by type or by instance. |
| `AddDatedExchangeRateProvider<TProvider>()` / `AddDatedExchangeRateProvider(provider)` | Registers a dated <xref:Bodu.Financial.IDatedExchangeRateProvider>. |
| `AddFinancialJson(FinancialJsonPolicy policy = FinancialJsonPolicy.Strict)` | Registers the `System.Text.Json` converters under the chosen policy. |

```csharp
using Bodu.Financial;

builder.Services.AddFinancialService(configure: financial =>
{
    financial
        .AddFinancialJson(FinancialJsonPolicy.Strict)
        .AddExchangeRateProvider<MyRateProvider>()
        .AddDatedExchangeRateProvider<HistoricalRateProvider>();
});
```

The delegate overload is sugar over the first form — `AddFinancialService()` followed by calls on the returned builder produces the same registrations, so chain directly when that reads better:

```csharp
services
    .AddFinancialService()
    .AddFinancialJson()
    .AddDatedExchangeRateProvider<HistoricalRateProvider>();
```

## Named monetary contexts

`AddMonetaryContext(name, context)` registers a <xref:Bodu.Financial.MonetaryContext> as a **keyed singleton**, so an application can carry several rounding regimes side by side — for example a settlement context that follows banker's rounding and a cash-desk context that snaps to the currency's cash increment:

```csharp
services.AddFinancialService(financial =>
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

services.AddFinancialService(financial =>
{
    financial.AddDatedExchangeRateProvider(new FixedDatedExchangeRateProvider(ecbObservations));
});
```

To group several providers behind one registration — prioritised fallback, averaging, or per-FX-pair routing — and add read-through caching, use `AddAggregatedExchangeRateProvider(...)` from the `Bodu.Financial.ExchangeRates.Caching` package (its DI registration ships in the package, in the `Bodu.Financial.ExchangeRates` namespace), which registers an <xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider> as the application's single <xref:Bodu.Financial.IDatedExchangeRateProvider>. `ExchangeRateLookupResult.Rate.Provider` records which source answered, so the audit trail survives the composition. See the [caching and aggregating guide](exchange-rate-caching.md#dependency-injection) for the full walkthrough.

Neither `AddFinancialService` overload registers an FX provider by default — an application that never crosses currencies pays nothing for the contract.

## Consuming the financial JSON options

`AddFinancialJson(policy)` registers a configured `JsonSerializerOptions` as a keyed singleton under `FinancialServiceBuilderExtensions.JsonOptionsKey` (`"Financial"`), with the financial converters applied for the chosen <xref:Bodu.Financial.Serialization.FinancialJsonPolicy>:

```csharp
JsonSerializerOptions financialJson =
    provider.GetRequiredKeyedService<JsonSerializerOptions>(
        FinancialServiceBuilderExtensions.JsonOptionsKey);

string payload = JsonSerializer.Serialize(new Money<USD>(19.99m), financialJson);
```

## Binding options from configuration

Passing an `IConfiguration` binds <xref:Bodu.Financial.FinancialOptions> — which carries the single `JsonPolicy` property — from the named section (default `"Financial"`):

```jsonc
// appsettings.json
{
  "Financial": {
    "JsonPolicy": "Strict"
  }
}
```

`JsonPolicy` is a <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> value (`Strict`, `Lenient`, or `Compact`), which `AddFinancialJson()` reads when registering the converters.

```csharp
builder.Services.AddFinancialService(builder.Configuration);
```

## Activating static currency resolution

`Bodu.Financial` exposes a static currency-resolution surface used by parsing and formatting. After the container is built, call `UseCurrencyResolution` (an `IServiceProvider` extension in the `Bodu.Financial` namespace) once so the resolved `ICurrencyLookup` backs that static surface:

```csharp
var app = builder.Build();
app.Services.UseCurrencyResolution();
```

This is a composition-root operation — it installs the container's lookup as the process-wide ambient default via `CurrencyResolution.SetDefault`. Omitting the call leaves the registry-backed default in place, so existing applications behave identically without it. Only the runtime-tagged <xref:Bodu.Financial.Money> consults the ambient lookup; `Money<TCurrency>` reads its precision from the currency tag and is unaffected.

## End-to-end with the Generic Host

A complete wiring — host builder, financial registration, and a service that consumes the dated provider through constructor injection:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddFinancialService(builder.Configuration)   // binds the "Financial" section
    .AddFinancialJson()
    .AddMonetaryContext("Settlement", MonetaryContext.Default)
    .AddDatedExchangeRateProvider(new FixedDatedExchangeRateProvider(observations));

builder.Services.AddSingleton<SettlementService>();

IHost host = builder.Build();
host.Services.UseCurrencyResolution();        // ambient lookup = the DI lookup

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
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddFinancialService(financial =>
{
    financial.AddDatedExchangeRateProvider(new FixedDatedExchangeRateProvider(new ExchangeRate[]
    {
        new(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 6, 14), 0.928m, "Test"),
    }));
});
services.AddSingleton<SettlementService>();

using ServiceProvider provider = services.BuildServiceProvider();
SettlementService sut = provider.GetRequiredService<SettlementService>();
// sut.Settle(new Money<USD>(100m), new DateOnly(2024, 6, 14))  →  EUR 92.80
```

Two registration details matter for tests:

- The provider registrations use `TryAdd` semantics — the *first* registration for a contract wins. Register the fake before any production wiring runs, or use `services.Replace(ServiceDescriptor.Singleton<IDatedExchangeRateProvider>(fake))` (from `Microsoft.Extensions.DependencyInjection.Extensions`) to override an existing registration.
- `UseCurrencyResolution` mutates *process-wide* ambient state. Avoid calling it in unit tests; if a test must exercise a custom ambient lookup, prefer the flow-scoped `CurrencyResolution.PushScoped(...)` from `Bodu.Financial`, which restores the previous lookup on dispose and isolates parallel tests.

## See also

- [Working with `Money<TCurrency>`](money.md) — the monetary type that the resolved services back.
- [Working with exchange rates](exchange-rates.md) — the FX provider contracts you register above.
- [Exchange-rate types — a usage-scenario catalogue](exchange-types.md) — choosing between the provider implementations.
- [Bodu.Financial guides](index.md) — the member overview for this package.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.
- [`IFinancialServiceBuilder`](xref:Bodu.Financial.IFinancialServiceBuilder) · [`FinancialOptions`](xref:Bodu.Financial.FinancialOptions) — the builder and bound options (in `Bodu.Financial`).
- [Bodu.Financial API reference](xref:Bodu.Financial) — full namespace overview; the `AddFinancialService` / builder / `UseCurrencyResolution` extension methods live in the `Bodu.Financial` namespace.
