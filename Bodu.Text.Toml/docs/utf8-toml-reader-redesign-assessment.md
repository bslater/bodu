# `Utf8TomlReader` Redesign — Assessment and Target Design

**Date:** 2026-06-11
**Status:** Assessment and design only — **no implementation ships with this document.**
**Relates to:** finding I1 of [`toml-spec-compliance-review.md`](./toml-spec-compliance-review.md)
**Decisions taken:** assessment covers both phases (UTF-8 span-native core *and* lexer/builder
separation); benchmarks consciously skipped (recorded as a risk in §7); the public token contract
will be **deliberately broken** in the eventual implementation — `Utf8TomlReader` becomes a true
source-order reader and the converter architecture is redesigned around it.

---

## 1. The proposal under review

An architectural critique of the current `Utf8TomlReader` was submitted for assessment. Its core
claims:

1. The type is named and shaped like `Utf8JsonReader` (a `ref struct` exposing `Read()`, `Skip()`,
   `CurrentDepth`, token types) but its constructor decodes the whole input, parses it, and
   materializes two further representations before the first `Read()` — the abstraction is
   misleading even though the behavior is correct.
2. The pipeline carries four representations:
   `UTF-8 bytes → UTF-16 string → TomlReaderNode tree → List<TomlReaderToken> → consumer`.
3. Three options: **(1)** keep the implementation but rename it and drop the `ref struct`;
   **(2)** redesign `Utf8TomlReader` to be genuinely sequential and source-oriented, with the
   document parser consuming it; **(3)** introduce a new low-level reader alongside the current
   one.
4. Recommendation: Option 2, scoped to a single-span, final-block UTF-8 reader; no
   `ReadOnlySequence<byte>` or resumable input initially; DOM parser consumes the new reader;
   `Toml.Parse`/object-model APIs preserved; benchmark before micro-optimising. Closing principle:
   *"The reader should parse the input; the document parser should build the document."*

## 2. Assessment

### 2.1 Where the proposal is right

- **The mismatch is real.** It is finding I1 of the spec-compliance review, verified at source:
  the constructor decodes the entire input to a `string`
  (`Utf8TomlReader.cs` — `Decode`, strict `UTF8Encoding`), runs `TomlDocumentParser.Parse()` to a
  fully validated node tree, then flattens it depth-first into `List<TomlReaderToken>`; `Read()`
  is a cursor over that list. A `ref struct` whose entire mutable state lives in heap objects gets
  the *restrictions* of the `Utf8JsonReader` idiom (no boxing, no async, no capture) with none of
  the zero-allocation *benefits*.
- **The four-representation pipeline is accurately described**, and the duplicated representations
  — not raw speed — are correctly identified as the main cost. Per document parsed today the
  library allocates: a doc-sized UTF-16 string, a `TomlReaderNode` tree with boxed scalars, a
  token list re-boxing nothing but re-referencing everything, plus `StringBuilder`s and
  `Substring` copies along the way.
- **The hard parts are correctly identified**: dotted-key semantics, reopened tables,
  implicit-vs-explicit tracking, arrays of tables, duplicate definitions, inline-table
  closedness, offsets. These are exactly the rules implemented by the five identity sets in
  `TomlDocumentParser` (`_headerDefined`, `_dotted`, `_inline`, `_implicitSuper`, `_tableArrays`)
  and they are the part of the system most worth protecting — they are now pinned by 1,410
  toml-test conformance cases in both spec profiles.
- **The scoping discipline is right**: single-span/final-block first, defer
  `ReadOnlySequence`/resumability, preserve `Toml.Parse`-style entry points.
- **The closing principle is the correct north star.**

### 2.2 What the proposal underweights

**(a) TOML cannot be POCO-deserialized in one source-order pass — and the converters are the
primary public consumers of the reader.** Out-of-line headers make a table's members
non-contiguous in source order:

```toml
[server]
host = "a"

[client]
timeout = 5

[server.tls]      # server's members resume after client's
enabled = true
```

