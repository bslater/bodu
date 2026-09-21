# Bodu.Security.Cryptography.Samples.HashingMacAndKdf

The message-authentication and key-derivation half of `Bodu.Security.Cryptography`: unkeyed cryptographic
hashes, keyed hashes and a one-time MAC, extendable-output functions, incremental hashing with a
constant-time verify, key-derivation functions, and one-time passwords. Every scenario uses fixed inputs
and fixed keys, so all output is deterministic and doubles as an executable reference. Offline; no data
files.

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.HashingMacAndKdf
```

Every scenario opens by printing a **What / Why / Expect** banner — the same three things this README
records per scenario — so a transcript stands on its own and a reader can tell a correct run from a broken
one without opening the source. The `text` blocks below show the value lines only; run the sample to see
the banner above each of them.

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

## Scenario 2 — MoreHashFamilies

**Intent.** Cover the hash families Scenario 1 does not reach, and show the two ways this library varies a hash
without introducing a new type: constructor parameters, and a variant property.

**What it does.** Prints BLAKE2s at four output sizes, both Snefru digests, and the two Ascon hashes. Then CubeHash
at its default, at a narrower output, and with its round and block parameters tuned; `AsconCxof128` squeezing under
two different customization strings; and Tiger/Tiger2 and the three Whirlpool revisions selected by property.

**What to expect.** BLAKE2s is the 32-bit sibling of BLAKE2b — a smaller state and word size, so the better fit on
32-bit and embedded targets — and its size is chosen at construction. `AsconHashA256` is the reduced-round variant
and must differ from `AsconHash256`. The customizable XOF is the interesting one: the same message under two
different customization strings yields unrelated output, which is domain separation done by the primitive rather
than by the caller prepending a label and hoping it cannot be confused with the data:

```text
--- Further hash families ---
  Blake2s-128   : 96fd07258925748a0d2fb1c8a1167a73
  Blake2s-160   : 5a604fec9713c369e84b0ed68daed7d7504ef240
  Blake2s-224   : e4e5cb6c7cae41982b397bf7b7d2d9d1949823ae78435326e8db4912
  Blake2s-256   : 606beeec743ccbeff6cbcdf5d5302aa855c256c29b88c8ed331ea1a6bf3c8812
  Snefru-128    : 59d9539d0dd96d635b5bdbd1395bb86c
  Snefru-256    : 674caa75f9d8fd2089856b95e93a4fb42fa6c8702f8980e11d97a142d76cb358
  AsconHash256  : 23414503bf4bde7ad0e85aec94c22ae2d7cd807996b537f9564fc2974053f139
  AsconHashA256 : ffda0fa068a0ce9a89488aa405b440653c15f94469bf56888d5067d8560c6537 (reduced rounds - differs)

  CubeHash is parameterised rather than fixed:
    default (512b): a9ba7b8c6b4ecc6660bb3b35f076db7fce4930296491922744c67ef08dc1217ce5eb26bb25247e3bc8904b46d468455e6807c21410c1fb95e44824dc7d57c7ff
    256-bit out  : 01c2917df4eb1da3af412da9c9322f1d5e576f25cefc45648cff98c654d02084
    8/1 tuned    : 5785cc435babeb93fdf55a7fb4b70d5cd5992ca967f80805419cc65846a17047 (a different function, not a faster one)

  AsconCxof128 - a customizable XOF:
    "invoice-signing/v1": f95f28b1f1950d057796ca6c1d212f17963e83a46a29c6c2ce002a175f6a53cd
    "audit-log/v1"      : 1e96b3ade5e5dc9e130eafb579f2f53223267c23006e7d1a03179cee60193339
    differ             : True (same message, different domain)
    reproducible       : True (the same domain string hashes the same way twice)

  Variant-selected behaviour:
    Tiger (Tiger)   : 6d12a41e72e644f017b6f0e2f7b44c6285f06dd5d2c5b075
    Tiger (Tiger2)  : 976abff8062a2e9dcea3a1ace966ed9c19cb85558b4976d8
    WhirlpoolInfo1  : 4f8f5cb531e3d49a61cf417cd133792ccfa501fd8da53ee368fed20e5fe0248c3a0b64f98a6533cee1da614c3a8ddec791ff05fee6d971d57c1348320f4eb42d
    WhirlpoolInfo2  : 3ccf8252d8bbb258460d9aa999c06ee38e67cb546cffcf48e91f700f6fc7c183ac8cc3d3096dd30a35b01f4620a1e3a20d79cd5168544d9e1b7cdf49970e87f1
    WhirlpoolInfo3  : b97de512e91e3828b40d2b0fdce9ceb3c4a71f9bea8d88e75c4fa854df36725fd2b52eb6544edcacd6f8beddfea403cb55ae31f03ad62a5ef54e42ee82c3fb35
