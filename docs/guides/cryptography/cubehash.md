---
title: Using CubeHash
---

# Using CubeHash

CubeHash is Daniel J. Bernstein's 2008 submission to the NIST SHA-3 competition. It is a sponge-style hash built from a single simple permutation ("Cube"), parameterised by round counts and block size so you can dial the speed / margin trade-off. The default configuration reproduces the **CubeHash 16/32–512** variant submitted to the SHA-3 contest.

The type is <xref:Bodu.Security.Cryptography.CubeHash>, and it derives from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>.

## Tunable parameters at a glance

| Property | Default | Range | Notes |
|---|---|---|---|
| `HashSize` | 512 bits | 8–512, multiple of 8 (`CubeHash.MinHashSize` / `MaxHashSize`) | Output width. |
| `TransformBlockSize` | 32 bytes | 1–128 (`MinInputBlockSize` / `MaxInputBlockSize`) | Bytes absorbed per permutation call. Larger = faster per byte. |
| `Rounds` | 16 | 1–4096 (`MinRounds` / `MaxRounds`) | Permutation rounds between blocks. More rounds = more margin, slower. |
| `InitializationRounds` | 16 | 1–4096 | Permutation rounds during init. |
| `FinalizationRounds` | 32 | 1–4096 | Permutation rounds after the last block. |

The published naming convention is **CubeHash `r+b`/`w+f`-`h`** — initialisation rounds `i`, transform rounds `r`, block size `b`, finalisation rounds `f`, output bits `h`. `AlgorithmName` reflects the current configuration, e.g. `"CubeHash16+16/32+32-512"`.

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

`HashSize` must be a multiple of 8 between 8 and 512. It is only settable before the first `ComputeHash` / `TransformBlock` — changing it mid-stream would invalidate the state, so the setter throws once hashing has started.

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

## When to use CubeHash

- **Research** into sponge constructions, SHA-3 alternatives, or round-count vs speed trade-offs.
- **Interoperability** with systems that use a specific CubeHash parameterisation.

For new work without an interoperability requirement, the BCL's SHA-2 and SHA-3 families are hardware-accelerated and have broader analysis behind them. CubeHash's value is its knob-turnable parameter space — useful when that is specifically what you want.

## Where to go next

- [Hashing overview](hashing.md) — where CubeHash sits next to Tiger, SipHash, Snefru, and the non-cryptographic families.
- [Using Tiger](tiger.md), [Using Snefru](snefru.md) — the other cryptographic digests in this package.
- [Bodu.Security.Cryptography namespace page](../../apidoc/Bodu.Security.Cryptography.md).
