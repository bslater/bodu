# Bodu.Security.Cryptography.Samples.StreamingPipelines

The `Bodu.Security.Cryptography.Extensions` surface used the way an application uses it: data that arrives and
leaves as streams. Four scenarios covering stream-to-stream encryption with a block cipher and a stream cipher, the
awaitable overloads and cancellation, the same extensions applied to the BCL's own `Aes` and `SHA256`, verifying a
download against a published manifest, and an encrypt-then-MAC sealed file composed from all of them.

Everything runs offline and deterministically: the 200 KiB payload, the keys, the IVs, and the nonce are fixed, so
every line of output is reproducible. The payload is larger than the 80 KiB default buffer, so every stream call
makes several reads.

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.StreamingPipelines
```

For NuGet consumers:

```bash
dotnet add package Bodu.Security.Cryptography
```

Every scenario opens by printing a **What / Why / Expect** banner - the same three things this README records per
scenario - so a transcript stands on its own and a reader can tell a correct run from a broken one without opening
the source. The `text` blocks below show the value lines only; run the sample to see the banner above each of them.

## Scenario 1 - EncryptingStreams

**Intent.** Show that a payload does not have to fit in memory to be encrypted, and that the stream overloads are a
faithful replacement for the array ones: the read buffer size must be invisible in the ciphertext. Show the one extra
rule a stream cipher adds - a key and nonce pair encrypts exactly one message - and that the library enforces it.

**What it does.** Configures Twofish-256 in CBC mode with PKCS#7 padding, encrypts the payload with
`Encrypt(Stream, Stream)`, and compares the result with `Encrypt(byte[])` and with a run whose `bufferSize` is 4 KiB
instead of the default 80 KiB. Decrypts with `Decrypt(Stream, Stream)`. Then encrypts the payload with XChaCha20
through the same call shape, asks the same XChaCha20 instance to encrypt a second message, and decrypts on a separate
instance holding the same key and nonce, as a receiver would.

**What to expect.** The stream overloads return the number of bytes they **read**. Twofish reads 204800 bytes and
writes 204816, because PKCS#7 always pads, adding a whole block when the input is already block-aligned. XChaCha20
writes exactly what it reads. Its second encryption is refused, because the instance latches its nonce as used once
it has issued a transform:

```text
  Twofish-256 CBC/PKCS7
    Encrypt(Stream, Stream) : read 204800 B, wrote 204816 B, ct bb7bbe8b50dde8b3...
    == Encrypt(byte[])      : True
    == 4 KiB buffer         : True
    Decrypt(Stream, Stream) : read 204816 B, recovers payload: True

  XChaCha20
    Encrypt(Stream, Stream) : read 204800 B, wrote 204800 B, ct eb044d3515774423...
    2nd Encrypt, same nonce : refused (CryptographicException: nonce already used)
    Decrypt(Stream, Stream) : recovers payload: True
