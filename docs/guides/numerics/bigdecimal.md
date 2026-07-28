---
title: Working with BigDecimal
---

# Working with `BigDecimal`

`BigDecimal` is an immutable, value-equatable arbitrary-precision
decimal number. Unlike `System.Decimal` — which is a fixed 128-bit
type capped at 28–29 significant digits and an exponent range of
10<sup>0</sup> to 10<sup>-28</sup> — `BigDecimal` grows to whatever
precision a value requires. It stores an arbitrary-magnitude
`BigInteger` *unscaled value* together with a non-negative `int`
*scale*, so the represented number is:

```text
value = unscaledValue × 10^-scale
```

For example `12.340` is `(unscaledValue: 12340, scale: 3)`. There is
no `MinValue` or `MaxValue`: like `BigInteger`, the type is unbounded
and arithmetic never overflows.

`BigDecimal` implements the full `INumber<BigDecimal>` /
`ISignedNumber<BigDecimal>` generic-math surface, so it composes with
`INumber<T>`-constrained algorithms without a bespoke wrapper.

## When to reach for `BigDecimal`

| Need | Type |
|---|---|
| Exact fractions such as `1/3`, continued fractions, rational approximation | `Fraction<T>` |
| Fixed-scale money within the `decimal` range (≤ 28 digits) | `System.Decimal` |
| Decimal values that exceed `decimal`'s precision or exponent range, or that must preserve trailing-zero *scale* | `BigDecimal` |

`BigDecimal` preserves scale: `1.0`, `1.00`, and `1` are **equal in
value** (they compare and hash equal) but retain their own scale for
formatting until you normalize or rescale.

## Creating values

```csharp
using System.Numerics;
using Bodu.Numerics;

// From an unscaled value and scale directly.
BigDecimal a = new BigDecimal(new BigInteger(12340), 3);  // 12.340

// Implicit lifts from the exact integer and decimal types.
BigDecimal fromInt     = 42;
BigDecimal fromLong    = 9_000_000_000L;
BigDecimal fromBigInt  = (BigInteger)1 << 200;   // exact, far beyond decimal
BigDecimal fromDecimal = 0.25m;                  // exact 0.25

// Parsing accepts plain and scientific decimal text.
BigDecimal parsed = BigDecimal.Parse("123456789012345678901234567890.123456789",
    CultureInfo.InvariantCulture);
BigDecimal sci    = BigDecimal.Parse("6.022e23", CultureInfo.InvariantCulture);

// Named constants.
BigDecimal zero = BigDecimal.Zero;
BigDecimal one  = BigDecimal.One;
BigDecimal ten  = BigDecimal.Ten;
```

`double` does **not** lift implicitly, because the nearest `double`
to a written decimal is rarely that decimal. Convert deliberately:

```csharp
BigDecimal exactBinary = (BigDecimal)0.1;              // the exact IEEE-754 value of 0.1d
BigDecimal shortest    = BigDecimal.FromDouble(0.1);   // same value; explicit intent
```

## Inspecting a value

```csharp
BigDecimal v = BigDecimal.Parse("12.340", CultureInfo.InvariantCulture);

BigInteger unscaled = v.UnscaledValue;   // 12340
int scale           = v.Scale;           // 3
int precision       = v.Precision;       // 5  — total significant digits
int sign            = v.Sign;            // 1  (-1, 0, or 1)

bool isZero     = v.IsZero;
bool isInteger  = v.IsInteger;           // false — has a fractional part
bool isNegative = v.IsNegative;
bool isPositive = v.IsPositive;

// Deconstruct into (unscaledValue, scale).
var (mantissa, s) = v;
```

## Arithmetic

Addition, subtraction, and multiplication are **exact** — the result
carries whatever scale is needed to represent it with no loss:

```csharp
BigDecimal sum  = BigDecimal.Add(0.1m, 0.2m);         // 0.3 exactly
BigDecimal diff = BigDecimal.Subtract(5m, 0.001m);    // 4.999
BigDecimal prod = BigDecimal.Multiply(1.5m, 1.5m);    // 2.25

BigDecimal neg  = BigDecimal.Negate(sum);
BigDecimal abs  = BigDecimal.Abs(neg);
BigDecimal pow  = BigDecimal.Pow(2m, 10);             // 1024

// Operators mirror the named methods.
BigDecimal total = (BigDecimal)0.1m + 0.2m - 0.05m;   // 0.25
```

### Division and precision

Decimal division is not generally terminating (`1/3`), so `BigDecimal`
cannot divide exactly and unboundedly at the same time. The contract:

- The `/` operator and the two-argument
  `Divide(BigDecimal, BigDecimal)` compute the quotient to a **default
  working precision** of 50 fractional digits, rounded half-to-even,
  then strip trailing zeros. This is enough for the common case while
  staying deterministic.
- For full control, use
  `Divide(BigDecimal dividend, BigDecimal divisor, int scale, MidpointRounding mode)`
  to fix the result scale and rounding mode explicitly.

```csharp
BigDecimal third = BigDecimal.Divide(1m, 3m);                                   // 0.3333…（50 digits）
BigDecimal cents = BigDecimal.Divide(10m, 3m, scale: 2, MidpointRounding.ToEven); // 3.33
BigDecimal rem   = BigDecimal.Remainder(10m, 3m);                               // 1
```

