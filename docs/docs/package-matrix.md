---
title: Bodu package matrix
---

# Bodu package matrix

<style>
  .bodu-matrix-gallery { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: .8rem; margin: 1rem 0 1.5rem; }
  .bodu-matrix-gallery figure { margin: 0; }
  .bodu-matrix-gallery img { display: block; width: 100%; height: auto; aspect-ratio: 480 / 220; border-radius: 6px; }
  .bodu-matrix-gallery figcaption { margin-top: .3rem; font-size: .8rem; opacity: .85; overflow-wrap: anywhere; }
</style>

The Bodu suite ships as a family of focused NuGet packages, each with a clear responsibility. This page is the authoritative list - what every package is for, where it lives in the dependency graph, how mature its public surface is today, and the version it ships at.

The **Version** column gives each package's version in the 1.3.0 release: the Stable packages ship together at 1.3.0, and the Preview and Experimental packages at 0.7.2. [Versioning, stability, and support](versioning-and-support.md) explains the two version streams, what each status commits to, and which releases receive fixes.

For the high-level shape of each library, follow the **Intro** link in the table; for runnable samples, the **Get started** link.

## At a glance

| Category | Package | Status | Version | Depends on | Intro | Get started |
|---|---|---|---|---|---|---|
| **Foundation** | <xref:Bodu> · `Bodu.Core` | Stable | 1.3.0 | (BCL only) | [Bodu.Core](core/index.md) | [Get started](core/getting-started.md) |
| **Collections** | `Bodu.Collections` | Stable | 1.3.0 | `Bodu.Core` | [Bodu.Collections](collections/index.md) | [Get started](collections/getting-started.md) |
| **Concurrent collections** | `Bodu.Collections.Concurrent` | Stable | 1.3.0 | `Bodu.Collections` | [Bodu.Collections.Concurrent](collections-concurrent/index.md) | [Get started](collections-concurrent/getting-started.md) |
| **Hashing** | `Bodu.IO.Hashing` | Stable | 1.3.0 | `Bodu.Core`, `System.IO.Hashing` | [Bodu.IO.Hashing](io-hashing/index.md) | [Get started](io-hashing/getting-started.md) |
| **Cryptography** | `Bodu.Security.Cryptography` | Stable | 1.3.0 | `Bodu.Core`, `System.Security.Cryptography` | [Bodu.Security.Cryptography](cryptography/index.md) | [Get started](cryptography/getting-started.md) |
| **Calendar runtime** | `Bodu.Globalization.Calendar` | Stable | 1.3.0 | `Bodu.Core`, `Microsoft.Extensions.Logging.Abstractions` | [Bodu.Globalization.Calendar](calendar/index.md) | [Get started](calendar/getting-started.md) |
| **Text encoding** | `Bodu.Text.Encoding` | Stable | 1.3.0 | `Bodu.Core` | [Bodu.Text.Encoding](text-encoding/index.md) | [Get started](text-encoding/getting-started.md) |
| **Text filtering** | `Bodu.Text.Filtering` | Stable | 1.3.0 | `Bodu.Core` | [Bodu.Text.Filtering](text-filtering/index.md) | [Get started](text-filtering/getting-started.md) |
| **Text formats (umbrella)** | `Bodu.Text.Formats` | Stable | 1.3.0 | `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, `Bodu.Text.Ini` | [Bodu.Text.Formats](formats/index.md) | [Get started](formats/getting-started.md) |
| **Delimited (CSV / TSV)** | `Bodu.Text.Delimited` | Stable | 1.3.0 | `Bodu.Text.Serialization`, `Bodu.Core` | [Bodu.Text.Formats](formats/index.md) | [Get started](formats/getting-started.md) |
| **DotEnv** | `Bodu.Text.DotEnv` | Stable | 1.3.0 | `Bodu.Text.Serialization`, `Bodu.Core` | [Bodu.Text.Formats](formats/index.md) | [Get started](formats/getting-started.md) |
| **INI** | `Bodu.Text.Ini` | Stable | 1.3.0 | `Bodu.Text.Serialization`, `Bodu.Core` | [Bodu.Text.Formats](formats/index.md) | [Get started](formats/getting-started.md) |
| **Text formats source generator** | `Bodu.Text.Formats.Generators` | Not packaged | - | `Microsoft.CodeAnalysis.CSharp` (build-time only; not yet packable; project reference only) | [Bodu.Text.Formats.Generators](formats/generators.md) | [Get started](../guides/formats/source-generator.md) |
| **TOML serializer** | `Bodu.Text.Toml` | Stable | 1.3.0 | `Bodu.Text.Serialization`, `Bodu.Core` | [Bodu.Text.Toml](serialization/toml/index.md) | [Get started](serialization/toml/getting-started.md) |
| **Bencode serializer** | `Bodu.Text.Bencode` | Stable | 1.3.0 | `Bodu.Text.Serialization`, `Bodu.Core` | [Bodu.Text.Bencode](serialization/bencode/index.md) | [Get started](serialization/bencode/getting-started.md) |
| **YAML serializer** | `Bodu.Text.Yaml` | Stable | 1.3.0 | `Bodu.Text.Serialization`, `Bodu.Core` | [Bodu.Text.Yaml](serialization/yaml/index.md) | [Get started](serialization/yaml/getting-started.md) |
| **Text configuration** | `Bodu.Text.Configuration` | Stable | 1.3.0 | `Bodu.Core` | [Bodu.Text.Configuration](text-configuration/index.md) | [Get started](text-configuration/getting-started.md) |
| **Configuration bridge** | `Bodu.Extensions.Configuration.Text` | Stable | 1.3.0 | `Bodu.Text.Configuration`, `Bodu.Text.Toml`, `Bodu.Text.Bencode`, `Bodu.Core`, `Microsoft.Extensions.Configuration` | [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md) | [Get started](extensions-configuration-text/getting-started.md) |
| **Numerics** | `Bodu.Numerics` | Stable | 1.3.0 | `Bodu.Core` | [Bodu.Numerics](numerics/index.md) | [Get started](numerics/getting-started.md) |
| **Financial** | `Bodu.Financial` | Stable | 1.3.0 | `Bodu.Numerics`, `Bodu.Core` | [Bodu.Financial](financial/index.md) | [Get started](financial/getting-started.md) |

## Companion packages

Several capabilities ship as separate companion packages, so a consumer installs only what it uses and the main library takes on none of their dependencies. They version with the rest of their stream (see [Versioning, stability, and support](versioning-and-support.md)). The calendar runtime in particular is intentionally small - fluent rule authoring, plugin loading, and dependency-injection registration are all opt-in.

| Package | Status | Version | Purpose | Depends on |
|---|---|---|---|---|
| `Bodu.Globalization.Calendar.Builder` | Stable | 1.3.0 | Fluent, chainable C# API for authoring notable-date documents in code, with XML / JSON serialization and load/save. | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.DependencyInjection` | Stable | 1.3.0 | `IServiceCollection` extensions for registering `INotableDateService` over a loaded `NotableDateResource`. | `Bodu.Globalization.Calendar`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options` |
| `Bodu.Globalization.Calendar.Plugins` | Stable | 1.3.0 | Trust-gated loading of external assemblies that contribute custom `INotableDateAlgorithm` implementations. | `Bodu.Globalization.Calendar`, `Microsoft.Extensions.Logging.Abstractions` |
| `Bodu.Globalization.Calendar.Tool` | Preview | Not on nuget.org | The `bodu-calendar` command-line tool (installed with `dotnet tool install` from a locally packed feed): validates notable-date XML/JSON documents with the stable `BODU-CAL-*` diagnostics (`lint`), compiles them to sealed `.bcal` binary packs (`compile`), and inspects compiled packs (`info`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.Build` | Preview | Not on nuget.org | MSBuild integration for notable-date rule packs: the `CompileNotableDatePack` task and `NotableDatePack` items compile XML/JSON documents to sealed `.bcal` packs during build, incrementally, via the bundled `bodu-calendar` tool. A development dependency - no runtime reference is added to the consuming project. | (build-time only; bundles the `bodu-calendar` tool) |
| `Bodu.Globalization.Calendar.Caching` | Stable | 1.3.0 | `CachingNotableDateService`, a decorator that wraps any `INotableDateService` and serves computed notable dates from a per-territory, per-civil-year cache (in-memory, or one TOML/JSON file per territory), refreshing on a configurable time-to-live or a resource-version change. Includes its own DI registration. | `Bodu.Globalization.Calendar`, `Bodu.Text.Toml`, `Bodu.Core` |
| `Bodu.Globalization.Calendar.Caching.Distributed` | Stable | 1.3.0 | Distributed (`IDistributedCache` / Redis) storage backend for the notable-date cache: `DistributedNotableDateCache` over any `IDistributedCache`, with the `AddDistributedNotableDateCache` / `AddRedisNotableDateCache` DI registrations. | `Bodu.Globalization.Calendar.Caching`, `Bodu.Globalization.Calendar`, `Bodu.Core`, `Microsoft.Extensions.Caching.StackExchangeRedis` |
| `Bodu.Globalization.Calendar.Caching.Sqlite` | Stable | 1.3.0 | SQLite storage backend for the notable-date cache: `SqliteNotableDateCache` persisting computed years in a SQLite database, with the `AddSqliteNotableDateCache` DI registration. | `Bodu.Globalization.Calendar.Caching`, `Bodu.Globalization.Calendar`, `Bodu.Core`, `Microsoft.Data.Sqlite` |
| `Bodu.Globalization.Recurrence` | Stable | 1.3.0 | RFC 5545 (iCalendar) recurrence rules - `RecurrenceRule` / `RecurrenceRuleBuilder` / `RecurrenceSet` parsing, formatting, and occurrence enumeration - plus `CronExpression` cron-schedule parsing and `AnchoredInterval` instant-anchored intervals, every form answering both next- and previous-occurrence queries. | `Bodu.Core` |
| `Bodu.Text.Serialization` | Stable | 1.3.0 | Shared, format-agnostic serialization primitives for the Bodu `System.Text.Json`-shaped text serializers (Bencode, TOML, YAML, Delimited, DotEnv, INI): the attribute family, ignore/creation/unmapped-member/naming enums, serialization callback interfaces, and naming policies. Consumed by all six per-format serializer packages. | `Bodu.Core` |
| `Bodu.Financial.DependencyInjection` | Stable | 1.3.0 | `IServiceCollection` extensions for registering Bodu.Financial currency-lookup and monetary services via `AddFinancialService`. | `Bodu.Financial`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `Bodu.Financial.Serialization.Json` | Stable | 1.3.0 | `System.Text.Json` converters, the `FinancialJsonPolicy` (`Strict` / `Lenient` / `Compact`), the `AddFinancialJsonConverters()` registration, and the `AddFinancialJson()` DI registration for the `Bodu.Financial` types (`Money`, `Money<TCurrency>`, `CalculatedMoney`, `MoneyBag`, `ExchangeRate`, `CurrencyPair`), keeping the core financial library serialization-agnostic. | `Bodu.Financial`, `System.Text.Json`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `Bodu.Numerics.Serialization.Json` | Stable | 1.3.0 | `System.Text.Json` converters, converter factories, and the `AddNumericsJsonConverters()` registration for the `Bodu.Numerics` types (`Fraction<T>`, `BigDecimal`, `Complex<T>`, `Interval<T>`, `DiscreteInterval<T>`, `IntervalSet<T>`), keeping the core numerics library serialization-agnostic. | `Bodu.Numerics`, `System.Text.Json` |

