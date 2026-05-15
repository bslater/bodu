---
title: Getting started
---

# Getting started

This page is a **cross-library tour**. It installs every Bodu package and runs one minimal example from each. If you only care about a single library, jump straight to its dedicated getting-started page:

- [Bodu.Core — getting started](core/getting-started.md)
- [Bodu.IO.Hashing — getting started](io-hashing/getting-started.md)
- [Bodu.Security.Cryptography — getting started](cryptography/getting-started.md)
- [Bodu.Globalization.Calendar — getting started](calendar/getting-started.md)
- [Bodu.Text.Encoding — getting started](text-encoding/getting-started.md)
- [Bodu.Text.Formats — getting started](formats/getting-started.md)

## Prerequisites

- The **.NET 8 SDK** — every package in the solution targets `net8.0`.

```bash
dotnet --version
```

## Install

Each package is independent; install only the ones you need.

```bash
dotnet add package Bodu.Core
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Text.Encoding
dotnet add package Bodu.Text.Formats

# Optional region-specific calendar data packs:
dotnet add package Bodu.Globalization.Calendar.Data.Americas
dotnet add package Bodu.Globalization.Calendar.Data.Europe
dotnet add package Bodu.Globalization.Calendar.Data.AsiaPacific
```

## One-minute samples

### Bodu.Core — a circular buffer

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

`CircularBuffer<T>` is a fixed-capacity FIFO collection. With `allowOverwrite: true` it silently drops the oldest item when full; with `allowOverwrite: false` it throws instead. See the [Bodu.Core getting-started](core/getting-started.md) for the full collections / `EvictingDictionary` / `WeekPattern` / extensions tour.

### Bodu.IO.Hashing — a CRC-32 checksum

```csharp
using System.Text;
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
crc.Append(data);
string hex = Convert.ToHexString(crc.GetCurrentHash());
```

Swap `CRC32_ISOHDLC` for any of the 113 entries in the [CRC catalogue](../guides/io-hashing/crc-catalogue.md), or pick a fingerprint (`Fnv1a64`, `CityHash64`, `MurmurHash3_128`) for hash-table use. The [Bodu.IO.Hashing getting-started](io-hashing/getting-started.md) walks through all three subtypes.

### Bodu.Security.Cryptography — a keyed SipHash

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");
byte[] key  = RandomNumberGenerator.GetBytes(16);

using var sip = new SipHash64 { Key = key };
ulong digest = BitConverter.ToUInt64(sip.ComputeHash(data));
```

`SipHash64` derives from `System.Security.Cryptography.HashAlgorithm`, so it drops into any API that expects a standard .NET hash. For block ciphers, AEAD modes, Merkle trees, and the ASCON family, see the [Bodu.Security.Cryptography getting-started](cryptography/getting-started.md).

### Bodu.Globalization.Calendar — resolve Easter Sunday

```csharp
using Bodu.Globalization.Calendar.Algorithms;

var algorithm = new EasterSundayNotableDateAlgorithm();
DateTime easter2026 = algorithm.Calculate(2026);
// 2026-04-05

DateTime goodFriday2026 = easter2026.AddDays(-2);
```

For the rule-driven `NotableDateService`, territory filtering, the observance-adjustment pipeline, and the regional data packs, see the [Bodu.Globalization.Calendar getting-started](calendar/getting-started.md).

### Bodu.Text.Encoding — hex / Base32 / Base64 / Base58 / Base85

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

For lenient parsing (whitespace, `0x` prefix, missing padding), `OperationStatus` streaming, and per-variant
options like MIME line wrapping or Crockford alias decoding, see the
[Bodu.Text.Encoding getting-started](text-encoding/getting-started.md).

### Bodu.Text.Formats — decode a Bencode document

```csharp
using Bodu.Text.Formats;

byte[] payload = File.ReadAllBytes("ubuntu.iso.torrent");

BencodedValue root = Bencode.Decode(payload);
BencodedDictionary doc = (BencodedDictionary)root;

string tracker = ((BencodedString)doc["announce"]).GetUtf8String();
BencodedDictionary info = (BencodedDictionary)doc["info"];
string name = ((BencodedString)info["name"]).GetUtf8String();
long pieceLength = ((BencodedInteger)info["piece length"]).Value;
```

The parser enforces every BEP 3 invariant — no leading zeros, no negative zero, dictionary keys sorted by raw byte
order, no trailing bytes — so a successful decode round-trips bit-exactly through `Bencode.Encode`. For
non-throwing parsing, stream support, and the full value model, see the
[Bodu.Text.Formats getting-started](formats/getting-started.md).

## Where to go next

- **[Introduction](introduction.md)** — what each library is for and how they fit together.
- **[Algorithm families](algorithm-families.md)** — the cross-library taxonomy if your problem touches hashing, checksums, or encryption.
- **Library introductions:** [Bodu.Core](core/index.md) · [Bodu.IO.Hashing](io-hashing/index.md) · [Bodu.Security.Cryptography](cryptography/index.md) · [Bodu.Globalization.Calendar](calendar/index.md) · [Bodu.Text.Encoding](text-encoding/index.md) · [Bodu.Text.Formats](formats/index.md).
- **API references:** [Bodu.Collections.Generic](../apidoc/Bodu.Collections.Generic.md) · [Bodu.IO.Hashing](../apidoc/Bodu.IO.Hashing.md) · [Bodu.Security.Cryptography](../apidoc/Bodu.Security.Cryptography.md) · [Bodu.Globalization.Calendar](../apidoc/Bodu.Globalization.Calendar.md).
- **Guides:** [Bodu.Core](../guides/core/index.md) · [Bodu.IO.Hashing](../guides/io-hashing/index.md) · [Bodu.Security.Cryptography](../guides/cryptography/index.md) · [Bodu.Globalization.Calendar](../guides/calendar/index.md) · [Bodu.Text.Encoding](../guides/text-encoding/index.md) · [Bodu.Text.Formats](../guides/formats/index.md).
