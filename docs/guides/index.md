---
title: Guides
---

# Guides

Recipe-style walk-throughs and conceptual introductions for every library in the Bodu suite, organized by the suite's **seven topics**. Each topic has its own guides landing page that maps the member libraries' guide sections, and each library's section below is organized by **namespace**, with one walk-through per headline type.

If you are new to Bodu, start with the [introduction](../docs/introduction.md) for the project overview, or the [getting-started page](../docs/getting-started.md) for install commands. To choose between hashing or cryptography types that sound similar, see the [Bodu.IO.Hashing](../docs/io-hashing/index.md) and [Bodu.Security.Cryptography](../docs/cryptography/index.md) introductions.

**Topic guide landings:** [Core Foundations](topics/core-foundations.md) · [Hashing & Cryptography](topics/hashing-and-cryptography.md) · [Globalization & Calendars](topics/globalization-and-calendars.md) · [Text & Serialization](topics/text-and-serialization.md) · [Configuration](topics/configuration.md) · [Numerics & Financial](topics/numerics-and-financial.md) · [Binary Formats & I/O](topics/binary-formats.md)

## Core Foundations

General-purpose building blocks every other package depends on — see the **[Core Foundations guides landing](topics/core-foundations.md)**.

### Bodu.Core

Bounded collections, eviction-aware caches, day-of-week patterns, date and numeric extensions.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="core/index.md">Overview</a></h3>
  <p>Namespace map (<code>Bodu.Collections.Generic</code>, <code>Bodu</code>, <code>Bodu.Extensions</code>) — key types and which guide covers each.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/circular-buffer.md">Circular buffer</a></h3>
  <p>Fixed-capacity FIFO ring buffer — single-threaded and thread-safe variants, overwrite mode, peek / dequeue / try-enqueue patterns.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/deque.md">Deque</a></h3>
  <p>Double-ended queue with O(1) add and remove at both ends; growable or fixed-capacity.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/evicting-dictionary.md">Evicting dictionary</a></h3>
  <p>Capacity-bounded key-value store with FIFO, LRU, LFU, MRU, Random, and Second-Chance eviction policies.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/week-pattern.md">WeekPattern</a></h3>
  <p>Immutable bitmask value type for day-of-week sets — composition, parsing, bitwise operators.</p>
</div>

</div>

[Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic)

---

## Hashing & Cryptography

Non-cryptographic hashing on one side, a formal adversary model on the other — see the **[Hashing & Cryptography guides landing](topics/hashing-and-cryptography.md)** for the combined map.

### Bodu.IO.Hashing

Non-cryptographic hashing — fingerprints, checksums, and check digits — built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract. Nothing here is safe against an adversary; everything is fast and portable.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/index.md">Overview</a></h3>
  <p>Namespace map (<code>Bodu.IO.Hashing</code>, <code>.Checksums</code>, <code>.CheckDigits</code>) — key types and which guide covers each.</p>
</div>

</div>

#### `Bodu.IO.Hashing` — Fingerprints

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/fnv.md">Using FNV</a></h3>
  <p>FNV-1 and FNV-1a at 32- and 64-bit widths — the textbook constant-memory fingerprint.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/cityhash.md">Using CityHash</a></h3>
  <p>32-, 64-, and 128-bit Google CityHash — SIMD-friendly fingerprint for long inputs.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/murmurhash3.md">Using MurmurHash3</a></h3>
  <p>32- and 128-bit MurmurHash3 — seeded, excellent avalanche.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/pearson.md">Using Pearson</a></h3>
  <p>Table-driven hash with output widths from 8 to 2048 bits.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/string-hashes.md">Classic string hashes</a></h3>
  <p>Bernstein, BKDR, SDBM, JSHash, Elf64, ApHash, PJW, SuperFastHash.</p>
</div>

</div>

> For xxHash specifically, use `System.IO.Hashing.XxHash32` / `XxHash64` / `XxHash3` / `XxHash128` from the BCL — Bodu does not duplicate them.

