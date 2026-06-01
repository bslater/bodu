---
title: Introduction
---

# Introduction

**Bodu** is a solution that ships eight independent .NET NuGet packages, each focused on a narrow, well-defined problem domain. Every package is versioned and released on its own — and most are self-contained, with the few cross-package dependencies listed below — but they share a single set of source and documentation conventions, a single analyzer and test configuration, and a single quality bar.

If you are new to Bodu, start with the **library introductions** below to understand what each package is for. Each links to a dedicated introduction page that maps its namespaces and headline types, and to the matching getting-started page.

## The libraries at a glance

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.Core](core/index.md)** | Bounded collections (`CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey,TValue>`), a day-of-week `WeekPattern` value type, pooled buffers, and a comprehensive set of date, numeric, span, and text extensions sitting on a centralized `ThrowHelper`. | `net8.0` |
| **[Bodu.IO.Hashing](io-hashing/index.md)** | Non-cryptographic hashing on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract — fingerprints (FNV, CityHash, MurmurHash3, Pearson, Bernstein and the classic string hashes), checksums (CRC, Fletcher, Adler), and check digits (Luhn, Damm, Verhoeff, IBAN, ISBN, …). Nothing here is safe against an adversary; everything is fast and portable. | `net8.0` |
| **[Bodu.Security.Cryptography](cryptography/index.md)** | Cryptographic primitives on the BCL <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType> and <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> contracts — managed block ciphers (Threefish, Serpent, Camellia, Twofish, Blowfish, Skipjack), AES paired with six AEAD mode transforms (GCM, CCM, OCB, EAX, SIV, GCM-SIV), keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru, Whirlpool, BLAKE2/3, Skein, Shake), Merkle-tree hashing, and the full ASCON family. | `net8.0` |
| **[Bodu.Globalization.Calendar](calendar/index.md)** | Rule-driven notable-date resolution — public holidays, observances, religious festivals — for any year, territory, or calendar system. Built-in algorithms cover Gregorian and Orthodox Easter, Hindu Lunar dates, Losar, Vesak, Asalha Puja, and Qingming, with a pluggable algorithm registry, observance-adjustment pipeline, and trust-policy-driven plugin host. | `net8.0` |
| **[Bodu.Text.Encoding](text-encoding/index.md)** | Binary-to-text encoders for Base16, Base32, Base64, Base58, and Base85 with every common variant (RFC 4648 standard / hex-extended / URL-safe / MIME, Crockford, z-base-32, Bitcoin/Flickr / Ripple, Ascii85 / Z85). Each encoding exposes the same modern API shape: span- and UTF-8-friendly overloads, `OperationStatus` streaming, length-prediction helpers, validation predicates, plus a unified `IBinaryEncoding` interface for runtime-pluggable encoding choice. | `net8.0` |
| **[Bodu.Text.Formats](formats/index.md)** | Self-framing text and binary serialization formats with strongly-typed value models and span- and stream-friendly codecs. Ships four sibling namespaces — **Bencode** (the BitTorrent BEP 3 grammar), **Delimited** (CSV / TSV), **Ini**, and **DotEnv** — each with `Encode` / `Decode` (or `Parse` / `Format`) and `Try*` overloads, an immutable value tree, and strict invariant enforcement on both sides of the pipeline. | `net8.0` |
| **[Bodu.Text.Configuration](text-configuration/index.md)** | EditorConfig-style configuration layering over an INI document model. Layers a preamble plus glob-anchored sections in source order for a target file path, then projects the result into a flat, colon-delimited `ConfigurationView` with typed accessors (`GetInt32`, `GetEnum<T>`, `GetValue<T>`). Profile presets, optional diagnostic collection, and byte-faithful round-trip save. | `net8.0` |
| **[Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md)** | Bridges `Bodu.Text.Configuration` to `Microsoft.Extensions.Configuration`. Adds an `AddConfiguration` entry point on `IConfigurationBuilder` — mirroring `AddJsonFile` / `AddJsonStream` — so a Bodu configuration file layers alongside JSON, INI, XML, and environment-variable sources, with `IOptions<T>` binding and reload-on-change support. | `net8.0` |

Each package is versioned and released independently — take the one you need and ignore the others. The only shared runtime dependency is `Bodu.Core`, whose `ThrowHelper` provides argument validation for `Bodu.IO.Hashing`, `Bodu.Security.Cryptography`, `Bodu.Text.Encoding`, `Bodu.Text.Formats`, `Bodu.Text.Configuration`, and `Bodu.Extensions.Configuration.Text`. Beyond that, `Bodu.Text.Formats` references `Bodu.Text.Encoding`; `Bodu.Text.Configuration` builds on `Bodu.Text.Formats`; and `Bodu.Extensions.Configuration.Text` builds on `Bodu.Text.Configuration` plus `Microsoft.Extensions.Configuration`.

## Library introductions

Each library has a dedicated introduction page that explains its namespaces, the role of each headline type, and the scenarios it is designed for. Pair it with the matching getting-started page for install commands and a minimal sample.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="core/index.md">Bodu.Core</a></h3>
  <p>Bounded collections, eviction-aware caches, day-of-week patterns, pooled buffers, and date / numeric / span extensions. Useful in almost any application; depended on internally by the hashing and cryptography packages.</p>
  <div class="bodu-card-links">
    <a href="core/index.md">Introduction</a>
    <a href="core/getting-started.md">Getting started</a>
    <a href="../guides/core/index.md">Guides</a>
    <a href="xref:Bodu.Collections.Generic">API reference</a>
  </div>
</div>

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
  <h3><a href="formats/index.md">Bodu.Text.Formats</a></h3>
  <p>Self-framing text and binary serialization formats with strongly-typed value trees and span- and stream-friendly codecs. Ships Bencode, Delimited (CSV / TSV), Ini, and DotEnv as sibling namespaces, each with strict invariant enforcement.</p>
  <div class="bodu-card-links">
    <a href="formats/index.md">Introduction</a>
    <a href="formats/getting-started.md">Getting started</a>
    <a href="../guides/formats/index.md">Guides</a>
  </div>
</div>

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
  <p>The <code>Microsoft.Extensions.Configuration</code> bridge — an <code>AddConfiguration</code> builder entry point that layers a Bodu configuration file alongside JSON, INI, and environment-variable sources with <code>IOptions&lt;T&gt;</code> binding.</p>
  <div class="bodu-card-links">
    <a href="extensions-configuration-text/index.md">Introduction</a>
    <a href="extensions-configuration-text/getting-started.md">Getting started</a>
    <a href="../guides/extensions-configuration-text/index.md">Guides</a>
    <a href="xref:Bodu.Extensions.Configuration.Text">API reference</a>
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

- **[Getting started](getting-started.md)** — prerequisites, install commands, and a one-minute sample from each library.
- **Library introductions:** [Bodu.Core](core/index.md) · [Bodu.IO.Hashing](io-hashing/index.md) · [Bodu.Security.Cryptography](cryptography/index.md) · [Bodu.Globalization.Calendar](calendar/index.md) · [Bodu.Text.Encoding](text-encoding/index.md) · [Bodu.Text.Formats](formats/index.md) · [Bodu.Text.Configuration](text-configuration/index.md) · [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md).
- **API references:** [Bodu.Collections.Generic](xref:Bodu.Collections.Generic) · [Bodu.IO.Hashing](xref:Bodu.IO.Hashing) · [Bodu.Security.Cryptography](xref:Bodu.Security.Cryptography) · [Bodu.Globalization.Calendar](xref:Bodu.Globalization.Calendar).
