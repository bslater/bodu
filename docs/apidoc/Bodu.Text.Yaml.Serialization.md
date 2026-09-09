---
uid: Bodu.Text.Yaml.Serialization
---

![Bodu.Text.Yaml.Serialization](~/images/hero-yaml.svg)

## Purpose

**Bodu.Text.Yaml.Serialization** holds the extensibility surface of the <xref:Bodu.Text.Yaml.YamlSerializer>: the converter base types you derive from to control how a CLR type is written and read, and the built-in enum converters. A converter reads through the <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> and writes through the <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>. The attribute family, naming policies, and serialization callbacks it works with are shared across formats and live in <xref:Bodu.Text.Serialization>.

## Key types

- <xref:Bodu.Text.Yaml.Serialization.YamlConverter> — the non-generic root (`CanConvert`) that `YamlSerializerOptions.Converters` holds.
- <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> — the per-type base: implement `Read` and `Write` for `T`.
- <xref:Bodu.Text.Yaml.Serialization.YamlConverterFactory> — the base for converter families: `CanConvert` plus `CreateConverter` for a closed type.
- <xref:Bodu.Text.Yaml.Serialization.YamlStringEnumConverter> / <xref:Bodu.Text.Yaml.Serialization.YamlStringEnumConverter`1> — enums as member-name strings, with an optional <xref:Bodu.Text.Serialization.NamingPolicy> and integer fallback.
- <xref:Bodu.Text.Yaml.Serialization.YamlNumberEnumConverter`1> — enums as their underlying integer.

## Example

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

public sealed class VersionConverter : YamlConverter<Version>
{
    public override Version Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        Version.Parse(reader.GetString());

    public override void Write(Utf8YamlWriter writer, Version value, YamlSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

var options = new YamlSerializerOptions();
options.Converters.Add(new VersionConverter());
```

## Notes

- **Scalar converters are format-local.** YAML's implicit typing coerces across scalar kinds (string / integer / float / boolean / null), so the built-in scalar converters live here rather than in the shared engine; the structural (nullable, collection, dictionary, object) factories are the shared ones.
- **Resolution order.** A `[Converter]` attribute on the member or type wins, then `Converters` in registration order, then the built-in catalog.
- **Internal converters.** The `Bodu.Text.Yaml.Serialization.Converters` sub-namespace is internal; extend through the public base types above.
- **See also:** the [Bodu.Text.Yaml introduction](~/docs/serialization/yaml/index.md) and the [writing converters](~/guides/serialization/yaml/converters.md) guide.
