---
title: Working with Interval<T>
---

# Working with `Interval<T>`

`Interval<T>` is an immutable, value-equatable, bounded interval over
any `INumber<T>` endpoint type. Endpoint inclusivity is independent on
each side, so a single type expresses all four conventional shapes:

- Closed-closed `[a, b]` — both endpoints included.
- Open-open `(a, b)` — both endpoints excluded.
- Closed-open `[a, b)` — lower included, upper excluded.
- Open-closed `(a, b]` — lower excluded, upper included.

The type works with any numeric backing type that implements
`INumber<T>`: `int`, `long`, `double`, `decimal`, `BigInteger`, and
consumer-defined numeric types built on the generic-math interfaces.

## Creating intervals

Use the static factory methods on `Interval<T>` directly when the
endpoint type is fixed, or the non-generic `Interval` helper class
when you want the compiler to infer the type from the arguments:

```csharp
using Bodu.Numerics;

// Explicit endpoint type:
Interval<int> a = Interval<int>.Closed(1, 5);          // [1, 5]
Interval<int> b = Interval<int>.Open(1, 5);            // (1, 5)
Interval<int> c = Interval<int>.ClosedOpen(0, 100);    // [0, 100)
Interval<int> d = Interval<int>.OpenClosed(-10, 0);    // (-10, 0]
Interval<int> e = Interval<int>.Singleton(42);         // [42, 42]
Interval<int> none = Interval<int>.Empty;              // ∅

// Inferred from arguments:
var f = Interval.Closed(1.5, 2.5);          // Interval<double>
var g = Interval.ClosedOpen(0m, 100m);      // Interval<decimal>
```

The primary constructor — `new Interval<T>(lower, upper,
lowerInclusive, upperInclusive)` — is also public for cases where the
inclusivity comes from a runtime computation:

```csharp
bool isPercentage = true;
var range = new Interval<double>(0.0, 100.0, true, isPercentage);
```

## Empty intervals

An interval is **empty** when its bounds do not admit any value.
Two cases produce an empty interval:

1. The lower bound exceeds the upper bound (`Lower > Upper`).
2. The bounds are equal *and* at least one endpoint is open
   (`(5, 5)`, `[5, 5)`, `(5, 5]`).

The third equal-bounds case, `[5, 5]`, contains exactly one value
and is called a **degenerate** interval (`IsDegenerate` returns
`true`).

All empty intervals are equal to `Interval<T>.Empty` regardless of the
bounds they were constructed with — the type honors the mathematical
fact that there is one empty set, not many:

```csharp
var a = Interval<int>.Empty;
var b = new Interval<int>(5, 1, true, true);    // inverted bounds
var c = new Interval<int>(0, 0, false, false);  // equal + both open

Console.WriteLine(a == b);  // True
Console.WriteLine(a == c);  // True
```

The default-constructed `Interval<T>` is empty: the all-zero
representation `(0, 0, false, false)` satisfies the equal-bounds
both-open case.

## Membership testing

`Contains(T)` tests a single value, honoring the inclusivity of each
endpoint:

```csharp
var range = Interval<int>.ClosedOpen(1, 5);   // [1, 5)

range.Contains(1);  // True  — lower endpoint included
range.Contains(4);  // True  — interior
range.Contains(5);  // False — upper endpoint excluded
range.Contains(0);  // False — outside the interval
```

`Contains(Interval<T>)` tests whether the supplied interval is a
subset of this one — every value of the inner interval is also a
value of the outer:

```csharp
var outer = Interval<int>.Closed(0, 10);

outer.Contains(Interval<int>.Closed(2, 8));   // True
outer.Contains(Interval<int>.Closed(2, 11));  // False
outer.Contains(Interval<int>.Empty);          // True — every set contains ∅
```

The empty interval is a subset of every interval, so any interval
contains the empty interval.

## Overlap and intersection

`Overlaps(other)` reports whether the two intervals share at least
one value:

```csharp
Interval<int>.Closed(1, 5).Overlaps(Interval<int>.Closed(3, 7));      // True
Interval<int>.ClosedOpen(1, 5).Overlaps(Interval<int>.Closed(5, 10)); // False — touch only
Interval<int>.OpenClosed(1, 5).Overlaps(Interval<int>.Closed(5, 10)); // True — both include 5
```

Note that intervals that *touch* at a value but do not both contain
it — for example `[1, 5)` and `[5, 10]` — do not overlap, because no
value belongs to both.

`Intersect(other)` returns the intersection interval — the set of
values shared by both operands. When the intersection is empty, the
result is `Interval<T>.Empty`:

```csharp
Interval<int>.Closed(1, 5).Intersect(Interval<int>.Closed(3, 7));  // [3, 5]
Interval<int>.Closed(1, 3).Intersect(Interval<int>.Closed(5, 7));  // ∅
```

When endpoint values tie, the *stricter* (open) inclusivity wins —
this guarantees `Intersect` returns a true subset of both operands:

```csharp
var a = Interval<int>.Closed(1, 5);    // [1, 5]
var b = Interval<int>.Open(1, 5);      // (1, 5)
var ab = a.Intersect(b);               // (1, 5) — open wins on both ends
```

## Union and adjacency

`TryUnion(other, out result)` succeeds when the union of the two
intervals is itself a single contiguous interval — that is, when the
operands either overlap or are *adjacent*. Two intervals are adjacent
when the upper endpoint of one equals the lower endpoint of the other
and at least one of those endpoints is inclusive:

```csharp
// Adjacent — [1, 5) ∪ [5, 10] -> [1, 10]
if (Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.Closed(5, 10), out var u))
{
    Console.WriteLine(u);  // [1, 10]
}

// Disjoint — [1, 5) ∪ (5, 10] returns false because 5 is in neither
//            interval and the result would not be contiguous.
bool ok = Interval<int>.ClosedOpen(1, 5)
    .TryUnion(Interval<int>.OpenClosed(5, 10), out var _);
Console.WriteLine(ok);  // False
```

