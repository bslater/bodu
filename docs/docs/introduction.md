---
title: Introduction
---

# Introduction

**Bodu** is a solution that ships a family of independent .NET NuGet packages, each focused on a narrow, well-defined problem domain. Every package is versioned and released on its own — and most are self-contained, with the few cross-package dependencies listed below — but they share a single set of source and documentation conventions, a single analyzer and test configuration, and a single quality bar.

The suite is organized into **seven topics**. Each topic groups the packages that solve related problems, and each has a dedicated overview page explaining the collective purpose of its members and how they fit together. If you are new to Bodu, start with the topic that matches your problem, then drill into the member library's introduction.

## The suite in seven topics

### [Core Foundations](topics/core-foundations.md)

The foundation every other package builds on — collections, buffers, extensions, argument validation, and text-encoding utilities.

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.Core](core/index.md)** | The foundation package — a day-of-week `WeekPattern` value type, pooled buffers, async coordination primitives, railway outcomes (`Option<T>` / `Result<T>` / `Either<TLeft,TRight>`), and a comprehensive set of date, numeric, span, and text extensions sitting on a centralized `ThrowHelper`. | `net8.0` |
| **[Bodu.Collections](collections/index.md)** | The specialized collection catalogue (depends on `Bodu.Core`; namespaces unchanged) — fixed-capacity rings (`CircularBuffer<T>`, `Deque<T>`), policy-driven caches (`EvictingDictionary<TKey,TValue>` with TTL expiry), navigable sets/dictionaries with rank/select, range-keyed lookups and overlap-storing interval trees, graphs, tries and multi-pattern text search, and the probabilistic sketches. | `net8.0` |
| **[Bodu.Collections.Concurrent](collections-concurrent/index.md)** | The thread-safe collection companion (depends on `Bodu.Collections`) — the lock-free `ConcurrentCircularBuffer<T>` (Vyukov MPMC, `IProducerConsumerCollection<T>`) and the lock-free split-ordered `ConcurrentHashSet<T>` with snapshot enumeration. | `net8.0` |
| **[Bodu.Text](text/index.md)** *(namespace in Bodu.Core)* | Encoding-detection and text / byte conversion helpers over `System.Text.Encoding` — BOM-based `EncodingDetection`, plus `EncodingExtensions` and `StringEncodingExtensions` for span-, UTF-8-, and pooled-buffer-friendly transcoding, preamble handling, and validation. | `net8.0` |

### [Hashing & Cryptography](topics/hashing-and-cryptography.md)

Two packages split by a single question — *is there an adversary?* Non-cryptographic fingerprints, checksums, and check digits on one side; ciphers, AEAD, MACs, digests, and KDFs on the other.

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.IO.Hashing](io-hashing/index.md)** | Non-cryptographic hashing on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract — fingerprints (FNV, CityHash, MurmurHash3, Pearson, Bernstein and the classic string hashes), checksums (CRC, Fletcher, Adler), and check digits (Luhn, Damm, Verhoeff, IBAN, ISBN, …). Nothing here is safe against an adversary; everything is fast and portable. | `net8.0` |
| **[Bodu.Security.Cryptography](cryptography/index.md)** | Cryptographic primitives on the BCL <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType> and <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> contracts — managed block ciphers (Threefish, Serpent, Camellia, Twofish, Blowfish, Skipjack), AES paired with six AEAD mode transforms (GCM, CCM, OCB, EAX, SIV, GCM-SIV), keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru, Whirlpool, BLAKE2/3, Skein, Shake), Merkle-tree hashing, and the full ASCON family. | `net8.0` |

### [Globalization & Calendars](topics/globalization-and-calendars.md)

A resource-driven notable-date engine plus an ecosystem of opt-in companions (fluent authoring, dependency injection, trust-gated plugins) and per-region holiday data packs.

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.Globalization.Calendar](calendar/index.md)** | Rule-driven notable-date resolution — public holidays, observances, religious festivals — for any year, territory, or calendar system. Built-in algorithms cover Gregorian and Orthodox Easter, Hindu Lunar dates, Losar, Vesak, Asalha Puja, and Qingming, with a pluggable algorithm registry, observance-adjustment pipeline, and trust-policy-driven plugin host. Companion packages add fluent authoring (`…Builder`), `IServiceCollection` registration (`…DependencyInjection`), plugin loading (`…Plugins`), and five regional data packs. | `net8.0` |

