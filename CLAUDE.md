# CLAUDE.md

Guidance for AI assistants working in this repository. Read this file before making changes.

## Repository Overview

**Bodu** is a multi-project C# utility library solution focused on high-performance, well-documented, framework-style building blocks. The solution lives at `bodu.slnx` (Visual Studio's modern solution format - note the `.slnx` extension, not `.sln`). The projects are organised by domain (below). Where several projects share an identical structure and naming - notably the regional calendar **data bundles** (`Bodu.Globalization.Calendar.Data.<Region>`) - they are shown as a single `<…>` wildcard row rather than enumerated individually:

| Project | Path | Responsibility |
|---|---|---|
| `Bodu.Core` | `Bodu.Core/` | Buffers, extension surfaces (incl. the `Bodu.Collections.*Extensions` enumerable/dictionary/list operators), threading primitives (`Bodu.Threading`), sequences and functional seams, text-encoding utilities (`Bodu.Text` namespace: `EncodingDetection`, `EncodingExtensions`, `StringEncodingExtensions`), XML, argument validation helpers (`ThrowHelper`), `WeekPattern`. |
| `Bodu.Collections` | `Bodu.Collections/` | The specialized generic-collection catalogue, split out of `Bodu.Core` (namespaces unchanged; references `Bodu.Core`): `Bodu.Collections.Generic` (circular buffer, deque, evicting dictionary, sequenced/multi-value dictionaries, multiset, indexed/ordered sets, indexed priority queue, range set/dictionary, segmented buffer), `Bodu.Collections.Specialized` (`BitSet`), `.Generic.Graphs` (graph, algorithms, disjoint set), `.Generic.Trees` (tree, tries), and `Bodu.Collections.Probabilistic` (Bloom filter, count-min sketch, HyperLogLog - approximate sketches over comparer-derived hashing). `ShuffleHelpers` and the internal `SequenceUtility` stay in `Bodu.Core` because the staying `IEnumerableExtensions` partials depend on them.<br/><br/>**`Bodu.Collections.Specialized`** holds the members of the package that serve a specialised purpose rather than acting as general-purpose containers - today only `BitSet` (packed bit set). The dividing line is coupling - it references nothing else in the package and nothing references it, whereas every other non-generic type in `Bodu.Collections.Generic` is either a collection in its own right or a policy/option satellite of one (`EvictingDictionaryPolicy`, `DequeOverflowPolicy`, `GraphAlgorithms`, …) that would be orphaned by a move. The files sit in `src/Collections.Specialized/` per the folder-follows-namespace rule. The RFC 6962 Merkle tree that formerly lived here now ships as `MerkleTree` in `Bodu.Security.Cryptography` (see that row); no `Bodu.Collections.Merkle` namespace, `shared/` folder, or source-only guard project remains. |
| `Bodu.Collections.Concurrent` | `Bodu.Collections.Concurrent/` | Thread-safe collection variants split out of `Bodu.Collections` (namespace `Bodu.Collections.Generic.Concurrent` unchanged; references `Bodu.Collections`): `ConcurrentCircularBuffer<T>` (lock-free Vyukov MPMC ring), `ConcurrentHashSet<T>` (lock-free split-ordered set), `ConcurrentEvictingDictionary<TKey,TValue>` (lock-striped bounded cache: all six eviction policies, optional TTL, single-flight `GetOrAdd`, post-commit `ItemEvicted`), `ConcurrentLruCache<TKey,TValue>` (read-optimized bounded cache: lock-free reads over a segmented pseudo-LRU with hot/warm/cold queues, maintenance amortized onto writers, striped hit/miss telemetry - no TTL, no policy choice, `GetOrAdd` is not single-flight, and `Count` may transiently exceed `Capacity` by the number of in-flight writers). |
| `Bodu.Test` | `Bodu.Test/` | Shared test infrastructure: KAT records (`Bodu.Test.Kat`), assertion helpers (`Bodu.Test.Assertions.ExceptionAssert`), reusable stream mocks (`Bodu.Test.IO`), test category constants (`TestCategories`). Referenced by other test projects. |
| `Bodu.Numerics` | `Bodu.Numerics/` | `Fraction<T>` (rational arithmetic, parse/format, generic math, UTF-8) and the interval algebra (`Interval<T>`, `DiscreteInterval<T>`, `IntervalSet<T>`, the pair result types). Serialization-agnostic - no `System.Text.Json` dependency; XML helpers stay here. |
| `Bodu.Numerics.Serialization.Json` | `Bodu.Numerics.Serialization.Json/` | `System.Text.Json` integration for `Bodu.Numerics` - converters + factories, `NumericsJsonPolicy`, `FractionJsonExtensions`, and the `options.AddNumericsJsonConverters()` registration for `Fraction<T>` / `Interval<T>` / `DiscreteInterval<T>` / `IntervalSet<T>`. Types live in the `Bodu.Numerics.Serialization.Json` namespace. Keeps the core library serialization-agnostic (the NodaTime companion-package pattern). |
| `Bodu.IO.Hashing` | `Bodu.IO.Hashing/` | Non-cryptographic hashing (Fletcher-16/32/64, full RevEng CRC catalogue, check-digit algorithms: Luhn, Damm, ABA, EAN, GTIN, IBAN, ISBN, ISIN, LEI, ISO 7064). |
| `Bodu.IO.Compound` | `Bodu.IO.Compound/` | Reader, editor, and writer for the OLE2 / Compound File Binary (CFB) container format - the structured-storage envelope used by legacy Microsoft Office files (`.xls`, `.doc`, `.msg`). Exposes the embedded named streams without any application-format knowledge, edits existing containers transactionally (BCL-style `OpenStream(name, FileMode, FileAccess)` writable cursors staged until `Commit`, with `Revert`), and authors new containers through a staged builder API. |
| `Bodu.IO.Pst` | `Bodu.IO.Pst/` | Low-level, read-only container reader for the Outlook personal-folders format (PST / MS-PST), Unicode and ANSI formats (one internal layout descriptor selects the 64-bit or 32-bit widths per file; the 4 KiB-page OST variant is rejected): the node-database (NDB) layer - header, node/block B-trees, block data with the permute/cyclic content encodings decoded and checksums verified, multi-block data trees, and per-node subnode trees - and the LTP layer over it - heap-on-node, BTree-on-heap, and the public property-context and table-context views on `PstNode` (wire-typed values, no MAPI semantics). The substrate the `Bodu.Formats.Outlook.Pst` mail-store reader builds on; no writing. |
| `Bodu.Security.Cryptography` | `Bodu.Security.Cryptography/` | Block ciphers (Threefish 256/512/1024, Skipjack, Blowfish, Twofish, Camellia), AEAD (Ascon), keyed/cryptographic hashes (Skein, BLAKE2, Tiger, SipHash, FNV1a, Adler), asymmetric algorithms (X25519, Ed25519, ML-KEM, ML-DSA), crypto transforms, helpers.<br/><br/>**Merkle trees.** The RFC 6962 Merkle tree lives here as a single type, `MerkleTree(Func<HashAlgorithm> algorithmFactory, int fanOut = 2, int maxDegreeOfParallelism = 1)` - immutable, stateless, not `IDisposable`, shareable across threads - in `src/Security.Cryptography/` alongside its family: `MerkleBlockAccumulator` (the push-style writer-side counterpart created by `CreateBlockAccumulator`), `MerkleBlockComputation`, and the optional trace recorder `MerkleTreeDiagnostics` (with its nested public `Node` record). The block arithmetic (`BlockCount` / `BlockOffset` / `BlockLength`) is static on `MerkleTree`. Everything else is a partial of `MerkleTree` itself, kept to the minimum number of declared types: `MerkleTree.Primitives.cs` (the three domain-separation prefix constants and the stateless RFC 6962 primitives - split point, `Mth`, the inclusion and subproof walks - as `internal static` members so the tests can drive them), `MerkleTree.LeafHashing.cs` (the sequential/async/parallel leaf loops, with the private nested `LeafBatch`), and the internal nested `MerkleTree.LevelFold` (the k-ary level-by-level streaming fold with lone-node promotion - at fan-out two it visits exactly RFC 6962's nodes, holding at most `fanOut − 1` pending hashes per level - which reports straight into a `MerkleTreeDiagnostics?`). Every root computation and the accumulator fold through `LevelFold`; `Mth` serves only the proof walks. `fanOut = 2` is RFC 6962's tree and the default; a wider fan-out is an explicit non-RFC mode on which every proof member throws `NotSupportedException`. `maxDegreeOfParallelism` (1 sequential, −1 unbounded, ≥ 2 bounded) hashes leaves in `Parallel.For` batches with one `HashAlgorithm` per worker while the fold and any observer stay on the caller thread, in order; cancellation is normalised to `OperationCanceledException` carrying the caller's token, and a faulting worker surfaces its first inner exception rather than an `AggregateException`. It is deliberately **not** in `Bodu.Collections`: a commitment consumer takes this package. |
| `Bodu.Text.Encoding` | `Bodu.Text.Encoding/` | Binary encodings: Base16, Base32, Base58, Base64, Base64Url, Base85 (with variants, formatting options, span/UTF-8 surfaces). |
| `Bodu.Text.Configuration` | `Bodu.Text.Configuration/` | Bodu text configuration parser/resolver (INI-compatible profile, resolver precedence, typed view getters, write options) over its **own** trivia-preserving INI document model (`IniDocumentBase`/`IniDocument`/`IniSection`/`IniEntry`/`IniComment` in the `Bodu.Text.Configuration` namespace) - no format-library dependency. |
| `Bodu.Text.Filtering` | `Bodu.Text.Filtering/` | Include/exclude filtering engine for lists of text values (namespace `Bodu.Text.Filtering`): glob (wildcard, character-class, `{a,b}` brace-alternation) and regex patterns compile once into a cost-tiered `TextFilter` that runs the cheapest strategies first (MatchAll → Literal → Prefix/Suffix → Contains → general wildcard → Regex), with Ant/MSBuild-style include/exclude set semantics (`AnyMatch`) or gitignore-style last-match-wins ordered rules, gitignore-convention list parsing, always-on match statistics, and an optional per-decision `ITextFilterObserver`. Core-only dependency. |
| `Bodu.Text.Serialization` | `Bodu.Text.Serialization/` | The shared serialization core for the `System.Text.Json`-shaped text serializers: the compiled attribute family, ignore/creation/unmapped-member/naming enums, serialization callback interfaces, and naming policies, plus the `shared/**` source (metadata resolver, converter engine) the per-format packages compile under their format symbol. Referenced by `Bodu.Text.Bencode`/`.Toml`/`.Yaml`/`.Delimited`/`.DotEnv`/`.Ini`. |
| `Bodu.Text.Delimited` | `Bodu.Text.Delimited/` | Standalone STJ-shaped Delimited (RFC 4180 CSV/TSV) library - `Utf8DelimitedReader`/`Utf8DelimitedWriter`, `DelimitedSerializer` (incl. `IAsyncEnumerable<TRecord>` streaming), mutable `.Nodes` DOM, read-only `.Document` DOM, dialect policies, csv-spectrum-derived Regression corpus. References `Bodu.Text.Serialization`. |
| `Bodu.Text.DotEnv` | `Bodu.Text.DotEnv/` | Standalone STJ-shaped DotEnv library - `Utf8DotEnvReader`/`Utf8DotEnvWriter` (export prefix, quoting, inline comments), `DotEnvSerializer`, mutable `.Nodes` DOM (export-flag-preserving), read-only `.Document` DOM. References `Bodu.Text.Serialization`. |
| `Bodu.Text.Ini` | `Bodu.Text.Ini/` | Standalone STJ-shaped INI library - source-order `Utf8IniReader` + normalized `IniDocumentReader` (duplicate-section merge), `Utf8IniWriter`, `IniSerializer` (`GlobalSectionName`, depth-2 gate), **comment-preserving** mutable `.Nodes` DOM, read-only `.Document` DOM. References `Bodu.Text.Serialization`. |
| `Bodu.Text.Formats` | `Bodu.Text.Formats/` | Umbrella meta-package (no code): references `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, and `Bodu.Text.Ini`. |
| `Bodu.Text.Formats.Generators` | `Bodu.Text.Formats.Generators/` | Incremental Roslyn source generator (netstandard2.0, not yet packable): emits reflection-free `IDelimitedRecordFactory<TRecord>` / `IIniSectionFactory<TSection>` implementations for `[DelimitedRecord]` / `[IniSection]` partial POCOs (generated static `DelimitedFactory` / `IniFactory` properties), consumed by the serializer factory overloads for trimming/AOT-safe binding. Diagnostics `BTFG001`-`BTFG003`; consumers reference it with `OutputItemType="Analyzer"`. |
| `Bodu.IO.Biff` | `Bodu.IO.Biff/` | Low-level codec for the Excel Binary Interchange File Format record streams (BIFF5 and BIFF8) found inside `.xls` workbooks - the substrate beneath `Bodu.Formats.Excel.Binary`, in the same relation `Bodu.IO.Pst` has to `Bodu.Formats.Outlook.Pst`. The forward-only, allocation-free `ref struct` `BiffReader` frames physical records over a span (resumable across buffers through `BiffReaderState`), establishes the version from `BOF` and the code page from `CODEPAGE`, exposes every record's identifier and raw payload (unknown records are never an error), and decodes the common structural and cell records through typed accessors returning `Biff*Record` structs; `BiffString` is a span-backed view over BIFF8 Unicode and BIFF5 code-page strings; `BiffSstReader` walks the shared string table across its `CONTINUE` records; `BiffRk` encodes and decodes RK numbers. No compound-file dependency, no workbook or cell model. Core-only dependency (plus `System.Text.Encoding.CodePages` for BIFF5 byte strings). |
| `Bodu.Formats.Excel.Binary` | `Bodu.Formats.Excel.Binary/` | Narrow, read-only reader for the Excel 97-2003 binary workbook format (BIFF8 / `.xls`), built on `Bodu.IO.Compound` for the container and `Bodu.IO.Biff` for the record stream. Exposes raw worksheet cell values (strings, numbers, booleans, errors - including a formula cell's cached result and the spreadsheet error code) plus each sheet's declared used range, without formula evaluation, styling, or higher-level interpretation. |
| `Bodu.Formats.Outlook` | `Bodu.Formats.Outlook/` | The shared MAPI value model for the Outlook format readers (namespace `Bodu.Formats.Outlook`, flattened per the `Bodu.Formats.Excel` convention): property tags/types (`MapiPropertyTag` / `MapiPropertyType`), decoded values with the tag-addressed `MapiPropertyCollection`, named-property identities (`MapiNamedProperty`), curated `MapiPropertyIds`, recipient/attachment enums, and `OutlookFormatException`. Container-free - consumed by `Bodu.Formats.Outlook.Msg` and `Bodu.Formats.Outlook.Pst` rather than owned by either. |
| `Bodu.Formats.Outlook.Msg` | `Bodu.Formats.Outlook.Msg/` | Read-only reader for the Outlook message format (`.msg` / MS-OXMSG) over `Bodu.IO.Compound`, sharing the `Bodu.Formats.Outlook` value model (public types in the flattened `Bodu.Formats.Outlook` namespace; internal record layer under `Bodu.Formats.Outlook.Msg`). Opens a message as a disposable `OutlookMessage` session: all decoded MAPI properties, recipients, attachments, nested attached messages, named-property resolution, and the text/HTML/compressed-RTF bodies. No authoring, no MAPI session emulation. |
| `Bodu.Formats.Outlook.Pst` | `Bodu.Formats.Outlook.Pst/` | Read-only reader for the Outlook personal-folders format (`.pst` / MS-PST, Unicode and ANSI formats) over `Bodu.IO.Pst`, sharing the `Bodu.Formats.Outlook` value model (public types in the flattened `Bodu.Formats.Outlook` namespace; internal readers under `Bodu.Formats.Outlook.Pst`). Opens a mail store as a disposable `OutlookMailStore` session: store properties, the folder hierarchy, and every message with decoded MAPI properties, recipients (the shared `OutlookRecipient`), attachments with nested embedded messages, store-wide named-property resolution, and the text/HTML/compressed-RTF bodies. Compiles the `Bodu.Formats.Outlook/shared/**` decode layer under the `OUTLOOK_PST` symbol. No authoring, no MAPI session emulation. |
| `Bodu.Text.Bencode` | `Bodu.Text.Bencode/` | Self-contained Bencode (BEP 3) library shaped after the `System.Text.Json` BCL: ref-struct `Utf8BencodeReader`/`Utf8BencodeWriter` (+ `BencodeTokenType`/`BencodeValueKind`), the `BencodeSerializer` POCO mapper (converters, the full attribute family, options, naming policies, callbacks, enum converters), a mutable `JsonNode`-style DOM (`BencodeNode`/`BencodeObject`/`BencodeArray`/`BencodeValue`), and a read-only `JsonDocument`-style DOM (`BencodeDocument`/`BencodeElement`). Folders/namespaces follow the S.T.J source layout: `Bodu.Text.Bencode[.Reader/.Writer/.Document/.Nodes/.Serialization]`. |
| `Bodu.Text.Toml` | `Bodu.Text.Toml/` | Self-contained TOML (v1.0.0 / v1.1.0) library, the same `System.Text.Json`-aligned shape and `Text.Toml.*` folder/namespace structure as `Bodu.Text.Bencode`: `Utf8TomlReader`/`Utf8TomlWriter`, the `TomlSerializer` POCO mapper, and the mutable (`TomlNode`) and read-only (`TomlDocument`) DOMs. TOML's richer value model adds native float, boolean, and the four RFC 3339 date-time kinds, plus `TomlSpecVersion` and `TomlByteArrayHandling`. |
| `Bodu.Text.Yaml` | `Bodu.Text.Yaml/` | Self-contained YAML library and the third `System.Text.Json`-shaped serializer alongside `Bodu.Text.Bencode` / `Bodu.Text.Toml`: it exposes `Utf8YamlReader`/`Utf8YamlWriter`, the `YamlSerializer` POCO mapper, and the mutable (`YamlNode`) and read-only (`YamlDocument`) DOMs. Like its siblings it compiles the shared `Bodu.Text.Serialization/shared/**` source (under the `YAML` symbol): the `MetadataResolver`/`TypeMetadata`/`PropertyMetadata` trio, the structural converter factories (nullable/dictionary/collection/object), the full attribute family, and the non-null `GetConverter` pipeline with `YamlConverter{T}`/`YamlConverterFactory`. Its **scalar converters stay format-local** (`Text.Yaml.Serialization.Converters`) because YAML's implicit typing coerces across scalar kinds (string/integer/float/boolean/null), which the token-strict shared scalar converters cannot express; the DOM↔serializer bridges are adopted (the shared `NodeConverter` under the `YAML` symbol, plus format-local `YamlElement`/`YamlDocument` converters), and the options surface matches its siblings (`DefaultIgnoreCondition`, `YamlSerializerDefaults` presets, `YamlStringEnumConverter`/`YamlNumberEnumConverter{TEnum}`). Ships the same stream/async facade as its siblings (`Serialize<T>(IBufferWriter<byte>)`, `Deserialize<T>(Stream)`, `SerializeAsync`/`DeserializeAsync` - buffered in full; only the stream copy is asynchronous). |
| `Bodu.Extensions.Configuration.Text` | `Bodu.Extensions.Configuration.Text/` | Bridge between `Microsoft.Extensions.Configuration` and `Bodu.Text.Configuration`. |
| `Bodu.Globalization.Calendar` | `Bodu.Globalization.Calendar/` | Resource-driven notable-date engine on the notable-date schema: rule model, date-calculation strategies and astronomical algorithms (`Algorithms`), range resolution (`RangeResolution`), observed-date adjustments, working-day extensions (`Bodu.Extensions`), and `NotableDateService`. |
| `Bodu.Globalization.Calendar.Plugins` | `Bodu.Globalization.Calendar.Plugins/` | Trust-gated external plugin loading for assemblies contributing custom `INotableDateAlgorithm` implementations. |
| `Bodu.Globalization.Calendar.Builder` | `Bodu.Globalization.Calendar.Builder/` | Fluent authoring API (`NotableDateDocumentBuilder`) that constructs notable-date documents on the notable-date schema, with full XML and JSON-subset serialization (`ToXml`/`ToJson`/`Save`) and parsing (`FromXml`/`FromJson`/`Load`); materializes a `NotableDateResource` via the loader. |
| `Bodu.Globalization.Calendar.Caching[.Sqlite/.Distributed]` | `…Calendar.Caching[.Sqlite/.Distributed]/` | Caching layer for the notable-date engine: `CachingNotableDateService`, a decorator over any `INotableDateService` serving computed dates from a per-territory, per-civil-year cache (in-memory or one TOML/JSON file per territory, TTL/resource-version refresh) behind the `INotableDateCache` contract, with the `AddCachedNotableDateService` DI registration in the core package and durable `Sqlite` (`SqliteNotableDateCache` / `AddSqliteNotableDateCache`) and `Distributed` (`DistributedNotableDateCache` over `IDistributedCache` / `AddDistributedNotableDateCache` / `AddRedisNotableDateCache`) backends as add-on packages. |
| `Bodu.Globalization.Calendar.Tool` / `.Build` | `…Calendar.Tool/`, `…Calendar.Build/` | Rule-pack toolchain (not in a shipping wave yet): `Tool` is the command-line compiler/lint for notable-date rule packs (stable `BODU-CAL-*` diagnostics, compiles XML/JSON documents to sealed `.bcal` binary packs); `Build` is the MSBuild integration (`CompileNotableDatePack` task + `NotableDatePack` items) that invokes the tool incrementally during build. |
| `Bodu.Globalization.Calendar.<Region>` | `…Calendar.Data.<Region>/` | Per-region calendar data bundles - one self-contained embedded pack per country (national rules plus ISO 3166-2 subdivisions), each exposing a `<Region>CalendarData` factory and importing the shared catalogues through a `<region>-common` hub. `<Region>` ∈ `Americas` (US, CA, MX, BR, AR, CL, CO, PE), `AsiaPacific` (AU, CN, IN, JP, KR, MY, NZ, SG, ID, TH, PH, VN, HK, TW), `Europe` (28 EU/EEA territories incl. GB, FR, DE), `MiddleEast` (AE, SA, IL, TR, QA, JO), `Africa` (ZA, NG, KE, GH, ET, EG, MA). |
| `Bodu.Globalization.Calendar.DependencyInjection` | `…Calendar.DependencyInjection/` | `IServiceCollection` extensions for registering calendar services; `AddNotableDateService` / `AddReloadableNotableDateService` are declared in the `Bodu.Globalization.Calendar` namespace (consumers add `using Bodu.Globalization.Calendar;`). |
| `Bodu.Globalization.Recurrence` | `Bodu.Globalization.Recurrence/` | Recurrence-rule evaluation, Core-only: `RecurrenceRule` (RFC 5545 `RRULE` parse/format/occurrence enumeration), `RecurrenceRuleBuilder`, `RecurrenceSet` (rules composed with `RDATE`/`EXDATE`, canonical property-block round-trip), `CronExpression` (Vixie five-field / optional-seconds six-field / `@` macros), and `AnchoredInterval` (instant-anchored interval recurrence in the RFC 5545 duration grammar). Every form answers `GetNextOccurrence` **and** `GetPreviousOccurrence` with inclusive flags over `DateTime` and `DateTimeOffset`, and exposes a defect-naming `TryParse(s, out result, out failureMessage)` overload. Pure in its arguments (no wall clock, no machine timezone - enforced by a metadata-scan `PurityTests` guard). A natural sibling of `Bodu.Globalization.Calendar` without depending on it. |
| `Bodu.Financial` | `Bodu.Financial/` | Money and currency primitives across three namespaces: `Bodu.Financial` (`Money` / `Money<TCurrency>` / `CalculatedMoney` / `MoneyBag`, allocation and rounding policies, formatting/parsing, `MonetaryContext`), `Bodu.Financial.Currencies` (the entire currency surface - `ICurrency`, the ISO 4217 `CurrencyCode` catalogue and per-currency tag types, `CurrencyInfo`, `CurrencyRegistry`, `CurrencyLookupService` / `ICurrencyLookup`, `CurrencyResolution`), and `Bodu.Financial.ExchangeRates` (the exchange-rate core - `ExchangeRate` / `ExchangeRate<TBase, TQuote>`, `CurrencyPair`, `RateObservation`, the `IRateProvider` / `IDatedRateProvider` / `IHistoricalRateProvider` contracts, snapshot/lookup types `RateSeries` / `RateBook` / `FixedRateTable` / `FixedDatedRateProvider` / `RateLookupResult` / `RateProvenance` / `DateRangeCoverage`, and the `RateSeriesBuilder` / `RateTableBuilder` editors). Serialization-agnostic - JSON support lives in the companion `Bodu.Financial.Serialization.Json` package. No HTTP machinery - that lives in the `Bodu.Financial.ExchangeRates` package. |
| `Bodu.Financial.Serialization.Json` | `Bodu.Financial.Serialization.Json/` | `System.Text.Json` integration for `Bodu.Financial` - converters (+ factory for `Money<TCurrency>`), `FinancialJsonPolicy`, the `options.AddFinancialJsonConverters()` registration for `Money` / `Money<TCurrency>` / `MoneyBag` / `ExchangeRate` / `CurrencyPair`, and the `services.AddFinancialJson()` DI registration (keyed `JsonSerializerOptions`, key `"Financial"`). Types live in the `Bodu.Financial.Serialization.Json` namespace. Keeps the core library serialization-agnostic (the NodaTime companion-package pattern; same shape as `Bodu.Numerics.Serialization.Json`). |
| `Bodu.Financial.ExchangeRates` | `Bodu.Financial.ExchangeRates/` | Web exchange-rate provider infrastructure (namespace `Bodu.Financial.ExchangeRates`, shared with the core FX types and the per-source packages): the `WebRateProvider` / `PairWebRateProvider<TSeries>` base classes and `WebRateProviderOptions`, plus the fetch machinery - `SingleFlightCoordinator<TKey>`, `FileSystemByteCache<TKey>`, `RateProviderHttpClientFactory`, `PairRateData<TSeries>`, the `IPairRateLoader` / `IPairRateSource<TSeries>` contracts, and `ExchangeRateFormatException`. Referenced by every per-source provider package. |
| `Bodu.Financial.DependencyInjection` | `Bodu.Financial.DependencyInjection/` | DI registration for financial services (currency lookup, named monetary contexts); financial JSON registration lives in the `Bodu.Financial.Serialization.Json` companion. The `AddFinancialService` / builder extension methods and `UseCurrencyResolution` are declared in the `Bodu.Financial` namespace (alongside the `IFinancialServiceBuilder` builder and `FinancialOptions`), so consumers add `using Bodu.Financial;`. |
| `Bodu.Financial.ExchangeRates.<Source>` | `…ExchangeRates.<Source>/` | Per-source exchange-rate provider packages over `WebRateProvider` / `PairWebRateProvider<TSeries>` (from the `Bodu.Financial.ExchangeRates` package), each isolating one feed's dependencies and parsing **and shipping its own DI registration** (uniformly `Add<Source>ExchangeRates`, e.g. `AddBoeExchangeRates`, declared in the flattened `Bodu.Financial.ExchangeRates` namespace alongside the provider types) - there is no longer a per-provider `*.DependencyInjection` package. All provider types (`<Source>RateProvider` / `<Source>RateProviderOptions`) **and their DI registration extensions** share the single flattened `Bodu.Financial.ExchangeRates` namespace. The shared `Bodu.Financial.ExchangeRates.DependencyInjection` package supplies the generic `AddWebRateProvider` machinery (named `HttpClient` + Polly resilience) every provider delegates to. `<Source>` ∈ `Boe` (Bank of England), `Ecb` (European Central Bank), `Rba` (Reserve Bank of Australia), `Yahoo`, `Ofx`, `Xe` (XE.com), `Oanda` (OANDA - anonymous rolling ~180-day window, declared via `RateHistoryAvailability`), `Fixer` (fixer.io - `access_key`, base+quotes), `ExchangeRateHost` (exchangerate.host - `access_key`, source+quotes), `Fred` (St. Louis Fed FRED - `api_key`, per-pair `series_id` map), `Imf` (International Monetary Fund - keyless, daily **USD-anchored Representative Exchange Rates** downloaded from the IMF's monthly tab-separated report; a single-base **bulk** provider over `WebRateProvider` like `Ecb`, not a pair provider). `Fixer`/`ExchangeRateHost`/`Fred` are pair providers over `PairWebRateProvider<TSeries>` and require an API key on their options; `Imf` is a keyless USD-base bulk provider over `WebRateProvider` (USD/X and X/USD only, cross pairs rejected). |
| `Bodu.Financial.ExchangeRates.Caching[.Sqlite/.Distributed]` | `…ExchangeRates.Caching[.Sqlite/.Distributed]/` | Provider-agnostic caching layer: the `CachingRateProvider` read-through decorator and `AggregatingRateProvider` (priority / average strategies, per-pair routing) over the `IRateCache` contract (`StoreFetchedRange` + `DateRangeCoverage`), with in-memory / TOML-file backends in the core package and durable `Sqlite` and shared `Distributed` (`IDistributedCache`) backends as add-on packages. Each package ships its own DI registration (`AddCachedRateProvider` / `AddAggregatedRateProvider`, `AddSqliteRateCache`, `AddDistributedRateCache` / `AddRedisRateCache`, declared in the root `Bodu.Financial.ExchangeRates` namespace) - no separate `*.DependencyInjection` packages - and all backend cache types share the single `Bodu.Financial.ExchangeRates.Caching` namespace. |
| `Bodu.Financial.ExchangeRates.Testing` | `…ExchangeRates.Testing/` | Shared test infrastructure for the exchange-rate provider and cache test projects: the `DatedRateProviderContractTests<TProvider>` and `PairWebRateProviderContractTests<TProvider, TSeries>` contract-test bases, shipped from a conventional `src/` layout. |
| `docs` | `docs/` | DocFX documentation project. |

A separate solution **`Bodu.CodeStyle/Bodu.CodeStyle.sln`** holds the Bodu code-style analyzers, code fixes, and XML-doc formatter (`Bodu.CodeStyle.XmlDocumentation.{Analyzers,CodeFixes,Core}` plus `Bodu.CodeStyle.Test.Common`). It is **not** referenced by `bodu.slnx` - treat it as an independent unit.

Each project has the layout:

```
<Project>/
  src/   # production code, grouped by namespace folder
  test/  # MSTest project mirroring src structure (Bodu.Test has only test/)
```

### Target Frameworks

Every .NET project in the tree multi-targets **`net8.0;net10.0`**. No project names a framework
literally: they all set `<TargetFrameworks>$(BoduNetTargets)</TargetFrameworks>`, and that property
is defined once in `bld/TargetFrameworks.props`, so the supported matrix widens or narrows in a
single place. Three projects are exceptions and stay `netstandard2.0` because their hosts require
it - the Roslyn source generator `Bodu.Text.Formats.Generators`, the MSBuild task
`Bodu.Globalization.Calendar.Build`, and `docs/doc.csproj`. A library that additionally needs
downlevel reach uses `$(BoduMultiTargetFrameworks)` (`net8.0;net10.0;netstandard2.0`).

`net8.0` is retained deliberately: adding a target framework is additive and invisible to existing
consumers, whereas removing `net8.0` would strand everyone still on the .NET 8 LTS runtime. Drop it
only as a deliberate major-version decision.

Three things to know when writing code that must compile on both legs. First, the SDK defines the
`NETx_0_OR_GREATER` symbols cumulatively, so `NET8_0_OR_GREATER` is true on the `net10.0` leg too -
gate genuinely net10-only code on `NET10_0_OR_GREATER`, never on the absence of a lower symbol.
Second, `net10.0` widens the BCL, which can make a Bodu extension method ambiguous with a new
framework overload where it was not before. Such a break surfaces only on the `net10.0` leg, and
only in files that import `Bodu.Extensions` rather than declaring into it - inside the namespace the
enclosing scope outranks an imported one, so the library's own tests can stay green while every
consumer breaks. The first instance was `ArrayExtensions.Reverse<T>(T[])` against .NET 10's new
`System.Linq.Enumerable.Reverse<TSource>(TSource[])`; it was resolved by renaming the whole family
to **`ToReversed`**, which also reads better (the past participle says the source is not mutated,
unlike `Array.Reverse`) and matches both the neighbouring `SpanExtensions.ToReversed` and the BCL's
`To*` convention for materializing a new collection. Note that
`[OverloadResolutionPriority]` cannot fix this class of collision - it is not consulted between
candidates from different declaring types.
Third, the .NET 8 JIT inlines less than the .NET 10 JIT, so an `[AggressiveInlining]` wrapper that costs
nothing on `net10.0` can cost a great deal on `net8.0` inside a heavily unrolled method: each call is one
more inline, and enough of them exhaust .NET 8's per-method inlining budget, after which the method's own
helpers stop being inlined. The scalar BLAKE2 `G` functions are the instance: through Bodu.Core's
`RotateBitsRightUnchecked` their scalar path ran about 3.4 times slower on .NET 8, and exactly as fast
on .NET 10. They therefore rotate through the extension under `NET10_0_OR_GREATER` and call
`BitOperations` directly on `net8.0`. Before routing a hot, unrolled path through a wrapper, compare the
generated code on both legs (`DOTNET_JitDisasm` with `DOTNET_TieredCompilation=0`), and switch on the
framework only where the two legs actually differ.

Compiling and testing the solution requires the **.NET 10 SDK**: the sources use C# 14 language features and the `.slnx` solution format, neither of which the 8.0 SDK supports. The repository-root `global.json` pins the exact SDK version, and CI installs that version too (see **Reproducing what CI runs** below). Its `rollForward: patch` accepts a later patch in the same feature band only when the pinned version itself is not installed. The separate `Bodu.CodeStyle` solution pins its own SDK via `Bodu.CodeStyle/global.json` and is unaffected.

Nullable reference types are enabled everywhere. `ImplicitUsings` is enabled across all projects, including `Bodu.Core`. Test projects have `ImplicitUsings` enabled and pre-import MSTest via `<Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />`. `Bodu.Core/test/Bodu.Core.Test.csproj` additionally pre-imports `Bodu.Test.Assertions.ExceptionAssert` statically so the shared `AssertGuard(...)` call resolves unqualified across all `ThrowHelperTests.*.cs` partial files.

## Key Types

- **Bodu.Core**: `PooledBufferBuilder`, `WeekPattern`, `ThrowHelper`, the extension surfaces, the `Bodu.Threading` async primitives, `SequenceGenerator`; the `Bodu.Functional` seam (`Memoizer`, and the railway primitives `Option<T>` / `Result` / `Result<T>` / `ResultError` / `Either<TLeft,TRight>` with their Task-based async extension companions); text-encoding utilities in the `Bodu.Text` namespace (`EncodingDetection`, `EncodingExtensions`, `StringEncodingExtensions`); and, internal and shared with the cryptography cores through `InternalsVisibleTo`, the unchecked rotations - the scalar `NumericExtensions.RotateBitsLeftUnchecked` / `RotateBitsRightUnchecked`, and the vector `VectorExtensions.RotateBitsLeftUnchecked<TIsa>` over `Vector128<uint>` / `Vector256<uint>` / `Vector256<ulong>` / `Vector512<uint>`, whose `TIsa` is one of the per-instruction-set `VectorRotation` structs (`Ssse3`, `AdvSimd`, `Avx2`, `Avx512`, behind `IVector128Rotation` / `IVector256Rotation` / `IVector512Rotation`), so a kernel generic over it is compiled, and testable, once per instruction set. A rotation that is not a byte shuffle is a pair of the portable shifts (`Vector256.ShiftLeft(value, count) | Vector256.ShiftRightLogical(value, 32 - count)`), not the instruction set's intrinsics: .NET 10 compiles both to immediate shifts, but .NET 8 does not fold `(byte)(32 - count)` into an intrinsic's immediate, passes the count from memory, and the ChaCha20 and Salsa20 kernels spilled more and ran 9-18% slower on that form.
- **Bodu.Collections**: `CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey, TValue>`, `IndexedSet<T>`, `IndexedPriorityQueue<TElement, TPriority>`, `SequencedDictionary<,>`, `MultiValueDictionary<,>`, `Multiset<T>`, `OrderedSet<T>`, `RangeDictionary<,>` / `RangeSet<T>`, `SegmentedBuffer<T>`, `Graph<T>` / `GraphAlgorithms` / `DisjointSet`, `Tree<T>` / `Trie` / `Trie<TValue>`, `BiDictionary<TKey,TValue>`, `LayeredDictionary<,>` / `DefaultingDictionary<,>`, `Table<TRow,TColumn,TValue>`, `NavigableSet<T>` / `NavigableDictionary<,>`, `IntervalTree<T>` / `IntervalTree<T,TValue>`, `AhoCorasickAutomaton` (+`<TValue>`), `RadixTrie` / `RadixTrie<TValue>`; plus the `Bodu.Collections.Probabilistic` sketches `BloomFilter<T>` (approximate membership, no false negatives), `CountMinSketch<T>` (approximate frequencies, never underestimates), `HyperLogLog<T>` (approximate distinct counts, ~1.04/√m standard error).
- **Bodu.Collections.Concurrent**: `ConcurrentCircularBuffer<T>`, `ConcurrentHashSet<T>`, `ConcurrentEvictingDictionary<TKey,TValue>` - the thread-safe variants, split from `Bodu.Collections` - plus `ConcurrentLruCache<TKey,TValue>`, which has no non-concurrent peer: the read-optimized bounded cache (lock-free reads, approximate LRU) that trades the evicting dictionary's exactness, TTL, and single-flight `GetOrAdd` for read throughput.
- **Bodu.Collections.Specialized**: `BitSet` (growable packed bit set, Java semantics).
- **Bodu.Numerics**: `Fraction<T>`, `BigDecimal`, `Complex<T>` (a generic complex number over `IFloatingPointIeee754<T>`, the generic counterpart of the `double`-only `System.Numerics.Complex`), `Interval<T>` / `DiscreteInterval<T>` / `IntervalSet<T>` and the `IntervalPair<T>` / `DiscreteIntervalPair<T>` result types (each with a `ToIntervalSet()` bridge), plus the statistics aggregates (`RunningStatistics<T>` / `RunningQuantile<T>` / `MovingSum<T>` / `MovingMinMax<T>`). JSON support lives in the companion **Bodu.Numerics.Serialization.Json** (`AddNumericsJsonConverters`, `NumericsJsonPolicy`, the per-type converters/factories, `FractionJsonExtensions`).
- **Bodu.IO.Hashing**: `Fletcher16` / `Fletcher32` / `Fletcher64`, `Crc`, `CrcStandard`(s), `CrcLookupTableCache`, `BlockNonCryptographicHashAlgorithm<T>`, `IResumableHashAlgorithm`, the FNV (`Fnv1a32` / `Fnv1a64`) and Adler (`Adler32` / `Adler32C` / `Adler64`) families, check-digit algorithms (`Luhn`, `Iban`, `Isbn10` / `Isbn13`, `Ean8` / `Ean13`, `Gtin14`, etc.).
- **Bodu.IO.Biff** (namespace `Bodu.IO.Biff`): the forward-only `ref struct` `BiffReader` (`Read`, `RecordId` / `RecordType` / `RecordLength` / `ValueSpan` / `Header` / `RecordStartIndex`, `Version` / `CodePage`, `TryReadContinuation`, `CurrentState` / `BytesConsumed` with the `(data, isFinalBlock, state)` constructor for resumption, and the typed accessors `GetBof` / `GetBoundSheet` / `GetDimensions` / `GetRow` / `GetNumber` / `GetRk` / `GetMulRk` / `GetLabel` / `GetLabelSst` / `GetRString` / `GetBoolErr` / `GetBlank` / `GetMulBlank` / `GetFormula` / `GetString` / `GetXf` / `GetFormat` / `GetFont` / `GetCodePage` / `GetDateMode` / `GetFilePass` / `GetSstHeader`) with `BiffReaderOptions` / `BiffReaderState`; `BiffSstReader` (`Read(ref reader)`, `Current` / `IsFragmented` / `GetString` / `CopyTo`); the `ref struct` `BiffWriter` over `IBufferWriter<byte>` with `BiffWriterOptions` (`WriteRecord` / `WriteContinuedRecord`, `WriteBof` / `WriteEof`, the cell and globals writers, `WriteSst` with `CONTINUE` splitting, `BytesCommitted` / `OpenSubstreamDepth`); the typed `Biff*Record` structs (`ref struct` when they carry text or spans, `record struct` otherwise) with `BiffRkCell` / `BiffCachedResultKind` / `BiffEncryptionType`; `BiffString` (span-backed BIFF8 Unicode / BIFF5 code-page text view); `BiffRk` (`Decode` / `TryEncode`); the vocabulary `BiffRecordType` / `BiffVersion` / `BiffSubstreamType` / `BiffSheetState` / `BiffSheetType`, `BiffRecordHeader` (`TryParse` / `WriteTo`), `BiffLimits`; exceptions `BiffFormatException` (with `Offset`) / `BiffUnsupportedVersionException` (with `RawVersion`). Internal helpers `BiffPayload` (bounds-checked reads) and `BiffTextEncoding` (code-page normalization and resolution).
- **Bodu.IO.Compound** (namespace `Bodu.IO.Compound`): `CompoundFile` (`Open` to read, `Open(stream, FileMode, FileAccess)` to edit with `Commit` / `Revert`, `Create` plus the `CompoundEntryBuilder` / `CompoundStreamBuilder` API to author), `CompoundStorage`, `CompoundStream` (read-only or writable cursor per the open mode), `CompoundEntryInfo` / `CompoundEntryType` / `CompoundEntryColor`, `CompoundFileOptions` (`CompoundReadStrategy` / `CompoundValidationLevel`), `CompoundFileVersion`, the OLE property-set surface (`OlePropertySet` / `OlePropertySection` / `OlePropertyType` / `SummaryInformation` / `DocumentSummaryInformation`), and the exception hierarchy `CompoundFileException` / `CompoundFileFormatException` / `CompoundFileSerializationException` / `CompoundStreamNotFoundException` (with the `CompoundFileError` category).
- **Bodu.IO.Pst** (namespace `Bodu.IO.Pst`): the disposable read-only session `PstFile` (`OpenRead(path/Stream)`, `Open(Stream, options)`, `IsPstFile`; `Format` / `CryptMethod`, `EnumerateNodes`, `GetNode` / `TryGetNode`) with `PstFileOptions` (`PstValidationLevel` Compatible/Strict/Minimal); `PstNode` (`ReadAllBytes` / `OpenDataStream`, `EnumerateSubnodes` / `TryGetSubnode`, and the LTP views `ReadPropertyContext` / `ReadTableContext`), the LTP surface `PstPropertyContext` (tag-ordered `IReadOnlyCollection<PstPropertyValue>`) / `PstPropertyValue` (raw wire type + resolved payload, typed accessors) / `PstTableContext` (`Columns` / `RowCount` / streaming `EnumerateRows` / `TryGetRow`) / `PstTableColumn` / `PstTableRow`; `PstNodeInfo`, `PstNodeId` (5 type bits + 27-bit index; well-knowns `MessageStore` / `NameToIdMap` / `RootFolder`) / `PstNodeType`, `PstFileFormat` / `PstCryptMethod`; exceptions `PstFileException` / `PstFileFormatException` / `PstUnsupportedFormatException`. The NDB and LTP record layers are **internal** under namespace `Bodu.IO.Pst.Internal` (`PstHeader`, `PstSource`, `PstBTree`, `PstDataTree`, `PstSubnodeTree`, `PstCrypt` §5.1/§5.2 decoders, `PstCrc` §5.3 checksum, `PstBref` / `PstNbtEntry` / `PstBbtEntry`; LTP: `PstHeapNode` / `PstHnid` / `PstBTreeOnHeap` / `PstWireType` / `PstLtpContext` / `PstPropertyContextReader` / `PstTableContextReader`).
- **Bodu.Formats.Excel.Binary** (namespace `Bodu.Formats.Excel` - the package/assembly name keeps the `.Binary` suffix, but the namespace is flattened to the `Bodu.Formats.Excel` domain so a future Excel-format package can share the value model, mirroring the `Bodu.Financial.ExchangeRates` convention): the disposable read-only session `ExcelBinaryWorkbook` (`OpenRead(path/FileInfo/Stream)`, `Open(Stream, options)`, keeps the container open and seeks to each sheet's `lbPlyPos`) with `ExcelBinaryReaderOptions`; the forward-only `ExcelWorksheetReader` (`TryReadCell` / `ReadCells` / `ReadRows`) as the primary surface and the materialized `ExcelWorksheet` / `ExcelRow` as the convenience surface; `ExcelWorksheetInfo` (name/index/`ExcelSheetVisibility`/`ExcelSheetType`/dimensions) / `ExcelWorksheetDimensions`, `ExcelWorkbookProperties` (flattened document fields only - the raw property sets are not exposed), `ExcelCell` / `ExcelCellKind` / `ExcelErrorCode`, `ExcelCellReference`, `ExcelSerialDate` / `ExcelDateSystem`; exceptions `ExcelBinaryFormatException`, `ExcelBinaryUnsupportedException`, `ExcelBinaryEncryptedWorkbookException`, `ExcelBinaryWorkbookStreamNotFoundException`. Record framing and decoding come from the `Bodu.IO.Biff` codec (`BiffReader` / `BiffSstReader` / the typed `Biff*Record` structs); the Excel-specific layer is **internal** under namespace `Bodu.Formats.Excel.Biff` (`BiffWorkbookGlobals` parses the globals substream, `BiffSubstreamLoader` copies one BOF…EOF substream from the container stream, `BiffDimensionsScanner` reads a sheet's used range without loading its cells, `ExcelCellMapper` projects decoded cell records onto `ExcelCell`, `BiffFormatTable`, `BiffSheetDirectoryEntry`) plus the internal `ExcelNumberFormat` date classifier. Codec exceptions are translated at the boundary: `BiffFormatException` becomes `ExcelBinaryFormatException` and `BiffUnsupportedVersionException` becomes `ExcelBinaryUnsupportedException`, each keeping the codec exception as `InnerException`.
- **Bodu.Formats.Outlook** (+ **Bodu.Formats.Outlook.Msg**; both in the flattened namespace `Bodu.Formats.Outlook`): the shared MAPI value model - `MapiPropertyTag` (32-bit tag: 16-bit id + `MapiPropertyType`, with `IsMultiValued` / `IsNamed`), `MapiProperty` / `MapiPropertyCollection` (tag-addressed, typed accessors), `MapiNamedProperty`, the curated `MapiPropertyIds`, `OutlookRecipientType` / `OutlookAttachmentMethod`, `OutlookFormatException` - and the `.msg` reader: the disposable session `OutlookMessage` (`OpenRead(path/Stream)`, `Open(Stream, options)`, `IsMsgFile`; `Properties`, `Recipients`, `Attachments`, named-property lookup, scalar and body conveniences incl. MS-OXRTFCP-decompressed RTF) with `OutlookMessageReaderOptions`, `OutlookRecipient` / `OutlookAttachment` (`OpenContentStream` / `OpenMessage` for nested messages), `OutlookMsgFormatException`. The MS-OXMSG record layer is **internal** under namespace `Bodu.Formats.Outlook.Msg` (`MsgStreamNames`, `MsgPropertyStreamReader`, `MsgPropertyDecoder`, `MsgEncodingResolver`, `MsgStorageWalker`, `MsgNamedPropertyMap`, `CompressedRtf`).
- **Bodu.Formats.Outlook.Pst** (namespace `Bodu.Formats.Outlook`): the `.pst` mail-store reader over `Bodu.IO.Pst` - the disposable session `OutlookMailStore` (`OpenRead(path/Stream)`, `Open(Stream, options, leaveOpen)`, `IsPstFile`; `Properties` / `DisplayName` / `RootFolder`, store-wide `TryGetNamedPropertyId` / `TryGetPropertyName`) with `OutlookMailStoreReaderOptions` (`ValidationLevel` / `BlockCacheSize` / `DecompressRtf`), `OutlookMailFolder` (declared counts, streaming `EnumerateSubfolders` / `EnumerateMessages` / `EnumerateAssociatedMessages`; search folders excluded), `OutlookMailMessage` (decoded `Properties`, prefix-normalized `Subject`, scalar conveniences, `BodyText` / `BodyHtml` / `BodyRtf`, `Recipients` as the shared `OutlookRecipient`, `Attachments`), `OutlookMailAttachment` (`Method` / `FileName` / `ContentId` / `MimeTag` / `Size`, `OpenContentStream`, `OpenMessage` for embedded messages with code-page inheritance), `OutlookPstFormatException`. The PST-specific readers are **internal** under namespace `Bodu.Formats.Outlook.Pst` (`PstMapiPropertyReader`, `PstStoreLayout`, `PstNamedPropertyMap`, plus the shared-compiled `MapiValueDecoder` / `MapiEncodingResolver` / `CompressedRtf`).
- **Bodu.Security.Cryptography**: `Threefish256` / `Threefish512` / `Threefish1024`, `Skipjack`, `Blowfish`, `Twofish`, `Camellia`, the Serpent family (`Serpent128Cipher` and the wide-block `Serpent256/512/1024Cipher`, over the internal `SerpentCore`: Osvik's Boolean S-box circuits, generated per word type from Crypto++'s public-domain `serpentp.h` and held to the S-box tables, with Serpent-128's rounds unrolled eight at a time; the wide-block rounds run eight at a time too, each naming its S-box as a type argument (`IRoundSBox` over `SBox0`-`SBox7`) so the circuit is inlined: `EncryptResidentWideBlock` / `DecryptResidentWideBlock` hold Serpent-256's eight words in locals, the word rotation a renaming of them, and `EncryptStreamedWideBlock` / `DecryptStreamedWideBlock` stream any width a four-word group at a time between two copies of the state, each group written one word to the left, behind `EncryptWideBlock` / `DecryptWideBlock`; `SerpentBlockCipher` folds the tweak injected after every fourth round into the key of the round that follows it, so it keeps no tweak schedule; `Serpent128Cipher` re-implements `IBlockCipher.EncryptBlocks` / `DecryptBlocks` explicitly over `Vector256Kernel<TIsa>` (eight blocks) and `Vector128Kernel<TIsa>` (four), rotating through Bodu.Core's `VectorRotation` implementations and transposing with `ChaCha20Core.Transpose`, and implements the internal `ICounterModeBlockCipher.XorCounterKeystream`, through which `CtrModeTransform` (up to the counter's wrap) and `CounterKeystream.TransformBigEndian128` (EAX's and SIV's counter) hand it the counter: the same kernels' `XorCounterBlocks` hold each lane's big-endian 128-bit counter as four native-order words, one vector per word, advance them by masks rather than branches, byte-reverse them into the words Serpent reads, and XOR the keystream into the output as they store it, with a scalar loop for the rest and a partial last block through a stack buffer cleared afterwards; every `SerpentCore` entry point is `NoInlining | AggressiveOptimization` so dynamic PGO cannot inline it into a caller and exhaust the budget; the replaced table-driven code lives on in the tests as `SerpentReference`, and the replaced wide-block rounds as `SerpentWideReference`), `AsconAead128` (with `AsconHash256` / `AsconXof128` sharing the sponge), `Skein256/512/1024`, `Blake2b` / `Blake2s` (compressing through the internal `Blake2bCore` / `Blake2sCore`, which `Argon2Blake2b` shares: scalar kernels with the σ schedule unrolled into constant indices, BLAKE2b's `Vector256Kernel<TIsa>` over `Avx512Isa` / `Avx2Isa` and `Vector128Kernel<TIsa>` over Argon2's 128-bit shims, and BLAKE2s's `Vector128Kernel<TIsa>` over `Avx512Isa` / `Ssse3Isa` / `AdvSimdIsa`), `Blake3` (over the internal `Blake3Core`: an unrolled scalar kernel and a one-block `Vector128Kernel<TIsa>` over the BLAKE2s shims, which also gained a 4×4 `Transpose`, plus many-input kernels that give each lane its own chunk or parent - the same `Vector128Kernel<TIsa>` four at a time, `Vector256Kernel<TIsa>` eight at a time over `Avx2Isa` / `Avx512Isa`, and `Vector512Kernel` sixteen at a time under the `Avx512Wide` kind, selected only where `Vector512.IsHardwareAccelerated` - behind `CompressChunks` / `CompressParents` / `CompressSubtree` / `CompressSubtrees`; `Blake3.HashCore` hashes whole aligned power-of-two runs of chunks as subtrees, and `MaxDegreeOfParallelism`, default `1`, divides a write of 256 KiB or more into 64 KiB parts on threads), `Tiger`, `CubeHash` (over the internal `CubeHashCore`: a scalar kernel, and kernels that hold the whole 1024-bit state for a run of rounds in two 512-bit registers under AVX-512F, four 256-bit under AVX2, or eight 128-bit over `VectorRotation.Ssse3` / `VectorRotation.AdvSimd`, the lower words' exchanges written into which register each result lands in), `SipHash64` / `SipHash128`, `Poly1305` (over the internal `Poly1305Core`: a struct on three 64-bit limbs of 44, 44 and 42 bits, each 64×64 product split at bit 44 as it is formed - the high half from `mulx` / `umulh`, or from `SplitProduct`'s three 64-bit multiplies on processors with neither - which the AEADs also keep on the stack; runs of whole blocks from `Avx2MinimumBytes` go through vector kernels that give each 64-bit lane a block over five 26-bit limbs, multiplying by a power of r per group and by each lane's own power after the last - `Vector256Kernel` four lanes on AVX2, one group at a time (`Blocks`) or, from `Avx2PairedMinimumBytes` where AVX-512VL supplies the 32 registers it needs, two at a time as (h + m)·r⁸ + m′·r⁴ (`BlocksPaired`), and `Vector512Kernel` eight lanes, paired, from `Avx512MinimumBytes` where `Vector512.IsHardwareAccelerated` - with the powers formed per run by the scalar multiply and cleared after it, the thresholds set by a per-kernel sweep on both runtimes, and `Update(KernelKind, …)` letting the tests drive each loop; right shifts are written `>>>` because .NET 8 does not expand a signed shift of 64-bit lanes without AVX-512; the scalar loop `Blocks` is `NoInlining | AggressiveOptimization` and the dispatch for runs from `Avx2MinimumBytes` stays out of line in `KernelBlocks`, because on .NET 10 dynamic PGO inlined each into its callers - the loop into `Poly1305.HashCore`, the dispatch into the AEADs' framing methods - and exhausted their inlining budget), `HashAlgorithmHelper`, AEAD mode transforms (EAX, GCM, OCB, CCM, SIV, GCM-SIV), block-cipher modes; the stream ciphers `ChaCha20` / `XChaCha20` / `Salsa20` / `XSalsa20` over the internal `ChaCha20Core` / `Salsa20Core` (the block functions, plus many-block kernels that keep one state word of 4, 8 or 16 consecutive blocks per vector - `Vector128Kernel<TIsa>` over `VectorRotation.Ssse3` / `.AdvSimd` / `.Avx512`, `Vector256Kernel<TIsa>` over `VectorRotation.Avx2` / `.Avx512`, and `Vector512Kernel` where `Vector512.IsHardwareAccelerated` - every rotation going through Bodu.Core's `VectorExtensions.RotateBitsLeftUnchecked<TIsa>`, sharing `KernelKind` and the transposes (`ChaCha20Core.Transpose` for four blocks, by architecture: SSE2 unpacks or ARM64 zips); the engines expose them through the internal `IBulkStreamCipher.XorKeystreamBlocks`, which `StreamCipherTransform` uses for whole blocks) and the Poly1305 AEADs `XChaCha20Poly1305` / `XSalsa20Poly1305` / `XSalsa20Poly1305Aead` (the internal `Poly1305AeadCore` framings are generic over `IKeystreamSource`, so the sealed AEADs draw each message's keystream from a `ChaCha20Core.Keystream` / `Salsa20Core.Keystream` value on the stack and, holding key and nonce inline, allocate nothing in `Encrypt` / `Decrypt`; each message draws its keystream as it comes - the block that keys Poly1305, the whole blocks in one `XorBlocks` call, the last partial block - unless `Poly1305AeadCore.OnePassBlocks` finds it cheaper, for the kernel `IKeystreamSource.Kernel` reports, to draw all of it, that block included, in one run of up to sixteen blocks through a stack buffer, rounded up where a kernel group makes that cheaper, and a longer message's blocks after its last whole group of four pass through the buffer the same way; the costs are `ChaCha20Core.StepCost` / `CostFor`'s estimates, measured on both runtimes (a four- or eight-block step costs about one call of the block function with AVX-512VL's 32 registers and about two without them), each kernel's plan is computed on its first message and kept, `LanesFor`, `StepCost` and the plan's per-message helpers are `AggressiveInlining` so that the decisions fold for each keystream's kernel, a constant when the framing is compiled (without it .NET 10 called `LanesFor` and divided on every message drawn as it comes), and an engine reports the scalar kernel, on which every way of drawing costs the same, so it is drawn exactly as it comes; a type derived from the public `Poly1305AeadTransform` still goes through its `CreateEngine` engine); asymmetric algorithms over `AsymmetricAlgorithm`: `X25519` (RFC 7748), `Ed25519` (RFC 8032) - both over the internal `Curve25519FieldElement` (five 51-bit limbs with a dedicated square, each 64×64 product split at bit 51 as it is formed: the high half from `mulx` / `umulh`, or from `SplitProduct`'s four 64-bit multiplies on processors with neither) and `Ed25519Point` (dbl-2008-hwcd doubling, with the ref10 point forms nested in it - `CompletedPoint`, `ProjectivePoint`, `AffineNielsPoint`, `ProjectiveNielsPoint` - and mixed additions into completed coordinates; `ScalarMultBase` recodes the scalar into 64 signed radix-16 digits and a carry (`RecodeSignedRadix16`) and adds one multiple per digit from a table of 32 rows of 8 in affine Niels form, selected and negated in constant time (`SelectBaseMultiple`), the odd digits first, then four doublings, then the even digits; the same table forms X25519 public keys through the birational map `EncodeMontgomeryU`; verification's `DoubleScalarMultBaseVartime` recodes its scalars into width-7 and width-5 non-adjacent forms (`ComputeNonAdjacentForm`), adds 32 precomputed odd multiples of B and 8 of the public point formed on the stack, doubles in projective coordinates, and compares points projectively with `AreEqual`; signing and verification hash a message of up to `Ed25519.StackHashMaximumMessageLength` (1 KiB) in one `SHA512.HashData` call over a stack copy, and a longer one incrementally) - `MLKem512/768/1024` (FIPS 203) and `MLDsa44/65/87` (FIPS 204) - over the internal `MLKemEngine` / `MLDsaEngine`, whose Reduction partials replace the remainder operator with Montgomery reduction (R = 2^16 for ML-KEM, 2^32 for ML-DSA) and Barrett-style reductions, whose transforms leave their sums unreduced between layers, and whose ML-DSA `Decompose` uses the reference implementation's multiply-and-shift quotients rather than a division. On AVX2 both engines run their transforms and products through a `Vector256Kernel` selected by a `KernelKind` dispatch (`SelectKernel` / `IsSupported`, every operation also taking the kind explicitly): ML-DSA's over eight `int` lanes, forming each Montgomery product with `vpmuldq` on the even and odd lanes, together with its per-coefficient signing and key set-up passes, polynomial-wide (`HighBits`, `LowBitsNorm`, `MakeHints`, `InfinityNorm`, `AddModQ` / `SubtractModQ`, `ToMontgomery`, `Reduce32`), which also keeps them out of `Sign`'s inlining budget; ML-KEM's over sixteen 16-bit lanes of a packed copy of the `int` coefficients, every intermediate fitting in 16 bits. Both expand their matrices, secret and noise vectors and masks four XOF streams at a time through `KeccakSponge4`, a four-lane SHAKE over `KeccakPermutation.Permute4` (`Vector256Kernel<TIsa>` over `VectorRotation.Avx2` / `.Avx512`, one Keccak state per 64-bit lane, reading its round constants from a `ReadOnlySpan<ulong>` over the assembly's data so that no class-initialization helper leaves its state on the stack), wherever `KeccakPermutation.IsFourWayAccelerated`, and one stream at a time otherwise, with the same coefficients either way; ML-DSA holds its polynomials flat in `ArrayPool<int>` workspaces cleared on return, so signing allocates only the signature. Each keeps the values FIPS 203/204 derive from the encoded keys on every operation in `MLKemKeyMaterial` / `MLDsaKeyMaterial` (the matrix Â, H(ek) or tr, and the key's vectors in the Montgomery form the products use), computed once when the key is set: key generation and ML-DSA private-key import hand over what they have already computed, ML-DSA's NTT(t₁·2ᵈ) by linearity as Â ∘ ŝ₁ + ŝ₂ − t̂₀, and `Clear` zeroes the secret vectors. The engines keep overloads over the encoded keys that derive everything per call; the tests use them as the oracle for the cached paths - X25519/Ed25519 implement the SPKI/PKCS#8 DER and RFC 7468 PEM key formats (via the internal `Rfc8410KeyFormat`); ML-KEM/ML-DSA are raw key encodings only by design. Password KDFs: `Argon2d` / `Argon2i` / `Argon2id` over `Argon2Parameters` (RFC 9106; `MaxDegreeOfParallelism` bounds the threads a derivation divides its lanes among, from about 1 MiB per lane, and `Argon2.Verify` takes the same bound) and `Scrypt` (RFC 7914; its `V` and ROMix scratch live in a native `ScryptCore.Workspace` rented from the same pool as Argon2's matrix, and `MaxDegreeOfParallelism` - default `1`, since each concurrent unit holds its own `V` - runs the `p` units on threads, capped so their `V`s stay within the 2 GiB single-`V` ceiling; `Scrypt.Verify` takes the same bound; ROMix is generic over `ScryptCore.IScryptKernel` - `ScalarKernel`, and `Vector128Kernel<TIsa>` over the `Sse2Isa` / `AdvSimdIsa` lane-rotation shims, which keeps each block in the diagonal word order - dispatched once per unit through the `SimdCapabilities.AdvSimd` / `.Sse2` gates). Argon2's engine is the internal `Argon2Core`: compression kernels behind `IArgon2Kernel` (`ScalarKernel`, `Avx2Kernel`, and `Vector128Kernel<TIsa>` over the `Ssse3Isa` / `AdvSimdIsa` shims), dispatched once per derivation through the `SimdCapabilities.Avx2` / `.AdvSimd` / `.Ssse3` gates, with the matrix in pooled 64-byte-aligned native memory (`Argon2Matrix` over the shared `NativeBufferPool`; the `Bodu.Security.Cryptography.Argon2.DisableMatrixReuse` switch turns retention off for every renter, scrypt included) - the reason the project enables `AllowUnsafeBlocks`. Merkle family: `MerkleTree` (`HashLeaf` / `HashNode` / `BindRoot`, `ComputeRoot` / `ComputeRootOfLeafHashes`, `ComputeBlocked` / `ComputeRootOfBlocks` over `Stream` / `ReadOnlyMemory<byte>` / `ReadOnlySpan<byte>` / `byte[]` with `ComputeBlockedAsync` / `ComputeRootOfBlocksAsync`, `CreateBlockAccumulator`, `AuthenticationPath`, `VerifyInclusion` / `VerifyInclusionOfLeafHash` / `VerifyInclusionBound` / `VerifyBlockInclusion`, `ConsistencyProof` / `ConsistencyProofOfLeafHashes` / `VerifyConsistency`; properties `HashLength` / `FanOut` / `MaxDegreeOfParallelism` / `IsBinary`), `MerkleBlockAccumulator` (`Append`, `Finish` / `FinishBound` / `FinishComputation`, `Reset`, `Dispose`; `Length` / `LeafCount` / `IsFinished` / `RetainsLeafHashes`), `MerkleBlockComputation`, `MerkleTreeDiagnostics` (nested `Node`), and the static block arithmetic `MerkleTree.BlockCount` / `BlockOffset` / `BlockLength`. Verification is total - every malformed input returns `false` and only a null path or proof throws - and `VerifyInclusion`'s `treeSize` is documented as trusted input, with `VerifyInclusionBound` / `VerifyBlockInclusion` as the fail-closed alternatives.
- **Bodu.Text.Encoding**: `Base16`, `Base32`, `Base58`, `Base64`, `Base85`, `BaseFormattingOptions`, `BaseFormatStyles`, variant enums.
- **Bodu.Text.Configuration**: `ConfigurationDocument`, `ConfigurationParseOptions`, `ConfigurationWriteOptions`, `ConfigurationProfile`, view getters.
- **Bodu.Text.Filtering**: `TextFilter` (`Parse` / `Filter` / `IsMatch` over compiled pattern sets), `TextFilterBuilder` (`AddInclude` / `AddExclude`), `TextFilterOptions` / `TextFilterEvaluationMode` (`AnyMatch` set semantics, `LastMatchWins` gitignore rules), `TextFilterPattern` / `TextFilterPatternKind`, `WildcardPattern` (glob compile + match), `TextFilterStatistics` / `TextFilterPatternStatistics` (`GetStatistics()`), `ITextFilterObserver`.
- **Bodu.Text.Delimited** (namespaces `Bodu.Text.Delimited[.Reader/.Writer/.Nodes/.Document]`): `Utf8DelimitedReader` / `Utf8DelimitedWriter`, `DelimitedReaderOptions` / `DelimitedWriterOptions` (dialect policies: `DelimitedFieldCountBehavior`, `DelimitedMalformedRecordBehavior`, `DelimitedDuplicateHeaderBehavior`), `DelimitedSerializer` (+`Options`/`Defaults`; the truly incremental `DeserializeAsyncEnumerableAsync<TRecord>` / `SerializeAsync(IAsyncEnumerable<TRecord>)` streaming pair; and the reflection-free factory overloads over `IDelimitedRecordFactory<TRecord>` + `DelimitedRecordAttribute`), `DelimitedNode`/`DelimitedArray`/`DelimitedObject`/`DelimitedValue`, `DelimitedDocument`/`DelimitedElement`/`DelimitedProperty`, `DelimitedFormatException` / `DelimitedSerializationException`.
- **Bodu.Text.DotEnv** (same namespace layout): `Utf8DotEnvReader` / `Utf8DotEnvWriter`, `DotEnvSerializer` (+`Options`/`Defaults` incl. the SCREAMING_SNAKE_CASE `Web` preset), `DotEnvNode`/`DotEnvObject`/`DotEnvValue` (export-flag-preserving), `DotEnvDocument`/`DotEnvElement`/`DotEnvProperty`, `DotEnvFormatException` / `DotEnvSerializationException`.
- **Bodu.Text.Ini** (same namespace layout): the two readers `Utf8IniReader` (source-order) and `IniDocumentReader` (normalized object-of-objects; applies `IniDocumentOptions` duplicate policies), `Utf8IniWriter`, `IniSerializer` (+`Options`/`Defaults` incl. `Strict` configparser mode and `GlobalSectionName`; and the reflection-free `SerializeSection`/`DeserializeSection` overloads over `IIniSectionFactory<TSection>` + `IniSectionAttribute`), the **trivia-bearing** mutable `IniNode`/`IniObject`/`IniValue` DOM (leading/trailing comments; the one sanctioned deviation from the trivia-free quartet DOMs), the read-only `IniDocument`/`IniElement`/`IniProperty`, `IniFormatException` / `IniSerializationException`. All three line formats wire scalars as **strings**, so scalar conversion stays serializer-local (invariant-culture parse/format) rather than adopting the shared recursive converter engine.
- **Bodu.Globalization.Calendar**: `NotableDateService` / `INotableDateService`, `NotableDateResource` / `NotableDateResourceLoader`, `NotableDateRule`, `NotableDate`, `NotableDateDefinition`, `NotableDateFilter`, `TerritoryCode`; namespace `Bodu.Globalization.Calendar.Algorithms` (`INotableDateAlgorithm`, `EasterCalculator`, `HinduLunarCalculator`, `LunarPhaseCalculator`, `SolarTermCalculator`, and the `IDateCalculationStrategy` implementations); namespace `Bodu.Globalization.Calendar.RangeResolution` (`ResolutionPolicy`, collision/duplicate policies); working-day/date extensions in `Bodu.Extensions`.
- **Bodu.Globalization.Calendar.Data.<Region>** (data bundles): the `<Region>CalendarData` static factory (`SupportedCountries`, `LoadResource(territory)`, `CreateService(territory)`) over the embedded per-country packs; `CommonNotableDateResources` resolves the shared catalogues (`global-core`, `christian-western`, `christian-orthodox`, `catholic`, `global-islamic` / `global-islamic-umm-al-qura`, `global-jewish`, `global-buddhist`, `global-hindu`, `global-persian`, …) that the region hubs import.
- **Bodu.Globalization.Recurrence**: `RecurrenceRule` (RFC 5545 `RRULE` over `IParsable`/`ISpanParsable`/`IFormattable`, the full `FREQ`/`INTERVAL`/`COUNT`/`UNTIL`/`WKST` + `BY*` model, occurrence enumeration for `DAILY`-`YEARLY`), `RecurrenceRuleBuilder`, `RecurrenceSet` (`RDATE`/`EXDATE` composition, iCalendar property-block parse/format round-trip, value equality), `CronExpression` (documented 12-year search horizon), `AnchoredInterval` (occurrences at `anchor + k·interval` for `k ≥ 1`, anchor passed per query, RFC 5545 §3.3.6 duration text), `WeekDayNum`, `RecurrenceFrequency`, `CronFormat`. All four schedule forms answer `GetNextOccurrence` / `GetPreviousOccurrence` (inclusive flags, `DateTime` + `DateTimeOffset`) and carry defect-naming `TryParse` overloads.
- **Bodu.Test** (test infrastructure): `IKat`, generic KAT records (`ValidKat<TInput,TExpected>`, `InvalidKat<TInput>`, `RoundTripKat<TValue,TWire>`, `BinaryKat<TInput,TExpected>`, `GuardValidKat<T>`, `GuardInvalidKat<T>` - the domain-shaped `EnumerableKat`, `BinaryEncodingKat`, and `InvalidEncodedTextKat` live alongside their consumers per the Test Consolidation section below), `KatDisplayName` helper, `ExceptionAssert` (with `ThrowsExactlyWithParamName<T>` and `AssertGuard`), MSTest tier constants in `TestCategories`, reusable stream mocks under `Bodu.Test.IO`.

## Build & Tooling

- Shared MSBuild configuration lives in `bld/Bodu.props` (Authors, MIT licence, deterministic builds, package metadata, doc-comment warnings as errors - e.g. CS1591).
- `.editorconfig` lives at the repository root (`/.editorconfig`, `root = true`) and drives formatter and code-style settings for the entire tree.
- Analyzers in use: **StyleCop.Analyzers**, **Roslynator.Analyzers**, **Microsoft.CodeAnalysis.NetAnalyzers**, **AsyncFixer**, **VisualStudio.Threading.Analyzers**. Treat analyzer warnings as actionable - fix rather than suppress unless there is a strong reason.
- Licence header template: `Bodu.sln.licenseheader` (carries `company="Bodu Pty. Ltd."`, matching `stylecop.json:companyName` - preserve the banner exactly as used in existing files).
- `.filenesting.json` nests partial-class files: any `<Base>.<Part>.cs` file nests under `<Base>.cs`. Keep partial splits consistent with this pattern.
- CI: `.github/workflows/docfx-build-publish.yml` builds the DocFX site on every pull request and push to `master` or a `v*` tag, through `bld/docs/build-api-docs.sh` (see **API documentation pipeline** below), and publishes it to the `gh-pages` branch: `/dev/` from `master`, the root and `/<series>/` from a release tag.

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

**Reproducing what CI runs.** Five differences make a green local run a weaker signal than it looks,
and the first two have bitten:

- **CI uses `test.runsettings`, not `bvt.runsettings`.** `test.runsettings` excludes only `Stress`
  and `Census`, so it *includes* the Regression tier; `bvt.runsettings` excludes Regression as well.
  A Regression-only failure is invisible to a BVT run. To match CI:
  `dotnet test bodu.slnx --settings test.runsettings`.
- **Both target frameworks need their own runtime installed.** With only the .NET 10 runtime present,
  the `net8.0` leg still *runs* - on .NET 10, via roll-forward - so it compiles per-framework but
  executes against the wrong BCL, and a net8/net10 behavioural difference can pass locally and fail
  in CI. Install the .NET 8 runtime and leave `DOTNET_ROLL_FORWARD` unset when the result is meant to
  mean anything about the `net8.0` leg.
- **Some code runs only on ARM64.** `build-test.yml`'s `build-test-arm64` job runs the two
  cryptography test projects on GitHub's hosted ARM64 runner, the only place the Argon2 AdvSimd shim
  executes; on x64 its tests report inconclusive. A green x64 run says nothing about that path.
- **CI treats warnings as errors.** `build-test.yml` builds every test project with
  `-p:TreatWarningsAsErrors=true`, so any compiler or analyzer warning - StyleCop, Roslynator, the .NET
  analyzers, the BODU XML-doc rules - fails the pull request, and `release.yml`'s test gate builds the
  same way, so the same warning stops a release. The release's pack step goes further and also fails on
  NuGet restore and pack warnings, so a newly published advisory for a dependency stops a release until
  the dependency is updated or that advisory is suppressed with a `NuGetAuditSuppress` item giving the
  reason. A local build still reports all of these as warnings. To match CI:
  `dotnet build <project> -c Release -p:TreatWarningsAsErrors=true`, and for the release's pack,
  `dotnet pack bodu.slnx -c Release -p:BoduShipping=true -p:TreatWarningsAsErrors=true`. A deliberate
  finding is suppressed at its site with a `Justification`, as the existing suppressions are.
- **CI builds with the SDK that `global.json` pins.** Every workflow except `Bodu.CodeStyle`'s,
  which builds against that solution's own `global.json`, installs exactly the pinned version, so
  CI's analyzers change only when the pin does. Dependabot (`.github/dependabot.yml`) proposes each
  new .NET 10 SDK as a pull request that bumps the pin, so any warning the new analyzers add fails
  that pull request and is fixed there. A local build uses the pinned SDK once it is installed; a
  different one can disagree with CI in either direction (`CA1873`, for one, reports different
  sites under 10.0.100 and 10.0.401).
  `dotnet --version` at the repository root names the SDK in use, and
  `dotnet-install.sh --jsonfile global.json --install-dir <dir>` installs the pinned one beside the
  others.

**Restore and build must agree on the configuration.** Fifteen projects - the benchmarks, the AOT
smoke app, the `Calendar.Tool` / `.Build` toolchain, and three samples - are excluded from the *Debug*
solution configuration in `bodu.slnx` (`<Build Solution="Debug|*" Project="false" />`). `dotnet
restore bodu.slnx` defaults to Debug and therefore skips them, while `dotnet build bodu.slnx -c
Release` builds them, so the pair

```bash
dotnet restore bodu.slnx                        # Debug: 15 projects skipped
dotnet build   bodu.slnx -c Release --no-restore # ... which then fail with NETSDK1004
```

fails on projects that are perfectly well configured. Either let the build restore implicitly (plain
`dotnet build bodu.slnx -c Release`, which is what CI's per-project steps effectively do), or pass the
configuration to both: `dotnet restore bodu.slnx -p:Configuration=Release`. The exclusions are
deliberate - they keep the benchmarks and toolchain out of ordinary Debug builds - so this is a
usage constraint, not a defect in the solution file.

A project that runs the toolchain has to be excluded from Debug along with it. A solution build does
not build a reference that the active configuration excludes, so in Debug the `CompileNotableDatePack`
task assembly under `Bodu.Globalization.Calendar.Build/src/bin/Debug` never exists, and the consuming
project fails with `MSB4062`, in Visual Studio and in `dotnet build bodu.slnx` alike. That is why the
`RulePackToolchain` sample is one of the three samples excluded.

**After the target-framework list changes, restore before building.** Every
`obj/project.assets.json` records the frameworks it was restored for. Change a project's
`TargetFrameworks` - or pull a change that does, as the `net8.0` → `net8.0;net10.0` retarget did -
and every assets file written by an earlier restore is stale, so the build fails with `NETSDK1005`
naming whichever leg the assets file lacks:

```text
error NETSDK1005: Assets file '...\obj\project.assets.json' doesn't have a target for 'net10.0'.
```

A plain `dotnet restore` rewrites the assets files in place and clears it; deleting `obj/` and
`bin/` is not required. Two things are worth knowing about the failure mode:

- **A restore scoped to one framework poisons the project for every other leg.** Restoring with a
  `TargetFramework` global property in scope (`dotnet restore -p:TargetFramework=net10.0`, or a
  build that leaks the property into a reference) writes an assets file containing *only* that
  framework. The next full build then reports `NETSDK1005` for the legs it dropped - including
  `netstandard2.0` on the analyzer and generator projects - which reads like a repository defect
  and is not one. An unqualified `dotnet restore` fixes it.
- **Visual Studio needs its own cache cleared as well.** Alongside `NETSDK1005` it reports
  `MSB4057: The target "ResolveProjectReferences" does not exist in the project`. That target is
  defined only for a project's *inner* (per-framework) build; the outer cross-targeting build of a
  multi-targeted project genuinely does not have it. Seeing it means the IDE is still addressing
  these projects as single-targeting from cached state written before the retarget. Close the
  solution, delete `.vs/`, reopen, and let the restore run.

### API documentation pipeline

The API reference is generated from the **compiled Release assemblies**, once per framework in
`$(BoduNetTargets)`, and merged into **one page per API** that says which frameworks it applies to
and which package, at which version, it ships in. `bld/docs/build-api-docs.sh` runs the stages, in
the same order locally and in CI:

```bash
python3 -m venv docs/obj/venv && docs/obj/venv/bin/pip install -r bld/docs/requirements.txt
export PYTHON=docs/obj/venv/bin/python          # docs/obj is git-ignored
bash bld/docs/build-api-docs.sh all             # assemblies metadata merge build validate
bash bld/docs/build-api-docs.sh test            # the pipeline's own unit tests
bash bld/docs/build-api-docs.sh serve           # browse docs/_site
```

| Stage | What it does |
|---|---|
| `assemblies` | Builds every package `bld/release-manifest.txt` records (published or withheld), less `bld/docs/api-exclusions.txt`, in Release for every framework, with warnings as errors (`bld/docs/Bodu.Docs.Api.proj` over each project's `BoduGetDocsInputs` target in `bld/DocsInputs.targets`). `bld/docs/prepare_api_inputs.py` then checks each build produced its `.dll`, `.xml`, and `.pdb`, resolves each package's id and version **as MSBuild evaluated them** (so a `BoduPackageVersionOverride` shows exactly), and stages each framework's assemblies with the exact reference assemblies the compiler used, and their `.xml`, in `docs/obj/api/input/<tfm>` - which is what lets `<inheritdoc />` resolve framework documentation. |
| `metadata` | `docfx metadata` once per framework, from `docs/docfx.metadata.json` (the shared API settings), into `docs/obj/api/metadata/<tfm>`. |
| `merge` | `bld/docs/merge_framework_metadata.py` unions the frameworks by UID into `docs/api` - the newest framework supplies an API's content, an API only an older framework has is kept - and annotates every item with `frameworks`, every type with `package`, and every namespace with `packages`. It compares every API present in more than one framework (declaration, and the compiler-written XML documentation) and **fails on a difference** not listed, with a reason, in `bld/docs-checks/framework-divergence-allowlist.txt`. The report lands in `docs/obj/api/divergence-report.md` and the CI job summary. |
| `build` | `docfx build docs/docfx.json` with the `default` + `modern` + `templates/bodu` templates. |
| `validate` | `bld/docs/validate_api_site.py` checks the merged metadata and the rendered pages (Applies to, package facts, source links, navigation, landing pages, overlay version), that every link in the rendered site reaches a file and anchor the site has, and that every href in an XML documentation comment anywhere in the codebase that leads into the site (relative, as the API pages render it, or an absolute URL of the published site) does too. DocFX checks neither the links in an `apidoc` overwrite file nor XML-doc hrefs, so link an overview to another namespace with `xref:`, never by its `.md` file. It also fails when a line of documented XML documentation continues a paragraph with a Markdown block marker (see **Punctuation** under Documentation Tone). |

Things to know when changing it:

- **Sources of truth.** Frameworks come from `$(BoduNetTargets)` and are labelled by rule (`net10.0` →
  ".NET 10"); packages from the release manifest; versions from MSBuild. Adding a framework to
  `bld/TargetFrameworks.props` needs no pipeline or template change.
- **C# 14 extension blocks.** The documentation assemblies are built with `BuildingForDocfx=true`,
  which compiles the classic extension-method fallback instead (see the comment in
  `Directory.Build.targets`). That fallback exists only for documentation, but it compiles under
  warnings as errors, so its XML documentation must satisfy the BODU analyzers too.
- **The overlay is thin.** `docs/templates/bodu` overrides only the partials it must (`class.header`,
  `class`, `enum`, `namespace`, `title`) plus its own `bodu.*` partials, `ManagedReference.extension.js`,
  and `public/main.{css,js}` (the framework selector). Each overridden partial's first line names the
  DocFX version it was copied from, and `validate` fails when that differs from the DocFX pinned in
  `docs/.config/dotnet-tools.json` - re-copy the partial from the new version and re-apply the Bodu
  changes when bumping DocFX.
- **YAML.** DocFX writes YAML 1.2; the scripts read and write it through `bld/docs/docfx_yaml.py`,
  never PyYAML's YAML 1.1 defaults (which cannot read DocFX's `name.vb: =`).
- **Generated output is not committed.** `docs/obj/` and `docs/api/` are ignored; `docs/apidoc/*.md`
  (namespace overviews) is the hand-written API content.

### SDK Bootstrap (Claude Code on the web)

`.claude/hooks/session-start.sh` installs the SDK that `global.json` pins, and the .NET 8 runtime the `net8.0` test legs run on, on session start when running in the remote Claude Code on the web environment (`CLAUDE_CODE_REMOTE=true`). It uses Microsoft's `dotnet-install.sh` rather than `apt`, because Ubuntu's archive carries only the 10.0.1xx feature band, and installs into the installation the `dotnet` on `PATH` already uses. It is idempotent - when `dotnet --version` at the repository root resolves the pinned SDK and a .NET 8 runtime is present it installs nothing, so resume / clear / compact sessions pay no extra cost, and a new pin is installed by the next session start.

The hook also repairs the `dotnet-dnceng` plugin that `.claude/settings.json` enables from the `dotnet/arcade-skills` marketplace (`extraKnownMarketplaces` / `enabledPlugins`): the upstream plugin manifest currently fails Claude Code's path validation (its `agents` entry lacks the required `./` prefix), so the hook patches the cached marketplace clone and installs the plugin. Its skills then load from the next session in the container. The repair is a no-op once the manifest is fixed upstream and can be removed at that point.

Local developer machines are untouched (the hook short-circuits when `CLAUDE_CODE_REMOTE` is unset), and the hook is registered via `.claude/settings.json`. Note for local use: until the upstream manifest fix lands, the plugin install triggered by the project settings may fail validation on a local machine; sessions still work, just without the plugin's skills.

## Branching and Commits

- **One branch per session, by default.** Use the branch the harness designates at session start (typically `claude/<topic>-<id>`) and make multiple commits to it as the session progresses. Do not spin up additional branches for each edit, fix, or intermediate step within the same session.
- **Commit incrementally.** Prefer a fresh commit per logical step over batching unrelated changes into one large commit. The branch should accumulate work across the session, not be replaced.
- **Test before fix.** For a defect-driven change, the failing-regression-test commit precedes the fix commit - do not bundle them (see *Test-First for Fixes (Red-Green)* under Test Conventions).
- **Exceptions that justify additional branches:**
  - Resolving conflicts on multiple existing PR branches - each PR has its own remote head that must be checked out and pushed back to.
  - Work that must land on a specific pre-existing branch other than the session branch.
  In these cases, use disposable local branches and delete them once the work is pushed.
- **Push** to the session branch when changes are ready; do not push to `master` directly.

## Test Conventions

- Framework: **MSTest** (`Microsoft.VisualStudio.TestTools.UnitTesting`, `[TestClass]` / `[TestMethod]`). Do **not** introduce xUnit or NUnit.
- Tests live in `<Project>/test/` and are organised as **partial classes** that mirror the source layout - e.g. `CircularBuffer{T}.cs` → `CircularBufferTests.Enqueue.cs`, `CircularBufferTests.Dequeue.cs`. Extend the existing partial class when adding tests for an existing type.
- No shared test base classes; each test is self-contained.

### Test-First for Fixes (Red-Green)

When fixing a bug - or changing behaviour in response to a defect - **write the test before the fix**:

1. **Red.** Add a regression test that reproduces the defect, and run it to confirm it **fails** against the current (buggy) code. A test that has never been seen to fail does not prove it guards anything.
2. **Green.** Only then apply the fix, and confirm the test now **passes** along with the rest of the suite.
3. **Separate commits, test first.** Commit the failing test and the fix as two commits, the test commit preceding the fix commit, so the history documents the reproduction. A red intermediate commit on a feature branch is expected - that is the point; the branch head (the fix commit) is green.

The regression test follows every other convention below (member-named partial file, `<MethodOrProperty>_When…_Should…` naming, the `Verifies that …` summary, `Assert.ThrowsExactly<T>` for exceptions, the correct tier). Where a reference oracle exists - for example `System.Numerics.Complex` for `Complex<T>` - pin the corrected behaviour against it rather than a hand-guessed expected value, so the test cannot bake in the same mistake as the fix.

This rule governs **defect-driven changes**. Net-new features follow the conventions below without the red-first step (there is no pre-existing behaviour to reproduce), though writing tests alongside the implementation is still expected.

### Test File Organisation

Default to grouping tests by the member under test. For a type `Foo`, use partial files named after the public method, property, constructor group, operator, or interface surface being validated.

**Every test type must carry member-named backbone partials for its primary public methods and properties.** This is the rule that `Bodu.Core` and `Bodu.Security.Cryptography` set (e.g. `Blake2bTests.Ctor.cs` / `.HashSize.cs` / `.Key.cs`, `TigerTests.ComputeHash.cs` / `.Variant.cs`) and it is the bar for every test project. A test type is **not** allowed to be organised purely by feature/concern with no member backbone. Beyond the backbone, two - and only two - kinds of non-member partials are permitted: (1) **subject-based** partials for genuinely cross-cutting behavioural contracts that span multiple members (below), and (2) **corpus / vector / spec / fixture** files that are inherently data-driven rather than member-shaped. When a member has a single dominant operation (for example a forward-only reader whose one public method is `Read`), splitting that operation's many concerns into subject partials *is* the member-aligned shape - do not collapse them into one giant file.

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

**`System.Text.Json`-shaped serializers** (`TomlSerializer`, `BencodeSerializer`, `YamlSerializer`, `DelimitedSerializer`, `DotEnvSerializer`, `IniSerializer`, and any future peer) follow this rule explicitly: the backbone is the public operations - `<Type>SerializerTests.Serialize.cs`, `.Deserialize.cs`, and the `.SerializeAsync.cs` / `.DeserializeAsync.cs` overloads - plus `.RoundTrip.cs` as the home for `SerializeDeserialize_*` (round-trip) tests. A method's tests are routed to its backbone file by the test-method name prefix (`Serialize_*` → `.Serialize.cs`, `Deserialize_*` → `.Deserialize.cs`, and so on). Feature areas (`.NamingPolicy.cs`, `.Required.cs`, `.Nullables.cs`, `.ExtensionData.cs`, enum converters, `.Collections.cs`, `.Dictionaries.cs`, object-construction/creation handling, …) are **subject** partials layered on top of that backbone - they are not a substitute for it. Shared model POCOs, `[DynamicData]` providers, and bespoke KAT records live in the root `<Type>SerializerTests.cs`.

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
| **Stress** | `[TestCategory("Stress")]` | Long-running, high-iteration loops that exceed the standard 10-minute session guard. Excluded from BVT **and** Regression; run on demand via `stress.runsettings`. |

Run-settings files at the repository root drive each tier. Every file except `stress.runsettings` caps the session at 10 minutes per assembly (`TestSessionTimeout`); `stress.runsettings` relaxes this to 60 minutes because the stress loops (e.g. the RFC 7748 §5.2 one-million-iteration ladder) run for tens of minutes:

```bash
dotnet test bodu.slnx --settings smoke.runsettings        # Smoke only
dotnet test bodu.slnx --settings bvt.runsettings          # BVT (default build run)
dotnet test bodu.slnx --settings regression.runsettings   # Smoke + BVT + Regression (excludes Stress)
dotnet test bodu.slnx --settings test.runsettings         # legacy alias for regression.runsettings
dotnet test bodu.slnx --settings stress.runsettings       # Stress only (60-minute session guard)
```

Conventions:

- Default a new test to **BVT** by leaving `TestCategory` unset.
- Mark a test **Regression** when it is data-driven over a published vector table, an exhaustive catalogue, or a wide parameter sweep that duplicates structural coverage.
- Mark a test **Smoke** sparingly - one per primary type, exercising the most important public method on a happy-path input.
- Pre-existing `[TestCategory("Stress")]` tags retain their semantics.

### Test Method Naming

Convention: `<MethodOrProperty>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>`

- `When<Condition>` - the input or state under test.
- `_For<TypedCondition>` - optional qualifier for a type/overload variant.
- `Should<ExpectedResult>` - the observable outcome.

Examples:

```csharp
Enqueue_WhenFull_ShouldThrowInvalidOperationException()
Parse_WhenInputIsEmpty_ForNullableInt_ShouldReturnNull()
Capacity_WhenSetToZero_ShouldThrowArgumentOutOfRangeException()
```

### Test Method Documentation

Every test method has an XML `<summary>` starting with **"Verifies that ..."**, describing scenario and expected outcome in 1-2 sentences so intent is clear without reading the body.

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

- **`Bodu.Test.Kat`** namespace - generic KAT (known-answer test) primitives consumed by multiple test projects (or by `ExceptionAssert.AssertGuard` itself): `IKat` (marker interface exposing `Name`), `ValidKat<TInput,TExpected>`, `InvalidKat<TInput>`, `BinaryKat<TInput,TExpected>`, `GuardValidKat<T>`, `GuardInvalidKat<T>`. Plus `KatDisplayName.GetDisplayName(MethodInfo, object?[])` for `[DynamicData(... DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]` wiring so failures show the row's `Name` instead of an opaque index.
- **`Bodu.Test.Contracts`** namespace - the multi-consumer contract test bases: `ParseFormatContractTests<T>` (Bodu.Core.Test + Bodu.Numerics.Test) and the promoted collection contract bases `CollectionContractTests<>`, `SetContractTests<>`, `EnumeratorContractTests<>`, `DebugViewContractTests<>`, `NonGenericCollectionContractTests<>` (with `SyncRootSupported` opt-out), `ReadOnlyCollectionContractTests<>` (Bodu.Collections.Test + Bodu.Collections.Concurrent.Test).
- **`Bodu.Test.Assertions.ExceptionAssert`** - `ThrowsExactlyWithParamName<TException>(action, expectedParamName)` and the `AssertGuard(testName, act, expectedExceptionType, expectedParamName)` matrix helper, plus KAT-aware overloads `AssertGuard<T>(GuardValidKat<T>, Action<T,T,string?>)` and `AssertGuard<T>(GuardInvalidKat<T>, Action<T,T,string?>)`.
- **`Bodu.Test.IO`** namespace - stream mocks (`FaultingStream`, `ThrottledIncrementingByteStream`, `NonSeekableStream`, etc.).
- **`Bodu.Test.TestCategories`** - tier constants (Smoke / Regression / Stress) consumed by every domain test project.

**Lives alongside the consumer (per domain test project)**, each in a `Contracts/` or similar folder under the test project root. Contract bases and KAT records share the local `<area>.Contracts` namespace so subclasses pick them up without an extra `using`:

- **Bodu.Core.Test** (namespaces `Bodu.Buffers` / `Bodu.Collections.Generic.Extensions`): the domain-shaped KATs `BufferCapacityKat`, `BufferWriteKat`, `EnumerableKat<,>`, `WeekPatternParseKat`, `InvalidWeekPatternParseKat`.
- **Bodu.Security.Cryptography.Test** (namespace `Bodu.Security.Cryptography.Infrastructure`): `SymmetricStreamAlgorithmTests<TTest, TAlgorithm>` - abstract base inherited by every stream cipher (ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit, Hc128) covering key/nonce sizing, lifecycle, transform reuse, overlap rules, and disposal. Other test surfaces inherit directly from the local abstract bases: block ciphers extend `BlockCipherTests<TTest, TCipher, TVariant>`, block-cipher transforms extend `BlockCipherTransformTests<TTest, TCryptoTransform>`, and AEAD / hash families use their own per-family bases - no parallel contract layer is duplicated. The retained KAT records are `HashExtensionKat` plus the domain-local `AeadKnownAnswerVector`, `BlockCipherKnownAnswer`, `HashAlgorithmKnownAnswers` + `HashAlgorithmKnownAnswer`, and `KeyedHashAlgorithmKnownAnswer`. The asymmetric families use a hierarchical contract layer mirroring the symmetric one: `AsymmetricAlgorithmTests<TTest,TAlgorithm>` (root: ctor/key-size/import-export/dispose contract, driven by an `AsymmetricAlgorithmSpecification` record) with the operation bases `KeyAgreementAlgorithmTests<,>` (X25519Tests), `SignatureAlgorithmTests<,>` (Ed25519Tests, and via `MLDsaContractTests<TTest,TDsa>` the MLDsa44/65/87 tests), and `KemAlgorithmTests<,>` (via `MLKemContractTests<TTest,TKem>` the MLKem512/768/1024 tests); plus the `HexFieldKatReader` (`Field = value` block format) with the KAT records `KeyAgreementKnownAnswer`, `SignatureKnownAnswer`, `Kem*KnownAnswer`, and `Dsa*KnownAnswer`, and embedded Wycheproof / NIST ACVP vector files (each with a provenance header) driven by the `MLKemAcvpVectors` / `MLDsaAcvpVectors` loaders in the Regression tier; the RFC 7748/8032 vectors are expressed as in-memory KAT rows of the same record types. A companion **`Bodu.Security.Cryptography.Simd.Test`** is a separate test assembly (necessarily so, because the switch is process-wide) that sets the `Bodu.Security.Cryptography.DisableSimd` feature switch via its `runtimeconfig.template.json`, forcing SIMD dispatch off so the AVX-512-accelerated primitives are exercised through their scalar reference paths and must still produce the published digests. The RFC 6962 suite lives here too (`test/Security.Cryptography/`): member-named backbone partials on `MerkleTreeTests` (`.Ctors`, `.ComputeRoot`, `.ComputeBlocked`, `.ComputeBlockedAsync`, `.ComputeRootOfBlocks`, `.ComputeRootOfBlocksAsync`, `.AuthenticationPath`, `.VerifyInclusion`, `.VerifyConsistency`, …) with the subject partials `.FanOut`, `.Parallelism`, `.DomainSeparation`, `.RealAlgorithm`, `.WorkerFaults`, `.Diagnostics`, and the internal-surface partials `.Primitives` (`Mth`, `SplitPoint`, `WalkToHead`, the prefixes), `.LeafHashing` (the stream and parallel leaf loops) and `.LevelFold`, plus the member partials `.BlockCount` / `.BlockOffset` / `.BlockLength` for the static block arithmetic; alongside them `MerkleBlockAccumulatorTests.*` (chunk-boundary sweeps against `ComputeRootOfBlocks`, the FallbackPlan bound-root vectors through seeded random chunking) and `MerkleBlockComputationTests`. Published vectors reuse the shared `ValidKat<int,string>`; the path and consistency rows need the local `MerkleInclusionKat` / `MerkleConsistencyKat` records, because their input is a pair of sizes. Negative coverage is a **systematic mutation matrix** rather than a fixed list (shifted and flipped indices and sizes, wrong/empty/swapped roots, each root injected as a proof step at either end, every step bit-flipped, removed, duplicated and mis-sized), plus seeded malformed-input sweeps asserting only that verification never throws. The Argon2 suite is pinned three ways: RFC 9106 §5, the reference implementation's `test.c` and version 0x10 traces (`Fixtures/Argon2/PhcReference`, read by `Argon2ReferenceKatReader`), and a corpus recorded from the published 1.0.0 package (`Fixtures/Argon2/Bodu100`, `Argon2RecordedCorpusReader`, `KatSourceKind.InternalRegression`). `Argon2Tests.KnownAnswers` is self-contained so the Simd test assembly links it and holds the scalar kernel to the same vectors; `Argon2CoreTests` drives the engine's internal seams - every supported kernel against the scalar kernel on seeded random blocks, each ISA shim against its scalar definition, the vector corpus through each kernel explicitly and across thread bounds, a matrix poisoned with garbage, and a worker fault surfacing unwrapped. The scrypt suite pins the output to RFC 7914 (Sections 8-12) and an 80-row corpus generated with OpenSSL's `EVP_PBE_scrypt` (`Fixtures/Scrypt/OpenSsl`; the N = 16384 rows are Regression), and `ScryptCoreTests` drives the internal seams: each kernel against RFC 7914's Salsa20/8, BlockMix and ROMix vectors and against the scalar kernel on seeded units, ROMix over a garbage-filled `V`, the workspace's round trip through the pool, and a thread sweep over the corpus rows with `p` > 1. `Blake3CoreTests` drives every BLAKE3 kernel explicitly through a minimal specification-shaped hasher, so the official `test_vectors.json` checks the keyed-hash and key-derivation modes `Blake3` does not expose; it holds the many-input kernels to chunk-by-chunk and parent-by-parent scalar compression over every lane remainder and a wrapping counter, parents reduced in place, subtrees to the specification's tree, and the parallel subtree plans to the calling thread's, while `Blake3Tests` checks the public hasher's large, ragged and threaded writes against small-write streaming, which never takes the subtree path. `ChaCha20CoreTests` and `Salsa20CoreTests` pin the block functions to RFC 8439 and the eSTREAM vectors and hold every kernel, driven explicitly, at every block count to past the widest run and across counter wraps and carries, to the block function one block at a time, as they hold each `Keystream` value; `Poly1305CoreTests` hold the three-limb core, and each vector kernel driven explicitly, to a reference implementation over seeded messages, piece-wise feeds, the largest limbs, and runs either side of the dispatch thresholds, and check that the scalar loop, the dispatch and every kernel forbid inlining; the `Poly1305AeadCoreTests` partials hold each framing's value-keystream path to its engine path at every length to 1,299 bytes, and, through a `RecordingKeystream` that reports whichever kernel a test names, every kernel's plan to the engine too, with its draws costing what the plan estimates and less than drawing as it comes wherever they depart from it; they pin `PlanOnePass` / `CheapestRunBlocks` / `KeystreamCost` at chosen lengths, hold the plan kept per kernel to planning afresh, and check that a tampered tag leaves the output unwritten on both paths; and `StreamAeadTransformContractTests<TAead>` runs the AEAD contract - allocation-free `Encrypt` / `Decrypt` included - over the three sealed AEADs and over `EngineBackedXChaCha20Poly1305`, which derives from `Poly1305AeadTransform` the way a type in another assembly must. `Curve25519FieldElementTests` hold the field arithmetic to `BigInteger` arithmetic modulo p at its limb bounds and over seeded values, and `SplitProduct` to `UInt128` products; `Ed25519PointTests` hold the doubling to self-addition, the small-order points included, `EncodeMontgomeryU` and fixed-base X25519 key generation (`Curve25519Tests.ScalarMultBase`) to the ladder, and `AreEqual` to both coordinates; they hold `ScalarMultBase` and `DoubleScalarMultBaseVartime` to the ladder and to `Ed25519PointReference` (the replaced 1.1.0 table of 64 rows of 16 extended points and its unsigned 4-bit windows, kept as the oracle) over scalars that give every window each signed digit, the recodings' bounds and seeded scalars, `SelectBaseMultiple` to a plain lookup for every signed digit of every row, the two recodings to their values, and the mixed additions and projective doublings to the unified addition and doubling; `Ed25519Tests.MessageHashing` holds signing and verification either side of the stack-hash limit to an RFC 8032 reference signer and checks that neither allocates below it, and RFC 8032 TEST 1024 (a 1023-byte message) joins the signing vectors. `CubeHashCoreTests` hold every CubeHash kernel, driven explicitly, to the scalar kernel over seeded states and round counts. `SerpentCoreTests` hold the wide-block rounds, resident and streamed, at every width and at one, two and the variant's own number of eight-round passes, in and out of place, to `SerpentWideReference`, the replaced 1.1.0 rounds over `SerpentReference`'s tables, given the round keys with the tweak folded in, and the wide-block cipher tests hold `Serpent256/512/1024Cipher` to it over seeded keys, tweaks and blocks; `SerpentCoreTests.XorCounterKeystream` drives every counter kernel explicitly over every length to forty blocks with and without a partial block, over carries out of 32, 64 and 96 bits and the wrap at every position in a group, in place and into a longer output, against encrypting each counter block on its own, and `CtrModeTransformTests` / `CounterKeystreamTests` hold CTR, and EAX's and SIV's counter, to block-at-a-time references at every split and carry, with Serpent-128 among the ciphers. `MLKemEngineTests` and `MLDsaEngineTests` hold the reductions at their input bounds, the transforms and the products to `MLKemReference` / `MLDsaReference` (the replaced remainder-operator code, kept as the oracle) and to plain integer arithmetic, with ML-DSA's `Decompose`, and its polynomial `HighBits` and `LowBitsNorm` through every kernel, checked over every coefficient in the Regression tier and the key-generation and import hand-overs held to what the `Expand` methods derive. Every transform, product and polynomial pass is driven per kernel (`ParseSupportedKernel` reports an unsupported one inconclusive), held bit for bit to the scalar kernel and to the reference over edge, run-patterned, signed and seeded polynomials, and its vector entry point checked for `NoInlining | AggressiveOptimization`; the four-way samplers (`SampleMatrix`, `SampleSecretVector`, `ExpandMaskVector`, `SampleNoiseVector`) are held to one stream at a time over every batch remainder and across nonce and counter carries. `KeccakPermutationTests.Permute4` holds each four-way kernel to the scalar permutation over seeded and uniform-but-distinct states, and `KeccakSponge4Tests` hold the four-way sponge to four scalar sponges at every message length below the rate and every split of up to three whole squeezed blocks; the ML-KEM and ML-DSA ACVP suites are linked into the SIMD-off assembly, which also checks that every one of these dispatches takes its scalar path; `MLKemKeyMaterialTests` / `MLDsaKeyMaterialTests` check that every way of setting a key keeps the same values and that `Clear` zeroes the secret ones, and the `KeyReuse` partials of `MLKemContractTests` / `MLDsaContractTests` hold many operations on one key, and key replacement, to the uncached engine.
- **Bodu.Collections.Test** (`test/Collections.Specialized/`): the `BitSetTests` partials.
- **Bodu.IO.Hashing.Test** (namespace `Bodu.IO.Hashing.Contracts`): `NonCryptographicHashAlgorithmContractTests<TAlgorithm>`, `CheckDigitContractTests<TAlgorithm>`, `MultiCharCheckDigitContractTests<TAlgorithm>`; plus the KATs `HashKat`, `HashStreamingKat`, `CrcCatalogKat`, `CheckDigitKat`; and the pre-existing domain-local KATs `NonCryptographicHashKnownAnswer`, `CheckDigitKnownAnswer`, `MultiCharCheckDigitKnownAnswer`, `MultiCharCheckDigitIsValidKnownAnswer`.
- **Bodu.Text.Encoding.Test** (namespace `Bodu.Text.Encoding.Contracts`): `BinaryEncodingContractTests<TEncoding>`; plus the KATs `BinaryEncodingKat`, `InvalidEncodedTextKat`; and the pre-existing domain-local KATs `EncodingKnownAnswerVector` and `EncodingNegativeDecodeVector` (both implement `IKat`, so every `[DynamicData]` site binds them through `KatDisplayName`).
- **Bodu.Text.Delimited.Test** + **Bodu.Text.DotEnv.Test** + **Bodu.Text.Ini.Test**: each quartet library is tested end to end and self-contained, mirroring the Bencode/Toml model - reader token-transcript tests (incl. hardened BOM/line-ending/multibyte and dialect sweeps), writer canonical-byte tests, the serializer backbone partials per the rule above, and DOM tests (INI adds duplicate-policy, global-hoist, and comment-trivia round-trip coverage). Rows that fit `input → expected` reuse the shared `ValidKat<,>` (e.g. the Delimited RFC 4180 Regression corpus and the DotEnv python-dotenv/godotenv-derived conformance corpus both use `ValidKat<string, string[][]>`, with `InvalidKat<string>` for the malformed sweeps); no shared document-format contract base is promoted.
- **Bodu.Text.Formats.Generators.Test**: references the generator with `OutputItemType="Analyzer"` over its own compilation, so the end-to-end tests exercise factories the generator actually emitted at build time (`GeneratedPerson.DelimitedFactory`, `GeneratedServerSection.IniFactory`) - header/key resolution, byte parity with the reflection binders, and round-trips - plus `CSharpGeneratorDriver`-based tests asserting the `BTFG00x` diagnostics against in-memory compilations.
- **Bodu.Text.Bencode.Test** + **Bodu.Text.Toml.Test**: each self-contained library is tested end to end - the ref-struct `Utf8*Reader`/`Utf8*Writer` token surface (canonical round-trips and malformed-input rejection), the `*Serializer` POCO-mapping suite, the `System.Text.Json`-aligned feature surface (serialization callbacks, unmapped-member handling, object-creation/Populate, naming policies, the string/number enum converters, and the full attribute family), and both DOMs (the mutable `*Node` tree and the read-only `*Document`). Full-grammar and malformed-input sweeps are tagged `[TestCategory("Regression")]`. Tests are colocated per library and assert exact canonical bytes/text where applicable; the two libraries are independent and self-contained, so no shared serialization contract base is promoted. Canonical-form rows that reduce to `input → expected string` use the shared `ValidKat<,>` (e.g. Toml's float/string canonical rows); rows carrying delegates or extra selector fields stay local (Bencode's `RoundTripCase` / shape KATs, Toml's `IntCanon` / `DecimalCanon`).
- **Bodu.Text.Configuration.Test**: the `ConfigurationKat` catalogue (`ConfigurationKnownAnswerData`) driven by `ConfigurationKatRunnerTests`. `ConfigurationKat` implements `IKat` (`Name => Title`) and binds through the shared `KatDisplayName`; each runner is split into binary `_WhenValid_…` / `_WhenInvalid_…` methods over the `…Pass` / `…Fail` filtered suppliers rather than branching on `ConfigurationKatOutcome`.
- **Bodu.Globalization.Calendar.Test** (plus the `.Data.*`, `.DependencyInjection`, `.Plugins`, and `.Builder` test projects): the calendar suite uses self-contained `*KnownAnswerTests` classes (e.g. `EasterKnownAnswerTests`, `IslamicCalendarKnownAnswerTests`, `StrategyResolution*KnownAnswerTests`) that drive known-answer vectors directly, with shared XML inputs embedded under `test/Globalization.Calendar/Fixtures/`. The `.Builder` test project validates the fluent authoring API end-to-end by building documents, serializing to XML/JSON, and asserting against the real `NotableDateResourceLoader` / `NotableDateService`. Each `.Data.<Region>` bundle test project follows the `<Region>CalendarDataTests` pattern - `[DataRow]` known-answer vectors that pin every floating or computed holiday (Easter offsets, nth-weekday rules, lunar/Hijri/Hebrew festivals, weekend-substitution shifts) to confirmed published dates, with exact assertions where the date is deterministic and a ±2-day tolerance for moon-sighting/astronomical festivals, plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test. It does not currently promote reusable contract bases or KAT-record types; add them under a local `Contracts/` folder if a second consumer in a different calendar test project emerges.

When adding a new contract base or KAT record, default to colocating it with its sole consumer - only promote it to `Bodu.Test` once a second consumer in a different test project exists.

Conventions:

- **`[DataRow]` is for primitive scalars only** (int, bool, string, enum). Use `[DynamicData]` with a strongly typed KAT record for byte arrays, expected exception types, options objects, parser state, or object graphs.
- **Binary tests** - one `[TestMethod]` asserts one observable outcome. Do not write methods like `_ShouldEitherReturnExpectedOrThrow` that branch on a row flag. Split into separate methods over filtered data sources: typically `_ShouldNotThrowAndReportNothing` (pass rows) and `_ShouldThrowOn<Param>` or `_ShouldThrowExpected` (fail rows).
- Each `[DynamicData]` row should carry a human-readable name (a `Name` field on the KAT record, or the first `testName` parameter on a `[DataRow]`) so failures surface the scenario rather than a row index.
- Keep KAT-record `Name` synthesis sensible: when multiple fields disambiguate the row (e.g. `{Algorithm} {Year} {CalendarKind}`), implement `IKat.Name` explicitly to compose them.

#### The KAT row standard

A KAT row is a single immutable `record` implementing `IKat` (its `Name` drives the failure label). When adding KAT-driven tests, pick the row type by the **shape of the assertion**, preferring the shared `Bodu.Test.Kat` generics over a bespoke record:

| Intent | Row type | Notes |
|---|---|---|
| input → expected result (one direction) | `ValidKat<TInput,TExpected>` | encode/decode, parse, canonicalize-to-string (e.g. Toml's float/string canonical-form rows) |
| value ↔ wire (both directions) | `RoundTripKat<TValue,TWire>` | serializer round-trips; `TWire` is `string` or `byte[]` |
| input → expected boolean / yes-no | `BinaryKat<TInput,TExpected>` | predicates |
| input → throws | `InvalidKat<TInput>` | carries `ExceptionType`, optional `ParamName` / `MessageContains` |
| guard pass / fail matrix | `GuardValidKat<T>` / `GuardInvalidKat<T>` | argument-validation sweeps via `ExceptionAssert.AssertGuard` |

Rules:

- **Reuse a generic when it fits exactly; keep a bespoke record local only when no generic does.** A record stays domain-shaped (and local, in the consumer's `Contracts/` or `*.Kat` folder) when it carries extra fields or delegates the generics cannot express - for example Bencode's `RoundTripCase` / `CollectionShapeKat` (writer/`Func` delegates), Toml's `IntCanon` (a `Func<string>` over multiple write paths) and `DecimalCanon` (an added `TomlByteArrayHandling`-style selector field), or Financial's `RateLookupKat` / `RateDateResolutionKat` (multi-field result rows). A bespoke record must still implement `IKat` and synthesize a meaningful `Name`.
- **Every `[DynamicData]` KAT site wires the shared display name**: `DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName)`, `DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName)` (with `using Bodu.Test.Kat;`). Do not hand-roll a per-runner display-name method when the row implements `IKat` - `KatDisplayName` already returns `IKat.Name`.
- **Pass and fail are separate `[TestMethod]`s over filtered data sources** - never one method that branches on an outcome flag (`if (kat.ExpectedSuccess) …`, `switch (kat.Outcome)`). Provide `…Pass` / `…Fail` supplier properties that filter the catalogue and bind a `_WhenValid_Should…` method and a `_WhenInvalid_ShouldThrow…` method, each asserting one outcome unconditionally.

## Source File Conventions

### File Header

Every `.cs` file begins with the standard banner - preserve the separator lines and the `file=` / `company=` attributes exactly:

```csharp
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
```

### Namespace Style

- Use **file-scoped** namespaces - terminate the namespace declaration with `;` and do **not** wrap the file contents in `{ }`. This applies to every project in the solution; no exceptions.

  ```csharp
  namespace Bodu.Collections.Generic;

  public sealed class CircularBuffer<T> { ... }
  ```

- `Bodu.Core`, `Bodu.IO.Hashing`, and `Bodu.Globalization.Calendar` already follow this convention throughout.
- `Bodu.Security.Cryptography` contains legacy block-scoped nested namespace files. Do not mix styles within a file, but when a file's primary type is being edited for other reasons, convert it to the file-scoped `;` form at the same time.

### File Layout

- **One public type per file.** Every `.cs` file declares exactly one top-level type. Nested / child types must live in separate partial-class files nested under the parent file per `.filenesting.json` (see Build & Tooling).
- **Generic type files.** Every `.cs` file whose primary declared type is generic is suffixed with `{T}` for one type parameter, `{T,T}` for two, `{T,T,T}` for three, using a single literal `T` per parameter position - e.g. `CircularBuffer{T}.cs`, `EvictingDictionary{T,T}.cs`. Partial files for a generic type carry the same infix before the part name: `TypeName{T}.PartName.cs`. Non-generic companion types (extension method classes, factory classes, non-generic overloads) are not renamed. Nested or secondary type declarations within a partial file of a non-generic parent are also not renamed. The `.filenesting.json` configuration nests `Foo{T}.cs` under `Foo.cs` so generic companions visually group with their non-generic counterpart in the IDE. Do not use the full type-parameter name in file suffixes - always use a single `T` per position regardless of the declared parameter name (e.g. `<TKey, TValue>` → `{T,T}`).
- Partial-file naming is `<Base>.<Part>.cs` where `<Base>.cs` holds the root declaration. Examples:
  - `CircularBuffer{T}.cs` ← root
  - `CircularBuffer{T}.Enumerator.cs`, `CircularBuffer{T}.Debug.cs` ← partials/child-type splits
  - `CrcStandard.cs` ← root; `CrcStandard.Catalog.cs` ← auto-generated catalogue partial
- Don't stack unrelated helper types into the same file. If a type only makes sense alongside its parent (private nested enum, internal helper record), split it into a partial file under the parent rather than co-locating it in the root.

### Namespace-Folder Alignment

Folders directly under a project's source root (`src/` or `test/`) map one-to-one to namespaces. The **namespace declared in a file is the source of truth**; its folder is derived from it. Concretely, for a file with namespace `N` in a project whose csproj declares `<RootNamespace>R</RootNamespace>`:

| Case | Folder (relative to `src/`/`test/`) |
|---|---|
| `N` equals `R` | the project root (no folder) |
| `N` starts with `R.` | a **single flat folder** named `N` minus the leading `R.`, dots preserved - e.g. `Bodu.IO.Hashing.CheckDigits` (with `R = Bodu`) ⇒ folder `IO.Hashing.CheckDigits` |

Rules:

- **No nested namespace folders.** A folder must never contain a child namespace folder. `IO.Hashing/` containing `Target/` is wrong - use sibling folders `IO.Hashing/` and `IO.Hashing.Target/`.
- **One namespace per folder.** A folder holds only files whose namespace equals `R` + the folder name. If two namespaces currently share a folder, split them into two flat folders.
- **Derive the folder from the namespace, never the reverse.** When a file's folder and namespace disagree, move the file to the folder its namespace dictates; do not silently rename the namespace to match the folder. (A namespace that looks wrong is a separate, explicit decision.)
- `src/` and `test/` are not namespace components (the csproj sits at `<Project>/src/` or `<Project>/test/`). `<RootNamespace>` is taken from the csproj as-is.

Carve-outs (exempt from the rules above):

- **Asset / embedded-resource folders** may keep their own nested structure and are not required to match a namespace: any folder named `Fixtures` or `TomlTestCorpus`, or ending in `Resources` (calendar `.xml` packs, spec test corpora, `.resx`/`.Designer.cs` pairs, and other `EmbeddedResource`/`None`/`Content` data). These are wired to csproj globs by path, so do not move them.
- **BCL-convention foreign namespaces** live at the project root: files deliberately declared in `Microsoft.*` or `System.*` (e.g. `IServiceCollection` registration extensions in `Microsoft.Extensions.DependencyInjection`).
- **Shared source compiled into more than one project** switches namespace with `#if <SYMBOL>` (the Outlook shared test sources under `Bodu.Formats.Outlook.Msg/test/Formats.Outlook.Msg/`). The folder follows the namespace the *owning* project compiles - the `#else` branch, since the owning project does not define the symbol; the `#if` branch is what the file becomes when linked into the other project. The checker resolves the declaration the same way.

This convention is the dotted-flat reading of `dotnet_style_namespace_match_folder` / IDE0130 (a folder literally named `Collections.Generic` maps to `Bodu.Collections.Generic`). Run `bld/check-folder-namespace-alignment.sh` to verify a project or the whole tree; it encodes the rules and carve-outs above and exits non-zero on any violation.

### Naming

- Private instance fields: `_camelCase`.
- Private static fields: `s_camelCase`.
- Internal (and any other non-private) fields: `PascalCase`, with no prefix. StyleCop's SA1304/SA1307 require it, so `.editorconfig` applies the `_` and `s_` rules to private fields only.
- Prefer `var` where the type is obvious; see **Implicit Typing (`var`)** under C# Code Style Guidelines for the decision cascade.
- No primary constructors on documented public types (they conflict with `<param>` XML documentation).
- Expression-bodied members for methods, properties, and accessors with a small implementation footprint - see **Expression-Bodied Members** below for the required layout.
- Public argument validation goes through the `ThrowHelper.ThrowIf…` members (in `Bodu.Core`) - see **Parameter Validation** below.

## C# Code Style Guidelines

### File and Header Formatting

- Include the copyright header in the standard format with separator banner lines.
- Preserve consistent spacing and alignment within the header.
- Follow the established file presentation style for partial classes and related files.

### XML Documentation

**All documentation must be in US English.**

**All documentation must align to BCL standards.**

**Documentation scope**
- Provide complete XML documentation for **every** member of a declared type - `public`, `protected`, `internal`, **and** `private`. Private members are documented to the same standard as public members.
- The only exception is `<remarks>`: it is optional on private members and should be added only when the private implementation genuinely warrants it (for example, a subtle concurrency protocol, a lock-free state transition, or a non-obvious invariant that aids future maintainers).

**`<summary>`**
- Write a concise, professional summary describing the purpose, intent, or responsibility of the type or member.
- Keep the tone factual and API-consumer focused.
- Do not mechanically repeat the member name.
- Prefer strong verb-led phrasing: *Provides…*, *Gets…*, *Initializes…*, *Attempts to…*, *Returns…*, *Removes…*, *Adds…*.

**`<param>`**
- Add a `<param>` for every parameter.
- Keep descriptions concise - ideally a single line.
- Describe the parameter in the context of the member's behaviour.
- Use `Must not be <see langword="null" />.` style wording only for basic nullability expectations where useful.
- Do not document validation rules, permitted ranges, allowed values, formats, or exceptional conditions in `<param>` text.
- Put validation constraints, boundary rules, permitted values, and failure behaviour in `<remarks>` and/or `<exception>` documentation instead.
- Avoid repeating information that is already expressed by the parameter name unless it improves clarity.
- Prefer neutral descriptions such as “The number of transformation rounds.” over imperative descriptions such as “Specify the number of transformation rounds.”
- For optional parameters, describe their behavioural role rather than restating the default value unless the default has semantic meaning.

**`<returns>`**
- Add `<returns>` for every non-void **method** and **operator**, describing the return value.
- Describe the result in the context of the member's purpose, not merely the raw type.
- **Do not use `<returns>` on a property.** Per Microsoft's C# XML documentation guidance and the C# language specification, `<returns>` documents the return value of a *method declaration*; the value a property represents is documented with `<value>`. Use `<summary>` (and optionally `<value>`) on properties instead - never `<returns>`.

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
- `<value>` is the property counterpart of a method's `<returns>`: it describes the value a property represents.
- Include `<value>` on properties where the semantics require clarification beyond the summary. It is optional - a property whose summary already fully conveys its value needs only `<summary>`.
- Never substitute `<returns>` for `<value>` on a property.

**Property vs. method documentation summary**
- **Properties:** `<summary>` and optionally `<value>`. Never `<returns>`.
- **Methods / operators:** `<summary>` and `<returns>` (for non-void members).

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

#### Punctuation

- **Never use an em-dash or an en-dash**, anywhere written to this repository: code, comments, XML
  documentation, resx strings, Markdown, scripts, workflows, commit messages, and pull request text. Use a
  hyphen: ` - ` (spaced) for an aside, `-` (unspaced) in a range such as `2020-2026` or `net8.0-net10.0`. Often a
  comma, a colon, or parentheses reads better than a dash at all.
- The only exceptions are dashes that are data rather than prose (a test input exercising non-ASCII text, verbatim
  third-party text). Each such file is listed, with its reason, in `bld/policy/dash-allowlist.txt`. Policy
  **BODU-P014** (`bld/check-policy.sh`, run by the Policy Gate and the pre-push hook) fails on a dash in any other
  added line.
- **Never let a Markdown block marker open a continuation line.** DocFX renders XML documentation as Markdown, so a
  wrapped line that begins with `- `, `+ `, `* `, `1. `, `#` or `>` (written `&gt;`) turns the rest of the paragraph
  into a list, heading or quote on the API site. The same holds for hand-wrapped Markdown. The Bodu XML-doc formatter
  never wraps a marker to the start of a line (it keeps it on the line before), and the docs pipeline's `doclines`
  check (**BODU-P015**) fails on one in documented source. In Markdown, end the previous line with the dash instead,
  or reword.

### Inline Comments

- Add inline comments only where they provide real value.
- Use them to explain non-obvious logic, concurrency coordination, lock-free or low-level state transitions, defensive clamping, important sequencing requirements, or why a block exists when it is not self-evident.
- Explain *why*, the protocol intent, or subtle state meaning - not basic syntax.
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
- Wire the resx and Designer into the project's `.csproj` with the standard `EmbeddedResource Update="..."` + `Compile Update="..."` block - see `Bodu.Financial/src/Bodu.Financial.csproj` for the canonical form.
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

Use `{0}`, `{1}`, … for format placeholders and combine via `string.Format(CultureInfo.CurrentCulture, …)` at the throw site. All user-facing resource text - exception messages, validation diagnostics, and similar display strings - is formatted with `CultureInfo.CurrentCulture`; reserve `CultureInfo.InvariantCulture` for wire/serialization, code generation, and round-trippable parsing where the format must not vary by culture. Diagnostics live in code, not in the resource string - keep messages short and free of conditional grammar; let the caller insert the dynamic context.

Analyzer note: **CA1863** ("Use 'CompositeFormat'") is disabled repo-wide in `.editorconfig`. Every site it flags formats a resx accessor on an exception/error path that runs once per failure, so caching parsed formats buys nothing measurable and would freeze resource resolution at type-init. Format resource-backed messages with plain `string.Format(CultureInfo.CurrentCulture, <Domain>ResourceStrings.<Key>, …)` at the throw/error site; do not introduce cached `CompositeFormat` fields for them.

**Cross-file ThrowHelper convention:**

Mirror the partial-file pattern used by `FinancialThrowHelper`:

- Root file `<Domain>ThrowHelper.cs` - `internal static partial class` declaration with the standard StyleCop / Roslynator suppressions inherited from the reference implementations.
- `<Domain>ThrowHelper.CallerExpression.cs` (and a `<Domain>ThrowHelper.NetStandard.cs` companion when the project multi-targets `netstandard2.0`) - holds the actual guard implementations.
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

- Reuse `Bodu.Core`'s `ThrowHelper` catalogue whenever an existing helper fits - only add to the domain helper when no general-purpose member covers the rule.

**Migrating an existing throw site:**

1. Add a resx entry with a precise key per the naming convention above. Use a comment in the resx if context is helpful.
2. Regenerate (or hand-edit, mirroring the existing pattern) the Designer accessor.
3. Replace the literal string at the throw site with `string.Format(CultureInfo.CurrentCulture, <Domain>ResourceStrings.<Key>, …args)` (omit `string.Format` for messages with no placeholders).
4. If the rule now appears in two or more files, promote the entire `if (…) throw …` block to a `ThrowIf*` member on the domain helper and replace the call sites with the helper call.
5. Tests that assert exception message content via `Contains` must continue to find their expected substrings - preserve the `{0}` placeholder arguments that carry the dynamic context (e.g. type name, ISO code, value).

### Implicit Typing (`var`)

Prefer `var` for local declarations where it does not obscure the type, per the root `.editorconfig` (`csharp_style_var_for_built_in_types = true`, `csharp_style_var_when_type_is_apparent = true`, `csharp_style_var_elsewhere = false`). This is the Roslyn **IDE0007** ("use var") rule, enforced at `warning`. Apply this cascade to each local declaration:

1. **Built-in type → `var`.** When the variable's type is a C# built-in (`int`, `uint`, `long`, `bool`, `byte`, `string`, `double`, `char`, …):

   ```csharp
   var count = 5;
   var name = reader.ReadString();
   var a = year % 19;            // matches existing algorithm code in Bodu.IO.Hashing / Bodu.Security.Cryptography
   ```

2. **Type named on the right-hand side → `var`.** When the right-hand side makes the type apparent - `new T(...)`, a cast `(T)x`, `T.Parse(...)`:

   ```csharp
   var buffer = new CircularBuffer<int>(8);
   var node = (BencodeObject)element;
   ```

3. **Otherwise → explicit type.** When the type is neither built-in nor apparent from the right-hand side, keep the explicit type so the declaration stays self-documenting:

   ```csharp
   NotableDateResource resource = loader.Load(territory);   // return type not visible at the call site
   ```

   (`csharp_style_var_elsewhere = false`, left at `suggestion` - advisory, not build-enforced.)

Clarifications:

- **Interface- or base-typed locals stay explicit.** `IList<int> items = new List<int>();` keeps its declared type - `var` would change the static type to `List<int>`. IDE0007 never fires when the declared type differs from the right-hand-side type, so these are safe automatically.
- **Target-typed `new` is unaffected.** `DateRange year = new(start, end);` does not name its type on the right-hand side, so it is *not* an IDE0007 site and is left as-is. For a *new* local, prefer the apparent `var x = new T(...)` form over `T x = new(...)`, but do not churn existing `T x = new(...)` declarations.
- **`foreach`**, **`out` variables** (`out var value`), and **`using` / `await using`** declarations follow the same cascade.
- `var` is required for anonymous types; explicit typing is required wherever the compiler cannot infer the intended type. Generated files (`<auto-generated>` banner, e.g. `*ResourceStrings.Designer.cs`, `CrcStandard.Catalog.cs`) are exempt - the analyzers and `dotnet format` skip them.

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
- Keep style consistent across the file - do not mix documentation styles.
- Avoid excessive comments or overlong XML documentation.
- Extend an established style consistently rather than replacing it.
