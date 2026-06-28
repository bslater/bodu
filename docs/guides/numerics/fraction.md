---
title: Working with Fraction<T>
---

# Working with `Fraction<T>`

`Fraction<T>` is an immutable, value-equatable rational number whose
backing integer type is the type parameter `T`. The type auto-reduces
to canonical form on construction (GCD-normalised, sign on the
numerator, denominator strictly positive), promotes intermediate
arithmetic to `BigInteger` for safe evaluation, and implements the
full `INumber<T>` / `ISignedNumber<T>` surface so it composes with
generic-math algorithms without bespoke wrappers.

The type works with any backing type that implements
`IBinaryInteger<T>`: `sbyte`, `byte`, `short`, `ushort`, `int`,
`uint`, `long`, `ulong`, `Int128`, `UInt128`, `BigInteger`, and
consumer-defined integer types built on the generic-math interfaces.
Use `Fraction<BigInteger>` whenever a calculation chains several
multiplications or divisions — overflow is not possible and there is
no narrowing step.

## Creating fractions

Use the static factory methods on `Fraction<T>` directly when the
backing type is fixed. Unlike `Interval<T>`, there is no non-generic
helper class for inference — pick the backing type up front.

```csharp
using Bodu.Numerics;

// Two-argument factories normalise on construction.
Fraction<int> half     = Fraction<int>.Create(1, 2);     // 1/2
Fraction<int> twoFourths = Fraction<int>.Create(2, 4);   // 1/2 — auto-reduced
Fraction<int> negThirds  = Fraction<int>.Create(3, -4);  // -3/4 — sign flipped to numerator

// Single-argument factory for whole numbers.
Fraction<int> seven = new Fraction<int>(7);              // 7/1

// Implicit lift from T to Fraction<T>.
Fraction<int> three = 3;                                 // 3/1

// Non-throwing variant.
if (Fraction<int>.TryCreate(7, 0, out var bad)) { /* … */ }
```

Both constructors and factories reduce to canonical form before
returning. Two operations are guaranteed to throw at construction:

- `Fraction<T>.Create(numerator, 0)` throws `DivideByZeroException`.
- A canonical result that does not fit in `T` (rare for `int`, common
  near `T.MinValue`) throws `OverflowException`.

`TryCreate(numerator, denominator, out result)` reports both
conditions through a `false` return without throwing.

### From other numeric types

```csharp
Fraction<int>.FromDecimal(0.125m);              // 1/8 — exact decimal
Fraction<int>.FromDouble(0.5);                  // 1/2 — exact for round halves
Fraction<BigInteger>.FromDouble(Math.PI);       // Very large rational — Math.PI bits
Fraction<int>.FromBigInteger(7, 3);             // Narrows BigInteger → int safely
```

`FromDecimal` is exact: it decomposes the `decimal`'s mantissa and
scale. `FromDouble` is exact in the IEEE 754 sense — it decomposes the
`double`'s mantissa and exponent, which may produce a fraction with a
very large denominator for values that look "nice" in base 10
(`FromDouble(0.1)` is not `1/10`). For a *best rational
approximation* to a real number within a denominator bound, use
`Approximate` (see below).

`FromDouble` throws `ArgumentException` on non-finite input; the
`Try…` variants return `false` instead.

### Best rational approximation

```csharp
Fraction<int> piApprox = Fraction<int>.Approximate(Math.PI, maxDenominator: 1000);
// 355/113 — the Zǔ Chōngzhī approximation, error ≈ 2.7×10⁻⁷
```

`Approximate(value, maxDenominator)` uses convergents of the
continued-fraction expansion to find the best rational with
denominator ≤ the bound. Overloads accept `double`, `decimal`, and
string input. There is also a streaming form,
`LimitDenominator(maxDenominator)`, that produces the best
approximation of an existing fraction within a tighter denominator
bound.

### Continued fractions

