# Roadmap

Forward-looking plan for the **Bodu** C# utility library. Pairs with
[`CHANGELOG.md`](CHANGELOG.md) (what shipped) and [`CLAUDE.md`](CLAUDE.md)
(repository conventions for contributors).

*Last updated: 2026-06-08. The calendar territorial expansion has largely landed: the Americas pack now spans the Latin American set (AR, BR, CL, CO, MX, PE) alongside US/CA; Asia-Pacific has grown to fourteen countries (adding HK, ID, PH, TH, TW, VN); Europe now ships twenty-eight EU/EEA territories with the Orthodox-Easter overrides wired for Greece, Cyprus, Bulgaria, and Romania; and the two packs previously marked "proposed / does not exist" — `Bodu.Globalization.Calendar.Africa` (EG, ET, GH, KE, MA, NG, ZA) and `Bodu.Globalization.Calendar.MiddleEast` (AE, IL, JO, QA, SA, TR) — now exist in the solution. With that expansion done, the ChaCha/Salsa stream-cipher family and its AEAD layer complete, and the **password-hashing KDFs Argon2 and scrypt now shipped** (RFC 9106 / RFC 7914 — the last real BCL crypto gap), the highest-leverage net-new engineering items are now a TOML reader/writer in `Bodu.Text.Formats` and the Base45 / Bech32 encodings in `Bodu.Text.Encoding`.*

## How to read this

- **Release focus** lists everything sitting in `[Unreleased]` that needs
  to ship.
- **Non-goals** lists things the repository is deliberately *not* doing,
  to keep scope discussions short.
- **Per-project roadmap** has a short subsection per project in
  `bodu.slnx`. Each entry gives the current state plus 1–3 concrete
  forward-looking items.
- **Cross-cutting themes** covers concerns that span multiple projects
  (TFM policy, AOT/trim, API stability tiers, source generators).

Items in this file are intent, not commitments. The order under each
project is rough priority — the first bullet is what would land next if
work started today.

## Repository conventions

The roadmap assumes the conventions already documented in
[`CLAUDE.md`](CLAUDE.md). The ones most relevant to forward planning:

- **TFM baseline.** All shipping projects target `net8.0` only — no
  multi-targeting today. Bumping the floor is a roadmap decision (see
  *Cross-cutting themes*).
- **Test model.** Contract test bases under `Bodu.Test.Contracts` plus
  KAT records under `Bodu.Test.Kat`. New types should plug into the
  existing contract suite rather than introducing bespoke harnesses.
- **Style enforcement.** `Bodu.CodeStyle.XmlDocumentation` analyzers
  enforce documentation shape; `Bodu.props` carries `WarningsAsErrors`
  for CS1591. Treat doc gaps as build breaks.
- **Package metadata.** Shared in `bld/Bodu.props`. New packages should
  flow through the same props rather than redefining metadata locally.
- **Package validation.** `BoduEnablePackageValidation` is opt-in today;
  the roadmap commits to making it the default on all packable projects.

## Release focus

The `[Unreleased]` block in [`CHANGELOG.md`](CHANGELOG.md) is the
immediate publishing target. The queued packages are:

| Package | Version | Notes |
| --- | --- | --- |
| `Bodu.Numerics` | 1.0.0 | Initial release. `Fraction<T>` over any `IBinaryInteger<T>` and `Interval<T>` over any `INumber<T>`. |
| `Bodu.Financial` | 1.0.0 | Initial release. `Money<TCurrency>`, `MoneyValue`, `MoneyBag`, the ISO 4217 catalogue, and the timeless + dated FX provider stack. References `Bodu.Numerics`. |
| `Bodu.Globalization.Calendar` | 1.1.0 | Multi-assembly rule resolution; embedded `region-*.xml` resources removed. **Behavioural change** — parameterless `NotableDateService()` no longer ships every region's rules; consumers must reference a data pack. |
| `Bodu.Globalization.Calendar.Americas` | 1.0.0 | Initial release. AR, BR, CA, CL, CO, MX, PE, US. |
| `Bodu.Globalization.Calendar.AsiaPacific` | 1.0.0 | Initial release. AU, CN, HK, ID, IN, JP, KR, MY, NZ, PH, SG, TH, TW, VN. |
| `Bodu.Globalization.Calendar.Europe` | 1.0.0 | Initial release. 28 EU/EEA territories (AT, BE, BG, CY, CZ, DE, DK, EE, ES, FI, FR, GB, GR, HR, HU, IE, IT, LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK), with Orthodox-Easter overrides for GR, CY, BG, RO. |
| `Bodu.Globalization.Calendar.MiddleEast` | 1.0.0 | Initial release. AE, IL, JO, QA, SA, TR. |
| `Bodu.Globalization.Calendar.Africa` | 1.0.0 | Initial release. EG, ET, GH, KE, MA, NG, ZA. |

