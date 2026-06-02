---
title: Bodu.Financial — Core concepts
---

# Bodu.Financial — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/financial/index.md), and refer back whenever a term feels imprecise.

For the high-level shape of the library and the namespace map, start with the [introduction](index.md).

## Currency tag

A **currency tag** is a sealed class in `Bodu.Financial.Currencies` (one per ISO 4217 code) that implements <xref:Bodu.Financial.ICurrency>. Tags exist solely to parameterise <xref:Bodu.Financial.Money`1> and to carry the currency's static metadata. They are never instantiated — every shipped tag declares a `private` constructor and exposes only `static` members.

Tags use C# 11 static-abstract members, so metadata is accessed through the type itself (`USD.IsoCode`, `USD.MinorUnits`) and through `TCurrency.IsoCode` from generic code constrained by `where TCurrency : ICurrency`. `IsoCode` and `MinorUnits` are required; `CashRoundingIncrement`, `IsHistoric`, `DemonetizedOn`, and `SuccessorIsoCode` are `static virtual` with sensible defaults so existing custom tags compile unchanged.

## `Money<TCurrency>` vs. `Money`

<xref:Bodu.Financial.Money`1> is **compile-time-typed**: the currency is the type parameter. `Money<USD>` and `Money<JPY>` are distinct types, and adding them is a compile error. Use it whenever the currency is known at the call site.

<xref:Bodu.Financial.Money> is **runtime-tagged**: it carries the currency as an ISO 4217 string. Cross-currency arithmetic surfaces as <xref:System.InvalidOperationException> at runtime rather than at build time. Use it when the currency is data — deserialised payloads, generic invoicing engines, configuration-driven accounting — and convert to typed money with `Money.As<T>()` or `TryAs` at the boundary where the currency becomes known.

Both types are immutable, value-equatable readonly structs and both round on construction.

## Cross-currency safety

> **Fails the build, not at runtime.**

Because the currency is in the type system, the compiler rejects the entire class of "added the wrong currencies" defects without a single runtime check:

```csharp
Money<USD> a = new(100m);
Money<JPY> b = new(2500m);
var oops = a + b;                  // CS0019 — operator '+' cannot be applied
Money<EUR> c = a;                  // CS0029 — cannot implicitly convert
bool eq = (a == new Money<JPY>(0m)); // CS0019 — comparison disallowed
```

Cross-currency conversion is available only through the explicit `Money<T>.Convert<TTarget>(decimal, MidpointRounding)` call, which forces the caller to supply the rate and the destination currency together.

## ISO 4217

The international standard for currency codes. Every <xref:Bodu.Financial.ICurrency> tag carries the standard's three-letter alphabetic code (`USD`, `EUR`, `JPY`, `BHD`) and its declared minor-unit count. Validation throughout the library matches the standard's shape: codes are exactly three uppercase ASCII letters, and any value outside that shape is rejected at the boundary.

## Minor unit

The smallest commonly-issued denomination of a currency, expressed as the number of fractional digits its major unit subdivides into:

| `MinorUnits` | Examples |
|---|---|
| `0` | `JPY`, `KRW`, `CLP`, `ISK`, `VND`, `XAF`, `XOF` |
| `2` | `USD`, `EUR`, `GBP`, `AUD`, `CAD`, `CHF`, and most others |
| `3` | `BHD`, `IQD`, `JOD`, `KWD`, `LYD`, `OMR`, `TND` |

