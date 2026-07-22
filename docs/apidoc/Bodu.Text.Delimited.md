---
uid: Bodu.Text.Delimited
---

![Bodu.Text.Delimited](~/images/hero-delimited.svg)

## Purpose

**Bodu.Text.Delimited** parses and emits **delimited** records — CSV, TSV, and other character-separated value documents — as a standalone `System.Text.Json`-shaped library: a typed record serializer, a forward-only UTF-8 token reader/writer pair, a mutable node DOM, and a read-only document DOM. It ships as its own package (also available through the `Bodu.Text.Formats` umbrella); see also <xref:Bodu.Text.DotEnv>, <xref:Bodu.Text.Ini>, <xref:Bodu.Text.Bencode>, and <xref:Bodu.Text.Toml>.

## Key types

- <xref:Bodu.Text.Delimited.DelimitedSerializer> — static serializer: typed records ↔ delimited text, including the `IAsyncEnumerable` record-streaming overloads.
- <xref:Bodu.Text.Delimited.DelimitedSerializerOptions> — naming policy, case sensitivity, dialect characters, and header mode for binding.
- <xref:Bodu.Text.Delimited.Reader.Utf8DelimitedReader> / <xref:Bodu.Text.Delimited.Writer.Utf8DelimitedWriter> — forward-only `ref struct` token surfaces over UTF-8, with <xref:Bodu.Text.Delimited.Reader.DelimitedReaderOptions> carrying the dialect policies.
- <xref:Bodu.Text.Delimited.DelimitedFieldCountBehavior> / <xref:Bodu.Text.Delimited.DelimitedMalformedRecordBehavior> / <xref:Bodu.Text.Delimited.DelimitedDuplicateHeaderBehavior> — the strictness knobs for real-world files.
- <xref:Bodu.Text.Delimited.Document.DelimitedDocument> / <xref:Bodu.Text.Delimited.Document.DelimitedElement> — read-only, disposable document model (records as header-keyed objects or positional arrays).
- <xref:Bodu.Text.Delimited.Nodes.DelimitedNode> — mutable DOM root for parse → edit → write scenarios.
- <xref:Bodu.Text.Delimited.DelimitedFormatException> / <xref:Bodu.Text.Delimited.DelimitedSerializationException> — malformed input vs. binding failures.

## Example

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Serialization;

var options = new DelimitedSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
List<Trade> trades = DelimitedSerializer.Deserialize<Trade>("trade_id,symbol\n1,AAPL\n", options);
string csv = DelimitedSerializer.Serialize(trades, options);
```

## Notes

- **Streaming preferred for large inputs.** Use `DeserializeAsyncEnumerableAsync<TRecord>` for typed rows, or the `Utf8DelimitedReader` token loop for full control.
- **Round-trip determinism.** Writing quotes only the fields that require it under the configured delimiter/quote characters.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [delimited guide](~/guides/formats/delimited.md).
