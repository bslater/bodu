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

A record is a `string[]`, whose fields are written and read by position, or an object whose public properties (and, with `IncludeFields`, public fields) map to columns. Reading creates each record through its public parameterless constructor, so a record type read back needs one. A record type may also be a structure, nullable or not: a nullable structure's records have the structure's columns, and a `null` record is left out when records are written, as a `null` record of a class is. A scalar, such as `int`, `string`, `decimal`, an enum, `Guid`, a date or time type or `Uri`, and a collection are not records: every `Serialize`, `SerializeAsync`, `Deserialize` and `DeserializeAsyncEnumerableAsync` overload rejects such a record type with `DelimitedSerializationException` before it reads or writes anything, as it rejects a root that is not a collection, a `string` among them. A dictionary is a collection of `KeyValuePair<TKey, TValue>` records, written with `Key` and `Value` columns, and `Deserialize<KeyValuePair<TKey, TValue>>` and `DeserializeAsyncEnumerableAsync<KeyValuePair<TKey, TValue>>` read them back: although `Key` and `Value` are get-only, each record is created through the pair's constructor from the fields of those columns, matched by header name under the naming policy, as they are written, or by position with `NoHeader`, and a column the document lacks leaves its half at its default. Every other get-only member's column is written and not read back. The source generator cannot annotate `KeyValuePair<TKey, TValue>`, so these records are always bound by reflection.

The header row comes from the record type, so serializing an empty collection writes the header row alone, through every `Serialize` and `SerializeAsync` overload; with `NoHeader` it writes nothing. With `NoHeader` a POCO record also reads by position: each field binds to the member written in its column, the readable members in member order, as a generated factory binds it, so a get-only member's field is skipped and a member past the last field keeps its default. A member is one field, so its type must be a scalar (a string or `char`, a number, `bool`, an enum, a date, time or time span, `Guid` or `Uri`) or `object` holding one: every `Serialize`, `SerializeAsync`, `Deserialize` and `DeserializeAsyncEnumerableAsync` overload rejects a record type with a member of any other type, such as a dictionary or a class, readable, writable or both, with `DelimitedSerializationException` naming the column, before it reads or writes anything, and writing rejects an `object` member holding such a value when it reaches it. No such value is written as its `ToString()` text, which does not read back, and none is read back from a missing column. Scalars parse and format with `InvariantCulture`; `[PropertyName]`, `[Ignore]` with `IgnoreCondition.Always`, and `[PropertyOrder]` apply per member, and a property hidden with `new` maps once, to the most derived declaration. `[Required]` and a conditional `[Ignore]` are not read: a missing column leaves its member at its default, and every other mapped member is always written. An empty field, or one of white space only, binds `null` to a nullable value-type member (`int?`, `DateTime?`, `decimal?`, and so on), except a `char?` member, to which only an empty field binds `null`: it converts any other field as a `char` member does, so a single space binds a space. Other members convert the text as it is, so a `string` member keeps the spaces and an `int` member throws `DelimitedSerializationException`. Temporal values are written in invariant round-trip forms that read back equal:

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

Both directions are genuinely incremental: records are parsed and yielded as stream segments arrive (memory is bounded by the longest record, not the document), and the write direction - `SerializeAsync(stream, records)` where `records` is an `IAsyncEnumerable<Trade>` - encodes each record as it is produced, flushing in bounded batches. A `DelimitedFormatException` from the streaming read reports its line and byte offset in the whole stream, as the buffered `Deserialize` does, and a byte-order mark is skipped only at the start of the stream, so a U+FEFF that begins a later record is kept, as `Deserialize` keeps it.

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

`DelimitedFormatException` for malformed input (position attached); `DelimitedSerializationException` for binding failures (a root that is not a collection, a record type that is not a record or cannot be created, a non-convertible value). A missing column is not a failure, since Delimited does not read `[Required]`.

## When *not* to use it

Nested or typed structures (use TOML/YAML/Bencode), and spreadsheets' native formats (`Bodu.Formats.Excel.Binary` reads `.xls`).

## See also

- [Streams and token-level I/O](streaming.md) for the `Utf8DelimitedReader` token loop.
- [Choosing a text format](choosing-a-format.md)
