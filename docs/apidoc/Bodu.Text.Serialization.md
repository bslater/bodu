---
uid: Bodu.Text.Serialization
---

![Bodu.Text.Serialization](~/images/hero-serialization.svg)

## Purpose

**Bodu.Text.Serialization** is the shared engine behind the Bodu object-mapping serializers — a [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializer)-style stack that binds your own types to and from document formats. It owns the reflection-based binder, the converter and options model, and the lossless concrete-syntax-tree abstraction; the per-format packages (<xref:Bodu.Text.Serialization.Toml>, <xref:Bodu.Text.Serialization.Bencode>) supply the parser, the syntax tree, and the reader/writer adapters.

A converter reads and writes **format-neutral value tokens** through <xref:Bodu.Text.Serialization.ISerializationReader> and <xref:Bodu.Text.Serialization.ISerializationWriter>, so a converter written once works for every format. The <xref:Bodu.Text.Serialization.Syntax.SyntaxNode> base carries an integer `RawKind` "type code" that each format maps onto its own `Kind` enumeration, the way `System.Text.Json` exposes a `JsonValueKind`.

## Static documentation

- **[Bodu.Text.Serialization introduction](~/docs/serialization/index.md)** — packages, headline types, scenarios.
- **[Core concepts](~/docs/serialization/concepts.md)** — the syntax tree, the adapter seam, converter resolution, and options.
- **[Getting started](~/docs/serialization/getting-started.md)** — install and the first round trip.
- **[Writing converters](~/guides/serialization/converters.md)** — custom shapes with `FormatConverter<T>`.

## Key types

- <xref:Bodu.Text.Serialization.FormatConverter`1> — base for a custom converter; `Read` and `Write` over the neutral seam.
- <xref:Bodu.Text.Serialization.FormatConverterFactory> — produces converters for a family of types.
- <xref:Bodu.Text.Serialization.FormatSerializerOptions> — converters, naming policy, null handling, and depth; cached and frozen on first use.
- <xref:Bodu.Text.Serialization.FormatNamingPolicy> — camel, snake, and kebab casing policies.
- <xref:Bodu.Text.Serialization.FormatPropertyNameAttribute>, <xref:Bodu.Text.Serialization.FormatIgnoreAttribute>, <xref:Bodu.Text.Serialization.FormatConverterAttribute> — the member and type attributes.
- <xref:Bodu.Text.Serialization.ISerializationReader>, <xref:Bodu.Text.Serialization.ISerializationWriter>, <xref:Bodu.Text.Serialization.SerializationValueKind> — the format-neutral value seam.
- <xref:Bodu.Text.Serialization.Syntax.SyntaxNode> — the concrete-syntax-tree base carrying `RawKind`.
- <xref:Bodu.Text.Serialization.FormatSerializationException> — a binding-level failure.