<div class="bodu-matrix-gallery">
<figure><img src="../images/hero-calendar-builder.svg" alt="Bodu.Globalization.Calendar.Builder" /><figcaption><code>Bodu.Globalization.Calendar.Builder</code></figcaption></figure>
<figure><img src="../images/hero-calendar-di.svg" alt="Bodu.Globalization.Calendar.DependencyInjection" /><figcaption><code>Bodu.Globalization.Calendar.DependencyInjection</code></figcaption></figure>
<figure><img src="../images/hero-calendar-plugins.svg" alt="Bodu.Globalization.Calendar.Plugins" /><figcaption><code>Bodu.Globalization.Calendar.Plugins</code></figcaption></figure>
<figure><img src="../images/hero-calendar-tool.svg" alt="Bodu.Globalization.Calendar.Tool" /><figcaption><code>Bodu.Globalization.Calendar.Tool</code></figcaption></figure>
<figure><img src="../images/hero-calendar-build.svg" alt="Bodu.Globalization.Calendar.Build" /><figcaption><code>Bodu.Globalization.Calendar.Build</code></figcaption></figure>
<figure><img src="../images/hero-calendar-caching.svg" alt="Bodu.Globalization.Calendar.Caching" /><figcaption><code>Bodu.Globalization.Calendar.Caching</code></figcaption></figure>
<figure><img src="../images/hero-calendar-caching-distributed.svg" alt="Bodu.Globalization.Calendar.Caching.Distributed" /><figcaption><code>Bodu.Globalization.Calendar.Caching.Distributed</code></figcaption></figure>
<figure><img src="../images/hero-calendar-caching-sqlite.svg" alt="Bodu.Globalization.Calendar.Caching.Sqlite" /><figcaption><code>Bodu.Globalization.Calendar.Caching.Sqlite</code></figcaption></figure>
<figure><img src="../images/hero-text-serialization.svg" alt="Bodu.Text.Serialization" /><figcaption><code>Bodu.Text.Serialization</code></figcaption></figure>
<figure><img src="../images/hero-financial-di.svg" alt="Bodu.Financial.DependencyInjection" /><figcaption><code>Bodu.Financial.DependencyInjection</code></figcaption></figure>
<figure><img src="../images/hero-financial-json.svg" alt="Bodu.Financial.Serialization.Json" /><figcaption><code>Bodu.Financial.Serialization.Json</code></figcaption></figure>
<figure><img src="../images/hero-numerics-json.svg" alt="Bodu.Numerics.Serialization.Json" /><figcaption><code>Bodu.Numerics.Serialization.Json</code></figcaption></figure>
</div>

