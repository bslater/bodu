---
title: Exchange-rate types — a usage-scenario catalogue
---

# Exchange-rate types — a usage-scenario catalogue

`Bodu.Financial` ships roughly a dozen foreign-exchange types. They are
not interchangeable layers of the same idea — each one exists for a
specific job, and the fastest way to use the library well is to start
from *the scenario you are in* and let it point at the type. This page
is that map. For the conversion mechanics and the lookup-option
behaviour, see [Working with exchange rates](exchange-rates.md) and
[Exchange-rate lookups on a known dataset](exchange-rate-lookups.md).

## The one-line map

| You have / want… | Reach for | Kind |
|---|---|---|
| A single observed rate to pass around | [`ExchangeRate`](xref:Bodu.Financial.ExchangeRate) | value (record struct) |
| A direction-typed rate checked at compile time | [`ExchangeRate<TBase, TQuote>`](xref:Bodu.Financial.ExchangeRate`2) | value (struct) |
| A `(from, to)` dictionary key | [`ExchangeRatePair`](xref:Bodu.Financial.ExchangeRatePair) | value (record struct) |
| A bare `(date, rate)` point | [`ExchangeRateObservation`](xref:Bodu.Financial.ExchangeRateObservation) | value (record struct) |
| Every dated rate for one pair + provider | [`ExchangeRateSeries`](xref:Bodu.Financial.ExchangeRateSeries) | immutable store |
| To build or edit a series imperatively | [`ExchangeRateSeriesBuilder`](xref:Bodu.Financial.ExchangeRateSeriesBuilder) | mutable builder |
| A pair + provider key for many series | [`ExchangeRateSeriesKey`](xref:Bodu.Financial.ExchangeRateSeriesKey) | value (record struct) |
| Many series in one immutable store | [`ExchangeRateBook`](xref:Bodu.Financial.ExchangeRateBook) | immutable store |
| To import across many pairs/providers | [`ExchangeRateTableBuilder`](xref:Bodu.Financial.ExchangeRateTableBuilder) | mutable builder |
| A "current rate" lookup, no dates | [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider) + [`FixedExchangeRateTable`](xref:Bodu.Financial.FixedExchangeRateTable) | contract + impl |
| A dated lookup with audit metadata | [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider) + [`FixedDatedExchangeRateProvider`](xref:Bodu.Financial.FixedDatedExchangeRateProvider) | contract + impl |
| A primary feed with fallbacks | [`AggregatingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider) | aggregator (caching package) |
| To expose a dated source as timeless | [`DatedExchangeRateProviderAdapter`](xref:Bodu.Financial.DatedExchangeRateProviderAdapter) | adapter |
| How far back a provider serves rates | [`ExchangeRateHistoryAvailability`](xref:Bodu.Financial.ExchangeRateHistoryAvailability) | value (readonly record struct) |
| The rules applied on a date miss | [`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions) | options |
| The outcome of a dated lookup | [`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult) | value (record struct) |
| A converted amount + its rate provenance | [`MoneyConversionResult<,>`](xref:Bodu.Financial.MoneyConversionResult`2) | value (record struct) |

The rest of this page groups these by the role they play.

## Rate values — the things you observe and pass

### `ExchangeRate` — the runtime observation

[`ExchangeRate`](xref:Bodu.Financial.ExchangeRate) is the immutable
record struct every provider returns: source ISO, destination ISO,
observation date, a strictly-positive multiplier, the publishing
provider's name, and an `IsInverted` flag. **Reach for it** whenever
the direction is *data* — bank feeds, broker exports, a flat list of
quotes to load into a provider. It deliberately does **not** round;
the destination currency's minor-unit precision is applied only when
the rate meets a `Money` at the conversion boundary.

```csharp
var rate = new ExchangeRate(CurrencyCode.USD, CurrencyCode.EUR, new DateOnly(2024, 6, 14), 0.928m, "ECB");
decimal eurAmount = rate.Convert(100m);   // 92.80 — unrounded
```

### `ExchangeRate<TBase, TQuote>` — the compile-time-typed rate

When a contract, desk, or account is pinned to one specific direction,
[`ExchangeRate<TBase, TQuote>`](xref:Bodu.Financial.ExchangeRate`2)
encodes the direction in the type parameters. Applying it the wrong
way round, or to the wrong currency, is a **build error** rather than a
runtime surprise. **Reach for it** for `Money<TCurrency>` conversions
where both ends are known at the call site.

