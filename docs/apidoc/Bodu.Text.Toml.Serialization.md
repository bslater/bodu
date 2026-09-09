---
uid: Bodu.Text.Toml.Serialization
---

![Bodu.Text.Toml.Serialization](~/images/hero-toml.svg)

## Purpose

**Bodu.Text.Toml.Serialization** holds the extensibility surface of the <xref:Bodu.Text.Toml.TomlSerializer>: the converter base types you derive from to control how a CLR type is written and read, and the built-in enum converters. A converter reads through the normalized <xref:Bodu.Text.Toml.Reader.TomlDocumentReader> and writes through the <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter>. The attribute family, naming policies, and serialization callbacks it works with are shared across formats and live in <xref:Bodu.Text.Serialization>.

## Key types

- <xref:Bodu.Text.Toml.Serialization.TomlConverter> — the non-generic root (`CanConvert`) that `TomlSerializerOptions.Converters` holds.
- <xref:Bodu.Text.Toml.Serialization.TomlConverter`1> — the per-type base: implement `Read` and `Write` for `T`.
- <xref:Bodu.Text.Toml.Serialization.TomlConverterFactory> — the base for converter families: `CanConvert` plus `CreateConverter` for a closed type.
- <xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter> / <xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter`1> — enums as member-name strings, with an optional <xref:Bodu.Text.Serialization.NamingPolicy> and integer fallback.
- <xref:Bodu.Text.Toml.Serialization.TomlNumberEnumConverter`1> — enums as their underlying integer.

## Example

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

public sealed class VersionConverter : TomlConverter<Version>
{
    public override Version Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        Version.Parse(reader.GetString());

    public override void Write(Utf8TomlWriter writer, Version value, TomlSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

var options = new TomlSerializerOptions();
options.Converters.Add(new VersionConverter());
```

## Notes

- **Read binds through `TomlDocumentReader`.** Unlike the Bencode and YAML siblings, the TOML read path hands converters the normalized tree-order cursor, not the source-order `Utf8TomlReader`, so headers and dotted keys are already resolved.
- **Resolution order.** A `[Converter]` attribute on the member or type wins, then `Converters` in registration order, then the built-in catalog.
- **Internal converters.** The `Bodu.Text.Toml.Serialization.Converters` sub-namespace is internal; extend through the public base types above.
- **See also:** the [Bodu.Text.Toml introduction](~/docs/serialization/toml/index.md) and the [writing converters](~/guides/serialization/toml/converters.md) guide.