### [Text & Serialization](topics/text-and-serialization.md)

Three different jobs that all sound like "text": binary-to-text codecs, document formats, and object serializers.

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.Text.Encoding](text-encoding/index.md)** | Binary-to-text encoders for Base16, Base32, Base64, Base58, and Base85 with every common variant (RFC 4648 standard / hex-extended / URL-safe / MIME, Crockford, z-base-32, Bitcoin/Flickr / Ripple, Ascii85 / Z85). Each encoding exposes the same modern API shape: span- and UTF-8-friendly overloads, `OperationStatus` streaming, length-prediction helpers, validation predicates, plus a unified `IBinaryEncoding` interface for runtime-pluggable encoding choice. | `net8.0` |
| **[Bodu.Text.Filtering](text-filtering/index.md)** | A high-performance include/exclude filtering engine for lists of text values. Glob (wildcard, character-class, `{a,b}` alternation) and regex patterns compile once into an immutable `TextFilter` that runs the cheapest matching strategies first; choose Ant / MSBuild-style `AnyMatch` sets or gitignore-style `LastMatchWins` ordered rules, parse raw lines with the gitignore conventions, and observe decisions through built-in statistics and a per-decision observer. | `net8.0` |
| **[Bodu.Text.Formats](formats/index.md)** | Self-framing text document formats with strongly-typed value models and span- and stream-friendly codecs. Ships three sibling namespaces — **Delimited** (CSV / TSV), **DotEnv**, and **Ini** — each with `Parse` / `Format` and `Try*` overloads, a typed value model, and strict invariant enforcement. | `net8.0` |
| **[Bodu.Text.Bencode](serialization/bencode/index.md)** · **[Bodu.Text.Toml](serialization/toml/index.md)** · **[Bodu.Text.Yaml](serialization/yaml/index.md)** | Three self-contained serializers that map your own types to and from a format — a shared architecture and `System.Text.Json`-aligned shape, each shipping a `…Serializer`, a mutable `…Node` and a read-only `…Document` DOM, and a low-level `Utf8…Reader` / `Utf8…Writer` pair. **Bencode** covers BitTorrent BEP 3; **TOML** covers v1.0.0 / v1.1.0; **YAML** the 1.2 core schema with block / flow collections, anchors, and multi-document streams. See the [shared family introduction](serialization/index.md). | `net8.0` |

### [Configuration](topics/configuration.md)

Layered, EditorConfig-style configuration — a parser/resolver plus a bridge into the `Microsoft.Extensions.Configuration` pipeline.

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.Text.Configuration](text-configuration/index.md)** | EditorConfig-style configuration layering over an INI document model. Layers a preamble plus glob-anchored sections in source order for a target file path, then projects the result into a flat, colon-delimited `ConfigurationView` with typed accessors (`GetInt32`, `GetEnum<T>`, `GetValue<T>`). Profile presets, optional diagnostic collection, and byte-faithful round-trip save. | `net8.0` |
| **[Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md)** | Bridges `Bodu.Text.Configuration` to `Microsoft.Extensions.Configuration`. Adds `AddTextConfiguration*` entry points on `IConfigurationBuilder` — mirroring `AddJsonFile` / `AddJsonStream` — so a Bodu configuration file layers alongside JSON, INI, XML, and environment-variable sources, with `IOptions<T>` binding and reload-on-change support. | `net8.0` |

### [Numerics & Financial](topics/numerics-and-financial.md)

Exact arithmetic — rational numbers and intervals, and the money, currency, and exchange-rate primitives built on top of them.

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.Numerics](numerics/index.md)** | Generic-math value primitives — `Fraction<T>` for exact rational arithmetic over any `IBinaryInteger<T>` with canonical-form auto-reduction and `BigInteger`-promoted intermediates, and `Interval<T>` for closed / open / half-open bounded intervals with intersection, union, and adjacency. | `net8.0` |
| **[Bodu.Financial](financial/index.md)** | Type-safe monetary primitives — `Money<TCurrency>` (currency as type parameter, so cross-currency arithmetic fails the build), `Money` for runtime-tagged scenarios, `MoneyBag` for multi-currency portfolios, the ISO 4217 currency catalogue, exchange-rate providers, allocation, and cash rounding. | `net8.0` |

