---
title: Generic math with Fraction<T> and Interval<T>
---

# Generic math with `Fraction<T>` and `Interval<T>`

`Bodu.Numerics` is built on the .NET 7+ generic-math interfaces
(`System.Numerics.INumber<TSelf>` and friends), and that shows up in two
distinct places:

- **`Fraction<T>` constrains its backing type** to
  `IBinaryInteger<T>` — the numerator and denominator are stored in `T` —
  and **the fraction type itself implements `INumber<Fraction<T>>` and
  `ISignedNumber<Fraction<T>>`**, so a `Fraction<int>` is a first-class
  `INumber`.
- **`Interval<T>` constrains its endpoint type** to `INumber<T>`, so the
  same interval code carries an `int` window, a `double` percentage band,
  or a `BigInteger` magnitude bound.

This guide covers what those constraints buy you: writing one generic
method that accepts every `Fraction<int>` / `Fraction<BigInteger>`,
choosing the backing integer type, and the overflow versus
`BigInteger`-promotion trade-off.

## The two constraint layers on `Fraction<T>`

There are two separate generic-math relationships in play, and keeping
them apart removes most of the confusion:

| Layer | Constraint | What it means |
|---|---|---|
| Backing component `T` | `where T : IBinaryInteger<T>` | The numerator and denominator are integers of type `T`. |
| The fraction itself | `Fraction<T> : INumber<Fraction<T>>, ISignedNumber<Fraction<T>>` | `Fraction<T>` *is* a number and composes with any `INumber`-constrained algorithm. |

So `Fraction<int>` stores two `int`s, and `Fraction<int>` is itself an
`INumber<Fraction<int>>`. The first layer governs storage and overflow;
the second governs composability.

`Interval<T>` has only the first kind of relationship — its endpoints are
`INumber<T>` — and the interval struct is *not* itself an `INumber`,
because a range is not a scalar.

## Writing a method constrained to `INumber<TSelf>`

Because `Fraction<T>` implements `INumber<Fraction<T>>`, any algorithm
written against the generic-math abstractions accepts it without a bespoke
overload. A single constrained method works for `Fraction<int>`,
`Fraction<BigInteger>`, and the built-in numeric types alike:

```csharp
using System.Numerics;
using Bodu.Numerics;

// One implementation, every INumber: int, double, decimal, Fraction<T>, …
static TSelf Mean<TSelf>(ReadOnlySpan<TSelf> values)
    where TSelf : INumber<TSelf>
{
    if (values.IsEmpty)
        return TSelf.Zero;

    TSelf sum = TSelf.Zero;
    foreach (TSelf v in values)
        sum += v;

    // TSelf.CreateChecked lifts the element count into TSelf.
    return sum / TSelf.CreateChecked(values.Length);
}
```

The same method body now serves exact rational inputs and ordinary
floating-point inputs:

```csharp
// Exact: the mean of 1/2, 1/3, 1/6 is exactly 1/3 — no rounding.
Fraction<int>[] fractions =
{
    Fraction<int>.Create(1, 2),
    Fraction<int>.Create(1, 3),
    Fraction<int>.Create(1, 6),
};
Fraction<int> exact = Mean<Fraction<int>>(fractions);   // 1/3

// The identical method over double — inexact, as floating point always is.
double approx = Mean<double>(stackalloc[] { 0.5, 0.3333, 0.1667 });
```

The static abstract members of `INumber<TSelf>` — `TSelf.Zero`,
`TSelf.One`, `TSelf.CreateChecked`, the operators, the `IsXxx`
predicates — all resolve to the `Fraction<T>` implementations, so nothing
in the generic body special-cases the rational type.

A handful of these are worth knowing when you write the generic body:

| Generic-math member | `Fraction<T>` behaviour |
|---|---|
| `TSelf.Zero` / `TSelf.One` | `0/1` and `1/1`. |
| `TSelf.AdditiveIdentity` / `TSelf.MultiplicativeIdentity` | `Zero` and `One`. |
| `TSelf.CreateChecked<TOther>(x)` | Exact for integer and `decimal` `x`; via nearest `double` otherwise; non-finite `x` is rejected; overflow throws. |
| `TSelf.CreateSaturating` / `CreateTruncating` | As checked, but clamp to `MinValue` / `MaxValue` on overflow instead of throwing. |
| `TSelf.IsFinite` / `IsNaN` / `IsInfinity` | `true` / `false` / `false` — a rational is always a finite real. |
| `TSelf.Abs` / `MaxMagnitude` / `MinMagnitude` | Magnitude operations; the magnitude helpers break a tie by sign per the BCL convention. |

`CreateChecked` over a non-bounded backing type never overflows from the
conversion — `Fraction<BigInteger>.CreateChecked(anyInteger)` always
succeeds — which is another reason a routine that must accept arbitrary
magnitudes should be left parameterised over `T`.

`Fraction<T>` additionally implements `ISignedNumber<Fraction<T>>`, so a
method that needs a signed-only operation (for example
`TSelf.NegativeOne`) can tighten the constraint to
`where TSelf : ISignedNumber<TSelf>` and still accept every signed
`Fraction<T>`.

## Choosing the backing integer type

