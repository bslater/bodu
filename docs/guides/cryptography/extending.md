---
title: Extending the library
---

# Extending the library

`Bodu.Security.Cryptography` is built from a small number of public abstract bases and interfaces, and the shipped algorithms use only those seams. This page shows how to add your own hash, your own block cipher, and your own transform on the same footing as the built-ins — and how to prove them with the contract-test bases the library's own tests derive from. It promotes the material in the [runnable samples](../../samples/cryptography.md) (`Bodu.Security.Cryptography.Samples.CustomHash` and its `.Test` companion) into a walk-through.

> [!WARNING]
> The hash and cipher on this page are **teaching devices** with no security properties; they exist to show the plumbing. Do not ship them. Like the rest of the library, nothing here is independently audited.

## The seams

| To add a… | Derive from / implement | Then it works with |
|---|---|---|
| Hash | <xref:Bodu.Security.Cryptography.BlockHashAlgorithm> (Merkle–Damgård style, padded final block); <xref:Bodu.Security.Cryptography.DeferredFinalBlockHashAlgorithm> (BLAKE-style, last block held back); <xref:Bodu.Security.Cryptography.KeyedBlockHashAlgorithm> / <xref:Bodu.Security.Cryptography.KeyedDeferredFinalBlockHashAlgorithm> (adds `Key`); or <xref:Bodu.Security.Cryptography.BufferedBlockHashAlgorithm> for a sponge or other non-block shape | every `HashAlgorithm` consumer: `ComputeHash`, `CryptoStream`, `AppendData` / `VerifyHash`, `MerkleTreeHash`, `HashAlgorithmHelper` |
| Block cipher | <xref:Bodu.Security.Cryptography.IBlockCipher> | `BlockCipherModeFactory` (ECB / CBC / CFB / OFB / CTR), `CtsModeTransform`, `XtsModeTransform`; the AEAD transforms only if the block is 128 bits |
| `ICryptoTransform` for that cipher | <xref:Bodu.Security.Cryptography.BlockCipherTransform> (its constructors are `protected internal`) | `CryptoStream`, <xref:Bodu.Security.Cryptography.Extensions.ICryptoTransformExtensions> |
| Poly1305 AEAD over a keystream | <xref:Bodu.Security.Cryptography.Poly1305AeadTransform> + your own <xref:Bodu.Security.Cryptography.IStreamCipher> | `AeadTransformExtensions`, anything typed `IStreamAeadTransform` / `IAeadTransform` |
| Padding scheme | <xref:Bodu.Security.Cryptography.IPaddingStrategy> | hand composition (the factories only know the built-in schemes) |
| Algorithm factory | <xref:Bodu.Security.Cryptography.IHashAlgorithmFactory`1> / <xref:Bodu.Security.Cryptography.DelegateHashAlgorithmFactory`1> | `MerkleTreeHash`, `ParallelMerkleTreeHash`, `HashAlgorithmHelper` |

## Pattern 1 — a custom `BlockHashAlgorithm`

`BlockHashAlgorithm` owns the residual buffer and the byte counter; you supply the block size (in **bits**) and the digest size through `HashSizeValue`, plus three hooks: `ProcessBlock` for each full block, `PadBlock` to turn the residual into one or two final blocks, and `ProcessFinalBlock` to emit the digest. `Initialize()` must reset your chaining state after calling `base.Initialize()`, and the constructor should call it once.

