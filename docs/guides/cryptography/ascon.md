---
title: Using ASCON-HASH256 and ASCON-HASHA256
---

# Using ASCON-HASH256 and ASCON-HASHA256

ASCON-HASH256 and ASCON-HASHA256 are the two fixed-output hash algorithms from the **ASCON** family, standardised by NIST in SP 800-232. Both are sponge constructions built on a 320-bit internal state with an 8-byte (64-bit) absorption rate, and both produce a **256-bit (32-byte) digest**. The family was designed for constrained hardware — but the same properties that make it attractive there (small state, simple round function, well-studied security) make it a sound choice in software as well.

**Bodu.Security.Cryptography** ships two concrete types:

| Type | Algorithm name | Absorption rounds | Final-block rounds | Characteristics |
|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.AsconHash256> | `ASCON-HASH256` | 12 (Ascon-p12) | 12 | Maximum security margin — the conservative default. |
| <xref:Bodu.Security.Cryptography.AsconHashA256> | `ASCON-HASHA256` | 8 (Ascon-p8) | 12 | Higher throughput — reduced but still substantial security margin. |

Both derive from `BlockHashAlgorithm<T>`, which in turn derives from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, so they slot into any API that accepts a standard .NET hash.

## Fixed parameters at a glance

| Parameter | Value | Notes |
|---|---|---|
| State size | 320 bits (40 bytes) | Fixed by the ASCON specification. |
| Rate (absorption block) | 64 bits (8 bytes) | Bytes consumed per permutation call. |
| Output | 256 bits (32 bytes) | Fixed; no truncation variants. |
| Permutation rounds — `AsconHash256` | 12 per absorbed block; 12 for final block | Conservative margin throughout. |
| Permutation rounds — `AsconHashA256` | 8 per absorbed block; 12 for final block | Reduced absorption work, unchanged squeeze. |

## Pattern 1 — one-shot hash with ASCON-HASH256

`AsconHash256` is the conservative choice. It uses 12 permutation rounds at every stage, matching the security margin of the original ASCON submission.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data   = Encoding.UTF8.GetBytes("the quick brown fox");

using var hash   = new AsconHash256();
byte[]    digest = hash.ComputeHash(data);          // 32 bytes
string    hex    = Convert.ToHexString(digest);
```

`CanReuseTransform` is `true` on both types, so the same instance can hash multiple independent messages in a loop without being recreated or re-initialised.

## Pattern 2 — ASCON-HASHA256 for throughput-sensitive paths

`AsconHashA256` uses 8 permutation rounds per absorbed block instead of 12. The API is identical — only the number of internal rounds (and therefore the throughput and security margin) differs.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data   = Encoding.UTF8.GetBytes("the quick brown fox");

using var hash   = new AsconHashA256();
byte[]    digest = hash.ComputeHash(data);
```

`AlgorithmName` reports `"ASCON-HASHA256"` automatically, so logs and manifests carry the variant name without any extra bookkeeping.

## Pattern 3 — choosing between the two variants

Both variants share the same state size, output width, padding rule, and API shape. The sole difference is the number of Ascon-p rounds applied between each 8-byte absorbed block.

```csharp
using Bodu.Security.Cryptography;

// Conservative: 12 absorption rounds.
// Prefer for signatures, long-lived commitments, content-addressing,
// or wherever throughput is not the primary constraint.
using var conservative = new AsconHash256();

// Performance: 8 absorption rounds.
// Prefer when hashing many inputs in a hot path (deduplication,
// per-request cache-key generation) and the throughput difference matters.
using var performance = new AsconHashA256();

Console.WriteLine(conservative.AlgorithmName);   // "ASCON-HASH256"
Console.WriteLine(performance.AlgorithmName);    // "ASCON-HASHA256"
```

The 12-round variant applies 50 % more permutation work per absorbed block, giving deeper cryptanalytic margin. The 8-round variant reduces that work in exchange for higher throughput; the squeeze phase retains 12 rounds in both cases, so the finalisation step is equally strong.

When in doubt, use `AsconHash256`. Switch to `AsconHashA256` only when profiling shows the difference is measurable in your workload.

## Pattern 4 — streaming a file

Both types inherit `ComputeHash(Stream)` from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>:

```csharp
using Bodu.Security.Cryptography;

using var hash   = new AsconHash256();
using var stream = File.OpenRead("document.pdf");
byte[]    digest = hash.ComputeHash(stream);
```

For a block-by-block pipeline, drive the standard BCL `TransformBlock` / `TransformFinalBlock` contract directly:

```csharp
using Bodu.Security.Cryptography;

using var hash   = new AsconHash256();
byte[]    buffer = new byte[8192];
int       read;

using (var stream = File.OpenRead("document.pdf"))
{
    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        hash.TransformBlock(buffer, 0, read, null, 0);
}

hash.TransformFinalBlock([], 0, 0);
byte[] digest = hash.Hash!;
```

`CanReuseTransform` being `true` means the instance resets automatically at the next `ComputeHash` call, or you can call `Initialize()` explicitly between messages.

## Pattern 5 — verifying a digest in constant time

Always compare digests in constant time. The `VerifyHash` helper in `Bodu.Security.Cryptography.Extensions` wraps <xref:System.Security.Cryptography.CryptographicOperations.FixedTimeEquals*>:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] expected = LoadExpectedDigest("document.pdf");

using var hash = new AsconHash256();
bool ok = hash.VerifyHash(fileBytes, expected);
```

A plain `SequenceEqual` comparison leaks timing information to a side-channel observer and is unsafe when the comparison result drives an authentication or integrity decision.

## When to use ASCON

- **Standards compliance** — both variants are NIST-approved (SP 800-232), making them appropriate for contexts that require a formally standardised algorithm beyond SHA-2.
- **Content addressing or deduplication** — 256-bit output gives strong collision resistance with a compact 32-byte footprint.
- **Constrained environments** — the 320-bit state and simple permutation are designed for embedded and IoT targets where SHA-2 hardware acceleration is unavailable.
- **Performance-sensitive paths without hardware acceleration** — `AsconHashA256` can outperform software SHA-2 on targets where AES-NI is absent, because the Ascon-p permutation is efficient in software.

On x86-64 with AES-NI available, the BCL's hardware-accelerated <xref:System.Security.Cryptography.SHA256?displayProperty=nameWithType> will generally outperform software ASCON. Use ASCON when you need a standards-backed alternative, are targeting hardware without SHA-2 acceleration, or have an interoperability requirement that specifies the ASCON family specifically.

## Where to go next

- [Hashing overview](hashing.md) — how ASCON sits alongside Tiger, SipHash, CubeHash, and the non-cryptographic families.
- [Using Tiger](tiger.md) — a 192-bit cryptographic digest optimised for 64-bit software with a long track record.
- [Using CubeHash](cubehash.md) — another sponge-style hash with tunable round counts; useful for research into the speed / margin trade-off.
- [Bodu.Security.Cryptography namespace page](../../apidoc/Bodu.Security.Cryptography.md).
