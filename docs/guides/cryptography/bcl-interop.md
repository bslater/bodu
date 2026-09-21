---
title: Interoperating with System.Security.Cryptography
---

# Interoperating with System.Security.Cryptography

`Bodu.Security.Cryptography` is built on the BCL's own abstractions rather than beside them: every hash is a `System.Security.Cryptography.HashAlgorithm`, every block-cipher wrapper is a `SymmetricAlgorithm` (through <xref:Bodu.Security.Cryptography.ExtendedSymmetricAlgorithm> or <xref:Bodu.Security.Cryptography.TweakableSymmetricAlgorithm>), the raw-key asymmetric types derive from `AsymmetricAlgorithm`, and the KDF and OTP surfaces take `HashAlgorithmName` or an equivalent enum. This page shows, with verified round trips, exactly where a Bodu type drops into a BCL-shaped call site — and the two places where it deliberately does not.

> [!NOTE]
> `Bodu.Security.Cryptography` is not independently audited and offers best-effort, not guaranteed, side-channel resistance. Where the BCL implements the same algorithm, prefer the BCL; see [Choosing a primitive](choosing-a-primitive.md).

## Pattern 1 — one-shot versus incremental hashing

A Bodu hash is a `HashAlgorithm`, so the BCL lifecycle applies unchanged: `ComputeHash` for one shot, `TransformBlock` … `TransformFinalBlock` then `Hash` for incremental use. <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions> adds a span-friendly `AppendData` that forwards to `TransformBlock`, so you can mix the two freely.

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] part1 = "The quick brown fox "u8.ToArray();
byte[] part2 = "jumps over the lazy dog"u8.ToArray();

// One shot: ComputeHash resets, hashes, and finalizes.
using var oneShot = new Blake2b(256);
byte[] whole = oneShot.ComputeHash([.. part1, .. part2]);

// Incremental, the BCL way.
using var incremental = new Blake2b(256);
incremental.TransformBlock(part1, 0, part1.Length, null, 0);
incremental.TransformFinalBlock(part2, 0, part2.Length);
byte[] streamed = incremental.Hash!;

// Incremental, the Bodu way: AppendData over spans, then an empty final block.
using var appended = new Blake2b(256);
appended.AppendData(part1);
appended.AppendData(part2);
appended.TransformFinalBlock([], 0, 0);
byte[] appendedHash = appended.Hash!;

// All three: 01718CEC35CD3D796DD00020E0BFECB473AD23457D063B75EFF29C0FFA2E58A9
```

`HashAlgorithm.ComputeHash(Stream)` and `ComputeHashAsync(Stream, CancellationToken)` work too — they are inherited, not reimplemented. See [Streams and async](streaming-and-async.md) for the buffer sizes and the verify helpers.

## Pattern 2 — `IncrementalHash` has no Bodu equivalent, and does not need one

`System.Security.Cryptography.IncrementalHash` only accepts algorithms a `HashAlgorithmName` can name, so it cannot host BLAKE2, Skein, or Tiger. The equivalent shape on a Bodu hash is `AppendData` followed by an empty `TransformFinalBlock`, and `Initialize()` plays the part of `GetHashAndReset`'s reset:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] data = "abc"u8.ToArray();

using IncrementalHash bcl = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
bcl.AppendData(data);
byte[] sha = bcl.GetHashAndReset();          // BA7816BF…F20015AD

using var bodu = new Blake3();
bodu.AppendData(data);
bodu.TransformFinalBlock([], 0, 0);
byte[] blake3 = bodu.Hash!;                  // 6437B3AC…BD9D85
bodu.Initialize();                           // ready for the next message
```

Do not look for `GetHashAndReset` on a Bodu hash — it does not exist on any public type.

## Pattern 3 — `CryptoStream` over a Bodu cipher

`CreateEncryptor()` / `CreateDecryptor()` on any wrapper return an `ICryptoTransform` (a <xref:Bodu.Security.Cryptography.BlockCipherTransform>, or a stream-cipher transform), so `CryptoStream` composes as it would with `Aes`:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] plaintext = "CryptoStream over a Bodu block cipher"u8.ToArray();

using var camellia = new Camellia { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7 };
camellia.Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
camellia.IV  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");

byte[] ciphertext;
using (var output = new MemoryStream())
{
    using (var crypto = new CryptoStream(output, camellia.CreateEncryptor(), CryptoStreamMode.Write))
        crypto.Write(plaintext);
    ciphertext = output.ToArray();           // 48 bytes: 6265D259…D92B2B2A
}

