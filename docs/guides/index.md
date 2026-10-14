---
title: Guides
---

# Guides

Recipe-style walk-throughs and conceptual introductions for every library in the Bodu suite, organized by the suite's **seven topics**. This page has one entry per package: its **guide index** lists every guide for that package, by namespace, and the links beside it are good first reads. Each topic also has a guides landing page that maps its packages' guides side by side.

If you are new to Bodu, start with the [introduction](../docs/introduction.md) for the project overview, or the [getting-started page](../docs/getting-started.md) for install commands.

**Topic guide landings:** [Core Foundations](topics/core-foundations.md) · [Hashing & Cryptography](topics/hashing-and-cryptography.md) · [Globalization & Calendars](topics/globalization-and-calendars.md) · [Text & Serialization](topics/text-and-serialization.md) · [Configuration](topics/configuration.md) · [Numerics & Financial](topics/numerics-and-financial.md) · [Binary Formats & I/O](topics/binary-formats.md)

## Core Foundations

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="core/index.md">Bodu.Core</a></h3>
  <p>The calendar value sets, pooled buffers, async coordination and railway primitives, memoization, natural string ordering, sequence operators, and the date, numeric, span, and stream extensions.</p>
  <div class="bodu-card-links">
    <a href="core/index.md">Guide index</a>
    <a href="core/day-of-week-set.md">DayOfWeekSet</a>
    <a href="core/functional-results.md">Options, results, and eithers</a>
    <a href="core/async-primitives.md">Async primitives</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="core/choosing-a-collection.md">Bodu.Collections</a></h3>
  <p>The specialized collection catalogue: rings and deques, evicting and sequenced dictionaries, ordered, navigable, and range-keyed lookups, interval trees, graphs, tries, the bit set, and the probabilistic sketches. Its guides share the <code>core/</code> guide index with <code>Bodu.Core</code>.</p>
  <div class="bodu-card-links">
    <a href="core/index.md">Guide index</a>
    <a href="core/choosing-a-collection.md">Choosing a collection</a>
    <a href="core/evicting-dictionary.md">Evicting dictionary</a>
    <a href="core/probabilistic-collections.md">Sketches</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="core/concurrent-collections.md">Bodu.Collections.Concurrent</a></h3>
  <p>The thread-safe variants: <code>ConcurrentCircularBuffer&lt;T&gt;</code> (lock-free Vyukov MPMC), <code>ConcurrentHashSet&lt;T&gt;</code> (lock-free split-ordered), <code>ConcurrentEvictingDictionary&lt;TKey,TValue&gt;</code> (lock-striped bounded cache), and <code>ConcurrentLruCache&lt;TKey,TValue&gt;</code> (read-optimized bounded cache).</p>
  <div class="bodu-card-links">
    <a href="core/concurrent-collections.md">Concurrent collections</a>
    <a href="core/thread-safety.md">Thread-safety contracts</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="core/encoding-helpers.md">Bodu.Text (namespace in Bodu.Core)</a></h3>
  <p>Character-encoding helpers over <code>System.Text.Encoding</code>: byte-order-mark detection, span- and UTF-8-friendly transcoding, preamble handling, fallbacks, and chunked transcoding.</p>
  <div class="bodu-card-links">
    <a href="core/encoding-helpers.md">Encoding helpers and BOM detection</a>
  </div>
</div>

</div>

## Hashing & Cryptography

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/index.md">Bodu.IO.Hashing</a></h3>
  <p>Non-cryptographic hashing: the FNV, CityHash, MurmurHash3, Pearson, and classic string fingerprints, the CRC engine with 112 named standards (CRC-3 through CRC-64), Fletcher and Adler, and the check-digit schemes.</p>
  <div class="bodu-card-links">
    <a href="io-hashing/index.md">Guide index</a>
    <a href="io-hashing/crc.md">Using CRC</a>
    <a href="io-hashing/check-digits.md">Check digits</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/index.md">Bodu.Security.Cryptography</a></h3>
  <p>Block, tweakable, and stream ciphers, cipher modes and padding, AEAD (the AES modes, ASCON, and the Poly1305 stream AEADs), digests and XOFs, MACs, Merkle trees, X25519, Ed25519, ML-KEM, ML-DSA, and HPKE, the KDFs (Argon2, scrypt, HKDF), one-time passwords, BCL interop, and hardware acceleration.</p>
  <div class="bodu-card-links">
    <a href="cryptography/index.md">Guide index</a>
    <a href="cryptography/choosing-a-primitive.md">Choosing a primitive</a>
    <a href="cryptography/aead-modes.md">AEAD modes</a>
    <a href="cryptography/security-posture.md">Security posture</a>
  </div>