> **Note.** The data packs in the solution have outgrown the country
> sets recorded in the `[Unreleased]` CHANGELOG block (which still lists
> Americas as US/CA, Europe as eight countries, Asia-Pacific as eight,
> and omits the Middle East and Africa packs entirely). Reconcile the
> CHANGELOG to the table above before cutting the release.

**Release order.** The six Calendar packages must release together, as
Calendar 1.1.0 is the breaking change that necessitates the data packs.
`Bodu.Numerics` 1.0.0 / `Bodu.Financial` 1.0.0 can ship independently and should go first to
exercise the package-validation pipeline on a brand-new package ID.

**Versioning policy.** SemVer per package. Breaking changes inside a
single package bump the package's own major. Coordinated releases (like
this one) bump independently — Calendar 1.1.0 does not force Calendar
.Data.* to be 1.1.0 of their own. Git tags follow `<package>/<version>`,
e.g. `Bodu.Numerics/v1.0.0` or `Bodu.Financial/v1.0.0`.

## Non-goals

The repository is deliberately not doing these. They appear here so
proposals can be closed quickly.

- **Wrapping or replacing `bc-csharp`.** The vendored Bouncy Castle
  source lives under `bc-csharp/` purely as a KAT reference for the
  cryptography test suites. It is not redistributed.
- **Asymmetric cryptography.** `Bodu.Security.Cryptography` stays
  symmetric/AEAD/hash. RSA, ECDSA, Ed25519, key exchange — out of scope.
  Consumers should use `System.Security.Cryptography` directly.
- **A full IANA timezone database.** `Bodu.Globalization.Calendar`
  defers to `TimeZoneInfo`; it does not ship its own zone data.
- **General JSON / YAML / XML parsers.** `Bodu.Text.Formats` is for
  under-served formats (Bencode, Delimited, DotEnv, INI, and the
  proposed TOML). The framework-shipped parsers are sufficient for the
  mainstream formats.
- **Shipping the `Plugin*.TestAssembly` projects as packages.** Those
  exist purely to exercise the calendar plugin loader in tests.
- **Duplicating algorithms already shipped in the .NET BCL or
  Microsoft's first-party `System.*` NuGet packages.** Where the
  framework ships a stable equivalent, consumers should use it
  directly rather than a Bodu type. Concretely, the roadmap will not
  re-implement: `System.Security.Cryptography.ChaCha20Poly1305`,
  `HKDF`, `Rfc2898DeriveBytes.Pbkdf2`, `Shake128` / `Shake256`,
  `CShake128` / `CShake256`, `Kmac128` / `Kmac256` /
  `KmacXof128` / `KmacXof256`; `System.IO.Hashing.XxHash32` /
  `XxHash64` / `XxHash3` / `XxHash128`; `System.Buffers.Text.Base64` /
  `Base64Url`; `Convert.ToHexString` / `FromHexString`. Bodu only
  takes on algorithms with a genuine BCL gap — extended-nonce or raw
  cipher variants, configurable algorithm catalogues, encodings
  Microsoft has not shipped, or KDFs the BCL team has explicitly
  declined (Argon2, scrypt). Pre-existing types in the repository
  that overlap with later BCL additions (the legacy `Shake` internal
  primitive, the single-polynomial `Crc32` paths covered by
  `System.IO.Hashing.Crc32`) are kept for source compatibility but
  are not extended.

## Active focus

