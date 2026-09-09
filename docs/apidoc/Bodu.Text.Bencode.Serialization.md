---
uid: Bodu.Text.Bencode.Serialization
---

![Bodu.Text.Bencode.Serialization](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode.Serialization** holds the extensibility surface of the <xref:Bodu.Text.Bencode.BencodeSerializer>: the converter base types you derive from to control how a CLR type is written and read, and the built-in enum converters. A converter reads through the <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and writes through the <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>. The attribute family, naming policies, and serialization callbacks it works with are shared across formats and live in <xref:Bodu.Text.Serialization>.

## Key types

- <xref:Bodu.Text.Bencode.Serialization.BencodeConverter> — the non-generic root (`CanConvert`) that `BencodeSerializerOptions.Converters` holds.
- <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> — the per-type base: implement `Read` and `Write` for `T`.
- <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory> — the base for converter families: `CanConvert` plus `CreateConverter` for a closed type.
- <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter> / <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter`1> — enums as member-name byte strings, with an optional <xref:Bodu.Text.Serialization.NamingPolicy> and integer fallback.
- <xref:Bodu.Text.Bencode.Serialization.BencodeNumberEnumConverter`1> — enums as their underlying integer.

## Example

```csharp
using Bodu.Text.Bencode;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

public sealed class VersionConverter : BencodeConverter<Version>
{
    public override Version Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
        Version.Parse(reader.GetString());

    public override void Write(Utf8BencodeWriter writer, Version value, BencodeSerializerOptions options) =>
        writer.WriteString(value.ToString());
}

var options = new BencodeSerializerOptions();
options.Converters.Add(new VersionConverter());
```

## Notes

- **Resolution order.** A `[Converter]` attribute on the member or type wins, then `Converters` in registration order, then the built-in catalog.
- **Kinds Bencode cannot represent.** Booleans, floating-point values, and date-times have no built-in mapping — a registered converter must reduce them to an integer or byte string.
- **Internal converters.** The `Bodu.Text.Bencode.Serialization.Converters` sub-namespace is internal; extend through the public base types above.
- **See also:** the [Bodu.Text.Bencode introduction](~/docs/serialization/bencode/index.md) and the [writing converters](~/guides/serialization/bencode/converters.md) guide.