</div>

</div>

## Globalization & Calendars

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="calendar/index.md">Bodu.Globalization.Calendar</a></h3>
  <p>The notable-date runtime and its companions: rule authoring, the strategy kinds and built-in algorithms, working days, the fluent builder, dependency injection, plugin trust, caching, the rule-pack toolchain, and the five regional data packs (Americas, Asia-Pacific, Europe, Middle East, and Africa).</p>
  <div class="bodu-card-links">
    <a href="calendar/index.md">Guide index</a>
    <a href="calendar/notable-dates.md">Using NotableDateService</a>
    <a href="calendar/data-packs.md">Data packs</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="recurrence/index.md">Bodu.Globalization.Recurrence</a></h3>
  <p>RFC 5545 recurrence rules and rule sets, cron expressions with the Quartz day tokens, and anchored intervals, behind one next and previous occurrence surface; plus a scheduling host built on them.</p>
  <div class="bodu-card-links">
    <a href="recurrence/index.md">Guide index</a>
    <a href="recurrence/rrule.md">RRULE</a>
    <a href="recurrence/cron.md">Cron</a>
  </div>
</div>

</div>

## Text & Serialization

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-encoding/index.md">Bodu.Text.Encoding</a></h3>
  <p>Base16, Base32, Base45, Base58, Base62, Base64, Base85, and Bech32, the <code>IBinaryEncoding</code> registry, and the percent-encoding and quoted-printable escapes.</p>
  <div class="bodu-card-links">
    <a href="text-encoding/index.md">Guide index</a>
    <a href="text-encoding/base64.md">Base64</a>
    <a href="text-encoding/binary-encodings-interface.md">IBinaryEncoding</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="text-filtering/index.md">Bodu.Text.Filtering</a></h3>
  <p>Include/exclude filtering: the glob grammar, AnyMatch sets and gitignore-style ordered rules, and the statistics and observer telemetry.</p>
  <div class="bodu-card-links">
    <a href="text-filtering/index.md">Guide index</a>
    <a href="text-filtering/patterns-and-globs.md">Patterns and globs</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="formats/index.md">Bodu.Text.Formats (Delimited, DotEnv, INI)</a></h3>
  <p>The line formats: reading and writing CSV and TSV, <code>.env</code>, and INI, streaming, error handling, the writer and DOM options, and the reflection-free source generator.</p>
  <div class="bodu-card-links">
    <a href="formats/index.md">Guide index</a>
    <a href="formats/choosing-a-format.md">Choosing a format</a>
    <a href="formats/error-handling.md">Error handling</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="serialization/index.md">Bodu serializers (Bencode, TOML, YAML)</a></h3>
  <p>The three <code>System.Text.Json</code>-shaped serializers: type mapping, both DOMs, the token readers and writers, attributes, converters, and options.</p>
  <div class="bodu-card-links">
    <a href="serialization/index.md">Guide index</a>
    <a href="serialization/toml/index.md">TOML</a>
    <a href="serialization/bencode/index.md">Bencode</a>
    <a href="serialization/yaml/index.md">YAML</a>
  </div>
</div>

</div>

## Configuration

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-configuration/index.md">Bodu.Text.Configuration</a></h3>
  <p>Parsing and profiles, glob-anchored resolution into a typed view, and diagnostics.</p>
  <div class="bodu-card-links">
    <a href="text-configuration/index.md">Guide index</a>
    <a href="text-configuration/views-and-resolution.md">Views and resolution</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="extensions-configuration-text/index.md">Bodu.Extensions.Configuration.Text</a></h3>
  <p>Layering Bodu configuration, TOML, and Bencode files into <code>Microsoft.Extensions.Configuration</code>, with options binding and reload.</p>
  <div class="bodu-card-links">
    <a href="extensions-configuration-text/index.md">Guide index</a>
    <a href="extensions-configuration-text/configuration-sources.md">Configuration sources</a>
  </div>
