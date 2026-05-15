---
title: Introduction
---

# Introduction

**Bodu** is a solution that ships six independent .NET NuGet packages, each focused on a narrow, well-defined problem domain. The packages share nothing at runtime — every assembly is self-contained — but they share a single set of source and documentation conventions, a single analyzer and test configuration, and a single quality bar.

If you are new to Bodu, start with the **library introductions** below to understand what each package is for. The deeper conceptual map across the hashing and cryptography libraries lives in [Algorithm families](algorithm-families.md).

## The libraries at a glance

| Package | What it provides | Target framework |
|---|---|---|
| **[Bodu.Core](core/index.md)** | Bounded collections (`CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey,TValue>`), a day-of-week `WeekPattern` value type, pooled buffers, and a comprehensive set of date, numeric, span, and text extensions sitting on a centralized `ThrowHelper`. | `net8.0` |
| **[Bodu.IO.Hashing](io-hashing/index.md)** | Non-cryptographic hashing on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract — fingerprints (FNV, CityHash, MurmurHash3, Pearson, Bernstein and the classic string hashes), checksums (CRC, Fletcher, Adler), and check digits (Luhn, Damm, Verhoeff, IBAN, ISBN, …). Nothing here is safe against an adversary; everything is fast and portable. | `net8.0` |
| **[Bodu.Security.Cryptography](cryptography/index.md)** | Cryptographic primitives on the BCL <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType> and <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> contracts — managed block ciphers (Threefish, Serpent, Camellia, Twofish, Blowfish, Skipjack), AES paired with six AEAD mode transforms (GCM, CCM, OCB, EAX, SIV, GCM-SIV), keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru, Whirlpool, BLAKE2/3, Skein, Shake), Merkle-tree hashing, and the full ASCON family. | `net8.0` |
| **[Bodu.Globalization.Calendar](calendar/index.md)** | Rule-driven notable-date resolution — public holidays, observances, religious festivals — for any year, territory, or calendar system. Built-in algorithms cover Gregorian and Orthodox Easter, Hindu Lunar dates, Losar, Vesak, Asalha Puja, and Qingming, with a pluggable algorithm registry, observance-adjustment pipeline, and trust-policy-driven plugin host. | `net8.0` |
| **[Bodu.Text.Encoding](text-encoding/index.md)** | Binary-to-text encoders for Base16, Base32, Base64, Base58, and Base85 with every common variant (RFC 4648 standard / hex-extended / URL-safe / MIME, Crockford, z-base-32, Bitcoin/Flickr / Ripple, Ascii85 / Z85). Each encoding exposes the same modern API shape: span- and UTF-8-friendly overloads, `OperationStatus` streaming, length-prediction helpers, validation predicates, plus a unified `IBinaryEncoding` interface for runtime-pluggable encoding choice. | `net8.0` |
| **[Bodu.Text.Formats](formats/index.md)** | Self-framing binary serialization formats with a strongly-typed value model and a span- and stream-friendly codec. Ships **Bencode** (the BitTorrent serialization grammar from BEP 3) as the first format — `Bencode.Encode` / `Decode` / `TryEncode` / `TryDecode` / `GetEncodedLength` over `ReadOnlySpan<byte>`, `byte[]`, and `Stream`; an immutable `BencodedValue` tree (`Integer`, `String`, `List`, `Dictionary`); and BEP 3 canonicality enforcement on both sides of the pipeline. | `net8.0` |

Each package is versioned and released independently. Take the one you need and ignore the others — there are no cross-package runtime dependencies. `Bodu.IO.Hashing`, `Bodu.Security.Cryptography`, `Bodu.Text.Encoding`, and `Bodu.Text.Formats` all depend on `Bodu.Core` for shared argument-validation helpers; `Bodu.Text.Formats` additionally references `Bodu.Text.Encoding`.

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
    <a href="../apidoc/Bodu.Collections.Generic.md">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/index.md">Bodu.IO.Hashing</a></h3>
  <p>Non-cryptographic hashes — fingerprints, checksums, and human-readable check digits. Optimized for speed, portability, and error-detection coverage rather than adversary resistance.</p>
  <div class="bodu-card-links">
    <a href="io-hashing/index.md">Introduction</a>
    <a href="io-hashing/getting-started.md">Getting started</a>
    <a href="../guides/io-hashing/index.md">Guides</a>
    <a href="../apidoc/Bodu.IO.Hashing.md">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/index.md">Bodu.Security.Cryptography</a></h3>
  <p>Block ciphers, authenticated encryption, keyed hashes, and cryptographic digests with a formal adversary model. Drops into any API that expects <code>SymmetricAlgorithm</code> or <code>HashAlgorithm</code>.</p>
  <div class="bodu-card-links">
    <a href="cryptography/index.md">Introduction</a>
    <a href="cryptography/getting-started.md">Getting started</a>
    <a href="../guides/cryptography/index.md">Guides</a>
    <a href="../apidoc/Bodu.Security.Cryptography.md">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="calendar/index.md">Bodu.Globalization.Calendar</a></h3>
  <p>Notable-date resolution and dynamic calendar calculators driven from pluggable XML or JSON rule sources, with an observance-adjustment pipeline, plugin host, and territory filtering.</p>
  <div class="bodu-card-links">
    <a href="calendar/index.md">Introduction</a>
    <a href="calendar/getting-started.md">Getting started</a>
    <a href="../guides/calendar/index.md">Guides</a>
    <a href="../apidoc/Bodu.Globalization.Calendar.md">API reference</a>
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
  <p>Self-framing binary serialization formats with a strongly-typed value tree and a span- and stream-friendly codec. Ships Bencode (BitTorrent BEP 3) as the first format, with full canonicality enforcement on both sides of the pipeline.</p>
  <div class="bodu-card-links">
    <a href="formats/index.md">Introduction</a>
    <a href="formats/getting-started.md">Getting started</a>
    <a href="../guides/formats/index.md">Guides</a>
  </div>