```csharp
var typed = new ExchangeRate<USD, EUR>(0.928m, new DateOnly(2024, 6, 14), "ECB");
Money<EUR> eur = Money.Of<USD>(100m).Convert(typed);   // typed Convert overload
ExchangeRate<EUR, USD> reverse = typed.Inverse();      // reciprocal, still typed

// Bridge to/from the runtime form:
ExchangeRate runtime = typed.ToRuntime();
var back = ExchangeRate<USD, EUR>.FromRuntime(runtime); // throws on ISO mismatch
```

### `ExchangeRatePair` — the directional key

[`ExchangeRatePair`](xref:Bodu.Financial.ExchangeRatePair) is an
immutable `(FromIsoCode, ToIsoCode)` record struct that validates both
codes at construction and exposes `Inverse()`. **Reach for it** instead
of a `(string, string)` tuple anywhere a currency direction is used as
a dictionary key or method argument — the named fields make the
direction unambiguous and centralise ISO validation.

### `ExchangeRateObservation` — the bare data point

[`ExchangeRateObservation`](xref:Bodu.Financial.ExchangeRateObservation)
is the lightweight `(Date, Rate)` carrier used for series enumeration,
builder mutation, and bulk import. It carries **no** provider or
inversion metadata — those belong to the enclosing series — so it is
the right shape when you are streaming points into a series and the
provider is already fixed by context.

## Storing dated rates — series, key, and book

### `ExchangeRateSeries` — one pair, one provider, every date

[`ExchangeRateSeries`](xref:Bodu.Financial.ExchangeRateSeries) holds
every observation for a single `(pair, provider)` in two parallel
sorted arrays (day numbers and rates), giving allocation-free
`O(log n)` resolution and good cache locality versus a
`SortedDictionary`. It is **immutable** and thread-safe to share after
construction. **Reach for it** as the read-side store behind a dated
provider; `GetObservations()` enumerates in ascending date order, and
the copy-on-write helpers `WithRate(date, rate)` / `WithoutRate(date)`
return a fresh series for single edits.

### `ExchangeRateSeriesBuilder` — the mutable companion

[`ExchangeRateSeriesBuilder`](xref:Bodu.Financial.ExchangeRateSeriesBuilder)
is how you construct or edit a series imperatively while keeping the
"strictly ascending unique dates, strictly positive rates" invariant.
The three explicit shapes encode intent:

- `Add(date, rate)` — throws if the date already exists (the data is wrong).
- `Set(date, rate)` — throws if the date is missing (you expected it).
- `Upsert(date, rate)` — insert-or-replace, the merge shape.

Each has a `Try`-prefixed boolean sibling; bulk import uses `AddRange`
(rejects duplicates) and `UpsertRange` (replaces existing dates) with
atomic rollback — a mid-batch failure leaves the builder untouched.
`ToSeries()` snapshots an immutable series; it throws on an empty
builder because a series must hold at least one observation.

```csharp
var builder = new ExchangeRateSeriesBuilder(new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.AUD), "RBA");
builder.Add(new DateOnly(2026, 6, 1), 1.50m);
builder.Upsert(new DateOnly(2026, 6, 1), 1.51m);   // replace
ExchangeRateSeries series = builder.ToSeries();
```

### `ExchangeRateSeriesKey` and `ExchangeRateBook`

[`ExchangeRateSeriesKey`](xref:Bodu.Financial.ExchangeRateSeriesKey)
is the `(pair, provider)` record struct that keys a series when the
same pair carries rates from more than one source.
[`ExchangeRateBook`](xref:Bodu.Financial.ExchangeRateBook) is the
immutable, frozen-dictionary store of many series keyed by that key —
the bridge between the mutable build side and the read-side providers.
It permits multiple providers per pair; the provider layered on top
decides which one answers.

### `ExchangeRateTableBuilder` — multi-pair, multi-provider import

[`ExchangeRateTableBuilder`](xref:Bodu.Financial.ExchangeRateTableBuilder)
owns one `ExchangeRateSeriesBuilder` per `(pair, provider)` key.
**Reach for it** when ingest data arrives flat — many pairs from many
providers — and you want to accumulate before producing immutable
snapshots. `Upsert(pair, provider, date, rate)` is the per-point entry
point, `GetOrAddSeries(...)` exposes a builder for bulk work,
`ToSeries()` snapshots every non-empty series, and `ToBook()`
materialises the whole `ExchangeRateBook` ready to hand to a provider.

