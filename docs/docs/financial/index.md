---
title: Bodu.Financial — Introduction
---

# Bodu.Financial

**Bodu.Financial** is the monetary-primitives package of the Bodu suite. It ships type-parameter-tagged and runtime-tagged money types, a shipped catalogue of ~185 ISO 4217 currencies, an exchange-rate provider stack with both timeless and dated lookup, and JSON converters with three policy shapes for ledger-style, lenient-import, and compact-wire integrations.

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
| <xref:Bodu.Financial.FixedExchangeRateTable>, <xref:Bodu.Financial.FixedDatedExchangeRateProvider>, <xref:Bodu.Financial.CompositeDatedExchangeRateProvider> | In-memory provider implementations and a composite stack for prioritised fallback across multiple FX sources. |
| <xref:Bodu.Financial.TypedMoneyConversionResult`2> | Audit record returned by extension methods that convert through a dated provider — pairs source and target amount with the full lookup result. |

### `Bodu.Financial.Currencies`

Sealed tag types — one class per ISO 4217 code — each implementing `ICurrency`. Includes:

- **~150 active currencies** — USD, EUR, GBP, JPY, AUD, CAD, CHF, CNY, …
- **~30 historic currencies** — all twenty Euro-zone predecessors (ATS, BEF, DEM, ESP, FRF, GRD, IEP, ITL, NLG, PTE, …) plus other notable replacements (AZM, GHC, MZM, ROL, SRG, TMM, VEB, VEF, ZWL). Each declares `IsHistoric`, `DemonetizedOn`, `SuccessorIsoCode`.

The tag types only ever exist statically — every one has a `private` constructor — so `Money<USD>` is the only way to materialise a value.

### `Bodu.Financial.Serialization`

`System.Text.Json` converters and the <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> enum (`Strict`, `Lenient`, `Compact`). Register all converters at once via `FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters(options, policy)`.

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
| Prioritised fallback across multiple FX sources | <xref:Bodu.Financial.CompositeDatedExchangeRateProvider> |
| Build a rate series imperatively, or edit an existing one and snapshot the result | <xref:Bodu.Financial.ExchangeRateSeriesBuilder>, `ExchangeRateSeries.ToBuilder()` / `WithRate(...)` / `WithoutRate(...)` |
| Import rates for many `(pair, provider)` combinations before producing immutable snapshots | <xref:Bodu.Financial.ExchangeRateTableBuilder> |
| Three JSON wire shapes (strict-canonical, lenient-import, compact-string) for the same monetary type | <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> |

## Design choices

- **Currency in the type system, not as a field.** `Money<USD>` and `Money<JPY>` are different types. Adding them fails the build rather than running with the wrong unit. The escape hatch is `Money` when the currency is genuinely unknown until runtime.
- **Banker's rounding default.** Construction rounds to the currency's minor-unit precision using `MidpointRounding.ToEven`, matching .NET's `decimal` convention and IEEE 754. Pass an explicit `MidpointRounding` argument to opt out.
- **Audit-friendly FX.** Dated provider lookups return `ExchangeRateLookupResult`, which carries the provider name, the actual date used, the offset-day distance from the requested date, the resolution policy that fired, and an inversion flag — enough to reconstruct any conversion after the fact.
- **Zero-balance pruning.** `MoneyBag` removes zero balances on every operation, so equality and enumeration are stable across insertion order and across serialisation round trips.

## Where to go next

- **[Core concepts](concepts.md)** — glossary the rest of the documentation assumes.
- **[Getting started](getting-started.md)** — install the package and run minimal samples for `Money<TCurrency>`, `Money`, `MoneyBag`, the FX provider stack, and the JSON policies.
- **[Working with `Money<TCurrency>`](../../guides/financial/money.md)** — type-parameter currency, allocation, conversion, exact-arithmetic chains, formatting and parsing, cash rounding, historic currencies, `Money` interop, `MoneyBag` portfolios.
- **[Bodu.Numerics introduction](../numerics/index.md)** — the rational-arithmetic library that backs `Money<T>.ToFraction()`.
- **[Bodu.Financial API reference](xref:Bodu.Financial)** — full type-by-type docs.
- **[Bodu.Financial.Currencies API reference](xref:Bodu.Financial.Currencies)** — the shipped ISO 4217 catalogue.
- **[Bodu.Financial.Serialization API reference](xref:Bodu.Financial.Serialization)** — JSON converters and policies.
