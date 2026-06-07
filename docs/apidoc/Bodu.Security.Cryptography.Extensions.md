---
uid: Bodu.Security.Cryptography.Extensions
---

![Bodu.Security.Cryptography](~/images/hero-crypto.svg)

## Purpose

**Bodu.Security.Cryptography.Extensions** holds the extension-method surfaces that compose on top of the BCL cryptography types and the Bodu cipher / hash bases. The types here add convenience methods that keep the cipher / hash usage sites concise without pulling additional dependencies into the core algorithms.

## Key types

- <xref:Bodu.Security.Cryptography.Extensions.SymmetricAlgorithmExtensions> — convenience methods on `System.Security.Cryptography.SymmetricAlgorithm` for one-shot encrypt / decrypt over spans and arrays.
- <xref:Bodu.Security.Cryptography.Extensions.SymmetricStreamAlgorithmExtensions> — convenience methods on <xref:Bodu.Security.Cryptography.SymmetricStreamAlgorithm> for one-shot stream-cipher encrypt / decrypt and span-friendly variants.
- <xref:Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions> — convenience methods on <xref:Bodu.Security.Cryptography.TweakableSymmetricAlgorithm> — Threefish — including tweak-passing one-shot APIs.
- <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions> — convenience methods on `System.Security.Cryptography.HashAlgorithm` for one-shot hashing over spans and string / encoding pairs.
- <xref:Bodu.Security.Cryptography.Extensions.ICryptoTransformExtensions> — convenience methods on `System.Security.Cryptography.ICryptoTransform` for one-shot transform-block / transform-final-block use without managing intermediate buffers.
- <xref:Bodu.Security.Cryptography.Extensions.AeadTransformExtensions> — `byte[]`-returning `Encrypt` / `Decrypt` overloads over the stream-cipher AEAD transforms (<xref:Bodu.Security.Cryptography.Poly1305AeadTransform> and friends), so callers can seal / open a message without sizing the output span by hand.

## Example

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

using var blowfish = Blowfish.Create();
blowfish.Key = key;

// One-shot encryption over a span — no manual buffer juggling.
byte[] ciphertext = blowfish.EncryptEcb(plaintext, PaddingMode.PKCS7);
byte[] plain      = blowfish.DecryptEcb(ciphertext, PaddingMode.PKCS7);
```

## Notes

- **Allocation-aware overloads.** Where the BCL surface only ships allocating variants, the extensions add span-based or buffer-passing overloads.
- **No new primitives.** This namespace adds no new cipher or hash *algorithms* — it ships only the convenience surfaces. The algorithms themselves live in the parent <xref:Bodu.Security.Cryptography> namespace.
- **See also:** the [Bodu.Security.Cryptography introduction](~/docs/cryptography/index.md), the [encryption basics guide](~/guides/cryptography/encryption-basics.md), the [composing primitives guide](~/guides/cryptography/composing-primitives.md).
