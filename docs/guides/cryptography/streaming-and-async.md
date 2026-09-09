---
title: Streams and async
---

# Streams and async

Everything in `Bodu.Security.Cryptography` that touches a `Stream` lives in `Bodu.Security.Cryptography.Extensions`, layered over the BCL abstractions the types already implement: <xref:Bodu.Security.Cryptography.Extensions.SymmetricAlgorithmExtensions> and <xref:Bodu.Security.Cryptography.Extensions.SymmetricStreamAlgorithmExtensions> for ciphers, <xref:Bodu.Security.Cryptography.Extensions.ICryptoTransformExtensions> for the transforms underneath them, and <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions> for hashes — plus <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash.ComputeHashAsync(System.IO.Stream,Bodu.Security.Cryptography.MerkleTreeDiagnostics,System.Threading.CancellationToken)> and <xref:Bodu.Security.Cryptography.HashAlgorithmHelper>. This page lists every stream and `Task` member, the buffer sizes they default to, how they react to cancellation, and what they do with memory.

> [!NOTE]
> `Bodu.Security.Cryptography` is not independently audited and offers best-effort, not guaranteed, side-channel resistance.

## Pattern 1 — encrypt a stream with a block cipher

`Encrypt(Stream, Stream)` / `Decrypt(Stream, Stream)` on any `SymmetricAlgorithm` create a transform with `CreateEncryptor()` / `CreateDecryptor()`, pump the source through it in `bufferSize`-byte reads, and return the number of bytes **read**. Neither stream is disposed. The default buffer is `SymmetricAlgorithmExtensions.DefaultBufferSize` = 81920 bytes (80 KiB); the overload with `bufferSize` overrides it and rejects values ≤ 0 with `ArgumentOutOfRangeException`.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] payload = new byte[300_000];
for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i * 31);

using var alg = new Camellia { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7 };
alg.Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
alg.IV  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");

using var source = new MemoryStream(payload);
using var encrypted = new MemoryStream();
int consumed = alg.Encrypt(source, encrypted, bufferSize: 64 * 1024);     // 300000 bytes read; 300016 written (PKCS7)

encrypted.Position = 0;
using var decrypted = new MemoryStream();
int ciphertextConsumed = alg.Decrypt(encrypted, decrypted);               // default 80 KiB buffer
```

Because the transform is created from the algorithm's current `Key` / `IV` / `BlockMode` / `Padding`, set those first. The same overloads exist on <xref:Bodu.Security.Cryptography.Extensions.SymmetricStreamAlgorithmExtensions> for the stream ciphers (`ChaCha20`, `XChaCha20`, `Salsa20`, `XSalsa20`, `Rabbit`, `Hc128`).

## Pattern 2 — the same, awaitable

`EncryptAsync` / `DecryptAsync` have the same signatures plus a `CancellationToken`, and delegate to `ICryptoTransformExtensions.TransformAsync`. Cancellation is honoured between reads and **before the final block is flushed**: a token that fires after the last read but before finalization throws and leaves the target stream partial. An already-cancelled token surfaces as `TaskCanceledException` (an `OperationCanceledException`).

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] payload = new byte[300_000];
using var alg = new Camellia();
alg.Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
alg.IV  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");

using var source = new MemoryStream(payload);
using var encrypted = new MemoryStream();
await alg.EncryptAsync(source, encrypted, bufferSize: 32 * 1024, cancellationToken: CancellationToken.None);

encrypted.Position = 0;
using var decrypted = new MemoryStream();
await alg.DecryptAsync(encrypted, decrypted);          // default buffer, no token

using var cts = new CancellationTokenSource();
cts.Cancel();
try { await alg.EncryptAsync(new MemoryStream(payload), new MemoryStream(), cts.Token); }
catch (OperationCanceledException) { /* nothing was finalized */ }
```

The stream-cipher extension class has **no** `*Async` members. For an awaitable stream-cipher copy, drop one level to the transform (Pattern 3).

## Pattern 3 — `ICryptoTransform.TransformAsync`

`TransformAsync(sourceStream, targetStream, bufferSize, cancellationToken)` is the engine behind the cipher overloads and works on any `ICryptoTransform` — a Bodu <xref:Bodu.Security.Cryptography.BlockCipherTransform>, a stream-cipher transform, or a BCL one. It rents its read buffer from `ArrayPool<byte>.Shared` and returns it cleared on every exit path, wraps the target in a `CryptoStream` with `leaveOpen: true`, and disposes neither stream.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] payload = new byte[300_000];
using var alg = new ChaCha20();
alg.Key   = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
alg.Nonce = Convert.FromHexString("000000000000004a00000000");

