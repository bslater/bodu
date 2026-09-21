# Bodu.Security.Cryptography.Samples.SymmetricAndAead

The symmetric-encryption half of `Bodu.Security.Cryptography`: raw block ciphers, block-cipher modes, the
Ascon authenticated cipher, AEAD modes over AES, and additive stream ciphers. Every scenario uses fixed
keys, IVs, nonces, and associated data, so all ciphertext is reproducible and the round trips and
tamper-detection flags are stable. Offline; no data files.

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.SymmetricAndAead
```

> Fixed keys/IVs/nonces are a determinism device for the sample only. Real encryption uses a random key and
> a unique nonce per message — nonce reuse under one key is catastrophic for every cipher shown here.

Every scenario opens by printing a **What / Why / Expect** banner — the same three things this README
records per scenario — so a transcript stands on its own and a reader can tell a correct run from a broken
one without opening the source. The `text` blocks below show the value lines only; run the sample to see
the banner above each of them.

## Scenario 1 — BlockCiphers

**Intent.** Show the raw block-cipher primitives — the keyed permutation on a single block, before any mode
of operation — across the library's catalogue with their different key and block sizes.

**What it does.** For Threefish (256 / 512 / 1024), Twofish, Camellia, Serpent, Skipjack, and Blowfish,
encrypts one block of fixed plaintext in ECB mode with no padding under a fixed key, then decrypts and
checks the round trip.

**What to expect.**

```text
--- Block ciphers: single-block ECB round trip ---

  Threefish-256  (key  32B / block  32B) round-trips: True
      ct: 02c82e3010d30127f84ed35efeb4d6119f42e650af0401888a04fb4a091931f1
  Threefish-512  (key  64B / block  64B) round-trips: True
      ct: 195fe30a88e3d74a18712e0d0e4ec7229055d052196819b6e392a7b501b4d58352cae7b91f41171ad58e1e2a4c507ef153ea2597ce20d177c11d6ba9bf71abd8
  Threefish-1024 (key 128B / block 128B) round-trips: True
      ct: 8bd0fc35492367112b670c3ba8d38d4cc4d140f339cd1580a81cb148636b8923ee64a27c01bc86c93f7932ae2bd133e7222b83adb98b82d4e23317a50300dc1d1f0b3b058243dfc88368f5baeebcbef6183fddd9bbf028044ef7b2ec4598eb0282c3712ec706ba37fcaad3f00a173617c4edaa2f59bcd6d1d13b1e8ce6a6c8f6
  Twofish        (key  16B / block  16B) round-trips: True
      ct: 8439c1538a1c70a19fd82ada3e03eba8
  Camellia       (key  16B / block  16B) round-trips: True
      ct: 26515e25ade830519e2586903c6e7183
  Serpent-128    (key  16B / block  16B) round-trips: True
      ct: 1838aa44e3d4444cd69c9471664e422c
  Skipjack       (key  10B / block   8B) round-trips: True
      ct: 32e9a7f0bb80fb03
  Blowfish       (key  16B / block   8B) round-trips: True
      ct: 5ee32f6ff47cfbf0
```

Threefish is a *tweakable* cipher: a fresh instance auto-generates a random tweak and IV, so the sample
pins both to fixed values — without that, the two instances would disagree and the ciphertext would change
run to run. The non-tweakable ciphers need only a fixed key.

**APIs demonstrated.** `Threefish256` / `Threefish512` / `Threefish1024`, `Twofish`, `Camellia`,
`Serpent128`, `Skipjack`, `Blowfish`, `SymmetricAlgorithm.CreateEncryptor` / `TransformFinalBlock`, and
`TweakableSymmetricAlgorithm.Tweak`.

## Scenario 2 — CipherModes

**Intent.** Show how a mode of operation extends a block cipher across a multi-block message, and how CBC
(with padding) and CTR (streaming) differ in what they do to the message length.

**What it does.** Encrypts a 31-byte message under Twofish-CBC with PKCS#7 padding (which rounds up to 32
bytes) and a 32-byte message under Twofish-CTR with no padding (which preserves length), round-tripping each.

**What to expect.**

```text
--- Cipher modes over Twofish (fixed key + IV) ---

  CBC/PKCS7 : plaintext 31B -> ciphertext 32B (padded)
      ct: f3cafec872d5c67a423f0e78f717ca2e6574562e873090080f2a62b98aa5de55
      round-trips: True

  CTR       : plaintext 32B -> ciphertext 32B (no growth)
      ct: 8fa69eedf4b20e2054da8c9d53ac489cd50d3a61dfe3671ae904a156cbd9e5c7
      round-trips: True
