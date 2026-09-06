# Roadmap

Forward-looking plan for the **Bodu** C# utility library. Pairs with
[`CLAUDE.md`](CLAUDE.md) (repository conventions for contributors).

*Last updated: 2026-08-19. Since the previous revision, the
**`Bodu.IO.Pst` LTP layer (P1) landed** — heap-on-node, BTree-on-heap,
and the public property-context and table-context views on `PstNode`,
pinned to the `lspst` oracle corpus (see the per-project section) — the
**`Bodu.Text.Yaml` serializer reached parity with Bencode/Toml** (the
wire-name enum tier with the public string/number enum converters,
`DefaultIgnoreCondition` replacing the pre-release `IgnoreNullValues`,
the `YamlSerializerDefaults` presets, the native-sized and 128-bit
integer widths, and the `NodeConverter`/`YamlElement`/`YamlDocument`
serializer bridges), the lock-step baseline was revved to **0.3.0**, and
**`Bodu.Text.Filtering` was registered** — the include/exclude text
filtering engine landed complete on 2026-08-02 (#648) but had never been
entered here or in the shipping manifest; it now has a per-project
section below and a Wave-2 manifest entry — and, earlier,
**`Bodu.Financial` was restructured**: the currency surface now lives in
`Bodu.Financial.Currencies`, the FX core in
`Bodu.Financial.ExchangeRates`, the `Exchange`-stuttered type names were
de-stuttered (`RateBook`, `RateSeries`, `CachingRateProvider`, …), and
the `WebRateProvider` / `PairWebRateProvider<TSeries>` web-provider
machinery was extracted into its own **`Bodu.Financial.ExchangeRates`
package** that every per-source provider references. A
**runnable-samples suite** landed alongside — a `samples/` tree covering
Financial, Calendar, the text libraries, and the IO group (IO.Hashing,
IO.Compound, Formats.Excel), with snippet-compile guards keeping the
guide snippets building, a live FX sample, and a testing
guide — and the FX libraries gained a **swallowed-exception logging
pass** plus follow-up renames (`IHistoryAwareRateProvider` →
`IHistoricalRateProvider`, `RbaEra` → `RbaEraWorkbook`). Before that,
step 2 of the
**Numerics growth wave landed — `BigDecimal`** ships as an
arbitrary-precision decimal with the full `INumber<BigDecimal>`
generic-math surface, span/UTF-8 parse and format, rounding, and a
JSON converter registered through `AddNumericsJsonConverters` — and
step 3, the **running-statistics aggregates**, has landed as well
(`RunningStatistics<T>` / `RunningQuantile<T>` accumulators and the
rolling-window `MovingSum<T>` / `MovingMinMax<T>` companions),
**completing the Numerics growth wave** (see Active focus).
`Bodu.Security.Cryptography` also gained **Merkle
tree hashing** (`MerkleTreeHash` / `ParallelMerkleTreeHash` with
RFC 6962-style leaf/node domain separation and length binding), an
untrusted-input **hardening pass** swept the parsers and AEAD
transforms (bounds validation in the calendar-document and CFB-sector
readers, a constant-time GCM-SIV GF multiply, EAX CMAC zeroing on
fault), and every packable project now carries generated NuGet
icon + hero-banner artwork wired through `bld/Packaging.props` —
another release-readiness gate cleared ahead of the Wave 1 cut.
Before that, the
**`Bodu.Collections` package split was executed** (the
collections pillar — `Collections.Generic` / `.Concurrent` / `.Graphs`
/ `.Trees` — now ships as its own package referencing Core, namespaces
unchanged) and the `WeekPattern` extraction was retired as unnecessary
post-split; both decisions are recorded in
`Bodu.Core/docs/roadmap-implementation-plan.md`. Earlier still, the
`Bodu.Security.Cryptography` interop wave has merged — RFC 7468 **PEM**
key wrapping for Ed25519 / X25519 (closing the key-encoding story), the
`Bodu.Security.Cryptography.DisableSimd` **AVX-512 opt-out** and capability
contract, and the **`Hotp` / `Totp`** one-time-password codes — and the
**Numerics growth wave** began (now complete: `Interval<T>` extensions,
`BigDecimal`, and the running-statistics aggregates have all
landed — see Active focus). A
release-discipline pass earlier moved `Bodu.Numerics` from Stable to
**Preview**; its serialization and documentation conventions have since
caught up (JSON now ships in the `Bodu.Numerics.Serialization.Json`
companion), and the tier now holds only while the new `BigDecimal` and
statistics surfaces settle (see *API-stability
tiers*). This revision also folds in a **cross-ecosystem gap review** —
Bodu's surface compared against the highest-adoption Java (Guava, Apache
Commons, libphonenumber, ical4j / Quartz, Caffeine) and Python (stdlib
`difflib` / `email`, `dateutil`, `rapidfuzz`, `phonenumbers`, `pint`)
utility staples that .NET consumers currently obtain from
independently-built packages — expanding *New library candidates*, the
`Bodu.Core` forward list, and *Non-goals* accordingly. The
broader landed-surface summary from the prior rewrite still stands: (1) a
**Bodu.Core** structural expansion — the
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
**`Bodu.Financial.ExchangeRates.*` ecosystem** — eleven web providers (Boe,
Ecb, Rba, Yahoo, Ofx, Xe, Oanda, Fixer, ExchangeRateHost, Fred, Imf) over a
shared `WebRateProvider` base, the provider-agnostic caching layer (`CachingRateProvider`,
`AggregatingRateProvider`) with in-memory / TOML / JSON / SQLite /
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
  Per-package NuGet icon and hero-banner artwork is generated by the
  `bld/artwork/` scripts and mapped to each `PackageId` via the manifest
  in `bld/Packaging.props` — a new package gets its artwork through that
  manifest, not ad-hoc image files.
- **Package validation.** `BoduEnablePackageValidation` is opt-in today;
  the roadmap commits to making it the default on all packable projects.

## Release focus

No package has been tagged or published yet. The first publish is
organised into waves so package-validation and dependency ordering are
exercised on the smallest self-contained units first.

**Wave 1 — foundation packages (no inter-Bodu package dependencies):**

| Package | Notes |
| --- | --- |
| `Bodu.Core` | The dependency root — buffers, extension surfaces, threading primitives, sequences, `WeekPattern`, `ThrowHelper`, text-encoding utilities. |
| `Bodu.Collections` | The specialized collection catalogue (incl. the graphs/trees pillars), split out of Core; references `Bodu.Core`. Namespaces unchanged (`Bodu.Collections.*`). |
| `Bodu.Numerics` | `Fraction<T>` over `IBinaryInteger<T>` and the interval algebra (`Interval<T>` / `DiscreteInterval<T>` / `IntervalSet<T>`) over `INumber<T>`. Serialization-agnostic — no `System.Text.Json` dependency. |
| `Bodu.Numerics.Serialization.Json` | `System.Text.Json` integration for `Bodu.Numerics` (`AddNumericsJsonConverters`, `NumericsJsonPolicy`, per-type converters). References `Bodu.Numerics`. |
| `Bodu.IO.Hashing` | Non-cryptographic hashing + the full RevEng CRC catalogue + the check-digit family. |
| `Bodu.Text.Encoding` | Base16/32/58/62/64/85 + Base45 + Bech32/Bech32m. |
| `Bodu.Security.Cryptography` | Block/stream ciphers, AEAD, keyed/crypto hashes, the asymmetric family, KDFs, HPKE. |

**Wave 2 — self-contained format & text libraries:**

| Package | Notes |
| --- | --- |
| `Bodu.Text.Bencode` | Standalone STJ-shaped Bencode library (reader/writer/serializer/DOM quartet). |
| `Bodu.Text.Toml` | Standalone STJ-shaped TOML v1.0.0 / v1.1.0 library; corpus-backed. |
| `Bodu.Text.Yaml` | Standalone YAML 1.2 core-profile library (symmetric read+write serializer at family parity). |
| `Bodu.Text.Serialization` | The shared serialization primitives (attribute family, naming policies, callbacks) the per-format serializers build on. |
| `Bodu.Text.Delimited` | Standalone STJ-shaped Delimited (RFC 4180 CSV/TSV) library; corpus-backed. |
| `Bodu.Text.DotEnv` | Standalone STJ-shaped DotEnv library. |
| `Bodu.Text.Ini` | Standalone STJ-shaped INI library (comment-preserving mutable DOM). |
| `Bodu.Text.Formats` | Umbrella meta-package over `Bodu.Text.Delimited` / `.DotEnv` / `.Ini`. |
| `Bodu.Text.Configuration` | INI-compatible profile, resolver, view getters (self-contained document model). |
| `Bodu.Text.Filtering` | Include/exclude text filtering engine — glob + regex patterns compiled into a cost-tiered matcher (Core-only). |
| `Bodu.IO.Compound` | OLE2 / CFB container read + edit + authoring. |
| `Bodu.Formats.Excel.Binary` | Read-only BIFF8 `.xls` reader (depends on `Bodu.IO.Compound`). |
| `Bodu.Formats.Outlook` | The shared, container-free MAPI value model. |
| `Bodu.Formats.Outlook.Msg` | Read-only `.msg` (MS-OXMSG) reader over `Bodu.IO.Compound`. |

**Wave 3 — financial core + calendar (coordinated breaking change):**

| Package | Notes |
| --- | --- |
| `Bodu.Financial` | `Money` / `Money<TCurrency>`, `CalculatedMoney`, `MoneyBag`, the ISO 4217 catalogue, rounding/allocation policies, and the FX abstractions (`ExchangeRate`, `IRateProvider` / `IDatedRateProvider`, `RateBook`, `WebRateProvider` base). References `Bodu.Numerics`. |
| `Bodu.Financial.DependencyInjection` | `AddFinancialService`, currency-resolution registration. |
| `Bodu.Globalization.Calendar` | 1.1.0 — multi-assembly rule resolution. **Behavioural change**: parameterless `NotableDateService()` no longer ships every region's rules; consumers must reference a data pack. |
| `Bodu.Globalization.Calendar.{Americas,AsiaPacific,Europe,MiddleEast,Africa}` | The five regional data packs (authoritative country set below). |
| `Bodu.Globalization.Calendar.{Builder,DependencyInjection,Plugins}` | Authoring API, DI registration, trust-gated plugin loader. |

**Wave 4 — exchange-rate providers, caching, and DI:**

| Package | Notes |
| --- | --- |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | Shared `AddWebRateProvider` machinery (named `HttpClient` + Polly resilience). |
| `Bodu.Financial.ExchangeRates.{Boe,Ecb,Rba,Yahoo,Ofx,Xe,Oanda,Fixer,ExchangeRateHost,Fred,Imf}` | Per-source provider packages, each shipping its own DI extension. |
| `Bodu.Financial.ExchangeRates.Caching` | `CachingRateProvider`, `AggregatingRateProvider`, in-memory / TOML / JSON backends. |
| `Bodu.Financial.ExchangeRates.Caching.{Sqlite,Distributed}` | Durable SQLite and shared `IDistributedCache` backends. |

Authoritative country set for each data pack at release time:

| Data pack | Countries |
| --- | --- |
| `.Americas` | AR, BR, CA, CL, CO, MX, PE, US |
| `.AsiaPacific` | AU, CN, HK, ID, IN, JP, KR, MY, NZ, PH, SG, TH, TW, VN |
| `.Europe` | 28 EU/EEA (AT, BE, BG, CY, CZ, DE, DK, EE, ES, FI, FR, GB, GR, HR, HU, IE, IT, LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK); Orthodox-Easter overrides for GR, CY, BG, RO |
| `.MiddleEast` | AE, IL, JO, QA, SA, TR |
| `.Africa` | EG, ET, GH, KE, MA, NG, ZA |