#### `Bodu.IO.Hashing.Checksums` — Checksums

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/crc.md">Using CRC</a></h3>
  <p>One engine, 113 named standards (CRC-1 through CRC-64), custom parameter sets, shared lookup-table cache.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/crc-catalogue.md">CRC catalogue</a></h3>
  <p>Reference table of every named CRC standard — name, width, polynomial, init, reflect, XOR-out.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/fletcher.md">Using Fletcher</a></h3>
  <p>Twin-accumulator checksums in 16-, 32-, and 64-bit widths.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/adler.md">Using Adler</a></h3>
  <p>Adler-32 (zlib), Adler-32C (SIMD), Adler-64.</p>
</div>

</div>

#### `Bodu.IO.Hashing.CheckDigits`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/check-digits.md">Check digits overview</a></h3>
  <p>Luhn, Damm, Verhoeff, EAN, GTIN, ISIN, IBAN, ISBN, SEDOL, CUSIP, ABA routing, LEI — single- and multi-character validators for human-typed identifiers.</p>
</div>

</div>

[Bodu.IO.Hashing API reference](xref:Bodu.IO.Hashing)

### Bodu.Security.Cryptography

Cryptographic primitives with a formal adversary model — block ciphers, stream ciphers, AEAD constructions, keyed and unkeyed hashes — derived from the standard BCL base classes (`SymmetricAlgorithm`, `HashAlgorithm`).

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/index.md">Overview</a></h3>
  <p>Namespace map and selection table for cipher, hash, and AEAD families.</p>
</div>

</div>

#### Foundations

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/encryption-basics.md">Encryption basics</a></h3>
  <p>Key, IV, Tweak, BlockMode, Padding — the mental model every cipher in the library follows.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/cipher-modes.md">Cipher block modes</a></h3>
  <p>ECB, CBC, CFB, OFB, CTR — one worked round-trip per mode.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/padding.md">Padding</a></h3>
  <p>PKCS7, Zeros, None, ISO 10126, ISO 7816-4, ANSI X9.23 — how each one pads and when it round-trips cleanly.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/composing-primitives.md">Composing primitives</a></h3>
  <p><code>IBlockCipher</code> + <code>BlockCipherModeFactory</code> + <code>PaddingFactory</code> vs the <code>SymmetricAlgorithm</code> wrappers.</p>
</div>

</div>

#### Symmetric ciphers — Standard

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/skipjack.md">Using Skipjack</a></h3>
  <p>NSA design (declassified 1998); legacy interoperability only.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/blowfish.md">Using Blowfish</a></h3>
  <p>Schneier 1993; 64-bit block; expensive key schedule.</p>
</div>

</div>

`Camellia`, `Twofish`, and `Serpent128` follow the same `SymmetricAlgorithm` lifecycle — see the [Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography) for their parameters.

#### Symmetric ciphers — Tweakable

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/threefish-256.md">Using Threefish-256</a></h3>
  <p>Smallest Threefish variant; 256-bit block, 256-bit key, 128-bit tweak.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/threefish-512.md">Using Threefish-512</a></h3>
  <p>Recommended general-purpose Threefish variant; 512-bit block, 512-bit key, 128-bit tweak.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/threefish-1024.md">Using Threefish-1024</a></h3>
  <p>Highest Threefish security margin; 1024-bit block, 1024-bit key, 128-bit tweak.</p>
</div>

</div>

`Serpent256` / `Serpent512` / `Serpent1024` are wide-block tweakable Serpent constructions — non-standard, see the [API reference](xref:Bodu.Security.Cryptography) for their parameters.

#### Symmetric ciphers — Stream

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/stream-ciphers.md">Using stream ciphers</a></h3>
  <p>ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit, HC-128 — raw XOR keystream ciphers (no block, no padding). Confidentiality only; pair with a MAC or prefer AEAD.</p>
</div>

</div>

#### Symmetric ciphers — AEAD

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/aead-modes.md">AEAD modes</a></h3>
  <p>GCM, CCM, OCB, EAX, SIV, GCM-SIV — authenticated encryption using <code>AesBlockCipher</code> + a mode transform.</p>
</div>

</div>