```

Two details worth noting. `WhirlpoolInfo1` is Whirlpool-0; the earlier revisions remain implemented because data
hashed under them still exists, but only `WhirlpoolInfo3` should be used for new work. And Tiger and Tiger2 differ
only in the padding byte that starts the final block — one byte of specification, an entirely different digest.

CubeHash's parameters are a design space, not a performance dial: changing the rounds or block size produces a
different function, and a narrower output is not a truncation of a wider one.

**APIs demonstrated.** `Blake2s(int hashSize)`, `Snefru128`, `Snefru256`, `AsconHash256`, `AsconHashA256`,
`CubeHash()` / `CubeHash(int)` / `CubeHash(int, int, int, int, int)` / `.HashSize`, `AsconCxof128.Customize` /
`.Absorb` / `.Squeeze`, `Tiger.Variant` with `TigerHashingVariant.Tiger` / `.Tiger2`, `Whirlpool.Version` with
`WhirlpoolVersion.WhirlpoolInfo1` / `.WhirlpoolInfo2` / `.WhirlpoolInfo3`.

## Scenario 3 — KeyedHashesAndMac

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

## Scenario 4 — ExtendableOutput

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

## Scenario 5 — StreamingAndVerify

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

## Scenario 6 — FactoriesAndValues

**Intent.** Show the two supporting surfaces a consumer composing hashes reaches for, and the reason each exists: a
`HashAlgorithm` is stateful, and a `byte[]` digest compares by reference.

**What it does.** Builds algorithms through `HashAlgorithmFactory.From`, confirms each `Create()` yields an
independent instance producing the same digest, and shows a factory carrying configuration. Then wraps a digest in
`HashValue` and exercises equality, `ParseHex`, and `TryParseHex` against valid, odd-length and non-hex input.

**What to expect.** The factory seam is why `MerkleTree`'s constructor takes `Func<HashAlgorithm>` rather than a
`HashAlgorithm`: a component that hashes on behalf of its caller cannot hold one instance, because two concurrent
calls would corrupt each other. On the value side, the first line is the trap — two `byte[]` digests of the same
message are not `==` to each other, while `HashValue` compares structurally:

```text
--- Hash factories and hash values ---
  IHashAlgorithmFactory<T>:
    two independent instances: True
    same digest from each     : True
    factory type              : DelegateHashAlgorithmFactory<Blake2s>
    Blake2s-256 via factory   : 84384fe8e05c1387a7a77fb6fdcb0f6bf9f1968b1ecc1bd528b534785acb7e2f
    configured Tiger2         : edd2815a70c70a744a1a98bf00a13b9a229543c2c74a56c5

  HashValue:
    byte[] == byte[]  : False (reference comparison - the trap)
    HashValue equality: True
    length            : 64 bytes, IsEmpty=False
    round-trips hex   : True
    TryParseHex valid : True -> matches: True
    TryParseHex odd   : False (odd length rejected without throwing)
    TryParseHex junk  : False (non-hex rejected without throwing)
    different message : True  (a different message gives an unequal HashValue)
