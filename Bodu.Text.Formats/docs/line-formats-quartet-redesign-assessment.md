# Line-oriented text formats — quartet redesign assessment

**Date:** 2026-07-21
**Status:** Approved — implementation sequenced as tranches T0–T6.
**Relates to:** [`ROADMAP.md`](../../ROADMAP.md) *Active focus #4 — "Consolidate
the two text-format tiers"* and *Architectural patterns #1 — the System.Text.Json-shaped
quartet*.

This assessment settles the architecture, naming, packaging, breaking-change
inventory, and risk positions for redesigning the three line-oriented formats
in `Bodu.Text.Formats` — **Delimited** (RFC 4180 CSV/TSV), **DotEnv**, and
**INI** — onto the modern quartet architecture used by `Bodu.Text.Bencode`,
`Bodu.Text.Toml`, and `Bodu.Text.Yaml`. The per-tranche work items live in the
plan; this document is the decision record they do not re-argue.

---

## 1. Problem statement

The repository ships two parallel structured-text architectures:

- **The quartet** (Bencode/Toml/Yaml) — a ref-struct `Utf8*Reader` /
  `Utf8*Writer` token surface over UTF-8 `byte`s, a `*Serializer` POCO mapper
  built on the shared `Bodu.Text.Serialization` source, a mutable `*Node` DOM,
  and a read-only `*Document` DOM. This is *Architectural pattern #1* and the
  documented template for every structured-text format.

- **The line-oriented trio** (`Bodu.Text.Formats`) — `char`/`string` I/O over
  `TextReader`/`TextWriter`, a static `Parse`/`Format`/`TryParse` facade, a
  streaming `*Reader`/`*Writer` pair, immutable DOMs (INI is the exception —
  mutable and authorable), and `GetValue<T>` typed access over
  `ISpanParsable<T>`. No byte surface, no token ref-struct, no serializer.

Active focus #4 asks us to either retrofit the trio onto the quartet or
document the tiering as a deliberate design choice. **Decision: retrofit.**
Nothing in the repository is released (no git tags), so the breaking change is
free today, and unifying on one structured-text template removes a standing
inconsistency rather than freezing it into the public surface.

## 2. Decisions

### D1 — Retrofit onto the quartet (not a documented split)

The three formats are rebuilt from the ground up on the quartet template. The
`char`/`TextReader` facade, the `*ParseOptions` structs, and the bespoke
immutable DOMs are retired.

### D2 — One assembly per format (forced), three standalone packages + umbrella

A format joins `Bodu.Text.Serialization` by (a) defining a compile symbol
(e.g. `TOML`), (b) `Compile Include`-ing `Bodu.Text.Serialization/shared/**`,
(c) supplying a `SharedSourceAliases.cs` that binds **ten** `global using
Format* = …` aliases 1:1 to that format's types
(`FormatReader`/`FormatWriter`/`FormatConverter`/`FormatConverterFactory`/
`FormatNode`/`FormatObject`/`FormatOptions`/`FormatWriteStack`/
`FormatResourceStrings`/`FormatSerializationException`), and (d) adding a
namespace `#elif <SYMBOL>` arm to every shared file that declares a namespace
(~25 files).

Because the aliases are 1:1, **three formats cannot co-exist in one assembly
that compiles the shared source once.** The redesign therefore splits
`Bodu.Text.Formats` into three standalone libraries — `Bodu.Text.Delimited`,
`Bodu.Text.DotEnv`, `Bodu.Text.Ini` — each a peer of Bencode/Toml/Yaml. This is
consistent with the roadmap's own language that "Bencode and TOML are fully
extracted to their own libraries."

**Packaging:** ship the three as standalone NuGet packages **plus a thin
`Bodu.Text.Formats` umbrella meta-package** that references all three. The
umbrella preserves the Wave-2 release-manifest entry and any existing package
reference to `Bodu.Text.Formats`, minimizing manifest and sample churn.

### D3 — `FormatReader` binds directly for DotEnv/Delimited; INI needs a pre-parse

For Bencode, `FormatReader = Utf8BencodeReader` directly, because Bencode source
order *is* tree order. TOML needs a separate `TomlDocumentReader` because
`[table]` / `[[array-of-tables]]` / dotted-key structure is declared out of
source order.

The three line formats are **flat and in source order**, so:

- **DotEnv** and **Delimited** bind `FormatReader = Utf8<Fmt>Reader` directly
  (the Bencode pattern — no pre-parse type).
- **INI** is the exception: duplicate-section **merge** (`[foo]` … later
  `[foo]` appends) declares structure out of source order, exactly TOML's
  condition. INI therefore keeps a public forward-only `Utf8IniReader` **and** a
  pre-parse `IniDocumentReader` (materialized flat-row store, TOML-identical
  shape) that also backs the read-only `IniDocument` DOM.

### D4 — Format-local scalar converters (string-only wire)

