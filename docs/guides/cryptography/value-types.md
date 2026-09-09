---
title: Nonces, salts, tags, and secrets
---

# Nonces, salts, tags, and secrets

Cryptographic APIs pass around a lot of look-alike byte buffers: a 32-byte key, a 32-byte digest, a 32-byte salt, and a 32-byte shared secret are all `byte[]`. `Bodu.Security.Cryptography` gives the important ones their own types so a nonce cannot be handed in where a tag belongs, so equality is constant-time by default, and so secrets are zeroed when they are released. This page covers the six: <xref:Bodu.Security.Cryptography.Nonce>, <xref:Bodu.Security.Cryptography.Salt>, <xref:Bodu.Security.Cryptography.HashValue>, <xref:Bodu.Security.Cryptography.AuthenticationTag>, <xref:Bodu.Security.Cryptography.SignatureValue> (with <xref:Bodu.Security.Cryptography.SignatureFormat>), and <xref:Bodu.Security.Cryptography.SecretBytes>.

> [!NOTE]
> These are conveniences over `byte[]` and `ReadOnlySpan<byte>`; every algorithm in the library also accepts raw spans. Like the rest of the library they are not independently audited.

## Two shapes

| | `Nonce`, `Salt`, `HashValue`, `AuthenticationTag`, `SignatureValue` | `SecretBytes` |
|---|---|---|
| Kind | `readonly struct`, immutable, `IEquatable<T>` | `sealed class`, `IDisposable` |
| Backing store | a private defensive copy; `default` is the empty value (`IsEmpty`, `Length == 0`) | a **pinned** array (`GC.AllocateArray(pinned: true)`) so the runtime never copies it during compaction |
| Construction | `FromBytes(ReadOnlySpan<byte>)`; `Random(int)` on `Nonce` and `Salt`; `ParseHex` / `TryParseHex` on `HashValue`; `FromBytes(bytes, SignatureFormat)` on `SignatureValue` | `CopyFrom(ReadOnlySpan<byte>)`, `Random(int)` |
| Read | `AsSpan()`, `ToArray()` (a fresh copy each call), `Length`, `IsEmpty` | `AsSpan()`, `ToArray()`, `Length` — throw `ObjectDisposedException` after `Dispose` |
| Equality | `Equals`, `==`, `!=` — **constant-time** via `CryptographicOperations.FixedTimeEquals`; `GetHashCode` over the bytes | no `Equals` override; `FixedTimeEquals(SecretBytes)` / `FixedTimeEquals(ReadOnlySpan<byte>)` |
| Explicit `FixedTimeEquals` | `HashValue`, `AuthenticationTag` (also a span overload), `SignatureValue` — not on `Nonce` / `Salt`, whose `Equals` is already constant-time | yes |
| `ToString()` | lowercase hex of the bytes (`HashValue` / `SignatureValue` also `ToHexString()`, `ToBase64String()`) | `"SecretBytes (Length = n)"` — never the bytes |
| Zeroization | none — these are public values by design | `Clear()` zeroes in place; `Dispose()` zeroes once and latches |

None of the structs enforce semantics (uniqueness, length); they carry bytes and intent. `Nonce.Random` and `Salt.Random` draw from the library's cryptographic generator and reject lengths ≤ 0 with `ArgumentOutOfRangeException`.

## Pattern 1 — a `Nonce` into AES-GCM

<xref:Bodu.Security.Cryptography.GcmModeTransform> is the one transform with a `Nonce`-typed constructor beside its `byte[]` and span constructors; it still requires exactly 12 bytes and throws `ArgumentException` otherwise. Reconstruct the nonce from the wire on the receiving side with `FromBytes`.

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = Convert.FromHexString("feffe9928665731c6d6a8f9467308308");
byte[] plaintext = "nonce as a value type"u8.ToArray();

Nonce nonce = Nonce.Random(12);                    // per message; unique, not secret

byte[] sealed_;
using (var aes = new AesBlockCipher(key))
    sealed_ = new GcmModeTransform(aes, nonce).Encrypt(plaintext);

// The nonce travels with the ciphertext. On the other side:
Nonce received = Nonce.FromBytes(nonce.ToArray());
byte[] recovered;
using (var aes = new AesBlockCipher(key))
    recovered = new GcmModeTransform(aes, received).Decrypt(sealed_);