byte[] recovered;
using (var input = new MemoryStream(ciphertext))
using (var crypto = new CryptoStream(input, camellia.CreateDecryptor(), CryptoStreamMode.Read))
using (var sink = new MemoryStream())
{
    crypto.CopyTo(sink);
    recovered = sink.ToArray();
}
```

Set the mode through `BlockMode` (<xref:Bodu.Security.Cryptography.CipherModeKind>) rather than the inherited `Mode`, because only `BlockMode` can name CTR; the two properties stay in sync for the values both enums share. See [Modes, transforms, and factories](cipher-composition-reference.md).

## Pattern 4 — BCL one-shots that do *not* work

The BCL `SymmetricAlgorithm` one-shot helpers (`EncryptCbc`, `EncryptEcb`, and the CFB variants) call the protected `System.Security.Cryptography.SymmetricAlgorithm.TryEncryptCbcCore` family, which the Bodu wrappers do not override — those calls throw `NotSupportedException` ("Method not supported. Derived class must override."). Use `CreateEncryptor` with `CryptoStream`, or the <xref:Bodu.Security.Cryptography.Extensions.SymmetricAlgorithmExtensions> one-shots (`alg.Encrypt(byte[])`, `alg.Encrypt(Stream, Stream)`), which route through `CreateEncryptor`.

## Pattern 5 — AES-GCM is wire-compatible with `AesGcm`

<xref:Bodu.Security.Cryptography.GcmModeTransform> over <xref:Bodu.Security.Cryptography.AesBlockCipher> implements the same 96-bit-nonce, 128-bit-tag profile of NIST SP 800-38D that `System.Security.Cryptography.AesGcm` does. The only difference is layout: Bodu returns `ciphertext ‖ tag` in one array, the BCL takes the ciphertext and tag as separate buffers. Split or concatenate at the boundary and the bytes match in both directions — this run uses the SP 800-38D test-case-4 material:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key   = Convert.FromHexString("feffe9928665731c6d6a8f9467308308");
byte[] nonce = Convert.FromHexString("cafebabefacedbaddecaf888");
byte[] plaintext = Convert.FromHexString(
    "d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a72" +
    "1c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b391aafd255");
byte[] aad = Convert.FromHexString("feedfacedeadbeeffeedfacedeadbeefabaddad2");

// Bodu encrypts: ciphertext ‖ tag.
byte[] boduSealed;
using (var cipher = new AesBlockCipher(key))
    boduSealed = new GcmModeTransform(cipher, nonce).Encrypt(plaintext, associatedData: aad);

// The BCL decrypts the same bytes.
byte[] ciphertext = boduSealed[..^16];
byte[] tag        = boduSealed[^16..];          // DA80CE830CFDA02DA2A218A1744F4C76
byte[] bclPlain   = new byte[ciphertext.Length];
using (var bcl = new AesGcm(key, tagSizeInBytes: 16))
    bcl.Decrypt(nonce, ciphertext, tag, bclPlain, aad);

// And the other way round: BCL encrypts, Bodu decrypts.
byte[] bclCiphertext = new byte[plaintext.Length];
byte[] bclTag = new byte[16];
using (var bcl = new AesGcm(key, tagSizeInBytes: 16))
    bcl.Encrypt(nonce, plaintext, bclCiphertext, bclTag, aad);     // same tag: DA80CE83…744F4C76

byte[] boduPlain;
using (var cipher = new AesBlockCipher(key))
    boduPlain = new GcmModeTransform(cipher, nonce).Decrypt([.. bclCiphertext, .. bclTag], associatedData: aad);
```

Both `bclPlain` and `boduPlain` equal `plaintext`. The BCL type supports 12- to 16-byte tags; the Bodu transform always produces 16, so pass `tagSizeInBytes: 16` on the BCL side.

> [!WARNING]
> Write `Encrypt(plaintext, associatedData: aad)`, naming the argument. With two positional `byte[]` arguments the public span overload `Encrypt(plaintext, output)` wins, the AAD array becomes the *output buffer*, and the call returns an `int`.

## Pattern 6 — `XChaCha20Poly1305` is not `ChaCha20Poly1305`

