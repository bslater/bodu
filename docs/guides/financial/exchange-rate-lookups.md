---
title: Exchange-rate lookups on a known dataset
---

# Exchange-rate lookups on a known dataset

A dated exchange-rate lookup rarely lands on the exact date you ask
for. Markets close on weekends and holidays, feeds skip days, and
accounting periods end on dates no rate was ever published for.
[`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions)
is the single knob that decides *what happens on a miss* — which
neighbouring observation (if any) answers the request, and how far
the lookup is allowed to reach.

This page pins one small dataset and walks the **same requests**
through **every option**, so you can see exactly how each setting
changes the result. For the narrative tour of the provider stack, see
[Working with exchange rates](exchange-rates.md); for the type-by-type
"which one do I reach for" catalogue, see
[Exchange-rate types](exchange-types.md).

## The dataset

Four `USD → EUR` observations from one provider (`"ECB"`), with two
deliberate gaps — a four-day gap that brackets a weekend, and a
three-day gap at the end:

| # | Date | Weekday | Rate |
|---|---|---|---|
| 0 | `2024-06-10` | Monday | `0.9250` |
| 1 | `2024-06-14` | Friday | `0.9280` |
| 2 | `2024-06-18` | Tuesday | `0.9300` |
| 3 | `2024-06-21` | Friday | `0.9330` |

```csharp
using Bodu.Financial;

ExchangeRate[] observations =
{
    new("USD", "EUR", new DateOnly(2024, 6, 10), 0.9250m, "ECB"),
    new("USD", "EUR", new DateOnly(2024, 6, 14), 0.9280m, "ECB"),
    new("USD", "EUR", new DateOnly(2024, 6, 18), 0.9300m, "ECB"),
    new("USD", "EUR", new DateOnly(2024, 6, 21), 0.9330m, "ECB"),
};

FixedDatedExchangeRateProvider provider = new(observations);
```

Every example below calls
[`provider.TryGetRate(...)`](xref:Bodu.Financial.IDatedExchangeRateProvider)
or its throwing sibling
[`GetRate(...)`](xref:Bodu.Financial.IDatedExchangeRateProvider).
A successful call returns an
[`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult);
a miss either returns `false` (`TryGetRate`) or throws
`KeyNotFoundException` (`GetRate`).

## How resolution works in one paragraph

A lookup first does a binary search for the **exact** requested date.
On a hit it returns immediately — the policy and tolerance are never
consulted. On a miss it identifies the two bracketing observations
(`previous` = the nearest earlier date, `next` = the nearest later
date), the
[`ExchangeRateDateResolution`](xref:Bodu.Financial.ExchangeRateDateResolution)
policy picks one of them (or neither), and *finally* the chosen
candidate's distance from the requested date is compared against
`ToleranceDays`. If the candidate is farther than the tolerance, the
lookup fails. The tolerance is checked **after** selection — a policy
never switches to the other side because its first choice was out of
range.

## The six date-resolution policies

