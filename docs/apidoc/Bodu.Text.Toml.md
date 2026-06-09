---
uid: Bodu.Text.Toml
---

![Bodu.Text.Toml](~/images/hero-toml.svg)

## Purpose

**Bodu.Text.Toml** parses and emits **TOML** (Tom's Obvious, Minimal Language) documents — the configuration format defined by the [TOML specification](https://toml.io/). It is one of five format namespaces shipped by the **Bodu.Text.Formats** package; see also <xref:Bodu.Text.Bencode>, <xref:Bodu.Text.Delimited>, <xref:Bodu.Text.DotEnv>, and <xref:Bodu.Text.Ini>.

TOML is exposed through a **reader/writer pair** layered under a convenience codec, the shape the package shares with <xref:Bodu.Text.Bencode>: a strongly-typed value tree (<xref:Bodu.Text.Toml.TomlTable> at the root), a <xref:Bodu.Text.Toml.TomlReader> that deserializes text into that model, a <xref:Bodu.Text.Toml.TomlWriter> that serializes it back to canonical TOML, and the static <xref:Bodu.Text.Toml.Toml> façade for one-line `Parse` / `Format`. The grammar covers every TOML value kind — strings (all four forms), integers in four radices, floats (including `inf` / `nan`), booleans, the four RFC 3339 date-time types, arrays, inline tables, standard tables, and arrays of tables.

The reader enforces strict **TOML v1.0.0** by default and opts in to **TOML v1.1.0** grammar additions through <xref:Bodu.Text.Toml.TomlReaderOptions>. The writer always emits constructs valid under both versions.

For EditorConfig-style INI configuration, see <xref:Bodu.Text.Ini>; for binary-to-text encodings (Base16 / Base32 / Base64 / Base58 / Base85) that operate on flat byte sequences without a structural grammar, see the companion <xref:Bodu.Text.Encoding> package.

## Static documentation

- **[Bodu.Text.Formats introduction](~/docs/formats/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Text.Formats core concepts](~/docs/formats/concepts.md)** — vocabulary: format vs codec, value vs document, framing tokens, canonical encoding, format exception.
- **[Bodu.Text.Formats getting started](~/docs/formats/getting-started.md)** — install and minimal samples.
- **[Parser policies](~/docs/formats/parser-policies.md)** — the `SpecVersion` selector and the line / column / offset diagnostics carried by <xref:Bodu.Text.Toml.TomlFormatException>.
- **[Using TOML](~/guides/formats/toml.md)** — the reader/writer pair, the value model, spec-version selection, and stream support.

## Key types

- <xref:Bodu.Text.Toml.Toml> — static codec façade. `Parse` / `TryParse` over `ReadOnlySpan<char>`, `Parse` / `ParseAsync` over `Stream` (UTF-8), and `Format` / `FormatAsync` to `string` or `Stream`. Each parse entry point has an overload accepting <xref:Bodu.Text.Toml.TomlReaderOptions>.
- <xref:Bodu.Text.Toml.TomlReader> — the deserialization half of the pair. Single-pass recursive-descent parser; reads from `ReadOnlySpan<char>`, `string`, `TextReader`, or a UTF-8 `Stream` (sync and async). Unsealed so consumers may inherit read capability without gaining mutation surface.
- <xref:Bodu.Text.Toml.TomlWriter> — the serialization half. Emits canonical, block-style TOML to a `string`, `TextWriter`, or UTF-8 `Stream` (sync and async).
- <xref:Bodu.Text.Toml.TomlReaderOptions> — reader configuration. Carries the init-only <xref:Bodu.Text.Toml.TomlReaderOptions.SpecVersion> property; immutable once constructed and safe to share across reads.
- <xref:Bodu.Text.Toml.TomlSpecVersion> — the spec selector: `V1_0` (default) or `V1_1`.
- <xref:Bodu.Text.Toml.TomlValue> — abstract base for every value; exposes `Kind` for switch-style dispatch.
- <xref:Bodu.Text.Toml.TomlValueKind> — `String`, `Integer`, `Float`, `Boolean`, `OffsetDateTime`, `LocalDateTime`, `LocalDate`, `LocalTime`, `Array`, `Table`.
- <xref:Bodu.Text.Toml.TomlString>, <xref:Bodu.Text.Toml.TomlInteger>, <xref:Bodu.Text.Toml.TomlFloat>, <xref:Bodu.Text.Toml.TomlBoolean> — scalar value types over `string`, `long`, `double`, and `bool`. Immutable; each implements `IEquatable<T>`.
- <xref:Bodu.Text.Toml.TomlLocalDate>, <xref:Bodu.Text.Toml.TomlLocalTime>, <xref:Bodu.Text.Toml.TomlLocalDateTime>, <xref:Bodu.Text.Toml.TomlOffsetDateTime> — the four RFC 3339 date-time kinds, backed by `DateOnly`, `TimeOnly`, `DateTime` (`Unspecified`), and `DateTimeOffset` respectively.
- <xref:Bodu.Text.Toml.TomlArray> — an ordered, mutable `IReadOnlyList<TomlValue>`; TOML permits mixed-type elements. `Add` appends.
- <xref:Bodu.Text.Toml.TomlTable> — an ordered, mutable, case-sensitive map of `string` → <xref:Bodu.Text.Toml.TomlValue>; the document root. `this[key]`, `Add`, `ContainsKey`, `TryGetValue`, `Keys`. Insertion order is preserved for deterministic output.
- <xref:Bodu.Text.Toml.TomlFormatException> — derives from <xref:Bodu.Text.TextFormatException> (a <xref:System.FormatException>); thrown when the source cannot be interpreted as a valid TOML document. Carries `LineNumber`, `ColumnNumber`, and `Offset`.

## Example

```csharp
using Bodu.Text.Toml;

// --- Parse a configuration document -----------------------------------------
TomlTable config = Toml.Parse("""
    title = "Bodu sample"

    [owner]
    name = "Tom"
    dob  = 1979-05-27T07:32:00Z

    [database]
    ports = [8000, 8001, 8002]
    enabled = true
    """);

string title = ((TomlString)config["title"]).Value;               // "Bodu sample"

var owner = (TomlTable)config["owner"];
string name = ((TomlString)owner["name"]).Value;                  // "Tom"
DateTimeOffset dob = ((TomlOffsetDateTime)owner["dob"]).Value;

var ports = (TomlArray)((TomlTable)config["database"])["ports"];
long first = ((TomlInteger)ports[0]).Value;                       // 8000

// --- Switch over the value kind for safe dispatch ---------------------------
foreach ((string key, TomlValue value) in config)
{
    switch (value.Kind)
    {
        case TomlValueKind.String:  Console.WriteLine($"{key} = string");  break;
        case TomlValueKind.Table:   Console.WriteLine($"{key} = table");   break;
        case TomlValueKind.Array:   Console.WriteLine($"{key} = array");   break;
    }
}

// --- Build a document and format it -----------------------------------------
var doc = new TomlTable
{
    ["title"]   = new TomlString("Generated"),
    ["retries"] = new TomlInteger(3),
};
string text = Toml.Format(doc);

// --- Opt in to TOML v1.1.0 grammar ------------------------------------------
var options = new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_1 };
TomlTable v11 = Toml.Parse("greeting = \"\\e[0m\"", options);     // \e escape is v1.1.0

// --- Non-throwing parse for untrusted input ---------------------------------
if (Toml.TryParse(userInput, out TomlTable? parsed))
{
    Use(parsed);
}

// --- Stream a UTF-8 document asynchronously ---------------------------------
await using FileStream fs = File.OpenRead("config.toml");
TomlTable fromStream = await Toml.ParseAsync(fs, cancellationToken);
```

## Notes

- **Reader/writer pair is the primary surface.** <xref:Bodu.Text.Toml.TomlReader> and <xref:Bodu.Text.Toml.TomlWriter> own deserialization and serialization, mirroring the relationship between `XmlReader` / `XmlWriter` and a document model. The static <xref:Bodu.Text.Toml.Toml> class is a thin convenience façade over shared, stateless reader and writer singletons — reach for it for one-line `Parse` / `Format`, and reach for the pair when you want to configure or reuse a reader.
- **Spec-version selection.** Parsing defaults to strict **TOML v1.0.0**. Setting <xref:Bodu.Text.Toml.TomlReaderOptions.SpecVersion> to `V1_1` accepts the v1.1.0 additions: `\e` and `\xHH` string escapes, time values without seconds, and multi-line / trailing-comma inline tables. The version affects parsing only — <xref:Bodu.Text.Toml.TomlWriter> always emits output valid under both versions (full `HH:mm:ss` times, standard escapes, single-line inline tables without trailing commas).
- **Semantic model, not source-preserving.** The value tree records *meaning*, not syntax. A standard `[table]`, an inline `{ }` table, dotted keys, and an array of tables all materialize as <xref:Bodu.Text.Toml.TomlTable> / <xref:Bodu.Text.Toml.TomlArray> instances. A `Parse` → `Format` round trip yields an equal model and canonical text, but it does not reproduce the original layout, comment placement, or whitespace. (Comments are not retained — when comment-preserving round-trips matter, prefer <xref:Bodu.Text.Ini>.)
- **Canonical writer output.** The writer emits a deterministic block-style document: scalars and non-array-of-tables arrays inline, sub-tables under `[header]` sections, arrays of tables under `[[header]]` sections, keys in insertion order. Cyclic document graphs and keys / string values containing unpaired surrogates are rejected with `InvalidOperationException`.
- **Ordinal, case-sensitive keys.** <xref:Bodu.Text.Toml.TomlTable> compares keys with ordinal semantics, matching the TOML specification, and preserves insertion order so enumeration and writer output are stable.
- **Strongly-typed dates.** The four RFC 3339 date-time kinds map onto first-class BCL types — `DateOnly`, `TimeOnly`, `DateTime` (with `DateTimeKind.Unspecified`), and `DateTimeOffset` — so a parsed offset date-time keeps its offset and a local date-time carries no spurious time-zone relation.
- **UTF-8 streams.** Stream input must be valid UTF-8; a leading byte-order mark is ignored. Stream output is UTF-8 without a BOM. The codec never closes a caller-supplied stream — its lifetime stays with the `using` block that owns it. Character input must consist of valid Unicode scalar values; unpaired surrogates are rejected.
- **Diagnostics.** <xref:Bodu.Text.Toml.TomlFormatException> derives from <xref:Bodu.Text.TextFormatException>, so a single `catch (TextFormatException)` handles parse failures uniformly across every format in the package. The exception carries a 1-based `LineNumber` and `ColumnNumber` and a 0-based `Offset` pinpointing the failure (each `0` / `null` when the error is not tied to a specific location).
- **See also:** the [introduction](~/docs/formats/index.md), [core concepts](~/docs/formats/concepts.md), [getting-started](~/docs/formats/getting-started.md), and [parser policies](~/docs/formats/parser-policies.md); the [Using TOML](~/guides/formats/toml.md) guide; and the other formats — [Bencode](xref:Bodu.Text.Bencode), [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini).
```
