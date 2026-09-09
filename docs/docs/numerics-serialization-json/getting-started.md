---
title: Bodu.Numerics.Serialization.Json — Getting started
---

# Bodu.Numerics.Serialization.Json — Getting started

Unfamiliar with terms like *policy*, *factory vs closed converter*, *raw JSON number*, or *unbounded marker*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Numerics.Serialization.Json
```

Targets `net8.0`. Depends on `Bodu.Numerics` (and transitively `Bodu.Core`); `System.Text.Json` is part of the shared framework. The package is marked AOT-compatible.

## Minimal samples

### Register the converters and round-trip a fraction

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

// Registration is required — the core types carry no [JsonConverter] attribute.
var options = new JsonSerializerOptions().AddNumericsJsonConverters();   // Strict

string json = JsonSerializer.Serialize(new Fraction<int>(3, 4), options);
// {"numerator":3,"denominator":4}

Fraction<int> back = JsonSerializer.Deserialize<Fraction<int>>(json, options);
// 3/4
```

`AddNumericsJsonConverters` returns the same options instance, so it chains inline. Call it once per options instance, before that instance is first used.

### Choose a policy

```csharp
var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);
var lenient = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Lenient);

JsonSerializer.Serialize(new Fraction<int>(8, 15), compact);            // "8/15"

// Lenient accepts a top-level string and the min/max aliases on read; it writes the Strict shape.
Fraction<int> fromText = JsonSerializer.Deserialize<Fraction<int>>("\"8/15\"", lenient);
Interval<int> fromFeed = JsonSerializer.Deserialize<Interval<int>>(
    """{ "min": 1, "max": 5 }""", lenient);                               // [1, 5] — flags default to closed
```

### Intervals, discrete intervals, and sets

```csharp
Interval<int> window = Interval<int>.ClosedOpen(1, 5);

JsonSerializer.Serialize(window, options);
// {"lower":1,"upper":5,"lowerInclusive":true,"upperInclusive":false}
JsonSerializer.Serialize(window, compact);                              // "[1, 5)"

JsonSerializer.Serialize(Interval<int>.AtLeast(1), options);
// {"lower":1,"upperUnbounded":true,"lowerInclusive":true}

JsonSerializer.Serialize(Interval<int>.Empty, options);                 // {"empty":true}

IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(8, 9));
JsonSerializer.Serialize(set, compact);                                 // ["[1, 3]","[8, 9]"]
```

`DiscreteInterval<T>` serializes through the same interval shape over its canonical closed bounds. The pair result types (`IntervalPair<T>`, `DiscreteIntervalPair<T>`) are not serializable — convert them with `ToIntervalSet()` first.

### Arbitrary precision without loss

```csharp
using System.Numerics;

var precise = new Fraction<BigInteger>(BigInteger.Parse("123456789012345678901234567890"), 7);

string exact = JsonSerializer.Serialize(precise, options);
// {"numerator":123456789012345678901234567890,"denominator":7} — raw JSON numbers, nothing truncated

// Consumers that cannot carry big JSON numbers may quote the components instead.
Fraction<BigInteger> quoted = JsonSerializer.Deserialize<Fraction<BigInteger>>(
    """{ "numerator": "123456789012345678901234567890", "denominator": "7" }""", options);
```

### `BigDecimal` and `Complex<T>`

```csharp
BigDecimal price = BigDecimal.Parse("12.340");

JsonSerializer.Serialize(price, options);                               // {"unscaledValue":12340,"scale":3}
JsonSerializer.Serialize(price, compact);                               // "12.340"

var z = new Complex<double>(3, 4);

JsonSerializer.Serialize(z, options);                                   // {"real":3,"imaginary":4}
JsonSerializer.Serialize(z, compact);                                   // "<3; 4>"

JsonSerializer.Serialize(new Complex<double>(double.NaN, double.NegativeInfinity), options);
// {"real":"NaN","imaginary":"-Infinity"}
```

### One-shot helpers for fractions

```csharp
using Bodu.Numerics.Serialization.Json;

string text = new Fraction<int>(-7, 8).ToJson();                        // {"numerator":-7,"denominator":8}
Fraction<int> parsed = FractionJsonExtensions.FromJson<int>(text);

string compactText = new Fraction<int>(-7, 8).ToJson(NumericsJsonPolicy.Compact);   // "-7/8"
```

Each helper configures a fresh options instance per call and uses the reflection-based serializer (it is annotated for trimming and AOT). For repeated work, build one options instance with `AddNumericsJsonConverters` and reuse it.

### Register one factory or one closed converter by hand

```csharp
var fractionsOnly = new JsonSerializerOptions();
fractionsOnly.Converters.Add(new FractionJsonConverterFactory(NumericsJsonPolicy.Compact));   // every Fraction<T>

var intOnly = new JsonSerializerOptions();
intOnly.Converters.Add(new FractionJsonConverter<int>(NumericsJsonPolicy.Compact));          // Fraction<int> only
intOnly.Converters.Add(new BigDecimalJsonConverter());                                       // Strict by default
```

### Use with a source-generated context

The converters are reflection-free at the value level; add them to the options a `JsonSerializerContext` is built from so the closed types stay visible to the trimmer:

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Measurement))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

sealed class Measurement
{
    public Fraction<int> Ratio { get; set; }
    public Interval<double> Range { get; set; }
}

var contextOptions = new JsonSerializerOptions().AddNumericsJsonConverters();
var context = new AppJsonContext(contextOptions);

string payload = JsonSerializer.Serialize(
    new Measurement { Ratio = new Fraction<int>(1, 3), Range = Interval<double>.Closed(0.5, 1.5) },
    context.Measurement);
```

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — the converter table and scenario index.
- **[Numerics JSON serialization guide](../../guides/numerics/json-serialization.md)** — the full wire-format reference, worked examples per policy, and the failure-mode table.
- **[Bodu.Numerics getting started](../numerics/getting-started.md)** — the value types being serialized.
- **[Bodu.Numerics.Serialization.Json API reference](xref:Bodu.Numerics.Serialization.Json)** — full type-by-type docs.
- **[Runnable samples](../../samples/numerics.md)** — `Bodu.Numerics.Samples.JsonConverters` runs every shape on this page.
