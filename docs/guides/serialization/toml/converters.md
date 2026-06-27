---
title: Writing converters
---

# Writing converters

A converter customises how a single type is read and written: derive `TomlConverter<T>` (<xref:Bodu.Text.Toml.Serialization.TomlConverter`1>) and read or write values through the format's reader and writer. A TOML converter reads through <xref:Bodu.Text.Toml.Reader.TomlDocumentReader> and writes through <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter>.

Because the library is self-contained, a converter is written against the TOML reader/writer pair. The sibling libraries ([Bodu.Text.Bencode](../bencode/index.md), [Bodu.Text.Yaml](../yaml/index.md)) follow the identical pattern with their own prefix and reader/writer types. The set of converters the library already ships — and therefore the types you never need to write one for — is listed in the [built-in converter catalog](builtin-converters.md).

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

1. a member-level converter attribute (`[TomlConverter(...)]`);
2. a type-level converter attribute;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options.

## Pattern 4 — Serve a family of types

To convert an open generic (say, every `Money<TCurrency>`), derive `TomlConverterFactory` (<xref:Bodu.Text.Toml.Serialization.TomlConverterFactory>), return `true` from `CanConvert` for the family, and build the concrete converter in `CreateConverter`. This is the same pattern the built-in nullable, enum, collection, and dictionary converters use.

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

The factory itself never reads or writes a value: the serializer calls `CanConvert` to decide whether the factory applies, then `CreateConverter` once per closed type and caches the result. `MoneyConverter<T>` here is an ordinary `TomlConverter<Money<T>>` written as in Pattern 1. The factory pattern is covered in full in [Polymorphic converters](polymorphic-converters.md).

## Pattern 5 — Map a type that needs a non-default shape

TOML has a native mapping for every common BCL scalar — strings, integers, floats, Booleans, the four date-time forms, plus `decimal`, `TimeSpan`, `Guid`, `Uri`, and `Version` (see the [built-in converter catalog](builtin-converters.md)). A converter is what you reach for when a *custom* type, or a non-default representation of an existing one, needs a wire form the defaults do not provide — for example writing an opaque identifier as a single string rather than the object table it would otherwise produce:

```csharp
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

public sealed class OrderIdConverter : TomlConverter<OrderId>
{
    public override OrderId Read(ref TomlDocumentReader reader, Type t, TomlSerializerOptions o) =>
        OrderId.Parse(reader.GetString());

    public override void Write(Utf8TomlWriter writer, OrderId value, TomlSerializerOptions o) =>
        writer.WriteString(value.ToString());
}
```

## Built-in enum converters

For enums you usually do not need a hand-written converter. The library ships a string-enum converter (member names) and a number-enum converter; reference them from a `[TomlConverter]` attribute on a member, property, or the enumeration itself, or register one on the options.

On the enumeration itself, use the generic string-enum form (<xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter`1>), optionally renaming individual members:

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

