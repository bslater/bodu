---
title: Bodu.Financial — Introduction
---

# Bodu.Financial

**Bodu.Financial** is the monetary-primitives package of the Bodu suite. It ships type-parameter-tagged and runtime-tagged money types, a shipped catalogue of ~185 ISO 4217 currencies, an exchange-rate provider stack with both timeless and dated lookup, and JSON converters with three policy shapes for ledger-style, lenient-import, and compact-wire integrations. Part of the **[Numerics & Financial](../topics/numerics-and-financial.md)** topic.

The package depends on `Bodu.Numerics` so `Money<TCurrency>` can round-trip through `Fraction<BigInteger>` for sub-minor-unit-precise intermediate calculations — interest accumulation, percentage-of-percentage, and other chains where deferred rounding matters.

![Bodu.Financial namespace map — three namespaces and the exchange-rate provider stack](../../images/diagrams/financial-namespace-map.svg)

## Namespaces and headline types

### `Bodu.Financial`

| Type | Purpose |
|---|---|
| <xref:Bodu.Financial.Money`1> | Immutable, value-equatable monetary amount whose currency is encoded as the type parameter. Cross-currency arithmetic fails the build, not at runtime. |
| <xref:Bodu.Financial.Money> | Runtime-tagged sister type — currency carried as an ISO code string. The fallback when the currency is data rather than type, e.g. deserialisation or generic invoicing. |
| <xref:Bodu.Financial.MoneyBag> | Immutable mixed-currency portfolio. Aggregates per-ISO balances, prunes zero balances, enumerates in lexicographic ISO order. |
| <xref:Bodu.Financial.ICurrency> | Static-abstract interface carrying ISO code, minor-unit count, cash rounding increment, and demonetisation metadata. Implemented by every shipped currency tag. |
| <xref:Bodu.Financial.CurrencyInfo>, <xref:Bodu.Financial.CurrencyRegistry> | Runtime currency metadata record and a thread-safe registry over shipped + caller-registered custom currencies. |
| <xref:Bodu.Financial.ExchangeRate>, <xref:Bodu.Financial.ExchangeRatePair>, <xref:Bodu.Financial.ExchangeRateObservation>, <xref:Bodu.Financial.ExchangeRateSeries> | Immutable FX observation value object, strongly-typed (from, to) key, single dated observation, and an O(log n) time series over observations. |
| <xref:Bodu.Financial.ExchangeRateSeriesBuilder>, <xref:Bodu.Financial.ExchangeRateSeriesKey>, <xref:Bodu.Financial.ExchangeRateTableBuilder> | Mutable companion to `ExchangeRateSeries` for building or editing observations, the `(pair, provider)` key, and a higher-level multi-series editor for import workflows. |
| <xref:Bodu.Financial.IExchangeRateProvider>, <xref:Bodu.Financial.IDatedExchangeRateProvider> | Timeless and dated provider contracts. The dated form returns an <xref:Bodu.Financial.ExchangeRateLookupResult> with provenance metadata (offset days, resolution policy, provider name). |
| <xref:Bodu.Financial.FixedExchangeRateTable>, <xref:Bodu.Financial.FixedDatedExchangeRateProvider> | In-memory provider implementations. Grouping several FX sources behind one entry point (prioritised fallback, averaging, per-FX-pair routing) now lives in the `Bodu.Financial.ExchangeRates.Caching` package as [`AggregatingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider). |
| <xref:Bodu.Financial.TypedMoneyConversionResult`2> | Audit record returned by extension methods that convert through a dated provider — pairs source and target amount with the full lookup result. |

### `Bodu.Financial.Currencies`

Sealed tag types — one class per ISO 4217 code — each implementing `ICurrency`. Includes:

- **~150 active currencies** — USD, EUR, GBP, JPY, AUD, CAD, CHF, CNY, …
- **~30 historic currencies** — all twenty Euro-zone predecessors (ATS, BEF, DEM, ESP, FRF, GRD, IEP, ITL, NLG, PTE, …) plus other notable replacements (AZM, GHC, MZM, ROL, SRG, TMM, VEB, VEF, ZWL). Each declares `IsHistoric`, `DemonetizedOn`, `SuccessorIsoCode`.

The tag types only ever exist statically — every one has a `private` constructor — so `Money<USD>` is the only way to materialise a value.

### `Bodu.Financial.Serialization`

`System.Text.Json` converters and the <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> enum (`Strict`, `Lenient`, `Compact`). Register all converters at once via `FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters(options, policy)`.

## Allocation

Splitting `$1.00` into three shares cannot return `[0.33, 0.33, 0.33]` — that loses a cent. `Money<T>.Allocate(int parts)` splits an amount into shares whose sum equals the original exactly: the residual minor units are distributed one per share from the start of the array, and the rule is sign-stable, so a negative amount distributes the residual in the same direction. `Allocate(ReadOnlySpan<decimal> ratios)` weights the shares proportionally:

```csharp
Money<USD>[] shares = new Money<USD>(0.10m).Allocate(3);
// [0.04, 0.03, 0.03]  — sums to exactly 0.10

