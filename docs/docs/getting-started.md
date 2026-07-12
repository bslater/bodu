---
title: Getting started
---

# Getting started

This page is a **cross-library tour**. It installs the Bodu packages, then for each library gives a one-minute orientation, a minimal working sample, and pointers to that library's introduction and getting-started guide. The sections follow the suite's seven topics — each topic heading links to its [topic overview](introduction.md#the-suite-in-seven-topics) page.

If you only need a single library, jump straight to its section below — each one ends with links to its dedicated **Introduction**, **Getting started**, and **Guides** pages.

## Prerequisites

- The **.NET 8 SDK** — every package in the solution targets `net8.0`.

```bash
dotnet --version
```

## Install

Each package is versioned and released independently; install only the ones you need. The only shared dependency is `Bodu.Core`, which the others pull in automatically.

```bash
# Core Foundations
dotnet add package Bodu.Core
dotnet add package Bodu.Collections
dotnet add package Bodu.Collections.Concurrent

# Hashing & Cryptography
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography

# Globalization & Calendars
dotnet add package Bodu.Globalization.Calendar

# Optional region-specific calendar data packs:
dotnet add package Bodu.Globalization.Calendar.Americas
dotnet add package Bodu.Globalization.Calendar.Europe
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
dotnet add package Bodu.Globalization.Calendar.Africa
dotnet add package Bodu.Globalization.Calendar.MiddleEast

# Text & Serialization
dotnet add package Bodu.Text.Encoding
dotnet add package Bodu.Text.Formats
dotnet add package Bodu.Text.Toml
dotnet add package Bodu.Text.Bencode

# Configuration
dotnet add package Bodu.Text.Configuration
dotnet add package Bodu.Extensions.Configuration.Text

# Numerics & Financial
dotnet add package Bodu.Numerics
dotnet add package Bodu.Financial

# Optional companions:
dotnet add package Bodu.Financial.DependencyInjection
```

## Core Foundations

The foundation of the suite — see the **[Core Foundations overview](topics/core-foundations.md)** for how every other topic builds on it.

### Bodu.Core

**Bodu.Core** is the foundation package — the `WeekPattern` value type, pooled buffers, async coordination primitives, railway outcomes (`Option<T>` / `Result<T>`), and date / numeric / span extensions sitting on a centralized `ThrowHelper`. It is the one package every other Bodu library depends on.

```csharp
using Bodu;

WeekPattern weekdays = WeekPattern.Parse("MTuWThF");
WeekPattern weekend  = WeekPattern.Parse("SaSu");
WeekPattern allDays  = weekdays | weekend;

bool monday = weekdays.Contains(DayOfWeek.Monday); // true
```

`WeekPattern` is an immutable 7-bit bitmask value type for sets of days of the week — compose with the bitwise operators, parse from compact text, and enumerate the selected days in order.

→ **[Introduction](core/index.md)** · **[Getting started](core/getting-started.md)** · **[Guides](../guides/core/index.md)**

### Bodu.Collections

**Bodu.Collections** is the specialized collection catalogue (it depends on `Bodu.Core`; the namespaces are unchanged) — fixed-capacity rings, policy-driven caches with TTL expiry, navigable and range-keyed lookups, interval trees, graphs, tries, and probabilistic sketches.

```csharp
using Bodu.Collections.Generic;

var buffer = new CircularBuffer<int>(capacity: 4, allowOverwrite: true);

buffer.Enqueue(1);
buffer.Enqueue(2);
buffer.Enqueue(3);
buffer.Enqueue(4);
buffer.Enqueue(5); // 1 is evicted; buffer holds [2, 3, 4, 5]

int oldest = buffer.Dequeue(); // 2
```

`CircularBuffer<T>` is a fixed-capacity FIFO collection. With `allowOverwrite: true` it silently drops the oldest item when full; with `allowOverwrite: false` it throws instead.

→ **[Introduction](collections/index.md)** · **[Getting started](collections/getting-started.md)** · **[Guides](../guides/core/index.md)**

### Bodu.Collections.Concurrent

**Bodu.Collections.Concurrent** ships the thread-safe members of the catalogue (it depends on `Bodu.Collections`) — the lock-free `ConcurrentCircularBuffer<T>` and the lock-free split-ordered `ConcurrentHashSet<T>`, both with snapshot enumeration that never throws on concurrent modification.

```csharp
using Bodu.Collections.Generic.Concurrent;

var seen = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

Parallel.ForEach(events, e =>
{
    if (seen.Add(e.CorrelationId))   // true only for the first arrival
        ProcessFirstOccurrence(e);
});
```

`Contains` is lock-free — readers never block writers — and disjoint writers proceed in parallel across independently locked bucket regions.

→ **[Introduction](collections-concurrent/index.md)** · **[Getting started](collections-concurrent/getting-started.md)** · **[Guides](../guides/core/concurrent-collections.md)**

### Bodu.Text

The **`Bodu.Text`** namespace — shipped in the `Bodu.Core` package — adds the ergonomic, allocation-aware surface the BCL leaves out on top of `System.Text.Encoding` — byte-order-mark detection, span- and UTF-8-friendly transcoding, preamble handling, and validation. (For binary-to-text codecs such as Base64, reach for `Bodu.Text.Encoding` below.)

```csharp
using Bodu.Text;

byte[] bytes = File.ReadAllBytes("data.txt");

System.Text.Encoding encoding =
    EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? detected)
        ? detected
        : System.Text.Encoding.UTF8;

string text = encoding.GetStringSkippingPreamble(bytes);
```

`EncodingDetection.TryDetectByPreamble` is non-allocating and recognises the five canonical Unicode BOMs; `GetStringSkippingPreamble` decodes the payload while dropping any leading preamble. The `EncodingExtensions` and `StringEncodingExtensions` surfaces add pooled, chunked, and `Try*` write-to-span overloads for the hot paths.

→ **[Introduction](text/index.md)** · **[API reference](xref:Bodu.Text)**

## Hashing & Cryptography

One question splits the two packages — *is there an adversary?* — see the **[Hashing & Cryptography overview](topics/hashing-and-cryptography.md)** for the decision rule.

### Bodu.IO.Hashing

**Bodu.IO.Hashing** covers non-cryptographic hashing — fingerprints for hash-table keys, checksums for error detection, and check digits for human-typed identifiers. Every type shares the BCL `Append` / `GetCurrentHash` / `Reset` lifecycle, and nothing here is safe against an adversary who can choose the input.

```csharp
using System.Text;
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
crc.Append(data);
string hex = Convert.ToHexString(crc.GetCurrentHash());
```

Swap `CRC32_ISOHDLC` for any of the 113 entries in the [CRC catalogue](../guides/io-hashing/crc-catalogue.md), or pick a fingerprint (`Fnv1a64`, `CityHash64`, `MurmurHash3_128`) for hash-table use.

→ **[Introduction](io-hashing/index.md)** · **[Getting started](io-hashing/getting-started.md)** · **[Guides](../guides/io-hashing/index.md)**

### Bodu.Security.Cryptography

**Bodu.Security.Cryptography** provides cryptographic primitives with a formal adversary model — block ciphers, AEAD modes, keyed hashes, and cryptographic digests — all on the standard `SymmetricAlgorithm` / `HashAlgorithm` contracts, so they drop into any code that already speaks .NET cryptography.

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");
byte[] key  = RandomNumberGenerator.GetBytes(16);

using var sip = new SipHash64 { Key = key };
ulong digest = BitConverter.ToUInt64(sip.ComputeHash(data));
```

`SipHash64` derives from `System.Security.Cryptography.HashAlgorithm`, so it drops into any API that expects a standard .NET hash. For block ciphers, AEAD modes, Merkle trees, and the ASCON family, see the per-library pages below.

→ **[Introduction](cryptography/index.md)** · **[Getting started](cryptography/getting-started.md)** · **[Guides](../guides/cryptography/index.md)**

## Globalization & Calendars

The notable-date runtime plus its companions and regional data packs — see the **[Globalization & Calendars overview](topics/globalization-and-calendars.md)** for how the package family composes.

### Bodu.Globalization.Calendar

**Bodu.Globalization.Calendar** resolves notable dates — public holidays, observances, religious festivals — for a date, range, or year and territory, from rule documents loaded into an immutable resource and queried through `NotableDateService`.

```csharp
using Bodu.Globalization.Calendar;

NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

foreach (NotableDate d in service.Resolve(2026, "AU-NSW"))
    Console.WriteLine($"{d.Date:yyyy-MM-dd}  {d.DisplayName}");
```

For authoring rule documents, territory filtering, the observance-adjustment pipeline, working-day arithmetic, and the regional data packs, see the getting-started page below.

→ **[Introduction](calendar/index.md)** · **[Getting started](calendar/getting-started.md)** · **[Guides](../guides/calendar/index.md)**

## Text & Serialization

Three different jobs that all sound like "text" — see the **[Text & Serialization overview](topics/text-and-serialization.md)** for the codec / document-format / serializer disambiguation.

### Bodu.Text.Encoding

**Bodu.Text.Encoding** is a library of binary-to-text encoders — Base16, Base32, Base64, Base58, and Base85 — each with span- and UTF-8-friendly overloads, `OperationStatus` streaming, and a unified `IBinaryEncoding` interface for runtime-pluggable selection.

```csharp
using Bodu.Text.Encoding;

byte[] digest = SHA256.HashData("hello"u8.ToArray());

string hex      = Base16.Encode(digest);                                    // canonical lower case
string hexUpper = Base16.Encode(digest, BaseFormattingOptions.UpperCase);   // RFC 4648 §8 canonical case
string base32   = Base32.Encode(digest);                                    // RFC 4648 §6 (padded)
string base64   = Base64.Encode(digest);                                    // RFC 4648 §4 (padded)
string urlSafe  = Base64.Encode(digest, Base64Variant.UrlSafe);             // RFC 4648 §5 (no padding)
```

Pick the encoding from configuration at runtime via the unified interface:

```csharp
IBinaryEncoding encoding = BinaryEncodings.Get("base64-urlsafe");
string token = encoding.Encode(rawBytes);
byte[] back  = encoding.Decode(token);
```

For lenient parsing (whitespace, `0x` prefix, missing padding), `OperationStatus` streaming, and per-variant options like MIME line wrapping or Crockford alias decoding, see the getting-started page below.

→ **[Introduction](text-encoding/index.md)** · **[Getting started](text-encoding/getting-started.md)** · **[Guides](../guides/text-encoding/index.md)**

### Bodu.Text.Formats

**Bodu.Text.Formats** parses and emits self-framing serialization formats — Delimited (CSV / TSV), DotEnv, and Ini — each through a strongly-typed value model and a span- and stream-friendly codec with `Try*` overloads.

```csharp
using Bodu.Text.Ini;

IniDocument config = Ini.Parse("""
    ; connection settings
    [database]
    host = localhost
    port = 5432
    """);

config.GetOrAddSection("database").SetEntry("port", "5433");

string text = Ini.Format(config);   // comments, ordering, and whitespace preserved
```

The parsed model retains comments, ordering, and whitespace, so a `Parse` → mutate → `Format` cycle round-trips the source. The Delimited and DotEnv namespaces follow the same `Parse` / `Format` shape.

→ **[Introduction](formats/index.md)** · **[Getting started](formats/getting-started.md)** · **[Guides](../guides/formats/index.md)**

### Bodu.Text.Bencode, Bodu.Text.Toml, and Bodu.Text.Yaml (serializers)

**Bodu.Text.Bencode**, **Bodu.Text.Toml**, and **Bodu.Text.Yaml** are three self-contained libraries that map your own types to and from a format. They share an architecture and a `System.Text.Json`-aligned shape, and each ships a serializer, two document object models, and a low-level `Utf8…Reader` / `Utf8…Writer` pair.

```csharp
using Bodu.Text.Toml;

public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}