#### Cryptographic hashes

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/hashing.md">Hashing overview</a></h3>
  <p>Cross-cutting overview of keyed hashes, cryptographic digests, and Merkle trees.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/tiger.md">Using Tiger</a></h3>
  <p>128 / 160 / 192-bit cryptographic digest optimized for 64-bit platforms.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/cubehash.md">Using CubeHash</a></h3>
  <p>SHA-3 finalist with tunable rounds and block size.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/snefru.md">Using Snefru</a></h3>
  <p>Legacy cryptographic digest; interop only (cryptanalytically broken).</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/merkle-trees.md">Using Merkle trees</a></h3>
  <p>Tree-structured streaming integrity over any inner <code>HashAlgorithm</code>.</p>
</div>

</div>

`Whirlpool`, `Blake2b`, `Blake2s`, `Blake3`, `Skein256` / `Skein512` / `Skein1024`, and `Shake` ship without dedicated walk-throughs — consult the [API reference](xref:Bodu.Security.Cryptography) directly.

#### Keyed hashes (MAC)

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/siphash.md">Using SipHash</a></h3>
  <p>SipHash-64 / SipHash-128 — keyed PRF for hash-flooding-resistant tables.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/poly1305.md">Using Poly1305</a></h3>
  <p>One-time authenticator (RFC 8439); pair with ChaCha20 or AES-CTR.</p>
</div>

</div>

#### ASCON family

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/ascon.md">ASCON overview</a></h3>
  <p>All five NIST SP 800-232 types with selection guidance.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-hashing.md">ASCON hashing</a></h3>
  <p><code>AsconHash256</code> and <code>AsconHashA256</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-xof.md">ASCON XOF</a></h3>
  <p><code>AsconXof128</code> and <code>AsconCxof128</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-aead.md">ASCON AEAD</a></h3>
  <p><code>AsconAead128</code> — sponge-based authenticated encryption.</p>
</div>

</div>

[Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography)

---

## Globalization & Calendars

The notable-date runtime, its companions, and the regional data packs — see the **[Globalization & Calendars guides landing](topics/globalization-and-calendars.md)** for the full map including the notable-date catalogue.

### Bodu.Globalization.Calendar

Rule-driven notable-date (public holiday, observance, festival) resolution for any year, territory, or calendar system.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="calendar/index.md">Overview</a></h3>
  <p>Resolution pipeline, namespace map, and key-type table.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/notable-dates.md">Using NotableDateService</a></h3>
  <p>Resolving for a year, filtering by territory and category, layering overrides.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/rule-authoring.md">Authoring notable-date rules</a></h3>
  <p>In-code, embedded XML / JSON, companion assemblies, runtime overrides.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/algorithms.md">Date calculation algorithms</a></h3>
  <p>Built-in algorithms (Easter, Hindu Lunar, Losar, Vesak, Asalha Puja, Qingming) and custom-algorithm walk-through.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/data-packs.md">Calendar data packs</a></h3>
  <p>Official Americas / Europe / Asia-Pacific companion assemblies.</p>
</div>

</div>

[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)

---

## Text & Serialization

Binary-to-text codecs, document formats, and object serializers — see the **[Text & Serialization guides landing](topics/text-and-serialization.md)** for the disambiguation between the three jobs.

### Bodu.Text.Encoding

Binary-to-text encoders for **Base16**, **Base32**, **Base64**, **Base58**, and **Base85** with every common
variant — span- and UTF-8-friendly, `OperationStatus`-aware, with a unified `IBinaryEncoding` interface for
runtime-pluggable encoding choice.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-encoding/index.md">Overview</a></h3>
  <p>The encoding family, payload-expansion comparison, and the choose-an-encoding decision table.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base16.md">Using Base16 (hexadecimal)</a></h3>
  <p>Formatting decorations (case / prefix / spacing / line breaks), lenient parsing, hex dumps, BCL aliases.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base32.md">Using Base32</a></h3>
  <p>Standard / HexExtended / Crockford / Z-Base-32 variants, TOTP secrets, padding control.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base64.md">Using Base64</a></h3>
  <p>Standard / URL-safe / MIME variants, JWT decoding, 76-character line wrapping.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base58.md">Using Base58</a></h3>
  <p>Bitcoin / Flickr / Ripple alphabets, leading-zero preservation, address decoding.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base85.md">Using Base85 (Ascii85 and Z85)</a></h3>
  <p>Adobe Ascii85 with the <code>z</code> shortcut and partial-group rules; ZeroMQ Z85 with shell-safe alphabet.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/binary-encodings-interface.md">The IBinaryEncoding interface</a></h3>
  <p>Runtime-selected encoding choice via <code>BinaryEncodings.Get(name)</code> and the <code>IBinaryEncoding</code> contract.</p>