```

`TryParseHex` rejects odd-length and non-hex input by returning `false` rather than throwing, so it is safe on
untrusted text — useful when an expected digest arrives from a manifest or a config file.

**APIs demonstrated.** `HashAlgorithmFactory.From<T>`, `IHashAlgorithmFactory<T>.Create`,
`DelegateHashAlgorithmFactory<T>`, `HashValue.FromBytes` / `.ParseHex` / `.TryParseHex` / `.Length` / `.IsEmpty` /
`operator ==` / `operator !=`.

## Scenario 7 — KeyDerivation

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

## Scenario 8 — PasswordHashing

**Intent.** Cover the password-hashing surface Scenario 7 does not reach: the three Argon2 variants and what
separates them, the PHC string format that is what you actually store, and `ScryptParameters` as a bound parameter
set.

**What it does.** Derives under Argon2d, Argon2i and Argon2id over one password and salt. Then produces a PHC
encoded hash with `Argon2id.Hash`, verifies it against the right and wrong password, and shows the variant being
part of the encoded value. Finally binds a `ScryptParameters` to a `Scrypt` instance, derives twice, and compares
against the positional static overload and against a higher cost.

**What to expect.** The three variants give three different values — the variant is not a tuning knob. The encoded
value carries the variant, version, parameters and salt inside itself, which is what lets old credentials keep
verifying after the cost parameters are raised for new ones:

```text
--- Password hashing (Argon2 variants and encoded hashes) ---
  parameters    : 64 KiB, 2 iterations, parallelism 1, 32-byte tag
  Argon2d       : cdb8ab7b1331663989b4271ea42a63b2be277bf041067072906920e5b374de06
  Argon2i       : 6318ae4b25876f0cb0f7e3f6c4aed296268d5fa6612b5f809753a3f538a44df9
  Argon2id      : 4218258ec7a86e150698e643766f12bfbd9e96c0bb8f7ce481bb95356c1c1e05 (the default choice)
  ^ three different functions over one password - the variant is not a tuning knob.

  Encoded hashes (what you actually store):
    stored value: $argon2id$v=19$m=64,t=2,p=1$Zml4ZWQtcGFzc3dvcmQtc2FsdA$QhgljseobhUGmOZDdm8Sv72elsC7j3zkgbuVNWwcHgU
    correct password: True
    wrong password  : False
    Argon2i value   : $argon2i$v=19$m=64,t=2,p=1$Zml4ZWQtcGFzc3dvcmQtc2FsdA$YxiuSyWHbwyw9+P2xK7SliaNX6ZhK1+Al1Oj9TikTfk
    verified by Argon2i : True

  ScryptParameters:
    bound parameters: N=1024, r=8, p=1
    derived key     : 882d073ef5fedc3abfe97aeed04442c972f668d22a2879ba7a2d178a15ac88d7
    reusable        : True, matches static overload: True
    N=2048 (2x cost): 28f3b4dcb0b258bebb2e982dde2f4896438c6b0303e68c4ad708b5512ed69b85 (a different value, as it must be)
```

The variant choice is about what the attacker is assumed to have. Argon2d uses data-dependent memory access:
strongest against GPU cracking, but its access pattern leaks through timing. Argon2i is data-independent:
side-channel resistant, weaker against time-memory trade-offs. Argon2id is the hybrid and RFC 9106's
recommendation — reach for it by default.

Cost parameters are deliberately small here so the sample stays fast; real deployments should be tuned to the
hardware.

**APIs demonstrated.** `Argon2d.DeriveKey`, `Argon2i.DeriveKey` / `.Hash` / `.Verify`, `Argon2id.DeriveKey` /
`.Hash` / `.Verify`, `Argon2Parameters`, `ScryptParameters` (`CostN` / `BlockSizeR` / `Parallelization`),
`Scrypt(ScryptParameters)` / `.Parameters` / `.GetBytes`, `Scrypt.DeriveKey`.

## Scenario 9 — OneTimePasswords

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
  SampleConsole.cs                 # the What / Why / Expect banner every scenario prints through
  Hex.cs                           # shared lowercase-hex helper
  Scenarios/CryptographicHashes.cs
  Scenarios/MoreHashFamilies.cs
  Scenarios/KeyedHashesAndMac.cs
  Scenarios/ExtendableOutput.cs
  Scenarios/StreamingAndVerify.cs
  Scenarios/FactoriesAndValues.cs
  Scenarios/KeyDerivation.cs
  Scenarios/PasswordHashing.cs
  Scenarios/OneTimePasswords.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.SymmetricAndAead` — block ciphers, cipher modes, AEAD, stream ciphers.
- `Bodu.Security.Cryptography.Samples.AsymmetricKeys` — X25519, Ed25519, ML-KEM, ML-DSA.
- `Bodu.Security.Cryptography.Samples.CustomHash` — authoring a custom hash on the `BlockHashAlgorithm` base.
