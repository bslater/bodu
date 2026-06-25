---
title: Polymorphic converters
---

# Polymorphic converters

A converter factory dispatches over a *family* of types rather than a
single type. Where a [hand-written converter](converters.md) handles one
fixed `T`, a `TomlConverterFactory`
(<xref:Bodu.Text.Toml.Serialization.TomlConverterFactory>) or
`BencodeConverterFactory`
(<xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory>) decides
*at resolution time* whether it applies to a requested type and builds the
right concrete converter for it. This is the mechanism behind two common
shapes:

- **Open-generic families** — every closed `Money<TCurrency>`, every
  `Stack<T>`, where one factory serves an unbounded set of closed types.
- **Tagged (discriminated) hierarchies** — a base type with a `"kind"`
  field whose value selects which derived type to materialize.

This guide builds on [Writing converters](converters.md); read that first
for the single-type `Read` / `Write` pattern, the precedence ladder, and
the statelessness rules. Everything here is TOML, but the Bencode shape is
identical with the `Bencode` prefix and that format's reader/writer pair.

## How a factory participates in resolution

A factory derives from `TomlConverterFactory` and overrides two methods:

```csharp
public abstract bool CanConvert(Type typeToConvert);
public abstract TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options);
```

The serializer treats the factory exactly like any other converter in the
[resolution order](converters.md): member attribute, then type attribute,
then `options.Converters`, then built-ins. When the candidate is a factory
it calls `CanConvert(type)`; on `true` it calls `CreateConverter(type, options)`
**once per closed type** and caches the result. The factory itself never
reads or writes a value — `CreateConverter` returns an ordinary
`TomlConverter<T>` that does the work.

## Pattern 1 — an open-generic family

To serve every closed `Money<TCurrency>` from one registration, match the
open generic in `CanConvert` and close `MoneyConverter<>` over the
requested type argument in `CreateConverter`:

```csharp
using Bodu.Text.Toml.Serialization;

public sealed class MoneyConverterFactory : TomlConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(Money<>);

    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options) =>
        (TomlConverter)Activator.CreateInstance(
            typeof(MoneyConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}
```

`MoneyConverter<T>` is an ordinary `TomlConverter<Money<T>>` written as in
[Writing converters](converters.md) Pattern 1. Register the factory once
on the options and every closed `Money<TCurrency>` resolves through it:

```csharp
var options = new TomlSerializerOptions();
options.Converters.Add(new MoneyConverterFactory());
// Money<Usd>, Money<Eur>, Money<Jpy>, … all now use MoneyConverter<>.
```

This is the same machinery the built-in nullable, enum, collection, and
dictionary converters use.

## Pattern 2 — a tagged (discriminated) hierarchy

The richer case is a base type whose concrete shape is chosen by a
discriminator field. Model the family as a closed set of derived types and
a `"kind"` tag:

```csharp
public abstract class Shape
{
    public string Kind { get; init; } = "";
}

public sealed class Circle : Shape
{
    public double Radius { get; init; }
}

public sealed class Rectangle : Shape
{
    public double Width { get; init; }
    public double Height { get; init; }
}
```

A factory matches the *base* type (and, optionally, its subclasses) and
hands back a single converter that knows the tag-to-type mapping:

```csharp
using Bodu.Text.Toml.Serialization;

public sealed class ShapeConverterFactory : TomlConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeof(Shape).IsAssignableFrom(typeToConvert);

    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options) =>
        new ShapeConverter();
}
```

`CanConvert` returns `true` for `Shape` itself and for any `Circle` /
`Rectangle` member declared as the base type, so a property typed
`Shape Outline { get; set; }` routes through the factory regardless of
which concrete value it currently holds.

## Pattern 3 — reading and writing the discriminator

The concrete converter is a `TomlConverter<Shape>`. On entry to `Read` the
reader is positioned on the table's `StartTable` token; the converter walks
the normalized token stream — `PropertyName` followed by the value's token,
through to the matching `EndTable` — collecting the discriminator and the
payload fields, then constructs the matching derived type:

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