```csharp
using System.Buffers.Binary;
using Bodu.Security.Cryptography;

// A tiny 64-bit, 8-byte-block digest — illustrative only, NOT a secure hash.
public sealed class Mix64Digest : BlockHashAlgorithm
{
    private const ulong Prime = 0x9E3779B97F4A7C15UL;
    private ulong _state;

    public Mix64Digest()
        : base(blockSize: 64)                   // block size in BITS
    {
        HashSizeValue = 64;                     // digest size in bits, read back through HashSize
        Initialize();
    }

    public override string AlgorithmName => "Mix64";

    public override void Initialize()
    {
        base.Initialize();                      // clears the residual buffer and byte counter
        _state = 0xCBF29CE484222325UL;
    }

    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        ulong word = BinaryPrimitives.ReadUInt64LittleEndian(block);
        _state = ulong.RotateLeft((_state ^ word) * Prime, 27) + word;
    }

    protected override int PadBlock(ReadOnlySpan<byte> block, ulong messageLength, Span<byte> destination)
    {
        // One-and-zeros padding plus the length in the last block (two blocks when the length does not fit).
        int blockBytes = BlockSize / 8;
        int total = block.Length + 1 + 8 > blockBytes ? 2 * blockBytes : blockBytes;
        Span<byte> padded = destination[..total];
        padded.Clear();
        block.CopyTo(padded);
        padded[block.Length] = 0x80;
        BinaryPrimitives.WriteUInt64LittleEndian(padded[(total - 8)..], messageLength);
        return total;
    }

    protected override byte[] ProcessFinalBlock()
    {
        ulong h = _state;
        h ^= h >> 33; h *= 0xFF51AFD7ED558CCDUL; h ^= h >> 33;
        byte[] digest = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(digest, h);
        return digest;
    }
}
```

Points the base fixes for you: `HashCore` splits arbitrary input into whole blocks and buffers the remainder; `HashFinal` calls `PadBlock` into a stack scratch (padding may be at most two blocks), feeds the result to `ProcessBlock` block by block, clears the scratch, then returns `ProcessFinalBlock()`; `Dispose` zeroes the residual block. Override `ShouldPadFinalBlock` to return `false` for algorithms that finalize an unpadded residual, and `AllowUnalignedFinalBlock` when the padded final block need not be block-aligned. Once written, the type is an ordinary `HashAlgorithm`:

```csharp
using Bodu.Security.Cryptography.Extensions;

byte[] message = "The quick brown fox jumps over the lazy dog"u8.ToArray();

using var digest = new Mix64Digest();
byte[] oneShot = digest.ComputeHash(message);        // A90C5725BABA5837

using var streaming = new Mix64Digest();
streaming.AppendData(message.AsSpan(0, 19));
streaming.AppendData(message.AsSpan(19));
streaming.TransformFinalBlock([], 0, 0);
bool same = streaming.Hash!.AsSpan().SequenceEqual(oneShot);   // true
bool ok = digest.VerifyHash(message, oneShot);                   // true
```

For a keyed hash derive from `KeyedBlockHashAlgorithm` instead: it adds the `Key` property (defensive copy, length-checked against the `keySize` you pass to its constructor, zeroed on `Dispose`) and an `OnKeyChanged` hook to rebuild key-dependent state. Reconfiguring any of these after hashing has started throws `CryptographicUnexpectedOperationException`, which `ThrowIfInvalidState()` provides for your own settable properties.

## Pattern 2 — a custom `IBlockCipher`

`IBlockCipher` is four members: `BlockSize` in bits, single-block `Encrypt` / `Decrypt`, and `Dispose`. The `EncryptBlocks` / `DecryptBlocks` loops are default interface members. Validate lengths yourself and zero any key-derived state on dispose.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

// A toy 64-bit block "cipher" (XOR with a key-derived mask) — illustrative only, NOT secure.
public sealed class XorBlockCipher : IBlockCipher
{
    private readonly byte[] _mask;
    private bool _disposed;

    public XorBlockCipher(ReadOnlySpan<byte> key)
    {
        if (key.Length != 8) throw new ArgumentException("Key must be 8 bytes.", nameof(key));
        _mask = key.ToArray();
    }

