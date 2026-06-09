---
uid: Bodu.Text.Delimited
---

![Bodu.Text.Delimited](~/images/hero-delimited.svg)

## Purpose

**Bodu.Text.Delimited** parses and emits **delimited** records — CSV, TSV, and other character-separated value documents — using a strongly-typed row model and a static codec with `Parse` / `Format` / `Try*` overloads over `ReadOnlySpan<char>` / `string` / `Stream` / `TextReader` / `TextWriter`. It is one of five format namespaces shipped by the **Bodu.Text.Formats** package; see also <xref:Bodu.Text.Bencode>, <xref:Bodu.Text.DotEnv>, <xref:Bodu.Text.Ini>, and <xref:Bodu.Text.Toml>.

## Key types

- <xref:Bodu.Text.Delimited.Delimited> — static codec exposing `Parse`, `Format`, `TryParse`, and the streaming variants.
- <xref:Bodu.Text.Delimited.DelimitedDocument> — parsed document: ordered <xref:Bodu.Text.Delimited.DelimitedRow> entries with header-keyed and index-keyed lookups.
- <xref:Bodu.Text.Delimited.DelimitedRow> — a single record with field values addressable by column name or index.
- <xref:Bodu.Text.Delimited.DelimitedReader> — forward-only streaming reader over a `TextReader` source.
- <xref:Bodu.Text.Delimited.DelimitedWriter> — append-only streaming writer over a `TextWriter` sink.
- <xref:Bodu.Text.Delimited.DelimitedParseOptions> — delimiter, quote, escape, header-presence, trim, and newline options for the parser and writer.
- <xref:Bodu.Text.Delimited.DelimitedFormatException> — derives from <xref:System.FormatException>; thrown on malformed input (unterminated quote, header / column count mismatch, etc.).

## Example

```csharp
using Bodu.Text.Delimited;

DelimitedDocument doc = Delimited.Parse(
    "name,age,city\nAlice,30,Paris\nBob,25,London");

Assert.AreEqual(2, doc.Rows.Count);
Assert.AreEqual("Alice", doc.Rows[0]["name"]);

string csv = Delimited.Format(doc);
```

## Notes

- **Streaming preferred for large inputs.** Use <xref:Bodu.Text.Delimited.DelimitedReader> / <xref:Bodu.Text.Delimited.DelimitedWriter> instead of materialising a full `DelimitedDocument` when the input is large or unbounded.
- **Round-trip determinism.** Under default options, parse then format reproduces the canonical form (quoting only where required by the configured delimiter / quote characters).
- **See also:** the [Bodu.Text.Formats introduction](~/docs/formats/index.md) and [getting-started](~/docs/formats/getting-started.md) pages.
