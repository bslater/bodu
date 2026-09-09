---
title: Bodu.Numerics.Serialization.Json — Core concepts
---

# Bodu.Numerics.Serialization.Json — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [numerics JSON guide](../../guides/numerics/json-serialization.md), and refer back whenever a term feels imprecise.

Part of the **[Numerics & Financial](../topics/numerics-and-financial.md)** topic. For the high-level shape of the package, start with the [introduction](index.md); the numeric vocabulary — canonical fractions, endpoint inclusivity, unbounded sides, discrete intervals, interval sets — lives in the [Bodu.Numerics concepts](../numerics/concepts.md) page.

## Serialization-agnostic core

`Bodu.Numerics` carries no `[JsonConverter]` attribute and no reference to `System.Text.Json`; this package supplies both. **Registration is required**: serializing a `Fraction<int>` with an options instance that has not had `AddNumericsJsonConverters` called on it does not throw — `JsonSerializer` falls back to its reflection-based object shape, which does not round-trip. Configure the options before first use; once an options instance has been used, its `Converters` collection is read-only and `AddNumericsJsonConverters` throws `InvalidOperationException`.

## Policy

A <xref:Bodu.Numerics.Serialization.Json.NumericsJsonPolicy> is passed once — to `AddNumericsJsonConverters`, to a helper, or to a converter's constructor — and fixes the wire shape and parsing strictness for every numeric type serialized with that options instance:

| Policy | Writes | Reads |
|---|---|---|
| `Strict` (`0`, default) | The canonical object and array shapes. | Those shapes only; case-insensitive property names, duplicates rejected, unknown properties ignored. |
| `Lenient` (`1`) | Exactly what `Strict` writes. | Everything `Strict` reads, plus a top-level string routed through the compact parser, `"min"` / `"max"` aliases for `"lower"` / `"upper"`, and missing inclusivity flags defaulted to closed. |
| `Compact` (`2`) | The single-string forms. | The string forms, via each type's `Parse` / `TryParse` under the invariant culture. |

A converter or factory constructed without a policy defaults to `Strict`.

## Factories vs closed converters

`Fraction<T>`, `Interval<T>`, `DiscreteInterval<T>`, `IntervalSet<T>`, and `Complex<T>` are open generics, so `AddNumericsJsonConverters` registers a **factory** for each (`CanConvert` recognizes the closed generic; `CreateConverter` produces the matching closed converter bound to the request's `T`). You never need to instantiate the closed converters — but you can, for a narrower registration: `new FractionJsonConverter<int>(NumericsJsonPolicy.Compact)` covers `Fraction<int>` only, while `new FractionJsonConverterFactory(NumericsJsonPolicy.Compact)` covers every backing type. `BigDecimal` is non-generic and registers as a single <xref:Bodu.Numerics.Serialization.Json.BigDecimalJsonConverter>.

## Fraction shape

```json
{ "numerator": 3, "denominator": 4 }
"3/4"
```

Both components are written as **raw** JSON numbers — not through the writer's `Int64` / `decimal` primitives — so a `BigInteger`-backed fraction round-trips at any magnitude. On read, each component accepts a JSON number or a numeric *string*, parsed as `T` under the invariant culture, so systems that cannot carry arbitrary-precision JSON numbers can quote them. A zero denominator is rejected, and a payload whose canonical reduction overflows `T` is rejected rather than wrapped.

## Interval shape

```json
{ "lower": 1, "upper": 5, "lowerInclusive": true, "upperInclusive": false }
{ "lower": 1, "upperUnbounded": true, "lowerInclusive": true }
{ "empty": true }
"[1, 5)"   "[1, +∞)"   "∅"
```

A bounded side carries its endpoint and inclusivity flag; an unbounded side is a `lowerUnbounded` / `upperUnbounded` marker with no endpoint; the empty interval is the single-property object `{ "empty": true }` and must stand alone. Under `Strict` a bounded side's inclusivity flag is required; `Lenient` defaults a missing flag to closed and accepts `"min"` / `"max"` as endpoint aliases. The compact form is the same ISO 31-11 bracket notation `Interval<T>.ToString()` / `TryParse` use, with `"∅"` for empty (non-ASCII characters are escaped per the options' encoder).

