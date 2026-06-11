# Bodu.Text.Bencode — BEP 3 Compliance and System.Text.Json Alignment Review

**Date:** 2026-06-11
**Scope:** `Bodu.Text.Bencode/src` and `Bodu.Text.Bencode/test`
**References:** [BEP 3 — The BitTorrent Protocol Specification](https://www.bittorrent.org/beps/bep_0003.html) (bencoding section), [BEP 52](https://www.bittorrent.org/beps/bep_0052.html) (canonical-form clarifications), and the `System.Text.Json` (S.T.J) API surface as the design reference named in the project charter.

## 1. Method

The review was conducted forensically and empirically, test-first:

1. Every source file in `src/` was read against a BEP 3 grammar checklist (integer grammar, byte-string grammar, dictionary key rules, container balance, single-root documents) and against its S.T.J counterpart API.
2. Every claim of intended behavior was captured as a committed, data-driven MSTest before any production change. Where a test failed, the production code was fixed in the same session (red → green visible in commit history); where it passed, the test stands as pinned evidence.
3. The full regression tier (`regression.runsettings`) was run before the review (**759 passed, 0 failed** baseline) and after (**778 passed, 0 failed**, including 19 new tests).

Findings are graded: **Fixed** (resolved this session, test-first), **Should fix** (defect or risk, deferred), **Alignment** (S.T.J parity gap, no spec impact), **Documentation**, **Test coverage**.

## 2. BEP 3 conformance matrix

Verdicts reflect the post-review state. "Reader" is `src/Text.Bencode.Reader/Utf8BencodeReader.cs`, "Writer" is `src/Text.Bencode.Writer/Utf8BencodeWriter.cs`.

| BEP 3 rule | Implementation | Test evidence | Verdict |
|---|---|---|---|
| Integers: `i<digits>e`, no leading zeros | Reader `ReadInteger` (≈381–432) rejects `i03e`, `i007e` | `Utf8BencodeReaderTests.Malformed.cs` rows *leading zero(s)* | ✅ Compliant |
| Integers: `i-0e` invalid | `ReadInteger` rejects negative zero and `i-03e` | rows *negative zero*, *negative leading zero* | ✅ Compliant |
| Integers: digits required (`ie`, `i-e`, `i+1e`, `i1a2e` invalid) | `ReadInteger` digit-count and terminator checks | rows *empty integer*, *lone minus*, *plus sign*, *non-digit in integer* | ✅ Compliant |
| Integers: terminator `e` required | `ReadInteger` | row *unterminated integer* | ✅ Compliant |
| Integers: no size limit in the spec | Supported range is [`long.MinValue`, `ulong.MaxValue`]; values above `long.MaxValue` readable via `GetUInt64` (≈275); out-of-range input rejected with `BencodeFormatException` | rows *integer overflow/underflow*, *beyond UInt64 by one*, *below Int64 by one*; `Utf8BencodeReaderTests.UnsignedIntegers.cs` | ✅ Compliant within documented range (see R‑7) |
| Strings: `<length>:<bytes>`, no leading zeros in length | Reader `ReadByteString` (≈437–460) | rows *leading-zero length*; *empty byte string* (`0:`) accepted | ✅ Compliant |
| Strings: length must not exceed input; separator required | `ReadByteString` bounds and `:` checks; lengths above `Int32.MaxValue` rejected | rows *length exceeds input*, *missing/bad separator*, *string length exceeds Int32 by one*, *far beyond Int32* (added this review) | ✅ Compliant |
| Strings carry arbitrary bytes (not text) | `ValueSpan`/`GetBytes` are lossless; `GetString` is a UTF‑8 convenience | `Read_WhenBinaryByteString_ShouldPreserveBytes`; `GetString_WhenContentNotValidUtf8_ShouldDecodeWithReplacementCharacters` (added) | ✅ Compliant (see R‑8) |
| Dictionary keys are byte strings | Reader `Read` rejects `i`/`l`/`d` in key position (196–214) | rows *non-string dictionary key (integer/list/dict)* | ✅ Compliant |
| Dictionary keys sorted by raw bytes, no duplicates | Reader: `SequenceCompareTo ≤ 0` check (≈220–224). Writer: canonical sort on close (171) **and, fixed this session, duplicate rejection (179)** | reader rows *unordered/duplicate keys*, *unordered by length*; writer `Utf8BencodeWriterTests.Lifecycle.cs` duplicate-key tests (string, non-adjacent, binary) | ✅ Compliant (was ❌ on write — see R‑1) |
| Containers balanced; values complete | Reader EOF/`e` validation (165–189) | rows *unterminated list/dictionary*, *lone end*, *dictionary value missing* | ✅ Compliant |
| Single root value, no trailing bytes | Reader (156–162); `BencodeDocument`/`BencodeNode` parse drives the reader to completion | rows *trailing data after …*; `BencodeDocumentTests.Parse.cs`, `BencodeNodeTests.Parse.cs` | ✅ Compliant on read; ⚠ writer permits multiple roots by design (see R‑2) |
| (Robustness) nesting depth bounded | Reader/Writer/Document default 256, configurable | depth-limit tests on all three surfaces | ✅ (see R‑9 on default divergence) |

## 3. Recommendations

### Fixed this session (test-first; see commits on this branch)

**R‑1. The writer emitted duplicate dictionary keys (invalid Bencode).** `WriteEndDictionary` sorted entries but never compared neighbours, so the same key written twice — directly, or via two POCO members colliding on one wire name (`MetadataResolver` builds `byWireName` last-wins but writes the full ordered member list) — produced documents the library's own reader rejects. *Fix:* after the canonical sort, adjacent equal keys throw `BencodeSerializationException` (`Op_Invalid_WriterDuplicateDictionaryKey`). Tests: `Utf8BencodeWriterTests.Lifecycle.cs` (3 duplicate-key tests), `BencodeSerializerTests.PropertyName.cs` `Serialize_WhenMembersCollideOnWireName_…`.

**R‑2 (partial). Writer call-sequence validation produced wrong or missing errors.** Misuse previously surfaced as `InvalidCastException` (`WriteEndList` on a dictionary, `WritePropertyName` inside a list), `ArgumentOutOfRangeException` (`WritePropertyName` with no container), silent key overwrite (double `WritePropertyName`), silent key drop (dangling name at close), or silently mis-emitted bytes (value in a dictionary without a name — the null key emitted as `0:`). *Fix:* all of these now throw `InvalidOperationException` with resx-sourced messages, mirroring `Utf8JsonWriter`'s state validation. Eleven new lifecycle tests pin the contract. **Remaining (not fixed):** the writer still accepts multiple root values — pinned as intentional by `WriteInteger_WhenWrittenTwiceAtTopLevel_ShouldConcatenateValues` ("the writer's trust in the caller"). Recommend revisiting: either remove that trust (BEP 3 documents are single-valued, and `Utf8JsonWriter` validates this) or expose it as an explicit opt-in (`SkipValidation`-style writer option).

**R‑3. `Utf8BencodeReader.Skip()` on a `PropertyName` token was a no-op.** `Utf8JsonReader.Skip` advances past the property's value; the Bencode reader silently did nothing, an alignment trap for converter authors. *Fix:* `Skip` now reads to the value and skips it (scalar or subtree). Tests: `Utf8BencodeReaderTests.Skip.cs` (2 new tests).

### Should fix

**R‑4. Writer architecture: per-value heap buffering.** Every `WriteInteger`/`WriteByteString` allocates an `ArrayBufferWriter<byte>` plus a `byte[]` copy, and each container close concatenates and re-copies its children (`Utf8BencodeWriter.cs` ≈ lines 124–195). Nothing reaches the output `IBufferWriter` until the root completes, and deep documents copy bytes O(depth) times. `Utf8JsonWriter` streams to the destination buffer. Dictionary sorting genuinely requires buffering *dictionary entries*, but lists and scalars at list/root level can stream, and dictionary buffering can use index/offset tables over one pooled buffer instead of one `byte[]` per value. Recommend a streaming rewrite before the library is positioned as high-performance; the existing canonical-bytes round-trip tests make the rewrite safe.

**R‑5. No metadata-time wire-name collision detection.** R‑1 makes collisions fail at write time, but S.T.J fails fast with a clear `InvalidOperationException` naming the type and property when metadata is built. Recommend a duplicate check over `byWireName` in `MetadataResolver.CreateTypeMetadata` so the error names the colliding members rather than the key.

**R‑6. No reader leniency for real-world torrents.** The reader is strictly canonical — correct per BEP 3, but unsorted (and occasionally duplicate) keys exist in the wild, produced by older encoders. A consumer cannot parse such files at all today. Recommend opt-in `BencodeReaderOptions` flags (e.g. `AllowUnsortedKeys`, `DuplicateKeyHandling: Error|First|Last`), defaulting to strict — the moral equivalent of `JsonReaderOptions.AllowTrailingCommas`/`CommentHandling`. Distinguishing the duplicate-key error from the unordered-key error (currently both report `Format_Invalid_BencodeUnorderedDictionaryKeys`) falls out of this work.

### Documentation

**R‑7. Document the supported integer range as a contract.** BEP 3 integers are unbounded; the implementation supports [`long.MinValue`, `ulong.MaxValue`] and rejects the rest (asymmetric: positives get the unsigned extension via `GetUInt64`, negatives stop at `long.MinValue`). This is a reasonable engineering bound (S.T.J's number model is similarly bounded), is fully tested at every boundary, and is described in the reader's `<remarks>` — but should also be stated in the package/README-level docs so torrent files with pathological integers fail predictably for consumers. Arbitrary-precision support (`BigInteger` accessor) is an option if ever needed; not recommended now.

**R‑8. Document `GetString` lossy decoding of binary content.** `GetString` (reader line ≈318), `BencodeElement.GetString`, and `StringConverter` decode with `Encoding.UTF8.GetString`, which substitutes U+FFFD for invalid sequences — so binding a binary field (e.g. a torrent's `pieces`) to a `string` property silently corrupts it, and `Utf8JsonReader.GetString` (which throws on invalid UTF‑8) sets a different expectation. The contract is now pinned by `GetString_WhenContentNotValidUtf8_ShouldDecodeWithReplacementCharacters` and documented on the member. Recommend a `<remarks>` warning on `StringConverter`-bound members in the serializer docs, and consider a future strict accessor (`GetStringStrict()` or a reader option) for consumers who prefer S.T.J's failure mode.

**R‑9. Default `MaxDepth` divergence.** Reader/Writer/Document default to 256; `BencodeSerializerOptions.DefaultMaxDepth` is 64. S.T.J uses 64 for reader/document/serializer and 1000 for the writer. The split is defensible (the serializer is the safety-critical entry point) but undocumented; state it in the options docs, or unify on 64 to match S.T.J if a breaking pass is ever taken.

### Alignment with System.Text.Json (parity inventory)

The library's shape is a faithful S.T.J transposition: ref‑struct reader/writer, read-only `BencodeDocument`/`BencodeElement`, mutable `BencodeNode` family, static `BencodeSerializer`, options with `MakeReadOnly()`/`IsReadOnly`, the full attribute family (`[BencodePropertyName]`, `[BencodeIgnore(Condition=…)]`, `[BencodeInclude]`, `[BencodeRequired]`, `[BencodeConstructor]`, `[BencodeExtensionData]`, `[BencodePropertyOrder]`, `[BencodeConverter]`, naming-policy and creation/unmapped-handling attributes), `IBencodeOn(De)Serializ(ing|ed)` callbacks, string/number enum converters with `[BencodeStringEnumMemberName]`, and the five built-in naming policies. Enum values match S.T.J (`BencodeUnmappedMemberHandling`, `BencodeObjectCreationHandling`, `BencodeIgnoreCondition` — including rejecting `Always` as the options-level default, as S.T.J does).

**R‑10. API-surface gaps worth closing, in priority order** (none affect spec compliance):

| Area | Missing vs S.T.J | Notes |
|---|---|---|
| Serializer | Synchronous `Serialize(Stream, …)` / `Deserialize(Stream, …)` | Only `byte[]`, `IBufferWriter`, and async stream overloads exist today. |
| Serializer ⇄ DOM | `SerializeToNode` / `Deserialize(BencodeNode)` / `SerializeToDocument` bridges | `BencodeNodeConverter` exists, so plumbing is small. |
| Document | `BencodeElement.GetRawBytes()` (S.T.J `GetRawText`), `BencodeDocument.WriteTo(Utf8BencodeWriter)`, element `Clone()` | Raw-bytes access matters for torrents: computing an info-hash requires the exact `info` slice; today only the reader surface can recover it. **Highest-value gap on the list.** |
| Reader | `ValueTextEquals(ReadOnlySpan<byte>)`, `CopyString`, multi-segment/`isFinalBlock` streaming with `BencodeReaderState` | Streaming reads are a larger investment; defer until a consumer needs incremental parsing. |
| Writer | `WriteRawValue(ReadOnlySpan<byte>)` | Pairs with `GetRawBytes` for round-tripping verified slices. |
| Nodes | `DeepClone()`, `GetPath()`, `ReplaceWith()` | `DeepEquals` already exists. |
| Exceptions | S.T.J funnels everything through `JsonException`; Bencode splits `BencodeFormatException : FormatException` / `BencodeSerializationException : Exception` | The split is reasonable and well-documented; recommend keeping it but adding a shared base or documenting the catch guidance. |
| Options | `PropertyNameCaseInsensitive` defaults to `true`; S.T.J `General` defaults to `false` (only `Web` is `true`) | Bencode wire keys are raw bytes and case-sensitive; the lenient read default is a deliberate, tested choice (`BencodeSerializerDefaults.General` docs) — document the divergence rather than change it. |

### Test coverage

**R‑11. Suite state and remaining gaps.** The suite (now 778 passing tests across 47 files) covers the BEP 3 grammar exhaustively on the reader — every malformed category in this report's matrix has a named `[DataRow]` — plus canonical round-trips asserting exact bytes, and both DOMs re-verify malformed rejection. Added this review: 16 writer/serializer/`Skip` contract tests and 3 reader pins (string-length `int` overflow ×2, lossy `GetString`). Remaining recommendations:

- Add a randomized round-trip sweep (`[TestCategory("Stress")]`): generate arbitrary node trees, write, re-read, `DeepEquals` — cheap insurance for the R‑4 writer rewrite.
- Add a real-world fixture test: a small authentic `.torrent` file parsed via all three surfaces (reader, document, serializer POCO), pinning the binary `pieces` handling end-to-end.
- When R‑6 lands, mirror the malformed-key rows into lenient-mode acceptance tests.

## 4. Verification record

| Checkpoint | Result |
|---|---|
| Baseline, full regression tier | 759 passed / 0 failed |
| Red phase (16 intended-behavior tests committed before fix) | 16 failed, as expected (commit `8e0c0a6b`) |
| Green phase (writer guards + `Skip` alignment, commit `1e17b512`) | 775 passed / 0 failed |
| Final, with coverage pins (commit `49234e2d`) | **778 passed / 0 failed** |
