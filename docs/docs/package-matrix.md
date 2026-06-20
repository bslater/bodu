---
title: Bodu package matrix
---

# Bodu package matrix

The Bodu suite ships as a family of focused NuGet packages, each with a clear responsibility. This page is the authoritative list — what every package is for, where it lives in the dependency graph, and how mature its public surface is today.

For the high-level shape of each library, follow the **Intro** link in the table; for runnable samples, the **Get started** link.

## At a glance

| Category | Package | Status | Depends on | Intro | Get started |
|---|---|---|---|---|---|
| **Foundation** | <xref:Bodu> · `Bodu.Core` | Stable | (BCL only) | [Bodu.Core](core/index.md) | [Get started](core/getting-started.md) |
| **Hashing** | `Bodu.IO.Hashing` | Stable | `Bodu.Core`, `System.IO.Hashing` | [Bodu.IO.Hashing](io-hashing/index.md) | [Get started](io-hashing/getting-started.md) |
| **Cryptography** | `Bodu.Security.Cryptography` | Stable | `Bodu.Core`, `System.Security.Cryptography` | [Bodu.Security.Cryptography](cryptography/index.md) | [Get started](cryptography/getting-started.md) |
| **Calendar runtime** | `Bodu.Globalization.Calendar` | Stable | `Bodu.Core` | [Bodu.Globalization.Calendar](calendar/index.md) | [Get started](calendar/getting-started.md) |
| **Text encoding** | `Bodu.Text.Encoding` | Stable | `Bodu.Core` | [Bodu.Text.Encoding](text-encoding/index.md) | [Get started](text-encoding/getting-started.md) |
| **Text formats** | `Bodu.Text.Formats` | Stable | `Bodu.Core` | [Bodu.Text.Formats](formats/index.md) | [Get started](formats/getting-started.md) |
| **TOML serializer** | `Bodu.Text.Toml` | **Preview** | `Bodu.Core` | [Bodu serializers](serialization/index.md) | [Get started](serialization/getting-started.md) |
| **Bencode serializer** | `Bodu.Text.Bencode` | **Preview** | `Bodu.Core` | [Bodu serializers](serialization/index.md) | [Get started](serialization/getting-started.md) |
| **Text configuration** | `Bodu.Text.Configuration` | Stable | `Bodu.Core`, `Bodu.Text.Formats` | [Bodu.Text.Configuration](text-configuration/index.md) | [Get started](text-configuration/getting-started.md) |
| **Configuration bridge** | `Bodu.Extensions.Configuration.Text` | Stable | `Bodu.Text.Configuration`, `Microsoft.Extensions.Configuration` | [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md) | [Get started](extensions-configuration-text/getting-started.md) |
| **Numerics** | `Bodu.Numerics` | **Preview** | `Bodu.Core` | [Bodu.Numerics](numerics/index.md) | [Get started](numerics/getting-started.md) |
| **Financial** | `Bodu.Financial` | **Preview** | `Bodu.Numerics`, `Bodu.Core` | [Bodu.Financial](financial/index.md) | [Get started](financial/getting-started.md) |

## Companion packages

Several capabilities ship as independent companion packages so they can release on their own cadence without forcing a main-library rebuild. The calendar runtime in particular is intentionally small — fluent rule authoring, plugin loading, and dependency-injection registration are all opt-in.

| Package | Status | Purpose | Depends on |
|---|---|---|---|
| `Bodu.Globalization.Calendar.Builder` | Stable | Fluent, chainable C# API for authoring notable-date documents in code, with XML / JSON serialization and load/save. | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.DependencyInjection` | Stable | `IServiceCollection` extensions for registering `INotableDateService` over a loaded `NotableDateResource`. | `Bodu.Globalization.Calendar`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `Bodu.Globalization.Calendar.Plugins` | Stable | Trust-gated loading of external assemblies that contribute custom `INotableDateAlgorithm` implementations. | `Bodu.Globalization.Calendar` |
| `Bodu.Financial.DependencyInjection` | Stable | `IServiceCollection` extensions for registering Bodu.Financial currency-lookup and monetary services via `AddBoduFinancial`. | `Bodu.Financial`, `Microsoft.Extensions.DependencyInjection.Abstractions` |

## File formats and exchange-rate data

Several exchange-rate providers ship as independent packages over a shared `IDatedExchangeRateProvider` / `IExchangeRateProvider` contract, so consumers pull in only the data sources they need, each paired with an opt-in dependency-injection companion. The contract exposes a symmetric single-date/range lookup matrix (synchronous and asynchronous): single-date getters return an `ExchangeRateLookupResult` (the resolved rate plus the applied date-resolution and offset), and range getters return an `ExchangeRateRangeResult` (the rates, the requested window, and the observed span; it is itself an `IReadOnlyList<ExchangeRate>`). The HTTP-backed providers share a `WebExchangeRateProvider` base and are `IDisposable` — each either builds and owns its `HttpClient` from options (via `ExchangeRateHttpClientFactory`) or borrows a caller-supplied one. The Reserve Bank of Australia provider sits on a small, strictly layered stack whose lower two layers carry no financial or RBA-specific concepts and can be reused on their own: a generic compound-file reader, and a narrow binary-`.xls` reader built on it. A standalone caching layer can decorate any provider with a shared on-disk TOML cache.