[`ExchangeRateDateResolution`](xref:Bodu.Financial.ExchangeRateDateResolution)
has six members. The four common shapes have static factories on
[`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions);
the two strict-`Nearest` variants are reached by constructing the
options directly.

| Policy | Factory | On a miss it selects… |
|---|---|---|
| `Exact` | `ExchangeRateLookupOptions.Exact` | nothing — exact date or fail. |
| `PreviousOnOrBefore` | `PreviousWithin(days)` | the nearest **earlier** observation. |
| `NextOnOrAfter` | `NextWithin(days)` | the nearest **later** observation. |
| `Nearest` | *(construct directly)* | the closer of the two; an exact tie **fails**. |
| `NearestPreferPrevious` | `NearestWithin(days)` | the closer of the two; a tie picks the **earlier**. |
| `NearestPreferNext` | *(construct directly)* | the closer of the two; a tie picks the **later**. |

```csharp
// The four factories (tolerance window in days):
ExchangeRateLookupOptions.Exact;            // exact date only
ExchangeRateLookupOptions.PreviousWithin(7);
ExchangeRateLookupOptions.NextWithin(7);
ExchangeRateLookupOptions.NearestWithin(7); // == NearestPreferPrevious

// The two strict-Nearest variants, constructed directly:
new ExchangeRateLookupOptions(ExchangeRateDateResolution.Nearest, toleranceDays: 7);
new ExchangeRateLookupOptions(ExchangeRateDateResolution.NearestPreferNext, toleranceDays: 7);
```

> [!NOTE]
> `Exact` requires `ToleranceDays == 0` — a non-zero tolerance with
> `Exact` throws `ArgumentException` from
> [`ExchangeRateLookupOptions.Validate()`](xref:Bodu.Financial.ExchangeRateLookupOptions).
> The factories enforce this for you.

## The results matrix

Five requested dates against the dataset, each run through all six
policies with a tolerance wide enough not to interfere (`7` days). A
cell shows the **resolved observation → rate**; `—` means the lookup
fails (returns `false` / throws).

| Requested date | `Exact` | `PreviousOnOrBefore` | `NextOnOrAfter` | `Nearest` | `NearestPreferPrevious` | `NearestPreferNext` |
|---|---|---|---|---|---|---|
| **`06-14`** Fri — exact hit | `06-14` → `0.9280` | `06-14` → `0.9280` | `06-14` → `0.9280` | `06-14` → `0.9280` | `06-14` → `0.9280` | `06-14` → `0.9280` |
| **`06-15`** Sat — prev 1d / next 3d | — | `06-14` → `0.9280` | `06-18` → `0.9300` | `06-14` → `0.9280` | `06-14` → `0.9280` | `06-14` → `0.9280` |
| **`06-16`** Sun — tie 2d / 2d | — | `06-14` → `0.9280` | `06-18` → `0.9300` | **—** *(tie)* | `06-14` → `0.9280` | `06-18` → `0.9300` |
| **`06-08`** Sat — before first | — | **—** *(no earlier)* | `06-10` → `0.9250` | `06-10` → `0.9250` | `06-10` → `0.9250` | `06-10` → `0.9250` |
| **`06-25`** Tue — after last | — | `06-21` → `0.9330` | **—** *(no later)* | `06-21` → `0.9330` | `06-21` → `0.9330` | `06-21` → `0.9330` |

Three rows carry the whole lesson:

- **`06-15`** sits closer to the earlier observation (1 day vs 3). All
  three `Nearest*` policies therefore land on `06-14` — when the two
  sides are *unequal*, the preference is irrelevant and the genuinely
  closer date always wins.
- **`06-16`** is the exact midpoint of the four-day gap. This is the
  only place the three `Nearest*` policies diverge: plain `Nearest`
  refuses to guess and **fails**, `NearestPreferPrevious` takes the
  earlier rate, `NearestPreferNext` takes the later one.
- **`06-08`** and **`06-25`** fall outside the data on either end. A
  one-directional policy has nothing to select and fails;
  `PreviousOnOrBefore` cannot reach forward to the first observation,
  and `NextOnOrAfter` cannot reach back to the last.

### Reading it from the result object

The matrix is just the resolved date and rate; the
[`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult)
carries the rest of the story so an audit trail never has to recompute
it:

```csharp
ExchangeRateLookupResult r = provider.GetRate(
    "USD", "EUR",
    new DateOnly(2024, 6, 15),                  // Saturday — no observation
    ExchangeRateLookupOptions.PreviousWithin(7));

r.Rate.Rate;          // 0.9280m
r.ResolvedDate;       // 2024-06-14   (== r.Rate.Date)
r.RequestedDate;      // 2024-06-15
r.Resolution;         // PreviousOnOrBefore
r.OffsetDays;         // 1            (absolute distance)
r.SignedOffsetDays;   // -1           (negative => resolved date is earlier)
r.IsExactDate;        // false
r.IsPreviousDate;     // true
r.IsFutureDate;       // false
r.Rate.Provider;      // "ECB"
r.Rate.IsInverted;    // false
```

`SignedOffsetDays` (and the `IsPreviousDate` / `IsFutureDate` flags)
let accounting and tax workflows distinguish a *historical* fallback
from a *forward-looking* one — often a policy requirement, and the
reason `PreviousWithin` is the usual choice for ledger postings.

## Tolerance: how far the reach extends

`ToleranceDays` is the maximum distance, in days, the *selected*
candidate may sit from the requested date. It is applied after the
policy chooses, so it only ever **shrinks** the matrix above — it never
changes *which* side is picked, only whether that pick is accepted.

Request `06-16` (the tie midpoint, 2 days from either neighbour):

| Options | Selected candidate | Distance | Result |
|---|---|---|---|
| `PreviousWithin(1)` | `06-14` | 2 | **—** (2 > 1) |
| `PreviousWithin(2)` | `06-14` | 2 | `06-14` → `0.9280` |
| `NearestWithin(1)` | `06-14` (tie → earlier) | 2 | **—** (2 > 1) |
| `NearestWithin(2)` | `06-14` (tie → earlier) | 2 | `06-14` → `0.9280` |

