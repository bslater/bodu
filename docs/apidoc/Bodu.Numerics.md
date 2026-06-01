---
uid: Bodu.Numerics
---

![Bodu.Numerics](~/images/hero-numerics.svg)

## Purpose

**Bodu.Numerics** provides exact rational arithmetic (`Fraction<T>`) and bounded numeric intervals (`Interval<T>`) on top of the .NET generic-math interfaces. Both types are immutable value types, value-equatable, and composable with any algorithm written against `INumber<T>` / `ISignedNumber<T>`.

Reach for this library when you need rational arithmetic that does not lose precision to floating-point drift, when you need a single representation for closed / open / half-open intervals, or when you need a precision escape hatch for chained calculations that would otherwise accumulate rounding error.

## Static documentation

- **[Bodu.Numerics introduction](~/docs/numerics/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Numerics getting started](~/docs/numerics/getting-started.md)** — install and minimal samples for `Fraction<T>` and `Interval<T>`.
- **[Bodu.Numerics guides](~/guides/numerics/index.md)** — recipe-style walk-throughs: [`Fraction<T>`](~/guides/numerics/fraction.md), [`Interval<T>`](~/guides/numerics/interval.md).

## Key types

**Rational arithmetic**

- <xref:Bodu.Numerics.Fraction`1> — immutable rational with auto-reduction to canonical form, `BigInteger`-promoted intermediates for safe arithmetic, and the full `INumber<T>` / `ISignedNumber<T>` surface. Backed by any `IBinaryInteger<T>` — `int`, `long`, `BigInteger`, or a custom type.
- <xref:Bodu.Numerics.FractionJsonConverter`1>, <xref:Bodu.Numerics.FractionJsonConverterFactory> — `System.Text.Json` converters auto-registered via `[JsonConverter]`; wire shape is the string form `"numerator/denominator"`.

**Bounded intervals**

- <xref:Bodu.Numerics.Interval`1> — immutable interval over any `INumber<T>` endpoint type. Independent endpoint inclusivity on each side expresses all four conventional shapes; one canonical `Empty` instance covers every degenerate / inverted-bound case.
- <xref:Bodu.Numerics.Interval> — non-generic helper class with factory methods that infer the endpoint type from arguments (`Interval.Closed(1.5, 2.5)` → `Interval<double>`).

## Example

```csharp
using System.Numerics;
using Bodu.Numerics;

// Exact rational arithmetic — no floating-point drift.
Fraction<int> sum = Fraction<int>.Create(1, 3) + Fraction<int>.Create(1, 5);
Console.WriteLine(sum);        // "8/15"

// Best rational approximation to a real number.
Fraction<int> piApprox = Fraction<int>.Approximate(Math.PI, maxDenominator: 1000);
Console.WriteLine(piApprox);   // "355/113"

// Bounded intervals with independent endpoint inclusivity.
var window = Interval<int>.ClosedOpen(0, 100);
Console.WriteLine(window.Contains(99));   // True
Console.WriteLine(window.Contains(100));  // False — upper exclusive
```

## Notes

- **Canonical form.** Every `Fraction<T>` is GCD-reduced on construction, with the sign on the numerator and the denominator strictly positive. `Fraction<int>.Create(2, 4)` and `Fraction<int>.Create(1, 2)` are indistinguishable; there is no unreduced form.
- **Overflow handling.** Arithmetic operations promote operands to `BigInteger`, evaluate exactly, then narrow back to `T`. Overflow on narrowing raises `OverflowException`. Use `Fraction<BigInteger>` to eliminate the narrowing step entirely.
- **The empty interval is unique.** Any `Interval<T>` constructed with inverted bounds, or with equal bounds and at least one open endpoint, compares equal to `Interval<T>.Empty` and shares its hash code.
- **Generic-math first.** Both types implement the relevant `INumber`-style interfaces — including `IParsable<T>`, `ISpanParsable<T>`, `ISpanFormattable`, and `IUtf8SpanFormattable` — so they slot into algorithms written against the generic-math abstractions without bespoke wrappers.
- **Cross-package.** `Bodu.Financial.Money<TCurrency>` round-trips through `Fraction<BigInteger>` via `ToFraction()` / `FromFraction()` / `MultiplyExact()` for sub-minor-unit-precise monetary chains; see the [Bodu.Financial overview](xref:Bodu.Financial).
- **See also:** the [`Fraction<T>` guide](~/guides/numerics/fraction.md), the [`Interval<T>` guide](~/guides/numerics/interval.md).