Construction of <xref:Bodu.Financial.Money`1> rounds to this precision; `Money<T>.FromMinorUnits(long)` and `Money<T>.ToMinorUnits()` bridge to integer minor-unit storage used by ledgers and wire formats.

## Cash rounding increment

A coin-or-note-aligned coarser denomination some currencies use for physical cash totals. CHF rounds to five rappen (`0.05m`), AUD and CAD cash totals round to five cents (`0.05m`), SEK / NOK / ISK round to the whole krona. <xref:Bodu.Financial.ICurrency.CashRoundingIncrement> exposes the value; `Money<T>.RoundToCash()` snaps to it.

Cash rounding is a presentation choice for physical payments, not a storage rule. Electronic transactions retain full minor-unit precision; call `RoundToCash()` only at the point a total becomes a cash payment. The default of `0m` means "no special cash rounding", and `RoundToCash()` is a no-op for those currencies.

## Banker's rounding

Construction and arithmetic default to <xref:System.MidpointRounding.ToEven> — midpoint values round toward the nearer even final digit. `new Money<USD>(1.225m)` stores as `1.22m`; `new Money<USD>(1.235m)` stores as `1.24m`. This matches the .NET `decimal` and IEEE 754 default and avoids the upward statistical bias of `AwayFromZero`. Every API that rounds accepts a `MidpointRounding` argument so callers can opt into a different rule when a regulator or accounting policy demands one.

## Sub-minor-unit precision

<xref:Bodu.Financial.Money`1> is settlement-grade — every operation rounds to <xref:Bodu.Financial.ICurrency.MinorUnits>. Calculation chains where each step's rounding would compound (compound interest, percentage-of-percentage, unit-rate products) need an exact intermediate representation.

`Money<T>.ToFraction()` returns a <xref:Bodu.Numerics.Fraction`1> over <xref:System.Numerics.BigInteger>, and `Money<T>.FromFraction(...)` snaps a fraction back to settlement precision in one rounding event. `Money<T>.MultiplyExact(Fraction<BigInteger>)` is the single-step shortcut. Use it for amortisation schedules, multi-rate FX chains, and any compound calculation where drift across hundreds of rounding events is unacceptable.

## Fair allocation

Splitting `$1.00` into three shares cannot return `[0.33, 0.33, 0.33]` — that loses a cent. `Money<T>.Allocate(int parts)` distributes the residual minor units one per share from the start of the array so the sum equals the original exactly:

```csharp
new Money<USD>(0.10m).Allocate(3);   // [0.04, 0.03, 0.03]
new Money<USD>(-10m).Allocate(3);    // [-3.34, -3.33, -3.33]
```

`Money<T>.Allocate(ReadOnlySpan<decimal> ratios)` weights shares proportionally and uses the **largest-remainder (Hamilton) method** to distribute the residual: each slot receives one extra minor unit in descending order of its fractional remainder, with ties broken by stable input order. Zero-ratio slots never receive residual. The algorithm is deterministic, sign-preserving, and round-trip-stable across runs.

## Currency catalogue

`Bodu.Financial.Currencies` ships approximately 185 sealed `ICurrency` tag classes — one per ISO 4217 code — split between active and historic:

- **Active**: USD, EUR, GBP, JPY, AUD, CAD, CHF, CNY, INR, BRL, MXN, ZAR, … (around 150 codes).
- **Historic**: every Euro-zone predecessor (ATS, BEF, CYP, DEM, EEK, ESP, FIM, FRF, GRD, IEP, ITL, LTL, LUF, LVL, MTL, NLG, PTE, SIT, SKK) plus other notable replacements (AZM, GHC, MZM, ROL, SRG, TMM, VEB, VEF, ZWL). Each historic tag declares `IsHistoric => true`, the `DemonetizedOn` date, and the `SuccessorIsoCode`.

Tag types are source-generated from `currencies.json` and registered with <xref:Bodu.Financial.CurrencyRegistry> at first access — no runtime reflection scans the assembly.

## `CurrencyRegistry`

<xref:Bodu.Financial.CurrencyRegistry> is the thread-safe runtime table over <xref:Bodu.Financial.CurrencyInfo> records — the runtime-shape counterpart of an `ICurrency` tag. It backs <xref:Bodu.Financial.Money> rounding and <xref:Bodu.Financial.MoneyBag> conversions, both of which only know the ISO code at runtime.

Custom or future currencies (e.g. cryptocurrencies, in-game tokens, regional vouchers) are registered via `CurrencyRegistry.Register(CurrencyInfo)` or `TryRegister`. Custom entries layer on top of the shipped catalogue and take precedence on conflict, so consumers can override shipped metadata in pinch.

## `ExchangeRate`

<xref:Bodu.Financial.ExchangeRate> is an immutable record-struct describing a single observation: source ISO, destination ISO, observation date, the strictly-positive multiplier, the publishing provider's name, and a flag indicating whether the rate was derived from the reverse pair. It is the unit returned by providers and embedded in <xref:Bodu.Financial.ExchangeRateLookupResult>; it deliberately does not round, so the destination currency's minor-unit precision applies only at the money boundary.

## `ExchangeRatePair`

<xref:Bodu.Financial.ExchangeRatePair> is the strongly-typed key for FX lookups — an immutable `(FromIsoCode, ToIsoCode)` record-struct that validates both codes at construction. Preferred over `(string, string)` tuples wherever a directional currency pair is used as a dictionary key or method argument; the named fields make the direction obvious and centralise validation. `Inverse()` returns the reverse-direction pair.

## `ExchangeRateSeries`

<xref:Bodu.Financial.ExchangeRateSeries> stores every observation for one `(pair, provider)` combination in two parallel sorted arrays — day numbers (<xref:System.DateOnly.DayNumber>) and rates — so resolution is allocation-free and runs in `O(log n)` over the day-number array via <xref:System.Array.BinarySearch``1(``0[],``0)>. The two-array layout improves cache locality compared to a <xref:System.Collections.Generic.SortedDictionary`2>, and instances are safe to share across threads after construction. Public APIs continue to accept and return <xref:System.DateOnly>; conversion to and from the internal day-number representation happens at the boundary.

