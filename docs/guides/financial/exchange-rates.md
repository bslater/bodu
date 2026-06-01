---
title: Working with exchange rates
---

# Working with exchange rates

`Bodu.Financial` ships a complete foreign-exchange provider stack:
two contracts (timeless and dated), an immutable observation record,
a strongly-typed pair key, an `O(log n)` series store, in-memory
tables, a deterministic composite for fallback, an adapter that
pins a date to a dated provider, and a conversion-audit record. This
guide walks the surface and the patterns it supports — unit-test
rates, ledger postings, tax reports, and multi-source feeds where the
answer has to carry its provenance.

## Concepts in one minute

- **Rate** — `ExchangeRate` is an immutable record-struct with
  `FromIsoCode`, `ToIsoCode`, `Date`, `Rate`, `Provider`, and an
  `IsInverted` flag. Rounding is deferred to the money boundary.
- **Pair** — `ExchangeRatePair` is the `(From, To)` key. It validates
  both ISO codes at construction and exposes `Inverse()`.
- **Series** — `ExchangeRateSeries` stores every observation for one
  `(pair, provider)` in two parallel sorted arrays — resolution is
  allocation-free and runs in `O(log n)` via `Array.BinarySearch`.
- **Provider** — `IExchangeRateProvider` is timeless
  (`GetRate(from, to)`); `IDatedExchangeRateProvider` is dated
  (`GetRate(from, to, date, options)` plus a non-throwing
  `TryGetRate`) and returns an `ExchangeRateLookupResult` with full
  provenance.
- **Lookup result** — `ExchangeRateLookupResult` carries the rate,
  the requested date, the resolution policy that fired, and the
  absolute day distance — enough to reconstruct the conversion later.

See the [core concepts page](../../docs/financial/concepts.md) for
the long-form treatment, including every
`ExchangeRateDateResolution` policy.

## A minimal in-memory provider

For unit tests, fixtures, and "current rate" lookups, the timeless
provider backed by a flat dictionary is the smallest implementation:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;

Dictionary<(string From, string To), decimal> rates = new()
{
    { ("USD", "EUR"), 0.93m },
    { ("USD", "JPY"), 155.5m },
};
FixedExchangeRateTable table = new(rates);

decimal usdToEur = table.GetRate("USD", "EUR");  // 0.93
decimal eurToUsd = table.GetRate("EUR", "USD");  // 1 / 0.93  (inverse fallback)
decimal usdToUsd = table.GetRate("USD", "USD");  // 1m         (identity)

Money<USD> price = new(100m);
Money<EUR> eur = price.Convert<EUR>(table.GetRate("USD", "EUR"));
```

The table short-circuits same-currency lookups to `1m` and tries the
inverse pair (returning `1 / rate`) when only the reverse direction
is present — most FX feeds publish in a single direction per pair, so
this convention keeps the dictionary minimal. Missing pairs (and
their inverse) throw `KeyNotFoundException`.

## Timeless vs. dated lookup

Use `IExchangeRateProvider` when the *date* of the rate is not part
of what you record — a unit-test fixture, a daily snapshot replaced
in place, a live ticker. Conversion is a single multiplication.

Use `IDatedExchangeRateProvider` when the date *is* part of the audit
trail — ledger postings, tax reports, regulatory filings. The dated
contract returns an `ExchangeRateLookupResult` carrying the provider
name, the actual observation date used (which may differ from the
request), the offset-day distance, the resolution policy, and the
inversion flag. The dated side ships paired `GetRate` (throws
`KeyNotFoundException`) and `TryGetRate` (returns `bool` without
allocating); the timeless contract has only the throwing form.

## Dated lookup with provenance

`FixedDatedExchangeRateTable` accepts a flat sequence of
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
FixedDatedExchangeRateTable table = new(observations);

ExchangeRateLookupResult lookup = table.GetRate(
    "USD", "EUR",
    new DateOnly(2024, 6, 15),                  // Saturday — no observation
    ExchangeRateLookupOptions.PreviousWithin(3));

lookup.Rate.Rate;        // 0.928m   — value
lookup.Rate.Date;        // 2024-06-14 — observation date used
lookup.Rate.Provider;    // "ECB"    — publishing source
lookup.RequestedDate;    // 2024-06-15
lookup.Resolution;       // PreviousOnOrBefore
lookup.OffsetDays;       // 1
lookup.IsExactDate;      // false
```

