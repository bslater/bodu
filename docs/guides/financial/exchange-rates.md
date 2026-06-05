---
title: Working with exchange rates
---

# Working with exchange rates

`Bodu.Financial` ships a complete foreign-exchange provider stack:
two contracts (timeless and dated), an immutable observation record,
a strongly-typed pair key, an `O(log n)` series store, in-memory
tables, a deterministic composite for fallback, a date-pinning
adapter, and a conversion-audit record. This guide walks the surface
and the patterns it supports — unit-test rates, ledger postings, tax
reports, and multi-source feeds that carry their provenance.

## Concepts in one minute

- **Rate** — `ExchangeRate` is an immutable record-struct (`FromIsoCode`, `ToIsoCode`, `Date`, `Rate`, `Provider`, `IsInverted`). Rounding is deferred to the money boundary.
- **Pair** — `ExchangeRatePair` is the `(From, To)` key. Validates both ISO codes at construction; exposes `Inverse()`.
- **Observation** — `ExchangeRateObservation` is the lightweight `(Date, Rate)` carrier used by series enumeration, builder mutation, and bulk-import APIs.
- **Series** — `ExchangeRateSeries` stores every observation for one `(pair, provider)` in two parallel sorted arrays. Resolution is `O(log n)` via `Array.BinarySearch`, allocation-free. Immutable; use `ExchangeRateSeriesBuilder` to construct or edit observations.
- **Builder** — `ExchangeRateSeriesBuilder` is the mutable companion that maintains strictly ascending unique dates and produces immutable `ExchangeRateSeries` snapshots via `ToSeries()`.
- **Table** — `ExchangeRateTableBuilder` keys one builder per `(pair, provider)` for multi-series import workflows.
- **Provider** — `IExchangeRateProvider` is timeless; `IDatedExchangeRateProvider` is dated and returns an `ExchangeRateLookupResult` with provenance.
- **Lookup result** — `ExchangeRateLookupResult` carries the rate, requested date, resolution policy, and offset-day distance.

See the [core concepts page](../../docs/financial/concepts.md) for
the long-form treatment of every `ExchangeRateDateResolution` policy,
the [exchange-rate types catalogue](exchange-types.md) for a
scenario-driven map of every type below, and
[Exchange-rate lookups on a known dataset](exchange-rate-lookups.md)
for a worked results matrix showing how each lookup option changes the
answer.

## A minimal in-memory provider

For unit tests, fixtures, and "current rate" lookups,
`FixedExchangeRateTable` backed by a flat dictionary is the smallest
implementation:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;

Dictionary<(string From, string To), decimal> rates = new()
{
    { ("USD", "EUR"), 0.93m },
};
FixedExchangeRateTable table = new(rates);

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

Reach for `IExchangeRateProvider` when the date of the rate is not
part of what you record — a unit-test fixture, a daily snapshot, a
live ticker. Conversion is a single multiplication.

Reach for `IDatedExchangeRateProvider` when the date *is* part of the
audit trail — ledger postings, tax reports, regulatory filings. It
returns an `ExchangeRateLookupResult` carrying the provider name, the
actual observation date used, the offset-day distance, the
resolution policy, and the inversion flag. The dated side ships
paired `GetRate` (throws) and `TryGetRate` (allocation-free `bool`);
the timeless contract has only the throwing form.

## Dated lookup with provenance

`FixedDatedExchangeRateProvider` accepts a flat sequence of
`ExchangeRate` observations and groups them into one
`ExchangeRateSeries` per `(pair, provider)`. Every observation for a
pair must carry the same provider name; for rates from multiple
sources, stack tables behind a `CompositeDatedExchangeRateProvider`.

```csharp
ExchangeRate[] observations =
{
    new("USD", "EUR", new DateOnly(2024, 6, 14), 0.928m, "ECB"),
    new("USD", "EUR", new DateOnly(2024, 6, 17), 0.931m, "ECB"),
    new("USD", "EUR", new DateOnly(2024, 6, 18), 0.930m, "ECB"),
};
FixedDatedExchangeRateProvider table = new(observations);

ExchangeRateLookupResult lookup = table.GetRate(
    "USD", "EUR",
    new DateOnly(2024, 6, 15),                  // Saturday — no observation
    ExchangeRateLookupOptions.PreviousWithin(3));

lookup.Rate.Rate;      // 0.928m
lookup.Rate.Date;      // 2024-06-14 — observation date actually used
lookup.Rate.Provider;  // "ECB"
lookup.RequestedDate;  // 2024-06-15
lookup.Resolution;     // PreviousOnOrBefore
lookup.OffsetDays;     // 1   (lookup.IsExactDate => false)
```

Same-currency lookups return a synthetic identity rate tagged with
`FixedDatedExchangeRateProvider.IdentityProviderName` (`"Identity"`), so
audit consumers can filter pass-throughs without a magic-string.