The 19-item Bodu.Core hardening pass that previously lived in `todo.md`
is **complete**. Evidence is in the repository — `XorShiftRandom`
correctness fixes, `PooledBufferBuilder<T>` checked growth and
convenience APIs, `ConcurrentHashSet<T>` approximate-count surface,
`WeekPattern` as a `readonly partial struct` with a struct enumerator,
single-TFM `net8.0` Core, intentional `InternalsVisibleTo` set.

With that pass closed, the active focus shifts to:

1. Cut the `[Unreleased]` packages above (reconciling the CHANGELOG
   country sets to the expanded data packs first).
2. **Argon2 and scrypt** are the highest-leverage net-new engineering
   item now that the calendar territorial expansion has largely shipped
   — see `Bodu.Security.Cryptography` below.
3. Begin the remaining per-project items below in roadmap order. The raw
   ChaCha20 / XChaCha20 family in crypto — the highest-leverage
   opening move — **has landed**: it closes the visible stream-cipher
   gaps that the BCL's `ChaCha20Poly1305` does not, and establishes the
   reusable `IStreamCipher` / `StreamCipherTransform` abstraction that
   the Salsa20 / XSalsa20, Rabbit, and HC-128 expansion now builds on.
   Hebrew, tabular Hijri, Umm
   al-Qura, and Persian (Solar Hijri) notable-date coverage has
   landed via XML resources resolved against the BCL
   `HebrewCalendar` / `HijriCalendar` / `UmAlQuraCalendar` /
   `PersianCalendar` and the existing `sweepCalendarYears`
   resolver — no custom algorithm code was required.

## Per-project roadmap

### `Bodu.Core`

Current state: mature; 398 src / 784 test files. Hardening pass closed.

- Extract `WeekPattern` to its own `Bodu.Globalization.WeekPattern`
  package now that it is a `readonly partial struct` with a struct
  enumerator. `Bodu.Globalization.Calendar` already consumes it
  heavily, and other globalization-adjacent packages should be able to
  take a dependency on the pattern type without pulling all of Core.

### `Bodu.Security.Cryptography`

Current state: mature; 152 src / 484 test files. Threefish 256/512/1024,
Skipjack, Blowfish, Twofish, Camellia, Ascon, Skein, BLAKE2/3, Tiger,
SipHash plus EAX/OFB/GCM/OCB/SIV modes.

- **Raw ChaCha20 and XChaCha20 have landed.** `ChaCha20` (RFC 8439)
  and the extended-nonce `XChaCha20` (`draft-irtf-cfrg-xchacha`) ship
  as confidentiality-only stream ciphers — the gap the BCL's
  `System.Security.Cryptography.ChaCha20Poly1305` does not fill (raw
  keystream for libsodium-, Noise-, and age-style protocols, plus the
  192-bit nonce). They introduced a reusable stream-cipher abstraction
  that parallels the block-cipher stack: `IStreamCipher` (raw
  keystream primitive), the abstract `StreamCipherTransform`
  (`ICryptoTransform` glue owning keystream carry, self-inverse XOR,
  and 32-bit counter-overflow protection), and an `IStreamCipherAlgorithm`
  marker so stream ciphers opt out of block padding/mode suites. The
  extended-nonce **XChaCha20-Poly1305 AEAD** that composes this engine
  with the existing `Poly1305` MAC has since shipped (see below).
- **Expand the stream-cipher family: Salsa20 / XSalsa20, Rabbit, and
  HC-128.** ✅ **Shipped.** All four ciphers are built on the
  `StreamCipherAlgorithm` base and the shared `IStreamCipher` /
  `StreamCipherTransform` stack introduced with ChaCha20, with concrete
  tests inheriting the common `StreamCipherAlgorithmTests` contract:
  - **Salsa20** over 128- and 256-bit keys (eSTREAM and Crypto++/ECRYPT
    vectors plus the 131,072-byte long-stream XOR digest) and its
    extended-nonce **XSalsa20** (HSalsa20 subkey + Salsa20, mirroring
    HChaCha20 / XChaCha20; NaCl `crypto_core_hsalsa20` and XSalsa20
    vectors).
  - **Rabbit** (RFC 4503; conformant to the RFC's I2OSP / big-endian
    octet convention, verified against the Appendix A.1/A.2 keystream
    vectors and the Appendix B internal-state debugging vectors).
  - **HC-128** (eSTREAM software portfolio; the canonical key=0/IV=0 and
    key=0x80../IV=0 vectors).

  Rabbit and HC-128 confirmed the value of the engine-owns-advancement
  `NextKeystreamBlock` contract: their keystreams come from evolving
  internal state with no seekable block counter, yet both dropped onto
  the shared transform with zero changes. All four are raw,
  confidentiality-only primitives carrying nonce-reuse and
  unauthenticated-ciphertext warnings in their XML docs; AEAD remains
  the recommended default.