string toml = TomlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = TomlSerializer.Deserialize<ServerConfig>(toml);
```

Swap `Toml` for `Bencode` (and `string` for `byte[]`) for the Bencode equivalent, or for `Yaml` for a YAML document. Bencode (BEP 3) object mapping lives here, not in `Bodu.Text.Formats`.

→ **[Introduction](serialization/index.md)** · **[Bencode](serialization/bencode/index.md)** · **[TOML](serialization/toml/index.md)** · **[YAML](serialization/yaml/index.md)** · **[Guides](../guides/serialization/index.md)**

## Configuration

Layered, EditorConfig-style configuration and its `Microsoft.Extensions.Configuration` bridge — see the **[Configuration overview](topics/configuration.md)** for the full pipeline.

### Bodu.Text.Configuration

**Bodu.Text.Configuration** layers EditorConfig-style configuration. It parses INI / EditorConfig text, resolves a preamble plus glob-anchored sections in source order for a target file path, and projects the result into a flat, colon-delimited `ConfigurationView` with typed accessors.

```csharp
using Bodu.Text.Configuration;

const string source = """
root = true

[*.cs]
format.indent.size = 4

[src/**/*.cs]
format.indent.size = 2
""";

ConfigurationView view = ConfigurationDocument
    .Parse(source)
    .Resolve("src/App/Program.cs");