</div>

</div>

### Bodu.Text.Filtering

Include/exclude filtering for lists of text values — glob and regex patterns compiled once into a
cost-tiered `TextFilter`, with Ant / MSBuild set semantics or gitignore-style ordered rules and
built-in match telemetry.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-filtering/index.md">Overview</a></h3>
  <p>How the engine works — compile-once filters, cost-tier classification, and the guide map.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-filtering/patterns-and-globs.md">Patterns and globs</a></h3>
  <p>The full glob grammar — classes, <code>{a,b}</code> alternation, escapes — and when to reach for regex.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-filtering/evaluation-modes.md">Evaluation modes</a></h3>
  <p><code>AnyMatch</code> sets vs <code>LastMatchWins</code> ordered rules, allowlists, gitignore-convention parsing.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-filtering/telemetry-and-tuning.md">Telemetry and tuning</a></h3>
  <p>Statistics, per-pattern hit counts, the observer hook, cost tiers, fail-safe regex timeouts.</p>
</div>

</div>

### Bodu.Text.Formats (Delimited · DotEnv · INI)

The line-oriented text formats — `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, and `Bodu.Text.Ini`, each a
standalone `System.Text.Json`-shaped library (token reader/writer, serializer, mutable and read-only DOMs)
reachable through the `Bodu.Text.Formats` umbrella package.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="formats/index.md">Overview</a></h3>
  <p>Namespace map and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="formats/delimited.md">Using delimited (CSV / TSV)</a></h3>
  <p>RFC 4180 quoting, delimiter selection, header handling, the streaming <code>Utf8DelimitedReader</code> / <code>Utf8DelimitedWriter</code>, typed records via <code>DelimitedSerializer</code>, and the dialect policies on <code>DelimitedReaderOptions</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="formats/dotenv.md">Using DotEnv</a></h3>
  <p><code>KEY=VALUE</code> parsing, quoting and escape rules, the <code>export</code> prefix, and typed settings via <code>DotEnvSerializer</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="formats/ini.md">Using INI</a></h3>
  <p>Hoisted globals and section objects, typed binding via <code>IniSerializer</code>, duplicate-section and duplicate-key policies, and comment-preserving mutation through the <code>IniNode</code> DOM.</p>
</div>

<div class="bodu-card">
  <h3><a href="formats/streaming.md">Streams and async I/O</a></h3>
  <p>The forward-only <code>Utf8*Reader</code> / <code>Utf8*Writer</code> token surfaces, the typed record-streaming serializer overloads, and the lifetime and mid-stream error contracts.</p>
</div>

</div>

### Bodu.Text.Bencode, Bodu.Text.Toml, and Bodu.Text.Yaml (serializers)

Three self-contained serializers that map your own types to and from a format.
They share an architecture and a `System.Text.Json`-aligned shape — what you learn
for one transfers to the next — and each has its own guide set.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="serialization/index.md">Overview</a></h3>
  <p>The three libraries, the shared tiers (serializer, DOMs, reader/writer), and how to choose a format.</p>
</div>

<div class="bodu-card">
  <h3><a href="serialization/toml/index.md">TOML guides</a></h3>
  <p><code>TomlSerializer</code>, the type mapping, spec-version selection, both DOMs, converters, callbacks, and the built-in catalog.</p>
</div>

<div class="bodu-card">
  <h3><a href="serialization/bencode/index.md">Bencode guides</a></h3>
  <p><code>BencodeSerializer</code>, byte strings, canonical key ordering, both DOMs, and the kinds Bencode cannot represent.</p>
</div>

<div class="bodu-card">
  <h3><a href="serialization/yaml/index.md">YAML guides</a></h3>
  <p><code>YamlSerializer</code>, the 1.2 core schema, both DOMs, multi-document streams, <code>[Yaml…]</code> attributes, and custom converters.</p>
</div>

</div>

---

## Configuration

Layered, EditorConfig-style configuration and its `Microsoft.Extensions.Configuration` bridge — see the **[Configuration guides landing](topics/configuration.md)**.

### Bodu.Text.Configuration

Parse a configuration document under one of the four profiles, resolve it for a target path, and read typed values back out.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-configuration/index.md">Overview</a></h3>
  <p>Namespace map, the parse → resolve → view pipeline, and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-configuration/parsing-and-profiles.md">Parsing and profiles</a></h3>
  <p><code>ConfigurationDocument.Parse</code>, <code>ConfigurationParseOptions</code>, and the four profile presets — inline comments, duplicate handling, length limits.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-configuration/views-and-resolution.md">Views and resolution</a></h3>
  <p><code>Resolve</code> → <code>ConfigurationView</code>: glob matching against a target path, key projection, typed getters, missing-key fallbacks.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-configuration/diagnostics.md">Diagnostics</a></h3>
  <p>The structured diagnostic surface — modes, severities, and the full <code>ConfigurationDiagnosticCode</code> catalogue.</p>
</div>

</div>

[Bodu.Text.Configuration API reference](xref:Bodu.Text.Configuration)

### Bodu.Extensions.Configuration.Text

Surface a parsed and resolved document through the standard `IConfiguration` pipeline that ASP.NET Core and Generic Host already consume.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="extensions-configuration-text/index.md">Overview</a></h3>
  <p>Namespace map — builder extensions, file and stream sources and providers, DI options helpers.</p>
</div>

<div class="bodu-card">
  <h3><a href="extensions-configuration-text/configuration-sources.md">Configuration sources</a></h3>
  <p><code>AddTextConfigurationFile</code> / <code>AddTextConfigurationStream</code>, the conventional file probe, reload-on-change, target-path anchoring, and <code>IOptions&lt;T&gt;</code> binding.</p>
</div>

</div>

[Bodu.Extensions.Configuration.Text API reference](xref:Bodu.Extensions.Configuration.Text)

---

## Numerics & Financial

Exact arithmetic and the monetary primitives built on it — see the **[Numerics & Financial guides landing](topics/numerics-and-financial.md)**.

### Bodu.Numerics

Generic-math value primitives — exact rational arithmetic and bounded intervals.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="numerics/index.md">Overview</a></h3>
  <p>The two value types, their generic-math surfaces, and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="numerics/fraction.md">Working with Fraction&lt;T&gt;</a></h3>
  <p>Canonical form, GCD reduction, <code>BigInteger</code> promotion, the <code>INumber&lt;T&gt;</code> surface, approximation.</p>
</div>

<div class="bodu-card">
  <h3><a href="numerics/formatting-and-parsing.md">Formatting and parsing</a></h3>
  <p>Format specifiers, mixed numbers, vulgar fractions, culture handling, parse shapes.</p>
</div>

<div class="bodu-card">
  <h3><a href="numerics/interval.md">Working with Interval&lt;T&gt;</a></h3>
  <p>Closed / open / half-open bounds, containment, intersection, union, adjacency.</p>
</div>

<div class="bodu-card">
  <h3><a href="numerics/json-serialization.md">JSON serialization</a></h3>
  <p>The System.Text.Json converters, wire shapes, and round-tripping <code>Fraction&lt;BigInteger&gt;</code>.</p>
</div>

</div>

[Bodu.Numerics API reference](xref:Bodu.Numerics)

### Bodu.Financial

Type-safe money, the ISO 4217 currency catalogue, exchange rates, allocation, and cash rounding.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="financial/index.md">Overview</a></h3>
  <p>The money types, the currency catalogue, and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="financial/money.md">Working with Money&lt;TCurrency&gt;</a></h3>
  <p>Compile-time currency safety, arithmetic, allocation, rounding, and the runtime <code>Money</code> form.</p>
</div>

<div class="bodu-card">
  <h3><a href="financial/exchange-rates.md">Working with exchange rates</a></h3>
  <p><code>ExchangeRate</code> and <code>ExchangeRate&lt;TBase, TQuote&gt;</code> — conversion, inversion, composition.</p>
</div>

<div class="bodu-card">
  <h3><a href="financial/exchange-rate-lookups.md">Exchange-rate lookups</a></h3>
  <p>Dated providers, lookup results, provenance, and fallback stacks over a worked dataset.</p>
</div>

<div class="bodu-card">
  <h3><a href="financial/dependency-injection.md">Dependency injection</a></h3>
  <p><code>AddFinancialService</code> — registering currency lookup, monetary contexts, and rate providers.</p>
</div>

</div>

[Bodu.Financial API reference](xref:Bodu.Financial)

---

## Binary Formats & I/O

Legacy binary container and document formats — a read/edit/author compound-file container with narrower read-only format readers on top; see the **[Binary Formats & I/O guides landing](topics/binary-formats.md)**.

### Bodu.IO.Compound

A reader, editor, and writer for the OLE2 / Compound File Binary (CFB) container — the structured-storage "file system in a file" used by legacy Office documents (`.xls`, `.doc`, `.ppt`, `.msg`). It navigates the storage hierarchy, reads the raw byte payload of each named stream, edits and authors containers with a transactional `Commit` / `CommitAsync`, and reads and writes the OLE property sets, all with no application-format knowledge.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-compound/index.md">Overview</a></h3>
  <p>Namespace map (<code>Bodu.IO.Compound</code>, <code>.PropertySets</code>), the storage-hierarchy mental model, and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-compound/reading-compound-files.md">Reading compound files</a></h3>
  <p>Open a file, probe the signature, walk the hierarchy with the enumerate and <code>TryOpen</code> surfaces, and read a named stream's bytes.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-compound/streaming-and-buffering.md">Buffered vs streaming access</a></h3>
  <p>The <code>buffered</code> flag, the <code>CompoundStream</code> cursor, <code>AsMemory</code> vs chunked <code>Read</code>, and bounding memory for large files.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-compound/property-sets.md">Reading property sets</a></h3>
  <p>The <code>SummaryInformation</code> / <code>DocumentSummaryInformation</code> metadata streams, the raw <code>OlePropertySet</code>, and the <code>TryGet*</code> convenience methods.</p>
</div>

</div>

[Bodu.IO.Compound API reference](xref:Bodu.IO.Compound)

### Bodu.Formats.Excel.Binary

A narrow, read-only BIFF8 (`.xls`) reader built on `Bodu.IO.Compound`. It surfaces the raw cell values of each worksheet — strings, numbers, booleans, and errors, including a formula cell's cached result — without formula evaluation, styling, or higher-level interpretation.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="excel/index.md">Overview</a></h3>
  <p>Namespace map (<code>Bodu.Formats.Excel</code>), the layered BIFF8-on-compound-file model, and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="excel/reading-workbooks.md">Reading workbooks</a></h3>
  <p>Open an <code>.xls</code> from a path or stream, list the sheets and used ranges, control ownership and optional work, and read document properties.</p>
</div>

<div class="bodu-card">
  <h3><a href="excel/cell-values-and-dates.md">Cell values and dates</a></h3>
  <p>The <code>ExcelCell</code> kinds and value projections, a formula's cached result, date-format detection, serial-date conversion, and A1 references.</p>
</div>

<div class="bodu-card">
  <h3><a href="excel/worksheets-and-rows.md">Streaming vs materialized</a></h3>
  <p>The forward-only <code>ExcelWorksheetReader</code> versus the randomly addressable <code>ExcelWorksheet</code> — when to reach for each, and how to bound allocation.</p>
</div>

</div>

[Bodu.Formats.Excel API reference](xref:Bodu.Formats.Excel)