```csharp
Fraction<int> phi = Fraction<int>.Create(610, 377);
int[] coeffs = phi.ToContinuedFraction();
// [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1] — golden-ratio convergent

Fraction<int> reconstructed = Fraction<int>.FromContinuedFraction(coeffs);
```

The leading coefficient carries the sign; subsequent coefficients are
strictly positive. `FromContinuedFraction` enforces this on input:

- A `null` coefficient array throws <xref:System.ArgumentNullException>.
- An empty array throws <xref:System.ArgumentException>.
- A coefficient after the first that is zero or negative throws
  <xref:System.ArgumentOutOfRangeException>.

`LimitDenominator(maxDenominator)` returns the value unchanged when its
denominator already fits the bound, and throws
<xref:System.ArgumentOutOfRangeException> when `maxDenominator` is less
than one. The `Approximate(value, maxDenominator)` overloads
(`double`, `decimal`, `string`) share the same bound contract; the
`double` overload additionally throws <xref:System.ArgumentException>
on non-finite input, and the `string` overload throws
<xref:System.FormatException> on non-numeric text. Both
`Approximate` and `LimitDenominator` evaluate the search at
`BigInteger` precision and narrow only the final result, so an exact
value too large for `T` is bounded *before* it is narrowed.

```csharp
var pi = Fraction<int>.Create(355, 113);
pi.LimitDenominator(100);                    // 311/99 — closest with denominator ≤ 100
Fraction<int>.Approximate(0.1, 1000);        // 1/10 — recovers the intended rational from a double
```

## Canonical form

Every `Fraction<T>` is reduced to GCD-normalised form, with the sign
on the numerator and the denominator strictly positive. This means
`2/4` and `1/2` are indistinguishable after construction — there is
no unreduced form to preserve. Equality compares the canonical
components, so `Fraction<int>.Create(2, 4) == Fraction<int>.Create(1, 2)`
is `true`.

```csharp
var a = Fraction<int>.Create(2, 4);    // 1/2
var b = Fraction<int>.Create(1, 2);    // 1/2
var c = Fraction<int>.Create(-3, -6);  // 1/2 — both negatives cancel

Console.WriteLine(a == b);             // True
Console.WriteLine(a == c);             // True
Console.WriteLine(a.Numerator);        // 1
Console.WriteLine(a.Denominator);      // 2
```

The default-constructed `Fraction<T>` is zero: the all-zero
representation `(numerator: 0, denominator: 0)` is interpreted as
`0/1` by the `Denominator` property.

## Properties

| Property | Meaning |
|---|---|
| `Numerator` | Signed canonical numerator. |
| `Denominator` | Strictly positive canonical denominator (interprets default-init zero as one). |
| `Sign` | `-1`, `0`, or `1`. |
| `IsZero` | Numerator is zero. |
| `IsInteger` / `IsWhole` | Canonical denominator is one. `IsWhole` is an alias for `IsInteger`. |
| `IsProper` | Magnitude strictly less than one (`|numerator| < denominator`). |
| `IsImproper` | Magnitude at least one — the negation of `IsProper`. |
| `IsUnit` | Numerator magnitude is one (a unit fraction such as `1/7` or `-1/7`). |
| `IsNegative` / `IsPositive` | Sign classification; both are `false` for zero. |
| `IsEvenInteger` / `IsOddInteger` | `true` only when the value is an integer *and* the numerator has the stated parity. A non-integer is neither even nor odd. |
| `IsCanonical` | Always `true` — the type maintains the invariant. |
| `IsReducible` | Always `false` — there is no unreduced form to reduce. |

`Reduce()` returns the value unchanged for the same reason `IsReducible` is `false`: reduction already happened at construction. It exists so generic code that expects a `Reduce` step compiles and behaves correctly.

## Static constants

