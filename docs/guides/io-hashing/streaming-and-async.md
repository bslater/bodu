---
title: Streaming, async, and resumable hashing
---

# Streaming, async, and resumable hashing

Every algorithm in `Bodu.IO.Hashing` derives from `System.IO.Hashing.NonCryptographicHashAlgorithm`, whose core is `Append` / `GetCurrentHash` / `Reset`. Three things build on that core for data that does not fit in one span: <xref:Bodu.IO.Hashing.HashingStream> computes a digest as a side effect of ordinary stream I/O; <xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> adds the `Stream`, `async`, and verify overloads; and <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> lets a stored digest be extended with new bytes without re-reading the original input. This page walks through all three and ends with the digest byte-order table you need when comparing results across tools.

## Pattern 1 — `HashingStream` while copying

`HashingStream(Stream innerStream, NonCryptographicHashAlgorithm algorithm, bool leaveOpen = false)` is a pass-through stream. Bytes are fed to the algorithm whichever way they flow — reads append what the inner stream actually returned, writes append after the inner stream accepted them — so one pass over the data both moves it and checksums it. `CanSeek` is always `false` (seeking would let bytes bypass or re-enter the digest; `Seek` and `SetLength` throw `NotSupportedException`), and the algorithm is yours: the stream never disposes it.

<!-- compile -->
```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

byte[] payload = new byte[200_000];
for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i * 7);

using var source = new MemoryStream(payload);
using var destination = new MemoryStream();

var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
using (var hashing = new HashingStream(source, crc, leaveOpen: true))
{
    hashing.CopyTo(destination);                  // every byte read from the source is fed to the CRC
    byte[] soFar = hashing.GetCurrentHash();      // 707A0D67 — non-destructive snapshot
}

byte[] digest = crc.GetCurrentHash();             // same value; the algorithm outlives the stream
```

`GetCurrentHash()` reports the digest of the bytes transferred so far without finalizing; `GetHashAndReset()` returns it and resets the algorithm, which is the natural fit for framing several records through one writer:

<!-- compile -->
```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

using var sink = new MemoryStream();
using var hashing = new HashingStream(sink, new Fletcher32());

hashing.Write("abcde"u8);
byte[] first = hashing.GetHashAndReset();         // Fletcher-32 of "abcde": 05C301EF, then reset
hashing.Write("abcdef"u8);
byte[] second = hashing.GetCurrentHash();         // Fletcher-32 of "abcdef" alone: 08180255
```

`Read`, `Write`, `ReadByte`, `WriteByte`, and the `Memory<byte>` / `Task` async overloads are all routed through the algorithm; `Flush` / `FlushAsync` and `DisposeAsync` forward to the inner stream (which is disposed unless `leaveOpen` is `true`). Like the algorithm it wraps, a `HashingStream` is not thread-safe.

## Pattern 2 — the extension surface

<xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> covers `byte[]`, span, `string` + `Encoding`, and `Stream` inputs. The stream members:

| Member | Default buffer | Finalizes? | Notes |
|---|---|---|---|
| `ComputeHash(Stream source, int bufferSize = 4096)` | 4 KiB | Resets first, then `GetCurrentHash` | synchronous |
| `ComputeHashAsync(Stream source, int bufferSize = 81920, CancellationToken)` | 80 KiB | as above | returns `ValueTask<byte[]>` |
| `AppendData(Stream source, int bufferSize = 4096)` / `AppendDataAsync(Stream, int = 4096, CancellationToken)` | 4 KiB | **No** — call `GetCurrentHash` when done | accumulate several sources into one digest |
| `VerifyHash(Stream, byte[] \| string hex)` / `VerifyHashAsync(Stream, byte[] \| string \| ReadOnlyMemory<byte>, CancellationToken)` | via `ComputeHash*` | Yes | constant-time compare; throws on `null` |
| `TryVerifyHash(…)` / `TryVerifyHashAsync(…)` | as above | Yes | `false` for `null` arguments, malformed hex, cancellation, or any exception |

Read buffers are rented from `ArrayPool<byte>.Shared` and returned on every exit path; `bufferSize` must be positive (`ArgumentOutOfRangeException`). An already-cancelled token surfaces from `ComputeHashAsync` as `TaskCanceledException` (an `OperationCanceledException`).

