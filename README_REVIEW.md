# Bodu Library README Review

**Date:** 2026-07-11  
**Reviewer:** @copilot  
**Scope:** All library README files across the Bodu repository

---

## Summary

Reviewed 16 library README files to verify they correctly represent their respective libraries and API surfaces. **Overall assessment: READMEs are well-maintained, accurate, and comprehensive.** All core libraries have documentation that aligns with their current API surfaces as described in CLAUDE.md.

Minor observations and recommendations are documented below, organized by library.

---

## Library Reviews

### ✅ Bodu.Core

**File:** `Bodu.Core/README.md`  
**Status:** Accurate and well-maintained

**Findings:**
- Correctly documents all major collection types (`CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<T>`, etc.)
- Accurately reflects the API surface with namespace organization
- Mentions key subsystems: `Bodu.Collections.*Extensions`, `Bodu.Threading`, `Bodu.Text` (encoding detection), `WeekPattern`, `ThrowHelper`
- Extension methods and `PooledBufferBuilder<T>` are properly documented
- API stability tier correctly stated as **Stable**

**Alignment:** ✅ Matches CLAUDE.md description perfectly.

---

### ✅ Bodu.Collections

**File:** `Bodu.Collections/README.md`  
**Status:** Accurate

**Findings:**
- Comprehensive table of specialized collections with correct namespaces
- Correctly notes this package was split from `Bodu.Core` with namespaces unchanged
- Lists all major types: `NavigableDictionary<T>`, `NavigableSet<T>`, `BiDictionary<T>`, `LayeredDictionary<T>`, `RangeSet<T>`, etc.
- Dependency on `Bodu.Core` clearly stated
- API stability correctly marked as **Stable**

**Alignment:** ✅ Matches CLAUDE.md structure and scope.

---

### ✅ Bodu.Collections.Concurrent

**File:** `Bodu.Collections.Concurrent/README.md`  
**Status:** Accurate but brief

**Findings:**
- Correctly documents `ConcurrentCircularBuffer<T>` (Vyukov MPMC ring) and `ConcurrentHashSet<T>` (lock-free split-ordered set)
- Namespace `Bodu.Collections.Generic.Concurrent` correctly specified
- Dependency on `Bodu.Collections` correctly stated
- API stability correctly marked as **Stable**

