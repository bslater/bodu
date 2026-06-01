---
title: Getting started
---

# Getting started

This page is a **cross-library tour**. It installs the Bodu packages, then for each library gives a one-minute orientation, a minimal working sample, and pointers to that library's introduction and getting-started guide.

If you only need a single library, jump straight to its section below — each one ends with links to its dedicated **Introduction**, **Getting started**, and **Guides** pages.

## Prerequisites

- The **.NET 8 SDK** — every package in the solution targets `net8.0`.

```bash
dotnet --version
```

## Install

Each package is versioned and released independently; install only the ones you need. The only shared dependency is `Bodu.Core`, which the others pull in automatically.

```bash
dotnet add package Bodu.Core
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Text.Encoding
dotnet add package Bodu.Text.Formats
dotnet add package Bodu.Text.Configuration
dotnet add package Bodu.Extensions.Configuration.Text

# Optional region-specific calendar data packs:
dotnet add package Bodu.Globalization.Calendar.Data.Americas
dotnet add package Bodu.Globalization.Calendar.Data.Europe
dotnet add package Bodu.Globalization.Calendar.Data.AsiaPacific
```

## Bodu.Core

**Bodu.Core** is the foundation package — bounded collections, eviction-aware caches, the `WeekPattern` value type, pooled buffers, and date / numeric / span extensions sitting on a centralized `ThrowHelper`. It is the one package every other Bodu library depends on.

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

→ **[Introduction](core/index.md)** · **[Getting started](core/getting-started.md)** · **[Guides](../guides/core/index.md)**

## Bodu.IO.Hashing

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

## Bodu.Security.Cryptography

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

## Bodu.Globalization.Calendar

**Bodu.Globalization.Calendar** resolves notable dates — public holidays, observances, religious festivals — for any year, territory, or calendar system, either through standalone calculators or the rule-driven `NotableDateService`.

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var algorithm = new EasterSundayNotableDateAlgorithm();
DateTime easter2026 = algorithm.Calculate(2026);
// 2026-04-05

DateTime goodFriday2026 = easter2026.AddDays(-2);
```

For the rule-driven `NotableDateService`, territory filtering, the observance-adjustment pipeline, and the regional data packs, see the getting-started page below.

→ **[Introduction](calendar/index.md)** · **[Getting started](calendar/getting-started.md)** · **[Guides](../guides/calendar/index.md)**

## Bodu.Text.Encoding

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

## Bodu.Text.Formats

**Bodu.Text.Formats** parses and emits self-framing serialization formats — Bencode, Delimited (CSV / TSV), Ini, and DotEnv — each through a strongly-typed value model and a span- and stream-friendly codec with `Try*` overloads.

```csharp
using Bodu.Text.Bencode;

byte[] payload = File.ReadAllBytes("ubuntu.iso.torrent");

BencodedValue root = Bencode.Decode(payload);
BencodedDictionary doc = (BencodedDictionary)root;

string tracker = ((BencodedString)doc["announce"]).GetUtf8String();
BencodedDictionary info = (BencodedDictionary)doc["info"];
string name = ((BencodedString)info["name"]).GetUtf8String();
long pieceLength = ((BencodedInteger)info["piece length"]).Value;
```

The parser enforces every BEP 3 invariant — no leading zeros, no negative zero, dictionary keys sorted by raw byte order, no trailing bytes — so a successful decode round-trips bit-exactly through `Bencode.Encode`. The Delimited, Ini, and DotEnv namespaces follow the same shape.

→ **[Introduction](formats/index.md)** · **[Getting started](formats/getting-started.md)** · **[Guides](../guides/formats/index.md)**

## Bodu.Text.Configuration

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

## Bodu.Extensions.Configuration.Text

**Bodu.Extensions.Configuration.Text** bridges `Bodu.Text.Configuration` into `Microsoft.Extensions.Configuration`. An `AddConfiguration` builder call layers a Bodu configuration file alongside JSON, INI, XML, and environment-variable sources, with `IOptions<T>` binding and reload-on-change support.

```csharp
using Microsoft.Extensions.Configuration;
using Bodu.Extensions.Configuration.Text;

IConfiguration config = new ConfigurationBuilder()
    .AddConfiguration("app.editorconfig", optional: true, reloadOnChange: true)
    .Build();

string? indentSize = config["format:indent:size"];
```

`AddConfiguration` mirrors `AddJsonFile` — the Bodu source layers into the standard provider stack and participates in `IOptions<T>` binding, so existing `Microsoft.Extensions.Configuration` code adopts it with no learning curve.

→ **[Introduction](extensions-configuration-text/index.md)** · **[Getting started](extensions-configuration-text/getting-started.md)** · **[Guides](../guides/extensions-configuration-text/index.md)**

## Where to go next

- **[Introduction](introduction.md)** — what each library is for and how they fit together.
- **Library introductions:** [Bodu.Core](core/index.md) · [Bodu.IO.Hashing](io-hashing/index.md) · [Bodu.Security.Cryptography](cryptography/index.md) · [Bodu.Globalization.Calendar](calendar/index.md) · [Bodu.Text.Encoding](text-encoding/index.md) · [Bodu.Text.Formats](formats/index.md) · [Bodu.Text.Configuration](text-configuration/index.md) · [Bodu.Extensions.Configuration.Text](extensions-configuration-text/index.md).
- **API references:** [Bodu.Collections.Generic](xref:Bodu.Collections.Generic) · [Bodu.IO.Hashing](xref:Bodu.IO.Hashing) · [Bodu.Security.Cryptography](xref:Bodu.Security.Cryptography) · [Bodu.Globalization.Calendar](xref:Bodu.Globalization.Calendar).
