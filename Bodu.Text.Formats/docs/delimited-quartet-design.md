# Delimited (CSV/TSV) → quartet design note

**Date:** 2026-07-21
**Status:** Implemented. The design implements tranche **T2**; the package
shipped in 1.0.0, first published in 0.5.0. Where the shipped code differs from
this design, an **As built** paragraph after the claim records what the code
does, as of master `849e280436`.
**Relates to:** [`line-formats-quartet-redesign-assessment.md`](./line-formats-quartet-redesign-assessment.md).

Delimited is the one line format whose value model is a genuine two-level tree
(an array of records), so the full four-type quartet DOM is a natural fit. Its
headline value - genuine row-at-a-time streaming - must survive the redesign and
is promoted to a first-class `IAsyncEnumerable<TRecord>` surface.

## Value model

Document = an **array of homogeneous records**.

- **With a header row:** each record is an **object keyed by header name**.
- **Headerless:** each record is a **positional array of string fields**.

Field values are always **string**; typed access is via format-local
`ISpanParsable<T>` converters (assessment D4).

**As built:** there are no format-local or `ISpanParsable<T>` converters. Typed
access goes through the shared `InvariantScalarCodec` under its `Delimited`
policy (`Bodu.Text.Serialization/shared/Flat/InvariantScalarCodec.cs`, compiled
into the package and called by `ValueToString` and `ConvertFromString` in
`Bodu.Text.Delimited/src/Text.Delimited/DelimitedSerializer.Binder.cs`).

## Token model - `DelimitedTokenType`

```
None, StartArray, EndArray, StartObject, EndObject, PropertyName, String
```

`Utf8DelimitedReader` (`public ref partial struct` over `ReadOnlySpan<byte>` +
`ReadOnlySequence<byte>`) **is the `FormatReader`**, synthesizing the tree
framing with a one-row header lookahead (RFC 4180 has no out-of-order structure,
so no pre-parse type is needed). Token stream for `name,age⏎Ada,36⏎Grace,45`
with header:

```
StartArray
  StartObject
    PropertyName "name"   String "Ada"
    PropertyName "age"    String "36"
  EndObject
  StartObject
    PropertyName "name"   String "Grace"
    PropertyName "age"    String "45"
  EndObject
EndArray
```

**As built:** `Utf8DelimitedReader` is a plain `public ref struct`, not
`partial`. Its public constructors take a `ReadOnlySpan<byte>`, with or without
`DelimitedReaderOptions`; its only other constructor is an internal one that
reads a segment of a larger input and counts lines and offsets from the
segment's position in it
(`Bodu.Text.Delimited/src/Text.Delimited.Reader/Utf8DelimitedReader.cs`). It is
not a `FormatReader`: the package compiles none of the shared converter engine,
and `DelimitedSerializer` reads every record into a list of field arrays
(`ReadRows`) and binds by reflection from that list
(`Bodu.Text.Delimited/src/Text.Delimited/DelimitedSerializer.cs`).

Headerless mode emits `StartArray … String* … EndArray` per record instead of
objects (binds to `string[]` / `List<string>`).

