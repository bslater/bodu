# Roadmap

Forward-looking plan for the **Bodu** C# utility library. Pairs with
[`CLAUDE.md`](CLAUDE.md) (repository conventions for contributors).

*Last updated: 2026-07-02. A large amount of net-new surface has landed
since the previous revision, and this roadmap is rewritten to match. The
headline additions: (1) a **Bodu.Core** structural expansion — the
`Collections.Generic.Graphs` / `Collections.Generic.Trees` pillars
(`Graph<T>`, `GraphAlgorithms`, `DisjointSet`, `Tree<T>`, `Trie` /
`Trie<TValue>`), a full `Threading` async-primitive suite (`AsyncLock`,
`AsyncReaderWriterLock`, `AsyncSemaphore`, the reset-event family,
`AsyncLazy<T>`, `AsyncDebouncer`, `RateGate`), the `Sequences`
(`SequenceGenerator`) and `Functional` (`Memoizer`) seams, and a broadened
collection catalogue (`SequencedDictionary<,>`, `MultiValueDictionary<,>`,
`RangeDictionary<,>` / `RangeSet<T>`, `Multiset<T>`, `OrderedSet<T>`,
`SegmentedBuffer<T>`); (2) three **standalone `System.Text.Json`-shaped
text libraries** extracted into their own projects — `Bodu.Text.Bencode`,
`Bodu.Text.Toml`, and the new `Bodu.Text.Yaml` (YAML 1.2 core) — each the
ref-struct `Utf8*Reader` / `Utf8*Writer` + `*Serializer` + mutable/read-only
DOM quartet; (3) the **container + office-format reader** pair
`Bodu.IO.Compound` (OLE2/CFB read + edit + fluent authoring) and
`Bodu.Formats.Excel.Binary` (read-only BIFF8 `.xls`); (4) the entire
**`Bodu.Financial.ExchangeRates.*` ecosystem** — seven web providers (Boe,
Ecb, Rba, Yahoo, Ofx, Xe, Oanda) over a shared `WebExchangeRateProvider`
base, the provider-agnostic caching layer (`CachingExchangeRateProvider`,
`AggregatingExchangeRateProvider`) with in-memory / TOML / JSON / SQLite /
distributed backends, and per-package DI; and (5) DI packages for
`Bodu.Financial` and the calendar service, plus the calendar plugin loader.
Central package management (`Directory.Packages.props`) is now in place.
Nothing has been tagged/released yet (no git tags), so the release tranches
below remain the first publishing target rather than a shipped state.*

## How to read this

- **Release focus** lists the packages queued for their first publish,
  grouped into release waves.
- **Non-goals** lists things the repository is deliberately *not* doing,
  to keep scope discussions short.
- **Per-project roadmap** has a short subsection per project (or project
  family) in `bodu.slnx`. Each entry gives the current state plus 1–3
  concrete forward-looking items.
- **New library candidates** collects proposed *net-new* projects that
  fill genuine BCL / ecosystem gaps and reuse the repository's proven
  architectural patterns.
- **Cross-cutting themes** covers concerns that span multiple projects
  (TFM policy, AOT/trim, API stability tiers, source generators, and the
  recurring architectural patterns that new work should conform to).

Items in this file are intent, not commitments. The order under each
project is rough priority — the first bullet is what would land next if
work started today.

## Repository conventions

The roadmap assumes the conventions already documented in
[`CLAUDE.md`](CLAUDE.md). The ones most relevant to forward planning:

- **TFM baseline.** All shipping projects target `net8.0` only — no
  multi-targeting today. Compiling/testing requires the .NET 10 SDK
  (C# 14, `.slnx`), pinned via the root `global.json`. Bumping the
  floor is a roadmap decision (see *Cross-cutting themes*).
- **Central package management.** `Directory.Packages.props` now pins
  every NuGet version centrally; new dependencies flow through it, not
  per-project `<PackageReference Version=...>`.
- **Test model.** Contract test bases and KAT records now live
  *alongside their consumer* per test project (a `Contracts/` folder in
  the domain project), with only genuinely cross-project primitives in
  `Bodu.Test` (`Bodu.Test.Kat`, `Bodu.Test.Contracts`,
  `Bodu.Test.Assertions`, `Bodu.Test.IO`). New types plug into the
  existing contract suite rather than introducing bespoke harnesses.
- **Style enforcement.** `Bodu.CodeStyle.XmlDocumentation` analyzers
  enforce documentation shape; `Bodu.props` carries `WarningsAsErrors`
  for CS1591. Treat doc gaps as build breaks.
- **Package metadata.** Shared in `bld/*.props`. New packages should
  flow through the same props rather than redefining metadata locally.
- **Package validation.** `BoduEnablePackageValidation` is opt-in today;
  the roadmap commits to making it the default on all packable projects.

## Release focus

No package has been tagged or published yet. The first publish is
organised into waves so package-validation and dependency ordering are
exercised on the smallest self-contained units first.

**Wave 1 — foundation packages (no inter-Bodu package dependencies):**

| Package | Notes |
| --- | --- |
| `Bodu.Core` | The dependency root — buffers, collections (incl. the new graphs/trees pillars), threading primitives, sequences, `WeekPattern`, `ThrowHelper`, text-encoding utilities. |
| `Bodu.Numerics` | `Fraction<T>` over `IBinaryInteger<T>` and `Interval<T>` over `INumber<T>`, with JSON converters. |
| `Bodu.IO.Hashing` | Non-cryptographic hashing + the full RevEng CRC catalogue + the check-digit family. |
| `Bodu.Text.Encoding` | Base16/32/58/62/64/85 + Base45 + Bech32/Bech32m. |
| `Bodu.Security.Cryptography` | Block/stream ciphers, AEAD, keyed/crypto hashes, the asymmetric family, KDFs, HPKE. |

**Wave 2 — self-contained format & text libraries:**

| Package | Notes |
| --- | --- |
| `Bodu.Text.Bencode` | Standalone STJ-shaped Bencode library (reader/writer/serializer/DOM quartet). |
| `Bodu.Text.Toml` | Standalone STJ-shaped TOML v1.0.0 / v1.1.0 library; corpus-backed. |
| `Bodu.Text.Yaml` | Standalone YAML 1.2 core-profile library (read-focused serializer). |
| `Bodu.Text.Formats` | Delimited (RFC 4180), DotEnv, INI. |
| `Bodu.Text.Configuration` | INI-compatible profile, resolver, view getters. |
| `Bodu.IO.Compound` | OLE2 / CFB container read + edit + authoring. |
| `Bodu.Formats.Excel.Binary` | Read-only BIFF8 `.xls` reader (depends on `Bodu.IO.Compound`). |

**Wave 3 — financial core + calendar (coordinated breaking change):**

| Package | Notes |
| --- | --- |
| `Bodu.Financial` | `Money` / `Money<TCurrency>`, `CalculatedMoney`, `MoneyBag`, the ISO 4217 catalogue, rounding/allocation policies, and the FX abstractions (`ExchangeRate`, `IExchangeRateProvider` / `IDatedExchangeRateProvider`, `ExchangeRateBook`, `WebExchangeRateProvider` base). References `Bodu.Numerics`. |
| `Bodu.Financial.DependencyInjection` | `AddFinancialService`, currency-resolution registration. |
| `Bodu.Globalization.Calendar` | 1.1.0 — multi-assembly rule resolution. **Behavioural change**: parameterless `NotableDateService()` no longer ships every region's rules; consumers must reference a data pack. |
| `Bodu.Globalization.Calendar.{Americas,AsiaPacific,Europe,MiddleEast,Africa}` | The five regional data packs (authoritative country set below). |
| `Bodu.Globalization.Calendar.{Builder,DependencyInjection,Plugins}` | Authoring API, DI registration, trust-gated plugin loader. |

**Wave 4 — exchange-rate providers, caching, and DI:**

| Package | Notes |
| --- | --- |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | Shared `AddWebExchangeRateProvider` machinery (named `HttpClient` + Polly resilience). |
| `Bodu.Financial.ExchangeRates.{Boe,Ecb,Rba,Yahoo,Ofx,Xe,Oanda}` | Per-source provider packages, each shipping its own DI extension. |
| `Bodu.Financial.ExchangeRates.Caching` | `CachingExchangeRateProvider`, `AggregatingExchangeRateProvider`, in-memory / TOML / JSON backends. |
| `Bodu.Financial.ExchangeRates.Caching.{Sqlite,Distributed}` | Durable SQLite and shared `IDistributedCache` backends. |

Authoritative country set for each data pack at release time:

| Data pack | Countries |
| --- | --- |
| `.Americas` | AR, BR, CA, CL, CO, MX, PE, US |
| `.AsiaPacific` | AU, CN, HK, ID, IN, JP, KR, MY, NZ, PH, SG, TH, TW, VN |
| `.Europe` | 28 EU/EEA (AT, BE, BG, CY, CZ, DE, DK, EE, ES, FI, FR, GB, GR, HR, HU, IE, IT, LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK); Orthodox-Easter overrides for GR, CY, BG, RO |
| `.MiddleEast` | AE, IL, JO, QA, SA, TR |
| `.Africa` | EG, ET, GH, KE, MA, NG, ZA |

**Versioning policy.** SemVer per package. Breaking changes inside a
single package bump that package's own major. The Calendar 1.1.0 wave is
a coordinated release — the breaking change in Calendar necessitates the
data packs, so they ship together. Git tags follow `<package>/v<version>`,
e.g. `Bodu.Numerics/v1.0.0`.

## Non-goals

The repository is deliberately not doing these. They appear here so
proposals can be closed quickly.

- **Wrapping or replacing `bc-csharp`.** The vendored Bouncy Castle
  source lives under `bc-csharp/` purely as a KAT reference for the
  cryptography test suites. It is not redistributed.
- **Classical prime-curve and RSA public-key cryptography.**
  `Bodu.Security.Cryptography` ships the curve25519 / edwards25519 and
  post-quantum primitives the BCL lacked on `net8.0` — Ed25519 (RFC
  8032), X25519 (RFC 7748), ML-KEM (FIPS 203), ML-DSA (FIPS 204), and
  HPKE (RFC 9180). Out of scope: RSA, DSA, and prime-curve ECDSA / ECDH,
  which are well covered by `System.Security.Cryptography`.
- **Re-implementing the plain ChaCha20-Poly1305 / AES-GCM AEADs.** Bodu
  ships the *raw* stream ciphers and the *extended-nonce* X-variant AEADs
  (XChaCha20-Poly1305, XSalsa20-Poly1305) the BCL lacks; the 12-byte-nonce
  `ChaCha20Poly1305` and `AesGcm` are used directly from the BCL (HPKE
  composes them).
- **A full IANA timezone database.** `Bodu.Globalization.Calendar`
  defers to `TimeZoneInfo`; it does not ship its own zone data.
- **A general-purpose JSON or XML parser.** `System.Text.Json` and
  `System.Xml` are sufficient. Bodu's structured-text libraries target
  formats the BCL does *not* ship a first-party parser for — Bencode,
  TOML, and YAML (YAML 1.2 core has no in-box .NET parser), plus the
  line-oriented Delimited / DotEnv / INI formats. *(This supersedes the
  previous blanket "no YAML" non-goal — `Bodu.Text.Yaml` has shipped.)*
- **A formula-evaluating / styling / writing Excel engine.**
  `Bodu.Formats.Excel.Binary` is a narrow read-only value reader over
  cached results; EPPlus / ClosedXML / NPOI cover the full-fidelity
  authoring space.
- **Shipping the `Plugin*.TestAssembly` projects as packages.** Those
  exist purely to exercise the calendar plugin loader in tests.
- **Duplicating algorithms already shipped in the .NET BCL or
  Microsoft's first-party `System.*` NuGet packages.** Where the
  framework ships a stable equivalent, consumers should use it directly.
  Concretely, the roadmap will not re-implement:
  `System.Security.Cryptography.ChaCha20Poly1305`, `AesGcm`, `HKDF`,
  `Rfc2898DeriveBytes.Pbkdf2`, `Shake128/256`, `CShake128/256`,
  `Kmac*`; `System.IO.Hashing.XxHash32/64/3/128`;
  `System.Buffers.Text.Base64` / `Base64Url`; `Convert.ToHexString` /
  `FromHexString`; `System.Formats.Cbor` / `System.Formats.Asn1`. Bodu
  only takes on algorithms with a genuine BCL gap.

## Active focus

The Bodu.Core hardening pass is closed and Core has since grown two new
structural pillars (graphs/trees) and the threading async-primitive suite.
The active focus is now:

1. **Cut Wave 1–2 packages** (Core, Numerics, IO.Hashing, Text.Encoding,
   Security.Cryptography, then the self-contained text/format libraries)
   to exercise package-validation on brand-new package IDs before the
   coordinated Calendar/Financial waves.
2. **Consolidate the two text-format tiers.** The repository now has two
   parallel shapes: the modern `Utf8*` ref-struct quartet
   (Bencode/Toml/Yaml) and the older `*Reader`/`*Writer`/`*Document`
   trio (Delimited/DotEnv/INI). Decide and document whether the older
   trio is retrofitted onto the quartet or the two tiers are an explicit
   API-design choice (see *Architectural patterns*).
3. **Close the remaining crypto key-encoding gap.** DER (PKCS#8 /
   SubjectPublicKeyInfo, incl. encrypted PKCS#8) already ships across the
   asymmetric family; **PEM text wrapping is the one remaining format**
   (`ImportFromPem` / `ExportPem`). Finalise it, then land the AVX-512
   capability-detection contract.
4. **Advertise history windows uniformly across FX providers.**
   `HistoryAvailability` is wired only for OANDA today; the other six
   providers should declare their earliest resolvable date so the caching
   / aggregation layer can reason about coverage.

## Per-project roadmap

### `Bodu.Core`

Current state: mature and broad; ~485 src / ~937 test files. Beyond the
buffers/collections/extensions base, it now carries:

- **`Collections.Generic.Graphs`** — `Graph<T>` (directed/undirected
  adjacency-list), the read-only graph interfaces, `GraphAlgorithms`
  (traversal, topological sort, connected components, Dijkstra),
  `ShortestPathResult<TVertex>`, and `DisjointSet` / `DisjointSet<T>`
  (union-find).
- **`Collections.Generic.Trees`** — `Tree<T>` (n-ary), `Trie` and
  `Trie<TValue>` (prefix trees).
- **`Threading`** — `AsyncLock`, `AsyncReaderWriterLock`,
  `AsyncSemaphore`, `AsyncManualResetEvent` / `AsyncAutoResetEvent` /
  `AsyncCountdownEvent`, `AsyncLazy<T>`, `AsyncDebouncer`, `RateGate`.
- **`Sequences`** (`SequenceGenerator`) and **`Functional`**
  (`Memoizer`) seams.
- A broadened collection catalogue: `SequencedDictionary<,>`
  (insertion-ordered), `MultiValueDictionary<,>`, `RangeDictionary<,>` /
  `RangeSet<T>`, `Multiset<T>`, `OrderedSet<T>`, `SegmentedBuffer<T>`,
  alongside the established `CircularBuffer<T>`, `Deque<T>`,
  `EvictingDictionary<,>`, `IndexedPriorityQueue<,>`, etc.

Forward-looking:

- **Extract `WeekPattern` to `Bodu.Globalization.WeekPattern`** now that
  it is a `readonly partial struct` with a struct enumerator, so
  globalization-adjacent packages can depend on the pattern without
  pulling all of Core.
- **Grow the `Functional` seam** — it currently holds only `Memoizer`.
  A `Result<T>` / `Option<T>` / `Either<TLeft,TRight>` railway-oriented
  set is the most-requested independently-built .NET surface (LanguageExt,
  CSharpFunctionalExtensions) and a natural fit for the seam. See
  *New library candidates* for the extraction option.
- **Probabilistic / sketch data structures** — Bloom filter, Count-Min
  sketch, HyperLogLog. Commonly built independently, absent from the BCL,
  and a clean fit for the collections pillar.

### `Bodu.Security.Cryptography`

Current state: mature; ~255 src / ~615 test files. Block ciphers
(Threefish 256/512/1024, Skipjack, Blowfish, Twofish, Camellia), the full
stream-cipher family (ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit,
HC-128 on the shared `IStreamCipher` / `StreamCipherTransform` stack),
AEAD (Ascon, the extended-nonce XChaCha20-Poly1305 / XSalsa20-Poly1305 on
`Poly1305AeadTransform`, plus mode transforms EAX/GCM/OCB/CCM/SIV/GCM-SIV),
keyed/crypto hashes (Skein, BLAKE2/3, Tiger, SipHash, Poly1305, FNV, Adler),
the asymmetric family (X25519, Ed25519, ML-KEM 512/768/1024, ML-DSA
44/65/87), the password KDFs (Argon2d/i/id, scrypt) and HKDF, and **HPKE
(RFC 9180)** with the DH-KEM-X25519 KEM and preset suites.

Forward-looking:

- **PEM key wrapping.** DER encodings (PKCS#8, SubjectPublicKeyInfo,
  encrypted PKCS#8 with `PbeParameters`) are fully implemented across the
  asymmetric types via their `*.KeyFormats.cs` partials; **PEM
  (`ImportFromPem` / `ExportPem`) is the one remaining key-encoding
  format** and closes the interop story.
- **AVX-512 capability-detection contract.** Document when the SIMD fast
  paths on BLAKE2/BLAKE3/Threefish engage and how to disable them in
  constant-time-sensitive contexts.
- **One-time-password codes (RFC 6238 TOTP / RFC 4226 HOTP).** A genuine
  BCL gap, near-universally pulled in as a third-party dependency
  (Otp.NET). Small, well-specified, and a natural companion to the KDF
  work — a candidate for a `Security.Otp` surface or a small sibling
  package.

### `Bodu.IO.Hashing`

Current state: mature; 83 src / ~235 test files. Fletcher 16/32/64, the
full RevEng CRC catalogue (112 standards), and the check-digit family
(Luhn, Damm, Verhoeff, Gumm, Code 39 mod 43, Crockford Base32 mod 37, ABA,
EAN, GTIN, IBAN, ISBN, ISIN, LEI, ISO 7064).

- **Unify every algorithm behind the BCL
  `System.IO.Hashing.NonCryptographicHashAlgorithm` shape.** Some types
  inherit it, others expose bespoke surfaces — the mix is a documentation
  hazard. This is the primary consistency debt in the project.
- **Keep documenting the `System.IO.Hashing` interop story.** xxHash and
  the single-polynomial `Crc32` / `Crc64` (ISO 3309) ship in Microsoft's
  package; the headline value here is the full 112-polynomial catalogue,
  the legacy non-cryptographic family, and the check-digit family — none
  of which are in the BCL.

### `Bodu.Text.Encoding`

Current state: mature; ~72 src / ~156 test files. Standalone encoders
`Base16`, `Base32` (Standard/HexExtended/Crockford/ZBase32), `Base45`
(RFC 9285), `Base58` / `Base58Check`, `Base62`, `Base64` / `Base64Url`,
`Base85` (Ascii85 / Z85 / GitCompact), the standalone HRP-carrying
`Bech32` / `Bech32m` (BIP-173 / BIP-350), plus `PercentEncoding` and
`QuotedPrintable`. Name-resolvable variants are exposed through the
`BinaryEncodings` / `IBinaryEncoding` registry.

- **Audit `Base*.Utf8.cs` writer parity.** Confirm every encoder's UTF-8
  span surface has full `IUtf8SpanFormattable`-style parity with the char
  paths — several skew char-first.
- **Reversible short-ID encodings (Sqids / the Hashids successor).**
  Heavily used independently-built functionality with no BCL equivalent;
  fits this project's alphabet-transform focus and the check-digit
  neighbour. See *New library candidates* for the `Bodu.Identifiers`
  option (ULID/Snowflake) that would consume the existing Crockford
  Base32 support.

### `Bodu.Text.Bencode`

Current state: new standalone library; ~86 src / ~81 test files. The full
`System.Text.Json`-shaped quartet — the ref-struct `Utf8BencodeReader` /
`Utf8BencodeWriter`, the `BencodeSerializer` POCO mapper (converters, the
full attribute family, naming policies, the string/number enum converters,
the four serialization callbacks), the read-only `BencodeDocument` /
`BencodeElement` DOM, and the mutable `BencodeNode` tree.

- **Add a conformance corpus.** Unlike TOML and YAML, Bencode has no
  vendored spec corpus — the test project is fixtures + unit tests. A
  BEP-3 malformed-input sweep in the Regression tier would raise it to
  the same maturity bar as its siblings.
- **Ship the read-only configuration source** (`AddBencodeStream`) in
  `Bodu.Extensions.Configuration.Text` — the one format bridge still
  owed there.

### `Bodu.Text.Toml`

Current state: new standalone library; ~123 src / ~89 test files. The most
mature of the three — the `Utf8TomlReader` / `Utf8TomlWriter` +
`TomlSerializer` + `TomlDocument` (read-only) / `TomlNode` (mutable) DOMs,
`TomlSpecVersion` (v1.0.0 default / opt-in v1.1.0), `TomlByteArrayHandling`
/ `TomlDecimalHandling`, validated against the vendored **toml-test
conformance corpus** (532 valid + 505 invalid cases) in both profiles.

- **Feature-complete for the grammar.** The remaining work is polish —
  benchmark the emitter's allocation profile against `Tomlyn`, and keep
  the vendored corpus current with upstream `toml-test`.

### `Bodu.Text.Yaml`

Current state: new; ~48 src / ~53 test files. The YAML 1.2 core-schema
quartet — the `Utf8YamlReader` (over the multi-partial `YamlParser`
handling anchors/aliases/tags/merge-keys and multi-document streams via
`YamlDocument.ParseAllDocuments`), the `Utf8YamlWriter`, the
**read-focused** `YamlSerializer`, and the `YamlDocument` / `YamlNode`
DOMs. Validated against the vendored `yaml-test-suite` (353 cases).

- **Bring the serializer to parity with Bencode/Toml.** The writer and
  serializer are the thinnest of the three — a minimal converter model,
  no rich attribute/metadata/callback suite. Round out the *write* path
  (attribute family, naming policies, callbacks) so `YamlSerializer` is a
  symmetric read+write mapper rather than a read-first one.
- **Document the supported-schema boundary.** Be explicit about which
  YAML 1.1/1.2 features are in vs out (tag resolution, complex keys,
  directives) so consumers know when to reach for a full YAML engine.

### `Bodu.Text.Formats`

Current state: mature; ~41 src / ~76 test files. **Bencode and TOML are
fully extracted** to their own libraries; this project is now the
line-oriented formats only — Delimited (RFC 4180 CSV/TSV), DotEnv, and INI,
each with a `*Reader` / `*Writer` / `*Document` trio and `ValueTask` async
streaming.

- **Resolve the two-tier shape (see *Active focus* #2).** Either retrofit
  Delimited/DotEnv/INI onto the modern `Utf8*` ref-struct quartet used by
  the standalone libraries, or document the tiering as an explicit
  design choice for line-oriented vs structured formats.
- **Add a source generator that binds `[DelimitedRecord]` and
  `[IniSection]` POCOs** so consumers can avoid runtime reflection — a
  clear AOT win.
- **Layer an `IAsyncEnumerable<T>` projection** over the existing
  `ReadAsync` loops.

### `Bodu.Text.Configuration`

Current state: mature; 37 src / ~74 test files. A layered
config-resolution engine (profile, resolver, view getters, diagnostics)
over the format readers.

- **Stabilise `ConfigurationPattern.Compile`** — the expression-
  compilation surface needs an API-stability pass before consumers build
  on it.
- **Add JSON-pointer / JMESPath-style resolvers** alongside the existing
  `ConfigurationResolver` to broaden applicability beyond the
  Bodu-specific query syntax.

### `Bodu.Extensions.Configuration.Text`

Current state: bridge layer connecting `Microsoft.Extensions.Configuration`
to the Bodu text stack. The read-only **TOML source has landed**
(`AddTomlFile` / `AddTomlStream`).

- **Add the Bencode configuration source** — the one remaining format
  bridge, mirroring the TOML provider shape.
- **Document precedence semantics** when stacked with the `Json` and
  `EnvironmentVariables` providers.

### `Bodu.Numerics`

Current state: new; ~31 src / ~33 test files. Ships `Fraction<T>` (exact
rationals over `IBinaryInteger<T>`, with continued-fraction, generic-math,
UTF-8, and parse/format surfaces) and `Interval<T>` (open/closed-endpoint
intervals over `INumber<T>`), each with JSON converters. Money/currency/FX
live in the companion `Bodu.Financial`.

- **Extend `Interval<T>`** — unbounded / half-bounded intervals,
  `Difference` / `SymmetricDifference` returning disjoint-interval sets,
  and algebraic `|` / `&` operators for guaranteed-contiguous operands.
  The 1.0 surface ships only the contiguous-result subset.
- **Grow the generic-math value-type catalogue.** The project is
  positioned as a home for `INumber<T>`-style building blocks; the
  strongest gap-fillers are a **`BigDecimal`** (arbitrary-precision
  decimal — no BCL equivalent), a **generic `Complex<T>`** (the BCL
  `Complex` is `double`-only), and small **running-statistics** aggregates
  (mean/variance/quantile). See *New library candidates*.

### `Bodu.Financial`

Current state: new and large; ~322 src / ~255 test files. Ships the
`Money` / `Money<TCurrency>` value types, the deferred-rounding
`CalculatedMoney`, the multi-currency `MoneyBag`, the ISO 4217 catalogue
(`CurrencyCode` source-generated enum, `CurrencyRegistry`, ~180
per-currency `ICurrency` types, `CurrencyLookupService`), formatting /
parsing (`MoneyFormatter`, `MoneyParseOptions`), rounding / allocation
policies (`IRoundingStrategy`, `MonetaryContext`, the policy enums), the
full FX abstraction stack (`ExchangeRate` / `ExchangeRate<TBase,TQuote>`,
`IExchangeRateProvider` / `IDatedExchangeRateProvider`, `ExchangeRateBook`
/ `ExchangeRateSeries`, `FixedDatedExchangeRateProvider`), and the
abstract `WebExchangeRateProvider` / `PairWebExchangeRateProvider<TSeries>`
bases the provider packages extend. References `Bodu.Numerics` for the
`Fraction<BigInteger>` exact-arithmetic escape hatch.

- **Ship the 1.0 package** (Wave 3) with its DI companion.
- **A second `IRoundingStrategy`.** The abstraction has a single
  implementation (`MidpointRoundingStrategy`); a
  banker's/stochastic/away-from-zero set would validate the seam.
- Possible v1.1: a **cross-currency Roslyn analyzer** for
  `Money<T1> + Money<T2>` catching generic-helper mixing the operator
  signatures cannot; a **`Bodu.Financial.Xml`** child package if XML
  serialisation is asked for; a **`MoneyBag` mutable builder** if
  benchmarks show hot-path allocation cost.

### `Bodu.Financial.DependencyInjection`

Current state: bridge; `IServiceCollection` extensions declared in the
`Bodu.Financial` namespace — `AddFinancialService`, the
`IFinancialServiceBuilder` chain, `UseCurrencyResolution`,
`FinancialOptions`, named monetary contexts.

- **Ship alongside `Bodu.Financial` in Wave 3.**
- **Add `IOptionsMonitor<FinancialOptions>` rebuild support** so
  rounding/context config changes propagate without a restart.

### `Bodu.Financial.ExchangeRates.*` *(provider + caching family)*

Current state: new and extensive. Seven web providers over the shared
`WebExchangeRateProvider` base, split into two architectural families:
central-bank whole-file sources (**Boe** IADB CSV, **Ecb** eurofxref XML,
**Rba** `.xls` eras) and arbitrary-pair sources over
`PairWebExchangeRateProvider<TSeries>` (**Yahoo** chart JSON, **Ofx**,
**Xe** — scrape-token auth, **Oanda** — rolling ~180-day window). The
shared `.DependencyInjection` package owns the `AddWebExchangeRateProvider`
machinery (named `HttpClient` + Polly resilience) each provider's `Add*`
extension delegates to. The provider-agnostic `.Caching` package supplies
the `CachingExchangeRateProvider` read-through decorator and the
`AggregatingExchangeRateProvider` (priority-fallback / average strategies,
per-pair routing) over `IExchangeRateCache`, with in-memory / TOML / JSON
backends in-package and durable `Sqlite` + shared `Distributed`
(`IDistributedCache` / Redis) backends as add-ons.

- **Advertise `HistoryAvailability` on every provider**, not just OANDA,
  so the caching and aggregation layers can reason about each source's
  earliest resolvable date.
- **Give Yahoo a dedicated provider subclass.** It is currently the
  generic pair base wired through an adapter — inconsistent with the
  other six and awkward to extend.
- **De-risk or de-emphasise the XE provider.** Its scrape-based
  `Authorization: Basic` token is documented as brittle; consider marking
  the package Experimental until an official-endpoint path exists.
- **Broaden provider coverage** if there is demand — Fixer,
  exchangerate.host, IMF, and FRED are the common free/commercial APIs
  not yet wrapped. Each is a small package following the established
  provider + DI shape.

### `Bodu.IO.Compound`

Current state: new; ~44 src / ~43 test files. A read + edit + authoring
implementation of the OLE2 / Compound File Binary (CFB) container — the
structured-storage envelope behind legacy Office files (`.xls`, `.doc`,
`.msg`). `CompoundFile` opens for read, edits transactionally
(`CreateStream` / `Delete` / `Rename` + `Commit` / `Revert`), and authors
from scratch through the fluent `CompoundStorageBuilder` /
`CompoundStreamBuilder` tree. Full OLE property-set surface
(`SummaryInformation`, `DocumentSummaryInformation`, `OlePropertySet` with
MS-OLEPS read/emit) and a complete exception hierarchy.

- **Writable stream cursors.** `CompoundStream` is a read-only cursor
  today; mutation goes through `CreateStream(content)` or the builders. A
  writable/seekable stream would round out the `IStream` counterpart.
- **This project is the substrate for new office-format readers** — see
  `Bodu.Formats.Excel.Binary` (already built on it) and the `.msg` /
  `.doc` candidates under *New library candidates*.

### `Bodu.Formats.Excel.Binary`

Current state: new; ~34 src / ~41 test files. A narrow, read-only reader
for the Excel 97-2003 BIFF8 `.xls` format over `Bodu.IO.Compound`.
Surfaces raw worksheet cell values (text, number, boolean, error, and a
formula's *cached* result) plus each sheet's used range, via a forward-only
`ExcelWorksheetReader` and a buffered `ExcelWorksheet` / `ExcelRow`
surface. The BIFF8 record layer is internal under `Bodu.Formats.Excel.Biff8`.
The namespace is deliberately flattened to `Bodu.Formats.Excel` (dropping
the `.Binary` suffix) **so a future Excel-format reader can share the value
model** — the same convention as `Bodu.Financial.ExchangeRates`.

- **`Bodu.Formats.Excel.OpenXml` sharing the value model.** The flattened
  namespace was chosen for exactly this — a read-only `.xlsx` reader over
  an OPC/ZIP container reusing `ExcelCell` / `ExcelWorksheet` /
  `ExcelWorkbookProperties`. See *New library candidates*.
- **BIFF5 fallback** for the oldest `.xls` variants, if demand appears
  (currently BIFF8-only; older versions raise
  `ExcelBinaryUnsupportedException`).

### `Bodu.Globalization.Calendar`

Current state: mature; ~150 src / ~219 test files. Easter (Western and
Orthodox), lunar/solar festivals, rule providers, observed-date
adjustments, `NotableDateService`, and the working-day extensions. Hebrew,
tabular Hijri, Umm al-Qura, and Persian observances ship as XML resources
resolved against the BCL calendars plus the `sweepCalendarYears` resolver.

- **Add observation-based algorithm variants** for the four lunar /
  solar-Hijri families where the BCL's tabular calculation can diverge
  from the announced civil date by a day (Saudi crescent sighting, Tehran
  vernal-equinox boundaries). Opt-in alternatives to the tabular
  resources, not replacements.
- **Extend the Hebcal-aligned regression catalogue** from the six-year
  starter set to a full 50-year sweep, and owe the same to the Umm
  al-Qura and Persian tables.
- **Add `IAsyncEnumerable<NotableDate>` projections** for streaming
  large multi-year date-range queries.

### `Bodu.Globalization.Calendar.Builder`

Current state: thin; fluent `NotableDateDocumentBuilder` authoring API
with XML + JSON-subset serialization and a loader that materializes a
`NotableDateResource`.

- **Add fluent rule-validation lint** with diagnostic codes mirroring
  `Bodu.Text.Configuration`'s diagnostic surface, for build-time feedback
  on rule-pack errors.
- **Ship an MSBuild task and `dotnet` tool** that compiles JSON rule
  packs to a sealed binary format — critical for trim/AOT scenarios (see
  the AOT theme).
- **Document round-trip guarantees** between builder output and the JSON
  resource rule provider.

### `Bodu.Globalization.Calendar.DependencyInjection`

Current state: bridge; `AddNotableDateService` /
`AddReloadableNotableDateService` (declared in the `Bodu.Globalization.Calendar`
namespace).

- **Add key-aware `AddNotableDateService("AU")`** for multi-tenant
  processes serving multiple jurisdictions.
- **Add `IHostedService` cache warm-up** so the first post-start request
  does not pay the rule-load cost.
- **Add `IOptionsMonitor<NotableDateOptions>` rebuild support**.

### `Bodu.Globalization.Calendar.Plugins`

Current state: new; trust-gated external plugin loader for assemblies
contributing custom `INotableDateAlgorithm` implementations.

- **This is the AOT-blocking component** (reflective assembly load). Its
  path to AOT-compatibility is the binary-rule-pack format from the
  Builder roadmap; until then it is correctly marked AOT-incompatible.
- **Document the trust-gate contract** — how assemblies are validated and
  what the security boundary guarantees.

### `Bodu.Globalization.Calendar.Data.*` *(regional packs)*

Current state: five packs shipping in Wave 3 — Americas, AsiaPacific,
Europe, MiddleEast, Africa (country sets in the release table). Each is a
self-contained embedded pack importing the shared catalogues through a
`<region>-common` hub.

- **Subdivision-level data is the common open gap** across every pack.
  US states, Canadian provinces, AU states, German *Länder*, and the UK
  constituent countries already ship; the remaining bulk of regional
  holidays (Brazilian/Mexican states, Indian/Indonesian/Philippine
  subdivisions, Spanish autonomous communities, Swiss cantons) is
  subdivision-specific.
- **Document holiday-source citations** per country so consumers can
  audit each rule pack against authoritative sources.
- **Targeted country additions**: Iran (IR) via `global-persian.xml` in
  MiddleEast (in the original v1 set, not yet shipped); Switzerland (CH)
  in Europe alongside its canton data. **Verify Ethiopia's Ge'ez-calendar
  coverage** in Africa.
- **Ship fiscal-calendar packs** (US federal FY, retail 4-5-4) as the
  next natural "notable dates" layer beyond religious/civil holidays.
- **Multi-day Chinese New Year / Lunar New Year regional variants** in
  AsiaPacific (today the rule fires for the single primary date), and
  wire Saudi-sighting subdivisions to `global-islamic-umm-al-qura.xml`.

### `Bodu.Test` *(shared test infrastructure)*

Current state: infrastructure project; shared assertions, stream mocks,
the `IKat` marker, the generic KAT primitives, and the one multi-consumer
contract base. Not published.

- **Promote `Bodu.Test.Kat` as a public NuGet** so downstream consumers
  can plug into the same testing model.
- **Add a benchmark-results contract** so the `bench/` projects produce
  comparable JSON across the Encoding / Configuration / Formats /
  Cryptography suites.

### `Bodu.CodeStyle` *(separate solution)*

Current state: independent analyzer / code-fix / XML-doc-formatter
solution, not in `bodu.slnx`.

- **Document each analyzer code** under `docs/codestyle/` (rule,
  rationale, examples, suppression guidance).
- **Add code-fix coverage** for every rule that currently only diagnoses.
- **Publish a JSON-schema** for `bodu.xmldocstyle.json`.

### `bc-csharp` *(vendored)*

Bouncy Castle source vendored as a crypto KAT reference. Non-goal: do not
redistribute, do not extend.

## New library candidates

Proposed *net-new* projects. Each fills a genuine BCL / ecosystem gap,
targets functionality that today is pulled in as an independently-developed
GitHub dependency, and reuses one of the repository's proven architectural
patterns (see *Architectural patterns*). Listed roughly by leverage.

- **`Bodu.Formats.Outlook.Msg`** — a read-only `.msg` (Outlook message)
  reader over `Bodu.IO.Compound`. The `.msg` container *is* CFB, so this
  is the highest-leverage reuse of the existing container: it inherits
  the full CFB read stack and only adds MAPI property-name decoding.
  Independently, `.msg` reading is served almost entirely by commercial
  libraries or `MsgReader`; there is no first-party option.
- **`Bodu.Formats.Excel.OpenXml`** — a read-only `.xlsx` value reader over
  an OPC/ZIP container, **sharing the flattened `Bodu.Formats.Excel`
  value model** (`ExcelCell` / `ExcelWorksheet` / `ExcelWorkbookProperties`).
  The namespace was already flattened in anticipation of this. Would
  likely sit on a new **`Bodu.IO.Packaging`** (Open Packaging Convention
  over `System.IO.Compression`) container — the ZIP-era sibling to
  `Bodu.IO.Compound`.
- **`Bodu.Identifiers`** — ULID, Snowflake, NanoID, KSUID generation and
  parsing. Ubiquitous independently-built functionality with no BCL home,
  and a natural consumer of the existing Crockford Base32 support (in
  `Bodu.IO.Hashing`) and the `Bodu.Text.Encoding` alphabets. Pairs
  naturally with **Sqids** (reversible short-ID encoding), which could
  live here or in `Bodu.Text.Encoding`.
- **`Bodu.Functional`** — if the `Bodu.Core/Functional` seam grows beyond
  a couple of types, extract `Result<T>` / `Option<T>` /
  `Either<TLeft,TRight>` and the railway-oriented combinators into a
  dedicated package rather than bloating Core. This is the single
  most-reached-for independently-built .NET surface.
- **Numeric value types in `Bodu.Numerics`** — `BigDecimal`
  (arbitrary-precision decimal, no BCL equivalent), generic `Complex<T>`,
  and running-statistics aggregates. These extend the existing generic-math
  project rather than needing a new one.
- **One-time-password codes (TOTP/HOTP)** — RFC 6238 / RFC 4226, either
  in `Bodu.Security.Cryptography` or a small `Bodu.Security.Otp` sibling.
  A well-specified, universally-needed gap.
- **Probabilistic data structures** — Bloom filter, Count-Min sketch,
  HyperLogLog. Could extend `Bodu.Core`'s collections pillar or form a
  focused `Bodu.Collections.Probabilistic` package.

## Cross-cutting themes

### Architectural patterns

Three architectures have proven themselves across the repository. Treat
them as first-class templates, and conform new work to the closest match
rather than inventing a fourth shape.

1. **The `System.Text.Json`-shaped quartet** — a ref-struct
   `Utf8*Reader` / `Utf8*Writer` token surface, a `*Serializer` POCO
   mapper (converters + attribute family + naming policies + callbacks),
   a mutable `*Node` DOM, and a read-only `*Document` DOM. Proven by
   `Bodu.Text.Bencode`, `Bodu.Text.Toml`, and `Bodu.Text.Yaml`. **This is
   the template for every new structured-text format** and the shape the
   older `Bodu.Text.Formats` trio should either be retrofitted onto or be
   explicitly documented as tiered against (see *Active focus* #2).
2. **The container + format-reader split** — a low-level container
   (`Bodu.IO.Compound` for CFB; a proposed `Bodu.IO.Packaging` for OPC)
   with format readers layered on top that share a *flattened* value
   model (`Bodu.Formats.Excel.*`). New office/document readers (`.msg`,
   `.doc`, `.xlsx`) plug into this split.
3. **The resilient web-data-provider stack** — an abstract provider base
   (`WebExchangeRateProvider`) owning `HttpClient` + Polly resilience and
   single-flight coalescing, a read-through cache decorator, an
   aggregator with pluggable strategies over a storage-agnostic cache
   contract, and per-package DI extensions delegating to shared
   registration machinery. Proven by `Bodu.Financial.ExchangeRates.*`.
   Any future networked reference-data source should adopt this shape.

A fourth pattern — the **contract-test base + KAT record** model — is
already the universal testing convention (see `CLAUDE.md`).

### TFM policy

All shipping projects target `net8.0` only. Direction: follow Microsoft's
LTS cadence — move the floor to `net10.0` when `net8.0` exits standard
support, and never multi-target older `netstandard` without a concrete
consumer ask. The dead `netstandard2.0` `ItemGroup` conditionals in a few
`.csproj` files should be removed in the next routine sweep.

### AOT and trim readiness

No project sets `IsAotCompatible` or `IsTrimmable` today. Target state:

- **AOT-clean (achievable now):** `Bodu.Core`, `Bodu.Numerics`,
  `Bodu.IO.Hashing`, `Bodu.IO.Compound`, `Bodu.Text.Encoding`,
  `Bodu.Security.Cryptography`, and the three `Utf8*` text libraries
  (`Bodu.Text.Bencode` / `.Toml` / `.Yaml`) — the ref-struct readers are
  reflection-free on the token path.
- **AOT-clean with work:** `Bodu.Text.Formats` and the `*Serializer`
  reflection paths (need the source-generator binding), `Bodu.Financial`
  and `Bodu.Formats.Excel.Binary` (audit the property-mapping paths).
- **AOT-blocked by design:** the `Bodu.Globalization.Calendar.Plugins`
  loader — needs the binary-rule-pack format from the Builder roadmap
  before this changes.

### API-stability tiers

**Done.** Every packable project now carries a single tier label as a
blockquote directly under its README title. The assignment follows the
policy below: all packages are **Stable** except `Bodu.Text.Yaml`
(**Preview** — the serializer is read-first and its write surface is still
being rounded out) and `Bodu.Financial.ExchangeRates.Xe` (**Experimental**
— it depends on a scraped auth token). **Preview** / **Experimental**
remain available for any future package whose surface is still settling.
Revisit the tier of the newest, network-dependent provider packages before
their first release if their endpoints prove unstable.

### Source generators

Code generation today is **tooling-based, not Roslyn**: the CRC catalogue
is generated by the `tools/Generate-CrcCatalog.ps1` script (from
`crc-specs.json`), and the ISO 4217 `CurrencyCode` enum + registration by
the `tools/CurrencyCatalogueGenerator` console tool (from
`currencies.json`). These run out-of-band and check their output into
source; there is no incremental-source-generator infrastructure in the
tree yet.

The forward direction is to introduce true Roslyn generators where they
buy AOT/trim readiness or remove runtime reflection:

- Calendar rule packs (Builder roadmap — binary output for trim/AOT).
- Delimited / INI POCO binding (Text.Formats roadmap).

New generators should live under `<Project>.Builder/` mirroring the
Calendar.Builder layout.

### Package validation rollout

**Done.** `EnablePackageValidation` (+ `ApiCompatStrictMode`) is now on by
default for every shipping, non-test, non-benchmark package. The default
is set in `Directory.Build.props` (`BoduEnablePackageValidation`, opt-out
per project or per build) and the gate is applied in
`Directory.Build.targets`, where `IsTestProject` and the benchmark flag
are resolved. Validation is inert until a `PackageValidationBaselineVersion`
exists (nothing is published yet), so it costs nothing today; once the
first packages ship, set each package's baseline so the gate begins
catching accidental breaking changes at pack time.

### Documentation parity

**Largely achieved.** Every shipping project has a `docs/guides/<project>/`
entry, and coverage is broad — including the articles the previous roadmap
listed as owed (`numerics/fraction.md`, `serialization/yaml/`,
`financial/exchange-rate-caching.md` and the other FX guides). The one
remaining gap is a dedicated **calendar plugin-loader** guide under
`docs/guides/calendar/` (the loader is currently covered only implicitly by
`building-the-service.md` / `dependency-injection.md`).

## Proposing changes to this file

Treat this file the same as any other source change — open a PR, link the
issue or discussion that motivates the change, and bump the "Last updated"
line at the top. Changes should be **directional** (add a project, change
a non-goal, retire an item) rather than release-tracking.