```csharp
Fraction<int>.Zero;       // 0/1
Fraction<int>.One;        // 1/1
Fraction<int>.MinusOne;   // -1/1 — throws OverflowException if T is unsigned
Fraction<int>.MinValue;   // T.MinValue/1
Fraction<int>.MaxValue;   // T.MaxValue/1
```

`MinValue` / `MaxValue` throw `NotSupportedException` for unbounded
backing types like `BigInteger`.

## Arithmetic

The full operator set is supported with cross-multiplication semantics
and `BigInteger` intermediates:

```csharp
var a = Fraction<int>.Create(1, 3);
var b = Fraction<int>.Create(1, 2);

a + b;     // 5/6
a - b;     // -1/6
a * b;     // 1/6
a / b;     // 2/3
a % b;     // 1/3   — remainder of the floored quotient
-a;        // -1/3
++a;       // 4/3
```

Convenience methods cover the common patterns:

```csharp
a.Abs();                       // magnitude
a.Negate();                    // unary negation
a.Reciprocal();                // 3/1 — throws DivideByZeroException on 0
a.Invert();                    // alias for Reciprocal()
a.Pow(3);                      // 1/27 — negative exponents allowed via reciprocal
a.Squared();                   // 1/9 — alias for a * a
a.Cubed();                     // 1/27 — alias for a * a * a
a.Remainder(b);                // alias for a % b
```

`Pow` is defined for the whole `int` exponent range:

- `Pow(0)` returns `One` for every value, **including** `Fraction<T>.Zero` (the conventional `0⁰ = 1`).
- A negative exponent raises the *reciprocal* to the corresponding magnitude, so `Fraction<int>.Create(2, 3).Pow(-2)` is `9/4`. Applying a negative exponent to zero throws <xref:System.DivideByZeroException>, and an exponent magnitude exceeding `int.MaxValue` throws <xref:System.OverflowException>.

The `%` operator (and its `Remainder` alias) returns the remainder of the *floored-quotient* division and carries the **sign of the dividend** — so `Fraction<int>.Create(7, 2) % Fraction<int>.Create(1, 1)` is `1/2`. Dividing by zero throws <xref:System.DivideByZeroException>.

### Overflow handling

Arithmetic operations promote operands to `BigInteger`, evaluate
exactly, then narrow back to `T`. Overflow on narrowing raises
`OverflowException`:

```csharp
var huge = Fraction<int>.Create(int.MaxValue, 1);
var doubled = huge + huge;     // OverflowException
```

Switch the backing type to `BigInteger` to eliminate narrowing
entirely:

```csharp
var hugeBI = Fraction<BigInteger>.Create(int.MaxValue, 1);
var doubledBI = hugeBI + hugeBI;   // 4294967294/1 — no overflow
```

### Unsigned backing types

`Fraction<T>` accepts unsigned backing types (`uint`, `ulong`, `byte`,
…) but negative values cannot be represented. Any operation that
would produce a negative numerator on an unsigned backing type throws
`OverflowException` at runtime — including `MinusOne`, unary `-`,
`Negate()`, the reciprocal of a value larger than one, and certain
subtraction patterns.

## Comparison and equality

```csharp
var a = Fraction<int>.Create(1, 3);
var b = Fraction<int>.Create(2, 5);

a < b;                                      // True
a.CompareTo(b);                             // -1
Fraction<int>.Compare(a, b);                // -1 — static cross-multiply

Fraction<int>.Min(a, b);                    // 1/3
Fraction<int>.Max(a, b);                    // 2/5
Fraction<int>.Clamp(value, lo: a, hi: b);   // clamps to [1/3, 2/5]
```

Equality compares canonical components, not raw structural fields —
because the canonical form is unique, two fractions are equal exactly
when they represent the same rational value. The hash code is derived
from the same canonical components, so equal fractions share a hash
code.

## Conversion

