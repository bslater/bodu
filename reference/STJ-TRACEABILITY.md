# S.T.J → Bodu Test-Suite Traceability Report

Audience: a maintainer deciding whether the System.Text.Json-parity test goal for
`Bodu.Text.Bencode` and `Bodu.Text.Toml` was met before merging this branch.

All S.T.J paths below are relative to `reference/system-text-json-tests/`; all Bodu paths
are relative to the repository root. Companion documents: `reference/REMAINING-TEST-PLAN.md`
(§0 pass history, §3 alignment map, §4 probed contracts, §5 scenario catalogs with
`← STJ:File.Method` citations) and `reference/system-text-json-tests/README.md` (corpus
provenance: `dotnet/runtime`, MIT).

---

## 1. Summary

**The goal.** Mirror the S.T.J test suite across both self-contained libraries at
*comprehensive per-dimension depth* — scenario-for-scenario against every S.T.J dimension
that has a Bencode/TOML analogue, **not** a literal line-of-code copy. Each library mirrors
S.T.J's architecture (ref-struct `Utf8*Reader`/`Utf8*Writer`, `*Serializer` POCO mapper,
mutable `*Node` DOM, read-only `*Document` DOM), so the S.T.J test taxonomy maps almost
file-for-file once JSON-specific value semantics are translated.

**The S.T.J corpus** (committed at `reference/system-text-json-tests/`, reference-only,
to be removed before any `master` merge):

| Measure | Value |
|---|---|
| `.cs` files | 303 (164 contain at least one test; the rest are test models, wrappers, generated source-gen output) |
| Total `[Fact]`/`[Theory]` methods | 3,793 |
| Largest in-scope files | `Common/JsonCreationHandlingTests.Enumerable.cs` (214), `System.Text.Json.Tests/Utf8JsonWriterTests.cs` (162), `System.Text.Json.Tests/JsonDocumentTests.cs` (124), `Common/PropertyVisibilityTests.cs` (106), `Common/ConstructorTests/ConstructorTests.ParameterMatching.cs` (102) |

A substantial fraction of the 3,793 is **out of scope by design** (polymorphism, `$ref`,
source-gen metadata, pipes, multi-segment readers, Newtonsoft-compat ports — see §3),
roughly 1,000+ methods.

**The Bodu suites, as of the GUARD pass** (final for this branch; see the note after this
report):

| Library | Test files | `[TestMethod]` declarations | Executed BVT | Executed Regression |
|---|---|---|---|---|
| `Bodu.Text.Bencode` (`Bodu.Text.Bencode/test/`) | 45 | 513+ | 542 | 759 |
| `Bodu.Text.Toml` (`Bodu.Text.Toml/test/`) | 54 | 651+ | 765 | 1,103 |

Executed counts exceed declaration counts because `[DataRow]`/`[DynamicData]` rows fan out
(190 `[DataRow]`s in Bencode, 207 in TOML, plus KAT-record `[DynamicData]` sweeps — e.g.
`Bodu.Text.Toml/test/Utf8TomlReaderTests.Malformed.cs` is one `[TestMethod]` driving a
large malformed-input vector table).

**Verdict shape.** Every in-scope dimension from the plan's §3 alignment map has a named
Bodu home in both libraries (matrix in §2). The known shortfalls are listed in §5 — most
notably the stream/async serializer overloads, which the plan said would "fold into each
serializer pass" but which no committed test exercises.

---

## 2. Dimension-by-dimension traceability

Counts in the S.T.J column are `[Fact]`/`[Theory]` declarations (≈); Bodu columns name the
mirroring files (declaration counts in parentheses). Representative method-level citations
follow each group.

### 2.1 Reader / writer

