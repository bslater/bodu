---
uid: Bodu.Text.Toml.Writer
---

![Bodu.Text.Toml.Writer](~/images/hero-toml.svg)

## Purpose

**Bodu.Text.Toml.Writer** is the emitting counterpart of <xref:Bodu.Text.Toml.Reader> in <xref:Bodu.Text.Toml>: a token writer that produces normalized, block-style TOML into an `IBufferWriter<byte>` or a `Stream`. The <xref:Bodu.Text.Toml.TomlSerializer> and both DOMs (`WriteTo`) emit through it, and a custom <xref:Bodu.Text.Toml.Serialization.TomlConverter`1> writes its value through it.

## Key types

- <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> — the `ref struct` writer: `WriteStartTable` / `WriteEndTable`, `WriteStartArray` / `WriteEndArray`, `WritePropertyName`, the scalar writers (`WriteString`, `WriteInteger`, `WriteFloat`, `WriteBoolean`, `WriteOffsetDateTime`, `WriteLocalDateTime`, `WriteLocalDate`, `WriteLocalTime`) and their name-plus-value conveniences, plus `Flush` / `Reset` / `Dispose` for the stream constructors.
- <xref:Bodu.Text.Toml.Writer.TomlWriterOptions> — `SpecVersion` and `MaxDepth`.

## Example

```csharp
using System.Buffers;
using Bodu.Text.Toml.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8TomlWriter(buffer);

writer.WriteStartTable();               // the required root table
writer.WriteString("name", "app");
writer.WriteStartTable("server");       // emitted as a [server] block
writer.WriteString("host", "localhost");
writer.WriteInteger("port", 8080);
writer.WriteEndTable();
writer.WriteEndTable();                 // closing the root emits the document
```

## Notes

- **Not progressive.** TOML layout is a whole-document property, so the writer buffers a value tree and serializes it when the root table closes: scalars and arrays first as `key = value` lines, then sub-tables as `[dotted.path]` blocks, arrays of tables as `[[path]]` runs, and tables inside arrays as inline `{ … }`.
- **Version-neutral output.** The emitted text is valid under both v1.0.0 and v1.1.0 regardless of `SpecVersion`.
- **See also:** the [Bodu.Text.Toml introduction](~/docs/serialization/toml/index.md) and the [Using TOML](~/guides/serialization/toml/using.md) guide (Pattern 9 — Process tokens by hand).
