# `Bodu.Text.Toml` — Specification-Compliance and `System.Text.Json`-Alignment Review

**Date:** 2026-06-11 · **Remediation completed:** 2026-06-11
**Scope:** `Bodu.Text.Toml/src` and `Bodu.Text.Toml/test`
**Specifications:** TOML v1.0.0 (final) and TOML v1.1.0 (draft)
**Alignment target:** `System.Text.Json` (functionality, API shape, and usage idiom)
**Status:** **Remediated** — every Critical/Major/Minor recommendation below has been implemented
(or, for m1, withdrawn against the authoritative v1.1.0 draft); the 16 findings tests are green,
the toml-test conformance corpus is vendored and passes 1,410/1,410 cases in both profiles, and
the full suite stands at **2,584 passed / 0 failed**. Per-finding status is annotated inline.

---

## 1. Executive summary

`Bodu.Text.Toml` is a strong, carefully built TOML implementation. Its lexical and
structural parser is rigorous: every "commonly missed" rule that this review probed —
Unicode escape surrogate rejection, underscore placement, leading-zero rejection,
`Int64.MinValue` boundary handling, bare-carriage-return rejection, three-or-more quote
runs in multi-line strings, trailing-content enforcement, and the v1.0/v1.1 gating of
`\e`/`\x`/optional-seconds/inline-table relaxations — was found **compliant**. The value
formatter is equally solid: this review could not construct a *value* that the writer
emits and the project's own parser then rejects or re-reads as a different type. The
serializer and DOM follow the `System.Text.Json` shape closely and faithfully, including
the freeze-after-first-use options contract and attribute-over-policy precedence.

The defects that do exist cluster in three places:

1. **Robustness — a remotely triggerable denial of service.** Table nesting created by
   dotted keys and `[a.b.c…]` headers is **not** bounded by `MaxDepth`, and the reader
   flattens the parsed tree with unbounded recursion. A ~200 KB document crashes the
   process with an uncatchable `StackOverflowException`. *Empirically reproduced (process
   exit code 134).*

2. **Writer structural validation is almost entirely absent.** Unlike `Utf8JsonWriter`,
   which validates by default, `Utf8TomlWriter` will buffer and emit duplicate keys,
   accept a value with no property name (deferred `NullReferenceException`), silently emit
   *zero bytes* for a root scalar, concatenate a second document, and surface misuse as raw
   `InvalidCastException`. The duplicate-key path is reachable from the **public
   serializer** via wire-name collisions, so a normal POCO can serialize to a document the
   library itself cannot read back. *Empirically reproduced.*

3. **A serializer round-trip break for `DateTime`.** A `DateTime` whose `Kind` is `Utc`
   or `Local` is written as an offset date-time but the `DateTime` converter only *reads*
   local date-times, so `Serialize` then `Deserialize<T>` of the same type throws. *Empirically
   reproduced.*

Two further items are worth a decision rather than a fix: the deliberate rejection of RFC
3339 leap seconds (`:60`), and the fact that `Utf8TomlReader`/`Utf8TomlWriter` are
presented as high-performance forward-only `ref struct`s in the `Utf8JsonReader`/`Writer`
idiom when they are in fact buffering facades over a fully materialized document tree.

Finally, `ROADMAP.md` states the implementation was "Validated against … the vendored
**toml-test conformance corpus**". **That corpus is not present in the repository and no
test references it.** Wiring it in is the single highest-leverage way to close the
remaining edge-case gaps catalogued in §6.

### Verdict

| Area | Verdict |
|---|---|
| Lexical / value parsing (strings, numbers, date-times, encoding) | **Compliant**, with one robustness defect (unbounded recursion) and one deliberate deviation (leap second) |
| Structural parsing (keys, tables, arrays of tables, inline tables) | **Compliant**, with one gray-area redefinition corner |
| Writer — value formatting | **Compliant** (round-trip safe) |
| Writer — structural validity | **Non-compliant by omission** (no duplicate-key / state validation) |
| Serializer + options + converters | **Strong alignment**, one round-trip break (`DateTime` UTC/Local) |
| DOM (mutable `TomlNode`, read-only `TomlDocument`) | **Strong alignment**, minor gaps (`GetPath`, `GetRawText`) |
| Test suite | **Excellent conventions and breadth**; specific edge-case gaps; **conformance corpus missing** |