## Rounding and scale

```csharp
BigDecimal v = BigDecimal.Parse("2.455", CultureInfo.InvariantCulture);

BigDecimal r2 = BigDecimal.Round(v, 2, MidpointRounding.ToEven);  // 2.46
BigDecimal r0 = BigDecimal.Round(v);                              // 2  (default scale 0)

BigDecimal floor    = BigDecimal.Floor(v);       // 2
BigDecimal ceiling  = BigDecimal.Ceiling(v);     // 3
BigDecimal truncate = BigDecimal.Truncate(v);    // 2  (toward zero)
```

## Comparison and equality

Equality and ordering are by **numeric value**, so differing scales do
not matter:

```csharp
BigDecimal a = BigDecimal.Parse("1.0", CultureInfo.InvariantCulture);
BigDecimal b = BigDecimal.Parse("1.00", CultureInfo.InvariantCulture);

bool equal = a == b;                       // true
int order  = BigDecimal.Min(a, b) == a;    // both equal — Min returns either

// GetHashCode() is consistent with value equality: a and b hash equal.
```

## Formatting and parsing

`BigDecimal` implements `IFormattable`, `ISpanFormattable`,
`IUtf8SpanFormattable`, `IParsable`, `ISpanParsable`, and
`IUtf8SpanParsable`. The `G` (general, plain culture-aware decimal)
and `F` (fixed-point) standard specifiers are supported. As with the
rest of the library, an `IFormatProvider` controls only the decimal
separator and sign glyphs; the digit sequence is culture-invariant.

```csharp
BigDecimal v = BigDecimal.Parse("1234.5", CultureInfo.InvariantCulture);

string general = v.ToString();                               // "1234.5"
string fixed2  = v.ToString("F2", CultureInfo.InvariantCulture); // "1234.50"

// Span and UTF-8 surfaces avoid intermediate string allocation.
Span<char> buffer = stackalloc char[32];
v.TryFormat(buffer, out int written, "G", CultureInfo.InvariantCulture);
```

## Generic math

Because `BigDecimal` satisfies `INumber<BigDecimal>`, it flows through
generic-math algorithms and the `CreateChecked` / `CreateTruncating`
conversion factories:

```csharp
static T Sum<T>(params T[] values) where T : INumber<T>
{
    T acc = T.Zero;
    foreach (T value in values)
        acc += value;
    return acc;
}

BigDecimal exact = Sum<BigDecimal>(0.1m, 0.2m, 0.3m);  // 0.6 exactly
BigDecimal five  = BigDecimal.CreateChecked(5);        // via INumberBase<T>
int back         = int.CreateTruncating(five);         // 5
```

The classification predicates (`IsInteger`, `IsEvenInteger`,
`IsNegative`, …) are exposed as static members of `INumberBase<T>` and
are reached through a generic constraint, matching the BCL numeric
types. `Radix` is `10`.

## Conversions

| To | How | Notes |
|---|---|---|
| `BigInteger` | `(BigInteger)value` / `value.ToBigInteger()` | Truncates toward zero. |
| `decimal` | `(decimal)value` / `value.ToDecimal()` | Throws `OverflowException` when the value exceeds `decimal`'s range; `value.TryToDecimal(out var d)` reports `false` instead of throwing. |
| `double` | `(double)value` / `value.ToDouble()` | Nearest `double`; may lose precision, and saturates to `±Infinity` outside the finite range — `value.TryToDouble(out var d)` reports `false` in that case. |
| from `double` | `(BigDecimal)d` / `BigDecimal.FromDouble(d)` | Non-finite input throws. |
| from `decimal` | implicit / `BigDecimal.FromDecimal(d)` | Exact. |

## JSON serialization

Like the rest of `Bodu.Numerics`, the core type carries **no**
`[JsonConverter]` attribute — JSON support ships in the companion
[`Bodu.Numerics.Serialization.Json`](json-serialization.md) package.
Register the converters with a single call and pick a wire shape:

```csharp
using Bodu.Numerics.Serialization.Json;

var options = new JsonSerializerOptions()
    .AddNumericsJsonConverters(NumericsJsonPolicy.Strict);

// Strict — canonical object form:
//   { "unscaledValue": 12340, "scale": 3 }
// Compact — the plain decimal string:
//   "12.340"
string json = JsonSerializer.Serialize(
    BigDecimal.Parse("12.340", CultureInfo.InvariantCulture), options);
```

The Strict object shape writes the unscaled value as a raw JSON number,
so an arbitrary-magnitude mantissa round-trips without precision loss.
The Compact shape writes a string rather than a bare JSON number,
because many JSON consumers silently narrow long numbers to IEEE-754
`double`.

## See also

- [JSON serialization](json-serialization.md) — the `Strict`,
  `Lenient`, and `Compact` wire shapes and how to register them.
- [Working with `Fraction<T>`](fraction.md) — the exact-rational type;
  reach for it when you need `1/3` exactly rather than to a fixed
  number of decimal places.
- [`BigDecimal` API reference](xref:Bodu.Numerics.BigDecimal)
