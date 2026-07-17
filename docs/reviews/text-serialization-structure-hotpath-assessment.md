# Text Serialization (Bencode / TOML / YAML) — Structural and Hot-Path Assessment

**Date:** 2026-07-16
**Scope:** `Bodu.Text.Bencode/src/` (53 files), `Bodu.Text.Toml/src/` (90 files), `Bodu.Text.Yaml/src/` (48 files), and the shared `Bodu.Text.Serialization/` core (43 files, of which `shared/**` is compiled into the Bencode and Toml assemblies).
**Focus:** poorly designed types and implementation problems, with an emphasis on structural improvements and hot-path optimisation — the same remit as `crypto-hashing-structure-hotpath-assessment.md`.

## Relationship to prior reviews

This is a deeper follow-up pass to three earlier documents, not a replacement:

- `docs/forensic-review/02-parsers.md` cleared these packages on the **security** axis: depth hard-capped at 64, the YAML 10M-node expansion budget enforced by a memoized explicit-stack walk, every attacker-controlled length bounds-checked before allocation. That verdict holds and was re-confirmed here (with one composition-order refinement, A2, and one recursion inconsistency the earlier pass did not examine, A1 — both in passes *adjacent to* the budget walk it verified, not in the budget walk itself).
- `docs/reviews/bencode-bep3-compliance-review.md` covered Bencode's **spec** axis; its conformance matrix stands and is not re-litigated.
- `docs/forensic-review/06-architecture-duplication.md` identified the Bencode/Toml serializer duplication. The shared `Bodu.Text.Serialization` core it recommended now exists (attribute/policy surface as a `ProjectReference`, engine/metadata/format-agnostic converters as `shared/**` compiled source bound through per-package `SharedSourceAliases.cs`). This pass audits what that consolidation produced — and what it has not yet reached. The Yaml→shared-core migration remains a roadmap item, **out of scope for this round**; its precondition (a Yaml behavioural test backbone) is part of this round's Phase 3.

This pass asked: *is the code well-structured, and does it run as fast as its careful reader cores deserve?*

## Overall verdict

The **ref-struct reader cores are span-first and allocation-disciplined** — Bencode parses integers and lengths via `Utf8Parser` on spans, TOML's lexer validates UTF-8 with `Rune.DecodeFromUtf8` and stackallocs its numeric scratch, and the security hardening the forensic review verified is real. The problems cluster into three themes:

1. **The serializer glue above the readers throws the discipline away.** Members are read, written, and constructed via reflection invoke with no compiled delegates; every object deserialized allocates a buffering dictionary and boxes every member; the Bencode facade copies or re-parses whole documents at four seams; and the Bencode/TOML facades mint fresh options — with cold metadata and converter caches — on every default-options call (YAML, ironically, already caches a default).
2. **YAML is the structurally divergent sibling, and it shows in the hot paths.** Its scalar resolver allocates up to two throwaway strings per plain scalar; quoted scalars double-copy; the writer re-encodes and re-resolves per scalar; converter resolution is an uncached linear scan; and the binder re-runs the naming policy per key comparison. None of these costs exist in the other two packages' equivalents.
3. **A handful of genuine correctness traps** — none security-grade, but each makes a wrong result or a crash the easy path: a comparer-dropping `DeepClone`, float identity silently lost on YAML write, a cycle-detection pass that is natively recursive two functions away from a pass that deliberately is not, and inconsistent error offsets between the Bencode reader and its serializer.

Severity keys: **High** = fix promptly (safety or order-of-magnitude perf), **Medium**, **Low**.

---

## A. Safety / correctness

