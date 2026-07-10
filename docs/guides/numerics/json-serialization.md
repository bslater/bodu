---
title: JSON serialization
---

# JSON serialization

`Bodu.Numerics.Serialization.Json` is the companion package that round-trips the `Bodu.Numerics` value types through `System.Text.Json`. It covers <xref:Bodu.Numerics.Fraction`1>, <xref:Bodu.Numerics.Interval`1>, <xref:Bodu.Numerics.DiscreteInterval`1>, <xref:Bodu.Numerics.IntervalSet`1>, and <xref:Bodu.Numerics.BigDecimal>.

The core `Bodu.Numerics` library is deliberately **serialization-agnostic** — the value types carry no `[JsonConverter]` attribute and take no dependency on `System.Text.Json`. JSON support is opt-in through this package, so a consumer of just `Fraction<T>` pays nothing for the serializer. Install it alongside the core package:

```shell
dotnet add package Bodu.Numerics.Serialization.Json
```

## Registration

Register the converters once per `JsonSerializerOptions` with `AddNumericsJsonConverters`, then serialize as normal:

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

var options = new JsonSerializerOptions().AddNumericsJsonConverters();

string json = JsonSerializer.Serialize(new Fraction<int>(3, 4), options);
// → {"numerator":3,"denominator":4}

Fraction<int> value = JsonSerializer.Deserialize<Fraction<int>>(json, options);
// → 3/4
```

`AddNumericsJsonConverters` registers a coherent converter set for every numeric type from one policy value. The <xref:Bodu.Numerics.IntervalPair`1> and <xref:Bodu.Numerics.DiscreteIntervalPair`1> result types are transient and are not serializable; call `ToIntervalSet()` and serialize the resulting <xref:Bodu.Numerics.IntervalSet`1> instead.

## Choosing a policy

Pass a <xref:Bodu.Numerics.Serialization.Json.NumericsJsonPolicy> to select the wire shape:

```csharp
var compact = new JsonSerializerOptions()
    .AddNumericsJsonConverters(NumericsJsonPolicy.Compact);
```

| Policy | `Fraction<T>` shape | `Interval<T>` shape | Use for |
|---|---|---|---|
| `Strict` (default) | object `{ "numerator": 3, "denominator": 4 }` | object `{ "lower", "upper", "lowerInclusive", "upperInclusive" }`, `{ "empty": true }`, or the `lowerUnbounded` / `upperUnbounded` markers for infinite sides | Canonical persistence and interchange. |
| `Lenient` | as `Strict`, plus a top-level string is accepted on read | as `Strict`, plus `"min"`/`"max"` aliases and defaulted inclusivity | Spreadsheet / external-feed ingest. |
| `Compact` | string `"3/4"` | ISO 31-11 bracket string `"[1, 5)"`, or `"∅"` for empty | Compact payloads where size matters. |

`DiscreteInterval<T>` serializes through the `Interval<T>` shape over its canonical closed integer bounds (compact form `"[1, 5]"`), and `IntervalSet<T>` serializes as a JSON **array** of its `Interval<T>` pieces (empty set → `[]`). Both honour the selected policy.

`BigDecimal` serializes as the canonical object `{ "unscaledValue": 12340, "scale": 3 }` under `Strict` (the unscaled value is a raw JSON number, so an arbitrary-magnitude mantissa round-trips exactly) and as the plain decimal string `"12.340"` under `Compact`. `Lenient` reads either shape. The string form is used for `Compact` — rather than a bare JSON number — because many consumers narrow long numbers to IEEE-754 `double`.

Under `Strict` and `Lenient`, property names compare case-insensitively, duplicate properties are rejected, and unknown properties are ignored. `Lenient` writes the same shape as `Strict` — it only differs on *read*, where it is an *import* convenience; persist with `Strict` or `Compact`. Compact reads delegate to each type's `TryParse` path under the invariant culture, so payloads are stable regardless of the ambient culture.

## Worked example — each policy

```csharp
var options = new JsonSerializerOptions().AddNumericsJsonConverters();               // Strict
var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);

var frac = new Fraction<int>(8, 15);
JsonSerializer.Serialize(frac, options);                          // → {"numerator":8,"denominator":15}
JsonSerializer.Serialize(frac, compact);                         // → "8/15"

var window = Interval<int>.ClosedOpen(1, 5);
JsonSerializer.Serialize(window, options);
// → {"lower":1,"upper":5,"lowerInclusive":true,"upperInclusive":false}
JsonSerializer.Serialize(window, compact);                       // → "[1, 5)"

JsonSerializer.Serialize(Interval<int>.AtLeast(1), options);
// → {"lower":1,"upperUnbounded":true,"lowerInclusive":true}

JsonSerializer.Serialize(Interval<int>.Empty, options);          // → {"empty":true}
JsonSerializer.Serialize(Interval<int>.Empty, compact);
// → the empty-set glyph "∅" (non-ASCII characters are escaped per the options' encoder)

var set = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(8, 9));
JsonSerializer.Serialize(set, compact);                          // → ["[1, 3]","[8, 9]"]
```

## Registration surfaces

Two surfaces exist; pick the narrowest one that covers your need:

| Surface | Scope | Policy selection |
|---|---|---|
| `options.AddNumericsJsonConverters(policy)` | Everything serialized with that `JsonSerializerOptions` | Any policy; registers a coherent factory set for every numeric type. |
| Manual `options.Converters.Add(...)` of a factory or closed converter | Whatever you add — e.g. only fractions, or only one backing type | Any policy, per instance: `new FractionJsonConverterFactory(NumericsJsonPolicy.Compact)` covers every `Fraction<T>`; `new FractionJsonConverter<int>(NumericsJsonPolicy.Compact)` covers `Fraction<int>` only. |

