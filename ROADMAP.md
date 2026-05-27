# Roadmap

Forward-looking plan for the **Bodu** C# utility library. Pairs with
[`CHANGELOG.md`](CHANGELOG.md) (what shipped) and [`CLAUDE.md`](CLAUDE.md)
(repository conventions for contributors).

*Last updated: 2026-05-27.*

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
immediate publishing target. Five packages are queued:

| Package | Version | Notes |
| --- | --- | --- |
| `Bodu.Numerics` | 1.0.0 | Initial release. `Fraction<T>` over any `IBinaryInteger<T>`. |
| `Bodu.Globalization.Calendar` | 1.1.0 | Multi-assembly rule resolution; embedded `region-*.xml` resources removed. **Behavioural change** — parameterless `NotableDateService()` no longer ships every region's rules; consumers must reference a data pack. |
| `Bodu.Globalization.Calendar.Data.Americas` | 1.0.0 | Initial release. US and CA. |
| `Bodu.Globalization.Calendar.Data.AsiaPacific` | 1.0.0 | Initial release. AU, CN, IN, JP, KR, MY, NZ, SG. |
| `Bodu.Globalization.Calendar.Data.Europe` | 1.0.0 | Initial release. DE, ES, FR, GB, IE, IT, NL, SE. |

**Release order.** The four Calendar packages must release together, as
Calendar 1.1.0 is the breaking change that necessitates the data packs.
`Bodu.Numerics` 1.0.0 can ship independently and should go first to
exercise the package-validation pipeline on a brand-new package ID.

**Versioning policy.** SemVer per package. Breaking changes inside a
single package bump the package's own major. Coordinated releases (like
this one) bump independently — Calendar 1.1.0 does not force Calendar
.Data.* to be 1.1.0 of their own. Git tags follow `<package>/<version>`,
e.g. `Bodu.Numerics/v1.0.0`.

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

1. Cut the five `[Unreleased]` packages above.
2. Begin the per-project items below in roadmap order. Calendar
   algorithm gaps (Islamic, Hebrew, Persian) and the raw ChaCha20 /
   XChaCha20 family in crypto are the highest-leverage opening moves —
   they unblock data-pack expansion and close the visible
   stream-cipher gaps that the BCL's `ChaCha20Poly1305` does not.

## Per-project roadmap

### `Bodu.Core`

Current state: mature; 398 src / 784 test files. Hardening pass closed.

- Extract `WeekPattern` to its own `Bodu.Globalization.WeekPattern`
  package now that it is a `readonly partial struct` with a struct
  enumerator. `Bodu.Globalization.Calendar` already consumes it
  heavily, and other globalization-adjacent packages should be able to
  take a dependency on the pattern type without pulling all of Core.
- Promote `ConcurrentCircularBuffer<T>` to a documented public type
  with the same `IProducerConsumerCollection<T>` story planned for
  `ConcurrentHashSet<T>`. Today it is reachable but undocumented.

### `Bodu.Security.Cryptography`

Current state: mature; 152 src / 484 test files. Threefish 256/512/1024,
Skipjack, Blowfish, Twofish, Camellia, Ascon, Skein, BLAKE2/3, Tiger,
SipHash plus EAX/OFB/GCM/OCB/SIV modes.

- **Add raw ChaCha20 and the XChaCha20 / XChaCha20-Poly1305 family.**
  ChaCha20-Poly1305 itself ships in the BCL as
  `System.Security.Cryptography.ChaCha20Poly1305` (.NET 6+); the gap
  is the raw ChaCha20 stream cipher (needed for libsodium-, Noise-,
  and age-style protocols) and the extended-nonce XChaCha20 variants,
  which Microsoft has not shipped.
- **Add password-hashing KDFs: Argon2 and scrypt.** No
  password-hashing surface today. `HKDF` and `Pbkdf2` are already in
  `System.Security.Cryptography` and are not in scope. Argon2 and
  scrypt are the real gap — Microsoft has explicitly declined to ship
  Argon2 because only OpenSSL implements it among the supported OS
  crypto providers.
- Finalise the AVX-512 fast paths shipped for BLAKE2/BLAKE3/Threefish
  behind a documented capability-detection contract, so consumers can
  reason about when SIMD paths engage and how to disable them in
  constant-time-sensitive contexts.

### `Bodu.IO.Hashing`

Current state: mature; 79 src / 209 test files. Fletcher 16/32/64, full
RevEng CRC catalogue (112 standards), check-digit algorithms (Luhn,
Damm, ABA, EAN, GTIN, IBAN, ISBN, ISIN, LEI, ISO 7064).

- **Expand check digits**: Verhoeff, Gumm, Mod-43, and a
  ULID/Crockford32 check-digit. The existing set is strong on financial
  identifiers; these fill the rest of the common catalogue.
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
- **Publish `ConfigurationDiagnosticCode` as a stable enum** with
  doc-comments tying each code to its meaning, so consumers can write
  diagnostic-aware error handlers.

### `Bodu.Extensions.Configuration.Text`

Current state: bridge layer; 7 src / 19 test files. Connects
`Microsoft.Extensions.Configuration` to `Bodu.Text.Configuration`.