| S.T.J source (≈ tests) | What S.T.J validates | Bencode mirror | TOML mirror | Adaptation notes |
|---|---|---|---|---|
| `System.Text.Json.Tests/Utf8JsonReaderTests.cs` (90), `.TryGet.cs` (35), `.TryGet.Date.cs` (14), `.ValueTextEquals.cs` (15), `Serialization/InvalidJsonTests.cs` (2) | Token-by-token reading, typed value extraction, skip/trailing data, malformed-input rejection | `Utf8BencodeReaderTests.cs` (26), `.Skip.cs` (9), `.Malformed.cs` (6), `.UnsignedIntegers.cs` (8) | `Utf8TomlReaderTests.cs` (28), `.Strings.cs` (14), `.Integers.cs` (8), `.Floats.cs` (5), `.DateTimes.cs` (8), `.Keys.cs` (8), `.Tables.cs` (7), `.Arrays.cs` (6), `.InlineTables.cs` (8), `.Comments.cs` (6), `.SpecVersion.cs` (5), `.Malformed.cs` (1 method, vector sweep) | JSON's token set is replaced by each format's grammar: Bencode has only integer/byte-string/list/dict (length-prefixed strings, no escapes), so the TryGet matrix shrinks; TOML has a *richer* token set than JSON (`TomlTokenType` adds four date-time kinds), so the TryGet.Date analogue expands. Malformed-input errors report byte `Offset` in Bencode vs `LineNumber`/`ColumnNumber`/`Offset` in `TomlFormatException`. |
| `System.Text.Json.Tests/Utf8JsonWriterTests.cs` (162), `.Values.StringSegment.cs` (20), `.WriteRaw.cs` (7), `Serialization/SpanTests.cs` (8) | Writer state machine, value emission, formatting options | `Utf8BencodeWriterTests.cs` (23), `Utf8BencodeReaderWriterTests.RoundTrip.cs` (3) | `Utf8TomlWriterTests.cs` (11), `.Scalars.cs` (11), `.Layout.cs` (9), `.Keys.cs` (3), `.RoundTrip.cs` (7) | Bencode output is canonical by construction (no indentation/escaping options), collapsing S.T.J's large formatting matrix; the writer instead enforces key ordering and depth. TOML adds layout dimensions JSON lacks: `[header]` blocks vs inline tables, empty-sub-table rendering, multiline-string folding, canonical float spelling via .NET `"R"` (`1e100` → `1E+100`). |

Representative citations: Bencode malformed sweeps port the B1 vectors (leading zeros,
`i-0e`, unterminated tokens) where S.T.J uses `InvalidJsonTests`; TOML
`Utf8TomlReaderTests.SpecVersion.cs` (v1.0 reject / v1.1 accept for `\xHH`, `\e`,
optional seconds, multiline inline tables) has **no S.T.J analogue** — see §4.

### 2.2 Serializer — values, nullables, collections, dictionaries

| S.T.J source (≈ tests) | What S.T.J validates | Bencode mirror | TOML mirror | Adaptation notes |
|---|---|---|---|---|
| `Serialization/Value.ReadTests.cs` (25), `Value.WriteTests.cs` (8), `ReadValueTests.cs` (36), `WriteValueTests.cs` (13), `Common/NumberHandlingTests.cs` (78, int/overflow part only), `Common/UnsupportedTypesTests.cs` (7) | Per-scalar serialize/deserialize with exact wire text, numeric overflow, unsupported-type rejection | `BencodeSerializerTests.Values.cs` (19), `BencodeSerializerAlignmentTests.cs` (11), `BencodeBinderAlignmentTests.cs` (13) | `TomlSerializerTests.Values.cs` (41), `TomlSerializerAlignmentTests.cs` (26) | The unsupported-type set is format-driven: JSON's `null`/`bool`/float forms have no Bencode encoding, so `bool`, `double`, `float`, `decimal`, `char`, `Guid`, `Uri`, and all date-time types throw `NotSupportedException` in Bencode (with converter escape-hatch tests proving the extension point), whereas TOML natively supports bool/float/four date-time kinds and only `decimal`/`TimeSpan` are unsupported. Bencode strings are **UTF-8 byte-length prefixed** (`"héllo"` → length 6), tested explicitly. Integer overflow on read (`300` → `byte`) throws `BencodeSerializationException` / TOML equivalent, mirroring S.T.J's overflow rows. |
| `Serialization/Null.ReadTests.cs` (15), `Null.WriteTests.cs` (11), `Serialization/NullableTests.cs` (6) | `Nullable<T>` delegation, null roundtrips | `BencodeSerializerTests.Nullables.cs` (6) | `TomlSerializerTests.Nullables.cs` (8) | Neither format has a `null` literal, so S.T.J's "write null token" tests translate to **member omission**: a null member is omitted on write and an absent key reads back `null`. Null collection *elements* throw (`BencodeSerializationException` "A null collection element cannot be written") instead of round-tripping. |
| `Serialization/Array.ReadTests.cs` (51), `Array.WriteTests.cs` (25), `Common/CollectionTests/CollectionTests.Generic.{Read,Write}.cs` (76+71), `.Concurrent.cs` (4), `.ObjectModel.{Read,Write}.cs` (3) | Collection shape support, interface materialization, ordering, nesting | `BencodeSerializerTests.Collections.cs` (19) | `TomlSerializerTests.Collections.cs` (23) | Supported set pinned: `T[]`, `List<T>`, the four list-shaped interfaces (deserialize to `List<T>`), `HashSet<T>`/`SortedSet<T>`, `Collection<T>`/`ObservableCollection<T>`, `LinkedList<T>`, plus (RICH) `Queue<T>`/`Stack<T>`/`ConcurrentQueue<T>`/`ConcurrentStack<T>`/`ConcurrentBag<T>` with S.T.J's Stack-reversal semantics (`SerializeDeserialize_WhenStack_ShouldReverseElementOrder`). TOML adds a root-shape rule with no JSON analogue: **root must be a table**, so top-level collections throw (`Serialize_WhenRootIsQueue_ShouldThrowTomlSerializationException`) and collections are tested as table members. |
| `Common/CollectionTests/CollectionTests.Dictionary.cs` (53), `.Dictionary.KeyPolicy.cs` (21), `.Dictionary.NonStringKey.cs` (20) | Dictionary shapes, key policies, non-string keys | `BencodeSerializerTests.Dictionaries.cs` (21) | `TomlSerializerTests.Dictionaries.cs` (27) | The headline three-way ordering contrast, asserted on both sides: JSON preserves insertion order; **Bencode canonically bytewise-sorts keys on write** (`Serialize_WhenDictionaryKeysOutOfOrder_ShouldEmitSortedKeys`); **TOML preserves insertion order and quotes non-bare keys** (`Serialize_WhenDictionaryKeyNotBare_ShouldQuoteKey`). RICH added non-string keys (integer family incl. `ulong.MaxValue`, enum/flags, `Guid`, `bool`, `char`) mirroring `CollectionTests.Dictionary.NonStringKey`; string-keyed dictionaries map to TOML tables and are valid at the TOML root. |

