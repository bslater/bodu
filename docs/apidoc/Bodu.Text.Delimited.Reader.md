---
uid: Bodu.Text.Delimited.Reader
---

![Bodu.Text.Delimited.Reader](~/images/hero-delimited.svg)

## Purpose

**Bodu.Text.Delimited.Reader** is the lowest tier of <xref:Bodu.Text.Delimited>: a forward-only, allocation-light token reader over UTF-8 delimited text (RFC 4180 CSV, TSV, and other single-character dialects). The <xref:Bodu.Text.Delimited.DelimitedSerializer>, the mutable <xref:Bodu.Text.Delimited.Nodes.DelimitedNode> DOM, and the read-only <xref:Bodu.Text.Delimited.Document.DelimitedDocument> DOM are all built over it, and its options struct is the single place the dialect and strictness policies are declared.

## Key types

- <xref:Bodu.Text.Delimited.Reader.Utf8DelimitedReader> — the `ref struct` cursor: `Read`, `TokenType` (a <xref:Bodu.Text.Delimited.DelimitedTokenType>), `ValueSpan` / `GetString`, `Headers`, `LineNumber`, and `BytesConsumed`.
- <xref:Bodu.Text.Delimited.Reader.DelimitedReaderOptions> — `Delimiter`, `Quote`, `NoHeader`, `TrimFields`, `AllowComments` / `CommentChar`, and the policies <xref:Bodu.Text.Delimited.DelimitedFieldCountBehavior>, <xref:Bodu.Text.Delimited.DelimitedMalformedRecordBehavior>, and <xref:Bodu.Text.Delimited.DelimitedDuplicateHeaderBehavior>.

## Example

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Reader;

var reader = new Utf8DelimitedReader("symbol,qty\nAAPL,10\n"u8);

while (reader.Read())
{
    if (reader.TokenType == DelimitedTokenType.PropertyName)
        Console.Write($"{reader.GetString()}=");
    else if (reader.TokenType == DelimitedTokenType.String)
        Console.WriteLine(reader.GetString());
}
```

## Notes

- **Header-shaped tokens.** With a header row (the default) each record is an object of `PropertyName` / `String` pairs keyed by the header; with `NoHeader` each record is a positional array of `String` tokens.
- **String-only wire.** Every field surfaces as a `String` token; numeric or date conversion belongs to the serializer or the caller.
- **Malformed input** throws <xref:Bodu.Text.Delimited.DelimitedFormatException> unless the relevant behavior policy relaxes it.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [delimited guide](~/guides/formats/delimited.md).
