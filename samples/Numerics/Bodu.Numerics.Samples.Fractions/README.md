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
--- Fraction<T> - exact rational arithmetic ---
  What   : Reduces a fraction on construction, runs the four operators, checks the additive and multiplicative
           identities, and adds tenths - then repeats with a numerator of 10^20.
  Why    : Binary floating point cannot represent one tenth, so 0.1 + 0.2 != 0.3 in double and every subsequent
           comparison inherits that error. A rational keeps a numerator and a denominator, so tenths are exact and
           stay exact however long the chain gets. The second half is the other half of the argument: backed by
           BigInteger the numerator can exceed what a long holds, so precision does not quietly fall off a cliff at
           a magnitude nobody tested.
  Expect : 2/4 reduces to 1/2 at construction, not on demand, so equality and hashing work on the canonical form.
           1/10 + 2/10 is exactly 3/10 rather than 0.30000000000000004, and the 10^20 sum is exact to the last
           digit.

  2/4 reduces to        : 1/2  (reduced at construction, so equality and GetHashCode work on the canonical form rather than on 2 and 4)
  1/2 + 1/3             : 5/6
  1/2 - 1/3             : 1/6
  1/2 * 1/3             : 1/6
  1/2 / 1/3             : 3/2
  identities            : Zero=0, One=1  (Zero and One come from INumber<T>, which is what lets generic algorithms seed an accumulator)
  1/2 + Zero, 1/2 * One : 1/2, 1/2
  1/10 + 2/10 exactly   : 3/10  (expected 3/10 exactly - the same sum in double gives 0.30000000000000004, and every later comparison inherits that)
  (10^20)/7 + 1/7        : 100000000000000000001/7  (BigInteger-backed, so precision does not fall off a cliff at a magnitude nobody thought to test)
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
--- Fraction<T> - parse and format ---
  What   : Parses fraction text, shows TryParse on a good and a malformed input, then renders one value as a plain
           fraction, a mixed number and a percentage, and writes it through both span formatters.
  Why    : A rational is only useful in a system if it survives the round trip to text and back, which is what makes
           it safe in configuration, a wire format or a database column. Parse reduces on the way in, so 6/8 and 3/4
           are the same value and the text form does not have to be canonical. The span and UTF-8 formatters exist
           so that writing one into a buffer costs no intermediate string.
  Expect : 6/8 parses to 3/4 - reduced on construction. TryParse returns False for nonsense rather than throwing.
           The same 7/3 renders three ways, and both TryFormat overloads report the same three units written, one in
           chars and one in UTF-8 bytes.

  Parse("6/8")            : 3/4  (expected 3/4 - reduced on the way in, so stored text does not have to be canonical)
  TryParse("22/7")        : True -> 22/7  (already in lowest terms, so it parses unchanged)
  TryParse("not-a-fraction"): False  (expected False - malformed input is a return value, not an exception, which is what makes this safe on untrusted text)
  ToString()              : 7/3
  ToMixedNumberString()   : 2 1/3  (the same 7/3 for a human reader; the value is unchanged, only its rendering)
  ToPercentString()       : 25%
  TryFormat(char)         : '7/3' (3 chars)  (writes into a caller-supplied span - no intermediate string allocated)
  TryFormat(utf8)         : '7/3' (3 bytes)  (the same via UTF-8 bytes, for writing straight to a wire format or a file)
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
--- Fraction<T> - generic math (INumber<T>) ---
  What   : Runs one generic Sum<T> over ints and then over fractions, summing the first five terms of the harmonic
           series, and converts the result to double.
  Why    : Implementing INumber<T> means Fraction<T> is not a special case to be handled separately - the same
           generic algorithm, written once against the interface, accepts it alongside the built-in numeric types.
           The harmonic series is the example that earns it: every term has a different denominator, so summing in
           double accumulates rounding at each step while the rational sum is exact and only converts at the end,
           where the caller can see it happen.
  Expect : One Sum implementation serves both element types. The harmonic sum is exactly 137/60; the double
           conversion is where precision is deliberately given up, and it happens once, at the boundary, rather than
           silently at every addition.

  Sum<int>(1..5)          : 15  (expected 15 - one generic implementation, written against INumber<T>)
  Sum<Fraction>(H_5)      : 137/60  (expected 137/60 - the SAME Sum, now over rationals: exact, where summing in double would round at every term)
  H_5 as double           : 2.283333  (precision is given up once, here at the boundary, rather than silently at each addition)
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
--- Fraction<T> - continued fractions and approximation ---
  What   : Expands a fraction into its continued-fraction terms and rebuilds it, then approximates pi under two
           denominator limits and re-approximates a rational under a tighter one.
  Why    : A continued fraction gives the best rational approximation for any bound on the denominator - not merely
           a good one - which is what you want when a value must be expressed in whole units: gear teeth,
           sample-rate conversion, a timing divisor. Rounding a decimal to a fraction gets this wrong, since the
           nearest short denominator is rarely the one you reach by truncating.
  Expect : 415/93 expands to [4; 2; 6; 7] and rebuilds exactly, which is the round trip. Allowing a denominator up
           to 1000 finds 355/113 - correct to six decimal places, and the classical approximation to pi - where a
           limit of 100 can only reach 311/99.

  415/93 expands to       : [4; 2; 6; 7]  (the continued-fraction terms; every rational has a finite expansion)
  reconstructed           : 415/93 (matches: True)  (expected True - the expansion round-trips exactly, so nothing was lost)
  Approximate(Pi, <=100)  : 311/99 = 3.141414  (the best possible fraction with a denominator under 100 - not merely a close one)
  Approximate(Pi, <=1000) : 355/113 = 3.141593  (expected 355/113 - the classical approximation, correct to six decimal places)
  415/93 limited to d<=20 : 58/13 = 4.461538  (a tighter bound forces a coarser answer; this is the trade made explicit rather than hidden in a rounding)
```

**APIs demonstrated.** `Fraction<T>.ToContinuedFraction`, `Fraction<T>.FromContinuedFraction`,
`Fraction<T>.Approximate(double, T)`, `Fraction<T>.LimitDenominator`, `Fraction<T>.ToDouble`,
`operator ==`.

## Layout

```text
Bodu.Numerics.Samples.Fractions/
  Program.cs                     # runs the scenarios in order
  SampleConsole.cs               # the what/why/expect banner every scenario opens with
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