```csharp
// Implicit lift from T.
Fraction<int> three = 3;

// Explicit narrowing conversions to numeric types.
decimal d = (decimal) Fraction<int>.Create(1, 4);   // 0.25m — throws on decimal overflow
double  x = (double)  Fraction<int>.Create(1, 3);   // 0.3333333333333333
float   f = (float)   Fraction<int>.Create(1, 3);   // 0.33333334

// Try-variants for the narrowing direction.
Fraction<long>.Create(very_large, 1).TryToDecimal(out decimal v);

// Truncated integer extraction.
Fraction<int>.Create(7, 3).ToInteger();             // 2 — truncates toward zero
Fraction<int>.Create(-7, 3).ToBigInteger();         // -2

// Cross-backing-type conversion.
Fraction<BigInteger> bigHalf = Fraction<int>.Create(1, 2).As<BigInteger>();
```

`As<TOther>()` rejects values whose canonical components do not fit
in `TOther` with `OverflowException`.

The conversion surface divides cleanly into *exact* and *approximate* directions:

| Conversion | Direction | Exactness | Failure mode |
|---|---|---|---|
| `FromDecimal` / `(Fraction<T>)decimal` | in | exact (mantissa × 10⁻ˢᶜᵃˡᵉ) | `OverflowException` if the canonical components exceed `T` |
| `FromDouble` / `(Fraction<T>)double` | in | exact in the IEEE-754 sense (mantissa × 2ᵉˣᵖ) | `ArgumentException` on non-finite input; `OverflowException` on narrowing |
| `ToDecimal` / `(decimal)` | out | rounded to `decimal` precision | `OverflowException` outside `decimal` range |
| `ToDouble` / `ToSingle` | out | rounded to `double` / `float` | never throws — `TryToDouble` / `TryToSingle` always return `true` |
| `ToInteger` / `ToBigInteger` / `GetWholePart` | out | truncated **toward zero** | `ToInteger` / `GetWholePart` may overflow `T` for an out-of-range integer part |
| `As<TOther>` | re-backing | exact (same canonical value) | `OverflowException` if a component does not fit `TOther` |

Truncation toward zero is the rule for the integer-extraction members: `Fraction<int>.Create(-7, 3).ToInteger()` is `-2`, not `-3`. Use `Floor()` / `Ceiling()` / `Round()` (below) when you need a different rounding direction.

## Rounding and mixed parts

```csharp
var x = Fraction<int>.Create(7, 3);   // 2.333…

x.Floor();        // 2/1
x.Ceiling();      // 3/1
x.Truncate();     // 2/1
x.Round();        // 2/1 — banker's rounding (to-even)
x.Round(MidpointRounding.AwayFromZero);  // 3/1

x.GetWholePart();      // 2 (T)
x.GetFractionalPart(); // 1/3
var (whole, frac) = x.ToMixedParts();    // (2, 1/3)

var (n, d) = Fraction<int>.Create(7, 3); // (7, 3) — Deconstruct over canonical components
```

The full <xref:System.MidpointRounding> enum is supported on `Round`.

## Generic math

`Fraction<T>` implements `INumber<Fraction<T>>` and
`ISignedNumber<Fraction<T>>`. The standard identities and predicates
are provided:

```csharp
Fraction<int>.AdditiveIdentity;        // 0/1
Fraction<int>.MultiplicativeIdentity;  // 1/1
Fraction<int>.NegativeOne;             // -1/1

Fraction<int>.IsZero(Fraction<int>.Zero);             // True
Fraction<int>.IsInteger(Fraction<int>.Create(4, 2));  // True — canonical is 2/1
Fraction<int>.IsNaN(any);                             // False — Fraction<T> is never NaN
Fraction<int>.IsFinite(any);                          // True
Fraction<int>.IsRealNumber(any);                      // True

Fraction<int>.MaxMagnitude(a, b);
Fraction<int>.MinMagnitude(a, b);
```

