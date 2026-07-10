---
title: Writing converters
---

# Writing converters

A converter customises how a single type is read and written: derive `BencodeConverter<T>` (<xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>), reading through <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and writing through <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>. The set of converters the library already ships — and therefore the types you never need to write one for — is listed in the [built-in converter catalog](builtin-converters.md).

The sibling [TOML](../toml/index.md) and [YAML](../yaml/index.md) serializers follow the identical pattern against their own reader/writer pair; only the prefix differs.

## Pattern 1 — Convert a value type

A converter that stores a `Point` as an `"x,y"` byte string:

```csharp
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

public sealed class PointConverter : BencodeConverter<Point>
{
    public override Point Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        string[] parts = reader.GetString().Split(',');
        return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public override void Write(Utf8BencodeWriter writer, Point value, BencodeSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

On entry to `Read`, the reader is already positioned on the value's first token; `Write` is called with the writer positioned to emit a value. A converter for a *structural* type (a list or dictionary) walks or emits the framing tokens itself — see [Polymorphic converters](polymorphic-converters.md) for the dictionary case. The members each side exposes:

| <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> | Purpose |
|---|---|
| `TokenType` | The current token's <xref:Bodu.Text.Bencode.BencodeTokenType> — check it before calling a typed getter. |
| `GetString()` / `GetBytes()` | The current byte-string (or property-name) token as UTF-8 text or raw bytes. |
| `GetInt32()` / `GetInt64()` / `GetUInt64()` and the `TryGet…` overloads | The current integer token, range-checked; the `TryGet…` forms return `false` rather than throwing. |
| `Read()` / `Skip()` | Advance to the next token; step over the current value including a nested subtree. |

| <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> | Purpose |
|---|---|
| `WriteInteger(long)` / `WriteInteger(ulong)` | Emit an integer (`i…e`). |
| `WriteString(string)` / `WriteByteString(ReadOnlySpan<byte>)` | Emit a byte string — UTF-8 text or raw bytes. |
| `WriteStartList()` / `WriteEndList()`, `WriteStartDictionary()` / `WriteEndDictionary()`, `WritePropertyName(…)` | Frame a structural value and its keys; the writer re-sorts dictionary keys into canonical order on close. |

## Pattern 2 — Register it

Two ways, highest precedence first:

```csharp
// On a member or type:
public sealed class Shape
{
    [BencodeConverter(typeof(PointConverter))]
    public Point Origin { get; set; }
}