    public int BlockSize => 64;                 // bits

    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (input.Length != 8 || output.Length != 8) throw new ArgumentException("Exactly one 8-byte block is required.");
        for (int i = 0; i < 8; i++) output[i] = (byte)(input[i] ^ _mask[i]);
    }

    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output) => Encrypt(input, output);

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_mask);
        _disposed = true;
    }
}
```

The engine now composes with every classic mode and padding scheme exactly as `CamelliaBlockCipher` does (see [Modes, transforms, and factories](cipher-composition-reference.md)):

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] key = Convert.FromHexString("0001020304050607");
byte[] iv  = Convert.FromHexString("1011121314151617");
byte[] plaintext = "custom IBlockCipher + CBC + PKCS7"u8.ToArray();

using IBlockCipher cipher = new XorBlockCipher(key);
using IBlockCipherModeTransform cbc = BlockCipherModeFactory.Create(CipherModeKind.CBC, cipher, iv);
IPaddingStrategy pkcs7 = PaddingFactory.Create(PaddingMode.PKCS7);

byte[] padded = pkcs7.Pad(plaintext, cipher.BlockSize);       // 64 bits → 40 bytes
byte[] ciphertext = new byte[padded.Length];
cbc.Transform(padded, ciphertext, encrypt: true);
```

## Pattern 3 — exposing it as an `ICryptoTransform`

<xref:Bodu.Security.Cryptography.BlockCipherTransform> is the `ICryptoTransform` the wrappers return: it owns the engine, a mode transform from `BlockCipherModeFactory`, and a padding strategy, and it implements the `TransformBlock` / `TransformFinalBlock` protocol (including the held-back final block on decrypt). Its constructors are `protected internal`, so a two-line subclass is the way to put a custom engine behind `CryptoStream`:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

public sealed class XorCbcTransform : BlockCipherTransform
{
    public XorCbcTransform(byte[] key, byte[] iv, bool encrypt)
        : base(new XorBlockCipher(key), CipherModeKind.CBC, PaddingMode.PKCS7, iv, encrypt)
    {
    }
}
```

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = Convert.FromHexString("0001020304050607");
byte[] iv  = Convert.FromHexString("1011121314151617");
byte[] plaintext = "custom IBlockCipher + CBC + PKCS7"u8.ToArray();

byte[] ciphertext;
using (ICryptoTransform t = new XorCbcTransform(key, iv, encrypt: true))
    ciphertext = t.Transform(plaintext);                    // 7363636…5E3F — identical to the hand-composed bytes

byte[] recovered;
using (ICryptoTransform t = new XorCbcTransform(key, iv, encrypt: false))
    recovered = t.Transform(ciphertext);
```

The transform disposes the engine, so construct a fresh engine per transform as the sample does. The base accepts either `PaddingMode` or <xref:Bodu.Security.Cryptography.PaddingModeKind> (for ISO 7816-4), and only the five factory modes — pass `CTS` or `XTS` and the base constructor throws `NotSupportedException`.

## Pattern 4 — factories and `HashAlgorithmHelper`

Consumers that need a *fresh* algorithm per operation — Merkle trees, parallel pipelines — take an <xref:Bodu.Security.Cryptography.IHashAlgorithmFactory`1> rather than an instance. The interface is one covariant `Create()`; <xref:Bodu.Security.Cryptography.DelegateHashAlgorithmFactory`1> wraps a `Func<T>`, and `HashAlgorithmFactory.From(Func<T>)` on <xref:Bodu.Security.Cryptography.HashAlgorithmFactory> is the inference-friendly way to build one. <xref:Bodu.Security.Cryptography.HashAlgorithmHelper> then hashes through a factory, creating and disposing per call.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] message = "abc"u8.ToArray();

IHashAlgorithmFactory<Blake2b> factory = HashAlgorithmFactory.From(() => new Blake2b(256));
DelegateHashAlgorithmFactory<Mix64Digest> custom = new(() => new Mix64Digest());

byte[] a = HashAlgorithmHelper.HashData(factory, message);          // BDDD813C…D52319
byte[] b = HashAlgorithmHelper.HashData(custom, message);           // E64FA35F1559A638
Span<byte> destination = stackalloc byte[32];
bool ok = HashAlgorithmHelper.TryHashData(factory, message, destination, out int written);   // true, 32

using var merkle = new MerkleTreeHash(factory, blockSize: 1024, fanOut: 2);
byte[] root = merkle.ComputeHash(new byte[5000]);

