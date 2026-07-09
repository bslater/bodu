---
title: Bodu.Financial — Core concepts
---

# Bodu.Financial — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/financial/index.md), and refer back whenever a term feels imprecise.

For the high-level shape of the library and the namespace map, start with the [introduction](index.md). `Bodu.Financial` is part of the **[Numerics & Financial](../topics/numerics-and-financial.md)** topic; the [topic concepts page](../topics/numerics-and-financial-concepts.md) covers the vocabulary shared across both libraries.

## Currency tag

A **currency tag** is a sealed class in `Bodu.Financial.Currencies` (one per ISO 4217 code) that implements <xref:Bodu.Financial.Currencies.ICurrency>. Tags exist solely to parameterise <xref:Bodu.Financial.Money`1> and to carry the currency's static metadata. They are never instantiated — every shipped tag declares a `private` constructor and exposes only `static` members.

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

The international standard for currency codes. Every <xref:Bodu.Financial.Currencies.ICurrency> tag carries the standard's three-letter alphabetic code (`USD`, `EUR`, `JPY`, `BHD`) and its declared minor-unit count. Validation throughout the library matches the standard's shape: codes are exactly three uppercase ASCII letters, and any value outside that shape is rejected at the boundary.

## Minor unit

The smallest commonly-issued denomination of a currency, expressed as the number of fractional digits its major unit subdivides into:

| `MinorUnits` | Examples |
|---|---|
| `0` | `JPY`, `KRW`, `CLP`, `ISK`, `VND`, `XAF`, `XOF` |
| `2` | `USD`, `EUR`, `GBP`, `AUD`, `CAD`, `CHF`, and most others |
| `3` | `BHD`, `IQD`, `JOD`, `KWD`, `LYD`, `OMR`, `TND` |

Construction of <xref:Bodu.Financial.Money`1> rounds to this precision; `Money<T>.FromMinorUnits(long)` and `Money<T>.ToMinorUnits()` bridge to integer minor-unit storage used by ledgers and wire formats.

## Cash rounding increment

A coin-or-note-aligned coarser denomination some currencies use for physical cash totals. CHF rounds to five rappen (`0.05m`), AUD and CAD cash totals round to five cents (`0.05m`), SEK / NOK / ISK round to the whole krona. <xref:Bodu.Financial.Currencies.ICurrency.CashRoundingIncrement> exposes the value; `Money<T>.RoundToCash()` snaps to it.

Cash rounding is a presentation choice for physical payments, not a storage rule. Electronic transactions retain full minor-unit precision; call `RoundToCash()` only at the point a total becomes a cash payment. The default of `0m` means "no special cash rounding", and `RoundToCash()` is a no-op for those currencies.

## Banker's rounding

Construction and arithmetic default to <xref:System.MidpointRounding.ToEven> — midpoint values round toward the nearer even final digit. `new Money<USD>(1.225m)` stores as `1.22m`; `new Money<USD>(1.235m)` stores as `1.24m`. This matches the .NET `decimal` and IEEE 754 default and avoids the upward statistical bias of `AwayFromZero`. Every API that rounds accepts a `MidpointRounding` argument so callers can opt into a different rule when a regulator or accounting policy demands one.

## Sub-minor-unit precision

<xref:Bodu.Financial.Money`1> is settlement-grade — every operation rounds to <xref:Bodu.Financial.Currencies.ICurrency.MinorUnits>. Calculation chains where each step's rounding would compound (compound interest, percentage-of-percentage, unit-rate products) need an exact intermediate representation.

`Money<T>.ToFraction()` returns a <xref:Bodu.Numerics.Fraction`1> over <xref:System.Numerics.BigInteger>, and `Money<T>.FromFraction(...)` snaps a fraction back to settlement precision in one rounding event. `Money<T>.MultiplyExact(Fraction<BigInteger>)` is the single-step shortcut. Use it for amortisation schedules, multi-rate FX chains, and any compound calculation where drift across hundreds of rounding events is unacceptable.

## `CalculatedMoney`

