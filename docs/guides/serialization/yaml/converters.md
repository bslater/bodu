---
title: Writing converters
---

# Writing converters

A converter customizes how a single type is read and written: derive `YamlConverter<T>` (<xref:Bodu.Text.Yaml.Serialization.YamlConverter`1>) and read or write values through the format's reader and writer. A YAML converter reads through <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> and writes through <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>:

```csharp
public abstract T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options);
public abstract void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options);
```

The reader is a token cursor over an already-composed tree — YAML's anchors, aliases, and merge keys are resolved during the parse, so a converter never sees an alias token. On entry the reader is positioned on the first token of the value; on return it must be positioned on the value's last token (the scalar itself, or the container's end token). The sibling libraries ([TOML](../toml/index.md), [Bencode](../bencode/index.md)) follow the identical pattern with their own prefix and reader/writer types. The set of converters the library already ships — and therefore the types you never need to write one for — is listed in the [built-in converter catalog](builtin-converters.md).

## Pattern 1 — A value type as a single scalar

A converter that stores a `Point` as an `"x,y"` string scalar:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

public sealed class PointConverter : YamlConverter<Point>
{
    public override Point Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        string[] parts = reader.GetString().Split(',');
        return new Point(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    public override void Write(Utf8YamlWriter writer, Point value, YamlSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

## Pattern 2 — Register it

Two ways, highest precedence first:

```csharp
// On a member or type:
public sealed class Shape
{
    [Converter(typeof(PointConverter))]
    public Point Origin { get; set; }
}

// Or on the options:
var options = new YamlSerializerOptions();
options.Converters.Add(new PointConverter());

