# Bodu.Security.Cryptography.Samples.CustomHash

Extending the hash catalogue: a complete consumer-authored digest — `AdditiveDigest`, a small 128-bit
Merkle-Damgard block hash — built on the library's `BlockHashAlgorithm` base (the same base Tiger,
Whirlpool, and the SHA-2 family derive from), plus a companion test project that derives the shared
`BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>` contract to prove the implementation. Offline and
deterministic; no data files.

```bash
dotnet run  --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.CustomHash
dotnet test samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.CustomHash.Test --settings bvt.runsettings
```

> This is a teaching construction, not a cryptographic primitive: it is deterministic and well-defined
> but makes no collision-resistance claims. Its value is showing that a consumer type slots into the exact
> same `HashAlgorithm` surface — and the same shared test contract — as the shipped algorithms.

## The implementation — `AdditiveDigest`

`AdditiveDigest : BlockHashAlgorithm` consumes input in 16-byte blocks, mixing each into four 32-bit
chaining words with an add / rotate / multiply / cross-diffuse round, and finalizes by appending a `0x80`
pad byte plus the little-endian message bit-length before a last avalanche. The whole contract is four
members — `AlgorithmName`, `ProcessBlock`, `PadBlock`, `ProcessFinalBlock` — plus `Initialize` to reset
the chaining state and a public parameterless constructor. The base class drives residual buffering, block
alignment, and final-block padding orchestration, so the derived type never re-implements the streaming
plumbing that `HashAlgorithm` consumers rely on.

## Scenario 1 — ImplementAndHash

**Intent.** Exercise the custom digest the way any consumer would: construct it and hash fixed inputs
through the standard `HashAlgorithm.ComputeHash` surface, confirming a consumer-authored type is an
ordinary hash with nothing special at the call site.

**What it does.** Prints the algorithm name and hash size, then hashes four fixed inputs — the empty
input, `"abc"`, the quick-brown-fox pangram, and 32 repeated `0xAB` bytes (which spans multiple 16-byte
blocks) — printing each digest as lowercase hex.

**What to expect.**

```text
--- AdditiveDigest: a custom BlockHashAlgorithm ---
name       : AdditiveDigest/128
hash size  : 128 bits

  empty      -> caac1613ec962f5c9196d874d73d3458
  abc        -> 8b2d25c74ddf3f317d378602ec42428d
  fox        -> ff043f423a5e05d9382e8badcba165dd
  32 x 0xAB  -> f584e979f6990277e78b0d311a8d5438
```

The empty-input digest is not zero: `ProcessFinalBlock` runs the finalization avalanche over the fixed
initial chaining state, so even a zero-length message produces a fully mixed digest.

**APIs demonstrated.** Deriving `BlockHashAlgorithm` (the four abstract members plus `Initialize`), the
inherited `HashAlgorithm.ComputeHash` / `HashSize` / `AlgorithmName` surface.

## Scenario 2 — BesideTheBuiltIns

**Intent.** Show the payoff of deriving the base class: the custom digest is a drop-in `HashAlgorithm`
peer. A loop typed against the base class drives `AdditiveDigest` and a shipped `Tiger` identically, and
the streaming `AppendData` extension proves the incremental path reproduces the one-shot digest.

**What it does.** Runs the pangram through both algorithms via base-class-typed `ComputeHash`, then feeds
the same message to `AdditiveDigest` in two `AppendData` fragments (19 + 24 bytes), finalizes, and
compares the streamed digest to the one-shot digest.

**What to expect.**

```text
--- AdditiveDigest beside a shipped hash ---
  AdditiveDigest : ff043f423a5e05d9382e8badcba165dd
  Tiger/192      : 6d12a41e72e644f017b6f0e2f7b44c6285f06dd5d2c5b075

  streamed 19|24 : ff043f423a5e05d9382e8badcba165dd
  one-shot       : ff043f423a5e05d9382e8badcba165dd
  streaming == one-shot? True
```

The `Tiger/192` digest is the published `Tiger` test vector for the pangram, confirming the shipped hash
is invoked correctly. The two `AdditiveDigest` lines agree because the base class buffers fragments into
whole 16-byte blocks regardless of how input arrives — so streaming and one-shot always converge.

**APIs demonstrated.** `HashAlgorithm` polymorphism, the `AppendData(ReadOnlySpan<byte>)` streaming
extension, `TransformFinalBlock` / `Hash`, running a shipped `Tiger` through the same surface.

## The contract test — `Bodu.Security.Cryptography.Samples.CustomHash.Test`

`AdditiveDigestTests` derives the library test suite's
`BlockHashAlgorithmTests<AdditiveDigestTests, AdditiveDigest, AdditiveDigest.Variant>` (namespace
`Bodu.Security.Cryptography`) and supplies only three members: a `HashAlgorithmSpecification` (hash size,
block size, boundary lengths, and five `MessageDigestKnownAnswer` rows over the canonical shared inputs),
the `CreateAlgorithm` factory, and the dense incremental-input digest table (lengths 0 through 17, spanning
the residual-buffer and 16-byte block boundaries). The inherited tests then verify the full block-buffered
hashing contract — residual-buffer accumulation across transform calls, block-aligned vs. unaligned
parity, padded-final-block correctness, streaming/async parity, `Initialize` reset semantics, disposal
state, and property reflection — the same bar `Tiger`, `Whirlpool`, and `BLAKE2b` are held to. The digests
are self-computed (the algorithm is not a standardized primitive) and pin the output so any change surfaces
as a failure.

Result: **139 passed, 0 failed, 7 skipped** (the skipped cases are the base contract's inconclusive
branches — for example the hash-size-constructor path, which `AdditiveDigest` intentionally omits). The
test project references `Bodu.Test`, the `Bodu.Security.Cryptography` source and `Bodu.Security.Cryptography.Test`
projects (where the contract base and KAT records live, per the "colocate with the consumer" rule), and the
sample, and runs in the default BVT tier.

## Layout

```text
Bodu.Security.Cryptography.Samples.CustomHash/
  Program.cs                        # runs the scenarios in order
  AdditiveDigest.cs                 # the BlockHashAlgorithm implementation
  Scenarios/ImplementAndHash.cs
  Scenarios/BesideTheBuiltIns.cs
Bodu.Security.Cryptography.Samples.CustomHash.Test/
  AdditiveDigestTests.cs            # derives BlockHashAlgorithmTests<…, AdditiveDigest, AdditiveDigest.Variant>
```

## Related

- `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` — the shipped hash, MAC, XOF, KDF, and OTP catalogue
  the custom digest joins.