The series is **immutable**. Use the companion <xref:Bodu.Financial.ExchangeRateSeriesBuilder> to construct or edit observations imperatively, or the copy-on-write helpers `ExchangeRateSeries.WithRate(date, rate)` and `ExchangeRateSeries.WithoutRate(date)` for single-edit return-new patterns. `ExchangeRateSeries.GetObservations()` yields the observations as an <xref:Bodu.Financial.ExchangeRateObservation> sequence in strictly ascending date order, and `ExchangeRateSeries.ToBuilder()` returns a fresh builder pre-populated from the snapshot.

## `ExchangeRateObservation`

<xref:Bodu.Financial.ExchangeRateObservation> is the lightweight `(Date, Rate)` `readonly record struct` used as the transport shape for series enumeration, builder mutation, and bulk-import APIs. Unlike <xref:Bodu.Financial.ExchangeRate> it does not carry provider or inversion metadata — those are owned by the enclosing series. The type itself does not validate `Rate`; the surrounding series and builder reject zero or negative values at the boundary.

## `ExchangeRateSeriesBuilder`

<xref:Bodu.Financial.ExchangeRateSeriesBuilder> is the mutable companion to <xref:Bodu.Financial.ExchangeRateSeries>. It maintains strictly ascending unique observation dates and strictly positive rates while supporting single-observation edits and bulk import. The public surface distinguishes intent through three explicit shapes — `Add` (throws on duplicate), `Set` (throws on missing), and `Upsert` (insert-or-replace) — plus their `Try`-prefixed boolean siblings. Bulk import uses `AddRange` (rejects duplicates) and `UpsertRange` (replaces existing dates, rejects in-batch duplicates) with atomic-rollback semantics: a mid-batch validation failure leaves the builder unchanged. `ToSeries()` produces an immutable <xref:Bodu.Financial.ExchangeRateSeries> snapshot; further builder mutations do not affect previously produced snapshots, and vice versa. Instances are not thread-safe.

## `ExchangeRateSeriesKey` and `ExchangeRateTableBuilder`