public sealed class ShapeConverter : TomlConverter<Shape>
{
    public override Shape Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.StartTable)
            throw new TomlSerializationException($"Expected a table but found '{reader.TokenType}'.");

        string? kind = null;
        double radius = 0, width = 0, height = 0;

        // Walk PropertyName/value pairs until the matching EndTable.
        while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
        {
            string name = reader.GetString();   // the PropertyName token
            reader.Read();                       // advance onto the value

            switch (name)
            {
                case "kind":   kind = reader.GetString(); break;
                case "radius": radius = reader.GetDouble(); break;
                case "width":  width = reader.GetDouble(); break;
                case "height": height = reader.GetDouble(); break;
                default:       reader.Skip(); break;   // ignore unknown members
            }
        }

        return kind switch
        {
            "circle"    => new Circle { Kind = kind, Radius = radius },
            "rectangle" => new Rectangle { Kind = kind, Width = width, Height = height },
            null        => throw new TomlSerializationException("Shape is missing the 'kind' discriminator."),
            _           => throw new TomlSerializationException($"Unknown shape kind '{kind}'."),
        };
    }

    public override void Write(Utf8TomlWriter writer, Shape value, TomlSerializerOptions options)
    {
        // Dispatch on the runtime type so the right payload — including the tag — is written.
        switch (value)
        {
            case Circle c:
                writer.WriteStartTable();
                writer.WritePropertyName("kind");   writer.WriteString("circle");
                writer.WritePropertyName("radius"); writer.WriteFloat(c.Radius);
                writer.WriteEndTable();
                break;

            case Rectangle r:
                writer.WriteStartTable();
                writer.WritePropertyName("kind");   writer.WriteString("rectangle");
                writer.WritePropertyName("width");  writer.WriteFloat(r.Width);
                writer.WritePropertyName("height"); writer.WriteFloat(r.Height);
                writer.WriteEndTable();
                break;

            default:
                throw new TomlSerializationException($"Unsupported shape '{value.GetType()}'.");
        }
    }
}
```

The key moves:

- **Walk the normalized token stream.** `TomlDocumentReader` projects the
  document onto a uniform `StartTable` / `PropertyName` / value /
  `EndTable` sequence regardless of how the source spelled the table, so a
  single read loop handles inline and header-defined tables alike. Call
  `Skip()` to step over an unknown member's whole value, including nested
  tables and arrays.
- **Dispatch on the runtime type when writing.** The `switch` over the
  concrete subtype emits the discriminator plus exactly that type's
  payload, so the value round-trips back through `Read`.
- **Fail with the serialization exception.** A missing or unknown tag is a
  *well-formed value that does not fit*, so throw
  <xref:Bodu.Text.Toml.TomlSerializationException> — the same family the
  built-in converters throw — not the parse exception, which is reserved
  for syntactically invalid documents.

## Registration and resolution order

A factory occupies the same slots and obeys the same precedence as a
single-type converter (see [Writing converters](converters.md) Pattern 3),
highest first:

1. a member-level converter attribute;
2. a type-level converter attribute;
3. the first matching entry in `options.Converters`;
4. the built-in converters.

For a *family*, the natural placements are a type-level attribute on the
base type, or a single registration on the options:

```csharp
// Option A — annotate the base type so every Shape member uses the factory.
[TomlConverter(typeof(ShapeConverterFactory))]
public abstract class Shape { /* … */ }

// Option B — register once on the options.
var options = new TomlSerializerOptions();
options.Converters.Add(new ShapeConverterFactory());
```

Three ordering consequences are worth keeping in mind:

- **The first match wins, and order in `options.Converters` matters.** When
  more than one factory could claim a type, the earlier registration is
  asked first. Register the most specific factory ahead of any broader one
  whose `CanConvert` would also return `true`.
- **`CreateConverter` runs once per closed type and is cached.** A factory
  matching an open generic produces one converter per closed type
  (`Money<Usd>`, `Money<Eur>`, …), each cached independently on the
  options.
- **Register before first use.** As with all converters, a
  `…SerializerOptions` instance freezes the first time it is used (or via
  `MakeReadOnly()`); add factories before then, and reuse one options
  instance so the resolution and reflection work is paid once.

## See also

- [Writing converters](converters.md) — the single-type `Read` / `Write` pattern, the precedence ladder, and converter statelessness.
- [Built-in converter catalog](builtin-converters.md) — the families that already have a factory (nullable, enum, collection, dictionary) and their wire forms.
- [Mapping attributes](attributes.md) — `[…Converter]` placement and the precedence ladder in detail.
- API reference — <xref:Bodu.Text.Toml.Serialization.TomlConverterFactory>, <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory>, <xref:Bodu.Text.Toml.Serialization.TomlConverter`1>, <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic.
