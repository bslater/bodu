# CLAUDE.md

Guidance for AI assistants working in this repository. Read this file before making changes.

## Repository Overview

**Bodu** is a multi-project C# utility library solution focused on high-performance, well-documented, framework-style building blocks. The solution lives at `bodu.slnx` (Visual Studio's modern solution format — note the `.slnx` extension, not `.sln`). The projects are organised by domain (below). Where several projects share an identical structure and naming — notably the regional calendar **data bundles** (`Bodu.Globalization.Calendar.Data.<Region>`) — they are shown as a single `<…>` wildcard row rather than enumerated individually:

| Project | Path | Responsibility |
|---|---|---|
| `Bodu.Core` | `Bodu.Core/` | Buffers, generic collections (circular buffer, deque, evicting dictionary), extensions, text-encoding utilities (`Bodu.Text` namespace: `EncodingDetection`, `EncodingExtensions`, `StringEncodingExtensions`), XML, argument validation helpers (`ThrowHelper`), `WeekPattern`. |
| `Bodu.Test` | `Bodu.Test/` | Shared test infrastructure: KAT records (`Bodu.Test.Kat`), assertion helpers (`Bodu.Test.Assertions.ExceptionAssert`), reusable stream mocks (`Bodu.Test.IO`), test category constants (`TestCategories`). Referenced by other test projects. |
| `Bodu.Numerics` | `Bodu.Numerics/` | `Fraction<T>` (rational arithmetic, parse/format, generic math, UTF-8). |
| `Bodu.IO.Hashing` | `Bodu.IO.Hashing/` | Non-cryptographic hashing (Fletcher-16/32/64, full RevEng CRC catalogue, check-digit algorithms: Luhn, Damm, ABA, EAN, GTIN, IBAN, ISBN, ISIN, LEI, ISO 7064). |
| `Bodu.Security.Cryptography` | `Bodu.Security.Cryptography/` | Block ciphers (Threefish 256/512/1024, Skipjack, Blowfish, Twofish, Camellia), AEAD (Ascon), keyed/cryptographic hashes (Skein, BLAKE2, Tiger, SipHash, FNV1a, Adler), crypto transforms, helpers. |
| `Bodu.Text.Encoding` | `Bodu.Text.Encoding/` | Binary encodings: Base16, Base32, Base58, Base64, Base64Url, Base85 (with variants, formatting options, span/UTF-8 surfaces). |
| `Bodu.Text.Configuration` | `Bodu.Text.Configuration/` | Bodu text configuration parser/resolver (INI-compatible profile, resolver precedence, typed view getters, write options). |
| `Bodu.Text.Formats` | `Bodu.Text.Formats/` | Document formats: Delimited (RFC 4180 CSV/TSV), DotEnv, INI. |
| `Bodu.Text.Bencode` | `Bodu.Text.Bencode/` | Self-contained Bencode (BEP 3) library shaped after the `System.Text.Json` BCL: ref-struct `Utf8BencodeReader`/`Utf8BencodeWriter` (+ `BencodeTokenType`/`BencodeValueKind`), the `BencodeSerializer` POCO mapper (converters, the full attribute family, options, naming policies, callbacks, enum converters), a mutable `JsonNode`-style DOM (`BencodeNode`/`BencodeObject`/`BencodeArray`/`BencodeValue`), and a read-only `JsonDocument`-style DOM (`BencodeDocument`/`BencodeElement`). Folders/namespaces follow the S.T.J source layout: `Bodu.Text.Bencode[.Reader/.Writer/.Document/.Nodes/.Serialization]`. |
| `Bodu.Text.Toml` | `Bodu.Text.Toml/` | Self-contained TOML (v1.0.0 / v1.1.0) library, the same `System.Text.Json`-aligned shape and `Text.Toml.*` folder/namespace structure as `Bodu.Text.Bencode`: `Utf8TomlReader`/`Utf8TomlWriter`, the `TomlSerializer` POCO mapper, and the mutable (`TomlNode`) and read-only (`TomlDocument`) DOMs. TOML's richer value model adds native float, boolean, and the four RFC 3339 date-time kinds, plus `TomlSpecVersion` and `TomlByteArrayHandling`. |
| `Bodu.Extensions.Configuration.Text` | `Bodu.Extensions.Configuration.Text/` | Bridge between `Microsoft.Extensions.Configuration` and `Bodu.Text.Configuration`. |
| `Bodu.Globalization.Calendar` | `Bodu.Globalization.Calendar/` | Resource-driven notable-date engine on the notable-date schema: rule model, date-calculation strategies and astronomical algorithms (`Algorithms`), range resolution (`RangeResolution`), observed-date adjustments, working-day extensions (`Bodu.Extensions`), and `NotableDateService`. |
| `Bodu.Globalization.Calendar.Plugins` | `Bodu.Globalization.Calendar.Plugins/` | Trust-gated external plugin loading for assemblies contributing custom `INotableDateAlgorithm` implementations. |
| `Bodu.Globalization.Calendar.Builder` | `Bodu.Globalization.Calendar.Builder/` | Fluent authoring API (`NotableDateDocumentBuilder`) that constructs notable-date documents on the notable-date schema, with full XML and JSON-subset serialization (`ToXml`/`ToJson`/`Save`) and parsing (`FromXml`/`FromJson`/`Load`); materializes a `NotableDateResource` via the loader. |
| `Bodu.Globalization.Calendar.<Region>` | `…Calendar.Data.<Region>/` | Per-region calendar data bundles — one self-contained embedded pack per country (national rules plus ISO 3166-2 subdivisions), each exposing a `<Region>CalendarData` factory and importing the shared catalogues through a `<region>-common` hub. `<Region>` ∈ `Americas` (US, CA, MX, BR, AR, CL, CO, PE), `AsiaPacific` (AU, CN, IN, JP, KR, MY, NZ, SG, ID, TH, PH, VN, HK, TW), `Europe` (28 EU/EEA territories incl. GB, FR, DE), `MiddleEast` (AE, SA, IL, TR, QA, JO), `Africa` (ZA, NG, KE, GH, ET, EG, MA). |
| `Bodu.Globalization.Calendar.DependencyInjection` | `…Calendar.DependencyInjection/` | `IServiceCollection` extensions for registering calendar services. |
| `Bodu.Financial` | `Bodu.Financial/` | Money and currency primitives: `Money` / `Money<TCurrency>`, the ISO 4217 `CurrencyCode` catalogue and per-currency `ICurrency` types, exchange rates (`ExchangeRate` / `ExchangeRate<TBase, TQuote>`, rate series and providers), allocation and rounding policies, and JSON converters. |
| `Bodu.Financial.DependencyInjection` | `Bodu.Financial.DependencyInjection/` | `IServiceCollection` extensions for registering financial services (`IFinancialServiceBuilder`, named monetary contexts, `FinancialOptions`). |
| `BouncyCastle.Crypto` | `bc-csharp/crypto/src/` | Third-party vendor reference (used by Cryptography tests for reference vectors). |
| `docs` | `docs/` | DocFX documentation project. |