### 2.3 Serializer — member shaping (visibility, name, order, naming policy)

| S.T.J source (≈ tests) | What S.T.J validates | Bencode mirror | TOML mirror | Adaptation notes |
|---|---|---|---|---|
| `Common/PropertyVisibilityTests.cs` (106), `.NonPublicAccessors.cs` (30), `.InitOnly.cs` (11) | Getter/setter accessibility, `init`, `[JsonInclude]`, fields | `BencodeSerializerTests.PropertyVisibility.cs` (7), `BencodeSerializerTests.Fields.cs` (11) | `TomlSerializerTests.PropertyVisibility.cs` (7), `TomlSerializerTests.Fields.cs` (12) | Same contract in both Bodu libs (a deliberate twin src fix, B2b-p2 + T2b): get-only is written but not assigned on read; a non-public setter is assigned on read **only with `[BencodeInclude]`/`[TomlInclude]`** (mirrors `Honor_JsonSerializablePropertyAttribute_OnProperties`). Field support (mirroring S.T.J `IncludeFields` + `[JsonInclude]` fields) landed in RICH. |
| `Common/PropertyNameTests.cs` (30), `Serialization/PropertyNameTests.cs` (10) | `[JsonPropertyName]`, case sensitivity, collisions | `BencodeSerializerTests.PropertyName.cs` (7) | `TomlSerializerTests.PropertyName.cs` (8) | Direct translation; TOML additionally rejects a duplicate key binding the same member (incl. via case-insensitive match) with "appears more than once". |
| `Serialization/NamingPolicyUnitTests.cs` (7), `System.Text.Json.Tests/JsonNamingPolicyTests.cs` (5) | camelCase/snake_case policy unit behavior, policy applied to wire names | `BencodeSerializerTests.NamingPolicy.cs` (8) | `TomlSerializerTests.NamingPolicy.cs` (8) | Direct translation, including policy flowing into constructor-argument matching (`ArgumentDeserialization_UseNamingPolicy_ToMatch` analogue). |
| `Serialization/PropertyOrderTests.cs` (3) | `[JsonPropertyOrder]` reorders output | `BencodeSerializerTests.PropertyOrder.cs` (3) | `TomlSerializerTests.PropertyOrder.cs` (6) | The sharpest divergence, asserted as such on each side: `[BencodePropertyOrder]` is a documented **no-op** (canonical key sorting always wins), while `[TomlPropertyOrder]` **is honored** and visibly reorders output lines (mirrors `PropertyOrderTests.{BeforeDefaultOrder,AfterDefaultOrder,BeforeAndAfterDefaultOrder}`). |

### 2.4 Serializer — features (constructors, required, extension data, unmapped, creation handling, callbacks, converters, enums, options)