bool same = nonce == received;                     // true, compared in constant time
```

The stream AEADs and `AsconAead128` take `byte[]` / span nonces; pass `nonce.AsSpan()`.

## Pattern 2 — `SecretBytes` and `Salt` into scrypt

<xref:Bodu.Security.Cryptography.Scrypt> has the one overload in the library typed on these values: `DeriveKey(SecretBytes password, Salt salt, int costN, int blockSizeR, int parallelization, int length)`. The password buffer is pinned for its lifetime and zeroed on `Dispose`.

```csharp
using Bodu.Security.Cryptography;

Salt salt = Salt.Random(16);                                                   // store beside the hash
using SecretBytes password = SecretBytes.CopyFrom("correct horse battery staple"u8);

byte[] key = Scrypt.DeriveKey(password, salt, costN: 1 << 14, blockSizeR: 8, parallelization: 1, length: 32);

bool matches = password.FixedTimeEquals("correct horse battery staple"u8);    // true
Console.WriteLine(password);                                                   // "SecretBytes (Length = 28)" — never the bytes
```

After `Dispose` — or the end of the `using` — `AsSpan`, `ToArray`, `FixedTimeEquals`, and `Clear` throw `ObjectDisposedException`. With the fixed salt `000102…0f` the derived key is `D7590ACA2C9801CF06EEBA772A69DC31CE3862591D96522AC4E6BBA6AD1F31A5`.

## Pattern 3 — `HashValue` for stored digests

`HashValue` is the type for a digest you keep and compare: parse it from configuration with `ParseHex` (throws `FormatException`) or `TryParseHex`, emit it with `ToHexString` / `ToBase64String`, and compare with `==` or `FixedTimeEquals` — both constant-time.

```csharp
using Bodu.Security.Cryptography;

using var blake = new Blake2b(256);
HashValue computed = HashValue.FromBytes(blake.ComputeHash("abc"u8.ToArray()));
HashValue stored   = HashValue.ParseHex("bddd813c634239723171ef3fee98579b94964e3bb1cb3e427262c8c068d52319");

bool ok = computed == stored;                          // true
bool ok2 = computed.FixedTimeEquals(stored);           // true
string hex = computed.ToHexString();                   // bddd813c…d52319
string b64 = computed.ToBase64String();                // vd2BPGNCOXIxce8/7phXm5SWTjuxyz5CcmLIwGjVIxk=
bool parsed = HashValue.TryParseHex("not hex", out _); // false
```

## Pattern 4 — `AuthenticationTag` and detached AEAD

`EncryptDetached` / `DecryptDetached` on <xref:Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions> split the tag out of the `ciphertext ‖ tag` layout for protocols that carry it out of band. They are defined for <xref:Bodu.Security.Cryptography.IAeadBlockCipherModeTransform> (GCM, GCM-SIV, CCM, OCB, EAX, SIV, and `AsconAead128`); a tag of the wrong length throws `ArgumentException`, a wrong tag `CryptographicException`.

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key   = Convert.FromHexString("feffe9928665731c6d6a8f9467308308");
byte[] nonce = Convert.FromHexString("cafebabefacedbaddecaf888");
byte[] plaintext = "detached tag"u8.ToArray();
byte[] aad = "hdr"u8.ToArray();

(byte[] ciphertext, AuthenticationTag tag) result;
using (var aes = new AesBlockCipher(key))
    result = new GcmModeTransform(aes, nonce).EncryptDetached(plaintext, associatedData: aad);
// tag: 8D192FEC98549A247E2823F469780A38 (16 bytes)

AuthenticationTag fromWire = AuthenticationTag.FromBytes(result.tag.ToArray());
byte[] recovered;
using (var aes = new AesBlockCipher(key))
    recovered = new GcmModeTransform(aes, nonce).DecryptDetached(result.ciphertext, fromWire, associatedData: aad);

bool same = result.tag.FixedTimeEquals(fromWire.AsSpan());   // true
```

