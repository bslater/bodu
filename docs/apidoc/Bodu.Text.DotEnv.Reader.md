---
uid: Bodu.Text.DotEnv.Reader
---

![Bodu.Text.DotEnv.Reader](~/images/hero-dotenv.svg)

## Purpose

**Bodu.Text.DotEnv.Reader** is the lowest tier of <xref:Bodu.Text.DotEnv>: a forward-only token reader over UTF-8 `.env` text that handles the `export` prefix, single- and double-quoted values with escapes, and full-line and inline comments. The <xref:Bodu.Text.DotEnv.DotEnvSerializer>, the mutable <xref:Bodu.Text.DotEnv.Nodes.DotEnvNode> DOM, and the read-only <xref:Bodu.Text.DotEnv.Document.DotEnvDocument> DOM are all built over it.

## Key types

- <xref:Bodu.Text.DotEnv.Reader.Utf8DotEnvReader> — the `ref struct` cursor: `Read` / `Skip` / `TrySkip`, `TokenType` (a <xref:Bodu.Text.DotEnv.DotEnvTokenType>), `ValueSpan` / `GetString`, `ValueTextEquals`, `CurrentIsExport`, `LineNumber`, `CurrentDepth`, and `BytesConsumed`.
- <xref:Bodu.Text.DotEnv.Reader.DotEnvReaderOptions> — `DisallowExportPrefix`, `DisallowInlineComments`, and `SkipComments`.

## Example

```csharp
using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Reader;

var reader = new Utf8DotEnvReader("export APP_ENV=production\nAPP_PORT=8080 # inline\n"u8);

while (reader.Read())
{
    if (reader.TokenType == DotEnvTokenType.PropertyName)
        Console.WriteLine($"{reader.GetString()} (export: {reader.CurrentIsExport})");
}
```

## Notes

- **One flat object.** The stream is `StartObject`, then `PropertyName` / `String` pairs (with `Comment` tokens unless `SkipComments`), then `EndObject`; there is no nesting.
- **Literal values.** Quoting and escapes are decoded, but `${VAR}` interpolation is never performed — what the file says is what you get.
- **Malformed input** throws <xref:Bodu.Text.DotEnv.DotEnvFormatException>.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [DotEnv guide](~/guides/formats/dotenv.md).
