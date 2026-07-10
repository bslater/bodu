# Bodu.Financial.Samples.CurrencyServices

Currency services and host wiring: the ambient resolution seam, named monetary contexts, and the
`AddFinancialService` composition root.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.CurrencyServices
```

## What it demonstrates

- `Scenarios/AmbientResolution.cs` — `CurrencyResolution.Current` / `PushScoped`: runtime `Money`
  resolves currency metadata through the ambient lookup, so a scoped `RestrictedCurrencyLookup`
  makes unsupported currencies fail parsing inside the scope (and only there).
- `Scenarios/NamedContexts.cs` — `MonetaryContext` record overrides (`with`), settling the same
  `CalculatedMoney` under different policies, and `AddMonetaryContext("name", ctx)` keyed
  registration.
- `Scenarios/FinancialServiceHost.cs` — `AddFinancialService()` + `AddFinancialJson` +
  `AddDatedExchangeRateProvider(instance)`, then `UseCurrencyResolution()` to promote the DI
  lookup to the ambient seam, and consuming each registration (`ICurrencyLookup`, the keyed
  financial `JsonSerializerOptions`, `IDatedRateProvider`).
- `RestrictedCurrencyLookup.cs` — a delegating `ICurrencyLookup` allow-list decorator, the shape
  to copy for custom lookups.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial.DependencyInjection
```
