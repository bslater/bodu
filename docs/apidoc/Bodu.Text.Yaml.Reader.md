---
uid: Bodu.Text.Yaml.Reader
---

![Bodu.Text.Yaml.Reader](~/images/hero-yaml.svg)

## Purpose

**Bodu.Text.Yaml.Reader** is the lowest tier of <xref:Bodu.Text.Yaml>: a forward-only token cursor over a parsed YAML document, mirroring the `Utf8JsonReader` surface. Unlike its Bencode and TOML siblings the reader is **buffered** — construction parses the input into an in-memory node store (resolving anchors, aliases, tags, and merge keys) and `Read` then walks that store in document order. The <xref:Bodu.Text.Yaml.YamlSerializer> and both DOMs are built over it, and a custom <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> consumes values through it.

## Key types

- <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> — the `ref struct` cursor: `Read` / `Skip`, `TokenType` (a <xref:Bodu.Text.Yaml.YamlTokenType>), `CurrentDepth`, the scalar accessors `GetString` / `GetInt64` / `GetDouble` / `GetBoolean`, and `ValueTextEquals`.
- <xref:Bodu.Text.Yaml.Reader.YamlReaderOptions> — `SpecVersion` (a <xref:Bodu.Text.Yaml.YamlSpecVersion>), `DuplicateKeyBehavior`, `MergeKeyBehavior`, and `MaxDepth`.

## Example

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;

var reader = new Utf8YamlReader("host: localhost\nport: 8080\n"u8);

while (reader.Read())
{
    if (reader.TokenType == YamlTokenType.PropertyName && reader.ValueTextEquals("port"u8))
    {
        reader.Read();
        long port = reader.GetInt64();   // 8080
    }
}
```

## Notes

- **Presentation is resolved, not reported.** Scalar style, block vs. flow layout, anchors, and aliases are handled during parsing; the token stream exposes only `StartMapping` / `StartSequence`, `PropertyName`, and the typed scalar kinds (`Null`, `String`, `Integer`, `Float`, `Boolean`).
- **Spec version.** The 1.2 core schema is the default; `V1_1` additionally accepts `yes`/`no`/`on`/`off` and sexagesimal numbers. Malformed input throws <xref:Bodu.Text.Yaml.YamlFormatException> with line, column, and offset.
- **See also:** the [Bodu.Text.Yaml introduction](~/docs/serialization/yaml/index.md) and the [Using YAML](~/guides/serialization/yaml/using.md) guide (Pattern 9 — Drive the low-level reader and writer).
