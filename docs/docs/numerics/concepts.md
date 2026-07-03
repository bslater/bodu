---
title: Bodu.Numerics — Core concepts
---

# Bodu.Numerics — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/numerics/index.md), and refer back whenever a term feels imprecise.

For the high-level shape of the library and the type map, start with the [introduction](index.md). `Bodu.Numerics` is part of the **[Numerics & Financial](../topics/numerics-and-financial.md)** topic; the [topic concepts page](../topics/numerics-and-financial-concepts.md) covers the vocabulary shared across both libraries.

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

## `INumber` classification predicates

Implementing <xref:System.Numerics.INumberBase`1> obliges `Fraction<T>` to answer the full battery of static classification predicates — and because a rational is always a finite real, most have constant answers:

| Predicate | Result for `Fraction<T>` | Why |
|---|---|---|
| `IsNaN`, `IsInfinity`, `IsPositiveInfinity`, `IsNegativeInfinity`, `IsSubnormal`, `IsComplexNumber`, `IsImaginaryNumber` | always `false` | A rational has no non-finite, subnormal, or complex states; division by zero throws rather than yielding a non-finite value. |
| `IsFinite`, `IsRealNumber` | always `true` | Every representable value is a finite real. |
| `IsNormal` | `true` unless zero | Mirrors the floating-point convention that zero is not "normal". |
| `IsCanonical` | always `true` | The type maintains canonical form as an invariant. |
| `IsInteger`, `IsEvenInteger`, `IsOddInteger`, `IsZero`, `IsNegative`, `IsPositive` | value-dependent | Computed from the canonical components. |
| `Radix` | `2` | Inherited from the binary-integer backing component. |

The practical consequence is that an `INumber`-generic algorithm guarding on `TSelf.IsFinite(x)` or `TSelf.IsNaN(x)` behaves correctly over `Fraction<T>` without special-casing — the guards simply never trip the non-finite paths.

## Cross-type conversion (`CreateChecked` / `CreateSaturating` / `CreateTruncating`)

The `INumberBase<TSelf>.TryConvertFrom*` / `TryConvertTo*` hooks let `Fraction<T>` participate in `TSelf.CreateChecked<TOther>(value)`-style generic conversions:

- **From another numeric type:** integer and `decimal` sources convert *exactly*; any other finite source converts through its nearest `double` and then to the exact rational of that `double` (so `Create*<Fraction<int>>(0.1)` yields the rational of the IEEE-754 `double` `0.1`, not `1/10`). Non-finite sources fail the conversion. The *checked* path raises <xref:System.OverflowException> when the result does not fit `T`; the *saturating* path clamps to `MinValue` / `MaxValue` instead; the *truncating* path shares the saturating clamp.
- **To another numeric type:** an integer-valued fraction converts from its exact numerator; a non-integer fraction converts through `ToDecimal` (checked, for `decimal` targets) or `ToDouble` (otherwise). The checked / saturating / truncating distinction is forwarded to the destination type's own `Create*`.

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

## Range as a first-class value

An <xref:Bodu.Numerics.Interval`1> is a **range encoded as a single value**: lower endpoint, upper endpoint, and the inclusivity of each side, packed into one immutable `readonly struct`. The point of having a dedicated type is that the range can be passed, stored, compared, and operated on without splitting it into separate `min` / `max` / `(bool, bool)` parameters whose meaning depends on convention.

Reach for it whenever the range itself is the data:

- A scheduling window or time slot persisted in a database column.
- A validation predicate exposed by an API (`var valid = Interval<int>.Closed(0, 100);`).
- A reservation that needs to detect overlap with other reservations.
- A bucket whose membership semantics carry through a pipeline.

When the range is *only* an immediate `for (int i = 0; i < count; i++)` loop bound, a plain `int` pair or `System.Range` is enough and `Interval<T>` is overkill. The line is "is the range a value other code receives" — `Interval<T>` when yes, primitive pair when no.

## Comparison with `System.Range`, tuples, and `(bool, bool)` flags