---

## 2. Methodology

This review combined static forensic analysis with **empirical execution**, as required.

1. **Five forensic source reviews** read the implementation line by line against the TOML
   v1.0.0 and v1.1.0 grammars and the `System.Text.Json` reference behavior:
   lexical/value parsing, structural semantics, the writer, the serializer/DOM alignment,
   and the existing test suite.

2. **Empirical validation by data-driven tests (TDD).** Every Critical and Major finding
   is encoded as a new MSTest test that asserts the **spec-intended** behavior (acceptance,
   rejection with the correct exception type, round-trip equality, or structural
   validation). Tests that describe behavior the library already gets right pass; tests
   that expose a defect fail. The failing tests are the executable proof of each finding
   and double as the ready-made "red" for the recommended fix. They are committed
   alongside this report.

3. **Direct execution probes.** The denial-of-service and duplicate-key findings were
   additionally reproduced by running the compiled library in a child process and
   observing the actual outcome (process abort, emitted bytes, re-read failure).

**Baseline:** before any additions, the full suite was green —
`dotnet test … --settings regression.runsettings` → **1103 passed, 0 failed**.

**After adding the empirical tests:** **1168 total, 1152 passed, 16 failed.** All 1103
pre-existing tests remained green; the 16 failures were the new findings tests and map
one-to-one to the recommendations below.

**After remediation:** **2,584 total, 2,584 passed, 0 failed** — the original suite, the findings
tests (now green), the section 6 test improvements, and the vendored toml-test conformance corpus
(1,410 cases across the v1.0.0 and v1.1.0 profiles, empty skip list).

---

## 3. Empirical results — the 16 findings tests

Each row is a committed, runnable test. "Result today" records the behavior observed **at review
time**; after remediation all sixteen tests pass (F16 was re-pointed at the authoritative v1.1.0
draft behavior — see m1).

