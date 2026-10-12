# DotEnv → quartet design note

**Date:** 2026-07-21
**Status:** Implemented. The design implements tranche **T1**
(template-establishing format); the package shipped in 1.0.0, first published
in 0.5.0. Where the shipped code differs from this design, an **As built**
paragraph after the claim records what the code does, as of master
`dd6d435742`.
**Relates to:** [`line-formats-quartet-redesign-assessment.md`](./line-formats-quartet-redesign-assessment.md).

DotEnv is the flattest of the three line formats - a single ordered object of
string-valued keys, no arrays, no nesting, values always string. It is done
**first** because it exercises the entire quartet + shared-serialization wiring
with the least format-specific noise, so the pattern it sets is the cleanest
template for Delimited and INI.

## Value model

A single flat, ordered **object** of `string → string`. The root is always an
object; there is no array form and no nested object form. Closest quartet
analogue: a Bencode dictionary at the root, but with string-only scalar values.

## Token model - `DotEnvTokenType`

```
None, StartObject, EndObject, PropertyName, String, Comment
```

No `StartArray`/`EndArray` - DotEnv has no array syntax.

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

**As built:** `Utf8DotEnvReader` is a plain `public ref struct`, not `partial`,
and its only constructors take a `ReadOnlySpan<byte>`, with or without
`DotEnvReaderOptions`
(`Bodu.Text.DotEnv/src/Text.DotEnv.Reader/Utf8DotEnvReader.cs`). It is not a
`FormatReader`: the package compiles none of the shared converter engine
(Bencode, TOML and YAML alias `FormatReader` in their `SharedSourceAliases.cs`;
DotEnv has no such file), and `DotEnvSerializer` reads every entry into a
`List<KeyValuePair<string, string>>` (`ReadEntries`) and binds by reflection
from that list (`Bodu.Text.DotEnv/src/Text.DotEnv/DotEnvSerializer.cs`).
Comment tokens reach only direct users of the reader: neither DOM keeps them
(see the mutable DOM below).

- `export ` is a **reader flag** (`bool CurrentIsExport`) on the property, not a
  token - presentation trivia preserved by the mutable DOM, dropped by the
  read-only Document.
- Typed accessors follow the quartet shape but coerce the string:
  `GetString()`, `ValueTextEquals(...)`, `BytesConsumed`, and `GetXxx()` over
  `ISpanParsable<T>` via the format-local converters (assessment D4). Comments,
  quoting (single-quote literal, double-quote with escapes), and the `export`
  prefix are handled in `Utf8DotEnvReader.Value.cs`.

  **As built:** the reader's value accessors are `GetString`, three
  `ValueTextEquals` overloads and `ValueSpan`, beside `BytesConsumed`,
  `CurrentDepth`, `CurrentIsExport` and `LineNumber`. There is no `GetXxx` and
  no `ISpanParsable<T>` path; the serializer converts scalars (see below).
  Comments, quoting and the `export` prefix are all handled in
  `Bodu.Text.DotEnv/src/Text.DotEnv.Reader/Utf8DotEnvReader.cs` (`ReadBody`,
  `ReadEntry`, and `ReadValue` with its quoted and unquoted helpers); there is
  no `Utf8DotEnvReader.Value.cs`.

- **Genuinely line-incremental streaming:** resumable-block ctors + a
  `DotEnvReaderState` (partial-line carry across `ReadOnlySequence<byte>`
  segments), matching today's genuinely-incremental `DotEnvReader.ReadAsync`.

  **As built:** there are no resumable-block constructors, no
  `DotEnvReaderState` and no `ReadOnlySequence<byte>` input. The reader works
  over one complete buffer, which is why `TrySkip` always returns `true`.
  `DotEnvSerializer.Deserialize(Stream)` and `DeserializeAsync` copy the whole
  stream into a `MemoryStream` before parsing, so only the stream copy is
  asynchronous (`Bodu.Text.DotEnv/src/Text.DotEnv/DotEnvSerializer.cs`).

`Utf8DotEnvWriter` (`public ref partial struct` to `IBufferWriter<byte>` and
`Stream`): `WriteStartObject`/`WriteEndObject`/`WritePropertyName`/`WriteString`/
`WriteComment`, `Flush`/`Dispose`/`Reset`, `BytesCommitted`/`BytesPending`.

