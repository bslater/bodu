---
title: Writing converters
---

# Writing converters

A converter customises how a single type is read and written: derive `TomlConverter<T>` (<xref:Bodu.Text.Toml.Serialization.TomlConverter`1>) or `BencodeConverter<T>` (<xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>), and read or write values through the format's reader and writer. A TOML converter reads through <xref:Bodu.Text.Toml.Reader.TomlDocumentReader> and writes through <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter>; a Bencode converter reads through <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and writes through <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>.

Because each library is self-contained, a converter is written against one format's reader/writer. The pattern is identical across the two; only the prefix and the reader/writer types differ.

## Pattern 1 — Convert a value type

A TOML converter that stores a `Point` as a `"x,y"` string:

```csharp
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

public sealed class PointConverter : TomlConverter<Point>
{
    public override Point Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        string[] parts = reader.GetString().Split(',');
        return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public override void Write(Utf8TomlWriter writer, Point value, TomlSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

The Bencode equivalent derives `BencodeConverter<Point>` and takes `ref Utf8BencodeReader` / `Utf8BencodeWriter` — the same shape with the `Bencode` prefix and that format's reader/writer pair.

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

```csharp
public sealed class MoneyConverterFactory : TomlConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Money<>);

    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options) =>
        (TomlConverter)Activator.CreateInstance(
            typeof(MoneyConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}
```

The factory itself never reads or writes a value: the serializer calls `CanConvert` to decide whether the factory applies, then `CreateConverter` once per closed type and caches the result. `MoneyConverter<T>` here is an ordinary `TomlConverter<Money<T>>` written as in Pattern 1. The Bencode factory is the same shape with the `Bencode` prefix.

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

For enums you usually do not need a hand-written converter. Each library ships a string-enum converter (member names) and a number-enum converter; reference them from a `[…Converter]` attribute on a member, property, or the enumeration itself, or register one on the options.

On the enumeration itself, use the generic string-enum form (<xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter`1> / <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter`1>), optionally renaming individual members:

```csharp
[TomlConverter(typeof(TomlStringEnumConverter<Status>))]
public enum Status
{
    Active,

    [TomlStringEnumMemberName("on-hold")]
    OnHold,
}

// Status.OnHold serializes as: Status = "on-hold"
```

On a single member, the generic number-enum form (<xref:Bodu.Text.Toml.Serialization.TomlNumberEnumConverter`1> / <xref:Bodu.Text.Bencode.Serialization.BencodeNumberEnumConverter`1>) writes the underlying value instead:

```csharp
public sealed class WorkItem
{
    [TomlConverter(typeof(TomlNumberEnumConverter<Priority>))]
    public Priority Priority { get; set; }
}

// Priority.High (underlying value 2) serializes as: Priority = 2
```

To cover *every* enumeration in one registration, add the non-generic string-enum factory (<xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter> / <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter>) to the options, optionally with a naming policy:

```csharp
var options = new TomlSerializerOptions();
options.Converters.Add(new TomlStringEnumConverter(TomlNamingPolicy.SnakeCaseLower, allowIntegerValues: false));

// Status.OnHold now serializes as "on_hold" everywhere.
```

The generic forms expose a public parameterless constructor, which is what makes them usable from a `[…Converter]` attribute; the non-generic factory is the options-level, all-enums form. There is no non-generic number-enum converter.
