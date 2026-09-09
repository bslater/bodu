---
uid: Bodu.Text.Ini.Reader
---

![Bodu.Text.Ini.Reader](~/images/hero-ini.svg)

## Purpose

**Bodu.Text.Ini.Reader** is the lowest tier of <xref:Bodu.Text.Ini> and exposes two forward-only cursors over UTF-8 INI text. <xref:Bodu.Text.Ini.Reader.Utf8IniReader> reports the file **as authored** — section headers, entries, and comments in source order, duplicates included — while <xref:Bodu.Text.Ini.Reader.IniDocumentReader> walks the **normalized** object-of-objects shape (global keys hoisted, duplicate sections and keys resolved by the <xref:Bodu.Text.Ini.IniDocumentOptions> policies) that the <xref:Bodu.Text.Ini.IniSerializer> and both DOMs bind through.

## Key types

- <xref:Bodu.Text.Ini.Reader.Utf8IniReader> — the source-order `ref struct` lexer: `Read`, `TokenType` (a <xref:Bodu.Text.Ini.IniTokenType>: `SectionHeader`, `PropertyName`, `String`, `Comment`), `GetString`, `LineNumber`, and `BytesConsumed`.
- <xref:Bodu.Text.Ini.Reader.IniDocumentReader> — the normalized cursor: `Read`, `TokenType` (`StartObject` / `EndObject` / `PropertyName` / `String`), `GetString`, and `CurrentDepth`.
- <xref:Bodu.Text.Ini.Reader.IniReaderOptions> — `DisallowHashComments` and `SkipComments`.

## Example

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Ini.Reader;

var reader = new Utf8IniReader("; owned by ops\n[server]\nhost=localhost\n"u8);

while (reader.Read())
{
    if (reader.TokenType == IniTokenType.SectionHeader)
        Console.WriteLine($"[{reader.GetString()}] at line {reader.LineNumber}");
    else if (reader.TokenType == IniTokenType.Comment)
        Console.WriteLine($"comment: {reader.GetString()}");
}
```

## Notes

- **Choose the reader by intent.** Use `Utf8IniReader` for linting, comment handling, or faithful transcription; use `IniDocumentReader` when you want the logical configuration shape with duplicates already merged.
- **Conservative dialect.** `=` is the only separator; values run literally to end of line; `;` and `#` start full-line comments only.
- **Malformed input** throws <xref:Bodu.Text.Ini.IniFormatException>.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [INI guide](~/guides/formats/ini.md) (The two readers).