</div>

</div>

## Cross-library map: which library do I need?

![Algorithm taxonomy — family hierarchy across both hashing libraries](../images/diagrams/algorithm-taxonomy.svg)

If your problem touches **hashing**, **checksums**, or **encryption**, the [Algorithm families](algorithm-families.md) page maps the six algorithm families across `Bodu.IO.Hashing` and `Bodu.Security.Cryptography` and tells you which type to reach for.

| You need… | Reach for | Library |
|---|---|---|
| A fixed-capacity ring buffer, deque, or evicting cache | `CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<K,V>` | Bodu.Core |
| A day-of-week bitmask value type | `WeekPattern` | Bodu.Core |
| A standard on-the-wire checksum (CRC, Adler-32, Fletcher) | `Crc` + `CrcStandard`, `Adler32`, `Fletcher32` | Bodu.IO.Hashing |
| A fast hash-table fingerprint | `Fnv1a64`, `CityHash64`, `MurmurHash3_128` | Bodu.IO.Hashing |
| Validation of a credit card, IBAN, ISBN, GTIN, … | `Luhn`, `Iban`, `Isbn13`, `Gtin14` | Bodu.IO.Hashing |
| Encryption of data under a key | `Threefish*`, `Serpent*`, `Camellia`, `Twofish`, `Blowfish`, `AesBlockCipher` | Bodu.Security.Cryptography |
| Authenticated encryption (encrypt + integrity in one) | `AesBlockCipher` + `GcmModeTransform`, `AsconAead128` | Bodu.Security.Cryptography |
| Keyed hash / message authentication | `SipHash64` / `SipHash128`, `Poly1305` | Bodu.Security.Cryptography |
| Cryptographic digest for content addressing | `Tiger`, `CubeHash`, `AsconHash256`, `Whirlpool`, `Blake2b` | Bodu.Security.Cryptography |
| Resolve a public holiday, observance, or religious festival | `NotableDateService` + `NotableDateRule` | Bodu.Globalization.Calendar |
| Encode a hash digest as hex / Base32 / Base64 | `Base16`, `Base32`, `Base64` | Bodu.Text.Encoding |
| Decode a JWT token segment (URL-safe Base64) | `Base64.Decode(token, Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding)` | Bodu.Text.Encoding |
| Parse a Bitcoin / IPFS / Solana identifier | `Base58.Decode(id)` | Bodu.Text.Encoding |
| Format a TOTP / HOTP shared secret for a user | `Base32.Encode(secret, Base32Variant.Standard, BaseFormattingOptions.OmitPadding)` | Bodu.Text.Encoding |
| Decode a `.torrent` file or other Bencode payload | `Bencode.Decode(bytes)` | Bodu.Text.Formats |
| Canonically re-encode a Bencode value tree | `Bencode.Encode(tree)` | Bodu.Text.Formats |

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
- **[Algorithm families](algorithm-families.md)** — the cross-library taxonomy of fingerprints, checksums, check digits, cryptographic hashes, keyed hashes, and symmetric ciphers.
- **Library introductions:** [Bodu.Core](core/index.md) · [Bodu.IO.Hashing](io-hashing/index.md) · [Bodu.Security.Cryptography](cryptography/index.md) · [Bodu.Globalization.Calendar](calendar/index.md) · [Bodu.Text.Encoding](text-encoding/index.md) · [Bodu.Text.Formats](formats/index.md).
- **API references:** [Bodu.Collections.Generic](../apidoc/Bodu.Collections.Generic.md) · [Bodu.IO.Hashing](../apidoc/Bodu.IO.Hashing.md) · [Bodu.Security.Cryptography](../apidoc/Bodu.Security.Cryptography.md) · [Bodu.Globalization.Calendar](../apidoc/Bodu.Globalization.Calendar.md).
