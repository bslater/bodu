---
title: Writing converters
---

# Writing converters

A converter customises how a single type is read and written. It mirrors [`JsonConverter<T>`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonconverter-1): derive `TomlConverter<T>` (<xref:Bodu.Text.Toml.Serialization.TomlConverter`1>) or `BencodeConverter<T>` (<xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>), and read or write tokens through the format's `Utf8…Reader` and `Utf8…Writer`.

Because each library is self-contained, a converter is written against one format's reader/writer. The pattern is identical across the two; only the prefix differs.

## Pattern 1 — Convert a value type

A TOML converter that stores a `Point` as a `"x,y"` string:

```csharp
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

public sealed class PointConverter : TomlConverter<Point>
{
    public override Point Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        string[] parts = reader.GetString().Split(',');
        return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public override void Write(Utf8TomlWriter writer, Point value, TomlSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

The Bencode equivalent derives `BencodeConverter<Point>` and takes `ref Utf8BencodeReader` / `Utf8BencodeWriter` — the same shape with the `Bencode` prefix.

## Pattern 2 — Register it

Two ways, highest precedence first:

```csharp
// On a member or type:
public sealed class Shape
{
    [TomlConverter(typeof(PointConverter))]
    public Point Origin { get; set; }
}

// Or on the options:
var options = new TomlSerializerOptions();
options.Converters.Add(new PointConverter());
```

## Pattern 3 — Understand resolution order

For a given type the serializer selects a converter by checking, in order:

1. a member-level converter attribute (`[TomlConverter(...)]` / `[BencodeConverter(...)]`);
2. a type-level converter attribute;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options.

## Pattern 4 — Serve a family of types

To convert an open generic (say, every `Money<TCurrency>`), derive `TomlConverterFactory` (<xref:Bodu.Text.Toml.Serialization.TomlConverterFactory>) — or `BencodeConverterFactory` (<xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory>) — return `true` from `CanConvert` for the family, and build the concrete converter in `CreateConverter`. This is the same pattern the built-in nullable, enum, collection, and dictionary converters use.

## Pattern 5 — Map a type the format cannot represent

Some BCL types have no native form in a format and are rejected unless a converter maps them: Booleans, floating-point, and date-times in **Bencode**; `decimal` and `TimeSpan` in **TOML**. A converter bridges the gap — for example, writing a `bool` as a Bencode integer:

```csharp
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

public sealed class BoolAsIntConverter : BencodeConverter<bool>
{
    public override bool Read(ref Utf8BencodeReader reader, Type t, BencodeSerializerOptions o) =>
        reader.GetInt64() != 0;

    public override void Write(Utf8BencodeWriter writer, bool value, BencodeSerializerOptions o) =>
        writer.WriteInteger(value ? 1 : 0);
}
```

## Built-in enum converters

For enums you usually do not need a hand-written converter. Each library ships a string-enum converter (member names) and a number-enum converter; reference them from a `[…Converter]` attribute on a member, property, or the enumeration itself — the `JsonStringEnumConverter` / number-enum analogues.
