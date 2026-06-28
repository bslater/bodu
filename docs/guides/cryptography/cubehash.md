---
title: Using CubeHash
---

# Using CubeHash

CubeHash is Daniel J. Bernstein's 2008 submission to the NIST SHA-3 competition. It is a sponge-style hash built from a single simple permutation ("Cube"), parameterized by round counts and block size so you can dial the speed / margin trade-off. The default configuration reproduces the **CubeHash 16/32–512** variant submitted to the SHA-3 contest.

The type is <xref:Bodu.Security.Cryptography.CubeHash>, and it derives from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>.

## Tunable parameters at a glance

| Property | Default | Range | Notes |
|---|---|---|---|
| `HashSize` | 512 bits | One of **224, 256, 384, 512** (`CubeHash.MinHashSize` = 224, `MaxHashSize` = 512) | Output width — a discrete set, not any multiple of 8. |
| `TransformBlockSize` | 32 bytes | 1–128 (`MinInputBlockSize` / `MaxInputBlockSize`) | Bytes absorbed per permutation call. Larger = faster per byte. |
| `Rounds` | 16 | 1–4096 (`MinRounds` / `MaxRounds`) | Permutation rounds between blocks. More rounds = more margin, slower. |
| `InitializationRounds` | 16 | 1–4096 | Permutation rounds during init. |
| `FinalizationRounds` | 32 | 1–4096 | Permutation rounds after the last block. |

`CubeHash` derives directly from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, so the standard `ComputeHash` / `TransformBlock` lifecycle applies. A `HashSize` outside the permitted set throws `ArgumentOutOfRangeException` at construction (or when the setter runs).

The published naming convention is **CubeHash `r+b`/`w+f`-`h`** — initialization rounds `i`, transform rounds `r`, block size `b`, finalization rounds `f`, output bits `h`. `AlgorithmName` reflects the current configuration, e.g. `"CubeHash16+16/32+32-512"`.

## Pattern 1 — default CubeHash (the SHA-3 submission)

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var cube = new CubeHash();              // default 512-bit output, 16/32-512 parameters
byte[] digest = cube.ComputeHash(data);       // 64 bytes
```

This is the configuration referenced by most published CubeHash test vectors.

## Pattern 2 — a shorter digest

Reduce the hash size before hashing begins:

```csharp
using Bodu.Security.Cryptography;

using var cube = new CubeHash { HashSize = 256 };   // 32-byte digest
byte[] digest = cube.ComputeHash(data);
```

`HashSize` must be one of 224, 256, 384, or 512 bits — the same discrete set the SHA-3 submission defined, not an arbitrary multiple of 8. It is only settable before the first `ComputeHash` / `TransformBlock` — changing it mid-stream would invalidate the state, so the setter throws once hashing has started.

## Pattern 3 — trading speed for margin

Increase the transform rounds or the block size to move along the speed / margin curve:

```csharp
using Bodu.Security.Cryptography;

// Faster: larger blocks, fewer rounds between them.
using var fast = new CubeHash
{
    TransformBlockSize = 64,
    Rounds             = 8,
    HashSize           = 256,
};

// More conservative margin: smaller blocks, more rounds.
using var safe = new CubeHash
{
    TransformBlockSize = 16,
    Rounds             = 32,
    FinalizationRounds = 64,
    HashSize           = 512,
};
```

All four round-count properties (`Rounds`, `InitializationRounds`, `FinalizationRounds`) and `TransformBlockSize` are only settable before the first `ComputeHash` / `TransformBlock`. The `AlgorithmName` surfaces the effective parameters:

```csharp
Console.WriteLine(fast.AlgorithmName);   // e.g. "CubeHash16+8/64+32-256"
Console.WriteLine(safe.AlgorithmName);   // e.g. "CubeHash16+32/16+64-512"
```

## Pattern 4 — streaming

CubeHash plugs into the BCL streaming shape:

```csharp
using Bodu.Security.Cryptography;

using var cube = new CubeHash();

using var stream = File.OpenRead("archive.bin");
byte[] digest = cube.ComputeHash(stream);
```

`CanReuseTransform` is `true`, so the same `CubeHash` instance can hash multiple independent inputs in a loop — call `Initialize()` between messages, or just let the next `ComputeHash` reset the state for you.

## Pattern 5 — matching published test vectors

The CubeHash test-vector files in the NIST competition submission use the notation `CubeHash i+r/b+f-h`. To reproduce a specific vector, set all four parameters plus `HashSize`:

```csharp
using Bodu.Security.Cryptography;

// CubeHash 16+32/32+32-256 — a common reference configuration.
using var cube = new CubeHash
{
    InitializationRounds = 16,
    Rounds               = 32,
    TransformBlockSize   = 32,
    FinalizationRounds   = 32,
    HashSize             = 256,
};
```

## Security caveats

- **Status.** CubeHash was a first-round SHA-3 *submission* — it did not advance to the final round (the finalists were BLAKE, Grøstl, JH, Keccak, and Skein). The default `16/32` configuration is unbroken but conservative parameterizations exist precisely because the low-round variants have a thinner margin; do not reduce `Rounds` below the default for security-sensitive use.
- **Output size vs security level.** As with any digest, an n-bit output gives ≈ n-bit pre-image resistance but only ≈ n/2-bit collision resistance. The 224-bit minimum therefore offers 112-bit collision resistance — pick 256 or wider when collisions matter.
- **Length extension.** CubeHash is a sponge construction, so it is immune to length extension and is safe to use as a keyed MAC by prepending the key (HMAC is unnecessary) — but no native MAC mode is exposed here; for a built-in keyed mode reach for [Skein](skein.md) or [BLAKE2b](blake.md).

## When to use CubeHash

- **Research** into sponge constructions, SHA-3 alternatives, or round-count vs speed trade-offs.
- **Interoperability** with systems that use a specific CubeHash parameterization.

For new work without an interoperability requirement, the BCL's SHA-2 and SHA-3 families are hardware-accelerated and have broader analysis behind them. CubeHash's value is its knob-turnable parameter space — useful when that is specifically what you want.

## Where to go next

- [Hashing overview](hashing.md) — where CubeHash sits next to Tiger, SipHash, Snefru, and the non-cryptographic families.
- [Using Tiger](tiger.md), [Using Snefru](snefru.md) — the other cryptographic digests in this package.
- [Bodu.Security.Cryptography namespace page](xref:Bodu.Security.Cryptography).
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