<xref:Bodu.Financial.CalculatedMoney> is the middle precision tier between settlement-grade <xref:Bodu.Financial.Money`1> and exact <xref:Bodu.Numerics.Fraction`1>. It is a runtime-tagged `readonly struct` that carries the full `decimal` precision (28–29 significant digits) through `+`, `-`, `*`, `/`, and the named `Multiply` / `Divide`, deferring rounding until `RoundToMoney()` settles it to a <xref:Bodu.Financial.Money> in a single event. `ToCalculated()` materialises one from either money type; mixing two currencies throws <xref:System.InvalidOperationException> at runtime. Reach for it when `decimal` precision is enough and a full `Fraction` round-trip is heavier than the calculation warrants.

## `MonetaryContext`

<xref:Bodu.Financial.MonetaryContext> bundles the rounding strategy (<xref:Bodu.Financial.IRoundingStrategy>), the <xref:Bodu.Financial.ScalePolicy>, the <xref:Bodu.Financial.CashRoundingPolicy>, the <xref:Bodu.Financial.AllocationPolicy>, and the <xref:Bodu.Financial.ConversionRoundingPolicy> into one immutable record, so a settlement regime can be configured once and carried by name through the DI container. It governs *operation* boundaries — multiplication, division, conversion, and `CalculatedMoney.RoundToMoney()` — not allocation residual distribution, which is fixed. `MonetaryContext.Default` is banker's rounding at currency minor-unit scale.

## Fair allocation

Splitting `$1.00` into three shares cannot return `[0.33, 0.33, 0.33]` — that loses a cent. `Money<T>.Allocate(int parts)` distributes the residual minor units one per share from the start of the array so the sum equals the original exactly:

```csharp
new Money<USD>(0.10m).Allocate(3);   // [0.04, 0.03, 0.03]
new Money<USD>(-10m).Allocate(3);    // [-3.34, -3.33, -3.33]
```

`Money<T>.Allocate(ReadOnlySpan<decimal> ratios)` weights shares proportionally and uses the **largest-remainder (Hamilton) method** to distribute the residual: each slot receives one extra minor unit in descending order of its fractional remainder, with ties broken by stable input order. Zero-ratio slots never receive residual. The algorithm is deterministic, sign-preserving, and round-trip-stable across runs.

## Currency catalogue

`Bodu.Financial.Currencies` ships 184 sealed `ICurrency` tag classes — one per ISO 4217 code — split between active and historic:

- **Active** (155 codes): USD, EUR, GBP, JPY, AUD, CAD, CHF, CNY, INR, BRL, MXN, ZAR, …
- **Historic** (29 codes): the Euro-zone predecessors (ATS, BEF, CYP, DEM, EEK, ESP, FIM, FRF, GRD, HRK, IEP, ITL, LTL, LUF, LVL, MTL, NLG, PTE, SIT, SKK) plus other notable replacements (AZM, GHC, MZM, ROL, SRG, TMM, VEB, VEF, ZWL). Each historic tag declares `IsHistoric => true`, the `DemonetizedOn` date, and the `SuccessorIsoCode`.

Tag types are source-generated from `currencies.json`, and the runtime catalogue is built from the same generated data — no runtime reflection scans the assembly. The <xref:Bodu.Financial.Currencies.CurrencyCode> enum carries the same 184 codes plus a `None = 0` sentinel; `CurrencyCode.GetStatus()` / `IsActive()` / `IsHistoric()` read the per-member <xref:Bodu.Financial.Currencies.CurrencyStatus> tag.

## `CurrencyRegistry`

<xref:Bodu.Financial.Currencies.CurrencyRegistry> is the read-only runtime catalogue of <xref:Bodu.Financial.Currencies.CurrencyInfo> records — the runtime-shape counterpart of an `ICurrency` tag. It backs <xref:Bodu.Financial.Money> rounding and <xref:Bodu.Financial.MoneyBag> conversions, which resolve a currency's metadata at runtime.