**Versioning policy.** Packages version in **lock-step** at the shared
`BoduBaseVersion` (`bld/Versioning.props`), so a matched version number
across `Bodu.*` packages is a coherent set; SemVer discipline still
applies to each package's surface. A single git tag `v<version>` releases
every package listed in the shipping manifest
(`bld/release-manifest.txt`) — the release workflow packs the whole
solution but publishes only the manifest set. Later waves append their
package ids to the manifest and bump the base version (the coordinated
Calendar wave is slated 1.1.0, shipping together with its data packs);
earlier packages re-publish at the new coherent version. Per-package
divergence is reserved for out-of-band fixes via
`BoduPackageVersionOverride`. See `bld/RELEASING.md` for the full
procedure. *(This supersedes the earlier `<package>/v<version>`
per-package tag scheme, which predated the lock-step baseline.)*

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
- **Shipping the plugin fixture projects as packages.** The
  `Bodu.Globalization.Calendar.Plugins.TestPlugin*` fixture assemblies
  (under the Plugins test project's `Fixtures/`) exist purely to
  exercise the calendar plugin loader in tests.
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
- **HTML parsing or sanitization.** The jsoup / BeautifulSoup niche is
  deliberately out: the full WHATWG parsing algorithm is an enormous
  grammar surface, and AngleSharp / HtmlAgilityPack already serve it
  well.
- **Template engines.** The Jinja2 / Freemarker / Mustache space is
  served by Scriban and Fluid; a text-templating language is a product,
  not a utility building block.
- **A date-time model replacement.** No Joda-Time / NodaTime analogue:
  `DateOnly` / `TimeOnly` / `DateTimeOffset` / `TimeZoneInfo` plus
  NodaTime cover the space. Bodu stays at the layer above — notable
  dates, working days, and (proposed) recurrence.
- **DataFrames, ndarrays, and scientific computing.** The pandas / NumPy
  space belongs to Microsoft.Data.Analysis, ML.NET, and TorchSharp;
  `Bodu.Numerics` stays at value-type scalars, intervals, and
  accumulators.
- **New compression codecs.** No zstd / LZ4 / xz reimplementation (the
  commons-compress niche): these are performance-critical formats where
  the existing bindings (ZstdSharp, K4os.Compression.LZ4) are the right
  answer, and the BCL covers deflate / gzip / Brotli / ZIP / TAR.
- **Object mappers and validation frameworks.** No AutoMapper /
  MapStruct or pydantic / Hibernate-Validator analogue — mapping is
  application code, and DataAnnotations / FluentValidation own the
  validation space.
- **A Disruptor-style ring buffer / sequencer.** The LMAX Disruptor
  niche (pre-allocated slots, sequence-claimed publishing, batch
  consumers, wait strategies) is not a utility building block — it is a
  specialized messaging engine with its own .NET port (Disruptor-net).
  For bounded concurrent streaming the first-party
  `System.Threading.Channels` already covers the space (bounded
  capacity, `DropOldest` / `DropNewest` full modes, single-reader /
  single-writer fast paths), and Bodu ships
  `ConcurrentCircularBuffer<T>` for the simple concurrent-ring case.

## Active focus

The Bodu.Core hardening pass is closed and Core has since grown two new
structural pillars (graphs/trees) and the threading async-primitive suite.
The `Bodu.Security.Cryptography` interop wave has also landed — RFC 7468
**PEM** wrapping for Ed25519 / X25519 (closing the key-encoding story), the
`Bodu.Security.Cryptography.DisableSimd` **AVX-512 opt-out** and capability
contract, and the **HOTP / TOTP** one-time-password codes. The active focus
is now:

1. **The Numerics growth wave — complete.** ✅ All three sequenced steps
   have landed (details in the `Bodu.Numerics` section); the engineering
   focus now shifts to items 2–3 below:
   - **`Interval<T>` extensions have landed.** ✅ Unbounded / half-bounded
     endpoints (explicit metadata, not float sentinels; `AtLeast` / `AtMost`
     / `All` …), `Difference` / `SymmetricDifference` returning the
     allocation-free `IntervalPair<T>` (≤2 disjoint pieces), and the `&` / `|`
     operators. The architecture review's continuous-vs-discrete finding was
     also resolved by adding **`DiscreteInterval<T>`** (integer domain,
     successor-aware emptiness and adjacency), plus hardening (NaN rejection,
     culture-safe text) and an exhaustive membership-law test harness. The
     `DiscreteInterval<T>` `Difference` / `SymmetricDifference` follow-up (via
     `DiscreteIntervalPair<T>`) and the first-class N-ary `IntervalSet<T>`
     (normalized disconnected ranges with N-ary union / intersection /
     `Except` / `Complement`) have since landed as well.
   - **`BigDecimal` has landed.** ✅ An arbitrary-precision decimal
     (`BigInteger` mantissa + scale) with the full `INumber<BigDecimal>`
     generic-math surface, span/UTF-8 parse and format, rounding, and a
     `BigDecimalJsonConverter` registered through
     `AddNumericsJsonConverters` alongside the existing `Fraction<T>` /
     `Interval<T>` converters.
   - **Running-statistics aggregates have landed.** ✅ Online
     mean / variance (Welford, with the Chan et al. parallel `Combine`),
     exact min / max / count, and a streaming quantile (P²) as mutable
     struct accumulators, plus the rolling-window `MovingSum<T>` /
     `MovingMinMax<T>` companions. (`Complex<T>` — the generic follow-on
     to this wave — has since **landed** as well; see the `Bodu.Numerics`
     section.)
2. **Advertise history windows uniformly across FX providers — done.** ✅
   All seven providers now declare `HistoryAvailability`: ECB computes it
   from the configured feeds (full-history feed → since the 1999-01-04
   euro epoch), RBA from the configured era catalogue (1983-01-01 for the
   default), BoE via a settable options floor (1975-01-02 daily-spot
   inception), Yahoo since the 2003-12-01 chart inception, OFX as a
   deliberate documented Unbounded, XE as an estimated ten-year rolling
   window, alongside OANDA's existing ~180-day window; the shared
   pair-provider contract test now forces every future provider to
   declare deliberately. The consuming side has since landed as well:
   the new `IHistoricalRateProvider` capability interface is
   implemented across the provider base, the fixed-book provider, and
   both decorators; `CachingRateProvider` clamps or skips
   fetches outside the inner source's advertised history (recording the
   unavailable prefix as covered-with-no-rows); and
   `AggregatingRateProvider` drops candidates that declared they
   cannot serve the requested date or window before the strategy runs —
   both behind `RespectHistoryAvailability` flags that default on. The
   Preview→Stable promotion for the FX family now waits only on
   live-endpoint soak per the stability-tier policy.
3. **Cut Wave 1–2 packages — release-readiness landed; tag to publish.**
   The shipping manifest (`bld/release-manifest.txt`, the Wave 1–2
   package ids) now scopes what the release workflow publishes (pack
   stays full-solution; only the manifest set is pushed), the missing
   `Bodu.Numerics.Serialization.Json` README landed, the package
   repository/project URL metadata was corrected, and the
   package-validation baseline is wired (inert until the first publish).
   The remaining action is the release itself: tag `v1.0.0` per
   `bld/RELEASING.md`, then set `BoduPackageValidationBaseline` so
   ApiCompat begins guarding the published surface.
4. **Consolidate the two text-format tiers — done.** ✅ The older
   line-oriented trio was redesigned from the ground up onto the
   `System.Text.Json`-shaped quartet: **`Bodu.Text.Delimited`**,
   **`Bodu.Text.DotEnv`**, and **`Bodu.Text.Ini`** are now standalone
   libraries (ref-struct `Utf8*Reader`/`Utf8*Writer`, `*Serializer` with
   the shared attribute/naming/callback layer, mutable `*Node` DOM,
   read-only `*Document` DOM), `Bodu.Text.Formats` became a thin umbrella
   meta-package over the three, `Bodu.Text.Configuration` was decoupled
   onto its own INI document model, and the Imf/Boe FX parsers and
   samples migrated. Two deliberate deviations are documented in the
   design notes under `Bodu.Text.Formats/docs/`: the mutable DotEnv/INI
   DOMs bear comment trivia (faithful round-trips of human-owned files),
   and the string-only wire keeps scalar conversion serializer-local
   rather than adopting the shared recursive converter engine. The
   quartet is now the single structured-text template across the tree.

## Per-project roadmap

### `Bodu.Core` (and `Bodu.Collections`)

Current state: mature and broad. The collections pillar now ships as
the separate **`Bodu.Collections`** package (split executed — see the
first forward-looking item), leaving `Bodu.Core` as the primitive
layer (buffers, extensions, threading, sequences, functional seam,
text utilities, `ThrowHelper`, `WeekPattern`). Across the pair the
surface carries:

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

- **The `Bodu.Collections` package split has landed.** ✅ The collections
  pillar (`Collections.Generic` + `.Concurrent` + `.Graphs` + `.Trees`)
  now ships as the **`Bodu.Collections`** package (referencing Core),
  leaving Core as the small always-referenced primitive layer.
  Namespaces did not change — only the assembly/package boundary moved.
  `ShuffleHelpers` and the internal `SequenceUtility` stayed in Core
  because the staying `IEnumerableExtensions` partials
  (`Randomize` / `ContainsAll` / `ContainsAny`) depend on them, and
  `Bodu.Collections` carries its own `CollectionsResourceStrings` resx
  per the per-project resource convention. New collection-type work
  below lands in `Bodu.Collections`; naming stays BCL-style
  (`MultiValueDictionary`, `RangeDictionary`, `BiDictionary`,
  `Multiset`, `EvictingDictionary`) rather than the Java-style
  `MultiMap` / `RangeMap` / `BiMap` / `MultiSet` / `LruCache` synonyms.
  See `Bodu.Core/docs/roadmap-implementation-plan.md` (T0) for the full
  decision record.
- **The concurrent variants now ship as `Bodu.Collections.Concurrent`.** ✅
  A follow-on split (post-plan decision D5): `ConcurrentCircularBuffer<T>`
  and `ConcurrentHashSet<T>` moved to their own opt-in package
  referencing `Bodu.Collections` (namespace unchanged), with the shared
  contract-test bases promoted to `Bodu.Test.Contracts` per the
  documented second-consumer rule and the package carrying its own
  resource strings.
- **`ConcurrentEvictingDictionary<TKey,TValue>` has landed.** ✅ The
  thread-safe variant of `EvictingDictionary<,>` in
  `Bodu.Collections.Concurrent`: lock-striped segments,
  where each segment runs an exact policy
  cache over its slice of the capacity — eviction order is exact per
  segment, approximate globally, while the slices sum to `Capacity`
  exactly. All six policies plus the TTL layer are supported; concurrent
  idioms added over the non-concurrent surface are `TryRemove(key, out
  value)`, single-flight `GetOrAdd` (factory inside the segment lock),
  and lock-free `ApproximateCount`. Only the post-commit `ItemEvicted`
  event survives (raised after lock release, handler exceptions
  suppressed — the `ConcurrentCircularBuffer` precedent); `ItemEvicting`,
  `PeekEvictionCandidate`, and `TouchOrThrow` are deliberately omitted
  as unhonorable or trap-prone under concurrency. A differential test
  suite pins single-segment eviction parity against the non-concurrent
  oracle.
- **`WeekPattern` stays in `Bodu.Core` — extraction retired.** With the
  collections split done, Core *is* the small always-referenced
  primitive layer the proposed `Bodu.Globalization.WeekPattern`
  extraction was meant to enable, so the extraction buys nothing while
  costing a package boundary. Recorded as final before the Wave 1 cut
  (moving the type after `Bodu.Core/v1.0.0` tags would be breaking);
  revisit only if an external consumer emerges that cannot reference
  Core at all.