This means `Fraction<T>` slots into algorithms written against the
`INumber` abstractions — `Sum`, `Aggregate`, generic linear-algebra
routines — without special-casing.

`MaxMagnitude` / `MinMagnitude` compare absolute values and break a tie
by sign, mirroring the BCL convention: `MaxMagnitude` prefers the
positive operand on a magnitude tie, `MinMagnitude` the negative one.
`Clamp`, `CopySign`, `Max` / `MaxNumber`, and `Min` / `MinNumber` are
all present; because `Fraction<T>` is never `NaN`, the `*Number`
variants behave identically to their plain counterparts.

`Fraction<T>` also participates in generic cross-type conversion via
`TSelf.CreateChecked` / `CreateSaturating` / `CreateTruncating`. Integer
and `decimal` sources convert exactly; other finite sources convert
through their nearest `double`; non-finite sources fail. The checked
path overflows to <xref:System.OverflowException>, the saturating /
truncating paths clamp to `MinValue` / `MaxValue` instead.

```csharp
Fraction<int>.CreateChecked(42);            // 42/1 — exact integer source
Fraction<int>.CreateChecked(0.25m);         // 1/4  — exact decimal source
Fraction<int>.CreateSaturating(1e30);       // MaxValue — clamps instead of throwing
```

The backing type is constrained as `where T : IBinaryInteger<T>`.

## Parsing and formatting

`Fraction<T>` implements `IParsable<T>`, `ISpanParsable<T>`,
`IUtf8SpanParsable<T>`, `IFormattable`, `ISpanFormattable`, and
`IUtf8SpanFormattable`. The accepted input shapes are:

| Input | Parses as |
|---|---|
| `"3"`, `"-5"` | Whole-number fractions: 3/1, -5/1 |
| `"3/4"`, `"-7/2"` | Ratios |
| `"2 1/3"` | Mixed numbers: 7/3 — sign applies to whole result |
| `"½"`, `"⅖"` | Unicode vulgar fractions (18 glyphs — see [Formatting and parsing](formatting-and-parsing.md)) |
| `"2⅜"` | Whole + vulgar fraction: 19/8 |
| `"50%"`, `"3/4%"` | Percentage form — trailing `%` divides denominator by 100 |

Leading / trailing whitespace is trimmed. Numeric components parse
with `NumberStyles.None`, so scientific notation and group separators
are rejected.

```csharp
Fraction<int>.Parse("3/4");                // 3/4
Fraction<int>.Parse("2 1/3");              // 7/3
Fraction<int>.Parse("⅗");                  // 3/5
Fraction<int>.Parse("75%");                // 3/4
Fraction<int>.TryParse("nope", out var _); // false
```

### Format specifiers

| Specifier | Output |
|---|---|
| `null`, `""`, `"G"` | `"numerator/denominator"` or bare integer if denominator is 1 |
| `"M"` | Mixed-number form: `"2 1/3"` |
| `"U"` | Unicode vulgar where a glyph exists (denominator ≤ 16), otherwise mixed |
| `"P"` | Percentage form: scales by 100, re-reduces, renders as `numerator/denominator%` (bare `numerator%` when whole) |

```csharp
var x = Fraction<int>.Create(7, 3);

x.ToString();         // "7/3"
x.ToString("M");      // "2 1/3"
x.ToString("U");      // "2⅓"
x.ToString("P");      // "700/3%" — 7/3 × 100 = 700/3, already in lowest terms
```

The percentage form is a *ratio*, not a mixed number: `Fraction<int>.Create(7, 4).ToString("P")` is `"175%"` (700/4 reduces to 175/1) and `Fraction<int>.Create(3, 4).ToString("P")` is `"75%"`. Any specifier other than `G`/`M`/`U`/`P` (case-insensitive) throws <xref:System.FormatException>.

Helper convenience methods:
`ToUnicodeString(provider)`,
`ToMixedString(provider)` /
`ToMixedNumberString(provider)`,
`ToPercentString(provider)`.