Same-currency lookups return a synthetic identity rate tagged with
`FixedDatedExchangeRateTable.IdentityProviderName` (`"Identity"`), so
audit consumers can filter pass-throughs without a magic-string
literal.

### Lookup options

`ExchangeRateLookupOptions` carries the resolution policy and a
tolerance window. Use the static factories for the common shapes:

| Factory | Resolution | Use case |
|---|---|---|
| `ExchangeRateLookupOptions.Exact` | `Exact` | Strict-match audit; fail fast when the date is missing. |
| `ExchangeRateLookupOptions.PreviousWithin(int)` | `PreviousOnOrBefore` | Accounting and tax — never silently selects a future rate. |
| `ExchangeRateLookupOptions.NextWithin(int)` | `NextOnOrAfter` | Forward-looking pricing (delivery on a future date). |
| `ExchangeRateLookupOptions.NearestWithin(int)` | `NearestPreferPrevious` | General convenience; ties prefer the earlier date. |

For finer control, construct the record directly with
`ExchangeRateDateResolution.Nearest` (rejects ties),
`NearestPreferPrevious`, or `NearestPreferNext`. The `AllowInverse`
and `AllowSameCurrencyIdentityRate` flags (both default `true`)
disable the reverse-pair fallback and identity short-circuit when an
audit requires only direct observations.

## Composite fallback stack

`CompositeDatedExchangeRateProvider` wraps an ordered set of dated
providers and resolves every lookup with a deterministic
first-available strategy: providers are consulted in construction
order, and the first successful result wins.

```csharp
IDatedExchangeRateProvider primary   = new FixedDatedExchangeRateTable(ecbObservations);
IDatedExchangeRateProvider backup    = new FixedDatedExchangeRateTable(oandaObservations);
IDatedExchangeRateProvider lastKnown = new FixedDatedExchangeRateTable(snapshotObservations);

CompositeDatedExchangeRateProvider stack = new(new[] { primary, backup, lastKnown });

ExchangeRateLookupResult lookup = stack.GetRate(
    "USD", "GBP",
    new DateOnly(2024, 6, 15),
    ExchangeRateLookupOptions.PreviousWithin(7));

// lookup.Rate.Provider identifies which underlying provider answered.
```

The composite never re-orders results — if the primary returns a
four-day-old rate before the backup is consulted, that four-day-old
rate wins. Cross-provider policies (such as preferring an exact-date
hit from any provider before any fallback) are deferred until a
concrete consumer requires them.

## Pinning a date to a dated provider

`DatedExchangeRateProviderAdapter` exposes a dated provider through
the timeless `IExchangeRateProvider` surface by pinning a fixed
valuation date and a fixed `ExchangeRateLookupOptions`. Reach for it
when an existing consumer accepts only the timeless contract — for
example `MoneyBag.ConvertTo<TTarget>(IExchangeRateProvider)` — but
the rates should still come from a dated source, typically a single
reporting-period end-date applied consistently across an accounting
workflow:

```csharp
IDatedExchangeRateProvider dated = new FixedDatedExchangeRateTable(observations);

IExchangeRateProvider periodEnd = new DatedExchangeRateProviderAdapter(
    inner:   dated,
    date:    new DateOnly(2024, 6, 30),
    options: ExchangeRateLookupOptions.PreviousWithin(7));

Money<USD> totalUsd = wallet.ConvertTo<USD>(periodEnd);
```

The adapter delegates to the inner provider on every lookup and
returns only the raw rate. To preserve provenance, call the dated
provider directly.

## Audit-grade conversion through `Money<TCurrency>`

