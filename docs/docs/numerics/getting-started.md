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
seven_thirds.ToString("P");   // "233/1%" (percentage form)

Fraction<int>.Parse("2 1/3");                 // 7/3
Fraction<int>.Parse("⅗");                     // 3/5
Fraction<int>.Parse("75%");                   // 3/4
```

`Parse` and `TryParse` accept integers, ratios, mixed numbers, the 18 Unicode vulgar-fraction glyphs the library ships, and percentage syntax. See [Formatting and parsing `Fraction<T>`](../../guides/numerics/formatting-and-parsing.md) for the full table.

### JSON

`Fraction<T>` is decorated with `[JsonConverter(typeof(FractionJsonConverterFactory))]`, so `System.Text.Json` round-trips without extra wiring. The wire shape is the string form:

```csharp
using System.Text.Json;
using Bodu.Numerics;

string json = JsonSerializer.Serialize(Fraction<int>.Create(3, 4));
// "3/4"

Fraction<int> roundTrip = JsonSerializer.Deserialize<Fraction<int>>(json);
```

### Bounded intervals (`Interval<T>`)

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

Use the non-generic `Interval` helper class when you want the compiler to infer the endpoint type:

```csharp
var span = Interval.Closed(1.5, 2.5);   // Interval<double>
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
- **[Bodu.Financial getting started](../financial/getting-started.md)** — for monetary primitives built on `Fraction<BigInteger>`.
- **[Bodu.Numerics API reference](xref:Bodu.Numerics)** — full type-by-type docs.
