---
title: JSON serialization
---

# JSON serialization

`Bodu.Numerics.Serialization` round-trips <xref:Bodu.Numerics.Fraction`1> and <xref:Bodu.Numerics.Interval`1> through `System.Text.Json`. Both value types ship with `[JsonConverter]` attributes, so they serialize correctly with the default options out of the box — you only need this namespace when you want to select a non-default wire shape across a whole `JsonSerializerOptions` instance.

## Zero-configuration round-trip

Because the converters are attached at the type level, no setup is required:

```csharp
using System.Text.Json;
using Bodu.Numerics;

string json = JsonSerializer.Serialize(Fraction<int>.Create(3, 4));
// → {"numerator":3,"denominator":4}

Fraction<int> value = JsonSerializer.Deserialize<Fraction<int>>(json);
// → 3/4
```

## Choosing a policy

To change the wire shape, register the converters with a <xref:Bodu.Numerics.Serialization.NumericsJsonPolicy> via `AddNumericsJsonConverters`. A converter registered on `JsonSerializerOptions.Converters` takes precedence over the type-level attribute:

```csharp
using Bodu.Numerics.Serialization;

var options = new JsonSerializerOptions()
    .AddNumericsJsonConverters(NumericsJsonPolicy.Compact);
```

| Policy | `Fraction<T>` shape | `Interval<T>` shape | Use for |
|---|---|---|---|
| `Strict` (default) | object `{ "numerator": 3, "denominator": 4 }` | object `{ "lower", "upper", "lowerInclusive", "upperInclusive" }`, or `{ "empty": true }` | Canonical persistence and interchange. |
| `Lenient` | as `Strict`, plus a top-level string is accepted on read | as `Strict`, plus `"min"`/`"max"` aliases and defaulted inclusivity | Spreadsheet / external-feed ingest. |
| `Compact` | string `"3/4"` | ISO 31-11 bracket string `"[1, 5)"`, or `"∅"` for empty | Compact payloads where size matters. |

Under `Strict` and `Lenient`, property names compare case-insensitively, duplicate properties are rejected, and unknown properties are ignored. `Lenient` writes the same shape as `Strict` — it only differs on *read*, where it is an *import* convenience; persist with `Strict` or `Compact`. Compact reads delegate to each type's `TryParse` path under the invariant culture, so payloads are stable regardless of the ambient culture.

## Worked example — each policy

```csharp
var frac = Fraction<int>.Create(8, 15);

// Strict — object form (the default, no registration needed):
JsonSerializer.Serialize(frac);
// → {"numerator":8,"denominator":15}

// Compact — string form:
var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);
JsonSerializer.Serialize(frac, compact);                          // → "8/15"

var window = Interval<int>.ClosedOpen(1, 5);
JsonSerializer.Serialize(window);
// → {"lower":1,"upper":5,"lowerInclusive":true,"upperInclusive":false}
JsonSerializer.Serialize(window, compact);                        // → "[1, 5)"

JsonSerializer.Serialize(Interval<int>.Empty);                    // → {"empty":true}
JsonSerializer.Serialize(Interval<int>.Empty, compact);
// → the empty-set glyph "∅" (non-ASCII characters are escaped per the options' encoder)
```

## Registration options

Three registration surfaces exist; pick the narrowest one that covers your need:

| Surface | Scope | Policy selection |
|---|---|---|
| Type-level `[JsonConverter]` attribute (ships on `Fraction<T>` and `Interval<T>`) | Every serialization that has no overriding registration | Always `Strict` — the attribute instantiates <xref:Bodu.Numerics.Serialization.FractionJsonConverterFactory> / <xref:Bodu.Numerics.Serialization.IntervalJsonConverterFactory> through their parameterless constructors. |
| `options.AddNumericsJsonConverters(policy)` | Everything serialized with that `JsonSerializerOptions` | Any policy; registers a coherent pair of factories for both types. |
| Manual `options.Converters.Add(...)` of a factory or closed converter | Whatever you add — e.g. only fractions, or only one backing type | Any policy, per instance: `new FractionJsonConverterFactory(NumericsJsonPolicy.Compact)` covers every `Fraction<T>`; `new FractionJsonConverter<int>(NumericsJsonPolicy.Compact)` covers `Fraction<int>` only. |