```

The transform is created from the algorithm's `Key`, `IV`, `BlockMode`, and `BlockPadding` at the moment of the call,
so configure the instance first. Neither stream is disposed; the caller owns both ends.

**APIs demonstrated.** `SymmetricAlgorithmExtensions.Encrypt(Stream, Stream)`, `.Encrypt(Stream, Stream, int)`,
`.Encrypt(byte[])`, `.Decrypt(Stream, Stream)`; `SymmetricStreamAlgorithmExtensions.Encrypt(Stream, Stream)`,
`.Encrypt(byte[])`, `.Decrypt(Stream, Stream)`; `Twofish`, `XChaCha20`, `CipherModeKind`, `PaddingModeKind`.

## Scenario 2 - AsyncAndBclInterop

**Intent.** Show the awaitable overloads a server uses, what cancellation leaves behind, and that the extensions are
defined over the BCL abstractions (`SymmetricAlgorithm`, `ICryptoTransform`) rather than over Bodu types, so they
apply unchanged to the framework's own ciphers.

**What it does.** Encrypts and decrypts the payload with `EncryptAsync` / `DecryptAsync` and compares the result with
the synchronous path. Calls `EncryptAsync` again with a token that has already been cancelled. Then creates a
`System.Security.Cryptography.Aes`, encrypts the payload through the same `Encrypt(Stream, Stream)` extension,
through `TransformAsync(Stream, Stream, int)` over the transform `Aes.CreateEncryptor()` returns, and through
`Transform(ReadOnlySpan<byte>)`, comparing all three with the framework's one-shot `Aes.EncryptCbc`.

**What to expect.** The awaited ciphertext is byte-identical to the synchronous one. The cancelled call throws
`TaskCanceledException` (an `OperationCanceledException`) before anything is written, so the target is still empty.
A cancellation that lands mid-stream leaves the whole blocks already written, never a finalized padding block, so a
target that saw any cancellation is still incomplete and must be discarded. All three `Aes` routes agree with
`EncryptCbc`, which is the proof that no chaining or padding detail differs from the framework's own implementation:

```text
  Twofish-256 CBC/PKCS7, awaited
    EncryptAsync == Encrypt(byte[]) : True
    DecryptAsync recovers payload   : True
    cancelled token                 : TaskCanceledException, target length 0 B

  System.Security.Cryptography.Aes-256 CBC/PKCS7 (EncryptCbc: 204816 B, ct d7509b21e57d55c0...)
    Encrypt(Stream, Stream)         : True
    TransformAsync(Stream, Stream)  : True
    Transform(ReadOnlySpan<byte>)   : True
```

`TransformAsync` flushes the final, padded block itself, so the transform needs no `TransformFinalBlock` call, and it
rents its read buffer from `ArrayPool<byte>.Shared` and clears it on every exit path.

**APIs demonstrated.** `SymmetricAlgorithmExtensions.EncryptAsync(Stream, Stream, CancellationToken)`,
`.DecryptAsync(Stream, Stream, CancellationToken)`, `.Encrypt(Stream, Stream)` on `Aes`;
`ICryptoTransformExtensions.TransformAsync(Stream, Stream, int, CancellationToken)`,
`.Transform(ReadOnlySpan<byte>)`; `Aes.EncryptCbc` as the reference.

## Scenario 3 - VerifyingDownloads

**Intent.** Check a downloaded artefact against the digests its publisher lists, the way a package manager or an
updater does: exactly, in constant time, without holding the download in memory, and without trusting the manifest to
be well formed.

**What it does.** Publishes BLAKE2b-256 and SHA-256 hex digests for the payload. Verifies the payload stream against
each with `VerifyHashAsync(Stream, string)`, using `Blake2b` and the BCL's `SHA256.Create()` through the same
extension. Digests the payload again as two separately delivered parts (a 512-byte header and the body) with
`AppendDataAsync`, closing the digest with `TransformFinalBlock`. Verifies a copy with one flipped byte. Then feeds
`TryVerifyHash` and `TryVerifyHashAsync` a missing digest, a malformed digest, and a closed stream, and contrasts
`VerifyHash` given the missing digest.

**What to expect.** Both published digests verify, the two-part digest equals the one-shot digest, and the tampered
copy fails under both algorithms. Every `Try` call returns `false` instead of throwing; the plain form treats a missing
digest as a programming error:

```text
  manifest: BLAKE2b-256 c7723a56b352b284...  SHA-256 7d228b55282b9c93...

  VerifyHashAsync, BLAKE2b-256 : True
  VerifyHashAsync, SHA-256     : True
  AppendDataAsync, two parts   : True
  tampered copy, BLAKE2b-256   : False
  tampered copy, SHA-256       : False

  TryVerifyHash, no digest listed      : False
  TryVerifyHash, malformed digest      : False
  TryVerifyHashAsync, closed stream    : False
  VerifyHash, no digest listed         : ArgumentNullException (expectedHex)
