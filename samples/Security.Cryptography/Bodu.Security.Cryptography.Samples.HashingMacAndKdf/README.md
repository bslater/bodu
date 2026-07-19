# Bodu.Security.Cryptography.Samples.HashingMacAndKdf

The message-authentication and key-derivation half of `Bodu.Security.Cryptography`: unkeyed cryptographic
hashes, keyed hashes and a one-time MAC, extendable-output functions, incremental hashing with a
constant-time verify, key-derivation functions, and one-time passwords. Every scenario uses fixed inputs
and fixed keys, so all output is deterministic and doubles as an executable reference. Offline; no data
files.

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.HashingMacAndKdf
```

## Scenario 1 — CryptographicHashes

**Intent.** Show that a spread of the library's unkeyed hashes are all ordinary `HashAlgorithm`
implementations — a single helper drives every one of them.

**What it does.** Hashes the pangram `"The quick brown fox jumps over the lazy dog"` with BLAKE2b-512,
BLAKE3-256, Tiger/192, the three Skein sizes, and Whirlpool via `ComputeHash`, printing each digest and its
bit length.

**What to expect.**

```text
--- Cryptographic hashes over a fixed message ---
message: "The quick brown fox jumps over the lazy dog"

  BLAKE2b-512  ( 512 bits) : a8add4bdddfd93e4877d2746e62817b116364a1fa7bc148d95090bc7333b3673f82401cf7aa2e4cb1ecd90296e3f14cb5413f8ed77be73045b13914cdcd6a918
  BLAKE3-256   ( 256 bits) : 2f1514181aadccd913abd94cfa592701a5686ab23f8df1dff1b74710febc6d4a
  Tiger/192    ( 192 bits) : 6d12a41e72e644f017b6f0e2f7b44c6285f06dd5d2c5b075
  Skein-256    ( 256 bits) : c0fbd7d779b20f0a4614a66697f9e41859eaf382f14bf857e8cdb210adb9b3fe
  Skein-512    ( 512 bits) : 94c2ae036dba8783d0b3f7d6cc111ff810702f5c77707999be7e1c9486ff238a7044de734293147359b4ac7e1d09cd247c351d69826b78dcddd951f0ef912713
  Skein-1024   (1024 bits) : 4cf6152f1a7e598098d28f04e13d7742ba39b7fadbbcf2167bda4e1615d551f3f6b4edbbb391ffa09e6cc0a4af1eb366b30b5f107b437e2ea5cb586afb0341bd97dabe7cc46e7be3a054aa605395e43b243654c01ffc14c8b5443488f35d80b504a612f3d29d767106d0d9249aaa4fd99b67a94fb8661a3520004501192d84fa
  Whirlpool    ( 512 bits) : b97de512e91e3828b40d2b0fdce9ceb3c4a71f9bea8d88e75c4fa854df36725fd2b52eb6544edcacd6f8beddfea403cb55ae31f03ad62a5ef54e42ee82c3fb35
```

The Tiger/192 line is the published Tiger test vector for the pangram, so this line also cross-checks the
sample against the canonical value.

**APIs demonstrated.** `Blake2b`, `Blake3`, `Tiger`, `Skein256` / `Skein512` / `Skein1024`, `Whirlpool`,
all through `HashAlgorithm.ComputeHash`.

## Scenario 2 — KeyedHashesAndMac

**Intent.** Distinguish the keyed constructions from the plain hashes: the tag now depends on a key. Show
SipHash (a keyed PRF), BLAKE2b's keyed-MAC mode, and Poly1305 (a one-time authenticator).

**What it does.** Tags the message `"authenticate me"` under fixed keys — the SipHash reference key
`00 01 … 0f`, a fixed 32-byte BLAKE2b key, and a fixed 32-byte Poly1305 one-time key.

**What to expect.**

```text
--- Keyed hashes and a one-time MAC (fixed keys) ---
message: "authenticate me"

  SipHash-64      : 66bfbbb5720de602
  SipHash-128     : 1ad4184f44c0ca8b8fbb7b5c43e73c01
  BLAKE2b-MAC-256 : 6910ee52465b8369a49dd231e5d28e5bcfe1c6c148109140c30c7d62e7a88341
  Poly1305        : f137072adc416687d4f3129a5ba1d2f4
```

The key is set through the `KeyedHashAlgorithm` surface (`{ Key = … }`); a non-empty key is what switches
BLAKE2b into MAC mode. Poly1305 reports `CanReuseTransform == false` because it is single-use — a fresh
instance and key per message.

**APIs demonstrated.** `SipHash64`, `SipHash128`, keyed `Blake2b`, `Poly1305`, the `KeyedHashAlgorithm.Key`
initializer.

## Scenario 3 — ExtendableOutput

**Intent.** Show extendable-output functions, whose output length is a caller choice rather than a fixed
digest size, and the prefix property that makes them a stream.

**What it does.** Produces SHAKE128 output at 16 and 32 bytes, and Ascon-XOF128 output at 16 and 48 bytes,
then checks that the first 16 bytes of the longer squeeze equal the shorter one.

**What to expect.**

```text
--- Extendable-output functions (XOFs) ---
message: "extend me to any length"

  SHAKE128 (16 bytes)  : c88dee80f38df32667fc273f8f80ee82
  SHAKE128 (32 bytes)  : c88dee80f38df32667fc273f8f80ee82556363b2be83d1165b0c30f51b03bf2e
  Ascon-XOF128 (16 B)  : 2c59d2710906d41a80a0853a17a51d3f
  Ascon-XOF128 (48 B)  : 2c59d2710906d41a80a0853a17a51d3f82a9e2c6e0703fab2eda990b496c8ce279de7ffc6f02a9f2d76d15e782724326
  48-byte output extends 16-byte output? True
