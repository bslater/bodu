---
uid: Bodu.Text.Ini.Document
---

![Bodu.Text.Ini.Document](~/images/hero-ini.svg)

## Purpose

**Bodu.Text.Ini.Document** is the read-only document object model of <xref:Bodu.Text.Ini>, shaped after `System.Text.Json`'s `JsonDocument` / `JsonElement`. A parse produces a disposable <xref:Bodu.Text.Ini.Document.IniDocument> over the normalized object-of-objects shape — global keys hoisted onto the root, sections as nested objects, duplicates resolved by the <xref:Bodu.Text.Ini.IniDocumentOptions> policies; each <xref:Bodu.Text.Ini.Document.IniElement> is a lightweight `struct` cursor. It is trivia-free: prefer it over the mutable <xref:Bodu.Text.Ini.Nodes> tier when you only need to query.

## Key types

- <xref:Bodu.Text.Ini.Document.IniDocument> — the disposable owner: `Parse` (from `string` or UTF-8 bytes, with optional <xref:Bodu.Text.Ini.Reader.IniReaderOptions> and <xref:Bodu.Text.Ini.IniDocumentOptions>) and `RootElement`.
- <xref:Bodu.Text.Ini.Document.IniElement> — the cursor: `ValueKind` (a <xref:Bodu.Text.Ini.IniValueKind>), `GetString`, `GetProperty` / `TryGetProperty`, and `EnumerateObject`.
- <xref:Bodu.Text.Ini.Document.IniElement.ObjectEnumerator> — the struct enumerator returned by `EnumerateObject`.
- <xref:Bodu.Text.Ini.Document.IniProperty> — a `Name` / `Value` pair yielded by `EnumerateObject`.

## Example

```csharp
using Bodu.Text.Ini.Document;

using IniDocument document = IniDocument.Parse("timeout=30\n[server]\nhost=localhost\nport=8080\n");

string timeout = document.RootElement.GetProperty("timeout").GetString();   // global key
IniElement server = document.RootElement.GetProperty("server");
foreach (IniProperty entry in server.EnumerateObject())
    Console.WriteLine($"{entry.Name}={entry.Value.GetString()}");
```

## Notes

- **Lifetime.** Elements are only valid while their document is undisposed.
- **String-only values.** Every entry is a `String` element; a section is an `Object`. Convert numbers yourself or bind through the <xref:Bodu.Text.Ini.IniSerializer>.
- **Comments are dropped.** Use the <xref:Bodu.Text.Ini.Nodes.IniNode> DOM when comments must survive a rewrite.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [INI guide](~/guides/formats/ini.md) (Pattern 1 — query a document).
