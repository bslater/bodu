---
title: Working with exchange rates
---

# Working with exchange rates

`Bodu.Financial` ships a complete foreign-exchange provider stack:
two contracts (timeless and dated), an immutable observation record,
a strongly-typed pair key, an `O(log n)` series store, in-memory
tables, a date-pinning adapter, and a conversion-audit record. This guide walks the surface
and the patterns it supports — unit-test rates, ledger postings, tax
reports, and multi-source feeds that carry their provenance.

## Concepts in one minute

- **Rate** — `ExchangeRate` is an immutable record-struct (`FromIsoCode`, `ToIsoCode`, `Date`, `Rate`, `Provider`, `IsInverted`). Rounding is deferred to the money boundary.
- **Pair** — `CurrencyPair` is the `(From, To)` key. Validates both ISO codes at construction; exposes `Inverse()`.
- **Observation** — `RateObservation` is the lightweight `(Date, Rate)` carrier used by series enumeration, builder mutation, and bulk-import APIs.
- **Series** — `RateSeries` stores every observation for one `(pair, provider)` in two parallel sorted arrays. Resolution is `O(log n)` via `Array.BinarySearch`, allocation-free. Immutable; use `RateSeriesBuilder` to construct or edit observations.
- **Builder** — `RateSeriesBuilder` is the mutable companion that maintains strictly ascending unique dates and produces immutable `RateSeries` snapshots via `ToSeries()`.
- **Table** — `RateTableBuilder` keys one builder per `(pair, provider)` for multi-series import workflows.
- **Provider** — `IRateProvider` is timeless; `IDatedRateProvider` is dated and returns a `RateLookupResult` with provenance.
- **Lookup result** — `RateLookupResult` carries the rate, requested date, resolution policy, and offset-day distance.

See the [core concepts page](../../docs/financial/concepts.md) for
the long-form treatment of every `RateDateResolution` policy,
the [exchange-rate types catalogue](exchange-types.md) for a
scenario-driven map of every type below, and
[Exchange-rate lookups on a known dataset](exchange-rate-lookups.md)
for a worked results matrix showing how each lookup option changes the
answer.

## A minimal in-memory provider

For unit tests, fixtures, and "current rate" lookups,
`FixedRateTable` backed by a flat dictionary is the smallest
implementation:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

Dictionary<(string From, string To), decimal> rates = new()
{
    { ("USD", "EUR"), 0.93m },
};
FixedRateTable table = new(rates);

table.GetRate("USD", "EUR");  // 0.93
table.GetRate("EUR", "USD");  // 1 / 0.93   (inverse fallback)
table.GetRate("USD", "USD");  // 1m         (identity)

Money<EUR> eur = new Money<USD>(100m).Convert<EUR>(table.GetRate("USD", "EUR"));
```

The table short-circuits same-currency lookups to `1m` and tries the
inverse pair (returning `1 / rate`) when only the reverse direction
is present. Missing pairs (and their inverse) throw
`KeyNotFoundException`.

## Timeless vs. dated lookup

Reach for `IRateProvider` when the date of the rate is not
part of what you record — a unit-test fixture, a daily snapshot, a
live ticker. Conversion is a single multiplication.

Reach for `IDatedRateProvider` when the date *is* part of the
audit trail — ledger postings, tax reports, regulatory filings. It
returns a `RateLookupResult` carrying the provider name, the
actual observation date used, the offset-day distance, the
resolution policy, and the inversion flag. The dated side ships
paired `GetRate` (throws) and `TryGetRate` (allocation-free `bool`);
the timeless contract has only the throwing form.

## Dated lookup with provenance

`FixedDatedRateProvider` accepts a flat sequence of
`ExchangeRate` observations and groups them into one
`RateSeries` per `(pair, provider)`. Every observation for a
pair must carry the same provider name; to group rates from multiple
sources, see [`AggregatingRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider)
in `Bodu.Financial.ExchangeRates.Caching`.

