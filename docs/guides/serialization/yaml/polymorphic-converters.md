---
title: Polymorphic converters
---

# Polymorphic converters

A converter factory dispatches over a *family* of types rather than a single type. Where a [hand-written converter](converters.md) handles one fixed `T`, a `YamlConverterFactory` (<xref:Bodu.Text.Yaml.Serialization.YamlConverterFactory>) decides *at resolution time* whether it applies to a requested type and builds the right concrete converter for it. This is the mechanism behind two common shapes:

- **Open-generic families** — every closed `Money<TCurrency>`, every `Stack<T>`, where one factory serves an unbounded set of closed types.
- **Tagged (discriminated) hierarchies** — a base type with a `kind` key whose value selects which derived type to materialize.

This guide builds on [Writing converters](converters.md); read that first for the single-type `Read` / `Write` pattern, the precedence ladder, and the statelessness rules. Everything here is YAML; the sibling libraries ([Bodu.Text.Toml](../toml/index.md), [Bodu.Text.Bencode](../bencode/index.md)) follow the identical shape against their own reader/writer pair.

## What YAML does without a factory

Before writing one, it is worth knowing how far the built-ins go. The serializer already dispatches on the **runtime** type when writing, so a `Circle` held in a `Shape`-typed member serializes with `Circle`'s members — but with no discriminator, because nothing knows the family exists:

```csharp
public abstract class Shape
{
    public string Kind { get; init; } = "";
}

public sealed class Circle : Shape
{
    public double Radius { get; init; }
}

public sealed class Drawing
{
    public string Title { get; set; } = "";
    public Shape? Outline { get; set; }
    public List<Shape> Shapes { get; set; } = [];
}
```

```csharp
string plainYaml = YamlSerializer.Serialize(new Drawing { Title = "t", Outline = new Circle { Kind = "circle", Radius = 2 } });
// Title: t
// Outline:
//   Radius: 2.0
//   Kind: circle
// Shapes: []

YamlSerializer.Deserialize<Drawing>(plainYaml);
// → throws YamlSerializationException: The type 'Shape' could not be instantiated.
```

Reading is where the built-ins stop: a member declared as an abstract class or interface is served by a built-in converter that accepts only a null scalar and otherwise throws, because it has no way to choose a concrete type. A factory closes that gap.

## How a factory participates in resolution

A factory derives from `YamlConverterFactory` and overrides two methods:

```csharp
public abstract bool CanConvert(Type typeToConvert);
public abstract YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options);
```

The serializer treats the factory exactly like any other converter in the [resolution order](converters.md): member attribute, then type attribute, then `options.Converters`, then built-ins. When the candidate is a factory it calls `CanConvert(type)`; on `true` it calls `CreateConverter(type, options)` **once per closed type**, verifies that the returned converter's own `CanConvert` accepts the type, and caches the result. The factory itself never reads or writes a value — `CreateConverter` returns an ordinary `YamlConverter<T>` that does the work.

## Pattern 1 — an open-generic family

To serve every closed `Money<TCurrency>` from one registration, match the open generic in `CanConvert` and close `MoneyConverter<>` over the requested type argument in `CreateConverter`:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Serialization;