| S.T.J source (≈ tests) | What S.T.J validates | Bencode mirror | TOML mirror | Adaptation notes |
|---|---|---|---|---|
| `Common/ConstructorTests/ConstructorTests.ParameterMatching.cs` (102), `.AttributePresence.cs` (16), `.Exceptions.cs` (16), `.Cache.cs` (5) | Parameterized-ctor binding, `[JsonConstructor]` selection, param↔property matching | `BencodeSerializerTests.Constructor.cs` (11) | `TomlSerializerTests.Constructor.cs` (11) | Scenario-for-scenario per plan §5.1: attributed > parameterless > single-parameterized selection (`SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported`), multiple attributes throw, case-insensitive name matching, wire-name/naming-policy flow into args, absent args use CLR defaults, positional records. Expected wire bytes account for Bencode key sorting. |
| `Common/RequiredKeywordTests.cs` (20) | C# `required` + `[JsonRequired]` enforcement | `BencodeSerializerTests.Required.cs` (10) | `TomlSerializerTests.Required.cs` (10) | Both libs enforce C# `required`, `[BencodeRequired]`/`[TomlRequired]`, and no-default ctor params; TOML's error names the **wire name**, pinned by test ("Required member '<wireName>' was not present…"). |
| `Common/ExtensionDataTests.cs` (60) | Overflow-member capture and write-back | `BencodeSerializerTests.ExtensionData.cs` (10) | `TomlSerializerTests.ExtensionData.cs` (10) | Mirrors `ExtensionPropertyRoundTrip`, `DeserializeIntoGenericDictionary`, pre-instantiated append, capture-beats-Disallow. Ordering diverges per format: Bencode merges extension keys into the **canonical sort** with mapped keys; TOML writes them **after declared members in insertion order**. TOML's mapped-key collision on write is a flagged bug (§5). |
| `Common/UnmappedMemberHandlingTests.cs` (8) | Skip vs Disallow for unknown members | `BencodeSerializerTests.UnmappedMembers.cs` (6) | `TomlSerializerTests.UnmappedMembers.cs` (6) | Direct translation; Disallow message names both the offending key and target type. |
| `Common/JsonCreationHandlingTests.{Object,Dictionary,Enumerable,Generic}.cs` (40+92+214+5) | Populate vs Replace on properties/collections/dictionaries | `BencodeSerializerTests.ObjectCreation.cs` (9) | `TomlSerializerTests.ObjectCreation.cs` (9) | S.T.J's 351 methods are heavily combinatorial over collection types; Bodu mirrors the *semantic* matrix (options/type/member precedence, dictionary merge, list append, get-only populate, null-seed Replace fallback, parameterized-ctor + Populate → `NotSupportedException`) at far lower row count by design. |
| `Serialization/OnSerializeTests.cs` (8) | The four (de)serialization callbacks | `BencodeSerializerTests.Callbacks.cs` (7) | `TomlSerializerTests.Callbacks.cs` (7) | Direct: interface-based `I*OnSerializing/Serialized/Deserializing/Deserialized`, firing order asserted via a log list, structs and per-element collection callbacks (`Test_MyStruct`, `Test_MyCollection`). |
| `Serialization/CustomConverterTests/*` (≈165 across 27 files) | Custom converter registration, factories, precedence | `BencodeSerializerTests.ConverterResolution.cs` (6) | `TomlSerializerTests.ConverterResolution.cs` (6) | Bodu pins the resolution chain (property attr > type attr > `options.Converters` > built-in) plus factory dispatch; the converter *escape-hatch* tests in `Values.cs` (custom `bool`/`double` converters in Bencode; `decimal`/`TimeSpan` in TOML) carry the rest of the weight, since unsupported-scalar coverage doubles as converter coverage. |
| `Serialization/EnumConverterTests.cs` (40), `EnumTests.cs` (8) | String/number enum converters, member-name overrides, flags | `BencodeEnumConverterTests.cs` (19) | `TomlSerializerTests.EnumConverters.cs` (20) | Member-name string default, number-converter alternative, `[*StringEnumMemberName]` overrides beating naming policy (`EnumFlagsWithValidMemberNameOverrides` analogue), undefined values → decimal text, case-insensitive read, non-enum type → `ArgumentOutOfRangeException`. TOML adds string-converter `allowIntegerValues:false` rejection. |
| `Serialization/OptionsTests.cs` (93), `CacheTests.cs` (7) | Options immutability after first use, defaults, MaxDepth | `BencodeOptionsTests.cs` (9), `BencodeSerializerTests.MaxDepth.cs` (7) | `TomlSerializerTests.MaxDepth.cs` (9) | MaxDepth contract pinned in full: setter rejects negatives, 0 resets to default, mutation after first use → `InvalidOperationException`; over-depth on write → `*SerializationException`, on read → `*FormatException`. A B2b-p2 src fix made `BencodeSerializer.Serialize` actually propagate `options.MaxDepth`. TOML has **no dedicated options test class** (coverage is spread across MaxDepth + alignment files) — noted in §5. The TOML read-side MaxDepth has a flagged asymmetry (§5). |