```

The 32-byte SHAKE line begins with the 16-byte line, and the Ascon check is `True`, illustrating that a XOF
is one output stream read to whatever length you ask for.

**APIs demonstrated.** `Shake` (output length chosen at construction), `AsconXof128.Absorb` / `Squeeze` /
`Initialize`.

## Scenario 4 — StreamingAndVerify

**Intent.** Show incremental hashing across arbitrary fragment boundaries and the constant-time comparison
helper used to check a digest without a timing side channel.

**What it does.** Feeds `"stream this message in several fragments"` to BLAKE2b in three `AppendData`
fragments, finalizes, and compares to the one-shot digest; then uses `VerifyHash` against the correct
expected value and against a value with its first nibble flipped.

**What to expect.**

```text
--- Incremental hashing and constant-time verify ---
  streamed (10|15|15) : 2798392585f687e53ab8cf5552f39e93e785cb5e87f378fa3140328d4f487a96
  one-shot            : 2798392585f687e53ab8cf5552f39e93e785cb5e87f378fa3140328d4f487a96
  streaming == one-shot? True

  VerifyHash(correct expected)  = True
  VerifyHash(tampered expected) = False
```

Streaming and one-shot agree because the algorithm buffers fragments into whole blocks internally, and
`VerifyHash` cleanly separates a match from a one-nibble corruption.

**APIs demonstrated.** The `AppendData(ReadOnlySpan<byte>)` and `VerifyHash(byte[], string)` extensions,
`TransformFinalBlock` / `Hash`.

## Scenario 5 — KeyDerivation

**Intent.** Show the three key-derivation functions the library ships, all producing a stable 32-byte key
from a fixed password and salt.

**What it does.** Derives a 32-byte key with HKDF-SHA256 (extract-then-expand, with an `info` label),
Argon2id (memory-hard, small cost parameters to stay fast), and scrypt (`N=1024, r=8, p=1`).

**What to expect.**

```text
--- Key derivation (fixed salt) ---

  HKDF-SHA256          : a96966b5e9aa98fa99d63e8fda4c1a7d3a3d5a3ff25455bcffec6a7cf32f99f6
  Argon2id (64KiB,t=2) : 8dd59f8ee49b3283656e47571e7ad28ff6ac5460a5d8831a9aef0238c7cf0561
  scrypt (N=1024,r=8)  : d4a1d7e1128d6544c7ebbe331d265c67ba134da389823540d9132ea7352188dd
```

The salt is fixed here purely so the sample reproduces; a real deployment uses a fresh random salt per
password. The cost parameters are deliberately small for a fast sample run — production values are much
higher.

**APIs demonstrated.** `Hkdf.DeriveKey`, `Argon2id.DeriveKey` with `Argon2Parameters`, `Scrypt.DeriveKey`.

## Scenario 6 — OneTimePasswords

**Intent.** Show counter-based (HOTP) and time-based (TOTP) one-time passwords generated and verified from a
fixed secret, using the canonical RFC test key so the codes are the published reference values.

**What it does.** Generates HOTP codes for counters 0-2 and verifies each; generates a TOTP code at a fixed
instant (59 seconds past the epoch) and verifies it, then shows the same code failing verification one
time-step later with a zero window.

**What to expect.**

```text
--- HOTP / TOTP (fixed key, fixed counter/time) ---

  HOTP (RFC 4226 test key):
    counter 0 -> 755224  (verify: True)
    counter 1 -> 287082  (verify: True)
    counter 2 -> 359152  (verify: True)

  TOTP at t=+59s -> 287082  (verify: True)
  same code at t=+89s (window 0) -> verify: False
```

The three HOTP codes are exactly the RFC 4226 Appendix D vectors (`755224`, `287082`, `359152`), and the
TOTP code is the 6-digit truncation of the RFC 6238 `T=59s` value — determinism here comes from injecting a
fixed `DateTimeOffset` rather than reading the clock.

**APIs demonstrated.** `Hotp.GenerateCode` / `Hotp.VerifyCode`, `Totp.GenerateCode` / `Totp.VerifyCode`
with an injected timestamp.

## Layout

```text
Bodu.Security.Cryptography.Samples.HashingMacAndKdf/
  Program.cs                       # runs the scenarios in order
  Hex.cs                           # shared lowercase-hex helper
  Scenarios/CryptographicHashes.cs
  Scenarios/KeyedHashesAndMac.cs
  Scenarios/ExtendableOutput.cs
  Scenarios/StreamingAndVerify.cs
  Scenarios/KeyDerivation.cs
  Scenarios/OneTimePasswords.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.SymmetricAndAead` — block ciphers, cipher modes, AEAD, stream ciphers.
- `Bodu.Security.Cryptography.Samples.AsymmetricKeys` — X25519, Ed25519, ML-KEM, ML-DSA.
- `Bodu.Security.Cryptography.Samples.CustomHash` — authoring a custom hash on the `BlockHashAlgorithm` base.