## JSON

`Fraction<T>` carries
`[JsonConverter(typeof(FractionJsonConverterFactory))]`, so
`System.Text.Json` round-trips without registration. The attribute
path defaults to the `Strict` policy, which emits the canonical
**object** form:

```csharp
using System.Text.Json;

string json = JsonSerializer.Serialize(Fraction<int>.Create(3, 4));
// {"numerator":3,"denominator":4}

Fraction<int> r = JsonSerializer.Deserialize<Fraction<int>>(json);
```

Each component is written as a *raw* JSON number, so a
`Fraction<BigInteger>` round-trips at any magnitude without losing
precision through the writer's `Int64` / `decimal` primitives. On read,
a component may be either a JSON number or a numeric string token.

To switch to the compact single-string form `"3/4"`, register the
converters with `AddNumericsJsonConverters(NumericsJsonPolicy.Compact)`
on a `JsonSerializerOptions`; the `Compact` read path delegates to
`Fraction<T>.TryParse(text, CultureInfo.InvariantCulture, …)`. See
[JSON serialization](json-serialization.md) for the full policy table
and failure modes.

Instance helpers `ToJson()` and `Fraction<T>.FromJson(string)`
delegate to `JsonSerializer` under the default (`Strict`) policy.
Equivalent XML helpers `ToXml()` / `FromXml(string)` wrap the
invariant-culture *general* text form in
`<fraction>numerator/denominator</fraction>`.

> [!NOTE]
> `ToJson()` / `FromJson()` use the reflection-based `JsonSerializer`
> and are annotated `RequiresUnreferencedCode` / `RequiresDynamicCode`.
> For trimming or AOT, register the converters via
> `AddNumericsJsonConverters` against a source-generated
> `JsonSerializerContext` instead.

## Equality, hashing, and `Equals(object?)`

`Fraction<T>` is value-equatable via `IEquatable<Fraction<T>>` and
implements `IComparable<Fraction<T>>` / `IComparable`. The static
`==` / `!=` operators delegate to `Equals`; the ordering operators
delegate to `Compare` (cross-multiplication). `Equals(object?)` does
the type-check / dispatch.

All empty / default / canonical-equivalent representations of the
same rational value compare equal. Equal fractions share a hash code.

## When *not* to use `Fraction<T>`

- **Continuous measurements with no need for exact arithmetic.** If
  you are working in physics or graphics where the inputs are already
  approximate `double`s, the rational form adds storage and
  computation cost without giving you anything. Stay with `double`.
- **Tight per-iteration loops with `int`-sized values.** Auto-reduction
  costs a GCD per operation, and the `BigInteger` intermediate is not
  free either. For hot inner loops where overflow cannot happen and
  exactness is not required, plain `int` arithmetic is faster.
- **NaN / infinity semantics.** `Fraction<T>` does not model `NaN` or
  infinity — division by zero throws, non-finite `double` input to
  `FromDouble` throws, and the IEEE 754 propagation rules do not
  apply. If you need NaN-aware arithmetic, stay with `double`.
- **Storage in an unsigned backing type when negative values are
  possible.** `Fraction<uint>` cannot represent `-1/2`; certain
  operations on `Fraction<uint>` values throw at runtime. Either pick
  a signed backing type or model the sign separately.

## See also

- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1)
- [`FractionJsonConverter<T>` API reference](xref:Bodu.Numerics.Serialization.FractionJsonConverter`1)
- [`FractionJsonConverterFactory`](xref:Bodu.Numerics.Serialization.FractionJsonConverterFactory)
- [`Interval<T>` guide](interval.md) — the other `Bodu.Numerics` value type.
- [`Money<TCurrency>` guide](../financial/money.md) — uses `Fraction<BigInteger>` as the precision escape hatch via `ToFraction()` / `FromFraction()` / `MultiplyExact()`.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