The serializer's binding loops require the *normalized tree-order* contract — a table's entire
contents contiguous between `StartTable`/`EndTable`, with out-of-line headers merged into nested
position, and every `PropertyName` immediately followed by its complete value:

- `TomlConverter<T>.Read(ref Utf8TomlReader reader, …)` is **public abstract**
  (`TomlConverterOfT.cs:55`) with an explicit positioning contract: *"On entry the reader is
  positioned on the value's first token. On return it must be positioned on the value's last
  token"* (`TomlConverterOfT.cs:51-54`).
- `ObjectConverter<T>.Read` loops `while (reader.Read() && reader.TokenType !=
  TomlTokenType.EndTable)` reading `PropertyName` → `Read()` → value
  (`ObjectConverter.cs:45-71`).
- `DictionaryConverter<…>.Read` does the same (`DictionaryConverter.cs:89-94`), as does
  `CollectionConverter<…>.Read` over arrays (`CollectionConverter.cs:73-74`), and
  `TomlNode.ReadFrom` recursively (`TomlNode.cs:264-299`).

Feeding these loops from a source-order reader is impossible without buffering the document —
which is what the current implementation does. The proposal's step 3 ("make the DOM parser
consume the new reader") is correct but incomplete: it is silent on the serializer, which is the
larger consumer surface (~20 built-in converters plus any external custom converters).

**(b) The architecture therefore needs three boxes, not two.** The proposal's target diagram —

```
UTF-8 bytes → Utf8TomlReader ─┬─ direct consumer
                              └─ TomlDocumentBuilder → object model
```

— omits the binding layer. The honest target is:

```
UTF-8 bytes
    ↓
Utf8TomlReader            (source-order lexer; lexical validation only)
    ↓
TomlDocumentBuilder       (structural semantics; the five identity sets; one authoritative parser)
    ├─ TomlNode / TomlDocument            (object models)
    └─ TomlDocumentReader                 (normalized tree-order cursor → converters)
```

The normalized view does not disappear under any option; the question is only what it is called
and what `Utf8TomlReader` means. The "Option 2 vs Option 3" framing is therefore a false
dichotomy: choosing Option 2 *implies* a second, normalized reader type for the binding layer.

**(c) "Selective reading without constructing a document" is structurally limited in TOML.**
A JSON reader can stop after extracting one value. In TOML, a table is not known complete until
end of input (a later `[a.b]` may legally extend the namespace), and duplicate/redefinition
validation is inherently whole-document. A source-order reader can *lexically* skip values it
does not care about — a real win — but it cannot deliver "validated subset reads" the way
`Utf8JsonReader` can. The benefit is smaller than the JSON analogy suggests and should not be the
selling point.

**(d) The performance motivation is unquantified.** The repository has no benchmark
infrastructure (no BenchmarkDotNet, no `[Benchmark]`, no perf-tagged tests), and the full
1,410-document conformance corpus parses in ~0.3 s inside the MSTest harness. For the stated
primary workload (5–50 KB configuration files) the proposal itself concedes the user-facing
difference may be small. The decision has been made to proceed on architectural grounds and skip
benchmarks; §7 records this as an accepted risk.

### 2.3 Verdict

**The redesign is worthwhile — as a foundational correction, exactly as the proposal's closing
line frames it.** Option 2 is the right direction, with two corrections to its blueprint:

1. The target architecture must include the document builder *and* a normalized binding cursor
   (three boxes, §2.2(b)); the converter signature change this implies is a deliberate, accepted
   public break.
2. The biggest single allocation win (eliminating the doc-sized UTF-16 string by scanning
   `ReadOnlySpan<byte>` directly) is deliverable in a first phase entirely behind the existing
   public surface, de-risking the public swap in the second phase.