decimal[] ratios = { 1m, 1m, 2m };
Money<USD>[] split = new Money<USD>(100m).Allocate(ratios);
// [25.00, 25.00, 50.00]
```

The residual-distribution rule is the <xref:Bodu.Financial.AllocationPolicy> `LargestRemainder` (Hamilton) strategy — each leftover minor unit goes to the share with the largest fractional remainder, with ties broken by stable input order — so the parts always sum back to the original amount with no penny lost or invented, deterministically across runs. See [Working with `Money<TCurrency>`](../../guides/financial/money.md) for the validation rules and worked examples.

## Cash rounding

A handful of currencies round physical cash totals to a coarser increment than their electronic minor unit — Switzerland's 5-rappen coin, Australia's and Canada's 5-cent cash totals, New Zealand's 10-cent rounding, Sweden and Norway's whole-krona rounding. The shipped catalogue surfaces the convention through <xref:Bodu.Financial.ICurrency.CashRoundingIncrement> (the smallest cash denomination in the major unit, or `0m` when no special rounding applies), and `Money<T>.RoundToCash()` snaps an amount to the nearest multiple of that increment using banker's rounding by default:

```csharp
new Money<CHF>(12.34m).RoundToCash();    // CHF 12.35
new Money<NZD>(5.07m).RoundToCash();     // NZD 5.10
new Money<USD>(19.99m).RoundToCash();    // USD 19.99 — no-op, no cash increment
```

Cash rounding is a presentation choice for physical payments, not a storage rule: electronic transactions retain full minor-unit precision, so call `RoundToCash()` only at the point a total becomes a cash payment. An explicit `MidpointRounding` argument opts out of the banker's-rounding default.

## JSON serialization

`Money<TCurrency>`, `Money`, and `MoneyBag` all carry `[JsonConverter]` attributes, so the default `Strict` policy works without extra wiring. The <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> enum selects the wire shape and parsing strictness:

| Policy | Wire shape | Use case |
|---|---|---|
| `Strict` (default) | `{ "amount": 19.99, "currency": "USD" }` for money; `{ "balances": { … } }` for bags. | Canonical ledger, persistence, audit. |
| `Lenient` | Same shape as `Strict`, but normalizes lowercase ISO codes to uppercase and trims whitespace before validation. | Spreadsheet and external-feed import — not a canonical storage shape. |
| `Compact` | Single string `"19.99 USD"` for money; flat object `{ "USD": 19.99, "EUR": 12.34 }` for bags. | Wire-size-sensitive APIs and human-readable logs. |

```csharp
using Bodu.Financial.Serialization;

var options = new JsonSerializerOptions();
options.AddFinancialJsonConverters(FinancialJsonPolicy.Compact);

