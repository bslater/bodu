---
title: Using delimited (CSV / TSV)
---

# Using delimited (CSV / TSV)

`Bodu.Text.Delimited` reads and writes RFC 4180 delimited text through the quartet surfaces: the read-only `DelimitedDocument`, the `DelimitedSerializer` record binder, the mutable `DelimitedNode` DOM, and the token-level `Utf8DelimitedReader` / `Utf8DelimitedWriter`.

## Pattern 1 - query a document

<!-- compile -->
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

## Pattern 2 - typed records via the serializer

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

The header row comes from the record type, so serializing an empty collection writes the header row alone, through every `Serialize` and `SerializeAsync` overload; with `NoHeader` it writes nothing. Scalars parse and format with `InvariantCulture`; `[PropertyName]`, `[Ignore]`, `[Required]`, and `[PropertyOrder]` apply per member, and a property hidden with `new` maps once, to the most derived declaration. An empty field, or one of white space only, binds `null` to a nullable value-type member (`int?`, `DateTime?`, `decimal?`, and so on), except a `char?` member, to which only an empty field binds `null`: it converts any other field as a `char` member does, so a single space binds a space. Other members convert the text as it is, so a `string` member keeps the spaces and an `int` member throws `DelimitedSerializationException`. Temporal values are written in invariant round-trip forms that read back equal:

| Type | Written form | Example |
|---|---|---|
| `DateTime` | `O`, every tick and the kind | `2021-02-06T01:02:03.4567891Z` |
| `DateTimeOffset` | `O`, every tick and the offset | `2021-02-06T01:02:03.4567891+10:00` |
| `DateOnly` | `yyyy-MM-dd` | `2021-02-06` |
| `TimeOnly` | `HH:mm:ss.fffffff` | `01:02:03.4567891` |
| `TimeSpan` | `c` | `1.02:03:04.5670000` |

Reading also accepts the invariant general forms that earlier versions wrote, such as `02/06/2021 01:02:03`.

## Pattern 3 - stream records from a large file

```csharp
await foreach (Trade trade in DelimitedSerializer.DeserializeAsyncEnumerableAsync<Trade>(stream, options))
{
    Process(trade);
}
```

Both directions are genuinely incremental: records are parsed and yielded as stream segments arrive (memory is bounded by the longest record, not the document), and the write direction - `SerializeAsync(stream, records)` where `records` is an `IAsyncEnumerable<Trade>` - encodes each record as it is produced, flushing in bounded batches. A `DelimitedFormatException` from the streaming read reports its line and byte offset in the whole stream, as the buffered `Deserialize` does.

### Reflection-free binding

Annotate a partial record type with `[DelimitedRecord]` and reference the `Bodu.Text.Formats.Generators` source generator, and a static `DelimitedFactory` property (`IDelimitedRecordFactory<Trade>`) is emitted at compile time. Passing it to the factory overloads - `Serialize(records, Trade.DelimitedFactory)` / `Deserialize(csvText, Trade.DelimitedFactory)` - avoids the reflection binder entirely, making the path trimming- and AOT-safe. The interface can also be implemented by hand.

## Pattern 4 - TSV and other dialects

The delimiter, quote, and comment characters live on the reader/writer options. Each must be an ASCII character other than CR and LF, and no two may be the same; the reader and writer constructors throw `ArgumentException` otherwise (see [Parser policies](../../docs/formats/parser-policies.md)):

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

## Pattern 5 - dirty input

<!-- compile -->
```csharp
var lenient = new DelimitedReaderOptions
{
    FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,        // accept short/long rows
    MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord, // skip malformed records whole
    DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeFirst,
};
```

Strict field counts (the default) are measured against the header row and throw `DelimitedFormatException` with the line number and byte offset at which the offending record starts. See [Parser policies](../../docs/formats/parser-policies.md).

## Exceptions

`DelimitedFormatException` for malformed input (position attached); `DelimitedSerializationException` for binding failures (unsupported record type, missing `[Required]` member, non-convertible value).

## When *not* to use it

Nested or typed structures (use TOML/YAML/Bencode), and spreadsheets' native formats (`Bodu.Formats.Excel.Binary` reads `.xls`).

## See also

- [Streams and token-level I/O](streaming.md) for the `Utf8DelimitedReader` token loop.
- [Choosing a text format](choosing-a-format.md)