On a single member, the generic number-enum form (<xref:Bodu.Text.Toml.Serialization.TomlNumberEnumConverter`1>) writes the underlying value instead:

```csharp
public sealed class WorkItem
{
    [TomlConverter(typeof(TomlNumberEnumConverter<Priority>))]
    public Priority Priority { get; set; }
}

// Priority.High (underlying value 2) serializes as: Priority = 2
```

To cover *every* enumeration in one registration, add the non-generic string-enum factory (<xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter>) to the options, optionally with a naming policy:

```csharp
var options = new TomlSerializerOptions();
options.Converters.Add(new TomlStringEnumConverter(TomlNamingPolicy.SnakeCaseLower, allowIntegerValues: false));

// Status.OnHold now serializes as "on_hold" everywhere.
```

The generic forms expose a public parameterless constructor, which is what makes them usable from a `[TomlConverter]` attribute; the non-generic factory is the options-level, all-enums form. There is no non-generic number-enum converter.

## Pattern 6 — Fail clearly on malformed data

By the time `Read` runs, the document has already parsed — a syntactically malformed document raises <xref:Bodu.Text.Toml.TomlFormatException> before any converter is consulted. What a converter must handle is a *well-formed value that does not fit*: the wrong kind, or text that does not parse into the target type. Signal that by throwing <xref:Bodu.Text.Toml.TomlSerializationException> — the same type the built-in converters throw, so callers need one catch clause regardless of which converter rejected the value. Hardening the `PointConverter` from Pattern 1:

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

## Error recovery and partial deserialization

Pattern 6 shows the strict default: a value that does not fit throws the
serialization exception. Some inputs are better served by recovering — an
optional field that may be absent, a feed where one bad record should not abort
the batch, a UI that wants to report *every* problem at once. A converter is the
right place to encode that policy, because it sees the raw token before any typed
getter runs. The strategies below all live inside `Read`.

**Validate the token kind before reading.** The first defence is to check
`reader.TokenType` against the kind you expect (<xref:Bodu.Text.Toml.TomlTokenType>)
and branch instead of letting a typed getter throw an opaque exception. This is
what turns "wrong type" into a decision point rather than a failure.

**Default on a missing or wrong-kind value.** When a field is optional, return a
fallback instead of throwing — useful for forward-compatible schemas where older
documents simply omit a key:

```csharp
public override TimeSpan Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
{
    // A non-string value (or an absent one the caller mapped to a default) yields Zero
    // rather than aborting the whole object graph.
    if (reader.TokenType != TomlTokenType.String)
        return TimeSpan.Zero;

    return TimeSpan.TryParse(reader.GetString(), CultureInfo.InvariantCulture, out TimeSpan value)
        ? value
        : TimeSpan.Zero;
}
```

**Partial object construction.** A converter for a composite type can read the
members it understands, skip what it does not, and return a usable instance —
trading completeness for resilience. Read field by field, and substitute a
default for any member that fails to parse rather than propagating the failure.

**Collect multiple errors instead of failing fast.** To surface every problem in
one pass (IDE-style validation), have the converter append to a shared sink kept
in a `readonly` field set at construction, then return a sentinel:

```csharp
public sealed class CollectingPointConverter(List<string> errors) : TomlConverter<Point>
{
    private readonly List<string> _errors = errors;

    public override Point Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.String)
        {
            _errors.Add($"Expected a string for Point but found '{reader.TokenType}'.");
            return default;
        }

        string text = reader.GetString();
        string[] parts = text.Split(',');
        if (parts.Length != 2
            || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out int x)
            || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out int y))
        {
            _errors.Add($"'{text}' is not a valid Point ('x,y').");
            return default;
        }

        return new Point(x, y);
    }

    public override void Write(Utf8TomlWriter writer, Point value, TomlSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

The error list is shared, unsynchronized state, so this pattern is for a
single-threaded validation pass over one options instance, not concurrent
deserialization. After the call returns, inspect `errors` to decide whether the
result is trustworthy.

**Where the two exception families fit.** Recovery only applies to *well-formed*
documents — a syntactically broken document raises the format exception
(<xref:Bodu.Text.Toml.TomlFormatException>) during parsing, before any converter
runs, and no converter can intercept it. Once `Read` is executing, the document
parsed; from there you choose between recovering (default, partial, or collect)
and rejecting with the serialization exception
(<xref:Bodu.Text.Toml.TomlSerializationException>). Reserve the format exception
for the parser; never throw it from a converter.

## Design notes — statelessness and caching

**Write converters stateless.** The serializer resolves the converter for a type once, caches the result on the options instance, and reuses that single converter instance for every subsequent value of the type — across calls and across threads. Instance fields mutated during `Read` or `Write` are therefore shared, unsynchronized state. Keep configuration in `readonly` fields set at construction (the way the built-in string-enum converter takes its naming policy), and derive everything else from the arguments the serializer passes in.

**Options freeze on first use.** As described in [core concepts](../../../docs/serialization/toml/concepts.md), a `TomlSerializerOptions` instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` — and from then on caches its resolved converters and type metadata; later mutation of `Converters` is rejected. Two practical consequences:

- **Reuse one options instance.** The expensive work — reflection over your types, converter resolution — happens once per options instance. Constructing fresh options per call discards the caches and repeats it.
- **Register before first use.** Converter changes after the options have been used (or frozen) throw; the resolution order in Pattern 3 is evaluated against the converter list as it stood when the type was first seen.

## See also

- [Built-in converter catalog](builtin-converters.md) — the types that already have a converter, and their exact wire forms.
- [Polymorphic converters](polymorphic-converters.md) — the factory pattern for open generics and discriminated hierarchies.
- [Mapping attributes](attributes.md) — declarative shaping; `[TomlConverter]` placement and the precedence ladder.
- [Using TOML](using.md) — the format walk-through, including the error-handling pattern.
- [Core concepts](../../../docs/serialization/toml/concepts.md) — converter resolution and options caching in the family vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) and the [topic overview](../../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Toml.Serialization.TomlConverter`1>, <xref:Bodu.Text.Toml.Serialization.TomlConverterFactory>.
