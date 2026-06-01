---
title: Bodu.Numerics — Core concepts
---

# Bodu.Numerics — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/numerics/index.md), and refer back whenever a term feels imprecise.

For the high-level shape of the library and the type map, start with the [introduction](index.md).

## Canonical form

Every <xref:Bodu.Numerics.Fraction`1> is held in **canonical form**: the GCD-reduced numerator and denominator share no common factor other than one, the denominator is strictly positive, and the sign rides on the numerator. The type maintains this invariant on every construction, factory, arithmetic, and conversion path, so no unreduced representation can leak out.

Canonical form makes equality, comparison, and hashing structural. Two fractions are equal exactly when their canonical components match; equal fractions share a hash code; ordering uses a straight cross-multiplication. There is no separate "reduced" view to compute or compare against.

## Reduction (GCD normalisation)

**Reduction** is the act of dividing both components by their greatest common divisor and flipping the sign onto the numerator when the supplied denominator is negative. It happens once on construction — `Fraction<int>.Create(2, 4)` and `Fraction<int>.Create(1, 2)` both leave the factory as `1/2` and are indistinguishable from that point on.

A canonical result that does not fit in `T` — for example, when the supplied components are at the edge of `T.MinValue` — throws <xref:System.OverflowException> from the factory. `Fraction<T>.TryCreate` reports the same condition through a `false` return without throwing.

## `BigInteger` promotion

Arithmetic on <xref:Bodu.Numerics.Fraction`1> **promotes** both operands to <xref:System.Numerics.BigInteger>, evaluates the operation exactly, reduces the result to canonical form, and **narrows** back to `T`. Intermediate magnitudes can therefore exceed `T`'s range without truncation; only the final canonical components need to fit in `T`.

`Fraction<BigInteger>` eliminates the narrowing step. Pick it whenever a calculation chains several multiplications or divisions and the canonical magnitude might exceed a fixed-width range — long compound-interest schedules, exact percentage-of-percentage chains, and the `Money<TCurrency>.ToFraction()` precision escape hatch are the canonical examples.

## Overflow on narrow

When the canonical result of an arithmetic operation does not fit in a fixed-width `T`, the narrowing step raises <xref:System.OverflowException>. The promoted `BigInteger` computation is always exact — the exception flags that *the result cannot be stored*, not that the math was lossy.

There is no silent wrap, no saturation, and no truncation; an overflow is always a thrown exception. Switching the backing type to `BigInteger` removes the failure mode entirely. The same exception covers unsigned-backing edge cases (a negative numerator on a `Fraction<uint>` triggers it).

## `IBinaryInteger<T>` backing

<xref:Bodu.Numerics.Fraction`1> is constrained as `where T : IBinaryInteger<T>` — the generic-math interface every binary-integer type implements. That covers the BCL signed types (`sbyte`, `short`, `int`, `long`, `Int128`), the unsigned types (`byte`, `ushort`, `uint`, `ulong`, `UInt128`), and `BigInteger`, plus any consumer-defined integer that implements the interface.

The constraint is what unlocks `Fraction<BigInteger>` as a first-class backing — the same generic code path serves fixed-width and arbitrary-precision storage without bespoke overloads or specialisations.

## `INumber<T>` / `ISignedNumber<T>`

<xref:Bodu.Numerics.Fraction`1> implements <xref:System.Numerics.INumber`1> and <xref:System.Numerics.ISignedNumber`1>, the same generic-math interfaces that .NET 8's `int`, `double`, and `BigInteger` implement. That makes `Fraction<T>` substitutable in any algorithm written against the abstractions — `Sum`, `Aggregate`, generic linear-algebra routines, statistical helpers — without bespoke wrappers.

<xref:Bodu.Numerics.Interval`1> uses the lighter <xref:System.Numerics.INumber`1> constraint on its endpoint type so non-integer endpoints (`double`, `decimal`, fixed-point types) are accepted.

## Mixed-number formatting