| # | Sev | Finding | Evidence |
|---|---|---|---|
| A1 | High | YAML alias-cycle detection (`VisitForCycle`) is **natively recursive** over the resolved node graph, while its sibling pass `EnforceExpansionBudget` deliberately uses an explicit stack with a comment explaining that "a recursive counter would itself risk a StackOverflowException on a long alias chain." The same argument applies here and was not applied: a document of chained anchors (`a2: [*a1]`, `a3: [*a2]`, …) drives recursion depth O(document size). The 64-level physical nesting cap does not protect — aliases compose *resolved* depth beyond physical nesting — so a modest crafted document produces an uncatchable `StackOverflowException` in the pass whose job is to make alias graphs safe. | `YamlParser.Compose.cs:139-162` vs the explicit-stack discipline at `:48-95` |
| A2 | Medium | `Compose()` runs `EnforceExpansionBudget` **before** `ExpandMergeKeys`, and merge expansion appends new alias rows (`NewMergeAlias`) that the budget never counted. Analysis suggests materialized totals stay within a small constant of the counted total (injected aliases point at already-counted targets and replace a counted `<<` child), but the enforcement-order reasoning gap is real and the fix is cheap: run the budget over the post-injection row store. | `YamlParser.Compose.cs:21-28` (order), `:271,313-334` (`NewMergeAlias` appends) |
| A3 | Medium | `Utf8YamlWriter.FormatDouble` uses `"R"` bare, so `WriteDouble(1.0)` emits `1` — which the scalar resolver reclassifies as **Integer** on read. Whole-valued doubles silently lose their float identity across a round trip (`YamlValue.Create(1.0)` → write → parse → `long`). Both 1.1 and 1.2 schemas resolve bare `1` as int, so this is a type-fidelity bug in every profile. | `Utf8YamlWriter.cs:452-464` |
| A4 | Medium | The double-quoted scalar's escaped line-break branch (`\` at end of line) advances and `SkipSpaces()` but skips both guards the ordinary fold branch applies — no `IsBoundaryAt` check and no `RequireQuotedContinuationIndent`. An under-indented or document-boundary-crossing continuation after `\` is accepted where the same text without the backslash is rejected. | `YamlParser.Scalars.cs:271-279` (escaped) vs `:288-298` (fold branch with both guards) |
| A5 | Medium | `TomlObject.DeepClone` constructs `new TomlObject()`, which is hard-wired to `StringComparer.Ordinal` — an object created case-insensitive (via `TomlNodeOptions.PropertyNameCaseInsensitive`) silently becomes case-sensitive on clone, changing lookup and duplicate-key semantics of the copy. | `TomlObject.cs:288-295` (clone), `:54-59` (Ordinal ctor), `:71-76` (case-insensitive ctor) |
| A6 | Medium | Bencode serializer exceptions report `reader.BytesConsumed` — the position *after* the offending token — while the reader's own `BencodeFormatException`s carry the token-start offset (pinned by `Utf8BencodeReaderTests.TokenStartIndex.cs` and the offset assertions in `.Malformed.cs`). The two surfaces disagree about where the same defect is. TOML's converter does not share the bug (it stamps positions through `reader.StampPosition`). | `Bodu.Text.Bencode/.../ObjectConverter{T}.cs:39,66,78,92` vs `Utf8BencodeReader.cs` token-start errors |
| A7 | Low | `YamlParser.ErrorAt` recomputes line/column by counting only `\n`, but the lexer honours a lone `\r` as a line break — error positions on CR-only documents report the wrong line. The live-cursor `Error` path and the rescanning `ErrorAt` path can therefore disagree about the same offset. | `YamlParser.Lex.cs:252-259` vs `\r` handling at `:71-79` |
| A8 | Low | Contract/documentation defects: (a) `ResolveAliases`'s XML doc claims it "resolves each alias to the most recent anchor of the same name defined earlier" while the code rejects redefinition outright (the *behaviour* is a pinned profile exclusion — see "What is right" — the *comment* is wrong); (b) `BencodeSerializer.Deserialize(Stream)` / `DeserializeAsync` buffer the entire stream through a `MemoryStream` without saying so, and the async overload is asynchronous only for the copy; (c) `nint`/`nuint` route through the signed `IntegerConverter<T>` (`long.CreateChecked`), so a 64-bit `nuint` above `long.MaxValue` throws where `ulong` uses the dedicated unsigned surface. | `YamlParser.Compose.cs:170-178`; `BencodeSerializer.cs:263-297`; `IntegerConverterFactory.cs:34-35,55` |

---

## B. Hot paths