The catalogue is closed: it is fixed to the shipped ISO 4217 set (active and historic) and exposes no runtime registration seam, so a currency outside it cannot be constructed as a runtime <xref:Bodu.Financial.Money>. For a generic amount in a unit outside ISO 4217 (a commodity, an in-game token), declare your own `ICurrency` tag and use `Money<TCurrency>`; to substitute or restrict the metadata used for the *shipped* currencies, install a custom `ICurrencyLookup` through <xref:Bodu.Financial.Currencies.CurrencyResolution>.

## `ExchangeRate`

<xref:Bodu.Financial.ExchangeRates.ExchangeRate> is an immutable record-struct describing a single observation: source ISO, destination ISO, observation date, the strictly-positive multiplier, the publishing provider's name, and a flag indicating whether the rate was derived from the reverse pair. It is the unit returned by providers and embedded in <xref:Bodu.Financial.ExchangeRates.RateLookupResult>; it deliberately does not round, so the destination currency's minor-unit precision applies only at the money boundary.

## `CurrencyPair`

<xref:Bodu.Financial.ExchangeRates.CurrencyPair> is the strongly-typed key for FX lookups — an immutable `(FromIsoCode, ToIsoCode)` record-struct that validates both codes at construction. Preferred over `(string, string)` tuples wherever a directional currency pair is used as a dictionary key or method argument; the named fields make the direction obvious and centralise validation. `Inverse()` returns the reverse-direction pair.

## `RateSeries`