**As built:** `Utf8DotEnvWriter` is a plain `public ref struct`, not `partial`,
with `Flush` and `Dispose` but no `Reset`. It adds a
`WritePropertyName(string, bool export)` overload that decides the `export`
prefix per entry; the one-argument overload follows
`DotEnvWriterOptions.WriteExportPrefix`
(`Bodu.Text.DotEnv/src/Text.DotEnv.Writer/Utf8DotEnvWriter.cs`).

## Mutable DOM (`Text.DotEnv.Nodes`) - trivia-bearing (D5)

- `DotEnvNode` (abstract) / `DotEnvObject` (the only container; ordered
  `string → DotEnvNode` map) / `DotEnvValue` (string). **No `DotEnvArray`.**

  **As built:** `DotEnvObject` is an insertion-ordered `string → DotEnvValue`
  map, and its indexer gets and sets a `DotEnvValue`
  (`Bodu.Text.DotEnv/src/Text.DotEnv.Nodes/DotEnvObject.cs`).

- `DotEnvObject` entries carry optional `LeadingComments` + `Export` trivia so
  the authoring / format round-trip stays faithful. Successor to today's
  `DotEnvDocument` / `DotEnvEntry` / `DotEnvComment`.

  **As built:** the only trivia an entry keeps is its `export` flag, read and
  set through `DotEnvObject.IsExport(string)` and `SetExport(string, bool)`.
  There are no `LeadingComments` or `Export` members, and `DotEnvNode.Parse`
  discards comment tokens, so neither DOM retains comments
  (`Bodu.Text.DotEnv/src/Text.DotEnv.Nodes/DotEnvObject.cs`, `DotEnvNode.cs`).

- `DotEnvNode.Parse(ReadOnlySpan<byte>[, DotEnvNodeOptions])`, `WriteTo(...)`,
  `ToUtf8Bytes()`, `DeepClone()`, `DeepEquals(...)`, implicit conversions from
  `string`.

  **As built:** the parse overloads are `Parse(ReadOnlySpan<byte>)`,
  `Parse(ReadOnlySpan<byte>, DotEnvReaderOptions)` and `Parse(string)`, each
  returning a `DotEnvObject`. There is no `DotEnvNodeOptions` type and no
  `DeepEquals` (`Bodu.Text.DotEnv/src/Text.DotEnv.Nodes/DotEnvNode.cs`).

## Read-only DOM (`Text.DotEnv.Document`) - trivia-free, `IDisposable`

`DotEnvDocument` / `DotEnvElement` / `DotEnvProperty`, `JsonElement`-shaped
(`ValueKind`, `GetString`/`GetXxx`, `GetProperty`, `TryGetProperty`,
`EnumerateObject`), root element is an object, flat pooled row store, comments
dropped (documented).

**As built:** `DotEnvElement` has `ValueKind`, `GetString`, `GetProperty`,
`TryGetProperty` and `EnumerateObject`, and no `GetXxx`. The store behind
`DotEnvDocument` is a plain `List<KeyValuePair<string, string>>`, not pooled
(`Bodu.Text.DotEnv/src/Text.DotEnv.Document/DotEnvElement.cs`,
`DotEnvDocument.cs`).

## Serializer (`DotEnvSerializer`)

Standard facade - `Serialize` (string / `IBufferWriter<byte>`), `Deserialize`
(string / `ReadOnlySpan<byte>` / `Stream`), `SerializeAsync`/`DeserializeAsync`
(`Stream`, buffered-in-full). Binding targets: `Dictionary<string,string>`,
`IDictionary<string,string>`, or a flat POCO whose properties are string /
`ISpanParsable<T>`. **Root must map to an object/dictionary**; a scalar or
collection root throws `DotEnvSerializationException` (the DotEnv analogue of
TOML's root-is-table gate). `DotEnvSerializerOptions` (partial, read-only on
first use, `DotEnvSerializerDefaults`) reuses the shared attribute /
`NamingPolicy` / callback layer; scalar converters are format-local (D4).

