---
uid: Bodu.Text.Yaml.Writer
---

![Bodu.Text.Yaml.Writer](~/images/hero-yaml.svg)

## Purpose

**Bodu.Text.Yaml.Writer** is the emitting counterpart of <xref:Bodu.Text.Yaml.Reader> in <xref:Bodu.Text.Yaml>: a forward-only writer that produces a block-style YAML document into an `IBufferWriter<byte>`, in the manner of `Utf8JsonWriter`. The <xref:Bodu.Text.Yaml.YamlSerializer> and both DOMs (`WriteTo`) emit through it, and a custom <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> writes its value through it.

## Key types

- <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> — the `ref struct` writer: `WriteStartMapping` / `WriteEndMapping`, `WriteStartSequence` / `WriteEndSequence`, `WritePropertyName`, and the scalar writers `WriteString`, `WriteInteger`, `WriteDouble`, `WriteBoolean`, `WriteNull`.
- <xref:Bodu.Text.Yaml.Writer.YamlWriterOptions> — `IndentSize`, `MaxDepth`, and `NewLine`.

## Example

```csharp
using System.Buffers;
using System.Text;
using Bodu.Text.Yaml.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8YamlWriter(buffer);

writer.WriteStartMapping();
writer.WritePropertyName("host");
writer.WriteString("localhost");
writer.WritePropertyName("port");
writer.WriteInteger(8080);
writer.WriteEndMapping();

string yaml = Encoding.UTF8.GetString(buffer.WrittenSpan);   // "host: localhost\nport: 8080\n"
```

## Notes

- **Safe scalar presentation.** Each string is written plain when unambiguous and double-quoted otherwise, so a value such as `"yes"` or `"8080"` reads back as a string; empty collections are emitted as `{}` / `[]` so they round-trip as empty rather than null.
- **Block style only.** The writer emits block mappings and sequences with configurable indentation; anchors, aliases, tags, and comments are not written.
- **Shared state.** The writer is a `ref struct` whose state lives behind one shared reference, so a copy passed by value continues the same document.
- **See also:** the [Bodu.Text.Yaml introduction](~/docs/serialization/yaml/index.md) and the [Using YAML](~/guides/serialization/yaml/using.md) guide (Pattern 9 — Drive the low-level reader and writer).
