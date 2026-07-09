---
title: Numerics & Financial — Overview
---

# Numerics & Financial

The **Numerics & Financial** topic covers three packages built around one idea: **exact arithmetic**. [`Bodu.Numerics`](../numerics/index.md) ships <xref:Bodu.Numerics.Fraction`1> for rational arithmetic with no binary-floating-point drift and <xref:Bodu.Numerics.Interval`1> for generic-math ranges with independent endpoint inclusivity, unbounded endpoints, and full set algebra — including the binary-result <xref:Bodu.Numerics.IntervalPair`1>, the N-ary <xref:Bodu.Numerics.IntervalSet`1>, and the integer-domain <xref:Bodu.Numerics.DiscreteInterval`1>. [`Bodu.Financial`](../financial/index.md) builds the monetary layer on top — typed and runtime-tagged money, the ISO 4217 currency catalogue, fair allocation, and an exchange-rate provider stack with audit-grade provenance. [`Bodu.Financial.DependencyInjection`](../../guides/financial/dependency-injection.md) registers that stack in a `Microsoft.Extensions.DependencyInjection` container.

The dependency runs one way: `Bodu.Financial` references `Bodu.Numerics` so that <xref:Bodu.Financial.Money`1> can hand off to `Fraction<BigInteger>` for sub-minor-unit-precise intermediate calculations. `Money<TCurrency>` itself is settlement-grade — every operation rounds to the currency's minor units — but calculation chains where each step's rounding would compound (interest accumulation, percentage-of-percentage, unit-rate products) escape to exact rationals via `ToFraction()`, evaluate without loss, and snap back to settlement precision in a single rounding event via `FromFraction(...)` or the one-step `MultiplyExact(...)`. Consumers of `Fraction<T>` who never touch money are not penalised: the split keeps the 184-currency catalogue and the FX provider stack out of purely numeric workloads.

Both value-type libraries are **generic-math first**. `Fraction<T>` accepts any `IBinaryInteger<T>` backing (from `sbyte` through `Int128` to `BigInteger`) and implements `INumber<T>` / `ISignedNumber<T>`; `Interval<T>` accepts any `INumber<T>` endpoint type. Code written against the .NET generic-math abstractions composes with them without bespoke wrappers.

## The packages

| Package | Status | What it provides | Docs |
|---|---|---|---|
| `Bodu.Numerics` | Stable | `Fraction<T>` — canonical, GCD-reduced rational arithmetic with `BigInteger` intermediates, mixed-number and Unicode-vulgar-fraction formatting, continued fractions, best rational approximation. `Interval<T>` — closed / open / half-open / unbounded intervals with membership, intersection, union, adjacency, difference, and the `&` / `|` operators; plus the integer-domain `DiscreteInterval<T>`, the binary-result `IntervalPair<T>` / `DiscreteIntervalPair<T>`, and the N-ary `IntervalSet<T>`. `System.Text.Json` converters for `Fraction<T>` and `Interval<T>`. | [Intro](../numerics/index.md) · [Concepts](../numerics/concepts.md) · [Get started](../numerics/getting-started.md) |
| `Bodu.Financial` | Stable | `Money<TCurrency>` (compile-time currency) and `Money` (runtime-tagged), `MoneyBag` multi-currency portfolios, the ISO 4217 catalogue of 184 currency tags, fair allocation, cash rounding, timeless and dated exchange-rate providers with provenance, and three JSON wire policies. | [Intro](../financial/index.md) · [Concepts](../financial/concepts.md) · [Get started](../financial/getting-started.md) |
| `Bodu.Financial.DependencyInjection` | Stable | `IServiceCollection` extensions: `AddFinancialService(...)`, the fluent `IFinancialServiceBuilder`, named monetary contexts, FX provider registration, JSON converter registration, and `FinancialOptions` binding. | [Guide](../../guides/financial/dependency-injection.md) |

## Why exact arithmetic

`double` cannot represent one third, and `0.1 + 0.2 != 0.3` under binary floating point. `decimal` fares better for money but still rounds on every division — split `$1.00` three ways with naive division and a cent disappears. The types in this topic attack the problem from two directions:

- **Compute exactly, round once.** `Fraction<T>` holds the true rational value through an entire calculation chain; the only rounding event is the explicit one at the boundary. Arithmetic promotes to `BigInteger` internally, so intermediate magnitudes never truncate.
- **Make the rounding rules first-class.** `Money<TCurrency>` rounds to the currency's minor units on construction using banker's rounding by default, exposes `Allocate(...)` so splits never lose a penny, and snaps to coarse coin denominations via `RoundToCash()` only when a total becomes a physical cash payment.

And because the currency rides in the type parameter, `Money<USD> + Money<JPY>` fails the build, not the nightly batch:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;

Money<USD> dinner = new(54.30m);
Money<USD> tip    = dinner * 0.18m;
Money<USD> total  = dinner + tip;       // OK — same currency

Money<JPY> sushi  = new(2500m);
var oops = dinner + sushi;              // CS0019 — fails the build
```

When the currency genuinely is data — a deserialised payload, a configuration-driven ledger — the runtime-tagged <xref:Bodu.Financial.Money> is the escape hatch, converting to typed money with `As<T>()` at the boundary where the currency becomes known.

## Ranges as first-class values

<xref:Bodu.Numerics.Interval`1> is the topic's second numeric primitive: lower endpoint, upper endpoint, and the inclusivity of each side, packed into one immutable `readonly struct`. Reach for it whenever the range itself is the data — a validation predicate exposed by an API, a scheduling window persisted to a database, a reservation that must detect overlap with others:

