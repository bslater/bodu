---
uid: Bodu.Text.Toml.Reader
---

![Bodu.Text.Toml.Reader](~/images/hero-toml.svg)

## Purpose

**Bodu.Text.Toml.Reader** is the lowest tier of <xref:Bodu.Text.Toml>: the forward-only token readers over UTF-8 TOML (v1.0.0, or v1.1.0 by opt-in). It exposes two cursors — the source-order lexer <xref:Bodu.Text.Toml.Reader.Utf8TomlReader>, which reports table headers, dotted-key segments, comments, and values as they appear, and the normalized <xref:Bodu.Text.Toml.Reader.TomlDocumentReader>, which walks the resolved table tree and is the reader a custom <xref:Bodu.Text.Toml.Serialization.TomlConverter`1> binds through. The <xref:Bodu.Text.Toml.TomlSerializer> and both DOMs are built over this tier.

## Key types

- <xref:Bodu.Text.Toml.Reader.Utf8TomlReader> — the source-order `ref struct` lexer: `Read` / `Skip` / `TrySkip`, `TokenType` (a <xref:Bodu.Text.Toml.TomlTokenType>), `ValueSpan`, `LineNumber` / `ColumnNumber`, typed `Get*` / `TryGet*` accessors for every TOML scalar kind, `GetComment`, and `ValueTextEquals`; the `isFinalBlock` + state constructors support resumable multi-block reads.
- <xref:Bodu.Text.Toml.Reader.TomlDocumentReader> — the normalized tree-order cursor: `Read` / `Skip`, `TokenType`, `CurrentDepth`, and the `Get*` scalar accessors.
- <xref:Bodu.Text.Toml.Reader.TomlReaderOptions> — `SpecVersion` (a <xref:Bodu.Text.Toml.TomlSpecVersion>) and `MaxDepth`.
- <xref:Bodu.Text.Toml.Reader.TomlReaderState> — the opaque snapshot carried between input blocks of a `Utf8TomlReader`.

## Example

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Toml.Reader;

var reader = new Utf8TomlReader("port = 8080"u8);

while (reader.Read())
{
    if (reader.TokenType == TomlTokenType.Key)
        Console.Write($"{reader.GetString()} = ");
    else if (reader.TokenType == TomlTokenType.Integer)
        Console.WriteLine(reader.GetInt64());
}
```

## Notes

- **Two token streams.** `Utf8TomlReader` yields `TableHeader` / `ArrayTableHeader` / `Key` / `Comment` tokens in file order; `TomlDocumentReader` yields the normalized `StartTable` / `PropertyName` / value shape with headers and dotted keys already resolved.
- **Strict by default.** Parsing enforces v1.0.0; set `SpecVersion` to `V1_1` for the v1.1.0 additions. Malformed input throws <xref:Bodu.Text.Toml.TomlFormatException> with line, column, and offset.
- **See also:** the [Bodu.Text.Toml introduction](~/docs/serialization/toml/index.md) and the [Using TOML](~/guides/serialization/toml/using.md) guide (Pattern 9 — Process tokens by hand).