A **mixed number** is the `whole + proper-fraction` form — `2 1/3` instead of the improper ratio `7/3`. <xref:Bodu.Numerics.Fraction`1>.`ToString("M")` produces this representation, with the sign on the whole part and the fractional part suppressed when zero. The improper-ratio form is the default (`G` specifier).

`Fraction<T>.Parse` accepts the same shape on the input side: `"2 1/3"` parses to `7/3`, with the sign applying to the entire whole + fraction result.

## Unicode vulgar fraction

A **vulgar fraction** is the single-codepoint glyph form — `½`, `⅗`, `¾`. The `"U"` format specifier emits these glyphs when one exists (18 numerator/denominator pairs are shipped, with denominator at most 16) and falls back to mixed-number form otherwise. The parser accepts the same glyphs (`"⅗"` parses to `3/5`, `"2⅜"` parses to `19/8`). See [Formatting and parsing `Fraction<T>`](../../guides/numerics/formatting-and-parsing.md) for the full glyph table.

## Continued fraction expansion

A **continued fraction** is the recursive representation `a0 + 1/(a1 + 1/(a2 + …))`, written compactly as the coefficient list `[a0; a1, a2, …]`. `Fraction<T>.ToContinuedFraction()` returns the coefficient array; `Fraction<T>.FromContinuedFraction(coeffs)` reconstructs the rational. The leading coefficient carries the sign; every following coefficient is strictly positive.

The **convergents** of a continued fraction — the rationals produced by truncating the expansion at successive coefficients — form a sequence of progressively better rational approximations to the original value. This is the algorithmic core of `Approximate` and `LimitDenominator`.

## Best rational approximation

`Fraction<T>.Approximate(value, maxDenominator)` returns the rational closest to `value` whose denominator does not exceed `maxDenominator`. "Best" is defined in the standard sense: no other rational with a smaller denominator and no other rational with the same denominator gets closer.

The search walks the convergents of the continued-fraction expansion and selects between the last convergent and a final semiconvergent. Overloads accept `double`, `decimal`, and string input. `Fraction<T>.LimitDenominator(maxDenominator)` is the streaming variant that re-approximates an existing fraction within a tighter denominator bound.

## Endpoint inclusivity

An <xref:Bodu.Numerics.Interval`1> endpoint is either **closed** (included in the set — written with a square bracket: `[`, `]`) or **open** (excluded — written with a round bracket: `(`, `)`). Inclusivity is tracked independently on each side, so a single interval value expresses any of the four conventional shapes; the constructor takes a separate `lowerInclusive` / `upperInclusive` flag for each end.

`Contains(value)` honours inclusivity: a closed endpoint accepts the boundary value, an open endpoint rejects it.

## Interval kinds

The four canonical shapes follow from independent inclusivity flags on each end:

| Shape | Notation | Factory |
|---|---|---|
| Closed-closed | `[a, b]` | `Interval<T>.Closed(a, b)` |
| Open-open | `(a, b)` | `Interval<T>.Open(a, b)` |
| Closed-open | `[a, b)` | `Interval<T>.ClosedOpen(a, b)` |
| Open-closed | `(a, b]` | `Interval<T>.OpenClosed(a, b)` |

A **degenerate** interval is a closed-closed interval whose endpoints are equal — `[5, 5]` — and contains exactly one value. `Interval<T>.Singleton(value)` is the dedicated factory.

## Empty interval

An interval is **empty** when its bounds admit no value — either `Lower > Upper`, or `Lower == Upper` with at least one endpoint open. The type honours the mathematical fact that there is one empty set: every empty construction compares equal to <xref:Bodu.Numerics.Interval`1>.`Empty` and shares its hash code, regardless of the bounds it was constructed with.

The default-constructed `Interval<T>` is empty — the all-zero representation `(0, 0, false, false)` satisfies the equal-bounds-both-open case. `IsEmpty` reports the predicate; equality with `Empty` is the same test.

## Membership

`Interval<T>.Contains(T value)` reports whether `value` belongs to the interval. The lower test uses `>=` when `LowerInclusive` and `>` otherwise; the upper test mirrors it. An empty interval contains no value, including itself.

`Interval<T>.Contains(Interval<T> other)` tests subset containment — every value of `other` is also a value of this interval. The empty interval is a subset of every interval, so any interval contains the empty interval.

## Overlap, intersection, union, adjacency

`Overlaps(other)` reports whether the two intervals share at least one value. Intervals that *touch* at a boundary without both including it — `[1, 5)` and `[5, 10]` — do not overlap.

`Intersect(other)` returns the interval of shared values; when the operands share none, the result is `Empty`. On endpoint ties, the **stricter** (open) inclusivity wins so the result is a true subset of both.

`TryUnion(other, out result)` succeeds when the union is itself a single contiguous interval — that is, when the operands either overlap or are *adjacent*. Two intervals are **adjacent** when one's upper endpoint equals the other's lower endpoint and at least one of those endpoints is inclusive. On endpoint ties in the union, the **looser** (closed) inclusivity wins. When the operands are disjoint with a true gap, `TryUnion` returns `false` rather than synthesising a two-piece union.

## Half-open by convention

The **closed-open** shape `[a, b)` is the most common in programming contexts: it matches `System.Range`, `Enumerable.Range`, slice iterators, and the inclusive-start / exclusive-end convention used in scheduling, time windows, and bucket ranges. `Interval<T>.ClosedOpen` and the non-generic `Interval.ClosedOpen` are the factories. Adjacent half-open intervals partition a span cleanly with no overlap and no gap, which is why the convention dominates.

## Where to go next

- **[Introduction](index.md)** — the high-level shape of the library.
- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Working with `Fraction<T>`](../../guides/numerics/fraction.md)** — construction, arithmetic, parsing, formatting, continued fractions, rational approximation.
- **[Working with `Interval<T>`](../../guides/numerics/interval.md)** — endpoint inclusivity, membership, intersection, union, adjacency.
- **[Bodu.Numerics API reference](xref:Bodu.Numerics)** — full type-by-type docs.