### [Binary Formats & I/O](topics/binary-formats.md)

Legacy binary container and document formats — a general-purpose compound-file container (read, edit, and author) with narrower read-only format readers layered on top.

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.IO.Compound](io-compound/index.md)** | A reader, editor, and writer for the OLE2 / Compound File Binary (CFB) container — the structured-storage "file system in a file" behind legacy Office documents (`.xls`, `.doc`, `.ppt`, `.msg`). Navigates the `RootStorage` hierarchy, reads each named stream through a seekable `CompoundStream` cursor (buffered or on-demand), edits and authors containers with a transactional `Commit` / `CommitAsync`, and reads and writes the OLE summary-information property sets. | `net8.0` |
| **[Bodu.Formats.Excel.Binary](excel/index.md)** | A narrow, read-only BIFF8 (`.xls`) reader built on `Bodu.IO.Compound` that surfaces raw worksheet cell values — strings, numbers, booleans, and errors — without formula evaluation, styling, or higher-level interpretation. | `net8.0` |

Each package is versioned and released independently — take the one you need and ignore the others. The only shared runtime dependency is `Bodu.Core`, whose `ThrowHelper` provides argument validation for `Bodu.Collections`, `Bodu.IO.Hashing`, `Bodu.Security.Cryptography`, `Bodu.Text.Encoding`, `Bodu.Text.Formats`, `Bodu.Text.Configuration`, `Bodu.Extensions.Configuration.Text`, `Bodu.Text`, `Bodu.Numerics`, `Bodu.Financial`, and `Bodu.IO.Compound`. Beyond that, `Bodu.Collections` builds on `Bodu.Core`, and `Bodu.Collections.Concurrent` builds on `Bodu.Collections`; `Bodu.Text.Formats` references `Bodu.Text.Encoding`; `Bodu.Text.Configuration` builds on `Bodu.Text.Formats`; `Bodu.Extensions.Configuration.Text` builds on `Bodu.Text.Configuration` plus `Microsoft.Extensions.Configuration`; `Bodu.Financial` builds on `Bodu.Numerics` for its `Fraction<BigInteger>` precision escape hatch; and `Bodu.Formats.Excel.Binary` builds on `Bodu.IO.Compound` to read BIFF8 `.xls` workbooks.

## Library introductions

Each library has a dedicated introduction page that explains its namespaces, the role of each headline type, and the scenarios it is designed for. Pair it with the matching getting-started page for install commands and a minimal sample. The cards below follow the seven-topic order.

### Core Foundations

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="core/index.md">Bodu.Core</a></h3>
  <p>Day-of-week patterns, pooled buffers, async coordination and railway primitives, and date / numeric / span extensions. Useful in almost any application; depended on internally by every other Bodu package.</p>
  <div class="bodu-card-links">
    <a href="core/index.md">Introduction</a>
    <a href="core/getting-started.md">Getting started</a>
    <a href="../guides/core/index.md">Guides</a>
    <a href="xref:Bodu">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="collections/index.md">Bodu.Collections</a></h3>
  <p>The specialized collection catalogue — bounded rings, eviction-aware caches with TTL expiry, navigable and range-keyed lookups, interval trees, graphs, tries, and probabilistic sketches. Depends on <code>Bodu.Core</code>; namespaces unchanged.</p>
  <div class="bodu-card-links">
    <a href="collections/index.md">Introduction</a>
    <a href="collections/getting-started.md">Getting started</a>
    <a href="../guides/core/index.md">Guides</a>
    <a href="xref:Bodu.Collections.Generic">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="collections-concurrent/index.md">Bodu.Collections.Concurrent</a></h3>
  <p>The thread-safe collection companion — a lock-free MPMC <code>ConcurrentCircularBuffer&lt;T&gt;</code> and a lock-free split-ordered <code>ConcurrentHashSet&lt;T&gt;</code>, both with snapshot enumeration. Depends on <code>Bodu.Collections</code>.</p>
  <div class="bodu-card-links">
    <a href="collections-concurrent/index.md">Introduction</a>
    <a href="collections-concurrent/getting-started.md">Getting started</a>
    <a href="../guides/core/concurrent-collections.md">Guides</a>
    <a href="xref:Bodu.Collections.Generic.Concurrent">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="text/index.md">Bodu.Text</a></h3>
  <p>Encoding detection and ergonomic text / byte conversion over <code>System.Text.Encoding</code> — BOM-based <code>EncodingDetection</code>, plus span-, UTF-8-, and pooled-buffer-friendly <code>EncodingExtensions</code> and <code>StringEncodingExtensions</code>.</p>
  <div class="bodu-card-links">
    <a href="text/index.md">Introduction</a>
    <a href="xref:Bodu.Text">API reference</a>
  </div>