The backing type `T` is a storage-and-overflow decision, not a
correctness decision: every `Fraction<T>` holds the same canonical
reduced form regardless of `T`. The choice trades compactness against the
risk of an `OverflowException` on narrowing.

| Backing `T` | Use when | Trade-off |
|---|---|---|
| `int` | Values and denominators stay small; storage matters. | Compact (8 bytes); overflows near `int.MaxValue` on narrowing. |
| `long` | Headroom for chained arithmetic over moderate magnitudes. | 16 bytes; still bounded, so still narrows. |
| `Int128` / `UInt128` | Large fixed-width magnitudes without `BigInteger` allocation. | 32 bytes; bounded. |
| `BigInteger` | Long chains of multiplications / divisions, or unknown magnitude. | Unbounded — never overflows; heap-allocates per component. |

Unsigned backing types (`uint`, `ulong`, `byte`, …) are accepted but
cannot represent negative rationals: any operation that would produce a
negative numerator — unary `-`, `Negate()`, `MinusOne`, certain
subtractions — throws `OverflowException` at run time. Pick a signed
backing type whenever negative values are possible.

## Overflow versus `BigInteger` promotion

Every `Fraction<T>` arithmetic operation promotes its operands to
`BigInteger`, evaluates the result exactly, reduces to canonical form,
then **narrows the canonical components back to `T`**. The intermediate is
always exact; the narrowing step is where a fixed-width `T` can fail:

```csharp
using System.Numerics;
using Bodu.Numerics;

// int backing: the canonical result does not fit back into int.
var huge = Fraction<int>.Create(int.MaxValue, 1);
var doubled = huge + huge;   // throws OverflowException on narrowing
```

Switching the backing type to `BigInteger` removes the narrowing step
entirely — the exact intermediate *is* the stored value, so overflow is
impossible:

```csharp
var hugeBig = Fraction<BigInteger>.Create(int.MaxValue, 1);
var doubledBig = hugeBig + hugeBig;   // 4294967294/1 — no overflow
```

The trade-off is the usual one: `Fraction<BigInteger>` allocates for its
components and runs slower per operation, while `Fraction<int>` is compact
and fast but bounded. A practical rule:

- **Bounded inputs, no chaining, hot loop →** a fixed-width `T`
  (`int` / `long`). The exactness is free; you only pay GCD reduction.
- **Long arithmetic chains, accumulation, or unknown magnitude →**
  `Fraction<BigInteger>`. Pay the allocation to make overflow unreachable.

A generic algorithm can defer the decision to its caller by staying
parameterised over the backing type, so the same routine runs in either
mode:

```csharp
// Works for Fraction<int> in the fast path and Fraction<BigInteger>
// when the caller needs overflow safety.
static Fraction<T> Harmonic<T>(int n)
    where T : IBinaryInteger<T>
{
    Fraction<T> sum = Fraction<T>.Zero;
    for (int k = 1; k <= n; k++)
        sum += Fraction<T>.Create(T.One, T.CreateChecked(k));

    return sum;
}

Harmonic<int>(8);          // fast; fits in int for small n
Harmonic<BigInteger>(50);  // exact; the int backing would overflow here
```

To move an existing value between backing types without recomputing, use
`As<TOther>()`, which re-validates the canonical components against the
target type:

```csharp
Fraction<int> half = Fraction<int>.Create(1, 2);
Fraction<BigInteger> wide = half.As<BigInteger>();   // 1/2, now unbounded
// As<TOther>() throws OverflowException if the components do not fit TOther.
```

## `Interval<T>` over a generic endpoint type

`Interval<T>` constrains `T` to `INumber<T>`, so a range routine can be
written once and reused across endpoint types. The body relies only on
the `INumber` surface (`T.Zero`, the comparison operators, subtraction
for `Length`):

```csharp
using System.Numerics;
using Bodu.Numerics;

// Clamp a value into a closed interval, for any numeric endpoint type.
static T ClampTo<T>(T value, Interval<T> range)
    where T : INumber<T>
{
    if (value < range.Lower) return range.Lower;
    if (value > range.Upper) return range.Upper;
    return value;
}

ClampTo(150, Interval<int>.Closed(0, 100));            // 100
ClampTo(1.5, Interval<double>.Closed(0.0, 1.0));       // 1.0
ClampTo(42m, Interval<decimal>.Closed(0m, 1000m));     // 42m
```

Because the endpoint type is just `INumber<T>`, the same interval value
can carry an integer scheduling window, a `double` percentage band, or a
`BigInteger` magnitude bound — and `Length` (computed as
`Upper - Lower`) is exact for `decimal` and `BigInteger` endpoints where a
`double` range would accumulate floating-point error.

## See also

- [Working with `Fraction<T>`](fraction.md) — the rational type's full surface: construction, arithmetic, parsing, formatting.
- [Working with `Interval<T>`](interval.md) and [Interval algebra](interval-algebra.md) — the range type and its set operations.
- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1) and [`Interval<T>` API reference](xref:Bodu.Numerics.Interval`1).
- <xref:System.Numerics.INumber`1> and <xref:System.Numerics.IBinaryInteger`1> — the BCL generic-math interfaces these types build on.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