using var source = new MemoryStream(payload);
using var encrypted = new MemoryStream();
int consumed = alg.Encrypt(source, encrypted, bufferSize: 16 * 1024);    // synchronous stream-cipher overload

// The parameterless CreateDecryptor() would throw: SymmetricStreamAlgorithm allows one transform per nonce.
// Pass the key and nonce explicitly for the second transform.
encrypted.Position = 0;
using var decrypted = new MemoryStream();
using ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, alg.Nonce);
await decryptor.TransformAsync(encrypted, decrypted, bufferSize: 16 * 1024);
```

A second call to the parameterless `CreateEncryptor()` / `CreateDecryptor()` under the same nonce throws `CryptographicException` — the library refuses to hand out two keystreams for one `(key, nonce)`. The `(key, nonce)` overloads are the documented way to build the matching decryptor.

## Pattern 4 — hashing a stream, synchronously and asynchronously

Every Bodu hash is a `HashAlgorithm`, so the BCL's `ComputeHash(Stream)` and `ComputeHashAsync(Stream, CancellationToken)` already work. The extension class adds:

| Member | Buffer | Finalizes? | Returns |
|---|---|---|---|
| `AppendData(ReadOnlySpan<byte>)` | pooled copy of the span, cleared on return | No | — |
| `AppendDataAsync(Stream, bufferSize = 4096, CancellationToken)` | pooled, cleared on every exit | No | `Task` |
| `VerifyHash(…)` / `VerifyHashAsync(Stream, byte[] \| string \| ReadOnlyMemory<byte>, CancellationToken)` | via `ComputeHash` / `ComputeHashAsync` | Yes | `bool` — constant-time compare |
| `TryVerifyHash(…)` / `TryVerifyHashAsync(…)` | as above | Yes | `bool`; `false` for `null` arguments, malformed hex, or any exception |

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] payload = new byte[300_000];
for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i * 31);

using var blake = new Blake2b(256);
byte[] expected = blake.ComputeHash(payload);                           // 7C453235…255798F6

// BCL member, inherited: reads to end, then finalizes.
using var s1 = new MemoryStream(payload);
byte[] viaBcl = await blake.ComputeHashAsync(s1);

// Bodu: feed TransformBlock from a stream, finalize when you decide to.
using var s2 = new MemoryStream(payload);
using var incremental = new Blake2b(256);
await incremental.AppendDataAsync(s2, bufferSize: 8192);
incremental.TransformFinalBlock([], 0, 0);
byte[] viaAppend = incremental.Hash!;

// Verify helpers.
using var s3 = new MemoryStream(payload);
bool ok = await blake.VerifyHashAsync(s3, expected);                    // true
using var s4 = new MemoryStream(payload);
bool okHex = await blake.TryVerifyHashAsync(s4, Convert.ToHexString(expected));   // true; hex is case-insensitive
bool empty = await blake.TryVerifyHashAsync(Stream.Null, expected);     // false, no exception
```

`VerifyHashAsync` checks the token before any I/O and throws `OperationCanceledException` directly; `TryVerifyHashAsync` swallows that into `false`. Both compare with `CryptographicOperations.FixedTimeEquals`.

## Pattern 5 — Merkle roots over a stream, in parallel

<xref:Bodu.Security.Cryptography.MerkleTreeHash> hashes a `Stream` synchronously. <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> is the awaitable, pipelined variant: `ComputeHashAsync(Stream, MerkleTreeDiagnostics? = null, CancellationToken = default)` reads eight leaf blocks per `ReadAsync`, hands leaves to per-level workers, and drains those workers before propagating a cancellation. The two produce the same root for the same `blockSize` and `fanOut`, but note the defaults differ: `MerkleTreeHash` defaults to `blockSize: 1024, fanOut: 3`, `ParallelMerkleTreeHash` to `blockSize: 4096, fanOut: 2`.