string json = JsonSerializer.Serialize(new Money<USD>(19.99m), options);   // "19.99 USD"
```

Deserialization on `Money<TCurrency>` rejects payloads whose `currency` field does not match `TCurrency.IsoCode` — currency drift surfaces as `JsonException`, not as a silently re-interpreted amount. Converters registered on <xref:System.Text.Json.JsonSerializerOptions.Converters> take precedence over the type-level attribute, which defaults to `Strict`.

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Type-safe monetary arithmetic that catches USD-vs-JPY mistakes at compile time | <xref:Bodu.Financial.Money`1> |
| Runtime-tagged amount for deserialisation or generic invoicing | <xref:Bodu.Financial.Money> |
| Multi-currency portfolio with aggregate-then-convert workflows | <xref:Bodu.Financial.MoneyBag> |
| Sub-minor-unit-precise interest / percentage chains | `Money<T>.ToFraction()` / `FromFraction()` / `MultiplyExact()` |
| Splitting an amount fairly across N shares without remainder loss | `Money<T>.Allocate(parts)` / `Allocate(ratios)` |
| Cash rounding for currencies with coarse coin denominations (CHF, AUD, NZD, …) | `Money<T>.RoundToCash()` and `ICurrency.CashRoundingIncrement` |
| ISO 4217 currency lookup at runtime | <xref:Bodu.Financial.CurrencyRegistry> |
| Custom or future currencies not in the shipped catalogue | `CurrencyRegistry.Register(CurrencyInfo)` + your own `ICurrency` tag |
| Dated FX lookup with audit-grade provenance metadata | <xref:Bodu.Financial.IDatedExchangeRateProvider> + <xref:Bodu.Financial.ExchangeRateLookupResult> |
| Prioritised fallback (or averaging) across multiple FX sources | [`AggregatingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider) (in `Bodu.Financial.ExchangeRates.Caching`) |
| Build a rate series imperatively, or edit an existing one and snapshot the result | <xref:Bodu.Financial.ExchangeRateSeriesBuilder>, `ExchangeRateSeries.ToBuilder()` / `WithRate(...)` / `WithoutRate(...)` |
| Import rates for many `(pair, provider)` combinations before producing immutable snapshots | <xref:Bodu.Financial.ExchangeRateTableBuilder> |
| Three JSON wire shapes (strict-canonical, lenient-import, compact-string) for the same monetary type | <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> |

## Design choices

- **Currency in the type system, not as a field.** `Money<USD>` and `Money<JPY>` are different types. Adding them fails the build rather than running with the wrong unit. The escape hatch is `Money` when the currency is genuinely unknown until runtime.
- **Banker's rounding default.** Construction rounds to the currency's minor-unit precision using `MidpointRounding.ToEven`, matching .NET's `decimal` convention and IEEE 754. Pass an explicit `MidpointRounding` argument to opt out.
- **Audit-friendly FX.** Dated provider lookups return `ExchangeRateLookupResult`, which carries the provider name, the actual date used, the offset-day distance from the requested date, the resolution policy that fired, and an inversion flag — enough to reconstruct any conversion after the fact.
- **Zero-balance pruning.** `MoneyBag` removes zero balances on every operation, so equality and enumeration are stable across insertion order and across serialisation round trips.

## Bodu.Financial.DependencyInjection

The companion **`Bodu.Financial.DependencyInjection`** package — a separate, Stable package — wires the stack into a `Microsoft.Extensions.DependencyInjection` container:

```bash
dotnet add package Bodu.Financial.DependencyInjection
```

The entry point is `AddBoduFinancial(...)` on <xref:Bodu.Financial.DependencyInjection.ServiceCollectionExtensions>. Both overloads register the default <xref:Bodu.Financial.ICurrency> lookup and return a fluent <xref:Bodu.Financial.DependencyInjection.IFinancialServiceBuilder> on which you compose the rest of the stack: a replacement currency lookup, named monetary contexts, timeless and dated exchange-rate providers, and the JSON converters under a chosen policy. Passing an `IConfiguration` additionally binds <xref:Bodu.Financial.DependencyInjection.FinancialOptions> (`JsonPolicy`, `UnknownCurrency`) from a configuration section (default `"Financial"`).

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

The package depends only on `Bodu.Financial` and `Microsoft.Extensions.DependencyInjection.Abstractions`; applications that construct the financial types by hand (consoles, libraries, tests) do not need to reference it. See [Financial dependency injection](../../guides/financial/dependency-injection.md) for the full builder surface, options binding, and the post-build `UseBoduFinancialCurrencyResolution` activation step.

## Where to go next

- **[Core concepts](concepts.md)** — glossary the rest of the documentation assumes.
- **[Getting started](getting-started.md)** — install the package and run minimal samples for `Money<TCurrency>`, `Money`, `MoneyBag`, the FX provider stack, and the JSON policies.
- **[Working with `Money<TCurrency>`](../../guides/financial/money.md)** — type-parameter currency, allocation, conversion, exact-arithmetic chains, formatting and parsing, cash rounding, historic currencies, `Money` interop, `MoneyBag` portfolios.
- **[Financial dependency injection](../../guides/financial/dependency-injection.md)** — `AddBoduFinancial`, the fluent builder, options binding, and activation.
- **[Numerics & Financial topic overview](../topics/numerics-and-financial.md)** — how this package, `Bodu.Numerics`, and the DI companion fit together.
- **[Numerics & Financial guides](../../guides/topics/numerics-and-financial.md)** — the guides landing page for both libraries.
- **[Bodu.Numerics introduction](../numerics/index.md)** — the rational-arithmetic library that backs `Money<T>.ToFraction()`.
- **[Bodu.Financial API reference](xref:Bodu.Financial)** — full type-by-type docs.
- **[Bodu.Financial.Currencies API reference](xref:Bodu.Financial.Currencies)** — the shipped ISO 4217 catalogue.
- **[Bodu.Financial.Serialization API reference](xref:Bodu.Financial.Serialization)** — JSON converters and policies.
