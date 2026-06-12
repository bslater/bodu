---
title: Using Snefru
---

# Using Snefru

Snefru is a 1990 cryptographic hash by Ralph Merkle, named after the Egyptian pharaoh. It runs a block-mix function over the input and keeps the low half of the state as output. **Bodu.Security.Cryptography** ships both published widths:

| Type | Output | Block size |
|---|---|---|
| <xref:Bodu.Security.Cryptography.Snefru128> | 128 bits | 48 bytes |
| <xref:Bodu.Security.Cryptography.Snefru256> | 256 bits | 32 bytes |

Both derive from a shared `Snefru<T>` base, which in turn derives from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>.

> [!IMPORTANT]
> Snefru is **cryptographically broken**. Eli Biham showed practical collisions on Snefru-2 (the published 2-pass variant) in 2008. The implementation in this package is provided for **interoperability with legacy systems and for research**, not for protecting real data. For any new work where you need a cryptographic hash, use the BCL's `System.Security.Cryptography.SHA256` / `System.Security.Cryptography.SHA512`, or <xref:Bodu.Security.Cryptography.Tiger> if you need 192-bit output.

## Pattern 1 — compute a digest

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var snefru = new Snefru256();
byte[] digest   = snefru.ComputeHash(data);   // 32 bytes
string hex      = Convert.ToHexString(digest);
```

Swap `Snefru256` for `Snefru128` for a 16-byte digest.

## Pattern 2 — streaming

Snefru inherits the standard BCL streaming shape:

```csharp
using Bodu.Security.Cryptography;

using var snefru = new Snefru256();
using var stream = File.OpenRead("archive.bin");
byte[] digest = snefru.ComputeHash(stream);
```

You can also drive it block-by-block via `TransformBlock` / `TransformFinalBlock`.

## Pattern 3 — the two widths

The two classes are completely independent — the block size differs (48 bytes for Snefru-128, 32 bytes for Snefru-256), and the outputs are not truncations of one another. Pick the width your interoperating system specifies.

```csharp
using Bodu.Security.Cryptography;

using var snefru128 = new Snefru128();   // 128-bit output, 48-byte blocks
using var snefru256 = new Snefru256();   // 256-bit output, 32-byte blocks
```

## When to use Snefru

- **Interoperability** with a legacy system that already uses Snefru (rare — Snefru is mostly of historical interest).
- **Research** into early hash-function design, or as a test case for cryptanalysis tooling.

For everything else, pick a modern digest. The [hashing overview](hashing.md) lists the options in this package; for brand-new work, the BCL's SHA-2 family is the right default.

## Where to go next

- [Hashing overview](hashing.md) — how Snefru compares to the other hashes in this package.
- [Using Tiger](tiger.md) — another classic cryptographic hash with wider deployment.
- [Using CubeHash](cubehash.md) — a modern, highly tunable cryptographic hash (SHA-3 finalist).
- [Bodu.Security.Cryptography namespace page](xref:Bodu.Security.Cryptography).
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
