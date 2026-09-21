# Bodu.Security.Cryptography.Samples.CipherModesAndPadding

The layer beneath `SymmetricAlgorithm.Mode`: the `IPaddingStrategy` implementations as standalone objects, the
confidentiality mode transforms driven directly over an `IBlockCipher`, and the specialist modes — ciphertext
stealing, XTS, and the nonce-misuse-resistant authenticated modes. Three scenarios.

This is the complement to `…Samples.SymmetricAndAead`, which configures modes through the algorithm facade. Direct
use of this layer is appropriate when wiring a custom block cipher into the existing mode infrastructure, or
building a higher-level construction on top — which is exactly what `MerkleTree` and HPKE do internally.

Keys, IVs and tweaks are fixed so every byte of output is reproducible. **In real use a CBC IV must be fresh and
unpredictable per message, and a CTR/CCM/GCM-SIV nonce must never repeat under one key.**

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.CipherModesAndPadding
```

For NuGet consumers:

```bash
dotnet add package Bodu.Security.Cryptography
```

## Scenario 1 — PaddingStrategies

**Intent.** Show each padding scheme's actual bytes side by side, and make the two things that bite callers
explicit: the block size is in **bits**, and only some schemes can recover the original length on unpad.

**What it does.** Pads a 5-byte (misaligned) and an 8-byte (already aligned) message under PKCS#7, ANSI X9.23,
ISO 7816-4, ISO 10126 and Zeros, printing the padded bytes and confirming the round trip. It then shows
`ZeroPadding` losing the length, `NoPadding` refusing a misaligned input, PKCS#7 rejecting a corrupted count, and
`PaddingFactory` mapping both enums onto strategies.

**What to expect.** Each scheme's trailer is visible and distinct: PKCS#7 repeats the count (`030303`), ANSI X9.23
zero-fills and puts the count last (`000003`), ISO 7816-4 writes a `0x80` marker then zeros (`800000`), and Zeros
just zero-fills. The **aligned** case is where the reversible schemes must add a whole extra block — otherwise the
last byte of real data could be mistaken for a padding count — while Zeros adds nothing and stays at 8 bytes:

```text
  block size    : 64 bits = 8 bytes
  misaligned in : aaaaaaaaaa (5 bytes)
  aligned in    : bbbbbbbbbbbbbbbb (8 bytes)

  PKCS#7      (StripsPaddingOnUnpad=True)
    5 bytes -> 8B  aaaaaaaaaa030303
               unpad recovers the original 5 bytes exactly: True
    8 bytes -> 16B  bbbbbbbbbbbbbbbb 0808080808080808
               unpad recovers the original 8 bytes exactly: True

  ANSI X9.23  (StripsPaddingOnUnpad=True)
    5 bytes -> 8B  aaaaaaaaaa000003
               unpad recovers the original 5 bytes exactly: True
    8 bytes -> 16B  bbbbbbbbbbbbbbbb 0000000000000008
               unpad recovers the original 8 bytes exactly: True

  ISO 7816-4  (StripsPaddingOnUnpad=True)
    5 bytes -> 8B  aaaaaaaaaa800000
               unpad recovers the original 5 bytes exactly: True
    8 bytes -> 16B  bbbbbbbbbbbbbbbb 8000000000000000
               unpad recovers the original 8 bytes exactly: True

  ISO 10126   (StripsPaddingOnUnpad=True)
    5 bytes -> 8B  ??????????????03 (random fill, count in the last byte)
               unpad recovers the original 5 bytes exactly: True
    8 bytes -> 16B  ??????????????????????????????08 (random fill, count in the last byte)
               unpad recovers the original 8 bytes exactly: True

  Zeros       (StripsPaddingOnUnpad=False)
    5 bytes -> 8B  aaaaaaaaaa000000
    8 bytes -> 8B  bbbbbbbbbbbbbbbb

  ZeroPadding round-trip: in 5B -> padded 8B -> unpadded 8B (length not recovered)
  NoPadding on 5 bytes  : rejected (ArgumentException)
  NoPadding on 8 bytes  : bbbbbbbbbbbbbbbb (passes through unchanged)
  Pkcs7 bad count       : rejected (CryptographicException)

  PaddingFactory.Create:
    PaddingMode.PKCS7     -> Pkcs7Padding
    PaddingMode.Zeros     -> ZeroPadding
    PaddingMode.ANSIX923  -> Ansix923Padding
    PaddingMode.ISO10126  -> Iso10126Padding
    PaddingMode.None      -> NoPadding
    PaddingModeKind.ISO7816_4 -> Iso7816_4Padding (no BCL equivalent)