`AddNumericsJsonConverters` returns the same `JsonSerializerOptions` instance for inline chaining. It throws `ArgumentNullException` for a null options instance, `ArgumentOutOfRangeException` for an undefined policy value, and `InvalidOperationException` when the options instance has already been used for (de)serialization and its `Converters` collection has become read-only — configure options before first use.

## How the converters resolve the generic parameter

`Fraction<T>` and `Interval<T>` are open generics, so the registered entries are *factories* (<xref:Bodu.Numerics.Serialization.FractionJsonConverterFactory>, <xref:Bodu.Numerics.Serialization.IntervalJsonConverterFactory>) that bind the concrete `T` per request and produce the matching <xref:Bodu.Numerics.Serialization.FractionJsonConverter`1> / <xref:Bodu.Numerics.Serialization.IntervalJsonConverter`1>. You never instantiate the closed converters directly — register the factory (or rely on the type-level attribute) and serialize as normal.

## Custom backing types — `Fraction<BigInteger>` without precision loss

The object-form converter writes each component as a *raw* JSON number — not through the writer's `Int64` / `decimal` primitives — so a `BigInteger`-backed fraction round-trips at any magnitude:

```csharp
using System.Numerics;

var precise = Fraction<BigInteger>.Create(
    BigInteger.Parse("123456789012345678901234567890"), 7);

string json = JsonSerializer.Serialize(precise);
// → {"numerator":123456789012345678901234567890,"denominator":7}

Fraction<BigInteger> back = JsonSerializer.Deserialize<Fraction<BigInteger>>(json);
// → exact round-trip; nothing was truncated through long or decimal
```

On read, each component accepts either a JSON number or a numeric *string* token, parsed as `T` under the invariant culture — so systems that cannot carry arbitrary-precision JSON numbers can quote them instead:

```csharp
JsonSerializer.Deserialize<Fraction<BigInteger>>(
    """{ "numerator": "123456789012345678901234567890", "denominator": "7" }""");
// → same value as above
```

The same number-or-string tolerance applies to `Interval<T>` endpoint values under `Strict` and `Lenient`.

## Failure modes

Malformed payloads surface as `JsonException` on read; the converters never silently coerce:

| Input | Policy | Result |
|---|---|---|
| Token is not an object (e.g. a bare string under `Strict`) | `Strict` | `JsonException` — object form expected. (`Lenient` routes a top-level string through the compact parser instead.) |
| Missing `"numerator"` / `"denominator"` (fraction) or `"lower"` / `"upper"` (interval) | `Strict`, `Lenient` | `JsonException` naming the missing property. |
| Duplicate property (e.g. `"numerator"` twice) | `Strict`, `Lenient` | `JsonException` — duplicates are rejected, never last-wins. |
| `"denominator": 0` | `Strict`, `Lenient` | `JsonException` — a zero denominator is invalid on the wire. |
| Missing `"lowerInclusive"` / `"upperInclusive"` | `Strict` | `JsonException`; `Lenient` defaults the missing flags to closed. |
| `{ "empty": true }` carrying extra endpoint properties | `Strict`, `Lenient` | `JsonException` — the empty form must stand alone. |
| Component value that is neither a number nor a parseable numeric string | all | `JsonException` reporting the type mismatch. |
| Compact token that is not a string, or a string `TryParse` rejects (e.g. `"3/"`, `"[1, )"`) | `Compact` | `JsonException` carrying the offending text. |

Unknown properties are *ignored* (skipped), matching the BCL convention for forward compatibility.

## See also

- [Working with `Fraction<T>`](fraction.md) — the rational type being serialized.
- [Working with `Interval<T>`](interval.md) — the interval type and its bracket notation.
- [Formatting and parsing `Fraction<T>`](formatting-and-parsing.md) — the text forms the `Compact` policy delegates to.
- [Bodu.Numerics guides](index.md) — the member overview for this package.
- [Numerics & Financial topic guides](../topics/numerics-and-financial.md) — every guide in the topic.
- [Numerics & Financial topic overview](../../docs/topics/numerics-and-financial.md) — package boundaries and the decision table.
- [`NumericsJsonPolicy`](xref:Bodu.Numerics.Serialization.NumericsJsonPolicy) · [`FractionJsonConverterFactory`](xref:Bodu.Numerics.Serialization.FractionJsonConverterFactory) · [`IntervalJsonConverterFactory`](xref:Bodu.Numerics.Serialization.IntervalJsonConverterFactory)
- [Bodu.Numerics.Serialization API reference](xref:Bodu.Numerics.Serialization) — full namespace overview.