| # | Test (method) | File | Demonstrates | Result today |
|---|---|---|---|---|
| F1 | `Constructor_WhenHeaderNestingExceedsMaxDepth_ShouldThrowTomlFormatException` | `Utf8TomlReaderTests.SpecCompliance.cs` | `MaxDepth` not enforced for `[a.b.c…]` headers | No exception (deep input accepted) |
| F2 | `Constructor_WhenDottedKeyNestingExceedsMaxDepth_ShouldThrowTomlFormatException` | `Utf8TomlReaderTests.SpecCompliance.cs` | `MaxDepth` not enforced for dotted keys | No exception |
| F3 | `Serialize_WhenMembersShareWireName_ShouldThrowTomlSerializationException` | `TomlSerializerTests.RoundTrip.cs` | Serializer emits duplicate keys → invalid doc | No exception; emits `shared = 1` / `shared = 2` |
| F4 | `Serialize_WhenExtensionDataKeyCollidesWithMember_ShouldThrowTomlSerializationException` | `TomlSerializerTests.RoundTrip.cs` | Extension-data key collides with member → duplicate key | No exception |
| F5 | `SerializeDeserialize_WhenDateTimeKindUtc_ShouldRoundTripToSameInstant` | `TomlSerializerTests.RoundTrip.cs` | `DateTime(Utc)` write→read break | Throws `TomlSerializationException: Expected a local date-time but found 'OffsetDateTime'` |
| F6 | `SerializeDeserialize_WhenDateTimeKindLocal_ShouldRoundTripToSameInstant` | `TomlSerializerTests.RoundTrip.cs` | `DateTime(Local)` write→read break | Same as F5 |
| F7 | `Write_WhenDuplicatePropertyNameInTable_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Writer accepts duplicate keys | No exception |
| F8 | `Write_WhenValueWrittenWithoutPropertyName_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Value with no key → deferred crash | Throws `NullReferenceException` (wrong type, wrong site) |
| F9 | `Write_WhenRootValueIsScalar_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Root scalar silently emits nothing | No exception; 0 bytes written |
| F10 | `Write_WhenRootValueIsArray_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Root array silently emits nothing | No exception; 0 bytes written |
| F11 | `Write_WhenSecondRootTableWritten_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Second root document concatenated | No exception |
| F12 | `WritePropertyName_WhenCurrentContainerIsArray_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Misuse → raw cast failure | Throws `InvalidCastException` (wrong type) |
| F13 | `WritePropertyName_WhenCalledTwiceConsecutively_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Second name silently drops the first | No exception |
| F14 | `WriteEndTable_WhenCurrentContainerIsArray_ShouldThrowInvalidOperationException` | `Utf8TomlWriterTests.Validation.cs` | Mismatched close → raw cast failure | Throws `InvalidCastException` (wrong type) |
| F15 | `WriteString_WhenValueContainsLoneSurrogate_ShouldThrowArgumentException` | `Utf8TomlWriterTests.Validation.cs` | Lone surrogate fails late and mistyped | No exception at call site (deferred to close) |
| F16 | `Constructor_WhenCommentContainsControlCharacter_ForV11_ShouldNotThrow` | `Utf8TomlReaderTests.SpecCompliance.cs` | v1.1 relaxed comment chars not implemented | Throws `TomlFormatException` |

> The new `Utf8TomlReaderTests.SpecCompliance.cs` also adds **49 passing** acceptance/rejection
> vectors (leap-year matrix, surrogate-escape boundaries, signed radix integers, offset
> ranges, bare-vs-quoted duplicate keys, invalid/overlong UTF-8, CRLF documents,
> EOF-without-newline, BOM) that confirm correct behavior and lock it against regression.
> The new `TomlSerializerTests.SystemTextJsonParity.cs` adds **side-by-side** tests that run
> the same models through `JsonSerializer` and `TomlSerializer` and confirm matching
> behavior for camelCase naming, `WhenWritingNull`, attribute-over-policy precedence, and
> options freezing.

### Direct execution evidence

```text
# Unbounded recursion (F1/F2) — child process, default options, [a.a.a…] header:
depth=300,    doc bytes=602     parsed OK   (MaxDepth default 256 NOT enforced)
depth=20000,  doc bytes=40002   parsed OK
depth=100000, doc bytes=200002  Stack overflow.  Repeat 23502 times: Utf8TomlReader.Flatten(...)
                                 → process aborted, exit code 134

