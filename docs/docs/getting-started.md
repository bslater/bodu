---
title: Getting started
---

# Getting started

This page is a **cross-library tour**. It installs every Bodu package and runs one minimal example from each. If you only care about a single library, jump straight to its dedicated getting-started page:

- [Bodu.Core — getting started](core/getting-started.md)
- [Bodu.IO.Hashing — getting started](io-hashing/getting-started.md)
- [Bodu.Security.Cryptography — getting started](cryptography/getting-started.md)
- [Bodu.Globalization.Calendar — getting started](calendar/getting-started.md)

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

Swap `CRC32_ISOHDLC` for any of the 113 entries in the [CRC catalogue](../guides/io-hashing/crc-catalogue.md), or pick a fingerprint (`Fnv1a64`, `CityHash64`, `XxHash64`, `MurmurHash3`) for hash-table use. The [Bodu.IO.Hashing getting-started](io-hashing/getting-started.md) walks through all three subtypes.

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

## Where to go next

- **[Introduction](introduction.md)** — what each library is for and how they fit together.
- **[Algorithm families](algorithm-families.md)** — the cross-library taxonomy if your problem touches hashing, checksums, or encryption.
- **Library introductions:** [Bodu.Core](core/index.md) · [Bodu.IO.Hashing](io-hashing/index.md) · [Bodu.Security.Cryptography](cryptography/index.md) · [Bodu.Globalization.Calendar](calendar/index.md).
- **[API reference](xref:Bodu)** — the full auto-generated type-by-type documentation.
- **Guides:** [Bodu.Core](../guides/core/index.md) · [Bodu.IO.Hashing](../guides/io-hashing/index.md) · [Bodu.Security.Cryptography](../guides/cryptography/index.md) · [Bodu.Globalization.Calendar](../guides/calendar/index.md).