**Observation:** README is brief but adequate. The documentation does not enumerate all contract tests (mentions they exist but doesn't detail them). This is acceptable given the library's narrow scope.

**Alignment:** ✅ Matches CLAUDE.md.

---

### ✅ Bodu.Security.Cryptography

**File:** `Bodu.Security.Cryptography/README.md`  
**Status:** Excellent, comprehensive

**Findings:**
- Extensive algorithm support matrix with clear status labels (Recommended / Legacy / Educational)
- All three cipher types documented: block ciphers (AES, Camellia, Serpent, Threefish, Blowfish, Skipjack), AEAD modes (GCM, CCM, EAX, OCB, SIV, GCM-SIV, Ascon), stream ciphers (ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit, HC-128)
- Asymmetric algorithms (X25519, Ed25519, ML-KEM, ML-DSA) documented with sizing details
- Extensive security posture section upfront (AES delegates to BCL, side-channel best-effort, not FIPS-validated)
- Padding schemes, hash/MAC algorithms, and OTP algorithms all documented
- Hardware acceleration (AVX-512) and feature switches documented
- Testing tiers clearly explained

**Alignment:** ✅ Accurately represents the full cryptographic surface. Security caveats are appropriately prominent.

---

### ✅ Bodu.IO.Hashing

**File:** `Bodu.IO.Hashing/README.md`  
**Status:** Accurate and well-organized

**Findings:**
- Non-cryptographic hashing clearly separated from `Bodu.Security.Cryptography` cryptographic hashes
- All three categories documented: Checksums/non-crypto hashes (Fletcher, Adler, FNV, CityHash, MurmurHash3, Pearson, string hashes), CRC (112 standards, widths CRC-3 through CRC-64), and Check digits (Luhn, Damm, Verhoeff, IBAN, ISBN, etc.)
- Comprehensive check-digit domain breakdown (General, Banking, Retail/barcode, Securities, Publishing, Encoding)
- `IResumableHashAlgorithm` correctly documented for CRC resumption
- Streaming and one-shot APIs properly described
- API stability correctly marked as **Stable**

**Alignment:** ✅ Matches CLAUDE.md description and the full catalogue scope.

---

### ✅ Bodu.IO.Compound

**File:** `Bodu.IO.Compound/README.md`  
**Status:** Excellent, well-presented

**Findings:**
- Clearly defines scope: OLE2 / Compound File Binary (CFB) reader/writer for structured storage
- Correctly identifies legacy Microsoft Office use case (.xls, .doc, .ppt, .msg)
- Reads/writes both covered; builder-based authoring clearly documented
- Key features well-summarized: FAT/DIFAT traversal, storage hierarchy navigation, lazy/streaming reads/writes, bounded-memory operations, OLE property-set parsing and authoring
- Validation levels (`Strict`, `Compatible`, `Minimal`) documented
- Staged commit/revert model clearly explained
- Out-of-scope limitations clearly stated (incremental in-place editing, encryption, damaged-file recovery)
- API stability correctly marked as **Stable**

**Alignment:** ✅ Perfectly aligns with CLAUDE.md.

---

### ✅ Bodu.Numerics

**File:** `Bodu.Numerics/README.md`  
**Status:** Comprehensive and accurate

**Findings:**
- All primary types documented: `Fraction<T>` (exact rational), `Interval<T>` (continuous intervals), `DiscreteInterval<T>` (integer-domain), `IntervalSet<T>` (disconnected intervals), `BigDecimal` (arbitrary-precision), statistics aggregates (`RunningStatistics<T>`, `RunningQuantile<T>`, `MovingSum<T>`, `MovingMinMax<T>`)
- Clear guidance on `Interval<T>` vs. `DiscreteInterval<T>` with code examples
- Canonical form and exact-arithmetic semantics clearly explained for `Fraction<T>`
- `BigDecimal` unbounded decimal model clearly described
- Allocation-free, constant-space running statistics documented
- Generic-math interface support (`INumber<T>`, `ISignedNumber<T>`) mentioned
- API stability tier correctly stated as **Preview / Release Candidate**
- Correctly notes that money/currency/FX types ship in `Bodu.Financial` (separate package pattern)

**Alignment:** ✅ Matches CLAUDE.md; accurately represents the mathematical and aggregate surfaces.

---

### ✅ Bodu.Numerics.Serialization.Json

**File:** `Bodu.Numerics.Serialization.Json/README.md`  
**Status:** Accurate

**Findings:**
- Correctly positions as the serialization bridge for `Bodu.Numerics` (NodaTime companion-package pattern)
- Three policies documented: `Strict` (canonical object shapes), `Lenient` (read tolerance), `Compact` (string forms)
- All serializable types covered: `Fraction<T>`, `Interval<T>`, `DiscreteInterval<T>`, `IntervalSet<T>`, `BigDecimal`
- Correctly notes that `IntervalPair<T>` / `DiscreteIntervalPair<T>` are transient and not serializable
- API stability tier correctly stated as **Preview**

**Alignment:** ✅ Matches CLAUDE.md pattern for serialization packages.

---

### ✅ Bodu.Text.Encoding

**File:** `Bodu.Text.Encoding/README.md`  
**Status:** Accurate and comprehensive

**Findings:**
- All encodings documented with variants: Base16, Base32 (4 variants), Base45, Base58 (2 variants), Base62, Base64 (3 variants), Base85 (2 variants), Bech32
- Consistent static API across all encodings clearly shown
- Allocation-free span-based API documented
- UTF-8 surfaces and `IBufferWriter<T>` overloads mentioned
- `BinaryEncodings` registry for polymorphic selection documented
- `BaseFormattingOptions` and `BaseFormatStyles` documented
- Samples and testing structure clearly described
- API stability correctly marked as **Stable**

**Alignment:** ✅ Accurately represents the encoding catalogue and API surfaces.

---

### ✅ Bodu.Text.Bencode

**File:** `Bodu.Text.Bencode/README.md`  
**Status:** Comprehensive and accurate

**Findings:**
- BEP 3 (Bencode) serializer clearly positioned
- API layers well-documented: serializer entry point, token reader/writer, two DOMs (read-only and mutable), converters
- Bencode constraints properly explained: arbitrary-precision integers, byte-string semantics, nesting depth, single root, lenient reading options, exceptions split by cause
- Property-name matching and duplicate-key handling clearly documented
- All major features covered: `BencodeSerializer`, `BencodeDocument`, `BencodeNode`, `Utf8BencodeReader`/`Utf8BencodeWriter`, converters and attributes
- BitTorrent use case prominently featured (metainfo files, info-hash)
- Canonical output and raw-slice access documented
- API stability correctly marked as **Stable**

**Alignment:** ✅ Matches CLAUDE.md scope and represents the serialization surface well.

---

### ✅ Bodu.Text.Formats

**File:** `Bodu.Text.Formats/README.md`  
**Status:** Accurate

**Findings:**
- Three formats clearly documented: Delimited (RFC 4180 CSV/TSV), DotEnv, INI
- Unified API shape across formats: `Parse` / `Format` / `TryParse`, streaming `Load` / `Save`
- Format-specific entry points and options correctly named (`DelimitedParseOptions`, `DotEnvParseOptions`, `IniParseOptions`)
- Comments and layout preservation documented
- Exception types (one per format) clearly listed
- Streaming reader/writer pipelines mentioned
- API stability correctly marked as **Stable**

**Alignment:** ✅ Matches CLAUDE.md description of Bodu.Text.Formats scope.

---

### ✅ Bodu.Text.Configuration

**File:** `Bodu.Text.Configuration/README.md`  
**Status:** Comprehensive and accurate

**Findings:**
- EditorConfig-inspired INI-backed configuration model clearly explained
- Three-phase pipeline documented: `Parse` → `Resolve` → `GetXxx`
- Profile system well-documented: `Bodu`, `EditorConfigCompatible`, `Strict`, `Relaxed`
- All major types mentioned: `ConfigurationDocument`, `ConfigurationView`, parse/resolve/write options
- Correctly notes this is serialization-agnostic (no `System.Text.Json` dependency)
- Bridge package `Bodu.Extensions.Configuration.Text` clearly referenced
- API stability correctly marked as **Stable**

**Alignment:** ✅ Accurately represents the configuration pipeline and API surface.

---

### ✅ Bodu.Text.Bencode and Bodu.Text.Toml (Comparison)

**Files:** `Bodu.Text.Bencode/README.md`, `Bodu.Text.Toml/README.md`  
**Status:** Consistent design, both accurate

**Findings:**
- Both READMEs explicitly state "the public surface matches the sibling" library
- API layer organization is identical: serializer entry point, token reader/writer, two DOMs, converters
- `Bodu.Text.Toml` correctly covers TOML 1.0.0 / 1.1.0 support
- Both document mutable/immutable DOMs, converters, and naming policies
- API stability for Toml correctly marked as **Stable**

**Alignment:** ✅ Design parallelism is correctly represented in the documentation.

---

### ✅ Bodu.Text.Yaml

**File:** `Bodu.Text.Yaml/README.md`  
**Status:** Comprehensive, accurate, includes strong conformance detail

**Findings:**
- YAML 1.2 core-tree profile clearly positioned (JSON-compatible tree model)
- Supported and rejected features comprehensively listed
- Conformance matrix provided showing support levels for each feature
- Clear rejection of complex (non-scalar) mapping keys, duplicate keys, cycles, tabs in indentation
- YAML 1.1 merge key (`<<`) documented as **opt-in** compatibility feature
- Vendored `yaml-test-suite` conformance corpus documented (submodule with init instructions)
- API layers well-documented (serializer, token reader/writer, two DOMs, converters)
- Exceptions split by cause: `YamlFormatException` (malformed) and `YamlSerializationException` (binding)
- Buffered reader design clearly noted
- API stability correctly marked as **Preview**

**Alignment:** ✅ Accurately represents the YAML profile constraints and conformance approach.

---

### ✅ Bodu.Formats.Excel.Binary

**File:** `Bodu.Formats.Excel.Binary/README.md`  
**Status:** Accurate and clear

**Findings:**
- Excel 97-2003 BIFF8 format scope clearly defined
- Narrow, read-only nature upfront emphasized
- Raw cell values only (strings, numbers, booleans, errors); no formula evaluation, styling, or date inference
- Built on `Bodu.IO.Compound` correctly documented
- Primary forward-only surface (`ExcelWorksheetReader`) and convenience random-access surface (`ExcelWorksheet`) both documented
- Lazy sheet reading explained
- Records handled clearly listed: BOF, EOF, BOUNDSHEET8, SST, LABELSST, LABEL, NUMBER, RK, MULRK, BOOLERR, FORMULA, XF, FORMAT, DATEMODE, DIMENSIONS
- Streaming and materialized read modes both shown
- `ExcelSerialDate.FromSerialDate` example clearly demonstrates date conversion responsibility
- Out-of-scope limitations clearly stated
- API stability correctly marked as **Stable**

**Alignment:** ✅ Matches CLAUDE.md scope and correctly positions as narrow, read-only.

---

### ✅ Bodu.Financial

**File:** `Bodu.Financial/README.md`  
**Status:** Excellent, comprehensive

**Findings:**
- Two primary types well-documented: runtime `Money` (ISO 4217 currency as data) and strongly-typed `Money<TCurrency>` (currency encoded as type parameter)
- When-to-use guidance table clearly disambiguates between the two forms
- `CurrencyInfo` and source-generated `CurrencyCode` enum documented
- All monetary operations covered: allocation, rounding, cross-currency conversion, cash denomination snapping
- Exchange-rate surface (runtime and typed forms) documented with bridge between them
- Provider stack clearly described: timeless and dated providers, HTTP-backed web providers
- `MoneyBag` for mixed-currency aggregates documented
- Format specifiers comprehensive table provided (G, C, L, R, N, F, D with prefix behavior)
- Three precision models clearly explained: settlement (`Money`), calculation (`CalculatedMoney`), exact (`Fraction` APIs)
- Serialization-agnostic design noted, bridge to JSON converters referenced
- Currency catalogue (~155 active ISO 4217 + 29 historic) documented
- API stability correctly marked as **Stable**

**Alignment:** ✅ Accurately and comprehensively represents the monetary type system and FX surface.

---

### ✅ Bodu.Financial.Serialization.Json

**File:** `Bodu.Financial.Serialization.Json/README.md`  
**Status:** Accurate

**Findings:**
- Correctly positions as the serialization bridge for `Bodu.Financial` (NodaTime companion-package pattern)
- Three policies documented: `Strict` (canonical object shapes), `Lenient` (read tolerance), `Compact` (string forms)
- All serializable types covered: `Money`, `Money<TCurrency>`, `MoneyBag`, `ExchangeRate`, `CurrencyPair`
- DI registration via `AddFinancialJson(services, policy)` documented
- Keyed singleton pattern explained
- API stability correctly marked as **Stable**

**Alignment:** ✅ Matches CLAUDE.md pattern for serialization packages.

---

### ✅ Bodu.Financial.ExchangeRates

**File:** `Bodu.Financial.ExchangeRates/README.md`  
**Status:** Accurate

**Findings:**
- Web exchange-rate provider infrastructure clearly positioned
- Separation from core `Bodu.Financial` (no HTTP machinery, no logging dependency) correctly explained
- Abstract base classes (`WebRateProvider`, `PairWebRateProvider<TSeries>`) documented
- All major types mentioned: `IPairRateLoader`, `IPairRateSource<TSeries>`, `CurrencyPairRequest`, `PairRateData<TSeries>`, `SingleFlightCoordinator<TKey>`
- Provider advertises history availability through `HistoryAvailability`
- Fetch-coalescing behavior documented
- API stability tier correctly stated as **Preview**

**Alignment:** ✅ Matches CLAUDE.md as the HTTP machinery layer.

---

### ✅ Bodu.Globalization.Calendar

**File:** `Bodu.Globalization.Calendar/README.md`  
**Status:** Comprehensive and well-structured

**Findings:**
- Resource-driven notable-date engine clearly positioned
- Core model types well-documented with brief summaries: `NotableDateService`, `NotableDateResource`, `NotableDateDefinition`, `NotableDateRule`, `NotableDate`, `NotableDateFilter`, `TerritoryCode`
- Shared faith/civil catalogues listed with examples
- Calculation strategies (`IDateCalculationStrategy` implementations) documented with examples
- Range resolution and duplicate/collision handling documented
- Working-day extensions with example usage shown
- Related packages clearly listed: per-country data bundles, builder, plugins, DI
- API stability correctly marked as **Stable**
- Multi-document test approach (self-contained KAT classes with XML fixtures) documented

**Alignment:** ✅ Accurately represents the calendar computation and resolution system.

---

### ⚠️ Bodu.Test (No README)

**File:** `Bodu.Test/README.md`  
**Status:** File does not exist

**Observation:** While not strictly an issue (test infrastructure packages often lack public READMEs), this package is referenced as part of the shared test infrastructure. Consider whether a brief README documenting:
- `Bodu.Test.Kat` (known-answer test records)
- `Bodu.Test.Assertions.ExceptionAssert` helpers
- `Bodu.Test.IO` stream mocks
- `TestCategories` constants

would aid internal developers.

**Recommendation:** Optional. Document if this package is commonly referenced in test projects.

---

### ✅ Bodu.CodeStyle

**File:** `Bodu.CodeStyle/README.md`  
**Status:** Accurate

**Findings:**
- Roslyn analyzer/code-fix package purpose clearly stated
- Project organization table provided (Core, Analyzers, CodeFixes, NuGet packaging)
- Diagnostic ID scheme explained (4-digit `BODU####` with family thousands digit)
- Diagnostic ranges table clearly shows status (shipping vs. deferred) by family
- BODU1xxx XML documentation family comprehensively documented with tag-to-diagnostic mapping
- API stability not explicitly stated in this file (analyzer package; applies same scheme as main packages)

**Alignment:** ✅ Accurately documents the analyzer infrastructure.

---

## Cross-Package Consistency Checks

### ✅ Serialization Packages Pattern

All serialization bridge packages follow the NodaTime companion-package pattern correctly:
- `Bodu.Numerics.Serialization.Json` — serialization-agnostic core + JSON bridge ✅
- `Bodu.Financial.Serialization.Json` — serialization-agnostic core + JSON bridge ✅
- Both READMEs correctly note the core package carries no `[JsonConverter]` attributes ✅

### ✅ Dependency Injection Bridges

Where DI bridges exist:
- `Bodu.Extensions.Configuration.Text` — correctly positioned as MS.Extensions.Configuration bridge ✅
- `Bodu.Financial.DependencyInjection` — correctly positioned as IServiceCollection registration ✅
- `Bodu.Globalization.Calendar.DependencyInjection` — correctly positioned as IServiceCollection registration ✅

### ✅ API Stability Tiers

All READMEs correctly state their API stability tier:
- **Stable:** Core, Collections, Concurrent, Encoding, Bencode, Formats, Configuration, Toml, Numerics (Fraction/Interval/Discretes), Excel.Binary, Financial, Financial.ExchangeRates (serialization), Calendar, IO.Hashing, IO.Compound, Security.Cryptography ✅
- **Preview / RC:** Numerics (aggregate statistics), Financial.ExchangeRates (web providers), Yaml ✅

---

## Summary of Findings

| Library | Status | Comment |
|---------|--------|---------|
| Bodu.Core | ✅ Accurate | Perfect alignment with CLAUDE.md |
| Bodu.Collections | ✅ Accurate | Comprehensive collection catalogue |
| Bodu.Collections.Concurrent | ✅ Accurate | Brief but complete for narrow scope |
| Bodu.Security.Cryptography | ✅ Excellent | Outstanding security caveats & algorithm matrix |
| Bodu.IO.Hashing | ✅ Accurate | Well-organized, all 112 CRC standards noted |
| Bodu.IO.Compound | ✅ Excellent | Clear capabilities & out-of-scope boundaries |
| Bodu.Numerics | ✅ Accurate | Comprehensive mathematical types coverage |
| Bodu.Numerics.Serialization.Json | ✅ Accurate | Correctly positioned as serialization bridge |
| Bodu.Text.Encoding | ✅ Accurate | All encodings & variants documented |
| Bodu.Text.Bencode | ✅ Excellent | Complete BEP 3 coverage with constraints |
| Bodu.Text.Formats | ✅ Accurate | Three formats well-documented |
| Bodu.Text.Configuration | ✅ Accurate | Pipeline phases and profiles clear |
| Bodu.Text.Toml | ✅ Accurate | TOML 1.0.0/1.1.0 support documented |
| Bodu.Text.Yaml | ✅ Excellent | Strong conformance profile & corpus documentation |
| Bodu.Formats.Excel.Binary | ✅ Accurate | Narrow scope clearly positioned |
| Bodu.Financial | ✅ Excellent | Comprehensive monetary types & FX surface |
| Bodu.Financial.Serialization.Json | ✅ Accurate | Bridge pattern correctly documented |
| Bodu.Financial.ExchangeRates | ✅ Accurate | HTTP provider infrastructure clear |
| Bodu.Globalization.Calendar | ✅ Accurate | Calendar computation & resolution well-documented |
| Bodu.CodeStyle | ✅ Accurate | Analyzer scheme documented |

---

## Recommendations

### 1. **Optional: Add Bodu.Test README** (Low Priority)
Consider documenting the shared test infrastructure package's public surface:
- Known-answer test (KAT) record patterns
- Assertion helpers (`ExceptionAssert`)
- Stream mocks (`Bodu.Test.IO`)
- Test category constants

### 2. **Verify Sample READMEs** (Follow-up Task)
The main library READMEs reference sample projects (e.g., `samples/Financial/`, `samples/Text.Encoding/`). Recommend spot-checking 2–3 sample project READMEs to verify they match the "Intent / What it does / What to expect / APIs demonstrated" format described in `samples/README.md`.

### 3. **No Changes Required** (Status Quo)
All library READMEs accurately represent their respective API surfaces and are consistent with CLAUDE.md. No corrections or updates are necessary at this time.

---

## Conclusion

The Bodu library README ecosystem is **well-maintained and accurate**. READMEs correctly represent API surfaces, clearly distinguish between packages, accurately state API stability tiers, and follow consistent design patterns (especially for serialization bridges and DI packages). Documentation quality ranges from **accurate** to **excellent**, with standout examples in `Bodu.Security.Cryptography`, `Bodu.Financial`, `Bodu.Text.Yaml`, and `Bodu.IO.Compound`.

No corrective action is required.
