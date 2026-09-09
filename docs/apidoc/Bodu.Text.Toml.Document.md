---
uid: Bodu.Text.Toml.Document
---

![Bodu.Text.Toml.Document](~/images/hero-toml.svg)

## Purpose

**Bodu.Text.Toml.Document** is the read-only document object model of <xref:Bodu.Text.Toml>, shaped after `System.Text.Json`'s `JsonDocument` / `JsonElement`. A parse produces a single disposable <xref:Bodu.Text.Toml.Document.TomlDocument> that indexes the input once; every <xref:Bodu.Text.Toml.Document.TomlElement> is a lightweight `struct` cursor into that index. Prefer it over the mutable <xref:Bodu.Text.Toml.Nodes> tier for inspection, and note that an `object`-typed member deserialized by the <xref:Bodu.Text.Toml.TomlSerializer> reads back as a `TomlElement`.

## Key types

- <xref:Bodu.Text.Toml.Document.TomlDocument> — the disposable owner: `Parse` (from `string` or `ReadOnlySpan<byte>`) and `RootElement`.
- <xref:Bodu.Text.Toml.Document.TomlElement> — the value cursor: `ValueKind`, the scalar accessors `GetString` / `GetInt64` / `GetDouble` / `GetBoolean` / `GetDateTimeOffset` / `GetDateTime` / `GetDateOnly` / `GetTimeOnly`, `GetProperty` / `TryGetProperty`, an integer indexer, `GetArrayLength`, `EnumerateArray` / `EnumerateObject`, and `WriteTo`.
- <xref:Bodu.Text.Toml.Document.TomlElement.ArrayEnumerator> / <xref:Bodu.Text.Toml.Document.TomlElement.ObjectEnumerator> — the struct enumerators returned by `EnumerateArray` / `EnumerateObject`.
- <xref:Bodu.Text.Toml.Document.TomlProperty> — a `Name` / `Value` pair yielded by `EnumerateObject`.
- <xref:Bodu.Text.Toml.Document.TomlDocumentOptions> — `SpecVersion` and `MaxDepth`.

## Example

```csharp
using Bodu.Text.Toml.Document;

using TomlDocument document = TomlDocument.Parse("[server]\nhost = \"localhost\"\nport = 8080\n");
TomlElement server = document.RootElement.GetProperty("server");

long port = server.GetProperty("port").GetInt64();
foreach (TomlProperty property in server.EnumerateObject())
    Console.WriteLine($"{property.Name}: {property.Value.ValueKind}");
```

## Notes

- **Lifetime.** Elements are only valid while their document is undisposed.
- **Kind-checked accessors.** Calling an accessor that does not match `ValueKind` throws <xref:System.InvalidOperationException>; check `ValueKind` (a <xref:Bodu.Text.Toml.TomlValueKind>) first when the shape is not known.
- **See also:** the [Bodu.Text.Toml introduction](~/docs/serialization/toml/index.md) and the [Using TOML](~/guides/serialization/toml/using.md) guide (Pattern 7 — Inspect a document with the read-only DOM).
