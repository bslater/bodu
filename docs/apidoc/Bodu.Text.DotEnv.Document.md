---
uid: Bodu.Text.DotEnv.Document
---

![Bodu.Text.DotEnv.Document](~/images/hero-dotenv.svg)

## Purpose

**Bodu.Text.DotEnv.Document** is the read-only document object model of <xref:Bodu.Text.DotEnv>, shaped after `System.Text.Json`'s `JsonDocument` / `JsonElement`. A parse produces a disposable <xref:Bodu.Text.DotEnv.Document.DotEnvDocument> whose root is the flat object of entries; each <xref:Bodu.Text.DotEnv.Document.DotEnvElement> is a lightweight `struct` cursor over the root or a single string value. Prefer it over the mutable <xref:Bodu.Text.DotEnv.Nodes> tier when you only need to look values up.

## Key types

- <xref:Bodu.Text.DotEnv.Document.DotEnvDocument> — the disposable owner: `Parse` (from `string` or UTF-8 bytes, with an optional <xref:Bodu.Text.DotEnv.Reader.DotEnvReaderOptions>) and `RootElement`.
- <xref:Bodu.Text.DotEnv.Document.DotEnvElement> — the cursor: `ValueKind` (a <xref:Bodu.Text.DotEnv.DotEnvValueKind>), `GetString`, `GetProperty` / `TryGetProperty`, and `EnumerateObject`.
- <xref:Bodu.Text.DotEnv.Document.DotEnvElement.ObjectEnumerator> — the struct enumerator returned by `EnumerateObject`.
- <xref:Bodu.Text.DotEnv.Document.DotEnvProperty> — a `Name` / `Value` pair yielded by `EnumerateObject`.

## Example

```csharp
using Bodu.Text.DotEnv.Document;

using DotEnvDocument env = DotEnvDocument.Parse("export APP_ENV=production\nAPP_PORT=8080\n"u8);

string appEnv = env.RootElement.GetProperty("APP_ENV").GetString();
if (env.RootElement.TryGetProperty("APP_PORT", out DotEnvElement port))
    Console.WriteLine(port.GetString());
```

## Notes

- **Lifetime.** Elements are only valid while their document is undisposed.
- **Export flag not surfaced.** The read-only model exposes names and values only; use the <xref:Bodu.Text.DotEnv.Nodes.DotEnvObject> DOM or the reader's `CurrentIsExport` when the prefix matters.
- **Literal values.** No `${VAR}` interpolation is applied.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [DotEnv guide](~/guides/formats/dotenv.md) (Pattern 1 — query a document).