```csharp
var table = new ExchangeRateTableBuilder();
table.Upsert(new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.AUD), "RBA", new DateOnly(2026, 6, 1), 1.50m);
table.Upsert(new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.JPY), "BoJ", new DateOnly(2026, 6, 1), 110m);

var provider = new FixedDatedExchangeRateProvider(table.ToBook());
```

## Looking rates up — the provider stack

### Timeless vs. dated: the two contracts

[`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider)
exposes a single `GetRate(from, to)` returning a `decimal`. **Reach for
it** when the rate is simply "current" and the date is not part of what
you record — a unit-test fixture, a daily snapshot, a live ticker.

[`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
takes a `DateOnly` and
[`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions)
and returns an
[`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult)
with full provenance. **Reach for it** when the *date* of the rate is
part of the audit trail — ledger postings, tax reports, regulatory
filings. It provides both `GetRate` (throws `KeyNotFoundException`) and
allocation-free `TryGetRate` (`bool`); the timeless contract has only
the throwing form.

| | `IExchangeRateProvider` | `IDatedExchangeRateProvider` |
|---|---|---|
| Input | `from, to` | `from, to, date, options` |
| Output | `decimal` | `ExchangeRateLookupResult` |
| Date in audit trail | no | yes |
| Try-pattern | no | yes |
| Default impl | `FixedExchangeRateTable` | `FixedDatedExchangeRateProvider` |

### `FixedExchangeRateTable` — the smallest timeless provider

[`FixedExchangeRateTable`](xref:Bodu.Financial.FixedExchangeRateTable)
implements the timeless contract from a flat `(from, to) → rate`
dictionary. Same-currency lookups short-circuit to `1m`, a missing pair
falls back to the inverse (returning `1 / rate`), and a pair missing in
both directions throws `KeyNotFoundException`.

```csharp
var table = new FixedExchangeRateTable(new Dictionary<(string, string), decimal>
{
    { ("USD", "EUR"), 0.93m },
});
table.GetRate("USD", "EUR");  // 0.93
table.GetRate("EUR", "USD");  // 1 / 0.93   (inverse fallback)
table.GetRate("USD", "USD");  // 1m         (identity)
```

### `FixedDatedExchangeRateProvider` — the in-memory dated provider

[`FixedDatedExchangeRateProvider`](xref:Bodu.Financial.FixedDatedExchangeRateProvider)
implements the dated contract over an `ExchangeRateBook`. Construct it
from a flat `IEnumerable<ExchangeRate>`, from a book, or from a book
plus a provider-priority list when a pair carries more than one source.
This is the workhorse store behind dated lookups; its behaviour under
every `ExchangeRateLookupOptions` setting is the subject of the
[worked-dataset page](exchange-rate-lookups.md).

### `AggregatingExchangeRateProvider` — primary plus fallbacks

[`AggregatingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider)
(in `Bodu.Financial.ExchangeRates.Caching`) groups an ordered set of named dated
providers. Under the default
[`PriorityFallbackStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.PriorityFallbackStrategy)
it returns the first success, in construction order. **Reach for it** to stack a
primary feed over a backup over a last-known-good table. Other strategies
([`AverageStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.AverageStrategy) or a
custom [`IExchangeRateAggregationStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateAggregationStrategy))
and per-FX-pair routing are covered in the
[caching and aggregating guide](exchange-rate-caching.md).

### `DatedExchangeRateProviderAdapter` — dated source, timeless surface

[`DatedExchangeRateProviderAdapter`](xref:Bodu.Financial.DatedExchangeRateProviderAdapter)
exposes a dated provider through `IExchangeRateProvider` by pinning a
fixed valuation date and options. **Reach for it** when an existing
consumer — such as `MoneyBag.ConvertTo<TTarget>(IExchangeRateProvider)`
— accepts only the timeless contract but the rates must come from a
dated source resolved with one consistent policy (a reporting-period
end-date, say).

### `ExchangeRateHistoryAvailability` — how deep a provider's history goes

[`ExchangeRateHistoryAvailability`](xref:Bodu.Financial.ExchangeRateHistoryAvailability)
is a small immutable value a provider exposes through
[`WebExchangeRateProvider.HistoryAvailability`](xref:Bodu.Financial.WebExchangeRateProvider.HistoryAvailability)
to declare how far back it can serve rates, in one of three shapes (the
[`ExchangeRateHistoryAvailabilityKind`](xref:Bodu.Financial.ExchangeRateHistoryAvailabilityKind)):
`Unbounded` (no known floor), `Since(earliest)` (a fixed inception date),
or `RollingDays(n)` (only the most recent *n* days — for example OANDA's
anonymous endpoint exposes roughly the last 180). **Reach for it** to find
the earliest date worth requesting before a lookup: `GetEarliestAvailable(asOf)`
resolves the floor against a reference date (`null` when unbounded), and
`IsAvailable(date, asOf)` reports whether a given date falls within it.

## Lookup configuration and outcome

### `ExchangeRateLookupOptions` — the rules on a miss

[`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions)
bundles the date-resolution policy, the tolerance window, and the
`AllowInverse` / `AllowSameCurrencyIdentityRate` switches. It is a
reference type so `null` means "use the safe `Exact` default". Use the
static factories (`Exact`, `PreviousWithin`, `NextWithin`,
`NearestWithin`) for the common shapes. Every detail of how these
change a result is in the
[worked-dataset page](exchange-rate-lookups.md).

### `ExchangeRateLookupResult` — the answer with provenance

[`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult)
carries the resolved `ExchangeRate`, the `RequestedDate`, the
`Resolution` that fired, and `OffsetDays`, plus the derived
`ResolvedDate`, `SignedOffsetDays`, `IsExactDate`, `IsPreviousDate`,
and `IsFutureDate`. It is everything needed to explain *which* observed
value was selected and *how far* from the request — without re-querying
the table.

### `MoneyConversionResult<TSource, TTarget>` — convert + audit

[`MoneyConversionResult<,>`](xref:Bodu.Financial.MoneyConversionResult`2)
is what `Money<T>.ConvertToWithRate<TSource, TTarget>(...)` returns: the
source amount, the rounded target amount, and the full
`ExchangeRateLookupResult` that produced it. **Reach for it** for a
ledger entry that must record both the converted figure and the rate
provenance in a single value.

```csharp
MoneyConversionResult<USD, EUR> audited = Money.Of<USD>(100m)
    .ConvertToWithRate<USD, EUR>(provider, new DateOnly(2024, 6, 15),
        ExchangeRateLookupOptions.PreviousWithin(3));

audited.SourceAmount;                // Money<USD> 100.00
audited.TargetAmount;                // Money<EUR>  92.80
audited.ExchangeRate.Rate.Provider;  // "ECB"
audited.ExchangeRate.OffsetDays;     // 1
```

`Money` has the analogous runtime-tagged `ConvertTo` / `ConvertToWithRate`
extensions, and `MoneyBag.ConvertToWithAudit<TTarget>(...)` returns one
line of provenance per source currency alongside the total.

## A decision walk-through

1. **Does the date matter to your records?** No → timeless
   (`IExchangeRateProvider` / `FixedExchangeRateTable` /
   `DatedExchangeRateProviderAdapter`). Yes → dated
   (`IDatedExchangeRateProvider` / `FixedDatedExchangeRateProvider`).
2. **One source or several?** One → a single provider. Several →
   `AggregatingExchangeRateProvider` (caching package), or one
   `FixedDatedExchangeRateProvider` over a multi-provider
   `ExchangeRateBook` with a priority list.
3. **How do you build the data?** A one-shot literal → construct the
   provider from `IEnumerable<ExchangeRate>`. Incremental or merged →
   `ExchangeRateSeriesBuilder` (one pair) or
   `ExchangeRateTableBuilder` (many) → `ToBook()`.
4. **Is the conversion direction fixed at compile time?** Yes →
   `Money<TCurrency>` + `ExchangeRate<TBase, TQuote>`. No →
   `Money` + `ExchangeRate`.
5. **Do you need to record provenance per conversion?** Yes →
   `ConvertToWithRate(...)` → `MoneyConversionResult<,>`. No →
   `ConvertTo(...)`.

## See also

- [Exchange-rate lookups on a known dataset](exchange-rate-lookups.md) — every option, worked end to end.
- [Working with exchange rates](exchange-rates.md) — the provider-stack walkthrough and editing surface.
- [Working with `Money<TCurrency>`](money.md) — the money types these rates convert.
- [Bodu.Financial — Core concepts](../../docs/financial/concepts.md) — the shared vocabulary.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
