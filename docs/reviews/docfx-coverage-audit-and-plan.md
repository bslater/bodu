# DocFX Documentation — Coverage Audit and Remediation Plan

**Date:** 2026-09-08
**Scope:** `docs/` (DocFX project: `docs/docs`, `docs/guides`, `docs/samples`, `docs/apidoc`, `docs/articles`, the TOCs, `docfx.json`, the publish workflow) audited against every shipping project in `bodu.slnx`.
**Method:** see §1. Every finding below was verified against `src/` by grep or by reading the source; file and line references are relative to `docs/` unless prefixed.

---

## 0. Executive summary

The site is **structurally sound but not complete, and in places not accurate**.

| Measure | Result |
|---|---|
| `docfx docs/docfx.json` build | Clean — 0 warnings, 9,539 HTML files |
| TOC hrefs that do not resolve | 0 (across `toc.yml`, `docs/toc.yml`, `guides/toc.yml`, `samples/toc.yml`, `articles/toc.yml`) |
| Relative Markdown links that do not resolve | 0 |
| Guide pages on disk but absent from every TOC | 2 (`guides/cryptography/hardware-acceleration.md`, `guides/cryptography/one-time-passwords.md`) |
| Folders published by the `**/*.md` glob but linked from no TOC | 3 (`reviews/`, `forensic-review/`, `articles/`) |
| Shipping projects in `bodu.slnx` (`*/src/*.csproj`) | 61 (59 packable) |
| Packages with a full Introduction / Concepts / Getting-started trio | 26 |
| Packages with **no** trio | 35 — every companion package (Financial ExchangeRates ×15, Calendar companions ×11, Outlook ×3, `Bodu.Text.Serialization`, `Bodu.Numerics.Serialization.Json`, `Bodu.Financial.Serialization.Json`, `Bodu.Globalization.Recurrence`, `Bodu.Text.Formats.Generators`) |
| Packages with no guide page at all | `Bodu.IO.Pst`, `Bodu.Formats.Outlook.Pst`, `Bodu.Text.Serialization`, `Bodu.Financial.Serialization.Json`, `Bodu.Text.Formats.Generators` |
| Generated API namespace pages with no `apidoc/` overview (render as a bare type list) | 35 of 83 |
| Runnable sample project with no docs page | `samples/IO.Pst` (not in `samples/toc.yml` or `samples/index.md`) |
| Public types never mentioned in any conceptual page | ~330 (dominated by 132 currency tags in `Bodu.Financial` — acceptable — but also `Complex<T>`, `StringExtensions` ×77 files, `HashingStream`, `SerpentBlockCipher`, all `OutlookMailStoreReaderOptions` limits, `CalendarTool`, …) |
| Code samples that do not compile or throw at run time | ≥ 30 verified sites (§4) |
| Prose claims contradicted by the code | ≥ 60 verified sites (§4) |

The three biggest problems, in order:

1. **Accuracy.** Guides and `apidoc/` overviews carry snippets that will not compile (`Fraction<int>.CreateChecked`, `new NotableDateService(resource, registry)`, `holidays.IsHoliday(...)`, `Luhn.Instance`, `PearsonTableType.Pearson`, `Hkdf.Extract(ikm:)`) or throw (`GcmModeTransform` with a 16-byte nonce, `BlockMode = CipherModeKind.CTS`, `IPaddingStrategy.Pad(blockSize: 32)`), and prose that says the opposite of the code (YAML "has no Stream or async API"; serializers are "self-contained, no shared engine"; "only `Crc` is resumable"; PEM/PKCS#8 "out of scope" for Ed25519/X25519; "the `.pst` reader is future work").
2. **Coverage.** The documentation model is "one trio + guides + samples per package", but only the 26 primary packages have it. The 35 companion packages — which is where a consumer needs the most help (API keys, `appsettings` shapes, DI, caching, tooling) — are documented, if at all, as a paragraph inside a parent package's page.
3. **Navigation parity.** The home page names 18 of 59 packages; `introduction.md` omits 43; `getting-started.md`'s install block omits YAML, IO.Compound, Excel and every companion; the seven topic hubs exist in two copies (`docs/topics`, `guides/topics`) that have drifted; 22 of the 28 `guides/core/*` pages document `Bodu.Collections`, not `Bodu.Core`.

§6 lays out a five-phase plan. Phase 0 (guard rails) and Phase 1 (accuracy fixes) are small and should land first; Phases 2–4 add the missing pages in priority order.

---

## 1. Method

1. **Inventory.** Enumerated all 61 `*/src/*.csproj` projects in `bodu.slnx`, every `namespace` declaration in `src/`, every public top-level type per project, and every `.md`/`.yml` under `docs/`.
2. **Structural checks (scripted).** Resolved every `href:` in the five TOC files; resolved every relative and `~/`-rooted Markdown link; listed pages present on disk but in no TOC; compared generated `_site/api/*.html` namespace pages against `apidoc/*.md` overwrites; scanned for stub pages and placeholder markers.
3. **Build.** Installed the `docfx` global tool and ran the full metadata + site build exactly as CI does (minus `--warningsAsErrors`).
4. **Identifier cross-check (scripted).** Every backticked PascalCase identifier and every `new X(` / `X.Y(` call in the conceptual pages was checked against the full identifier bag of `src/`. Hits were then verified by hand.
5. **Domain reviews.** Six parallel read-throughs (Financial; Globalization; Core/Collections/Numerics; Text/Serialization/Configuration; Hashing/Cryptography; Binary formats + landing pages), each reading the pages in full and grepping every named type, member, option, DI registration, diagnostic code, and package id against `src/`.

Existing CI guards in `.github/workflows/docfx-build-publish.yml` (package-matrix ↔ csproj inventory, install commands, hero banners, icons, xref-instead-of-apidoc-links, stale profile names) all pass and are **not** the gap: they check that packages are *listed*, not that they are *documented* or that what is written is *true*.

---

## 2. Structural findings

### 2.1 TOCs and links — clean

All TOC hrefs resolve; all relative links resolve; every `xref:` uid used in the audited pages resolves to a declared type or namespace (1,182 distinct uids). The `--warningsAsErrors` build passes.

### 2.2 Orphaned and unlinked content

| Item | Finding | Action |
|---|---|---|
| `guides/cryptography/hardware-acceleration.md`, `guides/cryptography/one-time-passwords.md` | On disk, linked from `guides/cryptography/index.md:52,203`, absent from `guides/toc.yml`. Both are accurate (OTP signatures match `Hotp`/`Totp`); hardware-acceleration omits the PCLMULQDQ GHASH path. | Add to TOC (Foundations; new "One-time passwords" group). |
| `reviews/` (7 files), `forensic-review/` (10 files) | Matched by `docfx.json` `build.content` `**/*.{md,yml}`; no TOC; one inbound link (`docs/serialization/bencode/index.md:72`). Content is internal engineering material — pre-remediation security findings with file:line references, hot-path assessments, a KAT provenance tracker, "Nothing here has been applied" remediation lists. Published and indexed by site search. | Exclude `reviews/**` and `forensic-review/**` in `docfx.json`; replace the one bencode link with a short conformance section. |
| `articles/` | Has its own `articles/toc.yml` but is not in the root `toc.yml`; reachable only from `index.md:332`. `code-coverage.md` / `coverage-baseline.md` are maintainer material; `articles/index.md` is stale (calls the collection catalogue "Bodu.Core", counts two serializers). | Either add an "Articles" root nav entry and fix `articles/index.md`, or move the coverage pages to contributor docs and drop the folder. |
| Project-level `*/docs/` folders (`Bodu.Collections/docs`, `Bodu.Core/docs`, `Bodu.Formats.Outlook{,.Pst}/docs`, `Bodu.IO.{Compound,Pst}/docs`, `Bodu.Text.{Formats,Toml}/docs`) | Design/implementation plans; not part of the DocFX build. Fine as-is, but they are the only place some behaviour is described (e.g. `CompileNotableDatePack` task properties live only in `Bodu.Globalization.Calendar.Build/README.md`). | No action beyond lifting the consumer-relevant parts into guides (§5). |
| `samples/IO.Pst/Bodu.IO.Pst.Samples.PstBasics` | Runnable sample exists (`samples/README.md:31,82`) but there is no `docs/samples/io-pst.md`, no `samples/toc.yml` entry, and `samples/index.md:9-12` omits `IO.Pst/`. | Add the page. |
| `docs/samples/calendar.md` | Omits two shipped samples: `Bodu.Globalization.Calendar.Samples.Caching`, `…Samples.ValidationLint`. | Add. |

### 2.3 `docfx.json` metadata scope

`metadata.src.files = **/src/*.csproj` with exclusions for CodeStyle, Generators, Build, archive. Consequences:

- `Bodu.Financial.ExchangeRates.Testing` (`IsPackable=false`) and `Bodu.Globalization.Calendar.Tool` are in the API reference with no overview and no page that explains them; `guides/financial/testing-providers.md` and `samples/index.md:63` present the Testing package as *shipped*, which it is not.
- `Bodu.Text.Formats.Generators` is excluded, so `[DelimitedRecord]` / `[IniSection]` API pages point at a generator with no documentation target and no `BTFG001–003` reference.
- The workflow's "Validate generated API output" step spot-checks 11 namespaces; it does not check that every public namespace has an `apidoc/` overview.

### 2.4 Namespace overviews (`apidoc/`)

35 of 83 generated namespace pages have no overwrite and therefore render as a type list with an empty summary:

`Bodu.Financial.ExchangeRates.Testing`, `Bodu.Financial.Extensions` (35 files; xref'd from concept pages), `Bodu.Formats.Outlook` (xref'd from three pages), `Bodu.Globalization.Calendar.Caching` (24 public types across three packages), `Bodu.Globalization.Calendar.Tool`, `Bodu.IO.Compound.Builders`, `Bodu.IO.Compound.PropertySets`, `Bodu.Text.Serialization` (22 public types — the namespace every attribute xref lands in), and the 24 sub-namespaces `Bodu.Text.{Bencode,Toml,Yaml,Delimited,DotEnv,Ini}.{Reader,Writer,Nodes,Document[,Serialization]}`.

Two existing overviews describe something other than their namespace: `apidoc › Bodu.Globalization.Extensions.md` (claims `CultureInfo`/`RegionInfo`/`WeekPattern` helpers; the namespace holds one method, `DateTimeFormatInfoExtensions.LastDayOfWeek`) and `apidoc › Bodu.Text.md` (calls `Bodu.Text` a library and demonstrates `Bodu.Text.Ini.IniDocument`). `apidoc › Bodu.Globalization.Calendar.DependencyInjection.md` is keyed to a class uid but renders an H1 for a namespace that does not exist (the DI extensions are declared in `Bodu.Globalization.Calendar`).

---

## 3. Package coverage matrix

"Trio" = `docs/docs/<pkg>/{index,concepts,getting-started}.md`. "Shared" = covered only inside a parent package's page or a single multi-package guide.

| Package | Trio | Guides | Sample page | apidoc overview |
|---|---|---|---|---|
| Bodu.Core | yes | 6 Core-specific (+22 Collections guides in the same folder) | yes | yes (with errors, §4.3) |
| Bodu.Collections | yes | 22 (under `guides/core/`) | yes | yes (stale type list) |
| Bodu.Collections.Concurrent | yes | 1 | yes | yes (omits `ConcurrentEvictingDictionary`) |
| Bodu.Numerics | yes | 10 (no `Complex<T>`) | yes | yes (omits BigDecimal, Complex, statistics) |
| Bodu.Numerics.Serialization.Json | **no** | 1 | no | yes (omits Complex/BigDecimal converters) |
| Bodu.IO.Hashing | yes | 11 | yes | yes (with errors) |
| Bodu.Security.Cryptography | yes | 39 (+2 orphans) | yes | yes (omits asymmetric/KDF/OTP/HPKE) |
| Bodu.IO.Compound | yes | 6 | yes | yes; Builders/PropertySets **missing** |
| Bodu.IO.Pst | yes | **none** | **none** (sample exists) | yes |
| Bodu.Formats.Excel.Binary | yes | 4 | yes | yes |
| Bodu.Formats.Outlook | **no** | 3 (`.msg` only) | **none** | **missing** |
| Bodu.Formats.Outlook.Msg | **no** | shared | **none** | n/a (internal ns) |
| Bodu.Formats.Outlook.Pst | **no** | **none** (two code blocks in IO.Pst pages) | **none** | n/a |
| Bodu.Text.Encoding | yes | 13 | yes | yes |
| Bodu.Text.Filtering | yes | 4 | yes | yes |
| Bodu.Text.Configuration | yes | 4 | yes | yes (with errors) |
| Bodu.Extensions.Configuration.Text | yes | 2 | **none** | yes |
| Bodu.Text.Serialization | **no** | **none** | **none** | **missing** |
| Bodu.Text.Bencode / Toml | yes | 7 each | yes | yes; sub-namespaces missing |
| Bodu.Text.Yaml | yes | 5 (no callbacks / polymorphic pages) | yes | yes; sub-namespaces missing |
| Bodu.Text.Delimited / DotEnv / Ini | umbrella only | 1 each | shared (`formats.md`) | yes; sub-namespaces missing |
| Bodu.Text.Formats (meta) | yes | 6 | yes | n/a |
| Bodu.Text.Formats.Generators | **no** | **none** | **none** | excluded from build |
| Bodu.Globalization.Calendar | yes | 21 (+ 11 catalogue) | yes | yes |
| Calendar.Builder / .Plugins / .DependencyInjection | **no** | 1 shared each | no | yes (DI one mis-keyed) |
| Calendar.Caching (+Sqlite, +Distributed) | **no** | 1 shared | no | **missing** |
| Calendar.<Region> ×5 | **no** | 1 shared (`data-packs.md`) | shared | n/a |
| Calendar.Tool / .Build | **no** | section of `binary-rule-packs.md` | no | Tool **missing**; Build excluded |
| Bodu.Globalization.Recurrence | **no** | 1 (design essay; example does not compile) | yes | yes |
| Bodu.Financial | yes | 10 | yes | yes |
| Bodu.Financial.Serialization.Json | **no** | **none** (fragments in 5 pages) | no | yes (omits `CalculatedMoneyJsonConverter`) |
| Bodu.Financial.DependencyInjection | **no** | 1 shared | no | n/a |
| Bodu.Financial.ExchangeRates (+DI) | **no** | 1–2 shared | shared | yes |
| Bodu.Financial.ExchangeRates.Caching (+Sqlite, +Distributed) | **no** | 1 shared | no | yes |
| Bodu.Financial.ExchangeRates.<Source> ×11 | **no** | 1 shared (`exchange-rate-providers.md`) | shared | yes (IMF cache types omitted) |
| Bodu.Financial.ExchangeRates.Testing | **no** | 1 (describes a package that is not published) | no | **missing** |

---

## 4. Accuracy defects (verified against `src/`)

Severity: **C** = sample will not compile; **R** = sample throws at run time; **P** = prose contradicts the code. Only the highest-impact items are listed; each domain review recorded more of the same kind.

### 4.1 Cross-cutting / landing pages

| Sev | Location | Finding |
|---|---|---|
| P | `docs/introduction.md:79` | `Bodu.Text.Formats` "references `Bodu.Text.Encoding`" (it references Delimited/DotEnv/Ini); `Bodu.Text.Configuration` "builds on `Bodu.Text.Formats`" (references only Core); `Bodu.Extensions.Configuration.Text` omits its Toml and Bencode references. |
| P | `docs/getting-started.md:21`; `docs/licensing.md:46` | "The only shared dependency is `Bodu.Core`" — Financial→Numerics, IO.Pst→Collections, Excel→IO.Compound, every serializer→Text.Serialization. |
| P | `docs/getting-started.md:13` | "every package targets `net8.0`" — `Bodu.Globalization.Calendar.Build` targets `netstandard2.0`. |
| P | `docs/licensing.md:14,39-46` | License expression is in `bld/Copyright.props`, not `Bodu.props`; third-party table omits `Microsoft.Data.Sqlite`, StackExchange Redis, Polly/Http.Resilience, `System.Text.Encoding.CodePages`, `System.Text.Json`. |
| P | `index.md:63`, `docs/introduction.md:21`, `docs/getting-started.md:108` | `Bodu.Collections.Concurrent` described as two types; `ConcurrentEvictingDictionary<TKey,TValue>` missing (also from `docs/collections/index.md`, `topics/core-foundations.md`, `guides/core/index.md`, `choosing-a-collection.md`, `apidoc/…Concurrent.md`). |
| P | `docs/package-matrix.md:38,57,79,85,86,89-97,103` | Extensions.Configuration.Text deps omit Bencode; `Bodu.Text.Serialization` "consumed by (Bencode, TOML)" omits Yaml; providers "each paired with an opt-in DI companion" (none exist); IO.Pst deps omit `Bodu.Collections`; ".pst reader is future"; provider rows list `Bodu.Financial.ExchangeRates` twice; base ExchangeRates is *Preview* while its dependants are *Stable*; XE is *Stable* here but *Experimental* in the provider guide; `Bodu.Text.Formats` is *Preview* here, *Stable* in the topic page. |
| P | `docs/topics/text-and-serialization.md:41,54`; `docs/serialization/index.md:10`; the three serializer getting-started pages; `apidoc › Bodu.Text.{Bencode,Toml,Yaml}.md` | "Each depends only on `Bodu.Core` / no shared engine" — all six format packages reference `Bodu.Text.Serialization`, where every attribute, naming policy and callback interface lives. |
| P | `docs/topics/numerics-and-financial.md:7,15-19,112-118` | "three packages"; JSON converters and "three JSON wire policies" credited to `Bodu.Numerics` / `Bodu.Financial` instead of the `.Serialization.Json` companions; 20 financial packages absent. |
| P | `guides/index.md:17-48` | Under "### Bodu.Core" the cards are `Bodu.Collections` types; the namespace map says `Bodu.Collections.Generic` lives in Core. |
| P | `samples/index.md:63`, `guides/financial/testing-providers.md:7`, `samples/financial.md:92-96` | `Bodu.Financial.ExchangeRates.Testing` "ships" — csproj is `IsPackable=false`. |
| P | `articles/index.md:11,29-30` | Collection catalogue attributed to `Bodu.Core`; "two serializers". |

### 4.2 Financial

| Sev | Location | Finding |
|---|---|---|
| C | `docs/financial/getting-started.md:110` | `new Money(5m, invoice.IsoCode)` — `Money` exposes `CurrencyCode Code`; no `IsoCode`. |
| P | `guides/financial/exchange-rates.md:16`; `docs/financial/concepts.md:113`; `guides/financial/exchange-types.md:79` | `ExchangeRate`/`CurrencyPair` described with `FromIsoCode`/`ToIsoCode`; members are `From`/`To` (`CurrencyCode`). |
| P | `guides/financial/dependency-injection.md:25` vs `:128` | `FinancialOptions` "has a single `JsonPolicy` property" — the class is empty; the page contradicts itself. |
| P | `docs/topics/numerics-and-financial.md:19,81`; `guides/topics/numerics-and-financial.md:89`; `guides/financial/index.md:86`; `dependency-injection.md:7` | `AddFinancialService` "registers JSON converters" — that is `AddFinancialJson` in the JSON companion. |
| P | `docs/financial/index.md:59`, `concepts.md:190`, `apidoc › Bodu.Financial.Serialization.Json.md:11,22-27`, `package-matrix.md:59` | "all five converters" — `AddFinancialJsonConverters` registers six; `CalculatedMoneyJsonConverter` is never named. |
| P | `apidoc › Bodu.Financial.ExchangeRates.Caching.md:33-34` | "Extend `RateCacheBase<TOptions>` / `FileRateCacheBase<TOptions>`" — the storage seam is `internal abstract`; the caching guide (`:424-429`) says the opposite, correctly. |
| P | `guides/financial/exchange-rate-caching.md:420,450`; `package-matrix.md:103-104` | Consumers can validate a backend "against `RateCacheContractTests`" — the class lives only in the Caching **test** project. |
| P | `guides/financial/exchange-rate-caching.md:8` | Provider list stops at five of eleven. |
| P | `guides/financial/index.md:11` | "185-currency catalogue" — 184 (every other page says 184). |
| C | `docs/financial/getting-started.md:107`; `guides/financial/money.md:586` | `JsonSerializer.Deserialize<Money>(payload)` with no options, on pages that say registration is required. |

### 4.3 Core, Collections, Numerics

| Sev | Location | Finding |
|---|---|---|
| C | `guides/numerics/fraction.md:336-371`; `guides/numerics/bigdecimal.md:194` | `Fraction<int>.AdditiveIdentity`, `.IsNaN(...)`, `.MaxMagnitude`, `.CreateChecked(...)`, `.CreateSaturating(...)`, `BigDecimal.CreateChecked(5)` — all explicit interface implementations or DIM-only static virtuals; not callable on the concrete type. |
| C | `guides/numerics/fraction.md:322` | `x.ToMixedParts()` does not exist (three-way `Deconstruct` does). |
| C | `guides/numerics/bigdecimal.md:153` | `int order = BigDecimal.Min(a, b) == a;` assigns `bool` to `int`. |
| C | `apidoc › Bodu.Collections.Extensions.md:14,22,25-28` | `RecursiveSelectControl.IncludeAndDescend/IncludeAndStop/SkipAndDescend/SkipAndStop` — real members are `None, Yield, Recurse, Skip, Break, Exit, YieldAndRecurse, …`; `CountOrDefault(@default: 0)` has no parameter; non-generic `RecursiveSelect` shown with a typed lambda. |
| C | `apidoc › Bodu.md:18,30,35-36` | `WeekPattern.Monday \| WeekPattern.Tuesday` (no per-day statics; presets are `Weekdays`, `Weekend`, `AllDays`, `MondayToFriday`, …); `IRandomGenerator.Next(1, 7)` (interface has `Next(int)` only). |
| C | `apidoc › Bodu.Buffers.md:13,23` | `Append(span)`, `Span`, `ToArray` — real API is `Append(T)`, `AppendRange(ReadOnlySpan<T>)`, `GetSpan()`, `ToArrayAndDispose()`. |
| C | `apidoc › Bodu.Extensions.md:31,49,68,70` | `CalendarQuarterDefinition.Fiscal/Calendar` (members are `JanuaryToDecember`, `JulyToJune`, …); `StringExtensions.Normalize/Slice` (`NormalizeLineEndings`/`SliceSafe`); `GetFirstDateOfWeek(DayOfWeek)` does not exist; `IsoWeekOfYear` is a method. |
| C | `apidoc › Bodu.Collections.Generic.Concurrent.md:21` | `new ConcurrentCircularBuffer<long>` — constrained to `where T : class?`. |
| C | `apidoc › Bodu.Collections.Generic.md:9,51` | `Bodu.Text` "`BaseEncoding` entry points for Base16, Base24, …" — neither exists. |
| P | `apidoc › Bodu.Collections.Generic.Graphs.md:9`; `guides/core/graphs.md:7` | "two union-find structures, `DisjointSet` and `DisjointSet<T>`" — only the generic one exists. |
| P | `docs/core/index.md:64,88-95,110` | `IListExtensions` described as shuffles (that is `ShuffleHelpers`/`Randomize`); the "`Bodu.Text`" section lists `Bodu.Text.Encoding` types and omits `EncodingDetection`/`EncodingExtensions`/`StringEncodingExtensions`. |
| P | `docs/core/concepts.md:18`; `guides/core/week-pattern.md:37,143-159` | "use `Weekdays \| Weekend` for all seven days" — `WeekPattern.AllDays` exists; eight named presets absent from the API table. |
| P | `docs/numerics/index.md:9,13,46`; `guides/topics/numerics-and-financial.md:7`; `apidoc › Bodu.Numerics.md` | "two/three value types" — predates `BigDecimal`, `Complex<T>`, the four statistics types. |
| P | `guides/core/range-dictionary.md:177` | "reach for a dedicated interval-tree implementation" — `IntervalTree<T>` ships in the same package with its own guide. |
| P | `guides/core/choosing-a-collection.md:7,42,179`; ten guide footers | "Bodu.Core ships more than a dozen collection types" — they are in `Bodu.Collections`. |
| P | `guides/core/deque.md:157` | Leaks the `private const MinGrowCapacity`. |

### 4.4 Text, serialization, configuration

| Sev | Location | Finding |
|---|---|---|
| P | `docs/serialization/index.md:70`; `docs/serialization/yaml/{index:55,concepts:13-16,getting-started:47}.md`; `guides/serialization/yaml/using.md:7,26`; `apidoc › Bodu.Text.Yaml.md:28` | "YAML has no `Stream` overloads and no async API" — `YamlSerializer` has `Serialize<T>(IBufferWriter<byte>)`, `SerializeAsync(Stream)`, `Deserialize<T>(Stream)`, `DeserializeAsync<T>(Stream)`; `samples/yaml.md:41-42` lists them correctly. |
| C | `docs/serialization/toml/concepts.md:39-41`; `bencode/concepts.md:61-63`; `guides/serialization/{index:28,toml/index:20,35,bencode/index:33}.md`; `attributes.md`, `converters.md`, `callbacks.md`, `polymorphic-converters.md` in both TOML and Bencode; `guides/topics/text-and-serialization.md:133` | `[TomlConverter]`, `[BencodeConverter]`, `[Toml…]`/`[Yaml…]` attribute families, `ITomlOn…`/`IBencodeOn…` hooks — none exist; the shared `ConverterAttribute`, `IOnSerializing` etc. are in `Bodu.Text.Serialization`. |
| C | `docs/serialization/{toml,bencode}/getting-started.md:77-82 / 37-42` | `NamingPolicy.SnakeCaseLower` with only `using Bodu.Text.Toml;` — needs `using Bodu.Text.Serialization;`. |
| C | `guides/serialization/yaml/index.md:27`; `docs/serialization/yaml/concepts.md:70,90`; `guides/serialization/yaml/using.md:118,235` | Converter `Read(YamlElement, …)` (real: `Read(ref Utf8YamlReader, Type, options)`); `WriteTo(ref Utf8YamlWriter)` (no `ref`); `WriteInt64` (real: `WriteInteger`). |
| P | `apidoc › Bodu.Text.Configuration.md:34-35,46,55`; `docs/text-configuration/{concepts:25-26,115; getting-started:137}.md` | `ConfigurationMissingPathRootMode.IgnoreAnchoredPatterns` (enum has `UseEmptyRoot`, `Throw`); severity enum omits `Info`; four write presets claimed (real: `Bodu`, `EditorConfigCompatible`, `Normalized`); "public surface is read-only" while `getting-started` calls `SetEntry`. |
| P | `apidoc › Bodu.Extensions.Configuration.Text.md:44`; `docs/extensions-configuration-text/{index:77,78; concepts:22,189}.md` | Xrefs to `TextConfigurationLoader`, which is `internal`; `TomlConfigurationSource` called `FileConfigurationSource`-shaped (implements `IConfigurationSource`). |
| P | `docs/topics/text-and-serialization.md:34-36,49,60-66,78,84-90,97-98,128`; `text-and-serialization-concepts.md:7,21,30,64`; `docs/topics/configuration.md:102`; `guides/topics/text-and-serialization.md:85,90,95` | Line formats described with `Parse`/`Format`/`Try*`/`*ParseOptions` (real: `Utf8*Reader`/`Utf8*Writer`, `*ReaderOptions`/`*WriterOptions`); "without the serializer tier" (each has one); YAML omitted from the serializer section, namespace table and install block; Configuration "layered on the INI model that `Bodu.Text.Formats` ships" (it has its own); DotEnv "comment preservation and duplicate-key policies" (neither). |
| P | `docs/text-encoding/index.md:157`; `apidoc › Bodu.Text.Encoding.md:13,141` | TOML placed inside `Bodu.Text.Formats`; a "`Pearson` permutation tables" paragraph copied from the hashing apidoc. |

### 4.5 Hashing and cryptography

| Sev | Location | Finding |
|---|---|---|
| P | `docs/io-hashing/index.md:40,129-136,141,161`; `topics/hashing-and-cryptography-concepts.md:164`; `guides/io-hashing/{index:115,crc:427}.md`; `apidoc › Bodu.IO.Hashing.md:32,85` | "Only `Crc` implements `IResumableHashAlgorithm`" — Fnv ×4, Fletcher ×3, Adler32/32C/64 also do. |
| P | `docs/topics/hashing-and-cryptography.md:53`; `docs/io-hashing/{index:46,54-56; concepts:206,328,340,351}.md`; `guides/io-hashing/index.md:20,91`; `apidoc › Bodu.IO.Hashing.md:9,43` | Alphanumeric check digits placed in `Bodu.IO.Hashing.Checksums`; they are in `.CheckDigits`. `Isin` derives from `AlphanumericCheckDigitAlgorithm`, not `CheckDigitAlgorithm`. Root `CheckValueAlgorithm` never mentioned ("three base classes"). |
| P | `guides/io-hashing/crc-catalogue.md:183`; `crc.md:468`; three index pages | Catalogue lists `CRC64_JONES` (absent from `CrcStandards`) and omits `CRC8_BLUETOOTH`; count given as 113 (112 members). |
| C | `docs/io-hashing/getting-started.md:67`; `guides/io-hashing/pearson.md:43,65,101` | `new Pearson(outputWidthBits: 256)` (no such ctor); `PearsonTableType.Pearson` unqualified — the enum is nested (`Pearson.PearsonTableType`). |
| C | `apidoc › Bodu.IO.Hashing.CheckDigits.md:49-58` | `Luhn.Instance.IsValid`, `ComputeCheckDigit` — no `Instance`, no `ComputeCheckDigit`; API is static `Compute`/`IsValid` plus `Append`/`GetCurrentCheckDigit`. |
| P | `apidoc › Bodu.IO.Hashing.{Extensions:9,13,22; Checksums:9,13,32}.md`; `docs/io-hashing/index.md:148-150` | `CrcLookupTableBuilder` placed in Extensions (it is in Checksums); `CrcStandard` called a static class with a check-value member (instantiable; no such member); samples lack `using Bodu.IO.Hashing.Checksums;`. |
| C | `guides/io-hashing/{index:19,fnv:20,cityhash:137,murmurhash3:243}.md`; `guides/cryptography/{siphash:20,poly1305:11,skein:21,snefru:14,blake:28,ascon-hashing:21}.md` | Non-generic bases written as `BlockNonCryptographicHashAlgorithm<T>`, `Fnv<TSelf>`, `SipHash<T>`, `KeyedBlockHashAlgorithm<T>`, … |
| P | `guides/io-hashing/check-digits.md:41-42` | Luhn example body/check digit inconsistent (`"799273987"` → `'5'`, not `'3'`). |
| P | `docs/cryptography/{index:155; concepts:169,191}.md` | PKCS#8/SPKI/PEM "deliberately out of scope" — `Ed25519`/`X25519` implement them (`asymmetric-overview.md:60-78` is correct). |
| P | `docs/cryptography/{index:97; concepts:51,93,188}.md`; `apidoc › Bodu.Security.Cryptography.md:37,93`; `guides/cryptography/stream-ciphers.md:7` | `SymmetricStreamAlgorithm` is "a `SymmetricAlgorithm`… configure `Key` and `IV`" — it is `IDisposable` with `Key`, `Nonce`, `GenerateNonce()`; `SymmetricAlgorithmExtensions.GenerateNonce` does not exist. |
| C | `docs/cryptography/{index:127; concepts:189; getting-started:149-151,159-163}.md` | `GetHashAndReset()` exists on no public type; `AsconXof128` shown as `HashAlgorithm` (real: `Absorb`/`Squeeze`/`GetHash`); `new MerkleTreeHash(inner, leafSize:)` + `AppendData` (real: factory ctor, `ComputeHash` only). |
| R | `guides/cryptography/aead-modes.md:91-93,109-111,281-283` | 16-byte J0 passed to `GcmModeTransform` — ctor requires exactly 12 bytes; throws `ArgumentException`. |
| R | `guides/cryptography/cipher-modes.md:316-327`; `docs/cryptography/index.md:116-117`; `concepts.md:67`; `apidoc/…Cryptography.md:60` | `BlockMode = CipherModeKind.CTS` — `BlockCipherModeFactory` supports ECB/CBC/CFB/OFB/CTR only; CTS/XTS/OCB/EAX/SIV throw `NotSupportedException`. Enum has 10 members, docs list 7. |
| R | `guides/cryptography/padding.md:117-118,165-166` | `Pad(input, blockSize: 32)` — block size is in **bits** (256). |
| C | `guides/cryptography/hkdf.md:54` | `Hkdf.Extract(…, ikm: …)` — parameter is `inputKeyingMaterial`. |
| P | `guides/cryptography/aes-family.md:25,81-89` | "Camellia has no `SymmetricAlgorithm` wrapper" — `Camellia.Create()` exists and is used in getting-started. |
| R | `apidoc › Bodu.Security.Cryptography.Extensions.md:26-31` | `blowfish.EncryptEcb(...)` — BCL one-shot with no `TryEncryptEcbCore` override → `NotSupportedException`. |
| P | `apidoc › Bodu.Security.Cryptography.md:123` | `CryptoHelpers.ClearIfNotNull` does not exist. |
| P | `docs/cryptography/index.md:57,131,136,217`; `concepts.md:131` | BLAKE3 "in XOF mode / configurable output" (fixed 256-bit); `Tiger.HashingVariant` (real: `Variant`). |
| P | `guides/cryptography/hardware-acceleration.md:274` | "No other SIMD instruction set is used" — GCM/GCM-SIV use a PCLMULQDQ GHASH path. |
| P | `guides/cryptography/index.md:133`; `hashing.md:9-17` | Says BLAKE/Skein/Whirlpool/SHAKE have no walk-throughs "yet" — all four exist. |

### 4.6 Globalization

| Sev | Location | Finding |
|---|---|---|
| C | `apidoc › Bodu.Globalization.Calendar.Algorithms.md:188`; `guides/calendar/{algorithms:251, building-the-service:243}.md` | `new NotableDateService(resource, registry)` — no such ctor; registry goes in `NotableDateServiceOptions.Algorithms`. |
| C | `guides/recurrence/index.md:65-67` | `holidays.IsHoliday(date)` does not exist on `INotableDateService`; the only cross-package example does not compile. |
| C | `guides/calendar/adjustment-rules.md:284,311` | `context.Date` — property is `BaseDate` on both adjustment contexts. |
| P | `guides/calendar/caching/notable-date-caching.md:211` | Hit/miss log levels "default to `Trace`" — both default to `Information`. |
| P | `guides/calendar/{identity-and-resolution:194, resolution-pipeline:84}.md` | Unknown algorithm key "is a warning" — `BODU-CAL-ALGORITHM` is an Error (`validation-diagnostics.md` is correct). |
| P | `docs/calendar/index.md:155`; `apidoc/…DependencyInjection.md:13,23-26` | "No options object; three overloads" — six `AddNotableDateService` (incl. keyed) and four `AddReloadableNotableDateService` overloads. |
| P | `apidoc › Bodu.Globalization.Calendar.md:26,78`; `docs/calendar/{index:25,27; concepts:98}.md`; `guides/calendar/*` (8 pages); `guides/toc.yml:272` | Data packs named `Bodu.Globalization.Calendar.Data.*` / namespace `.Data` — package ids are `Bodu.Globalization.Calendar.<Region>`, namespace `Bodu.Globalization.Calendar`; Americas listed as "US, CA" (8 packs), AsiaPacific 8 of 14; Africa/MiddleEast omitted. |
| P | `docs/calendar/{index:49; concepts:51-60}.md`; `docs/topics/globalization-and-calendars.md:35`; `apidoc/…{Builder:25, Algorithms:152-160}.md`; 4 guides | "Six resolution strategies" — 13 `IDateCalculationStrategy` + 4 `IDateRecurrenceStrategy` implementations (`algorithms.md` and `binary-rule-packs.md` are correct). |
| P | `apidoc › Bodu.Globalization.Recurrence.md:32` | "`Bodu.Globalization.Calendar` consumes recurrence strategies from this package" — no reference exists in either direction. |
| P | `guides/calendar/catalogue/{theme-aggregates:11, theme-civil-and-christian:76}.md` | Literal PowerShell `$(@{Stem=…}.ResourceId)` rendered — generator bug. Eight bundled catalogues (`catholic`, `christian-anglican`, `christian-protestant`, `christian-oriental-orthodox`, `global-bahai`, `global-jain`, `global-sikh`, `global-zoroastrian`) have no section; 162 "Source" cells link `americas-common`/`africa-common`/`middleeast-common` to `index.md` sections that do not exist; catalogue stamped 2026-06-25, region XML changed since. |

### 4.7 Binary formats

| Sev | Location | Finding |
|---|---|---|
| P | `guides/outlook/index.md:15`; `package-matrix.md:86` | "a **future** `.pst` reader" — `Bodu.Formats.Outlook.Pst` ships. |
| P | `docs/io-pst/getting-started.md:15`; `docs/topics/binary-formats.md:58` | IO.Pst "depends only on `Bodu.Core`" — also `Bodu.Collections`. |
| C | `docs/io-compound/index.md:71-72` | `OpenStream(name).Open()` — no `Open()`; `TryOpenStream(name, out entry)` out-param is a `CompoundStream`. |
| P | `guides/outlook/reading-msg-files.md:24` | Raw `<see langword="null" />` leaked into Markdown. |
| P | `apidoc › Bodu.Formats.Excel.md:79` | Stray unbalanced code fence as the final line. |

---

## 5. Undocumented public surface and consumer-readiness gaps

Grouped by the question a consumer would ask that no page answers.

### 5.1 "How do I read a `.pst`?" — Outlook / PST (largest single hole)
`Bodu.Formats.Outlook.Pst` is documented only as two code blocks at the bottom of the IO.Pst pages. Never mentioned: `OutlookMailStoreReaderOptions` and its `Max*` hardening limits, `OutlookPstFormatException`, `OutlookMailFolder.EnumerateAssociatedMessages/ContainerClass/UnreadCount`, `OutlookMailMessage.MessageClass/InternetMessageId/TransportMessageHeaders/BodyHtml/BodyRtf`, `OutlookMailAttachment.OpenMessage`. `Bodu.Formats.Outlook` (shared model) has no apidoc overview. The `.msg` guide documents 2 of 6 reader options. `Bodu.IO.Pst` has no guide for `PstFileOptions.MaxNodeDataLength/MaxDataTreeLeaves`, the `PstFileError` catalogue, typed `PstPropertyValue` accessors, or the `TryGetValueLength`/`TryOpenValueStream` streaming pair.

### 5.2 "Which exchange-rate provider, and how do I configure it?" — Financial companions
No `appsettings.json` shape for any `Financial:<Source>` section; no page on how one `Financial:RateCache` section binds three option types; `StubHttpMessageHandler`/`IPairRateSource<TSeries>` offline-testing recipe promised but never shown (and the package holding it is not published); no "write your own web provider" page (only the `AddAcmeRates` doc-comment example in source); provider failure modes (`ExchangeRateFormatException`, `RateSeriesNotFoundException`, resilience give-up) unlisted; `RateRangeResult` members only in the package matrix; payload-cache location/disable unnamed; `MoneyBag.ConvertToWithAudit` → `MoneyBagConversionAudit` never shown; `Bodu.Financial.Extensions` money helpers (`Abs`, `Clamp`, `Min`, `Max`, …) and `StochasticRoundingStrategy` never mentioned; financial JSON spread across five pages with no page of its own.

### 5.3 "Show me a working Recurrence program" — Globalization
`Bodu.Globalization.Recurrence` has a design essay, a samples page, and no getting-started. `RecurrenceRule` typed parts, `RecurrenceRuleBuilder`, `RecurrenceSet` property-block format, `CronFormat`/`@` macros, `AnchoredInterval` ctor, `WeekDayNum` never shown. Calendar companions: Caching options (`ThrowOnStorageFailure`, `ValidateStorageOnStart`, SQLite WAL/busy timeout, `KeyPrefix`, `Calendar:NotableDateCache` section, on-disk schema, implementing `INotableDateCache`), the `bodu-calendar` CLI (`--resolver-dir`, exit codes), `CompileNotableDatePack` task properties, `ResolveAsync`, `NotableDateNameLocalizer.Register`, `CommonNotableDateCatalog` enum + `CommonNotableDateResources.Load(catalog)`, `NotableDateDurationDefinition`, adjustment-context members.

### 5.4 "What string / date helpers does Core give me?" — Core
`Bodu.Extensions` is 275 files. `StringExtensions` (77 methods: substring, wrapping, whitespace, ordinal predicates, affixes, filtering, casing with `WordCasingOptions`/`TitleCaseOptions`/`SentenceCaseOptions`, `ToSlug`/`SlugOptions`, `ToIdentifier`/`IdentifierCase`, safe file names) has **no page**. `DateTimeExtensions`/`DateOnlyExtensions` (66 + 56 files) have one getting-started snippet and no overload matrix (culture / `WorkingDaysOfWeek` / `CalendarQuarterDefinition` / provider). `IQuarterDefinitionProvider`/`FiscalWeekQuarterProvider`/`IWeekendDefinitionProvider` never shown in use. `IEnumerableExtensions` operator catalogue (laziness, `BatchPooled`, `RecursiveSelectControl` semantics), `ShuffleHelpers` vs `Randomize`, `SequenceGenerator` (which members are infinite), `Enums.GetValues`, `StreamExtensions`, `Tree<T>`, `RingBackedCollection<T>` extension point — apidoc one-liners or nothing. `NaturalStringComparer` guide has no card on any index page.

### 5.5 "Which serializer, and what is shared?" — Text
`Bodu.Text.Serialization` — the package every attribute, naming policy and callback lives in — has no page, no TOC entry, no apidoc overview. No STJ→Bodu migration table. No trimming/AOT page (`YamlSerializer` carries `[RequiresUnreferencedCode]`; the only reflection-free path is the Delimited/INI source generator, which has no page, no csproj wiring example, no `BTFG001–003` table, no `IDelimitedRecordFactory<T>`/`IIniSectionFactory<T>` contract). YAML has callbacks, converter factories, `[ExtensionData]`, `[Constructor]`, `[Required]` in code but no `callbacks.md`/`polymorphic-converters.md`, and its concepts page implies features are missing. Writer/DOM option types (`*WriterOptions`, `*NodeOptions`, `*DocumentOptions`, `AllowUnsortedKeys`, `AllowMultipleRootValues`) and `QuotedPrintableEncodingOptions` unnamed. The two identically named `IniDocument` types (`Bodu.Text.Configuration` vs `Bodu.Text.Ini.Document`) are never juxtaposed. `BencodeConfigurationProvider` missing from the Extensions.Configuration.Text index tables.

### 5.6 "Which primitive, and how does it interoperate with the BCL?" — Cryptography / Hashing
No hash-vs-hash or AEAD selection table; no BCL interop page (`IncrementalHash`: 0 mentions; `AesGcm`/`ChaCha20Poly1305` wire compatibility; PEM); no streaming/async page for either package (`HashingStream`: 0 guide mentions; `SymmetricAlgorithmExtensions.EncryptAsync`; `ParallelMerkleTreeHash.ComputeHashAsync`); value types `HashValue`/`AuthenticationTag`/`Nonce`/`Salt`/`SecretBytes`/`SignatureValue` and detached AEAD (`EncryptDetached`) never explained; Serpent-256/512/1024 (11 files) have no guide; Poly1305-AEAD constructions buried under "stream ciphers"; `Code39Mod43`/`Crockford32`/`Gumm` apidoc-only; `CheckValueAlgorithm`, `ExtendedSymmetricAlgorithm`, `RawKeyAsymmetricAlgorithm`, `IStreamCipher`, `TransformMode` zero mentions; `DisableSimd` switch only in an orphaned guide; no `CipherModeKind` support matrix (root cause of the CTS defect); no security-posture page (audit status, constant-time claims, zeroization, exception contract); trimming/AOT: 0 mentions.

### 5.7 "How do I edit a CFB file in place / author custom property sets?" — IO.Compound
In-place editing (`Open(…, FileMode.Open, FileAccess.ReadWrite)` → `Delete`/`Rename`/`CreateStream(name, bytes)`/writable `OpenStream` → `Commit`/`Revert`) is the headline capability in CLAUDE.md and the home page but is never demonstrated. `OlePropertySection`/`OlePropertyType`/`OlePropertyValue.Create*` authoring promised (`authoring-compound-files.md:141`) and never shown. `CompoundEntryBuilder` metadata, `CompoundStreamBuilder.Create*`, `CompoundStorageBuilder`'s `IDictionary` surface, `Load(Stream)` absent from the guide. `CompoundFileError` list incomplete.

### 5.8 Cross-cutting
- Package **status** (Stable / Preview / Experimental) is stated only in `package-matrix.md` and contradicts itself and the guides (§4.1).
- **Thread-safety** and **instance-reuse** contracts are scattered per guide; no summary for Core, Collections, or Cryptography.
- **Trimming/AOT** is not mentioned anywhere on the site.
- Every guide footer promises "every guide in this topic" while the topic cards cover a fraction (e.g. 11 of ~37 crypto guides; 6 of 11 hashing; 5 of 10 financial omitted).

---

## 6. Remediation plan

Phases are ordered by leverage. Effort is a rough size for one author (S ≈ half a day, M ≈ 1–2 days, L ≈ 3–5 days). Each phase is independently shippable; each new page follows the existing house pattern (front-matter `title`, `xref:` links for API, hero banner where the page is a package landing, `DocumentationSnippetCompileTests` for every code block — see Phase 5).

### Phase 0 — Guard rails and hygiene (S–M)

Stop the site from regressing before adding to it.

| # | Change | Where |
|---|---|---|
| 0.1 | Exclude `reviews/**` and `forensic-review/**` from `build.content`; decide `articles/` (add to root nav + fix `articles/index.md`, or move out). Replace the single inbound bencode link with a "Standards conformance" paragraph. | `docs/docfx.json`, `docs/toc.yml`, `docs/docs/serialization/bencode/index.md:72` |
| 0.2 | Add the two orphaned crypto guides to `guides/toc.yml`; add a CI step that fails when a `docs/**/*.md` page (outside `apidoc/`, `templates/`) is referenced by no TOC. | `docs/guides/toc.yml`, workflow |
| 0.3 | Add a CI step that fails when a generated `_site/api/<Namespace>.html` page has no `apidoc/<Namespace>.md` overwrite (allow-list for namespaces that are internal-only). Seed by creating the 35 missing overviews in Phase 2. | workflow |
| 0.4 | Add a CI step that extracts backticked PascalCase identifiers and `new X(` / `X.Y(` calls from the conceptual pages and fails on tokens absent from `src/` (curated allow-list for BCL names and illustrative placeholders such as `MoneyConverter`, `LoadKeyFromVault`). The audit script from §1 step 4 is the starting point. | `bld/check-docs-identifiers.py` (new), workflow |
| 0.5 | Fix the catalogue generator (`Bodu.Globalization.Calendar/Generate-NotableDateCatalogue.ps1`): the `$(@{…}.ResourceId)` interpolation, sections for the 8 missing catalogues and 3 region hubs, Source-cell links; regenerate; add a CI check that the catalogue stamp is newer than the newest `Resources/*.xml`. | generator + `guides/calendar/catalogue/**` |
| 0.6 | Reconcile package status: one source of truth (`package-matrix.md`), and a CI grep that every "Stable/Preview/Experimental" claim in `docs/` matches it. | `package-matrix.md`, provider guide, topic pages |
| 0.7 | Add `docs/samples/io-pst.md`; add `IO.Pst` to `samples/toc.yml` and `samples/index.md`; add the two missing calendar samples to `samples/calendar.md`. | `docs/samples/**` |

### Phase 1 — Accuracy fixes (M)

Every item in §4, in this order: **C** (will not compile) → **R** (throws) → **P** (prose). Concretely, by file cluster:

1. **Numerics guides** — `fraction.md:322-371`, `bigdecimal.md:153,194`: rewrite the generic-math section to show the `TSelf : INumber<TSelf>` pattern (as `generic-math-constraints.md` already does) and remove the direct static calls.
2. **Crypto guides** — `aead-modes.md` (12-byte nonces), `cipher-modes.md` (drop CTS via `BlockMode`; add the support matrix), `padding.md` (bits), `hkdf.md:54`, `getting-started.md:149-163` (Ascon XOF, Merkle), `aes-family.md:25,81-89` (Camellia wrapper), the six `…<T>` base-class names, `index.md:133` / `hashing.md` coverage sentences, `hardware-acceleration.md:274`.
3. **Hashing guides/apidoc** — resumable list, CheckDigits namespace, `CheckValueAlgorithm`, CRC catalogue (`CRC64_JONES` → remove, `CRC8_BLUETOOTH` → add, 112), Pearson ctor + nested enum, `Luhn.Instance`, `CrcLookupTableBuilder` namespace, `CrcStandard` description, Luhn example digits.
4. **`apidoc/` overviews for Core** — `Bodu.md`, `Bodu.Buffers.md`, `Bodu.Extensions.md`, `Bodu.Collections.Extensions.md`, `Bodu.Collections.Generic.md` (refresh the type list to all 48 types and link all guides), `Bodu.Collections.Generic.Concurrent.md`, `Bodu.Collections.Generic.Graphs.md`, `Bodu.Globalization.Extensions.md` and `Bodu.Text.md` (rewrite to describe their namespaces), `Bodu.Numerics.md`, `Bodu.Numerics.Serialization.Json.md`.
5. **Serialization** — YAML stream/async (6 pages), "self-contained" (8 pages), `[…Converter]`/`I…On…` naming (≈16 pages: global replace with `ConverterAttribute` / `IOnSerializing` family + `using Bodu.Text.Serialization;`), YAML converter/writer/`WriteTo` signatures, Configuration enum/preset lists and `TextConfigurationLoader` xrefs, the topic-page line-format API description, Toml-in-Formats, the Pearson leftover.
6. **Financial** — `IsoCode` ×4, `FinancialOptions`, `AddFinancialService` JSON claim ×5, "five converters" ×4, `RateCacheBase` "extend it", `RateCacheContractTests` claims ×3, provider list, 184.
7. **Globalization** — `NotableDateService(resource, registry)` ×3, `IsHoliday` (replace with `date.IsNotableDate(service, territory)`), `context.BaseDate`, log-level default, algorithm-key severity ×2, DI "no options object", `Data.*` naming (11 sites + `guides/toc.yml:272`), "six strategies" (10 sites), Recurrence dependency claim, `apidoc/…DependencyInjection.md` H1.
8. **Binary / landing** — "future .pst" ×2, IO.Pst deps ×3, `OpenStream().Open()`, `<see langword>` leak, stray fence, `introduction.md:79` dependency paragraph, `getting-started.md:13,21`, `licensing.md:14,39-46`, `ConcurrentEvictingDictionary` ×8, `package-matrix.md` rows, `guides/index.md` Core/Collections heading, `articles/index.md`.

### Phase 2 — Package landing pages and namespace overviews (L)

Give every package the same entry point the 26 primary packages have. Thin trios are acceptable where the guides already exist; the point is discoverability, install command, status, dependency list, and a "which package do I need" table.

| Package family | New pages | TOC |
|---|---|---|
| **Outlook** | `docs/docs/outlook/{index,concepts,getting-started}.md` (shared MAPI model; `.msg` vs `.pst`; property-tag anatomy; named properties; code pages; compressed RTF; validation levels and resource limits); `apidoc › Bodu.Formats.Outlook.md` | `docs/toc.yml` → Binary Formats & I/O |
| **Recurrence** | `docs/docs/recurrence/{index,concepts,getting-started}.md` (four schedule forms, purity/offset model, parse → next/previous → enumerate for each) | `docs/toc.yml` → Globalization; card on `index.md` |
| **Text.Serialization** | `docs/docs/serialization/core/{index,concepts,getting-started}.md` (attribute family, `NamingPolicy`/`KnownNamingPolicy`, ignore/creation/unmapped enums, callbacks, "reference this alone from a model library"); `apidoc › Bodu.Text.Serialization.md` | `docs/toc.yml` → Text & Serialization, sibling of the serializer node |
| **Text.Formats.Generators** | `docs/docs/formats/generators.md` + `guides/formats/source-generator.md` (csproj `OutputItemType="Analyzer"`, `[DelimitedRecord]`/`[IniSection]` rules, emitted `DelimitedFactory`/`IniFactory`, `IDelimitedRecordFactory<T>`/`IIniSectionFactory<T>` contracts with a hand-written implementation, `BTFG001–003` table, trimming/AOT); `package-matrix.md` row | under Bodu.Text.Formats |
| **Delimited / DotEnv / Ini** | per-package `docs/docs/formats/{delimited,dotenv,ini}/index.md` landing stubs (or "Static documentation" sections in their apidoc files) | nested under the umbrella node |
| **Financial.ExchangeRates** | `docs/docs/exchange-rates/{index,concepts,getting-started}.md` (warm-then-lookup, `WebRateProvider` vs `PairWebRateProvider<TSeries>`, `HistoryAvailability`, payload cache vs rate cache, `RateRangeResult`, failure modes); `apidoc › Bodu.Financial.Extensions.md`, `apidoc › Bodu.Financial.ExchangeRates.Testing.md` (or make Testing packable) | `docs/toc.yml` → Numerics & Financial |
| **Financial / Numerics JSON companions** | `docs/docs/{financial,numerics}-serialization-json/{index,concepts,getting-started}.md`; `guides/financial/json-serialization.md` consolidating the five fragments | `docs/toc.yml` |
| **Calendar companions** | `docs/docs/calendar-caching/{…}` + `apidoc › Bodu.Globalization.Calendar.Caching.md`; `docs/docs/calendar-di/{…}` (full overload table incl. keyed and options-monitor) and rewrite `apidoc/…DependencyInjection.md` under the class uid; thin trios for Builder, Plugins, Data packs; `apidoc › Bodu.Globalization.Calendar.Tool.md` | `docs/toc.yml` → Globalization; add package nodes in `guides/toc.yml` (Builder, Plugins, DI, Caching, Tool/Build) and retitle the data-pack node |
| **IO.Compound sub-namespaces** | `apidoc › Bodu.IO.Compound.Builders.md`, `apidoc › Bodu.IO.Compound.PropertySets.md` | — |
| **Serializer sub-namespaces** | 24 templated one-paragraph `apidoc › Bodu.Text.<Format>.{Reader,Writer,Nodes,Document,Serialization}.md` files linking to the format's guide sections | — |

### Phase 3 — Guides that close the consumer-readiness gaps (L, can be parallelised by domain)

Priority order within each domain; P1 items first across domains.

**Binary formats**
- P1 `guides/outlook/reading-pst-mail-stores.md` — `OutlookMailStore` session, folder walk, message conveniences and bodies, attachments and embedded messages, store-wide named properties, all reader options and limits, `OutlookPstFormatException`.
- P1 `guides/outlook/reader-options-and-limits.md` — validation levels and every `Max*` knob for both readers, and what surfaces when each trips.
- P1 `guides/io-pst/{index,reading-nodes-and-tables,streaming-and-validation}.md` — NIDs, contexts, typed accessors, length/stream pairs, `PstFileOptions`, full `PstFileError` catalogue.
- P2 `guides/io-compound/editing-in-place.md`; `guides/io-compound/custom-property-sets.md`.

**Financial**
- P1 `guides/financial/provider-configuration.md` — every `Financial:<Source>` section key list, API-key sourcing, resilience, `AllowSynchronousNetworkAccess`, payload-cache location/disable.
- P1 `guides/financial/caching-configuration.md` (or rewrite the caching section) — the `Financial:RateCache` JSON shape for all three option types plus warmup.
- P1 rewrite `guides/financial/testing-providers.md` — `StubHttpMessageHandler`, file-backed `IPairRateSource`, `FixedDatedRateProvider` in DI; resolve the Testing package's publish status.
- P2 `guides/financial/custom-web-provider.md`; new sections in `money.md` (extension helpers, `ConvertToWithAudit`, `StochasticRoundingStrategy`) and `exchange-rate-providers.md` (failure modes, `RateRangeResult`, `*SeriesInfo` discovery).

**Globalization**
- P1 `guides/recurrence/{rrule,cron,anchored-intervals,recurrence-sets,scheduling-host}.md` — per-form reference with a corrected calendar-composition recipe.
- P1 `guides/calendar/caching/{backends-and-options,custom-backend}.md`.
- P2 `guides/calendar/tooling/{bodu-calendar-cli,msbuild-integration}.md`; `guides/calendar/async-and-localization.md` (`ResolveAsync`, `NotableDateNameLocalizer`, typed catalogues); add the eight missing guide cards to `guides/calendar/index.md`.

**Core / Collections / Numerics**
- P1 `guides/core/string-extensions.md`; `guides/core/date-extensions.md` (with the overload matrix); `guides/core/calendar-shapes-and-providers.md` (quarter/weekend providers, `FiscalWeekPattern`).
- P1 `guides/numerics/complex.md` + rows in the numerics index/concepts/apidoc/JSON pages.
- P2 `guides/core/sequence-operators.md` (`IEnumerableExtensions`, `ShuffleHelpers`/`Randomize`, `SequenceGenerator`); `guides/core/tree.md`; `guides/core/ring-backed-collections.md`; `guides/core/numeric-enum-stream-extensions.md`; `guides/core/thread-safety.md`.
- P2 move `guides/text-encoding/encoding-helpers.md` under Core Foundations (or add a Core TOC node) and add the promised `Bodu.Text` sample to `docs/getting-started.md`.

**Text / serialization / configuration**
- P1 `guides/serialization/migrating-from-system-text-json.md`.
- P1 `guides/serialization/yaml/{callbacks,polymorphic-converters}.md`.
- P2 `guides/serialization/options-and-lifetime.md` (freeze, thread-safety, converter order — replaces six copies); `guides/formats/writer-options-and-dom-options.md`; `QuotedPrintableEncodingOptions` section; Bencode provider added to the Extensions.Configuration.Text index cards; an "`IniDocument` vs `IniDocument`" call-out with fully qualified names.

**Hashing / cryptography**
- P1 `guides/cryptography/choosing-a-primitive.md`; `guides/cryptography/bcl-interop.md`; `guides/cryptography/cipher-composition-reference.md` (the `CipherModeKind` support matrix).
- P1 `guides/cryptography/serpent.md`; `guides/cryptography/stream-aead.md` (move the Poly1305-AEAD material under the AEAD group).
- P2 `guides/cryptography/{streaming-and-async,value-types,security-posture,extending}.md`; `guides/io-hashing/{streaming-and-async,alphanumeric-check-digits}.md`; update `apidoc › Bodu.Security.Cryptography.md` with asymmetric/HPKE/KDF/OTP/value-type sections.

### Phase 4 — Navigation and landing-page parity (M)

1. **Home (`index.md`)**: cards for `Bodu.IO.Pst`, the Outlook family, `Bodu.Globalization.Recurrence`; a "Companion packages" strip pointing at the matrix; fix the Collections.Concurrent card; replace the partial install block with a link to `package-matrix.md#install-commands`.
2. **`docs/introduction.md`, `docs/getting-started.md`**: add the missing packages (IO.Pst, Outlook, Recurrence, YAML, IO.Compound, Excel, Delimited/DotEnv/Ini, all companions) to the tables, cards, install blocks and "where next" lists; add Excel/IO.Pst/Outlook subsections; rewrite the dependency paragraph from the csproj graph.
3. **Topic hubs**: keep the two-page structure (Overview in `docs/topics`, Guides landing in `guides/topics`) but make each guides hub's package list a mechanical mirror of the docs hub's package table, and make every hub list every guide in its topic (the footers promise this). Add IO.Pst/Outlook, Caching/Tool/Build/Recurrence, ExchangeRates/Caching/Serialization.Json, Delimited/DotEnv/Ini rows where absent.
4. **`guides/core/` folder**: either split into `guides/collections/` and `guides/collections-concurrent/` (mirrors `docs/`), or rename to `guides/core-foundations/`; in both cases fix the ten "Bodu.Core overview" footers on collection guides. Add `Bodu.Sequences`, `Bodu.Collections.(Generic.)Extensions`, `Bodu.Text`, `Bodu.Xml.Linq` nodes to the Core Foundations guide TOC.
5. **`guides/toc.yml`**: restructure the Financial node into per-package sub-nodes (as the Calendar node already is); regroup crypto (aes-family placement, Poly1305-AEAD under AEAD, one-time passwords group); add the calendar companion nodes.
6. **`package-matrix.md`**: move the ExchangeRates rows beside Financial; add `Bodu.Globalization.Calendar.Tool` (`dotnet tool install`) and `.Build` to the install block; add a `Bodu.Text.Formats.Generators` row; note the Testing package's status.

### Phase 5 — Sustainment (M, ongoing)

1. **Compile every snippet.** Ten projects have a `DocumentationSnippetCompileTests.cs`; the defects in §4 cluster in domains without one (Numerics, Cryptography, Financial core, Calendar, Core/Collections, Yaml, Delimited/DotEnv/Ini, Outlook, IO.Pst). Add one per test project and make every `guides/**` and `docs/**` C# block a guarded snippet. This is the single highest-value guard: 30+ of the findings above would have been build failures.
2. **Namespace-overview and orphan checks** (Phase 0.2/0.3) stay in CI.
3. **Identifier lint** (Phase 0.4) stays in CI with a maintained allow-list.
4. **Catalogue regeneration** runs whenever `Resources/*.xml` changes (Phase 0.5).
5. **Definition of done for a new package**: trio + apidoc overview + at least one guide + a samples page entry + a `package-matrix.md` row + hero banner + icon. Extend the existing "Validate package inventory" step to check the first four, not only the matrix row.

---

## 7. Suggested sequencing

| Sprint | Content |
|---|---|
| 1 | Phase 0 (all), Phase 1 clusters 1–3 (numerics, crypto, hashing — the throwing/non-compiling samples) |
| 2 | Phase 1 clusters 4–8; Phase 5.1 for the four worst domains (Numerics, Cryptography, Calendar, Financial) |
| 3 | Phase 2: Outlook, Recurrence, Text.Serialization, ExchangeRates trios + the 35 apidoc overviews |
| 4 | Phase 3 P1 guides (Outlook/PST, provider configuration, Recurrence, string/date extensions, STJ migration, YAML parity, crypto selection/interop/composition) |
| 5 | Phase 2 remainder (calendar companions, JSON companions, generators, format stubs); Phase 4 |
| 6 | Phase 3 P2 guides; Phase 5.1 for the remaining projects; Phase 5.5 definition-of-done check |

---

## Appendix A — Public types with zero conceptual mentions (excluding currency tags, enumerators, and `Bodu.CodeStyle`)

`Bodu.Core`: `DateTimeResolution`, `EitherAsyncExtensions`, `EnumExtensions`, `FiscalWeekQuarterProvider`, `IQuarterDefinitionProvider`, `IWeekendDefinitionProviderExtensions`, `IdentifierCase`, `RecursiveSelectControl` (mis-described), `SentenceCaseOptions`, `ShuffleHelpers`, `SlugOptions`, `StreamExtensions`, `StringExtensions`, `TitleCaseOptions`, `WordCasingOptions`.
`Bodu.Numerics(.Serialization.Json)`: `Complex`, `ComplexJsonConverter`, `ComplexJsonConverterFactory`, `DiscreteIntervalJsonConverterFactory`, `IntervalSetJsonConverterFactory`.
`Bodu.IO.Hashing`: `Adler32Base`, `Adler64Base`, `CheckValueAlgorithm`, `Code39Mod43`, `Crockford32`, `Gumm`, `HashingStream` (guides).
`Bodu.Security.Cryptography`: `AeadTransformExtensions`, `AsconHash`, `AuthenticationTag`, `ExtendedSymmetricAlgorithm`, `HashValue`, `IStreamAeadTransform`, `IStreamCipher`, `RawKeyAsymmetricAlgorithm`, `Salt`, `SecretBytes`, `SerpentBlockCipher`, `SerpentBlockCipherBase`, `SymmetricStreamAlgorithmExtensions`, `TransformMode`.
`Bodu.IO.Compound` (guides): `CompoundEntryBuilder`, `OlePropertySection`, `OlePropertyType`.
`Bodu.Formats.Outlook(.Pst)`: `OutlookRecipientType`, `OutlookMailStoreReaderOptions`, `OutlookPstFormatException`.
`Bodu.Text.*`: `BencodeDocumentOptions`, `BencodeNodeOptions`, `BencodeWriterOptions`, `TomlDocumentOptions`, `TomlNodeOptions`, `TomlReaderOptions`, `DelimitedProperty`, `DelimitedRecordAttribute`, `DelimitedSerializerDefaults`, `DelimitedValueKind`, `DotEnvValueKind`, `DotEnvWriterOptions`, `IniSectionAttribute`, `IniValueKind`, `IniWriterOptions`, `IniComment`, `QuotedPrintableEncodingOptions`, `FormatFactoryGenerator`, `BencodeConfigurationProvider` (index tables).
`Bodu.Globalization.*`: `CommonNotableDateCatalog`, `FixedDurationDefinition`, `NotableDateDurationDefinition`, `NotableDateNameLocalizer`, `NotableDateServiceAsyncExtensions`, `RuleApplicability`, `RecurrenceFrequency`, `CalendarTool`, and eleven `Bodu.Globalization.Calendar.Caching` types (`NotableDateCacheOptions`, `NotableDateCacheWarmupOptions`, `NotableDateCachingExtensions`, `NotableDateCacheWarmupExtensions`, `NotableDateCacheBase`, `FileNotableDateCacheBase`, `NotableDateCacheEntry`, `NotableDateCacheFile`, `NotableDateCacheYearRow`, `NotableDateCacheOccurrenceRow`, `NotableDateCacheWriteStatus`), `SqliteNotableDateCacheExtensions/Options`, `DistributedNotableDateCacheExtensions/Options`.
`Bodu.Financial.*`: `CurrencyCodeExtensions`, `CurrencyPairRequest`, `CurrencyStatusAttribute`, `MoneyBagConversionAudit`, `MoneyExtensions`, `MoneyOfTCurrencyExchangeRateExtensions`, `MoneyOfTCurrencyExtensions`, `RateLookupResultExtensions`, `RateSeriesNotFoundException`, `StochasticRoundingStrategy`, `FinancialServiceBuilderExtensions`, `ServiceProviderExtensions`, `ExchangeRateFormatException`, `IByteCache`, `NullByteCache`, `PairRateData`, `WebRateProviderExtensions`, `CachingRateProviderBase`, `IAggregatedRateBuilder`, `RateCacheCoverageEntry`, `RateCacheDirectoryContext`, `RateCacheEntry`, `RateCacheFile`, `RateCacheFileContext`, `RateCacheOptions`, `RateCacheWarmupExtensions`, `RateCacheWarmupOptions`, `RateCacheWriteStatus`, `RateCachingExtensions`, `DistributedRateCacheExtensions`, `SqliteRateCacheExtensions`, `StubHttpMessageHandler`, `CalculatedMoneyJsonConverter` and the six other financial JSON converters, and for every provider its `*ServiceCollectionExtensions`, `*FinancialServiceBuilderExtensions`, `*SeriesInfo`, `*EndpointOptions`, `FileSystem*Cache` types.