```csharp
var tight = provider.TryGetRate(
    "USD", "EUR", new DateOnly(2024, 6, 16),
    ExchangeRateLookupOptions.PreviousWithin(1), out _);   // false — 2 days > 1

var ok = provider.TryGetRate(
    "USD", "EUR", new DateOnly(2024, 6, 16),
    ExchangeRateLookupOptions.PreviousWithin(2), out var hit); // true
// hit.Rate.Rate == 0.9280m, hit.OffsetDays == 2
```

### Tolerance does not let a policy switch sides

This is the subtlety worth internalising. Request `06-13` (Thursday):
it is **3 days** after the `06-10` observation but only **1 day**
before `06-14`.

```csharp
// PreviousOnOrBefore selects 06-10 (distance 3) and never looks forward.
provider.TryGetRate(
    "USD", "EUR", new DateOnly(2024, 6, 13),
    ExchangeRateLookupOptions.PreviousWithin(2), out _);   // false — 3 > 2

// Nearest picks the genuinely closer 06-14 (distance 1) and fits easily.
provider.TryGetRate(
    "USD", "EUR", new DateOnly(2024, 6, 13),
    ExchangeRateLookupOptions.NearestWithin(2), out var near); // true
// near.ResolvedDate == 2024-06-14, near.OffsetDays == 1
```

`PreviousWithin(2)` fails even though a rate sits one day away —
because `PreviousOnOrBefore` is a *directional* policy that committed
to the earlier side before the tolerance was checked. If "closest
within N days, either direction" is what you mean, use
`NearestWithin(N)`.

## Inverse fallback: a direction switch, not a date switch

`AllowInverse` (default `true`) lets a lookup answer a pair it has no
data for by consulting the **reverse** pair and returning the
reciprocal. The dataset holds only `USD → EUR`, so a `EUR → USD`
request succeeds only through the inverse:

```csharp
var r = provider.GetRate(
    "EUR", "USD", new DateOnly(2024, 6, 14),
    ExchangeRateLookupOptions.Exact);

r.Rate.Rate;        // 1m / 0.9280m  ≈ 1.07758621
r.Rate.FromIsoCode; // "EUR"
r.Rate.ToIsoCode;   // "USD"
r.Rate.IsInverted;  // true   — derived from the reverse pair
r.Rate.Provider;    // "ECB"
r.OffsetDays;       // 0      — date resolution still ran on the USD/EUR series

// Turn it off and the same request fails:
var found = provider.TryGetRate(
    "EUR", "USD", new DateOnly(2024, 6, 14),
    new ExchangeRateLookupOptions(ExchangeRateDateResolution.Exact, allowInverse: false),
    out _);                                                  // false
```

Two points decide how this composes with the date policy:

- The inverse is consulted **only after the direct pair has fully
  failed**, including its own date-resolution and tolerance. It is a
  fallback in the *direction* dimension, not a second date search.
- Date resolution and tolerance still apply, but to whichever series
  actually holds the data. Here `06-14` exists on the `USD/EUR`
  series, so even `Exact` succeeds and reports `OffsetDays == 0`,
  flagging only `IsInverted == true`.

## Identity short-circuit: same-currency requests

`AllowSameCurrencyIdentityRate` (default `true`) makes a lookup whose
source and destination codes are equal return a synthetic rate of `1`
**before** any table is consulted — so it succeeds even for a pair the
provider has never seen:

```csharp
var r = provider.GetRate(
    "USD", "USD", new DateOnly(2024, 6, 16),                 // a gap date — irrelevant
    ExchangeRateLookupOptions.Exact);

r.Rate.Rate;      // 1m
r.Rate.Provider;  // "Identity"  (== FixedDatedExchangeRateProvider.IdentityProviderName)
r.OffsetDays;     // 0
r.IsExactDate;    // true
```

The well-known provider label `"Identity"` lets an audit consumer
filter out pass-throughs without a magic string. Turn the flag off
(`allowSameCurrencyIdentityRate: false`) and a `USD → USD` request
falls through to the table like any other — failing unless a literal
same-currency series was loaded.

## Stacking providers: the composite

[`CompositeDatedExchangeRateProvider`](xref:Bodu.Financial.CompositeDatedExchangeRateProvider)
applies the *same* `ExchangeRateLookupOptions` to an ordered list of
providers and returns the **first** success. The lookup options decide
the date behaviour within each provider; the composite decides the
order they are tried.

