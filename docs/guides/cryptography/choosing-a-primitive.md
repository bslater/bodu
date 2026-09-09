---
title: Choosing a primitive
---

# Choosing a primitive

`Bodu.Security.Cryptography` ships several algorithms in every family the BCL already covers — hashes, MACs, AEAD, KDFs, signatures — plus families the BCL does not (tweakable and wide-block ciphers, post-quantum KEM/DSA, HPKE, extended-nonce AEAD). This page is the decision table: for each family it lists every Bodu type beside its BCL counterpart, with the sizes and defaults read from the source, and says when each one is the right call.

> [!NOTE]
> `Bodu.Security.Cryptography` is not independently audited and offers best-effort, not guaranteed, side-channel resistance. Where the BCL implements the same algorithm (SHA-2, SHA-3, HMAC, AES-GCM, ChaCha20-Poly1305, HKDF, PBKDF2, ECDSA/ECDH/RSA), prefer the BCL: it is FIPS-validated on supporting platforms and backed by OS or hardware implementations. Reach for Bodu when you need an algorithm the BCL lacks, a construction the BCL cannot express (a tweak, a 24-byte nonce, a wide block), or a pure-managed implementation that behaves identically on every platform. See [Security guarantees and limitations](security-posture.md).

## Hashes

| Algorithm | Type | Output (bits) | Keyed mode | Fast path | Standard | Use it for |
|---|---|---|---|---|---|---|
| BLAKE2b | <xref:Bodu.Security.Cryptography.Blake2b> | 128 / 160 / 192 / 224 / 256 / 384 / 512 — default **512** | Yes — `Key` of 1–64 bytes (RFC 7693 §2.8 MAC mode) | AVX-512 (VL) | RFC 7693 | The general-purpose fast digest on 64-bit hosts; keyed hashing without HMAC. |
| BLAKE2s | <xref:Bodu.Security.Cryptography.Blake2s> | 128 / 160 / 192 / 224 / 256 — default **256** | Yes — `Key` of 1–32 bytes | AVX-512 (VL) | RFC 7693 | Same as BLAKE2b on 32-bit or constrained hosts. |
| BLAKE3 | <xref:Bodu.Security.Cryptography.Blake3> | **256** (fixed) | No — keyed and KDF modes are not exposed | AVX-512 (VL) | BLAKE3 specification (no RFC) | Tree-parallel digest of large inputs; the fixed-output subset only. |
| Skein-256 / 512 / 1024 | <xref:Bodu.Security.Cryptography.Skein256> / <xref:Bodu.Security.Cryptography.Skein512> / <xref:Bodu.Security.Cryptography.Skein1024> | 256: 128 / 160 / 224 / 256 · 512: 128 / 160 / 224 / 256 / 384 / 512 · 1024: 384 / 512 / 1024 — default = state width | Yes — `Key` up to 8192 bits | Through the Threefish kernels (AVX-512) | SHA-3 finalist | Skein's native keyed mode (a MAC without HMAC) and the Skein/Threefish family. |
| Whirlpool | <xref:Bodu.Security.Cryptography.Whirlpool> | **512** | No | — | ISO/IEC 10118-3 | Interoperability with formats that mandate it; three historical `Version` variants. |
| Tiger | <xref:Bodu.Security.Cryptography.Tiger> | 128 / 160 / 192 — default **192**; Tiger and Tiger2 `Variant` | No | — | Legacy (TTH, Direct Connect) | Interoperability only, including Tiger-Tree hashes through <xref:Bodu.Security.Cryptography.MerkleTreeHash>. |
| CubeHash | <xref:Bodu.Security.Cryptography.CubeHash> | 224–512 — default **512**; tunable rounds and block size | No | AVX-512 (F) | SHA-3 round 2 | Research and interoperability with CubeHash parameterizations. |
| Snefru-128 / 256 | <xref:Bodu.Security.Cryptography.Snefru128> / <xref:Bodu.Security.Cryptography.Snefru256> | 128 / 256 | No | — | Legacy | Interoperability only. |
| Ascon-Hash256 / HashA256 | <xref:Bodu.Security.Cryptography.AsconHash256> / <xref:Bodu.Security.Cryptography.AsconHashA256> | **256** | No | — | NIST SP 800-232 | Lightweight hashing that shares a permutation with Ascon-AEAD128. |
| Ascon-XOF128 / CXOF128 | <xref:Bodu.Security.Cryptography.AsconXof128> / <xref:Bodu.Security.Cryptography.AsconCxof128> | Any (`Absorb` / `Squeeze` / `GetHash(n)`) | No (CXOF takes a customization string) | — | NIST SP 800-232 | Variable-length output from the Ascon family. |
| SHAKE | <xref:Bodu.Security.Cryptography.Shake> | Any — default `Shake()` is 256 output bits at security level 128 | No | — | FIPS 202 | A managed SHAKE where the BCL's `Shake128` / `Shake256` are unavailable. |
| SHA-256 / 384 / 512 | `System.Security.Cryptography.SHA256` … | 256 / 384 / 512 | via `HMACSHA256` … | SHA-NI / ARMv8 (platform) | FIPS 180-4 | **The default choice** for any hash the peer must also compute. |
| SHA3-256 / 384 / 512 | `System.Security.Cryptography.SHA3_256` … | 256 / 384 / 512 | via `HMACSHA3_256` … | Platform (`IsSupported`) | FIPS 202 | When SHA-3 is mandated and the platform supports it. |