```csharp
ExchangeRate[] observations =
{
    new(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 6, 14), 0.928m, "ECB"),
    new(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 6, 17), 0.931m, "ECB"),
    new(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 6, 18), 0.930m, "ECB"),
};
FixedDatedRateProvider table = new(observations);

RateLookupResult lookup = table.GetRate(
    "USD", "EUR",
    new DateOnly(2024, 6, 15),                  // Saturday — no observation
    RateLookupOptions.PreviousWithin(3));

lookup.Rate.Rate;      // 0.928m
lookup.Rate.Date;      // 2024-06-14 — observation date actually used
lookup.Rate.Provider;  // "ECB"
lookup.RequestedDate;  // 2024-06-15
lookup.Resolution;     // PreviousOnOrBefore
lookup.OffsetDays;     // 1   (lookup.IsExactDate => false)
```

Same-currency lookups return a synthetic identity rate tagged with
`FixedDatedRateProvider.IdentityProviderName` (`"Identity"`), so
audit consumers can filter pass-throughs without a magic-string.

### Lookup options

`RateLookupOptions` carries the resolution policy and a
tolerance window. Use the static factories for the common shapes:

| Factory | Resolution | Use case |
|---|---|---|
| `Exact` | `Exact` | Strict-match audit; fail fast when missing. |
| `PreviousWithin(int)` | `PreviousOnOrBefore` | Accounting and tax — never selects a future rate. |
| `NextWithin(int)` | `NextOnOrAfter` | Forward-looking pricing. |
| `NearestWithin(int)` | `NearestPreferPrevious` | General convenience; ties prefer the earlier date. |

For finer control, construct the record directly with
`RateDateResolution.Nearest` (rejects ties),
`NearestPreferPrevious`, or `NearestPreferNext`. `AllowInverse` and
`AllowSameCurrencyIdentityRate` (both default `true`) disable the
reverse-pair fallback and identity short-circuit.

## Building a series imperatively

`RateSeries` is immutable, so the construction path for series
that aren't shipped as a one-shot literal is `RateSeriesBuilder`.
Use it for manual data entry, streaming imports, and merge-with-history
flows. Three explicit shapes distinguish caller intent:

- `Add(date, rate)` — throws if the date is already present (the data
  is wrong if you see this).
- `Set(date, rate)` — throws if the date is missing (you expected an
  observation to be there).
- `Upsert(date, rate)` — insert-or-replace; the right shape for merge
  semantics.

Each has a `Try`-prefixed `bool` sibling. Bulk import uses `AddRange`
(rejects duplicates outright) and `UpsertRange` (replaces existing
dates; rejects in-batch duplicates). Both apply atomic rollback: a
mid-batch validation failure leaves the builder unchanged.

```csharp
CurrencyPair pair = new(CurrencyCode.USD, CurrencyCode.AUD);
RateSeriesBuilder builder = new(pair, "RBA");

builder.Add(new DateOnly(2026, 6, 1), 1.50m);
builder.AddRange(new[]
{
    new RateObservation(new DateOnly(2026, 6, 2), 1.51m),
    new RateObservation(new DateOnly(2026, 6, 3), 1.52m),
});
builder.Upsert(new DateOnly(2026, 6, 3), 1.53m);  // replaces 1.52m

RateSeries snapshot = builder.ToSeries();
```

`ToSeries()` produces a fresh immutable `RateSeries` that is
isolated from further builder mutations. Calling `ToSeries()` on an
empty builder throws `InvalidOperationException` because the immutable
series contract requires at least one observation.

### Copy-on-write edits on an existing series

When the source of truth is already a `RateSeries` snapshot,
the copy-on-write helpers wrap the builder roundtrip for the common
single-edit case:

```csharp
RateSeries withUpdate = original.WithRate(new DateOnly(2026, 6, 3), 1.55m);
RateSeries withRemoval = original.WithoutRate(new DateOnly(2026, 6, 3));

foreach (var observation in original.GetObservations())
{
    // observation.Date / observation.Rate
}
```

`original` is unchanged in both cases. `ToBuilder()` returns a fresh
builder seeded from the snapshot for multi-edit workflows.

## Editing across many pairs and providers

When import data arrives flat — many pairs from many providers — keep
the builder bookkeeping in `RateTableBuilder`. It owns one
`RateSeriesBuilder` per `(pair, provider)` key and exposes
both lazy creation and a multi-series snapshot operation:

```csharp
RateTableBuilder table = new();

table.Upsert(new CurrencyPair(CurrencyCode.USD, CurrencyCode.AUD), "RBA", new DateOnly(2026, 6, 1), 1.50m);
table.Upsert(new CurrencyPair(CurrencyCode.USD, CurrencyCode.JPY), "BoJ", new DateOnly(2026, 6, 1), 110m);

// Reach for the underlying builder if you need bulk operations on one series.
RateSeriesBuilder rba = table.GetOrAddSeries(new CurrencyPair(CurrencyCode.USD, CurrencyCode.AUD), "RBA");
rba.AddRange(/* observations */);

// Snapshot every non-empty series in one pass.
IReadOnlyList<RateSeries> snapshots = table.ToSeries();
```

`TryGetSeries` returns a fresh immutable snapshot when the series
exists and is non-empty; `TryGetBuilder` returns the mutable builder
directly. Empty builders are skipped by `ToSeries()` because an
immutable series cannot be empty. The table is not thread-safe; use
external synchronisation for concurrent edits.

## Grouping providers with fallback

Grouping several providers behind one entry point — prioritised fallback,
averaging, or per-FX-pair routing — lives in the
[`Bodu.Financial.ExchangeRates.Caching`](xref:Bodu.Financial.ExchangeRates.Caching)
package as [`AggregatingRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider).
It wraps an ordered set of **named** dated providers; the default
[`PriorityFallbackStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.PriorityFallbackStrategy)
consults them in order and returns the first success.

```csharp
IDatedRateProvider stack = new AggregatingRateProvider(new[]
{
    new NamedDatedRateProvider("ECB", new FixedDatedRateProvider(ecbObservations)),
    new NamedDatedRateProvider("OANDA", new FixedDatedRateProvider(oandaObservations)),
});

RateLookupResult lookup = stack.GetRate(
    "USD", "GBP",
    new DateOnly(2024, 6, 15),
    RateLookupOptions.PreviousWithin(7));

// lookup.Rate.Provider identifies which underlying provider answered.
```