- **The `Functional` seam has grown — railway primitives shipped.** ✅
  `Option<T>` (plus the non-generic `Option` companion), `Result` /
  `Result<T>` / `ResultError`, and `Either<TLeft,TRight>` landed as
  readonly structs with `Map` / `Bind` / `Match` combinators and
  Task-based async extensions (`OptionAsyncExtensions` /
  `ResultAsyncExtensions`) — the most-requested independently-built
  .NET surface (LanguageExt, CSharpFunctionalExtensions). Present
  values are strictly non-null (lenient null→`None` lifts are
  explicit), each type documents its `default` contract (`None` /
  empty-error failure / uninitialized-throwing `Either`), and
  `ValueTask` combinator variants are a deferred follow-up. The seam
  now sits at the `Bodu.Functional` extraction trigger recorded under
  *New library candidates* — grow it further only alongside that
  decision.
- **Probabilistic / sketch data structures have landed.** ✅
  `BloomFilter<T>` answers approximate membership with no false
  negatives, sized from an expected item count and design
  false-positive rate. `CountMinSketch<T>` estimates per-element
  frequencies and never underestimates — with probability at least
  `1 − δ` an estimate is at most the true count plus `ε · TotalCount`.
  `HyperLogLog<T>` estimates distinct-element cardinality at ~`1.04/√m`
  relative standard error in one byte per register. All three hash via
  the element's `IEqualityComparer<T>` (SplitMix64-avalanched,
  Kirsch–Mitzenmacher double hashing), merge parameter-compatible
  instances, and round-trip state through an opaque version-checked
  export/import. They ship in the `Bodu.Collections` package under the
  `Bodu.Collections.Probabilistic` namespace per decision D4 — no
  separate package.
- **Sequence-operator extras have landed.** ✅ `CartesianProduct`,
  `Permutations` / `Combinations`, and `Interleave` (skip-exhausted
  round-robin) joined the already-shipped `Pairwise` / `Windowed` /
  `Scan` / `RunLengthEncode` / `ZipLongest` / `SplitWhen` / `ChunkBy` /
  `Batch` operators, each verified
  absent from System.Linq against the installed .NET 10 ref assembly
  before landing (`Chunk`, `CountBy`, `AggregateBy`, `Index`, `Shuffle`
  remain excluded as BCL-shipped).
- **The time-based expiry layer for `EvictingDictionary<,>` has
  landed.** ✅ `EvictingDictionaryExpiration` (default TTL, absolute vs
  sliding, `TimeProvider`-driven) composes orthogonally with all six
  capacity policies: per-entry TTL `Add`/`TryAdd` overloads, lazy purge
  on access plus explicit `RemoveExpired()`, expired entries invisible
  to reads and preferred as capacity-eviction victims, a documented
  O(1) raw `Count` contract, and a zero-overhead path when expiry is
  not configured. No background timers; W-TinyLFU admission remains the
  recorded stretch follow-up.
- **The natural-order string comparer has landed.** ✅
  `NaturalStringComparer` (`Bodu.Extensions`) ships the numeric-aware
  `file2` < `file10` ordering with StringComparer-shaped statics
  (Ordinal / OrdinalIgnoreCase / CurrentCulture variants plus
  `Create(culture, ignoreCase)`), overflow-free digit-run comparison,
  and an equality/hash contract — scope deliberately excludes sign,
  decimal, and version-tuple semantics.
- **The navigable / order-statistic sorted collections have
  landed.** ✅ `NavigableSet<T>` and `NavigableDictionary<,>` ship
  floor / ceiling / higher / lower navigation, rank (`IndexOf`) and
  select (`GetAt`), O(log n) `CountInRange`, and live fail-fast
  ascending / descending / range views over order-statistic red-black
  trees with subtree-size augmentation (design note in
  `Bodu.Collections/docs/navigable-collections-design.md`; the
  skip-list-based concurrent sorted map remains an explicitly
  unforeclosed follow-on).
- **`BiDictionary<TKey,TValue>` has landed.** ✅ A bidirectional
  one-to-one map over two shared dictionary indexes with O(1) lookups
  both ways, a live reference-stable `Inverse` view, independent key
  and value comparers, and a construction-time duplicate-value policy
  (`Throw` — Guava `BiMap.put` — or `Replace` — `forcePut`).
- **`Table<TRow,TColumn,TValue>` has landed.** ✅ The two-key map
  adopted precisely for its projections: live `Row` / `Column`
  read-only views, `RowKeys` / `ColumnKeys`, and per-row `RowMap()`
  iteration over row-major nested dictionaries, with the O(rows)
  column-view cost documented as the v1 trade-off.