## File formats

Legacy binary container and document formats, layered so each level is usable on its own: a general-purpose compound-file container, a record-stream codec, and the narrow read-only readers built on them. `Bodu.Formats.Excel.Binary` reads `.xls` workbooks over `Bodu.IO.Compound` and `Bodu.IO.Biff`; the Outlook readers share one MAPI value model, with the `.msg` reader over `Bodu.IO.Compound` and the `.pst` mail-store reader over `Bodu.IO.Pst`.

| Package | Status | Version | Purpose | Depends on |
|---|---|---|---|---|
| `Bodu.IO.Compound` | Stable | 1.3.0 | Reader and writer for the OLE2 / Compound File Binary (CFB) container - the structured-storage envelope used by legacy Office files. Opens existing containers (exposing the embedded named streams) and authors new ones via `CompoundFile.Create` and the builder API, with no application-format knowledge. | `Bodu.Core` |
| `Bodu.IO.Biff` | Stable | 1.3.0 | A low-level codec for the Excel Binary Interchange File Format (BIFF5 and BIFF8) record streams found inside legacy `.xls` workbooks - the substrate beneath `Bodu.Formats.Excel.Binary`, in the same relation `Bodu.IO.Pst` has to `Bodu.Formats.Outlook.Pst`. The forward-only, allocation-free `BiffReader` frames each record, establishes the version from BOF and the code page from CODEPAGE, and exposes typed accessors for cell, sheet, and workbook-globals records; `BiffSstReader` walks the BIFF8 shared string table across its CONTINUE records; `BiffWriter` emits BIFF5 or BIFF8 records. No compound-file dependency, no workbook or cell model, no formula evaluation. BIFF5 byte strings decode through the shared framework's `CodePagesEncodingProvider`, so no code-pages package is needed. | `Bodu.Core` |
| `Bodu.Formats.Excel.Binary` | Stable | 1.3.0 | Narrow, read-only BIFF5 and BIFF8 (`.xls`) reader that surfaces raw worksheet cell values - strings, numbers, booleans, and errors - without formula evaluation, styling, or higher-level interpretation. | `Bodu.IO.Compound`, `Bodu.IO.Biff`, `Bodu.Core` |
| `Bodu.IO.Pst` | Stable | 1.3.0 | Low-level, read-only container reader for the Outlook personal-folders format (PST / MS-PST). Reads the node database of Unicode and ANSI files - header, node and block B-trees, block data with the permute and cyclic encodings decoded and checksums verified, multi-block data trees, and per-node subnode trees - and the LTP layer over it: heap-on-node, BTree-on-heap, and per-node property-context and table-context views with wire-typed values. The substrate a message-level reader builds on. No MAPI semantics and no writing. | `Bodu.Collections`, `Bodu.Core` |
| `Bodu.Formats.Outlook` | Stable | 1.3.0 | The shared MAPI value model for the Outlook format readers: property tags and types, decoded property values with a tag-addressed collection, named-property identities, recipient and attachment enumerations, and the shared exception hierarchy. Container-free - the `.msg` reader (`Bodu.Formats.Outlook.Msg`) and the `.pst` mail-store reader (`Bodu.Formats.Outlook.Pst`, over `Bodu.IO.Pst`) build on it rather than owning the model. | `Bodu.Core` |
| `Bodu.Formats.Outlook.Msg` | Stable | 1.3.0 | Read-only reader for the Outlook message format (`.msg` / MS-OXMSG) over the `Bodu.IO.Compound` OLE2 container. Opens a message as a disposable session exposing every decoded MAPI property, the recipient and attachment tables, nested attached messages, named-property resolution, and the text, HTML, and compressed-RTF bodies - with no MAPI session emulation or message authoring. | `Bodu.IO.Compound`, `Bodu.Formats.Outlook`, `Bodu.Core` |
| `Bodu.Formats.Outlook.Pst` | Stable | 1.3.0 | Read-only reader for the Outlook personal-folders format (`.pst` / MS-PST, Unicode and ANSI formats) over the `Bodu.IO.Pst` node-database container. Opens a mail store as a disposable session exposing the store properties, the folder hierarchy, and every message with its decoded MAPI properties, recipients, attachments, nested embedded messages, store-wide named-property resolution, and the text, HTML, and compressed-RTF bodies - with no MAPI session emulation or store authoring. | `Bodu.IO.Pst`, `Bodu.Formats.Outlook`, `Bodu.Core` |

