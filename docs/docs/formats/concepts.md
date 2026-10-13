---
title: Line formats - Core concepts
---

# Line formats - Core concepts

The vocabulary shared by `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, and `Bodu.Text.Ini`. It is the same vocabulary as the structured-text quartet (Bencode / TOML / YAML); this page notes where the line formats specialize it.

## Token reader and writer

The `Utf8*Reader` types are forward-only `ref struct` cursors over `ReadOnlySpan<byte>`: call `Read()` to advance, inspect `TokenType`, and decode text with `GetString()`. They live on the stack, allocate only for decoded strings, and report source positions (`LineNumber`, `BytesConsumed`) for diagnostics. The `Utf8*Writer` types emit UTF-8 to an `IBufferWriter<byte>` or a `Stream` (buffered via an internal scratch writer until `Flush`).

Delimited and DotEnv are pure source-order token streams. INI has **two** readers: the source-order `Utf8IniReader` (section headers, keys, values, comments, exactly as authored) and the normalized `IniDocumentReader`, which pre-parses the document so that duplicate-section **merge** - structure declared out of source order - can surface as a single `StartObject … EndObject` per section.

## Value model

- **Delimited** is an *array of records*. With a header row, each record is an object keyed by column name; in headerless (positional) mode, each record is an array of strings.
- **DotEnv** is a single flat object of string-valued keys.
- **INI** is a two-level object-of-objects: global keys hoist onto the root, and each `[section]` is a nested object of string values. Nothing nests deeper.

## Serializer

Each `*Serializer` is a static facade shaped after `JsonSerializer`: `Serialize` / `Deserialize` over strings, UTF-8 spans, buffer writers, and streams, with buffered `SerializeAsync` overloads and, for DotEnv and INI, buffered `DeserializeAsync` overloads (only the stream copy is asynchronous); Delimited's asynchronous read is the incremental `DeserializeAsyncEnumerableAsync` below. Binding honours the shared `Bodu.Text.Serialization` layer - the attribute family (`[PropertyName]`, `[Ignore]`, `[Required]`, `[PropertyOrder]`), `NamingPolicy` (camelCase, snake_case, SCREAMING_SNAKE_CASE, kebab-case), and the `IOnSerializing` / `IOnSerialized` / `IOnDeserializing` / `IOnDeserialized` callbacks - except that Delimited reads neither `[Required]` nor a conditional `[Ignore]`: a missing column leaves its member at its default, and only `[Ignore]` with `IgnoreCondition.Always` excludes a member. DotEnv and INI treat a member declared with the C# `required` keyword as one marked `[Required]`. In all three, each member name maps once, to its most derived declaration, so a property or field hidden with `new` is neither written nor read, and a type that maps two members to one name throws the format's serialization exception before anything is read or written.

Because the wire is string-only, scalar conversion is serializer-local: values parse and format with `InvariantCulture` over one scalar set in all three formats - strings, numbers (the primitive ones, `decimal`, `Int128`, `UInt128`, `Half` and `BigInteger`), booleans, enums, `Guid`, `DateTime` / `DateTimeOffset` / `DateOnly` / `TimeOnly` / `TimeSpan`, `Uri`, and their nullables - and a member of any of them maps to one value (in INI a key, never a section). Delimited writes the temporal values in their round-trip forms (`O`, and `c` for `TimeSpan`); DotEnv and INI write them in the invariant general form, except `TimeOnly`, which they too write in its round-trip form, since its general form holds only hours and minutes.

`DelimitedSerializer` additionally exposes a record-streaming surface: `DeserializeAsyncEnumerableAsync<TRecord>(Stream)` yields typed records as an `IAsyncEnumerable<TRecord>`, and `SerializeAsync(Stream, IAsyncEnumerable<TRecord>)` accepts one. Both directions are truly incremental - records parse and yield as stream segments arrive, and writes flush in bounded batches - so memory use is bounded by the longest record, not the document.

## Reflection-free binding

The serializer binders use reflection by default. For trimming and ahead-of-time compilation, `DelimitedSerializer` and `IniSerializer` also accept compile-time factories - `IDelimitedRecordFactory<TRecord>` and `IIniSectionFactory<TSection>` - through dedicated overloads that carry no reflection annotations. Annotate a partial POCO with `[DelimitedRecord]` or `[IniSection]` and reference the `Bodu.Text.Formats.Generators` source generator, and the factory is emitted at build time as a static `DelimitedFactory` / `IniFactory` property; the interfaces can also be implemented by hand.

## The two DOMs

- The **mutable `*Node` DOM** is for authoring and editing: parse, mutate values, add entries, and write back via `WriteTo(ref Utf8*Writer)` / `ToUtf8Bytes()` / `ToString()`.
- The **read-only `*Document` DOM** is `JsonDocument`-shaped: cheap to query (`GetProperty` / `TryGetProperty` / enumerators over `readonly struct` elements), owns its backing store, and must be disposed - elements become invalid after disposal.

## Trivia and round-trip preservation

The read-only DOMs drop comments. The mutable DOMs preserve what each format needs for faithful rewrites: DotEnv keeps the per-entry `export` flag, and INI nodes carry `LeadingComments` (comment lines before a section or entry) plus a `TrailingComments` block per object. INI deliberately does **not** model inline comments - the dialect treats an inline `;` or `#` and the text after it as value content, so an emitted inline comment would be re-read as part of the value.

## Dialect policies

Real-world files break the specs, so strictness is configurable per format - field counts, malformed records, and duplicate headers for Delimited; duplicate sections and keys for INI. See [Parser policies](parser-policies.md).

## Format exceptions

Malformed input throws the format's `*FormatException` carrying the source position (line / offset). Binding failures - an unmappable root, a missing `[Required]` member (DotEnv and INI), a value that cannot convert - throw the format's `*SerializationException`.

## Where to go next

- [Parser policies](parser-policies.md) - the per-format strictness knobs.
- The [format guides](../../guides/formats/index.md) - task-oriented recipes.