- **The layered and defaulting dictionary utilities have landed.** ✅
  `LayeredDictionary<,>` is the Python-`ChainMap` first-layer-wins live
  view (writes to layer 0, documented unshadowing, precedence aligned
  with `Bodu.Text.Configuration`'s resolver) and
  `DefaultingDictionary<,>` is the `defaultdict` store-on-indexer-miss
  wrapper, distinct from the per-call-site `GetOrAdd` extension.
- **The growable bit set has landed.** ✅ `BitSet` ships Java
  `BitSet` semantics — auto-grow, single-bit and range `Set` / `Clear`
  / `Flip`, `NextSetBit` / `NextClearBit`, PopCount `Cardinality`,
  in-place logical ops over mismatched lengths, value equality
  insensitive to trailing zero words, and a non-boxing struct
  enumerator over set-bit indices — closing the fixed-size,
  query-free, boxing-enumerator gaps of the BCL `BitArray`.
- **The trie-family extensions have landed.** ✅
  `AhoCorasickAutomaton` (+ `<TValue>`) provides immutable
  build-then-match multi-pattern search with a pinned deterministic
  match order and span-based eager conveniences, and `RadixTrie` /
  `RadixTrie<TValue>` are member-for-member path-compressed drop-in
  siblings of `Trie` / `Trie<TValue>`, differentially tested against
  the uncompressed tries as oracles.
- **The set-backed `MultiValueDictionary<,>` option has landed.** ✅
  `MultiValueBacking.Set` deduplicates per-key values through an
  injectable value comparer (first-occurrence order preserved, the
  `IReadOnlyList<TValue>` live-view contract intact), closing Guava's
  `SetMultimap` half as a construction-time option rather than a second
  type; `List` remains the default with its historical behaviour
  unchanged.
- **`IntervalTree<T>` / `IntervalTree<T,TValue>` have landed.** ✅
  Closed-interval overlap storage with O(log n + k) stabbing and
  window queries over max-endpoint augmented red-black trees,
  first-class duplicates, and the family boundary documented: only
  this type stores overlaps (`RangeSet` / `RangeDictionary` reject
  them; Numerics' `IntervalSet` normalizes them).
- **The `Deque<T>` overflow policy has landed.** ✅
  `DequeOverflowPolicy.EvictOpposite` gives the fixed-capacity deque
  Python's `deque(maxlen=N)` silently-discard-opposite-end behaviour
  (default remains `Reject`, preserving the historical throw/false
  contract), raising the same `ItemEvicting` / `ItemEvicted` pair as
  `CircularBuffer<T>`'s overwrite mode so the ring family stays
  consistent.

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

Also shipped: **one-time-password codes** — `Hotp` (RFC 4226) and `Totp`
(RFC 6238) sit in the flat namespace beside the KDFs — and **Merkle tree
hashing** — `MerkleTreeHash` and the multi-core `ParallelMerkleTreeHash`
(configurable hash algorithm, block size, and fan-out) with RFC 6962-style
leaf/node domain separation and length binding, plus a
`MerkleTreeDiagnostics` inspection surface and a dedicated docs guide
(`docs/guides/cryptography/merkle-trees.md`).

Forward-looking:

- **PEM key wrapping has landed.** ✅ Ed25519 / X25519 round-trip the
  RFC 8410 PKCS#8 / SubjectPublicKeyInfo DER containers and, through the
  inherited `AsymmetricAlgorithm` helpers, RFC 7468 PEM (`ImportFromPem`,
  `ExportPkcs8PrivateKeyPem`, `ExportSubjectPublicKeyInfoPem`), pinned by
  exact canonical golden-string vectors so raw → DER → PEM is fully
  verified. ML-KEM / ML-DSA stay raw-encoding only; XML and encrypted
  PKCS#8 remain out of scope. This closes the key-encoding interop story.
- **The AVX-512 capability-detection contract has landed.** ✅
  `SimdCapabilities` gates the BLAKE2 / BLAKE3 / Threefish / CubeHash fast
  paths behind the hardware intrinsic plus a process-wide
  `Bodu.Security.Cryptography.DisableSimd` `AppContext` switch, documented
  in `docs/guides/cryptography/hardware-acceleration.md` and exercised by a
  dedicated SIMD-off test assembly. Because the paths are ARX and
  bit-identical, the switch is for determinism / reproducibility / audit,
  not leakage.
- **One-time-password codes have landed.** ✅ `Hotp` (RFC 4226) and `Totp`
  (RFC 6238) ship as static, span-based surfaces over the BCL one-shot
  HMAC, with constant-time verification, HOTP resync / TOTP drift windows,
  and **no new dependency** (raw-byte secrets; Base32 / `otpauth://`
  provisioning left to the consumer). Validated against the RFC 4226
  Appendix D and RFC 6238 Appendix B vectors.
- **A side-channel / fault-hardening review pass is in flight.** The first
  batches replaced the GCM-SIV GF(2^128) multiply with a constant-time
  implementation and zero the EAX CMAC state on transform fault. Batch 3
  (2026-08-28) removed the last branch-on-secret from the GF(2^128)
  doubling paths (the EAX/SIV/OCB `dbl()` now routes through a shared
  branch-free `GaloisField128.Double`), completed the zero-on-fault
  contract across all six block-cipher AEADs (any exception escaping
  `Decrypt` leaves the output's plaintext region zeroed — pinned by a
  fault-injection sweep over a new `FaultingBlockCipher` test double —
  with CCM retrofitted from no clearing at all), reset the Ascon sponge
  state on authentication failure, fault-protected the
  Twofish/HC-128/Serpent-128 key-schedule scratch and the
  Ed25519/X25519/Scrypt/Argon2/HOTP secret clears, added the missing
  `ConstantTimeDifference`/`ConstantTimeSelect` tests, and documented the
  cache-timing stance on every table-driven cipher and hash (correcting
  the CCM/OCB remarks that wrongly claimed verify-before-release).
  Further review batches continue on the same cadence.

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

- **The conformance corpus has landed.** ✅ A Regression-tier BEP-3
  sweep (`Bep3CorpusTests`) of ~100 valid and ~115 malformed KAT rows
  adapted from the BEP 3 grammar and the BEP 5/9/10/12/23 wire shapes,
  and from the test suites of libtorrent, Transmission, bencodepy,
  bencode-go, and bendy/serde_bencode — pinned reader token sequences,
  fanned across the read-only document and mutable node surfaces.
  Bencode has no canonical upstream corpus repository (nothing like
  `toml-test`), so the cases are inline KAT rows with per-group
  attribution rather than a vendored file corpus.
- **The read-only configuration source has landed.** ✅ `AddBencodeFile`
  / `AddBencodeStream` in `Bodu.Extensions.Configuration.Text`, mirroring
  the TOML provider shape — strict-canonical parse, dictionary-rooted
  documents, colon-delimited flattening with list-index segments.

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

Current state: new; ~55 src / ~60 test files. The YAML 1.2 core-schema
quartet — the `Utf8YamlReader` (over the multi-partial `YamlParser`
handling anchors/aliases/tags/merge-keys and multi-document streams via
`YamlDocument.ParseAllDocuments`), the `Utf8YamlWriter`, the symmetric
read+write `YamlSerializer`, and the `YamlDocument` / `YamlNode` DOMs.
Validated against the vendored `yaml-test-suite` (353 cases).

- **Serializer parity with Bencode/Toml has landed.** ✅ The enum tier
  (wire-name maps honoring `StringEnumMemberNameAttribute` and naming
  policies; the public `YamlStringEnumConverter` (+ generic) and
  `YamlNumberEnumConverter<TEnum>` factories), the options surface
  (`DefaultIgnoreCondition` replacing the pre-release `IgnoreNullValues`;
  the `YamlSerializerDefaults` General/Web presets), the full fixed-width
  integer set (`nint`/`nuint`/`Int128`/`UInt128` with invariant-text
  fallback outside the signed 64-bit writer surface), and the
  DOM↔serializer bridges (`NodeConverter` adopted under an `#elif YAML`
  branch, plus `YamlElement`/`YamlDocument` converters over a
  shared-row-store `ParseValue`). The scalar converter tier deliberately
  stays format-local — YAML's implicit typing coerces across scalar kinds
  the token-strict shared converters cannot express — as does the
  `SerializerEngine` seam (null-root write and empty-document read
  semantics differ).
- **The supported-schema boundary is documented.** ✅ The README's *Bodu
  YAML Core Tree Profile* section and compliance matrix carry the
  explicit in/out list (tag resolution, complex keys, directives,
  anchors) so consumers know when to reach for a full YAML engine.
- Remaining polish: raise the `bld/coverage-thresholds.json` floor toward
  Toml's 95.0/92.3 after the next coverage collection run.

### `Bodu.Text.Delimited` / `Bodu.Text.DotEnv` / `Bodu.Text.Ini` (and the `Bodu.Text.Formats` umbrella)

Current state: new — the ground-up quartet redesign of the retired
line-oriented trio (see *Active focus* #4). Each is a standalone
`System.Text.Json`-shaped library: a ref-struct `Utf8*Reader` /
`Utf8*Writer` token surface over UTF-8, a `*Serializer` reflection binder
over the shared `Bodu.Text.Serialization` attribute/naming/callback layer,
a mutable `*Node` DOM, and a read-only `*Document` DOM. Delimited adds the
RFC 4180 dialect policies (field-count / malformed-record /
duplicate-header) and a **truly incremental** `IAsyncEnumerable<TRecord>`
streaming surface (records parse and yield as stream segments arrive, and
the `IAsyncEnumerable` serialize overload writes in bounded batches);
INI adds the two-reader model (source-order `Utf8IniReader` + normalized
`IniDocumentReader` applying duplicate-section merge), a
comment-preserving mutable DOM, and the `GlobalSectionName` mapping; the
Delimited Regression tier carries a csv-spectrum-derived RFC 4180 corpus
and the DotEnv Regression tier a python-dotenv/godotenv-derived
conformance corpus. `Bodu.Text.Formats` is a thin umbrella meta-package
over the three. Reflection-free binding is in place: `[DelimitedRecord]`
/ `[IniSection]` partial POCOs get `IDelimitedRecordFactory<TRecord>` /
`IIniSectionFactory<TSection>` implementations emitted by the
`Bodu.Text.Formats.Generators` Roslyn source generator, consumed by the
factory overloads on `DelimitedSerializer` / `IniSerializer` (no
`RequiresUnreferencedCode`/`RequiresDynamicCode` — trimming- and
AOT-safe).

- **Package `Bodu.Text.Formats.Generators`** (analyzer nupkg layout,
  icon/hero artwork, release-manifest entry) when the Wave-2 release wave
  picks it up; the project currently builds and tests as an unpackaged
  Roslyn component.
- **Resumable reader states** (`*ReaderState`) as public API, so
  consumers can drive their own segment loops; the serializers'
  incremental surfaces currently keep the segment-accumulation logic
  internal.

### `Bodu.Text.Configuration`

Current state: mature; 37 src / ~74 test files. A layered
config-resolution engine (profile, resolver, view getters, diagnostics)
over its own trivia-preserving INI document model (self-contained since
the *Active focus* #4 decouple — no format-library dependency).

- **Stabilise `ConfigurationPattern.Compile`** — the expression-
  compilation surface needs an API-stability pass before consumers build
  on it.
- **Add JSON-pointer / JMESPath-style resolvers** alongside the existing
  `ConfigurationResolver` to broaden applicability beyond the
  Bodu-specific query syntax.

### `Bodu.Text.Filtering`

Current state: landed complete 2026-08-02 (#648) — ~25 src / ~24 test
files, a benchmark project, and a runnable sample. An include/exclude
filtering engine for lists of text values: glob (wildcard,
character-class, `{a,b}` brace-alternation) and regex patterns compile
once into a cost-tiered `TextFilter` that runs the cheapest strategies
first (MatchAll → Literal → Prefix/Suffix → Contains → general wildcard
→ Regex — the `globset` idea), with Ant/MSBuild-style include/exclude
set semantics (`AnyMatch`) or gitignore-style last-match-wins ordered
rules (`LastMatchWins`), gitignore-convention list parsing, always-on
match statistics, and an optional per-decision `ITextFilterObserver`.
Regexes prefer the linear-time non-backtracking engine and always carry
a match timeout that fails safe on both include and exclude. Core-only
dependency; ships in Wave 2 (`bld/release-manifest.txt`).

- **No open items.** The one demand-driven follow-on candidate recorded
  for completeness: a path-segment (`**`) mode in the
  `Microsoft.Extensions.FileSystemGlobbing` style (single-value spans
  are already served by `IsMatch(ReadOnlySpan<char>)`).

### `Bodu.Extensions.Configuration.Text`

Current state: bridge layer connecting `Microsoft.Extensions.Configuration`
to the Bodu text stack. The read-only **TOML and Bencode sources have
landed** (`AddTomlFile` / `AddTomlStream`, `AddBencodeFile` /
`AddBencodeStream`).

- **Document precedence semantics** when stacked with the `Json` and
  `EnvironmentVariables` providers.

### `Bodu.Numerics`

Current state: new. Ships `Fraction<T>` (exact rationals over
`IBinaryInteger<T>`), **`BigDecimal`** (arbitrary-precision decimal over a
`BigInteger` mantissa + scale), the continuous `Interval<T>` (open/closed/unbounded
endpoints over `INumber<T>`) with full set algebra, the `DiscreteInterval<T>`
integer-domain interval, the `IntervalPair<T>` / `DiscreteIntervalPair<T>`
binary-result types, and the N-ary `IntervalSet<T>` — each with JSON
converters where applicable — plus the **statistics aggregates**:
`RunningStatistics<T>` / `RunningQuantile<T>` (single-pass stream
accumulators) and `MovingSum<T>` / `MovingMinMax<T>` (rolling windows).
Money/currency/FX live in the companion `Bodu.Financial`.

The **Numerics growth wave is complete** (see *Active focus*) — all three
sequenced steps have shipped:

1. **`Interval<T>` extensions — shipped.** ✅ Unbounded / half-bounded
   endpoints (explicit metadata, not float sentinels), `Difference` /
   `SymmetricDifference` returning the allocation-free `IntervalPair<T>`
   (≤2 disjoint pieces), and the `&` / `|` operators (`|` throws on a
   non-contiguous union). The architecture review's continuous-vs-discrete
   contradiction was resolved by adding **`DiscreteInterval<T>`** over
   `IBinaryInteger<T>` — canonical closed-integer form with successor-aware
   emptiness (`Open(1,2)` is empty) and adjacency (`[1,2] ∪ [3,4] == [1,4]`).
   Review hardening folded in: NaN-endpoint rejection and a culture-safe
   endpoint separator, with an exhaustive finite-domain membership-law test
   harness. The follow-ups have since landed too: `DiscreteInterval<T>`
   `Difference` / `SymmetricDifference` (via `DiscreteIntervalPair<T>`) and a
   first-class N-ary `IntervalSet<T>` — a normalized disconnected-range value
   with N-ary union / intersection / `Except` / `Complement` (taken over the
   whole line, so the double complement is the identity).
2. **`BigDecimal` — shipped.** ✅ An arbitrary-precision decimal (a
   `BigInteger` mantissa plus an `int` scale) with the full
   `INumber<BigDecimal>` / `INumberBase<BigDecimal>` generic-math surface,
   span/UTF-8 parse and format, rounding, conversions, and a
   `BigDecimalJsonConverter` in `Bodu.Numerics.Serialization.Json`
   registered by `AddNumericsJsonConverters`. No BCL equivalent existed —
   the highest-leverage gap-filler, now closed.
3. **Running-statistics aggregates — shipped.** ✅ Online mean / variance
   (Welford, with the Chan et al. parallel merge exposed as `Combine`),
   exact min / max / count, and a streaming quantile
   (`RunningQuantile<T>`, the P² algorithm with an exact empirical
   warm-up under five samples) as mutable `struct` accumulators over
   `INumber<T>` — extrema exact in `T`, moments in `double` via
   `CreateChecked` with an explicit post-widening finiteness guard, and
   non-finite samples rejected. The **windowed / rolling companion set**
   shipped alongside: `MovingSum<T>` (last-N sum in `T` + mean, with a
   periodic exact rebuild bounding floating-point eviction drift) and
   `MovingMinMax<T>` (monotonic ring deques, amortized O(1)). One
   deliberate deviation from the plan as originally written: the moving
   types are sealed classes over a **private internal ring**, not
   `CircularBuffer<T>` — the collections split made that reuse a brand-new
   `Bodu.Numerics → Bodu.Collections` package edge, which was judged not
   worth it, so Numerics keeps its Core-only dependency. A bare
   `RollingWindow<T>` *collection* is still deliberately not added
   anywhere: the overwrite-mode `CircularBuffer<T>` in `Bodu.Collections`
   already is one; the gap was the aggregates, and they live here.

A generic **`Complex<T>`** has since **landed** ✅ — the natural follow-on
after this wave. It generalizes the `double`-only `System.Numerics.Complex`
to any `IFloatingPointIeee754<T>` (`float` / `double` / `Half` / `NFloat`),
mirroring the framework type's surface: the operators and named arithmetic,
`Magnitude` / `Phase` / `Conjugate` / `Reciprocal`, the transcendental
functions (`Sqrt` / `Exp` / `Log` / `Pow` / the trig-hyperbolic family), the
`<real; imaginary>` format / parse round-trip, and the full
`INumberBase<Complex<T>>` / `ISignedNumber<Complex<T>>` generic-math surface
(no total order, so not `INumber`). It reproduces the framework's behaviour
including its documented quirks (Smith's-algorithm division, naive
multiplication without infinity recovery, `Reciprocal(0) == 0`), validated by
a differential oracle against `System.Numerics.Complex` for `Complex<double>`.
A `ComplexJsonConverter<T>` ships in `Bodu.Numerics.Serialization.Json`
(registered by `AddNumericsJsonConverters`), writing non-finite components as
the named literals `"NaN"` / `"Infinity"` / `"-Infinity"`.

### `Bodu.Financial`

Current state: new and large; ~322 src / ~255 test files. Ships the
`Money` / `Money<TCurrency>` value types, the deferred-rounding
`CalculatedMoney`, the multi-currency `MoneyBag`, the ISO 4217 catalogue
(`CurrencyCode` source-generated enum, `CurrencyRegistry`, ~180
per-currency `ICurrency` types, `CurrencyLookupService`), formatting /
parsing (`MoneyFormatter`, `MoneyParseOptions`), rounding / allocation
policies (`IRoundingStrategy`, `MonetaryContext`, the policy enums), the
full FX abstraction stack (`ExchangeRate` / `ExchangeRate<TBase,TQuote>`,
`IRateProvider` / `IDatedRateProvider` / `IHistoricalRateProvider`,
`RateBook` / `RateSeries`, `FixedDatedRateProvider`), delineated across
the `Bodu.Financial` / `.Currencies` / `.ExchangeRates` namespaces. The
abstract `WebRateProvider` / `PairWebRateProvider<TSeries>` bases the
provider packages extend now ship from the separate
`Bodu.Financial.ExchangeRates` package. References `Bodu.Numerics` for
the `Fraction<BigInteger>` exact-arithmetic escape hatch.

- **The provider → immutable conversion surface has landed.** ✅
  `WebRateProvider` exposes its accumulated state through
  `GetLoadedBook()` / `GetLoadedSnapshot()` (the internal immutable book
  and ready-to-query snapshot, pinned at call time),
  `FixedDatedRateProvider` exposes its wrapped `Book`, and
  `RateBook.ToBuilder()` round-trips into the mutable table
  builder. The `Bodu.Financial.Extensions` companions complete the chain:
  `IEnumerable<ExchangeRate>.ToBook()` (multi-provider-safe,
  inversion-normalizing, `FetchedAtUtc`-preserving),
  `RateBook.ToFixedProvider(…)`, and the fetch-based
  `IDatedRateProvider.ToFixedProviderAsync(pairs, start, end)`.
  Deferred follow-ups if demand emerges: book `Merge` / `Slice` helpers,
  a per-pair-window `ToFixedProviderAsync` overload, and promoting book
  exposure onto `IPairRateLoader`.
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

Current state: new and extensive. Eleven web providers over the shared
`WebRateProvider` base, split into two architectural families:
central-bank / single-base whole-file sources (**Boe** IADB CSV, **Ecb**
eurofxref XML, **Rba** `.xls` eras, **Imf** — keyless USD-anchored daily
Representative Exchange Rates over a monthly tab-separated report) and
arbitrary-pair sources over `PairWebRateProvider<TSeries>` (**Yahoo** chart
JSON, **Ofx**, **Xe** — scrape-token auth, **Oanda** — rolling ~180-day
window, **Fixer** — fixer.io `access_key` base+quotes, **ExchangeRateHost** —
exchangerate.host `access_key` source+quotes, **Fred** — St. Louis Fed
`api_key` per-pair `series_id` map). The
shared `.DependencyInjection` package owns the `AddWebRateProvider`
machinery (named `HttpClient` + Polly resilience) each provider's `Add*`
extension delegates to. The provider-agnostic `.Caching` package supplies
the `CachingRateProvider` read-through decorator and the
`AggregatingRateProvider` (priority-fallback / average strategies,
per-pair routing) over `IRateCache`, with in-memory / TOML / JSON
backends in-package and durable `Sqlite` + shared `Distributed`
(`IDistributedCache` / Redis) backends as add-ons.

- **`HistoryAvailability` is now advertised on every provider — and
  consumed.** ✅ Declared per source (fixed floors for the central banks
  and Yahoo, a deliberate Unbounded for OFX, rolling windows for XE and
  OANDA) and enforced for pair providers by the shared contract test.
  The caching / aggregation layer now reads it through the
  `IHistoricalRateProvider` capability interface: the caching
  decorator skips single-date misses and clamps range fetches to the
  inner source's advertised earliest date (recording the unavailable
  prefix as covered-with-no-rows), and the aggregator drops candidates
  that declared they cannot serve the requested date or window before
  the strategy runs — both behind `RespectHistoryAvailability` flags
  that default on.
- **Yahoo is now structurally identical to the other pair providers.** ✅
  The dedicated `YahooRateProvider` subclass already existed; the
  remaining inconsistency — a bespoke `IYahooExchangeRateChartSource`
  bridged through an internal adapter — has been collapsed:
  `YahooChartRateSource` implements
  `IPairRateSource<YahooSeriesInfo>` directly and the parser
  returns `PairRateData<YahooSeriesInfo>` like its OFX/XE peers.
- **De-risk the XE provider — Experimental marking done; official
  endpoint still open.** The package is already labelled Experimental
  (README tier badge; see *API-stability tiers*) and the scrape-token
  brittleness is documented in the README, the XML docs, and the
  providers guide. The remaining open half is replacing the scraped
  `Authorization: Basic` token with an official-endpoint path, if one
  ever becomes available.
- **Broaden provider coverage** — *done for the common free/commercial
  APIs.* Fixer (`Bodu.Financial.ExchangeRates.Fixer`), exchangerate.host
  (`.ExchangeRateHost`), FRED (`.Fred`), and IMF (`.Imf`) are now wrapped
  as pair providers over `PairWebRateProvider<TSeries>`, each a small
  package following the established provider + DI shape. IMF is a keyless,
  USD-anchored bulk provider (like ECB) that downloads the IMF's monthly
  Representative Exchange Rates tab-separated report and serves daily rates;
  quotation direction (some currencies are USD-per-unit) is normalized on
  ingest. Further sources can follow the same template on demand.

### `Bodu.IO.Compound`

Current state: ~44 src / ~46 test files. A read + edit + authoring
implementation of the OLE2 / Compound File Binary (CFB) container — the
structured-storage envelope behind legacy Office files (`.xls`, `.doc`,
`.msg`). `CompoundFile` opens for read (buffered or streaming), edits
transactionally through BCL-style writable cursors (`OpenStream(name,
FileMode, FileAccess)` / `CreateStream` / `Delete` / `Rename` staged
until `Commit`, with `Revert`), and authors from scratch through the
fluent `CompoundStorageBuilder` / `CompoundStreamBuilder` tree —
including deferred `Func<Stream>` payload sources for streaming-scale
writes and version 3 **and** 4 emit. Full OLE property-set surface
(`SummaryInformation`, `DocumentSummaryInformation`, `OlePropertySet` with
MS-OLEPS read/emit) and a complete exception hierarchy.

The forward items below are sequenced and scoped in
[`Bodu.IO.Compound/docs/roadmap-implementation-plan.md`](Bodu.IO.Compound/docs/roadmap-implementation-plan.md)
(tranches T0–T5, with the audit evidence behind each).

- **Writable stream cursors — done.** ✅ Delivered as the BCL
  `Package.Open`-style model rather than a COM `IStream` clone: a
  writable, seekable `CompoundStream` over the staging tree, obtained
  via `OpenStream(name, FileMode, FileAccess)` / `CreateStream(name)`,
  flushed into the tree on dispose and persisted only by `Commit`. The
  remaining distance to `IStream` is per-stream transacted commit,
  which stays out of scope (plan decision D2).
- **Property-set write-back symmetry — done.** ✅ (plan T1)
  `PropertySetWriter` now emits every value shape the reader parses,
  including `VT_VECTOR` values (variant round-trips guarantee value
  identity, not byte identity), and `CompoundStorage.WritePropertySet` /
  `CompoundFile.SetSummaryInformation` /
  `SetDocumentSummaryInformation` are the write counterparts of the
  `TryGet…` readers.
- **Writable-cursor memory model — done.** ✅ (plan T2) The writable
  cursor no longer double-copies on flush (it transfers its buffer to
  the staging node at dispose and tracks pending writes so an unchanged
  re-flush is a no-op) and rejects payloads past `int.MaxValue` with a
  deliberate `NotSupportedException` that routes callers to the deferred
  stream sources.
- **Entry metadata on the edit surface — done.** ✅ (plan T3)
  `CompoundStorage` exposes settable `ClassId` / `CreationTime` /
  `ModifiedTime` / `StateBits` on a writable file (storages only, per
  MS-CFB §2.6.1); nothing is auto-stamped, so byte-identical re-saves
  stay possible.
- **True-async commit and streaming reads — done.** ✅ (plan T4)
  `CommitAsync` / `FlushAsync` serialize asynchronously (sharing the
  synchronous layout, so bytes are identical) and streaming-mode
  `ReadAsync` reads its sectors with real async I/O; buffered and
  writable cursors keep synchronous completion.
- **This project is the substrate for new office-format readers** — see
  `Bodu.Formats.Excel.Binary` (already built on it) and the `.msg` /
  `.doc` candidates under *New library candidates*. The `.msg`
  substrate-readiness review (plan T5) **executed 2026-07-31**: MS-OXMSG
  maps entirely onto the shipped surface and no new container API is
  required — findings recorded in the `Bodu.Formats.Outlook` kickoff
  plan
  ([`Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md`](Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md),
  §1), which un-gates the `.msg` reader.

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

### `Bodu.Formats.Outlook` / `Bodu.Formats.Outlook.Msg`

Current state: **shipped at Preview** (kickoff and full delivery
2026-07-31; ~30 src / ~55 test files). Two packages executing the kickoff
plan
([`Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md`](Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md),
now marked executed with its recorded deviations): the container-free
shared MAPI value model (`Bodu.Formats.Outlook`) and the read-only
`.msg` reader over `Bodu.IO.Compound` (`Bodu.Formats.Outlook.Msg`),
both in the flattened `Bodu.Formats.Outlook` namespace so the future
`.pst` reader shares the model. The reader covers the full property
surface (fixed/variable/multi-valued, Unicode + code-page strings with
two-pass resolution), recipients/attachments/nested messages,
named-property resolution, and the text/HTML/compressed-RTF bodies
(MS-OXRTFCP decoder pinned to the specification example vector), with a
DocFX guide section and compile-guarded snippets.

- **Real-world `.msg` reference corpus — done.** ✅ (2026-07-31) 41
  Apache POI `test-data/hsmf` files (Apache-2.0; provenance `NOTICE.md`
  + carried license text per the `Bodu.IO.Compound` pattern) under
  `Bodu.Formats.Outlook.Msg/test/Fixtures/Reference/`, with a
  cross-implementation expectations manifest generated by olefile and a
  Regression sweep: the 39 well-formed files decode matching the
  independent oracle (code pages 950–65001, HTML/RTF bodies, a
  1,321-recipient message), the 2 fuzzer-minimized malformed containers
  are rejected. A companion pstsdk seed corpus (2 Unicode + 2 ANSI
  PSTs, Apache-2.0, `lspst`-generated oracle) landed under
  `Bodu.IO.Pst/test/Fixtures/Reference/`, resolving the `.pst`
  exploration's fixture-acquisition risk (R2).
- **Package icons — done.** ✅ (2026-07-31)
  `bld/icons/Bodu.Formats.Outlook{,.Msg}.png` plus the hero banners.
- **`.msg` authoring** and **TNEF** remain demand-driven candidates; see
  the kickoff plan's out-of-scope list.

### `Bodu.IO.Pst`

Current state: shipping (P0 spike landed 2026-07-31; **P1 — the LTP
layer — landed 2026-08-19** per
[`docs/ltp-implementation-plan.md`](Bodu.IO.Pst/docs/ltp-implementation-plan.md);
**P2 — the `Bodu.Formats.Outlook.Pst` messaging reader and the
container hardening pass — landed 2026-08-31** per
[`Bodu.Formats.Outlook.Pst/docs/pst-reader-implementation-plan.md`](Bodu.Formats.Outlook.Pst/docs/pst-reader-implementation-plan.md)).
The NDB (node database) read layer of MS-PST for the Unicode format —
header parse with the §5.3 CRC, NBT/BBT B-tree walks, block reads with
trailer validation and the permute/cyclic content encodings decoded,
XBLOCK/XXBLOCK data trees, and SLBLOCK/SIBLOCK subnode trees — plus the
LTP layer over it: the heap-on-node (`bSig 0xEC`) over ordered block
segments, BTree-on-heap, and the public `PstNode.ReadPropertyContext()`
/ `ReadTableContext()` surfaces (`PstPropertyContext` /
`PstPropertyValue` / `PstTableContext` / `PstTableRow`, wire-typed and
MAPI-free, values resolved on access, row matrices streamed
block-at-a-time), behind the `PstFile` / `PstNode` session surface with
tiered validation (`Compatible` / `Strict` / `Minimal`), validated
against the pstsdk reference corpus and the `lspst` oracle (folder
names, subjects, senders, contents-table rows, and a no-dangling-HNID
every-node sweep). Ships at **Preview**; ANSI and OST variants are
recognized and rejected.

- **P2 — `Bodu.Formats.Outlook.Pst` — landed** ✅ (2026-08-31, per
  [`Bodu.Formats.Outlook.Pst/docs/pst-reader-implementation-plan.md`](Bodu.Formats.Outlook.Pst/docs/pst-reader-implementation-plan.md)):
  the messaging layer — `OutlookMailStore` / `OutlookMailFolder` /
  `OutlookMailMessage` / `OutlookMailAttachment` over the shared
  `Bodu.Formats.Outlook` MAPI value model (recipients via the shared
  `OutlookRecipient`, embedded messages, store-wide named-property
  resolution from the name-to-id map node, and the text/HTML/
  compressed-RTF bodies via the new `Bodu.Formats.Outlook/shared/**`
  decode layer) — plus the container hardening pass (`BlockCacheSize`
  decoded-block LRU, streaming `OpenDataStream` with a memory-ceiling
  Regression, `PstNode.DataLength`, the `PstFileError` /
  `PstNodeNotFoundException` exception taxonomy, and bit-flip/truncation
  malformed sweeps at both the container and messaging levels).
  `Bodu.IO.Pst` and `Bodu.Formats.Outlook.Pst` joined the release
  manifest as wave 3 at `BoduBaseVersion` 0.4.0, with the docs-site
  debut and `samples/IO.Pst/` scenario project.
- **Outlook hardening pass — landed** ✅ (2026-09-03, per
  [`Bodu.Formats.Outlook/docs/outlook-hardening-plan.md`](Bodu.Formats.Outlook/docs/outlook-hardening-plan.md)):
  a tests-first (68 red tests committed before any fix) security,
  exception-contract, and performance sweep of `Bodu.IO.Pst`,
  `Bodu.Formats.Outlook`, `.Msg`, and `.Pst`. Bounded every hostile-input
  hole (NBT/BBT descent depth and level checks, BTH index-level cap and
  descent-path check, data-tree materialization and fan-out limits via
  the new `PstFileOptions.MaxNodeDataLength` / `MaxDataTreeLeaves`,
  `CompressedRtf` expansion bounds, and `MaxEmbeddedMessageDepth` /
  `MaxDecompressedRtfBytes` on both reader option types — a limit
  violation is a format failure at every validation level, reported as
  `PstFileError.LimitExceeded` at the container); closed the exception
  leaks (`.msg` container faults translate to `OutlookMsgFormatException`
  via `MsgContainer`, the shared `MapiNamedPropertyRecords` parser, the
  page/block cache split, UTC-anchored FILETIME decoding, nested-session
  and view disposal guards, strict size cross-checks); removed the
  silent wrong answers (unordered property contexts, narrow inline cells,
  1200/1201 code pages, `PT_NULL` / zero-FILETIME as present-with-null,
  folder code-page inheritance, missing store object); and cut the
  copies (`CompoundStorage` child index — the one `Bodu.IO.Compound`
  change — cached bodies, zero-copy attachment streams, in-place row-id
  enumeration, span-based variable-value decoding).
- **Streaming attachment payloads — landed** ✅ (2026-09-04, the
  follow-on the hardening pass recorded): `PstPropertyContext` /
  `PstTableRow` gain value-length and value-stream accessors that serve
  subnode-resident values block by block without materializing them, and
  both readers gain `MaxInlineAttachmentBytes` (1 MiB) — a larger
  `PidTagAttachDataBinary` stays a present-but-null property and
  `OpenContentStream` streams it from the container, so an attachment of
  any size can be enumerated, sized, and copied without ever being held
  in memory in full.
- **ANSI format — landed** ✅ (2026-09-04): `wVer` 14/15 stores are read
  through the same NDB readers as Unicode, driven by an internal
  `PstLayout` descriptor (32-bit identifiers and offsets, the 12-byte
  trailers with the block identifier before the checksum, the unpadded
  subnode block header, the 8,180-byte row-matrix payload, the two-byte
  row-index number). Validated against the two ANSI corpus fixtures and
  their `lspst` oracle listings, the synthetic fixture builder in ANSI
  mode, and the malformed sweeps at both levels; `OutlookMailStore`
  walks an ANSI store unchanged. Only the 4 KiB-page OST variant remains
  rejected.
- **Follow-on closure — landed** ✅ (2026-09-04): `PstDataStream` gained
  a real lifecycle (disposal, session binding, synchronous-completing
  async reads); `.msg` value streams are read with a single allocation
  (a pre-sized compound chain read plus a zero-copy hand-off from a
  read-only cursor); the dead storage-name formatters are gone; and the
  `CompressedRtf` tests are linked into the PST test project under the
  `OUTLOOK_PST` namespace switch.
- **Scale-tier corpus**: the EDRM Enron PSTs (CC-BY) remain the
  multi-megabyte stress option recorded in the fixture NOTICE.

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
  resources, not replacements. The Tehran side is delivered
  (`tehran-nowruz`); for the Saudi side the acceptance baseline now
  exists in-repo: the embedded gazetted-announcements table
  (`SaudiAnnouncedObservances-1422-1448.csv`) shows the announcements
  moved seventeen month starts by exactly one day in both directions
  across 1422–1448 AH, so a sighting variant must reproduce those
  seventeen ±1 shifts against the KACST table. The Bahá'í acceptance
  data is also now in-repo (`corpus/bahai/uhj-holy-days-172-221-be.csv`,
  the official UHJ 50-year table): a Tehran-sunset equinox variant for
  Naw-Rúz must flip exactly the twenty official-21-March boundary years
  the current UT model runs one day early on, and the Twin Birthday
  columns are the verification set for the pending
  eighth-new-moon-after-Naw-Rúz algorithm (whose counting convention —
  after the *day* of Naw-Rúz, Tehran sunset epoch — is pinned by the
  2023/2034 boundary years and documented in
  `tools/verify-bahai-poya-vectors.py`).
- ~~**Extend the Hebcal-aligned regression catalogue**~~ — delivered
  across all three families: the 13 `global-jewish` observances are
  pinned across Gregorian 1990–2039 by an embedded vector table
  generated from an independent Dershowitz–Reingold implementation
  (`tools/generate-hebrew-observance-vectors.py`); the 10
  `global-islamic-umm-al-qura` observances by a KACST-table projection
  whose every underlying month start is astronomically cross-checked
  against an independent Meeus lunar-conjunction implementation
  (`tools/verify-islamic-observance-vectors.py`, 517/517 within the
  expected +1..+2-day window, double-occurrence years asserted as full
  lists — a sweep that surfaced and fixed the `OffsetFromRule`
  multi-occurrence defect); and the 3 `global-persian` observances by an
  independent Meeus equinox implementation of the official Solar Hijri
  new-year rule (`tools/generate-persian-observance-vectors.py`,
  150/150 against the BCL projection). The Umm al-Qura table is
  externally reconciled three ways: against the KFUPM Research
  Institute *Comparison Calendar 1356–1411 AH* print (24/24 month
  starts, 17/17 derivable vector rows for 1990 / early 1991), against
  ummulqura.org.sa full-year exports sampled across the range
  (1420/1430/1446/1448 AH: 48/48 month starts, 1,418/1,418 day rows,
  40/40 derivable vector rows; the site's retrospective 1410 table
  diverges from the contemporaneous print by a day — the vectors
  follow the print), and against van Gent's computed comparison table
  for 1422–1448 AH (216/216 derivable vector rows). The same table's
  **announced** column — the High Judiciary Council's gazetted dates —
  is embedded as its own 171-row sweep asserting the measured one-day
  bound. Three further fifty-year tables complete the deep corpus:
  `global-islamic` (tabular Hijri, astronomically braced 517/517),
  `global-zoroastrian` (independent Meeus derivation, 300/300 vs the
  BCL), and `global-hindu` (engine-pinned regression freeze braced by
  an independent tithi-proximity check, 700 rows — whose generation
  surfaced and fixed the lost-Magha-month ingress-day defect).
  ICU cross-vendor tables (committed under `corpus/`) reconcile every
  month of UAQ 1410–1462 (1420–1450 exact 372/372; the pre-1420
  retrospective-table and post-1450 vendor-extension divergences are
  classified and documented) and confirm all 50 Persian Nowruz dates
  and leap flags, closing the Persian civil-confirmation residual.
  Provenance and cross-check counts live in
  `NotableDateCatalogueVerification.md`. Remaining residual: reconcile
  future official KACST publications for 1451 AH onward as they
  appear — a targeted check (Aug 2026) confirmed none exists yet: the
  relaunched official ummulqura.org.sa is an interactive service with
  no fixed post-1450 publication table, so this stays a periodic
  recheck, not an open action. An eighth fifty-year table is now also
  in place: the Universal House of Justice Badí table 172–221 B.E.
  (2015–2064) sweeps all nine catalogue-modelled Bahá'í holy days at
  the measured signed bound (exact in every official-20-March year,
  one day early in every official-21-March boundary year).
- ~~**Add `IAsyncEnumerable<NotableDate>` projections**~~ — delivered:
  `NotableDateServiceAsyncExtensions.ResolveAsync` streams a range's
  occurrences one civil year at a time with cooperative cancellation,
  element-for-element identical to the synchronous overloads.

### `Bodu.Globalization.Calendar.Builder`

Current state: thin; fluent `NotableDateDocumentBuilder` authoring API
with XML + JSON-subset serialization and a loader that materializes a
`NotableDateResource`.

- ~~**Add fluent rule-validation lint**~~ — delivered:
  `NotableDateDocumentBuilder.Validate()` / `TryBuild(...)` and
  `NotableDateResourceLoader.TryLoad` / `TryLoadJson` collect every
  diagnostic (stable `BODU-CAL-*` codes) without throwing, documented in
  the validation-diagnostics guide; the throwing overloads are
  unchanged. Remaining nicety: source locations on diagnostics.
- ~~**Ship an MSBuild task and `dotnet` tool**~~ — delivered end to end:
  the sealed `.bcal` format (`NotableDateBinaryResource.Write`/`Read`,
  `NotableDateResourceLoader.LoadBinary`, `SaveBinary` on the builder,
  documented in the binary-rule-packs guide) round-trips every bundled
  catalogue byte-stably with integrity digests; the
  `Bodu.Globalization.Calendar.Tool` package ships the `bodu-calendar`
  tool (`lint` / `compile` / `info` over the stable `BODU-CAL-*`
  diagnostics); and `Bodu.Globalization.Calendar.Build` wires
  `NotableDatePack` items to incremental build-time compilation via a
  `ToolTask` over the same tool. Neither package is in a shipping wave
  yet — they join `bld/release-manifest.txt` with the calendar wave.
- **Document round-trip guarantees** between builder output and the JSON
  resource rule provider.

### `Bodu.Globalization.Calendar.DependencyInjection`

Current state: bridge; `AddNotableDateService` /
`AddReloadableNotableDateService` (declared in the `Bodu.Globalization.Calendar`
namespace).

- ~~**Add key-aware `AddNotableDateService("AU")`**~~ — delivered:
  keyed overloads (`AddNotableDateService(serviceKey, resource | factory)`)
  register per-jurisdiction singletons resolvable through the .NET 8
  keyed-service surface, alongside `NotableDateServiceOptions` overloads
  and `TryAdd` idempotent registration.
- ~~**Add `IHostedService` cache warm-up**~~ — delivered by the
  `Bodu.Globalization.Calendar.Caching` package
  (`AddNotableDateCacheWarmup` registers the hosted
  `NotableDateCacheWarmupService`, which drives
  `CachingNotableDateService.Warm` over a configurable rolling
  territory/year window). Remaining first-request cost for non-caching
  setups is the uncached document parse in the core loader — tracked as
  a core-package item, not a DI one.
- ~~**Add `IOptionsMonitor<NotableDateOptions>` rebuild support**~~ —
  delivered: `AddReloadableNotableDateService<TOptions>` binds the
  reloadable service to `IOptionsMonitor<TOptions>`, rebuilding the
  resource through the registration's factory on every options change;
  a factory failure is logged and keeps the previous resource serving.

### `Bodu.Globalization.Calendar.Plugins`

Current state: new; trust-gated external plugin loader for assemblies
contributing custom `INotableDateAlgorithm` implementations.

- **This is the AOT-blocking component** (reflective assembly load). Its
  path to AOT-compatibility is the binary-rule-pack format from the
  Builder roadmap; until then it is correctly marked AOT-incompatible —
  the loader's public surface now carries `[RequiresUnreferencedCode]` /
  `[RequiresDynamicCode]` so trimmed and AOT consumers get an analyzer
  signal instead of a runtime surprise.
- ~~**Document the trust-gate contract**~~ — delivered: the calendar
  plugin-trust guide states what each policy validates, the
  admission-check-not-sandbox boundary, entry-point strength
  (path overloads vs the weak already-loaded overload), registration
  collision policy, and unloading; the README and apidoc now position
  `StrongNamePluginTrustPolicy` as an identity label, not integrity.

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
  audit each rule pack against authoritative sources. The first
  territory-layer official sweeps now exist as the pattern to extend:
  the US pack carries the 210-row OPM federal-holiday vector table
  (2011–2030, `UsFederalHolidays-2011-2030.csv` — statutory Sat→Fri /
  Sun→Mon substitution, three cross-year 31 December in-lieu New Year's
  Days, Juneteenth from 2021), and the NZ pack pins the Employment New
  Zealand 2026–2027 dates including the conflict-aware Boxing Day 2027
  Tuesday-28-December substitution chain, and the GB pack is swept
  against the official GOV.UK `bank-holidays.json` feed (264 rows,
  2019–2028, exact dates and substitute flags per home nation —
  delivered 2026-08 and archived at `corpus/uk/`; the sweep surfaced
  and fixed the Scottish 2 January chained-substitution defect). Next
  candidates: TH from the Bank of Thailand corpus table, and LK once
  the pack-existence decision is made.
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

The 2026-07 additions come from a systematic gap review against the
highest-adoption Java and Python utility ecosystems (Guava / Apache
Commons / libphonenumber / ical4j / Quartz on the Java side; the Python
stdlib plus `dateutil`, `rapidfuzz`, `phonenumbers`, `pint` on the Python
side). Only functionality passing all three of the section's criteria was
kept: (a) no `net8.0`+ BCL equivalent, (b) demonstrably reached for in
.NET today via an independently-built package, and (c) a clean mapping
onto one of the proven architectural patterns. Proposals that failed the
filter were added to *Non-goals* instead.

- **`Bodu.Formats.Outlook.Msg`** — a read-only `.msg` (Outlook message)
  reader over `Bodu.IO.Compound`. The `.msg` container *is* CFB, so this
  is the highest-leverage reuse of the existing container: it inherits
  the full CFB read stack and only adds MAPI property-name decoding.
  Independently, `.msg` reading is served almost entirely by commercial
  libraries or `MsgReader`; there is no first-party option. The
  namespace should flatten to **`Bodu.Formats.Outlook`** (the
  `Bodu.Formats.Excel` convention) so the MAPI property surface —
  property tags / types, named-property resolution, the recipient and
  attachment tables, and the message / folder value types — is shared
  with the `.pst` candidate below rather than owned by either package.
  **In flight** — kickoff plan authored and scaffolding landed
  2026-07-31; tracked in the per-project section
  *`Bodu.Formats.Outlook` / `Bodu.Formats.Outlook.Msg`* above, executing
  [`Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md`](Bodu.Formats.Outlook/docs/msg-reader-implementation-plan.md).
- **`Bodu.Formats.Outlook.Pst`** (over a new low-level **`Bodu.IO.Pst`**
  container) — a read-only `.pst` / `.ost` mailbox-archive reader:
  folder hierarchy, message enumeration, recipients, and attachments
  (including nested attached messages). Deliberately **not** built on
  `Bodu.IO.Compound`: MS-PST is its own three-layer container — the NDB
  block / B-tree layer (with the "compressible encryption" byte
  permutation), the LTP heap-on-node / property-context / table-context
  layer, and the messaging layer — with no CFB inside, so it adds a
  third low-level container to the container + format-reader split
  (`Bodu.IO.Compound` for CFB, the proposed `Bodu.IO.Packaging` for
  OPC, `Bodu.IO.Pst` for NDB/LTP). What it *does* reuse is the
  flattened `Bodu.Formats.Outlook` MAPI value model shared with the
  `.msg` reader. Java: `java-libpst`; Python: `libpff` / `readpst`;
  .NET: XstReader or commercial suites (Aspose.Email) — one of the
  least-served document formats in the ecosystem. Initial scope:
  Unicode-format PST (the post-2003 default), with the legacy ANSI
  variant and OST deltas as demand-driven follow-ons.
  **Exploration authored 2026-07-31** — format anatomy, the NDB+LTP
  vs messaging layering split, API sketch, the fixture-acquisition
  blocker, and sequencing P0–P3:
  [`Bodu.IO.Pst/docs/pst-container-exploration.md`](Bodu.IO.Pst/docs/pst-container-exploration.md).
  **P0 executed same day** — `Bodu.IO.Pst` landed with the full NDB
  read layer at Preview; **P1 (the LTP layer) executed 2026-08-19** per
  [`Bodu.IO.Pst/docs/ltp-implementation-plan.md`](Bodu.IO.Pst/docs/ltp-implementation-plan.md);
  **P2 (the `Bodu.Formats.Outlook.Pst` messaging reader plus the
  container hardening pass) executed 2026-08-31** per
  [`Bodu.Formats.Outlook.Pst/docs/pst-reader-implementation-plan.md`](Bodu.Formats.Outlook.Pst/docs/pst-reader-implementation-plan.md)
  — all tracked in the per-project *`Bodu.IO.Pst`* section above. Both
  packages ship in release wave 3; the ANSI variant and OST deltas
  remain the demand-driven follow-ons.
- **`Bodu.Formats.Excel.OpenXml`** — a read-only `.xlsx` value reader over
  an OPC/ZIP container, **sharing the flattened `Bodu.Formats.Excel`
  value model** (`ExcelCell` / `ExcelWorksheet` / `ExcelWorkbookProperties`).
  The namespace was already flattened in anticipation of this. Would
  likely sit on a new **`Bodu.IO.Packaging`** (Open Packaging Convention
  over `System.IO.Compression`) container — the ZIP-era sibling to
  `Bodu.IO.Compound`.
- **`Bodu.Text.Similarity`** — string distance and phonetic matching: the
  edit-distance family (Levenshtein, Damerau-Levenshtein, Jaro-Winkler,
  longest-common-subsequence, n-gram / cosine similarity) plus the
  phonetic encoders (Soundex, Metaphone / Double Metaphone, NYSIIS,
  Caverphone). The Java analogue is `commons-text` similarity +
  `commons-codec` language; Python's is `rapidfuzz` / `jellyfish`; .NET
  consumers currently pull FuzzySharp or Fastenshtein. Span-based,
  allocation-free, KAT-driven — the same profile as the check-digit
  family it conceptually neighbours.
- **`Bodu.Globalization.Recurrence` has landed.** ✅ Recurrence-rule
  evaluation shipped as its own Core-only package: `RecurrenceRule`
  (RFC 5545 `RRULE` parse/format over `IParsable`/`ISpanParsable`/
  `IFormattable`, the full `FREQ`/`INTERVAL`/`COUNT`/`UNTIL`/`WKST` and
  `BY*` model, and occurrence enumeration for `DAILY`..`YEARLY` with
  `BYSETPOS` over `DateTime`/`DateTimeOffset`), the fluent
  `RecurrenceRuleBuilder`, `RecurrenceSet` (compose rules with
  `RDATE`/`EXDATE` and parse an iCalendar property block), and
  `CronExpression` (Vixie five-field and optional-seconds six-field
  layouts, ranges/steps/lists/names, the `@yearly`…`@hourly` macros, and
  `GetNextOccurrence`/`GetPreviousOccurrence`). Conforms to the existing
  calendar `IDateRecurrenceStrategy` contract shape (ascending,
  deduplicated, window-invariant) and reuses `Bodu.Core`'s `NextOccurrence`
  primitives rather than taking a `Bodu.Globalization.Calendar` dependency.
  Validated against an RFC 5545 §3.8.5.3 worked-example corpus. A second
  wave (driven by the FallbackPlan shared-scheduling requirements) added
  `AnchoredInterval` (instant-anchored interval recurrence in the RFC 5545
  §3.3.6 duration grammar, `anchor + k·interval` for `k ≥ 1` with the
  anchor passed per query), previous-occurrence queries on every form,
  `RecurrenceSet` canonical formatting and value equality, defect-naming
  `TryParse(s, out result, out failureMessage)` overloads on all four
  forms, and a metadata-scan purity guard banning wall-clock and
  machine-time-zone APIs from the assembly. A third pass compared the
  engine against defects reported to python-dateutil, rrule.js, ical4j,
  ical.net, ical.js, libical, lib-recur, ice_cube, Cronos, NCrontab,
  cronie, croniter, robfig/cron and Quartz, adding that corpus as
  regression tests and fixing what it found: Vixie's leading-character
  rule for cron day-field restriction (and the day-mask combination that
  depends on it), candidate-set deduplication before `BYSETPOS`/`COUNT`,
  the `BYDAY` ordinal when `BYDAY` limits alongside `BYMONTHDAY`, a
  calendar overflow while scanning never-matching weekly rules, and
  `BYWEEKNO` week numbering (now the ISO rule generalized to `WKST`,
  keeping year-straddling weeks and expanding the whole week when
  `BYDAY` is absent). Deferred
  follow-ons: sub-daily RRULE frequencies (`HOURLY`/`MINUTELY`/`SECONDLY`
  parse and round-trip but do not yet enumerate), Quartz cron extensions
  (`L`/`W`/`#`/`?`), a read-only `.ics` (iCalendar) reader, and a
  period fast-forward for `RecurrenceRule` point queries with distant
  anchors (they currently enumerate from `DTSTART`).
- **`Bodu.Identifiers`** — ULID, Snowflake, NanoID, KSUID generation and
  parsing. Ubiquitous independently-built functionality with no BCL home,
  and a natural consumer of the existing Crockford Base32 support (in
  `Bodu.IO.Hashing`) and the `Bodu.Text.Encoding` alphabets. Pairs
  naturally with **Sqids** (reversible short-ID encoding), which could
  live here or in `Bodu.Text.Encoding`. (Note: `Guid.CreateVersion7`
  ships in the BCL from `net9.0`, so the candidate's durable value
  concentrates in ULID / Snowflake / NanoID / KSUID and their string
  encodings, not UUIDv7 generation itself.)
- **`Bodu.Text.Diff`** — sequence and text differencing: Myers O(ND)
  diff over generic sequences, line / word / char inline diff, and
  unified-diff (`@@`-hunk) read / write. Python ships this *in the
  stdlib* (`difflib`); Java uses `java-diff-utils`; .NET has no BCL
  story and pulls DiffPlex. Self-contained and purely algorithmic — the
  same profile as `Bodu.IO.Hashing`. Three-way merge is a possible later
  layer, not initial scope.
- **`Bodu.IO.FileSignatures`** — content-type detection from leading
  magic bytes (ZIP / OLE2 / PDF / PNG / ELF / …), exposed through a
  name-resolvable registry like `BinaryEncodings`. Python:
  `python-magic` / `filetype`; Java: Tika's detector (commonly pulled
  for this alone); .NET: MimeDetective / FileSignatures. Complements
  `EncodingDetection` in Core (text sniffing) with the binary side, and
  gives `Bodu.IO.Compound` / `Bodu.Formats.*` a shared front door for
  "what is this file?".
- **`Bodu.Formats.Mime`** — a read-only RFC 5322 / MIME (`.eml`) message
  reader: header parsing with encoded-word (RFC 2047) decoding, the
  multipart body tree, and attachment extraction. Python ships `email`
  in the stdlib; Java has jakarta.mail; .NET's only real option is
  MimeKit. Pairs with the `.msg` candidate above so the two email
  containers share one reading story, and consumes `QuotedPrintable` /
  `Base64` from `Bodu.Text.Encoding`. Read-only — composing and sending
  mail stay out of scope.
- **`Bodu.Globalization.PhoneNumbers`** — E.164 phone-number parse /
  validate / format with per-region metadata packs mirroring the
  `Calendar.Data.<Region>` pattern. Google's libphonenumber is among the
  most-adopted utility libraries in the Java world and `phonenumbers`
  its Python twin; .NET consumers use the `libphonenumber-csharp` port.
  The honest cost is metadata upkeep (numbering plans change
  constantly) — scope discipline is parse / format / validate only, no
  carrier lookup or geocoding, and the pack split keeps the data out of
  the core assembly.
- **`Bodu.Text.Transliteration`** — Unicode-to-ASCII folding (the
  `unidecode` operation) and slug generation. Python: `unidecode` +
  `python-slugify`; Java: ICU4J's `Transliterator` (often pulled for
  this alone); .NET: Slugify.Core / Unidecode.NET. Data-driven mapping
  tables and span-based transforms — the `Bodu.Text.Encoding` profile
  applied to text normalization.
- **`Bodu.Functional`** — the railway set (`Result<T>` / `Option<T>` /
  `Either<TLeft,TRight>` plus the async combinators) has now **shipped
  inside the `Bodu.Core/Functional` seam**, deliberately not as a
  package (the HOTP/TOTP restraint precedent). The extraction trigger
  stands: if the seam grows further — `Validation`-style error
  accumulation, `ValueTask` combinator variants, applicative
  surfaces — extract the whole seam into this dedicated package rather
  than bloating Core. Decide before `Bodu.Core/v1.0.0` tags if such
  growth is imminent; afterwards the move is breaking.
- **`Bodu.Units`** — dimensioned quantities and unit conversion over
  generic math (length / mass / duration / data size / …, with
  compile-time dimension safety on the `Money<TCurrency>` model).
  Python: `pint`; Java: JSR-385 / Indriya; .NET: UnitsNet, one of the
  most-depended-on community packages. The unit catalogue would be
  generated by an out-of-band tool exactly like the ISO 4217
  `CurrencyCode` pipeline. Large surface — needs an explicit scoping
  pass before adoption.
- **`Bodu.Globalization.Humanize`** — human-readable rendering: relative
  time ("3 hours ago"), byte sizes, ordinals, number-to-words, English
  pluralization. Python: `humanize` / `inflection`; Java: spread across
  `commons-lang` and one-off helpers; .NET: Humanizer — a top-tier
  community package. The scope risk is localization sprawl: an
  invariant-English core with per-culture packs (the data-pack pattern
  again) is the only shape worth shipping; without that discipline this
  stays a candidate, not a commitment.
- ~~**Numeric value types in `Bodu.Numerics`** — a generic `Complex<T>`
  (the BCL `Complex` is `double`-only)~~ — **shipped.** `Complex<T>` over
  `IFloatingPointIeee754<T>` landed inside the existing `Bodu.Numerics`
  project (no new package), with a companion `ComplexJsonConverter<T>` in
  `Bodu.Numerics.Serialization.Json`. `BigDecimal` and the
  running-statistics aggregates shipped earlier in the same wave.
- ~~**One-time-password codes (TOTP/HOTP)**~~ — **shipped.** `Hotp` /
  `Totp` landed inside `Bodu.Security.Cryptography` (flat namespace, no new
  package or dependency), not the `Bodu.Security.Otp` sibling that was
  floated — the raw-byte-secret design kept it dependency-free, so a
  sibling was not warranted.
- ~~**Probabilistic data structures**~~ — **shipped.** `BloomFilter<T>`,
  `CountMinSketch<T>`, and `HyperLogLog<T>` landed inside the
  `Bodu.Collections` package as the `Bodu.Collections.Probabilistic`
  namespace — no separate package was warranted (the comparer-based
  hashing kept the types dependency-free beyond Core). See the
  `Bodu.Core` section above for the shipped contracts.

## Cross-cutting themes

### Architectural patterns

Three architectures have proven themselves across the repository. Treat
them as first-class templates, and conform new work to the closest match
rather than inventing a fourth shape.

1. **The `System.Text.Json`-shaped quartet** — a ref-struct
   `Utf8*Reader` / `Utf8*Writer` token surface, a `*Serializer` POCO
   mapper (converters + attribute family + naming policies + callbacks),
   a mutable `*Node` DOM, and a read-only `*Document` DOM. Proven by
   `Bodu.Text.Bencode`, `Bodu.Text.Toml`, and `Bodu.Text.Yaml`, and — via
   the *Active focus* #4 redesign — by `Bodu.Text.Delimited`,
   `Bodu.Text.DotEnv`, and `Bodu.Text.Ini`. **This is the template for
   every new structured-text format**; the line formats deviate only
   where documented (trivia-bearing mutable DOMs for DotEnv/INI, and
   serializer-local scalar conversion for the string-only wire).
2. **The container + format-reader split** — a low-level container
   (`Bodu.IO.Compound` for CFB; a proposed `Bodu.IO.Packaging` for OPC)
   with format readers layered on top that share a *flattened* value
   model (`Bodu.Formats.Excel.*`). New office/document readers (`.msg`,
   `.doc`, `.xlsx`) plug into this split.
3. **The resilient web-data-provider stack** — an abstract provider base
   (`WebRateProvider`) owning `HttpClient` + Polly resilience and
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

Target state:

- **AOT-clean (achievable now):** `Bodu.Core`, `Bodu.Numerics`,
  `Bodu.IO.Hashing`, `Bodu.IO.Compound`, `Bodu.Text.Encoding`,
  `Bodu.Security.Cryptography`, and the three `Utf8*` text libraries
  (`Bodu.Text.Bencode` / `.Toml` / `.Yaml`) — the ref-struct readers are
  reflection-free on the token path. **`Bodu.Numerics` is now verified**
  by a NativeAOT smoke app (`Bodu.Numerics/aot`, published + run in the
  `numerics-aot-smoke` CI workflow): it surfaced that `Fraction<T>`'s
  bounded-type probe used `MakeGenericMethod` (which throws under AOT
  despite the IL3050 suppressions), so that path was replaced with a
  reflection-free type matrix over the built-in bounded integer types.
  The companion `Bodu.Numerics.Serialization.Json` uses the standard
  reflection-based `JsonConverterFactory` pattern (annotated), so its
  consumers pair it with a source-generated `JsonSerializerContext` for AOT.
- **AOT-clean with work:** the `Bodu.Text.Delimited` / `.Ini`
  `*Serializer` reflection binders now have a reflection-free escape
  hatch — the `Bodu.Text.Formats.Generators` factories and the serializer
  factory overloads — while `.DotEnv` and the non-factory overloads
  remain reflection-bound; `Bodu.Financial` and
  `Bodu.Formats.Excel.Binary` (audit the property-mapping paths).
- **AOT-blocked by design:** the `Bodu.Globalization.Calendar.Plugins`
  loader — needs the binary-rule-pack format from the Builder roadmap
  before this changes.

### API-stability tiers

**Done.** Every packable project now carries a single tier label as a
blockquote directly under its README title. The assignment:

- **Stable** — the mature core of the solution: `Bodu.Core`,
  `Bodu.Collections.Concurrent`,
  `Bodu.Collections` (the specialized collection catalogue split out of
  `Bodu.Core` with namespaces unchanged; the tranche additions shipped
  with settled APIs per the implementation plan),
  `Bodu.IO.Hashing`, `Bodu.IO.Compound`,
  `Bodu.Text.Encoding`, `Bodu.Security.Cryptography`, the text-format and
  configuration libraries (`Bodu.Text.Bencode` / `.Toml` / `.Formats` /
  `.Configuration`, `Bodu.Extensions.Configuration.Text`),
  `Bodu.Formats.Excel.Binary`, `Bodu.Financial` (+ its DI package), the
  whole `Bodu.Globalization.Calendar` family (core, Builder, DI, Plugins,
  and the five data packs), and the shared
  `Bodu.Financial.ExchangeRates.DependencyInjection` plumbing.
- **Preview** — `Bodu.Numerics` (the interval algebra expanded quickly —
  `DiscreteInterval<T>`, `IntervalSet<T>`, and the pair result types are
  still settling their conventions, and the new `BigDecimal` and
  statistics-aggregate surfaces are settling; `Fraction<T>` is a stable
  candidate) and its companion
  `Bodu.Numerics.Serialization.Json` (the JSON contract is new — the core
  types are now serialization-agnostic and support is opt-in via
  `AddNumericsJsonConverters`), `Bodu.Text.Yaml` (the serializer reached
  family parity in 0.3.0 — enum converters, presets, the DOM bridges —
  and the new surface has not yet shipped) and the network-dependent
  exchange-rate family: the web providers `Bodu.Financial.ExchangeRates.{Boe,Ecb,Rba,Yahoo,Ofx,Oanda,Fixer,ExchangeRateHost,Fred,Imf}`
  and the three caching backends `Bodu.Financial.ExchangeRates.Caching{,.Sqlite,.Distributed}`.
  These are held at Preview until they have shipped and been exercised
  against their live upstream endpoints; the public API is largely settled,
  but behaviour against third-party feeds is not yet battle-tested.
- **Experimental** — `Bodu.Financial.ExchangeRates.Xe`, which depends on a
  scraped auth token and can break without notice.

Promote the Preview providers to Stable per package as each proves reliable
against its endpoint across a release cycle.

### Source generators

Most code generation is **tooling-based, not Roslyn**: the CRC catalogue
is generated by the `tools/Generate-CrcCatalog.ps1` script (from
`crc-specs.json`), and the ISO 4217 `CurrencyCode` enum + registration by
the `tools/CurrencyCatalogueGenerator` console tool (from
`currencies.json`). These run out-of-band and check their output into
source.

The first true incremental Roslyn generator is
**`Bodu.Text.Formats.Generators`** (`Bodu.Text.Formats.Generators/`,
netstandard2.0): it emits reflection-free
`IDelimitedRecordFactory<TRecord>` / `IIniSectionFactory<TSection>`
implementations for `[DelimitedRecord]` / `[IniSection]` partial POCOs,
with `BTFG00x` diagnostics and analyzer release tracking, and its test
project consumes it as an analyzer over its own compilation. It sets the
layout template for future generators: a sibling top-level project with
the standard `src`/`test` split, referenced by consumers with
`OutputItemType="Analyzer"`.

Remaining candidates where a generator buys AOT/trim readiness or
removes runtime reflection:

- Calendar rule packs (Builder roadmap — binary output for trim/AOT).

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
`financial/exchange-rate-caching.md` and the other FX guides). The guides
are now backed by the **runnable-samples suite** — the `samples/` tree
(Financial, Calendar, the text libraries, and the IO group, including a
live FX sample)
whose snippet-compile guards keep the documented code building. The one
remaining gap is a dedicated **calendar plugin-loader** guide under
`docs/guides/calendar/` (the loader is currently covered only implicitly by
`building-the-service.md` / `dependency-injection.md`).

## Proposing changes to this file

Treat this file the same as any other source change — open a PR, link the
issue or discussion that motivates the change, and bump the "Last updated"
line at the top. Changes should be **directional** (add a project, change
a non-goal, retire an item) rather than release-tracking.
