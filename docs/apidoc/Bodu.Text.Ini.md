---
uid: Bodu.Text.Ini
---

![Bodu.Text.Ini](~/images/hero-ini.svg)

## Purpose

**Bodu.Text.Ini** parses and emits **INI**-style configuration documents — section-organised `key = value` lines with optional comments. It is one of four format namespaces shipped by the **Bodu.Text.Formats** package; see also <xref:Bodu.Text.Bencode>, <xref:Bodu.Text.Delimited>, and <xref:Bodu.Text.DotEnv>.

INI is exposed through the same shape used across the format family: a strongly-typed value tree, a static codec with `Parse` / `Format` / `Load` / `Save` over `ReadOnlySpan<char>` / `string` / `Stream` / `TextReader` / `TextWriter`, and parser invariants the writer always honours and the parser always enforces. The model preserves comments, key order, and section order so round-tripping is byte-faithful under default options.

For EditorConfig-style configuration layering over <xref:Bodu.Text.Ini.IniDocument> with profile-driven parse and resolve options, see <xref:Bodu.Text.Configuration>. For the Microsoft.Extensions.Configuration bridge, see <xref:Bodu.Extensions.Configuration.Text>.

## Static documentation

- **[Bodu.Text.Formats introduction](~/docs/formats/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Text.Formats core concepts](~/docs/formats/concepts.md)** — vocabulary: format vs codec, value vs document, framing tokens, canonical encoding, byte string vs text, format exception.
- **[Bodu.Text.Formats getting started](~/docs/formats/getting-started.md)** — install and minimal samples.

## Key types

- <xref:Bodu.Text.Ini.IniDocument> — root model: a preamble (global section) and zero or more named sections in source order. Mutable; supports round-trip parse + save.
- <xref:Bodu.Text.Ini.IniSection> — a named section with an ordered list of <xref:Bodu.Text.Ini.IniEntry> values, plus comment lines preserved verbatim.
- <xref:Bodu.Text.Ini.IniEntry> — a single `key = value` line with optional trailing comment.
- <xref:Bodu.Text.Ini.IniComment> — a preserved comment line, with prefix character (`;` or `#`) and trimmed text.
- <xref:Bodu.Text.Ini.Ini> — static codec for INI files: `Parse(text, options?)`, `Load(path | Stream)`, `Save(document, path | Stream | TextWriter, options?)`.
- <xref:Bodu.Text.Ini.IniParseOptions> — duplicate-key, duplicate-section, comment-preservation, case-sensitivity options for the INI parser.
- <xref:Bodu.Text.DuplicateKeyPolicy> — `LastWins`, `FirstWins`, `Disallowed`, `Merge`.
- <xref:Bodu.Text.Ini.IniDuplicateSectionBehavior> — `Preserve`, `Merge`, `Disallowed`.
- <xref:Bodu.Text.Ini.IniFormatException> — derives from <xref:System.FormatException>; thrown when the parser cannot reconcile a structural invariant.

## Example

```csharp
using Bodu.Text.Ini;

IniDocument iniDoc = Ini.Parse("""
[server]
host = localhost
port = 8080
""");

string host = iniDoc.Sections[0]["host"]!;     // "localhost"
int port = iniDoc.Sections[0].GetValue<int>("port");

using StringWriter sw = new();
Ini.Save(iniDoc, sw);   // re-emits the canonical form
```

## Notes

- **Comment preservation.** Leading and trailing comments are stored on the section or entry they precede or follow, so `Parse` followed by `Save` round-trips an unchanged document byte-for-byte under default options.
- **Case sensitivity.** Default key comparison is ordinal-ignore-case to match common INI dialects; set <xref:Bodu.Text.Ini.IniParseOptions> to opt into case-sensitive comparison.
- **Duplicate handling.** Both keys within a section and sections within a document have configurable duplicate behaviour via <xref:Bodu.Text.DuplicateKeyPolicy> and <xref:Bodu.Text.Ini.IniDuplicateSectionBehavior>.
- **No dotted-key splitting.** Unlike configuration overlays, the INI primitive does not split `a.b.c = value` into segments. Use <xref:Bodu.Text.Configuration> when colon- or dot-delimited hierarchical keys are required.
- **See also:** [Bodu.Text.Configuration](~/docs/text-configuration/index.md) for the EditorConfig-style layering on top of `IniDocument`.
