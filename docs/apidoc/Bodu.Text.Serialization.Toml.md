---
uid: Bodu.Text.Serialization.Toml
---

![Bodu.Text.Serialization.Toml](~/images/hero-serialization.svg)

## Purpose

**Bodu.Text.Serialization.Toml** is the TOML object-mapper of the Bodu suite: a [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializer)-style serializer that binds your types to and from [TOML](https://toml.io/) v1.0.0 / v1.1.0. It builds on the shared <xref:Bodu.Text.Serialization> engine — converters, options, naming policies, and attributes all apply.

The document root must be a table, so the type serialized at the root maps to an object. Parsing produces a source-preserving concrete syntax tree (<xref:Bodu.Text.Serialization.Toml.Syntax.TomlSyntaxTree>) whose `ToFullString()` reproduces the input exactly; the reader adapter binds it to your type, and the writer emits canonical, block-style TOML.

For the document-model TOML codec (parse to a `TomlTable` rather than your own types), see <xref:Bodu.Text.Toml> in the **Bodu.Text.Formats** package.

## Static documentation

- **[Using TOML](~/guides/serialization/toml.md)** — type mapping, spec-version selection, and streams.
- **[Bodu.Text.Serialization introduction](~/docs/serialization/index.md)** and **[core concepts](~/docs/serialization/concepts.md)**.

## Key types

- <xref:Bodu.Text.Serialization.Toml.TomlSerializer> — static façade. `Serialize` to `string` / `TextWriter` / `Stream` and `Deserialize<T>` from `string` / `ReadOnlySpan<char>` / `Stream`, sync and async.
- <xref:Bodu.Text.Serialization.Toml.TomlSerializerOptions> — extends <xref:Bodu.Text.Serialization.FormatSerializerOptions> with the `SpecVersion` selector.
- <xref:Bodu.Text.Serialization.Toml.Syntax.TomlSyntaxTree> — the lossless parse entry point.
- <xref:Bodu.Text.Serialization.Toml.Syntax.TomlDocumentSyntax>, <xref:Bodu.Text.Serialization.Toml.Syntax.TomlTableSyntax> — the document model.
- <xref:Bodu.Text.Serialization.Toml.TomlFormatException> — a parse failure with line, column, and offset.