A separate solution **`Bodu.CodeStyle/Bodu.CodeStyle.sln`** holds the Bodu code-style analyzers, code fixes, and XML-doc formatter (`Bodu.CodeStyle.XmlDocumentation.{Analyzers,CodeFixes,Core}` plus `Bodu.CodeStyle.Test.Common`). It is **not** referenced by `bodu.slnx` — treat it as an independent unit.

Each project has the layout:

```
<Project>/
  src/   # production code, grouped by namespace folder
  test/  # MSTest project mirroring src structure (Bodu.Test has only test/)
```

### Target Frameworks

All projects target `net8.0`.

Nullable reference types are enabled everywhere. `ImplicitUsings` is enabled across all projects, including `Bodu.Core`. Test projects have `ImplicitUsings` enabled and pre-import MSTest via `<Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />`. `Bodu.Core/test/Bodu.Core.Test.csproj` additionally pre-imports `Bodu.Test.Assertions.ExceptionAssert` statically so the shared `AssertGuard(...)` call resolves unqualified across all `ThrowHelperTests.*.cs` partial files.

## Key Types

- **Bodu.Core**: `CircularBuffer<T>`, `ConcurrentCircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey, TValue>`, `IndexedSet<T>`, `IndexedPriorityQueue<TElement, TPriority>`, `ConcurrentHashSet<T>`, `PooledBufferBuilder`, `WeekPattern`, `ThrowHelper`; text-encoding utilities in the `Bodu.Text` namespace (`EncodingDetection`, `EncodingExtensions`, `StringEncodingExtensions`).
- **Bodu.Numerics**: `Fraction<T>`.
- **Bodu.IO.Hashing**: `Fletcher16` / `Fletcher32` / `Fletcher64`, `Crc`, `CrcStandard`(s), `CrcLookupTableCache`, `BlockNonCryptographicHashAlgorithm<T>`, `IResumableHashAlgorithm`, check-digit algorithms (`LuhnCheckDigitAlgorithm`, `IbanCheckDigitAlgorithm`, etc.).
- **Bodu.Security.Cryptography**: `Threefish256` / `Threefish512` / `Threefish1024`, `Skipjack`, `Blowfish`, `Twofish`, `CamelliaEcb`, `Ascon128`, `Skein256/512/1024`, `Blake2b`, `Blake3`, `Tiger`, `SipHash`, `FNV1a`, `Adler32`, `CryptoHelpers`, AEAD modes (EAX, OFB, GCM), block-cipher modes.
- **Bodu.Text.Encoding**: `Base16`, `Base32`, `Base58`, `Base64`, `Base85`, `BaseFormattingOptions`, `BaseFormatStyles`, variant enums.
- **Bodu.Text.Configuration**: `ConfigurationDocument`, `ConfigurationParseOptions`, `ConfigurationWriteOptions`, `ConfigurationProfile`, view getters.
- **Bodu.Text.Formats**: `Bencode` / `BencodedValue`, `Delimited` / `DelimitedParseOptions`, `DotEnv`, `Ini` / `IniDocument` / `IniParseOptions`.
- **Bodu.Globalization.Calendar**: `NotableDateService` / `INotableDateService`, `NotableDateResource` / `NotableDateResourceLoader`, `NotableDateRule`, `NotableDate`, `NotableDateDefinition`, `NotableDateFilter`, `TerritoryCode`; namespace `Bodu.Globalization.Calendar.Algorithms` (`INotableDateAlgorithm`, `EasterCalculator`, `HinduLunarCalculator`, `LunarPhaseCalculator`, `SolarTermCalculator`, and the `IDateCalculationStrategy` implementations); namespace `Bodu.Globalization.Calendar.RangeResolution` (`ResolutionPolicy`, collision/duplicate policies); working-day/date extensions in `Bodu.Extensions`.
- **Bodu.Globalization.Calendar.Data.<Region>** (data bundles): the `<Region>CalendarData` static factory (`SupportedCountries`, `LoadResource(territory)`, `CreateService(territory)`) over the embedded per-country packs; `CommonNotableDateResources` resolves the shared catalogues (`global-core`, `christian-western`, `christian-orthodox`, `catholic`, `global-islamic` / `global-islamic-umm-al-qura`, `global-jewish`, `global-buddhist`, `global-hindu`, `global-persian`, …) that the region hubs import.
- **Bodu.Test** (test infrastructure): `IKat`, generic KAT records (`ValidKat<TInput,TExpected>`, `InvalidKat<TInput>`, `RoundTripKat<TValue,TWire>`, `BinaryKat<TInput,TExpected>`, `ExceptionKat<TInput>`, `GuardValidKat<T>`, `GuardInvalidKat<T>`, `EnumerableKat<TInput,TExpected>`, `BinaryEncodingKat`, `InvalidEncodedTextKat`), `KatDisplayName` helper, `ExceptionAssert` (with `ThrowsExactlyWithParamName<T>` and `AssertGuard`), MSTest tier constants in `TestCategories`, reusable stream mocks under `Bodu.Test.IO`.

