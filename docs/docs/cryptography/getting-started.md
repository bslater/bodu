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

### Stream cipher — ChaCha20

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

using var cipher = new ChaCha20();
cipher.GenerateKey();               // 32-byte key
cipher.GenerateNonce();                // 12-byte nonce — unique per message

byte[] ciphertext = cipher.Encrypt(plaintext);
byte[] roundtrip  = cipher.Decrypt(ciphertext);   // self-inverse
```

Swap `ChaCha20` for `XChaCha20`, `Salsa20`, `XSalsa20`, `Rabbit`, or `Hc128` — the lifecycle is identical (no block mode or padding). These are **raw, confidentiality-only** ciphers: never reuse a `(key, nonce)` pair, and pair with a MAC such as `Poly1305` or prefer an AEAD construction when you need integrity.

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

### Digital signature — Ed25519

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("transfer 100 to account 42");

using var signer = Ed25519.Create();
signer.GenerateKey();

byte[] signature = signer.SignData(message);          // 64 bytes
bool valid       = signer.VerifyData(message, signature);   // true
```

Swap `Ed25519` for `MLDsa65` for a post-quantum (FIPS 204) signature — the lifecycle is identical, only the key and signature sizes differ.

### Key agreement — X25519

```csharp
using Bodu.Security.Cryptography;

using var alice = X25519.Create();
using var bob   = X25519.Create();
alice.GenerateKey();
bob.GenerateKey();

// Each side publishes its public key, then derives the secret from the peer's.
byte[] aliceShared = alice.DeriveSharedSecret(bob.ExportPublicKey());
byte[] bobShared   = bob.DeriveSharedSecret(alice.ExportPublicKey());

// aliceShared and bobShared are identical (32 bytes each).
```

Swap `X25519` for `MLKem768` when you need post-quantum (FIPS 203) key establishment — the receiver publishes an encapsulation key and the sender calls `Encapsulate()`.

### Hybrid public-key encryption — HPKE

```csharp
using Bodu.Security.Cryptography;

using var recipient = X25519.Create();
recipient.GenerateKey();
byte[] recipientPublicKey = recipient.ExportPublicKey();   // published, 32 bytes

HpkeSuite suite = HpkeSuite.X25519_HkdfSha256_Aes128Gcm;
byte[] info = "myapp v1"u8.ToArray();   // binds the exchange to a context
byte[] aad  = "headers"u8.ToArray();    // authenticated, not encrypted

// Sender — needs only the recipient's public key.
var (enc, ciphertext) = Hpke.Seal(suite, recipientPublicKey, info, aad, "secret message"u8);

// Recipient — needs its private key, the encapsulated key, and the same info / aad.
byte[] plaintext = Hpke.Open(suite, recipient, enc, info, aad, ciphertext);
```

### Password hashing — Argon2id

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");
byte[] salt     = RandomNumberGenerator.GetBytes(16);

var parameters = new Argon2Parameters
{
    MemoryKiB   = 65536,   // 64 MiB
    Iterations  = 3,
    Parallelism = 4,
    TagLength   = 32,
};

byte[] key = Argon2id.DeriveKey(password, salt, parameters);   // 32 bytes
```

Swap `Argon2id` for `Scrypt` (`Scrypt.DeriveKey(password, salt, costN: 16384, blockSizeR: 8, parallelization: 1, length: 32)`) for RFC 7914 interoperability.

### Key derivation — HKDF

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] sharedSecret = GetSharedSecret();   // high-entropy, but not uniform

byte[] sessionKey = Hkdf.DeriveKey(
    HashAlgorithmName.SHA256,
    inputKeyingMaterial: sharedSecret,
    outputLength: 32,
    salt: salt,                            // optional, non-secret; binds the derivation
    info: "myapp v1 traffic key"u8);       // optional context / label

CryptographicOperations.ZeroMemory(sharedSecret);   // wipe the raw secret once stretched
```

HKDF is for *high-entropy* inputs only — for passwords reach for Argon2id or scrypt above.

## Where to go next

- **[Bodu.Security.Cryptography introduction](index.md)** — namespaces, headline types, scenarios.
- **[Bodu.IO.Hashing](../io-hashing/index.md)** — the sibling library, for non-cryptographic checksums and fingerprints (no adversary model).
- **[Bodu.Security.Cryptography guides](../../guides/cryptography/index.md)** — encryption basics, modes, padding, AEAD, hashing.
- **[Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography)** — full type-by-type docs.
- **For non-cryptographic checksums and fingerprints** (CRC, Fletcher, Adler, FNV, CityHash), see [Bodu.IO.Hashing](../io-hashing/index.md).