Option 1 (rename only) is rejected: after the redesign the `Utf8TomlReader` name and `ref struct`
shape become *earned* (the reader holds the input `ReadOnlySpan<byte>`), so renaming would solve
the honesty problem in the wrong direction. Option 3 (two permanent public token readers over the
same bytes) is rejected as API noise; the normalized cursor survives, but as the binding-layer
type (`TomlDocumentReader`), not as a peer "reader of TOML text".

---

## 3. Coupling analysis (what the redesign touches)

### 3.1 Consumers of `Utf8TomlReader` and their contract dependency

| Consumer | Site | Consumes | Normalized-contract dependency |
|---|---|---|---|
| `TomlSerializer.Deserialize` (string/UTF-8/Stream/async) | `TomlSerializer.cs:155-205` | constructs reader, hands `ref` to engine | Yes (transitively) |
| `TomlSerializerEngine.Deserialize` | `TomlSerializerEngine.cs:55-66` | first `Read()`, dispatch to converter | Implicit |
| **`TomlConverter<T>.Read` (public abstract)** | `TomlConverterOfT.cs:55` | positioning contract (first token → last token) | **Critical — public API** |
| `ObjectConverter<T>.Read` | `ObjectConverter.cs:45-71` | `while Read() && != EndTable`, `PropertyName`→value adjacency, `Skip()` | Critical |
| `DictionaryConverter<…>.Read` | `DictionaryConverter.cs:89-94` | same loop shape | Critical |
| `CollectionConverter<…>.Read` | `CollectionConverter.cs:73-74` | `while Read() && != EndArray` | Moderate (arrays are contiguous in source order too) |
| `TomlNode.ReadFrom` / `TomlNode.Parse` | `TomlNode.cs:264-299`, `:208-232` | recursive descent over tree-order tokens | Critical |
| `TomlDocument.Parse` (read-only DOM) | `TomlDocument.cs:96-112` | flattens token stream into `Row[]` index | Critical |
| `Bodu.Extensions.Configuration.Text.TomlConfigurationParser` | `TomlConfigurationParser.cs:49-52` | `TomlDocument.Parse` only | Indirect — insulated if `TomlDocument` keeps working |

Every binding/DOM consumer depends critically on tree order; none can consume source order
directly. The conformance corpus and the 2,584-test suite pin the *observable values* these
consumers produce, which is what makes the rewrite safe to attempt.

### 3.2 Public-surface constraints

Public today: `Utf8TomlReader` (ref struct, ctors over `ReadOnlySpan<byte>` +
`TomlReaderOptions`, `Read`/`Skip`/`TokenType`/`CurrentDepth`/`Get*`), `TomlReaderOptions`,
`TomlTokenType` (13 members), `TomlConverter<T>` (abstract `Read`/`Write`),
`TomlSerializer`, the DOMs. Internal (free to change): `TomlDocumentParser`,
`TomlReaderToken`, the `TomlReaderNode` tree, `TomlCanonicalWriter`.

### 3.3 UTF-16 dependencies to eliminate in the core

`TomlDocumentParser` is char-based throughout: the `string _src` field and `char Current`
indexing; `Substring` for keys (`:444`, `:962`) and numbers (`:575`); `StringBuilder` escape
paths (`AppendEscape`/`AppendUnicodeEscape`, `char.ConvertFromUtf32`); the `'﻿'` BOM check
(`:137-141`); the up-front surrogate validation pass (`EnsureValidScalarValues`, `:179-206`,
redundant once scanning is byte-native); and char-based `_pos`/`_lineStart` offsets that
propagate into `TomlReaderNode.Offset` and `TomlFormatException` (the reason `Offset` is
currently documented as a *character* offset — review finding m6).

---

## 4. Target design

### 4.1 `Utf8TomlReader` — redesigned as a true source-order lexer (public, breaking)

A forward-only, single-span, final-block lexer over `ReadOnlySpan<byte>`:

- **Token vocabulary** (`TomlTokenType` additions; existing 13 members retained for the
  normalized cursor's use): `TableHeader` and `ArrayTableHeader` (one token per header, fired
  before its key segments), `Key` (one token per dotted segment, both in headers and in
  key/value pairs), `Comment` (surfaced, skippable), and `StartInlineTable`/`EndInlineTable`
  distinct from the normalized `StartTable`/`EndTable`. Scalars keep their existing members.
  Example:

  ```toml
  [server.tls]          # → TableHeader, Key("server"), Key("tls")
  enabled = true        # → Key("enabled"), Boolean
  ports = [1, 2]        # → Key("ports"), StartArray, Integer, Integer, EndArray
  ```

- **Lazy decoding**: `ValueSpan` (raw UTF-8 slice), `HasEscapes`; `GetString()` decodes on
  demand (escape-free literals can be transcoded straight from the span); numbers and date-times
  parsed from bytes at the `Get*` call.
- **Positions**: `TokenStartIndex` (byte offset), `LineNumber`, `ColumnNumber` — all byte-true.
- **Validation split**: the lexer enforces *lexical* well-formedness only (UTF-8 validity, string
  termination and escapes, number/date-time grammar, newline discipline, control characters,
  spec-version gating of `\e`/`\x`/optional-seconds/inline-table relaxations). All *structural*
  semantics move to the builder: dotted-key rules, table reopening, implicit-vs-explicit,
  arrays of tables, duplicate definitions, inline-table closedness, `MaxDepth` for
  dotted/header nesting.
- **Out of scope** (unchanged from the proposal): `ReadOnlySequence<byte>`, resumable/partial
  input (`isFinalBlock` semantics), async. The struct layout should not preclude adding a
  sequence-backed constructor later.

### 4.2 `TomlDocumentBuilder` — the one authoritative parser (internal)

Consumes the lexer and owns everything in today's `TomlDocumentParser` *except* lexing: the five
identity sets, redefinition rules, AoT append semantics, depth bounding, and tree
materialization. Output remains the internal `TomlReaderNode` tree (now carrying byte offsets).
All three entry surfaces converge on it: `TomlSerializer.Deserialize`, `TomlNode.Parse`,
`TomlDocument.Parse`. The 1,410-case conformance corpus revalidates the builder wholesale after
the move.

### 4.3 `TomlDocumentReader` — the normalized binding cursor (public, new name)

Today's `Utf8TomlReader` implementation — the tree-order token cursor with
`Read`/`Skip`/`TokenType`/`CurrentDepth`/`Get*` — survives under the name `TomlDocumentReader`,
constructed by the engine over the builder's output. The converter API migrates to it:

```csharp
public abstract T Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options);
```

The positioning contract (enter on first token, leave on last token) is unchanged, so converter
*bodies* port mechanically; only the parameter type changes. `Write` is untouched. This is the
deliberate public break, and it is honest in both directions: `Utf8TomlReader` reads UTF-8
source; `TomlDocumentReader` walks a parsed document.

### 4.4 Error model

`TomlFormatException.LineNumber`/`ColumnNumber`/`Offset` become byte-true (lexical errors from
the lexer; structural errors from the builder, which receives token positions). This completes
review finding m6 properly instead of the current documented char-offset compromise.

---

## 5. Breaking-change inventory and migration

| Change | Kind | Blast radius | Migration |
|---|---|---|---|
| `Utf8TomlReader` token semantics: tree-order → source-order; ctor no longer throws on *structural* errors (only lexical); new members (`ValueSpan`, `TokenStartIndex`, …) | **Breaking (behavioral + additive)** | Any external code reading tokens directly | Consumers wanting the old behavior switch to `TomlDocumentReader` |
| `TomlConverter<T>.Read` parameter type → `ref TomlDocumentReader` | **Breaking (signature)** | All external custom converters; ~20 internal converters | Mechanical: same contract, new type name |
| `TomlTokenType` gains `TableHeader`, `ArrayTableHeader`, `Key`, `Comment`, `StartInlineTable`, `EndInlineTable` | Additive | Exhaustive `switch`es over the enum | Existing members keep their values |
| `TomlDocumentReader` introduced | Additive | — | — |
| `TomlFormatException.Offset` char → byte offsets | Behavioral (documented) | Anyone comparing offsets on non-ASCII input | Release note |
| `TomlSerializer.*`, `TomlNode.Parse`, `TomlDocument.Parse`, `Utf8TomlWriter`, all attributes/options | **Unchanged** | `Bodu.Extensions.Configuration.Text` is fully insulated via `TomlDocument` | None |