| Alternative | Limitation | When `Interval<T>` wins |
|---|---|---|
| `System.Range` | Integer-only; always `[start, end)`; intended for slicing, not membership. | Non-integer endpoints, mixed inclusivity, or set algebra. |
| `(T min, T max)` tuple | Inclusivity is implicit — readers have to infer or check a comment. | The contract is in the value; the four shapes are distinguishable. |
| `(T, T, bool, bool)` tuple | Possible but loses the named API (`Contains`, `Intersect`, `TryUnion`) and structural equality on the *set*. | All operations are methods on the type; equality compares sets, not field shapes. |
| `Predicate<T>` | Composable for membership but throws away the endpoints — no intersection, union, or persistence. | The data form is preserved; predicates are easy to derive when needed. |

`System.Range` and `Interval<T>` are not competitors — `Range` is the right tool for slicing `Span<T>` / arrays, while `Interval<T>` is the right tool when "the range itself" is a value.

## Numeric backing types

The type parameter is constrained as `where T : INumber<T>`, so any .NET generic-math number is a valid endpoint type: `int`, `long`, `Int128`, `double`, `float`, `decimal`, `BigInteger`, and consumer-defined numeric types built on the generic-math interfaces. The same set-algebra code runs over integer scheduling slots, decimal price bands, and arbitrary-magnitude `BigInteger` bounds without a per-type overload.

Endpoints are stored at full `T` precision — no widening, no narrowing — and `Contains`, `Intersect`, `Overlaps`, and `TryUnion` use `T`'s native comparison operators.

## Endpoint inclusivity

An <xref:Bodu.Numerics.Interval`1> endpoint is either **closed** (included in the set — written with a square bracket: `[`, `]`) or **open** (excluded — written with a round bracket: `(`, `)`). Inclusivity is tracked independently on each side, so a single interval value expresses any of the four conventional shapes; the constructor takes a separate `lowerInclusive` / `upperInclusive` flag for each end.

`Contains(value)` honours inclusivity: a closed endpoint accepts the boundary value, an open endpoint rejects it.

```csharp
var closed = Interval<int>.Closed(0, 10);       // [0, 10]
var open   = Interval<int>.Open(0, 10);         // (0, 10)

closed.Contains(0);    // True  — closed lower
open.Contains(0);      // False — open lower
closed.Contains(10);   // True  — closed upper
open.Contains(10);     // False — open upper
```

## Interval kinds

The four canonical shapes follow from independent inclusivity flags on each end:

| Shape | Notation | Factory | Typical use |
|---|---|---|---|
| Closed-closed | `[a, b]` | `Interval<T>.Closed(a, b)` | A percentage `[0, 100]`, a die roll `[1, 6]`, any range whose boundary values are valid members. |
| Open-open | `(a, b)` | `Interval<T>.Open(a, b)` | Strict-inequality conditions: `0 < rate < 1`, "strictly between". |
| Closed-open | `[a, b)` | `Interval<T>.ClosedOpen(a, b)` | Spans, slices, scheduling windows, bucket boundaries — see [Half-open by convention](#half-open-by-convention). |
| Open-closed | `(a, b]` | `Interval<T>.OpenClosed(a, b)` | Billing tiers, histogram bins owning their upper edge, "strictly above X, up to Y". |

A **degenerate** interval is a closed-closed interval whose endpoints are equal — `[5, 5]` — and contains exactly one value. `Interval<T>.Singleton(value)` is the dedicated factory.

## Empty interval

An interval is **empty** when its bounds admit no value — either `Lower > Upper`, or `Lower == Upper` with at least one endpoint open. The type honours the mathematical fact that there is one empty set: every empty construction compares equal to <xref:Bodu.Numerics.Interval`1>.`Empty` and shares its hash code, regardless of the bounds it was constructed with.

The default-constructed `Interval<T>` is empty — the all-zero representation `(0, 0, false, false)` satisfies the equal-bounds-both-open case. `IsEmpty` reports the predicate; equality with `Empty` is the same test.