**As built:** there are no format-local or `ISpanParsable<T>` converters.
Scalars convert through the shared `InvariantScalarCodec` under its
`Configuration` policy
(`Bodu.Text.Serialization/shared/Flat/InvariantScalarCodec.cs`, compiled into
the package and called by `ValueToString` and `ConvertFromString` in
`Bodu.Text.DotEnv/src/Text.DotEnv/DotEnvSerializer.Binder.cs`), which
handles `string`, `bool`, enums, `Guid`, `DateTime`, `DateTimeOffset`,
`TimeSpan` and `Uri` itself and passes any other type to `Convert.ChangeType`.
A dictionary target may be any `IDictionary<string, TValue>` whose values the
codec converts. `DotEnvSerializerOptions` is a `sealed` class, not `partial`.
The root gate (`IsScalarOrEnumerable`) rejects `string`, primitive, enum and
non-dictionary enumerable roots only, so another scalar root such as `decimal`,
`DateTime` or `Guid` binds as an object through its public properties:
`Serialize(1.5m)` writes `Scale=1`, and `Deserialize<Guid>` returns
`Guid.Empty`, without an error.

## Dialect migration

Today's `DotEnvParseOptions` knobs move onto the reader/writer/serializer
options: `AllowExportPrefix`, `AllowInlineComments`, `PreserveComments`, and
`DuplicateKeyBehavior` (`Bodu.Text.DuplicateKeyPolicy`, which relocates into the
DotEnv root bucket since DotEnv now owns it - INI carries its own copy after the
Configuration decouple, or the enum is duplicated per owner).

**As built:** the public reader options are inverted so that `default` enables
each feature: `DotEnvReaderOptions` has `DisallowExportPrefix`,
`DisallowInlineComments` and `SkipComments`, and the `AllowExportPrefix`,
`AllowInlineComments` and `PreserveComments` forms are internal
(`Bodu.Text.DotEnv/src/Text.DotEnv.Reader/DotEnvReaderOptions.cs`). They exist
only on the reader options: `DotEnvSerializer` always reads with
`DotEnvReaderOptions.Default`, and the write side has `WriteExportPrefix` on
`DotEnvWriterOptions` and `DotEnvSerializerOptions`. There is no duplicate-key
option and no DotEnv duplicate-key enum. A repeated key resolves to its last
definition in the serializer, in `DotEnvObject` (which keeps the key at its
first position) and in `DotEnvDocument` lookups, while `EnumerateObject` yields
every definition in source order. `DuplicateKeyPolicy` stayed in the
`Bodu.Text.Configuration` namespace
(`Bodu.Text.Configuration/src/Text.Configuration/DuplicateKeyPolicy.cs`), and
INI has its own `IniDuplicateKeyBehavior`.

## Tests (colocated in `Bodu.Text.DotEnv.Test`)

`Utf8DotEnvReaderTests` (token-stream KATs + malformed rejection + resumable
multi-segment state), `Utf8DotEnvWriterTests` (exact canonical bytes),
`DotEnvSerializerTests.{Serialize,Deserialize,SerializeAsync,DeserializeAsync,RoundTrip}.cs`
backbone + subject partials (`.NamingPolicy`, `.Nulls`, `.Dictionaries`,
`.ExtensionData`, enum converters), and `DotEnvDocument`/`DotEnvNode` DOM tests.
Migrate the existing `DotEnvKnownAnswerVector`. Curated malformed sweep in the
Regression tier (no canonical DotEnv spec corpus exists). One `Smoke` test.

**As built:** in `Bodu.Text.DotEnv/test/`, the reader tests are the
`Utf8DotEnvReaderTests` partials `Read`, `Values`, `Inspection`, `Encoding` and
`DotEnvCorpus`, with no multi-segment tests, since the reader has no resumable
state. `DotEnvSerializerTests` has the partials `Serialize`, `Deserialize`,
`RoundTrip` (which also covers `SerializeAsync` and `DeserializeAsync`) and
`Fields`: there is no `SerializeAsync` or `DeserializeAsync` partial and none of
the subject partials listed above. The DOM tests are `DotEnvDocumentTests`,
`DotEnvElementTests` and `DotEnvObjectTests`; there is no `DotEnvNodeTests`. No
`DotEnvKnownAnswerVector` type exists: the `DotEnvCorpus` rows use the shared
`ValidKat<string, string[][]>` and `InvalidKat<string>`. The Regression tier
holds that corpus, the release-note fix catalogue
(`DotEnvReleaseNoteCorpusTests`) and the guide-snippet compile check. There is
no `Smoke` test.
