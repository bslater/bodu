---
title: Discrete integer intervals
---

# Discrete integer intervals

<xref:Bodu.Numerics.DiscreteInterval`1> is the discrete counterpart to
<xref:Bodu.Numerics.Interval`1>. Where `Interval<T>` models a continuum of
coordinates, `DiscreteInterval<T>` (constrained to `IBinaryInteger<T>` —
`int`, `long`, `BigInteger`, …) models the **set of representable integers**
between its bounds. That difference changes two behaviours that matter for
integer ranges such as indices, pages, or IDs.

## Emptiness reflects representable membership

A continuous open interval over two consecutive integers is non-empty — it
holds all the reals between them — but it contains no *integer*. The discrete
type reports that correctly:

```csharp
Interval<int>.Open(1, 2).IsEmpty;           // False — continuous range (1, 2)
DiscreteInterval<int>.Open(1, 2).IsEmpty;   // True  — no integer strictly between 1 and 2
```

Every shape is canonicalized to inclusive `[First, Last]` integer bounds at
construction (an open bound shifts inward by one), so equal integer sets share
one representation and the default value is the empty set:

```csharp
DiscreteInterval<int>.Open(1, 5) == DiscreteInterval<int>.Closed(2, 4);   // True
DiscreteInterval<int>.Closed(1, 10).Count;                                // 10
```

## Adjacency is by successor

Two integer intervals with no integer between them are adjacent and union to a
single run, even though their endpoints are not equal:

```csharp
var a = DiscreteInterval<int>.Closed(1, 2);
var b = DiscreteInterval<int>.Closed(3, 4);

a.TryUnion(b, out var run);   // run = [1, 4], result = true — 2 and 3 are successors

DiscreteInterval<int>.Closed(1, 2)
    .TryUnion(DiscreteInterval<int>.Closed(4, 5), out _);   // false — 3 is missing
```

## The surface

`DiscreteInterval<T>` offers `First` / `Last` / `IsBounded` / `IsEmpty` /
`Count` (which throws for an unbounded interval), `Contains`, `Overlaps`,
`Intersect`, and `TryUnion`; the `Closed` / `Open` / `ClosedOpen` /
`OpenClosed` / `Singleton` / `Empty` factories and the unbounded `All` /
`AtLeast` / `GreaterThan` / `AtMost` / `LessThan` family (with type-inferring
`DiscreteInterval.*` helpers); equality; and `ToInterval()` /
`FromInterval(...)` conversions to and from the continuous type.

> [!NOTE]
> `Difference` / `SymmetricDifference` on the discrete type, and a first-class
> N-ary interval-set type, are planned additions; today the continuous
> `Interval<T>` carries the difference surface.

## See also

- [Interval algebra](interval-algebra.md) — the continuous `Interval<T>` set surface.
- [`DiscreteInterval<T>` API reference](xref:Bodu.Numerics.DiscreteInterval`1) and the [`DiscreteInterval` helpers](xref:Bodu.Numerics.DiscreteInterval).