### 2.5 DOMs and exceptions

| S.T.J source (≈ tests) | What S.T.J validates | Bencode mirror | TOML mirror | Adaptation notes |
|---|---|---|---|---|
| `System.Text.Json.Tests/JsonDocumentTests.cs` (124), `JsonElementWriteTests.cs` (50), `JsonElementParseTests.cs` (17), `JsonElementCloneTests.cs` (9), `Serialization/JsonElementTests.cs` (11), `Serialization/JsonDocumentTests.cs` (8), `JsonPropertyTests.cs` (8), `Serialization/DomTests.cs` (61) | Read-only DOM: parse, kind reporting, typed accessors, enumerators, disposal, depth limits | `BencodeDocumentTests.cs` (17), `.Parse.cs` (9), `.Elements.cs` (19), `.Enumeration.cs` (10), `.Disposal.cs` (9) | `TomlDocumentTests.cs` (19), `.Parse.cs` (12), `.Elements.cs` (15), `.Enumeration.cs` (11), `.Disposal.cs` (10) | Accessor×kind mismatch matrices sized to each format's `*ValueKind` (4 Bencode kinds vs 10 TOML kinds), `ArrayEnumeratorIndependentWalk`/`ObjectEnumeratorIndependentWalk` analogues, `CheckUseAfterDispose` → `UseAfterDispose_ShouldThrowObjectDisposedException`, MaxDepth boundary pinned exactly (TOML: 256 OK / 257 throws; inline-value nesting only). S.T.J's `GetPropertyFindsLast` (duplicate keys) inverts: Bencode/TOML **reject** duplicate keys, so the mirror asserts rejection. TOML malformed sweeps assert `LineNumber`/`ColumnNumber`/`Offset`; Bencode asserts offsets. |
| `JsonNode/JsonObjectTests.cs` (79), `JsonArrayTests.cs` (52), `JsonValueTests.cs` (52), `JsonNodeTests.cs` (16), `ParseTests.cs` (12), `ParentPathRootTests.cs` (9), `JsonNodeOperatorTests.cs` (9), `ToStringTests.cs` (2), `Common/NodeInteropTests.cs` (2) | Mutable DOM: collection surfaces, parent/path, DeepEquals/DeepClone, interop, ToString | `BencodeNodeTests.cs` (21), `.Parse.cs` (8), `.Conversions.cs` (8), `.DeepEquals.cs` (8), `BencodeObjectTests.cs` (25), `BencodeArrayTests.cs` (22), `BencodeValueTests.cs` (16) | `TomlNodeTests.cs` (30), `.Parse.cs` (7), `.Conversions.cs` (10), `.DeepEquals.cs` (10), `TomlObjectTests.cs` (27), `TomlArrayTests.cs` (25), `TomlValueTests.cs` (19) | Full `IDictionary`/`IList`/`ICollection` surfaces, `ChangeCollectionWhileEnumeratingFails` analogue, single-parent rule (B3/T3 src fixes made remove/replace **detach the child's `Parent`**, mirroring `JsonNode`), `BencodeNode.Parse` now rejects trailing bytes per its documented contract (B3 src fix; TOML's reader already rejects trailing junk eagerly). Serialization order asserted per format: `ToUtf8Bytes()` key-sorted for Bencode, insertion-order for TOML nodes. |
| `Serialization/ExceptionTests.cs` (26), `InvalidTypeTests.cs` (6) | Exception types, messages, paths | `BencodeExceptionTests.cs` (9) | `TomlExceptionTests.cs` (11) | Constructor surfaces and hierarchy for `BencodeFormatException`/`BencodeSerializationException` and `TomlFormatException` (with `LineNumber`/`ColumnNumber`/`Offset`)/`TomlSerializationException`. S.T.J's JSON-path-in-message convention has no equivalent; positional info replaces it. Malformed/grammar sweeps are `[TestCategory("Regression")]`-tagged on both sides. |

### 2.6 Streams / async

| S.T.J source (≈ tests) | Bodu status |
|---|---|
| `Serialization/Stream.ReadTests.cs` (16), `Stream.WriteTests.cs` (18), `Stream.Collections.cs` (3), `Common/ConstructorTests/ConstructorTests.Stream.cs` (4) | **Mirrored at smoke depth by the GUARD pass** (`8b2cd90c`/`ea79f958`): `BencodeSerializerTests.Streams.cs` and `TomlSerializerTests.Streams.cs` cover `SerializeAsync(Stream)` canonical output, `Deserialize(Stream)`, and an async stream round-trip per library, and the `*SerializerTests.Guards.cs` files cover the null/non-readable/non-writable stream guard matrix with `ParamName` assertions. S.T.J's deep streaming dimensions (tiny-buffer segmentation, async enumerable flushing, partial reads) have no analogue because both Bodu serializers buffer the whole document; that depth is deliberately out of scope. |

