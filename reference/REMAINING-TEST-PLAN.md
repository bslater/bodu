# Bodu.Text.Bencode + Bodu.Text.Toml — Remaining Test Plan (handoff)

**Branch: `claude/relaxed-rubin-m62s2x`** — all work continues on this branch. Create it locally from the latest remote head if needed (`git fetch origin claude/relaxed-rubin-m62s2x && git switch claude/relaxed-rubin-m62s2x`). Do **not** push to `master`; do **not** open a PR unless asked.

This document is **self-contained**: every scenario catalog, the expected test data, and the probed library contracts are written out below. In addition, the **full S.T.J test corpus is committed to this branch** alongside this file at [`system-text-json-tests/`](system-text-json-tests/) (its `Common/` directory holds the real `[Fact]`/`[Theory]` bodies; `System.Text.Json.Tests/` holds the reader/writer, DOM, and feature tests) — consult it for exact `[InlineData]` values and assertion patterns the catalogs below abbreviate. It is reference-only (not compiled, not shipped; remove before any `master` merge). The committed Bodu tests on this branch (B1, B2a, T1, B2b-part-1) are the canonical **style/pattern exemplars** — read them before writing more.

---

## 0. Goal & current state

We are mirroring the S.T.J test suite across both self-contained libraries — `Bodu.Text.Bencode` and `Bodu.Text.Toml` — at **comprehensive per-dimension depth** (the agreed bar: ~300–500 executed tests per library, scenario-for-scenario with S.T.J's applicable dimensions, **not** a literal LOC copy). Each library mirrors `System.Text.Json`: a ref-struct `Utf8*Reader`/`Utf8*Writer`, a `*Serializer` POCO mapper, a mutable `*Node` DOM, and a read-only `*Document` DOM.

### Committed passes (done)

| Pass | Scope | Commit | Result |
|---|---|---|---|
| B1 | Bencode `Utf8BencodeReader`/`Writer` — tokens, skip, malformed, round-trip | `8e62a33f` | — |
| B2a | Bencode serializer values/collections/dictionaries/nullables | `2705b2b3` | BVT 210 / Reg 344 |
| T1 | TOML `Utf8TomlReader`/`Writer` — strings, ints, floats, datetimes, keys, tables, arrays, inline tables, comments, spec-version, malformed | `5a4536b9` | BVT 264 / Reg 392 |
| B2b‑p1 | Bencode serializer features part 1: PropertyName, NamingPolicy, PropertyOrder, PropertyVisibility | `1f9c1e91` | BVT 233 / Reg 382 |
| **B2b‑p2** | Bencode serializer features part 2: Constructor, Required, ExtensionData, UnmappedMembers, ObjectCreation, Callbacks, ConverterResolution, MaxDepth, EnumConverters (+ src fix: non-public setter assigned on read only with `[BencodeInclude]`) | `80e24b22` | BVT 310 / Reg 459 |
| **T2a** | TOML serializer values/collections/dictionaries/nullables | `5e297e68` | BVT 400 / Reg 529 |
| **B3** | Bencode DOMs + exceptions: `BencodeDocument`/`BencodeElement` (Parse grammar/depth, accessor kind-matrix, enumerators, disposal), `BencodeNode`/`Object`/`Array`/`Value` (full collection surfaces, conversions, DeepEquals/DeepClone), exception ctor surfaces (+ src fixes: `BencodeNode.Parse` now rejects trailing bytes per its documented contract; `BencodeObject`/`BencodeArray` detach a removed/replaced child's `Parent`) | `23d8390e` | BVT 468 / Reg 680 |
| **T2b** | TOML serializer features: PropertyName, NamingPolicy, PropertyVisibility, PropertyOrder (**order IS honored** — reverses output lines), Constructor, Required, ExtensionData, UnmappedMembers, ObjectCreation, Callbacks, ConverterResolution, MaxDepth, EnumConverters (+ src fix mirroring B2b‑p2: non-public setter assigned on read only with `[TomlInclude]` — `PropertyMetadata.CanSet`) | `6a2f01d8` | BVT 516 / Reg 660 |
| **T3** | TOML DOMs + exceptions: `TomlDocument`/`TomlElement` (malformed sweeps with line/column/offset, 10-kind value sweep, accessor×kind mismatch matrix, enumerators, MaxDepth boundary at 256, disposal), `TomlNode`/`Object`/`Array`/`Value` (collection surfaces, all-kind scalar conversions, DeepEquals/DeepClone, insertion-order serialization), exception ctor surfaces (+ src fix, twin of B3: `TomlObject`/`TomlArray` detach a removed/replaced child's `Parent`) | `3a9eb137` | BVT 699 / Reg 1037 |
| **RICH** | Parity features + tests, both libs: Queue/Stack/Concurrent collections → list/array (S.T.J stack-reversal semantics); non-string dictionary keys (integer family, enum, Guid, bool, char — Bencode sorts stringified keys, TOML preserves insertion order and quotes non-bare keys; supported-key dicts valid at TOML root); `ulong` → full unsigned range (Bencode only — TOML spec-bound to signed 64); `IncludeFields` + `[BencodeInclude]`/`[TomlInclude]` field serialization | `e92b8596` (Bencode), `1fafec8f` (TOML) | Bencode BVT 513 / Reg 730 · TOML BVT 733 / Reg 1071 |

Plus two retroactive fixes folded into the above: CA1062 `ThrowIfNull(kat)` guards on the B2a KAT methods, and three src fixes (see §4).

### Remaining passes (this plan)

| Pass | Scope | Est. new tests |
|---|---|---|

**All planned passes are complete.** A follow-on **GUARD** pass (comprehensive public-API parameter validation with `ParamName` assertions via `ExceptionAssert.ThrowsExactlyWithParamName`) was added at user request — see §0 once committed. The detailed specs for the already-completed B2b‑p2 (§5.1) and T2a (§5.3) are retained below as exemplars — the committed files (`BencodeSerializerTests.{Constructor,Required,…}.cs`, `TomlSerializerTests.{Values,…}.cs`) are the canonical patterns to copy for T2b.

---

## 1. Environment & workflow (read first — hard-won)

### Build / test — per-project csproj, NOT the solution
The container pins **.NET SDK 8** via `global.json`; `bodu.slnx` needs SDK 9+, so **always build/test the per-project `.csproj`**:

```bash
# Bencode
dotnet build Bodu.Text.Bencode/src/Bodu.Text.Bencode.csproj   -v q --nologo
dotnet test  Bodu.Text.Bencode/test/Bodu.Text.Bencode.Test.csproj --settings bvt.runsettings        -v q --nologo
dotnet test  Bodu.Text.Bencode/test/Bodu.Text.Bencode.Test.csproj --settings regression.runsettings -v q --nologo
# TOML
dotnet build Bodu.Text.Toml/src/Bodu.Text.Toml.csproj   -v q --nologo
dotnet test  Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings bvt.runsettings        -v q --nologo
dotnet test  Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings regression.runsettings -v q --nologo
```

`bvt.runsettings` = default tier (untagged tests). `regression.runsettings` = everything (incl. `[TestCategory("Regression")]`). `smoke.runsettings` = `[TestCategory("Smoke")]` only.

### ⚠️ Lesson 1 — verify warnings with a CLEAN rebuild
Incremental builds **cache away analyzer warnings**, so a normal `dotnet build` reporting "0 Warning(s)" is **misleading**. Always do the final warning check with `--no-incremental`, filtered to your files:

```bash
dotnet build Bodu.Text.Bencode/test/Bodu.Text.Bencode.Test.csproj -v q --nologo --no-incremental 2>&1 \
  | grep -E "Bodu\.Text\.Bencode/(src|test)/[^/]+\.cs.*: warning"   # MUST be empty
```

The repo has a **large pre-existing latent-warning baseline** in `Bodu.Core`, `Bodu.Test`, and `Bodu.CodeStyle` (hundreds of SA1600/SA1401/SA1407/IDE1006/CA1062 lines that only surface on a clean rebuild). **Ignore those** — only *your* `Bodu.Text.Bencode/` and `Bodu.Text.Toml/` files must be warning-free. Doc-comment warnings (CS1591, CS1574) are **hard build errors** everywhere.

### ⚠️ Lesson 2 — the `ThrowIfNull(kat)` KAT idiom (avoids CA1062)
Every `[DynamicData]` test method that takes a KAT record parameter and dereferences it **must** start its body with:

```csharp
ArgumentNullException.ThrowIfNull(kat);
```

This is the established repo idiom (see `Utf8BencodeReaderWriterTests.RoundTrip.cs`). Omitting it produces a CA1062 warning that *only shows on a clean rebuild* — exactly the trap that bit B2a.

### ⚠️ Lesson 3 — the container suspends during idle and kills background agents
Long background subagents get frozen/killed when the session goes idle (work-on-disk survives; in-memory work is lost). If you delegate to subagents: instruct them to **write each file to disk progressively** (don't hold everything to the end) and **keep the build green incrementally**, so any interruption leaves salvageable partial work. After a suspend, re-verify the tree (`git status`) and salvage/commit whatever landed before relaunching. Prefer **smaller, faster passes**. Doing the work in the foreground is the most reliable.

### Commit protocol
- Commit **per pass**, scoped to the one project you touched (`git add Bodu.Text.Bencode/` or `git add Bodu.Text.Toml/`), so the two libraries stay independent.
- Verify before each commit: clean-rebuild src+test (0 net-new warnings) **and** BVT green **and** Regression green.
- Commit message ends with a blank line then `https://claude.ai/code/session_01CxbTUMY4zzfaQZCCDviPn5` (or the new session's URL). **Never** put a model identifier in commit messages/PRs/code.
- Push: `git push -u origin claude/relaxed-rubin-m62s2x` (retry up to 4× with 2/4/8/16s backoff on network errors).

---

## 2. Test conventions (from CLAUDE.md — distilled)

- **MSTest** only (`[TestClass]`/`[TestMethod]`). Partial classes mirror the member/subject under test: `BencodeSerializerTests.<Subject>.cs`, `Utf8TomlReaderTests.<Subject>.cs`, etc. Make the root class `partial` if it isn't.
- **File banner** (exact) + **file-scoped namespace** (`namespace Bodu.Text.Bencode;` / `Bodu.Text.Toml;`). One concept per file.
- **Method naming:** `<MemberOrFeature>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>`.
- **XML `<summary>`** on every test method, starting **"Verifies that …"**. Private test models/enums are documented to the same standard (every member).
- **Exceptions:** `Assert.ThrowsExactly<TException>(() => { ... })` with a **block-bodied** lambda; assert the **exact** type; assert `ParamName`/message substring/`InnerException` where contractual.
- **Data:** `[DataRow]` for **primitive scalars only**; `[DynamicData]` + a strongly-typed KAT record (`Bodu.Test.Kat`: `ValidKat<,>`, `InvalidKat<>`, `BinaryKat<,>`, `RoundTripKat<,>`, etc.) for byte arrays / expected-exception / options / object graphs. Wire KAT display names with `DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName)` and give every row a human-readable `Name`. Start KAT methods with `ArgumentNullException.ThrowIfNull(kat);`.
- **Tiers:** default (untagged) = **BVT**. `[TestCategory("Regression")]` for exhaustive vector tables / wide sweeps that duplicate structural coverage. `[TestCategory("Smoke")]` sparingly — at most one per primary public type, and the serializers/readers/writers already have theirs, so **add no new Smoke** in these passes unless a primary type has none.
- **One observable outcome per test.** Split pass-rows and fail-rows into separate methods over filtered data sources; never branch on a row flag.
- Production-code parameter validation goes through `ThrowHelper.ThrowIf…`; user-facing strings come from the project `*ResourceStrings.resx` (not relevant for test code, but relevant if you fix src).

---

## 3. S.T.J → Bodu alignment map (the scenario universe)

Every applicable S.T.J test file, mapped to the Bodu pass that mirrors it. **In scope** unless listed under "Out of scope."

| S.T.J file(s) | Dimension | Bodu pass |
|---|---|---|
| `Utf8JsonReaderTests(.TryGet/.ValueTextEquals)`, `Utf8JsonWriterTests(.WriteRaw)`, `SpanTests`, `InvalidJsonTests` | reader/writer | B1✅ / T1✅ |
| `Value.Read/WriteTests`, `Read/WriteValueTests`, `Array.Read/WriteTests`, `Common/CollectionTests/*`, `Null.Read/WriteTests`, `NullableTests`, `Common/NumberHandlingTests` (int/overflow part only), `Common/UnsupportedTypesTests` | serializer values/collections | B2a✅ / **T2a** |
| `Common/PropertyVisibilityTests`, `PropertyNameTests`, `NamingPolicyUnitTests`, `PropertyOrderTests` | serializer member shaping | B2b‑p1✅ / **T2b** |
| `Common/ConstructorTests/*`, `RequiredKeywordTests`, `Common/ExtensionDataTests`, `UnmappedMemberHandlingTests`, `Common/JsonCreationHandlingTests.*`, `OnSerializeTests`, `EnumConverterTests`, `EnumTests`, `OptionsTests`/`CacheTests` | serializer features | **B2b‑p2** / **T2b** |
| `JsonDocumentTests`, `JsonElementTests`, `JsonElementWriteTests`, `Serialization/DomTests`, `JsonNode/{JsonNode,JsonObject,JsonArray,JsonValue,Parse,ToString,ParentPathRoot,JsonNodeOperator}Tests`, `NodeInteropTests`, `ExceptionTests`, `InvalidTypeTests` | DOMs + exceptions | **B3** / **T3** |

**Out of scope (no Bencode/TOML analogue — confirmed exclusions):** `PolymorphicTests*`, `UnionTests`, `StructuralJsonTypeClassifier` (polymorphism/unions); `ReferenceHandlerTests`, `CyclicTests` (`$id`/`$ref`); `JsonSchemaExporterTests`; `DynamicTests`; `AsyncEnumerableTests`, `Pipe*` (async-enumerable/pipes); `TypeInfoResolver*`, `*ApiValidation`, `*Wrapper.Reflection`, `MetadataTests/*` (source-gen/AOT metadata); `Utf8JsonReaderTests.MultiSegment` (multi-segment); and the **number-as-string** portion of `NumberHandlingTests` (`AllowReadingFromString`/`WriteAsString` — neither library has a number-handling option). Streaming (`Stream.Read/WriteTests`, `StreamTests`) folds into each serializer pass via the existing `*SerializeAsync`/`Stream` overloads.

---

## 4. Probed Bodu library contracts (the test oracle)

These are the **actual, verified behaviors** of the two libraries — discovered by probing in earlier passes. Use them as the expected values. **Where a contract is uncertain, write a throwaway probe `[TestMethod]` that logs actual output to `/tmp`, run it, read it, then delete the probe before finishing** — never invent a contract.

### 4.1 Bencode serializer — verified mapping
- **string** → byte string, length = **UTF‑8 byte count**. `"hello"`→`d5:Value5:helloe`; `""`→`d5:Value0:e`; `"héllo"` (é=2 bytes) → length **6**.
- **byte[]** → byte string carrying raw bytes; `[]`→`0:`. Binary `[0x00,0x01,0x7f,0x80,0xff]` round-trips losslessly.
- **integer family** (`sbyte`,`byte`,`short`,`ushort`,`int`,`uint`,`long`,`ulong`) → `i…e`. Examples (member `Value`): `sbyte.MinValue`→`d5:Valuei-128ee`; `byte.MaxValue`→`…i255ee`; `int.MinValue`→`…i-2147483648ee`; `uint.MaxValue`→`…i4294967295ee`; `long.MaxValue`→`…i9223372036854775807ee`.
- **`ulong` > `long.MaxValue`** → **`BencodeSerializationException`** on serialize (signed‑64 surface). Reading an integer literal beyond `Int64` → **`BencodeFormatException`** at the reader. Over-range read into a narrow type (e.g. `300`→`byte`, `-1`→`byte`) → **`BencodeSerializationException`**.
- **enum** → member-name byte string. `Color.Green`→`d5:Color5:Greene`; `[Flags] Read|Write`→`d11:Permissions11:Read, Writee` (comma+space, from `Enum.ToString`); undefined `(Color)99`→`d5:Color2:99e`. Round-trips via `Enum.TryParse`.
- **Unsupported scalars** (no built-in converter → **`NotSupportedException`** on serialize AND deserialize): `bool`, `double`, `float`, `decimal`, `char`, `Guid`, `Uri`, `DateTime`, `DateTimeOffset`, `TimeSpan`. (B2a added a deny-set so these surface a clean "No converter is configured for type 'X'" instead of an empty dict / depth error — see §4.3.)
- **Converter escape hatch:** registering a `BencodeConverter<bool>` (e.g. `i0e`/`i1e`) or `BencodeConverter<double>` (string form) lets that type round-trip. Resolution order: property `[BencodeConverter]` > type `[BencodeConverter]` > `options.Converters` > built-in.
- **Collections → Bencode list** (supported): `T[]`, `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `HashSet<T>`, `SortedSet<T>`, `Collection<T>`, `ObservableCollection<T>`, `LinkedList<T>`. The **four list-shaped interfaces deserialize to `List<T>`**; concrete types round-trip to their own type. `[3,1,2]`→`li3ei1ei2ee`; empty→`le`.
- **`Queue<T>`/`Stack<T>`** → NOT recognized as lists (don't implement `ICollection<T>`): **deserialize throws `BencodeSerializationException`**; serialize *incidentally* emits `d5:Counti3ee` (object-converter fallback — a wart). **`ConcurrentBag<T>`** serialize → `NotSupportedException` (hits `IsEmpty:bool`). → fixed in **RICH** pass.
- **null collection element** → **`BencodeSerializationException`** ("A null collection element cannot be written to Bencode").
- **Dictionaries → Bencode dict** (string key only): `Dictionary<string,V>`, `IDictionary<string,V>`, `IReadOnlyDictionary<string,V>`, `SortedDictionary<string,V>`. Interfaces deserialize to `Dictionary`; `SortedDictionary` stays. **Keys are canonically bytewise-sorted on write**: `{zebra:1,apple:2,mango:3}`→`d5:applei2e5:mangoi3e5:zebrai1ee`; empty→`de`.
- **Non-string-keyed dict** (`Dictionary<int,int>`) → NOT a Bencode dict; serializes as a **list of `{Key,Value}` dicts** `ld3:Keyi1e5:Valuei2eee`; deserialize throws `BencodeSerializationException`. → addressed in **RICH**.
- **dictionary null value** → **omitted**: `{a:null,b:"x"}`→`d1:b1:xe`.
- **`Nullable<T>`**: present delegates to the underlying converter; **null member omitted** (`de`); absent reads back `null`.
- **Member shaping** (B2b‑p1, committed): read/write round-trips; `init`-only round-trips; **get-only is written** (public getter) but **not assigned on read**; **private-setter is written** (public getter), and is **assigned on read only with `[BencodeInclude]`**; **public fields are never serialized** (property-only mapping), even with `[BencodeInclude]`. **`[BencodePropertyOrder]` has no effect on serialized output** — canonical key sorting overrides it (Bencode-specific; contrast TOML).
- **Object** → dict with **sorted wire-name** keys: `{Id=1,Label="a"}`→`d2:Idi1e5:Label1:ae`.

### 4.2 TOML serializer — known mapping (probe exact text in T2a)
- `string`/`char`/`Guid`/`Uri` → **string**; integer family (range-checked) → **integer**; `double`/`float` → **float**; `bool` → **boolean**; `DateTimeOffset`→**offset-date-time**, `DateTime`→**local-date-time** (probe Utc/Local/Unspecified handling), `DateOnly`→**local-date**, `TimeOnly`→**local-time**; `byte[]` → **integer array** by default or **Base64 string** via `TomlSerializerOptions.ByteArrayHandling` (`TomlByteArrayHandling`); enums → **member-name string**.
- `decimal` and `TimeSpan` have **no native TOML form** → **`NotSupportedException`** unless a `TomlConverter<T>` is registered.
- **Root must be a table.** A top-level scalar, array, or bare collection **throws on serialize** (probe the exact exception type — likely `TomlSerializationException`). Collections/dicts are valid only as **table members**; string-keyed dictionaries map to **tables**.
- **Output preserves document/member order** (no key sort) → **`[TomlPropertyOrder]` IS honored** — the key contrast with Bencode.

### 4.3 TOML reader/writer — verified contracts (from T1)
- **Canonical float spelling uses .NET `"R"`:** `1e10`→`10000000000.0` (expanded, gains `.0`), `1e100`→`1E+100`, `6.626e-34`→`6.626E-34` (uppercase `E`, explicit `+`), `-0.0` keeps its sign. `inf`/`-inf`/`nan` literal.
- **Multiline strings** (`'''`/`"""`): trim exactly one immediately-following leading newline; a line-ending backslash in `"""` collapses the newline + all following whitespace (`"a \<nl>    b"`→`"a b"`); CRLF inside normalizes to `\n`; up to two trailing quotes are kept as content.
- **Layout:** empty root table → `""`; empty array → inline `[]`; an empty **sub-table member** becomes a `[header]` block (each intermediate table emits its own header, e.g. `[server]` then `[server."data center"]`); an empty table **as an array element** renders inline `{}` (`a = [[{}]]`).
- **Super-table rules:** a super-table header may be defined **after** its sub-table and merge keys; re-opening an explicitly-headed table, `[a]` over an `a.b = 1` dotted table, or `[[a]]` over a static array are **rejected**.
- **Spec-version gating** (v1.0 reject / v1.1 accept), verified: `\xHH`, `\e` (→U+001B), optional time seconds, multi-line inline tables, inline-table trailing commas. Closing `Z` and `+00:00` both yield zero offset; lowercase `t`/`z` separators accepted; leap seconds (`:60`) rejected; BOM stripped.
- `TomlFormatException` carries `LineNumber`/`ColumnNumber`/`Offset` on every malformed path. `TomlTokenType` = `None, StartTable, EndTable, StartArray, EndArray, PropertyName, String, Integer, Float, Boolean, OffsetDateTime, LocalDateTime, LocalDate, LocalTime`.

### 4.4 Src fixes already committed this session (context for regressions)
- `ObjectConverterFactory` deny-set: `DateTime, DateTimeOffset, TimeSpan, DateOnly, TimeOnly, Guid, Uri, Version, Half` → routed to the documented `NotSupportedException` instead of being mapped to a lossy dictionary.
- `ObjectConverter<T>` keeps the instance **boxed** through member assignment so deserializing into a **value type** assigns each member to the same box (struct-reflection correctness).
- `BencodeSerializer.Serialize` propagates `options.MaxDepth` into the writer (previously always default 256).

---

## 5. Pass specs (scenario catalogs + data)

Each pass: read the **src** types and the **existing committed tests** first; extend, never duplicate. The Bodu test name catalogs below are authored to convention; the parenthetical `← STJ:File.Method` cites the S.T.J scenario the row mirrors.

### 5.1 B2b‑p2 — Bencode serializer features (part 2) — ✅ DONE (`80e24b22`)

> Completed and committed (BVT 310 / Reg 459). Retained as the exemplar for **T2b** — copy the committed `BencodeSerializerTests.{Constructor,Required,ExtensionData,UnmappedMembers,ObjectCreation,Callbacks,ConverterResolution,MaxDepth}.cs` patterns when mirroring these for TOML.

**Src:** `Bodu.Text.Bencode/src/Text.Bencode.Serialization/` — read each: `BencodeConstructorAttribute`, `BencodeRequiredAttribute`, `BencodeExtensionDataAttribute`, `BencodeUnmappedMemberHandling(+Attribute)`, `BencodeObjectCreationHandling(+Attribute)`, `IBencodeOnSerializing/Serialized/Deserializing/Deserialized`, `BencodeConverter(+Attribute/Factory)`, `BencodeStringEnumConverter`, `BencodeNumberEnumConverter`, `BencodeStringEnumMemberNameAttribute`, `BencodeSerializerOptions.MaxDepth`.
**Existing (don't dup):** `BencodeSerializerTests.{PropertyName,NamingPolicy,PropertyOrder,PropertyVisibility,Values,Nullables,Collections,Dictionaries}.cs`, `BencodeBinderAlignmentTests`, `BencodeSerializerAlignmentTests`, `BencodeEnumConverterTests`, `BencodeOptionsTests`.

**`BencodeSerializerTests.Constructor.cs`** ← STJ `Common/ConstructorTests/*`
- `Deserialize_WhenSinglePublicParameterizedCtor_ShouldBindArgsByName` ← SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported
- `Deserialize_WhenParameterlessAndParameterizedCtors_NoAttribute_ShouldUseParameterless` ← SinglePublicParameterizedCtor_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor
- `Deserialize_WhenMultipleParameterizedCtors_NoAttribute_ShouldThrowNotSupported` ← Class_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_NotSupported
- `Deserialize_WhenConstructorAttributeOnOneOfManyCtors_ShouldUseAttributedCtor` ← NoPublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported
- `Deserialize_WhenMultipleConstructorAttributes_ShouldThrowInvalidOperation` ← MultipleAttributes_NotSupported
- `Deserialize_WhenNonPublicCtorWithAttribute_ShouldUseIt` ← NonPublicCtors_WithJsonConstructorAttribute_WorksAsExpected
- `Deserialize_WhenPositionalRecord_ShouldBindByParameterName`
- `Deserialize_WhenStructWithoutAttribute_ShouldUseDefaultCtor` ← Struct_Use_DefaultCtor_ByDefault
- `Deserialize_WhenCtorParamMatchesPropertyName_CaseInsensitive_ShouldBind` ← ArgumentDeserialization_Honors_JsonPropertyName_CaseInsensitiveWorks
- `Deserialize_WhenCtorParamHonorsPropertyNameAttribute_ShouldBindToWireName` ← ArgumentDeserialization_Honors_JsonPropertyName
- `Deserialize_WhenCtorParamHonorsNamingPolicy_ShouldBind` ← ArgumentDeserialization_UseNamingPolicy_ToMatch
- `Deserialize_WhenCtorArgMissingFromDocument_ShouldUseClrDefault` ← DefaultFor{Reference,Value}TypeCtorParam
- `Deserialize_WhenExtraMembersBeyondCtorParams_ShouldBindRemainingToSettableProps` ← AsProperty_Of_ObjectWithParameterizedCtor
- `Deserialize_WhenCtorParamHonorsIgnore_ShouldSkip` ← ArgumentDeserialization_Honors_JsonIgnore
- `Deserialize_WhenCtorParamHasConverter_ShouldUseConverter` ← ArgumentDeserialization_Honors_ConverterOnProperty
- *(Data: build small POCOs/records with `[BencodeConstructor]`, parameterized ctors, mixed param/property casing. Bencode dicts are key-sorted, so author expected bytes accordingly.)*

**`BencodeSerializerTests.Required.cs`** ← STJ `RequiredKeywordTests`
- `Deserialize_WhenRequiredMemberPresent_ShouldRoundTrip`
- `Deserialize_WhenRequiredMemberAbsent_ShouldThrowBencodeSerializationException` (assert message names the missing member)
- `Deserialize_WhenCSharpRequiredKeywordMemberAbsent_ShouldThrow`
- `Deserialize_WhenRequiredMemberBoundViaConstructor_ShouldSucceed`
- `Serialize_WhenRequiredMember_ShouldWriteNormally`
- *(Probe whether `[BencodeRequired]` and the C# `required` keyword are both honored; assert the truth.)*

**`BencodeSerializerTests.ExtensionData.cs`** ← STJ `Common/ExtensionDataTests`
- `Deserialize_WhenUnmappedMembers_ShouldCaptureIntoExtensionData` ← ExtensionPropertyRoundTrip
- `Serialize_WhenExtensionDataPopulated_ShouldWriteBackEntries` ← ExtensionFieldRoundTrip
- `RoundTrip_WhenExtensionDataIsBencodeObject_ShouldPreserve` ← DeserializeIntoJsonObjectProperty
- `RoundTrip_WhenExtensionDataIsIDictionary_ShouldPreserve` ← DeserializeIntoGenericDictionary
- `Deserialize_WhenExtensionPropertyPreInstantiated_ShouldAppend` ← ExtensionPropertyAlreadyInstantiated
- `Serialize_WhenExtensionDataAndMappedKeyCollide_ShouldPreferMapped` ← EmptyPropertyName_WinsOver_ExtensionDataEmptyPropertyName (adapt)
- `Deserialize_WhenExtensionDataEmpty_ShouldLeaveTargetNullOrEmpty` ← ExtensionPropertyObjectValue_Empty
- `Serialize_WhenExtensionDataKeysUnsorted_ShouldEmitSortedWithMappedKeys` (Bencode canonical-sort interaction — **probe**)

**`BencodeSerializerTests.UnmappedMembers.cs`** ← STJ `UnmappedMemberHandlingTests`
- `Deserialize_WhenUnknownKeyAndHandlingSkip_ShouldIgnore`
- `Deserialize_WhenUnknownKeyAndHandlingDisallow_ShouldThrowBencodeSerializationException` (assert the offending key in message)
- `Deserialize_WhenUnknownKeyAndDefaultHandling_ShouldSkip` (confirm default)
- `Deserialize_WhenHandlingDisallowButExtensionDataPresent_ShouldCaptureNotThrow` (probe precedence)

**`BencodeSerializerTests.ObjectCreation.cs`** ← STJ `Common/JsonCreationHandlingTests.Object/Dictionary/Enumerable`
- `Deserialize_WhenPopulateOnWritableCollectionProperty_ShouldMergeIntoExisting` ← CreationHandlingSetWithAttribute_CanPopulate_Class
- `Deserialize_WhenPopulateOnReadOnlyProperty_ShouldFillExistingInstance` ← CanPopulateReadOnlyProperty_SimpleClass
- `Deserialize_WhenReplaceHandling_ShouldReplaceInstance` ← CreationHandlingEffectCancelled… (adapt)
- `Deserialize_WhenPopulateWithParameterizedCtor_ShouldThrowNotSupported` ← ClassWithParameterizedCtor_UsingPopulateConfiguration_ThrowsNotSupportedException
- `Deserialize_WhenPopulateOnDictionaryProperty_ShouldMergeKeys`
- *(Probe Replace-vs-Populate default and whether read-only props are populated.)*

**`BencodeSerializerTests.Callbacks.cs`** ← STJ `OnSerializeTests`
- `Serialize_WhenTypeImplementsOnSerializing_ShouldInvokeBeforeWrite` ← OnSerializing
- `Serialize_WhenTypeImplementsOnSerialized_ShouldInvokeAfterWrite` ← OnSerialized
- `Deserialize_WhenTypeImplementsOnDeserializing_ShouldInvokeBeforeBind` ← OnDeserializing
- `Deserialize_WhenTypeImplementsOnDeserialized_ShouldInvokeAfterBind` ← OnDeserialized
- `SerializeDeserialize_WhenAllFourCallbacks_ShouldFireInOrder` (assert ordering via a log list)
- `Serialize_WhenStructImplementsCallbacks_ShouldInvoke` ← Test_MyStruct
- `Serialize_WhenCollectionElementImplementsCallbacks_ShouldInvokePerElement` ← Test_MyCollection

**`BencodeSerializerTests.ConverterResolution.cs`**
- `Serialize_WhenPropertyConverterAndTypeConverter_ShouldPreferProperty`
- `Serialize_WhenTypeConverterAndOptionsConverter_ShouldPreferType`
- `Serialize_WhenOptionsConverterAndBuiltIn_ShouldPreferOptions`
- `Serialize_WhenNoCustomConverter_ShouldUseBuiltIn`
- `Deserialize_WhenConverterFactoryMatches_ShouldUseProducedConverter`

**`BencodeSerializerTests.MaxDepth.cs`** (src fix already propagates MaxDepth on serialize)
- `Serialize_WhenGraphExceedsMaxDepth_ShouldThrowBencodeSerializationException`
- `Deserialize_WhenDocumentExceedsMaxDepth_ShouldThrow`
- `Serialize_WhenGraphWithinMaxDepth_ShouldSucceed`
- `Serialize_WhenMaxDepthDefault_ShouldAllowReasonableNesting`
- *(Build a self-nesting POCO/list; assert it throws at the configured `options.MaxDepth`.)*

**Enum converters** — audit `BencodeEnumConverterTests`; fill gaps ← STJ `EnumConverterTests`:
- string-converter default (member name) vs `BencodeNumberEnumConverter` (decimal); `[BencodeStringEnumMemberName]` override; flags with member overrides (`EnumFlagsWithValidMemberNameOverrides`); undefined value → decimal; case-insensitive read; `JsonStringEnumConverter_InvalidType_Throws` analogue (non-enum → `ArgumentOutOfRangeException`).

**Optional:** add `Deserialize_WhenPrivateSetterWithoutInclude_ShouldNotAssign` to `.PropertyVisibility.cs` (B2b‑p1 deferred it — **probe** whether an un-included private setter is assigned on read, then assert).

---

### 5.2 B3 — Bencode DOMs + exceptions — ✅ DONE

> Completed and committed (BVT 468 / Reg 680). The committed `BencodeDocumentTests.{Parse,Elements,Enumeration,Disposal}.cs`, `Bencode{Object,Array,Value}Tests.cs`, `BencodeNodeTests.{Parse,Conversions,DeepEquals}.cs`, and `BencodeExceptionTests.cs` are the exemplars to mirror for **T3**. Src fixes folded in: `BencodeNode.Parse` rejects trailing bytes (per its documented single-root contract), and `BencodeObject`/`BencodeArray` detach a removed/replaced child's `Parent` (single-parent rule; mirrors `JsonNode`).

**Src:** `src/Text.Bencode.Document/` (`BencodeDocument`, `BencodeElement`, `BencodeProperty`, options) and `src/Text.Bencode.Nodes/` (`BencodeNode`, `BencodeObject`, `BencodeArray`, `BencodeValue`, node options). **Existing:** `BencodeDocumentTests` (17), `BencodeNodeTests` (21) — extend.

**Read-only DOM — `BencodeDocumentTests.*`** ← STJ `JsonDocumentTests`, `JsonElementTests`
- `Parse_WhenValidDocument_ShouldExposeRootElement`; `Parse_WhenTrailingBytes_ShouldThrow`; `Parse_WhenMalformed_ShouldThrowBencodeFormatException` (port B1 malformed vectors).
- `RootElement_WhenInteger/ByteString/List/Dictionary_ShouldReportValueKind` (`BencodeValueKind`).
- `GetInt64/GetString/GetRawBytes_When<kind>_ShouldReturnValue`; `GetX_WhenWrongKind_ShouldThrowInvalidOperationException` ← JsonElement_Get*EdgeCases.
- `EnumerateArray_ShouldYieldElementsInOrder`; `EnumerateObject_ShouldYieldPropertiesInKeyOrder`; `ArrayEnumeratorIndependentWalk`, `ObjectEnumeratorIndependentWalk` ← same names.
- `TryGetProperty_WhenPresent/Absent_ShouldReturnExpected`; `GetProperty_WhenAbsent_ShouldThrowKeyNotFound`; `GetPropertyFindsLast` analogue (Bencode rejects dup keys → assert rejection instead).
- `Indexer_WhenArrayIndexOutOfRange_ShouldThrow`; `MixedArrayIndexing`.
- `UseAfterDispose_ShouldThrowObjectDisposedException` ← CheckUseAfterDispose; `Dispose_Twice_ShouldNotThrow`.
- `Parse_WhenDepthExceedsOptions_ShouldThrow` ← HonorReaderOptionsMaxDepth / CheckParseDepth.
- `GetRawText`/raw-bytes round-trip ← GetRawText; `WriteTo`/`ToString` shape ← Json{Array,Object}ToString.
- `Clone`/element-independence-after-dispose if supported.

**Mutable DOM — `BencodeNodeTests.*`, `BencodeObjectTests.*`, `BencodeArrayTests.*`, `BencodeValueTests.*`** ← STJ `JsonNode/*`
- **Value:** `BencodeValue.Create` from int/string/byte[]; `GetValue<T>`; `GetValue_WhenWrongType_ShouldThrow`; `GetValueKind` ← JsonValue_CreateFrom*/GetValueKind.
- **Object:** indexer get/set/add/remove; `ContainsKey`; `Clear`; `Insert`/`IndexOf`/`GetAt`; `IDictionary`/`IEnumerable`/`ICollection` surfaces; `Add_DuplicateKey_ShouldThrow`; enumeration order = **canonical key order** (probe); change-during-enumeration fails ← ChangeCollectionWhileEnumeratingFails; `CopyTo`.
- **Array:** add/insert/remove/clear/contains/indexOf; `IList`/`IEnumerable`; index out of range; nested arrays; `GetValues` ← Contains_IndexOf_Remove_Insert, AddOverloads, CopyTo, GetValues_*.
- **Node:** `Parse`/`ToUtf8Bytes`/`ToString` round-trips ← Parse/ParseThenEdit/ReadPrimitives; `DeepEquals` ← DeepEquals_*; `DeepClone` ← DeepClone; `ReplaceWith`/`SetProperty`/`GetPath`/`GetPropertyName`/`GetElementIndex` ← same; `GetValueKind`; null handling ← NullHandling; convert `BencodeDocument`↔`BencodeNode` (FromElement / interop) ← FromElement, NodeInteropTests; `ToString_StringValuesNotQuoted` analogue.
- Canonical-output assertions on `ToUtf8Bytes()` (key-sorted).

**Exceptions** ← STJ `ExceptionTests`, `InvalidTypeTests`: `BencodeFormatException` (line/offset where tracked), `BencodeSerializationException` — type hierarchy, message content, `InnerException` where wrapping is contractual. Tag full malformed sweeps `[TestCategory("Regression")]`.

---

### 5.3 T2a — TOML serializer values (mirror B2a) — ✅ DONE (`5e297e68`)

> Completed and committed (BVT 400 / Reg 529). Retained as reference; the committed `TomlSerializerTests.{Values,Nullables,Collections,Dictionaries}.cs` are the TOML value-mapping exemplars.

**Src:** `src/Text.Toml.Serialization/`, `src/Text.Toml/` (`TomlSerializer`, `TomlSerializerOptions`, `TomlByteArrayHandling`, `TomlSpecVersion`). **Existing:** `TomlSerializerTests` (14), `TomlSerializerAlignmentTests` (30) — extend. **Make `TomlSerializerTests` partial.** Use the §4.2 contracts; **probe exact canonical text**.

**`TomlSerializerTests.Values.cs`** ← STJ `Value.*Tests`, `NumberHandlingTests`(int part), `UnsupportedTypesTests`
- per native scalar, serialize+round-trip with exact TOML text: `string` (+escapes), `char`, `Guid` (D-form), `Uri`, integer family (min/max/zero/typical; **overflow on read → exception**), `double`/`float` (fraction/exponent/`inf`/`-inf`/`nan`; **note `"R"` spelling** from §4.3), `bool` (`true`/`false`), the four date-time kinds (offset/local datetime/date/time, fractional seconds).
- `Serialize_WhenDecimalMember_ShouldThrowNotSupportedException`; `…TimeSpan…ShouldThrow`; plus converter-escape-hatch round-trip for each.
- `byte[]`: `Serialize_WhenByteArrayDefault_ShouldWriteIntegerArray`; `…WhenByteArrayHandlingBase64_ShouldWriteBase64String`.
- **Root shape:** `Serialize_WhenTopLevelScalar_ShouldThrow…`; `…WhenTopLevelArray_ShouldThrow…` (probe exact exception type — §4.2).

**`TomlSerializerTests.Nullables.cs`** ← STJ `NullableTests`, `Null.*Tests`: present delegates; null member omitted; absent reads null.

**`TomlSerializerTests.Collections.cs`** ← STJ `Array.*`, `Common/CollectionTests/*`: collection shapes **as table members** → TOML arrays; ordering/empty/nested/null-element; materialized concrete type per interface; **top-level collection throws** (root-must-be-table).

**`TomlSerializerTests.Dictionaries.cs`** ← STJ `Common/CollectionTests/CollectionTests.Dictionary`: string-keyed dict → **TOML table**; supported shapes + materialized types; nested; empty; non-string-key behavior (**probe + assert**). Note: TOML **preserves member order** (no key sort) — assert order is document order, not sorted.

---

### 5.4 T2b — TOML serializer features (mirror B2b‑p1 + B2b‑p2) — ✅ DONE (`6a2f01d8`)

> Completed and committed (BVT 516 / Reg 660). Probed contracts discovered during the pass (record alongside §4.2):
> - **Required missing** → `TomlSerializationException`: “Required member '<wireName>' was not present in the input for type '<T>'.” (wire name, not CLR name). C# `required`, `[TomlRequired]`, and no-default ctor params are all enforced.
> - **Duplicate key binding the same member** (incl. via case-insensitive match) → `TomlSerializationException` “appears more than once”. **Unmapped Disallow** message names both the key and the target type.
> - **Extension data**: supported shapes `TomlObject` / `IDictionary<string,TomlNode?>` / `Dictionary<string,TomlNode?>` only (others → `InvalidOperationException`, as is a second `[TomlExtensionData]` member); entries write back **after declared members in insertion order** (no sort — contrast Bencode); stays null when no overflow; capture takes precedence over Disallow.
> - **Populate**: options/type/member precedence identical to Bencode; merges dictionaries (header-table input), appends lists, populates get-only collections, falls back to Replace on a null seed.
> - **MaxDepth**: serialize over-depth → `TomlSerializationException` (“maximum write depth of {n} has been exceeded”); read over-depth → `TomlFormatException` but **only for inline value nesting** (see flagged bug 1). Setter: negative → `ArgumentOutOfRangeException`, 0 → resets to `DefaultMaxDepth` (64), after first use → `InvalidOperationException`.
> - **Enums**: default member-name string; integers accepted on read; case-insensitive name match; numeric strings parsed; undefined → decimal text; flags → “Read, Write”; `[TomlStringEnumMemberName]` beats naming policy; `TomlNumberEnumConverter` reading a string, or the string converter with `allowIntegerValues:false` reading an integer → `TomlSerializationException`.
> - **Ctor binding**: attributed > parameterless > greatest arity; param↔member match by CLR name, always case-insensitive; renamed/policy wire names flow to ctor args; absent optional param uses its default.
> - Empty nested object member emits `[Child]\n`; empty root table emits `“”`.
>
> **Flagged bugs, not fixed (candidates for a later pass):**
> 1. Reader/writer MaxDepth asymmetry: `[a.b.c]` header paths are not bounded by MaxDepth on read (only inline-table/array nesting is), while the writer bounds the same graph on serialize. Pinned as current behavior in `Deserialize_WhenHeaderTablePathExceedsMaxDepth_ShouldNotThrow`.
> 2. Extension-data key collision on write: an extension entry whose key equals a declared member's wire name is emitted as a duplicate key, producing invalid TOML (Bencode prefers the mapped member). Left untested.


Same dimension set as B2b (all of §5.1) **plus** the B2b‑p1 dimensions (PropertyName, NamingPolicy, PropertyVisibility, PropertyOrder), adapted to TOML. **Key contrasts to assert** (vs Bencode):
- **`[TomlPropertyOrder]` IS honored** (output is document-order, not key-sorted) — mirror `PropertyOrderTests.{BeforeDefaultOrder,AfterDefaultOrder,BeforeAndAfterDefaultOrder}` with real reordering effects.
- TOML has native `bool`/`float`/date-times, so those are **not** "unsupported" here (unlike Bencode) — the unsupported set is `decimal`/`TimeSpan`.
- Extension data, unmapped handling, populate, callbacks, required, constructor binding, converter resolution, enum converters (`TomlStringEnumConverter`/`TomlNumberEnumConverter`/`TomlStringEnumMemberNameAttribute`), MaxDepth — same scenario catalogs as §5.1, with TOML wire text. Files: `TomlSerializerTests.{PropertyName,NamingPolicy,PropertyVisibility,PropertyOrder,Constructor,Required,ExtensionData,UnmappedMembers,ObjectCreation,Callbacks,ConverterResolution,MaxDepth,EnumConverters}.cs`.

### 5.5 T3 — TOML DOMs + exceptions (mirror B3) — ✅ DONE (`3a9eb137`)

> Completed and committed (BVT 699 / Reg 1037). Probed contracts: empty/comment-only input parses to an empty root table in both DOMs; document `MaxDepth` counts inline-value nesting only (root table free; default boundary exactly 256 OK / 257 throws); `TomlElement.ToString` uses the "O" round-trip format for date-times and invariant `double.ToString` for floats (`Infinity`/`NaN`), while `TomlValue.ToString` uses "s" for local date-times; node serialization emits seconds-precision date-times with `Z` for zero offset and `inf`/`-inf`/`nan` literals; `\e` is v1.1-gated via `TomlDocumentOptions.SpecVersion`. Src fix folded in (twin of B3): `TomlObject`/`TomlArray` detach a removed/replaced child's `Parent`. The trailing-bytes twin bug does **not** apply — `Utf8TomlReader` parses the whole document eagerly and rejects trailing junk itself.


`TomlDocumentTests`/`TomlElementTests` (read-only) and `TomlNodeTests`/`TomlObjectTests`/`TomlArrayTests`/`TomlValueTests` (mutable) — same S.T.J `JsonDocument`/`JsonNode/*` scenario catalog as §5.2, with TOML's richer value model (native float/bool/the four date-times; `TomlValueKind`). **Existing:** `TomlDocumentTests` (19), `TomlNodeTests` (30) — extend. Assert **document-order** preservation (not key-sorted). Exceptions: `TomlFormatException` (line/column/offset), `TomlSerializationException`. Regression-tag malformed/grammar sweeps.

### 5.6 RICH — parity enhancements (implementation + tests)

These are **feature additions** that close S.T.J-parity gaps the test passes pinned as current behavior. Implement in src, then add tests; update the affected B2a/T2a tests that currently assert the un-enhanced behavior.
1. **Queue/Stack/Concurrent collections → list** (both libs). Extend the collection converter factory to recognize `Queue<T>`/`Stack<T>`/`ConcurrentQueue<T>`/`ConcurrentStack<T>`/`ConcurrentBag<T>` (add via `Enqueue`/`Push`/`Add`). Mind **Stack's reversing round-trip** (S.T.J reverses). Replace the current "Queue→`d5:Counti3ee`"/"throws" tests with real list round-trips. ← STJ `Common/CollectionTests/*`.
2. **Non-string dictionary keys** (both libs). Support `int`/`enum`/`Guid`/`DateTime`-keyed dictionaries by stringifying keys (Bencode dict / TOML table). Replace the current "list-of-KVP"/"throws" assertions. ← STJ `EnumDictionaryKey{Serialization,Deserialization}`, `CollectionTests.Dictionary`.
3. **`ulong` > `Int64`** (Bencode). Bencode integers are arbitrary-precision in the spec; widen the reader/writer integer surface (or special-case `ulong`) so `ulong.MaxValue` round-trips. Replace the current "throws" assertions.
4. **Field serialization** (both libs, optional). S.T.J supports `[JsonInclude]` on fields + `IncludeFields`. If pursuing parity, add field support gated by `[BencodeInclude]`/`[TomlInclude]` (+ an `IncludeFields` option) and replace the "fields never serialized" assertions.

---

## 6. Definition of done (per pass)

1. `--no-incremental` clean rebuild of **src and test** → **0 warnings** in the touched project's files (`Bodu.Text.Bencode/` or `Bodu.Text.Toml/`). Ignore the repo baseline elsewhere.
2. BVT green; Regression green (note before→after counts).
3. No scratch/probe files left on disk.
4. Commit scoped to the one project, message per §1; push to `claude/relaxed-rubin-m62s2x` with backoff retry.
5. Update this file's §0 "Committed passes" table with the new commit + counts as you go.

**Tracking after all passes:** the two libraries should each land ~300–500 executed tests; cross-check against §3 that every in-scope S.T.J dimension has a Bodu home, and that the §4 contracts are each asserted somewhere.