| # | Sev | Finding | Evidence |
|---|---|---|---|
| B1 | High | The shared metadata layer reads, writes, and constructs **every member of every object via reflection invoke** — `PropertyInfo.GetValue`/`SetValue`, `FieldInfo.GetValue`/`SetValue`, `ConstructorInfo.Invoke`/`Activator.CreateInstance` — with no compiled delegates, no expression trees, no source generation. This is the dominant per-object cost of `BencodeSerializer` and `TomlSerializer` versus `System.Text.Json`, and it lands twice because the source is shared. | `Bodu.Text.Serialization/shared/Metadata/PropertyMetadata.cs:168-182`, `TypeMetadata.cs:162-165` |
| B2 | Medium | Both packages' `ObjectConverter<T>.Read` allocate a `Dictionary<PropertyMetadata, object?>` per object deserialized, box every member value into it, then run a second full `Properties` scan for required-member checks. An array of N objects pays N dictionaries + N×members boxes before construction even starts. (The two files are per-package near-duplicates — see C1.) | `Bodu.Text.Bencode/.../ObjectConverter{T}.cs:46,242,279`, `Bodu.Text.Toml/.../ObjectConverter{T}.cs:45,280,317` |
| B3 | Medium | Options-level cache friction, both packages: (a) `GetTypeMetadata` passes a `this`-capturing lambda and `GetConverter` an instance method group to `ConcurrentDictionary.GetOrAdd` — a delegate allocation per call *including cache hits*; (b) far worse, the facades default null options with `options ?? new XSerializerOptions()`, so every default-options `Serialize`/`Deserialize` call gets **fresh, empty metadata and converter caches** and re-runs full reflection resolution. YAML already does this right (`static readonly s_defaultOptions`, `YamlSerializer.cs:31`); the two packages with the *heavier* metadata layer are the two that forgot to cache it. | `BencodeSerializerOptions.cs:369,385`, `TomlSerializerOptions.cs:393,409`; `BencodeSerializer.cs:57,212`, `TomlSerializer.cs:97,177` |
| B4 | Medium-High | The Bencode facade copies or re-parses whole documents at four seams: `Serialize<T>` returns `buffer.WrittenSpan.ToArray()` (full extra copy); the `IBufferWriter<byte>` overload is `destination.Write(Serialize(value, options))` — allocating the intermediate array the overload exists to avoid; `SerializeToNode`/`SerializeToDocument` serialize to bytes and **re-parse** them; `Deserialize<T>(BencodeNode)` round-trips through `node.ToByteArray()`. | `BencodeSerializer.cs:62,85,129-146,169` |
| B5 | Medium | The Bencode reader and writer, though ref structs, heap-allocate per document and per container: a `List<Frame>` plus a `sealed class Frame` per container entered on the reader; the writer adds a `RootState`, per-dictionary `ArrayBufferWriter<byte>` + entry list, and `WritePropertyName` copies every key (`name.ToArray()` / `Encoding.UTF8.GetBytes(name)`). Note the per-dictionary *value buffering itself is inherent* — canonical key sorting cannot emit until the dictionary closes — but the frame objects and per-key copies are not. | `Utf8BencodeReader.cs:74,610,804`; `Utf8BencodeWriter.cs:192,234,299,317,793-825` |
| B6 | Medium | TOML string/number decode allocations: `DecodeEscapedString` allocates an intermediate string per verbatim run (`sb.Append(Encoding.UTF8.GetString(...))`) and per unicode escape (`char.ConvertFromUtf32`); `StripNumberUnderscores` allocates a `char[]` per numeric literal (its float sibling `ParseFloat` already stackallocs); the escaped path of `ValueTextEquals` double-allocates (`Encoding.UTF8.GetBytes(GetString())`). | `Utf8TomlReader.Strings.cs:438-484,513`; `Utf8TomlReader.TryGet.cs:364-374`; `Utf8TomlReader.cs:633-641` |
| B7 | Medium | `TomlCanonicalWriter.WriteTableBody` allocates two partition `List`s per table and rebuilds the full ancestor key path by copying it at every nesting level (`List<string> childPath = [.. path, entry.Key]`) — O(depth) copy per section, quadratic for deeply nested tables. | `TomlCanonicalWriter.cs:52-53,73` |
| B8 | High | `YamlScalarResolver` — run for **every plain and flow scalar parsed and every scalar written** — allocates a throwaway string via `AsAscii` in `TryResolveInteger` *and again* in `TryResolveFloat` (the two are tried independently, so a plain string like `hello` pays both), plus `StripUnderscores` (`string.Replace`) and multiple `Substring` calls on the numeric paths. The entire resolution is expressible over the `ReadOnlySpan<byte>` it receives. This is the dominant YAML parse-time allocation. | `YamlScalarResolver.cs:33-62,129,147-165,189,334-352` |
| B9 | Medium | YAML quoted scalars accumulate into a `List<byte>`, then finish with `Encoding.UTF8.GetString(buf.ToArray())` — the `ToArray` copies the whole list and `GetString` copies again; `TrimTrailingInlineSpace` pops via repeated `RemoveAt(Count-1)`. | `YamlParser.Scalars.cs:236,306,487-491` |
| B10 | Medium | `Utf8YamlWriter` re-encodes and re-resolves per token: `IsPlainSafe` allocates `Encoding.UTF8.GetBytes(value)` and runs a full `YamlScalarResolver.Resolve` for every string scalar written (keys included), and `WriteRaw` does a `GetByteCount` pass followed by a `GetBytes` pass for each of the many small fragments (indent, key, `": "`, value, newline) that make up one entry. | `Utf8YamlWriter.cs:372-412,489-495` |
| B11 | Medium-High | The YAML serializer has no converter-resolution cache — `GetConverter` linearly scans the converter list calling `CanConvert` for **every value read and written** (the missing metadata layer biting as perf, not just structure). The binder is O(members × keys): each incoming key linear-searches the member list, and `WireName` re-runs the naming-policy conversion on every comparison and every `PushPath`. | `YamlSerializerOptions.cs:246-255`; `YamlSerializer.Read.Collections.cs:153-160,235-242`; `YamlMemberInfo.cs:99-100` |
| B12 | Low-Medium | YAML serializer success-path waste: `WriteSequence`/`WriteDictionary` build `$"[{index}]"` diagnostic path strings per element even when nothing fails, and `OrderMembers` allocates its enumerator/sort per object written whenever any member declares an explicit order. | `YamlSerializer.cs:207,250,420-429` |
| B13 | Low-Medium | Read-only DOM lookup complexity, all three packages: property lookup is a linear child scan and indexed element access walks from the head (O(n²) to index an n-element array); Bencode additionally rents + UTF-8-encodes the needle key per `TryGetProperty` call. `System.Text.Json`'s `JsonDocument` has the same complexity contract, so this is documented parity — **except TOML**, whose builder already constructs a `(parent,key)` hash index past a 1024-child threshold and then throws it away instead of handing it to the `TomlDocument` it just built. | `BencodeDocument.cs:216-231,458-482`; `TomlDocument.cs:372-386,447-467` vs `TomlDocumentBuilder.cs:36-48`; `YamlDocument.cs:259-276` |

