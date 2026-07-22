---
title: Using delimited (CSV / TSV)
---

# Using delimited (CSV / TSV)

`Bodu.Text.Delimited` reads and writes RFC 4180 delimited text through the quartet surfaces: the read-only `DelimitedDocument`, the `DelimitedSerializer` record binder, the mutable `DelimitedNode` DOM, and the token-level `Utf8DelimitedReader` / `Utf8DelimitedWriter`.

## Pattern 1 — query a document

```csharp
using Bodu.Text.Delimited.Document;

using DelimitedDocument document = DelimitedDocument.Parse(File.ReadAllBytes("trades.csv"));

// Records are objects keyed by header name.
DelimitedElement root = document.RootElement;
for (int i = 0; i < root.GetArrayLength(); i++)
{
    string symbol = root[i].GetProperty("symbol").GetString();
}
```

In header mode, records are object elements (`GetProperty` / `TryGetProperty` / `EnumerateObject`); with `NoHeader = true`, they are positional arrays (`this[int]` / `GetArrayLength` / `EnumerateArray`).

## Pattern 2 — typed records via the serializer

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Serialization;

sealed class Trade
{
    public int TradeId { get; set; }
    public string? Symbol { get; set; }
    public decimal Price { get; set; }
}

var options = new DelimitedSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };

List<Trade> trades = DelimitedSerializer.Deserialize<Trade>(csvText, options); // header trade_id → TradeId
string back = DelimitedSerializer.Serialize(trades, options);                  // header row from the record type
```

Scalars parse and format with `InvariantCulture`; `[PropertyName]`, `[Ignore]`, `[Required]`, and `[PropertyOrder]` apply per member.

## Pattern 3 — stream records from a large file

```csharp
await foreach (Trade trade in DelimitedSerializer.DeserializeAsyncEnumerableAsync<Trade>(stream, options))
{
    Process(trade);
}
```

Both directions are genuinely incremental: records are parsed and yielded as stream segments arrive (memory is bounded by the longest record, not the document), and the write direction — `SerializeAsync(stream, records)` where `records` is an `IAsyncEnumerable<Trade>` — encodes each record as it is produced, flushing in bounded batches.

### Reflection-free binding

Annotate a partial record type with `[DelimitedRecord]` and reference the `Bodu.Text.Formats.Generators` source generator, and a static `DelimitedFactory` property (`IDelimitedRecordFactory<Trade>`) is emitted at compile time. Passing it to the factory overloads — `Serialize(records, Trade.DelimitedFactory)` / `Deserialize(csvText, Trade.DelimitedFactory)` — avoids the reflection binder entirely, making the path trimming- and AOT-safe. The interface can also be implemented by hand.

## Pattern 4 — TSV and other dialects

The delimiter, quote, and comment characters live on the reader/writer options:

```csharp
using Bodu.Text.Delimited.Reader;

var tsv = new DelimitedReaderOptions { Delimiter = '\t' };
using DelimitedDocument document = DelimitedDocument.Parse(bytes, tsv);
```

CSV → TSV conversion is a parse and a write through the mutable DOM:

```csharp
using System.Buffers;
using Bodu.Text.Delimited.Nodes;
using Bodu.Text.Delimited.Writer;

DelimitedArray records = DelimitedNode.Parse(csvBytes);

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { Delimiter = '\t' });
records.WriteTo(ref writer);
writer.Flush();
```

## Pattern 5 — dirty input

```csharp
var lenient = new DelimitedReaderOptions
{
    FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,        // accept short/long rows
    MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord, // truncate at structural errors
    DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeFirst,
};
```

Strict field counts (the default) are measured against the header row and throw `DelimitedFormatException` with the line number. See [Parser policies](../../docs/formats/parser-policies.md).

## Exceptions

`DelimitedFormatException` for malformed input (position attached); `DelimitedSerializationException` for binding failures (unsupported record type, missing `[Required]` member, non-convertible value).

## When *not* to use it

Nested or typed structures (use TOML/YAML/Bencode), and spreadsheets' native formats (`Bodu.Formats.Excel.Binary` reads `.xls`).

## See also

- [Streams and token-level I/O](streaming.md) for the `Utf8DelimitedReader` token loop.
- [Choosing a text format](choosing-a-format.md)