```csharp
var none      = Interval<int>.Empty;
var inverted  = new Interval<int>(5, 1, true, true);     // Lower > Upper
var collapsed = new Interval<int>(0, 0, false, false);   // equal + open
Interval<int> defaulted = default;

none == inverted && none == collapsed && none == defaulted;  // True
none.ToString();                                             // "∅"
```

`Empty` acts as the identity in the set algebra: `Intersect` returns it whenever two operands share no values, and `TryUnion` returns the other operand unchanged when one side is empty.

## Membership

`Interval<T>.Contains(T value)` reports whether `value` belongs to the interval. The lower test uses `>=` when `LowerInclusive` and `>` otherwise; the upper test mirrors it. An empty interval contains no value, including itself.

`Interval<T>.Contains(Interval<T> other)` tests subset containment — every value of `other` is also a value of this interval. The empty interval is a subset of every interval, so any interval contains the empty interval.

```csharp
var outer = Interval<int>.Closed(0, 10);

outer.Contains(5);                                    // True — value
outer.Contains(Interval<int>.Closed(2, 8));           // True — strict subset
outer.Contains(Interval<int>.Closed(2, 11));          // False — exceeds upper
outer.Contains(Interval<int>.Empty);                  // True — ∅ ⊆ every set
```

## Overlap, intersection, union, adjacency

`Overlaps(other)` reports whether the two intervals share at least one value. Intervals that *touch* at a boundary without both including it — `[1, 5)` and `[5, 10]` — do not overlap.

`Intersect(other)` returns the interval of shared values; when the operands share none, the result is `Empty`. On endpoint ties, the **stricter** (open) inclusivity wins so the result is a true subset of both.

`TryUnion(other, out result)` succeeds when the union is itself a single contiguous interval — that is, when the operands either overlap or are *adjacent*. Two intervals are **adjacent** when one's upper endpoint equals the other's lower endpoint and at least one of those endpoints is inclusive. On endpoint ties in the union, the **looser** (closed) inclusivity wins. When the operands are disjoint with a true gap, `TryUnion` returns `false` rather than synthesising a two-piece union.

> [!NOTE]
> The two tie-break rules are duals chosen to keep each operation total and exact. On an endpoint tie, `Intersect` keeps the **stricter** (open) side so the result is a genuine *subset* of both operands (`AND` of the inclusivity flags); `TryUnion` keeps the **looser** (closed) side so the result is a genuine *superset* of both (`OR` of the flags). This is why `[1, 5] ∩ (1, 5) = (1, 5)` while `[1, 5] ∪ (1, 5) = [1, 5]`.

```csharp
var a = Interval<int>.Closed(1, 5);
var b = Interval<int>.Closed(3, 7);

a.Overlaps(b);                          // True
a.Intersect(b);                         // [3, 5] — shared values
a.TryUnion(b, out var merged);          // merged = [1, 7], result = true

// Adjacent half-open shapes union cleanly.
Interval<int>.ClosedOpen(1, 5)
    .TryUnion(Interval<int>.Closed(5, 10), out var span);   // span = [1, 10]

// Disjoint operands cannot form a contiguous union.
Interval<int>.Closed(1, 2)
    .TryUnion(Interval<int>.Closed(5, 6), out _);           // returns false
```

## Half-open by convention

The **closed-open** shape `[a, b)` is the most common in programming contexts: it matches `System.Range`, `Enumerable.Range`, slice iterators, and the inclusive-start / exclusive-end convention used in scheduling, time windows, and bucket ranges. `Interval<T>.ClosedOpen` and the non-generic `Interval.ClosedOpen` are the factories. Adjacent half-open intervals partition a span cleanly with no overlap and no gap, which is why the convention dominates.

```csharp
// Quarter buckets that together cover [0, 365) with no overlap and no gap.
var q1 = Interval<int>.ClosedOpen(0,   90);
var q2 = Interval<int>.ClosedOpen(90,  181);
var q3 = Interval<int>.ClosedOpen(181, 273);
var q4 = Interval<int>.ClosedOpen(273, 365);
```

