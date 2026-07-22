# Delimited (CSV/TSV) → quartet design note

**Date:** 2026-07-21
**Status:** Design — implements tranche **T2**.
**Relates to:** [`line-formats-quartet-redesign-assessment.md`](./line-formats-quartet-redesign-assessment.md).

Delimited is the one line format whose value model is a genuine two-level tree
(an array of records), so the full four-type quartet DOM is a natural fit. Its
headline value — genuine row-at-a-time streaming — must survive the redesign and
is promoted to a first-class `IAsyncEnumerable<TRecord>` surface.

## Value model

Document = an **array of homogeneous records**.

- **With a header row:** each record is an **object keyed by header name**.
- **Headerless:** each record is a **positional array of string fields**.

Field values are always **string**; typed access is via format-local
`ISpanParsable<T>` converters (assessment D4).

## Token model — `DelimitedTokenType`

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

Headerless mode emits `StartArray … String* … EndArray` per record instead of
objects (binds to `string[]` / `List<string>`).

- **RFC 4180 in `Utf8DelimitedReader.Fields.cs`:** configurable delimiter/quote,
  quoted fields spanning lines, doubled-quote escapes, blank-line skipping,
  optional comment lines.
- **Genuine streaming is the headline:** resumable-block ctors + a
  `DelimitedReaderState` for row-at-a-time over `ReadOnlySequence<byte>`.

`Utf8DelimitedWriter` (to `IBufferWriter<byte>` / `Stream`): the `WriteStartX`/
`WriteEndX`/`WritePropertyName`/`WriteString` surface with automatic RFC 4180
quoting, `Flush`/`Dispose`/`Reset`, `BytesCommitted`/`BytesPending`.

## Mutable DOM (`Text.Delimited.Nodes`)

`DelimitedNode` / `DelimitedArray` / `DelimitedObject` / `DelimitedValue`.
Document = `DelimitedArray`; record = `DelimitedObject` (header mode) or
`DelimitedArray` (positional). Values string. This is the one line format where
the full quartet DOM is idiomatic; **no comment trivia** (Delimited never
retained comments — options only *skip* them).

## Read-only DOM (`Text.Delimited.Document`)

`DelimitedDocument` / `DelimitedElement` / `DelimitedProperty`, root element an
array; `EnumerateArray()` over records; header names surfaced via
`DelimitedDocument.Headers`. `IDisposable`, flat pooled row store.

## Serializer (`DelimitedSerializer`)

Standard facade (buffered-in-full) **plus** the incremental surface:

```csharp
// Buffered facade — parity with the quartet.
static string Serialize<T>(T value, DelimitedSerializerOptions? options = null);   // T = collection
static IReadOnlyList<TRecord> Deserialize<TRecord>(ReadOnlySpan<byte> utf8, ...);

// First-class incremental surface (the roadmap's IAsyncEnumerable item).
static IAsyncEnumerable<TRecord> DeserializeAsyncEnumerable<TRecord>(Stream source, ...);
static ValueTask SerializeAsync<TRecord>(Stream destination, IAsyncEnumerable<TRecord> rows, ...);
```

Binding target: `List<TRecord>` / `TRecord[]` / `IEnumerable<TRecord>` /
`IAsyncEnumerable<TRecord>` / `string[]`, where `TRecord` is a POCO whose mapped
properties correspond to header columns (respecting `NamingPolicy` /
`[PropertyName]`), or a positional `string[]`. `Serialize` writes the header row
from `TRecord`'s mapped names, then one row per element. **Root must map to a
collection** (the Delimited analogue of the root-is-table gate); a scalar/object
root throws `DelimitedSerializationException`.

## Dialect migration

Today's `DelimitedParseOptions` splits across `DelimitedReaderOptions` /
`DelimitedWriterOptions` / `DelimitedSerializerOptions`, carrying: `Delimiter`,
`Quote`, `HasHeader`, `TrimFields`, `AllowComments`, `CommentChar`, and the three
dialect enums `DelimitedFieldCountBehavior` (`Strict`/`Ragged`),
`DelimitedMalformedRecordBehavior` (`Throw`/`SkipRecord`),
`DelimitedDuplicateHeaderBehavior` (`Throw`/…), moved into the Delimited root
bucket.

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

## Downstream (verify in T2)

`Bodu.Financial.ExchangeRates.Imf` (`ImfReportParser`, TSV) and `.Boe`
(`BoeRateCsvParser`, CSV) `using Bodu.Text.Delimited;` and call `Delimited.Parse`
+ `DelimitedDocument`/`DelimitedRow`/`DelimitedParseOptions`. Migrate both to the
new `DelimitedDocument`/`DelimitedElement` DOM (or `DelimitedSerializer`) — their
`Ragged` + `SkipRecord` options map directly onto the new
`DelimitedReaderOptions`.
