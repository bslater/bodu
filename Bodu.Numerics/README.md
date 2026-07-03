# Bodu.Numerics

> **API stability — Preview / Release Candidate.** The rational and interval algebra is functionally complete and suitable for trial use. `Fraction<T>` is a stable candidate; the interval-set and discrete-interval APIs may still receive source-breaking refinement before the 1.0 stable release. Breaking changes are otherwise reserved for a major-version bump per [SemVer](https://semver.org).

Numeric value primitives for .NET. The public model is exact rational numbers plus an interval algebra:

- **`Fraction<T>`** — an immutable, exact-rational value type generic over any `IBinaryInteger<T>` backing component.
- **`Interval<T>`** — an immutable connected interval over any `INumber<T>` endpoint type, with independent open/closed (and unbounded) endpoints and set algebra.
- **`DiscreteInterval<T>`** — a connected integer-domain interval over `IBinaryInteger<T>`, with successor/predecessor-aware emptiness and adjacency.
- **`IntervalSet<T>`** — a normalized set of disconnected `Interval<T>` pieces, with N-ary union / intersection / difference / complement.
- **`IntervalPair<T>`** / **`DiscreteIntervalPair<T>`** — allocation-conscious results of a binary difference / symmetric-difference (zero, one, or two disjoint pieces), each convertible to an `IntervalSet<T>`.

> Money, currency, and foreign-exchange types ship in the companion **[Bodu.Financial](https://www.nuget.org/packages/Bodu.Financial)** package. Keeping them separate means a consumer of just `Fraction<T>` does not pull in the ~185-currency ISO 4217 catalogue and FX provider stack.

## Installation

```shell
dotnet add package Bodu.Numerics
```

Targets `net8.0`.

## `Fraction<T>`

`Fraction<T>` is always held in canonical form — strictly positive denominator, sign carried on the numerator, fully reduced. Arithmetic is exact: intermediate results are evaluated with `BigInteger` precision and narrowed back to `T`, throwing `OverflowException` when a fixed-width component cannot represent the canonical result.

```csharp
using Bodu.Numerics;

Fraction<int> a = Fraction<int>.Parse("1/3");
Fraction<int> b = Fraction<int>.Parse("1/6");

Fraction<int> sum = a + b;          // 1/2 (canonical, fully reduced)
string mixed     = (a + 1).ToString("M");   // "1 1/3"
decimal asDecimal = (decimal)sum;            // 0.5m
```

Highlights:

- Arithmetic and comparison operators, named methods (`Add`, `Negate`, `Abs`, `Reciprocal`, `Pow`, `Remainder`), and `GreatestCommonDivisor` / `LeastCommonMultiple`.
- Exact conversions to/from `decimal` and `double` (`FromDecimal` / `FromDouble`), plus `As<TOther>()` to retype the backing component.
- Continued-fraction expansion (`ToContinuedFraction` / `FromContinuedFraction`) and bounded best-rational approximation (`LimitDenominator`).
- Parsing of integer, ratio, mixed-number, Unicode vulgar-fraction, and percent forms across `string`, `ReadOnlySpan<char>`, and UTF-8 (`IParsable`, `ISpanParsable`, `IUtf8SpanParsable`); formatting with general, mixed (`M`), Unicode (`U`), and percent (`P`) specifiers.
- The full generic-math surface — `INumber<Fraction<T>>`, `INumberBase<Fraction<T>>`, `ISignedNumber<Fraction<T>>` — so `Fraction<T>` composes with `INumber<T>`-constrained code.
- XML serialization (`IXmlSerializable`) and `System.Text.Json` support.

## `Interval<T>`

```csharp
using Bodu.Numerics;

Interval<int> a = Interval.Closed(1, 5);      // [1, 5]
Interval<int> b = Interval.OpenClosed(4, 8);  // (4, 8]

bool overlaps          = a.Overlaps(b);       // true
Interval<int> meet     = a.Intersect(b);      // (4, 5]
bool joined            = a.TryUnion(b, out Interval<int> u); // true -> [1, 8]
bool contains          = a.Contains(3);       // true
```

Highlights:

- Independent endpoint inclusivity: `[a, b]`, `(a, b)`, `[a, b)`, `(a, b]`.
- Factory methods (`Closed`, `Open`, `ClosedOpen`, `OpenClosed`, `Singleton`, `Empty`) plus a non-generic `Interval` helper that infers `T` (`Interval.Closed(1, 5)`).
- Set algebra: `Contains(T)`, `Contains(Interval<T>)`, `Overlaps`, `Intersect`, and `TryUnion` (which succeeds only when the result is a single contiguous interval).
- `IsEmpty`, `IsDegenerate`, and `Length`. All empty intervals compare equal to `Empty`.
- ISO 31-11 bracket-notation formatting and parsing (`IFormattable` / `ISpanFormattable` / `IUtf8SpanFormattable` / `IParsable` / `ISpanParsable`). Empty intervals render as the U+2205 EMPTY SET glyph.

### When to use `Interval<T>` vs `DiscreteInterval<T>`

Use **`Interval<T>`** when the endpoints are coordinates in an ordered numeric *continuum* — even when `T` is an integer coordinate type — so the values between the bounds matter. Use **`DiscreteInterval<T>`** (over `IBinaryInteger<T>`) when the interval represents the *set of integers* between its bounds. The distinction is observable:

```csharp
Interval<int>.Open(1, 2).IsEmpty;          // False — the real coordinates between 1 and 2
DiscreteInterval<int>.Open(1, 2).IsEmpty;  // True  — no integer lies strictly between 1 and 2
```

`DiscreteInterval<T>` is integer-only; it is not a general discrete-domain abstraction over `DateOnly`, `char`, or enum ranges. Reach for `IntervalSet<T>` when a set operation can produce a disconnected result.

## Documentation

See the [Bodu.Numerics guide](https://github.com/bslater/bodu/blob/master/docs/guides/numerics/index.md), including the dedicated [`Interval<T>` article](https://github.com/bslater/bodu/blob/master/docs/guides/numerics/interval.md).

## License

MIT. © Bodu Pty. Ltd.
