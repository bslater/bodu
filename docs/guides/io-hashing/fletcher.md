---
title: Using Fletcher
---

# Using Fletcher

The Fletcher checksum family maintains two running accumulators, **A** and **B**, both reduced modulo a prime near 2^(N ⁄ 2). Each input word updates `A = (A + word) mod M`, then `B = (B + A) mod M`. The final output is `B ‖ A` truncated to the chosen width. Because `B` depends on the running `A`, Fletcher catches transpositions — swapping two words changes `B` even though the sum is unchanged — which a simple additive checksum misses.

![Fletcher twin-accumulator structure](../../images/diagrams/fletcher-accumulators.svg)

**Bodu.IO.Hashing** provides three widths: <xref:Bodu.IO.Hashing.Fletcher16>, <xref:Bodu.IO.Hashing.Fletcher32>, and <xref:Bodu.IO.Hashing.Fletcher64>. All three derive from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>.

## Pattern 1 — compute a digest in one call

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var fletcher = new Fletcher32();
fletcher.Append(data);
byte[] digest = fletcher.GetCurrentHash();
string hex = Convert.ToHexString(digest);  // 4 bytes, 8 hex characters
```

Swap `Fletcher32` for `Fletcher16` or `Fletcher64` depending on how much collision space you need.

## Pattern 2 — the `Append` / `GetCurrentHash` / `Reset` lifecycle

The BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> exposes three methods that Fletcher honours verbatim:

```csharp
using Bodu.IO.Hashing;

using var fletcher = new Fletcher64();

fletcher.Append(chunk1);                  // update A and B
fletcher.Append(chunk2);                  // continue
byte[] partial = fletcher.GetCurrentHash(); // snapshot, non-destructive
fletcher.Append(chunk3);                  // still works — GetCurrentHash did not change state
byte[] full = fletcher.GetCurrentHash();

fletcher.Reset();                         // back to zeroed A and B
```

`GetCurrentHash` finalises on a copy of the accumulators, so calling it mid-stream is cheap and safe.

## Pattern 3 — streaming over a file

```csharp
using Bodu.IO.Hashing;

using var fletcher = new Fletcher32();

using (FileStream fs = File.OpenRead("archive.bin"))
{
    byte[] buffer = new byte[64 * 1024];
    int read;
    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
    {
        fletcher.Append(buffer.AsSpan(0, read));
    }
}

byte[] fingerprint = fletcher.GetCurrentHash();
```

Fletcher's block size is fixed by the width (Fletcher-16 works in 1-byte words, Fletcher-32 in 2-byte words, Fletcher-64 in 4-byte words). Partial final blocks are zero-padded before the last update — the algorithm handles this internally.

## Picking a width

| Width | Use when | Notes |
|---|---|---|
| <xref:Bodu.IO.Hashing.Fletcher16> | Tiny frames, embedded protocols, when 16 bits of collision space is enough. | 1-byte blocks, fastest; same collision properties as CRC-16 for short inputs. |
| <xref:Bodu.IO.Hashing.Fletcher32> | General-purpose file and buffer checksums; the most common choice. | 2-byte blocks; comparable to CRC-32 for error detection on uncorrelated noise, cheaper to compute. |
| <xref:Bodu.IO.Hashing.Fletcher64> | Large buffers where a 32-bit space is uncomfortable. | 4-byte blocks; rarely needed, but useful for very long streams. |

## Fletcher vs CRC

- **CRC** is defined by polynomial arithmetic over GF(2). It is better at detecting bursts of errors that align with its polynomial structure. Reach for it when you need to match a standard on the wire (zlib, PNG, Modbus, etc.).
- **Fletcher** is defined by two running sums. It is cheaper to compute on a general-purpose CPU and still catches transpositions. Reach for it when you control both endpoints and want a simple, fast, position-dependent checksum.

Both are **non-cryptographic** — neither resists a motivated adversary. If you need authentication, see the [cryptography hashing guide](../cryptography/hashing.md) (SipHash, Tiger, Merkle trees).

## Where to go next

- [Using CRC](crc.md) — the other checksum family in this package.
- [Bodu.IO.Hashing namespace page](../../apidoc/Bodu.IO.Hashing.md) — key types and design notes.