<div class="bodu-matrix-gallery">
<figure><img src="../images/hero-io-biff.svg" alt="Bodu.IO.Biff" /><figcaption><code>Bodu.IO.Biff</code></figcaption></figure>
<figure><img src="../images/hero-io-pst.svg" alt="Bodu.IO.Pst" /><figcaption><code>Bodu.IO.Pst</code></figcaption></figure>
<figure><img src="../images/hero-outlook.svg" alt="Bodu.Formats.Outlook" /><figcaption><code>Bodu.Formats.Outlook</code></figcaption></figure>
<figure><img src="../images/hero-outlook-msg.svg" alt="Bodu.Formats.Outlook.Msg" /><figcaption><code>Bodu.Formats.Outlook.Msg</code></figcaption></figure>
<figure><img src="../images/hero-outlook-pst.svg" alt="Bodu.Formats.Outlook.Pst" /><figcaption><code>Bodu.Formats.Outlook.Pst</code></figcaption></figure>
</div>

## Exchange-rate providers and caching

Several exchange-rate providers ship as independent packages over a shared `IDatedRateProvider` / `IRateProvider` contract, so consumers pull in only the data sources they need; each provider package ships its own `Add<Source>ExchangeRates` dependency-injection registration, delegating the shared `HttpClient` and resilience plumbing to `Bodu.Financial.ExchangeRates.DependencyInjection`. The contract exposes a symmetric single-date/range lookup matrix (synchronous and asynchronous): single-date getters return a `RateLookupResult` (the resolved rate plus the applied date-resolution and offset), and range getters return a `RateRangeResult` (the rates, the requested window, and the observed span; it is itself an `IReadOnlyList<ExchangeRate>`). The HTTP-backed providers share the `WebRateProvider` base from the `Bodu.Financial.ExchangeRates` infrastructure package and are `IDisposable` - each either builds and owns its `HttpClient` from options (via `RateProviderHttpClientFactory`) or borrows a caller-supplied one. The Reserve Bank of Australia provider reads the RBA's published `.xls` files through the binary-format packages above - a dependency on the container and codec layers, not on anything RBA-specific. A standalone caching layer can decorate any provider with a shared on-disk TOML cache.