IHashAlgorithmFactory<HashAlgorithm> general = factory;             // covariant: IHashAlgorithmFactory<out T>
```

A factory must return a new instance every time; the parallel Merkle pipeline calls it from several workers and never shares an algorithm between them.

## Pattern 5 — proving it with the contract-test bases

The library's own tests are written against abstract, generic contract bases in the `Bodu.Security.Cryptography.Test` project (namespace `Bodu.Security.Cryptography`, infrastructure in `Bodu.Security.Cryptography.Infrastructure`). They are not a NuGet package: reference the test project from your own MSTest project, as `Bodu.Security.Cryptography.Samples.CustomHash.Test` does with a `ProjectReference` to `Bodu.Security.Cryptography.Test.csproj` and `Bodu.Test.csproj`. Deriving one of them runs the same scenarios the shipped algorithms pass — constructors, `HashSize`, `Initialize`, `TransformBlock` / `TransformFinalBlock`, `TryComputeHash`, `ComputeHashAsync`, `CryptoStream`, `Dispose`, residual-buffer accumulation across chunk boundaries, block-aligned versus unaligned parity, and the padded final block.

| Base | For |
|---|---|
| `HashAlgorithmTests<TTest, TAlgorithm, TVariant>` | any `HashAlgorithm` |
| `BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>` | a `BlockHashAlgorithm` (adds `PadBlock` and residual-buffer tests) |
| `KeyedBlockHashAlgorithmTests<…>`, `KeyedDeferredFinalBlockHashAlgorithmTests<…>` | keyed hashes (adds `Key` tests) |
| `BlockCipherTests<TTest, TCipher, TVariant>` | an `IBlockCipher` (known answers, block-size rules, disposal) |
| `BlockCipherModeTests<T>`, `BlockCipherTransformTests<TTest, TTransform>`, `CryptoTransformTests<T>` | mode transforms and `ICryptoTransform` implementations |
| `AeadBlockCipherModeTests<TTest, TTransform>`, `StreamAeadTransformContractTests<T>` | AEAD transforms (single-use, tamper rejection, overlap rules) |
| `SymmetricAlgorithmTests<…>`, `TweakableSymmetricAlgorithmTests<…>`, `SymmetricStreamAlgorithmTests<…>` | wrapper types |
| `PaddingStrategyTests<T>` | an `IPaddingStrategy` |
| `AsymmetricAlgorithmTests<…>` with `KeyAgreementAlgorithmTests`, `SignatureAlgorithmTests`, `KemAlgorithmTests`, `MLKemContractTests`, `MLDsaContractTests` | asymmetric algorithms |

A hash test supplies a `HashAlgorithmSpecification` (sizes, `ICryptoTransform` flags, boundary lengths, and `KnownAnswers` built with the `MessageDigestKnownAnswer` factories — note that `Abc` hashes the ASCII string `"ABC"`, and `Sequential0To255` is the 255 bytes `0x00…0xFE`), two `CreateAlgorithm` overloads, and the incremental series: entry *k* is the digest of the bytes `0, 1, …, k−1`, with `HashBlockSize + 2` entries required (ten for an 8-byte block). The values below were produced by running the digest from Pattern 1 once and pinning the output — the same way the shipped algorithms pin theirs against published vectors.

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class Mix64DigestTests
    : BlockHashAlgorithmTests<Mix64DigestTests, Mix64Digest, Mix64DigestTests.Variant>
{
    public enum Variant { Default }

    private static readonly HashAlgorithmSpecification Specification = new()
    {
        HashSize = 64,                         // bits
        HashBlockSize = 8,                     // bytes — drives the incremental-coverage window
        InputBlockSize = 1,
        OutputBlockSize = 1,
        CanReuseTransform = true,
        CanTransformMultipleBlocks = true,
        LongInputLength = 256,
        BoundaryLengths = [1, 7, 8, 9, 16, 64],
        KnownAnswers =
        [
            MessageDigestKnownAnswer.Empty("9364f0539d4dc0c0"),
            MessageDigestKnownAnswer.Abc("63c51b0ee8e60ba2"),                  // the input is "ABC"
            MessageDigestKnownAnswer.QuickBrownFox("a90c5725baba5837"),
            MessageDigestKnownAnswer.Zeros16("555ecff6b95f37dc"),
            MessageDigestKnownAnswer.Sequential0To255("563c99d3b043715d"),
        ],
    };

    protected override HashAlgorithmSpecification GetSpecification(Variant variant) => Specification;

    protected override Mix64Digest CreateAlgorithm() => new();

    protected override Mix64Digest CreateAlgorithm(Variant variant) => new();

    // Entry k is the digest of the bytes 0, 1, …, k-1; HashBlockSize 8 requires entries 0..9.
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Variant variant) =>
    [
        "9364f0539d4dc0c0", "5315a4bb69405155", "d3bfd323eac8bf35", "84d8e5ed7fa81724", "d1a4a60f7c3f6d0e",
        "057502495947e727", "c6ccb3aef11c8ed0", "e5af0b3dca0a136d", "b35138fffd0475a7", "44f10169b5a89abe",
    ];
}
```