---

## 3. Out-of-scope S.T.J areas (deliberate exclusions, from plan §3)

These were confirmed exclusions before any pass was written, because neither Bencode nor
TOML has the corresponding language/feature surface:

| Excluded S.T.J files | ≈ tests | Reason |
|---|---|---|
| `Serialization/PolymorphicTests*.cs`, `Common/UnionTests.cs`, `Common/StructuralJsonTypeClassifierTests.cs`, `Serialization/JsonPolymorphismOptionsTests.cs` | ≈300 | Polymorphism/unions: no `$type` discriminator concept in either format's serializer. |
| `Common/ReferenceHandlerTests/*`, `Serialization/CyclicTests.cs` | ≈156 | `$id`/`$ref` reference handling: no analogue; cyclic graphs surface as MaxDepth failures instead. |
| `Common/JsonSchemaExporterTests.cs` | 16 | Schema exporter: no Bencode/TOML schema facility. |
| `Serialization/DynamicTests` (Newtonsoft area) | — | `dynamic` support: not offered. |
| `Common/AsyncEnumerableTests.cs`, `Common/CollectionTests/CollectionTests.AsyncEnumerable.cs`, `Serialization/Pipe.{Read,Write}Tests.cs` | ≈83 | `IAsyncEnumerable<T>` / `PipeReader`/`PipeWriter` surfaces: not offered. |
| `Serialization/MetadataTests/*`, `Common/MetadataTests*.cs`, `Serialization/TypeInfoResolver*`, `*ApiValidation`, `*Wrapper.Reflection`, `SourceGenRegressionTests/*`, `TrimmingTests/*` | ≈250 | Source-gen / AOT contract-customization metadata (`JsonTypeInfo`/resolvers): the Bodu binders are reflection-only with no resolver seam. |
| `System.Text.Json.Tests/Utf8JsonReaderTests.MultiSegment.cs` | 71 | Multi-segment `ReadOnlySequence<byte>` readers: Bodu readers are single-span. |
| `Common/NumberHandlingTests.cs` — the number-as-string portion (`AllowReadingFromString`/`WriteAsString`) | (part of 78) | Neither library has a number-handling option; only the integer/overflow portion was in scope (mirrored in §2.2). |
| `NewtonsoftTests/*` | ≈63 | Newtonsoft.Json compatibility ports — JSON-ecosystem-specific. |
| Infra/auxiliary: `BitStackTests`, `DebuggerTests`, `JsonEncodedTextTests`, `JsonReaderStateAndOptionsTests`, `JsonWriterOptionsTests`, `ContinuationTests` | ≈51 | JSON-implementation internals (encoded-text cache, resumable continuation state) with no Bodu counterpart. |

---

## 4. Where Bodu goes beyond S.T.J

Behaviors tested on this branch with **no S.T.J analogue**:

- **Canonical Bencode grammar enforcement** (B1/B3): bytewise key-ordering on every write
  path (writer, serializer, node `ToUtf8Bytes()`), duplicate-key rejection on parse
  (where S.T.J tolerates duplicates — `GetPropertyFindsLast`), and leading-zero /
  negative-zero integer rejection (`i-0e`, `i03e`).
- **TOML spec-version gating** (T1): `TomlSpecVersion` v1.0-reject / v1.1-accept sweeps
  for `\xHH`, `\e`, optional time seconds, multiline inline tables, trailing commas —
  `Utf8TomlReaderTests.SpecVersion.cs`. JSON has no versioned spec dimension.
- **Binary byte-string round-trips** (B2a): `byte[]` carrying raw bytes losslessly
  (`[0x00,0x01,0x7f,0x80,0xff]`) — JSON must Base64; Bencode is binary-native. TOML's
  `TomlByteArrayHandling` (integer array vs Base64 string) is likewise Bodu-specific.
- **`ConcurrentBag<T>` deserialization** (RICH): S.T.J explicitly refuses
  `ConcurrentBag<T>` (`CollectionTests.Concurrent.cs` pins the `NotSupportedException`);
  both Bodu libs round-trip it (`SerializeDeserialize_WhenConcurrentBag_ShouldRoundTripToEquivalentElements`).
- **Unsigned-64 widening in Bencode** (RICH): Bencode integers are arbitrary-precision in
  spec, so the reader/writer/serializer gained a `GetUInt64`/`TryGetUInt64` surface and
  `ulong.MaxValue` round-trips (`Utf8BencodeReaderTests.UnsignedIntegers.cs`,
  `SerializeDeserialize_WhenUInt64KeyedDictionary_ShouldRoundTripMaxValue`). S.T.J treats
  large ulongs as ordinary JSON numbers — no equivalent boundary exists.