# Duplicate keys via serializer (F3) — two [TomlPropertyName("shared")] members:
--- emitted by TomlSerializer.Serialize ---
shared = 1
shared = 2
re-read by TomlSerializer.Deserialize FAILED: TomlFormatException: The key is already defined.
```

---

## 4. Recommendations by severity

### CRITICAL

**C1 — Bound table-nesting depth and remove unbounded recursion (DoS).**
`MaxDepth` is enforced only for arrays and inline tables (`EnterDepth`,
`TomlDocumentParser.cs:917`), not for tables created by dotted keys
(`WalkDottedSegment`, ~`:1562`) or `[a.b.c…]` headers (`WalkHeaderSegment`, ~`:1511`).
`Utf8TomlReader.Flatten` (`Utf8TomlReader.cs:289`) then recurses once per level. A hostile
~200 KB document exhausts the stack and aborts the process with an **uncatchable**
`StackOverflowException` — directly contradicting the `MaxDepth` documentation
(`TomlReaderOptions.cs:20`, "guards against stack-exhausting input").
*Recommendation:* count depth in `WalkHeaderSegment`/`WalkDottedSegment` against `MaxDepth`
(throwing `TomlFormatException`), and make `Flatten` iterative. **Tests: F1, F2.** ✅ **Addressed:** table creation is depth-tracked per node and bounded by `MaxDepth` in `WalkHeaderSegment`/`WalkDottedSegment`/`DefineStandardTable`/`DefineArrayTable`.

**C2 — Validate against duplicate keys in the writer (and serializer).**
`TomlTableWriterNode.Add` (`:37`) is a blind append; `TomlCanonicalWriter.WriteTableBody`
(`:55`) emits every pair. Writing a key twice yields a document the project's own parser
rejects (`TomlDocumentParser.cs:1551`). This is reachable from the **public serializer**:
two members sharing a wire name (duplicate `[TomlPropertyName]` or a naming-policy
collision — `MetadataResolver.cs:116`, `ObjectConverter.cs:103`) and an extension-data
entry colliding with a property (`ObjectConverter.cs:134`). Note this is *unlike* JSON,
where duplicate names are legal — for TOML "trust the caller" is not viable.
*Recommendation:* detect duplicates in `TomlTableWriterNode.Add` and throw at the `Write*`
call site; reject wire-name collisions in `MetadataResolver`. **Tests: C2 → F7; serializer
reachability → F3, F4.** ✅ **Addressed:** duplicate keys are rejected at the `WritePropertyName` call site and in `TomlTableWriterNode.Add`; `MetadataResolver` rejects duplicate wire names and `ObjectConverter` rejects colliding extension-data keys, both as `TomlSerializationException`.

**C3 — Reject a value written without a property name at the call site.**
`TableFrame.AddValue` (`Utf8TomlWriter.cs:314`) stores `PendingKey!` unchecked; failure is
deferred to a `NullReferenceException` inside `TomlCanonicalWriter.IsBareKey` at root close,
far from the bug. *Recommendation:* throw `InvalidOperationException` from the value-write
path when the enclosing table has no pending key. **Test: F8.** ✅ **Addressed:** the writer validates the pending-key state eagerly at every value-producing call.

### MAJOR

**M1 — Make the `DateTime` converter read what it writes.**
`DateTimeConverter.Write` maps `Kind != Unspecified` to an offset date-time
(`DateTimeConverter.cs:38`) but `Read` accepts only `LocalDateTime` (`:26`). Serializing any
POCO containing `DateTime.UtcNow`/`DateTime.Now` produces a document its own
`Deserialize<T>` rejects. *Recommendation:* have `Read` also accept `OffsetDateTime`
(returning `.UtcDateTime`/`.DateTime` per the captured kind). **Tests: F5, F6.** ✅ **Addressed:** `DateTimeConverter.Read` accepts an offset date-time and returns its UTC instant.

**M2 — Give the writer the structural validation `Utf8JsonWriter` ships by default.**
Beyond C2/C3, the writer silently emits zero bytes for a root scalar/array
(`Utf8TomlWriter.cs:258`), concatenates a second root document (no completion state),
drops the first of two consecutive property names (`:171`), and surfaces frame/kind misuse
as raw `InvalidCastException`/`ArgumentOutOfRangeException` and a **message-less**
`InvalidOperationException` (`:243`, which also violates the repo's resx-message rule).
*Recommendation:* validate frame kind, pending-key state, root-is-table, and
document-completion in each `Write*`, throwing resx-messaged exceptions; consider a
`SkipValidation` opt-out later. **Tests: F9, F10, F11, F12, F13, F14.** ✅ **Addressed:** the writer validates container kind, root-is-table, document completion, and consecutive property names, throwing resx-messaged `InvalidOperationException`.

**M3 — Add a conformance-corpus suite, or correct the ROADMAP.**
`ROADMAP.md:295` claims validation against "the vendored **toml-test conformance corpus**
(run in both the v1.0 and v1.1 profiles as a Regression-tier suite)". No such corpus or
test exists in the repository. *Recommendation:* vendor `toml-lang/toml-test` and run its
valid/invalid cases as a `[TestCategory("Regression")]` suite under both `TomlSpecVersion`
profiles. This mechanically closes most §6 gaps. Until then, the ROADMAP statement should
be corrected. ✅ **Addressed:** the corpus is vendored at `Bodu.Text.Toml/test/TomlTestCorpus/`
(MIT licence included) and run by `TomlTestCorpusTests` as a Regression-tier suite in both
profiles — **1,410/1,410 cases pass with an empty skip list**; the ROADMAP statement now points at
the vendored location.

**M4 — Decide leap-second policy explicitly.**
`second == 60` is rejected (`TomlDocumentParser.cs:1319`, `:1342`). RFC 3339 — incorporated
by reference by TOML for date-times — permits `:60`, so `1990-12-31T23:59:60Z` is strictly
valid TOML that this parser rejects (and `toml-test` would flag). The CLR types cannot
represent it. *Recommendation:* either clamp to `59.999…`/fold into the next minute, or keep
the rejection and document it as a known representational deviation. (Captured today by the
existing test `Read_WhenTimeIsLeapSecond_…`; this is a policy decision, not a code bug.)
✅ **Addressed (documented):** the rejection is retained — the CLR types cannot represent second
60 — and `Utf8TomlReader` now documents leap seconds, year 0000, and offsets beyond ±14:00 as
deliberate RFC 3339 representational deviations. The corpus contains no conflicting case.

### MINOR

**m1 — Implement (or document) v1.1 relaxed comment characters.** `SkipComment`
(`TomlDocumentParser.cs:252`) applies the v1.0 control-character prohibition unconditionally;
under `V1_1` a comment containing control characters is wrongly rejected. *Recommendation:*
gate the control-character check on `_specVersion`, rejecting only NUL and CR/LF-class
characters under v1.1. **Test: F16.** ⚠️ **Withdrawn:** verified against the authoritative
v1.1.0 draft document, which states "Control characters other than tab (U+0000 to U+0008, U+000A
to U+001F, U+007F) are not permitted in comments" — identical to v1.0.0. The library's
unconditional validation is correct; the finding was based on an earlier draft state, and F16 now
pins rejection in both profiles.

**m2 — Validate strings/keys eagerly in the writer.** A lone surrogate throws
`InvalidOperationException` (`TomlCanonicalWriter.cs:342`) at root close rather than
`ArgumentException` at the `WriteString` call, inconsistent with both `Utf8JsonWriter` and
the writer's own `TomlSerializationException` for `MaxDepth`. *Recommendation:* validate at
the call site with `ArgumentException`. **Test: F15.** ✅ **Addressed:** `WriteString`/`WritePropertyName` validate surrogate pairing eagerly and throw `ArgumentException` at the call site.

**m3 — Preserve CRLF inside multi-line strings.** CRLF is normalized to LF
(`TomlDocumentParser.cs:631`, `:681`); reference implementations preserve the source bytes.
*Recommendation:* append the source newline verbatim. ✅ **Addressed:** both multi-line forms preserve the source CRLF/LF spelling, matching the spec's "newline characters remain intact".

**m4 — Remove or correct dead `SpecVersion` on the writer.** `TomlWriterOptions.SpecVersion`
is never consulted (`Utf8TomlWriter.cs:105`) yet its XML doc claims it "selects … formatting
conveniences", contradicting `TomlSpecVersion.cs:20`. *Recommendation:* delete the property
or document it as currently inert. ✅ **Addressed (documented):** the property is retained for
compatibility and its documentation now states it is currently inert.

**m5 — `"canonical"` is not a canonical form.** Keys are emitted in insertion order
(`TomlTableWriterNode.cs:24`), and `TomlObject` is backed by a plain `Dictionary` whose order
is unspecified after `Remove` (`TomlObject.cs:27`), so equal documents can serialize to
different bytes. *Recommendation:* sort keys ordinally (or rename the concept to "normalized
layout") and back `TomlObject` with an order-preserving structure. ✅ **Addressed:** `TomlObject`
now preserves insertion order across removals (order list alongside the map), and the writer
documentation describes the output as a normalized, insertion-order-deterministic layout rather
than a hashable canonical form.

**m6 — Honor `MaxDepth`/positional diagnostics on offsets.** RFC offsets beyond ±14:00 and
year `0000` are rejected by CLR type limits with a generic message
(`TomlDocumentParser.cs:1160`, `:1301`); `TomlFormatException.Offset` is documented as a byte
offset but receives UTF-16 char offsets (`:1590` vs `TomlFormatException.cs:73`).
*Recommendation:* document the representational limits and reconcile the offset units.
✅ **Addressed (documented):** `TomlFormatException.Offset` is documented as a character offset
into the decoded text, and the representational limits are documented on `Utf8TomlReader`.

### INFORMATIONAL

- **I1 — The "forward-only `ref struct` reader/writer" framing oversells the design.**
  `Utf8TomlReader` decodes the whole input to a `string`, runs the full parser in its
  **constructor**, and flattens the tree into a `List<TomlReaderToken>` with boxed scalars;
  `Read()` is a cursor over that list (`Utf8TomlReader.cs:82`–`94`, `289`). `Utf8TomlWriter`
  buffers a node tree and transcodes UTF-16→UTF-8 once at the end (`Utf8TomlWriter.cs:275`).
  Both are `ref struct`s that allocate freely, gaining the usage restrictions of the
  `Utf8JsonReader`/`Writer` idiom without the zero-allocation benefit, and they throw from
  the constructor rather than from `Read()`. Missing surface vs `Utf8JsonReader`/`Writer`:
  `ValueSpan`/`BytesConsumed`/`TokenStartIndex`, continuation/`isFinalBlock` state,
  `Flush`/`Reset`/`SkipValidation`. The XML remarks are honest about the buffering; the
  *summary* lines should be softened to match. ✅ **Addressed (documented):** the reader summary
  no longer claims a high-performance streaming design, and the writer now validates by default
  (M2), narrowing the idiom gap.
- **I2 — `Skip()` is a no-op on `PropertyName`**, diverging from `Utf8JsonReader.Skip`
  (`Utf8TomlReader.cs:254`); document or align. ✅ **Addressed:** `Skip()` on a property name now
  advances past the property's value, matching `Utf8JsonReader.Skip`.
- **I3 — Gray-area redefinition corner:** `[x.y.z]` → `[x]` → `y.q = 1` → `[x.y]` is accepted
  because `WalkDottedSegment` never moves a traversed implicit super-table into the
  "defined-by-dotted-keys" set. Reference implementations differ here; pin the intended
  behavior with `toml-test` (M3). ✅ **Addressed:** `WalkDottedSegment` now marks traversed
  implicit super-tables as dotted-key-defined, so the later header is rejected; the full
  conformance corpus passes with this behavior.
- **I4 — `-nan` loses its sign** on both read and write (acceptable; the spec leaves NaN
  sign/payload implementation-defined).

---

## 5. `System.Text.Json` alignment matrix

Legend: **Aligned** · **Partial** · **Missing** · **Diverges (intentional)**

| Area | Status | Notes |
|---|---|---|
| `TomlSerializer.Serialize`/`Deserialize` (string, UTF-8 span, `IBufferWriter`, `Stream`, async) | **Aligned** | Mirrors `JsonSerializer`'s overload families. |
| Serialize-to/from DOM (`SerializeToNode`/`DeserializeFromNode`/`SerializeToDocument`/`SerializeToElement`) | **Missing** | No serializer↔DOM bridge; `JsonSerializer` has all four. Use `TomlNode.Parse`/`WriteTo` as a workaround. |
| Root non-object handling (`T = int`, `List<T>`) | **Diverges** | TOML's root must be a table; serializer enforces `RequireRootIsTable`. Principled, but the writer should *also* enforce it (M2/F9/F10). |
| `PropertyNamingPolicy` (+ camel/snake/kebab) | **Aligned** | Side-by-side parity confirmed; richer built-in policy set than STJ. |
| `PropertyNameCaseInsensitive` | **Aligned** | |
| `DefaultIgnoreCondition` (`WhenWritingNull`/`WhenWritingDefault`/`Always`/`Never`) | **Aligned** | `WhenWritingNull` parity confirmed empirically. |
| `IncludeFields` / `[TomlInclude]` | **Aligned** | |
| `UnmappedMemberHandling` | **Aligned** | |
| `PreferredObjectCreationHandling` | **Aligned** | |
| `MaxDepth` | **Aligned** | Honored for serializer object graphs, array/inline-table parsing, and (post-remediation) dotted/header table nesting (C1). |
| Options freeze after first use / `IsReadOnly` / `MakeReadOnly` | **Aligned** | Throws `InvalidOperationException` on post-use mutation, confirmed against `JsonSerializerOptions`. |
| `TomlSerializerDefaults` | **Aligned** | Defaults analogue present. |
| Attribute family (`[TomlPropertyName]`, `[TomlIgnore]`, `[TomlInclude]`, `[TomlRequired]`, `[TomlConstructor]`, `[TomlConverter]`, `[TomlExtensionData]`, `[TomlPropertyOrder]`, `[TomlUnmappedMemberHandling]`, `[TomlObjectCreationHandling]`) | **Aligned** | Attribute-over-policy precedence confirmed empirically. |
| Converter model (`TomlConverter<T>`, factory, `CanConvert`, precedence) | **Aligned** | `Read(ref reader, type, options)` / `Write` shape mirrors `JsonConverter<T>`. |
| Built-in converters (primitives, enum by-name/by-number, `Guid`, `Uri`, `char`, `byte[]`, `Nullable<T>`, collections, dictionaries incl. non-string keys) | **Aligned** | Non-string dictionary-key coverage is exemplary. |
| Native date/time kinds (`DateTimeOffset`/`DateOnly`/`TimeOnly`/`DateTime`) | **Diverges (intentional)** | Principled use of TOML's native date-time kinds; the `DateTime` UTC/Local read path is fixed (M1) and round-trips to the same instant. |
| Callbacks (`ITomlOnSerializing`/`Serialized`/`Deserializing`/`Deserialized`) | **Aligned** | Mirrors `IJsonOn*` interfaces. |
| Polymorphism (`[JsonPolymorphic]`/`[JsonDerivedType]`) | **Missing** | No equivalent exists; document as out of scope or implement. |
| Mutable DOM `TomlNode` vs `JsonNode` (`Parse`, `DeepClone`, `DeepEquals`, `GetValue<T>`, `AsObject/AsArray/AsValue`, `GetValueKind`, `WriteTo`, `Parent`/`Root`, re-parent guard) | **Aligned** | Re-parent guard throws `Op_Invalid_NodeAlreadyHasParent`, matching `JsonNode`. |
| `TomlNode.GetPath()` | **Missing** | `JsonNode.GetPath()` has no analogue. |
| Read-only DOM `TomlDocument`/`TomlElement` vs `JsonDocument`/`JsonElement` (`Parse`, `RootElement`, `Dispose`, `ValueKind`, `Get*`, `GetProperty`/`TryGetProperty`, `EnumerateObject`/`EnumerateArray`) | **Aligned** | Pooled-row design present (`TomlDocument.Row.cs`). |
| `TomlElement.GetRawText()` / `WriteTo` | **Missing** | `JsonElement.GetRawText()` has no analogue. |
| Reader idiom (`Utf8TomlReader` vs `Utf8JsonReader`) | **Partial / Diverges** | Same surface shape (`Read()`, `TokenType`, `Get*`, `CurrentDepth`, `Skip` — now `Utf8JsonReader`-aligned on property names) over a pre-parsed buffer; missing `ValueSpan`/positional/continuation state; throws from ctor (documented, I1). |
| Writer idiom (`Utf8TomlWriter` vs `Utf8JsonWriter`) | **Partial / Diverges** | Buffers a node tree; no `Flush`/`Reset`/`BytesCommitted`; **validates by default post-remediation** (duplicate keys, container kind, root-is-table, document completion — C2/C3/M2). |
| Exception model | **Diverges** | `TomlFormatException : FormatException` (carries line/column/offset) and `TomlSerializationException : Exception`; STJ funnels both through `JsonException`. The split is defensible; the position info is a plus. |

---

## 6. Test-suite audit

The existing suite (~640 methods across 56 files before this review) is **conventionally
exemplary**: zero naming-convention violations, `Assert.ThrowsExactly` with block-bodied
lambdas throughout (191 uses, no legacy patterns), consistent `ParamName` guard assertions,
correct KAT-record + `[DynamicData]` + `KatDisplayName` wiring, `[DataRow]` confined to
primitive scalars, one `Smoke` test per primary type, and `Regression` tags on the
exhaustive sweeps. Coverage breadth is genuinely good.

The gaps this review found and the new tests now close (all added per the same conventions):

**Closed by `Utf8TomlReaderTests.SpecCompliance.cs` (now passing, locking behavior):**
leap-year matrix (`2000-02-29` valid; `1900-02-29`/`2021-02-29` invalid; day `00`), `\uD800`–
`\uDFFF` surrogate-escape rejection and `\U00110000`/`\U0010FFFF` boundary, literal control
characters in all four string forms, bare-CR line terminator and bare-CR in multi-line
strings, signed radix integers (`+0x1`/`-0o7`/`+0b1`), offset range (`+24:00`/`+00:60`),
bare-vs-quoted duplicate keys, dotted-key-over-scalar, dotted extension of a closed inline
table, `[[a]]`-then-`[a]`, whole-document CRLF, EOF-without-trailing-newline (value,
comment, header), leading BOM, invalid and overlong UTF-8 byte sequences, `1e400`→∞, and
negative-zero with `double.IsNegative`.

**Remediation status of the items above:**

- **Conformance corpus (M3)** — ✅ vendored and wired (`TomlTestCorpusTests`, Regression tier,
  both profiles, 1,410/1,410 passing).
- **Stream serializer depth** — ✅ pre-canceled-token, non-seekable-stream, and chunked-stream
  tests added to `TomlSerializerTests.Streams.cs`.
- **Polymorphism** — **remains open by design**: no `[TomlPolymorphic]`/`[TomlDerivedType]`
  equivalent exists; the alignment matrix documents it as missing. Implementing it is a feature
  decision outside this remediation's scope.
- **Convention nits** — ✅ the dual-outcome `*_ShouldHonorSpecVersion` methods are split into
  one-outcome pairs, the inert negative-zero assertion is replaced by the sign-bit test, the
  root-file restatements are removed, and `InvalidKat.ExceptionType` is now asserted by the
  malformed-document runner.

---

## 7. Reproducing this review

```bash
# Full suite post-remediation (2,584 pass, 0 fail — includes the 1,410-case conformance corpus):
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings regression.runsettings

# Just the conformance corpus:
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings regression.runsettings \
  --filter "FullyQualifiedName~TomlTestCorpus"

# Just the findings/empirical suites:
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj \
  --filter "Name~SpecValid|Name~SpecInvalid|Name~RoundTrip|Name~SystemTextJson|Name~MaxDepth"
```

New test files added by this review:

- `Bodu.Text.Toml/test/Utf8TomlReaderTests.SpecCompliance.cs`
- `Bodu.Text.Toml/test/Utf8TomlWriterTests.Validation.cs`
- `Bodu.Text.Toml/test/TomlSerializerTests.RoundTrip.cs`
- `Bodu.Text.Toml/test/TomlSerializerTests.SystemTextJsonParity.cs`

All §3 findings tests are green. New test assets added during remediation:

- `Bodu.Text.Toml/test/TomlTestCorpusTests.cs` — the conformance-corpus runner.
- `Bodu.Text.Toml/test/TomlTestCorpus/` — the vendored `toml-lang/toml-test` corpus (MIT).