Priority fallback never re-orders results — if the primary returns a
four-day-old rate before the backup is consulted, that result wins. Other
strategies (averaging, or a custom
[`IRateAggregationStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.IRateAggregationStrategy))
and per-FX-pair routing are covered in the
[caching and aggregating guide](exchange-rate-caching.md).

## Pinning a date to a dated provider

`DatedRateProviderAdapter` exposes a dated provider through
the timeless `IRateProvider` surface by pinning a fixed
valuation date and options. Reach for it when an existing consumer
accepts only the timeless contract — for example
`MoneyBag.ConvertTo<TTarget>(IRateProvider)` — but the rates
should still come from a dated source:

```csharp
IRateProvider periodEnd = new DatedRateProviderAdapter(
    inner:   datedProvider,
    date:    new DateOnly(2024, 6, 30),
    options: RateLookupOptions.PreviousWithin(7));

Money<USD> totalUsd = wallet.ConvertTo<USD>(periodEnd);
```

The adapter delegates to the inner provider and returns only the raw
rate. To preserve provenance, call the dated provider directly.

## Direction-typed rates: `ExchangeRate<TBase, TQuote>`

When both ends of a conversion are known at the call site, the
compile-time-typed <xref:Bodu.Financial.ExchangeRates.ExchangeRate`2> encodes the
direction in its type parameters, so applying a rate the wrong way
round is a build error rather than a runtime surprise. It pairs with
the typed `Money<TCurrency>.Convert<TQuote>(ExchangeRate<TCurrency, TQuote>)`
overload:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

var typed = new ExchangeRate<USD, EUR>(0.928m, new DateOnly(2024, 6, 14), "ECB");

Money<EUR> eur = new Money<USD>(100m).Convert(typed);   // EUR 92.80
ExchangeRate<EUR, USD> reverse = typed.Inverse();       // reciprocal, still typed

ExchangeRate runtime = typed.ToRuntime();               // erase to the runtime form
var back = ExchangeRate<USD, EUR>.FromRuntime(runtime); // throws on ISO mismatch
```

`Convert(Money<USD>)` on the typed rate and `Money<USD>.Convert(typed)`
are equivalent; both round to the destination minor-unit precision
(`MidpointRounding.ToEven` by default).

## Audit-grade conversion through `Money<TCurrency>`

`Money<T>.Convert<TTarget>(decimal)` is the lowest-level conversion
(supply the rate, it rounds to the destination minor-unit precision).
When the rate comes from a dated provider and provenance matters,
prefer the extension methods on `Money<T>` and `Money`. They
resolve the rate, apply it, and return either the converted amount
(`ConvertTo`) or the full audit record (`ConvertToWithRate`):

```csharp
Money<USD> price = new(100m);

MoneyConversionResult<USD, EUR> audited = price.ConvertToWithRate<USD, EUR>(
    provider, new DateOnly(2024, 6, 15),
    RateLookupOptions.PreviousWithin(3));

audited.SourceAmount;                // Money<USD> 100.00
audited.TargetAmount;                // Money<EUR>  92.80
audited.ExchangeRate.Rate.Provider;  // "ECB"
audited.ExchangeRate.OffsetDays;     // 1
```

`Money` has analogous `ConvertTo` and `ConvertToWithRate`
extension methods for runtime-tagged amounts. For bags, see
`MoneyBag.ConvertToWithAudit<TTarget>(...)`, which returns one
`MoneyBagConversionLine` per source currency alongside the total.

## Common patterns

| Scenario | Reach for |
|---|---|
| Unit-test rates; "current rate" caches | `FixedRateTable` |
| In-memory table where the date matters | `FixedDatedRateProvider` + `RateLookupOptions.PreviousWithin(...)` |
| Primary feed plus fallbacks | `AggregatingRateProvider` (in `Bodu.Financial.ExchangeRates.Caching`) over multiple dated providers |
| Reporting period that pins one date everywhere | `DatedRateProviderAdapter` over the period-end date |
| Ledger entry that records the rate provenance | `Money<T>.ConvertToWithRate<,>(provider, date, options)` returning `MoneyConversionResult<,>` |
| Runtime-tagged amount via a dated provider | `MoneyExchangeRateExtensions.ConvertToWithRate(...)` |
| Aggregate-then-convert a bag with per-line provenance | `MoneyBag.ConvertToWithAudit<TTarget>(provider, date, options)` |
| Build a new series imperatively, or merge incoming observations into an existing one | `RateSeriesBuilder` + `Add` / `Upsert` / `AddRange` / `UpsertRange` |
| Single insert/replace/remove that returns a fresh immutable series | `RateSeries.WithRate(date, rate)` / `WithoutRate(date)` |
| Import flat rate data across many `(pair, provider)` combinations before producing immutable snapshots | `RateTableBuilder` |

## See also

- [Bodu.Financial introduction](../../docs/financial/index.md), [Core concepts](../../docs/financial/concepts.md), [Working with `Money<TCurrency>`](money.md)
- Contracts — [`IRateProvider`](xref:Bodu.Financial.ExchangeRates.IRateProvider), [`IDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.IDatedRateProvider)
- Values — [`ExchangeRate`](xref:Bodu.Financial.ExchangeRates.ExchangeRate), [`CurrencyPair`](xref:Bodu.Financial.ExchangeRates.CurrencyPair), [`RateObservation`](xref:Bodu.Financial.ExchangeRates.RateObservation), [`RateSeries`](xref:Bodu.Financial.ExchangeRates.RateSeries)
- Editing — [`RateSeriesBuilder`](xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder), [`RateSeriesKey`](xref:Bodu.Financial.ExchangeRates.RateSeriesKey), [`RateTableBuilder`](xref:Bodu.Financial.ExchangeRates.RateTableBuilder), [`RateBook`](xref:Bodu.Financial.ExchangeRates.RateBook)
- Providers — [`FixedRateTable`](xref:Bodu.Financial.ExchangeRates.FixedRateTable), [`FixedDatedRateProvider`](xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider), [`DatedRateProviderAdapter`](xref:Bodu.Financial.ExchangeRates.DatedRateProviderAdapter); grouping via [`AggregatingRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider)
- Lookup metadata — [`RateLookupOptions`](xref:Bodu.Financial.ExchangeRates.RateLookupOptions), [`RateLookupResult`](xref:Bodu.Financial.ExchangeRates.RateLookupResult), [`RateDateResolution`](xref:Bodu.Financial.ExchangeRates.RateDateResolution), [`MoneyConversionResult<TSource, TTarget>`](xref:Bodu.Financial.MoneyConversionResult`2)
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