---

## C. Type design / structure

| # | Sev | Finding | Evidence |
|---|---|---|---|
| C1 | Medium | The consolidation boundary stopped one layer short: `ObjectConverter<T>`, `CollectionConverter`, `DictionaryConverter`, and the serializer facades remain per-package near-duplicates of each other (the csproj comment explains the real constraint — ref-struct-typed generic virtuals cannot live in a shared *assembly* — but they **can** live in the shared *source* tree, which the engine and metadata already prove out). Until then, every fix to the object/collection binding logic must be applied twice; this round does exactly that (see P2.2). | `Bodu.Text.Bencode/src/Text.Bencode.Serialization.Converters/` vs `Bodu.Text.Toml/src/Text.Toml.Serialization.Converters/`; `Bodu.Text.Bencode.csproj:23-27` |
| C2 | Medium | YAML remains outside the shared core entirely: a single `YamlMemberInfo` reflection cache instead of the `MetadataResolver`/`TypeMetadata`/`PropertyMetadata` trio, a thinner converter model (no factories), two attributes instead of the full family, no `WriteStack`/`SerializerEngine`. This is the known, planned migration (CLAUDE.md names YAML the primary target); it stays roadmap this round, but B8/B10/B11 are the measurable cost of the divergence, and P2.6's caching fixes are shaped to map 1:1 onto `PropertyMetadata` when the migration lands. | `Bodu.Text.Yaml/src/Text.Yaml.Serialization/` |
| C3 | Low | YAML exposes no `SerializeAsync`/`DeserializeAsync`/stream surface at all, where both siblings do. Product-surface gap, roadmap alongside C2. | `YamlSerializer.cs` public surface |
| C4 | Medium | Test-suite asymmetry that hides regressions: YAML has no allocation tests, no `Utf8YamlReaderTests.Malformed.cs`, no serializer `MaxDepth` or `NamingPolicy` backbone files (BVT counts tell the story: Bencode 707, TOML 1087, YAML 169); the TOML conformance corpus (771 cases, zero skips — excellent) runs **only against `TomlDocumentReader`**, so a divergence in the DOMs, serializer, or writer on a conformance case is invisible; Bencode has no allocation tests despite its facade copy seams. | `TomlTestCorpusTests.cs:127,145`; absent files under `Bodu.Text.Yaml/test/` |
| C5 | Low | Minor API-shape items: `TomlObject.Values` allocates a fresh list via LINQ on every property access; `BencodeObject`'s insertion order is dictionary-backed and non-deterministic after removals (safe only because the writer re-sorts canonically — worth a remark, not a change). | `TomlObject.cs:110-111`; `BencodeObject.cs:42` |