**As built:** only `string[]` binds as a positional record. Any other
collection record type, `List<string>` among them, is rejected with
`DelimitedSerializationException` before a row is read or written
(`GetRecordMembers` in
`Bodu.Text.Delimited/src/Text.Delimited/DelimitedSerializer.Binder.cs`, #1050).

- **RFC 4180 in `Utf8DelimitedReader.Fields.cs`:** configurable delimiter/quote,
  quoted fields spanning lines, doubled-quote escapes, blank-line skipping,
  optional comment lines.

  **As built:** the RFC 4180 logic is in
  `Bodu.Text.Delimited/src/Text.Delimited.Reader/Utf8DelimitedReader.cs`
  (`ParseRecord`, `ParseField`, `ParseQuotedField`, `SkipBlankAndCommentLines`);
  there is no `Utf8DelimitedReader.Fields.cs`.

- **Genuine streaming is the headline:** resumable-block ctors + a
  `DelimitedReaderState` for row-at-a-time over `ReadOnlySequence<byte>`.

  **As built:** there are no resumable-block constructors, no
  `DelimitedReaderState` and no `ReadOnlySequence<byte>` input. Row-at-a-time
  streaming is `DelimitedSerializer.DeserializeAsyncEnumerableAsync`, which
  reads the stream into a buffer rented from `ArrayPool<byte>.Shared`, parses
  the complete records in it through the reader's internal segment
  constructor, yields them, and keeps only the bytes of the unfinished record
  for the next read
  (`Bodu.Text.Delimited/src/Text.Delimited/DelimitedSerializer.Streaming.cs`).

`Utf8DelimitedWriter` (to `IBufferWriter<byte>` / `Stream`): the `WriteStartX`/
`WriteEndX`/`WritePropertyName`/`WriteString` surface with automatic RFC 4180
quoting, `Flush`/`Dispose`/`Reset`, `BytesCommitted`/`BytesPending`.

**As built:** `Utf8DelimitedWriter` is a plain `public ref struct` with `Flush`
and `Dispose` but no `Reset`
(`Bodu.Text.Delimited/src/Text.Delimited.Writer/Utf8DelimitedWriter.cs`).

## Mutable DOM (`Text.Delimited.Nodes`)

`DelimitedNode` / `DelimitedArray` / `DelimitedObject` / `DelimitedValue`.
Document = `DelimitedArray`; record = `DelimitedObject` (header mode) or
`DelimitedArray` (positional). Values string. This is the one line format where
the full quartet DOM is idiomatic; **no comment trivia** (Delimited never
retained comments - options only *skip* them).

## Read-only DOM (`Text.Delimited.Document`)

`DelimitedDocument` / `DelimitedElement` / `DelimitedProperty`, root element an
array; `EnumerateArray()` over records; header names surfaced via
`DelimitedDocument.Headers`. `IDisposable`, flat pooled row store.

**As built:** the store is a plain `List<string[]>` of rows with a
`List<string>` of headers, not pooled
(`Bodu.Text.Delimited/src/Text.Delimited.Document/DelimitedDocument.cs`).

## Serializer (`DelimitedSerializer`)

Standard facade (buffered-in-full) **plus** the incremental surface:

```csharp
// Buffered facade - parity with the quartet.
static string Serialize<T>(T value, DelimitedSerializerOptions? options = null);   // T = collection
static IReadOnlyList<TRecord> Deserialize<TRecord>(ReadOnlySpan<byte> utf8, ...);

// First-class incremental surface (the roadmap's IAsyncEnumerable item).
static IAsyncEnumerable<TRecord> DeserializeAsyncEnumerable<TRecord>(Stream source, ...);
static ValueTask SerializeAsync<TRecord>(Stream destination, IAsyncEnumerable<TRecord> rows, ...);
```

**As built:** every `Deserialize` overload (string, `ReadOnlySpan<byte>`,
`Stream`, and the factory overloads) returns `List<TRecord>`, not
`IReadOnlyList<TRecord>`; the incremental read is named
`DeserializeAsyncEnumerableAsync`; and the record parameter of the incremental
`SerializeAsync` is named `records`
(`Bodu.Text.Delimited/src/Text.Delimited/DelimitedSerializer.cs`,
`DelimitedSerializer.Streaming.cs`). The buffered facade has `SerializeAsync`
but no `DeserializeAsync`: the only asynchronous read is
`DeserializeAsyncEnumerableAsync`.

Binding target: `List<TRecord>` / `TRecord[]` / `IEnumerable<TRecord>` /
`IAsyncEnumerable<TRecord>` / `string[]`, where `TRecord` is a POCO whose mapped
properties correspond to header columns (respecting `NamingPolicy` /
`[PropertyName]`), or a positional `string[]`. `Serialize` writes the header row
from `TRecord`'s mapped names, then one row per element. **Root must map to a
collection** (the Delimited analogue of the root-is-table gate); a scalar/object
root throws `DelimitedSerializationException`.

**As built:** the shapes differ by direction. `Serialize` and the buffered
`SerializeAsync` accept any array or `IEnumerable<TRecord>` root
(`GetRecordType` in
`Bodu.Text.Delimited/src/Text.Delimited/DelimitedSerializer.Binder.cs`), and the
incremental `SerializeAsync` takes an `IAsyncEnumerable<TRecord>`.
`Deserialize` always returns a `List<TRecord>` and
`DeserializeAsyncEnumerableAsync` yields an `IAsyncEnumerable<TRecord>`, with
`TRecord` a POCO or `string[]`. A root that is not enumerable, a `string` root,
and a record type that is a scalar or a collection other than `string[]` are
rejected with `DelimitedSerializationException`, and so, when reading, is a
record type with no public parameterless constructor (`GetRecordMembers` in
`Bodu.Text.Delimited/src/Text.Delimited/DelimitedSerializer.Binder.cs`, #1050).
A dictionary root is written as `Key` and `Value` columns.

## Dialect migration

Today's `DelimitedParseOptions` splits across `DelimitedReaderOptions` /
`DelimitedWriterOptions` / `DelimitedSerializerOptions`, carrying: `Delimiter`,
`Quote`, `HasHeader`, `TrimFields`, `AllowComments`, `CommentChar`, and the three
dialect enums `DelimitedFieldCountBehavior` (`Strict`/`Ragged`),
`DelimitedMalformedRecordBehavior` (`Throw`/`SkipRecord`),
`DelimitedDuplicateHeaderBehavior` (`Throw`/…), moved into the Delimited root
bucket.

**As built:** the header switch is the inverted `NoHeader`, on
`DelimitedReaderOptions`, `DelimitedWriterOptions` and
`DelimitedSerializerOptions`; `HasHeader` is internal
(`Bodu.Text.Delimited/src/Text.Delimited.Reader/DelimitedReaderOptions.cs`).
`TrimFields`, `AllowComments` and the three behavior enums exist only on
`DelimitedReaderOptions`. Of the dialect settings, `DelimitedSerializerOptions`
carries only `Delimiter`, `Quote`, `CommentChar` and `NoHeader`, so the
serializer never trims fields or skips comment lines, and it applies the default
field-count, malformed-record and duplicate-header policies.

## Tests (colocated in `Bodu.Text.Delimited.Test`)

`Utf8DelimitedReaderTests` (token-stream KATs, RFC 4180 quoting/embedded-newline/
doubled-quote, resumable multi-segment), `Utf8DelimitedWriterTests` (exact
canonical bytes incl. quoting), `DelimitedSerializerTests.*` backbone + subject
partials (`.NamingPolicy`, `.Collections`, headerless/positional, the
`DeserializeAsyncEnumerable` streaming path over `Bodu.Test.IO` mocks), and DOM
tests. Migrate `DelimitedKnownAnswerVector`. **Vendored RFC 4180 case corpus**
(quoted fields, embedded CRLF, doubled quotes, trailing-newline, ragged rows),
copied to output like Toml's `TomlTestCorpus`, driven in the Regression tier.
One `Smoke` test.

**As built:** in `Bodu.Text.Delimited/test/`, the `Utf8DelimitedReaderTests`
partials (`Read`, `Quoting`, `Headers`, `Policies`, `Rfc4180Corpus` and
others) have no multi-segment tests. Segmented input is tested through the
serializer, in the `DeserializeAsyncEnumerableAsync` and `Streaming` partials
of `DelimitedSerializerTests`, over the `Bodu.Test.IO` mocks and local test
streams (`PieceStream`, `FeedStream` and others, each in its own partial file).
The serializer's test partials are `Serialize`, `SerializeAsync`,
`Deserialize`, `DeserializeAsyncEnumerableAsync`, `RoundTrip` and `Streaming`,
with no `.NamingPolicy` or `.Collections` partial. No
`DelimitedKnownAnswerVector` type exists. The RFC 4180 corpus is not vendored or
copied to output: it is inline `ValidKat<string, string[][]>` rows derived from
csv-spectrum and run in the Regression tier
(`Utf8DelimitedReaderTests.Rfc4180Corpus.cs`), ragged rows are covered by
`Utf8DelimitedReaderTests.Policies`, and the project embeds only the
release-note fix catalogues (`Fixtures/ReleaseNotes`). There is no `Smoke`
test.

## Downstream (verify in T2)

`Bodu.Financial.ExchangeRates.Imf` (`ImfReportParser`, TSV) and `.Boe`
(`BoeRateCsvParser`, CSV) `using Bodu.Text.Delimited;` and call `Delimited.Parse` +
`DelimitedDocument`/`DelimitedRow`/`DelimitedParseOptions`. Migrate both to the
new `DelimitedDocument`/`DelimitedElement` DOM (or `DelimitedSerializer`) - their
`Ragged` + `SkipRecord` options map directly onto the new
`DelimitedReaderOptions`.

**As built:** the migration is done. `ImfReportParser` and `BoeRateCsvParser`
parse with `DelimitedDocument.Parse` over `DelimitedReaderOptions` that set
`FieldCountBehavior` to `Ragged` and `MalformedRecordBehavior` to `SkipRecord`,
and walk the records as `DelimitedElement` values
(`Bodu.Financial.ExchangeRates.Imf/src/Financial.ExchangeRates/ImfReportParser.cs`,
`Bodu.Financial.ExchangeRates.Boe/src/Financial.ExchangeRates/BoeRateCsvParser.cs`).
`Delimited.Parse`, `DelimitedRow` and `DelimitedParseOptions` no longer exist.