<xref:Bodu.Financial.ExchangeRates.RateSeries> stores every observation for one `(pair, provider)` combination in two parallel sorted arrays — day numbers (<xref:System.DateOnly.DayNumber>) and rates — so resolution is allocation-free and runs in `O(log n)` over the day-number array via <xref:System.Array.BinarySearch``1(``0[],``0)>. The two-array layout improves cache locality compared to a <xref:System.Collections.Generic.SortedDictionary`2>, and instances are safe to share across threads after construction. Public APIs continue to accept and return <xref:System.DateOnly>; conversion to and from the internal day-number representation happens at the boundary.

The series is **immutable**. Use the companion <xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder> to construct or edit observations imperatively, or the copy-on-write helpers `RateSeries.WithRate(date, rate)` and `RateSeries.WithoutRate(date)` for single-edit return-new patterns. `RateSeries.GetObservations()` yields the observations as an <xref:Bodu.Financial.ExchangeRates.RateObservation> sequence in strictly ascending date order, and `RateSeries.ToBuilder()` returns a fresh builder pre-populated from the snapshot.

## `RateObservation`

<xref:Bodu.Financial.ExchangeRates.RateObservation> is the lightweight `(Date, Rate)` `readonly record struct` used as the transport shape for series enumeration, builder mutation, and bulk-import APIs. Unlike <xref:Bodu.Financial.ExchangeRates.ExchangeRate> it does not carry provider or inversion metadata — those are owned by the enclosing series. The type itself does not validate `Rate`; the surrounding series and builder reject zero or negative values at the boundary.

## `RateSeriesBuilder`

<xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder> is the mutable companion to <xref:Bodu.Financial.ExchangeRates.RateSeries>. It maintains strictly ascending unique observation dates and strictly positive rates while supporting single-observation edits and bulk import. The public surface distinguishes intent through three explicit shapes — `Add` (throws on duplicate), `Set` (throws on missing), and `Upsert` (insert-or-replace) — plus their `Try`-prefixed boolean siblings. Bulk import uses `AddRange` (rejects duplicates) and `UpsertRange` (replaces existing dates, rejects in-batch duplicates) with atomic-rollback semantics: a mid-batch validation failure leaves the builder unchanged. `ToSeries()` produces an immutable <xref:Bodu.Financial.ExchangeRates.RateSeries> snapshot; further builder mutations do not affect previously produced snapshots, and vice versa. Instances are not thread-safe.

## `RateSeriesKey` and `RateTableBuilder`

<xref:Bodu.Financial.ExchangeRates.RateSeriesKey> is a `readonly record struct` carrying a <xref:Bodu.Financial.ExchangeRates.CurrencyPair> and the publishing provider's identifier — the natural dictionary key when the same pair has rates from multiple sources.

<xref:Bodu.Financial.ExchangeRates.RateTableBuilder> is the higher-level mutable collection that owns one <xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder> per `(pair, provider)` key. It exposes `GetOrAddSeries`, `Upsert(pair, provider, date, rate)`, `TryGetBuilder` (returns the mutable builder), `TryGetSeries` (returns an immutable snapshot), and `ToSeries()` (snapshots every non-empty series). Use it for import workflows that ingest rate observations across many currency pairs and providers before producing immutable snapshots for production lookup. Like the builder it is not thread-safe.

## Timeless vs. dated provider

<xref:Bodu.Financial.ExchangeRates.IRateProvider> exposes a single `GetRate(from, to)` method returning a `decimal`. It is the right abstraction when the rate is "current" — a static table for unit tests, a daily snapshot, or a live mid-market ticker.

<xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> takes a <xref:System.DateOnly> and <xref:Bodu.Financial.ExchangeRates.RateLookupOptions> and returns an <xref:Bodu.Financial.ExchangeRates.RateLookupResult>. It is the right abstraction for ledger postings, tax reports, and any workflow where the *date* of the rate is part of the audit trail. Both `GetRate` (throws <xref:System.Collections.Generic.KeyNotFoundException>) and `TryGetRate` (returns `bool`) shapes are required by the contract.

## Provenance

A dated lookup returns an <xref:Bodu.Financial.ExchangeRates.RateLookupResult> — a `readonly record struct` with five properties — plus a family of derived convenience members. The properties:

| Property | Meaning |
|---|---|
| `Rate` | The resolved <xref:Bodu.Financial.ExchangeRates.ExchangeRate>: `Rate.Provider`, `Rate.Date` (the observed date, may differ from the request), `Rate.Rate` (the multiplier), and `Rate.IsInverted` (derived from the reverse-direction pair). |
| `RequestedDate` | The date the caller originally asked for. |
| `Resolution` | The <xref:Bodu.Financial.ExchangeRates.RateDateResolution> policy that fired (`Exact`, `PreviousOnOrBefore`, `NextOnOrAfter`, `Nearest`, `NearestPreferPrevious`, `NearestPreferNext`). |
| `OffsetDays` | Absolute day distance between `RequestedDate` and `Rate.Date`. |
| `Provenance` | The <xref:Bodu.Financial.ExchangeRates.RateProvenance> — provider, <xref:Bodu.Financial.ExchangeRates.RateOrigin> (`Live` / `Cache`), backend label, and cache age when served from a cache. |

The derived members live on <xref:Bodu.Financial.Extensions> rather than on the record — `ResolvedDate` (`== Rate.Date`), `SignedOffsetDays` (negative when the resolved date is earlier), `IsExactDate` (`OffsetDays == 0`), `IsPreviousDate`, and `IsFutureDate` — emitted as C# extension *properties* or classic extension *methods* depending on the build. That set is what lets accounting and tax workflows answer "which observed rate produced this number, and how far off the requested date was it?" without re-querying the table.

## Aggregating provider

Grouping several FX sources behind one entry point is no longer part of core `Bodu.Financial` — it lives in the `Bodu.Financial.ExchangeRates.Caching` package as [`AggregatingRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider). It wraps a set of named child providers and combines their results through a pluggable [`IRateAggregationStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.IRateAggregationStrategy):

- `PriorityFallbackStrategy` resolves every lookup with a deterministic first-available strategy — child providers are consulted in order and the first successful result is returned. This is the direct successor to the old composite stack, keeping fallback behaviour explicit and auditable, useful for stacking a primary ECB feed over an OANDA backup over a static last-known-good table.
- `AverageStrategy` returns the mean of all contributing providers for the pair.

Strategies are expressed through `IRateAggregationStrategy` (PriorityFallback / Average / custom) rather than a fixed enum, and the aggregator supports optional per-FX-pair routing so different pairs can resolve through different child providers.

## In-memory providers

<xref:Bodu.Financial.ExchangeRates.FixedRateTable> implements the timeless <xref:Bodu.Financial.ExchangeRates.IRateProvider> from a fixed `(from, to) → rate` dictionary. Same-currency lookups return `1m` without consulting the table, and a missing pair triggers an inverse-pair fallback that returns `1 / rate` — the convention most FX feeds use to keep the table minimal.

<xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider> implements the dated <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> from a flat sequence of <xref:Bodu.Financial.ExchangeRates.ExchangeRate> observations grouped into one <xref:Bodu.Financial.ExchangeRates.RateSeries> per pair. Each pair is described by exactly one series and therefore one provider, so composing rates from multiple publishing sources is done by stacking several tables behind an [`AggregatingRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider) (in `Bodu.Financial.ExchangeRates.Caching`) rather than mixing providers in a single table. Identity (same-currency) results carry the well-known `FixedDatedRateProvider.IdentityProviderName` label so audit consumers can filter by it.