Every Bodu hash derives from `System.Security.Cryptography.HashAlgorithm` (through <xref:Bodu.Security.Cryptography.BlockHashAlgorithm> or one of its siblings), so `ComputeHash`, `TransformBlock` / `TransformFinalBlock`, `ComputeHashAsync(Stream)`, and `CryptoStream` all apply — see [Interoperating with System.Security.Cryptography](bcl-interop.md). The fast-path column comes from [Hardware acceleration and the SIMD opt-out](hardware-acceleration.md); every accelerated primitive produces bit-identical output on the scalar path.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] message = "abc"u8.ToArray();

using var blake2b = new Blake2b(256);      // 256-bit BLAKE2b
using var blake3  = new Blake3();          // fixed 256-bit output
using var skein   = new Skein512();        // default 512-bit output
using var ascon   = new AsconHash256();
using var sha256  = SHA256.Create();       // the BCL baseline

Console.WriteLine(Convert.ToHexString(blake2b.ComputeHash(message)));  // BDDD813C…D52319
Console.WriteLine(Convert.ToHexString(blake3.ComputeHash(message)));   // 6437B3AC…BD9D85
Console.WriteLine(Convert.ToHexString(ascon.ComputeHash(message)));    // 45AA0343…0935CF
Console.WriteLine(Convert.ToHexString(sha256.ComputeHash(message)));   // BA7816BF…F20015AD
```

## MACs and keyed hashes

| Algorithm | Type | Key | Tag | Use it for | Avoid it for |
|---|---|---|---|---|---|
| SipHash-2-4 | <xref:Bodu.Security.Cryptography.SipHash64> | 128 bits | 64 bits | Hash-flooding-resistant hash tables, short-message PRFs, keyed fingerprints of untrusted keys. | A general-purpose MAC on long messages or where a 64-bit tag is too short. |
| SipHash-2-4-128 | <xref:Bodu.Security.Cryptography.SipHash128> | 128 bits | 128 bits | Same as above with a full-width tag. | — |
| Poly1305 | <xref:Bodu.Security.Cryptography.Poly1305> | 256 bits, **one-time** | 128 bits | The authenticator inside an AEAD construction, with a per-message key derived from the cipher keystream. | A reusable MAC — a second message under the same key throws `CryptographicException`; use the AEAD types below instead. |
| BLAKE2b / BLAKE2s keyed | <xref:Bodu.Security.Cryptography.Blake2b> `Key` | 1–64 / 1–32 bytes | 8–64 / 8–32 bytes | A MAC without the HMAC double-hash, when both peers speak BLAKE2. | Peers that only implement HMAC. |
| Skein keyed | <xref:Bodu.Security.Cryptography.Skein512> `Key` | up to 8192 bits | per the variant | Skein-native MAC. | Peers that only implement HMAC. |
| HMAC-SHA-2 | `System.Security.Cryptography.HMACSHA256` … | any | 256 / 384 / 512 bits | **The default MAC** — FIPS 198, universally implemented. | — |

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] key128 = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] key256 = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
byte[] message = "abc"u8.ToArray();

using var sip  = new SipHash64 { Key = key128 };
using var b2   = new Blake2b(256) { Key = key256 };
using var hmac = new HMACSHA256(key256);

Console.WriteLine(Convert.ToHexString(sip.ComputeHash(message)));    // A50720AA53FABC5D
Console.WriteLine(Convert.ToHexString(b2.ComputeHash(message)));     // D63A32D3…203F7F
Console.WriteLine(Convert.ToHexString(hmac.ComputeHash(message)));   // F0133729…4E1E47
```

