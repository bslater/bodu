---
title: Numerics & Financial — Concepts
---

# Numerics & Financial — Concepts

The shared vocabulary for the [Numerics & Financial topic](numerics-and-financial.md) — the terms that cross the boundary between [`Bodu.Numerics`](../numerics/index.md) and [`Bodu.Financial`](../financial/index.md). Each member library has its own deeper concepts page (linked in the closing table); this page covers only what you need to navigate both at once.

## Canonical form and GCD reduction

Every <xref:Bodu.Numerics.Fraction`1> is held in **canonical form**: numerator and denominator share no common factor other than one, the denominator is strictly positive, and the sign rides on the numerator. **Reduction** — dividing both components by their greatest common divisor — happens once, on construction: `Fraction<int>.Create(2, 4)` and `Fraction<int>.Create(1, 2)` both leave the factory as `1/2` and are indistinguishable from that point on. Canonical form makes equality, comparison, and hashing structural; there is no unreduced representation to leak out.

## Exactness vs deferred rounding

The discipline both libraries are built around: **compute exactly, round once at the boundary**. A `Fraction<T>` carries the true rational value through an entire calculation chain; nothing is lost mid-stream. <xref:Bodu.Financial.Money`1>, by contrast, is settlement-grade — every operation rounds to the currency's minor units, which is correct for ledger postings but compounds across long chains.

The bridge is explicit: `Money<T>.ToFraction()` escapes to `Fraction<BigInteger>`, the chain evaluates without loss, and `Money<T>.FromFraction(...)` (or the one-step `MultiplyExact(...)`) snaps back to settlement precision in a single rounding event. That single event is where the rounding rule — banker's `ToEven` by default, or an explicit `MidpointRounding` — applies, and the one place an audit needs to examine.

## `BigInteger` promotion

Arithmetic on `Fraction<T>` **promotes** both operands to <xref:System.Numerics.BigInteger>, evaluates exactly, reduces to canonical form, and **narrows** back to `T`. Intermediate magnitudes can exceed `T`'s range without truncation; only the final canonical components must fit. When they do not, the narrowing step throws <xref:System.OverflowException> — never a silent wrap, saturation, or truncation. `Fraction<BigInteger>` eliminates the narrowing step entirely, which is why it backs the monetary escape hatch.

## Interval endpoints — closed, open, half-open

An <xref:Bodu.Numerics.Interval`1> endpoint is either **closed** (the boundary value belongs to the set — `[`, `]`) or **open** (it does not — `(`, `)`), tracked independently on each side. One immutable value therefore expresses all four conventional shapes:

| Shape | Notation | Factory | Typical use |
|---|---|---|---|
| Closed-closed | `[a, b]` | `Interval<T>.Closed(a, b)` | Ranges whose boundary values are valid members — a percentage `[0, 100]`. |
| Open-open | `(a, b)` | `Interval<T>.Open(a, b)` | Strict inequalities — `0 < rate < 1`. |
| Closed-open | `[a, b)` | `Interval<T>.ClosedOpen(a, b)` | Spans, slices, scheduling windows — adjacent half-open intervals partition cleanly. |
| Open-closed | `(a, b]` | `Interval<T>.OpenClosed(a, b)` | Billing tiers and histogram bins that own their upper edge. |

All empty constructions — inverted bounds, or equal bounds with an open endpoint — compare equal to the single `Interval<T>.Empty`, honouring the mathematical fact that there is one empty set.

## Minor unit

The smallest commonly-issued denomination of a currency, expressed as the number of fractional digits its major unit subdivides into: `0` for `JPY` and `KRW`, `2` for `USD` and `EUR` and most others, `3` for `BHD` and `KWD`. Construction of `Money<TCurrency>` rounds to this precision, and `FromMinorUnits` / `ToMinorUnits` bridge to the integer minor-unit storage that ledgers and wire formats use. The value travels on the <xref:Bodu.Financial.ICurrency> tag, so generic code reads it as `TCurrency.MinorUnits`.

## Cash-rounding increment

A coarser, coin-aligned denomination some currencies apply to physical cash totals — CHF rounds to five rappen, AUD and CAD cash totals to five cents. `ICurrency.CashRoundingIncrement` exposes the value and `Money<T>.RoundToCash()` snaps to it. Cash rounding is a presentation choice for physical payments, not a storage rule: electronic amounts retain full minor-unit precision, and `RoundToCash()` is a no-op for currencies whose increment is `0m`.

## Allocation without losing pennies

Splitting `$1.00` three ways cannot return `[0.33, 0.33, 0.33]` — a cent vanishes. `Money<T>.Allocate(int parts)` distributes the residual minor units one per share from the start of the array so the shares always sum to the original exactly:

```csharp
new Money<USD>(0.10m).Allocate(3);   // [0.04, 0.03, 0.03]
new Money<USD>(-10m).Allocate(3);    // [-3.34, -3.33, -3.33]
```

The ratio overload `Allocate(ReadOnlySpan<decimal> ratios)` weights shares proportionally and distributes the residual by the **largest-remainder (Hamilton) method** — deterministic, sign-preserving, and stable across runs. Naive per-share multiplication-and-rounding is the bug this API exists to eliminate.

## Multi-currency bag

<xref:Bodu.Financial.MoneyBag> is an immutable portfolio of balances across multiple ISO codes — the type behind the **aggregate-then-convert** pattern. Accumulate per-currency balances during a batch, then convert the whole bag to a target currency once at the boundary: one FX lookup per source currency instead of one per posting. Zero balances are pruned on every operation and enumeration is in lexicographic ISO order, so equality and iteration are stable across insertion order and serialization round trips.

## Dated FX lookup and provenance

<xref:Bodu.Financial.IExchangeRateProvider> answers "what is the rate now"; <xref:Bodu.Financial.IDatedExchangeRateProvider> answers "what was the rate on this date" — the contract for ledger postings and tax reports where the date is part of the audit trail. A dated lookup returns an <xref:Bodu.Financial.ExchangeRateLookupResult> carrying **provenance**: the publishing provider's name, the date the returned rate was actually observed, the offset-day distance from the requested date, the resolution policy that fired, and an inversion flag. That set is what lets an accounting workflow answer "which observed rate produced this number?" without re-querying the table. Grouping several sources behind one entry point with deterministic first-available fallback (or averaging, or per-FX-pair routing) lives in the `Bodu.Financial.ExchangeRates.Caching` package as [`AggregatingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider).

## Going deeper

| Concept area | Where it is covered in full |
|---|---|
| Reduction, overflow on narrow, `IBinaryInteger<T>` backing, formatting specifiers, continued fractions, interval set algebra | [Bodu.Numerics — Core concepts](../numerics/concepts.md) |
| Currency tags, cross-currency safety, banker's rounding, the currency catalogue, exchange-rate series and builders, JSON policies, demonetisation | [Bodu.Financial — Core concepts](../financial/concepts.md) |

## See also

- **[Numerics & Financial overview](numerics-and-financial.md)** — the topic landing page.
- **[Numerics & Financial guides](../../guides/topics/numerics-and-financial.md)** — recipe-style walk-throughs across the topic.