</div>

</div>

### Hashing & Cryptography

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/index.md">Bodu.IO.Hashing</a></h3>
  <p>Non-cryptographic hashes — fingerprints, checksums, and human-readable check digits. Optimized for speed, portability, and error-detection coverage rather than adversary resistance.</p>
  <div class="bodu-card-links">
    <a href="io-hashing/index.md">Introduction</a>
    <a href="io-hashing/getting-started.md">Getting started</a>
    <a href="../guides/io-hashing/index.md">Guides</a>
    <a href="xref:Bodu.IO.Hashing">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/index.md">Bodu.Security.Cryptography</a></h3>
  <p>Block ciphers, authenticated encryption, keyed hashes, and cryptographic digests with a formal adversary model. Drops into any API that expects <code>SymmetricAlgorithm</code> or <code>HashAlgorithm</code>.</p>
  <div class="bodu-card-links">
    <a href="cryptography/index.md">Introduction</a>
    <a href="cryptography/getting-started.md">Getting started</a>
    <a href="../guides/cryptography/index.md">Guides</a>
    <a href="xref:Bodu.Security.Cryptography">API reference</a>
  </div>
</div>

</div>

### Globalization & Calendars

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="calendar/index.md">Bodu.Globalization.Calendar</a></h3>
  <p>Notable-date resolution and dynamic calendar calculators driven from pluggable XML or JSON rule sources, with an observance-adjustment pipeline, plugin host, and territory filtering.</p>
  <div class="bodu-card-links">
    <a href="calendar/index.md">Introduction</a>
    <a href="calendar/getting-started.md">Getting started</a>
    <a href="../guides/calendar/index.md">Guides</a>
    <a href="xref:Bodu.Globalization.Calendar">API reference</a>
  </div>
</div>

</div>

### Text & Serialization

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-encoding/index.md">Bodu.Text.Encoding</a></h3>
  <p>Binary-to-text encoders for Base16, Base32, Base64, Base58, and Base85 with every common variant. Span-, UTF-8-, and <code>OperationStatus</code>-friendly; unified <code>IBinaryEncoding</code> interface for runtime-pluggable choice.</p>
  <div class="bodu-card-links">
    <a href="text-encoding/index.md">Introduction</a>
    <a href="text-encoding/getting-started.md">Getting started</a>
    <a href="../guides/text-encoding/index.md">Guides</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="text-filtering/index.md">Bodu.Text.Filtering</a></h3>
  <p>Include/exclude text filtering: glob and regex patterns compiled into a cost-tiered <code>TextFilter</code>, with Ant / MSBuild set semantics or gitignore-style ordered rules, gitignore-convention parsing, and built-in match telemetry.</p>
  <div class="bodu-card-links">
    <a href="text-filtering/index.md">Introduction</a>
    <a href="text-filtering/getting-started.md">Getting started</a>
    <a href="../guides/text-filtering/index.md">Guides</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="formats/index.md">Bodu.Text.Formats</a></h3>
  <p>Self-framing text document formats with strongly-typed value models and span- and stream-friendly codecs. Ships Delimited (CSV / TSV), DotEnv, and Ini as sibling namespaces, each with strict invariant enforcement.</p>
  <div class="bodu-card-links">
    <a href="formats/index.md">Introduction</a>
    <a href="formats/getting-started.md">Getting started</a>
    <a href="../guides/formats/index.md">Guides</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="serialization/index.md">Bodu serializers — Bencode, TOML &amp; YAML</a></h3>
  <p>Three self-contained serializers — POCO ↔ format — for Bencode (BEP 3), TOML, and YAML. A shared architecture, each with a serializer, a mutable and a read-only DOM, and a low-level <code>Utf8…Reader</code> / <code>Utf8…Writer</code> pair.</p>
  <div class="bodu-card-links">
    <a href="serialization/index.md">Introduction</a>
    <a href="serialization/bencode/index.md">Bencode</a>
    <a href="serialization/toml/index.md">TOML</a>
    <a href="serialization/yaml/index.md">YAML</a>
    <a href="../guides/serialization/index.md">Guides</a>
  </div>