### Lookup options

`ExchangeRateLookupOptions` carries the resolution policy and a
tolerance window. Use the static factories for the common shapes:

| Factory | Resolution | Use case |
|---|---|---|
| `Exact` | `Exact` | Strict-match audit; fail fast when missing. |
| `PreviousWithin(int)` | `PreviousOnOrBefore` | Accounting and tax — never selects a future rate. |
| `NextWithin(int)` | `NextOnOrAfter` | Forward-looking pricing. |
| `NearestWithin(int)` | `NearestPreferPrevious` | General convenience; ties prefer the earlier date. |

For finer control, construct the record directly with
`ExchangeRateDateResolution.Nearest` (rejects ties),
`NearestPreferPrevious`, or `NearestPreferNext`. `AllowInverse` and
`AllowSameCurrencyIdentityRate` (both default `true`) disable the
reverse-pair fallback and identity short-circuit.

## Building a series imperatively

`ExchangeRateSeries` is immutable, so the construction path for series
that aren't shipped as a one-shot literal is `ExchangeRateSeriesBuilder`.
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
ExchangeRatePair pair = new("USD", "AUD");
ExchangeRateSeriesBuilder builder = new(pair, "RBA");

builder.Add(new DateOnly(2026, 6, 1), 1.50m);
builder.AddRange(new[]
{
    new ExchangeRateObservation(new DateOnly(2026, 6, 2), 1.51m),
    new ExchangeRateObservation(new DateOnly(2026, 6, 3), 1.52m),
});
builder.Upsert(new DateOnly(2026, 6, 3), 1.53m);  // replaces 1.52m

ExchangeRateSeries snapshot = builder.ToSeries();
```

`ToSeries()` produces a fresh immutable `ExchangeRateSeries` that is
isolated from further builder mutations. Calling `ToSeries()` on an
empty builder throws `InvalidOperationException` because the immutable
series contract requires at least one observation.

### Copy-on-write edits on an existing series

When the source of truth is already an `ExchangeRateSeries` snapshot,
the copy-on-write helpers wrap the builder roundtrip for the common
single-edit case:

```csharp
ExchangeRateSeries withUpdate = original.WithRate(new DateOnly(2026, 6, 3), 1.55m);
ExchangeRateSeries withRemoval = original.WithoutRate(new DateOnly(2026, 6, 3));

foreach (var observation in original.GetObservations())
{
    // observation.Date / observation.Rate
}
```

`original` is unchanged in both cases. `ToBuilder()` returns a fresh
builder seeded from the snapshot for multi-edit workflows.

## Editing across many pairs and providers

When import data arrives flat — many pairs from many providers — keep
the builder bookkeeping in `ExchangeRateTableBuilder`. It owns one
`ExchangeRateSeriesBuilder` per `(pair, provider)` key and exposes
both lazy creation and a multi-series snapshot operation:

```csharp
ExchangeRateTableBuilder table = new();

table.Upsert(new ExchangeRatePair("USD", "AUD"), "RBA", new DateOnly(2026, 6, 1), 1.50m);
table.Upsert(new ExchangeRatePair("USD", "JPY"), "BoJ", new DateOnly(2026, 6, 1), 110m);

// Reach for the underlying builder if you need bulk operations on one series.
ExchangeRateSeriesBuilder rba = table.GetOrAddSeries(new ExchangeRatePair("USD", "AUD"), "RBA");
rba.AddRange(/* observations */);

// Snapshot every non-empty series in one pass.
IReadOnlyList<ExchangeRateSeries> snapshots = table.ToSeries();
```

`TryGetSeries` returns a fresh immutable snapshot when the series
exists and is non-empty; `TryGetBuilder` returns the mutable builder
directly. Empty builders are skipped by `ToSeries()` because an
immutable series cannot be empty. The table is not thread-safe; use
external synchronisation for concurrent edits.

## Composite fallback stack

`CompositeDatedExchangeRateProvider` wraps an ordered set of dated
providers. Every lookup consults them in construction order; the
first successful result wins.

```csharp
CompositeDatedExchangeRateProvider stack = new(new IDatedExchangeRateProvider[]
{
    new FixedDatedExchangeRateProvider(ecbObservations),
    new FixedDatedExchangeRateProvider(oandaObservations),
    new FixedDatedExchangeRateProvider(snapshotObservations),
});

ExchangeRateLookupResult lookup = stack.GetRate(
    "USD", "GBP",
    new DateOnly(2024, 6, 15),
    ExchangeRateLookupOptions.PreviousWithin(7));

