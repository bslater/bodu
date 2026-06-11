# `Utf8TomlReader` Redesign — Implementation Plan

**Date:** 2026-06-11
**Status:** Executed — Phases A and B are delivered on this branch (A1 `60766028`, A2/A3
`9df3f109`, A4 `f9650e0e`, B1 `64074c62`, B2 `d3168a64`, B3/B4 `0d51f4c7`). Deviations from the
plan as written: B3 and B4 landed as one commit (the public swap and the test port are not
separately compilable); the `TomlDocumentReader` internal tree ctor was dropped (every entry
point parses from bytes, so it had no consumer); the lexer enforces `MaxDepth` for bracket
nesting itself, keeping `TomlReaderOptions.MaxDepth` meaningful on the public reader; and the
internal source-order vocabulary/state enums shipped as `TomlReaderState`/`TomlScalarTextKind`
with the public `TomlTokenType` extended directly.
**Relates to:** [`utf8-toml-reader-redesign-assessment.md`](./utf8-toml-reader-redesign-assessment.md)
(target design §4, phasing §6) and finding I1 of
[`toml-spec-compliance-review.md`](./toml-spec-compliance-review.md).

This plan turns the assessment's target design into ordered, file-level work items. The
architecture, naming, breaking-change inventory, and risk positions are settled in the assessment
and are not re-argued here.

---

## 0. Ground rules for the whole effort

- **Safety net first.** The 2,584-test suite — including the 1,410-case toml-test conformance
  corpus in both spec profiles — must be green after every work item. No work item may be merged
  with a skipped or weakened test; behavioral changes land only where this plan explicitly calls
  them out (byte-true offsets, exception-site split).
- **One commit per work item** on the session branch, in the order below. Each work item leaves
  the solution building and the suite green so the effort can pause safely at any boundary.
- **Phase A ships no public-surface change.** The public `Utf8TomlReader` keeps its current
  normalized token contract as a façade until Phase B. Public API diffs are verified at each
  Phase A exit (e.g. by comparing the public type/member surface before and after).
- **`InternalsVisibleTo` is already in place** (`Bodu.Text.Toml.csproj:22`), so Phase A's
  internal lexer and builder get direct unit tests without waiting for Phase B.

### Settled design decisions carried into this plan

| Decision | Resolution |
|---|---|
| Lexer visibility in Phase A | `internal` ref struct `TomlLexer`; its implementation becomes the public `Utf8TomlReader` in Phase B (B5 renames/re-points; no second copy is ever maintained). |
| `TomlDocumentReader` construction surface | Public ctors mirroring today's `Utf8TomlReader` (`ReadOnlySpan<byte>`, `+ TomlReaderOptions`) so the existing reader tests and any external normalized-token consumers migrate by type rename alone; plus an internal ctor over the builder's node tree used by the serializer engine and DOMs to avoid re-parsing. |
| Where converters get their reader | `TomlSerializerEngine` constructs the `TomlDocumentReader` (internal ctor) and threads it by `ref`, exactly as it threads `Utf8TomlReader` today (`TomlSerializerEngine.cs:55-66`). |
| Boxed scalar values in `TomlReaderToken`/`TomlReaderNode` | Retained. Allocation tuning of the node tree is explicitly deferred (assessment §6). |
| Benchmarks | Consciously skipped (assessment §7); no perf work item appears below. |

---

## Phase A — byte-native core behind the existing surface

Phase A replaces the char-based `TomlDocumentParser` (1,620 lines, `string _src`,
`Substring`/`StringBuilder` throughout) with a byte-native lexer + structural builder, while the
public `Utf8TomlReader` continues to expose today's normalized tree-order tokens. Exit criteria
for the phase as a whole: full suite + corpus green; the doc-sized UTF-16 decode
(`Utf8TomlReader.Decode`), the up-front surrogate pass (`EnsureValidScalarValues`), and
`TomlDocumentParser` itself are gone; offsets flowing into `TomlReaderNode.Offset` and
`TomlFormatException` are byte-true.

### A1 — Pin the façade: exact token-stream characterization tests

**New file:** `test/Utf8TomlReaderTests.TokenStream.cs`

Before touching internals, add tests that assert the **exact token sequence**
(`TokenType`, decoded value, `CurrentDepth` at each step) produced by the current reader for a
set of representative documents: out-of-line header merge (`[server]` … `[client]` …
`[server.tls]`), dotted keys, arrays of tables, inline tables, every scalar kind, empty document,
and a deep-nesting case. These rows become the verbatim contract for the Phase A façade and the
Phase B `TomlDocumentReader`, complementing the corpus (which pins *values*, not token shapes).

