---
title: Bodu.Numerics — Getting started
---

# Bodu.Numerics — Getting started

## Install

```bash
dotnet add package Bodu.Numerics
```

Targets `net8.0`. No external runtime dependencies — `Bodu.Numerics` references only `Bodu.Core` for shared argument validation.

## Minimal samples

### Exact rational arithmetic (`Fraction<T>`)

```csharp
using Bodu.Numerics;

Fraction<int> third = Fraction<int>.Create(1, 3);
Fraction<int> half  = Fraction<int>.Create(1, 2);

Fraction<int> sum = third + half;          // 5/6 — exact, no float drift
Fraction<int> product = third * half;      // 1/6
Fraction<int> ratio = sum / Fraction<int>.Create(7, 12);  // 10/7
```

The constructor reduces to canonical form: `Fraction<int>.Create(2, 4)` and `Fraction<int>.Create(1, 2)` are indistinguishable after construction. Negative denominators are flipped so the sign always lives on the numerator.

### Arbitrary precision (`Fraction<BigInteger>`)

```csharp
using System.Numerics;
using Bodu.Numerics;

Fraction<BigInteger> principal = Fraction<BigInteger>.Create(1_000_000, 1);
Fraction<BigInteger> monthlyRate = Fraction<BigInteger>.Create(5, 1200);  // 5% / 12 months
Fraction<BigInteger> growth = Fraction<BigInteger>.One + monthlyRate;

Fraction<BigInteger> balance = principal;
for (int i = 0; i < 360; i++) balance *= growth;     // exact 30-year amortization, no rounding drift
```

Backed by `BigInteger`, intermediate results never overflow. Reach for `Fraction<BigInteger>` whenever a calculation chains several multiplications or divisions and you need a single, deferred rounding boundary.

> [!TIP]
> Three construction calls have defined failure modes worth knowing up front: `Create(n, 0)` throws <xref:System.DivideByZeroException>; a canonical result that does not fit a fixed-width `T` throws <xref:System.OverflowException>; and `FromDouble(double.NaN)` (or any non-finite `double`) throws <xref:System.ArgumentException>. Each has a non-throwing partner — `TryCreate`, `TryFromBigInteger`, `TryFromDouble`, `TryFromDecimal` — that reports the same condition with a `false` return.

### Exact versus best-fit conversion from `double`

`FromDouble` is exact in the IEEE-754 sense — it decomposes the `double`'s mantissa and exponent, so a value that looks "round" in base 10 can produce a fraction with an enormous denominator:

```csharp
Fraction<BigInteger>.FromDouble(0.1);
// 3602879701896397/36028797018963968 — the exact bits of the double 0.1, not 1/10
```

When you want the *intended* rational rather than the bit-exact one, reach for `Approximate` with a denominator bound (below), which recovers `1/10` from `0.1`.

### Best rational approximation

```csharp
Fraction<int> piApprox = Fraction<int>.Approximate(Math.PI, maxDenominator: 1000);
// 355/113 — the Zǔ Chōngzhī approximation, accurate to ~6×10⁻⁷
```

`Approximate` uses convergents of the continued-fraction expansion to find the best rational below a denominator bound.

### Mixed-number and Unicode formatting

```csharp
var seven_thirds = Fraction<int>.Create(7, 3);

seven_thirds.ToString("G");   // "7/3"
seven_thirds.ToString("M");   // "2 1/3"
seven_thirds.ToString("U");   // "2⅓"
seven_thirds.ToString("P");   // "700/3%" (percentage = value × 100, re-reduced)

Fraction<int>.Parse("2 1/3");                 // 7/3
Fraction<int>.Parse("⅗");                     // 3/5
Fraction<int>.Parse("75%");                   // 3/4
```

`Parse` and `TryParse` accept integers, ratios, mixed numbers, the 18 Unicode vulgar-fraction glyphs the library ships, and percentage syntax. See [Formatting and parsing `Fraction<T>`](../../guides/numerics/formatting-and-parsing.md) for the full table.

### JSON

JSON support ships in the companion **`Bodu.Numerics.Serialization.Json`** package — the core library is serialization-agnostic. Register the converters on a `JsonSerializerOptions` with `AddNumericsJsonConverters`; the default (`Strict`) wire shape is the canonical object form, and `NumericsJsonPolicy.Compact` selects the `"3/4"` string:

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

var options = new JsonSerializerOptions().AddNumericsJsonConverters();

string json = JsonSerializer.Serialize(new Fraction<int>(3, 4), options);
// {"numerator":3,"denominator":4}

Fraction<int> roundTrip = JsonSerializer.Deserialize<Fraction<int>>(json, options);

