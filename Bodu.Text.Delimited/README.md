# Bodu.Text.Delimited

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

An RFC 4180 delimited-text (CSV / TSV) library for .NET 8, shaped after `System.Text.Json`: a typed record serializer over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model, plus the dialect policies real-world files need (ragged rows, malformed records, duplicate headers).

## Installation

```shell
dotnet add package Bodu.Text.Delimited
```

Targets `net8.0`. Also available through the `Bodu.Text.Formats` umbrella package.

## API shape

The public surface matches the sibling `Bodu.Text.Bencode` / `Bodu.Text.Toml` libraries, so the patterns transfer directly:

| Type(s) | Namespace | Role |
|---|---|---|
| `DelimitedSerializer` / `DelimitedSerializerOptions` / `DelimitedSerializerDefaults` | `Bodu.Text.Delimited` | Static serializer entry point (records ↔ rows), configuration, and presets. |
| `DelimitedFieldCountBehavior` / `DelimitedMalformedRecordBehavior` / `DelimitedDuplicateHeaderBehavior` | `Bodu.Text.Delimited` | The dialect policies for input that breaks the RFC 4180 contract. |
| `DelimitedFormatException` / `DelimitedSerializationException` | `Bodu.Text.Delimited` | Failures split by cause: malformed input vs. values that cannot be mapped. |
| `Utf8DelimitedReader` (+ `DelimitedReaderOptions`) | `Bodu.Text.Delimited.Reader` | Forward-only `ref struct` token reader with header lookahead. |
| `Utf8DelimitedWriter` (+ `DelimitedWriterOptions`) | `Bodu.Text.Delimited.Writer` | Forward-only `ref struct` token writer (auto-quoting, dialect retargeting). |
| `DelimitedDocument` / `DelimitedElement` / `DelimitedProperty` | `Bodu.Text.Delimited.Document` | Read-only document object model (records as objects keyed by header, or positional arrays). |
| `DelimitedNode` / `DelimitedArray` / `DelimitedObject` / `DelimitedValue` | `Bodu.Text.Delimited.Nodes` | Mutable document object model: parse, edit, write back. |

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Serialization;

var options = new DelimitedSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
List<Trade> trades = DelimitedSerializer.Deserialize<Trade>(csvText, options);
string back = DelimitedSerializer.Serialize(trades, options);

// Stream typed records without materializing the file.
await foreach (Trade trade in DelimitedSerializer.DeserializeAsyncEnumerableAsync<Trade>(stream, options))
    Process(trade);
```

Values are carried as text on the wire; scalars convert with `InvariantCulture`. The serializer honours the shared `Bodu.Text.Serialization` attribute family, naming policies, and serialization callbacks. The Regression test tier carries a csv-spectrum-derived RFC 4180 conformance corpus.
