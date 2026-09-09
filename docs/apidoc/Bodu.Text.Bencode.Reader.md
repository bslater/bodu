---
uid: Bodu.Text.Bencode.Reader
---

![Bodu.Text.Bencode.Reader](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode.Reader** is the lowest tier of <xref:Bodu.Text.Bencode>: a forward-only, allocation-free token reader over canonical Bencode (BEP 3) bytes, in the manner of `Utf8JsonReader`. The <xref:Bodu.Text.Bencode.BencodeSerializer>, the mutable <xref:Bodu.Text.Bencode.Nodes.BencodeNode> DOM, and the read-only <xref:Bodu.Text.Bencode.Document.BencodeDocument> DOM are all built over it, and a custom <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> consumes values through it. Use it directly when you need full control over a token stream without materializing a document.

## Key types

- <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> — the `ref struct` token cursor: `Read` / `Skip` / `TrySkip`, `TokenType` (a <xref:Bodu.Text.Bencode.BencodeTokenType>), `ValueSpan`, and the typed accessors `GetString` / `GetBytes` / `GetInt32` / `GetInt64` / `GetUInt64` with their `TryGet*` counterparts, plus `ValueTextEquals` for allocation-free key matching.
- <xref:Bodu.Text.Bencode.Reader.BencodeReaderOptions> — `MaxDepth`, and the `AllowUnsortedKeys` / `AllowDuplicateKeys` relaxations for non-canonical producers.

## Example

```csharp
using Bodu.Text.Bencode;
using Bodu.Text.Bencode.Reader;

var reader = new Utf8BencodeReader("d6:lengthi1024e4:name10:ubuntu.isoe"u8);

while (reader.Read())
{
    if (reader.TokenType == BencodeTokenType.PropertyName && reader.ValueTextEquals("name"u8))
    {
        reader.Read();
        string name = reader.GetString();   // "ubuntu.iso"
    }
}
```

## Notes

- **Canonical by default.** The reader rejects unsorted or duplicate dictionary keys and non-minimal integers unless the corresponding option is set; malformed bytes throw <xref:Bodu.Text.Bencode.BencodeFormatException>.
- **Byte strings, not text.** `ValueSpan` and `GetBytes` return the raw byte string; `GetString` decodes it as UTF-8.
- **See also:** the [Bodu.Text.Bencode introduction](~/docs/serialization/bencode/index.md) and the [Using Bencode](~/guides/serialization/bencode/using.md) guide (Pattern 9 — Process tokens by hand).