## Build & Tooling

- Shared MSBuild configuration lives in `bld/Bodu.props` (Authors, MIT licence, deterministic builds, package metadata, doc-comment warnings as errors — e.g. CS1591).
- `.editorconfig` lives under `Bodu.Core/src/.editorconfig` and drives formatter settings.
- Analyzers in use: **StyleCop.Analyzers**, **Roslynator.Analyzers**, **Microsoft.CodeAnalysis.NetAnalyzers**, **AsyncFixer**, **VisualStudio.Threading.Analyzers**. Treat analyzer warnings as actionable — fix rather than suppress unless there is a strong reason.
- Licence header template: `Bodu.sln.licenseheader` (carries `company="Bodu Pty. Ltd."`, matching `stylecop.json:companyName` — preserve the banner exactly as used in existing files).
- `.filenesting.json` nests partial-class files: any `<Base>.<Part>.cs` file nests under `<Base>.cs`. Keep partial splits consistent with this pattern.
- CI: `.github/workflows/docfx-build-publish.yml` builds DocFX documentation on pushes to `master` and publishes to GitHub Pages.

### Common Commands

```bash
dotnet build bodu.slnx
dotnet test  bodu.slnx --settings bvt.runsettings              # default build run (BVT)
dotnet test  bodu.slnx --settings smoke.runsettings            # smoke only
dotnet test  bodu.slnx --settings regression.runsettings       # full regression
dotnet test  Bodu.Core/test/Bodu.Core.Test.csproj --settings bvt.runsettings
```

See **Test Tiers** below for the category convention each runsettings file applies.

`test.runsettings` enables parallel execution (`MaxCpuCount=0`) and disables AppDomains.

### SDK Bootstrap (Claude Code on the web)

`.claude/hooks/session-start.sh` installs `dotnet-sdk-10.0` from `apt` on session start when running in the remote Claude Code on the web environment (`CLAUDE_CODE_REMOTE=true`). It is idempotent — when a .NET 10 SDK is already installed it exits immediately, so resume / clear / compact sessions pay no extra cost.

The hook also repairs the `dotnet-dnceng` plugin that `.claude/settings.json` enables from the `dotnet/arcade-skills` marketplace (`extraKnownMarketplaces` / `enabledPlugins`): the upstream plugin manifest currently fails Claude Code's path validation (its `agents` entry lacks the required `./` prefix), so the hook patches the cached marketplace clone and installs the plugin. Its skills then load from the next session in the container. The repair is a no-op once the manifest is fixed upstream and can be removed at that point.

Local developer machines are untouched (the hook short-circuits when `CLAUDE_CODE_REMOTE` is unset), and the hook is registered via `.claude/settings.json`. Note for local use: until the upstream manifest fix lands, the plugin install triggered by the project settings may fail validation on a local machine; sessions still work, just without the plugin's skills.

## Branching and Commits

- **One branch per session, by default.** Use the branch the harness designates at session start (typically `claude/<topic>-<id>`) and make multiple commits to it as the session progresses. Do not spin up additional branches for each edit, fix, or intermediate step within the same session.
- **Commit incrementally.** Prefer a fresh commit per logical step over batching unrelated changes into one large commit. The branch should accumulate work across the session, not be replaced.
- **Exceptions that justify additional branches:**
  - Resolving conflicts on multiple existing PR branches — each PR has its own remote head that must be checked out and pushed back to.
  - Work that must land on a specific pre-existing branch other than the session branch.
  In these cases, use disposable local branches and delete them once the work is pushed.
- **Push** to the session branch when changes are ready; do not push to `master` directly.

## Test Conventions

- Framework: **MSTest** (`Microsoft.VisualStudio.TestTools.UnitTesting`, `[TestClass]` / `[TestMethod]`). Do **not** introduce xUnit or NUnit.
- Tests live in `<Project>/test/` and are organised as **partial classes** that mirror the source layout — e.g. `CircularBuffer.cs` → `CircularBufferTests.Enqueue.cs`, `CircularBufferTests.Dequeue.cs`. Extend the existing partial class when adding tests for an existing type.
- No shared test base classes; each test is self-contained.

### Test File Organisation

Default to grouping tests by the member under test. For a type `Foo`, use partial files named after the public method, property, constructor group, operator, or interface surface being validated.

Examples:

```text
FooTests.cs
FooTests.Ctors.cs
FooTests.Count.cs
FooTests.Add.cs
FooTests.Remove.cs
FooTests.IEnumerable.cs
FooTests.IReadOnlyCollection.cs
```

Use member-based files for the majority of tests because they make it easy to locate coverage for a specific API. Put tests for a method or property in that member's file when the scenario is primarily about that member's contract, including normal behaviour, boundary cases, exception behaviour, and simple state transitions.

Use subject-based partial files for cross-cutting behavioural contracts that span multiple members or would otherwise be duplicated across many member files. These files should still be specific, narrow, and named for the semantic contract being validated.

Common subject-based groups:

| Subject | Suggested file name | Use when |
|---|---|---|
| Null handling | `FooTests.Nulls.cs` | The type intentionally accepts, stores, rejects, or preserves `null` keys, values, elements, delegates, or options across multiple APIs. |
| Value-type behaviour | `FooTests.ValueTypes.cs` or `FooTests.Structs.cs` | The type must preserve value equality, default values, struct keys, struct values, or generic value-type behaviour across multiple APIs. |
| Reference-type behaviour | `FooTests.ReferenceTypes.cs` | The type must preserve reference identity, mutable reference values, aliasing semantics, or reference-equality expectations across multiple APIs. |
| Interface contracts | `FooTests.IEnumerable.cs`, `FooTests.ICollection.cs`, `FooTests.IReadOnlyCollection.cs` | The type has explicit or implicit interface members, or behaviour differs when accessed through the interface. |
| Enumeration/versioning | `FooTests.Enumeration.cs` | The type has iterator invalidation, reset/current semantics, fail-fast behaviour, or multiple enumeration shapes. |
| Comparer/equality semantics | `FooTests.Comparer.cs` or `FooTests.Equality.cs` | A comparer or equality contract affects multiple lookup, add, remove, or containment APIs. |
| Serialization/debugger contracts | `FooTests.Serialization.cs`, `FooTests.DebugView.cs` | The tests validate framework integration rather than a single public method. |

For collection types, add subject-based files when the collection has explicit semantic support for `null`, structs, reference types, custom comparers, enumeration invalidation, or interface access. For example, a collection that permits `null` values should have a focused `CollectionTests.Nulls.cs` file that validates `null` values through add, lookup, enumeration, removal, and containment APIs. If `null` keys are rejected, validate that rejection consistently in either the relevant member files or a focused `Nulls` file when the rule applies across many members.

Avoid creating broad catch-all files such as `FooTests.EdgeCases.cs`, `FooTests.Misc.cs`, or `FooTests.Behaviour.cs`. Prefer either the member name or a precise subject name.

When a scenario could fit both a member file and a subject file, choose the file based on the primary purpose of the test:

- If the test exists to validate a specific method/property contract, put it in the member file.
- If the test exists to validate a type-wide semantic contract across multiple APIs, put it in the subject file.
- If the test validates an explicit interface implementation, put it in the interface file even when the underlying behaviour overlaps with a concrete member.

Keep each partial file cohesive. Do not move a test into a subject-based file merely because it uses a struct, `null`, or a reference type incidentally; use subject files only when that type characteristic is the behaviour being validated.

### Test Tiers (Smoke / BVT / Regression / Stress)

The suite is partitioned into tiers via `[TestCategory(...)]` so the build can run a fast subset by default and the exhaustive set on demand. Tier names are also exposed as constants on `Bodu.Test.TestCategories` for projects that reference `Bodu.Test`; either the constant or the literal string works.

| Tier | Tag | Purpose |
|---|---|---|
| **Smoke** | `[TestCategory("Smoke")]` | One happy-path test per primary public type. Catches catastrophic breakage. |
| **BVT** *(default)* | *(no category)* | Structural, exception, property, and contract tests. |
| **Regression** | `[TestCategory("Regression")]` | Exhaustive vector tables, full algorithm catalogues, large parameter sweeps, multi-decade calendar tables. Excluded from BVT. |
| **Stress** | `[TestCategory("Stress")]` | Long-running, high-iteration loops. Excluded from BVT. |

Run-settings files at the repository root drive each tier:

```bash
dotnet test bodu.slnx --settings smoke.runsettings        # Smoke only
dotnet test bodu.slnx --settings bvt.runsettings          # BVT (default build run)
dotnet test bodu.slnx --settings regression.runsettings   # Everything
dotnet test bodu.slnx --settings test.runsettings         # Everything (legacy alias)
```

Conventions:

- Default a new test to **BVT** by leaving `TestCategory` unset.
- Mark a test **Regression** when it is data-driven over a published vector table, an exhaustive catalogue, or a wide parameter sweep that duplicates structural coverage.
- Mark a test **Smoke** sparingly — one per primary type, exercising the most important public method on a happy-path input.
- Pre-existing `[TestCategory("Stress")]` tags retain their semantics.

### Test Method Naming

Convention: `<MethodOrProperty>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>`

- `When<Condition>` — the input or state under test.
- `_For<TypedCondition>` — optional qualifier for a type/overload variant.
- `Should<ExpectedResult>` — the observable outcome.

Examples:

```csharp
Enqueue_WhenFull_ShouldThrowInvalidOperationException()
Parse_WhenInputIsEmpty_ForNullableInt_ShouldReturnNull()
Capacity_WhenSetToZero_ShouldThrowArgumentOutOfRangeException()
```

### Test Method Documentation

Every test method has an XML `<summary>` starting with **"Verifies that ..."**, describing scenario and expected outcome in 1–2 sentences so intent is clear without reading the body.

```csharp
/// <summary>
/// Verifies that enqueueing an item into a full buffer throws
/// <see cref="InvalidOperationException" />.
/// </summary>
[TestMethod]
public void Enqueue_WhenFull_ShouldThrowInvalidOperationException() { ... }
```

### Test Exception Handling

When validating exceptions, always capture them using `Assert.ThrowsExactly<TException>` with the action enclosed in a statement block.

Rules:

- Always use **`Assert.ThrowsExactly<TException>`** for exception assertions.
- Always assert the **specific expected exception type**. Do not use broader base exception types unless that is the exact expected contract.
- Always write the invocation being tested inside a block-bodied lambda:

```csharp
var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
{
    _ = new TestCityHash(hashSize);
});
```

- When applicable, validate the inner exception:
-- Assert that an inner exception exists when one is expected.
-- Assert its exact type.
-- Validate its message and other relevant properties where required by the contract.

```csharp
var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
{
    sut.Execute();
});

Assert.IsNotNull(ex.InnerException);
Assert.IsInstanceOfType<ArgumentException>(ex.InnerException);
Assert.IsTrue(ex.InnerException.Message.Contains("Invalid state", StringComparison.Ordinal));
```

