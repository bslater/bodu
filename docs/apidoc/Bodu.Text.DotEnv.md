---
uid: Bodu.Text.DotEnv
---

![Bodu.Text.DotEnv](~/images/hero-dotenv.svg)

## Purpose

**Bodu.Text.DotEnv** parses and emits **`.env`** files — a flat object of `KEY=value` entries with `export` prefixes, quoting, and comments — as a standalone `System.Text.Json`-shaped library: a typed settings serializer, a forward-only UTF-8 token reader/writer pair, a mutable node DOM, and a read-only document DOM. Values are deliberately literal: no `${VAR}` interpolation happens at parse time. It ships as its own package (also available through the `Bodu.Text.Formats` umbrella); see also <xref:Bodu.Text.Delimited> and <xref:Bodu.Text.Ini>.

## Key types

- <xref:Bodu.Text.DotEnv.DotEnvSerializer> — static serializer: settings classes / dictionaries ↔ DotEnv text.
- <xref:Bodu.Text.DotEnv.DotEnvSerializerOptions> / <xref:Bodu.Text.DotEnv.DotEnvSerializerDefaults> — naming policy and presets (`Web` = SCREAMING_SNAKE_CASE, case-insensitive).
- <xref:Bodu.Text.DotEnv.Reader.Utf8DotEnvReader> / <xref:Bodu.Text.DotEnv.Writer.Utf8DotEnvWriter> — forward-only `ref struct` token surfaces (export detection, quoting, inline comments).
- <xref:Bodu.Text.DotEnv.Document.DotEnvDocument> / <xref:Bodu.Text.DotEnv.Document.DotEnvElement> — read-only, disposable document model.
- <xref:Bodu.Text.DotEnv.Nodes.DotEnvNode> — mutable DOM root; preserves each entry's `export` flag.
- <xref:Bodu.Text.DotEnv.DotEnvFormatException> / <xref:Bodu.Text.DotEnv.DotEnvSerializationException> — malformed input vs. binding failures.

## Example

```csharp
using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Document;

using DotEnvDocument env = DotEnvDocument.Parse("export APP_ENV=production\nAPP_PORT=8080\n"u8);
string appEnv = env.RootElement.GetProperty("APP_ENV").GetString();
```

## Notes

- **Literal values.** What the file says is what your process gets; interpolation belongs to a higher layer.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [DotEnv guide](~/guides/formats/dotenv.md).