```

Two things worth pausing on. **`Pad` takes its block size in bits**, matching
`SymmetricAlgorithm.BlockSize` — passing `8` asks for alignment to a *one-byte* boundary, which every scheme
satisfies trivially and silently, so the sample's constant is named `BlockSizeBits`. And `ISO 10126` fills with
**random** bytes, encoding only the count in the last byte; its padded form differs on every run, so the sample
masks the random span as `?` rather than documenting output that cannot be reproduced.

`StripsPaddingOnUnpad=False` on `ZeroPadding` and `NoPadding` is a real limitation, not a gap: neither records how
much was added, so `Unpad` returns the input untouched and the original length is lost. Use them only when the
length is known out of band. The PKCS#7 corrupted-count rejection is also the reason padding errors must never be
reported distinguishably to a remote caller — that is the padding-oracle attack.

**APIs demonstrated.** `IPaddingStrategy.Pad` / `.Unpad` / `.StripsPaddingOnUnpad`, `Pkcs7Padding`,
`Ansix923Padding`, `Iso7816_4Padding`, `Iso10126Padding`, `ZeroPadding`, `NoPadding`,
`PaddingFactory.Create(PaddingMode)` / `.Create(PaddingModeKind)`, `PaddingModeKind.ISO7816_4`.

## Scenario 2 — ModeTransforms

**Intent.** Drive `IBlockCipherModeTransform` directly to show what a chaining mode actually does, and demonstrate
the composition the interface documents: padding is *not* the transform's job, it is the caller's.

**What it does.** Builds a `TwofishBlockCipher` (a public `IBlockCipher` with a plain constructor) and runs the
same **two identical 16-byte blocks** through ECB, CBC, CFB, OFB and CTR via `BlockCipherModeFactory`, printing the
ciphertext and whether the two ciphertext blocks match. It then composes `Pkcs7Padding` with `CbcModeTransform` on
a 21-byte message, shows the transform rejecting unpadded input, and shows CTR needing no padding at all.

**What to expect.** Only ECB's two ciphertext blocks are identical — it has no chaining, so identical plaintext
blocks always encrypt identically and the structure of the plaintext survives encryption. That is the whole reason
the other modes exist:

```text
  cipher        : Twofish, block size 128 bits = 16 bytes, 128-bit key
  plaintext     : 41414141414141414141414141414141 41414141414141414141414141414141 (two identical blocks)

  ECB - each block encrypted independently
    ciphertext  : 4401f6f8df282ff30789b898201e02a7 4401f6f8df282ff30789b898201e02a7
    round-trip  : True, identical blocks leak: True
  CBC - each block XORed with the previous ciphertext
    ciphertext  : 9666ab67d4bd04ce2d08b7af7cc5918c 813de965549401932c8f1961e10c2199
    round-trip  : True, identical blocks leak: False
  CFB - ciphertext fed back into the cipher
    ciphertext  : d5d8ce0d9166bd3f6ac6408d0e3dd97d 4d4f13f75e543a1ec3886af32c41c835
    round-trip  : True, identical blocks leak: False
  OFB - keystream generated independently of the data
    ciphertext  : d5d8ce0d9166bd3f6ac6408d0e3dd97d e2b00a180c004346b7be791367c56d52
    round-trip  : True, identical blocks leak: False
  CTR - keystream from an incrementing counter
    ciphertext  : d5d8ce0d9166bd3f6ac6408d0e3dd97d 3a71768ea35a95289e0520ea9950bc29
    round-trip  : True, identical blocks leak: False

  ^ only ECB leaks the repetition; that is what the chaining in the other modes buys.

  Composing padding with a mode (the caller's job):
    21B message -> 32B padded -> 32B ciphertext -> 21B recovered
    round-trip  : True
    unpadded 21B: rejected (CryptographicException)
    CTR on 21B  : 21B ciphertext - no padding, no length expansion
```

Notice that CFB, OFB and CTR share an identical **first** block (`d5d8ce0d…`) and diverge only from the second.
That is expected rather than a bug: all three XOR the plaintext with the encryption of their starting value, and
here the IV and the initial counter are the same bytes. They differ in how that value evolves afterwards — CFB
feeds back the ciphertext, OFB feeds back its own output, CTR increments a counter.

`IBlockCipher.BlockSize` is in **bits** here too, so byte-array work converts at the call site as `BlockSize / 8`.
Transforms are stateful (the evolving IV, feedback register or counter lives inside them), so the sample constructs
a fresh one per direction rather than reusing one.

**APIs demonstrated.** `TwofishBlockCipher`, `IBlockCipher.BlockSize`,
`BlockCipherModeFactory.Create(CipherModeKind, IBlockCipher, byte[])`, `IBlockCipherModeTransform.Transform`,
`CipherModeKind.ECB` / `.CBC` / `.CFB` / `.OFB` / `.CTR`, `CbcModeTransform`, `CtrModeTransform`,
`Pkcs7Padding.Pad` / `.Unpad`.

## Scenario 3 — SpecialistModes

**Intent.** Cover the modes that exist for one specific job: CTS when the ciphertext must be exactly as long as the
plaintext, XTS for disk sectors, and the authenticated modes CCM, SIV and GCM-SIV — ending with a measurement that
shows what "nonce-misuse resistant" actually buys.

**What it does.** Runs CTS at five lengths and below one block; encrypts identical sector contents at two different
tweaks under XTS; seals and opens under CCM, SIV and GCM-SIV with tamper rejection and the detached-tag surface.
Then, under a deliberately repeated nonce, it changes **one plaintext byte** and counts how many output bytes move.

**What to expect.** CTS never expands the ciphertext at any length — it borrows bytes from the second-to-last block
to fill the final short one — but it needs at least one full block, so it refuses 8 bytes rather than degrading to
ECB. XTS produces different ciphertext for identical plaintext at different sectors, which is how a disk encrypts
without storing a per-sector IV. The final measurement is the load-bearing one:

```text
  CTS (ciphertext stealing):
    16B -> 16B ciphertext (no expansion), round-trip True
    21B -> 21B ciphertext (no expansion), round-trip True
    31B -> 31B ciphertext (no expansion), round-trip True
    32B -> 32B ciphertext (no expansion), round-trip True
    45B -> 45B ciphertext (no expansion), round-trip True
    8B (under one block): rejected (ArgumentException)

  XTS (disk-sector encryption):
    identical 32B plaintext at sector 0 and 1:
      sector 0    : 63089d2540448c2f5ef4307a527fe40f d173df93517c75125d6befc5553ab3bf
      sector 1    : 2fa978bc150805eba960fbe9141c8417 f8c5d8687bdfd21160bbb091a3510cfb
      differ      : True (the tweak is the sector number)
      round-trip  : True

  Authenticated modes:
    CCM    : 24B -> 40B (+16 tag), round-trip True, tampered rejected (CryptographicException)
             detached: 24B + 16B tag, round-trip True
    SIV    : 24B -> 40B (+16 tag), round-trip True, tampered rejected (CryptographicException)
             detached: 24B + 16B tag, round-trip True
    GCM-SIV: 24B -> 40B (+16 tag), round-trip True, tampered rejected (CryptographicException)
             detached: 24B + 16B tag, round-trip True
    One plaintext byte changed, same key and nonce - how much of the output moves?
      CCM    : 17 of 48 bytes differ  (keystream reused - the change is localised)
      SIV    : 48 of 48 bytes differ  (keystream re-derived - everything moved)
      GCM-SIV: 48 of 48 bytes differ  (keystream re-derived - everything moved)
    Identical plaintext sealed twice is deterministic in all three (by construction):
      CCM    : same output: True
      SIV    : same output: True
      GCM-SIV: same output: True
```

**17 of 48 versus 48 of 48 is the whole point.** CCM builds its keystream from the nonce alone, so under a repeated
nonce the keystream repeats: one changed plaintext byte moves that one ciphertext byte and the 16-byte tag, and
nothing else. An observer who sees two messages sealed under the same `(key, nonce)` can XOR them and recover the
XOR of the plaintexts. SIV and GCM-SIV derive their counter from a MAC over the plaintext itself, so any change
re-derives the entire keystream — repeating a nonce then leaks only *whether* two messages were identical, never a
relationship between different ones.

The last block is included to head off a wrong conclusion: sealing the *same* plaintext twice is deterministic in
all three, so that test does **not** distinguish them. The guarantee to rely on is the one measured above.

SIV requires two independently keyed ciphers (K₁ for the S2V/CMAC step, K₂ for CTR) and ignores its `iv` argument
entirely, since the synthetic IV is derived from the data. GCM-SIV takes a master cipher plus a factory, from which
it derives per-message keys.

**APIs demonstrated.** `CtsModeTransform`, `XtsModeTransform`, `CcmModeTransform`, `SivModeTransform`,
`GcmSivModeTransform`, `IAeadBlockCipherModeTransform`, `AeadBlockCipherModeTransformExtensions.Encrypt` /
`.Decrypt` / `.EncryptDetached` / `.DecryptDetached`, `AuthenticationTag`, `AesBlockCipher`.

## A note on calling the AEAD helpers

The `byte[]`-returning `Encrypt` / `Decrypt` helpers are invoked through their declaring class rather than with
extension syntax:

```csharp
sealedBytes = AeadBlockCipherModeTransformExtensions.Encrypt(encryptor, plaintext, associatedData);
```

That is deliberate. `IAeadBlockCipherModeTransform` declares its own
`int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)`, and an instance method always beats an extension
method — so `encryptor.Encrypt(plaintext, associatedData)` binds to the span overload, treats the associated data as
the output buffer, and fails to compile on the return type. The sibling `…Samples.SymmetricAndAead` sample does the
same thing for the same reason.

## Layout

```text
Bodu.Security.Cryptography.Samples.CipherModesAndPadding/
  Program.cs                          # runs the scenarios in order
  Hex.cs                              # hex formatting, block grouping, fill
  Scenarios/PaddingStrategies.cs
  Scenarios/ModeTransforms.cs
  Scenarios/SpecialistModes.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.SymmetricAndAead` — the same modes configured through the algorithm facade
  (`SymmetricAlgorithm.Mode`), plus the block and stream ciphers and the GCM/EAX/OCB AEAD modes.
- `Bodu.Security.Cryptography.Samples.HybridEncryption` — RFC 9180 HPKE, a higher-level construction built on the
  AEAD layer this sample drives directly.
- `Bodu.Security.Cryptography.Samples.CustomHash` — the equivalent extension point on the hashing side.