- Guidance:
-- Validate only the exception details that form part of the public contract.
-- For argument exceptions, prefer asserting:
--- exact exception type
--- ParamName
--- relevant message content where useful
-- For wrapped exceptions, also validate the InnerException chain where that wrapping is intentional and contractually significant.
-- Keep exception assertions explicit and local to the test; do not hide them behind helper methods unless already established in the test suite.

### Test Consolidation Patterns (KATs and Binary Tests)

`Bodu.Test/test/Test/` hosts only the **cross-project test infrastructure**: assertions, stream mocks, the `IKat` marker, the `KatDisplayName` helper, the generic KAT primitives whose shape is consumed by more than one test project, and the one contract base shared across multiple test projects. Domain-shaped KATs and contract bases live alongside their consumer in the domain test project that owns them.

**Stays in `Bodu.Test`:**

- **`Bodu.Test.Kat`** namespace — generic KAT (known-answer test) primitives consumed by multiple test projects (or by `ExceptionAssert.AssertGuard` itself): `IKat` (marker interface exposing `Name`), `ValidKat<TInput,TExpected>`, `InvalidKat<TInput>`, `BinaryKat<TInput,TExpected>`, `GuardValidKat<T>`, `GuardInvalidKat<T>`. Plus `KatDisplayName.GetDisplayName(MethodInfo, object?[])` for `[DynamicData(... DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]` wiring so failures show the row's `Name` instead of an opaque index.
- **`Bodu.Test.Contracts`** namespace — the one multi-consumer contract test base: `ParseFormatContractTests<T>` (used by Bodu.Core.Test + Bodu.Numerics.Test).
- **`Bodu.Test.Assertions.ExceptionAssert`** — `ThrowsExactlyWithParamName<TException>(action, expectedParamName)` and the `AssertGuard(testName, act, expectedExceptionType, expectedParamName)` matrix helper, plus KAT-aware overloads `AssertGuard<T>(GuardValidKat<T>, Action<T,T,string?>)` and `AssertGuard<T>(GuardInvalidKat<T>, Action<T,T,string?>)`.
- **`Bodu.Test.IO`** namespace — stream mocks (`FaultingStream`, `ThrottledIncrementingByteStream`, `NonSeekableStream`, etc.).
- **`Bodu.Test.TestCategories`** — tier constants (Smoke / Regression / Stress) consumed by every domain test project.

**Lives alongside the consumer (per domain test project)**, each in a `Contracts/` or similar folder under the test project root. Contract bases and KAT records share the local `<area>.Contracts` namespace so subclasses pick them up without an extra `using`:

- **Bodu.Core.Test** (namespaces `Bodu.Collections.Generic.Contracts` / `Bodu.Buffers` / `Bodu.Collections.Generic.Extensions`): `CollectionContractTests<>`, `ReadOnlyCollectionContractTests<>`, `SetContractTests<>`, `EnumeratorContractTests<>`, `DebugViewContractTests<>`, `NonGenericCollectionContractTests<>` (with `SyncRootSupported` opt-out for concurrent collections); plus the domain-shaped KATs `BufferCapacityKat`, `BufferWriteKat`, `EnumerableKat<,>`, `WeekPatternParseKat`, `InvalidWeekPatternParseKat`.
- **Bodu.Security.Cryptography.Test** (namespace `Bodu.Security.Cryptography.Infrastructure`): `SymmetricStreamAlgorithmTests<TTest, TAlgorithm>` — abstract base inherited by every stream cipher (ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit, Hc128) covering key/nonce sizing, lifecycle, transform reuse, overlap rules, and disposal. Other test surfaces inherit directly from the local abstract bases: block ciphers extend `BlockCipherTests<TTest, TCipher, TVariant>`, block-cipher transforms extend `BlockCipherTransformTests<TTest, TCryptoTransform>`, and AEAD / hash families use their own per-family bases — no parallel contract layer is duplicated. The retained KAT records are `HashExtensionKat` plus the domain-local `AeadKnownAnswerVector`, `BlockCipherKnownAnswer`, `HashAlgorithmKnownAnswers` + `HashAlgorithmKnownAnswer`, and `KeyedHashAlgorithmKnownAnswer`.
- **Bodu.IO.Hashing.Test** (namespace `Bodu.IO.Hashing.Contracts`): `NonCryptographicHashAlgorithmContractTests<TAlgorithm>`, `CheckDigitContractTests<TAlgorithm>`, `MultiCharCheckDigitContractTests<TAlgorithm>`; plus the KATs `HashKat`, `HashStreamingKat`, `CrcCatalogKat`, `CheckDigitKat`; and the pre-existing domain-local KATs `NonCryptographicHashKnownAnswer`, `CheckDigitKnownAnswer`, `MultiCharCheckDigitKnownAnswer`, `MultiCharCheckDigitIsValidKnownAnswer`.
- **Bodu.Text.Encoding.Test** (namespace `Bodu.Text.Encoding.Contracts`): `BinaryEncodingContractTests<TEncoding>`; plus the KATs `BinaryEncodingKat`, `InvalidEncodedTextKat`; and the pre-existing domain-local KAT `EncodingKnownAnswerVector`.
- **Bodu.Text.Formats.Test** (namespace `Bodu.Text.Formats.Contracts`): `BinaryDocumentFormatContractTests<TDocument,TOptions>`, `TextDocumentFormatContractTests<TDocument,TOptions>`, `StreamRoundTripContractTests<T>`; plus the KATs `BinaryDocumentKat<,>`, `InvalidBinaryDocumentKat<>`, `TextDocumentKat<,>`, `InvalidTextDocumentKat<>`; and the pre-existing domain-local KATs `DelimitedKnownAnswerVector`, `DotEnvKnownAnswerVector`, `IniKnownAnswerVector`.
- **Bodu.Text.Bencode.Test** + **Bodu.Text.Toml.Test**: each self-contained library is tested end to end — the ref-struct `Utf8*Reader`/`Utf8*Writer` token surface (canonical round-trips and malformed-input rejection), the `*Serializer` POCO-mapping suite, the `System.Text.Json`-aligned feature surface (serialization callbacks, unmapped-member handling, object-creation/Populate, naming policies, the string/number enum converters, and the full attribute family), and both DOMs (the mutable `*Node` tree and the read-only `*Document`). Full-grammar and malformed-input sweeps are tagged `[TestCategory("Regression")]`. Tests are colocated per library and assert exact canonical bytes/text where applicable; the two libraries are independent and self-contained, so no shared serialization contract base is promoted.
- **Bodu.Text.Configuration.Test**: the bespoke `ConfigurationKatRunnerTests` framework + `ConfigurationKnownAnswerData`.
- **Bodu.Globalization.Calendar.Test** (plus the `.Data.*`, `.DependencyInjection`, `.Plugins`, and `.Builder` test projects): the calendar suite uses self-contained `*KnownAnswerTests` classes (e.g. `EasterKnownAnswerTests`, `IslamicCalendarKnownAnswerTests`, `StrategyResolution*KnownAnswerTests`) that drive known-answer vectors directly, with shared XML inputs embedded under `test/Globalization.Calendar/Fixtures/`. The `.Builder` test project validates the fluent authoring API end-to-end by building documents, serializing to XML/JSON, and asserting against the real `NotableDateResourceLoader` / `NotableDateService`. Each `.Data.<Region>` bundle test project follows the `<Region>CalendarDataTests` pattern — `[DataRow]` known-answer vectors that pin every floating or computed holiday (Easter offsets, nth-weekday rules, lunar/Hijri/Hebrew festivals, weekend-substitution shifts) to confirmed published dates, with exact assertions where the date is deterministic and a ±2-day tolerance for moon-sighting/astronomical festivals, plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test. It does not currently promote reusable contract bases or KAT-record types; add them under a local `Contracts/` folder if a second consumer in a different calendar test project emerges.