int indentSize = view.GetInt32("format:indent:size");   // 2 — the last matching section wins
```

`Resolve` walks the document once in source order, layering the preamble and every glob-matching section; the flat `ConfigurationView` then exposes typed accessors — `GetInt32`, `GetBoolean`, `GetEnum<T>`, `GetValue<T>` — plus optional diagnostic collection and byte-faithful round-trip save.

→ **[Introduction](text-configuration/index.md)** · **[Getting started](text-configuration/getting-started.md)** · **[Guides](../guides/text-configuration/index.md)**

### Bodu.Extensions.Configuration.Text

**Bodu.Extensions.Configuration.Text** bridges `Bodu.Text.Configuration` into `Microsoft.Extensions.Configuration`. An `AddTextConfiguration*` builder call layers a Bodu configuration file alongside JSON, INI, XML, and environment-variable sources, with `IOptions<T>` binding and reload-on-change support.

```csharp
using Microsoft.Extensions.Configuration;
using Bodu.Extensions.Configuration.Text;

IConfiguration config = new ConfigurationBuilder()
    .AddTextConfigurationFile("app.editorconfig", optional: true, reloadOnChange: true)
    .Build();

string? indentSize = config["format:indent:size"];
```

`AddTextConfigurationFile` mirrors `AddJsonFile` — the Bodu source layers into the standard provider stack and participates in `IOptions<T>` binding, so existing `Microsoft.Extensions.Configuration` code adopts it with no learning curve.

→ **[Introduction](extensions-configuration-text/index.md)** · **[Getting started](extensions-configuration-text/getting-started.md)** · **[Guides](../guides/extensions-configuration-text/index.md)**

## Numerics & Financial

Exact arithmetic and the monetary primitives built on it — see the **[Numerics & Financial overview](topics/numerics-and-financial.md)** for how `Money` rides on `Fraction<BigInteger>`.

### Bodu.Numerics

**Bodu.Numerics** ships two generic-math value types — `Fraction<T>` for exact rational arithmetic and `Interval<T>` for bounded intervals — both built on `INumber<T>` so they compose with any generic-math algorithm.

```csharp
using Bodu.Numerics;

