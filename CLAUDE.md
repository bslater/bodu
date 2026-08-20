# CLAUDE.md

Guidance for AI assistants working in this repository. Read this file before making changes.

## Repository Overview

**Bodu** is a multi-project C# utility library solution focused on high-performance, well-documented, framework-style building blocks. The solution lives at `bodu.slnx` (Visual Studio's modern solution format — note the `.slnx` extension, not `.sln`). The projects are organised by domain (below). Where several projects share an identical structure and naming — notably the regional calendar **data bundles** (`Bodu.Globalization.Calendar.Data.<Region>`) — they are shown as a single `<…>` wildcard row rather than enumerated individually:

| Project | Path | Responsibility |
|---|---|---|
| `Bodu.Core` | `Bodu.Core/` | Buffers, extension surfaces (incl. the `Bodu.Collections.*Extensions` enumerable/dictionary/list operators), threading primitives (`Bodu.Threading`), sequences and functional seams, text-encoding utilities (`Bodu.Text` namespace: `EncodingDetection`, `EncodingExtensions`, `StringEncodingExtensions`), XML, argument validation helpers (`ThrowHelper`), `WeekPattern`. |
| `Bodu.Collections` | `Bodu.Collections/` | The specialized generic-collection catalogue, split out of `Bodu.Core` (namespaces unchanged; references `Bodu.Core`): `Bodu.Collections.Generic` (circular buffer, deque, evicting dictionary, sequenced/multi-value dictionaries, multiset, indexed/ordered sets, indexed priority queue, range set/dictionary, segmented buffer), `.Generic.Graphs` (graph, algorithms, disjoint set), `.Generic.Trees` (tree, tries), and `Bodu.Collections.Probabilistic` (Bloom filter, count-min sketch, HyperLogLog — approximate sketches over comparer-derived hashing). `ShuffleHelpers` and the internal `SequenceUtility` stay in `Bodu.Core` because the staying `IEnumerableExtensions` partials depend on them. |
| `Bodu.Collections.Concurrent` | `Bodu.Collections.Concurrent/` | Thread-safe collection variants split out of `Bodu.Collections` (namespace `Bodu.Collections.Generic.Concurrent` unchanged; references `Bodu.Collections`): `ConcurrentCircularBuffer<T>` (lock-free Vyukov MPMC ring), `ConcurrentHashSet<T>` (lock-free split-ordered set), `ConcurrentEvictingDictionary<TKey,TValue>` (lock-striped bounded cache: all six eviction policies, optional TTL, single-flight `GetOrAdd`, post-commit `ItemEvicted`). |
| `Bodu.Test` | `Bodu.Test/` | Shared test infrastructure: KAT records (`Bodu.Test.Kat`), assertion helpers (`Bodu.Test.Assertions.ExceptionAssert`), reusable stream mocks (`Bodu.Test.IO`), test category constants (`TestCategories`). Referenced by other test projects. |
| `Bodu.Numerics` | `Bodu.Numerics/` | `Fraction<T>` (rational arithmetic, parse/format, generic math, UTF-8) and the interval algebra (`Interval<T>`, `DiscreteInterval<T>`, `IntervalSet<T>`, the pair result types). Serialization-agnostic — no `System.Text.Json` dependency; XML helpers stay here. |
| `Bodu.Numerics.Serialization.Json` | `Bodu.Numerics.Serialization.Json/` | `System.Text.Json` integration for `Bodu.Numerics` — converters + factories, `NumericsJsonPolicy`, `FractionJsonExtensions`, and the `options.AddNumericsJsonConverters()` registration for `Fraction<T>` / `Interval<T>` / `DiscreteInterval<T>` / `IntervalSet<T>`. Types live in the `Bodu.Numerics.Serialization.Json` namespace. Keeps the core library serialization-agnostic (the NodaTime companion-package pattern). |
| `Bodu.IO.Hashing` | `Bodu.IO.Hashing/` | Non-cryptographic hashing (Fletcher-16/32/64, full RevEng CRC catalogue, check-digit algorithms: Luhn, Damm, ABA, EAN, GTIN, IBAN, ISBN, ISIN, LEI, ISO 7064). |
| `Bodu.IO.Compound` | `Bodu.IO.Compound/` | Reader, editor, and writer for the OLE2 / Compound File Binary (CFB) container format — the structured-storage envelope used by legacy Microsoft Office files (`.xls`, `.doc`, `.msg`). Exposes the embedded named streams without any application-format knowledge, edits existing containers transactionally (BCL-style `OpenStream(name, FileMode, FileAccess)` writable cursors staged until `Commit`, with `Revert`), and authors new containers through a staged builder API. |
| `Bodu.IO.Pst` | `Bodu.IO.Pst/` | Low-level, read-only container reader for the Outlook personal-folders format (PST / MS-PST), Unicode format: the node-database (NDB) layer — header, node/block B-trees, block data with the permute/cyclic content encodings decoded and checksums verified, multi-block data trees, and per-node subnode trees — and the LTP layer over it — heap-on-node, BTree-on-heap, and the public property-context and table-context views on `PstNode` (wire-typed values, no MAPI semantics). The substrate for a future `Bodu.Formats.Outlook.Pst` message reader; no writing. |
| `Bodu.Security.Cryptography` | `Bodu.Security.Cryptography/` | Block ciphers (Threefish 256/512/1024, Skipjack, Blowfish, Twofish, Camellia), AEAD (Ascon), keyed/cryptographic hashes (Skein, BLAKE2, Tiger, SipHash, FNV1a, Adler), asymmetric algorithms (X25519, Ed25519, ML-KEM, ML-DSA), crypto transforms, helpers. |
| `Bodu.Text.Encoding` | `Bodu.Text.Encoding/` | Binary encodings: Base16, Base32, Base58, Base64, Base64Url, Base85 (with variants, formatting options, span/UTF-8 surfaces). |
| `Bodu.Text.Configuration` | `Bodu.Text.Configuration/` | Bodu text configuration parser/resolver (INI-compatible profile, resolver precedence, typed view getters, write options) over its **own** trivia-preserving INI document model (`IniDocumentBase`/`IniDocument`/`IniSection`/`IniEntry`/`IniComment` in the `Bodu.Text.Configuration` namespace) — no format-library dependency. |
| `Bodu.Text.Filtering` | `Bodu.Text.Filtering/` | Include/exclude filtering engine for lists of text values (namespace `Bodu.Text.Filtering`): glob (wildcard, character-class, `{a,b}` brace-alternation) and regex patterns compile once into a cost-tiered `TextFilter` that runs the cheapest strategies first (MatchAll → Literal → Prefix/Suffix → Contains → general wildcard → Regex), with Ant/MSBuild-style include/exclude set semantics (`AnyMatch`) or gitignore-style last-match-wins ordered rules, gitignore-convention list parsing, always-on match statistics, and an optional per-decision `ITextFilterObserver`. Core-only dependency. |
| `Bodu.Text.Serialization` | `Bodu.Text.Serialization/` | The shared serialization core for the `System.Text.Json`-shaped text serializers: the compiled attribute family, ignore/creation/unmapped-member/naming enums, serialization callback interfaces, and naming policies, plus the `shared/**` source (metadata resolver, converter engine) the per-format packages compile under their format symbol. Referenced by `Bodu.Text.Bencode`/`.Toml`/`.Yaml`/`.Delimited`/`.DotEnv`/`.Ini`. |
| `Bodu.Text.Delimited` | `Bodu.Text.Delimited/` | Standalone STJ-shaped Delimited (RFC 4180 CSV/TSV) library — `Utf8DelimitedReader`/`Utf8DelimitedWriter`, `DelimitedSerializer` (incl. `IAsyncEnumerable<TRecord>` streaming), mutable `.Nodes` DOM, read-only `.Document` DOM, dialect policies, csv-spectrum-derived Regression corpus. References `Bodu.Text.Serialization`. |
| `Bodu.Text.DotEnv` | `Bodu.Text.DotEnv/` | Standalone STJ-shaped DotEnv library — `Utf8DotEnvReader`/`Utf8DotEnvWriter` (export prefix, quoting, inline comments), `DotEnvSerializer`, mutable `.Nodes` DOM (export-flag-preserving), read-only `.Document` DOM. References `Bodu.Text.Serialization`. |
| `Bodu.Text.Ini` | `Bodu.Text.Ini/` | Standalone STJ-shaped INI library — source-order `Utf8IniReader` + normalized `IniDocumentReader` (duplicate-section merge), `Utf8IniWriter`, `IniSerializer` (`GlobalSectionName`, depth-2 gate), **comment-preserving** mutable `.Nodes` DOM, read-only `.Document` DOM. References `Bodu.Text.Serialization`. |
| `Bodu.Text.Formats` | `Bodu.Text.Formats/` | Umbrella meta-package (no code): references `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, and `Bodu.Text.Ini`. |
| `Bodu.Text.Formats.Generators` | `Bodu.Text.Formats.Generators/` | Incremental Roslyn source generator (netstandard2.0, not yet packable): emits reflection-free `IDelimitedRecordFactory<TRecord>` / `IIniSectionFactory<TSection>` implementations for `[DelimitedRecord]` / `[IniSection]` partial POCOs (generated static `DelimitedFactory` / `IniFactory` properties), consumed by the serializer factory overloads for trimming/AOT-safe binding. Diagnostics `BTFG001`–`BTFG003`; consumers reference it with `OutputItemType="Analyzer"`. |
| `Bodu.Formats.Excel.Binary` | `Bodu.Formats.Excel.Binary/` | Narrow, read-only reader for the Excel 97-2003 binary workbook format (BIFF8 / `.xls`), built on `Bodu.IO.Compound`. Exposes raw worksheet cell values (strings, numbers, booleans, errors — including a formula cell's cached result and the spreadsheet error code) plus each sheet's declared used range, without formula evaluation, styling, or higher-level interpretation. |
| `Bodu.Formats.Outlook` | `Bodu.Formats.Outlook/` | The shared MAPI value model for the Outlook format readers (namespace `Bodu.Formats.Outlook`, flattened per the `Bodu.Formats.Excel` convention): property tags/types (`MapiPropertyTag` / `MapiPropertyType`), decoded values with the tag-addressed `MapiPropertyCollection`, named-property identities (`MapiNamedProperty`), curated `MapiPropertyIds`, recipient/attachment enums, and `OutlookFormatException`. Container-free — consumed by `Bodu.Formats.Outlook.Msg` and the future `.pst` reader rather than owned by either. |
| `Bodu.Formats.Outlook.Msg` | `Bodu.Formats.Outlook.Msg/` | Read-only reader for the Outlook message format (`.msg` / MS-OXMSG) over `Bodu.IO.Compound`, sharing the `Bodu.Formats.Outlook` value model (public types in the flattened `Bodu.Formats.Outlook` namespace; internal record layer under `Bodu.Formats.Outlook.Msg`). Opens a message as a disposable `OutlookMessage` session: all decoded MAPI properties, recipients, attachments, nested attached messages, named-property resolution, and the text/HTML/compressed-RTF bodies. No authoring, no MAPI session emulation. |
| `Bodu.Text.Bencode` | `Bodu.Text.Bencode/` | Self-contained Bencode (BEP 3) library shaped after the `System.Text.Json` BCL: ref-struct `Utf8BencodeReader`/`Utf8BencodeWriter` (+ `BencodeTokenType`/`BencodeValueKind`), the `BencodeSerializer` POCO mapper (converters, the full attribute family, options, naming policies, callbacks, enum converters), a mutable `JsonNode`-style DOM (`BencodeNode`/`BencodeObject`/`BencodeArray`/`BencodeValue`), and a read-only `JsonDocument`-style DOM (`BencodeDocument`/`BencodeElement`). Folders/namespaces follow the S.T.J source layout: `Bodu.Text.Bencode[.Reader/.Writer/.Document/.Nodes/.Serialization]`. |
| `Bodu.Text.Toml` | `Bodu.Text.Toml/` | Self-contained TOML (v1.0.0 / v1.1.0) library, the same `System.Text.Json`-aligned shape and `Text.Toml.*` folder/namespace structure as `Bodu.Text.Bencode`: `Utf8TomlReader`/`Utf8TomlWriter`, the `TomlSerializer` POCO mapper, and the mutable (`TomlNode`) and read-only (`TomlDocument`) DOMs. TOML's richer value model adds native float, boolean, and the four RFC 3339 date-time kinds, plus `TomlSpecVersion` and `TomlByteArrayHandling`. |
| `Bodu.Text.Yaml` | `Bodu.Text.Yaml/` | Self-contained YAML library and the third `System.Text.Json`-shaped serializer alongside `Bodu.Text.Bencode` / `Bodu.Text.Toml`: it exposes `Utf8YamlReader`/`Utf8YamlWriter`, the `YamlSerializer` POCO mapper, and the mutable (`YamlNode`) and read-only (`YamlDocument`) DOMs. Like its siblings it compiles the shared `Bodu.Text.Serialization/shared/**` source (under the `YAML` symbol): the `MetadataResolver`/`TypeMetadata`/`PropertyMetadata` trio, the structural converter factories (nullable/dictionary/collection/object), the full attribute family, and the non-null `GetConverter` pipeline with `YamlConverter{T}`/`YamlConverterFactory`. Its **scalar converters stay format-local** (`Text.Yaml.Serialization.Converters`) because YAML's implicit typing coerces across scalar kinds (string/integer/float/boolean/null), which the token-strict shared scalar converters cannot express; the DOM↔serializer bridges are adopted (the shared `NodeConverter` under the `YAML` symbol, plus format-local `YamlElement`/`YamlDocument` converters), and the options surface matches its siblings (`DefaultIgnoreCondition`, `YamlSerializerDefaults` presets, `YamlStringEnumConverter`/`YamlNumberEnumConverter{TEnum}`). Ships the same stream/async facade as its siblings (`Serialize<T>(IBufferWriter<byte>)`, `Deserialize<T>(Stream)`, `SerializeAsync`/`DeserializeAsync` — buffered in full; only the stream copy is asynchronous). |
| `Bodu.Extensions.Configuration.Text` | `Bodu.Extensions.Configuration.Text/` | Bridge between `Microsoft.Extensions.Configuration` and `Bodu.Text.Configuration`. |
| `Bodu.Globalization.Calendar` | `Bodu.Globalization.Calendar/` | Resource-driven notable-date engine on the notable-date schema: rule model, date-calculation strategies and astronomical algorithms (`Algorithms`), range resolution (`RangeResolution`), observed-date adjustments, working-day extensions (`Bodu.Extensions`), and `NotableDateService`. |
| `Bodu.Globalization.Calendar.Plugins` | `Bodu.Globalization.Calendar.Plugins/` | Trust-gated external plugin loading for assemblies contributing custom `INotableDateAlgorithm` implementations. |
| `Bodu.Globalization.Calendar.Builder` | `Bodu.Globalization.Calendar.Builder/` | Fluent authoring API (`NotableDateDocumentBuilder`) that constructs notable-date documents on the notable-date schema, with full XML and JSON-subset serialization (`ToXml`/`ToJson`/`Save`) and parsing (`FromXml`/`FromJson`/`Load`); materializes a `NotableDateResource` via the loader. |
| `Bodu.Globalization.Calendar.Caching[.Sqlite/.Distributed]` | `…Calendar.Caching[.Sqlite/.Distributed]/` | Caching layer for the notable-date engine: `CachingNotableDateService`, a decorator over any `INotableDateService` serving computed dates from a per-territory, per-civil-year cache (in-memory or one TOML/JSON file per territory, TTL/resource-version refresh) behind the `INotableDateCache` contract, with the `AddCachedNotableDateService` DI registration in the core package and durable `Sqlite` (`SqliteNotableDateCache` / `AddSqliteNotableDateCache`) and `Distributed` (`DistributedNotableDateCache` over `IDistributedCache` / `AddDistributedNotableDateCache` / `AddRedisNotableDateCache`) backends as add-on packages. |
| `Bodu.Globalization.Calendar.Tool` / `.Build` | `…Calendar.Tool/`, `…Calendar.Build/` | Rule-pack toolchain (not in a shipping wave yet): `Tool` is the command-line compiler/lint for notable-date rule packs (stable `BODU-CAL-*` diagnostics, compiles XML/JSON documents to sealed `.bcal` binary packs); `Build` is the MSBuild integration (`CompileNotableDatePack` task + `NotableDatePack` items) that invokes the tool incrementally during build. |
| `Bodu.Globalization.Calendar.<Region>` | `…Calendar.Data.<Region>/` | Per-region calendar data bundles — one self-contained embedded pack per country (national rules plus ISO 3166-2 subdivisions), each exposing a `<Region>CalendarData` factory and importing the shared catalogues through a `<region>-common` hub. `<Region>` ∈ `Americas` (US, CA, MX, BR, AR, CL, CO, PE), `AsiaPacific` (AU, CN, IN, JP, KR, MY, NZ, SG, ID, TH, PH, VN, HK, TW), `Europe` (28 EU/EEA territories incl. GB, FR, DE), `MiddleEast` (AE, SA, IL, TR, QA, JO), `Africa` (ZA, NG, KE, GH, ET, EG, MA). |
| `Bodu.Globalization.Calendar.DependencyInjection` | `…Calendar.DependencyInjection/` | `IServiceCollection` extensions for registering calendar services; `AddNotableDateService` / `AddReloadableNotableDateService` are declared in the `Bodu.Globalization.Calendar` namespace (consumers add `using Bodu.Globalization.Calendar;`). |
| `Bodu.Globalization.Recurrence` | `Bodu.Globalization.Recurrence/` | Recurrence-rule evaluation, Core-only: `RecurrenceRule` (RFC 5545 `RRULE` parse/format/occurrence enumeration), `RecurrenceRuleBuilder`, `RecurrenceSet` (rules composed with `RDATE`/`EXDATE`, canonical property-block round-trip), `CronExpression` (Vixie five-field / optional-seconds six-field / `@` macros), and `AnchoredInterval` (instant-anchored interval recurrence in the RFC 5545 duration grammar). Every form answers `GetNextOccurrence` **and** `GetPreviousOccurrence` with inclusive flags over `DateTime` and `DateTimeOffset`, and exposes a defect-naming `TryParse(s, out result, out failureMessage)` overload. Pure in its arguments (no wall clock, no machine timezone — enforced by a metadata-scan `PurityTests` guard). A natural sibling of `Bodu.Globalization.Calendar` without depending on it. |
| `Bodu.Financial` | `Bodu.Financial/` | Money and currency primitives across three namespaces: `Bodu.Financial` (`Money` / `Money<TCurrency>` / `CalculatedMoney` / `MoneyBag`, allocation and rounding policies, formatting/parsing, `MonetaryContext`), `Bodu.Financial.Currencies` (the entire currency surface — `ICurrency`, the ISO 4217 `CurrencyCode` catalogue and per-currency tag types, `CurrencyInfo`, `CurrencyRegistry`, `CurrencyLookupService` / `ICurrencyLookup`, `CurrencyResolution`), and `Bodu.Financial.ExchangeRates` (the exchange-rate core — `ExchangeRate` / `ExchangeRate<TBase, TQuote>`, `CurrencyPair`, `RateObservation`, the `IRateProvider` / `IDatedRateProvider` / `IHistoricalRateProvider` contracts, snapshot/lookup types `RateSeries` / `RateBook` / `FixedRateTable` / `FixedDatedRateProvider` / `RateLookupResult` / `RateProvenance` / `DateRangeCoverage`, and the `RateSeriesBuilder` / `RateTableBuilder` editors). Serialization-agnostic — JSON support lives in the companion `Bodu.Financial.Serialization.Json` package. No HTTP machinery — that lives in the `Bodu.Financial.ExchangeRates` package. |
| `Bodu.Financial.Serialization.Json` | `Bodu.Financial.Serialization.Json/` | `System.Text.Json` integration for `Bodu.Financial` — converters (+ factory for `Money<TCurrency>`), `FinancialJsonPolicy`, the `options.AddFinancialJsonConverters()` registration for `Money` / `Money<TCurrency>` / `MoneyBag` / `ExchangeRate` / `CurrencyPair`, and the `services.AddFinancialJson()` DI registration (keyed `JsonSerializerOptions`, key `"Financial"`). Types live in the `Bodu.Financial.Serialization.Json` namespace. Keeps the core library serialization-agnostic (the NodaTime companion-package pattern; same shape as `Bodu.Numerics.Serialization.Json`). |
| `Bodu.Financial.ExchangeRates` | `Bodu.Financial.ExchangeRates/` | Web exchange-rate provider infrastructure (namespace `Bodu.Financial.ExchangeRates`, shared with the core FX types and the per-source packages): the `WebRateProvider` / `PairWebRateProvider<TSeries>` base classes and `WebRateProviderOptions`, plus the fetch machinery — `SingleFlightCoordinator<TKey>`, `FileSystemByteCache<TKey>`, `RateProviderHttpClientFactory`, `PairRateData<TSeries>`, the `IPairRateLoader` / `IPairRateSource<TSeries>` contracts, and `ExchangeRateFormatException`. Referenced by every per-source provider package. |
| `Bodu.Financial.DependencyInjection` | `Bodu.Financial.DependencyInjection/` | DI registration for financial services (currency lookup, named monetary contexts); financial JSON registration lives in the `Bodu.Financial.Serialization.Json` companion. The `AddFinancialService` / builder extension methods and `UseCurrencyResolution` are declared in the `Bodu.Financial` namespace (alongside the `IFinancialServiceBuilder` builder and `FinancialOptions`), so consumers add `using Bodu.Financial;`. |
| `Bodu.Financial.ExchangeRates.<Source>` | `…ExchangeRates.<Source>/` | Per-source exchange-rate provider packages over `WebRateProvider` / `PairWebRateProvider<TSeries>` (from the `Bodu.Financial.ExchangeRates` package), each isolating one feed's dependencies and parsing **and shipping its own DI registration** (uniformly `Add<Source>ExchangeRates`, e.g. `AddBoeExchangeRates`, declared in the flattened `Bodu.Financial.ExchangeRates` namespace alongside the provider types) — there is no longer a per-provider `*.DependencyInjection` package. All provider types (`<Source>RateProvider` / `<Source>RateProviderOptions`) **and their DI registration extensions** share the single flattened `Bodu.Financial.ExchangeRates` namespace. The shared `Bodu.Financial.ExchangeRates.DependencyInjection` package supplies the generic `AddWebRateProvider` machinery (named `HttpClient` + Polly resilience) every provider delegates to. `<Source>` ∈ `Boe` (Bank of England), `Ecb` (European Central Bank), `Rba` (Reserve Bank of Australia), `Yahoo`, `Ofx`, `Xe` (XE.com), `Oanda` (OANDA — anonymous rolling ~180-day window, declared via `RateHistoryAvailability`), `Fixer` (fixer.io — `access_key`, base+quotes), `ExchangeRateHost` (exchangerate.host — `access_key`, source+quotes), `Fred` (St. Louis Fed FRED — `api_key`, per-pair `series_id` map), `Imf` (International Monetary Fund — keyless, daily **USD-anchored Representative Exchange Rates** downloaded from the IMF's monthly tab-separated report; a single-base **bulk** provider over `WebRateProvider` like `Ecb`, not a pair provider). `Fixer`/`ExchangeRateHost`/`Fred` are pair providers over `PairWebRateProvider<TSeries>` and require an API key on their options; `Imf` is a keyless USD-base bulk provider over `WebRateProvider` (USD/X and X/USD only, cross pairs rejected). |
| `Bodu.Financial.ExchangeRates.Caching[.Sqlite/.Distributed]` | `…ExchangeRates.Caching[.Sqlite/.Distributed]/` | Provider-agnostic caching layer: the `CachingRateProvider` read-through decorator and `AggregatingRateProvider` (priority / average strategies, per-pair routing) over the `IRateCache` contract (`StoreFetchedRange` + `DateRangeCoverage`), with in-memory / TOML-file backends in the core package and durable `Sqlite` and shared `Distributed` (`IDistributedCache`) backends as add-on packages. Each package ships its own DI registration (`AddCachedRateProvider` / `AddAggregatedRateProvider`, `AddSqliteRateCache`, `AddDistributedRateCache` / `AddRedisRateCache`, declared in the root `Bodu.Financial.ExchangeRates` namespace) — no separate `*.DependencyInjection` packages — and all backend cache types share the single `Bodu.Financial.ExchangeRates.Caching` namespace. |
| `Bodu.Financial.ExchangeRates.Testing` | `…ExchangeRates.Testing/` | Shared test infrastructure for the exchange-rate provider and cache test projects: the `DatedRateProviderContractTests<TProvider>` and `PairWebRateProviderContractTests<TProvider, TSeries>` contract-test bases, shipped from a conventional `src/` layout. |
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

Compiling and testing the solution requires the **.NET 10 SDK**, pinned via the repository-root `global.json` (`10.0.100`, `rollForward: latestMinor`): the sources use C# 14 language features and the `.slnx` solution format, neither of which the 8.0 SDK supports. The separate `Bodu.CodeStyle` solution pins its own SDK via `Bodu.CodeStyle/global.json` and is unaffected.

Nullable reference types are enabled everywhere. `ImplicitUsings` is enabled across all projects, including `Bodu.Core`. Test projects have `ImplicitUsings` enabled and pre-import MSTest via `<Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />`. `Bodu.Core/test/Bodu.Core.Test.csproj` additionally pre-imports `Bodu.Test.Assertions.ExceptionAssert` statically so the shared `AssertGuard(...)` call resolves unqualified across all `ThrowHelperTests.*.cs` partial files.

## Key Types

- **Bodu.Core**: `PooledBufferBuilder`, `WeekPattern`, `ThrowHelper`, the extension surfaces, the `Bodu.Threading` async primitives, `SequenceGenerator`; the `Bodu.Functional` seam (`Memoizer`, and the railway primitives `Option<T>` / `Result` / `Result<T>` / `ResultError` / `Either<TLeft,TRight>` with their Task-based async extension companions); text-encoding utilities in the `Bodu.Text` namespace (`EncodingDetection`, `EncodingExtensions`, `StringEncodingExtensions`).
- **Bodu.Collections**: `CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey, TValue>`, `IndexedSet<T>`, `IndexedPriorityQueue<TElement, TPriority>`, `SequencedDictionary<,>`, `MultiValueDictionary<,>`, `Multiset<T>`, `OrderedSet<T>`, `RangeDictionary<,>` / `RangeSet<T>`, `SegmentedBuffer<T>`, `Graph<T>` / `GraphAlgorithms` / `DisjointSet`, `Tree<T>` / `Trie` / `Trie<TValue>`, `BiDictionary<TKey,TValue>`, `BitSet`, `LayeredDictionary<,>` / `DefaultingDictionary<,>`, `Table<TRow,TColumn,TValue>`, `NavigableSet<T>` / `NavigableDictionary<,>`, `IntervalTree<T>` / `IntervalTree<T,TValue>`, `AhoCorasickAutomaton` (+`<TValue>`), `RadixTrie` / `RadixTrie<TValue>`; plus the `Bodu.Collections.Probabilistic` sketches `BloomFilter<T>` (approximate membership, no false negatives), `CountMinSketch<T>` (approximate frequencies, never underestimates), `HyperLogLog<T>` (approximate distinct counts, ~1.04/√m standard error).
- **Bodu.Collections.Concurrent**: `ConcurrentCircularBuffer<T>`, `ConcurrentHashSet<T>`, `ConcurrentEvictingDictionary<TKey,TValue>` — the thread-safe variants, split from `Bodu.Collections`.
- **Bodu.Numerics**: `Fraction<T>`, `BigDecimal`, `Complex<T>` (a generic complex number over `IFloatingPointIeee754<T>`, the generic counterpart of the `double`-only `System.Numerics.Complex`), `Interval<T>` / `DiscreteInterval<T>` / `IntervalSet<T>` and the `IntervalPair<T>` / `DiscreteIntervalPair<T>` result types (each with a `ToIntervalSet()` bridge), plus the statistics aggregates (`RunningStatistics<T>` / `RunningQuantile<T>` / `MovingSum<T>` / `MovingMinMax<T>`). JSON support lives in the companion **Bodu.Numerics.Serialization.Json** (`AddNumericsJsonConverters`, `NumericsJsonPolicy`, the per-type converters/factories, `FractionJsonExtensions`).
- **Bodu.IO.Hashing**: `Fletcher16` / `Fletcher32` / `Fletcher64`, `Crc`, `CrcStandard`(s), `CrcLookupTableCache`, `BlockNonCryptographicHashAlgorithm<T>`, `IResumableHashAlgorithm`, the FNV (`Fnv1a32` / `Fnv1a64`) and Adler (`Adler32` / `Adler32C` / `Adler64`) families, check-digit algorithms (`Luhn`, `Iban`, `Isbn10` / `Isbn13`, `Ean8` / `Ean13`, `Gtin14`, etc.).
- **Bodu.IO.Compound** (namespace `Bodu.IO.Compound`): `CompoundFile` (`Open` to read, `Open(stream, FileMode, FileAccess)` to edit with `Commit` / `Revert`, `Create` plus the `CompoundEntryBuilder` / `CompoundStreamBuilder` API to author), `CompoundStorage`, `CompoundStream` (read-only or writable cursor per the open mode), `CompoundEntryInfo` / `CompoundEntryType` / `CompoundEntryColor`, `CompoundFileOptions` (`CompoundReadStrategy` / `CompoundValidationLevel`), `CompoundFileVersion`, the OLE property-set surface (`OlePropertySet` / `OlePropertySection` / `OlePropertyType` / `SummaryInformation` / `DocumentSummaryInformation`), and the exception hierarchy `CompoundFileException` / `CompoundFileFormatException` / `CompoundFileSerializationException` / `CompoundStreamNotFoundException` (with the `CompoundFileError` category).
- **Bodu.IO.Pst** (namespace `Bodu.IO.Pst`): the disposable read-only session `PstFile` (`OpenRead(path/Stream)`, `Open(Stream, options)`, `IsPstFile`; `Format` / `CryptMethod`, `EnumerateNodes`, `GetNode` / `TryGetNode`) with `PstFileOptions` (`PstValidationLevel` Compatible/Strict/Minimal); `PstNode` (`ReadAllBytes` / `OpenDataStream`, `EnumerateSubnodes` / `TryGetSubnode`, and the LTP views `ReadPropertyContext` / `ReadTableContext`), the LTP surface `PstPropertyContext` (tag-ordered `IReadOnlyCollection<PstPropertyValue>`) / `PstPropertyValue` (raw wire type + resolved payload, typed accessors) / `PstTableContext` (`Columns` / `RowCount` / streaming `EnumerateRows` / `TryGetRow`) / `PstTableColumn` / `PstTableRow`; `PstNodeInfo`, `PstNodeId` (5 type bits + 27-bit index; well-knowns `MessageStore` / `NameToIdMap` / `RootFolder`) / `PstNodeType`, `PstFileFormat` / `PstCryptMethod`; exceptions `PstFileException` / `PstFileFormatException` / `PstUnsupportedFormatException`. The NDB and LTP record layers are **internal** under namespace `Bodu.IO.Pst.Internal` (`PstHeader`, `PstSource`, `PstBTree`, `PstDataTree`, `PstSubnodeTree`, `PstCrypt` §5.1/§5.2 decoders, `PstCrc` §5.3 checksum, `PstBref` / `PstNbtEntry` / `PstBbtEntry`; LTP: `PstHeapNode` / `PstHnid` / `PstBTreeOnHeap` / `PstWireType` / `PstLtpContext` / `PstPropertyContextReader` / `PstTableContextReader`).
- **Bodu.Formats.Excel.Binary** (namespace `Bodu.Formats.Excel` — the package/assembly name keeps the `.Binary` suffix, but the namespace is flattened to the `Bodu.Formats.Excel` domain so a future Excel-format package can share the value model, mirroring the `Bodu.Financial.ExchangeRates` convention): the disposable read-only session `ExcelBinaryWorkbook` (`OpenRead(path/FileInfo/Stream)`, `Open(Stream, options)`, keeps the container open and seeks to each sheet's `lbPlyPos`) with `ExcelBinaryReaderOptions`; the forward-only `ExcelWorksheetReader` (`TryReadCell` / `ReadCells` / `ReadRows`) as the primary surface and the materialized `ExcelWorksheet` / `ExcelRow` as the convenience surface; `ExcelWorksheetInfo` (name/index/`ExcelSheetVisibility`/`ExcelSheetType`/dimensions) / `ExcelWorksheetDimensions`, `ExcelWorkbookProperties` (flattened document fields only — the raw property sets are not exposed), `ExcelCell` / `ExcelCellKind` / `ExcelErrorCode`, `ExcelCellReference`, `ExcelSerialDate` / `ExcelDateSystem`; exceptions `ExcelBinaryFormatException`, `ExcelBinaryUnsupportedException`, `ExcelBinaryEncryptedWorkbookException`, `ExcelBinaryWorkbookStreamNotFoundException`. The BIFF8 record layer is **internal** under namespace `Bodu.Formats.Excel.Biff8` (`Biff8RecordCursor` / `Biff8Record` / `Biff8RecordType`, `Biff8Payload` bounds-checked reads, `Biff8WorkbookGlobals`, `Biff8SubstreamLoader`, `Biff8DimensionsReader`, `Biff8CellDecoder`, `Biff8StringReader`, `Biff8SharedStringTable`, `Biff8FormatTable`, `Biff8SheetDirectoryEntry`) plus the internal `ExcelNumberFormat` date classifier.
- **Bodu.Formats.Outlook** (+ **Bodu.Formats.Outlook.Msg**; both in the flattened namespace `Bodu.Formats.Outlook`): the shared MAPI value model — `MapiPropertyTag` (32-bit tag: 16-bit id + `MapiPropertyType`, with `IsMultiValued` / `IsNamed`), `MapiProperty` / `MapiPropertyCollection` (tag-addressed, typed accessors), `MapiNamedProperty`, the curated `MapiPropertyIds`, `OutlookRecipientType` / `OutlookAttachmentMethod`, `OutlookFormatException` — and the `.msg` reader: the disposable session `OutlookMessage` (`OpenRead(path/Stream)`, `Open(Stream, options)`, `IsMsgFile`; `Properties`, `Recipients`, `Attachments`, named-property lookup, scalar and body conveniences incl. MS-OXRTFCP-decompressed RTF) with `OutlookMessageReaderOptions`, `OutlookRecipient` / `OutlookAttachment` (`OpenContentStream` / `OpenMessage` for nested messages), `OutlookMsgFormatException`. The MS-OXMSG record layer is **internal** under namespace `Bodu.Formats.Outlook.Msg` (`MsgStreamNames`, `MsgPropertyStreamReader`, `MsgPropertyDecoder`, `MsgEncodingResolver`, `MsgStorageWalker`, `MsgNamedPropertyMap`, `CompressedRtf`).
- **Bodu.Security.Cryptography**: `Threefish256` / `Threefish512` / `Threefish1024`, `Skipjack`, `Blowfish`, `Twofish`, `Camellia`, `AsconAead128` (with `AsconHash256` / `AsconXof128` sharing the sponge), `Skein256/512/1024`, `Blake2b`, `Blake3`, `Tiger`, `SipHash64` / `SipHash128`, `Poly1305`, `HashAlgorithmHelper`, AEAD mode transforms (EAX, GCM, OCB, CCM, SIV, GCM-SIV), block-cipher modes; asymmetric algorithms over `AsymmetricAlgorithm`: `X25519` (RFC 7748), `Ed25519` (RFC 8032), `MLKem512/768/1024` (FIPS 203), `MLDsa44/65/87` (FIPS 204) — X25519/Ed25519 implement the SPKI/PKCS#8 DER and RFC 7468 PEM key formats (via the internal `Rfc8410KeyFormat`); ML-KEM/ML-DSA are raw key encodings only by design.
- **Bodu.Text.Encoding**: `Base16`, `Base32`, `Base58`, `Base64`, `Base85`, `BaseFormattingOptions`, `BaseFormatStyles`, variant enums.
- **Bodu.Text.Configuration**: `ConfigurationDocument`, `ConfigurationParseOptions`, `ConfigurationWriteOptions`, `ConfigurationProfile`, view getters.
- **Bodu.Text.Filtering**: `TextFilter` (`Parse` / `Filter` / `IsMatch` over compiled pattern sets), `TextFilterBuilder` (`AddInclude` / `AddExclude`), `TextFilterOptions` / `TextFilterEvaluationMode` (`AnyMatch` set semantics, `LastMatchWins` gitignore rules), `TextFilterPattern` / `TextFilterPatternKind`, `WildcardPattern` (glob compile + match), `TextFilterStatistics` / `TextFilterPatternStatistics` (`GetStatistics()`), `ITextFilterObserver`.
- **Bodu.Text.Delimited** (namespaces `Bodu.Text.Delimited[.Reader/.Writer/.Nodes/.Document]`): `Utf8DelimitedReader` / `Utf8DelimitedWriter`, `DelimitedReaderOptions` / `DelimitedWriterOptions` (dialect policies: `DelimitedFieldCountBehavior`, `DelimitedMalformedRecordBehavior`, `DelimitedDuplicateHeaderBehavior`), `DelimitedSerializer` (+`Options`/`Defaults`; the truly incremental `DeserializeAsyncEnumerableAsync<TRecord>` / `SerializeAsync(IAsyncEnumerable<TRecord>)` streaming pair; and the reflection-free factory overloads over `IDelimitedRecordFactory<TRecord>` + `DelimitedRecordAttribute`), `DelimitedNode`/`DelimitedArray`/`DelimitedObject`/`DelimitedValue`, `DelimitedDocument`/`DelimitedElement`/`DelimitedProperty`, `DelimitedFormatException` / `DelimitedSerializationException`.
- **Bodu.Text.DotEnv** (same namespace layout): `Utf8DotEnvReader` / `Utf8DotEnvWriter`, `DotEnvSerializer` (+`Options`/`Defaults` incl. the SCREAMING_SNAKE_CASE `Web` preset), `DotEnvNode`/`DotEnvObject`/`DotEnvValue` (export-flag-preserving), `DotEnvDocument`/`DotEnvElement`/`DotEnvProperty`, `DotEnvFormatException` / `DotEnvSerializationException`.
- **Bodu.Text.Ini** (same namespace layout): the two readers `Utf8IniReader` (source-order) and `IniDocumentReader` (normalized object-of-objects; applies `IniDocumentOptions` duplicate policies), `Utf8IniWriter`, `IniSerializer` (+`Options`/`Defaults` incl. `Strict` configparser mode and `GlobalSectionName`; and the reflection-free `SerializeSection`/`DeserializeSection` overloads over `IIniSectionFactory<TSection>` + `IniSectionAttribute`), the **trivia-bearing** mutable `IniNode`/`IniObject`/`IniValue` DOM (leading/trailing comments; the one sanctioned deviation from the trivia-free quartet DOMs), the read-only `IniDocument`/`IniElement`/`IniProperty`, `IniFormatException` / `IniSerializationException`. All three line formats wire scalars as **strings**, so scalar conversion stays serializer-local (invariant-culture parse/format) rather than adopting the shared recursive converter engine.
- **Bodu.Globalization.Calendar**: `NotableDateService` / `INotableDateService`, `NotableDateResource` / `NotableDateResourceLoader`, `NotableDateRule`, `NotableDate`, `NotableDateDefinition`, `NotableDateFilter`, `TerritoryCode`; namespace `Bodu.Globalization.Calendar.Algorithms` (`INotableDateAlgorithm`, `EasterCalculator`, `HinduLunarCalculator`, `LunarPhaseCalculator`, `SolarTermCalculator`, and the `IDateCalculationStrategy` implementations); namespace `Bodu.Globalization.Calendar.RangeResolution` (`ResolutionPolicy`, collision/duplicate policies); working-day/date extensions in `Bodu.Extensions`.
- **Bodu.Globalization.Calendar.Data.<Region>** (data bundles): the `<Region>CalendarData` static factory (`SupportedCountries`, `LoadResource(territory)`, `CreateService(territory)`) over the embedded per-country packs; `CommonNotableDateResources` resolves the shared catalogues (`global-core`, `christian-western`, `christian-orthodox`, `catholic`, `global-islamic` / `global-islamic-umm-al-qura`, `global-jewish`, `global-buddhist`, `global-hindu`, `global-persian`, …) that the region hubs import.
- **Bodu.Globalization.Recurrence**: `RecurrenceRule` (RFC 5545 `RRULE` over `IParsable`/`ISpanParsable`/`IFormattable`, the full `FREQ`/`INTERVAL`/`COUNT`/`UNTIL`/`WKST` + `BY*` model, occurrence enumeration for `DAILY`–`YEARLY`), `RecurrenceRuleBuilder`, `RecurrenceSet` (`RDATE`/`EXDATE` composition, iCalendar property-block parse/format round-trip, value equality), `CronExpression` (documented 12-year search horizon), `AnchoredInterval` (occurrences at `anchor + k·interval` for `k ≥ 1`, anchor passed per query, RFC 5545 §3.3.6 duration text), `WeekDayNum`, `RecurrenceFrequency`, `CronFormat`. All four schedule forms answer `GetNextOccurrence` / `GetPreviousOccurrence` (inclusive flags, `DateTime` + `DateTimeOffset`) and carry defect-naming `TryParse` overloads.
- **Bodu.Test** (test infrastructure): `IKat`, generic KAT records (`ValidKat<TInput,TExpected>`, `InvalidKat<TInput>`, `RoundTripKat<TValue,TWire>`, `BinaryKat<TInput,TExpected>`, `GuardValidKat<T>`, `GuardInvalidKat<T>` — the domain-shaped `EnumerableKat`, `BinaryEncodingKat`, and `InvalidEncodedTextKat` live alongside their consumers per the Test Consolidation section below), `KatDisplayName` helper, `ExceptionAssert` (with `ThrowsExactlyWithParamName<T>` and `AssertGuard`), MSTest tier constants in `TestCategories`, reusable stream mocks under `Bodu.Test.IO`.

## Build & Tooling

- Shared MSBuild configuration lives in `bld/Bodu.props` (Authors, MIT licence, deterministic builds, package metadata, doc-comment warnings as errors — e.g. CS1591).
- `.editorconfig` lives at the repository root (`/.editorconfig`, `root = true`) and drives formatter and code-style settings for the entire tree.
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

`.claude/hooks/session-start.sh` installs `dotnet-sdk-10.0` from `apt` on session start when running in the remote Claude Code on the web environment (`CLAUDE_CODE_REMOTE=true`). It is idempotent — when a .NET 10 SDK is already installed it exits immediately, so resume / clear / compact sessions pay no extra cost. The repository-root `global.json` pins SDK resolution to the 10.0.1xx band, so once the hook has run, `dotnet build` / `dotnet test` pick up the installed SDK 10 automatically.

The hook also repairs the `dotnet-dnceng` plugin that `.claude/settings.json` enables from the `dotnet/arcade-skills` marketplace (`extraKnownMarketplaces` / `enabledPlugins`): the upstream plugin manifest currently fails Claude Code's path validation (its `agents` entry lacks the required `./` prefix), so the hook patches the cached marketplace clone and installs the plugin. Its skills then load from the next session in the container. The repair is a no-op once the manifest is fixed upstream and can be removed at that point.

Local developer machines are untouched (the hook short-circuits when `CLAUDE_CODE_REMOTE` is unset), and the hook is registered via `.claude/settings.json`. Note for local use: until the upstream manifest fix lands, the plugin install triggered by the project settings may fail validation on a local machine; sessions still work, just without the plugin's skills.

## Branching and Commits

- **One branch per session, by default.** Use the branch the harness designates at session start (typically `claude/<topic>-<id>`) and make multiple commits to it as the session progresses. Do not spin up additional branches for each edit, fix, or intermediate step within the same session.
- **Commit incrementally.** Prefer a fresh commit per logical step over batching unrelated changes into one large commit. The branch should accumulate work across the session, not be replaced.
- **Test before fix.** For a defect-driven change, the failing-regression-test commit precedes the fix commit — do not bundle them (see *Test-First for Fixes (Red-Green)* under Test Conventions).
- **Exceptions that justify additional branches:**
  - Resolving conflicts on multiple existing PR branches — each PR has its own remote head that must be checked out and pushed back to.
  - Work that must land on a specific pre-existing branch other than the session branch.
  In these cases, use disposable local branches and delete them once the work is pushed.
- **Push** to the session branch when changes are ready; do not push to `master` directly.

## Test Conventions

- Framework: **MSTest** (`Microsoft.VisualStudio.TestTools.UnitTesting`, `[TestClass]` / `[TestMethod]`). Do **not** introduce xUnit or NUnit.
- Tests live in `<Project>/test/` and are organised as **partial classes** that mirror the source layout — e.g. `CircularBuffer{T}.cs` → `CircularBufferTests.Enqueue.cs`, `CircularBufferTests.Dequeue.cs`. Extend the existing partial class when adding tests for an existing type.
- No shared test base classes; each test is self-contained.

### Test-First for Fixes (Red-Green)

When fixing a bug — or changing behaviour in response to a defect — **write the test before the fix**:

1. **Red.** Add a regression test that reproduces the defect, and run it to confirm it **fails** against the current (buggy) code. A test that has never been seen to fail does not prove it guards anything.
2. **Green.** Only then apply the fix, and confirm the test now **passes** along with the rest of the suite.
3. **Separate commits, test first.** Commit the failing test and the fix as two commits, the test commit preceding the fix commit, so the history documents the reproduction. A red intermediate commit on a feature branch is expected — that is the point; the branch head (the fix commit) is green.

The regression test follows every other convention below (member-named partial file, `<MethodOrProperty>_When…_Should…` naming, the `Verifies that …` summary, `Assert.ThrowsExactly<T>` for exceptions, the correct tier). Where a reference oracle exists — for example `System.Numerics.Complex` for `Complex<T>` — pin the corrected behaviour against it rather than a hand-guessed expected value, so the test cannot bake in the same mistake as the fix.

This rule governs **defect-driven changes**. Net-new features follow the conventions below without the red-first step (there is no pre-existing behaviour to reproduce), though writing tests alongside the implementation is still expected.

### Test File Organisation

Default to grouping tests by the member under test. For a type `Foo`, use partial files named after the public method, property, constructor group, operator, or interface surface being validated.

**Every test type must carry member-named backbone partials for its primary public methods and properties.** This is the rule that `Bodu.Core` and `Bodu.Security.Cryptography` set (e.g. `Blake2bTests.Ctor.cs` / `.HashSize.cs` / `.Key.cs`, `TigerTests.ComputeHash.cs` / `.Variant.cs`) and it is the bar for every test project. A test type is **not** allowed to be organised purely by feature/concern with no member backbone. Beyond the backbone, two — and only two — kinds of non-member partials are permitted: (1) **subject-based** partials for genuinely cross-cutting behavioural contracts that span multiple members (below), and (2) **corpus / vector / spec / fixture** files that are inherently data-driven rather than member-shaped. When a member has a single dominant operation (for example a forward-only reader whose one public method is `Read`), splitting that operation's many concerns into subject partials *is* the member-aligned shape — do not collapse them into one giant file.

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

**`System.Text.Json`-shaped serializers** (`TomlSerializer`, `BencodeSerializer`, `YamlSerializer`, `DelimitedSerializer`, `DotEnvSerializer`, `IniSerializer`, and any future peer) follow this rule explicitly: the backbone is the public operations — `<Type>SerializerTests.Serialize.cs`, `.Deserialize.cs`, and the `.SerializeAsync.cs` / `.DeserializeAsync.cs` overloads — plus `.RoundTrip.cs` as the home for `SerializeDeserialize_*` (round-trip) tests. A method's tests are routed to its backbone file by the test-method name prefix (`Serialize_*` → `.Serialize.cs`, `Deserialize_*` → `.Deserialize.cs`, and so on). Feature areas (`.NamingPolicy.cs`, `.Required.cs`, `.Nullables.cs`, `.ExtensionData.cs`, enum converters, `.Collections.cs`, `.Dictionaries.cs`, object-construction/creation handling, …) are **subject** partials layered on top of that backbone — they are not a substitute for it. Shared model POCOs, `[DynamicData]` providers, and bespoke KAT records live in the root `<Type>SerializerTests.cs`.

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
- **`Bodu.Test.Contracts`** namespace — the multi-consumer contract test bases: `ParseFormatContractTests<T>` (Bodu.Core.Test + Bodu.Numerics.Test) and the promoted collection contract bases `CollectionContractTests<>`, `SetContractTests<>`, `EnumeratorContractTests<>`, `DebugViewContractTests<>`, `NonGenericCollectionContractTests<>` (with `SyncRootSupported` opt-out), `ReadOnlyCollectionContractTests<>` (Bodu.Collections.Test + Bodu.Collections.Concurrent.Test).
- **`Bodu.Test.Assertions.ExceptionAssert`** — `ThrowsExactlyWithParamName<TException>(action, expectedParamName)` and the `AssertGuard(testName, act, expectedExceptionType, expectedParamName)` matrix helper, plus KAT-aware overloads `AssertGuard<T>(GuardValidKat<T>, Action<T,T,string?>)` and `AssertGuard<T>(GuardInvalidKat<T>, Action<T,T,string?>)`.
- **`Bodu.Test.IO`** namespace — stream mocks (`FaultingStream`, `ThrottledIncrementingByteStream`, `NonSeekableStream`, etc.).
- **`Bodu.Test.TestCategories`** — tier constants (Smoke / Regression / Stress) consumed by every domain test project.

**Lives alongside the consumer (per domain test project)**, each in a `Contracts/` or similar folder under the test project root. Contract bases and KAT records share the local `<area>.Contracts` namespace so subclasses pick them up without an extra `using`:

- **Bodu.Core.Test** (namespaces `Bodu.Buffers` / `Bodu.Collections.Generic.Extensions`): the domain-shaped KATs `BufferCapacityKat`, `BufferWriteKat`, `EnumerableKat<,>`, `WeekPatternParseKat`, `InvalidWeekPatternParseKat`.
- **Bodu.Security.Cryptography.Test** (namespace `Bodu.Security.Cryptography.Infrastructure`): `SymmetricStreamAlgorithmTests<TTest, TAlgorithm>` — abstract base inherited by every stream cipher (ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit, Hc128) covering key/nonce sizing, lifecycle, transform reuse, overlap rules, and disposal. Other test surfaces inherit directly from the local abstract bases: block ciphers extend `BlockCipherTests<TTest, TCipher, TVariant>`, block-cipher transforms extend `BlockCipherTransformTests<TTest, TCryptoTransform>`, and AEAD / hash families use their own per-family bases — no parallel contract layer is duplicated. The retained KAT records are `HashExtensionKat` plus the domain-local `AeadKnownAnswerVector`, `BlockCipherKnownAnswer`, `HashAlgorithmKnownAnswers` + `HashAlgorithmKnownAnswer`, and `KeyedHashAlgorithmKnownAnswer`. The asymmetric families use a hierarchical contract layer mirroring the symmetric one: `AsymmetricAlgorithmTests<TTest,TAlgorithm>` (root: ctor/key-size/import-export/dispose contract, driven by an `AsymmetricAlgorithmSpecification` record) with the operation bases `KeyAgreementAlgorithmTests<,>` (X25519Tests), `SignatureAlgorithmTests<,>` (Ed25519Tests, and via `MLDsaContractTests<TTest,TDsa>` the MLDsa44/65/87 tests), and `KemAlgorithmTests<,>` (via `MLKemContractTests<TTest,TKem>` the MLKem512/768/1024 tests); plus the `HexFieldKatReader` (`Field = value` block format) with the KAT records `KeyAgreementKnownAnswer`, `SignatureKnownAnswer`, `Kem*KnownAnswer`, and `Dsa*KnownAnswer`, and embedded Wycheproof / NIST ACVP vector files (each with a provenance header) driven by the `MLKemAcvpVectors` / `MLDsaAcvpVectors` loaders in the Regression tier; the RFC 7748/8032 vectors are expressed as in-memory KAT rows of the same record types. A companion **`Bodu.Security.Cryptography.Simd.Test`** is a separate test assembly (necessarily so, because the switch is process-wide) that sets the `Bodu.Security.Cryptography.DisableSimd` feature switch via its `runtimeconfig.template.json`, forcing SIMD dispatch off so the AVX-512-accelerated primitives are exercised through their scalar reference paths and must still produce the published digests.
- **Bodu.IO.Hashing.Test** (namespace `Bodu.IO.Hashing.Contracts`): `NonCryptographicHashAlgorithmContractTests<TAlgorithm>`, `CheckDigitContractTests<TAlgorithm>`, `MultiCharCheckDigitContractTests<TAlgorithm>`; plus the KATs `HashKat`, `HashStreamingKat`, `CrcCatalogKat`, `CheckDigitKat`; and the pre-existing domain-local KATs `NonCryptographicHashKnownAnswer`, `CheckDigitKnownAnswer`, `MultiCharCheckDigitKnownAnswer`, `MultiCharCheckDigitIsValidKnownAnswer`.
- **Bodu.Text.Encoding.Test** (namespace `Bodu.Text.Encoding.Contracts`): `BinaryEncodingContractTests<TEncoding>`; plus the KATs `BinaryEncodingKat`, `InvalidEncodedTextKat`; and the pre-existing domain-local KATs `EncodingKnownAnswerVector` and `EncodingNegativeDecodeVector` (both implement `IKat`, so every `[DynamicData]` site binds them through `KatDisplayName`).
- **Bodu.Text.Delimited.Test** + **Bodu.Text.DotEnv.Test** + **Bodu.Text.Ini.Test**: each quartet library is tested end to end and self-contained, mirroring the Bencode/Toml model — reader token-transcript tests (incl. hardened BOM/line-ending/multibyte and dialect sweeps), writer canonical-byte tests, the serializer backbone partials per the rule above, and DOM tests (INI adds duplicate-policy, global-hoist, and comment-trivia round-trip coverage). Rows that fit `input → expected` reuse the shared `ValidKat<,>` (e.g. the Delimited RFC 4180 Regression corpus and the DotEnv python-dotenv/godotenv-derived conformance corpus both use `ValidKat<string, string[][]>`, with `InvalidKat<string>` for the malformed sweeps); no shared document-format contract base is promoted.
- **Bodu.Text.Formats.Generators.Test**: references the generator with `OutputItemType="Analyzer"` over its own compilation, so the end-to-end tests exercise factories the generator actually emitted at build time (`GeneratedPerson.DelimitedFactory`, `GeneratedServerSection.IniFactory`) — header/key resolution, byte parity with the reflection binders, and round-trips — plus `CSharpGeneratorDriver`-based tests asserting the `BTFG00x` diagnostics against in-memory compilations.
- **Bodu.Text.Bencode.Test** + **Bodu.Text.Toml.Test**: each self-contained library is tested end to end — the ref-struct `Utf8*Reader`/`Utf8*Writer` token surface (canonical round-trips and malformed-input rejection), the `*Serializer` POCO-mapping suite, the `System.Text.Json`-aligned feature surface (serialization callbacks, unmapped-member handling, object-creation/Populate, naming policies, the string/number enum converters, and the full attribute family), and both DOMs (the mutable `*Node` tree and the read-only `*Document`). Full-grammar and malformed-input sweeps are tagged `[TestCategory("Regression")]`. Tests are colocated per library and assert exact canonical bytes/text where applicable; the two libraries are independent and self-contained, so no shared serialization contract base is promoted. Canonical-form rows that reduce to `input → expected string` use the shared `ValidKat<,>` (e.g. Toml's float/string canonical rows); rows carrying delegates or extra selector fields stay local (Bencode's `RoundTripCase` / shape KATs, Toml's `IntCanon` / `DecimalCanon`).
- **Bodu.Text.Configuration.Test**: the `ConfigurationKat` catalogue (`ConfigurationKnownAnswerData`) driven by `ConfigurationKatRunnerTests`. `ConfigurationKat` implements `IKat` (`Name => Title`) and binds through the shared `KatDisplayName`; each runner is split into binary `_WhenValid_…` / `_WhenInvalid_…` methods over the `…Pass` / `…Fail` filtered suppliers rather than branching on `ConfigurationKatOutcome`.
- **Bodu.Globalization.Calendar.Test** (plus the `.Data.*`, `.DependencyInjection`, `.Plugins`, and `.Builder` test projects): the calendar suite uses self-contained `*KnownAnswerTests` classes (e.g. `EasterKnownAnswerTests`, `IslamicCalendarKnownAnswerTests`, `StrategyResolution*KnownAnswerTests`) that drive known-answer vectors directly, with shared XML inputs embedded under `test/Globalization.Calendar/Fixtures/`. The `.Builder` test project validates the fluent authoring API end-to-end by building documents, serializing to XML/JSON, and asserting against the real `NotableDateResourceLoader` / `NotableDateService`. Each `.Data.<Region>` bundle test project follows the `<Region>CalendarDataTests` pattern — `[DataRow]` known-answer vectors that pin every floating or computed holiday (Easter offsets, nth-weekday rules, lunar/Hijri/Hebrew festivals, weekend-substitution shifts) to confirmed published dates, with exact assertions where the date is deterministic and a ±2-day tolerance for moon-sighting/astronomical festivals, plus a `CreateService_ForEverySupportedCountry_LoadsAndResolves` smoke test. It does not currently promote reusable contract bases or KAT-record types; add them under a local `Contracts/` folder if a second consumer in a different calendar test project emerges.

When adding a new contract base or KAT record, default to colocating it with its sole consumer — only promote it to `Bodu.Test` once a second consumer in a different test project exists.

Conventions:

- **`[DataRow]` is for primitive scalars only** (int, bool, string, enum). Use `[DynamicData]` with a strongly typed KAT record for byte arrays, expected exception types, options objects, parser state, or object graphs.
- **Binary tests** — one `[TestMethod]` asserts one observable outcome. Do not write methods like `_ShouldEitherReturnExpectedOrThrow` that branch on a row flag. Split into separate methods over filtered data sources: typically `_ShouldNotThrowAndReportNothing` (pass rows) and `_ShouldThrowOn<Param>` or `_ShouldThrowExpected` (fail rows).
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

- **Reuse a generic when it fits exactly; keep a bespoke record local only when no generic does.** A record stays domain-shaped (and local, in the consumer's `Contracts/` or `*.Kat` folder) when it carries extra fields or delegates the generics cannot express — for example Bencode's `RoundTripCase` / `CollectionShapeKat` (writer/`Func` delegates), Toml's `IntCanon` (a `Func<string>` over multiple write paths) and `DecimalCanon` (an added `TomlByteArrayHandling`-style selector field), or Financial's `RateLookupKat` / `RateDateResolutionKat` (multi-field result rows). A bespoke record must still implement `IKat` and synthesize a meaningful `Name`.
- **Every `[DynamicData]` KAT site wires the shared display name**: `DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName)`, `DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName)` (with `using Bodu.Test.Kat;`). Do not hand-roll a per-runner display-name method when the row implements `IKat` — `KatDisplayName` already returns `IKat.Name`.
- **Pass and fail are separate `[TestMethod]`s over filtered data sources** — never one method that branches on an outcome flag (`if (kat.ExpectedSuccess) …`, `switch (kat.Outcome)`). Provide `…Pass` / `…Fail` supplier properties that filter the catalogue and bind a `_WhenValid_Should…` method and a `_WhenInvalid_ShouldThrow…` method, each asserting one outcome unconditionally.

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
- **Generic type files.** Every `.cs` file whose primary declared type is generic is suffixed with `{T}` for one type parameter, `{T,T}` for two, `{T,T,T}` for three, using a single literal `T` per parameter position — e.g. `CircularBuffer{T}.cs`, `EvictingDictionary{T,T}.cs`. Partial files for a generic type carry the same infix before the part name: `TypeName{T}.PartName.cs`. Non-generic companion types (extension method classes, factory classes, non-generic overloads) are not renamed. Nested or secondary type declarations within a partial file of a non-generic parent are also not renamed. The `.filenesting.json` configuration nests `Foo{T}.cs` under `Foo.cs` so generic companions visually group with their non-generic counterpart in the IDE. Do not use the full type-parameter name in file suffixes — always use a single `T` per position regardless of the declared parameter name (e.g. `<TKey, TValue>` → `{T,T}`).
- Partial-file naming is `<Base>.<Part>.cs` where `<Base>.cs` holds the root declaration. Examples:
  - `CircularBuffer{T}.cs` ← root
  - `CircularBuffer{T}.Enumerator.cs`, `CircularBuffer{T}.Debug.cs` ← partials/child-type splits
  - `CrcStandard.cs` ← root; `CrcStandard.Catalog.cs` ← auto-generated catalogue partial
- Don't stack unrelated helper types into the same file. If a type only makes sense alongside its parent (private nested enum, internal helper record), split it into a partial file under the parent rather than co-locating it in the root.

### Namespace–Folder Alignment

Folders directly under a project's source root (`src/` or `test/`) map one-to-one to namespaces. The **namespace declared in a file is the source of truth**; its folder is derived from it. Concretely, for a file with namespace `N` in a project whose csproj declares `<RootNamespace>R</RootNamespace>`:

| Case | Folder (relative to `src/`/`test/`) |
|---|---|
| `N` equals `R` | the project root (no folder) |
| `N` starts with `R.` | a **single flat folder** named `N` minus the leading `R.`, dots preserved — e.g. `Bodu.IO.Hashing.CheckDigits` (with `R = Bodu`) ⇒ folder `IO.Hashing.CheckDigits` |

Rules:

- **No nested namespace folders.** A folder must never contain a child namespace folder. `IO.Hashing/` containing `Target/` is wrong — use sibling folders `IO.Hashing/` and `IO.Hashing.Target/`.
- **One namespace per folder.** A folder holds only files whose namespace equals `R` + the folder name. If two namespaces currently share a folder, split them into two flat folders.
- **Derive the folder from the namespace, never the reverse.** When a file's folder and namespace disagree, move the file to the folder its namespace dictates; do not silently rename the namespace to match the folder. (A namespace that looks wrong is a separate, explicit decision.)
- `src/` and `test/` are not namespace components (the csproj sits at `<Project>/src/` or `<Project>/test/`). `<RootNamespace>` is taken from the csproj as-is.

Carve-outs (exempt from the rules above):

- **Asset / embedded-resource folders** may keep their own nested structure and are not required to match a namespace: any folder named `Fixtures` or `TomlTestCorpus`, or ending in `Resources` (calendar `.xml` packs, spec test corpora, `.resx`/`.Designer.cs` pairs, and other `EmbeddedResource`/`None`/`Content` data). These are wired to csproj globs by path, so do not move them.
- **BCL-convention foreign namespaces** live at the project root: files deliberately declared in `Microsoft.*` or `System.*` (e.g. `IServiceCollection` registration extensions in `Microsoft.Extensions.DependencyInjection`).

This convention is the dotted-flat reading of `dotnet_style_namespace_match_folder` / IDE0130 (a folder literally named `Collections.Generic` maps to `Bodu.Collections.Generic`). Run `bld/check-folder-namespace-alignment.sh` to verify a project or the whole tree; it encodes the rules and carve-outs above and exits non-zero on any violation.

### Naming

- Private instance fields: `_camelCase`.
- Private static fields: `s_camelCase`.
- Prefer `var` where the type is obvious; see **Implicit Typing (`var`)** under C# Code Style Guidelines for the decision cascade.
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
- Add `<returns>` for every non-void **method** and **operator**, describing the return value.
- Describe the result in the context of the member's purpose, not merely the raw type.
- **Do not use `<returns>` on a property.** Per Microsoft's C# XML documentation guidance and the C# language specification, `<returns>` documents the return value of a *method declaration*; the value a property represents is documented with `<value>`. Use `<summary>` (and optionally `<value>`) on properties instead — never `<returns>`.

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
- Include `<value>` on properties where the semantics require clarification beyond the summary. It is optional — a property whose summary already fully conveys its value needs only `<summary>`.
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

Analyzer note: **CA1863** ("Use 'CompositeFormat'") is disabled repo-wide in `.editorconfig`. Every site it flags formats a resx accessor on an exception/error path that runs once per failure, so caching parsed formats buys nothing measurable and would freeze resource resolution at type-init. Format resource-backed messages with plain `string.Format(CultureInfo.CurrentCulture, <Domain>ResourceStrings.<Key>, …)` at the throw/error site; do not introduce cached `CompositeFormat` fields for them.

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

### Implicit Typing (`var`)

Prefer `var` for local declarations where it does not obscure the type, per the root `.editorconfig` (`csharp_style_var_for_built_in_types = true`, `csharp_style_var_when_type_is_apparent = true`, `csharp_style_var_elsewhere = false`). This is the Roslyn **IDE0007** ("use var") rule, enforced at `warning`. Apply this cascade to each local declaration:

1. **Built-in type → `var`.** When the variable's type is a C# built-in (`int`, `uint`, `long`, `bool`, `byte`, `string`, `double`, `char`, …):

   ```csharp
   var count = 5;
   var name = reader.ReadString();
   var a = year % 19;            // matches existing algorithm code in Bodu.IO.Hashing / Bodu.Security.Cryptography
   ```

2. **Type named on the right-hand side → `var`.** When the right-hand side makes the type apparent — `new T(...)`, a cast `(T)x`, `T.Parse(...)`:

   ```csharp
   var buffer = new CircularBuffer<int>(8);
   var node = (BencodeObject)element;
   ```

3. **Otherwise → explicit type.** When the type is neither built-in nor apparent from the right-hand side, keep the explicit type so the declaration stays self-documenting:

   ```csharp
   NotableDateResource resource = loader.Load(territory);   // return type not visible at the call site
   ```

   (`csharp_style_var_elsewhere = false`, left at `suggestion` — advisory, not build-enforced.)

Clarifications:

- **Interface- or base-typed locals stay explicit.** `IList<int> items = new List<int>();` keeps its declared type — `var` would change the static type to `List<int>`. IDE0007 never fires when the declared type differs from the right-hand-side type, so these are safe automatically.
- **Target-typed `new` is unaffected.** `DateRange year = new(start, end);` does not name its type on the right-hand side, so it is *not* an IDE0007 site and is left as-is. For a *new* local, prefer the apparent `var x = new T(...)` form over `T x = new(...)`, but do not churn existing `T x = new(...)` declarations.
- **`foreach`**, **`out` variables** (`out var value`), and **`using` / `await using`** declarations follow the same cascade.
- `var` is required for anonymous types; explicit typing is required wherever the compiler cannot infer the intended type. Generated files (`<auto-generated>` banner, e.g. `*ResourceStrings.Designer.cs`, `CrcStandard.Catalog.cs`) are exempt — the analyzers and `dotnet format` skip them.

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