The core money, currency, and FX value types live in `Bodu.Financial` (see **At a glance**); everything below is an opt-in companion.

| Package | Status | Version | Purpose | Depends on |
|---|---|---|---|---|
| `Bodu.Financial.ExchangeRates` | Stable | 1.3.0 | Web exchange-rate provider infrastructure: the abstract `WebRateProvider` and `PairWebRateProvider<TSeries>` bases every per-source provider package builds on, plus the shared fetch machinery - `WebRateProviderOptions`, single-flight request coalescing (`SingleFlightCoordinator<TKey>`), on-disk raw-response caching (`FileSystemByteCache<TKey>`), `HttpClient` construction (`RateProviderHttpClientFactory`), and the pair-load contracts (`IPairRateLoader`, `IPairRateSource<TSeries>`). Keeps the HTTP machinery out of the core `Bodu.Financial` package. | `Bodu.Financial`, `Bodu.Core`, `Microsoft.Extensions.Logging.Abstractions` |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | Stable | 1.3.0 | Shared dependency-injection machinery for the web-based exchange-rate providers. Exposes `AddWebRateProvider` on `IFinancialServiceBuilder` (declared in the `Bodu.Financial.ExchangeRates` namespace), handling `HttpClient` configuration with Polly resilience, options binding, and singleton registration so each provider package's own DI registration delegates its plumbing here. | `Bodu.Financial.ExchangeRates`, `Bodu.Financial.DependencyInjection`, `Bodu.Core`, `Microsoft.Extensions.Http.Resilience` |
| `Bodu.Financial.ExchangeRates.Rba` | Stable | 1.3.0 | Downloads and parses the RBA's published daily exchange-rate `.xls` files, serving them as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`, with an async range API and in-memory plus on-disk caching. Includes its own `AddRbaExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `RbaRateProviderOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Formats.Excel.Binary`, `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Boe` | Stable | 1.3.0 | Queries the Bank of England's Interactive Statistical Database (IADB) CSV endpoint for daily spot rates, serving them as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`, with an async range API and in-memory plus on-disk caching. Includes its own `AddBoeExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `BoeRateProviderOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Text.Delimited`, `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Ecb` | Stable | 1.3.0 | Downloads and parses the ECB's published `eurofxref` euro foreign-exchange reference-rate XML feeds, serving them as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`, with an async range API and in-memory plus on-disk caching. Includes its own `AddEcbExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `EcbRateProviderOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Yahoo` | Experimental | 0.7.2 | Fetches and parses the Yahoo Finance v8 chart JSON service, serving the results as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`, with an async range API over arbitrary currency pairs. Yahoo publishes no API for the service, and its terms of service do not permit automated collection without its permission, so check that your use is permitted and pair it with a stable provider. Includes its own `AddYahooExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `YahooRateProviderOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Ofx` | Experimental | 0.7.2 | Fetches and parses the OFX (ofx.com) public spot-rate-history JSON service, serving the results as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`, with an async range API over arbitrary currency pairs. OFX documents no API for the service, which backs its website and can change without notice; pair it with a stable provider. Includes its own `AddOfxExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `OfxRateProviderOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Xe` | Experimental | Not on nuget.org | Fetches and parses the XE.com charting-rates JSON service, serving the results as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`, with an async range API over arbitrary currency pairs; the authorization token the endpoint requires is recovered by scraping an unversioned public XE page, so a site change can silently reduce the provider to empty results - pair it with a stable primary feed. Includes its own `AddXeExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `XeRateProviderOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Oanda` | Experimental | Not on nuget.org | Fetches and parses the OANDA Historical Currency Converter rate-history JSON service, serving the results as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`, with an async range API over arbitrary currency pairs. The anonymous endpoint serves a rolling recent window (roughly the last 180 days), advertised through the provider's `HistoryAvailability`. OANDA documents no API for the service, which is gated by bot mitigation and can change without notice; pair it with a stable provider. Includes its own `AddOandaExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `OandaRateProviderOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Fixer` | Stable | 1.3.0 | Fetches and parses the Fixer (fixer.io) time-series and single-date JSON endpoints per currency pair, serving the results as `ExchangeRate` values through `IDatedRateProvider` / `IRateProvider`. Requires a Fixer `access_key` on `FixerRateProviderOptions`. Includes its own `AddFixerExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.ExchangeRateHost` | Stable | 1.3.0 | Fetches and parses the exchangerate.host time-frame and single-date JSON endpoints per currency pair (quotes keyed by concatenated source+quote code), serving the results through `IDatedRateProvider` / `IRateProvider`. Requires an `access_key` on `ExchangeRateHostRateProviderOptions`. Includes its own `AddExchangeRateHostExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Fred` | Stable | 1.3.0 | Fetches and parses the St. Louis Fed FRED `series/observations` JSON endpoint, mapping each currency pair to a FRED `series_id` through `FredRateProviderOptions.SeriesMap` (built-in map for the major USD pairs). Requires an `api_key`. Includes its own `AddFredExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Imf` | Experimental | 0.7.2 | Downloads and parses the IMF's monthly Representative Exchange Rates tab-separated report, serving daily USD-anchored rates through `IDatedRateProvider` / `IRateProvider`. A single-base bulk provider over `WebRateProvider` (base USD, cross pairs rejected) that normalizes the report's quotation direction on ingest; keyless, with a per-month on-disk cache. The IMF does not document the report's tab-separated export, which can change without notice; pair it with a stable provider. Includes its own `AddImfExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Text.Delimited`, `Bodu.Financial.ExchangeRates`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Caching` | Stable | 1.3.0 | `CachingRateProvider`, a one-cache-per-provider decorator that wraps any `IDatedRateProvider` and serves fresh rates from a pluggable cache cascade - `InMemoryRateCache` and the on-disk `TomlFileRateCache` (one TOML file per provider and currency pair) - delegating to the inner provider only on a miss; plus `AggregatingRateProvider`, which groups named child providers behind one entry point and combines them with a pluggable strategy (prioritised fallback, averaging, or per-FX-pair routing). Includes its own DI registration (`AddCachedRateProvider`, `AddAggregatedRateProvider`, in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Text.Toml`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Caching.Distributed` | Stable | 1.3.0 | `DistributedRateCache`, an `IRateCache` over a `Microsoft.Extensions.Caching.Distributed.IDistributedCache` (Redis-capable), persisting a provider's dated rates and fetch-coverage windows as a per-pair JSON blob; behaviourally identical to the in-memory, TOML, and SQLite caches (the repository's cache contract tests run unchanged against every backend). Includes its own `AddDistributedRateCache` / `AddRedisRateCache` DI registration (in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Core`, `Microsoft.Extensions.Caching.Abstractions` |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite` | Stable | 1.3.0 | `SqliteRateCache`, an `IRateCache` over a SQLite database (via `Microsoft.Data.Sqlite`), persisting a provider's dated rates and fetch-coverage windows in `rates` and `coverage` tables; behaviourally identical to the in-memory and TOML caches (the repository's cache contract tests run unchanged against every backend). Includes its own `AddSqliteRateCache` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `SqliteRateCacheOptions`. | `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Core`, `Microsoft.Data.Sqlite` |

