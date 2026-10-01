---
title: Using Argon2
---

# Using Argon2

Argon2 is the winner of the 2015 Password Hashing Competition and the algorithm RFC 9106 recommends for password storage and password-based key derivation. It is **memory-hard**: the work it imposes is dominated by filling a large block of RAM, which is exactly the resource that custom cracking hardware (GPUs, FPGAs, ASICs) finds expensive to scale. That makes it far more resistant to offline guessing attacks than a plain iterated hash.

**Bodu.Security.Cryptography** ships all three RFC 9106 variants as separate types over a shared engine:

| Type | Addressing | When to reach for it |
|---|---|---|
| <xref:Bodu.Security.Cryptography.Argon2id> | Hybrid (data-independent then data-dependent) | **The default.** The variant RFC 9106 requires every implementation to support, and the right pick for password hashing on shared hardware. |
| <xref:Bodu.Security.Cryptography.Argon2i> | Data-independent | Resistant to side-channel timing attacks, at the cost of weaker time-memory trade-off resistance. |
| <xref:Bodu.Security.Cryptography.Argon2d> | Data-dependent | Maximum trade-off resistance; appropriate only where memory access patterns are not observable (e.g. proof-of-work). |

The three are sealed types deriving from a shared `Argon2` base. Cost is supplied once through an <xref:Bodu.Security.Cryptography.Argon2Parameters> record.

> [!IMPORTANT]
> Argon2 is for **low-entropy** inputs - passwords, PINs, passphrases - where the slowness *is* the defence. For a **high-entropy** secret (a Diffie-Hellman or KEM output, a master key), Argon2 only wastes resources: use [HKDF](hkdf.md) instead, which is fast by design. Feeding a password to HKDF, or a high-entropy key to Argon2, is the classic KDF mismatch.

> [!NOTE]
> These types are not independently audited and offer best-effort, not guaranteed, side-channel resistance. Where the platform already covers your need - `Rfc2898DeriveBytes.Pbkdf2` (PBKDF2) or `HKDF` - prefer it. Argon2 and scrypt exist because the BCL ships neither.

## Parameters at a glance

`Argon2Parameters` carries the cost and auxiliary inputs:

| Property | Meaning | Notes |
|---|---|---|
| `MemoryKiB` (`m`) | Memory to fill, in kibibytes | **Required.** Must be at least `8 * Parallelism`. The dominant cost knob. |
| `Iterations` (`t`) | Passes over memory | **Required.** Tunes time independently of memory. |
| `Parallelism` (`p`) | Independent lanes | **Required.** 1-16777215. |
| `TagLength` (`T`) | Output length, in bytes | Default 32. Minimum 4. Folded into the hash, so it is fixed per derivation. |
| `Version` | Argon2 version code | Default `0x13` (RFC 9106). `0x10` is accepted for verifying legacy hashes. |
| `Secret` (`K`) | Optional server-side pepper | Not stored in the PHC string - supply it again at verification. |
| `AssociatedData` (`X`) | Optional associated data | Stored in the PHC string as `data=`. |

RFC 9106, Section 4 gives two uniformly-safe starting points: **Argon2id with `t=1`, `p=4`, `m=2^21` (2 GiB)** for general use, or **`t=3`, `p=4`, `m=2^16` (64 MiB)** for memory-constrained environments. `MemoryKiB` is the peak working memory a single derivation allocates - `65536` is a literal 64 MiB - so multiply by your expected concurrent logins when sizing a server. `MemoryKiB` must be at least `8 * Parallelism`; `Parallelism` and `Iterations` must each be at least 1, and `TagLength` at least 4, or construction throws <xref:System.ArgumentOutOfRangeException>. The salt should be at least 8 bytes (16 is recommended); a shorter salt throws <xref:System.ArgumentException>.

## Pattern 1 - derive a key (static one-shot)

The static `DeriveKey` mirrors the BCL's `Rfc2898DeriveBytes.Pbkdf2` shape:

<!-- compile -->
```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");

byte[] salt = new byte[16];
RandomNumberGenerator.Fill(salt);

var parameters = new Argon2Parameters
{
    MemoryKiB   = 65536,   // 64 MiB
    Iterations  = 3,
    Parallelism = 4,
    TagLength   = 32,
};

byte[] key = Argon2id.DeriveKey(password, salt, parameters);   // 32 bytes
```

Swap `Argon2id` for `Argon2i` or `Argon2d` to select a different variant; the call shape is identical.

## Pattern 2 - the instance surface

When you derive many keys with the same cost, bind the parameters once. The instance exposes an allocation-free span overload:

```csharp
using Bodu.Security.Cryptography;

var argon2 = new Argon2id(parameters);

byte[] key = argon2.GetBytes(password, salt);            // returns a new array

Span<byte> destination = stackalloc byte[32];            // must equal TagLength
argon2.DeriveKey(password, salt, destination);
```

## Pattern 3 - hash and verify a password (PHC string)

For password storage, `Hash` returns a self-describing PHC string - variant, version, cost parameters, salt, and tag all encoded together - and `Verify` checks a candidate password against it in constant time:

```csharp
using System.Text;
using Bodu.Security.Cryptography;

var parameters = new Argon2Parameters { MemoryKiB = 65536, Iterations = 3, Parallelism = 4 };

// Registration - a random 16-byte salt is generated for you.
string stored = new Argon2id(parameters).Hash(Encoding.UTF8.GetBytes(userPassword));
// stored == "$argon2id$v=19$m=65536,t=3,p=4$<base64 salt>$<base64 hash>"

// Login.
bool ok = Argon2.Verify(stored, Encoding.UTF8.GetBytes(candidatePassword));
```

`Argon2.Verify` on the base type reads the `$argon2{d,i,id}$` prefix and dispatches to the matching variant, so you do not need to know which variant produced a stored hash. The variant-specific `Argon2id.Verify` additionally asserts the string names that exact variant, returning `false` otherwise.

`Verify` is constant-time (it uses `CryptographicOperations.FixedTimeEquals`), and the working memory and computed tag are zeroed before the call returns.

### Peppered hashes

A `Secret` (pepper) strengthens stored hashes against a database-only breach, but it is **not** part of the PHC string. Supply it on both sides:

```csharp
var parameters = new Argon2Parameters { MemoryKiB = 65536, Iterations = 3, Parallelism = 4, Secret = pepper };
string stored = Argon2id.Hash(password, salt, parameters);

bool ok = Argon2id.Verify(stored, candidate, pepper);   // pepper supplied at verify time
```

## Threads, memory, and hardware

A derivation divides its lanes among threads when they are large enough to be worth dividing - from 768 KiB per lane, so `m=64 MiB, p=4` qualifies and `m=2 MiB, p=4` does not. It uses up to `Parallelism` threads, bounded by the processor count; the calling thread always takes part, so a derivation never waits on an idle thread pool. The tag never depends on the threads used: `p` is part of the result, the thread count is not.

A service that already verifies many passwords at once gains nothing from spreading each derivation across every core, and can bound the threads per derivation instead:

```csharp
using Bodu.Security.Cryptography;

// Confine each derivation to the thread that calls it, as a busy login service might.
var argon2 = new Argon2id(parameters, maxDegreeOfParallelism: 1);
byte[] key = argon2.GetBytes(password, salt);

bool ok = Argon2.Verify(stored, candidate, secret: [], maxDegreeOfParallelism: 1);
```

`-1`, the default, lets the library choose; `1` confines a derivation to the calling thread; a larger value caps the threads. `0` or anything below `-1` throws <xref:System.ArgumentOutOfRangeException>.

The compression function runs on **AVX2** where the processor has it, on **SSSE3** on other x64 processors, on **AdvSimd** on ARM64, and on a portable scalar path otherwise. All four produce identical output, and the `Bodu.Security.Cryptography.DisableSimd` switch pins the scalar path - see [Hardware acceleration and the SIMD opt-out](hardware-acceleration.md). A NativeAOT application is compiled for a baseline x64 instruction set that excludes AVX2, so it takes the SSSE3 kernel unless the project raises the target, for example with `<IlcInstructionSet>x86-64-v3</IlcInstructionSet>` - only where every machine it will run on has AVX2.

The memory matrix lives in native memory, not on the managed heap, so a derivation allocates no large array and provokes no gen2 collection. So that a burst of logins does not map fresh pages every time, up to one buffer per processor (of up to 256 MiB each) stays reserved for 30 seconds after the last derivation - cleared, so what stays resident is zeros. The reserve is shared with [scrypt](scrypt.md), which keeps its working memory the same way. Setting the `AppContext` switch `Bodu.Security.Cryptography.Argon2.DisableMatrixReuse` releases each buffer as soon as its derivation ends instead, for both functions. Every block of the matrix, and every buffer that held a password-derived value, is cleared before it is released; values the JIT keeps in registers or its own stack slots are beyond the library's reach.

## Choosing parameters

The right cost is "as high as your latency budget allows." Pick a target verification time (say 250-500 ms on your server hardware), fix `p = 4` and `T = 32`, then raise `MemoryKiB` until you hit that budget; only drop to a smaller `m` with a higher `t` when memory is genuinely constrained. With `p = 4` a derivation can finish several times sooner on a machine with four or more cores; the work it demands of an attacker is unchanged. Measure on the hardware that will run the check - not a developer laptop - and under the concurrency it will see.

## What Argon2 is not

- **Not a general-purpose KDF for high-entropy inputs.** If you are stretching an already-random key (not a password), `HKDF` (`System.Security.Cryptography.HKDF`) is the right, far cheaper tool.
- **Not a substitute for a salt.** Always pass a unique, random salt per password. The `Hash(password)` overload generates one for you.

## Where to go next

- [Using scrypt](scrypt.md) - the other memory-hard password KDF, when you want RFC 7914 compatibility or an established pre-Argon2 design.
- [Hashing overview](hashing.md) - where password KDFs sit relative to cryptographic digests and keyed MACs.
- [Bodu.Security.Cryptography namespace](xref:Bodu.Security.Cryptography) - the generated API reference.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** - every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