```csharp
using Bodu.Security.Cryptography;

byte[] payload = new byte[300_000];
for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i * 31);

using var sequential = new MerkleTreeHash(HashAlgorithmFactory.From(() => new Blake2b(256)), blockSize: 4096, fanOut: 2);
byte[] expected = sequential.ComputeHash(payload);

using var parallel = new ParallelMerkleTreeHash(() => new Blake2b(256), blockSize: 4096, fanOut: 2);
using var source = new MemoryStream(payload);
byte[] root = await parallel.ComputeHashAsync(source, diagnostics: null, cancellationToken: CancellationToken.None);
// root == expected: B643CFC0…C8A7179B
```

The factory delegate must return a fresh `HashAlgorithm` per call — workers never share an instance. `ComputeHashAsync` throws `InvalidOperationException` for an empty stream.

## Pattern 6 — factory-driven one-shots

<xref:Bodu.Security.Cryptography.HashAlgorithmHelper> hashes through an <xref:Bodu.Security.Cryptography.IHashAlgorithmFactory`1>, creating and disposing the algorithm per call: `HashData(factory, ReadOnlySpan<byte>)`, `HashData(factory, Stream)`, `HashDataAsync(factory, Stream, CancellationToken)` (an 8 KiB pooled buffer), and `TryHashData(factory, input, destination, out bytesWritten)`.

```csharp
using Bodu.Security.Cryptography;

byte[] payload = new byte[300_000];
using var source = new MemoryStream(payload);
byte[] digest = await HashAlgorithmHelper.HashDataAsync(HashAlgorithmFactory.From(() => new Blake2b(256)), source);
```

## Buffers, cancellation, and memory — the rules

- **Buffer sizes.** Cipher stream overloads default to 80 KiB; `AppendDataAsync` to 4 KiB; `HashAlgorithmHelper` to 8 KiB; `ParallelMerkleTreeHash` reads `8 × blockSize`. Every `bufferSize` parameter must be > 0.
- **Pooled buffers are cleared.** `TransformAsync`, `AppendData`, `AppendDataAsync`, `ComputeHashAsync` (Merkle), and the helper all rent from `ArrayPool<byte>.Shared` and return with `clearArray: true`, on success, cancellation, and exception paths alike, so plaintext never lingers in a pool.
- **Streams are never disposed** by these members; the caller owns both ends.
- **Cancellation is cooperative.** It is checked per read and, for `TransformAsync`, before the final block; a partially written target is the caller's to discard. Already-cancelled tokens fail fast before any I/O.
- **Instances are not thread-safe.** Hashes, transforms, and `ParallelMerkleTreeHash` are single-caller objects; parallelism inside the Merkle pipeline is internal.

## API summary

| Class | Stream / async members |
|---|---|
| <xref:Bodu.Security.Cryptography.Extensions.SymmetricAlgorithmExtensions> | `Encrypt(Stream, Stream[, int])`, `Decrypt(…)`, `EncryptAsync(Stream, Stream[, int], CancellationToken)`, `DecryptAsync(…)`; `DefaultBufferSize = 81920` |
| <xref:Bodu.Security.Cryptography.Extensions.SymmetricStreamAlgorithmExtensions> | `Encrypt(Stream, Stream[, int])`, `Decrypt(…)` — synchronous only |
| <xref:Bodu.Security.Cryptography.Extensions.ICryptoTransformExtensions> | `Transform(Stream, Stream, int)`, `TransformAsync(Stream, Stream, int, CancellationToken)` |
| <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions> | `AppendData`, `AppendDataAsync`, `VerifyHash`, `VerifyHashAsync`, `TryVerifyHash`, `TryVerifyHashAsync` |
| `System.Security.Cryptography.HashAlgorithm` (inherited) | `ComputeHash(Stream)`, `ComputeHashAsync(Stream, CancellationToken)` |
| <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | `ComputeHashAsync(Stream, MerkleTreeDiagnostics?, CancellationToken)` |
| <xref:Bodu.Security.Cryptography.HashAlgorithmHelper> | `HashData(factory, Stream)`, `HashDataAsync(factory, Stream, CancellationToken)` |

## Where to go next

- [Interoperating with System.Security.Cryptography](bcl-interop.md) — `CryptoStream` and the `HashAlgorithm` lifecycle.
- [Using Merkle trees](merkle-trees.md) — fan-out, Tiger-Tree hashes, and diagnostics.
- [Streaming, async, and resumable hashing](../io-hashing/streaming-and-async.md) — the same story for the non-cryptographic checksums, including `HashingStream`.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