All three formats carry every scalar on the wire as a **string** (DotEnv/INI
values; every CSV field). The shared token-strict scalar converters
(`IntegerConverter<T>` calls `GetInt64()`, etc.) assume typed scalar tokens the
wire does not have. So each format **excludes the shared scalar/enum converters
and the token-strict engine seam from its `Compile Include`** (the mechanism
Yaml already uses) and ships a `Text.<Fmt>.Serialization.Converters/` folder of
**string-coercing** scalar converters over `ISpanParsable<T>` / `IParsable<T>`.

This is today's `GetValue<T>` / `TryGetValue<T> : ISpanParsable<T>` behaviour
relocated behind the converter seam. The rationale differs from Yaml's (Yaml is
local because of *implicit typing*; these are local because of a *string-only
wire*) but the architectural outcome is the same. Everything structural is
reused unchanged: the attribute family, `NamingPolicy`, the four callbacks,
`MetadataResolver`/`TypeMetadata`/`PropertyMetadata`, `SerializationException`,
and the structural factories (object/dictionary/collection/enum/nullable).

The `NodeConverter` DOM↔serializer bridge is **not** adopted initially (same
posture as Yaml).

### D5 — Trivia asymmetry (deliberate, documented)

The read-only `*Document` DOMs are **trivia-free** (comments dropped) like the
quartet. The **mutable `.Nodes` DOMs for DotEnv and INI bear comment trivia**
(leading, and for INI inline) so authoring and parse→format round-trips stay
faithful. INI's trivia-bearing mutable DOM is the successor to today's
`IniDocument` / `IniSection` / `IniEntry` / `IniComment`. This is the one place
the quartet's "DOMs are trivia-free" convention is deliberately broken, and it
is ratified here rather than discovered later.

### D6 — Decouple `Bodu.Text.Configuration`

`ConfigurationDocument : IniDocumentBase` today — Configuration's document model
*inherits* the INI mutable, comment-trivia DOM, and `ConfigurationReader` builds
`IniSection`/`IniEntry`/`IniComment` directly. Rather than force the new
`Bodu.Text.Ini` DOM to remain Configuration's base class, **decouple**: move the
trivia-preserving INI document model into `Bodu.Text.Configuration` as its own
internal model and drop Config's dependency on the INI library. The new
`Bodu.Text.Ini` stays a clean quartet; the awkward "Configuration *is* INI's
DOM" coupling is removed.

## 3. Target per-format value & token model

Common quartet skeleton each project receives — folders under `src/` mirroring
`Bodu.Text.Toml` exactly:

```
Text.<Fmt>/                         <Fmt>Serializer, <Fmt>SerializerOptions/Defaults,
                                    <Fmt>TokenType, <Fmt>ValueKind, <Fmt>FormatException,
                                    <Fmt>SerializationException, <Fmt>Limits, <Fmt>Trimming,
                                    <Fmt>ResourceStrings, carried-over dialect enums
Text.<Fmt>.Reader/                  Utf8<Fmt>Reader (+ <Fmt>ReaderState); INI: + <Fmt>DocumentReader
Text.<Fmt>.Writer/                  Utf8<Fmt>Writer (+ <Fmt>WriterOptions)
Text.<Fmt>.Serialization/           <Fmt>Converter, <Fmt>Converter{T}, <Fmt>ConverterFactory,
                                    <Fmt>StringEnumConverter, <Fmt>WriteStack
Text.<Fmt>.Serialization.Converters/  format-local scalar converters + DefaultConverters
Text.<Fmt>.Nodes/                   mutable DOM (<Fmt>Node/Object/[Array]/Value)
Text.<Fmt>.Document/                read-only DOM (<Fmt>Document/Element/Property, IDisposable)
```

| Format | Value model | `<Fmt>TokenType` | FormatReader | Serializer binding target |
|---|---|---|---|---|
| **DotEnv** | flat object of string-valued keys (no arrays, no nesting) | `None, StartObject, EndObject, PropertyName, String, Comment` | `Utf8DotEnvReader` directly | `Dictionary<string,string>` / flat POCO of `ISpanParsable` props; root-is-object gate |
| **Delimited** | array of records; record = object keyed by header, or positional string array (headerless) | `None, StartArray, EndArray, StartObject, EndObject, PropertyName, String` | `Utf8DelimitedReader` directly (1-row header lookahead) | `List<TRecord>`/`TRecord[]`/`IEnumerable`/`IAsyncEnumerable<TRecord>`/`string[]`; root-is-collection gate |
| **INI** | 2-level object-of-objects (sections → string keys); no deeper nesting | source: `None, SectionHeader, PropertyName, String, Comment`; normalized: `None, StartObject, EndObject, PropertyName, String` | `Utf8IniReader` (source) + pre-parse `IniDocumentReader` (normalized) | section-POCO object / `Dictionary<string,Dictionary<string,string>>`; depth-2 gate |