Fraction<int> total = Fraction<int>.Create(1, 3) + Fraction<int>.Create(2, 3); // 1/1

Interval<int> range = Interval<int>.Closed(1, 10);
bool containsFive = range.Contains(5); // true
```

Every `Fraction<T>` is GCD-reduced on construction and promotes to `BigInteger` for exact intermediates; `Interval<T>` expresses closed / open / half-open bounds with intersection, union, and adjacency operations.

→ **[Introduction](numerics/index.md)** · **[Getting started](numerics/getting-started.md)** · **[Guides](../guides/numerics/index.md)**

### Bodu.Financial

**Bodu.Financial** provides type-safe monetary primitives: `Money` for runtime-tagged amounts, and `Money<TCurrency>` where the currency is encoded as the type parameter so cross-currency arithmetic fails the build.

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;

// Runtime-tagged money — currency carried as a CurrencyCode.
Money price = new Money(125.50m, CurrencyCode.AUD);
Money gst   = price * 0.10m;
Money total = price + gst;             // 138.05 AUD

// Compile-time currency safety — the currency is the type parameter.
Money<AUD> typed = new Money<AUD>(125.50m);
```

Mixing currencies on the typed form is a compile error; on the runtime form it throws. For service registration, the companion **Bodu.Financial.DependencyInjection** package wires currency lookup and monetary services into an `IServiceCollection`:

```csharp
using Bodu.Financial;

services.AddFinancialService();
```

→ **[Introduction](financial/index.md)** · **[Getting started](financial/getting-started.md)** · **[Guides](../guides/financial/index.md)**

## Binary Formats & I/O

Legacy binary container and document formats — a read/edit/author compound-file container with narrower read-only format readers on top; see the **[Binary Formats & I/O overview](topics/binary-formats.md)** for the layered container-vs-format split.

### Bodu.IO.Compound

**Bodu.IO.Compound** reads, edits, and authors the OLE2 / Compound File Binary (CFB) container behind legacy Office documents, exposing the embedded named streams with no application-format knowledge.

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));

// Walk the storage hierarchy.
foreach (CompoundEntryInfo info in file.RootStorage.EnumerateEntries())
    Console.WriteLine($"{info.EntryType}: {info.Name} ({info.Length} bytes)");

// Read a named stream's bytes.
CompoundStream workbook = file.RootStorage.OpenStream("Workbook");
byte[] bytes = workbook.ReadAllBytes();
```

Open with `buffered: false` to read sectors on demand for large files; `OpenStream(name)` returns a seekable `CompoundStream` cursor. The BIFF8 `.xls` reader **Bodu.Formats.Excel.Binary** is built on top of this container reader.

→ **[Introduction](io-compound/index.md)** · **[Getting started](io-compound/getting-started.md)** · **[Guides](../guides/io-compound/index.md)**

## Where to go next

- **[Introduction](introduction.md)** — what each library is for and how they fit together.
- **Topic overviews:** [Core Foundations](topics/core-foundations.md) · [Hashing & Cryptography](topics/hashing-and-cryptography.md) · [Globalization & Calendars](topics/globalization-and-calendars.md) · [Text & Serialization](topics/text-and-serialization.md) · [Configuration](topics/configuration.md) · [Numerics & Financial](topics/numerics-and-financial.md).
- **Library introductions:** [Bodu.Core](core/index.md) · [Bodu.Collections](collections/index.md) · [Bodu.Collections.Concurrent](collections-concurrent/index.md) · [Bodu.IO.Hashing](io-hashing/index.md) · [Bodu.Security.Cryptography](cryptography/index.md) · [Bodu.Globalization.Calendar](calendar/index.md) · [Bodu.Text.Encoding](text-encoding/index.md) · [Bodu.Text.Formats](formats/index.md) · [Bodu.Text.Bencode](serialization/bencode/index.md) · [Bodu.Text.Toml](serialization/toml/index.md) · [Bodu.Text.Yaml](serialization/yaml/index.md) · [Bodu.Text.Configuration](text-configuration/index.md) · [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md) · [Bodu.Text](text/index.md) · [Bodu.Numerics](numerics/index.md) · [Bodu.Financial](financial/index.md).
- **API references:** [Bodu.Collections.Generic](xref:Bodu.Collections.Generic) · [Bodu.IO.Hashing](xref:Bodu.IO.Hashing) · [Bodu.Security.Cryptography](xref:Bodu.Security.Cryptography) · [Bodu.Globalization.Calendar](xref:Bodu.Globalization.Calendar) · [Bodu.Text](xref:Bodu.Text) · [Bodu.Numerics](xref:Bodu.Numerics) · [Bodu.Financial](xref:Bodu.Financial).