</div>

</div>

### Configuration

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-configuration/index.md">Bodu.Text.Configuration</a></h3>
  <p>EditorConfig-style configuration layering over an INI document model — glob-anchored sections resolved for a target path into a flat, typed <code>ConfigurationView</code>, with profile presets, diagnostic collection, and round-trip save.</p>
  <div class="bodu-card-links">
    <a href="text-configuration/index.md">Introduction</a>
    <a href="text-configuration/getting-started.md">Getting started</a>
    <a href="../guides/text-configuration/index.md">Guides</a>
    <a href="xref:Bodu.Text.Configuration">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="extensions-configuration-text/index.md">Bodu.Extensions.Configuration.Text</a></h3>
  <p>The <code>Microsoft.Extensions.Configuration</code> bridge — an <code>AddTextConfiguration*</code> builder entry point that layers a Bodu configuration file alongside JSON, INI, and environment-variable sources with <code>IOptions&lt;T&gt;</code> binding.</p>
  <div class="bodu-card-links">
    <a href="extensions-configuration-text/index.md">Introduction</a>
    <a href="extensions-configuration-text/getting-started.md">Getting started</a>
    <a href="../guides/extensions-configuration-text/index.md">Guides</a>
    <a href="xref:Bodu.Extensions.Configuration.Text">API reference</a>
  </div>
</div>

</div>

### Numerics & Financial

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="numerics/index.md">Bodu.Numerics</a></h3>
  <p>Generic-math value primitives — <code>Fraction&lt;T&gt;</code> for exact rational arithmetic and <code>Interval&lt;T&gt;</code> for closed / open / half-open bounded intervals, both over the <code>INumber&lt;T&gt;</code> abstractions.</p>
  <div class="bodu-card-links">
    <a href="numerics/index.md">Introduction</a>
    <a href="numerics/getting-started.md">Getting started</a>
    <a href="../guides/numerics/index.md">Guides</a>
    <a href="xref:Bodu.Numerics">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="financial/index.md">Bodu.Financial</a></h3>
  <p>Type-safe monetary primitives — <code>Money&lt;TCurrency&gt;</code> with compile-time currency safety, <code>Money</code> for runtime-tagged scenarios, the ISO 4217 catalogue, exchange-rate providers, allocation, and cash rounding.</p>
  <div class="bodu-card-links">
    <a href="financial/index.md">Introduction</a>
    <a href="financial/getting-started.md">Getting started</a>
    <a href="../guides/financial/index.md">Guides</a>
    <a href="xref:Bodu.Financial">API reference</a>
  </div>
</div>

</div>

