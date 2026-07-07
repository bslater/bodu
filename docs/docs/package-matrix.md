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

The Bodu suite ships as a family of focused NuGet packages, each with a clear responsibility. This page is the authoritative list — what every package is for, where it lives in the dependency graph, and how mature its public surface is today.

For the high-level shape of each library, follow the **Intro** link in the table; for runnable samples, the **Get started** link.

## At a glance

| Category | Package | Status | Depends on | Intro | Get started |
|---|---|---|---|---|---|
| **Foundation** | <xref:Bodu> · `Bodu.Core` | Stable | (BCL only) | [Bodu.Core](core/index.md) | [Get started](core/getting-started.md) |
| **Collections** | `Bodu.Collections` | Stable | `Bodu.Core` | [Bodu.Collections](collections/index.md) | [Get started](collections/getting-started.md) |
| **Concurrent collections** | `Bodu.Collections.Concurrent` | Stable | `Bodu.Collections` | [Bodu.Collections.Concurrent](collections-concurrent/index.md) | [Get started](collections-concurrent/getting-started.md) |
| **Hashing** | `Bodu.IO.Hashing` | Stable | `Bodu.Core`, `System.IO.Hashing` | [Bodu.IO.Hashing](io-hashing/index.md) | [Get started](io-hashing/getting-started.md) |
| **Cryptography** | `Bodu.Security.Cryptography` | Stable | `Bodu.Core`, `System.Security.Cryptography` | [Bodu.Security.Cryptography](cryptography/index.md) | [Get started](cryptography/getting-started.md) |
| **Calendar runtime** | `Bodu.Globalization.Calendar` | Stable | `Bodu.Core` | [Bodu.Globalization.Calendar](calendar/index.md) | [Get started](calendar/getting-started.md) |
| **Text encoding** | `Bodu.Text.Encoding` | Stable | `Bodu.Core` | [Bodu.Text.Encoding](text-encoding/index.md) | [Get started](text-encoding/getting-started.md) |
| **Text formats** | `Bodu.Text.Formats` | Stable | `Bodu.Core` | [Bodu.Text.Formats](formats/index.md) | [Get started](formats/getting-started.md) |
| **TOML serializer** | `Bodu.Text.Toml` | Stable | `Bodu.Core` | [Bodu.Text.Toml](serialization/toml/index.md) | [Get started](serialization/toml/getting-started.md) |
| **Bencode serializer** | `Bodu.Text.Bencode` | Stable | `Bodu.Core` | [Bodu.Text.Bencode](serialization/bencode/index.md) | [Get started](serialization/bencode/getting-started.md) |
| **YAML serializer** | `Bodu.Text.Yaml` | Preview | `Bodu.Core` | [Bodu.Text.Yaml](serialization/yaml/index.md) | [Get started](serialization/yaml/getting-started.md) |
| **Text configuration** | `Bodu.Text.Configuration` | Stable | `Bodu.Core`, `Bodu.Text.Formats` | [Bodu.Text.Configuration](text-configuration/index.md) | [Get started](text-configuration/getting-started.md) |
| **Configuration bridge** | `Bodu.Extensions.Configuration.Text` | Stable | `Bodu.Text.Configuration`, `Microsoft.Extensions.Configuration` | [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md) | [Get started](extensions-configuration-text/getting-started.md) |
| **Numerics** | `Bodu.Numerics` | Stable | `Bodu.Core` | [Bodu.Numerics](numerics/index.md) | [Get started](numerics/getting-started.md) |
| **Financial** | `Bodu.Financial` | Stable | `Bodu.Numerics`, `Bodu.Core` | [Bodu.Financial](financial/index.md) | [Get started](financial/getting-started.md) |

## Companion packages

Several capabilities ship as independent companion packages so they can release on their own cadence without forcing a main-library rebuild. The calendar runtime in particular is intentionally small — fluent rule authoring, plugin loading, and dependency-injection registration are all opt-in.

| Package | Status | Purpose | Depends on |
|---|---|---|---|
| `Bodu.Globalization.Calendar.Builder` | Stable | Fluent, chainable C# API for authoring notable-date documents in code, with XML / JSON serialization and load/save. | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.DependencyInjection` | Stable | `IServiceCollection` extensions for registering `INotableDateService` over a loaded `NotableDateResource`. | `Bodu.Globalization.Calendar`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `Bodu.Globalization.Calendar.Plugins` | Stable | Trust-gated loading of external assemblies that contribute custom `INotableDateAlgorithm` implementations. | `Bodu.Globalization.Calendar` |
| `Bodu.Financial.DependencyInjection` | Stable | `IServiceCollection` extensions for registering Bodu.Financial currency-lookup and monetary services via `AddFinancialService`. | `Bodu.Financial`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `Bodu.Numerics.Serialization.Json` | Preview | `System.Text.Json` converters, converter factories, and the `ConfigureForBoduNumerics()` registration for the `Bodu.Numerics` types (`Fraction<T>`, `BigDecimal`, `Interval<T>`, `DiscreteInterval<T>`, `IntervalSet<T>`), keeping the core numerics library serialization-agnostic. | `Bodu.Numerics`, `System.Text.Json` |