When adding a new contract base or KAT record, default to colocating it with its sole consumer — only promote it to `Bodu.Test` once a second consumer in a different test project exists.

Conventions:

- **`[DataRow]` is for primitive scalars only** (int, bool, string, enum). Use `[DynamicData]` with a strongly typed KAT record for byte arrays, expected exception types, options objects, parser state, or object graphs.
- **Binary tests** — one `[TestMethod]` asserts one observable outcome. Do not write methods like `_ShouldEitherReturnExpectedOrThrow` that branch on a row flag. Split into separate methods over filtered data sources: typically `_ShouldNotThrowAndReportNothing` (pass rows) and `_ShouldThrowOn<Param>` or `_ShouldThrowExpected` (fail rows).
- Each `[DynamicData]` row should carry a human-readable name (a `Name` field on the KAT record, or the first `testName` parameter on a `[DataRow]`) so failures surface the scenario rather than a row index.
- Keep KAT-record `Name` synthesis sensible: when multiple fields disambiguate the row (e.g. `{Algorithm} {Year} {CalendarKind}`), implement `IKat.Name` explicitly to compose them.

## Source File Conventions

### File Header

Every `.cs` file begins with the standard banner — preserve the separator lines and the `file=` / `company=` attributes exactly:

```csharp
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
```

### Namespace Style

- Use **file-scoped** namespaces — terminate the namespace declaration with `;` and do **not** wrap the file contents in `{ }`. This applies to every project in the solution; no exceptions.

  ```csharp
  namespace Bodu.Collections.Generic;

  public sealed class CircularBuffer<T> { ... }
  ```

- `Bodu.Core`, `Bodu.IO.Hashing`, and `Bodu.Globalization.Calendar` already follow this convention throughout.
- `Bodu.Security.Cryptography` contains legacy block-scoped nested namespace files. Do not mix styles within a file, but when a file's primary type is being edited for other reasons, convert it to the file-scoped `;` form at the same time.

### File Layout

- **One public type per file.** Every `.cs` file declares exactly one top-level type. Nested / child types must live in separate partial-class files nested under the parent file per `.filenesting.json` (see Build & Tooling).
- Partial-file naming is `<Base>.<Part>.cs` where `<Base>.cs` holds the root declaration. Examples:
  - `CircularBuffer.cs` ← root
  - `CircularBuffer.Enumerator.cs`, `CircularBuffer.Debug.cs` ← partials/child-type splits
  - `CrcStandard.cs` ← root; `CrcStandard.Catalog.cs` ← auto-generated catalogue partial
- Don't stack unrelated helper types into the same file. If a type only makes sense alongside its parent (private nested enum, internal helper record), split it into a partial file under the parent rather than co-locating it in the root.

### Naming

- Private instance fields: `_camelCase`.
- Private static fields: `s_camelCase`.
- Explicit types preferred over `var`.
- No primary constructors on documented public types (they conflict with `<param>` XML documentation).
- Expression-bodied members for methods, properties, and accessors with a small implementation footprint — see **Expression-Bodied Members** below for the required layout.
- Public argument validation goes through the `ThrowHelper.ThrowIf…` members (in `Bodu.Core`) — see **Parameter Validation** below.

## C# Code Style Guidelines

### File and Header Formatting

- Include the copyright header in the standard format with separator banner lines.
- Preserve consistent spacing and alignment within the header.
- Follow the established file presentation style for partial classes and related files.

### XML Documentation

**All documentation must be in US English.**

**All documentation must align to BCL standards.**

**Documentation scope**
- Provide complete XML documentation for **every** member of a declared type — `public`, `protected`, `internal`, **and** `private`. Private members are documented to the same standard as public members.
- The only exception is `<remarks>`: it is optional on private members and should be added only when the private implementation genuinely warrants it (for example, a subtle concurrency protocol, a lock-free state transition, or a non-obvious invariant that aids future maintainers).

