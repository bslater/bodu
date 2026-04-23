# Getting started

This page walks through installing each of the four Bodu libraries and running a minimal example that exercises the headline type of each.

## Prerequisites

- The **.NET 8 SDK** — every package in the solution targets `net8.0`.

Verify your SDK:

```bash
dotnet --version
```

## Install

Each package is independent; install only what you need.

```bash
dotnet add package Bodu.Core
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
dotnet add package Bodu.Globalization.Calendar
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

`CircularBuffer<T>` is a fixed-capacity FIFO collection. With `allowOverwrite: true` it silently drops the oldest item when full; with `allowOverwrite: false` it throws instead.

See also: <xref:Bodu.Collections.Generic.CircularBuffer`1>, <xref:Bodu.Collections.Generic.EvictingDictionary`2>, <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1>.

### Bodu.IO.Hashing — a CRC-32 checksum

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
crc.Append(data);
string hex = Convert.ToHexString(crc.GetCurrentHash());
```

`Crc` derives from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> and is configured with a named <xref:Bodu.IO.Hashing.CrcStandard> — `CRC32_ISOHDLC` here is the canonical zlib / PNG / Ethernet CRC-32. Swap it for `CRC32_ISCSI`, `CRC16_MODBUS`, `CRC64_XZ`, or any of the 113 entries in the [CRC catalogue](../guides/io-hashing/crc-catalogue.md).

### Bodu.IO.Hashing — a Fletcher-32 checksum

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var fletcher = new Fletcher32();
fletcher.Append(data);
byte[] checksum = fletcher.GetCurrentHash();
```

`Fletcher32` is a 32-bit position-dependent checksum — the twin-accumulator structure catches transpositions that a simple sum misses.

See also: <xref:Bodu.IO.Hashing.Crc>, <xref:Bodu.IO.Hashing.CrcStandard>, <xref:Bodu.IO.Hashing.Fletcher16>, <xref:Bodu.IO.Hashing.Fletcher64>, <xref:Bodu.IO.Hashing.IResumableHashAlgorithm>.

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

`SipHash64` derives from `System.Security.Cryptography.HashAlgorithm`, so it drops into any API that expects a standard .NET hash. It is keyed and collision-resistant, which makes it suitable for protecting hash tables against collision-DoS attacks.

See also: <xref:Bodu.Security.Cryptography.Threefish256>, <xref:Bodu.Security.Cryptography.Tiger>, <xref:Bodu.Security.Cryptography.MerkleTreeHash>, <xref:Bodu.Security.Cryptography.Fnv1a64>.

### Bodu.Globalization.Calendar — resolve Easter Sunday

```csharp
using Bodu.Globalization.Calendar.Calculators;

var calculator = new EasterSundayNotableDateCalculator();
DateTime easter2026 = calculator.Calculate(2026);
// 2026-04-05

// Good Friday is always two days before Easter.
DateTime goodFriday2026 = easter2026.AddDays(-2);
```

The calculator uses the Gregorian Computus for years from 1583 onward and falls back to the Julian algorithm for earlier years.

See also: <xref:Bodu.Globalization.Calendar.NotableDateService>, <xref:Bodu.Globalization.Calendar.Calculators.LunarNewYearNotableDateCalculator>, <xref:Bodu.Globalization.Calendar.NotableDateRule>.

## Where to go next

- **[Introduction](introduction.md)** — project overview and design principles.
- **[Bodu.Core overview](../apidoc/Bodu.Collections.Generic.md)** — collections, buffers, text utilities.
- **[Bodu.IO.Hashing overview](../apidoc/Bodu.IO.Hashing.md)** — CRC and Fletcher checksums.
- **[Bodu.Security.Cryptography overview](../apidoc/Bodu.Security.Cryptography.md)** — ciphers, keyed and cryptographic hashes, Merkle trees.
- **[Bodu.Globalization.Calendar overview](../apidoc/Bodu.Globalization.Calendar.md)** — notable dates and calculators.
- **[API reference](../api/)** — full auto-generated type-by-type documentation.
- **Guides:** [Bodu.IO.Hashing](../guides/io-hashing/) · [Bodu.Security.Cryptography](../guides/cryptography/).
