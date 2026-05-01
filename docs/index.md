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
  <p class="tagline">A suite of small, focused .NET libraries for collections, non-cryptographic hashing, cryptography, and calendar computation.</p>
</div>

Four independent NuGet packages that share a single solution, a single set of conventions, and a single bar for quality: nullable-enabled, analyzer-clean, deterministic builds, and framework-style XML documentation.

## Libraries

<div class="bodu-cards">

<div class="bodu-card">
  <img src="images/hero-core.svg" alt="Bodu.Core" />
  <h3>Bodu.Core</h3>
  <p>Fixed-capacity collections (circular buffer, evicting dictionary), buffer conversion, array and text utilities, and a centralised argument-validation helper.</p>
  <div class="bodu-card-links">
    <a href="api/Bodu.Collections.Generic.html">Overview</a>
    <a href="api/Bodu.html">API reference</a>
  </div>
</div>

<div class="bodu-card">
  <img src="images/hero-io.svg" alt="Bodu.IO.Hashing" />
  <h3>Bodu.IO.Hashing</h3>
  <p>Non-cryptographic checksums built on <code>System.IO.Hashing.NonCryptographicHashAlgorithm</code> — the full CRC RevEng catalogue (widths 1–64 bits) and the Fletcher family (16 / 32 / 64 bits), with shared lookup-table caching and resumable hashing.</p>
  <div class="bodu-card-links">
    <a href="api/Bodu.IO.Hashing.html">Overview</a>
    <a href="guides/io-hashing/index.html">Guides</a>
  </div>
</div>

<div class="bodu-card">
  <img src="images/hero-crypto.svg" alt="Bodu.Security.Cryptography" />
  <h3>Bodu.Security.Cryptography</h3>
  <p>Managed block ciphers (Threefish 256 / 512 / 1024, Serpent 128 / 256 / 512 / 1024, Camellia, Twofish, Blowfish, Skipjack), an AES adapter paired with AEAD mode transforms, keyed and cryptographic hashes (SipHash, Tiger, ASCON), Merkle-tree hashing, and the classic non-cryptographic hash families (Adler, FNV-1a, CityHash).</p>
  <div class="bodu-card-links">
    <a href="api/Bodu.Security.Cryptography.html">Overview</a>
    <a href="guides/cryptography/index.html">Guides</a>
  </div>
</div>

<div class="bodu-card">
  <img src="images/hero-calendar.svg" alt="Bodu.Globalization.Calendar" />
  <h3>Bodu.Globalization.Calendar</h3>
  <p>Notable-date resolution with fixed, rule-based, offset-based, and dynamic calculators — including Gregorian-computus Easter and lunar-calendar Lunar New Year — driven by a pluggable XML rule source and adjustment pipeline. Region-specific public-holiday rules ship in companion <code>Data.Americas</code>, <code>Data.Europe</code>, and <code>Data.AsiaPacific</code> packs that ship and re-release independently of the main library.</p>
  <div class="bodu-card-links">
    <a href="api/Bodu.Globalization.Calendar.html">Overview</a>
    <a href="guides/calendar/data-packs.html">Data packs</a>
    <a href="api/Bodu.Globalization.Calendar.html">API reference</a>
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
- **Documentation-first**: every public type and member carries XML documentation in British English, which drives this API reference.
- **MIT licensed** and free of external runtime dependencies.

## Where to go next

<div class="bodu-nav">
  <a href="docs/introduction.html">Introduction</a>
  <a href="docs/getting-started.html">Getting started</a>
  <a href="api/Bodu.html">API reference</a>
  <a href="articles/index.html">Articles</a>
</div>