<div class="bodu-matrix-gallery">
<figure><img src="../images/hero-calendar-builder.svg" alt="Bodu.Globalization.Calendar.Builder" /><figcaption><code>Bodu.Globalization.Calendar.Builder</code></figcaption></figure>
<figure><img src="../images/hero-calendar-di.svg" alt="Bodu.Globalization.Calendar.DependencyInjection" /><figcaption><code>Bodu.Globalization.Calendar.DependencyInjection</code></figcaption></figure>
<figure><img src="../images/hero-calendar-plugins.svg" alt="Bodu.Globalization.Calendar.Plugins" /><figcaption><code>Bodu.Globalization.Calendar.Plugins</code></figcaption></figure>
<figure><img src="../images/hero-financial-di.svg" alt="Bodu.Financial.DependencyInjection" /><figcaption><code>Bodu.Financial.DependencyInjection</code></figcaption></figure>
<figure><img src="../images/hero-numerics-json.svg" alt="Bodu.Numerics.Serialization.Json" /><figcaption><code>Bodu.Numerics.Serialization.Json</code></figcaption></figure>
</div>

## File formats and exchange-rate data

Several exchange-rate providers ship as independent packages over a shared `IDatedExchangeRateProvider` / `IExchangeRateProvider` contract, so consumers pull in only the data sources they need, each paired with an opt-in dependency-injection companion. The contract exposes a symmetric single-date/range lookup matrix (synchronous and asynchronous): single-date getters return an `ExchangeRateLookupResult` (the resolved rate plus the applied date-resolution and offset), and range getters return an `ExchangeRateRangeResult` (the rates, the requested window, and the observed span; it is itself an `IReadOnlyList<ExchangeRate>`). The HTTP-backed providers share a `WebExchangeRateProvider` base and are `IDisposable` — each either builds and owns its `HttpClient` from options (via `ExchangeRateHttpClientFactory`) or borrows a caller-supplied one. The Reserve Bank of Australia provider sits on a small, strictly layered stack whose lower two layers carry no financial or RBA-specific concepts and can be reused on their own: a generic compound-file reader, and a narrow binary-`.xls` reader built on it. A standalone caching layer can decorate any provider with a shared on-disk TOML cache.

