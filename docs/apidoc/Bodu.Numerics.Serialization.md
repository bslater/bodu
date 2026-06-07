---
uid: Bodu.Numerics.Serialization
---

# Bodu.Numerics.Serialization

## Purpose

**Bodu.Numerics.Serialization** carries the `System.Text.Json` integration for [`Bodu.Numerics`](Bodu.Numerics.md). It supplies the converters that round-trip <xref:Bodu.Numerics.Fraction`1> and <xref:Bodu.Numerics.Interval`1> to and from JSON, together with a one-call extension that registers them under a chosen policy.

`Fraction<T>` and `Interval<T>` already carry `[JsonConverter]` attributes, so they serialize correctly with the default `JsonSerializerOptions`. Use this namespace when you want to select a non-default wire policy (lenient parsing, or a compact numeric form) across a whole `JsonSerializerOptions` instance.

## Static documentation

- **[Numerics JSON serialization guide](~/guides/numerics/json-serialization.md)** — wire formats, the three policies, and registering the converters.

## Key types

- <xref:Bodu.Numerics.Serialization.NumericsJsonSerializerOptionsExtensions> — `AddNumericsJsonConverters(JsonSerializerOptions, NumericsJsonPolicy)` registers the `Fraction<T>` and `Interval<T>` converter factories on an options instance and returns it for chaining.
- <xref:Bodu.Numerics.Serialization.NumericsJsonPolicy> — the wire-format selector: `Strict` (default), `Lenient`, and `Compact`.
- <xref:Bodu.Numerics.Serialization.FractionJsonConverter`1>, <xref:Bodu.Numerics.Serialization.FractionJsonConverterFactory> — converters for `Fraction<T>`; the factory binds the open-generic converter to the concrete `T` at run time.
- <xref:Bodu.Numerics.Serialization.IntervalJsonConverter`1>, <xref:Bodu.Numerics.Serialization.IntervalJsonConverterFactory> — the equivalent converters for `Interval<T>`.

## Example

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization;

var options = new JsonSerializerOptions()
    .AddNumericsJsonConverters(NumericsJsonPolicy.Strict);

string json = JsonSerializer.Serialize(Fraction<int>.Create(8, 15), options);
Fraction<int> round = JsonSerializer.Deserialize<Fraction<int>>(json, options);
```

## Notes

- **Default registration is automatic.** Because both value types declare `[JsonConverter]`, they serialize without calling `AddNumericsJsonConverters`. Register explicitly only to change the policy.
- **Factories bind the generic parameter.** `Fraction<T>` and `Interval<T>` are open generics; the converter *factories* resolve the concrete `T` per request, which is why registration adds the factory rather than a closed converter.
- **See also:** the [Numerics JSON serialization guide](~/guides/numerics/json-serialization.md) and the [Bodu.Numerics overview](Bodu.Numerics.md).