<!-- compile -->
```csharp
using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

byte[] payload = new byte[200_000];
for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i * 7);

var crc = new Crc(CrcStandard.CRC32_ISCSI);
byte[] expected = crc.ComputeHash(payload);                                     // 287966E6

using var s1 = new MemoryStream(payload);
byte[] digest = await crc.ComputeHashAsync(s1, bufferSize: 16 * 1024);

using var s2 = new MemoryStream(payload);
bool ok = await crc.VerifyHashAsync(s2, expected);                             // true

using var s3 = new MemoryStream(payload);
bool okHex = await crc.TryVerifyHashAsync(s3, Convert.ToHexString(expected));  // true

using var s4 = new MemoryStream(payload);
var running = new Crc(CrcStandard.CRC32_ISCSI);
await running.AppendDataAsync(s4, bufferSize: 4096);                           // not finalized …
byte[] same = running.GetCurrentHash();                                         // … until you ask
```

## Pattern 3 — resuming from a stored digest

<xref:Bodu.IO.Hashing.IResumableHashAlgorithm> is implemented by <xref:Bodu.IO.Hashing.Checksums.Crc>, the FNV family (<xref:Bodu.IO.Hashing.Fnv132>, <xref:Bodu.IO.Hashing.Fnv164>, <xref:Bodu.IO.Hashing.Fnv1a32>, <xref:Bodu.IO.Hashing.Fnv1a64>), <xref:Bodu.IO.Hashing.Checksums.Fletcher16> / <xref:Bodu.IO.Hashing.Checksums.Fletcher32> / <xref:Bodu.IO.Hashing.Checksums.Fletcher64>, and <xref:Bodu.IO.Hashing.Checksums.Adler32> / <xref:Bodu.IO.Hashing.Checksums.Adler32C> / <xref:Bodu.IO.Hashing.Checksums.Adler64> — every algorithm whose digest *is* (or reversibly encodes) the full working state. The interface exposes `HashLengthInBytes`, three `ComputeHashFrom(previousHash, newData)` overloads (span; `byte[]`; `byte[]` with offset and length), and the allocation-free `TryComputeHashFrom(previousHash, newData, destination, out bytesWritten)`. The result equals a single pass over the concatenated input; the original bytes are never needed again.

<!-- compile -->
```csharp
using System.IO.Hashing;
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

byte[] head = "the quick brown fox"u8.ToArray();
byte[] tail = " jumps over the lazy dog"u8.ToArray();

foreach (NonCryptographicHashAlgorithm algorithm in new NonCryptographicHashAlgorithm[]
{
    new Crc(CrcStandard.CRC32_ISOHDLC), new Fnv1a64(), new Fletcher32(), new Adler32(),
})
{
    var resumable = (IResumableHashAlgorithm)algorithm;

    byte[] stored  = algorithm.ComputeHash(head);                   // persisted with the log
    byte[] resumed = resumable.ComputeHashFrom(stored, tail);       // extend without re-reading head
    byte[] direct  = algorithm.ComputeHash([.. head, .. tail]);     // resumed.SequenceEqual(direct) == true

    byte[] destination = new byte[resumable.HashLengthInBytes];
    bool ok = resumable.TryComputeHashFrom(stored, tail, destination, out int written);
}
```

How each family resumes differs, and it dictates the one rule: **resume only with the same algorithm and parameters**. `Crc` reverse-finalizes the digest (undoing XOR-out, output reflection, and width masking) to recover the register, so a digest from a different `CrcStandard` yields garbage. FNV applies no finalization — the digest is the accumulator. Fletcher and Adler digests carry both accumulators (`B ‖ A`), which seed the sums directly. A `previousHash` whose length is not `HashLengthInBytes` throws `ArgumentException` (`previousHash`). The fingerprints that mix or finalize irreversibly — CityHash, MurmurHash3, Pearson, the classic string hashes — do not implement the interface.

## Digest byte order

`GetCurrentHash` serializes the final register in an order that is **not** uniform across the library. When a value looks "reversed" next to another tool, this is why:

| Family | Byte order | `"123456789"` / reference | Bytes returned |
|---|---|---|---|
| `Crc` (any standard) | little-endian, low byte first | CRC-32/ISO-HDLC check value `0xCBF43926` | `26 39 F4 CB` |
| `Crc` | little-endian | CRC-16/XMODEM check value `0x31C3` | `C3 31` |
| FNV-1 / FNV-1a | big-endian | FNV-1a-32 of `"a"` = `0xE40C292C` | `E4 0C 29 2C` |
| Fletcher-16 / 32 / 64 | big-endian, `B ‖ A` | Fletcher-32 of `"abcde"`: `B = 0x05C3`, `A = 0x01EF` | `05 C3 01 EF` |
| Adler-32 / 32C / 64 | big-endian, `(B << k) \| A` | Adler-32 of `"Wikipedia"` = `0x11E60398` | `11 E6 03 98` |

So `BitConverter.ToUInt32(new Crc().ComputeHash("123456789"u8.ToArray()))` yields `0xCBF43926` on a little-endian host, while the Adler and Fletcher arrays drop straight into a big-endian zlib trailer. Bodu's Fletcher consumes input **one byte at a time** at every width (so its Fletcher-32 is not the 16-bit-word variant some references tabulate) — see [Using Fletcher](fletcher.md). The [concepts page](../../docs/io-hashing/concepts.md#endianness) covers the CRC reflection parameters, which are a separate question from digest byte order.

<!-- compile -->
```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

byte[] check = "123456789"u8.ToArray();
byte[] crc32   = new Crc().ComputeHash(check);                                           // 26 39 F4 CB
byte[] xmodem  = new Crc(CrcStandard.Get(CrcStandards.CRC16_XMODEM)).ComputeHash(check); // C3 31
byte[] adler   = new Adler32().ComputeHash("Wikipedia"u8.ToArray());                     // 11 E6 03 98
byte[] fnv     = new Fnv1a32().ComputeHash("a"u8.ToArray());                             // E4 0C 29 2C
uint crcWord = BitConverter.ToUInt32(crc32);                                             // 0xCBF43926 on little-endian hosts
```

## API summary

| Type | Members |
|---|---|
| <xref:Bodu.IO.Hashing.HashingStream> | ctor `(Stream, NonCryptographicHashAlgorithm, bool leaveOpen = false)`; `Algorithm`; `GetCurrentHash()`, `GetHashAndReset()`; `Read` / `Write` (+ `Span`, `Memory`, `Task` overloads), `ReadByte`, `WriteByte`, `Flush[Async]`, `DisposeAsync`; `CanSeek == false` |
| <xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> | `ComputeHash(…)`, `ComputeHashAsync(Stream, int = 81920, CancellationToken)`, `AppendData(…)`, `AppendDataAsync(Stream, int = 4096, CancellationToken)`, `VerifyHash(…)`, `VerifyHashAsync(…)`, `TryVerifyHash(…)`, `TryVerifyHashAsync(…)` |
| <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> | `HashLengthInBytes`; `ComputeHashFrom(span, span)`, `ComputeHashFrom(byte[], byte[])`, `ComputeHashFrom(byte[], byte[], int offset, int length)`; `TryComputeHashFrom(previousHash, newData, destination, out int bytesWritten)` |
| Implementers | `Crc`, `Fnv132`, `Fnv164`, `Fnv1a32`, `Fnv1a64`, `Fletcher16`, `Fletcher32`, `Fletcher64`, `Adler32`, `Adler32C`, `Adler64` |

## Where to go next

- [Using CRC](crc.md#pattern-6--resume-from-a-stored-digest) — the CRC-specific reverse-finalization in more depth.
- [Using Fletcher](fletcher.md) and [Using Adler](adler.md) — the twin-accumulator checksums this page resumes.
- [Streams and async](../cryptography/streaming-and-async.md) — the equivalent surfaces for the cryptographic hashes and ciphers.
- [Runnable samples](../../samples/io-hashing.md) — `Bodu.IO.Hashing.Samples.ChecksumTour` exercises `HashingStream` and resumable hashing over a committed file.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
