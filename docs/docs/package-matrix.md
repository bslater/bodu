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
| **Serialization engine** | `Bodu.Text.Serialization` | **Preview** | `Bodu.Core` | [Bodu.Text.Serialization](serialization/index.md) | [Get started](serialization/getting-started.md) |
| **TOML serializer** | `Bodu.Text.Serialization.Toml` | **Preview** | `Bodu.Text.Serialization` | [Bodu.Text.Serialization](serialization/index.md) | [Get started](serialization/getting-started.md) |
| **Bencode serializer** | `Bodu.Text.Serialization.Bencode` | **Preview** | `Bodu.Text.Serialization` | [Bodu.Text.Serialization](serialization/index.md) | [Get started](serialization/getting-started.md) |
| **Text configuration** | `Bodu.Text.Configuration` | Stable | `Bodu.Core`, `Bodu.Text.Formats` | [Bodu.Text.Configuration](text-configuration/index.md) | [Get started](text-configuration/getting-started.md) |
| **Configuration bridge** | `Bodu.Extensions.Configuration.Text` | Stable | `Bodu.Text.Configuration`, `Microsoft.Extensions.Configuration` | [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md) | [Get started](extensions-configuration-text/getting-started.md) |
| **Text encoding helpers** | `Bodu.Text` | Stable | `Bodu.Core` | [Bodu.Text](text/index.md) | [Get started](getting-started.md#bodutext) |
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
dotnet add package Bodu.Text.Configuration
dotnet add package Bodu.Extensions.Configuration.Text
dotnet add package Bodu.Text
dotnet add package Bodu.Numerics
dotnet add package Bodu.Financial

# Companions
dotnet add package Bodu.Globalization.Calendar.Builder
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
dotnet add package Bodu.Globalization.Calendar.Plugins
dotnet add package Bodu.Financial.DependencyInjection

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