**`<summary>`**
- Write a concise, professional summary describing the purpose, intent, or responsibility of the type or member.
- Keep the tone factual and API-consumer focused.
- Do not mechanically repeat the member name.
- Prefer strong verb-led phrasing: *Provides…*, *Gets…*, *Initializes…*, *Attempts to…*, *Returns…*, *Removes…*, *Adds…*.

**`<param>`**
- Add a `<param>` for every parameter.
- Keep descriptions concise — ideally a single line.
- Describe the parameter in the context of the member's behaviour.
- Use `Must not be <see langword="null" />.` style wording only for basic nullability expectations where useful.
- Do not document validation rules, permitted ranges, allowed values, formats, or exceptional conditions in `<param>` text.
- Put validation constraints, boundary rules, permitted values, and failure behaviour in `<remarks>` and/or `<exception>` documentation instead.
- Avoid repeating information that is already expressed by the parameter name unless it improves clarity.
- Prefer neutral descriptions such as “The number of transformation rounds.” over imperative descriptions such as “Specify the number of transformation rounds.”
- For optional parameters, describe their behavioural role rather than restating the default value unless the default has semantic meaning.

**`<returns>`**
- Add `<returns>` for every non-void member.
- Describe the result in the context of the method's purpose, not merely the raw type.
- **Properties must always include a `<returns>` element** describing what the property yields on read, in addition to any `<value>` element that clarifies semantics.

**`<exception>`**
- Document all exceptions the member can throw, including `ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`, and `InvalidOperationException`.
- Describe the exact condition that causes each exception using the established style:
  - `<paramref name="capacity" /> ≤ 0.`
  - `The buffer is empty.`
  - `Thrown if <paramref name="owner" /> is <see langword="null" />.`

**`<remarks>`**
- Add `<remarks>` when it materially helps the consumer understand concurrency behaviour, snapshot semantics, ordering guarantees, side effects, edge cases, stability caveats, performance trade-offs, or design intent.
- Use `<para>` blocks within remarks where appropriate to maintain visual structure.

**`<example>`**
- Add examples when they improve usability or remove ambiguity.
- Keep examples minimal, realistic, and consumer-focused.
- Prefer examples for public types or members where usage is not immediately obvious.

**`<value>`**
- Include `<value>` on properties where the semantics require clarification beyond the summary.

**`<inheritdoc />`**
- Use `<inheritdoc />` where the implementation intentionally inherits interface or base member documentation and no further clarification is needed.

### Documentation Tone

- Be concise, but not abrupt.
- Be precise, but not overly academic.
- Explain observable behaviour, guarantees, and limitations.
- Use standard XML documentation idioms consistently.
- Do not write vague or filler summaries.
- Do not repeat obvious type information unnecessarily.
- Do not over-explain trivial members.
- Do not use casual or conversational wording.

### Inline Comments

- Add inline comments only where they provide real value.
- Use them to explain non-obvious logic, concurrency coordination, lock-free or low-level state transitions, defensive clamping, important sequencing requirements, or why a block exists when it is not self-evident.
- Explain *why*, the protocol intent, or subtle state meaning — not basic syntax.
- Do not add comments that merely narrate obvious code.

### Parameter Validation

All public interfaces (public methods, constructors, protected-virtual extension points, indexers) must validate their parameters using the `ThrowHelper.ThrowIf…` members declared in `Bodu.Core`.

- Prefer an existing `ThrowIf…` helper over hand-rolled checks. The catalogue covers nulls, ranges, enum values, array offsets/lengths, span sizes, type compatibility, and related cases.
- If no existing helper fits a validation rule, **add a new `ThrowIf…` member** to `ThrowHelper` when the rule is general-purpose enough to be reused. Follow the naming, signature, and XML-doc conventions established by the existing helpers (including the `CallerArgumentExpression`-driven `paramName`).
- Inline `if`-statement validation is permitted only for rules that are specific to a single call site and do not justify a shared helper. In that case, format the check on a **single line**:

  ```csharp
  if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentNullException(nameof(xml));
  ```

- **Group validation statements together** at the top of the member, before any real work. Keep helper calls and single-line `if` checks in a single contiguous block, then a blank line, then the method body.

Example:

```csharp
public static NotableDateRule Create(string name, int dayOffset, string? culture)
{
    ThrowHelper.ThrowIfNull(name);
    ThrowHelper.ThrowIfGreaterThan(dayOffset, MaxOffset);
    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(MyResourceStrings.Arg_Invalid_NameBlank, nameof(name));

    return new NotableDateRule(name, dayOffset, culture);
}
```

### Static Text and Exception Messages

Every project that throws exceptions, prints diagnostics, or otherwise exposes user-facing text must store that text in a `.resx` resource file alongside an auto-generated strongly-typed accessor. **Never hard-code an exception message as a string literal in production code.** This applies to every project in the solution; the established reference implementations are `Bodu.Globalization.Calendar/src/CalendarResourceStrings.{resx,Designer.cs}` and `Bodu.Financial/src/FinancialResourceStrings.{resx,Designer.cs}`.

**Resource file conventions:**

- Name the file `<Domain>ResourceStrings.resx` (e.g. `CalendarResourceStrings.resx`, `FinancialResourceStrings.resx`). Place it at the project's `src/` root.
- Pair it with the matching `<Domain>ResourceStrings.Designer.cs` generated by `ResXFileCodeGenerator` (the `internal` strongly-typed wrapper). Both files belong in the same folder.
- Wire the resx and Designer into the project's `.csproj` with the standard `EmbeddedResource Update="..."` + `Compile Update="..."` block — see `Bodu.Financial/src/Bodu.Financial.csproj` for the canonical form.
- Generate the Designer class as `internal` (matches the BCL convention; consumers should not depend on the resource keys directly).

**Resource key naming convention** (matches both reference implementations):

