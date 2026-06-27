---
title: Writing converters
---

# Writing converters

A converter customizes how a single type is read and written. Derive <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1>, read the already-parsed value from a <xref:Bodu.Text.Yaml.Document.YamlElement>, and write through the <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>:

```csharp
public abstract T? Read(YamlElement element, YamlSerializerOptions options);
public abstract void Write(ref Utf8YamlWriter writer, T value, YamlSerializerOptions options);
```

This asymmetry is specific to YAML: the **read side receives a resolved element**, not a forward-only reader, because YAML's anchors, aliases, and merge keys require a fully composed tree. The write side drives the writer directly. One converter handles exactly one type — `CanConvert` defaults to an exact-type check. **There is no converter factory and no converter attribute**: register every converter on `options.Converters`. (The [TOML](../toml/index.md) and [Bencode](../bencode/index.md) siblings have both a factory and a `[…Converter]` attribute; YAML has neither.)

## Pattern 1 — A value type as a single scalar

A converter that stores a `Point` as an `"x,y"` string scalar:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

public sealed class PointConverter : YamlConverter<Point>
{
    public override Point Read(YamlElement element, YamlSerializerOptions options)
    {
        string[] parts = element.GetString().Split(',');
        return new Point(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    public override void Write(ref Utf8YamlWriter writer, Point value, YamlSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

## Pattern 2 — Register it

A converter is registered on the options before first use, in priority order (first match wins):

```csharp
var options = new YamlSerializerOptions();
options.Converters.Add(new PointConverter());

string yaml = YamlSerializer.Serialize(new Shape { Origin = new Point(1, 2) }, options);
// Origin: 1,2
```

Because there is no `[YamlConverter]` attribute, the options list is the only registration point. Register converters before the options are first used — an options instance freezes on first use (or eagerly via `MakeReadOnly()`), and later mutation of `Converters` throws.

## Pattern 3 — Understand resolution order

For a given type the serializer selects a converter by checking, in order:

1. the first matching converter in `options.Converters`;
2. the built-in converters.

The first match wins, and the result is cached on the options.

## Pattern 4 — A type read from a mapping

A converter is not limited to scalars: read a mapping with `GetProperty` / `TryGetProperty` and write one by bracketing `WriteStartMapping` / `WriteEndMapping`:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

public readonly record struct GeoPoint(double Latitude, double Longitude);

public sealed class GeoPointConverter : YamlConverter<GeoPoint>
{
    public override GeoPoint Read(YamlElement element, YamlSerializerOptions options)
    {
        if (element.ValueKind != YamlValueKind.Mapping)
            throw new YamlSerializationException($"Expected a mapping but found '{element.ValueKind}'.");

        double lat = element.GetProperty("lat").GetDouble();
        double lon = element.GetProperty("lon").GetDouble();
        return new GeoPoint(lat, lon);
    }

    public override void Write(ref Utf8YamlWriter writer, GeoPoint value, YamlSerializerOptions options)
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

The writer's surface is `WriteStartMapping` / `WriteEndMapping`, `WriteStartSequence` / `WriteEndSequence`, `WritePropertyName(string)`, and the scalar writers `WriteString` / `WriteInt64` / `WriteDouble` / `WriteBoolean` / `WriteNull`. It emits block-style collections; an empty container falls back to flow `[]` / `{}`.

## Pattern 5 — Fail clearly on malformed data

By the time `Read` runs, the document has already parsed — a syntactically malformed document raises <xref:Bodu.Text.Yaml.YamlFormatException> before any converter is consulted. What a converter must handle is a *well-formed value that does not fit*: the wrong kind, or text that does not parse into the target type. Signal that by throwing <xref:Bodu.Text.Yaml.YamlSerializationException> — the same type the built-in converters throw, so callers need one catch clause. Hardening the `PointConverter` from Pattern 1:

```csharp
public override Point Read(YamlElement element, YamlSerializerOptions options)
{
    if (element.ValueKind != YamlValueKind.String)
        throw new YamlSerializationException($"Expected a string but found '{element.ValueKind}'.");

    string text = element.GetString();
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

Check the kind through `element.ValueKind` before calling a typed getter, and prefer `TryParse` plus an explicit throw over letting a `FormatException` escape. Do not throw <xref:Bodu.Text.Yaml.YamlFormatException> from a converter — that type is reserved for syntactically invalid documents.

## Design notes — statelessness and caching

**Write converters stateless.** The serializer resolves the converter for a type once, caches the result on the options instance, and reuses that single converter for every subsequent value — across calls and threads. Keep configuration in `readonly` fields set at construction, and derive everything else from the `element` / `value` / `options` the serializer passes in.

**Options freeze on first use.** A <xref:Bodu.Text.Yaml.YamlSerializerOptions> instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` — and then caches resolved converters and type metadata. Two consequences: reuse one options instance (fresh options per call discards the caches and repeats the reflection), and register every converter before first use (later `Converters` mutation throws).

## Where to go next

- [Built-in converter catalog](builtin-converters.md) — the types that already have a converter, so you do not rewrite a provisioned one.
- [Mapping attributes](attributes.md) — the declarative layer (`[YamlPropertyName]`, `[YamlIgnore]`, naming policies, options flags) that covers shaping short of a converter.
- [Using YAML](using.md) — the per-format walk-through, including the reader/writer seam and error handling.
- [Bodu.Text.Yaml core concepts](../../../docs/serialization/yaml/concepts.md) — converter resolution and options caching in the family vocabulary.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1>, <xref:Bodu.Text.Yaml.Document.YamlElement>, <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>.
