---
title: Using CityHash
---

# Using CityHash

CityHash is Google's family of fast, high-quality non-cryptographic hash functions. Each variant is carefully tuned for the input lengths it is asked to hash — short-key paths avoid loops entirely, medium-length paths use Murmur-style mixing, and long-input paths split the buffer into 64-byte chunks that are mixed in parallel. The result is a hash that is substantially faster than FNV or Adler on long inputs while distributing at least as well on short ones.

**Bodu.IO.Hashing** ships three widths:

| Type | Width | Notes |
|---|---|---|
| <xref:Bodu.IO.Hashing.CityHash32> | 32 bits | Small, fast, drop-in replacement for FNV-1a-32 as a hash-table function. |
| <xref:Bodu.IO.Hashing.CityHash64> | 64 bits | The most common CityHash choice — 64-bit fingerprints for de-duplication, sharding, content addressing. |
| <xref:Bodu.IO.Hashing.CityHash128> | 128 bits | Longer fingerprint space, still cheaper than a cryptographic digest. |

All three derive from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> via a shared `CityHash<T>` base.

## Pattern 1 — compute a digest in one call

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var city = new CityHash64();
city.Append(data);
byte[] digest = city.GetCurrentHash();
string hex    = Convert.ToHexString(digest);   // 8 bytes, 16 hex characters
```

Substitute `CityHash32` or `CityHash128` when the width needs to change.

## Pattern 2 — 64-bit fingerprint for deduplication or sharding

```csharp
using System.Text;
using Bodu.IO.Hashing;

ulong FingerprintFor(ReadOnlySpan<byte> data)
{
    using var city = new CityHash64();
    city.Append(data);
    return BitConverter.ToUInt64(city.GetCurrentHash());
}

int shardFor = (int)(FingerprintFor(recordBytes) % (ulong)shardCount);
```

The distribution is good enough that `% shardCount` gives very close to uniform spread. The hash is **not** keyed — do not use this for adversary-facing sharding where crafted input could be used to overload a shard.

## Pattern 3 — `Append` / `GetCurrentHash` / `Reset`

CityHash's reference implementation is a one-shot algorithm: it reads the whole buffer, decides which length-specialised path to take, and returns a result. To plug that into the streaming <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract, the Bodu implementation **buffers the appended bytes** and applies the final mixing on `GetCurrentHash`.

```csharp
using Bodu.IO.Hashing;

using var city = new CityHash64();

city.Append(chunk1);                       // stored
city.Append(chunk2);                       // stored
byte[] partial = city.GetCurrentHash();    // snapshot — mixes the buffered bytes
city.Append(chunk3);                       // state preserved after GetCurrentHash
byte[] full = city.GetCurrentHash();

city.Reset();                              // discards the buffer
```

The practical consequence is that memory use grows linearly with the amount of data appended between `Reset` calls. If you need to hash a long stream without buffering, reach for <xref:Bodu.IO.Hashing.Fnv1a64>, <xref:Bodu.IO.Hashing.Crc>, or one of the <xref:Bodu.IO.Hashing.Fletcher32>-family types — they update in place.

## Pattern 4 — file hashing

For files that comfortably fit in memory (tens or hundreds of MB), the streaming form is fine:

```csharp
using Bodu.IO.Hashing;

using var city = new CityHash64();

using (var stream = File.OpenRead("asset.bin"))
    city.Append(stream);

byte[] fingerprint = city.GetCurrentHash();
```

For very large files where you do not want the whole buffer in memory, do the hashing chunk-by-chunk with an incremental non-cryptographic algorithm (<xref:Bodu.IO.Hashing.Crc>, <xref:Bodu.IO.Hashing.Fnv1a64>) or use a Merkle tree — see the [cryptography hashing guide](../cryptography/hashing.md) for the `MerkleTreeHash` pattern.

## CityHash vs the other non-cryptographic hashes in this package

- **vs <xref:Bodu.IO.Hashing.Fnv1a64>** — CityHash is faster on long inputs (SIMD-friendly); FNV uses constant memory regardless of input size.
- **vs <xref:Bodu.IO.Hashing.Adler32>** — Adler is tuned for short checksums and has a fixed 4-byte digest. CityHash dominates for general-purpose in-memory fingerprinting.
- **vs <xref:Bodu.IO.Hashing.Crc>** — CRC is defined by wire specifications and has provable burst-error detection; CityHash is a better default when you control both endpoints and want the best speed/quality trade-off.
- **vs <xref:Bodu.Security.Cryptography.SipHash64>** — SipHash is keyed and resists adversarial collisions; CityHash does not. Use SipHash whenever untrusted input can reach the hash function.

CityHash is **not cryptographic**. An attacker who can choose inputs can construct collisions trivially. Do not use it to authenticate or to key-derive.

## Where to go next

- [Using FNV](fnv.md) — the simpler, streaming-friendly alternative.
- [Using Adler](adler.md), [Using CRC](crc.md), [Using Fletcher](fletcher.md) — the checksum families.
- [Cryptography hashing guide](../cryptography/hashing.md) — when you need SipHash's adversarial resistance or a cryptographic digest.
- [Bodu.IO.Hashing namespace page](../../apidoc/Bodu.IO.Hashing.md) — key types and design notes.