| Prefix | Exception type | Example |
|---|---|---|
| `Arg_Invalid_*` | `ArgumentException` | `Arg_Invalid_AllocationRatiosEmpty` |
| `Arg_Null_*` | `ArgumentNullException` with a context-specific message | `Arg_Null_ProviderAtIndex` |
| `Arg_OutOfRange_*` | `ArgumentOutOfRangeException` | `Arg_OutOfRange_ExchangeRateNotPositive` |
| `Op_Invalid_*` | `InvalidOperationException` | `Op_Invalid_CurrencyMinorUnitsOutOfRange` |
| `Op_NotSupported_*` | `NotSupportedException` | `Op_NotSupported_CalendarType` |
| `Format_Invalid_*` | `FormatException` | `Format_Invalid_FormatSpecifier` |
| `IO_KeyNotFound_*` / `IO_FileNotFound_*` | I/O failures | `IO_KeyNotFound_Currency` |
| `Json_Invalid_*` | `JsonException` | `Json_Invalid_DuplicateAmount` |

Use `{0}`, `{1}`, … for format placeholders and combine via `string.Format(CultureInfo.CurrentCulture, …)` at the throw site. All user-facing resource text — exception messages, validation diagnostics, and similar display strings — is formatted with `CultureInfo.CurrentCulture`; reserve `CultureInfo.InvariantCulture` for wire/serialization, code generation, and round-trippable parsing where the format must not vary by culture. Diagnostics live in code, not in the resource string — keep messages short and free of conditional grammar; let the caller insert the dynamic context.

**Cross-file ThrowHelper convention:**

Mirror the partial-file pattern used by `FinancialThrowHelper`:

- Root file `<Domain>ThrowHelper.cs` — `internal static partial class` declaration with the standard StyleCop / Roslynator suppressions inherited from the reference implementations.
- `<Domain>ThrowHelper.CallerExpression.cs` (and a `<Domain>ThrowHelper.NetStandard.cs` companion when the project multi-targets `netstandard2.0`) — holds the actual guard implementations.
- Each guard accepts a `[CallerArgumentExpression(nameof(value))] string? paramName = null` parameter and reads its message string from the resx accessor.
- Each guard is marked `[MethodImpl(MethodImplOptions.AggressiveInlining)]`.
- A `[DoesNotReturn]` `void`-returning throw helper is acceptable for unconditional throws used in multiple places (see `FinancialThrowHelper.ThrowFormatSpecifierUnsupported`).

**When to add a helper vs. inline the throw:**

- **Add to `<Domain>ThrowHelper`** when the same validation rule appears in **two or more** call sites across the library (e.g. ISO code validation in `Bodu.Financial`).
- **Keep inline** when a check appears in only one method, but **still source the message from resx**. A single-use throw becomes:

  ```csharp
  if (cond) throw new ArgumentException(MyResourceStrings.Arg_Invalid_SomeRule, nameof(arg));
  ```

  not:

  ```csharp
  if (cond) throw new ArgumentException("Some rule was violated.", nameof(arg));
  ```

- Reuse `Bodu.Core`'s `ThrowHelper` catalogue whenever an existing helper fits — only add to the domain helper when no general-purpose member covers the rule.

**Migrating an existing throw site:**

1. Add a resx entry with a precise key per the naming convention above. Use a comment in the resx if context is helpful.
2. Regenerate (or hand-edit, mirroring the existing pattern) the Designer accessor.
3. Replace the literal string at the throw site with `string.Format(CultureInfo.CurrentCulture, <Domain>ResourceStrings.<Key>, …args)` (omit `string.Format` for messages with no placeholders).
4. If the rule now appears in two or more files, promote the entire `if (…) throw …` block to a `ThrowIf*` member on the domain helper and replace the call sites with the helper call.
5. Tests that assert exception message content via `Contains` must continue to find their expected substrings — preserve the `{0}` placeholder arguments that carry the dynamic context (e.g. type name, ISO code, value).

### Expression-Bodied Members

Use the `=>` expression-bodied form for methods, properties, and accessors whose implementation is small (a single expression or trivial delegation). Format with `=>` on the declaring line and the expression on the **next** line, indented one level:

```csharp
public static List<NotableDateRule> ParseXml(string xml) =>
    ParseDocument(xml).LocalRules.ToList();
```

- The `=>` token stays at the end of the signature line, not on the body line.
- A single level of indentation separates the body from the declaration.
- Use a block body instead when the implementation spans multiple statements or needs intermediate locals, guard clauses, or inline documentation.
- Trivial property and accessor bodies (e.g. `=> _field;`) may remain on one line.

### Formatting and Layout

**Blank Lines**
- Insert blank lines between logical groups of code to make structure visually clear.
- Separate guard clauses and validation, field assignments, setup and initialization, core logic branches, success and failure paths, event invocation or side effects, and return statements.

**Member Layout**
- Maintain consistent spacing between members.
- Group related members logically.
- Use expression-bodied members (per **Expression-Bodied Members** above) where the body is a small, single expression.
- Use block bodies for members with meaningful logic.

**Braces and Wrapping**
- Follow standard modern C# brace style as shown in the examples.
- Wrap long XML documentation lines and remarks sensibly for readability.

**Naming and Qualification**
- Use consistent naming and qualification patterns aligned to the examples.
- Retain explicit interface qualification where it improves clarity.
- Use framework types and language keywords consistently.

### Code Quality

- Write code that is clear, maintainable, consistent, review-friendly, defensive where appropriate, and idiomatic C#.
- Prefer clarity over cleverness.
- All code must be suitable for shared library or framework-style use.

### Updating Existing Code

- Preserve the original intent and behaviour unless explicitly instructed otherwise.
- Improve documentation, formatting, naming clarity, and readability without introducing unnecessary rewrites.
- Keep style consistent across the file — do not mix documentation styles.
- Avoid excessive comments or overlong XML documentation.
- Extend an established style consistently rather than replacing it.