## `MoneyConversionResult<TSource, TTarget>`

<xref:Bodu.Financial.MoneyConversionResult`2> is the audit record returned by the `ConvertTo<TTarget>(IDatedRateProvider, …)` extension methods on `Money<T>`, `Money`, and `MoneyBag`. It bundles the original source amount, the rounded target amount, and the full <xref:Bodu.Financial.ExchangeRates.RateLookupResult> that produced it — so the consumer sees both the answer and the provenance of the rate in a single value, without a second lookup.

## `MoneyBag`

<xref:Bodu.Financial.MoneyBag> is an immutable mixed-currency portfolio — a snapshot of balances across multiple ISO codes. Mutators return new instances; zero balances are pruned automatically on every operation; enumeration yields one <xref:Bodu.Financial.Money> per non-zero currency in lexicographic ISO order, so iteration is stable and reproducible across runs.

The bag is the type that models the **aggregate-then-convert** pattern: accumulate per-currency balances during a batch, then convert the entire bag to a single target currency once at the boundary via `MoneyBag.ConvertTo<TTarget>(IRateProvider)` or its dated counterpart. Compared to converting each amount on the way in, aggregate-then-convert needs one FX lookup per source currency instead of one per posting.

## JSON policies

<xref:Bodu.Financial.Serialization.FinancialJsonPolicy> selects the wire shape and parsing strictness used by the shipped `System.Text.Json` converters:

| Policy | Shape | Use case |
|---|---|---|
| `Strict` (default) | `{ "amount": 19.99, "currency": "USD" }` for money; `{ "balances": { … } }` for bags. Property names compare case-insensitively, duplicate properties are rejected, unknown properties are ignored, and a `currency` that does not match `TCurrency.IsoCode` is rejected. | Canonical ledger, persistence, audit. |
| `Lenient` | Same shape as `Strict`, but also normalises lowercase ISO codes to uppercase and trims surrounding whitespace before validation. | Import workflows that ingest spreadsheets and external feeds. Not suitable as a canonical storage shape. |
| `Compact` | Single JSON string `"19.99 USD"` for money; flat object `{ "USD": 19.99, "EUR": 12.34 }` for bags. Reads accept either ISO-prefix or ISO-suffix string forms. | Wire-size-sensitive APIs and human-readable logs. |

Register a policy via `options.AddFinancialJsonConverters(policy)`; this installs the five financial converters — for `Money<TCurrency>` (through a `JsonConverterFactory`), `Money`, `MoneyBag`, <xref:Bodu.Financial.ExchangeRates.ExchangeRate>, and <xref:Bodu.Financial.ExchangeRates.CurrencyPair>. Converters added to <xref:System.Text.Json.JsonSerializerOptions.Converters> take precedence over the type-level `[JsonConverter]` attribute that defaults to `Strict`.

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
- **[Numerics & Financial topic overview](../topics/numerics-and-financial.md)** — how this package and `Bodu.Numerics` fit together.
- **[Numerics & Financial topic concepts](../topics/numerics-and-financial-concepts.md)** — the vocabulary shared across both libraries.
- **[Bodu.Numerics introduction](../numerics/index.md)** — the rational-arithmetic library that backs `Money<T>.ToFraction()`.