- **Add `IFileProvider` reload-on-change parity** with
  `Microsoft.Extensions.Configuration.Json`. Today static sources only.
- **Add Bencode and TOML sources** once `Bodu.Text.Formats` ships them.
- **Document precedence semantics** when combined with the `Json` and
  `EnvironmentVariables` providers — consumers stack providers and need
  ordering rules.

### `Bodu.Numerics`

Current state: new; 16 src / 18 test files. Ships `Fraction<T>` only.

- **Ship `Fraction<T>` 1.0** per `[Unreleased]`.
- **Add `Interval<T>`** arithmetic over `INumber<T>` — closed/open
  bounds, intersection/union, contains, length. Natural companion to
  `Fraction<T>` and gives the package a second header type. Use
  `Interval<T>`, not `Range<T>`: `System.Range` is the existing BCL
  slicing type and the names would collide for consumers.
- **Add fixed-point `Money<TCurrency>`** with proper rounding and
  currency-tagging. Re-uses `Fraction<T>` internally for exact
  decimal computation.

### `Bodu.Globalization.Calendar`

Current state: mature; 161 src / 202 test files. Easter (Western and
Orthodox), Lunar New Year, Vesak, Asalha Puja, Qingming, Losar, Hindu
lunar festivals, rule providers, observed-date adjustments,
`NotableDateService`.

- **Add Islamic (Hijri civil and Umm al-Qura), Hebrew, and Persian
  (Solar Hijri) notable-date algorithms.** Clear gap alongside the
  existing Eastern catalogue. Unblocks the Asia-Pacific data pack's
  Ramadan/Eid rules and the proposed Middle East pack.
- **Add `IAsyncEnumerable<NotableDate>` projections** for streaming
  large date-range queries (e.g. fiscal calendars across many years).
- **Promote the plugin loader** (today exercised only by the 4
  `Plugin*.TestAssembly` projects) to a documented public extension
  point.

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

### `Bodu.Globalization.Calendar.Data.Americas`

Current state: shipping in `[Unreleased]` 1.0.0. US, CA.

- **Expand to MX, BR, AR, CL, CO.** Today the Americas pack is North
  America only; Latin America is the single largest territorial gap.
- **Document holiday-source citations** per country so consumers can
  audit the rule pack against authoritative sources.
- **Ship fiscal-calendar packs** (US federal FY, retail 4-5-4). These
  are not religious or civil holidays, but they are the next natural
  layer of "notable dates" the service should answer.

### `Bodu.Globalization.Calendar.Data.AsiaPacific`

Current state: shipping in `[Unreleased]` 1.0.0. AU, CN, IN, JP, KR,
MY, NZ, SG.

- **Add subdivision-level data** for India, Pakistan, Bangladesh,
  Indonesia, Philippines, Vietnam, Thailand. AU subdivisions already
  exist; the rest of the region needs the same treatment.
- **Add multi-day Chinese New Year expansion** and Lunar New Year
  regional variants. Today the rule fires for the single primary date.
- **Ship Ramadan and Eid** via the new Hijri algorithm once
  `Bodu.Globalization.Calendar` adds it.

### `Bodu.Globalization.Calendar.Data.Europe`

Current state: shipping in `[Unreleased]` 1.0.0. DE, ES, FR, GB, IE,
IT, NL, SE.

- **Add subdivision-level packs** — German *Länder*, Spanish autonomous
  communities, Swiss cantons. The bulk of European regional holidays
  are subdivision-specific.
- **Add UK constituent-country splits** (England, Wales, Scotland,
  Northern Ireland) — bank holidays diverge meaningfully.
- **Add Orthodox-calendar overrides** for Greece, Cyprus, Bulgaria,
  Romania. The Orthodox Easter algorithm already exists in Calendar;
  the data pack just needs to wire it.

### `Bodu.Globalization.Calendar.Data.Africa` *(proposed)*

Does not yet exist. v1 set: ZA, NG, KE, EG, MA, GH, ET.

- **Create the package** and ship the v1 country set.
- Egypt and Morocco need the Hijri algorithm; depend on the Calendar
  algorithm work landing first.
- Ethiopia uses the Ge'ez calendar — may need its own algorithm in
  `Bodu.Globalization.Calendar` before this pack can fully cover it.

### `Bodu.Globalization.Calendar.Data.MiddleEast` *(proposed)*

Does not yet exist. v1 set: SA, AE, IL, TR, IR, JO, QA.

- **Create the package** once Hijri, Hebrew, and Persian (Solar Hijri)
  algorithms ship in `Bodu.Globalization.Calendar`.
- IL needs Hebrew calendar support; IR needs Solar Hijri; the Gulf
  states need Hijri.

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
`bodu.slnx`. Provides `BODU1001`–`BODU1018` and `BODU1040` analyzer
codes plus an XML-doc formatter.

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
Most do; `Bodu.Numerics` is the obvious gap. Bring it to parity before
shipping `Bodu.Numerics` 1.0.

## Proposing changes to this file

Treat this file the same as any other source change — open a PR, link
the issue or discussion that motivates the change, and bump the
"Last updated" line at the top. Changes should be **directional** (add
a project, change a non-goal, retire an item) rather than
release-tracking (the `CHANGELOG.md` is the authoritative shipping
record).
