---
uid: Bodu.Text.Ini
---

![Bodu.Text.Ini](~/images/hero-ini.svg)

## Purpose

**Bodu.Text.Ini** parses and emits **INI** documents — global keys plus `[section]` blocks of `key=value` entries — as a standalone `System.Text.Json`-shaped library: a typed serializer, a two-reader token surface, a **comment-preserving** mutable node DOM, and a read-only document DOM. It ships as its own package (also available through the `Bodu.Text.Formats` umbrella); see also <xref:Bodu.Text.Delimited> and <xref:Bodu.Text.DotEnv>. For EditorConfig-style layered configuration, use `Bodu.Text.Configuration` instead — it carries its own INI model.

## Key types

- <xref:Bodu.Text.Ini.IniSerializer> — static serializer: section POCOs / nested dictionaries ↔ INI text, with the `GlobalSectionName` mapping and the depth-2 gate.
- <xref:Bodu.Text.Ini.IniSerializerOptions> / <xref:Bodu.Text.Ini.IniSerializerDefaults> — naming policy, duplicate policies, and presets (`Strict` = configparser strict mode).
- <xref:Bodu.Text.Ini.IniDocumentOptions> with <xref:Bodu.Text.Ini.IniDuplicateSectionBehavior> / <xref:Bodu.Text.Ini.IniDuplicateKeyBehavior> — how repeated sections and keys resolve when the document materializes.
- <xref:Bodu.Text.Ini.Reader.Utf8IniReader> — forward-only `ref struct` reader over the file as authored (source order, comments included).
- <xref:Bodu.Text.Ini.Reader.IniDocumentReader> — normalized cursor over the logical object-of-objects shape (globals hoisted, duplicate sections merged).
- <xref:Bodu.Text.Ini.Writer.Utf8IniWriter> — forward-only `ref struct` writer (section headers, entries, comment lines).
- <xref:Bodu.Text.Ini.Document.IniDocument> / <xref:Bodu.Text.Ini.Document.IniElement> — read-only, trivia-free, disposable document model.
- <xref:Bodu.Text.Ini.Nodes.IniNode> / <xref:Bodu.Text.Ini.Nodes.IniObject> / <xref:Bodu.Text.Ini.Nodes.IniValue> — mutable, comment-preserving DOM for faithful rewrites of human-owned files.
- <xref:Bodu.Text.Ini.IniFormatException> / <xref:Bodu.Text.Ini.IniSerializationException> — malformed input / policy violations vs. binding failures.

## Example

```csharp
using Bodu.Text.Ini.Nodes;

IniObject root = IniNode.Parse("[server]\nhost=localhost\nport=8080\n"u8);
root["server"].AsObject()["port"].AsValue().Value = "9090";
byte[] back = root.ToUtf8Bytes();   // comments (had there been any) survive
```

## Notes

- **Conservative dialect.** `=` only; values run literally to end of line (an inline `;` is content); `;` and `#` start full-line comments.
- **Duplicate handling is a document-model policy.** The source-order reader reports the file verbatim; `Merge` / `LastWins` (the defaults) apply when the document materializes.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [INI guide](~/guides/formats/ini.md).