public sealed class MoneyConverterFactory : YamlConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(Money<>);

    public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options) =>
        (YamlConverter)Activator.CreateInstance(
            typeof(MoneyConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}
```

`MoneyConverter<TCurrency>` is an ordinary `YamlConverter<Money<TCurrency>>` written as in [Writing converters](converters.md) Pattern 1 — here it stores the amount as a fixed-point string scalar:

```csharp
public sealed class MoneyConverter<TCurrency> : YamlConverter<Money<TCurrency>>
{
    public override Money<TCurrency> Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        new(decimal.Parse(reader.GetString(), CultureInfo.InvariantCulture));

    public override void Write(Utf8YamlWriter writer, Money<TCurrency> value, YamlSerializerOptions options) =>
        writer.WriteString(value.Amount.ToString("0.00", CultureInfo.InvariantCulture));
}
```

Register the factory once on the options and every closed `Money<TCurrency>` resolves through it:

```csharp
var options = new YamlSerializerOptions();
options.Converters.Add(new MoneyConverterFactory());
// Money<Usd>, Money<Eur>, Money<Jpy>, … all now use MoneyConverter<>.

string w = YamlSerializer.Serialize(new Wallet { Cash = new(12.5m), Savings = new(1000m) }, options);
// Cash: "12.50"
// Savings: "1000.00"
```

The writer quotes the scalars because they would otherwise resolve as floats on the way back in — YAML's implicit typing is why a string-shaped wire form needs the quotes, and `WriteString` adds them for you. This is the same machinery the built-in nullable, enum, collection, and dictionary converters use.

## Pattern 2 — a tagged (discriminated) hierarchy

The richer case is a base type whose concrete shape is chosen by a discriminator key. Model the family as a closed set of derived types and a `kind` tag — the `Shape` / `Circle` pair above, plus a second subtype:

```csharp
public sealed class Rectangle : Shape
{
    public double Width { get; init; }
    public double Height { get; init; }
}
```

A factory matches the *base* type (and its subclasses) and hands back a single converter that knows the tag-to-type mapping:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Serialization;

public sealed class ShapeConverterFactory : YamlConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeof(Shape).IsAssignableFrom(typeToConvert);

    public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options) =>
        new ShapeConverter();
}
```

`CanConvert` returns `true` for `Shape` itself and for any `Circle` / `Rectangle` member declared as the base type, so a property typed `Shape Outline { get; set; }` routes through the factory regardless of which concrete value it currently holds.

## Pattern 3 — reading and writing the discriminator

The concrete converter is a `YamlConverter<Shape>`. On entry to `Read` the reader is positioned on the mapping's `StartMapping` token; the converter walks the composed token stream — a key scalar followed by the value's token, through to the matching `EndMapping` — collecting the discriminator and the payload fields, then constructs the matching derived type:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

public sealed class ShapeConverter : YamlConverter<Shape>
{
    public override bool CanConvert(Type typeToConvert) =>
        typeof(Shape).IsAssignableFrom(typeToConvert);

    public override Shape Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType != YamlTokenType.StartMapping)
            throw new YamlSerializationException($"Expected a mapping but found '{reader.TokenType}'.");

        string? kind = null;
        double radius = 0, width = 0, height = 0;

        // Walk key/value pairs until the matching EndMapping.
        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string name = reader.GetString();   // the key
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
            null        => throw new YamlSerializationException("Shape is missing the 'kind' discriminator."),
            _           => throw new YamlSerializationException($"Unknown shape kind '{kind}'."),
        };
    }

    public override void Write(Utf8YamlWriter writer, Shape value, YamlSerializerOptions options)
    {
        // Dispatch on the runtime type so the right payload — including the tag — is written.
        switch (value)
        {
            case Circle c:
                writer.WriteStartMapping();
                writer.WritePropertyName("kind");   writer.WriteString("circle");
                writer.WritePropertyName("radius"); writer.WriteDouble(c.Radius);
                writer.WriteEndMapping();
                break;

            case Rectangle r:
                writer.WriteStartMapping();
                writer.WritePropertyName("kind");   writer.WriteString("rectangle");
                writer.WritePropertyName("width");  writer.WriteDouble(r.Width);
                writer.WritePropertyName("height"); writer.WriteDouble(r.Height);
                writer.WriteEndMapping();
                break;

            default:
                throw new YamlSerializationException($"Unsupported shape '{value.GetType()}'.");
        }
    }
}
```

The key moves:

- **Override `CanConvert` on the converter too.** After a factory creates a converter the serializer verifies that the *converter* accepts the requested type. `YamlConverter<Shape>.CanConvert` defaults to an exact match, which would reject `Circle` when the factory is asked for a member declared as the subtype; widening it to `IsAssignableFrom` keeps the factory and its product in agreement.
- **Walk the composed token stream.** `Utf8YamlReader` presents an already-composed tree — anchors, aliases, and merge keys are resolved before the first token is delivered, and a block mapping and a flow mapping produce the same `StartMapping` / key / value / `EndMapping` sequence — so a single read loop handles every spelling. Call `Skip()` to step over an unknown member's whole value, including nested mappings and sequences.
- **Dispatch on the runtime type when writing.** The `switch` over the concrete subtype emits the discriminator plus exactly that type's payload, so the value round-trips back through `Read`.
- **Fail with the serialization exception.** A missing or unknown tag is a *well-formed value that does not fit*, so throw <xref:Bodu.Text.Yaml.YamlSerializationException> — the same family the built-in converters throw — not <xref:Bodu.Text.Yaml.YamlFormatException>, which is reserved for syntactically invalid documents.

With the factory registered, a document holding a base-typed member and a base-typed list carries the tag everywhere:

```csharp
var shapeOptions = new YamlSerializerOptions();
shapeOptions.Converters.Add(new ShapeConverterFactory());