When endpoint values tie, the *looser* (inclusive) inclusivity wins —
`TryUnion` returns the union, which is always a superset of either
operand:

```csharp
var a = Interval<int>.Closed(1, 5);    // [1, 5]
var b = Interval<int>.Open(1, 5);      // (1, 5)
a.TryUnion(b, out var u);              // [1, 5] — inclusive wins on both ends
```

Union with `Interval<T>.Empty` is always defined and leaves the other
operand unchanged.

## Length and degenerate intervals

`Length` is the **algebraic** length of the interval — the difference
between the upper and lower endpoints, regardless of endpoint
inclusion:

```csharp
Interval<int>.Closed(1, 5).Length;     // 4
Interval<int>.Open(1, 5).Length;       // 4
Interval<int>.ClosedOpen(1, 5).Length; // 4
Interval<int>.OpenClosed(1, 5).Length; // 4
Interval<int>.Empty.Length;            // 0
```

This matches the *Lebesgue measure* on continuous numeric types
(`double`, `decimal`) where endpoint inclusion does not affect the
measure of the interval. For integer ranges where you want the *count*
of integers in the interval, endpoint inclusion matters and you
should compute it directly:

```csharp
static int IntegerCount(Interval<int> r)
{
    if (r.IsEmpty) return 0;
    int lower = r.LowerInclusive ? r.Lower : r.Lower + 1;
    int upper = r.UpperInclusive ? r.Upper : r.Upper - 1;
    return upper - lower + 1;
}

IntegerCount(Interval<int>.Closed(1, 5));     // 5 — {1, 2, 3, 4, 5}
IntegerCount(Interval<int>.ClosedOpen(1, 5)); // 4 — {1, 2, 3, 4}
IntegerCount(Interval<int>.Open(1, 5));       // 3 — {2, 3, 4}
```

## Formatting

`Interval<T>` formats using ISO 31-11 bracket notation. Square
brackets indicate closed endpoints; round brackets indicate open
endpoints. Empty intervals render as the U+2205 EMPTY SET glyph:

```csharp
Interval<int>.Closed(1, 5).ToString();      // "[1, 5]"
Interval<int>.Open(1, 5).ToString();        // "(1, 5)"
Interval<int>.ClosedOpen(1, 5).ToString();  // "[1, 5)"
Interval<int>.OpenClosed(1, 5).ToString();  // "(1, 5]"
Interval<int>.Empty.ToString();             // "∅"
```

The format specifier and culture are forwarded to each endpoint:

```csharp
Interval<double>
    .Closed(1.5, 2.75)
    .ToString("F2", CultureInfo.InvariantCulture);   // "[1.50, 2.75]"
```

`Interval<T>` implements `ISpanFormattable` and `IUtf8SpanFormattable`
for allocation-free formatting into character or UTF-8 byte buffers.

## Parsing

`Interval<T>` implements `IParsable<Interval<T>>` and
`ISpanParsable<Interval<T>>`, so the static `Parse` and `TryParse`
methods accept any ISO 31-11 bracket-notation text and the empty-set
glyph:

```csharp
Interval<int>.Parse("[1, 5)", CultureInfo.InvariantCulture);
                                                // [1, 5)
Interval<int>.Parse("∅", CultureInfo.InvariantCulture);
                                                // empty

if (Interval<int>.TryParse("(0, 100]", CultureInfo.InvariantCulture, out var r))
{
    // r is the parsed interval
}
```

Whitespace around brackets and endpoints is ignored. Malformed inputs
return `false` from `TryParse` and throw `FormatException` from
`Parse`.

## Equality and hashing

`Interval<T>` is value-equatable via `IEquatable<Interval<T>>`. Two
intervals are equal when they describe the same set of values:

- Two non-empty intervals are equal iff their endpoints and
  inclusivity flags are identical.
- All empty intervals are equal to each other and share the same
  hash code, regardless of the bounds used to construct them.

```csharp
var a = Interval<int>.Closed(1, 5);
var b = Interval<int>.Closed(1, 5);
var c = Interval<int>.ClosedOpen(1, 5);

Console.WriteLine(a == b);  // True
Console.WriteLine(a == c);  // False — inclusivity differs

Console.WriteLine(a.GetHashCode() == b.GetHashCode());  // True
```

## When *not* to use `Interval<T>`

- **Unbounded ranges.** Both endpoints are always concrete values of
  `T`. To represent ranges with no lower or no upper bound, either
  use floating-point infinity (`double.NegativeInfinity` /
  `double.PositiveInfinity`) when `T` is `double`, or model the
  unbounded case at a higher level — `Interval<T>?` plus null for
  "no bound on this side", or a dedicated `RangeBound<T>` discriminated
  union.
- **Disjoint-union semantics.** `TryUnion` returns `false` for
  disjoint operands rather than producing two intervals. If you
  need a set type that can hold multiple disjoint intervals (e.g.
  "all dates in Q1 and Q3"), build an `IntervalSet<T>` on top — it
  is intentionally out of scope for the 1.0 type.
- **Cyclic / wrap-around ranges.** `Interval<T>` assumes the natural
  total ordering of `T`. Wrap-around ranges such as `[Mon, Wed]` on
  a `DayOfWeek` cycle, or `[23:00, 02:00]` on the clock, do not fit
  the contract and should be modeled separately.

## See also

- [`Interval<T>` API reference](xref:Bodu.Numerics.Interval`1)
- [`Interval` static factory helpers](xref:Bodu.Numerics.Interval)
- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1)