## Unbounded and half-bounded endpoints

A side of an <xref:Bodu.Numerics.Interval`1> may be **unbounded** — extending to `-∞` or `+∞` — rather than bounded by a concrete `T`. Boundedness is tracked as explicit endpoint metadata (spare flag bits), *not* as a floating-point infinity sentinel, so it works uniformly for `int`, `decimal`, and `BigInteger` endpoints. An unbounded side is always open (infinity is never a member) and never makes the interval empty.

The factories are `Interval<T>.AtLeast(a)` = `[a, +∞)`, `GreaterThan(a)` = `(a, +∞)`, `AtMost(b)` = `(-∞, b]`, `LessThan(b)` = `(-∞, b)`, and `All` = `(-∞, +∞)`. `IsBounded` reports whether both sides are finite; `Length` throws for an unbounded interval because the extent is infinite.

## Difference and symmetric difference

`Interval<T>.Difference(other)` is the set difference `this \ other` — the members of this interval not in `other`. Removing an interior slice leaves a **left** and a **right** remainder, so the result is zero, one, or two disjoint intervals, returned as an allocation-free <xref:Bodu.Numerics.IntervalPair`1>. `SymmetricDifference(other)` returns the members in exactly one operand. The `&` operator is `Intersect`; the `|` operator is the contiguous union (it throws when the operands are disjoint with a gap, since the result would not be a single interval).

## Disconnected sets (`IntervalSet<T>`)

`IntervalPair<T>` covers the at-most-two pieces of a *binary* operation. When a union or complement can produce arbitrarily many disjoint ranges, an <xref:Bodu.Numerics.IntervalSet`1> models the result: a **normalized** collection of disjoint, non-adjacent intervals in ascending order. Overlapping and adjacent inputs coalesce on construction, so equal sets share a canonical form and set equality is piecewise equality. It offers N-ary `Union`, `Intersect`, `Except`, and `Complement` (taken over the whole line, so the double complement is the identity) over both an `Interval<T>` and another `IntervalSet<T>`.

## Continuous versus discrete (`DiscreteInterval<T>`)

`Interval<T>` is a **continuous** interval over ordered coordinates: `Interval<int>.Open(1, 2)` is non-empty even though no *integer* lies strictly between 1 and 2. When the domain is the representable integers, use <xref:Bodu.Numerics.DiscreteInterval`1> (constrained to `IBinaryInteger<T>`). It canonicalizes every shape to closed integer bounds, so `DiscreteInterval<int>.Open(1, 2)` **is** empty and successor-adjacent runs — `[1, 2]` and `[3, 4]` — union into `[1, 4]`. Its binary `Difference` / `SymmetricDifference` return a <xref:Bodu.Numerics.DiscreteIntervalPair`1>, and `ToInterval()` / `FromInterval(...)` convert to and from the continuous type.

## Where to go next

- **[Introduction](index.md)** — the high-level shape of the library.
- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Working with `Fraction<T>`](../../guides/numerics/fraction.md)** — construction, arithmetic, parsing, formatting, continued fractions, rational approximation.
- **[Working with `Interval<T>`](../../guides/numerics/interval.md)** — endpoint inclusivity, membership, intersection, union, adjacency.
- **[Interval algebra](../../guides/numerics/interval-algebra.md)** — unbounded endpoints, difference / symmetric difference, operators, and `IntervalSet<T>`.
- **[Discrete integer intervals](../../guides/numerics/discrete-intervals.md)** — the integer-domain `DiscreteInterval<T>`.
- **[Numerics & Financial topic overview](../topics/numerics-and-financial.md)** — how this package and `Bodu.Financial` fit together.
- **[Numerics & Financial topic concepts](../topics/numerics-and-financial-concepts.md)** — the vocabulary shared across both libraries.
- **[Bodu.Numerics API reference](xref:Bodu.Numerics)** — full type-by-type docs.