The `TVariant` enum lets one test class cover several configurations of an algorithm (Tiger's `Variant`, SipHash's round counts); a single-configuration algorithm declares a one-member enum as above. Every test method in the bases is `virtual`, so a derived class can override one to add an algorithm-specific expectation or mark a scenario inconclusive.

## API summary

| Type | Members you implement or call |
|---|---|
| <xref:Bodu.Security.Cryptography.BlockHashAlgorithm> | ctor `(int blockSizeBits)`; abstract `ProcessBlock`, `ProcessFinalBlock`; virtual `PadBlock` (array or span form), `ShouldPadFinalBlock`, `AllowUnalignedFinalBlock`; inherited `AlgorithmName`, `Initialize`, `BlockSize`, `ThrowIfDisposed`, `ThrowIfInvalidState`, `IsDisposed` |
| <xref:Bodu.Security.Cryptography.KeyedBlockHashAlgorithm> | ctor `(blockSize, keySize)`; `Key`; virtual `OnKeyChanged`; `KeyValue`, `KeySizeValue` |
| <xref:Bodu.Security.Cryptography.IBlockCipher> | `BlockSize`, `Encrypt`, `Decrypt`, `Dispose`; default `EncryptBlocks` / `DecryptBlocks` |
| <xref:Bodu.Security.Cryptography.BlockCipherTransform> | `protected internal` ctors `(cipher, CipherModeKind, PaddingMode \| PaddingModeKind, iv, bool encrypt)`; `ThrowIfFinalized` |
| <xref:Bodu.Security.Cryptography.IHashAlgorithmFactory`1> / <xref:Bodu.Security.Cryptography.DelegateHashAlgorithmFactory`1> / <xref:Bodu.Security.Cryptography.HashAlgorithmFactory> | `Create()`; `new DelegateHashAlgorithmFactory<T>(Func<T>)`; `HashAlgorithmFactory.From(Func<T>)` |
| <xref:Bodu.Security.Cryptography.HashAlgorithmHelper> | `HashData(factory, span \| Stream)`, `HashDataAsync`, `TryHashData` |
| Test bases (`Bodu.Security.Cryptography.Test`) | `HashAlgorithmSpecification`, `MessageDigestKnownAnswer`, `BlockHashAlgorithmTests<,,>` and the rest of the table above |

## Where to go next

- [Runnable samples](../../samples/cryptography.md) — `Bodu.Security.Cryptography.Samples.CustomHash` and its contract-test companion, built by CI.
- [Modes, transforms, and factories](cipher-composition-reference.md) — everything a custom engine plugs into.
- [Using hashes and checksums](hashing.md) — the three structural hash shapes the bases model.
- [Security guarantees and limitations](security-posture.md) — the contracts (zeroization, exceptions, single use) your type should honour.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