<xref:Bodu.Financial.ExchangeRateSeriesKey> is a `readonly record struct` carrying a <xref:Bodu.Financial.ExchangeRatePair> and the publishing provider's identifier — the natural dictionary key when the same pair has rates from multiple sources.

<xref:Bodu.Financial.ExchangeRateTableBuilder> is the higher-level mutable collection that owns one <xref:Bodu.Financial.ExchangeRateSeriesBuilder> per `(pair, provider)` key. It exposes `GetOrAddSeries`, `Upsert(pair, provider, date, rate)`, `TryGetBuilder` (returns the mutable builder), `TryGetSeries` (returns an immutable snapshot), and `ToSeries()` (snapshots every non-empty series). Use it for import workflows that ingest rate observations across many currency pairs and providers before producing immutable snapshots for production lookup. Like the builder it is not thread-safe.

## Timeless vs. dated provider

<xref:Bodu.Financial.IExchangeRateProvider> exposes a single `GetRate(from, to)` method returning a `decimal`. It is the right abstraction when the rate is "current" — a static table for unit tests, a daily snapshot, or a live mid-market ticker.

<xref:Bodu.Financial.IDatedExchangeRateProvider> takes a <xref:System.DateOnly> and <xref:Bodu.Financial.ExchangeRateLookupOptions> and returns an <xref:Bodu.Financial.ExchangeRateLookupResult>. It is the right abstraction for ledger postings, tax reports, and any workflow where the *date* of the rate is part of the audit trail. Both `GetRate` (throws <xref:System.Collections.Generic.KeyNotFoundException>) and `TryGetRate` (returns `bool`) shapes are required by the contract.

## Provenance

A dated lookup returns an <xref:Bodu.Financial.ExchangeRateLookupResult> with everything required to reconstruct the conversion later:

| Field | Meaning |
|---|---|
| `Rate.Provider` | The publishing source's identifier. |
| `Rate.Date` | The date the returned rate was observed (may differ from the request). |
| `Rate.IsInverted` | Whether the rate was derived from the reverse-direction pair. |
| `RequestedDate` | The date the caller originally asked for. |
| `Resolution` | The <xref:Bodu.Financial.ExchangeRateDateResolution> policy that fired (`Exact`, `PreviousOnOrBefore`, `NextOnOrAfter`, `Nearest`, …). |
| `OffsetDays` | Absolute day distance between `RequestedDate` and `Rate.Date`. |
| `IsExactDate` | Convenience flag — `true` when `OffsetDays == 0`. |

That set is what lets accounting and tax workflows answer "which observed rate produced this number, and how far off the requested date was it?" without re-querying the table.

## Composite provider

<xref:Bodu.Financial.CompositeDatedExchangeRateProvider> wraps an ordered set of `IDatedExchangeRateProvider` instances and resolves every lookup with a deterministic first-available strategy: providers are consulted in construction order, and the first successful result is returned. This keeps fallback behaviour explicit and auditable — useful for stacking a primary ECB feed over an OANDA backup over a static last-known-good table.

More elaborate cross-provider policies (such as preferring an exact-date hit from any provider before any fallback) are intentionally deferred until a concrete consumer requires them.

## In-memory providers

<xref:Bodu.Financial.FixedExchangeRateTable> implements the timeless <xref:Bodu.Financial.IExchangeRateProvider> from a fixed `(from, to) → rate` dictionary. Same-currency lookups return `1m` without consulting the table, and a missing pair triggers an inverse-pair fallback that returns `1 / rate` — the convention most FX feeds use to keep the table minimal.

<xref:Bodu.Financial.FixedDatedExchangeRateProvider> implements the dated <xref:Bodu.Financial.IDatedExchangeRateProvider> from a flat sequence of <xref:Bodu.Financial.ExchangeRate> observations grouped into one <xref:Bodu.Financial.ExchangeRateSeries> per pair. Each pair is described by exactly one series and therefore one provider, so composing rates from multiple publishing sources is done by stacking several tables behind a `CompositeDatedExchangeRateProvider` rather than mixing providers in a single table. Identity (same-currency) results carry the well-known `FixedDatedExchangeRateProvider.IdentityProviderName` label so audit consumers can filter by it.