*Exit:* new tests green against the current implementation; committed before any production change.

### A2 — `TomlLexer`: the internal byte-native source-order lexer

**New files:** `src/Text.Toml.Reader/TomlLexer.cs` (+ partials per `.filenesting.json` as needed:
`TomlLexer.Strings.cs`, `TomlLexer.Numbers.cs`, `TomlLexer.DateTimes.cs`),
`src/Text.Toml.Reader/TomlLexTokenType.cs`

An `internal ref struct` over `ReadOnlySpan<byte>` with `Read()`, `TokenType`
(`TomlLexTokenType`), `ValueSpan` (raw UTF-8 slice), `HasEscapes`, `TokenStartIndex`,
`LineNumber`, `ColumnNumber`, and lazy `GetString()`/`GetInt64()`/`GetDouble()`/`GetBoolean()`/
date-time accessors that parse from bytes at the call site.

- **Token vocabulary** (internal enum; Phase B maps it onto the public `TomlTokenType`
  additions): `TableHeader`, `ArrayTableHeader`, `Key`, `KeySeparator` is *not* surfaced (dots
  are consumed; one `Key` token per segment), `Equals` is *not* surfaced (consumed),
  `Comment`, `StartArray`/`EndArray`, `StartInlineTable`/`EndInlineTable`, `String`, `Integer`,
  `Float`, `Boolean`, the four date-time kinds, `Newline` is *not* surfaced (line discipline is
  enforced, not tokenized).
- **Context tracking** is the lexer's own job (it is lexical, not structural): a small mode
  machine — line start expects a header or key; after `=` expects a value; inside `[ … ]` expects
  values; inside `{ … }` alternates key/value — with a depth counter for bracket nesting. This is
  what lets `1234` lex as a key in key position and an integer in value position.
- **Validation scope — lexical only:** UTF-8 validity (validated incrementally during scanning;
  no up-front decode), string termination and escape correctness, number/date-time grammar
  (including underscore rules, leading-zero rules, the RFC 3339 component ranges currently in
  `MakeDate`/`MakeTime`/`MakeDateTime`), newline discipline (bare `\r`, control characters),
  and `TomlSpecVersion` gating of the lexical relaxations (`\e`, `\x`, optional seconds,
  trailing comma / newlines in inline tables). Structural rules (duplicate keys, table
  redefinition, dotted-key semantics, AoT, inline-table closedness, `MaxDepth`) are **not** the
  lexer's concern and move to A3.
- **Porting source:** the scanning logic ports from `TomlDocumentParser` methods
  `SkipInlineWhitespace`/`SkipComment`/`ConsumeNewline`/`ParseSimpleKey`/`ParseBasicString`/
  `ParseLiteralString`/`ParseMultiline*`/`ParseNumber`/`ParseDateTime`/`ReadPartialTime`/etc.,
  re-expressed over bytes. ASCII-heavy paths index the span directly; non-ASCII appears only
  inside strings, comments, and (v1.1) bare keys, where `Rune.DecodeFromUtf8` handles
  multi-byte sequences. Escape decoding writes into a pooled/`stackalloc`-backed builder only
  when `HasEscapes` is true.

**New test files:** `test/TomlLexerTests.cs` + member partials
(`TomlLexerTests.Strings.cs`, `TomlLexerTests.Numbers.cs`, `TomlLexerTests.DateTimes.cs`,
`TomlLexerTests.Keys.cs`, `TomlLexerTests.Positions.cs`, `TomlLexerTests.Malformed.cs`,
`TomlLexerTests.SpecVersion.cs`). Coverage required by the assessment's test strategy:
source-order vocabulary per construct, `ValueSpan` slice correctness, lazy-decode equivalence
(decoded value equals what the current parser produces), byte-true `TokenStartIndex`/line/column
on multi-byte input, and rejection of every *lexical* malformation with position. Full-grammar
sweeps tagged `[TestCategory("Regression")]`.

*Exit:* lexer green standalone; production pipeline untouched (parser still in place).

### A3 — `TomlDocumentBuilder`: the one authoritative structural parser

**New file:** `src/Text.Toml.Reader/TomlDocumentBuilder.cs`

