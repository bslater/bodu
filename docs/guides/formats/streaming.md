---
title: Streams and token-level I/O
---

# Streams and token-level I/O

For inputs too large (or too hot) to materialize, each format exposes its forward-only `Utf8*Reader` / `Utf8*Writer` pair, and Delimited adds a typed record-streaming surface on its serializer.

## The token surface

The readers are `ref struct` cursors over `ReadOnlySpan<byte>`: `Read()` advances, `TokenType` reports the token, `GetString()` decodes it, and `LineNumber` / `BytesConsumed` locate it. The writers emit UTF-8 to an `IBufferWriter<byte>` or a `Stream` (call `Flush()` to commit in stream mode).

## Pattern 1 — walk delimited records one at a time

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Reader;

var reader = new Utf8DelimitedReader(csvBytes);
var fields = new List<string>();

while (reader.Read())
{
    switch (reader.TokenType)
    {
        case DelimitedTokenType.StartObject:   // StartArray in NoHeader mode
            fields.Clear();
            break;
        case DelimitedTokenType.String:
            fields.Add(reader.GetString());
            break;
        case DelimitedTokenType.EndObject:
            ProcessRow(fields);                // one record in memory at a time
            break;
    }
}
```

`reader.Headers` exposes the header row once it has been read.

## Pattern 2 — stream typed records

For typed rows, skip the token loop:

```csharp
await foreach (Trade trade in DelimitedSerializer.DeserializeAsyncEnumerableAsync<Trade>(stream))
{
    Process(trade);
}

await DelimitedSerializer.SerializeAsync(output, ProduceTradesAsync());  // IAsyncEnumerable<Trade> in
```

## Pattern 3 — scan a DotEnv source

```csharp
using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Reader;

var reader = new Utf8DotEnvReader(envBytes);
string? key = null;

while (reader.Read())
{
    if (reader.TokenType == DotEnvTokenType.PropertyName)
        key = reader.GetString();
    else if (reader.TokenType == DotEnvTokenType.String)
        Inspect(key!, reader.GetString(), reader.LineNumber);
}
```

## Pattern 4 — stream INI tokens as authored

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Ini.Reader;

var reader = new Utf8IniReader(iniBytes);

while (reader.Read())
{
    switch (reader.TokenType)
    {
        case IniTokenType.SectionHeader: EnterSection(reader.GetString()); break;
        case IniTokenType.PropertyName:  currentKey = reader.GetString(); break;
        case IniTokenType.String:        OnEntry(currentKey, reader.GetString()); break;
        case IniTokenType.Comment:       /* trivia */ break;
    }
}
```

Use the normalized `IniDocumentReader` when you want the logical object shape (globals hoisted, duplicate sections merged) instead of the physical file order — note it parses the whole document in its constructor, because merge is out-of-order.

## Pattern 5 — write tokens progressively

```csharp
using System.Buffers;
using Bodu.Text.Delimited.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8DelimitedWriter(buffer);
writer.WriteStartArray();
foreach (var row in rows)
{
    writer.WriteStartObject();
    writer.WritePropertyName("symbol"); writer.WriteString(row.Symbol);
    writer.WriteEndObject();
}
writer.WriteEndArray();
writer.Flush();
```

The DotEnv and INI writers are line-oriented (`WritePropertyName` + `WriteString` per entry; `WriteSectionHeader` / `WriteComment` for INI), so output is emitted as you go.

## Async facades

The `*Serializer` stream overloads (`SerializeAsync` / `DeserializeAsync`) buffer the document in full — only the stream copy is asynchronous. The one genuinely incremental async surface today is Delimited's record streaming (Pattern 2).

## Mid-stream errors

Readers throw their `*FormatException` at the offending token with `LineNumber` / byte offset attached; everything already consumed remains valid. The ref-struct readers hold no unmanaged resources — abandoning one is safe.

## See also

- [Parser policies](../../docs/formats/parser-policies.md)
- The per-format guides: [Delimited](delimited.md) · [DotEnv](dotenv.md) · [INI](ini.md)