- **Documented-contract src fixes the passes forced** (each pinned by tests): the
  unsupported-scalar deny-set routing to a clean `NotSupportedException`; boxed value-type
  member assignment (struct-deserialization correctness); `MaxDepth` propagation into the
  Bencode write path; `[BencodeInclude]`/`[TomlInclude]` gating of non-public setters
  (twin fixes); `BencodeNode.Parse` trailing-byte rejection; `Parent` detachment on node
  remove/replace in all four mutable containers (twin fixes).

---

## 5. Known gaps / flagged items

1. **Stream/async serializer overloads — CLOSED by the GUARD pass** (`8b2cd90c`/`ea79f958`).
   When this matrix was first built, no test invoked `SerializeAsync`, `Deserialize(Stream)`,
   or `DeserializeAsync`. The GUARD pass added `*SerializerTests.Streams.cs` (happy-path
   canonical output and round-trips) and `*SerializerTests.Guards.cs` (null and
   non-readable/non-writable stream guards with `ParamName`) in both libraries — see §2.6.
2. **TOML MaxDepth read/write asymmetry (flagged bug 1, plan §5.4).** `[a.b.c]` header
   paths are not bounded by `MaxDepth` on read (only inline-table/array nesting is),
   while the writer bounds the same graph on serialize. Pinned as current behavior in
   `TomlSerializerTests.MaxDepth.cs` (`Deserialize_WhenHeaderTablePathExceedsMaxDepth_ShouldNotThrow`); not fixed.
3. **TOML extension-data key collision on write (flagged bug 2, plan §5.4).** An extension
   entry whose key equals a declared member's wire name is emitted as a duplicate key,
   producing invalid TOML (Bencode prefers the mapped member). Left **untested** as well
   as unfixed — it is the one knowingly unpinned behavior.
4. **DOM-level `ulong` stays signed-64 (Bencode).** The RICH widening covered the reader,
   writer, and serializer, but `BencodeValue` still stores its integer as `long`
   (`Bodu.Text.Bencode/src/Text.Bencode.Nodes/BencodeValue.cs` — `GetValue<ulong>` is a
   `checked((ulong)_integer)` cast), so a document containing an integer above
   `long.MaxValue` is representable on the wire but not in the node DOM.
5. **Unsupported collection sub-families are neither mirrored nor pinned.** Within the
   in-scope `Common/CollectionTests/*` row, four sub-families correspond to converters the
   Bodu libraries simply do not implement: `CollectionTests.Immutable.{Read,Write}.cs`
   (≈85 tests — `System.Collections.Immutable`), `.NonGeneric.{Read,Write}.cs` (≈63 —
   `ArrayList`/`Hashtable`/`IList`), `.KeyValuePair.cs` (23 — `KeyValuePair<,>` as a
   value), and `.Memory.cs` (15 — `Memory<T>`/`ReadOnlyMemory<T>`). Unlike the unsupported
   scalars (which have explicit `NotSupportedException` deny-set tests) and unsupported
   dictionary *keys* (pinned via `Serialize_WhenDictionaryKeyIsUnsupportedType_ShouldEmitListOfKeyValuePairs`),
   these shapes have no test pinning their failure mode. Low risk, but a reviewer should
   know the cut was implicit rather than asserted.
6. **No dedicated TOML options test class.** Bencode has `BencodeOptionsTests.cs`
   (reader/writer/document/node options, 9 methods); TOML's equivalent coverage is spread
   across `TomlSerializerTests.MaxDepth.cs`, `TomlNodeTests.cs`
   (`TomlNodeOptions_WhenCaseInsensitive…`), and the alignment tests, with no single
   options-focused file mirroring `Serialization/OptionsTests.cs`'s immutability/defaults
   sweep.
7. **GUARD pass pending.** Comprehensive public-API parameter validation
   (`ParamName`-asserting guards via `ExceptionAssert.ThrowsExactlyWithParamName`) was
   added to the plan at user request and is not yet committed; all counts above are as of
   the RICH pass (`e92b8596` Bencode, `1fafec8f` TOML).

---

## Appendix A — Pass history (plan §0, condensed)