</div>

</div>

## Numerics & Financial

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="numerics/index.md">Bodu.Numerics</a></h3>
  <p><code>Fraction&lt;T&gt;</code>, <code>BigDecimal</code>, <code>Complex&lt;T&gt;</code>, the interval algebra (<code>Interval&lt;T&gt;</code>, <code>DiscreteInterval&lt;T&gt;</code>, <code>IntervalSet&lt;T&gt;</code>), the running statistics, generic-math constraints, and the JSON companion.</p>
  <div class="bodu-card-links">
    <a href="numerics/index.md">Guide index</a>
    <a href="numerics/fraction.md">Fraction&lt;T&gt;</a>
    <a href="numerics/json-serialization.md">JSON serialization</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="financial/index.md">Bodu.Financial</a></h3>
  <p>Money and currencies, monetary precision, exchange rates and lookups, the live-feed providers and their configuration, rate caching, dependency injection, and the JSON companion.</p>
  <div class="bodu-card-links">
    <a href="financial/index.md">Guide index</a>
    <a href="financial/money.md">Money</a>
    <a href="financial/exchange-rate-providers.md">Exchange-rate providers</a>
    <a href="financial/dependency-injection.md">Dependency injection</a>
  </div>
</div>

</div>

## Binary Formats & I/O

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-compound/index.md">Bodu.IO.Compound</a></h3>
  <p>The OLE2 compound-file container: reading, buffered and streaming access, property sets, authoring, and editing in place.</p>
  <div class="bodu-card-links">
    <a href="io-compound/index.md">Guide index</a>
    <a href="io-compound/reading-compound-files.md">Reading compound files</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="io-biff/index.md">Bodu.IO.Biff</a></h3>
  <p>The BIFF5 and BIFF8 record codec: authoring a complete workbook stream, and reading a stream a block at a time.</p>
  <div class="bodu-card-links">
    <a href="io-biff/index.md">Guide index</a>
    <a href="io-biff/authoring-records.md">Authoring a workbook stream</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="excel/index.md">Bodu.Formats.Excel.Binary</a></h3>
  <p>The read-only BIFF5 and BIFF8 <code>.xls</code> reader over <code>Bodu.IO.Compound</code> and <code>Bodu.IO.Biff</code>: workbooks, cell values and dates, and streaming versus materialized sheets.</p>
  <div class="bodu-card-links">
    <a href="excel/index.md">Guide index</a>
    <a href="excel/reading-workbooks.md">Reading workbooks</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="io-pst/index.md">Bodu.IO.Pst</a></h3>
  <p>The PST container layer: nodes, property and table contexts, streaming payloads, and validation levels.</p>
  <div class="bodu-card-links">
    <a href="io-pst/index.md">Guide index</a>
    <a href="io-pst/reading-nodes-and-tables.md">Reading nodes and tables</a>
  </div>
</div>

<div class="bodu-card">
  <h3><a href="outlook/index.md">Bodu.Formats.Outlook (.Msg, .Pst)</a></h3>
  <p>The <code>.msg</code> and <code>.pst</code> readers over the shared MAPI value model: messages, mail stores, properties and named properties, and the reader options and resource limits.</p>
  <div class="bodu-card-links">
    <a href="outlook/index.md">Guide index</a>
    <a href="outlook/reading-msg-files.md">Reading .msg files</a>
    <a href="outlook/reading-pst-mail-stores.md">Reading .pst mail stores</a>
  </div>
</div>

</div>

## Where to go next

- **[Introduction](../docs/introduction.md)** - what each library is for and how they fit together.
- **[Getting started](../docs/getting-started.md)** - install commands and a first sample from each library.
- **[Samples](../samples/index.md)** - runnable, offline sample projects for every package family.
- **[API reference](xref:Bodu)** - every type and member.