| Package | Status | Purpose | Depends on |
|---|---|---|---|
| `Bodu.IO.Compound` | **Preview** | Read-only reader for the OLE2 / Compound File Binary (CFB) container — the structured-storage envelope used by legacy Office files. Exposes the embedded named streams with no application-format knowledge. | `Bodu.Core` |
| `Bodu.Formats.Excel.Binary` | **Preview** | Narrow, read-only BIFF8 (`.xls`) reader that surfaces raw worksheet cell values — strings, numbers, booleans, and errors — without formula evaluation, styling, or higher-level interpretation. | `Bodu.IO.Compound`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | **Preview** | Shared dependency-injection machinery for the web-based exchange-rate providers. Exposes `AddWebExchangeRateProvider` on `IFinancialServiceBuilder`, handling `HttpClient` configuration with Polly resilience, options binding, and singleton registration so the per-source DI packages delegate their plumbing here. | `Bodu.Financial.DependencyInjection`, `Bodu.Core`, `Microsoft.Extensions.Http.Resilience` |
| `Bodu.Financial.ExchangeRates.Rba` | **Preview** | Downloads and parses the RBA's published daily exchange-rate `.xls` files, serving them as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API and in-memory plus on-disk caching. | `Bodu.Formats.Excel.Binary`, `Bodu.Financial`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Rba.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register the RBA provider as a singleton backed by a configured `HttpClient`, binding `RbaExchangeRateOptions` through `Microsoft.Extensions.Options`. | `Bodu.Financial.ExchangeRates.Rba`, `Bodu.Financial.DependencyInjection`, `Microsoft.Extensions.Http` |
| `Bodu.Financial.ExchangeRates.Boe` | **Preview** | Queries the Bank of England's Interactive Statistical Database (IADB) CSV endpoint for daily spot rates, serving them as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API and in-memory plus on-disk caching. | `Bodu.Text.Formats`, `Bodu.Financial`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Boe.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register the BoE provider as a singleton backed by a configured `HttpClient`, binding `BoeExchangeRateOptions` through `Microsoft.Extensions.Options`. | `Bodu.Financial.ExchangeRates.Boe`, `Bodu.Financial.DependencyInjection`, `Microsoft.Extensions.Http` |
| `Bodu.Financial.ExchangeRates.Ecb` | **Preview** | Downloads and parses the ECB's published `eurofxref` euro foreign-exchange reference-rate XML feeds, serving them as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API and in-memory plus on-disk caching. | `Bodu.Financial`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Ecb.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register the ECB provider as a singleton backed by a configured `HttpClient`, binding `EcbExchangeRateOptions` through `Microsoft.Extensions.Options`. | `Bodu.Financial.ExchangeRates.Ecb`, `Bodu.Financial.DependencyInjection`, `Microsoft.Extensions.Http` |
| `Bodu.Financial.ExchangeRates.Yahoo` | **Preview** | Fetches and parses the Yahoo Finance v8 chart JSON service, serving the results as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API over arbitrary currency pairs. | `Bodu.Financial`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register the Yahoo Finance provider as a singleton backed by a configured `HttpClient`, binding `YahooExchangeRateOptions` through `Microsoft.Extensions.Options`. | `Bodu.Financial.ExchangeRates.Yahoo`, `Bodu.Financial.DependencyInjection`, `Microsoft.Extensions.Http` |
| `Bodu.Financial.ExchangeRates.Ofx` | **Preview** | Fetches and parses the OFX (ofx.com) public spot-rate-history JSON service, serving the results as `ExchangeRate` values through `IDatedExchangeRateProvider` / `IExchangeRateProvider`, with an async range API over arbitrary currency pairs. | `Bodu.Financial`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Ofx.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register the OFX provider as a singleton backed by a configured `HttpClient`, binding `OfxExchangeRateOptions` through `Microsoft.Extensions.Options`. | `Bodu.Financial.ExchangeRates.Ofx`, `Bodu.Financial.DependencyInjection`, `Microsoft.Extensions.Http` |
| `Bodu.Financial.ExchangeRates.Caching` | **Preview** | `CachingExchangeRateProvider`, a one-cache-per-provider decorator that wraps any `IDatedExchangeRateProvider` and serves fresh rates from a pluggable cache cascade — `InMemoryExchangeRateCache` and the on-disk `TomlFileExchangeRateCache` (one TOML file per provider and currency pair) — delegating to the inner provider only on a miss; plus `AggregatingExchangeRateProvider`, which groups named child providers behind one entry point and combines them with a pluggable strategy (prioritised fallback, averaging, or per-FX-pair routing). | `Bodu.Text.Toml`, `Bodu.Financial`, `Bodu.Core` |
| `Bodu.Financial.ExchangeRates.Caching.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register the cache cascade (`InMemoryExchangeRateCache`, `TomlFileExchangeRateCache`) and decorate the registered `IDatedExchangeRateProvider` with a `CachingExchangeRateProvider`, so consumers transparently get cached lookups. | `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial.DependencyInjection` |
| `Bodu.Financial.ExchangeRates.Caching.Distributed` | **Preview** | `DistributedExchangeRateCache`, an `IExchangeRateCache` over a `Microsoft.Extensions.Caching.Distributed.IDistributedCache` (Redis-capable), persisting a provider's dated rates and fetch-coverage windows as a per-pair JSON blob; behaviourally identical to the in-memory, TOML, and SQLite caches and validated against the same `ExchangeRateCacheContractTests`. | `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial`, `Bodu.Core`, `Microsoft.Extensions.Caching.Abstractions` |
| `Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register `DistributedExchangeRateCache` as the exchange-rate cache — optionally backed by Redis via `Microsoft.Extensions.Caching.StackExchangeRedis` — binding its options through `Microsoft.Extensions.Options`. | `Bodu.Financial.ExchangeRates.Caching.Distributed`, `Bodu.Financial.DependencyInjection`, `Microsoft.Extensions.Caching.StackExchangeRedis` |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite` | **Preview** | `SqliteExchangeRateCache`, an `IExchangeRateCache` over a SQLite database (via `Microsoft.Data.Sqlite`), persisting a provider's dated rates and fetch-coverage windows in `rates` and `coverage` tables; behaviourally identical to the in-memory and TOML caches and validated against the same `ExchangeRateCacheContractTests`. | `Bodu.Financial.ExchangeRates.Caching`, `Bodu.Financial`, `Bodu.Core`, `Microsoft.Data.Sqlite` |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection` | **Preview** | `IServiceCollection` extensions that register `SqliteExchangeRateCache` as the exchange-rate cache, binding `SqliteExchangeRateCacheOptions` through `Microsoft.Extensions.Options`. | `Bodu.Financial.ExchangeRates.Caching.Sqlite`, `Bodu.Financial.DependencyInjection` |

