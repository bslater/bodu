---
uid: Bodu.Text.Delimited.Writer
---

![Bodu.Text.Delimited.Writer](~/images/hero-delimited.svg)

## Purpose

**Bodu.Text.Delimited.Writer** is the emitting counterpart of <xref:Bodu.Text.Delimited.Reader> in <xref:Bodu.Text.Delimited>: a forward-only writer that produces RFC 4180 delimited text as UTF-8 into an `IBufferWriter<byte>` or a `Stream`. The <xref:Bodu.Text.Delimited.DelimitedSerializer> and the mutable <xref:Bodu.Text.Delimited.Nodes.DelimitedNode> DOM (`WriteTo`) emit through it.

## Key types

- <xref:Bodu.Text.Delimited.Writer.Utf8DelimitedWriter> - the `ref struct` writer: `WriteStartArray` / `WriteEndArray` around the record set, `WriteStartObject` / `WritePropertyName` / `WriteString` / `WriteEndObject` for header-keyed records (or a nested array of `WriteString` for positional rows), plus `Flush`, `Dispose`, `BytesCommitted`, and `BytesPending`.
- <xref:Bodu.Text.Delimited.Writer.DelimitedWriterOptions> - `Delimiter`, `Quote`, `CommentChar`, and `NoHeader`.

## Example

```csharp
using System.Buffers;
using Bodu.Text.Delimited.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { Delimiter = '\t' });

writer.WriteStartArray();
writer.WriteStartObject();
writer.WritePropertyName("symbol");
writer.WriteString("AAPL");
writer.WritePropertyName("qty");
writer.WriteString("10");
writer.WriteEndObject();
writer.WriteEndArray();
writer.Flush();   // "symbol\tqty\r\nAAPL\t10\r\n"
```

## Notes

- **Header from the first record.** The property names of the first object record become the header row; subsequent records contribute value rows only.
- **Minimal quoting.** A field is quoted, with internal quotes doubled, only when the reader would otherwise read it back differently: when it contains the delimiter, the quote character, or a line break; when it begins or ends with a space or a tab; when it is a record's first field and begins with `CommentChar`; or when it is the only field of its record and is empty. Output therefore round-trips through the reader.
- **CRLF line endings.** Every row, the header row included, ends with a carriage return and a line feed, as RFC 4180 requires, whatever the platform.
- **Flush to commit.** Bytes are staged until `Flush` (or `Dispose`); check `BytesPending` to see what is still buffered.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [delimited guide](~/guides/formats/delimited.md).