## `MoneyConversionResult<TSource, TTarget>`

<xref:Bodu.Financial.MoneyConversionResult`2> is the audit record returned by the `ConvertTo<TTarget>(IDatedExchangeRateProvider, …)` extension methods on `Money<T>`, `Money`, and `MoneyBag`. It bundles the original source amount, the rounded target amount, and the full <xref:Bodu.Financial.ExchangeRateLookupResult> that produced it — so the consumer sees both the answer and the provenance of the rate in a single value, without a second lookup.

## `MoneyBag`

<xref:Bodu.Financial.MoneyBag> is an immutable mixed-currency portfolio — a snapshot of balances across multiple ISO codes. Mutators return new instances; zero balances are pruned automatically on every operation; enumeration yields one <xref:Bodu.Financial.Money> per non-zero currency in lexicographic ISO order, so iteration is stable and reproducible across runs.

The bag is the type that models the **aggregate-then-convert** pattern: accumulate per-currency balances during a batch, then convert the entire bag to a single target currency once at the boundary via `MoneyBag.ConvertTo<TTarget>(IExchangeRateProvider)` or its dated counterpart. Compared to converting each amount on the way in, aggregate-then-convert needs one FX lookup per source currency instead of one per posting.

## JSON policies

<xref:Bodu.Financial.Serialization.FinancialJsonPolicy> selects the wire shape and parsing strictness used by the shipped `System.Text.Json` converters:

| Policy | Shape | Use case |
|---|---|---|
| `Strict` (default) | `{ "amount": 19.99, "currency": "USD" }` for money; `{ "balances": { … } }` for bags. Property names compare case-insensitively, duplicate properties are rejected, unknown properties are ignored, and a `currency` that does not match `TCurrency.IsoCode` is rejected. | Canonical ledger, persistence, audit. |
| `Lenient` | Same shape as `Strict`, but also normalises lowercase ISO codes to uppercase and trims surrounding whitespace before validation. | Import workflows that ingest spreadsheets and external feeds. Not suitable as a canonical storage shape. |
| `Compact` | Single JSON string `"19.99 USD"` for money; flat object `{ "USD": 19.99, "EUR": 12.34 }` for bags. Reads accept either ISO-prefix or ISO-suffix string forms. | Wire-size-sensitive APIs and human-readable logs. |

Register a policy via `options.AddFinancialJsonConverters(policy)`; converters added to <xref:System.Text.Json.JsonSerializerOptions.Converters> take precedence over the type-level `[JsonConverter]` attribute that defaults to `Strict`.

## Demonetisation

Historic currency metadata travels with the tag, so legacy data still round-trips through arithmetic and formatting. An `ICurrency` reports the state through three members:

- `IsHistoric` — `true` when the currency is no longer in active circulation.
- `DemonetizedOn` — the withdrawal date as a <xref:System.DateOnly>, or `null` when unknown.
- `SuccessorIsoCode` — the ISO code of the replacement currency, when one exists.

For example, `DEM.IsHistoric => true`, `DEM.DemonetizedOn => 2002-02-28`, `DEM.SuccessorIsoCode => "EUR"`. Historic currencies participate fully in arithmetic and serialisation; the metadata exists for filtering, reporting, and migration tooling, not to gate behaviour.

## Where to go next

- **[Introduction](index.md)** — the high-level shape of the library.
- **[Getting started](getting-started.md)** — install + runnable minimal samples for every concept above.
- **[Bodu.Financial guides](../../guides/financial/index.md)** — deep-dive walk-throughs for typed money, allocation, FX, and serialisation.
- **[Bodu.Numerics introduction](../numerics/index.md)** — the rational-arithmetic library that backs `Money<T>.ToFraction()`.