// lookup.Rate.Provider identifies which underlying provider answered.
```

The composite never re-orders results — if the primary returns a
four-day-old rate before the backup is consulted, that result wins.
Cross-provider policies (e.g. preferring an exact-date hit across all
providers before any fallback) are deferred until a concrete consumer
requires them.

## Pinning a date to a dated provider

`DatedExchangeRateProviderAdapter` exposes a dated provider through
the timeless `IExchangeRateProvider` surface by pinning a fixed
valuation date and options. Reach for it when an existing consumer
accepts only the timeless contract — for example
`MoneyBag.ConvertTo<TTarget>(IExchangeRateProvider)` — but the rates
should still come from a dated source:

```csharp
IExchangeRateProvider periodEnd = new DatedExchangeRateProviderAdapter(
    inner:   datedProvider,
    date:    new DateOnly(2024, 6, 30),
    options: ExchangeRateLookupOptions.PreviousWithin(7));

Money<USD> totalUsd = wallet.ConvertTo<USD>(periodEnd);
```

The adapter delegates to the inner provider and returns only the raw
rate. To preserve provenance, call the dated provider directly.

## Audit-grade conversion through `Money<TCurrency>`

`Money<T>.Convert<TTarget>(decimal)` is the lowest-level conversion
(supply the rate, it rounds to the destination minor-unit precision).
When the rate comes from a dated provider and provenance matters,
prefer the extension methods on `Money<T>` and `Money`. They
resolve the rate, apply it, and return either the converted amount
(`ConvertTo`) or the full audit record (`ConvertToWithRate`):

```csharp
Money<USD> price = new(100m);

TypedMoneyConversionResult<USD, EUR> audited = price.ConvertToWithRate<USD, EUR>(
    provider, new DateOnly(2024, 6, 15),
    ExchangeRateLookupOptions.PreviousWithin(3));

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
| Unit-test rates; "current rate" caches | `FixedExchangeRateTable` |
| In-memory table where the date matters | `FixedDatedExchangeRateProvider` + `ExchangeRateLookupOptions.PreviousWithin(...)` |
| Primary feed plus fallbacks | `CompositeDatedExchangeRateProvider` over multiple dated providers |
| Reporting period that pins one date everywhere | `DatedExchangeRateProviderAdapter` over the period-end date |
| Ledger entry that records the rate provenance | `Money<T>.ConvertToWithRate<,>(provider, date, options)` returning `TypedMoneyConversionResult<,>` |
| Runtime-tagged amount via a dated provider | `MoneyExchangeRateExtensions.ConvertToWithRate(...)` |
| Aggregate-then-convert a bag with per-line provenance | `MoneyBag.ConvertToWithAudit<TTarget>(provider, date, options)` |
| Build a new series imperatively, or merge incoming observations into an existing one | `ExchangeRateSeriesBuilder` + `Add` / `Upsert` / `AddRange` / `UpsertRange` |
| Single insert/replace/remove that returns a fresh immutable series | `ExchangeRateSeries.WithRate(date, rate)` / `WithoutRate(date)` |
| Import flat rate data across many `(pair, provider)` combinations before producing immutable snapshots | `ExchangeRateTableBuilder` |

## See also

- [Bodu.Financial introduction](../../docs/financial/index.md), [Core concepts](../../docs/financial/concepts.md), [Working with `Money<TCurrency>`](money.md)
- Contracts — [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider), [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
- Values — [`ExchangeRate`](xref:Bodu.Financial.ExchangeRate), [`ExchangeRatePair`](xref:Bodu.Financial.ExchangeRatePair), [`ExchangeRateObservation`](xref:Bodu.Financial.ExchangeRateObservation), [`ExchangeRateSeries`](xref:Bodu.Financial.ExchangeRateSeries)
- Editing — [`ExchangeRateSeriesBuilder`](xref:Bodu.Financial.ExchangeRateSeriesBuilder), [`ExchangeRateSeriesKey`](xref:Bodu.Financial.ExchangeRateSeriesKey), [`ExchangeRateTableBuilder`](xref:Bodu.Financial.ExchangeRateTableBuilder), [`ExchangeRateBook`](xref:Bodu.Financial.ExchangeRateBook)
- Providers — [`FixedExchangeRateTable`](xref:Bodu.Financial.FixedExchangeRateTable), [`FixedDatedExchangeRateProvider`](xref:Bodu.Financial.FixedDatedExchangeRateProvider), [`CompositeDatedExchangeRateProvider`](xref:Bodu.Financial.CompositeDatedExchangeRateProvider), [`DatedExchangeRateProviderAdapter`](xref:Bodu.Financial.DatedExchangeRateProviderAdapter)
- Lookup metadata — [`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions), [`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult), [`ExchangeRateDateResolution`](xref:Bodu.Financial.ExchangeRateDateResolution), [`TypedMoneyConversionResult<TSource, TTarget>`](xref:Bodu.Financial.TypedMoneyConversionResult`2)
