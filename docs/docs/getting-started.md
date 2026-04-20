# Getting started

This page walks through installing each of the three Bodu libraries and running a minimal example that exercises the headline type of each.

## Prerequisites

- The **.NET 8 SDK** (required for `Bodu.Core` and `Bodu.Security.Cryptography`).
- `Bodu.Globalization.Calendar` also targets `net6.0` and `net7.0`, so either of those SDKs is sufficient for that package alone.

Verify your SDK:

```bash
dotnet --version
```

## Install

Each package is independent; install only what you need.

```bash
dotnet add package Bodu.Core
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

### Bodu.Security.Cryptography — a Fletcher-32 checksum

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var fletcher = new Fletcher32();
byte[] checksum = fletcher.ComputeHash(data);
string hex = Convert.ToHexString(checksum);
```

`Fletcher32` derives from `System.Security.Cryptography.HashAlgorithm`, so it drops into any API that expects a standard .NET hash.

See also: <xref:Bodu.Security.Cryptography.Threefish256>, <xref:Bodu.Security.Cryptography.SipHash64>, <xref:Bodu.Security.Cryptography.Tiger>.

### Bodu.Globalization.Calendar — resolve Easter Sunday

```csharp
using Bodu.Globalization.Calendar.Calculators;

var calculator = new EasterSundayNotableDateCalculator();
DateTime easter2026 = calculator.Calculate(2026);
// 2026-04-05
```

The calculator uses the Gregorian Computus for years from 1583 onward and falls back to the Julian algorithm for earlier years.

See also: <xref:Bodu.Globalization.Calendar.NotableDateService>, <xref:Bodu.Globalization.Calendar.Calculators.LunarNewYearNotableDateCalculator>.

## Where to go next

- **[Introduction](introduction.md)** — project overview and design principles.
- **[Bodu.Core overview](../api/Bodu.Collections.Generic.html)** — collections, buffers, text utilities.
- **[Bodu.Security.Cryptography overview](../api/Bodu.Security.Cryptography.html)** — ciphers, hashes, checksums.
- **[Bodu.Globalization.Calendar overview](../api/Bodu.Globalization.Calendar.html)** — notable dates and calculators.
- **[API reference](../api/)** — full auto-generated type-by-type documentation.