`AddNumericsJsonConverters` returns the same `JsonSerializerOptions` instance for inline chaining. It throws `ArgumentNullException` for a null options instance, `ArgumentOutOfRangeException` for an undefined policy value, and `InvalidOperationException` when the options instance has already been used for (de)serialization and its `Converters` collection has become read-only — configure options before first use.

## Convenience helpers

<xref:Bodu.Numerics.Serialization.Json.FractionJsonExtensions> offers `ToJson()` / `FromJson<T>(string)` wrappers that configure a fresh options instance per call:

```csharp
using Bodu.Numerics.Serialization.Json;

string text = new Fraction<int>(-7, 8).ToJson();                 // "{"numerator":-7,"denominator":8}"
Fraction<int> back = FractionJsonExtensions.FromJson<int>(text);
```

These helpers call the reflection-based `JsonSerializer` and are annotated `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` — using them in a trimmed or native-AOT app produces the standard analyzer warnings. For repeated serialization, build one `JsonSerializerOptions` with `AddNumericsJsonConverters` and reuse it.

## Trimming and AOT

The converters are reflection-free at the value level: `AddNumericsJsonConverters` registers the factory set, and you point a source-generated `JsonSerializerContext` at your DTO so the trimmer can see the closed types. Prefer that path over the `ToJson()` / `FromJson()` helpers whenever trimming or AOT is in play.

## How the converters resolve the generic parameter

`Fraction<T>`, `Interval<T>`, `DiscreteInterval<T>`, and `IntervalSet<T>` are open generics, so the registered entries are *factories* that bind the concrete `T` per request and produce the matching closed converter. You never instantiate the closed converters directly — register the factory (via `AddNumericsJsonConverters` or by adding it to `Converters`) and serialize as normal. `BigDecimal` is non-generic, so it registers as a single <xref:Bodu.Numerics.Serialization.Json.BigDecimalJsonConverter> rather than a factory.

## Custom backing types — `Fraction<BigInteger>` without precision loss

The object-form converter writes each component as a *raw* JSON number — not through the writer's `Int64` / `decimal` primitives — so a `BigInteger`-backed fraction round-trips at any magnitude:

```csharp
using System.Numerics;

var options = new JsonSerializerOptions().AddNumericsJsonConverters();
var precise = new Fraction<BigInteger>(
    BigInteger.Parse("123456789012345678901234567890"), 7);

string json = JsonSerializer.Serialize(precise, options);
// → {"numerator":123456789012345678901234567890,"denominator":7}

Fraction<BigInteger> back = JsonSerializer.Deserialize<Fraction<BigInteger>>(json, options);
// → exact round-trip; nothing was truncated through long or decimal
```

On read, each component accepts either a JSON number or a numeric *string* token, parsed as `T` under the invariant culture — so systems that cannot carry arbitrary-precision JSON numbers can quote them instead:

```csharp
JsonSerializer.Deserialize<Fraction<BigInteger>>(
    """{ "numerator": "123456789012345678901234567890", "denominator": "7" }""", options);
// → same value as above
```

The same number-or-string tolerance applies to `Interval<T>` endpoint values under `Strict` and `Lenient`.

## Failure modes

Malformed payloads surface as `JsonException` on read; the converters never silently coerce:

| Input | Policy | Result |
|---|---|---|
| Token is not an object (e.g. a bare string under `Strict`) | `Strict` | `JsonException` — object form expected. (`Lenient` routes a top-level string through the compact parser instead.) |
| Missing `"numerator"` / `"denominator"` (fraction) or a bounded side's `"lower"` / `"upper"` (interval) | `Strict`, `Lenient` | `JsonException` naming the missing property. |
| Duplicate property (e.g. `"numerator"` twice) | `Strict`, `Lenient` | `JsonException` — duplicates are rejected, never last-wins. |
| `"denominator": 0` | `Strict`, `Lenient` | `JsonException` — a zero denominator is invalid on the wire. |
| Missing `"lowerInclusive"` / `"upperInclusive"` on a bounded side | `Strict` | `JsonException`; `Lenient` defaults the missing flags to closed. |
| `{ "empty": true }` carrying extra endpoint or unbounded properties | `Strict`, `Lenient` | `JsonException` — the empty form must stand alone. |
| Component value that is neither a number nor a parseable numeric string | all | `JsonException` reporting the type mismatch. |
| Compact token that is not a string, or a string `TryParse` rejects (e.g. `"3/"`, `"[1, )"`) | `Compact` | `JsonException` carrying the offending text. |
| Non-array token for an `IntervalSet<T>` | all | `JsonException` — an interval set is a JSON array of pieces. |

Unknown properties are *ignored* (skipped), matching the BCL convention for forward compatibility.

## See also

- [Working with `Fraction<T>`](fraction.md) — the rational type being serialized.
- [Working with `Interval<T>`](interval.md) — the interval type and its bracket notation.
- [Formatting and parsing `Fraction<T>`](formatting-and-parsing.md) — the text forms the `Compact` policy delegates to.
- [Bodu.Numerics guides](index.md) — the member overview for this package.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.
- [`NumericsJsonPolicy`](xref:Bodu.Numerics.Serialization.Json.NumericsJsonPolicy) · [`FractionJsonConverterFactory`](xref:Bodu.Numerics.Serialization.Json.FractionJsonConverterFactory) · [`IntervalJsonConverterFactory`](xref:Bodu.Numerics.Serialization.Json.IntervalJsonConverterFactory)
- [Bodu.Numerics.Serialization.Json API reference](xref:Bodu.Numerics.Serialization.Json) — full namespace overview.