An `internal sealed class` whose `Parse(ReadOnlySpan<byte> utf8, TomlSpecVersion, int maxDepth)`
drives a `TomlLexer` and owns everything structural from `TomlDocumentParser`: the five identity
sets (`_headerDefined`, `_dotted`, `_inline`, `_implicitSuper`, `_tableArrays`), header
walking (`DefineStandardTable`/`DefineArrayTable`/`WalkHeaderSegment`), dotted-key assignment
(`AssignKeyValue`/`WalkDottedSegment`), inline-table/array materialization, and `MaxDepth`
bounding (`EnterDepth`/`LeaveDepth`). Output: the existing internal `TomlReaderNode` tree, with
`Offset` now carrying the lexer's **byte** offsets.

Structural errors throw `TomlFormatException` carrying the lexer-reported position of the
offending token. The builder consumes the lexer in method scope only (a `ref struct` local), so
the class shape is unconstrained.

**New test file:** `test/TomlDocumentBuilderTests.cs` — targeted structural tests asserting the
lexical/structural split explicitly: every rule enforced by an identity set has at least one test
proving the *builder* (not the lexer) rejects it, with position. The corpus's 983 invalid cases
provide the exhaustive sweep once A4 lands.

*Exit:* builder green standalone against direct unit tests; pipeline still untouched.

### A4 — Cut over the pipeline; delete the char-based parser

**Modified:** `src/Text.Toml.Reader/Utf8TomlReader.cs` — the ctor becomes
`TomlReaderNode root = new TomlDocumentBuilder().Parse(utf8Toml, options.SpecVersion, maxDepth)`
followed by the existing `Flatten`; `Decode` and `s_utf8` are deleted (UTF-8 validation now
happens in the lexer). XML docs updated: the constructor still parses fully, but no longer
"decodes" — keep the documented normalized contract otherwise verbatim.
**Deleted:** `src/Text.Toml.Reader/TomlDocumentParser.cs` (including `EnsureValidScalarValues`).
**Modified:** `src/Text.Toml/TomlFormatException.cs` — `Offset` XML docs change from the
documented character-offset compromise to byte offsets (completes review finding m6).

**Behavioral deltas to absorb (the only intended ones):**

1. Error offsets/columns on non-ASCII input shift from char-counted to byte-counted. Audit
   `test/Utf8TomlReaderTests.Malformed.cs` and `test/TomlExceptionTests.cs` for exact
   position assertions on non-ASCII rows and update the expected values (assessment risk
   register: most rows assert positions are *populated*, not exact).
2. Invalid UTF-8 now surfaces with a real position from the lexer rather than from the
   up-front `Decode` wrapper; the exception type and resource string
   (`Format_Invalid_TomlInvalidUtf8`) are unchanged.

*Exit (Phase A complete):* `dotnet test bodu.slnx --settings regression.runsettings` fully green,
including the corpus in both spec profiles and the A1 token-stream pins; no public-surface diff;
`TomlDocumentParser` gone. Update the spec-compliance review's I1/m6 entries to "Phase A
delivered" and commit.

---

## Phase B — the public swap

Phase B ships the deliberate break from the assessment's §5 inventory. It starts only after
Phase A is merged and stable.

### B1 — Introduce `TomlDocumentReader` (the normalized cursor, renamed)

**New file:** `src/Text.Toml.Reader/TomlDocumentReader.cs`

Move the current `Utf8TomlReader` implementation (token list, `Read`/`Skip`/`TokenType`/
`CurrentDepth`/`Get*`, `Flatten`) into a new public `ref struct TomlDocumentReader`:

- Public ctors `(ReadOnlySpan<byte>)` and `(ReadOnlySpan<byte>, TomlReaderOptions)` — parse via
  the builder, then flatten (today's behavior under the new, honest name).
- Internal ctor `(TomlTableNode root)` — flatten an already-built tree; used by the engine and
  DOMs so one parse feeds binding without reconstructing.
- XML docs are the current `Utf8TomlReader` docs with the framing corrected: this type *walks a
  parsed document*; it does not read UTF-8 source.

During B1, `Utf8TomlReader` temporarily remains as-is (both types compile side by side) so the
migration commits stay reviewable.

### B2 — Migrate the converter surface

**Modified:** `src/Text.Toml.Serialization/TomlConverterOfT.cs` — the public abstract `Read`
becomes:

```csharp
public abstract T Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options);
```

The positioning contract text (enter on the value's first token, leave on its last) is unchanged.
**Modified (mechanical, parameter type only):** all 27 files under
`src/Text.Toml.Serialization/Converters/`, `TomlStringEnumConverter(OfT)`,
`TomlNumberEnumConverter`, `TomlSerializerEngine` (constructs `TomlDocumentReader` via the
internal ctor), `TomlSerializer.Deserialize` overloads, `TomlNode.Parse`/`ReadFrom`
(`TomlNode.cs:227-299`), and `TomlDocument.Parse`/`ReadValue` (`TomlDocument.cs:104-175`).

*Exit:* solution builds with `Utf8TomlReader` no longer referenced by any production code; full
suite green (tests still reference it — migrated in B4).

### B3 — Re-point `Utf8TomlReader` at the lexer

**Modified:** `src/Text.Toml/TomlTokenType.cs` — add `TableHeader`, `ArrayTableHeader`, `Key`,
`Comment`, `StartInlineTable`, `EndInlineTable`; existing 13 members keep their values.
**Rewritten:** `src/Text.Toml.Reader/Utf8TomlReader.cs` — becomes the public face of the Phase A
lexer (rename `TomlLexer` → `Utf8TomlReader`, internal enum mapped onto the extended public
`TomlTokenType`, members per the assessment §4.1: `ValueSpan`, `HasEscapes`, `TokenStartIndex`,
`LineNumber`, `ColumnNumber`, lazy `Get*`). The ctor no longer throws on structural errors —
only lexical ones, surfaced from `Read()` as scanning proceeds; XML docs specify the
lexical-vs-structural split and the source-order vocabulary up front, with the header/key token
example from the assessment.
**Modified:** `TomlDocumentBuilder` consumes the renamed type.

### B4 — Port the test suite

- `test/Utf8TomlReaderTests.*.cs` (13 files) → `test/TomlDocumentReaderTests.*.cs`: rename class
  and constructor calls; token-contract content (including the A1 `TokenStream` pins) carries
  over verbatim — that is the proof the normalized contract survived the rename.
- `test/TomlLexerTests.*.cs` → `test/Utf8TomlReaderTests.*.cs`: the lexer tests become the new
  public reader's contract tests, re-targeted at the public type and extended with public-surface
  cases (token vocabulary as `TomlTokenType`, `Skip()` over source-order containers, `Comment`
  surfacing, structural-error *non*-rejection — e.g. a duplicate key lexes cleanly).
- `test/TomlDocumentBuilderTests.cs` is unchanged (still pins the structural split).
- Corpus and serializer suites are untouched by design; they must stay green throughout.

### B5 — Documentation and release notes

- Update `toml-spec-compliance-review.md` I1 and m6 to resolved, pointing at the assessment and
  this plan.
- Add the §5 breaking-change inventory from the assessment to the release notes /
  `PackageReleaseNotes` for the next **major** version of `Bodu.Text.Toml`: reader semantics,
  `TomlConverter<T>.Read` signature, `TomlTokenType` additions, byte-true offsets, and the
  one-line migration ("normalized-token consumers: rename `Utf8TomlReader` →
  `TomlDocumentReader`; custom converters: change the `Read` parameter type").
- Confirm `Bodu.Extensions.Configuration.Text` required no change (it consumes
  `TomlDocument.Parse` only) and note it in the release notes as insulated.

*Exit (Phase B complete):* full regression run green; public surface matches the assessment §4;
no internal type named `TomlLexer` remains.

---

## Sequencing summary

| # | Work item | Public break? | Gate |
|---|---|---|---|
| A1 | Token-stream characterization tests | No | Suite green |
| A2 | `TomlLexer` + lexer test suite | No | Lexer tests green |
| A3 | `TomlDocumentBuilder` + structural tests | No | Builder tests green |
| A4 | Pipeline cutover; delete `TomlDocumentParser`; byte-true offsets | No (behavioral: offsets) | **Full regression + corpus green; no public API diff** |
| B1 | `TomlDocumentReader` introduced | Additive | Suite green |
| B2 | Converter/engine/DOM migration | **Breaking (signature)** | Suite green; `Utf8TomlReader` unreferenced in src |
| B3 | `Utf8TomlReader` → source-order lexer; `TomlTokenType` additions | **Breaking (behavioral)** | Builds; new reader tests green |
| B4 | Test-suite port | No | **Full regression + corpus green** |
| B5 | Docs + release notes | No | Review docs updated |

The A4 and B4 gates are the two points where the whole effort is re-validated end to end; nothing
merges past them red. If Phase B stalls, the system left by A4 is self-consistent and
independently valuable (assessment risk register, last row).