### Binary Formats & I/O

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-compound/index.md">Bodu.IO.Compound</a></h3>
  <p>An OLE2 / Compound File Binary (CFB) container reader, editor, and writer — navigate the storage hierarchy, read each named stream through a seekable <code>CompoundStream</code> cursor, edit or author containers with a transactional commit, and read and write the OLE property sets. The BIFF8 <code>.xls</code> reader <code>Bodu.Formats.Excel.Binary</code> is built on it.</p>
  <div class="bodu-card-links">
    <a href="io-compound/index.md">Introduction</a>
    <a href="io-compound/getting-started.md">Getting started</a>
    <a href="../guides/io-compound/index.md">Guides</a>
    <a href="xref:Bodu.IO.Compound">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="excel/index.md">Bodu.Formats.Excel.Binary</a></h3>
  <p>A narrow, read-only BIFF8 (<code>.xls</code>) reader built on <code>Bodu.IO.Compound</code> — surface raw worksheet cell values (strings, numbers, booleans, errors, and a formula's cached result) through a forward-only <code>ExcelWorksheetReader</code> or a randomly addressable <code>ExcelWorksheet</code>, with date-format detection and serial-date conversion, without formula evaluation or styling.</p>
  <div class="bodu-card-links">
    <a href="excel/index.md">Introduction</a>
    <a href="excel/getting-started.md">Getting started</a>
    <a href="../guides/excel/index.md">Guides</a>
    <a href="xref:Bodu.Formats.Excel">API reference</a>
  </div>
</div>

</div>

## Design principles

- **Small by intent.** Each library solves one coherent problem. If something already fits well elsewhere in .NET, we don't duplicate it.
- **Nullable reference types** are enabled solution-wide. Public APIs make their null-intent explicit.
- **Analyzer-clean.** StyleCop.Analyzers, Roslynator, the .NET analyzers, AsyncFixer, and the Visual Studio Threading analyzers run at build time. Doc-comment warnings — including `CS1591` — are treated as errors.
- **Deterministic builds** produce reproducible package outputs.
- **Documentation-first.** Every public type and member carries XML documentation in US English, and that documentation is the source of truth for this site. The API reference you see here is generated directly from the source.
- **MIT licensed**, no external runtime dependencies beyond the BCL.

## Testing and conventions

The solution uses **MSTest** with a partial-class test layout that mirrors the source layout one-to-one. Test methods follow the naming convention `<MethodOrProperty>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>` and carry an XML `<summary>` that starts with "Verifies that …", which makes test intent readable directly in the test explorer without opening the test body.

## Where to go next

- **Topic overviews:** [Core Foundations](topics/core-foundations.md) · [Hashing & Cryptography](topics/hashing-and-cryptography.md) · [Globalization & Calendars](topics/globalization-and-calendars.md) · [Text & Serialization](topics/text-and-serialization.md) · [Configuration](topics/configuration.md) · [Numerics & Financial](topics/numerics-and-financial.md) · [Binary Formats & I/O](topics/binary-formats.md).
- **[Getting started](getting-started.md)** — prerequisites, install commands, and a one-minute sample from each library.
- **[Package matrix](package-matrix.md)** — the authoritative package list with status, dependencies, and install commands.
- **Library introductions:** [Bodu.Core](core/index.md) · [Bodu.Collections](collections/index.md) · [Bodu.Collections.Concurrent](collections-concurrent/index.md) · [Bodu.IO.Hashing](io-hashing/index.md) · [Bodu.Security.Cryptography](cryptography/index.md) · [Bodu.Globalization.Calendar](calendar/index.md) · [Bodu.Text.Encoding](text-encoding/index.md) · [Bodu.Text.Filtering](text-filtering/index.md) · [Bodu.Text.Formats](formats/index.md) · [Bodu.Text.Bencode](serialization/bencode/index.md) · [Bodu.Text.Toml](serialization/toml/index.md) · [Bodu.Text.Yaml](serialization/yaml/index.md) · [Bodu.Text.Configuration](text-configuration/index.md) · [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md) · [Bodu.Text](text/index.md) · [Bodu.Numerics](numerics/index.md) · [Bodu.Financial](financial/index.md) · [Bodu.IO.Compound](io-compound/index.md) · [Bodu.Formats.Excel.Binary](excel/index.md).
- **API references:** [Bodu.Collections.Generic](xref:Bodu.Collections.Generic) · [Bodu.IO.Hashing](xref:Bodu.IO.Hashing) · [Bodu.Security.Cryptography](xref:Bodu.Security.Cryptography) · [Bodu.Globalization.Calendar](xref:Bodu.Globalization.Calendar) · [Bodu.Text](xref:Bodu.Text) · [Bodu.Numerics](xref:Bodu.Numerics) · [Bodu.Financial](xref:Bodu.Financial) · [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.Formats.Excel](xref:Bodu.Formats.Excel).