**Test strategy.** The 2,584-test suite — including the 1,410-case toml-test corpus run in both
spec profiles — pins observable parse/bind/write behavior and is the safety net for the entire
rewrite. New coverage required: lexer-level token-contract tests (source-order vocabulary,
`ValueSpan` slices, lazy-decode equivalence, byte positions, lexical-vs-structural error split)
and migration tests proving `TomlDocumentReader` emits today's exact token sequences (the
existing `Utf8TomlReaderTests.*` largely become `TomlDocumentReaderTests.*` verbatim).

---

## 6. Phasing

- **Phase A — internal rewrite behind the existing surface (no public break).**
  Implement the byte-native lexer and `TomlDocumentBuilder`; rewrite `TomlDocumentParser` as the
  builder consuming the lexer; the existing public `Utf8TomlReader` keeps its current normalized
  contract as a temporary façade over the new pipeline. Exit criteria: full suite + corpus green;
  the doc-sized UTF-16 string, the separate decode pass, and `EnsureValidScalarValues` are gone.
- **Phase B — the public swap.**
  Introduce `TomlDocumentReader` (the façade, renamed); migrate `TomlConverter<T>.Read` and all
  built-in converters; re-point `Utf8TomlReader` at the source-order lexer; port the reader test
  suite; ship release notes for the inventory in §5.
- **Explicitly deferred:** `ReadOnlySequence<byte>`, resumable input, async streaming readers,
  allocation tuning of the node tree (boxed scalars), and any public lexer-level conveniences
  beyond the core vocabulary.

## 7. Risk register

| Risk | Severity | Position |
|---|---|---|
| **No benchmarks** (consciously skipped): allocation/speed claims unquantified; Phase A could even regress small-file latency without detection | Medium | Accepted by decision. Mitigation if revisited: lightweight Stress-tier measurements before/after Phase A |
| Public API break (`TomlConverter<T>.Read`, reader semantics) lands on external custom converters | High (by design) | Accepted as a deliberate foundational correction; mechanical migration; semver-major release |
| Behavioral drift in error positions (char → byte) breaks tests/consumers asserting exact columns on non-ASCII input | Low | The Malformed catalogue asserts positions are *populated*, not exact values, for most rows; audit the few exact assertions |
| Structural-rule regressions while relocating the five identity sets into the builder | Medium | Fully covered by the conformance corpus (983 invalid cases) + the structural test files |
| Lexical/structural validation split changes *which* exception site fires (ctor vs `Read()` vs builder) | Medium | New contract must be specified in XML docs up front; tests assert the split explicitly |
| Two-phase delivery stalls after Phase A, leaving a façade permanently | Low | Phase A is independently valuable (allocation + honesty of internals); the façade is exactly today's documented behavior |

## 8. Summary

The critique is sound where it describes the problem and right in its recommendation, with one
materially incomplete area: it treats the DOM parser as the only downstream consumer, when the
serializer's converter surface is the larger and more tightly coupled one, and TOML's
out-of-line headers make a normalized binding view non-optional. The adopted direction is
therefore Option 2 *plus* a named binding cursor: `Utf8TomlReader` becomes the genuine
source-order UTF-8 lexer, `TomlDocumentBuilder` becomes the single authoritative parser, and
today's normalized cursor survives as `TomlDocumentReader` feeding a converter API that changes
signature but not contract. Implementation is phased so the public break ships only after the
byte-native core has proven itself behind the existing surface against the full conformance
corpus.
