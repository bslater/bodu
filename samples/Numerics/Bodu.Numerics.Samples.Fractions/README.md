# Bodu.Numerics.Samples.Fractions

`Fraction<T>` from `Bodu.Numerics`: an immutable exact rational number — a ratio of two integers,
always held in canonical (fully reduced) form, backed by any `IBinaryInteger<T>` component type.
Four scenarios cover canonical arithmetic, the text surface, generic-math participation, and
continued-fraction expansion.

Everything runs offline with fixed inputs — deterministic output every run. Numeric formatting uses
`CultureInfo.InvariantCulture` so the output never varies by machine culture.

```bash
dotnet run --project samples/Numerics/Bodu.Numerics.Samples.Fractions
```

## Scenario 1 — ExactArithmetic

**Intent.** Show `Fraction<T>` as an exact rational: every value reduces to canonical form on
creation, and `+ - * /` stay exact with no floating-point drift and no manual reduction step.

**What it does.** Builds fractions from integer components with the two-argument constructor (which
reduces immediately — `2/4` arrives as `1/2`), combines them with all four operators, adds the
`Zero`/`One` identities, demonstrates the `0.1 + 0.2` floating-point trap resolving exactly to
`3/10`, and shows a `Fraction<BigInteger>` keeping a 21-digit numerator exact.

**What to expect.** Every result is already reduced; the identities print as `0` and `1`; `1/10 +
2/10` is exactly `3/10`; and the `BigInteger`-backed sum keeps full precision:

```text
--- Fraction<T>: exact rational arithmetic ---
2/4 reduces to        : 1/2
1/2 + 1/3             : 5/6
1/2 - 1/3             : 1/6
1/2 * 1/3             : 1/6
1/2 / 1/3             : 3/2
identities            : Zero=0, One=1
1/2 + Zero, 1/2 * One : 1/2, 1/2
1/10 + 2/10 exactly   : 3/10
(10^20)/7 + 1/7        : 100000000000000000001/7
```

**APIs demonstrated.** `new Fraction<int>(numerator, denominator)`, `new Fraction<BigInteger>(...)`,
`operator +` / `-` / `*` / `/`, `Fraction<T>.Zero`, `Fraction<T>.One`, `Fraction<T>.ToString`.

## Scenario 2 — ParseAndFormat

**Intent.** Cover the text surface: parsing the canonical `numerator/denominator` form, the
non-throwing `TryParse`, the alternate `ToString` shapes, and the allocation-free span/UTF-8
`TryFormat` path.

**What it does.** Parses `"6/8"` (parsing reduces, so it arrives as `3/4`), uses `TryParse` on a
good and a bad input, renders an improper fraction three ways (`ToString`, `ToMixedNumberString`,
`ToPercentString`), and formats into caller-owned `char` and `byte` spans with `TryFormat`.

**What to expect.** The parsed value is reduced, `TryParse` reports `True`/`False` without throwing,
the mixed-number form splits `7/3` into `2 1/3`, and both `TryFormat` overloads write `7/3`:

```text
--- Fraction<T>: parse and format ---
Parse("6/8")            : 3/4
TryParse("22/7")        : True -> 22/7
TryParse("not-a-fraction"): False
ToString()              : 7/3
ToMixedNumberString()   : 2 1/3
ToPercentString()       : 25%
TryFormat(char)         : '7/3' (3 chars)
TryFormat(utf8)         : '7/3' (3 bytes)
```

**APIs demonstrated.** `Fraction<T>.Parse(string, IFormatProvider)`, `Fraction<T>.TryParse`,
`Fraction<T>.ToString`, `Fraction<T>.ToMixedNumberString`, `Fraction<T>.ToPercentString`,
`Fraction<T>.TryFormat(Span<char>, ...)`, `Fraction<T>.TryFormat(Span<byte>, ...)`.

## Scenario 3 — GenericMath

**Intent.** Show that `Fraction<T>` implements `INumber<Fraction<T>>`, so it drops straight into any
algorithm written against the .NET generic-math interfaces — one generic method serves `int` and
`Fraction<int>` alike.

**What it does.** Defines a single `Sum<T>(IEnumerable<T>) where T : INumber<T>` that starts from
`T.Zero` and folds with `operator +`. It calls that method first over the integers `1..5`, then over
the unit fractions `1/1 .. 1/5` to compute the exact harmonic number `H_5`.

**What to expect.** The integer sum is `15`; the fraction sum is the exact `137/60` (not a rounded
decimal), which equals `2.283333` when projected to a `double`:

```text
--- Fraction<T>: generic math (INumber<T>) ---
Sum<int>(1..5)          : 15
Sum<Fraction>(H_5)      : 137/60
H_5 as double           : 2.283333
```

**APIs demonstrated.** `Fraction<T>` as `INumber<Fraction<T>>`, `T.Zero`, `operator +`,
`Fraction<T>.ToDouble`.

## Scenario 4 — ContinuedFractions

**Intent.** Show the continued-fraction and rational-approximation surface: expanding a rational
into its simple-continued-fraction coefficients and back, approximating a real number to a bounded
denominator, and snapping an exact fraction to a smaller denominator.

**What it does.** Expands `415/93` into its coefficients `[4; 2; 6; 7]`, reconstructs the exact value
from them, approximates `Math.PI` under denominator caps of 100 and 1000 (yielding the convergents
`311/99` and `355/113`), and calls `LimitDenominator(20)` to snap `415/93` to the nearest fraction
with a denominator of at most 20.

**What to expect.** The expansion round-trips exactly (`matches: True`); the tighter denominator cap
gives the sharper Pi convergent; and `LimitDenominator` yields `58/13`:

```text
--- Fraction<T>: continued fractions and approximation ---
415/93 expands to       : [4; 2; 6; 7]
reconstructed           : 415/93 (matches: True)
Approximate(Pi, <=100)  : 311/99 = 3.141414
Approximate(Pi, <=1000) : 355/113 = 3.141593
415/93 limited to d<=20 : 58/13 = 4.461538
```

**APIs demonstrated.** `Fraction<T>.ToContinuedFraction`, `Fraction<T>.FromContinuedFraction`,
`Fraction<T>.Approximate(double, T)`, `Fraction<T>.LimitDenominator`, `Fraction<T>.ToDouble`,
`operator ==`.

## Layout

```text
Bodu.Numerics.Samples.Fractions/
  Program.cs                     # runs the scenarios in order
  Scenarios/ExactArithmetic.cs
  Scenarios/ParseAndFormat.cs
  Scenarios/GenericMath.cs
  Scenarios/ContinuedFractions.cs
```

## Related

- `Bodu.Numerics.Samples.Intervals` — the interval algebra (`Interval<T>`, `DiscreteInterval<T>`,
  `IntervalSet<T>`) built over the same numeric surface.
- `Bodu.Numerics.Samples.JsonConverters` — round-tripping `Fraction<T>` and the interval types
  through `System.Text.Json` with the companion serialization package.
```
