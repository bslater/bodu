---
title: Using scrypt
---

# Using scrypt

scrypt is a password-based key-derivation function designed by Colin Percival and standardized as RFC 7914. Like Argon2 it is **memory-hard** — it forces an attacker to commit a large amount of fast RAM per guess — but it predates Argon2 and is the established choice in many existing systems (disk encryption, cryptocurrency wallets, and a wide range of password stores). Reach for it when you need RFC 7914 interoperability or are matching an existing deployment; for greenfield password hashing, [Argon2id](argon2.md) is the current recommendation.

**Bodu.Security.Cryptography** ships scrypt as a single type, <xref:Bodu.Security.Cryptography.Scrypt>, configured by three cost parameters.

> [!IMPORTANT]
> scrypt, like Argon2, is for **low-entropy** inputs — passwords. For a **high-entropy** secret (a key-agreement or KEM output, a master key), reach for [HKDF](hkdf.md) instead: it is fast and purpose-built for that, whereas scrypt's deliberate slowness buys nothing on an already-random input.

> [!NOTE]
> This type is not independently audited and offers best-effort, not guaranteed, side-channel resistance. The platform already ships PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`) and HKDF; scrypt fills the memory-hard gap they do not.

## Parameters at a glance

scrypt's cost is set by <xref:Bodu.Security.Cryptography.ScryptParameters>:

| Property | Meaning | Notes |
|---|---|---|
| `CostN` (`N`) | CPU/memory cost | **Required.** Must be a power of two greater than one. The dominant cost knob. |
| `BlockSizeR` (`r`) | Block size | **Required.** Tunes the memory/bandwidth mix; 8 is the conventional value. |
| `Parallelization` (`p`) | Independent iterations | **Required.** Usually 1. |

Peak memory is approximately `128 * N * r` bytes. RFC 7914, Section 2 suggests **`N = 16384`, `r = 8`, `p = 1`** (about 16 MiB) for interactive logins, scaling `N` up for more sensitive secrets. The key-derivation envelope uses PBKDF2-HMAC-SHA256, supplied by the platform. Construction validates the parameters and throws <xref:System.ArgumentOutOfRangeException> when `CostN` is not a power of two greater than one, or `BlockSizeR` / `Parallelization` is below 1.

## Pattern 1 — derive a key (static one-shot)

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");

byte[] salt = new byte[16];
RandomNumberGenerator.Fill(salt);

byte[] key = Scrypt.DeriveKey(password, salt, costN: 16384, blockSizeR: 8, parallelization: 1, length: 32);
```

## Pattern 2 — the instance surface

Bind the cost once and derive repeatedly; the instance offers an allocation-free span overload:

```csharp
using Bodu.Security.Cryptography;

var scrypt = new Scrypt(costN: 16384, blockSizeR: 8, parallelization: 1);

byte[] key = scrypt.GetBytes(password, salt, length: 32);

Span<byte> destination = stackalloc byte[32];
scrypt.DeriveKey(password, salt, destination);
```

`new Scrypt(N, r, p)` is shorthand for `new Scrypt(new ScryptParameters { CostN = N, BlockSizeR = r, Parallelization = p })`.

## Pattern 3 — hash and verify a password (PHC string)

For password storage, `Hash` returns a self-describing PHC string and `Verify` checks a candidate in constant time:

```csharp
using System.Text;
using Bodu.Security.Cryptography;

var scrypt = new Scrypt(16384, 8, 1);

// Registration — a random 16-byte salt is generated for you.
string stored = scrypt.Hash(Encoding.UTF8.GetBytes(userPassword), length: 32);
// stored == "$scrypt$ln=14,r=8,p=1$<base64 salt>$<base64 hash>"

// Login.
bool ok = Scrypt.Verify(stored, Encoding.UTF8.GetBytes(candidatePassword));
```

The `ln` field is `log2(N)` (so `N = 16384` encodes as `ln=14`). `Verify` re-derives with the parameters parsed from the string and compares with `CryptographicOperations.FixedTimeEquals`; the working memory and computed hash are zeroed before it returns.

## Choosing parameters

Hold `r = 8` and `p = 1`, then raise `N` (always a power of two) until verification fits your latency budget on the hardware that will run it. Each doubling of `N` doubles both time and memory. If you need more memory without more time, increase `r` instead. Remember that `128 * N * r` bytes must be available per concurrent derivation — size `N` against your server's memory and expected login concurrency, not just latency.

## What scrypt is not

- **Not the current default for new systems.** Argon2id (RFC 9106) is the more recent design and the general recommendation; choose scrypt for interoperability or to match an existing store. See [Using Argon2](argon2.md).
- **Not a KDF for high-entropy inputs.** For stretching an already-random key, use `HKDF` — it is far cheaper and purpose-built for that.
- **Not safe with a reused or missing salt.** Always pass a unique, random salt per password; `Hash(password, length)` generates one for you.

## Where to go next

- [Using Argon2](argon2.md) — the memory-hard KDF recommended for new password-hashing deployments.
- [Hashing overview](hashing.md) — how password KDFs relate to digests and keyed MACs.
- [Bodu.Security.Cryptography namespace](xref:Bodu.Security.Cryptography) — the generated API reference.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