// Compact policy — the single-string form.
var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);
JsonSerializer.Serialize(new Fraction<int>(3, 4), compact);   // "3/4"
```

### Bounded intervals (`Interval<T>`)

`Interval<T>` packs a range — lower endpoint, upper endpoint, and the inclusivity of each side — into a single immutable value over any `INumber<T>` endpoint type. The set algebra (membership, containment, intersection, union, overlap, adjacency) is defined on the type.

```csharp
using Bodu.Numerics;

Interval<int> period = Interval<int>.ClosedOpen(0, 100);   // [0, 100)
Interval<int> window = Interval<int>.Closed(50, 200);      // [50, 200]

bool overlap = period.Overlaps(window);                    // True
Interval<int> shared = period.Intersect(window);           // [50, 100)
bool joined = period.TryUnion(window, out var u);          // True; u = [0, 200]

period.Contains(50);                                       // True
period.Contains(100);                                      // False — upper exclusive
```

#### Inferring the endpoint type

The non-generic `Interval` helper class mirrors every factory on `Interval<T>` but lets the compiler infer `T` from the arguments — useful when the endpoint type is obvious from literals or locals:

```csharp
var span    = Interval.Closed(1.5, 2.5);            // Interval<double>
var prices  = Interval.OpenClosed(1000m, 10_000m);  // Interval<decimal>
var ints    = Interval.ClosedOpen(0, 100);          // Interval<int>
```

#### Scheduling: detect a clash and trim it

The closed-open shape `[a, b)` is the natural choice for time slots — adjacent slots share a single boundary without double-counting it.

```csharp
var morning = Interval<int>.ClosedOpen(9,  12);   // [9, 12) — 9am–noon
var meeting = Interval<int>.ClosedOpen(11, 13);   // [11, 13) — overlapping meeting

if (morning.Overlaps(meeting))
{
    var clash = morning.Intersect(meeting);       // [11, 12)
    Console.WriteLine($"Clash: {clash}");          // "Clash: [11, 12)"
}
```

#### Validation predicate

A `Closed` interval doubles as a "valid input" predicate that documents its own bounds:

```csharp
var percentage = Interval<double>.Closed(0.0, 100.0);

double Sanitize(double value) =>
    percentage.Contains(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));

Sanitize(99.5);   // 99.5
Sanitize(100.0);  // 100.0 — closed upper
Sanitize(101.0);  // throws
```

#### Partitioning a span into adjacent buckets

Adjacent half-open intervals partition a range cleanly with no overlap and no gap. Together they cover the whole span and exactly one of them contains any given value.

```csharp
var q1 = Interval<int>.ClosedOpen(0,   90);
var q2 = Interval<int>.ClosedOpen(90,  181);
var q3 = Interval<int>.ClosedOpen(181, 273);
var q4 = Interval<int>.ClosedOpen(273, 365);

int BucketOf(int dayOfYear) =>
    q1.Contains(dayOfYear) ? 1 :
    q2.Contains(dayOfYear) ? 2 :
    q3.Contains(dayOfYear) ? 3 :
    q4.Contains(dayOfYear) ? 4 :
    throw new ArgumentOutOfRangeException(nameof(dayOfYear));

BucketOf(0);    // 1 — closed lower of q1
BucketOf(90);   // 2 — q1 ends before 90; q2 includes it
BucketOf(364);  // 4 — q4 contains [273, 365)
```

#### Formatting and parsing

`Interval<T>` formats using ISO 31-11 bracket notation and parses the same syntax. The empty interval renders as the U+2205 EMPTY SET glyph (`∅`).

```csharp
using System.Globalization;

Interval<int>.Closed(1, 5).ToString();       // "[1, 5]"
Interval<int>.ClosedOpen(0, 100).ToString(); // "[0, 100)"
Interval<int>.Empty.ToString();              // "∅"

Interval<double>
    .Closed(1.5, 2.75)
    .ToString("F2", CultureInfo.InvariantCulture);   // "[1.50, 2.75]"

Interval<int> parsed = Interval<int>.Parse("(0, 100]", CultureInfo.InvariantCulture);
Interval<int>.TryParse("∅", CultureInfo.InvariantCulture, out var none);  // none = Empty
```

`Interval<T>` implements `ISpanFormattable` and `IUtf8SpanFormattable`, so the same text round-trips through character and UTF-8 byte buffers without allocation.

#### JSON

`Interval<T>` serializes through the companion `Bodu.Numerics.Serialization.Json` package. Register the converters with `AddNumericsJsonConverters`; the default policy is `Strict` and produces an explicit object shape (with `lowerUnbounded` / `upperUnbounded` markers for infinite sides), while `NumericsJsonPolicy.Compact` selects the bracket-notation string form.

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

// Default (Strict) — explicit object shape, both bounded sides required on read.
JsonSerializerOptions options = new JsonSerializerOptions().AddNumericsJsonConverters();
string json = JsonSerializer.Serialize(Interval<int>.ClosedOpen(0, 100), options);
// {"lower":0,"upper":100,"lowerInclusive":true,"upperInclusive":false}

Interval<int> roundTrip = JsonSerializer.Deserialize<Interval<int>>(json, options);

// Compact policy — string form using ISO 31-11 bracket notation.
JsonSerializerOptions compactOptions = new JsonSerializerOptions()
    .AddNumericsJsonConverters(NumericsJsonPolicy.Compact);

string compact = JsonSerializer.Serialize(Interval<int>.ClosedOpen(0, 100), compactOptions);
// "[0, 100)"
```