`Money<T>.Convert<TTarget>(decimal, MidpointRounding)` is the
lowest-level conversion — you supply the rate, it rounds to the
destination minor-unit precision:

```csharp
Money<USD> price = new(100m);
Money<EUR> eur = price.Convert<EUR>(0.93m);    // 93.00 EUR
```

When the rate comes from a dated provider and provenance matters,
prefer the extension methods on `Money<T>` and `MoneyValue`. They
resolve the rate, apply it, and either return the converted amount
or the full audit record:

```csharp
// Apply the rate, discard the metadata:
Money<EUR> eur = price.ConvertTo<USD, EUR>(
    provider, new DateOnly(2024, 6, 15),
    ExchangeRateLookupOptions.PreviousWithin(3));

// Apply the rate, keep the metadata:
MoneyConversionResult<USD, EUR> audited = price.ConvertToWithRate<USD, EUR>(
    provider, new DateOnly(2024, 6, 15),
    ExchangeRateLookupOptions.PreviousWithin(3));

audited.SourceAmount;                // Money<USD> 100.00
audited.TargetAmount;                // Money<EUR>  92.80
audited.ExchangeRate.Rate.Provider;  // "ECB"
audited.ExchangeRate.OffsetDays;     // 1
```

`MoneyValue` has analogous `ConvertTo` and `ConvertToWithRate`
extension methods for runtime-tagged amounts. For bags, see
`MoneyBag.ConvertToWithAudit<TTarget>(...)`, which returns one
`MoneyBagConversionLine` per source currency alongside the total.

## Common patterns

| Scenario | Reach for |
|---|---|
| Unit-test rates; "current rate" caches | `FixedExchangeRateTable` |
| In-memory table where the date matters | `FixedDatedExchangeRateTable` + `ExchangeRateLookupOptions.PreviousWithin(...)` |
| Primary feed plus fallbacks | `CompositeDatedExchangeRateProvider` over multiple dated providers |
| Reporting period that pins one date everywhere | `DatedExchangeRateProviderAdapter` over the period-end date |
| Ledger entry that records the rate provenance | `Money<T>.ConvertToWithRate<,>(provider, date, options)` returning `MoneyConversionResult<,>` |
| Runtime-tagged amount via a dated provider | `MoneyValueExchangeRateExtensions.ConvertToWithRate(...)` |
| Aggregate-then-convert a bag with per-line provenance | `MoneyBag.ConvertToWithAudit<TTarget>(provider, date, options)` |
| Strict accounting (never select a future rate) | `ExchangeRateLookupOptions.PreviousWithin(toleranceDays)` |
| Forward-looking pricing | `ExchangeRateLookupOptions.NextWithin(toleranceDays)` |
| Closest observation either side | `ExchangeRateLookupOptions.NearestWithin(toleranceDays)` |

## See also

- [Bodu.Financial introduction](../../docs/financial/index.md) and
  [Core concepts](../../docs/financial/concepts.md) — namespace map
  and FX vocabulary.
- [Working with `Money<TCurrency>`](money.md)
- [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider),
  [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider),
  [`ExchangeRate`](xref:Bodu.Financial.ExchangeRate),
  [`ExchangeRatePair`](xref:Bodu.Financial.ExchangeRatePair),
  [`ExchangeRateSeries`](xref:Bodu.Financial.ExchangeRateSeries)
- [`FixedExchangeRateTable`](xref:Bodu.Financial.FixedExchangeRateTable),
  [`FixedDatedExchangeRateTable`](xref:Bodu.Financial.FixedDatedExchangeRateTable),
  [`CompositeDatedExchangeRateProvider`](xref:Bodu.Financial.CompositeDatedExchangeRateProvider),
  [`DatedExchangeRateProviderAdapter`](xref:Bodu.Financial.DatedExchangeRateProviderAdapter)
- [`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions),
  [`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult),
  [`ExchangeRateDateResolution`](xref:Bodu.Financial.ExchangeRateDateResolution),
  [`MoneyConversionResult<TSource, TTarget>`](xref:Bodu.Financial.MoneyConversionResult`2)
