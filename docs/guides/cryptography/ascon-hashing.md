---
title: ASCON hashing — AsconHash256 and AsconHashA256
---

# ASCON hashing — AsconHash256 and AsconHashA256

`ASCON-HASH256` and `ASCON-HASHA256` are the two fixed-output hash algorithms in the ASCON
family, standardized in NIST SP 800-232. Both are sponge constructions built on a 320-bit
internal state with an 8-byte (64-bit) absorption rate, and both produce a **256-bit (32-byte)
digest**. The family was designed for constrained hardware — but the same properties that make
it attractive there (compact state, simple round function, well-studied security margin) make it
a sound choice in software as well.

**Bodu.Security.Cryptography** ships two concrete types:

| Type | Algorithm name | Absorption rounds | Squeeze rounds | Characteristics |
|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.AsconHash256> | `ASCON-HASH256` | 12 (Ascon-p12) | 12 | Maximum security margin — the conservative default. |
| <xref:Bodu.Security.Cryptography.AsconHashA256> | `ASCON-HASHA256` | 8 (Ascon-p8) | 12 | Higher throughput — reduced, but still substantial, absorption-phase margin. |

Both derive from `BlockHashAlgorithm<T>`, which in turn derives from
<xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, so they slot
into any API that accepts a standard .NET hash algorithm.

## Fixed parameters at a glance

| Parameter | Value | Notes |
|---|---|---|
| State size | 320 bits (40 bytes) | Fixed by the ASCON specification. |
| Rate (absorption block) | 64 bits (8 bytes) | Bytes consumed per permutation call. |
| Output | 256 bits (32 bytes) | Fixed; no truncation variants. |
| Absorption rounds — `AsconHash256` | 12 per block | Conservative margin throughout. |
| Absorption rounds — `AsconHashA256` | 8 per block | Reduced absorption work; squeeze unchanged. |

## Pattern 1 — one-shot hash with ASCON-HASH256

`AsconHash256` is the conservative choice. It uses 12 permutation rounds at every stage, giving
the widest cryptanalytic margin in the family.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data    = Encoding.UTF8.GetBytes("the quick brown fox");
using var hash = new AsconHash256();
byte[] digest  = hash.ComputeHash(data);    // 32 bytes
string hex     = Convert.ToHexString(digest);
```

`CanReuseTransform` is `true` on both types, so the same instance can hash multiple independent
messages without being recreated — `ComputeHash` resets the state automatically.

## Pattern 2 — ASCON-HASHA256 for throughput-sensitive paths

`AsconHashA256` uses 8 permutation rounds per absorbed block instead of 12. The API is
identical; only the round count (and therefore the throughput and absorption-phase margin) differs.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data    = Encoding.UTF8.GetBytes("the quick brown fox");
using var hash = new AsconHashA256();
byte[] digest  = hash.ComputeHash(data);
```

`AlgorithmName` reports `"ASCON-HASHA256"` automatically, so logs and manifests carry the
variant name without any extra bookkeeping.

## Pattern 3 — choosing between the two variants

Both variants share the same state size, output width, padding rule, and API shape. The only
difference is the number of Ascon-p rounds applied per absorbed 8-byte block.

```csharp
using Bodu.Security.Cryptography;

// Conservative — 12 absorption rounds.
// Prefer for signatures, long-lived commitments, content-addressing,
// or wherever throughput is not the primary constraint.
using var conservative = new AsconHash256();

// Performance — 8 absorption rounds.
// Prefer when hashing many inputs in a hot path (deduplication,
// per-request cache-key generation) and the throughput difference matters.
using var performance = new AsconHashA256();

Console.WriteLine(conservative.AlgorithmName);    // "ASCON-HASH256"
Console.WriteLine(performance.AlgorithmName);     // "ASCON-HASHA256"
```

The 12-round variant applies 50 % more permutation work per absorbed block, giving deeper
cryptanalytic margin. The 8-round variant reduces absorption work in exchange for higher
throughput; the squeeze phase retains 12 rounds in both cases, so finalization is equally strong
in both variants.

When in doubt, use `AsconHash256`. Switch to `AsconHashA256` only when profiling shows the
difference is measurable in your workload.

## Pattern 4 — streaming a file

Both types inherit `ComputeHash(Stream)` from
<xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>:

```csharp
using Bodu.Security.Cryptography;

using var hash   = new AsconHash256();
using var stream = File.OpenRead("document.pdf");
byte[]    digest = hash.ComputeHash(stream);
```

For a block-by-block pipeline, drive the standard BCL `TransformBlock` / `TransformFinalBlock`
contract directly:

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

`CanReuseTransform` being `true` means the instance resets automatically at the next
`ComputeHash` call, or you can call `Initialize()` explicitly between messages.

## Pattern 5 — hashing multiple messages with one instance

Because `CanReuseTransform` is `true`, a single instance can be reused across a batch:

```csharp
using Bodu.Security.Cryptography;

byte[][] messages = GetMessageBatch();
byte[][] digests  = new byte[messages.Length][];

using var hash = new AsconHash256();
for (int i = 0; i < messages.Length; i++)
    digests[i] = hash.ComputeHash(messages[i]);
```

Each `ComputeHash` call resets the internal state before processing the next message.

## Pattern 6 — verifying a digest in constant time

Always compare digests in constant time. Use the BCL's
`CryptographicOperations.FixedTimeEquals` directly, or the
`VerifyHash` extension method from `Bodu.Security.Cryptography.Extensions`:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] expected = LoadExpectedDigest();
byte[] computed;

using var hash = new AsconHash256();
computed = hash.ComputeHash(fileBytes);

bool ok = CryptographicOperations.FixedTimeEquals(computed, expected);
```

A plain `SequenceEqual` or `==` comparison leaks timing information and is unsafe whenever the
comparison result drives an authentication or integrity decision.

## Pattern 7 — reading the algorithm name at runtime

Both types report their canonical NIST algorithm identifier through the `AlgorithmName` property:

```csharp
using Bodu.Security.Cryptography;

using var h256  = new AsconHash256();
using var ha256 = new AsconHashA256();

Console.WriteLine(h256.AlgorithmName);     // "ASCON-HASH256"
Console.WriteLine(ha256.AlgorithmName);    // "ASCON-HASHA256"
```

This is useful for constructing audit log entries, manifest files, or HTTP response headers that
need to identify the algorithm used.

## When to use ASCON hashing

| Scenario | Recommendation |
|---|---|
| Standards-backed 256-bit digest | `AsconHash256` — NIST SP 800-232 approved |
| Content addressing or deduplication | Either; 256-bit output gives strong collision resistance |
| Constrained hardware, no SHA-2 acceleration | `AsconHashA256` — efficient software permutation |
| High-throughput pipeline on x86-64 with SHA extensions | Prefer `SHA256` (BCL, hardware-accelerated); fall back to `AsconHashA256` if ASCON is required |
| Long-lived commitment (signatures, audit trails) | `AsconHash256` — 12-round margin throughout |

## Where to go next

- [ASCON overview](ascon.md) — the full family and which algorithm to pick.
- [ASCON XOF](ascon-xof.md) — for variable-length output (`AsconXof128`, `AsconCxof128`).
- [ASCON AEAD](ascon-aead.md) — for authenticated encryption (`AsconAead128`).
- [Hashing overview](hashing.md) — how ASCON sits alongside SipHash, Tiger, CubeHash, and the non-cryptographic families.
- API reference: <xref:Bodu.Security.Cryptography.AsconHash256> · <xref:Bodu.Security.Cryptography.AsconHashA256>
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