---

## What is right (re-confirmed)

Recorded so the next reviewer does not re-raise them:

- **The reader cores are genuinely span-first.** Bencode: `Utf8Parser`/`Utf8Formatter` end to end, raw-byte canonical key validation (`SequenceCompareTo`), no per-scalar string materialization. TOML: `Rune.DecodeFromUtf8` validation, stackalloc numeric scratch, snapshot/restore resumability across partial blocks. The hot-path losses in §B live *above* these cores, not in them.
- **The security posture from the forensic review holds.** Depth clamped to 64 on every surface; YAML's `EnforceExpansionBudget` remains the exemplary pass — memoized, explicit-stack, O(nodes) — and A1/A2 are defects in its *neighbours*, not in it. TOML's table/dotted-key redefinition rules track the spec discussions they cite (`toml-lang/toml#846`).
- **Canonical output guarantees are real.** The Bencode writer sorts keys by raw bytes and rejects duplicates before emitting; `TorrentFixtureTests` pins a real SHA-1 info-hash and byte-exact round-trips across all surfaces.
- **Cleared hypotheses from this pass:**
  - *YAML `0x`/`0o` over-recognition* — **rejected**: the 1.2 core schema's integer rule is `[-+]?[0-9]+ | 0o[0-7]+ | 0x[0-9a-fA-F]+`, exactly what `YamlScalarResolver.cs:154-165` implements; only `0b` and leading-zero octal are 1.1 forms, and both are correctly gated to `V1_1`.
  - *YAML duplicate-anchor rejection as a spec bug* — **behaviour is a pinned, deliberate profile exclusion** (`YamlDocumentTests.ProfileEnforcement.cs` lists duplicate anchors among the excluded constructs and pins the throw). Only the contradictory XML doc is fixed (A8a).
  - *Bencode ≥2 GiB stream length truncation* — **impossible as stated**: the length being cast is a `MemoryStream.Length`, which is int-bounded; the copy throws long before the cast could overflow. The real (Low) finding is the undocumented whole-stream buffering (A8b).
  - *YAML `ScalarText` float precision loss* — not a defect on net8.0, where `double.ToString()` is shortest-round-trip by default; only the writer's `"R"`-without-`.0` half (A3) is real.
- **The corpus and KAT infrastructure is strong where it exists**: TOML vendors toml-test with provenance and case-count governance and skips nothing; Bencode carries a ~40-row malformed catalogue with offset assertions plus seeded randomized round-trips; YAML runs a corpus harness with semantic comparison.
- **The shared-source mechanism works.** The `#if BENCODE/#elif TOML` + `global using` alias pattern reads as ordinary C# and has kept the engine and metadata layers genuinely single-sourced.

---

## Remediation roadmap

### Phase 1 — Safety & contract fixes (small, independent diffs)

- **P1.1** (A1, A2): explicit-stack `VisitForCycle` mirroring `EnforceExpansionBudget`'s pattern; budget runs over the post-merge-expansion row store. Regression tests: 100k-chain alias document, merge-amplification document.
- **P1.2** (A3, A4, A7, A8a): `FormatDouble` appends `.0` when the round-trip form has no `.`/`e`; escaped continuation gains the boundary + indent guards; `ErrorAt` counts lone `\r` breaks; `ResolveAliases` doc corrected. *Wire change*: whole doubles now emit `1.0` — pinned output tests updated deliberately.
- **P1.3** (A5, C5): comparer-preserving `TomlObject.DeepClone`; `Values` without LINQ.
- **P1.4** (A6, A8b, A8c): Bencode serializer errors report token-start offsets; `nint`/`nuint` route through the width-appropriate surface; stream-buffering remarks added.