<div class="bodu-matrix-gallery">
<figure><img src="../images/hero-fx.svg" alt="Bodu.Financial.ExchangeRates" /><figcaption><code>Bodu.Financial.ExchangeRates</code></figcaption></figure>
<figure><img src="../images/hero-fx-di.svg" alt="Bodu.Financial.ExchangeRates.DependencyInjection" /><figcaption><code>Bodu.Financial.ExchangeRates.DependencyInjection</code></figcaption></figure>
<figure><img src="../images/hero-fx-rba.svg" alt="Bodu.Financial.ExchangeRates.Rba" /><figcaption><code>Bodu.Financial.ExchangeRates.Rba</code></figcaption></figure>
<figure><img src="../images/hero-fx-boe.svg" alt="Bodu.Financial.ExchangeRates.Boe" /><figcaption><code>Bodu.Financial.ExchangeRates.Boe</code></figcaption></figure>
<figure><img src="../images/hero-fx-ecb.svg" alt="Bodu.Financial.ExchangeRates.Ecb" /><figcaption><code>Bodu.Financial.ExchangeRates.Ecb</code></figcaption></figure>
<figure><img src="../images/hero-fx-yahoo.svg" alt="Bodu.Financial.ExchangeRates.Yahoo" /><figcaption><code>Bodu.Financial.ExchangeRates.Yahoo</code></figcaption></figure>
<figure><img src="../images/hero-fx-ofx.svg" alt="Bodu.Financial.ExchangeRates.Ofx" /><figcaption><code>Bodu.Financial.ExchangeRates.Ofx</code></figcaption></figure>
<figure><img src="../images/hero-fx-xe.svg" alt="Bodu.Financial.ExchangeRates.Xe" /><figcaption><code>Bodu.Financial.ExchangeRates.Xe</code></figcaption></figure>
<figure><img src="../images/hero-fx-oanda.svg" alt="Bodu.Financial.ExchangeRates.Oanda" /><figcaption><code>Bodu.Financial.ExchangeRates.Oanda</code></figcaption></figure>
<figure><img src="../images/hero-fx-fixer.svg" alt="Bodu.Financial.ExchangeRates.Fixer" /><figcaption><code>Bodu.Financial.ExchangeRates.Fixer</code></figcaption></figure>
<figure><img src="../images/hero-fx-exchangeratehost.svg" alt="Bodu.Financial.ExchangeRates.ExchangeRateHost" /><figcaption><code>Bodu.Financial.ExchangeRates.ExchangeRateHost</code></figcaption></figure>
<figure><img src="../images/hero-fx-fred.svg" alt="Bodu.Financial.ExchangeRates.Fred" /><figcaption><code>Bodu.Financial.ExchangeRates.Fred</code></figcaption></figure>
<figure><img src="../images/hero-fx-imf.svg" alt="Bodu.Financial.ExchangeRates.Imf" /><figcaption><code>Bodu.Financial.ExchangeRates.Imf</code></figcaption></figure>
<figure><img src="../images/hero-fx-caching.svg" alt="Bodu.Financial.ExchangeRates.Caching" /><figcaption><code>Bodu.Financial.ExchangeRates.Caching</code></figcaption></figure>
<figure><img src="../images/hero-fx-caching-distributed.svg" alt="Bodu.Financial.ExchangeRates.Caching.Distributed" /><figcaption><code>Bodu.Financial.ExchangeRates.Caching.Distributed</code></figcaption></figure>
<figure><img src="../images/hero-fx-caching-sqlite.svg" alt="Bodu.Financial.ExchangeRates.Caching.Sqlite" /><figcaption><code>Bodu.Financial.ExchangeRates.Caching.Sqlite</code></figcaption></figure>
</div>

