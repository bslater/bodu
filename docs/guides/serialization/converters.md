---
title: Writing converters
---

# Writing converters

A converter customises how a single type is read and written. It mirrors [`JsonConverter<T>`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonconverter-1): derive <xref:Bodu.Text.Serialization.FormatConverter`1>, and read or write **format-neutral value tokens** through the reader and writer.

## Pattern 1 — Convert a value type

```csharp
using Bodu.Text.Serialization;

public sealed class PointConverter : FormatConverter<Point>
{
    public override Point Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        string[] parts = reader.GetString().Split(',');
        return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public override void Write(ISerializationWriter writer, Point value, FormatSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

Because the converter only uses the neutral seam (`GetString` / `WriteString`), it works for both TOML and Bencode.

## Pattern 2 — Register it

Two ways, highest precedence first:

```csharp
// On a member or type:
public sealed class Shape
{
    [FormatConverter(typeof(PointConverter))]
    public Point Origin { get; set; }
}

// Or on the options:
var options = new TomlSerializerOptions();
options.Converters.Add(new PointConverter());
```

## Pattern 3 — Understand resolution order

For a given type the engine selects a converter by checking, in order:

1. a member-level <xref:Bodu.Text.Serialization.FormatConverterAttribute>;
2. a type-level <xref:Bodu.Text.Serialization.FormatConverterAttribute>;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options.

## Pattern 4 — Serve a family of types

To convert an open generic (say, every `Money<TCurrency>`), derive <xref:Bodu.Text.Serialization.FormatConverterFactory>, return `true` from `CanConvert` for the family, and build the concrete converter in `CreateConverter` — the same pattern the built-in nullable, enum, and collection converters use.