```csharp
CompositeDatedExchangeRateProvider stack = new(new IDatedExchangeRateProvider[]
{
    primaryEcbFeed,        // tried first
    backupOandaFeed,       // tried only if the primary misses
    lastKnownGoodTable,    // final fallback
});

ExchangeRateLookupResult r = stack.GetRate(
    "USD", "GBP", new DateOnly(2024, 6, 15),
    ExchangeRateLookupOptions.PreviousWithin(7));

r.Rate.Provider;   // identifies which underlying provider answered
```

The selection is **first-available**, not best-available: if the
primary returns a four-day-old `PreviousOnOrBefore` hit, that wins even
when a lower-priority provider has the exact date. The
[`ExchangeRateProviderSelectionPolicy`](xref:Bodu.Financial.ExchangeRateProviderSelectionPolicy)
enum names the alternative strategies (exact-before-fallback,
smallest-offset-first), but only `ProviderPriorityFirst` is
implemented in v1.0 — the others throw `NotSupportedException` so the
intent is expressible without yet being silently approximated.

## Pinning one date everywhere: the adapter

When a consumer only accepts the timeless
[`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider)
surface — for example
[`MoneyBag.ConvertTo<TTarget>(IExchangeRateProvider)`](xref:Bodu.Financial.MoneyBag) —
but the rate should still come from a dated source resolved with a
fixed policy,
[`DatedExchangeRateProviderAdapter`](xref:Bodu.Financial.DatedExchangeRateProviderAdapter)
pins the date and options once:

```csharp
IExchangeRateProvider periodEnd = new DatedExchangeRateProviderAdapter(
    inner:   provider,
    date:    new DateOnly(2024, 6, 30),                       // reporting period end
    options: ExchangeRateLookupOptions.PreviousWithin(14));

decimal rate = periodEnd.GetRate("USD", "EUR");              // 0.9330 — 06-21, 9 days before 06-30
```

Every `GetRate(from, to)` call now resolves against `2024-06-30` under
`PreviousWithin(14)` — the window has to span the nine-day gap back to
the last observation (`06-21`), a reminder that the pinned tolerance
must be sized for the worst expected gap. The adapter returns only the
raw `decimal` — when
you need the provenance (which date, how far off, which provider), call
the dated provider directly and read the
[`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult).

## Choosing the option for the job

| Workflow | Options | Why |
|---|---|---|
| Strict reconciliation; missing data is an error | `Exact` | Fails fast rather than substituting a neighbour. |
| Ledger posting, tax report, period-end valuation | `PreviousWithin(n)` | Never selects a *future* rate; `SignedOffsetDays` stays ≤ 0. |
| Forward pricing / earliest-available quote | `NextWithin(n)` | Reaches forward to the next published rate. |
| General "closest rate within a window" | `NearestWithin(n)` | Closest either way; ties resolve to the earlier date. |
| Closest, but ties must favour the newer rate | `new(NearestPreferNext, n)` | Same as above, tie-break reversed. |
| Closest, but a tie must be a hard error | `new(Nearest, n)` | Refuses to guess at an exact midpoint. |
| One currency converting to itself | leave `AllowSameCurrencyIdentityRate` on | Returns `1` tagged `"Identity"` without table data. |
| Only forward-direction pairs are stored | leave `AllowInverse` on | Reverse requests answered via the reciprocal. |

## See also

- [Working with exchange rates](exchange-rates.md) — the full provider-stack walkthrough.
- [Exchange-rate types](exchange-types.md) — which exchange type to reach for, by scenario.
- [Bodu.Financial — Core concepts](../../docs/financial/concepts.md) — the vocabulary these pages assume.
- Lookup metadata — [`ExchangeRateLookupOptions`](xref:Bodu.Financial.ExchangeRateLookupOptions), [`ExchangeRateDateResolution`](xref:Bodu.Financial.ExchangeRateDateResolution), [`ExchangeRateLookupResult`](xref:Bodu.Financial.ExchangeRateLookupResult).
- Providers — [`FixedDatedExchangeRateProvider`](xref:Bodu.Financial.FixedDatedExchangeRateProvider), [`CompositeDatedExchangeRateProvider`](xref:Bodu.Financial.CompositeDatedExchangeRateProvider), [`DatedExchangeRateProviderAdapter`](xref:Bodu.Financial.DatedExchangeRateProviderAdapter).
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
