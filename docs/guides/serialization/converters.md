---
title: Writing converters
---

# Writing converters

A converter customises how a single type is read and written: derive `TomlConverter<T>` (<xref:Bodu.Text.Toml.Serialization.TomlConverter`1>) or `BencodeConverter<T>` (<xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>), and read or write values through the format's reader and writer. A TOML converter reads through <xref:Bodu.Text.Toml.Reader.TomlDocumentReader> and writes through <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter>; a Bencode converter reads through <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and writes through <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>.

Because each library is self-contained, a converter is written against one format's reader/writer. The pattern is identical across the two; only the prefix and the reader/writer types differ. The set of converters each library already ships — and therefore the types you never need to write one for — is listed in the [built-in converter catalog](builtin-converters.md).

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

Some BCL types have no native form in a format and are rejected unless a converter maps them: in **Bencode** that is Booleans, floating-point types, `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, and the date-time types — by design, the library never invents a lossy representation implicitly. (TOML has built-in mappings for all of these, including `decimal` and `TimeSpan`; see the [built-in converter catalog](builtin-converters.md).) A converter bridges the gap — for example, writing a `bool` as a Bencode integer:

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

## Pattern 6 — Fail clearly on malformed data

By the time `Read` runs, the document has already parsed — a syntactically malformed document raises the format's parse exception (<xref:Bodu.Text.Toml.TomlFormatException> / <xref:Bodu.Text.Bencode.BencodeFormatException>) before any converter is consulted. What a converter must handle is a *well-formed value that does not fit*: the wrong kind, or text that does not parse into the target type. Signal that by throwing the format's serialization exception (<xref:Bodu.Text.Toml.TomlSerializationException> / <xref:Bodu.Text.Bencode.BencodeSerializationException>) — the same type the built-in converters throw, so callers need one catch clause regardless of which converter rejected the value. Hardening the `PointConverter` from Pattern 1:

```csharp
public override Point Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
{
    if (reader.TokenType != TomlTokenType.String)
        throw new TomlSerializationException($"Expected a string but found '{reader.TokenType}'.");

    string text = reader.GetString();
    string[] parts = text.Split(',');

    if (parts.Length != 2
        || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out int x)
        || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out int y))
    {
        throw new TomlSerializationException($"The value '{text}' is not a valid Point ('x,y').");
    }

    return new Point(x, y);
}
```

Check the kind through `reader.TokenType` before calling a typed getter, and prefer `TryParse` plus an explicit throw over letting a `FormatException` escape — the serialization exception tells the caller *which contract* failed, in the exception family they already handle. Do not throw the format exception from a converter: that type is reserved for syntactically invalid documents.

## Design notes — statelessness and caching

**Write converters stateless.** The serializer resolves the converter for a type once, caches the result on the options instance, and reuses that single converter instance for every subsequent value of the type — across calls and across threads. Instance fields mutated during `Read` or `Write` are therefore shared, unsynchronized state. Keep configuration in `readonly` fields set at construction (the way the built-in string-enum converter takes its naming policy), and derive everything else from the arguments the serializer passes in.

**Options freeze on first use.** As described in [core concepts](../../docs/serialization/concepts.md), a `…SerializerOptions` instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` — and from then on caches its resolved converters and type metadata; later mutation of `Converters` is rejected. Two practical consequences:

- **Reuse one options instance.** The expensive work — reflection over your types, converter resolution — happens once per options instance. Constructing fresh options per call discards the caches and repeats it.
- **Register before first use.** Converter changes after the options have been used (or frozen) throw; the resolution order in Pattern 3 is evaluated against the converter list as it stood when the type was first seen.

## See also

- [Built-in converter catalog](builtin-converters.md) — the types that already have a converter, and their exact wire forms.
- [Mapping attributes](attributes.md) — declarative shaping; `[…Converter]` placement and the precedence ladder.
- [Using TOML](toml.md) and [Using Bencode](bencode.md) — the per-format walk-throughs, including each format's error-handling pattern.
- [Core concepts](../../docs/serialization/concepts.md) — converter resolution and options caching in the family vocabulary.
- [Text & Serialization guides](../topics/text-and-serialization.md) and the [topic overview](../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Toml.Serialization.TomlConverter`1>, <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>, <xref:Bodu.Text.Toml.Serialization.TomlConverterFactory>, <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory>.
