# Bodu.Text.Serialization

The common serialization engine shared by the Bodu format serializers on .NET 8. It provides a `System.Text.Json`-style object mapper — converters, options, naming policies, and attributes — over a format-neutral reader/writer seam, plus the lossless concrete-syntax-tree (CST) abstraction that each format builds on.

This package is the shared core; it is consumed by the per-format serializers (`Bodu.Text.Serialization.Toml`, `Bodu.Text.Serialization.Bencode`) and is not normally referenced on its own.

## Installation

```shell
dotnet add package Bodu.Text.Serialization
```

Targets `net8.0`.

## What it provides

| Area | Types | Purpose |
|---|---|---|
| Syntax tree | `SyntaxNode`, `SyntaxToken`, `SyntaxTrivia`, `SyntaxList<T>` | A generic CST whose nodes carry an integer `RawKind` "type code" that each format maps onto its own `Kind` enumeration. Lossless: a parsed tree reproduces its source exactly. |
| Engine | `FormatSerializerOptions`, `FormatConverter<T>`, `FormatConverterFactory`, `FormatNamingPolicy` | The reflection binder and converter model, mirroring `JsonSerializerOptions` / `JsonConverter<T>` / `JsonNamingPolicy`. |
| Adapter seam | `ISerializationReader`, `ISerializationWriter`, `SerializationValueKind` | The format-neutral value surface a converter reads and writes; each format implements it over its CST. |
| Attributes | `FormatPropertyNameAttribute`, `FormatIgnoreAttribute`, `FormatConverterAttribute`, `FormatConstructorAttribute` | A single attribute set honored by every format serializer. |

## API shape

A converter mirrors `System.Text.Json`:

```csharp
public sealed class MyConverter : FormatConverter<MyType>
{
    public override MyType Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options) { ... }
    public override void Write(ISerializationWriter writer, MyType value, FormatSerializerOptions options) { ... }
}
```

Resolution order matches `System.Text.Json`: a property `[FormatConverter]` attribute, then a type `[FormatConverter]` attribute, then `options.Converters`, then the built-in converters.

## Testing

Tests live in `test/` as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Serialization/test/Bodu.Text.Serialization.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