## Calendar data packs

Region-specific public-holiday rules ship as independent NuGet packages — one per region — so consumers pull in only the territories they need. Each is built on the notable-date schema and exposes a `<Region>CalendarData` factory over per-country embedded resource packs.

| Package | Status | Purpose | Depends on |
|---|---|---|---|
| `Bodu.Globalization.Calendar.Americas` | Stable | Curated public-holiday rules for the Americas territory bundle (e.g. `US`, `CA`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.AsiaPacific` | Stable | Asia-Pacific bundle (e.g. `AU` with subdivisions, `CN`, `IN`, `JP`, `KR`, `MY`, `NZ`, `SG`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.Europe` | Stable | Europe bundle (e.g. `DE`, `ES`, `FR`, `GB`, `IT`, `NL`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.Africa` | Stable | Africa bundle (e.g. `ZA`, `NG`, `KE`, `GH`, `ET`, `EG`, `MA`). | `Bodu.Globalization.Calendar` |
| `Bodu.Globalization.Calendar.MiddleEast` | Stable | Middle East bundle (e.g. `AE`, `SA`, `IL`, `TR`, `QA`, `JO`). | `Bodu.Globalization.Calendar` |

See the [Calendar introduction](calendar/index.md) for how the companion packages compose with the runtime, and the [data-packs guide](../guides/calendar/data-packs.md) for per-bundle install commands and territory coverage.

## Status meanings

| Status | What it commits to |
|---|---|
| **Stable** | The public API surface is committed. Breaking changes are reserved for a major-version bump; additive changes ship in minor versions; bug fixes in patch versions. |
| **Preview** | The package is fully usable but still in its initial release. The public surface is intended to be stable, but minor breaking adjustments may land before promotion to *Stable*. Pin the version you adopt if breakage would be costly. |

## Install commands

The standard `dotnet add package` invocation for each shipped package:

```bash
# Primary libraries
dotnet add package Bodu.Core
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Text.Encoding
dotnet add package Bodu.Text.Formats
dotnet add package Bodu.Text.Toml
dotnet add package Bodu.Text.Bencode
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
dotnet add package Bodu.Financial.ExchangeRates.Rba.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Boe
dotnet add package Bodu.Financial.ExchangeRates.Boe.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Ecb
dotnet add package Bodu.Financial.ExchangeRates.Ecb.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Yahoo
dotnet add package Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Ofx
dotnet add package Bodu.Financial.ExchangeRates.Ofx.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Caching
dotnet add package Bodu.Financial.ExchangeRates.Caching.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Caching.Distributed
dotnet add package Bodu.Financial.ExchangeRates.Caching.Distributed.DependencyInjection
dotnet add package Bodu.Financial.ExchangeRates.Caching.Sqlite
dotnet add package Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection

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