```

CBC grows the 31-byte input to a 32-byte block boundary; CTR turns the cipher into a keystream generator and
leaves the length unchanged (so it is used here with a block-aligned message and `PaddingModeKind.None`).

**APIs demonstrated.** `Twofish.BlockMode` (`CipherModeKind.CBC` / `CipherModeKind.CTR`),
`Twofish.BlockPadding` (`PaddingModeKind.PKCS7` / `PaddingModeKind.None`), the `Encrypt` / `Decrypt`
extensions.

## Scenario 3 — AeadAscon

**Intent.** Show authenticated encryption end to end with Ascon-AEAD128 (NIST SP 800-232): confidentiality
plus integrity, where a single tampered byte causes decryption to fail rather than return garbage.

**What it does.** Encrypts `"attack at dawn"` under a fixed key, nonce, and associated data to produce
ciphertext plus a 16-byte tag; decrypts it back; then flips the first sealed byte and confirms decryption
throws.

**What to expect.**

```text
--- AEAD: Ascon-AEAD128 (fixed key + nonce + AD) ---

  plaintext        : "attack at dawn"
  ciphertext + tag : 7e2aa356feb97feaa4bd31b40f63262208e9434f0ea46cf6d75884ba322e

  decrypt (intact) : "attack at dawn"  (matches: True)
  decrypt (1 byte flipped) : rejected = True
```

The sealed output is 14 ciphertext bytes plus a 16-byte tag (30 bytes total). The associated data is
authenticated but not encrypted; supplying a different AD, key, nonce, or a mutated byte all fail the tag
check the same way.

**APIs demonstrated.** `AsconAead128` construction with a key and nonce, `ProcessAssociatedData`,
`Encrypt(ReadOnlySpan<byte>, Span<byte>)`, `Decrypt(ReadOnlySpan<byte>, Span<byte>)`.

## Scenario 4 — AeadModes

**Intent.** Show that classic block ciphers reach the same authenticated-encryption guarantee through an
AEAD *mode*, and that the library exposes several over one interface.

**What it does.** Runs AES through GCM, EAX, and OCB over a fixed key, nonce, and associated data — sealing,
opening, and then rejecting a tampered ciphertext for each.

**What to expect.**

```text
--- AEAD modes over AES (fixed key + nonce + AD) ---

  AES-GCM : round-trips=True tamper-rejected=True
      sealed: 7fba537fa84329d01149049f9720d5b1bf753497b4bf9f4dc83ae2a2ec1700bb51f91199cda3f4b9
  AES-EAX : round-trips=True tamper-rejected=True
      sealed: ffd626719c1f88b0cea3f704d0409f5dc503fdba3e7adeb489fe4bbf3a19da25d4003ff1f31e0158
  AES-OCB : round-trips=True tamper-rejected=True
      sealed: 793f2b75ac4ba8c036a5379505ed69173e208118f519f1f420a04f2d22997ca4bd5f682d02572378
```

Nonce sizing differs per mode: this GCM implementation — like the BCL's `AesGcm` and every TLS/IPsec
deployment — accepts only the 96-bit (12-byte) nonce; EAX authenticates the full block-sized 16-byte nonce;
OCB takes a block-sized IV but uses only its first 12 bytes as the nonce (the trailing four bytes are
padding — vary the leading bytes, never a trailing counter). All three share
the `IAeadBlockCipherModeTransform` surface, so one helper drives them; the sealed output is 25 ciphertext
bytes plus a 16-byte tag. The byte[]-returning `Encrypt` / `Decrypt` are called through
`AeadBlockCipherModeTransformExtensions` so the compiler does not bind to the span-writing instance overloads.

**APIs demonstrated.** `AesBlockCipher`, `GcmModeTransform` / `EaxModeTransform` / `OcbModeTransform`, the
`AeadBlockCipherModeTransformExtensions.Encrypt` / `Decrypt` helpers.

## Scenario 5 — StreamCiphers

**Intent.** Show additive stream ciphers, which XOR a key-and-nonce-derived keystream against the plaintext
and are self-inverse, and the differing nonce widths of the ChaCha/Salsa family.

**What it does.** Encrypts a fixed message under ChaCha20, XChaCha20, and Salsa20 with a fixed key and
nonce, then decrypts it back.

**What to expect.**

```text
--- Stream ciphers (fixed key + nonce) ---

  ChaCha20   (nonce 12B) round-trips: True
      ct: 6a24ec32b14d1bccc58d4e90e497cf6168795b3f8df96ded0a87c965a3beaaac
  XChaCha20  (nonce 24B) round-trips: True
      ct: f69a4373521003a54b655c392006261a451dfee13c5122fcdc1c4a24e7c97b09
  Salsa20    (nonce  8B) round-trips: True
      ct: a125fceca428ebc8b7cd4a1eadea0d4223e68aa02d31264150d75d50ec803635
