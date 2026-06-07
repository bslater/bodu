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
Fraction<int> value = JsonSerializer.Deserialize<Fraction<int>>(json);
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

Under `Strict` and `Lenient`, property names compare case-insensitively, duplicate properties are rejected, and unknown properties are ignored. `Lenient` is an *import* convenience — it is not a canonical storage shape; persist with `Strict` or `Compact`.

## Worked example — each policy

```csharp
var frac = Fraction<int>.Create(8, 15);

// Strict — object form
// {"numerator":8,"denominator":15}

// Compact — string form
var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);
string s = JsonSerializer.Serialize(frac, compact);   // "8/15"

var window = Interval<int>.ClosedOpen(1, 5);
string i = JsonSerializer.Serialize(window, compact);  // "[1, 5)"
```

## How the converters resolve the generic parameter

`Fraction<T>` and `Interval<T>` are open generics, so the registered entries are *factories* (<xref:Bodu.Numerics.Serialization.FractionJsonConverterFactory>, <xref:Bodu.Numerics.Serialization.IntervalJsonConverterFactory>) that bind the concrete `T` per request and produce the matching <xref:Bodu.Numerics.Serialization.FractionJsonConverter`1> / <xref:Bodu.Numerics.Serialization.IntervalJsonConverter`1>. You never instantiate the closed converters directly — register the factory (or rely on the type-level attribute) and serialize as normal.

## Where to go next

- [Working with `Fraction<T>`](fraction.md) — the rational type being serialized.
- [Working with `Interval<T>`](interval.md) — the interval type and its bracket notation.
- [Bodu.Numerics.Serialization API reference](xref:Bodu.Numerics.Serialization) — full namespace overview.