## Authenticated encryption (AEAD)

Every Bodu AEAD implements <xref:Bodu.Security.Cryptography.IAeadTransform>, emits `ciphertext ‖ tag`, and is **single-use per message**: construct one instance per encryption or decryption. None of them streams — each is a single `Encrypt` / `Decrypt` call over the whole message, so chunk large payloads yourself with a distinct nonce per chunk.

| Construction | Type | Key | Nonce | Nonce-misuse resistant | Tag | Associated data | Wire-compatible with |
|---|---|---|---|---|---|---|---|
| AES-GCM | <xref:Bodu.Security.Cryptography.GcmModeTransform> over <xref:Bodu.Security.Cryptography.AesBlockCipher> | 128 / 192 / 256 | **exactly 12 bytes** | No — reuse is catastrophic | 128 bits | Yes | `System.Security.Cryptography.AesGcm` (same bytes; see [BCL interop](bcl-interop.md#pattern-5--aes-gcm-is-wire-compatible-with-aesgcm)) |
| AES-GCM-SIV | <xref:Bodu.Security.Cryptography.GcmSivModeTransform> | 128 / 256 | 12 bytes (first 12 of a 16-byte IV) | **Yes** | 128 bits | Yes | RFC 8452 |
| AES-CCM | <xref:Bodu.Security.Cryptography.CcmModeTransform> | 128 / 192 / 256 | 12 bytes (first 12 of a 16-byte IV) | No | 128 bits | Yes | NIST SP 800-38C profile (12-byte nonce, 16-byte tag, messages < 2²⁴ bytes) |
| AES-OCB3 | <xref:Bodu.Security.Cryptography.OcbModeTransform> | 128 / 192 / 256 | 12 bytes (first 12 of a 16-byte IV) | No | 64 / 96 / **128** bits (`tagSize`) | Yes | RFC 7253 |
| AES-EAX | <xref:Bodu.Security.Cryptography.EaxModeTransform> | 128 / 192 / 256 | 16 bytes (= block size) | No | 128 bits | Yes | EAX (FSE 2004) |
| AES-SIV | <xref:Bodu.Security.Cryptography.SivModeTransform> | two independent AES keys | none — the IV argument is ignored; deterministic | **Yes** | 128 bits | Yes | RFC 5297 |
| Ascon-AEAD128 | <xref:Bodu.Security.Cryptography.AsconAead128> | 128 | 16 bytes | No | 128 bits | Yes | NIST SP 800-232 |
| XChaCha20-Poly1305 | <xref:Bodu.Security.Cryptography.XChaCha20Poly1305> | 256 | **24 bytes** — safe to draw at random | No, but random nonces are collision-safe | 128 bits | Yes | libsodium `crypto_aead_xchacha20poly1305_ietf` |
| XSalsa20-Poly1305 (secretbox) | <xref:Bodu.Security.Cryptography.XSalsa20Poly1305> | 256 | 24 bytes | No | 128 bits | **No** (throws) | NaCl `crypto_secretbox` (tag order differs — converters provided) |
| XSalsa20-Poly1305-AEAD | <xref:Bodu.Security.Cryptography.XSalsa20Poly1305Aead> | 256 | 24 bytes | No | 128 bits | Yes | Nothing — Bodu-defined |
| AES-GCM (BCL) | `System.Security.Cryptography.AesGcm` | 128 / 192 / 256 | 12 bytes | No | 96–128 bits | Yes | Bodu `GcmModeTransform` |
| ChaCha20-Poly1305 (BCL) | `System.Security.Cryptography.ChaCha20Poly1305` | 256 | 12 bytes | No | 128 bits | Yes | RFC 8439 — **not** XChaCha20-Poly1305 |

**Which one?** Use the BCL `AesGcm` when both peers can; use Bodu's `GcmModeTransform` when you need the same wire format on a platform where `AesGcm.IsSupported` is false or you want the `IBlockCipher` composition. If you cannot guarantee unique nonces, choose **GCM-SIV** (random-nonce tolerant with GCM-class performance) or **SIV** (fully deterministic; key wrapping). If you want to draw nonces at random from a counter-free source, choose **XChaCha20-Poly1305** — its 192-bit nonce is the reason the type exists. Choose Ascon for constrained peers speaking SP 800-232. Choose OCB, EAX, or CCM only when a protocol mandates them.

> [!WARNING]
> Every AEAD in the table exposes a public span-based `Encrypt(plaintext, output)` instance method *and* an array-returning `Encrypt(plaintext, associatedData)` extension method. With two positional `byte[]` arguments C# binds the **instance** method — `aad` becomes the output buffer and the call returns an `int`. Always name the argument: `aead.Encrypt(plaintext, associatedData: aad)`. The single-argument form `aead.Encrypt(plaintext)` is unambiguous. The same applies to `Decrypt`.

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key     = Convert.FromHexString("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
byte[] nonce24 = Convert.FromHexString("404142434445464748494a4b4c4d4e4f5051525354555657");
byte[] nonce12 = Convert.FromHexString("404142434445464748494a4b4c4d4e4f");
byte[] aad     = Convert.FromHexString("50515253c0c1c2c3c4c5c6c7");
byte[] message = "abc"u8.ToArray();

byte[] xchacha;
using (var aead = new XChaCha20Poly1305(key, nonce24))
    xchacha = aead.Encrypt(message, associatedData: aad);         // 906E10CF…B7481715AEE9EC9628E9BD

byte[] gcm;
using (var aes = new AesBlockCipher(key))
    gcm = new GcmModeTransform(aes, nonce12.AsSpan(0, 12)).Encrypt(message, associatedData: aad);

byte[] gcmSiv;
using (var master = new AesBlockCipher(key))
{
    byte[] iv = new byte[16];
    nonce12.AsSpan(0, 12).CopyTo(iv);
    gcmSiv = new GcmSivModeTransform(master, static k => new AesBlockCipher(k), iv).Encrypt(message, associatedData: aad);
}

byte[] ascon;
using (var aead = new AsconAead128(key.AsSpan(0, 16), nonce12.AsSpan(0, 16)))
    ascon = aead.Encrypt(message, associatedData: aad);
```

## Key derivation

| Function | Type | Input entropy | Standard | Use it for |
|---|---|---|---|---|
| Argon2id | <xref:Bodu.Security.Cryptography.Argon2id> (`Argon2Parameters { MemoryKiB, Iterations, Parallelism, TagLength = 32, Version = 0x13 }`) | **Low** (passwords) | RFC 9106 | **The default password hash / password KDF.** PHC-string `Hash` / `Verify`. |
| Argon2i / Argon2d | <xref:Bodu.Security.Cryptography.Argon2i> / <xref:Bodu.Security.Cryptography.Argon2d> | Low | RFC 9106 | Argon2i when memory access must be data-independent; Argon2d when side channels are irrelevant (proof-of-work). |
| scrypt | <xref:Bodu.Security.Cryptography.Scrypt> (`costN` power of two, `blockSizeR`, `parallelization`) | Low | RFC 7914 | Existing scrypt deployments; PHC-string `Hash` / `Verify`. Accepts <xref:Bodu.Security.Cryptography.SecretBytes> + <xref:Bodu.Security.Cryptography.Salt>. |
| HKDF | <xref:Bodu.Security.Cryptography.Hkdf> (SHA-1 / SHA-256 / SHA-384 / SHA-512) | **High** (DH secret, KEM output, master key) | RFC 5869 | Stretching a strong secret into labelled keys. Interchangeable with the BCL `HKDF`; prefer the BCL. |
| PBKDF2 | `System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2` | Low | RFC 8018 / NIST SP 800-132 | When FIPS compliance mandates PBKDF2. Not memory-hard — prefer Argon2id otherwise. |

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] password = "correct horse battery staple"u8.ToArray();
byte[] salt = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");

byte[] argon  = Argon2id.DeriveKey(password, salt, new Argon2Parameters { MemoryKiB = 64 * 1024, Iterations = 3, Parallelism = 4, TagLength = 32 });
byte[] scrypt = Scrypt.DeriveKey(password, salt, costN: 1 << 14, blockSizeR: 8, parallelization: 1, length: 32);
byte[] hkdf   = Hkdf.DeriveKey(HashAlgorithmName.SHA256, inputKeyingMaterial: scrypt, outputLength: 32, salt: salt, info: "traffic"u8);
byte[] pbkdf2 = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations: 600_000, HashAlgorithmName.SHA256, outputLength: 32);
```

## Asymmetric algorithms

| Algorithm | Type | Sizes (bytes) | Standard | Key encodings | BCL counterpart |
|---|---|---|---|---|---|
| Ed25519 | <xref:Bodu.Security.Cryptography.Ed25519> | private 32 · public 32 · signature 64 | RFC 8032 | raw, PKCS#8, SPKI, PEM | none — `ECDsa` on NIST curves is a different scheme |
| X25519 | <xref:Bodu.Security.Cryptography.X25519> | keys 32 · shared secret 32 | RFC 7748 | raw, PKCS#8, SPKI, PEM | none — `System.Security.Cryptography.ECDiffieHellman` on NIST curves |
| ML-KEM-512 / 768 / 1024 | <xref:Bodu.Security.Cryptography.MLKem512> … | ek 800 / 1184 / 1568 · dk 1632 / 2400 / 3168 · ct 768 / 1088 / 1568 · ss 32 | FIPS 203 | raw only (PKCS#8 / SPKI throw `NotSupportedException`) | — |
| ML-DSA-44 / 65 / 87 | <xref:Bodu.Security.Cryptography.MLDsa44> … | pk 1312 / 1952 / 2592 · sk 2560 / 4032 / 4896 · sig 2420 / 3309 / 4627 | FIPS 204 | raw only; `DeterministicSigning` switch | — |
| HPKE | <xref:Bodu.Security.Cryptography.Hpke> + <xref:Bodu.Security.Cryptography.HpkeSuite> | suites: X25519 + HKDF-SHA256 + AES-128-GCM / AES-256-GCM / ChaCha20-Poly1305 | RFC 9180 | via the X25519 key | — |
| ECDSA / ECDH / RSA | `System.Security.Cryptography.ECDsa`, `System.Security.Cryptography.ECDiffieHellman`, `System.Security.Cryptography.RSA` | curve / modulus dependent | FIPS 186-5, SP 800-56A, PKCS#1 | PKCS#8, SPKI, PEM, X.509 | **The default** when certificates or NIST curves are required. |

Pick Ed25519 / X25519 for modern protocols that specify Curve25519 (SSH, Signal-style, HPKE); pick the BCL types when you need X.509 certificates or FIPS-approved curves; add ML-KEM / ML-DSA in a hybrid alongside a classical scheme when quantum resistance is a requirement. The [asymmetric overview](asymmetric-overview.md) explains the shared `AsymmetricAlgorithm` base and the verify-or-fail discipline.

```csharp
using Bodu.Security.Cryptography;

using var signer = new Ed25519();
signer.ImportPrivateKey(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));
byte[] signature = signer.SignData(ReadOnlySpan<byte>.Empty);     // RFC 8032 test 1: E5564300C360AC72…8E7A100B

using var kem = new MLKem768();
kem.GenerateKey();
(byte[] ciphertext, byte[] sharedSecret) = kem.Encapsulate();      // 1088-byte ciphertext, 32-byte secret
bool agreed = kem.Decapsulate(ciphertext).AsSpan().SequenceEqual(sharedSecret);   // true
```

## Block ciphers — when to use one at all

A raw block cipher gives confidentiality only. For new designs, start from the AEAD table above; drop to a block cipher and a mode when a format, a protocol, or a tweak requirement forces it. All of the following compose with the same modes and padding through [Modes, transforms, and factories](cipher-composition-reference.md).

| Cipher | Type | Block | Key | Tweak | Standing | Reach for it when |
|---|---|---|---|---|---|---|
| AES | <xref:Bodu.Security.Cryptography.AesBlockCipher> (over the BCL `Aes`) | 128 | 128 / 192 / 256 | — | FIPS 197; hardware accelerated | Always the first choice for a 128-bit block; the only primitive the AEAD modes accept. |
| Camellia | <xref:Bodu.Security.Cryptography.Camellia> / `CamelliaBlockCipher` | 128 | 128 / 192 / 256 | — | ISO/IEC 18033-3, RFC 3713 | A regulatory or interop requirement names Camellia. |
| Twofish | <xref:Bodu.Security.Cryptography.Twofish> / `TwofishBlockCipher` | 128 | 128 / 192 / 256 | — | AES finalist | Legacy volume formats and files that used Twofish. |
| Serpent-128 | <xref:Bodu.Security.Cryptography.Serpent128> / `Serpent128Cipher` | 128 | 128 / 192 / 256 | — | AES finalist | Same — see [Using Serpent](serpent.md). |
| Threefish-256 / 512 / 1024 | <xref:Bodu.Security.Cryptography.Threefish256> … | 256 / 512 / 1024 | = block | 128 bits | Skein submission; AVX-512 path | You want a native tweak (per-record separation, sector encryption) or a wide block. |
| Serpent-256 / 512 / 1024 | <xref:Bodu.Security.Cryptography.Serpent256> … | 256 / 512 / 1024 | = block | 128 bits | **Bodu-only, experimental** | Experiments only — no reference implementation to interoperate with. |
| Blowfish | <xref:Bodu.Security.Cryptography.Blowfish> | 64 | 32–448 (8-bit steps) | — | Legacy | Reading data produced by Blowfish-era tools. The 64-bit block caps a key at a few gigabytes of data. |
| Skipjack | <xref:Bodu.Security.Cryptography.Skipjack> | 64 | 80 | — | Withdrawn (NIST) | Interoperability with legacy government-format data only. |

> [!WARNING]
> Camellia, Twofish, Serpent, Blowfish, Skipjack, Tiger, Whirlpool, and Snefru are table-driven: their control flow is constant-time but the S-box reads are data-dependent, and the source states they are **not hardened** against cache-timing attacks. The ARX designs (BLAKE2, BLAKE3, Threefish, ChaCha20, Salsa20) and the AES path (hardware) do not have that caveat. See [Security guarantees and limitations](security-posture.md#constant-time-claims-by-primitive).

## API summary

| Family | Bodu entry points | BCL entry points |
|---|---|---|
| Hash | `Blake2b`, `Blake2s`, `Blake3`, `Skein256/512/1024`, `Whirlpool`, `Tiger`, `CubeHash`, `Snefru128/256`, `AsconHash256`, `AsconHashA256`, `AsconXof128`, `AsconCxof128`, `Shake` | `SHA256`, `SHA384`, `SHA512`, `SHA3_256/384/512`, `Shake128`, `Shake256` |
| MAC | `SipHash64`, `SipHash128`, `Poly1305`, keyed `Blake2b` / `Blake2s` / `Skein*` | `HMACSHA256/384/512`, `HMACSHA3_*` |
| AEAD | `GcmModeTransform`, `GcmSivModeTransform`, `CcmModeTransform`, `OcbModeTransform`, `EaxModeTransform`, `SivModeTransform`, `AsconAead128`, `XChaCha20Poly1305`, `XSalsa20Poly1305`, `XSalsa20Poly1305Aead` | `AesGcm`, `AesCcm`, `ChaCha20Poly1305` |
| KDF | `Argon2id/i/d`, `Scrypt`, `Hkdf` | `HKDF`, `Rfc2898DeriveBytes.Pbkdf2` |
| Asymmetric | `Ed25519`, `X25519`, `MLKem512/768/1024`, `MLDsa44/65/87`, `Hpke` | `System.Security.Cryptography.ECDsa`, `System.Security.Cryptography.ECDiffieHellman`, `System.Security.Cryptography.RSA` |
| Block cipher | `AesBlockCipher`, `Camellia`, `Twofish`, `Serpent128`, `Threefish256/512/1024`, `Serpent256/512/1024`, `Blowfish`, `Skipjack` | `Aes` |

## Where to go next

- [Interoperating with System.Security.Cryptography](bcl-interop.md) — the BCL surfaces every Bodu type plugs into, and the wire-compatibility proofs.
- [Modes, transforms, and factories](cipher-composition-reference.md) — the `CipherModeKind` support matrix.
- [Security guarantees and limitations](security-posture.md) — audit status, constant-time claims, and the exception contract.
- [Bodu.Security.Cryptography introduction](../../docs/cryptography/index.md) — the shape of the library.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
