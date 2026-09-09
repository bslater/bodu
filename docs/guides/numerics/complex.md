---
title: Working with Complex<T>
---

# Working with `Complex<T>`

<xref:Bodu.Numerics.Complex`1> is an immutable complex number — a real and an imaginary component — generic over any IEEE 754 floating-point component type. It is the generic counterpart of <xref:System.Numerics.Complex>, which is fixed to `double`: `Complex<double>` reproduces the BCL type's arithmetic and elementary functions, while `Complex<float>` and `Complex<Half>` give the same surface at half and quarter the storage.

The type parameter is constrained to <xref:System.Numerics.IFloatingPointIeee754`1>, so `float`, `double`, `Half`, and any conforming user-defined type qualify. `Complex<T>` itself implements <xref:System.Numerics.INumberBase`1> and <xref:System.Numerics.ISignedNumber`1> — not `INumber<T>`, because complex numbers have no total order — together with the `IEquatable`, formatting, and parsing interfaces (string, `char` span, and UTF-8) that back the members shown below.

Every value in the comments of this guide was produced by running the sample; the function table in [Pattern 4](#pattern-4--elementary-functions-pinned-against-systemnumericscomplex) was pinned against `System.Numerics.Complex` on the same inputs.

## Pattern 1 — construction

```csharp
using System.Globalization;
using Bodu.Numerics;

var a = new Complex<double>(3, 4);                      // 3 + 4i
var b = Complex<double>.Create(1, -2);                  // 1 - 2i
Complex<double> real = 5.0;                             // implicit lift from T: 5 + 0i
var polar = Complex<double>.FromPolarCoordinates(2, Math.PI / 2);   // ~0 + 2i
var (re, im) = a;                                       // Deconstruct

Console.WriteLine(a);                                   // <3; 4>
Console.WriteLine(b);                                   // <1; -2>
Console.WriteLine(real);                                // <5; 0>
Console.WriteLine(polar.ToString("F3", CultureInfo.InvariantCulture)); // <0.000; 2.000>
Console.WriteLine(Complex<double>.Zero + " " + Complex<double>.One + " " + Complex<double>.ImaginaryOne);   // <0; 0> <1; 0> <0; 1>
Console.WriteLine(Complex<float>.NaN + " " + Complex<double>.Infinity);   // <NaN; NaN> <∞; ∞>
```

| Member | Description |
|---|---|
| `new Complex<T>(real, imaginary)` / `Create(real, imaginary)` | Component constructor and its static twin (useful as a method-group argument). |
| `implicit operator Complex<T>(T)` | Places a real value on the real axis. This is the *only* implicit conversion — a type parameter cannot carry the per-primitive conversions `System.Numerics.Complex` has, so lift `int` or `decimal` inputs to `T` first. |
| `FromPolarCoordinates(magnitude, phase)` | `magnitude · (cos φ + i sin φ)`. |
| `Real`, `Imaginary` | The components. |
| `Deconstruct(out real, out imaginary)` | Tuple-style deconstruction. |
| `Zero`, `One`, `ImaginaryOne`, `NaN`, `Infinity` | Well-known values; `NaN` and `Infinity` set both components. |

## Pattern 2 — arithmetic and operators

```csharp
using Bodu.Numerics;

var a = new Complex<double>(3, 4);
var b = new Complex<double>(1, -2);

Complex<double> sum  = a + b;    // <4; 2>
Complex<double> diff = a - b;    // <2; 6>
Complex<double> prod = a * b;    // <11; -2>
Complex<double> quot = a / b;    // <-1; 2>
Complex<double> neg  = -a;       // <-3; -4>
bool equal = a == new Complex<double>(3, 4);   // true — component-wise equality

var c = a;
c++;                             // <4; 4> — ++ and -- act on the real part

bool sameAsOperator = Complex<double>.Add(a, b) == a + b;   // true — Add/Subtract/Multiply/Divide/Negate mirror the operators
```

Multiplication uses the direct four-multiply form and division uses Smith's algorithm — the same choices as `System.Numerics.Complex` — so results and non-finite behaviour match the BCL bit for bit (see Pattern 4). `Complex<T>` also satisfies the generic-math operator interfaces, so it works in code written against `INumberBase<TSelf>`:

```csharp
using System.Numerics;
using Bodu.Numerics;

static T SumOfSquares<T>(IEnumerable<T> values) where T : INumberBase<T>
{
    T total = T.Zero;
    foreach (T v in values) total += v * v;
    return total;
}

var values = new[] { new Complex<double>(1, 1), new Complex<double>(2, -1) };
Complex<double> result = SumOfSquares(values);   // <5; -2>
```

## Pattern 3 — magnitude, phase, conjugate, reciprocal

```csharp
using Bodu.Numerics;

var a = new Complex<double>(3, 4);

double magnitude = a.Magnitude;                        // 5
double abs       = Complex<double>.Abs(a);             // 5 — same value, static form
double phase     = a.Phase;                            // 0.9272952180016122 (radians, atan2(4, 3))
Complex<double> conj  = Complex<double>.Conjugate(a);  // <3; -4>
Complex<double> recip = Complex<double>.Reciprocal(a); // <0.12; -0.16>
Complex<double> zeroRecip = Complex<double>.Reciprocal(Complex<double>.Zero);   // <0; 0> — mirrors System.Numerics.Complex

bool finite = Complex<double>.IsFinite(a);                        // true
bool nan    = Complex<double>.IsNaN(Complex<double>.NaN);         // true
bool inf    = Complex<double>.IsInfinity(Complex<double>.Infinity);   // true
```

`Magnitude` is computed with a scaled hypotenuse, so it does not overflow for components near `T.MaxValue`. `Reciprocal(Zero)` returning `Zero` rather than infinity is a deliberate BCL-compatibility choice.

## Pattern 4 — elementary functions, pinned against `System.Numerics.Complex`

`Complex{T}.Functions.cs` supplies `Sqrt`, `Exp`, `Log`, `Log(value, baseValue)`, `Log10`, `Pow(Complex)`, `Pow(T)`, the six trigonometric and hyperbolic functions, and the three inverse trigonometric functions. The sample below evaluates each on `0.5 − 1.25i` with both types and reports whether the `double` results are bit-identical:

```csharp
using Bodu.Numerics;
using SysComplex = System.Numerics.Complex;

var z = new Complex<double>(0.5, -1.25);
var s = new SysComplex(0.5, -1.25);

void Pin(string name, Complex<double> ours, SysComplex theirs)
{
    bool same = ours.Real == theirs.Real && ours.Imaginary == theirs.Imaginary;
    double err = Math.Max(Math.Abs(ours.Real - theirs.Real), Math.Abs(ours.Imaginary - theirs.Imaginary));
    Console.WriteLine($"{name,-8} {ours,-44} {(same ? "bit-identical" : $"max |diff| = {err:E1}")}");
}

Pin("Sqrt", Complex<double>.Sqrt(z), SysComplex.Sqrt(s));
Pin("Exp", Complex<double>.Exp(z), SysComplex.Exp(s));
Pin("Log", Complex<double>.Log(z), SysComplex.Log(s));
Pin("Pow", Complex<double>.Pow(z, new Complex<double>(2, 1)), SysComplex.Pow(s, new SysComplex(2, 1)));
Pin("Sin", Complex<double>.Sin(z), SysComplex.Sin(s));
Pin("Acos", Complex<double>.Acos(z), SysComplex.Acos(s));
```

| Function | `Complex<double>` result on `0.5 − 1.25i` | Versus `System.Numerics.Complex` |
|---|---|---|
| `Sqrt` | `<0.9608046632337985; -0.6504964265019547>` | bit-identical |
| `Exp` | `<0.5198786860084937; -1.5646111274988195>` | bit-identical |
| `Log` | `<0.2973535538733465; -1.1902899496825317>` | ≤ 1.7 × 10⁻¹⁶ |
| `Log10` | `<0.1291390076215157; -0.5169363570120227>` | ≤ 2.1 × 10⁻¹⁵ |
| `Log(z, 2)` | `<0.4289904975637862; -1.7172254076269622>` | ≤ 2.2 × 10⁻¹⁶ |
| `Pow(z, 2+i)` | `<-2.9219531216676917; -5.194090304070759>` | ≤ 2.7 × 10⁻¹⁵ |
| `Pow(z, 3.0)` | `<-2.218750000000001; 1.0156249999999998>` | ≤ 8.9 × 10⁻¹⁶ |
| `Sin`, `Cos`, `Tan` | `<0.9053586344209573; -1.4058162504314684>`, … | bit-identical |
| `Sinh`, `Cosh`, `Tanh` | `<0.16431300276137265; -1.0700996973668526>`, … | bit-identical |
| `Asin`, `Acos` | `<0.3079813715721185; -1.0855765577207788>`, `<1.262814955222778; 1.0855765577207788>` | ≤ 2.2 × 10⁻¹⁶ |
| `Atan` | `<1.1265564408348223; -0.708303336014054>` | bit-identical |
| `Sqrt(−1)` | `<0; 1>` | bit-identical |
| `Reciprocal`, `*`, `/`, `FromPolarCoordinates` | — | bit-identical |

The logarithm and power family differ from the BCL by a few units in the last place because they are evaluated through the generic `T.Log` / `T.Atan2` members rather than the `double`-specialized intrinsics the BCL calls; the arithmetic, square root, trigonometric, and hyperbolic functions reproduce the BCL exactly.

## Pattern 5 — parsing and formatting

The canonical text form is `<real; imaginary>` — the same bracketed, semicolon-separated shape `System.Numerics.Complex.ToString()` produces. The separator is `;` regardless of culture so that a decimal comma (`de-DE`, `fr-FR`) cannot collide with it; the components themselves honour the supplied format and culture.

```csharp
using System.Globalization;
using System.Text;
using Bodu.Numerics;

var z = new Complex<double>(3.5, -4.25);

string s1 = z.ToString();                                         // "<3.5; -4.25>"
string s2 = z.ToString("F1");                                     // "<3.5; -4.3>"
string s3 = z.ToString("E2", CultureInfo.InvariantCulture);       // "<3.50E+000; -4.25E+000>"
string s4 = z.ToString(null, CultureInfo.GetCultureInfo("de-DE"));// "<3,5; -4,25>" — separator stays ';'

Complex<double> p1 = Complex<double>.Parse("<3.5; -4.25>");                                 // <3.5; -4.25>
Complex<double> p2 = Complex<double>.Parse("<3,5; -4,25>", CultureInfo.GetCultureInfo("de-DE"));
Complex<double> p3 = Complex<double>.Parse("7");                                            // <7; 0> — a bare real is accepted
bool notAccepted   = Complex<double>.TryParse("3+4i", out _);                               // false — a+bi notation is not accepted
bool ok            = Complex<double>.TryParse("<1; 2>", CultureInfo.InvariantCulture, out var p4);   // true

Span<char> chars = stackalloc char[32];
if (z.TryFormat(chars, out int written, "F2", CultureInfo.InvariantCulture))
    Console.WriteLine(chars[..written].ToString());                                         // <3.50; -4.25>

Span<byte> utf8 = stackalloc byte[32];
if (z.TryFormat(utf8, out int bytes, default, CultureInfo.InvariantCulture))
    Console.WriteLine(Encoding.UTF8.GetString(utf8[..bytes]));                              // <3.5; -4.25>

Complex<double> fromUtf8 = Complex<double>.Parse("<1; 2>"u8, CultureInfo.InvariantCulture); // UTF-8 parse
```

Accepted input forms are exactly two: the bracketed `<real; imaginary>` pair, and a bare real number (placed on the real axis). Each component is parsed with `T.Parse` under the supplied provider (current culture when `null`), so anything `T` accepts — exponents, `NaN`, `∞`, `-Infinity` — round-trips through the same text.

| Member | Overloads |
|---|---|
| `ToString()` / `ToString(format)` / `ToString(format, provider)` | Format and provider apply to each component. |
| `TryFormat(Span<char>, out charsWritten, format, provider)` | `ISpanFormattable`. |
| `TryFormat(Span<byte>, out bytesWritten, format, provider)` | `IUtf8SpanFormattable`. |
| `Parse(string)` / `Parse(string, provider)` / `Parse(ReadOnlySpan<char>, provider)` / `Parse(ReadOnlySpan<byte>, provider)` | Throw `FormatException` on malformed text. |
| `TryParse(string?, out result)` / `TryParse(string?, provider, out result)` / span and UTF-8 forms | Non-throwing. |

## Pattern 6 — `float` and `Half` components

```csharp
using Bodu.Numerics;

var f = new Complex<float>(1.5f, 2.5f);
var h = new Complex<Half>((Half)0.25, (Half)(-1));

Complex<float> f2 = f * f;                                   // <-4; 7.5>
Complex<Half>  h2 = h + h;                                   // <0.5; -2>
Complex<float> root = Complex<float>.Sqrt(new Complex<float>(-4, 0));   // <0; 2>
string magnitudeType = f.Magnitude.GetType().Name;           // "Single" — Magnitude, Phase, and Abs return T
```

`Magnitude`, `Phase`, and `Abs` return `T`, so a `Complex<Half>` never silently widens to `double`. Choose `Half` only for storage-bound workloads: its functions run in `Half` arithmetic and inherit its ~3-decimal-digit precision.

## Pattern 7 — JSON

`Bodu.Numerics.Serialization.Json` ships <xref:Bodu.Numerics.Serialization.Json.ComplexJsonConverter`1> and the open-generic <xref:Bodu.Numerics.Serialization.Json.ComplexJsonConverterFactory>, registered by `AddNumericsJsonConverters()` alongside the other numeric converters:

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

var strict  = new JsonSerializerOptions().AddNumericsJsonConverters();
var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);
var z = new Complex<double>(3, 4);

string obj = JsonSerializer.Serialize(z, strict);                    // {"real":3,"imaginary":4}
string str = JsonSerializer.Serialize(z, compact);                   // "<3; 4>" — the default encoder escapes '<' and '>'
string nan = JsonSerializer.Serialize(Complex<double>.NaN, strict);  // {"real":"NaN","imaginary":"NaN"}

Complex<double> back  = JsonSerializer.Deserialize<Complex<double>>("{\"real\":3,\"imaginary\":4}", strict);   // <3; 4>
Complex<float>  small = JsonSerializer.Deserialize<Complex<float>>("\"<1.5; -2>\"", compact);                  // <1.5; -2>
```

Under `Strict` and `Lenient` the wire shape is the object `{ "real", "imaginary" }` with finite components as JSON numbers and non-finite ones as the strings `"NaN"`, `"Infinity"`, `"-Infinity"`; under `Compact` it is the `<real; imaginary>` string, parsed and formatted under the invariant culture. `System.Text.Json`'s default encoder writes `<` and `>` as `<` / `>` — harmless for round-tripping, but pass <xref:System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping> if the payload should read as `"<3; 4>"`. The policies and registration are described in full in [JSON serialization](json-serialization.md).

## Relationship to `System.Numerics.Complex`

| Aspect | `System.Numerics.Complex` | `Complex<T>` |
|---|---|---|
| Component type | `double` only | any `IFloatingPointIeee754<T>` |
| Implicit conversions | from every built-in numeric type | from `T` only |
| Arithmetic and non-finite rules | four-multiply product, Smith division, `Reciprocal(0) == 0` | identical |
| Text form | `<real; imaginary>` | identical, plus a bare-real convenience on parse, and UTF-8 parse/format |
| Generic-math interfaces | `INumberBase<Complex>`, `ISignedNumber<Complex>` | `INumberBase<Complex<T>>`, `ISignedNumber<Complex<T>>` |
| `Magnitude` / `Phase` type | `double` | `T` |
| JSON | none built in | `ComplexJsonConverter<T>` via `AddNumericsJsonConverters` |

There is no built-in conversion between the two types; when interoperating with an API that takes `System.Numerics.Complex`, construct it from `Real` and `Imaginary` (both `double` for `Complex<double>`).

## When not to use it

- **You only ever need `double`.** `System.Numerics.Complex` is the same algorithms with a wider set of implicit conversions; `Complex<double>` earns its keep when the surrounding code is already generic over `T` or when JSON support matters.
- **Ordering or comparison.** There is no `<` on complex numbers and `Complex<T>` does not pretend otherwise; compare `Magnitude` explicitly.
- **Exact arithmetic.** Components are floating point; for exact rational values use [`Fraction<T>`](fraction.md).

## See also

- [`Complex<T>` API reference](xref:Bodu.Numerics.Complex`1)
- [`ComplexJsonConverter<T>` API reference](xref:Bodu.Numerics.Serialization.Json.ComplexJsonConverter`1) · [`ComplexJsonConverterFactory`](xref:Bodu.Numerics.Serialization.Json.ComplexJsonConverterFactory)
- [JSON serialization](json-serialization.md) — every policy and wire shape across the numeric types.
- [Generic math constraints](generic-math-constraints.md) — writing code over `INumberBase<T>` that accepts `Complex<T>`.
- [`Fraction<T>` guide](fraction.md) — the exact-rational sibling.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