The stream AEADs have no detached overloads; slice the tag off the combined output and rebuild it with `AuthenticationTag.FromBytes` — see [Authenticated stream ciphers](stream-aead.md#pattern-5--detached-tags).

## Pattern 5 — `SignatureValue` and `SignatureFormat`

`SignatureValue` records not only the bytes but the encoding they use: `SignatureFormat.Raw` (the fixed-width `r ‖ s` form Ed25519 and ML-DSA produce), `Der` (ASN.1 `SEQUENCE`), or `P1363` (the IEEE fixed-width form BCL `ECDsa` emits by default); `Unknown` is the zero value. `FromBytes` rejects an undefined format with `ArgumentOutOfRangeException`. `Equals` / `==` compare **format and bytes**; `FixedTimeEquals` compares bytes only.

```csharp
using Bodu.Security.Cryptography;

using var signer = new Ed25519();
signer.ImportPrivateKey(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));
byte[] message = [];

SignatureValue signature = SignatureValue.FromBytes(signer.SignData(message), SignatureFormat.Raw);   // 64 bytes, E5564300…8E7A100B
SignatureValue again     = SignatureValue.FromBytes(signer.SignData(message), SignatureFormat.Raw);   // Ed25519 is deterministic
SignatureValue asDer     = SignatureValue.FromBytes(signature.AsSpan(), SignatureFormat.Der);         // same bytes, other label

bool a = signature == again;                        // true
bool b = signature == asDer;                        // false — the format is part of equality
bool c = signature.FixedTimeEquals(asDer);          // true — bytes only
bool verifies = signer.VerifyData(message, signature.AsSpan());
```

No library member consumes `SignatureValue`; the signers and verifiers take spans. It is the type to store, log (`ToHexString` / `ToBase64String`), and compare signatures with, and to record the wire format a peer expects.

## Equality semantics at a glance

- All five structs implement `IEquatable<T>` with constant-time content comparison, so they work as `HashSet<T>` / dictionary keys (`GetHashCode` hashes the bytes with `HashCode.AddBytes` — it is **not** constant-time, and it is not meant to be).
- `default(T)` equals `FromBytes(ReadOnlySpan<byte>.Empty)`; both report `IsEmpty` and print as an empty string.
- `ToArray()` returns a fresh copy every time; mutate it freely.
- `SecretBytes` deliberately has no value equality or hash code — a secret should never be a dictionary key.

## API summary

| Type | Create | Compare | Emit | Consumed by |
|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.Nonce> | `FromBytes`, `Random(int)` | `==`, `Equals` (constant-time) | `AsSpan`, `ToArray`, `ToString` (hex) | `GcmModeTransform(IBlockCipher, Nonce)` |
| <xref:Bodu.Security.Cryptography.Salt> | `FromBytes`, `Random(int)` | `==`, `Equals` (constant-time) | `AsSpan`, `ToArray`, `ToString` | `Scrypt.DeriveKey(SecretBytes, Salt, …)` |
| <xref:Bodu.Security.Cryptography.SecretBytes> | `CopyFrom`, `Random(int)` | `FixedTimeEquals(SecretBytes \| span)` | `AsSpan`, `ToArray`; `Clear`, `Dispose` | `Scrypt.DeriveKey(SecretBytes, Salt, …)` |
| <xref:Bodu.Security.Cryptography.HashValue> | `FromBytes`, `ParseHex`, `TryParseHex` | `==`, `FixedTimeEquals` | `ToHexString`, `ToBase64String`, `ToString` | — (storage and comparison) |
| <xref:Bodu.Security.Cryptography.AuthenticationTag> | `FromBytes` | `==`, `FixedTimeEquals(tag \| span)` | `AsSpan`, `ToArray`, `ToString` | `EncryptDetached` (returns), `DecryptDetached` (accepts) |
| <xref:Bodu.Security.Cryptography.SignatureValue> | `FromBytes(bytes, SignatureFormat)` | `==` (format + bytes), `FixedTimeEquals` (bytes) | `Format`, `ToHexString`, `ToBase64String` | — (storage and comparison) |

## Where to go next

- [Authenticated stream ciphers](stream-aead.md) — detaching a tag from the stream AEADs.
- [Using scrypt](scrypt.md) and [Using Argon2](argon2.md) — the password KDFs that salts feed.
- [Security guarantees and limitations](security-posture.md) — zeroization and constant-time claims across the library.
- [Interoperating with System.Security.Cryptography](bcl-interop.md#pattern-9--constant-time-comparison) — `CryptographicOperations.FixedTimeEquals` beside the value types.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