```

Hex is decoded case-insensitively, and a malformed hex digest is a non-match under both forms; the difference between
them is `null` arguments and exceptions such as the closed stream's `ObjectDisposedException`, which only the `Try`
forms turn into `false`. Comparisons use `CryptographicOperations.FixedTimeEquals`.

**APIs demonstrated.** `HashAlgorithmExtensions.VerifyHashAsync(Stream, string, CancellationToken)`,
`.AppendDataAsync(Stream, int, CancellationToken)`, `.TryVerifyHash(byte[], string)`,
`.TryVerifyHashAsync(Stream, string, CancellationToken)`, `.VerifyHash(byte[], string)`; `Blake2b`, `SHA256`.

## Scenario 4 - SealedFilePipeline

**Intent.** Compose the extensions into the standard shape for authenticating and encrypting a file too large for a
one-shot AEAD: encrypt while streaming, MAC the IV and ciphertext with an independent key, and on the way back verify
the whole tag before decrypting a single byte. For a message that fits in memory, use an AEAD instead (see
`Bodu.Security.Cryptography.Samples.SymmetricAndAead`).

**What it does.** Derives an encryption key and a MAC key from one master key with `Hkdf.DeriveKey`, binding each to
its purpose through the `info` string. Seals the payload into a `tag (32 B) | IV (16 B) | ciphertext` container: it
reserves the tag slot, writes the IV, streams the ciphertext in with `EncryptAsync`, computes a keyed BLAKE2b tag over
the IV (`AppendData`) and the ciphertext read back from the container (`AppendDataAsync`), and fills in the tag. Opens
the container by verifying everything after the tag with `VerifyHashAsync`, and only then creating a decryptor with
`TryCreateDecryptor` and decrypting with `TransformAsync`. Repeats the open on a copy with one ciphertext bit flipped.
Finally asks `TryCreateEncryptor` for a cipher under a misconfigured 20-byte key.

**What to expect.** The container is 48 bytes longer than the padded ciphertext (the tag and the IV). The intact
container verifies and decrypts back to the payload; the tampered one fails the tag check, so decryption is never
attempted and nothing reaches the output. Twofish accepts 16-, 24-, or 32-byte keys, so the 20-byte key is refused
with `false` instead of an exception:

```text
  sealed       : 204800 B plaintext -> 204864 B container (tag 1926321086475355...)
  opened       : tag verifies True, recovers payload True
  tampered     : tag verifies False, bytes decrypted 0
  20-byte key  : TryCreateEncryptor -> False, transform is null: True
```

The tag sits first so the reader can verify the rest of the container in one forward pass, and the IV sits inside the
authenticated region so it cannot be swapped either. The fixed IV is for a reproducible transcript; a real writer
draws a fresh random IV for every file.

**APIs demonstrated.** `Hkdf.DeriveKey`; `SymmetricAlgorithmExtensions.EncryptAsync(Stream, Stream)`,
`.TryCreateDecryptor(byte[], byte[], out ICryptoTransform?)`,
`.TryCreateEncryptor(byte[], byte[], out ICryptoTransform?)`; `HashAlgorithmExtensions.AppendData(ReadOnlySpan<byte>)`,
`.AppendDataAsync(Stream)`, `.VerifyHashAsync(Stream, byte[])`; `ICryptoTransformExtensions.TransformAsync(Stream, Stream, int)`; keyed
`Blake2b`.

## Layout

```text
Bodu.Security.Cryptography.Samples.StreamingPipelines/
  Program.cs                          # runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect banner every scenario prints through
  Hex.cs                              # lowercase-hex formatting, full and abbreviated
  SamplePayload.cs                    # the fixed 200 KiB payload, keys, IV, and nonce
  Scenarios/EncryptingStreams.cs
  Scenarios/AsyncAndBclInterop.cs
  Scenarios/VerifyingDownloads.cs
  Scenarios/SealedFilePipeline.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.SymmetricAndAead` - the ciphers and AEAD constructions these pipelines drive,
  one message at a time.
- `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` - the hash families, keyed hashing, and HKDF, with
  `AppendData` and `VerifyHash` over in-memory input.
- `Bodu.IO.Hashing.Samples.FileIntegrity` - the same stream, async, and verify extension shapes for the
  non-cryptographic checksums, over real files.