| Package | Status | Purpose | Depends on |
|---|---|---|---|
| `Bodu.IO.Compound` | Stable | Reader and writer for the OLE2 / Compound File Binary (CFB) container — the structured-storage envelope used by legacy Office files. Opens existing containers (exposing the embedded named streams) and authors new ones via `CompoundFile.Create` and the builder API, with no application-format knowledge. | `Bodu.Core` |
| `Bodu.Formats.Excel.Binary` | Stable | Narrow, read-only BIFF8 (`.xls`) reader that surfaces raw worksheet cell values — strings, numbers, booleans, and errors — without formula evaluation, styling, or higher-level interpretation. | `Bodu.IO.Compound`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | Stable | Shared dependency-injection machinery for the web-based exchange-rate providers. Exposes `AddWebExchangeRateProvider` on `IFinancialServiceBuilder` (in the `Bodu.Financial` namespace), handling `HttpClient` configuration with Polly resilience, options binding, and singleton registration so each provider package's own DI registration delegates its plumbing here. | `Bodu.Financial.DependencyInjection`, `Bodu.Core`, `Microsoft.Extensions.Http.Resilience` |
| `Bodu.Financial.ExchangeRates.Rba` | Stable | Downloads and parses the RBA's published daily exchange-rate `.xls` files, serving them as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API and in-memory plus on-disk caching. Includes its own `AddRbaHistoricalRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `RbaExchangeRateOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Formats.Excel.Binary`, `Bodu.Financial`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Boe` | Stable | Queries the Bank of England's Interactive Statistical Database (IADB) CSV endpoint for daily spot rates, serving them as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API and in-memory plus on-disk caching. Includes its own `AddBoeReferenceRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `BoeExchangeRateOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Text.Formats`, `Bodu.Financial`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Ecb` | Stable | Downloads and parses the ECB's published `eurofxref` euro foreign-exchange reference-rate XML feeds, serving them as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API and in-memory plus on-disk caching. Includes its own `AddEcbReferenceRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `EcbExchangeRateOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Yahoo` | Stable | Fetches and parses the Yahoo Finance v8 chart JSON service, serving the results as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API over arbitrary currency pairs. Includes its own `AddYahooExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `YahooExchangeRateOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Ofx` | Stable | Fetches and parses the OFX (ofx.com) public spot-rate-history JSON service, serving the results as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API over arbitrary currency pairs. Includes its own `AddOfxExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `OfxExchangeRateOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Xe` | Stable | Fetches and parses the XE.com charting-rates JSON service, serving the results as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API over arbitrary currency pairs; the authorization token the endpoint requires is acquired automatically from the XE website. Includes its own `AddXeExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `XeExchangeRateOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Oanda` | Stable | Fetches and parses the OANDA Historical Currency Converter rate-history JSON service, serving the results as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API over arbitrary currency pairs. The anonymous endpoint serves a rolling recent window (roughly the last 180 days), advertised through the provider's `HistoryAvailability`. Includes its own `AddOandaExchangeRates` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `OandaExchangeRateOptions` and backing the provider with a configured `HttpClient`. | `Bodu.Financial`, `Bodu.Financial.ExchangeRates.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Caching` | Stable | `CachingExchangeRateProvider`, a one-cache-per-provider decorator that wraps any `IDatedExchangeRateProvider` and serves fresh rates from a pluggable cache cascade — `InMemoryExchangeRateCache` and the on-disk `TomlFileExchangeRateCache` (one TOML file per provider and currency pair) — delegating to the inner provider only on a miss; plus `AggregatingExchangeRateProvider`, which groups named child providers behind one entry point and combines them with a pluggable strategy (prioritised fallback, averaging, or per-FX-pair routing). Includes its own DI registration (`AddCachedExchangeRateProvider`, `AddAggregatedExchangeRateProvider`, in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Text.Toml`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Caching.Distributed` | Stable | `DistributedExchangeRateCache`, an `IExchangeRateCache` over a `Microsoft.Extensions.Caching.Distributed.IDistributedCache` (Redis-capable), persisting a provider's dated rates and fetch-coverage windows as a per-pair JSON blob; behaviourally identical to the in-memory, TOML, and SQLite caches and validated against the same `ExchangeRateCacheContractTests`. Includes its own `AddDistributedRateCache` / `AddRedisRateCache` DI registration (in the `Bodu.Financial.ExchangeRates` namespace). | `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Core`, `Microsoft.Extensions.Caching.Abstractions` |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite` | Stable | `SqliteExchangeRateCache`, an `IExchangeRateCache` over a SQLite database (via `Microsoft.Data.Sqlite`), persisting a provider's dated rates and fetch-coverage windows in `rates` and `coverage` tables; behaviourally identical to the in-memory and TOML caches and validated against the same `ExchangeRateCacheContractTests`. Includes its own `AddSqliteRateCache` DI registration (in the `Bodu.Financial.ExchangeRates` namespace), binding `SqliteExchangeRateCacheOptions`. | `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial`, `Bodu.Financial.DependencyInjection`, `Bodu.Core`, `Microsoft.Data.Sqlite` |

<div class="bodu-matrix-gallery">
<figure><img src="../images/hero-fx-di.svg" alt="Bodu.Financial.ExchangeRates.DependencyInjection" /><figcaption><code>Bodu.Financial.ExchangeRates.DependencyInjection</code></figcaption></figure>
<figure><img src="../images/hero-fx-rba.svg" alt="Bodu.Financial.ExchangeRates.Rba" /><figcaption><code>Bodu.Financial.ExchangeRates.Rba</code></figcaption></figure>
<figure><img src="../images/hero-fx-boe.svg" alt="Bodu.Financial.ExchangeRates.Boe" /><figcaption><code>Bodu.Financial.ExchangeRates.Boe</code></figcaption></figure>
<figure><img src="../images/hero-fx-ecb.svg" alt="Bodu.Financial.ExchangeRates.Ecb" /><figcaption><code>Bodu.Financial.ExchangeRates.Ecb</code></figcaption></figure>
<figure><img src="../images/hero-fx-yahoo.svg" alt="Bodu.Financial.ExchangeRates.Yahoo" /><figcaption><code>Bodu.Financial.ExchangeRates.Yahoo</code></figcaption></figure>
<figure><img src="../images/hero-fx-ofx.svg" alt="Bodu.Financial.ExchangeRates.Ofx" /><figcaption><code>Bodu.Financial.ExchangeRates.Ofx</code></figcaption></figure>
<figure><img src="../images/hero-fx-xe.svg" alt="Bodu.Financial.ExchangeRates.Xe" /><figcaption><code>Bodu.Financial.ExchangeRates.Xe</code></figcaption></figure>
<figure><img src="../images/hero-fx-oanda.svg" alt="Bodu.Financial.ExchangeRates.Oanda" /><figcaption><code>Bodu.Financial.ExchangeRates.Oanda</code></figcaption></figure>
<figure><img src="../images/hero-fx-caching.svg" alt="Bodu.Financial.ExchangeRates.Caching" /><figcaption><code>Bodu.Financial.ExchangeRates.Caching</code></figcaption></figure>
<figure><img src="../images/hero-fx-caching-distributed.svg" alt="Bodu.Financial.ExchangeRates.Caching.Distributed" /><figcaption><code>Bodu.Financial.ExchangeRates.Caching.Distributed</code></figcaption></figure>
<figure><img src="../images/hero-fx-caching-sqlite.svg" alt="Bodu.Financial.ExchangeRates.Caching.Sqlite" /><figcaption><code>Bodu.Financial.ExchangeRates.Caching.Sqlite</code></figcaption></figure>
</div>

## Calendar data packs

Region-specific public-holiday rules ship as independent NuGet packages — one per region — so consumers pull in only the territories they need. Each is built on the notable-date schema and exposes a `<Region>CalendarData` factory over per-country embedded resource packs.

| Package | Status | Purpose | Depends on |
|---|---|---|---|
| `Bodu.Globalization.Calendar.Americas` | Stable | Curated public-holiday rules for the Americas territory bundle (e.g. `US`, `CA`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.AsiaPacific` | Stable | Asia-Pacific bundle (e.g. `AU` with subdivisions, `CN`, `IN`, `JP`, `KR`, `MY`, `NZ`, `SG`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.Europe` | Stable | Europe bundle (e.g. `DE`, `ES`, `FR`, `GB`, `IT`, `NL`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.Africa` | Stable | Africa bundle (e.g. `ZA`, `NG`, `KE`, `GH`, `ET`, `EG`, `MA`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.MiddleEast` | Stable | Middle East bundle (e.g. `AE`, `SA`, `IL`, `TR`, `QA`, `JO`). | `Bodu.Globalization.Calendar` |

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
| **Stable** | The public API surface is committed. Breaking changes are reserved for a major-version bump; additive changes ship in minor versions; bug fixes in patch versions. |
| **Preview** | The package is published for early evaluation. The public API surface is still taking shape and may change between releases without a major-version bump. |

## Install commands

The standard `dotnet add package` invocation for each shipped package:

```bash
# Primary libraries
dotnet add package Bodu.Core
dotnet add package Bodu.Collections
dotnet add package Bodu.Collections.Concurrent
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Text.Encoding
dotnet add package Bodu.Text.Formats
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
dotnet add package Bodu.Financial.DependencyInjection

# File formats and exchange-rate data
dotnet add package Bodu.IO.Compound
dotnet add package Bodu.Formats.Excel.Binary
dotnet add package Bodu.Financial.ExchangeRates.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Rba
dotnet add package Bodu.Financial.ExchangeRates.Boe
dotnet add package Bodu.Financial.ExchangeRates.Ecb
dotnet add package Bodu.Financial.ExchangeRates.Yahoo
dotnet add package Bodu.Financial.ExchangeRates.Ofx
dotnet add package Bodu.Financial.ExchangeRates.Xe
dotnet add package Bodu.Financial.ExchangeRates.Oanda
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

## Design principles

- **Minimal external runtime dependencies.** Core libraries depend only on the BCL. Extension packages (`Bodu.Extensions.Configuration.Text`, `Bodu.Globalization.Calendar.DependencyInjection`) intentionally bridge to the Microsoft.Extensions ecosystem.
- **Nullable reference types** are enabled throughout. Public APIs declare their null-intent explicitly.
- **Analyzer-clean**: StyleCop, Roslynator, .NET analyzers, AsyncFixer, and Threading analyzers run at build time; doc-comment warnings are treated as errors.
- **Deterministic builds** for reproducible package outputs.
- **Documentation-first**: every public type and member carries XML documentation in US English, which drives the API reference.
- **MIT licensed.**

## Where to go next

- **[Introduction](introduction.md)** — high-level overview of the suite.
- **[Getting started](getting-started.md)** — install and run minimal samples across the suite.