```

The nonce widths differ (ChaCha20 96-bit, XChaCha20 192-bit, Salsa20 64-bit); the extended-nonce XChaCha20
variant exists precisely so a random nonce can be chosen safely without a counter.

**APIs demonstrated.** `ChaCha20`, `XChaCha20`, `Salsa20`, `SymmetricStreamAlgorithm.Key` / `Nonce`, the
`Encrypt` / `Decrypt` extensions.

## Scenario 6 — MoreCiphers

**Intent.** Cover the ciphers the other scenarios do not reach, and draw out the distinctions that matter when
choosing between near-siblings: what a *tweak* buys, why an extended nonce matters, and the fact that the two
Poly1305 constructions are not interchangeable.

**What it does.** Round-trips the wide-block tweakable Serpent variants (256/512/1024) under CBC/PKCS#7 and shows
that changing only the tweak changes the ciphertext. Round-trips Rabbit, HC-128 and XSalsa20, reading each cipher's
key and nonce width from the instance rather than assuming it. Then seals and opens under XChaCha20-Poly1305 and
XSalsa20-Poly1305, shows the latter rejecting associated data, and converts a sealed message to libsodium's byte
order and back.

**What to expect.** `Serpent128` (Scenario 1) is the familiar 128-bit-block cipher; the 256/512/1024 variants widen
the **block** as well as the key and are tweakable, so one key can be domain-separated per use without a separate
key schedule. The stream ciphers preserve length exactly. The load-bearing line is the `secretbox + AAD` rejection:

```text
--- Further ciphers ---
  Wide-block tweakable Serpent:
    Serpent256 : block  256 bits, key 256 bits, tweak 128 bits -> 96B ciphertext, round-trip True
    Serpent512 : block  512 bits, key 512 bits, tweak 128 bits -> 128B ciphertext, round-trip True
    Serpent1024: block 1024 bits, key 1024 bits, tweak 128 bits -> 128B ciphertext, round-trip True
    same key and IV, tweak 0x30 vs 0x31 differ: True

  Remaining stream ciphers:
    Rabbit   : key 128 bits, nonce  64 bits -> 65B (no expansion), round-trip True
    Hc128    : key 128 bits, nonce 128 bits -> 65B (no expansion), round-trip True
    XSalsa20 : key 256 bits, nonce 192 bits -> 65B (no expansion), round-trip True

  Extended-nonce Poly1305 AEAD:
    XChaCha20Poly1305: nonce 192 bits, 65B -> 81B (+16 tag), round-trip True, tampered rejected (CryptographicException)
    XSalsa20Poly1305 : nonce 192 bits, 65B -> 81B (+16 tag), round-trip True, tampered rejected (CryptographicException)
    secretbox + AAD  : rejected (ArgumentException) - secretbox has no AAD input
    libsodium order : tag moves to the front: True
    converts back   : True
```

`XSalsa20Poly1305` is NaCl/libsodium's **secretbox**, which has no associated-data input at all — so it requires an
empty span and raises `ArgumentException` for anything else. That is the right behaviour: a construction that
quietly dropped the associated data would leave a caller believing a header was authenticated when it was not.
`XChaCha20Poly1305` is the IETF-style AEAD and does authenticate it. Both use a 192-bit nonce, wide enough to choose
at random per message without tracking a counter.

The ciphertext lengths follow from the block size and PKCS#7: a 65-byte plaintext pads to 96 bytes under Serpent256
(32-byte blocks) and to 128 bytes under both Serpent512 and Serpent1024, because one 128-byte block already holds
it.

This library appends the tag after the ciphertext; libsodium's combined format puts it first, so
`XSalsa20Poly1305` ships `ToLibsodiumCombined` / `FromLibsodiumCombined` — getting that order wrong is a silent
interoperability failure rather than an error.

**APIs demonstrated.** `Serpent256` / `Serpent512` / `Serpent1024` over `TweakableSymmetricAlgorithm`
(`.Tweak` / `.TweakSize` / `.CreateEncryptor(key, iv, tweak)`), `Rabbit`, `Hc128`, `XSalsa20` over
`SymmetricStreamAlgorithm` (`.KeySize` / `.NonceSize` / `.Nonce` / `.Encrypt` / `.Decrypt`),
`XChaCha20Poly1305` / `XSalsa20Poly1305` (`.KeySize` / `.NonceSize` consts,
`ToLibsodiumCombined` / `FromLibsodiumCombined`), `IAeadTransform`, `AeadTransformExtensions.Encrypt` / `.Decrypt`.

## Layout

```text
Bodu.Security.Cryptography.Samples.SymmetricAndAead/
  Program.cs                    # runs the scenarios in order
  SampleConsole.cs              # the What / Why / Expect banner every scenario prints through
  Hex.cs                        # shared lowercase-hex + fixed-key-material helpers
  Scenarios/BlockCiphers.cs
  Scenarios/CipherModes.cs
  Scenarios/AeadAscon.cs
  Scenarios/AeadModes.cs
  Scenarios/StreamCiphers.cs
  Scenarios/MoreCiphers.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` — hashes, MACs, XOFs, KDFs, and OTPs.
- `Bodu.Security.Cryptography.Samples.AsymmetricKeys` — X25519, Ed25519, ML-KEM, ML-DSA.