```csharp
using Bodu.Numerics;

var valid = Interval<int>.Closed(0, 100);          // [0, 100]
valid.Contains(100);                               // True — closed upper endpoint

var q1 = Interval<int>.ClosedOpen(0, 90);          // [0, 90)
var q2 = Interval<int>.ClosedOpen(90, 181);        // [90, 181)
q1.Overlaps(q2);                                   // False — half-open buckets partition cleanly
q1.TryUnion(q2, out var half);                     // half = [0, 181)
```

Endpoint inclusivity is independent on each side, so closed, open, and both half-open shapes are all expressible — see [Numerics & Financial concepts](numerics-and-financial-concepts.md) for when each shape fits.

## Choosing a type

| Scenario | Reach for | Notes |
|---|---|---|
| Exact thirds, percentages, or ratios with no drift | <xref:Bodu.Numerics.Fraction`1> | Canonical form on construction; `2/4` and `1/2` are indistinguishable. |
| Long calculation chains where overflow is possible | `Fraction<BigInteger>` | Eliminates the narrowing step entirely; no `OverflowException`. |
| Best rational approximation to a `double` or `decimal` | `Fraction<T>.Approximate(value, maxDenominator)` | Walks continued-fraction convergents. |
| Range membership, overlap, or set algebra over numbers | <xref:Bodu.Numerics.Interval`1> | Independent endpoint inclusivity; `Contains`, `Intersect`, `TryUnion`, `Overlaps`. |
| Unbounded or half-bounded ranges (`(-∞, 5]`, `[0, +∞)`) | `Interval<T>.All` / `AtLeast` / `AtMost` / … | Explicit endpoint metadata, not float infinities; works for `int` / `decimal` / `BigInteger`. |
| Difference or symmetric difference of two intervals | `Interval<T>.Difference` / `SymmetricDifference` | Returns <xref:Bodu.Numerics.IntervalPair`1> — at most two disjoint pieces. |
| Discrete integer ranges (indices, IDs, pages) | <xref:Bodu.Numerics.DiscreteInterval`1> | Successor-aware emptiness and adjacency; `Open(1, 2)` is empty. |
| An arbitrary union of disjoint ranges, with complement | <xref:Bodu.Numerics.IntervalSet`1> | Normalized N-ary `Union` / `Intersect` / `Except` / `Complement`. |
| An amount in a known currency with safe arithmetic | <xref:Bodu.Financial.Money`1> | Cross-currency arithmetic fails the build. |
| Currency known only at runtime (deserialisation, generic invoicing) | <xref:Bodu.Financial.Money> | Runtime ISO tag; convert to typed money at the boundary with `As<T>()`. |
| Multi-currency totals with aggregate-then-convert | <xref:Bodu.Financial.MoneyBag> | Zero balances pruned; one FX lookup per source currency. |
| Splitting an amount across N shares without remainder loss | `Money<T>.Allocate(parts)` / `Allocate(ratios)` | Largest-remainder distribution; the shares always sum to the original. |
| Sub-minor-unit-precise interest or percentage chains | `Money<T>.ToFraction()` / `FromFraction()` / `MultiplyExact()` | The bridge between the two libraries. |
| FX conversion with dated rates and audit provenance | <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> + <xref:Bodu.Financial.ExchangeRates.RateLookupResult> | Provider name, actual date used, offset days, resolution policy. |
| Prioritised fallback (or averaging) across multiple FX sources | [`AggregatingRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingRateProvider) | In the `Bodu.Financial.ExchangeRates.Caching` package; deterministic first-available (PriorityFallback) or mean (Average) via a pluggable strategy. |
| Registering the financial stack in a DI container | `AddFinancialService(...)` | Currency lookup, monetary contexts, providers, JSON converters. |

## How the pieces compose

A representative end-to-end flow — accumulate exactly, settle once:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Numerics;
using System.Numerics;

Money<USD> principal = new(10_000m);

// Escape to exact rationals for the compound-interest chain.
Fraction<BigInteger> exact = principal.ToFraction();
Fraction<BigInteger> monthlyRate = Fraction<BigInteger>.Create(425, 120_000); // 4.25% / 12

for (int month = 0; month < 360; month++)
{
    exact += exact * monthlyRate;          // no rounding, no drift
}

// One rounding event, at the boundary, back to settlement precision.
Money<USD> settled = Money<USD>.FromFraction(exact);
```

Each loop iteration is exact; the only place a cent can be gained or lost is the single, explicit `FromFraction` call — which is also the place an auditor looks.

## Install

```bash
dotnet add package Bodu.Numerics
dotnet add package Bodu.Financial
dotnet add package Bodu.Financial.DependencyInjection
```

`Bodu.Financial` depends on `Bodu.Numerics`, and `Bodu.Financial.DependencyInjection` depends on `Bodu.Financial` — install only the topmost package your application consumes.

## Where to go next

- **[Numerics & Financial concepts](numerics-and-financial-concepts.md)** — the cross-package vocabulary: canonical form, deferred rounding, `BigInteger` promotion, endpoint inclusivity, minor units, allocation, dated FX lookup.
- **[Bodu.Numerics introduction](../numerics/index.md)** — `Fraction<T>` and `Interval<T>` in detail.
- **[Bodu.Numerics getting started](../numerics/getting-started.md)** — install + minimal samples for both value types.
- **[Bodu.Financial introduction](../financial/index.md)** — money, currencies, FX, and serialization.
- **[Bodu.Financial getting started](../financial/getting-started.md)** — install + minimal samples for `Money<TCurrency>`, `Money`, `MoneyBag`, the provider stack, and the JSON policies.
- **[Numerics & Financial guides](../../guides/topics/numerics-and-financial.md)** — recipe-style walk-throughs across the topic.
- **API reference:** [Bodu.Numerics](xref:Bodu.Numerics) · [Bodu.Financial](xref:Bodu.Financial)