The BCL's `ChaCha20Poly1305` is RFC 8439 with a 96-bit nonce. Bodu's <xref:Bodu.Security.Cryptography.XChaCha20Poly1305> is the extended-nonce construction (`draft-irtf-cfrg-xchacha`, libsodium `crypto_aead_xchacha20poly1305_ietf`): it derives a subkey with HChaCha20 from the first 16 nonce bytes and runs ChaCha20 on the remaining 8. They are different constructions and are **not** wire-compatible, even under the same key. Bodu ships no plain ChaCha20-Poly1305 because the BCL already does.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = Convert.FromHexString("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
byte[] plaintext = "same key, different constructions"u8.ToArray();

using var xchacha = new XChaCha20Poly1305(key, Convert.FromHexString("404142434445464748494a4b4c4d4e4f5051525354555657"));
byte[] boduSealed = xchacha.Encrypt(plaintext);            // 24-byte nonce

byte[] bclCiphertext = new byte[plaintext.Length];
byte[] bclTag = new byte[16];
using var bcl = new ChaCha20Poly1305(key);                 // check ChaCha20Poly1305.IsSupported first
bcl.Encrypt(Convert.FromHexString("070000004041424344454647"), plaintext, bclCiphertext, bclTag);
```

Choose by the nonce discipline: a counter you can guarantee never repeats → the BCL type; nonces drawn at random → `XChaCha20Poly1305`. Both accept associated data; see [Authenticated stream ciphers](stream-aead.md).

## Pattern 7 — PEM, PKCS#8, and SPKI for X25519 and Ed25519

<xref:Bodu.Security.Cryptography.X25519> and <xref:Bodu.Security.Cryptography.Ed25519> override the `AsymmetricAlgorithm` import/export core (`ImportPkcs8PrivateKey`, `ImportSubjectPublicKeyInfo`, `TryExportPkcs8PrivateKey`, `TryExportSubjectPublicKeyInfo`) with the RFC 8410 encodings, so the **inherited** convenience members work unchanged: `ExportPkcs8PrivateKey()`, `ExportSubjectPublicKeyInfo()`, `ExportPkcs8PrivateKeyPem()`, `ExportSubjectPublicKeyInfoPem()`, and `ImportFromPem(string)`.

```csharp
using Bodu.Security.Cryptography;

using var signer = new Ed25519();
signer.ImportPrivateKey(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));

string privatePem = signer.ExportPkcs8PrivateKeyPem();       // -----BEGIN PRIVATE KEY-----  (48-byte DER)
string publicPem  = signer.ExportSubjectPublicKeyInfoPem();  // -----BEGIN PUBLIC KEY-----   (44-byte DER)
byte[] spki       = signer.ExportSubjectPublicKeyInfo();     // 302A300506032B6570032100 D75A9801…F707511A

using var verifier = new Ed25519();
verifier.ImportFromPem(publicPem);                           // dispatches to ImportSubjectPublicKeyInfo

byte[] message = "pem round trip"u8.ToArray();
bool ok = verifier.VerifyData(message, signer.SignData(message));     // true

using var restored = new Ed25519();
restored.ImportFromPem(privatePem);                          // dispatches to ImportPkcs8PrivateKey
bool same = restored.SignData(message).AsSpan().SequenceEqual(signer.SignData(message));   // true
```

The same members work on `X25519` (`-----BEGIN PUBLIC KEY-----` with the id-X25519 OID), which is how peers exchange agreement keys in PEM. What does **not** work, by design — each throws `NotSupportedException`:

| Member | X25519 / Ed25519 | ML-KEM / ML-DSA |
|---|---|---|
| `ImportPkcs8PrivateKey`, `ImportSubjectPublicKeyInfo`, `ImportFromPem` | Supported | `NotSupportedException` |
| `ExportPkcs8PrivateKey[Pem]`, `ExportSubjectPublicKeyInfo[Pem]` | Supported | `NotSupportedException` |
| `ImportEncryptedPkcs8PrivateKey`, `ExportEncryptedPkcs8PrivateKey[Pem]` | `NotSupportedException` | `NotSupportedException` |
| `ToXmlString`, `FromXmlString` | `NotSupportedException` | `NotSupportedException` |

The post-quantum types expose raw encodings only (`ExportEncapsulationKey`, `ExportDecapsulationKey`, `ImportPrivateSeed`, and their ML-DSA equivalents); see [ML-KEM](ml-kem.md) and [ML-DSA](ml-dsa.md).

## Pattern 8 — `HashAlgorithmName` in HKDF, an enum in OTP

<xref:Bodu.Security.Cryptography.Hkdf> takes a `HashAlgorithmName` and accepts SHA-1, SHA-256, SHA-384, and SHA-512 — anything else throws `ArgumentException` (`hashAlgorithm`). Its output is identical to the BCL `HKDF` for the same inputs, so the two are interchangeable (this is RFC 5869 test case 1):

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] ikm  = Convert.FromHexString("0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b");
byte[] salt = Convert.FromHexString("000102030405060708090a0b0c");
byte[] info = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9");

byte[] bodu = Hkdf.DeriveKey(HashAlgorithmName.SHA256, ikm, outputLength: 42, salt: salt, info: info);
byte[] bcl  = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, outputLength: 42, salt: salt, info: info);
// both: 3CB25F25FAACD57A90434F64D0362F2A2D2D0A90CF1A5A4C5DB02D56ECC4C5BF34007208D5B887185865
```

