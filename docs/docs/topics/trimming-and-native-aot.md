---
title: Trimming and native AOT
---

# Trimming and native AOT

Publishing an application trimmed (`PublishTrimmed`) or as native AOT (`PublishAot`) removes the code the app does not use and, for AOT, compiles the rest ahead of time. Both break code that discovers types by reflection or creates generic types at run time, so .NET marks such APIs with `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, and the trim and AOT analyzers report `IL2026` and `IL3050` wherever your code calls one. This page says which Bodu packages are checked for trimming and AOT, which of their entry points carry those attributes, and the route each package offers instead.

## Package by package

A package **declared AOT-compatible** (`IsAotCompatible` in its project) runs the trim and AOT analyzers over its own code on every build, so any reflection it needs is annotated rather than hidden. A package that is not declared may still work in a trimmed app, but nothing checks it: test the published application.

| Package | Declared AOT-compatible | Annotated entry points | The AOT route |
|---|---|---|---|
| `Bodu.Core` | Yes | None. The `Enums` description and display-name helpers read enum fields by reflection, but annotate the fields they keep, so they raise no warning. | Everything. |
| `Bodu.Collections`, `Bodu.Collections.Concurrent` | Yes | None. | Everything. |
| `Bodu.IO.Hashing`, `Bodu.Security.Cryptography` | Yes | None. | Everything. |
| `Bodu.Globalization.Recurrence` | Yes | None. | Everything. |
| `Bodu.Numerics` | Yes | None. | Everything; a native-AOT smoke app in the repository publishes and runs it on every change to `Bodu.Numerics`, its JSON companion, or `Bodu.Core`. |
| `Bodu.Numerics.Serialization.Json` | Yes | The five converter factories' constructors and `AddNumericsJsonConverters` (`[RequiresDynamicCode]`); `FractionJsonExtensions.ToJson` and `FromJson` (both attributes). | Register the closed converters (`FractionJsonConverter<T>`, `IntervalJsonConverter<T>`, `DiscreteIntervalJsonConverter<T>`, `IntervalSetJsonConverter<T>`, `ComplexJsonConverter<T>`, `BigDecimalJsonConverter`) and serialize through a source-generated `JsonSerializerContext`. The smoke app runs this route for every type. |
| `Bodu.Financial.Serialization.Json` | Yes | `MoneyOfTCurrencyJsonConverterFactory`'s constructors, `AddFinancialJsonConverters`, and `AddFinancialJson` (`[RequiresDynamicCode]`). Trimming alone raises nothing. | Register `MoneyOfTCurrencyJsonConverter<TCurrency>` for each currency with the five non-generic converters, and serialize through a source-generated context. |
| `Bodu.Text.Toml`, `Bodu.Text.Bencode`, `Bodu.Text.Yaml` | No | Every `TomlSerializer`, `BencodeSerializer`, and `YamlSerializer` entry point (both attributes). | The token reader and writer (`Utf8TomlReader` / `Utf8TomlWriter` and their siblings), which bind nothing by reflection; there is no reflection-free serializer path. |
| `Bodu.Text.Delimited`, `Bodu.Text.Ini` | No | The reflection-based `DelimitedSerializer` and `IniSerializer` entry points (both attributes). | The factory overloads - `DelimitedSerializer.Serialize` / `Deserialize` over an `IDelimitedRecordFactory<TRecord>`, and `IniSerializer.SerializeSection` / `DeserializeSection` over an `IIniSectionFactory<TSection>` - which carry neither attribute. |
| `Bodu.Text.DotEnv` | No | Every `DotEnvSerializer` entry point (both attributes). | The token reader and writer; there is no factory path. |
| `Bodu.Globalization.Calendar.Plugins` | No | `NotableDatePluginLoader.LoadFrom` and `LoadFromFile` (both attributes). Loading code at run time cannot work under native AOT. | Ship rules as data: compile documents to `.bcal` packs, which load without reflection, and put custom algorithms in your own assembly. |
| Every other package | No | None. | Not checked by the analyzers; test the published application. |

## Serializing your own types

The six text serializers (`TomlSerializer`, `BencodeSerializer`, `YamlSerializer`, `DelimitedSerializer`, `DotEnvSerializer`, `IniSerializer`) map your types by reflection, the way the reflection-based `System.Text.Json` serializer does. Under trimming or native AOT you have two routes that raise no warning:

- **Read and write tokens yourself.** Each format's `Utf8…Reader` and `Utf8…Writer` work on UTF-8 bytes and bind nothing, so code written against them carries no warning.
- **Use a factory, for Delimited and INI.** A factory is a small class that reads and writes one record or section type's fields by name. The `Bodu.Text.Formats.Generators` source generator emits one for a `[DelimitedRecord]` or `[IniSection]` partial type, but it is **not packaged** (`IsPackable=false`), so a NuGet consumer either references the generator from a clone, as [its page](../formats/generators.md) shows, or writes the factory by hand against the same interface.

## JSON for the Numerics and Financial types

The JSON companions' converters read and write through `Utf8JsonReader` and `Utf8JsonWriter` without reflection; only the factories, which close a generic converter at run time, need dynamic code. Register the closed converters for the types you use, and give the options to a source-generated context:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

var options = new JsonSerializerOptions();
options.Converters.Add(new FractionJsonConverter<int>(NumericsJsonPolicy.Strict));
options.Converters.Add(new IntervalJsonConverter<double>(NumericsJsonPolicy.Strict));

var context = new AppJsonContext(options);
var order = new Order { Share = Fraction<int>.Create(1, 3), Range = Interval<double>.Closed(0.5, 2.5) };

string json = JsonSerializer.Serialize(order, context.Order);
Order? back = JsonSerializer.Deserialize(json, context.Order);

public sealed class Order
{
    public Fraction<int> Share { get; set; }

    public Interval<double> Range { get; set; }
}

[JsonSerializable(typeof(Order))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
```

The closed converters are what the factories create, so the JSON is the same either way. The [Numerics](../../guides/numerics/json-serialization.md#trimming-and-aot) and [Financial](../../guides/financial/json-serialization.md#trimming-and-aot) JSON guides list the converters for every type.

## Calendars

The calendar runtime is not declared AOT-compatible. Its plugin loader is the one part that cannot work under native AOT by design, so it is annotated, and the alternative is data rather than code: compile your notable-date documents to sealed `.bcal` packs with the `bodu-calendar` tool or the MSBuild integration, and load them with `NotableDateResourceLoader.LoadBinary`. Loading a pack parses no XML or JSON and reconstructs every rule from a closed table, so it uses no reflection. See [binary rule packs](../../guides/calendar/binary-rule-packs.md).

## Where to go next

- **[Versioning, stability, and support](../versioning-and-support.md)** - the frameworks the packages target.
- **[Reflection-free binding](../formats/generators.md)** - the Delimited and INI source generator and its factories.
- **[Package matrix](../package-matrix.md)** - every package and its dependencies.
