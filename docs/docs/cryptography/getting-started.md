---
title: Bodu.Security.Cryptography — Getting started
---

# Bodu.Security.Cryptography — Getting started

## Install

```bash
dotnet add package Bodu.Security.Cryptography
```

Targets `net8.0`. Depends on `Bodu.Core` and the BCL `System.Security.Cryptography`.

## Minimal samples — one per subfamily

### Standard block cipher — Camellia in CBC mode

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = Encoding.UTF8.GetBytes("the quick brown fox");

using var cipher = new Camellia();
cipher.GenerateKey();
cipher.GenerateIV();
cipher.BlockMode    = CipherModeKind.CBC;
cipher.BlockPadding = PaddingModeKind.PKCS7;

byte[] ciphertext = cipher.Encrypt(plaintext);
byte[] roundtrip  = cipher.Decrypt(ciphertext);
```

Swap `Camellia` for `Twofish`, `Serpent128`, `Blowfish`, or `Skipjack` — the lifecycle is identical.

### Tweakable block cipher — Threefish-512 with a per-record tweak

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

using var cipher = new Threefish512();
cipher.GenerateKey();
cipher.GenerateIV();
cipher.GenerateTweak();              // 128-bit public domain separator
cipher.BlockMode = CipherModeKind.CBC;
cipher.Padding   = PaddingMode.PKCS7;

byte[] ciphertext = cipher.Encrypt(plaintext);
byte[] roundtrip  = cipher.Decrypt(ciphertext);
```

Encrypting the same plaintext under the same key with a *different* tweak yields an entirely independent ciphertext — useful for disk encryption (sector number as tweak) or per-record encryption.

### AEAD — AES-GCM via `AesBlockCipher` + `GcmModeTransform`

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key   = RandomNumberGenerator.GetBytes(32); // AES-256
byte[] nonce = RandomNumberGenerator.GetBytes(12);
byte[] aad   = "header"u8.ToArray();
byte[] data  = "the quick brown fox"u8.ToArray();

using var aes = new AesBlockCipher(key);
using var gcm = new GcmModeTransform(aes, nonce);

byte[] ciphertextWithTag = gcm.Encrypt(data, aad);

using var verify = new GcmModeTransform(aes, nonce);   // fresh transform per message
byte[] recovered = verify.Decrypt(ciphertextWithTag, aad);
```

Swap `GcmModeTransform` for `CcmModeTransform`, `OcbModeTransform`, `EaxModeTransform`, `SivModeTransform`, or `GcmSivModeTransform`. AEAD transforms are **single-use per message** — construct a fresh transform on the encrypt side and another on the decrypt side.

### AEAD — ASCON-AEAD128 (no separate cipher)

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] key   = RandomNumberGenerator.GetBytes(16);
byte[] nonce = RandomNumberGenerator.GetBytes(16);
byte[] aad   = "header"u8.ToArray();
byte[] data  = "the quick brown fox"u8.ToArray();

using var aead = new AsconAead128(key, nonce);
byte[] ciphertextWithTag = aead.Encrypt(data, aad);

using var verify = new AsconAead128(key, nonce);
byte[] recovered = verify.Decrypt(ciphertextWithTag, aad);
```

### Keyed hash (MAC) — SipHash-64

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");
byte[] key  = RandomNumberGenerator.GetBytes(16);

using var sip = new SipHash64 { Key = key };
ulong digest  = BitConverter.ToUInt64(sip.ComputeHash(data));
```

### Cryptographic digest — Tiger-192

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var tiger = new Tiger();
byte[] digest   = tiger.ComputeHash(data);
```

Swap `Tiger` for `CubeHash`, `Blake2b`, `Whirlpool`, `Skein512`, or `AsconHash256`.

### Cryptographic digest — variable length (XOF) ASCON-XOF-128

```csharp
using Bodu.Security.Cryptography;

using var xof = new AsconXof128();
xof.AppendData(data);
byte[] output = xof.GetHashAndReset(byteCount: 64); // squeeze 64 bytes
```

### Merkle tree — verifiable inclusion proofs

```csharp
using Bodu.Security.Cryptography;

using var inner = SHA256.Create();
using var tree  = new MerkleTreeHash(inner, leafSize: 1024);

tree.AppendData(largeFile);
byte[] root = tree.GetHashAndReset();
```

## Where to go next

- **[Bodu.Security.Cryptography introduction](index.md)** — namespaces, headline types, scenarios.
- **[Bodu.IO.Hashing](../io-hashing/index.md)** — the sibling library, for non-cryptographic checksums and fingerprints (no adversary model).
- **[Bodu.Security.Cryptography guides](../../guides/cryptography/index.md)** — encryption basics, modes, padding, AEAD, hashing.
- **[Bodu.Security.Cryptography API reference](../../apidoc/Bodu.Security.Cryptography.md)** — full type-by-type docs.
- **For non-cryptographic checksums and fingerprints** (CRC, Fletcher, Adler, FNV, CityHash), see [Bodu.IO.Hashing](../io-hashing/index.md).