**Presentation trivia not modelled as tokens:** DotEnv's `export ` prefix is a
reader flag (`CurrentIsExport`) preserved by the mutable DOM, dropped by the
read-only Document. INI section/entry comments live on the mutable DOM only.

**Streaming preserved.** DotEnv/INI are line-incremental and Delimited is
row-incremental via resumable `Utf8<Fmt>Reader` + `<Fmt>ReaderState` over
`ReadOnlySequence<byte>`. The shared `<Fmt>Serializer` facade stays
buffered-in-full (quartet parity — `SerializeAsync`/`DeserializeAsync` buffer
then issue one async stream copy). **Delimited additionally gets a first-class
incremental surface** — `DelimitedSerializer.DeserializeAsyncEnumerable<TRecord>(Stream)`
and a streaming `SerializeAsync(Stream, IAsyncEnumerable<TRecord>)` — driven off
the resumable reader/writer, which also delivers the roadmap's "Layer an
`IAsyncEnumerable<T>` projection" item for `Bodu.Text.Formats`.

## 4. Open design questions resolved here

- **INI global/unnamed section mapping.** Global keys **hoist to the root
  object** for POCO ergonomics; a reserved-key option
  (`IniSerializerOptions.GlobalSectionName`) preserves round-trip fidelity when
  needed. A collision between a global key and a section name is **rejected**
  (documented `IniSerializationException`); the reserved-key mode disambiguates.
- **Read-only vs mutable trivia (D5).** Ratified: read-only Documents are
  trivia-free; only the mutable DotEnv/INI `.Nodes` DOMs bear comments.
- **Delimited record shape.** Header present → record binds to an object keyed
  by header name (respecting `NamingPolicy`/`[PropertyName]`); headerless →
  positional `string[]` / `DelimitedArray`.

## 5. Breaking-change / blast-radius inventory

| Consumer | Coupling | Migration |
|---|---|---|
| `Bodu.Text.Configuration` | `ConfigurationDocument : IniDocumentBase`; builds `IniSection`/`IniEntry`/`IniComment` | **Decouple (D6)** — internalize the trivia model; drop the Formats dependency |
| `Bodu.Extensions.Configuration.Text` | consumes `ConfigurationDocument` + `IniDocumentBase`; references the trio directly | repoint to the three new assemblies |
| `Bodu.Financial.ExchangeRates.Imf` / `.Boe` | `using Bodu.Text.Delimited;` → `Delimited.Parse` + `DelimitedDocument`/`DelimitedRow`/`DelimitedParseOptions` | migrate to the new `Bodu.Text.Delimited` DOM/serializer (T2) |
| `samples/Text.Formats/*`, `samples/Text.Configuration/*` | trio API throughout | rewrite to the quartet (T6) |
| `DocumentationSnippetCompileTests` | trio API in guide snippets | rewrite with the guide pages (T6) — the compile guard fails the build until done |

## 6. Documentation corrections uncovered

CLAUDE.md's Test Consolidation section claims `Bodu.Text.Formats.Test`
(`Bodu.Text.Formats.Contracts`) hosts `BinaryDocumentFormatContractTests`,
`TextDocumentFormatContractTests`, and `StreamRoundTripContractTests`. **Only
`TextDocumentFormatContractTests<TDocument,TOptions>` actually exists**; the
other two are stale. The redesign follows the self-contained Bencode/Toml test
model (no promoted shared contract base), and T6 corrects the CLAUDE.md claim.

## 7. Sequencing & risk (summary; see the plan for work items)

| Tranche | Scope | Effort | Risk |
|---|---|---|---|
| T0 | This assessment + 3 per-format design notes | M | L |
| T1 | DotEnv quartet (template-establishing; flattest) | L | L–M |
| T2 | Delimited quartet + `IAsyncEnumerable` streaming + RFC 4180 corpus | L–XL | M |
| T3 | INI quartet (two readers, merge, trivia DOM) | XL | H |
| T4 | Configuration + bridge decouple (atomic with T3) | L–XL | H |
| T5 | Retire trio + umbrella package | M | M |
| T6 | Docs / ROADMAP / CLAUDE.md / samples | M | L |

**Top risks:** (1) the forced one-assembly-per-format split — committed here;
(2) INI duplicate-section merge forcing a pre-parse reader + the trivia-bearing
DOM deviation; (3) the `Bodu.Text.Configuration` inheritance coupling, migrated
atomically with the INI redesign (T3+T4 land together or `bodu.slnx` does not
build).

**Sequencing note (deviation from the plan's T0 line item):** the inert shared
`#elif <SYMBOL>` namespace arms are added **when each format's project is
created** (T1/T2/T3), co-located with the concrete partial types they bind to,
rather than as isolated inert arms in T0. Adding an arm before its types exist
provides nothing verifiable and risks drift from the final type names; folding
each arm into its project keeps the shared source and the consuming project
consistent and independently buildable.
