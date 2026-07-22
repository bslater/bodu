# DotEnv → quartet design note

**Date:** 2026-07-21
**Status:** Design — implements tranche **T1** (template-establishing format).
**Relates to:** [`line-formats-quartet-redesign-assessment.md`](./line-formats-quartet-redesign-assessment.md).

DotEnv is the flattest of the three line formats — a single ordered object of
string-valued keys, no arrays, no nesting, values always string. It is done
**first** because it exercises the entire quartet + shared-serialization wiring
with the least format-specific noise, so the pattern it sets is the cleanest
template for Delimited and INI.

## Value model

A single flat, ordered **object** of `string → string`. The root is always an
object; there is no array form and no nested object form. Closest quartet
analogue: a Bencode dictionary at the root, but with string-only scalar values.

## Token model — `DotEnvTokenType`

```
None, StartObject, EndObject, PropertyName, String, Comment
```

No `StartArray`/`EndArray` — DotEnv has no array syntax.

`Utf8DotEnvReader` (`public ref partial struct` over `ReadOnlySpan<byte>` +
`ReadOnlySequence<byte>`) **is the `FormatReader`** (Bencode pattern; no
pre-parse type). Source-order token stream for:

```
export FOO=bar
# note
BAZ=1
```

```
StartObject                (synthetic document frame, emitted on first Read)
  PropertyName "FOO"       CurrentIsExport = true
  String "bar"
  Comment " note"          (skipped by the binder; surfaced for tooling/raw DOM)
  PropertyName "BAZ"
  String "1"
EndObject
```

- `export ` is a **reader flag** (`bool CurrentIsExport`) on the property, not a
  token — presentation trivia preserved by the mutable DOM, dropped by the
  read-only Document.
- Typed accessors follow the quartet shape but coerce the string:
  `GetString()`, `ValueTextEquals(...)`, `BytesConsumed`, and `GetXxx()` over
  `ISpanParsable<T>` via the format-local converters (assessment D4). Comments,
  quoting (single-quote literal, double-quote with escapes), and the `export`
  prefix are handled in `Utf8DotEnvReader.Value.cs`.
- **Genuinely line-incremental streaming:** resumable-block ctors + a
  `DotEnvReaderState` (partial-line carry across `ReadOnlySequence<byte>`
  segments), matching today's genuinely-incremental `DotEnvReader.ReadAsync`.

`Utf8DotEnvWriter` (`public ref partial struct` to `IBufferWriter<byte>` and
`Stream`): `WriteStartObject`/`WriteEndObject`/`WritePropertyName`/`WriteString`/
`WriteComment`, `Flush`/`Dispose`/`Reset`, `BytesCommitted`/`BytesPending`.

## Mutable DOM (`Text.DotEnv.Nodes`) — trivia-bearing (D5)

- `DotEnvNode` (abstract) / `DotEnvObject` (the only container; ordered
  `string → DotEnvNode` map) / `DotEnvValue` (string). **No `DotEnvArray`.**
- `DotEnvObject` entries carry optional `LeadingComments` + `Export` trivia so
  the authoring / format round-trip stays faithful. Successor to today's
  `DotEnvDocument` / `DotEnvEntry` / `DotEnvComment`.
- `DotEnvNode.Parse(ReadOnlySpan<byte>[, DotEnvNodeOptions])`, `WriteTo(...)`,
  `ToUtf8Bytes()`, `DeepClone()`, `DeepEquals(...)`, implicit conversions from
  `string`.

## Read-only DOM (`Text.DotEnv.Document`) — trivia-free, `IDisposable`

`DotEnvDocument` / `DotEnvElement` / `DotEnvProperty`, `JsonElement`-shaped
(`ValueKind`, `GetString`/`GetXxx`, `GetProperty`, `TryGetProperty`,
`EnumerateObject`), root element is an object, flat pooled row store, comments
dropped (documented).

## Serializer (`DotEnvSerializer`)

Standard facade — `Serialize` (string / `IBufferWriter<byte>`), `Deserialize`
(string / `ReadOnlySpan<byte>` / `Stream`), `SerializeAsync`/`DeserializeAsync`
(`Stream`, buffered-in-full). Binding targets: `Dictionary<string,string>`,
`IDictionary<string,string>`, or a flat POCO whose properties are string /
`ISpanParsable<T>`. **Root must map to an object/dictionary**; a scalar or
collection root throws `DotEnvSerializationException` (the DotEnv analogue of
TOML's root-is-table gate). `DotEnvSerializerOptions` (partial, read-only on
first use, `DotEnvSerializerDefaults`) reuses the shared attribute /
`NamingPolicy` / callback layer; scalar converters are format-local (D4).

## Dialect migration

Today's `DotEnvParseOptions` knobs move onto the reader/writer/serializer
options: `AllowExportPrefix`, `AllowInlineComments`, `PreserveComments`, and
`DuplicateKeyBehavior` (`Bodu.Text.DuplicateKeyPolicy`, which relocates into the
DotEnv root bucket since DotEnv now owns it — INI carries its own copy after the
Configuration decouple, or the enum is duplicated per owner).

## Tests (colocated in `Bodu.Text.DotEnv.Test`)

`Utf8DotEnvReaderTests` (token-stream KATs + malformed rejection + resumable
multi-segment state), `Utf8DotEnvWriterTests` (exact canonical bytes),
`DotEnvSerializerTests.{Serialize,Deserialize,SerializeAsync,DeserializeAsync,RoundTrip}.cs`
backbone + subject partials (`.NamingPolicy`, `.Nulls`, `.Dictionaries`,
`.ExtensionData`, enum converters), and `DotEnvDocument`/`DotEnvNode` DOM tests.
Migrate the existing `DotEnvKnownAnswerVector`. Curated malformed sweep in the
Regression tier (no canonical DotEnv spec corpus exists). One `Smoke` test.