### Phase 2 — Hot paths (highest throughput return)

- **P2.1** (B1, B3) ⚠ shared: compiled getter/setter/constructor delegates in `PropertyMetadata`/`TypeMetadata` (expression-compiled, reflection fallback for non-public members; exception surface preserved at the setter seam); `GetOrAdd` state overloads; `static readonly` frozen default options in both facades. Gated by both packages' full suites.
- **P2.2** (B2) ×2: resolver-assigned slot indices; per-read `object?[]` + presence bits replace the dictionary.
- **P2.3** (B4, B5): direct `IBufferWriter` serialization path; pooled buffers for the byte[]/node/document bridges; reader frame class → struct list; pooled key encoding. New `BencodeAllocationTests` pins the wins.
- **P2.4** (B6, B7): pooled/stackalloc TOML decode, underscore strip, single-buffer `ValueTextEquals`; canonical writer push/pop path list. Output locked by round-trip suites + corpus.
- **P2.5** (B8, B9): span-based `YamlScalarResolver` rewrite; pooled quoted-scalar accumulation. New `YamlAllocationTests`; YAML regression tier on this commit.
- **P2.6** (B10, B11, B12): memoized converter cache; per-type wire-name dictionary in the binder; lazy failure-path segments; single-pass `IsPlainSafe`/`WriteRaw`.

### Phase 3 — Structure & test parity

- **P3.1** (C4): TOML corpus expanded to the `TomlDocument`, `TomlNode` round-trip, and writer-stability surfaces (Regression tier).
- **P3.2** (B13): the TOML builder's child index handed to `TomlDocument` (threshold-gated); Bencode/YAML documents remain S.T.J-parity by design.
- **P3.3** (C4): YAML `Utf8YamlReaderTests.Malformed.cs`, `YamlSerializerTests.MaxDepth.cs`, `YamlSerializerTests.NamingPolicy.cs` — also the pinned contract for the C2 migration.

### Documented and deferred

| Item | Disposition |
|---|---|
| YAML → shared `Bodu.Text.Serialization` core (C2) | Roadmap; precondition P3.3 lands this round. |
| YAML async/stream serializer surface (C3) | Roadmap, alongside C2. |
| `ObjectConverter`/facade lift into `shared/**` (C1) | Roadmap; P2.2 applies identical fixes to both copies first so the eventual lift is a pure file move. |
| Bencode/YAML DOM lookup complexity (B13 part) | S.T.J-parity; enumerators are the supported access pattern. |
| Bencode per-dictionary value buffering (B5 part) | Inherent to canonical key sorting; pooled in P2.3 and stopped there. |
| Converter-seam boxing (`ReadAsObject`/`WriteAsObject`) | Inherent to the boxed-converter architecture; revisit under the C2 shared-core roadmap. |

### Verification

Per-commit: `dotnet build bodu.slnx` + BVT for the affected packages; shared-source commits always run **both** the Bencode and TOML suites (the shared tree compiles into each assembly — there is no separate shared test project). Reader/writer/resolver byte-level changes additionally run the owning package's regression tier (the corpora and malformed sweeps are Regression-tagged). Perf evidence: allocation-multiplier bounds in the `TomlAllocationTests` pattern (`GC.GetAllocatedBytesForCurrentThread`, warm-up then measured run), extended to Bencode and YAML, with before/after multipliers recorded in each perf commit message.

---

## Remediation outcome (2026-07-16, same branch)

Every phased item above landed in this round, one commit per group, each gated by the affected packages' regression tiers. Findings and their dispositions:

| Finding | Outcome |
|---|---|
| A1, A2, A8a | **Fixed, A1 severity revised down on verification** — explicit-stack cycle walk; post-merge budget re-check plus a row-growth ceiling during injection; `ResolveAliases` doc corrected. Running the new 100k-anchor-chain test against the *pre-fix* code showed the recursive walk never actually went deep: aliases only ever point backward, so document-order visiting memoizes every anchor's subtree before any alias to it is walked, bounding recursion by physical nesting (≤64) rather than chain length. The crash scenario in A1's original wording does not reproduce; the explicit-stack rewrite stands as defense-in-depth consistency with `EnforceExpansionBudget` (and removes the reliance on that ordering argument), not as a crash fix. |
| A3, A4, A7 | **Fixed** — `WriteDouble(1.0)` emits `1.0` (wire change, tests updated deliberately); escaped continuations gain the fold branch's boundary + indent guards; `ErrorAt` counts lone CR breaks. |
| A5, C5 | **Fixed** — comparer-preserving `DeepClone` in **both** `TomlObject` and `BencodeObject` (the same bug, found in review of the sibling); `TomlObject.Values` without LINQ. |
| A6, A8b, A8c | **Fixed** — all Bencode serializer errors report token-start offsets (contract correction on `BytesOffset`); `nuint` gets a dedicated unsigned converter; stream-buffering remarks added. |
| B1, B3 | **Fixed, with an honest measurement note** — compiled getter/setter/constructor delegates in the shared metadata (box-mutating `Expression.Unbox` for struct targets; reflection's null-coercion preserved), `GetOrAdd` state overloads, and cached frozen default options on both facades. Measurement showed net8.0's post-warmup reflection stubs already amortize near compiled speed at realistic workloads — the steady-state wall-clock win is small and the row-store parse dominates; **B1's original "dominant cost" severity is revised down accordingly**, and the compiled path is kept as the canonical shape that removes residual invoke overhead as member counts grow. |
| B2 | **Fixed** — slot-indexed `object?[]` + presence flags replace the per-object dictionary in both `ObjectConverter`s; TOML POCO bind allocation −14% (381,768 → 329,648 bytes / 24 KB input), baseline tightened 22× → 18×. |
| B4, B5 | **Fixed** — direct `IBufferWriter` serialization, pooled buffers at every facade seam, range-based dictionary keys in a pooled frame buffer, reader frames as in-place-mutated structs. Serialize 102,608 → 46,376 bytes; POCO-row serialize 544,808 → 225,896 bytes (500-entry baseline, new `BencodeAllocationTests`). |
| B6, B7 | **Fixed** — pooled/stack TOML string decode, underscore strip, and `ValueTextEquals` compare; canonical writer two-pass partition + push/pop path. Byte-locked by the corpus. |
| B8, B9 | **Fixed** — span-based scalar resolver (shared single transcode); single-copy quoted decode. YAML serialize allocation −53% (241,672 → 113,672 bytes), because the writer's `IsPlainSafe` runs the resolver per scalar. New `YamlAllocationTests` records the parse baselines the C2 migration will be measured against. |
| B10, B11, B12 | **Fixed** — `YamlTypeBinding` (wire names computed/validated once, options-comparer lookup, precomputed write order) + memoized converter cache on the frozen options; unformatted sequence-index path segments; single-pass `IsPlainSafe`/`WriteRaw`. Serialize baseline further 113,672 → 97,704 bytes. `YamlTypeBinding` is deliberately shaped to map onto the shared-core `TypeMetadata` (C2). |
| B13 | **Fixed (TOML half)** — the builder's large-table key index is inherited by `TomlDocument`; Bencode/YAML documents stay S.T.J-parity by design (deferred half unchanged). |
| C4 | **Fixed** — the corpus now runs against `TomlDocument`, the `TomlNode` round-trip, and writer fixed-point (~1050 new rows, all green first run); YAML gains `Utf8YamlReaderTests.Malformed`, `YamlSerializerTests.MaxDepth`, and `YamlSerializerTests.NamingPolicy`. |
| C1, C2, C3, deferred B13/B5 halves, converter-seam boxing | **Deferred as planned** — roadmap items unchanged; C2's precondition (the YAML behavioural backbone) landed this round. |

Post-remediation suite sizes (regression tier): Bencode 940, TOML 4,168, YAML 1,429 — up from 935 / 3,311 / 1,371 at the start of the round, with zero failures throughout.

## Roadmap follow-up (2026-07-16, same branch, round 2)

The three deferred structural items landed as a follow-up sequence on the same branch, one commit each:

| Item | Outcome |
|---|---|
| C1 | **Done** — `MetadataResolver`, `SerializationThrowHelper`, `IntegerConverterFactory`, and `ObjectConverterFactory` lifted into `Bodu.Text.Serialization/shared/**` behind the alias seam with `#if`-gated format divergences (Bencode's offset stamping, unsigned-width routing, unsupported-scalar guard, and 4-arg duplicate-wire-name diagnostics). The three container converters stay per-package — measurement showed they are not twins (TOML's path/cycle apparatus, divergent token vocabularies), honestly revising the earlier "pure file move" expectation. Net −486 lines. |
| C2 | **Done** — YAML compiles the shared source under a `YAML` symbol: the metadata trio (compiled accessors, slot buffers, full attribute family) and the structural factories (nullable/dictionary/collection/object), with the non-null `GetConverter` pipeline, `GetTypeMetadata`, `InstantiateConverter`, a freeze-aware `IList<YamlConverter>`, and a new public `YamlConverterFactory`. YAML's scalar converters stay format-local (`Text.Yaml.Serialization.Converters`) because YAML's implicit typing coerces across scalar kinds, which the token-strict shared scalar converters cannot express; null handling and the depth ceiling moved to the single `WriteAsObject` dispatch seam. `YamlTypeBinding`/`YamlMemberInfo` and the `Bind*`/`WriteValue` walkers are retired. One deliberate public rename: `Utf8YamlWriter.WriteInt64` → `WriteInteger` (sibling parity); `Utf8YamlReader` gains `Skip()`. The full 1,429-test suite passed unmodified on first run — the behavioural contract carried over exactly. |
| C3 | **Done** — YAML ships the sibling stream/async facade (`Serialize<T>(IBufferWriter<byte>)`, `Deserialize<T>(Stream)`, `SerializeAsync`/`DeserializeAsync`, buffered in full with async stream I/O only), with the mirrored async test files. YAML regression: 1,442. |

Still deferred by design: the YAML DOM↔serializer `NodeConverter` bridge (`YamlNode` has no `ReadFrom(ref Utf8YamlReader)`; adding one is new surface, not consolidation), the Bencode/YAML DOM lookup S.T.J-parity, Bencode's per-dictionary value buffering, and the converter-seam boxing inherent to the boxed-converter architecture.

### Residual consolidation (round 3, same branch)

A measured, name-normalized diff sweep after C1–C3 found the remaining cross-package near-duplicates; the mechanical tier plus the options pipeline and the TOML/YAML exception pair were lifted (four commits):

| Item | Outcome |
|---|---|
| `{Fmt}ConverterFactory` (×3, 2–4 diff lines/62) + public `{Fmt}NumberEnumConverter<TEnum>` (B/T, doc-only) | **Lifted** — first shared files using the `#if`-switched public-type-name idiom: each branch declares its format's named type and constructor (doc comments must stay contiguous per branch), bodies bind through the `Format*` aliases. |
| `ConverterList` (T/Y twins, 2 diff lines/179; **absent in Bencode**) | **Lifted + Bencode contract fix** — Bencode exposed a raw `List`, so post-freeze mutation was silently ignored and null entries accepted; it now shares the guarded list (both changes verified red-first, +4 guard tests). |
| Options resolution pipeline (caches, `IsReadOnly`/`MakeReadOnly`, `GetConverter`→attribute→user→defaults, `GetTypeMetadata`, `InstantiateConverter`, `Materialize`, `VerifyMutable`) | **Lifted** into a shared `#if`-declared partial of the three options classes (~380 duplicated lines removed); per-format residue is the gated trimming attributes and TOML's `RootMapsToTable`, which stays in its own file. |
| `TomlSerializationException`/`YamlSerializationException` (8 diff lines/103) | **Lifted** — shared `Path`/position/`CombinePath` tail; Bencode's offset-based sibling stays per-package via the Bencode csproj's first shared-source `Exclude`. |

Measured and deliberately left per-package: the T/Y scalar converters (the C2 implicit-typing coercion boundary, 17–71 diff lines per ~40-line file), container-converter full bodies (`ObjectConverter` B/T 150/383, T/Y 328/425), `ByteArrayConverter` (different wire models), facades, `DefaultConverters` orderings, `ObjectTypeConverter`, the three `WriteStack`s (three deliberate maturity levels), and the per-package `FormatToken`/`SharedSourceAliases` binding seams themselves.