string yaml = YamlSerializer.Serialize(new Shape { Origin = new Point(1, 2) }, options);
// Origin: 1,2
```

Register options-level converters before the options are first used — an options instance freezes on first use (or eagerly via `MakeReadOnly()`), and later mutation of `Converters` throws.

## Pattern 3 — Understand resolution order

For a given type the serializer selects a converter by checking, in order:

1. a member-level converter attribute (`[Converter(...)]`);
2. a type-level converter attribute;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options. `CanConvert` defaults to an **exact** type check — `typeof(T) == typeToConvert` — so a `YamlConverter<Animal>` does not apply to a `Dog` subclass; override `CanConvert` (or use a factory, Pattern 4) to cover a hierarchy.

## Pattern 4 — Serve a family of types

To convert an open generic or a whole category of types, derive `YamlConverterFactory` (<xref:Bodu.Text.Yaml.Serialization.YamlConverterFactory>), return `true` from `CanConvert` for the family, and build the concrete converter in `CreateConverter`. This is the same pattern the built-in nullable, enum, collection, and dictionary converters — and the public <xref:Bodu.Text.Yaml.Serialization.YamlStringEnumConverter> — use:

```csharp
public sealed class MoneyConverterFactory : YamlConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Money<>);

    public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options) =>
        (YamlConverter)Activator.CreateInstance(
            typeof(MoneyConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}
```

The factory itself never reads or writes a value: the serializer calls `CanConvert` to decide whether the factory applies, then `CreateConverter` once per closed type and caches the result.

## Pattern 5 — A type read from a mapping

A converter is not limited to scalars: read a mapping by walking the reader's tokens, and write one by bracketing `WriteStartMapping` / `WriteEndMapping`:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

public readonly record struct GeoPoint(double Latitude, double Longitude);

public sealed class GeoPointConverter : YamlConverter<GeoPoint>
{
    public override GeoPoint Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType != YamlTokenType.StartMapping)
            throw new YamlSerializationException($"Expected a mapping but found '{reader.TokenType}'.");

        double lat = 0, lon = 0;
        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string key = reader.GetString();
            reader.Read();
            if (key == "lat") lat = reader.GetDouble();
            else if (key == "lon") lon = reader.GetDouble();
            else reader.Skip();
        }

        return new GeoPoint(lat, lon);
    }

    public override void Write(Utf8YamlWriter writer, GeoPoint value, YamlSerializerOptions options)
    {
        writer.WriteStartMapping();
        writer.WritePropertyName("lat");
        writer.WriteDouble(value.Latitude);
        writer.WritePropertyName("lon");
        writer.WriteDouble(value.Longitude);
        writer.WriteEndMapping();
    }
}
```

```yaml
lat: 51.5
lon: -0.12
```

The writer's surface is `WriteStartMapping` / `WriteEndMapping`, `WriteStartSequence` / `WriteEndSequence`, `WritePropertyName(string)`, and the scalar writers `WriteString` / `WriteInteger` / `WriteDouble` / `WriteBoolean` / `WriteNull`. It emits block-style collections; an empty container falls back to flow `[]` / `{}`.

## Built-in enum converters

For enums you usually do not need a hand-written converter. The library ships a string-enum converter (member names) and a number-enum converter; reference them from a `[Converter]` attribute on a member, property, or the enumeration itself, or register one on the options.

On the enumeration itself, use the generic string-enum form (<xref:Bodu.Text.Yaml.Serialization.YamlStringEnumConverter`1>), optionally renaming individual members:

```csharp
[Converter(typeof(YamlStringEnumConverter<Status>))]
public enum Status
{
    Active,

    [StringEnumMemberName("on-hold")]
    OnHold,
}

// Status.OnHold serializes as: Status: on-hold
```

On a single member, the generic number-enum form (<xref:Bodu.Text.Yaml.Serialization.YamlNumberEnumConverter`1>) writes the underlying value instead:

```csharp
public sealed class WorkItem
{
    [Converter(typeof(YamlNumberEnumConverter<Priority>))]
    public Priority Priority { get; set; }
}

// Priority.High (underlying value 2) serializes as: Priority: 2
```

To cover *every* enumeration in one registration, add the non-generic string-enum factory (<xref:Bodu.Text.Yaml.Serialization.YamlStringEnumConverter>) to the options, optionally with a naming policy:

```csharp
var options = new YamlSerializerOptions();
options.Converters.Add(new YamlStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues: false));

// Status.OnHold now serializes as on_hold everywhere.
```

The generic forms expose a public parameterless constructor, which is what makes them usable from a `[Converter]` attribute; the non-generic factory is the options-level, all-enums form. There is no non-generic number-enum converter. Without any of these, the default enum handling still honors `[StringEnumMemberName]` and the `WriteEnumsAsStrings` flag (see the [built-in converter catalog](builtin-converters.md)).

## Pattern 6 — Fail clearly on malformed data

By the time `Read` runs, the document has already parsed — a syntactically malformed document raises <xref:Bodu.Text.Yaml.YamlFormatException> before any converter is consulted. What a converter must handle is a *well-formed value that does not fit*: the wrong token kind, or text that does not parse into the target type. Signal that by throwing <xref:Bodu.Text.Yaml.YamlSerializationException> — the same type the built-in converters throw, so callers need one catch clause. Hardening the `PointConverter` from Pattern 1:

```csharp
public override Point Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
{
    if (reader.TokenType != YamlTokenType.String)
        throw new YamlSerializationException($"Expected a string but found '{reader.TokenType}'.");

    string text = reader.GetString();
    string[] parts = text.Split(',');

    if (parts.Length != 2
        || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out int x)
        || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out int y))
    {
        throw new YamlSerializationException($"The value '{text}' is not a valid Point ('x,y').");
    }

    return new Point(x, y);
}
```

Check the kind through `reader.TokenType` before calling a typed getter, and prefer `TryParse` plus an explicit throw over letting a `FormatException` escape. Do not throw <xref:Bodu.Text.Yaml.YamlFormatException> from a converter — that type is reserved for syntactically invalid documents.

## Design notes — statelessness and caching

**Write converters stateless.** The serializer resolves the converter for a type once, caches the result on the options instance, and reuses that single converter for every subsequent value — across calls and threads. Keep configuration in `readonly` fields set at construction, and derive everything else from the `reader` / `value` / `options` the serializer passes in.

**Options freeze on first use.** A <xref:Bodu.Text.Yaml.YamlSerializerOptions> instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` — and then caches resolved converters and type metadata. Two consequences: reuse one options instance (fresh options per call discards the caches and repeats the reflection), and register every converter before first use (later `Converters` mutation throws).

## Where to go next

- [Built-in converter catalog](builtin-converters.md) — the types that already have a converter, so you do not rewrite a provisioned one.
- [Mapping attributes](attributes.md) — the declarative layer (`[PropertyName]`, `[Ignore]`, `[Converter]`, naming policies, options flags) that covers shaping short of a converter.
- [Using YAML](using.md) — the per-format walk-through, including the reader/writer seam and error handling.
- [Bodu.Text.Yaml core concepts](../../../docs/serialization/yaml/concepts.md) — converter resolution and options caching in the family vocabulary.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1>, <xref:Bodu.Text.Yaml.Serialization.YamlConverterFactory>, <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader>, <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>.