## Calendar data packs

Region-specific public-holiday rules ship as independent NuGet packages - one per region - so consumers pull in only the territories they need. Each is built on the notable-date schema and exposes a `<Region>CalendarData` factory over per-country embedded resource packs.

| Package | Status | Version | Purpose | Depends on |
|---|---|---|---|---|
| `Bodu.Globalization.Calendar.Americas` | Stable | 1.3.0 | Curated public-holiday rules for the Americas territory bundle (e.g. `US`, `CA`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.AsiaPacific` | Stable | 1.3.0 | Asia-Pacific bundle (e.g. `AU` with subdivisions, `CN`, `IN`, `JP`, `KR`, `MY`, `NZ`, `SG`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.Europe` | Stable | 1.3.0 | Europe bundle (e.g. `DE`, `ES`, `FR`, `GB`, `IT`, `NL`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.Africa` | Stable | 1.3.0 | Africa bundle (e.g. `ZA`, `NG`, `KE`, `GH`, `ET`, `EG`, `MA`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.MiddleEast` | Stable | 1.3.0 | Middle East bundle (e.g. `AE`, `SA`, `IL`, `TR`, `QA`, `JO`). | `Bodu.Globalization.Calendar` |

<div class="bodu-matrix-gallery">
<figure><img src="../images/hero-calendar-americas.svg" alt="Bodu.Globalization.Calendar.Americas" /><figcaption><code>Bodu.Globalization.Calendar.Americas</code></figcaption></figure>
<figure><img src="../images/hero-calendar-asiapacific.svg" alt="Bodu.Globalization.Calendar.AsiaPacific" /><figcaption><code>Bodu.Globalization.Calendar.AsiaPacific</code></figcaption></figure>
<figure><img src="../images/hero-calendar-europe.svg" alt="Bodu.Globalization.Calendar.Europe" /><figcaption><code>Bodu.Globalization.Calendar.Europe</code></figcaption></figure>
<figure><img src="../images/hero-calendar-africa.svg" alt="Bodu.Globalization.Calendar.Africa" /><figcaption><code>Bodu.Globalization.Calendar.Africa</code></figcaption></figure>
<figure><img src="../images/hero-calendar-middleeast.svg" alt="Bodu.Globalization.Calendar.MiddleEast" /><figcaption><code>Bodu.Globalization.Calendar.MiddleEast</code></figcaption></figure>
</div>

See the [Calendar introduction](calendar/index.md) for how the companion packages compose with the runtime, and the [data-packs guide](../guides/calendar/data-packs.md) for per-bundle install commands and territory coverage.

## Status meanings

| Status | What it commits to |
|---|---|
| **Stable** | The public API surface is committed. Breaking changes are reserved for a major-version bump, except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml` - 1.3.0's replacement of `WeekPattern` with `DayOfWeekSet` in `Bodu.Core`, `Bodu.Globalization.Calendar`, and `Bodu.Globalization.Calendar.Builder` is the one so far. Additive changes ship in minor versions; bug fixes in patch versions. |
| **Preview** | Offered for early evaluation. The public API surface is still taking shape and may change between releases without a major-version bump. |
| **Experimental** | Behaviour depends on an external resource that offers no versioned contract (for example a scraped web page), so it can stop working without any change on the Bodu side. Treat it as best-effort and pair it with a stable alternative. |

Status describes the **API contract**, not availability: it says what a version bump may do to code you have already written. [Versioning, stability, and support](versioning-and-support.md) covers the two version streams, the 1.3.0 break, and which releases receive fixes. Whether a package is on nuget.org is a separate decision: 56 packages in this matrix are published, four are deliberately not, and the source generator is not packaged at all - see below.

## Not published to nuget.org

These four projects build and pack with the rest of the solution, and are documented here like any other package, but they are withheld from nuget.org on purpose. `bld/release-manifest.txt` records the reasoning; in short:

| Package | Why it is held back |
|---|---|
| `Bodu.Financial.ExchangeRates.Xe` | Authenticates with a token scraped from an unversioned XE.com page, so it can stop working through no fault of the code and no release can fix it. |
| `Bodu.Financial.ExchangeRates.Oanda` | Same class of fragility: OANDA's endpoint is undocumented and gated by bot mitigation. |
| `Bodu.Globalization.Calendar.Tool` | A `PackAsTool` CLI rather than a library - a different consumer contract from every other package here, warranting its own decision. |
| `Bodu.Globalization.Calendar.Build` | An MSBuild integration for the tool above, and held back with it. |

To use one, build it from a clone. The CLI installs from a local feed:

```bash
dotnet pack Bodu.Globalization.Calendar.Tool/src -c Release -o ./artifacts
dotnet tool install --global --add-source ./artifacts Bodu.Globalization.Calendar.Tool
```

For the two exchange-rate providers, reference the project directly with `<ProjectReference>` instead of a `PackageReference`. A `<ProjectReference>` to `Bodu.Globalization.Calendar.Build` compiles nothing, because the `NotableDatePack` items are processed by the package's `.targets`, which NuGet imports only from a package: pack it into a local feed and reference that, or import the targets from the clone as [Compiling packs in MSBuild](../guides/calendar/tooling/msbuild-integration.md#from-a-clone-without-a-package) shows.

`Bodu.Text.Formats.Generators`, the source generator behind the reflection-free Delimited and INI binding, is not packaged (`IsPackable=false`), so it has no status or version here: reference it from a clone as an analyzer, as [its page](formats/generators.md) shows.

## Install commands

The standard `dotnet add package` invocation for each **published** package:

```bash
# Primary libraries
dotnet add package Bodu.Core
dotnet add package Bodu.Collections
dotnet add package Bodu.Collections.Concurrent
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Text.Encoding
dotnet add package Bodu.Text.Filtering
dotnet add package Bodu.Text.Formats
dotnet add package Bodu.Text.Delimited
dotnet add package Bodu.Text.DotEnv
dotnet add package Bodu.Text.Ini
dotnet add package Bodu.Text.Toml
dotnet add package Bodu.Text.Bencode
dotnet add package Bodu.Text.Yaml
dotnet add package Bodu.Text.Configuration
dotnet add package Bodu.Extensions.Configuration.Text
dotnet add package Bodu.Numerics
dotnet add package Bodu.Financial

# Companions
dotnet add package Bodu.Globalization.Calendar.Builder
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
dotnet add package Bodu.Globalization.Calendar.Plugins
dotnet add package Bodu.Globalization.Calendar.Caching
dotnet add package Bodu.Globalization.Calendar.Caching.Distributed
dotnet add package Bodu.Globalization.Calendar.Caching.Sqlite
dotnet add package Bodu.Globalization.Recurrence

dotnet add package Bodu.Text.Serialization
dotnet add package Bodu.Financial.DependencyInjection
dotnet add package Bodu.Financial.Serialization.Json
dotnet add package Bodu.Numerics.Serialization.Json

# File formats and exchange-rate data
dotnet add package Bodu.IO.Compound
dotnet add package Bodu.IO.Biff
dotnet add package Bodu.Formats.Excel.Binary
dotnet add package Bodu.IO.Pst
dotnet add package Bodu.Formats.Outlook
dotnet add package Bodu.Formats.Outlook.Msg
dotnet add package Bodu.Formats.Outlook.Pst
dotnet add package Bodu.Financial.ExchangeRates
dotnet add package Bodu.Financial.ExchangeRates.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Rba
dotnet add package Bodu.Financial.ExchangeRates.Boe
dotnet add package Bodu.Financial.ExchangeRates.Ecb
dotnet add package Bodu.Financial.ExchangeRates.Yahoo
dotnet add package Bodu.Financial.ExchangeRates.Ofx
dotnet add package Bodu.Financial.ExchangeRates.Fixer
dotnet add package Bodu.Financial.ExchangeRates.ExchangeRateHost
dotnet add package Bodu.Financial.ExchangeRates.Fred
dotnet add package Bodu.Financial.ExchangeRates.Imf
dotnet add package Bodu.Financial.ExchangeRates.Caching
dotnet add package Bodu.Financial.ExchangeRates.Caching.Distributed
dotnet add package Bodu.Financial.ExchangeRates.Caching.Sqlite

# Calendar regional data packs (install only what you need)
dotnet add package Bodu.Globalization.Calendar.Americas
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
dotnet add package Bodu.Globalization.Calendar.Europe
dotnet add package Bodu.Globalization.Calendar.Africa
dotnet add package Bodu.Globalization.Calendar.MiddleEast
```

`Bodu.Financial.ExchangeRates.Testing` - the contract-test bases the provider and cache test projects derive - is a source-only project in the repository (`IsPackable=false`) and is not published to NuGet.

## Design principles

- **Minimal external runtime dependencies.** Most libraries depend only on Bodu packages and the shared framework. The exceptions are named in the **Depends on** column: `Bodu.IO.Hashing` takes the `System.IO.Hashing` package; `Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.Plugins`, and `Bodu.Financial.ExchangeRates` take `Microsoft.Extensions.Logging.Abstractions`; and the dependency-injection, configuration, caching, and HTTP companions intentionally bridge to the Microsoft.Extensions ecosystem. [Licensing](licensing.md) lists every third-party dependency and its license.
- **Nullable reference types** are enabled throughout. Public APIs declare their null-intent explicitly.
- **Analyzer-clean**: StyleCop, Roslynator, .NET analyzers, AsyncFixer, and Threading analyzers run at build time; doc-comment warnings are treated as errors.
- **Deterministic builds** for reproducible package outputs.
- **Documentation-first**: every public type and member carries XML documentation in US English, which drives the API reference.
- **MIT licensed.**

## Where to go next

- **[Introduction](introduction.md)** - high-level overview of the suite.
- **[Getting started](getting-started.md)** - install and run minimal samples across the suite.