| Pass | Commit | Scope | Result (BVT / Reg) |
|---|---|---|---|
| B1 | `8e62a33f` | Bencode reader/writer | — |
| B2a | `2705b2b3` | Bencode serializer values/collections/dictionaries/nullables | 210 / 344 |
| T1 | `5a4536b9` | TOML reader/writer | 264 / 392 |
| B2b-p1 | `1f9c1e91` | Bencode member shaping | 233 / 382 |
| B2b-p2 | `80e24b22` | Bencode serializer features (+ `[BencodeInclude]` setter fix) | 310 / 459 |
| T2a | `5e297e68` | TOML serializer values/collections/dictionaries/nullables | 400 / 529 |
| B3 | `23d8390e` | Bencode DOMs + exceptions (+ trailing-bytes & Parent-detach fixes) | 468 / 680 |
| T2b | `6a2f01d8` | TOML serializer features (+ `[TomlInclude]` setter fix) | 516 / 660 |
| T3 | `3a9eb137` | TOML DOMs + exceptions (+ Parent-detach fix) | 699 / 1037 |
| RICH | `e92b8596` + `1fafec8f` | Parity features: queue-like collections, non-string dict keys, ulong (Bencode), fields | 513 / 730 · 733 / 1071 |
| GUARD | `8b2cd90c` + `ea79f958` | Public-API parameter guards with ParamName assertions; stream-overload coverage; element-indexer ParamName src fix | 542 / 759 · 765 / 1103 |

## Appendix B — Representative method-level mappings

Spot-checkable examples (3–5 per dimension); the exhaustive per-scenario citations live in
plan §5's catalogs as `← STJ:File.Method` annotations.

| Dimension | S.T.J method (file) | Bodu mirror (file) |
|---|---|---|
| Constructors | `SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported` (`Common/ConstructorTests/ConstructorTests.AttributePresence.cs`) | `Deserialize_WhenSinglePublicParameterizedCtor_ShouldBindArgsByName` (`Bodu.Text.Bencode/test/BencodeSerializerTests.Constructor.cs`; TOML twin in `TomlSerializerTests.Constructor.cs`) |
| Constructors | `ArgumentDeserialization_UseNamingPolicy_ToMatch` (`…ConstructorTests.ParameterMatching.cs`) | `Deserialize_WhenCtorParamHonorsNamingPolicy_ShouldBind` (both `*SerializerTests.Constructor.cs`) |
| Extension data | `ExtensionPropertyRoundTrip` (`Common/ExtensionDataTests.cs`) | `Deserialize_WhenUnmappedMembers_ShouldCaptureIntoExtensionData` (both `*SerializerTests.ExtensionData.cs`) |
| Visibility | `Honor_JsonSerializablePropertyAttribute_OnProperties` (`Common/PropertyVisibilityTests.NonPublicAccessors.cs`) | private-setter-assigned-only-with-`[*Include]` tests (both `*SerializerTests.PropertyVisibility.cs`) |
| Enum converters | `EnumFlagsWithValidMemberNameOverrides` (`System.Text.Json.Tests/Serialization/EnumConverterTests.cs`) | flags-with-member-override tests (`BencodeEnumConverterTests.cs`, `TomlSerializerTests.EnumConverters.cs`) |
| Document DOM | `CheckUseAfterDispose` (`System.Text.Json.Tests/JsonDocumentTests.cs`) | `UseAfterDispose_ShouldThrowObjectDisposedException` (`BencodeDocumentTests.Disposal.cs`, `TomlDocumentTests.Disposal.cs`) |
| Document DOM | `GetPropertyFindsLast` (`JsonDocumentTests.cs`) | duplicate-key **rejection** tests — inverted contract (`BencodeDocumentTests.Parse.cs`, `TomlDocumentTests.Parse.cs`) |
| Node DOM | `ChangeCollectionWhileEnumeratingFails` (`System.Text.Json.Tests/JsonNode/JsonObjectTests.cs`) | change-during-enumeration tests (`BencodeObjectTests.cs`, `TomlObjectTests.cs`) |
| Collections (RICH) | Stack reversal semantics (`Common/CollectionTests/CollectionTests.Generic.{Read,Write}.cs`) | `SerializeDeserialize_WhenStack_ShouldReverseElementOrder` (`BencodeSerializerTests.Collections.cs`); `…WhenStackMember_…` (`TomlSerializerTests.Collections.cs`) |
| Dictionaries (RICH) | `CollectionTests.Dictionary.NonStringKey.cs` enum-key round-trips | `SerializeDeserialize_WhenEnumKeyedDictionary_ShouldUseMemberNames` (both `*SerializerTests.Dictionaries.cs`) |
| Property order | `PropertyOrderTests` reordering rows (`System.Text.Json.Tests/Serialization/PropertyOrderTests.cs`) | honored: `TomlSerializerTests.PropertyOrder.cs`; documented no-op: `BencodeSerializerTests.PropertyOrder.cs` |
