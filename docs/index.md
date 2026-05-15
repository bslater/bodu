---
title: Bodu — a suite of focused .NET libraries
_disableToc: true
_disableBreadcrumb: true
---

<style>
  .bodu-hero { margin: 1.5rem 0 2.5rem; }
  .bodu-hero h1 { margin: 0 0 .25rem; font-size: 2.2rem; letter-spacing: .5px; }
  .bodu-hero p.tagline { font-size: 1.15rem; opacity: .85; margin: 0 0 .5rem; max-width: 56rem; }
  .bodu-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 1rem; margin: 1.5rem 0; }
  .bodu-card { border: 1px solid var(--theme-border, #334155); border-radius: 10px; padding: 1rem 1.1rem; background: var(--theme-card-bg, rgba(59, 130, 246, 0.04)); }
  .bodu-card h3 { margin: 0 0 .4rem; font-size: 1.05rem; }
  .bodu-card p { margin: 0 0 .6rem; opacity: .9; font-size: .95rem; }
  .bodu-card .bodu-card-links a { margin-right: .9rem; font-size: .9rem; }
  .bodu-card img { display: block; width: 100%; height: auto; border-radius: 6px; margin-bottom: .6rem; }
  .bodu-install pre { margin: .25rem 0; }
  .bodu-nav { display: flex; flex-wrap: wrap; gap: .5rem 1.2rem; margin: 1rem 0 0; font-size: .95rem; }
</style>

<div class="bodu-hero">
  <h1>Bodu</h1>
  <p class="tagline">A suite of small, focused .NET libraries for collections, non-cryptographic hashing, cryptography, calendar computation, and binary-to-text encoding.</p>
</div>

Five independent NuGet packages that share a single solution, a single set of conventions, and a single bar for quality: nullable-enabled, analyzer-clean, deterministic builds, and framework-style XML documentation.

## Libraries

<div class="bodu-cards">

<div class="bodu-card">
  <img src="images/hero-core.svg" alt="Bodu.Core" />
  <h3>Bodu.Core</h3>
  <p>Fixed-capacity collections (<code>CircularBuffer&lt;T&gt;</code>, <code>Deque&lt;T&gt;</code>, <code>EvictingDictionary&lt;TKey,TValue&gt;</code>), a day-of-week <code>WeekPattern</code> value type, pooled buffers, date / numeric / span / array extensions, and a centralized <code>ThrowHelper</code>.</p>
  <div class="bodu-card-links">
    <a href="docs/core/index.md">Introduction</a>
    <a href="guides/core/index.md">Guides</a>
    <a href="apidoc/Bodu.Collections.Generic.md">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <img src="images/hero-io.svg" alt="Bodu.IO.Hashing" />
  <h3>Bodu.IO.Hashing</h3>
  <p>Non-cryptographic hashes on <code>System.IO.Hashing.NonCryptographicHashAlgorithm</code> — the full CRC RevEng catalogue (1–64 bits), Fletcher 16 / 32 / 64, Adler-32 / 32C / 64, FNV, CityHash, MurmurHash3, Pearson, classic string hashes — plus single- and multi-character check digits (Luhn, EAN, IBAN, ISBN, …).</p>
  <div class="bodu-card-links">
    <a href="docs/io-hashing/index.md">Introduction</a>
    <a href="guides/io-hashing/index.md">Guides</a>
    <a href="apidoc/Bodu.IO.Hashing.md">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <img src="images/hero-crypto.svg" alt="Bodu.Security.Cryptography" />
  <h3>Bodu.Security.Cryptography</h3>
  <p>Managed block ciphers (Threefish 256 / 512 / 1024, Serpent 128 / 256 / 512 / 1024, Camellia, Twofish, Blowfish, Skipjack), an AES adapter paired with six AEAD mode transforms (GCM, CCM, OCB, EAX, SIV, GCM-SIV), keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru, Whirlpool, BLAKE2/3, Skein, Shake, ASCON), and Merkle-tree hashing.</p>
  <div class="bodu-card-links">
    <a href="docs/cryptography/index.md">Introduction</a>
    <a href="guides/cryptography/index.md">Guides</a>
    <a href="apidoc/Bodu.Security.Cryptography.md">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <img src="images/hero-calendar.svg" alt="Bodu.Globalization.Calendar" />
  <h3>Bodu.Globalization.Calendar</h3>
  <p>Rule-driven notable-date resolution with fixed, day-of-week-in-month, offset, and algorithm strategies — including Gregorian and Orthodox Easter, Hindu Lunar dates, Losar, Vesak, Asalha Puja, and Qingming — driven from pluggable XML or JSON rule sources and an observance-adjustment pipeline. Region-specific public-holiday rules ship in companion <code>Data.Americas</code>, <code>Data.Europe</code>, and <code>Data.AsiaPacific</code> packs that release independently of the main library.</p>
  <div class="bodu-card-links">
    <a href="docs/calendar/index.md">Introduction</a>
    <a href="guides/calendar/index.md">Guides</a>
    <a href="apidoc/Bodu.Globalization.Calendar.md">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <h3>Bodu.Text.Encoding</h3>
  <p>Binary-to-text encoders for <strong>Base16</strong>, <strong>Base32</strong>, <strong>Base64</strong>, <strong>Base58</strong>, and <strong>Base85</strong> with every common variant — RFC 4648 standard / hex-extended / URL-safe / MIME, Crockford, z-base-32, Bitcoin / Flickr / Ripple, Adobe Ascii85, ZeroMQ Z85. Every encoding exposes the same modern API shape: span- and UTF-8-friendly overloads, <code>OperationStatus</code> streaming methods, length-prediction helpers, validation predicates, plus a unified <code>IBinaryEncoding</code> interface for runtime-pluggable encoding choice.</p>
  <div class="bodu-card-links">
    <a href="docs/text-encoding/index.md">Introduction</a>
    <a href="guides/text-encoding/index.md">Guides</a>
  </div>
</div>

</div>

## Install

<div class="bodu-install">

```bash
dotnet add package Bodu.Core
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Text.Encoding

# Optional region-specific calendar data packs:
dotnet add package Bodu.Globalization.Calendar.Data.Americas
dotnet add package Bodu.Globalization.Calendar.Data.Europe
dotnet add package Bodu.Globalization.Calendar.Data.AsiaPacific
```

</div>

## Design principles

- **Nullable reference types** are enabled throughout. Public APIs declare their null-intent explicitly.
- **Analyzer-clean**: StyleCop, Roslynator, .NET analyzers, AsyncFixer, and Threading analyzers run at build time; doc-comment warnings are treated as errors.
- **Deterministic builds** for reproducible package outputs.
- **Documentation-first**: every public type and member carries XML documentation in US English, which drives this API reference.
- **MIT licensed** and free of external runtime dependencies.

## Where to go next

<div class="bodu-nav">
  <a href="docs/introduction.md">Introduction</a>
  <a href="docs/getting-started.md">Getting started</a>
  <a href="docs/algorithm-families.md">Algorithm families</a>
  <a href="articles/index.md">Articles</a>
  <a href="api/index.html">API reference</a>
</div>