var drawing = new Drawing
{
    Title = "Logo",
    Outline = new Rectangle { Kind = "rectangle", Width = 10, Height = 4 },
    Shapes = [new Circle { Kind = "circle", Radius = 1.5 }, new Rectangle { Kind = "rectangle", Width = 2, Height = 3 }],
};

string yaml = YamlSerializer.Serialize(drawing, shapeOptions);
// Title: Logo
// Outline:
//   kind: rectangle
//   width: 10.0
//   height: 4.0
// Shapes:
//   -
//     kind: circle
//     radius: 1.5
//   -
//     kind: rectangle
//     width: 2.0
//     height: 3.0

Drawing round = YamlSerializer.Deserialize<Drawing>(yaml, shapeOptions)!;
// round.Outline is Rectangle; round.Shapes[0] is Circle { Radius = 1.5 }
```

And the failure paths surface as the serialization exception with the message the converter chose:

```csharp
YamlSerializer.Deserialize<Drawing>("Outline:\n  radius: 1", shapeOptions);
// → throws YamlSerializationException: Shape is missing the 'kind' discriminator.

YamlSerializer.Deserialize<Drawing>("Outline:\n  kind: hexagon", shapeOptions);
// → throws YamlSerializationException: Unknown shape kind 'hexagon'.

YamlSerializer.Deserialize<Drawing>("Outline: circle", shapeOptions);
// → throws YamlSerializationException: Expected a mapping but found 'String'.
```

## Registration and resolution order

A factory occupies the same slots and obeys the same precedence as a single-type converter (see [Writing converters](converters.md) Pattern 3), highest first:

1. a member-level converter attribute;
2. a type-level converter attribute;
3. the first matching entry in `options.Converters`;
4. the built-in converters.

For a *family*, the natural placements are a type-level attribute on the base type, or a single registration on the options:

```csharp
// Option A — annotate the base type so every Shape-typed member uses the factory.
[Converter(typeof(ShapeConverterFactory))]
public abstract class Shape { /* … */ }

// Option B — register once on the options.
var options = new YamlSerializerOptions();
options.Converters.Add(new ShapeConverterFactory());
```

The two placements are not equivalent. The type-level attribute is looked up on the *declared* type of a member and is not inherited, so it governs members declared as `Shape` — a member declared as `Circle` resolves `Circle`'s own converter, which is the plain object mapping unless `Circle` carries its own attribute:

```csharp
public sealed class Holder
{
    public Shape? Outline { get; set; }   // declared as the base → factory → tagged
    public Circle? Exact { get; set; }    // declared as the subtype → object mapping → untagged
}
```

```yaml
Outline:
  kind: circle
  radius: 1.0
Exact:
  Radius: 2.0
  Kind: ""
```

The options registration, by contrast, is consulted for every type the factory's `CanConvert` accepts, so a `List<Circle>` also serializes with the tag. Prefer Option B when subtype-declared members must share the wire form; prefer Option A when the family is small and you want the wiring to travel with the type.

Three ordering consequences are worth keeping in mind:

- **The first match wins, and order in `options.Converters` matters.** When more than one factory could claim a type, the earlier registration is asked first. Register the most specific factory ahead of any broader one whose `CanConvert` would also return `true`.
- **`CreateConverter` runs once per closed type and is cached.** A factory matching an open generic produces one converter per closed type (`Money<Usd>`, `Money<Eur>`, …), each cached independently on the options.
- **Register before first use.** As with all converters, a `YamlSerializerOptions` instance freezes the first time it is used (or via `MakeReadOnly()`); add factories before then, and reuse one options instance so the resolution and reflection work is paid once — see [Serializer options: freezing, caching, and thread safety](../options-and-lifetime.md).

## Where to go next

- [Writing converters](converters.md) — the single-type `Read` / `Write` pattern, the precedence ladder, and converter statelessness.
- [Built-in converter catalog](builtin-converters.md) — the families that already have a factory (nullable, enum, collection, dictionary) and their wire forms.
- [Mapping attributes](attributes.md) — `[Converter]` placement and the precedence ladder in detail.
- [Serialization callbacks](callbacks.md) — the lifecycle hooks a converter-handled type gives up, and how to keep them on the members instead.
- The sibling guides — [TOML polymorphic converters](../toml/polymorphic-converters.md) and [Bencode polymorphic converters](../bencode/polymorphic-converters.md).
- API reference — <xref:Bodu.Text.Yaml.Serialization.YamlConverterFactory>, <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1>, <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader>, <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
