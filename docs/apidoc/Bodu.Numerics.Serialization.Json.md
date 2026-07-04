---
uid: Bodu.Numerics.Serialization.Json
---

# Bodu.Numerics.Serialization.Json

## Purpose

**Bodu.Numerics.Serialization.Json** carries the `System.Text.Json` integration for [`Bodu.Numerics`](Bodu.Numerics.md). It supplies the converters that round-trip <xref:Bodu.Numerics.Fraction`1>, <xref:Bodu.Numerics.Interval`1>, <xref:Bodu.Numerics.DiscreteInterval`1>, and <xref:Bodu.Numerics.IntervalSet`1> to and from JSON, together with a one-call extension that registers them under a chosen policy.

The core `Bodu.Numerics` library is serialization-agnostic — its value types carry no `[JsonConverter]` attribute and take no dependency on `System.Text.Json`. Add this package and call `ConfigureForBoduNumerics` to opt into JSON support and select a wire policy across a whole `JsonSerializerOptions` instance.

## Static documentation

- **[Numerics JSON serialization guide](~/guides/numerics/json-serialization.md)** — wire formats, the three policies, and registering the converters.

## Key types

- <xref:Bodu.Numerics.Serialization.Json.NumericsJsonSerializerOptionsExtensions> — `ConfigureForBoduNumerics(JsonSerializerOptions, NumericsJsonPolicy)` registers the converter factories for every numeric value type on an options instance and returns it for chaining.
- <xref:Bodu.Numerics.Serialization.Json.NumericsJsonPolicy> — the wire-format selector: `Strict` (default), `Lenient`, and `Compact`.
- <xref:Bodu.Numerics.Serialization.Json.FractionJsonConverter`1>, <xref:Bodu.Numerics.Serialization.Json.FractionJsonConverterFactory> — converters for `Fraction<T>`; the factory binds the open-generic converter to the concrete `T` at run time.
- <xref:Bodu.Numerics.Serialization.Json.IntervalJsonConverter`1>, <xref:Bodu.Numerics.Serialization.Json.IntervalJsonConverterFactory> — the equivalent converters for `Interval<T>`, including the unbounded-endpoint markers.
- <xref:Bodu.Numerics.Serialization.Json.DiscreteIntervalJsonConverter`1>, <xref:Bodu.Numerics.Serialization.Json.IntervalSetJsonConverter`1> (and their factories) — converters for `DiscreteInterval<T>` (through the interval wire shape) and `IntervalSet<T>` (a JSON array of pieces).
- <xref:Bodu.Numerics.Serialization.Json.FractionJsonExtensions> — the `ToJson()` / `FromJson<T>(string)` convenience helpers.

## Example

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

var options = new JsonSerializerOptions()
    .ConfigureForBoduNumerics(NumericsJsonPolicy.Strict);

string json = JsonSerializer.Serialize(new Fraction<int>(8, 15), options);
Fraction<int> round = JsonSerializer.Deserialize<Fraction<int>>(json, options);
```

## Notes

- **Registration is required.** The core value types carry no `[JsonConverter]` attribute, so JSON support is opt-in: call `ConfigureForBoduNumerics` (or add a factory to `JsonSerializerOptions.Converters`) before serializing.
- **Factories bind the generic parameter.** The numeric value types are open generics; the converter *factories* resolve the concrete `T` per request, which is why registration adds the factory rather than a closed converter.
- **Pair results are not serializable.** `IntervalPair<T>` and `DiscreteIntervalPair<T>` are transient operation results; call `ToIntervalSet()` and serialize the resulting `IntervalSet<T>`.
- **See also:** the [Numerics JSON serialization guide](~/guides/numerics/json-serialization.md) and the [Bodu.Numerics overview](Bodu.Numerics.md).