The empty interval serializes to `{"empty":true}` under `Strict` and `Lenient`, and to `"∅"` under `Compact`.

#### The empty interval is unique

Any inverted-bounds or equal-bounds-with-open-endpoint interval compares equal to `Interval<T>.Empty` regardless of the bounds used to construct it. A default-constructed value is empty too, so `Interval<T>` fields never carry a malformed default.

```csharp
var none      = Interval<int>.Empty;
var inverted  = new Interval<int>(5, 1, true, true);
var collapsed = new Interval<int>(0, 0, false, false);
Interval<int> defaulted = default;

none == inverted && none == collapsed && none == defaulted;  // True
```

#### Unbounded ranges, difference, and operators

A side of an interval can be unbounded, and the set algebra includes difference and operators:

```csharp
Interval<double> nonNegative = Interval<double>.AtLeast(0.0);   // [0, +∞)
Interval<double> capped      = Interval<double>.AtMost(100.0);  // (-∞, 100]

nonNegative.Contains(1e300);                 // True
Interval<double> band = nonNegative & capped;   // & is Intersect → [0, 100]

// Difference yields at most two pieces (an IntervalPair<T>).
IntervalPair<int> gap = Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(3, 5));
gap.Count;                                    // 2
foreach (var piece in gap)
    Console.WriteLine(piece);                 // "[0, 3)" then "(5, 10]"

// | is the contiguous union; it throws on a gapped pair.
var merged = Interval<int>.ClosedOpen(1, 5) | Interval<int>.Closed(5, 10);   // [1, 10]
```

#### Disconnected sets (`IntervalSet<T>`)

When a result can be an arbitrary union of disjoint ranges, use `IntervalSet<T>`:

```csharp
IntervalSet<int> set = IntervalSet<int>.Of(
    Interval<int>.Closed(1, 3),
    Interval<int>.Closed(2, 5),
    Interval<int>.Closed(8, 9));   // coalesces to [1, 5] ∪ [8, 9]

set.Contains(4);                    // True
set.Except(Interval<int>.Closed(4, 4));   // [1, 4) ∪ (4, 5] ∪ [8, 9]
set.Complement();                   // (-∞, 1) ∪ (5, 8) ∪ (9, +∞)
```

#### Discrete integer intervals (`DiscreteInterval<T>`)

`Interval<T>` is continuous; `DiscreteInterval<T>` models the set of representable integers, so an open interval over consecutive integers is empty and successor-adjacent runs merge:

```csharp
DiscreteInterval<int>.Open(1, 2).IsEmpty;   // True — no integer strictly between 1 and 2

var a = DiscreteInterval<int>.Closed(1, 2);
var b = DiscreteInterval<int>.Closed(3, 4);
a.TryUnion(b, out var run);                  // run = [1, 4] — 2 and 3 are successors

DiscreteInterval<int>.Closed(1, 10).Count;   // 10
```

### Cross-package: Fraction-backed monetary arithmetic

`Bodu.Financial.Money<TCurrency>` round-trips through `Fraction<BigInteger>` for exact intermediate calculations:

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Numerics;
using System.Numerics;

Money<USD> principal = new Money<USD>(1000m);
Fraction<BigInteger> growth = Fraction<BigInteger>.One + Fraction<BigInteger>.Create(5, 1200);

Fraction<BigInteger> exact = principal.ToFraction();
for (int i = 0; i < 24; i++) exact *= growth;

Money<USD> balance = Money<USD>.FromFraction(exact);   // one rounding event
```

## Where to go next

- **[Bodu.Numerics introduction](index.md)** — namespaces, headline types, scenarios.
- **[Working with `Fraction<T>`](../../guides/numerics/fraction.md)** — construction, arithmetic, parsing/formatting, continued fractions, rational approximation.
- **[Working with `Interval<T>`](../../guides/numerics/interval.md)** — endpoint inclusivity, membership, intersection, union, adjacency.
- **[Interval algebra](../../guides/numerics/interval-algebra.md)** — unbounded endpoints, difference / symmetric difference, operators, and `IntervalSet<T>`.
- **[Discrete integer intervals](../../guides/numerics/discrete-intervals.md)** — the integer-domain `DiscreteInterval<T>`.
- **[Bodu.Financial getting started](../financial/getting-started.md)** — for monetary primitives built on `Fraction<BigInteger>`.
- **[Bodu.Numerics API reference](xref:Bodu.Numerics)** — full type-by-type docs.