<xref:Bodu.Security.Cryptography.Hotp> and <xref:Bodu.Security.Cryptography.Totp> do not take a `HashAlgorithmName`; they take <xref:Bodu.Security.Cryptography.OtpHashAlgorithm> (`Sha1`, `Sha256`, `Sha512`), which maps onto the BCL HMACs internally:

```csharp
using Bodu.Security.Cryptography;

byte[] secret = "12345678901234567890"u8.ToArray();                 // the RFC 4226 / 6238 test secret
string hotp   = Hotp.GenerateCode(secret, counter: 0);              // "755224" (SHA-1, 6 digits)
string hotp256 = Hotp.GenerateCode(secret, counter: 0, digits: 6, algorithm: OtpHashAlgorithm.Sha256);
string totp   = Totp.GenerateCode(secret, DateTimeOffset.FromUnixTimeSeconds(59), digits: 8);   // "94287082"
```

## Pattern 9 — constant-time comparison

`System.Security.Cryptography.CryptographicOperations.FixedTimeEquals` is what the library itself uses. The value types wrap it: <xref:Bodu.Security.Cryptography.HashValue>, <xref:Bodu.Security.Cryptography.AuthenticationTag>, and <xref:Bodu.Security.Cryptography.SignatureValue> compare in constant time through `Equals`, `==`, and an explicit `FixedTimeEquals`; the `VerifyHash` / `TryVerifyHash` extensions do the same over a freshly computed digest.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] expected = Convert.FromHexString("ba80a53f981c4d0d6a2797b69f12f6e94c212f14685ac4b74b12bb6fdbffa2d17d87c5392aab792dc252d5de4533cc9518d38aa8dbf1925ab92386edd4009923");
using var blake = new Blake2b();                           // 512-bit default
byte[] actual = blake.ComputeHash("abc"u8.ToArray());

bool a = CryptographicOperations.FixedTimeEquals(actual, expected);
bool b = HashValue.FromBytes(actual).FixedTimeEquals(HashValue.FromBytes(expected));
bool c = HashValue.FromBytes(actual) == HashValue.ParseHex(Convert.ToHexString(expected));
bool d = blake.VerifyHash("abc"u8.ToArray(), expected);
// all true
```

See [Nonces, salts, tags, and secrets](value-types.md) for the full value-type surface.

## API summary

| BCL surface | Bodu types that plug in | Notes |
|---|---|---|
| `HashAlgorithm` (`ComputeHash`, `TransformBlock`, `ComputeHashAsync`, `CryptoStream`) | every hash: `Blake2b`, `Skein*`, `Tiger`, `Whirlpool`, `SipHash*`, `Poly1305`, `AsconHash*`, `Shake`, … | plus `AppendData` / `AppendDataAsync` / `VerifyHash*` from `HashAlgorithmExtensions` |
| `IncrementalHash` | — | use `AppendData` + empty `TransformFinalBlock` |
| `SymmetricAlgorithm` (`CreateEncryptor`, `CryptoStream`) | `Blowfish`, `Skipjack`, `Camellia`, `Twofish`, `Serpent128`, `Threefish*`, `Serpent256/512/1024` | BCL one-shots (`EncryptCbc` …) throw `NotSupportedException` |
| `AesGcm` | `GcmModeTransform` + `AesBlockCipher` | byte-identical; split `ciphertext ‖ tag` |
| `ChaCha20Poly1305` | — | `XChaCha20Poly1305` is a different construction |
| `AsymmetricAlgorithm` PEM / PKCS#8 / SPKI | `X25519`, `Ed25519` | encrypted PKCS#8 and XML throw; ML-KEM / ML-DSA are raw-only |
| `HashAlgorithmName` | `Hkdf` (SHA-1/256/384/512) | OTP uses `OtpHashAlgorithm` |
| `CryptographicOperations.FixedTimeEquals` | `HashValue`, `AuthenticationTag`, `SignatureValue`, `SecretBytes`, `VerifyHash*` | |

## Where to go next

- [Choosing a primitive](choosing-a-primitive.md) — which Bodu type, and when the BCL is the better answer.
- [Streams and async](streaming-and-async.md) — the stream and `Task` surfaces over these same abstractions.
- [Authenticated stream ciphers](stream-aead.md) — XChaCha20-Poly1305 and the NaCl secretbox layout.
- [Asymmetric algorithms overview](asymmetric-overview.md) — key encodings across the four families.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