// Or on the options:
var options = new BencodeSerializerOptions();
options.Converters.Add(new PointConverter());
```

## Pattern 3 — Understand resolution order

For a given type the serializer selects a converter by checking, in order:

1. a member-level converter attribute (`[BencodeConverter(...)]`);
2. a type-level converter attribute;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options.

## Pattern 4 — Serve a family of types

To convert an open generic (say, every `Money<TCurrency>`), derive `BencodeConverterFactory` (<xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory>), return `true` from `CanConvert` for the family, and build the concrete converter in `CreateConverter`. This is the same pattern the built-in nullable, enum, collection, and dictionary converters use.

```csharp
public sealed class MoneyConverterFactory : BencodeConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Money<>);

    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options) =>
        (BencodeConverter)Activator.CreateInstance(
            typeof(MoneyConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}
```

The factory itself never reads or writes a value: the serializer calls `CanConvert` to decide whether the factory applies, then `CreateConverter` once per closed type and caches the result. `MoneyConverter<T>` here is an ordinary `BencodeConverter<Money<T>>` written as in Pattern 1. The polymorphic factory pattern — including tagged hierarchies — is covered in [Polymorphic converters](polymorphic-converters.md).

## Pattern 5 — Map a type the format cannot represent

Bencode has exactly two scalar kinds — integers and byte strings — so several BCL types have no native form and are rejected unless a converter maps them: Booleans, floating-point types, `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, and the date-time types. By design, the library never invents a lossy representation implicitly. A converter bridges the gap — for example, writing a `bool` as a Bencode integer:

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

For enums you usually do not need a hand-written converter. The library ships a string-enum converter (member names) and a number-enum converter; reference them from a `[BencodeConverter]` attribute on a member, property, or the enumeration itself, or register one on the options.

On the enumeration itself, use the generic string-enum form (<xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter`1>), optionally renaming individual members:

```csharp
[BencodeConverter(typeof(BencodeStringEnumConverter<Status>))]
public enum Status
{
    Active,

    [BencodeStringEnumMemberName("on-hold")]
    OnHold,
}

// Status.OnHold serializes as the byte string 7:on-hold
```

On a single member, the generic number-enum form (<xref:Bodu.Text.Bencode.Serialization.BencodeNumberEnumConverter`1>) writes the underlying value instead:

```csharp
public sealed class WorkItem
{
    [BencodeConverter(typeof(BencodeNumberEnumConverter<Priority>))]
    public Priority Priority { get; set; }
}

// Priority.High (underlying value 2) serializes as the integer i2e
```

To cover *every* enumeration in one registration, add the non-generic string-enum factory (<xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter>) to the options, optionally with a naming policy:

<!-- compile -->
```csharp
var options = new BencodeSerializerOptions();
options.Converters.Add(new BencodeStringEnumConverter(BencodeNamingPolicy.SnakeCaseLower, allowIntegerValues: false));

// Status.OnHold now serializes as the byte string "on_hold" everywhere.
```

The generic forms expose a public parameterless constructor, which is what makes them usable from a `[BencodeConverter]` attribute; the non-generic factory is the options-level, all-enums form. There is no non-generic number-enum converter.

## Pattern 6 — Fail clearly on malformed data

By the time `Read` runs, the document has already parsed — a syntactically malformed document raises <xref:Bodu.Text.Bencode.BencodeFormatException> before any converter is consulted. What a converter must handle is a *well-formed value that does not fit*: the wrong kind, or text that does not parse into the target type. Signal that by throwing <xref:Bodu.Text.Bencode.BencodeSerializationException> — the same type the built-in converters throw, so callers need one catch clause regardless of which converter rejected the value. Hardening the `PointConverter` from Pattern 1:

```csharp
public override Point Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
{
    if (reader.TokenType != BencodeTokenType.ByteString)
        throw new BencodeSerializationException($"Expected a byte string but found '{reader.TokenType}'.");

    string text = reader.GetString();
    string[] parts = text.Split(',');

    if (parts.Length != 2
        || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out int x)
        || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out int y))
    {
        throw new BencodeSerializationException($"The value '{text}' is not a valid Point ('x,y').");
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
`reader.TokenType` against the kind you expect (<xref:Bodu.Text.Bencode.BencodeTokenType>)
and branch instead of letting a typed getter throw an opaque exception. This is
what turns "wrong type" into a decision point rather than a failure.

**Default on a missing or wrong-kind value.** When a field is optional, return a
fallback instead of throwing — useful for forward-compatible schemas where older
documents simply omit a key:

```csharp
public override TimeSpan Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
{
    // A non-byte-string value (or an absent one the caller mapped to a default) yields Zero
    // rather than aborting the whole object graph.
    if (reader.TokenType != BencodeTokenType.ByteString)
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
public sealed class CollectingPointConverter(List<string> errors) : BencodeConverter<Point>
{
    private readonly List<string> _errors = errors;

    public override Point Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.ByteString)
        {
            _errors.Add($"Expected a byte string for Point but found '{reader.TokenType}'.");
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

    public override void Write(Utf8BencodeWriter writer, Point value, BencodeSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

The error list is shared, unsynchronized state, so this pattern is for a
single-threaded validation pass over one options instance, not concurrent
deserialization. After the call returns, inspect `errors` to decide whether the
result is trustworthy.

**Where the two exception families fit.** Recovery only applies to *well-formed*
documents — a syntactically broken document raises the format exception
(<xref:Bodu.Text.Bencode.BencodeFormatException>) during parsing, before any
converter runs, and no converter can intercept it. Once `Read` is executing, the
document parsed; from there you choose between recovering (default, partial, or
collect) and rejecting with the serialization exception
(<xref:Bodu.Text.Bencode.BencodeSerializationException>). Reserve the format
exception for the parser; never throw it from a converter.

## Design notes — statelessness and caching

**Write converters stateless.** The serializer resolves the converter for a type once, caches the result on the options instance, and reuses that single converter instance for every subsequent value of the type — across calls and across threads. Instance fields mutated during `Read` or `Write` are therefore shared, unsynchronized state. Keep configuration in `readonly` fields set at construction (the way the built-in string-enum converter takes its naming policy), and derive everything else from the arguments the serializer passes in.

**Options freeze on first use.** As described in [core concepts](../../../docs/serialization/bencode/concepts.md), a `BencodeSerializerOptions` instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` — and from then on caches its resolved converters and type metadata; later mutation of `Converters` is rejected. Two practical consequences:

- **Reuse one options instance.** The expensive work — reflection over your types, converter resolution — happens once per options instance. Constructing fresh options per call discards the caches and repeats it.
- **Register before first use.** Converter changes after the options have been used (or frozen) throw; the resolution order in Pattern 3 is evaluated against the converter list as it stood when the type was first seen.

## See also

- [Polymorphic converters](polymorphic-converters.md) — converter factories for open-generic families and tagged hierarchies.
- [Built-in converter catalog](builtin-converters.md) — the types that already have a converter, and their exact wire forms.
- [Mapping attributes](attributes.md) — declarative shaping; `[BencodeConverter]` placement and the precedence ladder.
- [Using Bencode](using.md) — the format walk-through, including the error-handling pattern.
- [Core concepts](../../../docs/serialization/bencode/concepts.md) — converter resolution and options caching in the Bencode vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) and the [topic overview](../../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>, <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory>.