## Discrete interval and interval set

<xref:Bodu.Numerics.DiscreteInterval`1> serializes **through the `Interval<T>` shape** over its canonical closed integer bounds — the integer domain has no open sides to express — so its compact form is always `"[1, 5]"`. <xref:Bodu.Numerics.IntervalSet`1> serializes as a JSON **array** of its `Interval<T>` pieces (`[]` when empty), each piece in the selected policy's shape; a non-array token is rejected.

## `BigDecimal` shape

```json
{ "unscaledValue": 12340, "scale": 3 }
"12.340"
```

`Strict` writes the exact pair — an arbitrary-magnitude unscaled mantissa as a raw JSON number plus the decimal scale — and `Compact` writes the plain decimal string. `Lenient` reads either. The string form is used for `Compact`, rather than a bare JSON number, because many consumers narrow long numbers to IEEE-754 `double`.

## `Complex` shape

```json
{ "real": 3, "imaginary": 4 }
{ "real": "NaN", "imaginary": "-Infinity" }
"<3; 4>"
```

A finite component is a JSON number; a non-finite one is the string `"NaN"`, `"Infinity"`, or `"-Infinity"`, and either form is accepted on read. The compact form delegates to `Complex<T>.ToString` / `TryParse` under the invariant culture.

## Convenience helpers

<xref:Bodu.Numerics.Serialization.Json.FractionJsonExtensions> offers `value.ToJson(policy)` and `FractionJsonExtensions.FromJson<T>(json, policy)`, each configuring a fresh `JsonSerializerOptions` per call. They exist for one-off calls; for repeated serialization build one options instance with `AddNumericsJsonConverters` and reuse it.

## Trimming and AOT

The converters are reflection-free at the value level, and the package is marked AOT-compatible. `AddNumericsJsonConverters` registers the factory set; point a source-generated `JsonSerializerContext` at your DTOs so the trimmer can see the closed types. The `ToJson` / `FromJson` helpers call the reflection-based `JsonSerializer` and are annotated `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`, so using them in a trimmed or native-AOT application produces the standard analyzer warnings — prefer the options-based path there.

## Failure modes

Every malformed payload surfaces as `JsonException`; the converters never coerce silently:

| Input | Result |
|---|---|
| A token that is not an object (`Fraction`, `Interval`, `BigDecimal`, `Complex`) under `Strict` | `JsonException` — object form expected. `Lenient` routes a top-level string through the compact parser instead. |
| A token that is not a string under `Compact` | `JsonException` — compact string expected. |
| A non-array token for `IntervalSet<T>` | `JsonException` — an interval set is a JSON array of pieces. |
| Missing `"numerator"` / `"denominator"`, a bounded side's `"lower"` / `"upper"`, or a `BigDecimal` / `Complex` property | `JsonException` naming the missing property. |
| Missing `"lowerInclusive"` / `"upperInclusive"` on a bounded side | `JsonException` under `Strict`; `Lenient` defaults to closed. |
| A duplicate property | `JsonException` — duplicates are rejected, never last-wins. |
| `"denominator": 0` | `JsonException`. |
| A fraction whose canonical reduction overflows `T` | `JsonException`. |
| `{ "empty": true }` carrying endpoint or unbounded properties | `JsonException` — the empty form must stand alone. |
| A component that is neither a number nor a parseable numeric string; an inclusivity flag that is not a boolean | `JsonException` reporting the type mismatch. |
| A compact string `TryParse` rejects (`"3/"`, `"[1, )"`, `"<3>"`) | `JsonException` carrying the offending text. |

Unknown properties are *ignored* (skipped), matching the BCL convention for forward compatibility.

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples for every concept above.
- **[Introduction](index.md)** — the converter table and scenario index.
- **[Numerics JSON serialization guide](../../guides/numerics/json-serialization.md)** — the full wire-format reference with worked examples per policy.
- **[Bodu.Numerics concepts](../numerics/concepts.md)** — the numeric vocabulary.
- **[Bodu.Numerics.Serialization.Json API reference](xref:Bodu.Numerics.Serialization.Json)** — full type-by-type docs.