- **Extended-nonce Poly1305 AEAD constructions.** ✅ **Shipped.** Three
  transforms now compose the raw stream ciphers with the existing
  `Poly1305` MAC on the public `Poly1305AeadTransform` base and the
  construction-neutral `IAeadTransform` / `IStreamAeadTransform` surface
  (one-shot `Encrypt`/`Decrypt` with optional associated data):
  `XChaCha20Poly1305` (RFC 8439 framing over XChaCha20, matching
  libsodium's `crypto_aead_xchacha20poly1305_ietf`), `XSalsa20Poly1305`
  (NaCl / libsodium `crypto_secretbox`-compatible — no associated data,
  MAC over ciphertext only, with explicit `tag ‖ ciphertext` layout
  converters), and the Bodu-defined `XSalsa20Poly1305Aead` (XSalsa20
  under the RFC 8439 framing — not an IETF standard and not
  interoperable). None ship in the BCL, whose `ChaCha20Poly1305` is
  12-byte-nonce only. Locked against the RFC 8439 §2.8.2,
  draft-irtf-cfrg-xchacha A.3.1, and libsodium secretbox vectors; the
  Bodu-defined XSalsa20 hybrid (no published
  vector) is checked against a derived oracle built from the
  independently-tested public `XSalsa20` keystream and `Poly1305` MAC.
- **Password-hashing KDFs Argon2 and scrypt have landed.** ✅
  **Shipped.** `Argon2d` / `Argon2i` / `Argon2id` (RFC 9106) and
  `Scrypt` (RFC 7914) close the remaining BCL gap (`HKDF` and `Pbkdf2`
  already ship in `System.Security.Cryptography`; Microsoft declined
  Argon2). Each offers an instance surface and static one-shot
  `DeriveKey`, plus PHC encoded-hash `Hash` / constant-time `Verify`
  for password storage. Argon2 bundles its own arbitrary-length
  BLAKE2b for `H'`; scrypt composes the BCL `Pbkdf2` (HMAC-SHA256)
  with a Salsa20/8 core. Locked against every RFC 9106 §5 and RFC 7914
  §12 vector (the ~1 GiB scrypt vector runs in the Stress tier). The
  next crypto step is the AVX-512 capability-detection contract below.
- Finalise the AVX-512 fast paths shipped for BLAKE2/BLAKE3/Threefish
  behind a documented capability-detection contract, so consumers can
  reason about when SIMD paths engage and how to disable them in
  constant-time-sensitive contexts.

### `Bodu.IO.Hashing`

Current state: mature; 83 src / 223 test files. Fletcher 16/32/64, full
RevEng CRC catalogue (112 standards), check-digit algorithms (Luhn,
Damm, Verhoeff, Gumm, Code 39 mod 43, Crockford Base32, ABA, EAN, GTIN,
IBAN, ISBN, ISIN, LEI, ISO 7064).

- The **check-digit expansion has landed**: Verhoeff and Gumm (both
  detecting all single-digit and adjacent-transposition errors), the
  Code 39 modulo-43 barcode check character, and the Crockford Base32
  modulo-37 check symbol (ULID-style) all ship in
  `IO.Hashing.CheckDigits`. Remaining catalogue gaps are minor; the
  family is now strong on financial, barcode, and identifier schemes.
- Unify every algorithm behind the BCL
  `System.IO.Hashing.NonCryptographicHashAlgorithm` shape uniformly.
  Some types inherit from it, others expose bespoke surfaces — the mix
  is a documentation hazard.
- **Document the `System.IO.Hashing` interop story.** xxHash
  (`XxHash32` / `XxHash64` / `XxHash3` / `XxHash128`) and the
  single-polynomial `Crc32` / `Crc64` (ISO 3309) ship in Microsoft's
  `System.IO.Hashing` package; consumers should reach for those first.
  The headline value of this project is the full RevEng CRC catalogue
  (112 named polynomials across CRC-8 / CRC-16 / CRC-32 / CRC-64),
  the legacy non-cryptographic family (FNV, MurmurHash3, CityHash,
  Fletcher, Pearson, Bernstein, etc.), and the check-digit family —
  none of which are in the BCL.

### `Bodu.Text.Encoding`

Current state: mature; 80 src / 137 test files. Base16, Base32, Base58,
Base64, Base64Url, Base85 with RFC 4648 / Bitcoin / Crockford / Ascii85
/ Z85 variants.

- **Ship Base45** (RFC 9285) and **Base62**. Base45 in particular is
  the QR-code workhorse encoding and a frequent request.
- **Ship Bech32 and Bech32m.** Base58Check is already present; Bech32
  is the natural sibling and the address encoding used across newer
  cryptocurrency protocols.
- Audit that every `Base*.Utf8.cs` surface has full
  `IUtf8SpanFormattable`-style writer parity with the char paths.
  Several paths skew char-first.

### `Bodu.Text.Formats`

Current state: mature; 49 src / 95 test files. Bencode, Delimited
(RFC 4180), DotEnv, INI.

- **Add a TOML reader and writer.** Conspicuously absent next to
  Ini/DotEnv/Bencode/Delimited, and the most-requested missing format
  for `.NET` configuration scenarios.
- **Add streaming async readers.** Current `*.Parser.cs` surfaces are
  synchronous; add `IAsyncEnumerable<T>` and `ValueTask`-returning
  read APIs for large inputs.
- **Add a source generator that binds `[DelimitedRecord]` and
  `[IniSection]` POCOs** so consumers can avoid reflection at runtime.
  This is a clear win for AOT readiness too.

### `Bodu.Text.Configuration`

Current state: mature; 37 src / 63 test files. INI-compatible profile,
resolver, view getters.

- **Stabilise `ConfigurationPattern.Compile`.** The expression-
  compilation surface needs an API-stability pass before consumers
  build dependencies on it.
- **Add JSON-pointer and JMESPath-style resolvers** alongside the
  existing `ConfigurationResolver`. Today the resolver story is
  Bodu-specific; standardising on at least one mainstream query
  syntax broadens applicability.

### `Bodu.Extensions.Configuration.Text`

Current state: bridge layer; 7 src / 19 test files. Connects
`Microsoft.Extensions.Configuration` to `Bodu.Text.Configuration`.

- **Add Bencode and TOML sources** once `Bodu.Text.Formats` ships them.
- **Document precedence semantics** when combined with the `Json` and
  `EnvironmentVariables` providers — consumers stack providers and need
  ordering rules.

### `Bodu.Numerics`

Current state: new. Ships `Fraction<T>` and `Interval<T>`. Money,
currency, and FX types live in the companion `Bodu.Financial`
package below.

- **Ship the 1.0 package** per `[Unreleased]` — covers `Fraction<T>`
  and `Interval<T>`.
- **Extend `Interval<T>` with the gaps from 1.0**: unbounded /
  half-bounded intervals (the current type is always bounded);
  `Difference` / `SymmetricDifference` returning disjoint-interval
  sets; algebraic operators (`|` for union, `&` for intersection)
  when both operands are guaranteed contiguous. The 1.0 surface
  intentionally ships only the contiguous-result subset.

### `Bodu.Financial`

Current state: new. Split out of `Bodu.Numerics` before the v1
release. Ships `Money<TCurrency>` with the full active + historic
ISO 4217 catalogue (~185 tag types), the runtime-tagged
`MoneyValue`, the multi-currency `MoneyBag` aggregate,
`CurrencyRegistry` for runtime ISO-to-metadata lookup, the timeless
`IExchangeRateProvider` with `FixedExchangeRateTable`, and the dated
FX stack (`IDatedExchangeRateProvider`, `FixedDatedExchangeRateProvider`,
`CompositeDatedExchangeRateProvider`, `ExchangeRateSeries`,
`DatedExchangeRateProviderAdapter`). References `Bodu.Numerics` for
the exact-arithmetic escape hatch through `Fraction<BigInteger>`.

- **Ship the 1.0 package** per `[Unreleased]`.

Possible v1.1:

- **Cross-currency Roslyn analyzer** for `Money<T1> + Money<T2>` to
  catch attempted compile-time mixing in code that bridges through
  `MoneyValue`. The operator signatures already enforce the
  type-parameter case; the analyzer would catch the rarer cases
  involving generic helpers.
- **Time-series exchange-rate provider with historical observation
  store** — `IDatedExchangeRateProvider` is the abstraction;
  consumer-facing historical-rate sources (ECB, FRED, RBA) could
  ship as separate companion packages.
- **`Bodu.Financial.Xml`** child package if XML serialisation
  support is needed. Kept opt-in because `System.Xml.Serialization`
  carries heavier dependencies than the always-present
  `System.Text.Json`.
- **MoneyBag mutable builder** if benchmarks show hot-path
  per-operation allocation cost; the immutable bag is sufficient
  for the documented v1 workloads.

### `Bodu.Globalization.Calendar`

Current state: mature; 161 src / 202 test files. Easter (Western and
Orthodox), Lunar New Year, Vesak, Asalha Puja, Qingming, Losar, Hindu
lunar festivals, rule providers, observed-date adjustments,
`NotableDateService`. Hebrew, tabular Hijri, Umm al-Qura, and Persian
(Solar Hijri) observances ship as XML resources resolved against the
BCL `HebrewCalendar` / `HijriCalendar` / `UmAlQuraCalendar` /
`PersianCalendar` plus the `sweepCalendarYears` resolver — no custom
algorithm classes were needed. The Fixed-strategy calendar-year sweep
is documented end-to-end in
`docs/guides/calendar/non-gregorian-calendars.md` with worked examples
for each supported calendar family.

- **Add observation-based algorithm variants for the four lunar /
  solar-Hijri families** where the BCL's tabular calculation can
  diverge from the announced civil date by one day: Saudi-observed
  crescent sighting (Umm al-Qura tabular vs Royal Court announcement),
  Tehran-observed vernal equinox (PersianCalendar tabular vs Iranian
  civil calendar at the cycle boundaries circa year 1488 / 1525 AP).
  These are opt-in alternatives to the tabular resources, not
  replacements.
- **Extend the Hebcal-aligned Hebrew regression catalogue** from the
  six-year (2020–2025) starter set already shipping in
  `GlobalJewishResourceTests.Regression` to a full 50-year sweep once
  the regression-tier surface area justifies the maintenance cost.
  Same shape is owed to the Saudi Umm al-Qura calendar (versus
  ummulqura.org.sa) and the Persian calendar (versus the Iranian civil
  calendar table from the Astronomical Applications Department).
- **Add `IAsyncEnumerable<NotableDate>` projections** for streaming
  large date-range queries (e.g. fiscal calendars across many years).

### `Bodu.Globalization.Calendar.Builder`

Current state: thin; 6 src / 14 test files. Source generator producing
calendar resource assemblies from rule XML/JSON.

- **Add fluent rule-validation lint** with diagnostic codes mirroring
  `Bodu.Text.Configuration`'s diagnostic-code surface, so authors get
  build-time feedback on rule pack errors.
- **Ship an MSBuild task and `dotnet` tool** that compiles JSON rule
  packs to a sealed binary format. Critical for trim/AOT scenarios
  where reflective JSON parsing at startup is undesirable.
- **Document round-trip guarantees** between the builder output and
  `JsonResourceNotableDateRuleProvider` — consumers building tooling
  on top need a stable contract.

### `Bodu.Globalization.Calendar.DependencyInjection`

Current state: bridge; `IServiceCollection` extensions for registering
calendar services.

- **Add key-aware `AddNotableDateService("AU")`** for multi-tenant
  scenarios where one process serves multiple jurisdictions.
- **Add `IHostedService` cache warm-up** so the first request after
  process start does not pay the rule-load cost.
- **Add `IOptionsMonitor<NotableDateOptions>` rebuild support** so
  config changes propagate without a process restart.

### `Bodu.Globalization.Calendar.Americas`

Current state: shipping in `[Unreleased]` 1.0.0. AR, BR, CA, CL, CO,
MX, PE, US.

- **The Latin America expansion has landed** — AR, BR, CL, CO, MX, and
  PE now ship alongside US/CA, all at national level. The next
  territorial gap is **subdivision-level data** (Brazilian states,
  Mexican states, Canadian provinces beyond the national set) where US
  state and Canadian provincial coverage is the model to extend.
- **Document holiday-source citations** per country so consumers can
  audit the rule pack against authoritative sources.
- **Ship fiscal-calendar packs** (US federal FY, retail 4-5-4). These
  are not religious or civil holidays, but they are the next natural
  layer of "notable dates" the service should answer.

### `Bodu.Globalization.Calendar.AsiaPacific`

Current state: shipping in `[Unreleased]` 1.0.0. AU, CN, HK, ID, IN,
JP, KR, MY, NZ, PH, SG, TH, TW, VN.

- **The country expansion has landed** — HK, ID, PH, TH, TW, and VN
  now ship alongside the original eight, all at national level.
  **Subdivision-level data** remains the open gap: India, Indonesia,
  Philippines, Vietnam, Thailand (and Pakistan/Bangladesh when added)
  are national-only today. AU subdivisions already exist; the rest of
  the region needs the same treatment.
- **Add multi-day Chinese New Year expansion** and Lunar New Year
  regional variants. Today the rule fires for the single primary date.
- **Wire territory rules to `global-islamic-umm-al-qura.xml`** for
  Saudi-aligned jurisdictions where the Royal Court's Eid
  announcements drive the local public-holiday calendar (currently
  Malaysia and Singapore rules cherry-pick from tabular
  `global-islamic.xml`; both have explicit subdivisions that follow
  Saudi sighting).

### `Bodu.Globalization.Calendar.Europe`

Current state: shipping in `[Unreleased]` 1.0.0. 28 EU/EEA territories
— AT, BE, BG, CY, CZ, DE, DK, EE, ES, FI, FR, GB, GR, HR, HU, IE, IT,
LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK.

- **The country expansion and Orthodox overrides have landed** — the
  pack grew from eight to twenty-eight territories, and the
  Orthodox-Easter overrides for Greece, Cyprus, Bulgaria, and Romania
  are wired against the existing Orthodox Easter algorithm.
- **Add subdivision-level packs** — Spanish autonomous communities and
  Swiss cantons. German *Länder* and the UK constituent-country splits
  (England, Wales, Scotland, Northern Ireland) already ship; the bulk of
  remaining European regional holidays are subdivision-specific. (Note:
  Switzerland is not yet in the national set above; add CH before, or
  alongside, its canton data.)

### `Bodu.Globalization.Calendar.Africa`

Current state: exists in the solution under
`Bodu.Globalization.Calendar.Data/`; queued for `[Unreleased]` 1.0.0.
EG, ET, GH, KE, MA, NG, ZA. Islamic observances are wired
(`global-islamic.xml` tabular and `global-islamic-umm-al-qura.xml`
Saudi-aligned).

- **Verify Ethiopia's Ge'ez-calendar coverage.** Ethiopia uses the
  Ge'ez calendar; confirm whether the shipped EG/ET rules cover the
  Ge'ez-dated observances correctly or whether a dedicated algorithm
  (or a BCL coverage check) is still owed in
  `Bodu.Globalization.Calendar`.
- **Add subdivision-level data** and **document holiday-source
  citations** per country, matching the pattern owed across the other
  packs.

### `Bodu.Globalization.Calendar.MiddleEast`

Current state: exists in the solution under
`Bodu.Globalization.Calendar.Data/`; queued for `[Unreleased]` 1.0.0.
AE, IL, JO, QA, SA, TR. Saudi/UAE/Qatar/Jordan wire
`global-islamic-umm-al-qura.xml`, IL wires `global-jewish.xml`, and TR
wires tabular `global-islamic.xml` (Diyanet uses tabular rather than
Saudi sighting).

- **Add Iran (IR).** The original v1 set included IR via
  `global-persian.xml`; it is not yet in the shipped pack and is the
  obvious next country.
- **Add subdivision-level data** and **document holiday-source
  citations** per country.

### `Bodu.Test` *(shared test infrastructure)*

Current state: infrastructure project; no `src/`, 82 files of shared
test helpers. Not published.

- **Promote `IKat` and the KAT record helpers as a public
  `Bodu.Test.Kat` NuGet** so downstream consumers can plug into the
  same testing model.
- **Migrate older `WeekPatternKats.cs` / `WeekPatternKatTests.cs`
  patterns** onto the unified `IKat` shape — they predate the standard
  and are the last meaningful holdouts.
- **Add a benchmark-results contract** so `bench/` projects produce
  comparable JSON across the Encoding, Configuration, Formats, and
  Cryptography benchmark suites.

### `Bodu.CodeStyle` *(separate solution)*

Current state: independent analyzer / code-fix solution, not in
`bodu.slnx`. Provides the `BODU1001`–`BODU1019` documentation-shape
analyzers, `BODU1039`–`BODU1041`, and the `BODU11xx`–`BODU14xx` XML-doc
wrap/formatting series (most recently `BODU1406` for overlong
`<typeparam>` content), plus an XML-doc formatter.

- **Document each analyzer code** under `docs/codestyle/` with a
  one-page entry: rule, rationale, examples, suppression guidance.
- **Add code-fix coverage** for any rule that currently only diagnoses
  — every analyzer should ship with at least a basic fixer.
- **Publish a JSON-schema** for `bodu.xmldocstyle.json` so editors can
  validate configuration.

### `bc-csharp` *(vendored)*

Bouncy Castle source vendored as a crypto KAT reference. Non-goal: do
not redistribute, do not extend.

## Cross-cutting themes

### TFM policy

All shipping projects currently target `net8.0` only. The roadmap
direction is to follow Microsoft's LTS cadence — move the floor to
`net10.0` when `net8.0` exits standard support, and never multi-target
older `netstandard` versions without a concrete consumer ask. The
existing `netstandard2.0` `ItemGroup` conditionals in a few `.csproj`
files are dead code and should be removed in the next routine sweep.

### AOT and trim readiness

No project sets `IsAotCompatible` or `IsTrimmable` today. Target state:

- **AOT-clean (achievable now):** `Bodu.Core`, `Bodu.Numerics`,
  `Bodu.IO.Hashing`, `Bodu.Text.Encoding`, `Bodu.Security.Cryptography`.
- **AOT-clean with work:** `Bodu.Text.Configuration`, `Bodu.Text.Formats`
  (needs the source-generator binding to replace reflection).
- **AOT-blocked by design:** `Bodu.Globalization.Calendar` plugin
  loader — needs the binary-rule-pack format from the Builder roadmap
  before this changes.

### API-stability tiers

Every published project should carry a single tier label in its
README: **Stable**, **Preview**, or **Experimental**. Recommended
starting labels:

- *Stable*: Core, IO.Hashing, Text.Encoding, Text.Formats,
  Text.Configuration, Extensions.Configuration.Text,
  Security.Cryptography.
- *Preview*: Globalization.Calendar (1.1.0 carries a breaking
  parameterless-constructor change), Numerics (initial release),
  Globalization.Calendar.Data.* (initial release),
  Globalization.Calendar.DependencyInjection,
  Globalization.Calendar.Builder.

### Source generators

Generators are a recurring theme across this roadmap:

- CRC catalogue (already generated from `crc-specs.json`).
- Calendar rule packs (Builder roadmap — binary output for trim/AOT).
- Delimited / INI POCO binding (Text.Formats roadmap).

Treat them as a first-class strategy rather than per-project
one-offs. New generators should live under
`<Project>.Builder/` mirroring the existing Calendar.Builder layout.

### Package validation rollout

`BoduEnablePackageValidation` is opt-in today. Make it the default for
all packable projects before the next coordinated release. Sweep any
warnings the rollout surfaces as part of that release's QA pass.

### Documentation parity

Every shipping project should have a `docs/guides/<project>/` entry.
The `Bodu.Numerics` directory has an overview and a dedicated
`Interval<T>` article; a per-feature `Fraction<T>` article is still
owed before the 1.0 ships.

## Proposing changes to this file

Treat this file the same as any other source change — open a PR, link
the issue or discussion that motivates the change, and bump the
"Last updated" line at the top. Changes should be **directional** (add
a project, change a non-goal, retire an item) rather than
release-tracking (the `CHANGELOG.md` is the authoritative shipping
record).
