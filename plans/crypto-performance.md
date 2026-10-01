# Implementation plan: faster primitives across Bodu.Security.Cryptography

**Status:** Done and released: W0-W10 merged in #710 and shipped in `Bodu.Security.Cryptography` 1.1.0,
released out of band (#711); results in §10, whose "Left for later" items are planned in
[`crypto-performance-followups.md`](crypto-performance-followups.md) ·
**Source:** the assessment run on 2026-09-27 after the Argon2 work (§1) · **Target:** `Bodu.Security.Cryptography`
1.1.0

The Argon2 work ([`argon2-performance.md`](argon2-performance.md)) used four techniques: a
clean hot loop, pooled native memory, threads over independent work, and vector kernels
behind ISA shims. It also built the scaffolding that makes them safe to use: SIMD gates,
kernel-against-scalar sweeps, an opt-out test assembly, and an ARM64 CI job. This plan
applies the same techniques to the rest of the library, ranked by the gaps the assessment
measured, and adds the algorithmic fixes the assessment found along the way.

The assessment also found a conformance bug: AES-256-GCM-SIV does not follow RFC 8452.
It is fixed first, test first, in W0.

---

## 1. Baseline

Measured at `182b257`, whose sources for every primitive below are unchanged from 1.0.0.

- **Machine:** one 4-vCPU Xeon VM at 2.8 GHz with AVX-512F/VL, AVX2, AES-NI and
  PCLMULQDQ.
- **Software:** .NET 10.0.0 and OpenSSL 3.0.13.
- **Method:** a Stopwatch harness that warms each case up, then takes the median of five
  0.4 s rounds, with allocation per operation from `GC.GetTotalAllocatedBytes`.
- **References:** OpenSSL through `openssl speed` or P/Invoke, and the BCL, on the same
  machine.
- **Without AVX-512:** measured with `DOTNET_EnableAVX512=0`, the .NET 10 name. .NET 10
  ignores .NET 8's `DOTNET_EnableAVX512F=0`, and .NET 8 ignores the .NET 10 name.

### 1.1 Hashes and XOFs (1 MiB input)

| Primitive | Bodu | Bodu without AVX-512 | Reference |
|---|---|---|---|
| SHAKE128 | 37 MiB/s | 34 MiB/s | OpenSSL 390 MiB/s |
| SHAKE256 | 31 MiB/s | 29 MiB/s | - |
| BLAKE2b-512 | 688 MiB/s | 151 MiB/s | OpenSSL (plain C) 622-673 MiB/s |
| BLAKE2s-256 | 535 MiB/s | 78 MiB/s | OpenSSL (plain C) 394-419 MiB/s |
| BLAKE3 | 278 MiB/s | 82 MiB/s | slower than Bodu's own BLAKE2b |
| CubeHash | 234 MiB/s | 20 MiB/s | - |
| Skein-512 | 301 MiB/s | 293 MiB/s | - |

### 1.2 Ciphers, modes and AEADs (1 MiB input unless noted)

| Primitive | Bodu | Reference |
|---|---|---|
| AES-128-CTR | 59 MiB/s | OpenSSL 4.9 GiB/s; Bodu's own ECB bulk path 3.4 GiB/s |
| AES-128-GCM | 57 MiB/s | BCL `AesGcm` 4.1 GiB/s |
| AES-128-GCM, PCLMULQDQ off (the path ARM64 takes) | 3.9 MiB/s | - |
| ChaCha20 | 290 MiB/s | OpenSSL 3.4 GiB/s |
| XChaCha20-Poly1305 | 145 MiB/s; 624 B allocated per message | BCL ChaCha20-Poly1305 1.7 GiB/s |
| XChaCha20-Poly1305, 64-byte message | 1.33 µs | BCL 1.27 µs |
| Poly1305 | 439 MiB/s | - |
| Serpent-128-CTR | 3.7 MiB/s | - |
| Camellia-128-CTR | 41 MiB/s | OpenSSL CBC 182 MiB/s |
| Twofish-CTR | 83 MiB/s | - |
| Threefish-512-CTR | 240 MiB/s | - |

### 1.3 Key derivation

| Case | Bodu | OpenSSL (plain C) |
|---|---|---|
| scrypt N=2¹⁴ r=8 p=1 (16 MiB) | 112 ms; 16 MiB allocated per call; gen2 collections | 48 ms |
| scrypt N=2¹⁴ r=8 p=4 | 415 ms (units run one at a time) | 201 ms |
| scrypt N=2¹⁷ r=8 p=1 (128 MiB) | 1,430 ms; 128 MiB allocated per call | 530 ms |
| Argon2id m=19 MiB t=2 p=1, for scale | 26 ms | - |

### 1.4 Public-key operations

| Operation | Bodu | Reference |
|---|---|---|
| X25519 shared secret | 246 µs | OpenSSL 40 µs |
| X25519 key generation | 222 µs | - |
| Ed25519 sign / verify | 105 µs / 388 µs (verify allocates 2.7 KB) | OpenSSL 53 µs / 160 µs |
| ML-KEM-768 keygen / encaps / decaps | 301 / 297 / 330 µs | - |
| ML-DSA-65 keygen / sign / verify | 1.16 / 3.44 / 1.06 ms; sign allocates 119 KB | - |

A what-if in a scratch copy replaced only the Keccak permutation with an unrolled one.
SHAKE128 went from 37 to 249 MiB/s, ML-KEM-768 from 297/330 to 130/167 µs, and ML-DSA-65
verify from 1.06 ms to 0.42 ms. That permutation is the basis of W1.

---

## 2. Goals

### 2.1 Principles

- **G1 - identical output.** Every change except W0's GCM-SIV fix is byte-for-byte
  identical to 1.0.0. Published vectors lock it, and so do differential tests against
  the replaced code, which moves into the test project as the oracle.
- **G2 - in-box only.** Changes use `System.Runtime.Intrinsics`, `NativeMemory`,
  `Parallel` and `TimeProvider`, with no new package or native dependency. The target
  frameworks stay `net8.0;net10.0`.
- **G3 - every vector path has a scalar twin, tested against it.**
  - `DisableSimd` and the `Bodu.Security.Cryptography.Simd.Test` assembly keep the
    scalar paths on every vector.
  - The ARM64 job runs both suites natively.
- **G4 - constant time is kept, and improved where it can be.**
  - No new branch or memory access depends on a secret.
  - Replacing per-bit lookups (Serpent) and tweak branches (XTS) removes existing ones.
- **G5 - additive public API.** The only planned additions are a thread bound for scrypt
  (W3) and, if W5 needs it, for BLAKE3.
- **G6 - measured.** Each workstream records before and after in §10, with the harness
  from §5.

### 2.2 Targets

Targets are on the §1 machine against the §1 baseline. "Scalar" means
`DOTNET_EnableHWIntrinsic=0`.

| Workstream | Measure | Baseline | Target |
|---|---|---|---|
| W1 Keccak | SHAKE128, 1 MiB | 37 MiB/s | ≥ 200 MiB/s |
| W1 | ML-KEM-768 encaps / decaps | 297 / 330 µs | ≤ 150 / ≤ 180 µs |
| W1 | ML-DSA-65 verify | 1.06 ms | ≤ 0.5 ms |
| W2 AES modes | AES-128-CTR | 59 MiB/s | ≥ 1 GiB/s |
| W2 | AES-128-GCM | 57 MiB/s | ≥ 800 MiB/s |
| W2 | AES-128-GCM, scalar GHASH | 3.9 MiB/s | ≥ 40 MiB/s |
| W2 | GCM-SIV, CCM, EAX, OCB, XTS | per §10 | ≥ 5× their baseline |
| W3 scrypt | N=2¹⁴ r=8 p=1 | 112 ms | ≤ 50 ms |
| W3 | allocation per call after warm-up | 16 MiB | ≤ 4 KiB, with no gen2 |
| W3 | N=2¹⁴ r=8 p=4, bound 4 | 415 ms | ≤ 120 ms |
| W4 BLAKE2 | BLAKE2b scalar / AVX2 | 151 MiB/s / - | ≥ 450 / ≥ 650 MiB/s |
| W4 | BLAKE2s scalar | 78 MiB/s | ≥ 300 MiB/s |
| W5 BLAKE3 | 1 MiB, one thread, AVX2 | 82 MiB/s | ≥ 1 GiB/s |
| W6 ChaCha | ChaCha20, AVX2 | 290 MiB/s | ≥ 1 GiB/s |
| W6 | XChaCha20-Poly1305 | 145 MiB/s | ≥ 500 MiB/s; nothing allocated beyond the output |
| W6 | Poly1305 | 439 MiB/s | ≥ 1 GiB/s |
| W7 Serpent | Serpent-128-CTR scalar / 8-way | 3.7 MiB/s | ≥ 60 / ≥ 200 MiB/s |
| W8 Curve25519 | X25519 derive / keygen | 246 / 222 µs | ≤ 150 / ≤ 70 µs |
| W8 | Ed25519 sign / verify | 105 / 388 µs | ≤ 80 / ≤ 200 µs |
| W9 ML-KEM/ML-DSA | ML-KEM-768 encaps / decaps, after W1 | 130 / 167 µs | ≤ 60 / ≤ 80 µs |
| W9 | ML-DSA-65 sign median / allocation | 1.97 ms / 119 KB | ≤ 1 ms / ≤ 8 KiB |

---

## 3. Techniques carried over from Argon2

| Tag | Technique | Argon2 phase |
|---|---|---|
| **T1** | A clean hot loop: state in locals, and no per-block `stackalloc`, copy, allocation, virtual call or bounds check. `[SkipLocalsInit]` where stack buffers remain. | 2 |
| **T2** | Large per-call buffers in pooled native memory: `Argon2MatrixPool` already rents raw bytes | 3 |
| **T3** | Independent work on threads, with a bound the caller sets. The caller thread participates, and a fault surfaces unwrapped. | 4 |
| **T4** | Vector kernels behind static-abstract kernel and ISA-shim interfaces, dispatched once per call through `SimdCapabilities` gates. One 128-bit kernel serves SSSE3 and AdvSimd. | 5 |
| **T5** | Validation: every-kernel-against-scalar sweeps, the `DisableSimd` assembly, the ARM64 job, and hardware-gated coverage classification | 5-6 |

---

## 4. Workstreams

Each workstream is one or more commits on the branch, and each leaves
`dotnet test bodu.slnx --settings test.runsettings` green, CI's tier including
Regression, on `net8.0` and `net10.0`. They are ordered by value against effort, and
none depends on a later one.

### W0 - correctness and hygiene

- **AES-256-GCM-SIV (RFC 8452 §4).**
  - **The defect:**
    - `GcmSivModeTransform.DeriveKeys` always derives a 16-byte `K_enc`.
    - With a 32-byte key-generating key, the RFC requires a 32-byte `K_enc` from six
      cipher calls.
    - Bodu's AES-256 tag for Appendix C.2's first vector is `c9bad4aa…` where the RFC
      publishes `07f5f416…`. An independent derivation with the BCL's AES reproduces
      the RFC value.
    - The suite pins only Appendix C.1, the AES-128 vectors, which all pass.
  - **Red:** add Appendix C.2's 24 vectors and C.3's two counter-wrap vectors, parsed
    from the RFC text and cross-checked against the 24 C.1 rows already in the suite.
    Commit them failing.
  - **Green:**
    - The transform learns the key-generating key's size from the master cipher.
      `AesBlockCipher` reports its key size internally, and RFC 8452 defines GCM-SIV
      for AES only.
    - A 256-bit key derives a 32-byte `K_enc`.
    - AES-128, and every other cipher, keep today's 16 bytes. AES-192 is not defined by
      the RFC and keeps 16 bytes; this is documented.
  - **Compatibility:** AES-256-GCM-SIV output changes, so data sealed by earlier versions
    under a 256-bit key no longer opens. The release notes must say so (§7).
- **Hardware-acceleration docs.**
  - `hardware-acceleration.md` and `code-coverage.md` name `DOTNET_EnableAVX512F=0`,
    which .NET 10 ignores. Name both knobs.
  - `collect-coverage.sh` also sets `DOTNET_EnableAVX512=0`. It is still correct
    without it, because it sets `DOTNET_EnableHWIntrinsic=0`.
- **XTS tweak doubling** branches on the carry bit (`XtsModeTransform.GfDouble`). Make
  it mask-based, as `GaloisField128.Double` already is. The output is unchanged and the
  IEEE 1619 vectors pin it.
- **Threefish-512's AVX-512 gate** checks `Avx512F`, but its kernel calls
  `Avx512F.VL` rotates. Gate on `Avx512FVL`, which is what the kernel's remarks already
  claim.

### W1 - Keccak (SHAKE, ML-KEM, ML-DSA)

- **Problem:**
  - `KeccakPermutation.Permute` is the textbook loop: `% 5` indexing, rho and pi through
    tables, bounds-checked spans, and a `stackalloc` plus clear on every permutation.
  - It runs at about 4.3 µs per permutation, and it dominates ML-KEM and ML-DSA.
- **Design:**
  - **T1.** The unrolled, register-resident permutation from the what-if: 25 lane
    locals, with theta, rho and pi folded into one pass and chi applied per row.
  - The samplers:
    - ML-KEM `SampleNtt`, and ML-DSA `RejNttPoly` / `RejBoundedPoly` / `SampleInBall`,
      squeeze whole rate blocks and parse them, instead of pulling 1-3 bytes per call.
    - The public `Shake` absorbs full blocks straight from the caller's span.
- **Tests:**
  - The loop permutation moves into the test project as `KeccakReference`.
  - A BVT test compares the two over 1,000 seeded random states.
  - The FIPS 202 vectors and the ML-KEM/ML-DSA ACVP vectors already in the suite pin
    the end-to-end output.
- **Later, in W9:** a four-way permutation over `Vector256<ulong>` for the independent
  XOFs.

### W2 - AES modes, GHASH and POLYVAL

- **Problem:**
  - Every mode except ECB calls `IBlockCipher.Encrypt` once per 16-byte block. For
    AES, that is a cached BCL `ICryptoTransform.TransformBlock` with two copies, about
    260 ns per block.
  - `AesBlockCipher.EncryptBlocks` already runs at 3.4 GiB/s, but only ECB uses it.
  - **GHASH:**
    - It re-shuffles both operands on every block, and the reduction is not shared
      across blocks.
    - There is no ARM64 path. ARM64 runs a bit-serial, byte-wise loop at about
      240 ms per MiB.
  - **POLYVAL** reflects the bytes and the bits of each block and recomputes
    `mulX(H)` per block.
  - Several modes allocate per block or per message:
    - EAX's and SIV's CMAC allocate per block.
    - OCB, GCM-SIV and CCM copy with `ToArray`.
- **Design:**
  - **T1 - one keystream core for the counter modes.**
    - An internal helper fills a stack buffer of counter blocks (256 by default) under
      the mode's increment rule:
      - big-endian 128-bit for CTR, EAX and SIV (SIV with its two bits cleared);
      - `inc32` for GCM;
      - little-endian 32-bit for GCM-SIV;
      - CCM's q-byte counter.
    - It encrypts the buffer with one `EncryptBlocks` call.
    - It XORs with the input through one vectorized `CryptographyHelper.Xor`, which
      also replaces the six private per-mode XOR helpers.
  - **Measure `AesBlockCipher.EncryptBlocks` itself:** the one-shot `EncryptEcb`
    against the cached transform's multi-block `TransformBlock`, at 0.5 to 4 KiB. Keep
    the faster.
  - **XTS and OCB** compute each run's tweaks or offsets first, then make one
    `EncryptBlocks` / `DecryptBlocks` call, then XOR. CBC and CFB decryption batch in
    the same way.
  - **T4 - GHASH.**
    - `GaloisField128` gains a multi-block update that works on the caller's span.
    - H to H⁴ are precomputed once per key, with one Karatsuba reduction per four
      blocks.
    - It uses a carry-less multiply shim: PCLMULQDQ on x64, and ARM64 PMULL through
      `Arm.Aes.PolynomialMultiplyWidening{Lower,Upper}`.
    - The scalar fallback works on 64-bit words instead of bytes and stays mask-based.
  - **POLYVAL** runs through the same kernel, using RFC 8452 Appendix A's
    `mulX_GHASH(ByteReverse(H))` identity, prepared once per key.
  - **Remove the per-block and per-message allocations** listed above.
- **Tests:**
  - The existing NIST SP 800-38A/D, RFC 8452, IEEE 1619, RFC 7253, RFC 3610, EAX and
    RFC 5297 vectors pin the output.
  - New differential tests cover the batched keystream against block-at-a-time at
    every length from 0 to 3 × batch + 17, and across every counter wrap point.
  - A kernel sweep holds each GHASH kernel against the scalar one on seeded blocks.
  - The ARM64 job runs the PMULL path.

### W3 - scrypt

- **Problem:**
  - `ScryptCore` allocates the whole `V` (`new uint[N·32r]`) on every call, plus
    working buffers.
  - It runs the p independent ROMix units one after another.
  - Salsa20/8 is scalar, with a `stackalloc` and copy on every call.
  - BlockMix copies each sub-block.
  - The result is 2.3-2.7× slower than OpenSSL's plain C.
- **Design:** the Argon2 phases, replayed in order.
  - **T1:**
    - Salsa20/8 with its sixteen words in locals, as `ChaCha20StreamCipher` already
      does.
    - BlockMix writes its output directly.
    - ROMix writes into `V` in place.
    - `[SkipLocalsInit]`.
  - **T2:**
    - `V` is rented from the pool that Argon2 uses. The pool's type becomes a neutral,
      shared name in the same commit, with Argon2's behavior and switch kept.
    - Only cleared buffers are retained, and the caps are shared.
  - **T3:**
    - The p units run on threads under a new `maxDegreeOfParallelism`:
      `Scrypt(ScryptParameters, int)` and `Scrypt(int, int, int, int)`.
    - The default stays **1**. Unlike Argon2's lanes, each concurrent unit needs its
      own `V`, so peak memory is min(p, bound) × 128·r·N. This is documented.
  - **T4:** a 128-bit Salsa20/8 kernel for SSE2 and AdvSimd.
    - It uses the classic diagonal layout: words are permuted once per BlockMix, not per
      core call.
    - Rotations by 7, 9, 13 and 18 use shifts, and rows rotate through the shim's
      shuffles.
- **Tests:**
  - The RFC 7914 vectors.
  - A recorded corpus of today's output, taken before any change, over N from 2 to 2¹⁴,
    r ∈ {1, 2, 3, 8}, p ∈ {1, 2, 3, 4}, and several lengths.
  - A kernel sweep against scalar, a thread sweep over the bounds, and a poisoned-`V`
    test.

### W4 - BLAKE2b and BLAKE2s

- **Problem:**
  - The only SIMD tier is AVX-512VL.
  - The scalar path is written like the reference: a span working vector, index-based
    `G`, and sigma-table indirection. It runs at a quarter of OpenSSL's plain C.
  - `Argon2Blake2b` is a second copy of the same scalar code.
- **Design:**
  - **T1:** a register-resident compress with the sigma schedule unrolled into constant
    indices, shared with `Argon2Blake2b`.
  - **T4:**
    - The AVX-512 kernels use only one AVX-512 instruction, VPROR; the rest is AVX2 and
      SSE2. An AVX2 kernel for BLAKE2b therefore replaces VPROR with Argon2's rotations:
      rot32 by `PSHUFD`, rot24 and rot16 by `PSHUFB`, rot63 by add and shift.
    - A 128-bit kernel through the `IVector128Isa` shim serves BLAKE2b, which needs
      rotations by 32, 24, 16 and 63, plus `UpperThenLower`. BLAKE2s needs a
      32-bit-lane shim.
    - Dispatch order: AVX-512, then AVX2, then 128-bit, then scalar.
- **Tests:** the RFC 7693 and reference vectors, the keyed vectors, and a kernel sweep
  against scalar.

### W5 - BLAKE3

- **Problem:**
  - One 64-byte block at a time, each copied and cleared.
  - A 2D-array message schedule.
  - No compression of several chunks at once, and no threads. The result is slower than
    BLAKE2b, which BLAKE3 is designed to outrun several times over.
- **Design:**
  - **T1:** a clean compress.
  - **T4:** chunks compressed in parallel, 4, 8 or 16 at a time in `Vector128`,
    `Vector256` or `Vector512`, with the state transposed so that each lane is one
    chunk. Parents are then combined N at a time.
  - **T3:** subtrees of large inputs on threads under an opt-in bound, following
    `MerkleTree`'s pattern.
- **Tests:** the official BLAKE3 vectors, which cover every length class, keyed hashing
  and key derivation. Also a kernel sweep, and a thread sweep over input sizes around
  each subtree boundary.

### W6 - ChaCha20, Salsa20, Poly1305 and the Poly1305 AEADs

- **Problem:**
  - Keystream is produced one 64-byte block per interface call. Salsa20 keeps its state
    in a heap array.
  - `StreamCipherTransform` XORs 8 bytes at a time.
  - Poly1305 uses five 26-bit limbs, copies each block, and makes one virtual call per
    block.
  - The AEADs allocate on every message: the key and nonce via `ToArray`, a new engine
    and a new `Poly1305`. They XOR byte by byte.
- **Design:**
  - **T4:**
    - An internal bulk keystream entry point on the stream ciphers produces 4, 8 or 16
      blocks per call.
    - The layout is vertical: one state word across N blocks per vector, with counter
      lanes base + i. Rotations by 16 and 8 are byte shuffles; 12 and 7 are shifts.
    - `StreamCipherTransform` and the AEADs use it, with the vector XOR from W2.
  - **T1:**
    - A struct Poly1305 core with 64-bit limbs and `UInt128` products (three limbs of
      44, 44 and 42 bits), fed straight from the caller's span.
    - The public `Poly1305` and both AEADs share it.
    - The AEADs allocate nothing per message.
- **Tests:** the RFC 8439, XChaCha draft (Appendix A.3.1) and libsodium secretbox vectors already in the suite. Differential
  tests hold bulk keystream against single-block at every length and offset, across
  block-counter boundaries. The Poly1305 core is compared with the old implementation
  over seeded messages.

### W7 - Serpent

- **Problem:**
  - The bitsliced layout applies each S-box through a 32-iteration loop: gather one bit
    per word, look it up in a 16-entry table, scatter the result.
  - That costs about 4 µs per block.
- **Design:**
  - **T1:** Osvik's Boolean S-box circuits (S0-S7 and their inverses) replace the loop.
    They are constant time by construction.
  - **T4:** `EncryptBlocks` / `DecryptBlocks` process 4 or 8 blocks through
    `Vector128<uint>` / `Vector256<uint>` for the batched modes from W2.
- **Tests:** the NESSIE and AES-submission vectors, the wide-block variants'
  existing vectors, and a differential test of the multi-block path against single
  blocks.

### W8 - Curve25519 and Ed25519

- **Problem:**
  - `Square` is `Multiply`, and `Double` is `Add(this)`, nine multiplies each.
  - X25519 key generation runs the variable-base ladder on u = 9.
  - Verification allocates a 16-entry table and inverts twice to encode.
- **Design:**
  - A dedicated field square.
  - A dedicated extended-coordinate doubling, dbl-2008-hwcd.
  - X25519 base-point multiplication through Ed25519's fixed-base table and the
    birational map.
  - Verification compares in projective coordinates and puts its table on the stack.
    Variable time is acceptable there, because every input is public.
- **Tests:** the RFC 7748 and RFC 8032 vectors and Wycheproof. Differential tests check
  `Square(x) = Multiply(x, x)` and `Double(P) = Add(P, P)` on seeded values, and that
  base-point key generation equals the ladder.

### W9 - ML-KEM and ML-DSA beyond Keccak

- **Problem:**
  - The NTT butterflies and pointwise products reduce with a plain `% Q`.
  - The matrix, and ML-KEM's H(ek), are recomputed on every operation.
  - ML-DSA uses jagged arrays, allocates k×l polynomials per operation, and clones y on
    every rejection iteration.
- **Design:**
  - **T1:**
    - Barrett and Montgomery reduction.
    - Â and H(ek) cached on the key object when the key is set, as FIPS 203/204
      permit.
    - Flat polynomial storage, pooled or on the stack.
  - **T4:**
    - A four-way Keccak over `Vector256<ulong>` for the matrix and vector expansions.
    - An AVX2 NTT behind shims, with a 128-bit variant for AdvSimd.
- **Tests:** the ACVP vectors and the key-reuse paths. Cached keys are checked against
  fresh keys over many operations.

### W10 - lower priority

- A 128-bit kernel for CubeHash's scalar fallback, which is 12× below its AVX-512 path.
- Camellia and Twofish gain from W2's batching; no kernel work.
- Whirlpool's per-block copies.

---

## 5. Measurement

- The scratch harness becomes `--crypto-harness` in
  `Bodu.Security.Cryptography.Benchmarks`, next to `--argon2-harness`:
  - It has a filter argument.
  - On Linux it adds optional OpenSSL references through `libcrypto.so.3`.
  - `-p:BoduCryptoBaseline=1.0.0` still builds it against the published package, with
    cases for APIs 1.0.0 lacks compiled out.
- Each workstream runs its rows before and after, with SIMD on and off, and records them
  in §10.

## 6. Validation

- The published vectors already in the suite, listed per workstream, pin the output.
  CI runs them on `net8.0` and `net10.0` under `test.runsettings`. Locally both runtimes
  are installed, and `DOTNET_ROLL_FORWARD` is left unset.
- Each replaced routine keeps its old implementation in the test project as an oracle,
  in a differential test over seeded random inputs.
- Each vector kernel is swept against scalar. `Bodu.Security.Cryptography.Simd.Test`
  replays the vectors with SIMD off, and the ARM64 job runs everything natively.
- Defect-driven changes (W0) are test first: the failing commit precedes the fix.

## 7. Compatibility and version

- Output is unchanged everywhere except two conformance fixes. Each breaks stored data,
  and the release notes must say so:
  - AES-256-GCM-SIV now derives a 256-bit message key, so data sealed under a 256-bit
    key by earlier releases does not decrypt;
  - AES-SIV now pads an empty plaintext in S2V, so data sealed with an empty plaintext by
    earlier releases does not decrypt. Non-empty plaintexts are unchanged.
- The public API changes only by addition.
- The work ships with the next lock-step `BoduBaseVersion` bump, as Argon2 does.

## 8. Risks

| Risk | Mitigation |
|---|---|
| Scope: ten workstreams | Each is self-contained and shippable. The order puts measured value first, and the plan can stop after any of them. |
| A vector path leaks timing | Kernels use constant shuffles only; no secret-indexed loads or secret-dependent branches. Per-bit lookups (Serpent) and the XTS branch go away. The review checklist is SECURITY-CHECKLIST §7. |
| .NET 8 and .NET 10 generate different code | Both legs are measured; the targets apply to .NET 10, and .NET 8 must not regress. |
| ARM64 is unmeasured here | The ARM64 job proves correctness. Performance is measured on hardware before release, as Argon2's is. |
| scrypt threads multiply memory | The bound defaults to 1, and the memory formula is documented. |
| The BCL's per-call ECB overhead | Batch size and API are chosen by measurement (W2). |
| GCM-SIV's AES-256 change breaks stored data | Release notes. AES-128 is unchanged. |

## 9. Out of scope

- A managed AES round function (AES-NI, ARMv8 AES): the BCL already reaches the
  hardware, and W2 removes the overhead around it.
- New algorithms, API redesigns, or changing a primitive's output.
- Tiger, Snefru, SipHash, Ascon and HC-128: none showed a gap worth a workstream.

## 10. Results

Filled in as each workstream lands. Measured with the crypto harness
(`Bodu.Security.Cryptography.Benchmarks --crypto-harness`) on the §1 machine: 4 vCPUs,
x64 with AVX-512, .NET 10 unless noted, the median of five rounds. Run-to-run noise on
this machine is about ±5-10%.

### W0 - correctness and hygiene (done)

- AES-256-GCM-SIV derives a 256-bit message-encryption key, as RFC 8452 §4 specifies.
  The C.2 and C.3 vectors are pinned, and the test that proved the bug was committed
  first. Output made by earlier AES-256-GCM-SIV versions does not decrypt (§7).
- The coverage script and the docs name both runtimes' AVX-512 switches:
  `DOTNET_EnableAVX512F` on .NET 8 and `DOTNET_EnableAVX512` on .NET 10.
- XTS doubles its tweak with a mask instead of a branch. Fifteen IEEE 1619-2007
  whole-block vectors are pinned; the previous vectors never doubled the tweak.
- Threefish-512's kernel is gated on AVX-512VL, which it uses.

### W1 - Keccak (done)

| Measure | Baseline | Result | Target |
|---|---|---|---|
| SHAKE128, 1 MiB | 35.2 MiB/s | 263.1 MiB/s | ≥ 200 MiB/s - met |
| SHAKE256, 1 MiB | 29.6 MiB/s | 178.0 MiB/s | - |
| SHAKE128, 64-byte input | 4.59 µs | 0.70 µs (BCL: 1.21 µs) | - |
| ML-KEM-768 keygen / encaps / decaps | 289 / 306 / 326 µs | 103 / 138 / 139 µs | ≤ 150 / ≤ 180 µs - met |
| ML-DSA-65 keygen / sign / verify | 1.14 / 3.31 / 1.04 ms | 0.33 / 2.24 / 0.30 ms | verify ≤ 0.5 ms - met |

On .NET 8: SHAKE128 goes from 36.4 to 269.9 MiB/s, ML-KEM-768 encaps / decaps from
290 / 416 to 114 / 162 µs, and ML-DSA-65 verify from 1.01 to 0.33 ms.

### W2 - AES modes, GHASH and POLYVAL (done)

| Measure | Baseline | Result | Target |
|---|---|---|---|
| AES-128-CTR, 1 MiB | 54.8 MiB/s | 1,665.8 MiB/s | ≥ 1 GiB/s - met |
| AES-128-GCM, 1 MiB | 54.9 MiB/s | 1,095.2 MiB/s, 240 B allocated | ≥ 800 MiB/s - met |
| AES-128-GCM, 64 bytes | 1.81 µs | 1.04 µs | - |
| AES-128-GCM, scalar GHASH kernel | 4.1 MiB/s | 208.9 MiB/s | ≥ 40 MiB/s - met |
| AES-128-GCM-SIV, 1 MiB | 38.8 MiB/s, 1,496 B | 1,154.5 MiB/s, 848 B | ≥ 5× - met (30×) |
| AES-128-GCM-SIV, 64 bytes | 10.47 µs | 5.09 µs | - |
| Threefish-512-CTR / Twofish-CTR | 244 / 83.7 MiB/s | 338 / 89.4 MiB/s | - |

How it was done, and where it departs from the design above:

- **Counter modes** lay out a 4 KiB run of counter blocks and encrypt it with one
  `EncryptBlocks` call (`CounterKeystream`). A run stops after the block the per-block
  loop would have stopped at, so a counter-wrap exception leaves exactly the output the
  old loop left. New tests pin CTR to a block-at-a-time reference, GCM to the platform's
  `AesGcm`, and GCM-SIV to a block-at-a-time reference built from RFC 8452's own
  definition of POLYVAL.
- **`AesBlockCipher.EncryptBlocks`** was measured both ways, in MiB/s:

  | Run | One-shot `EncryptEcb` | Cached transform |
  |---|---|---|
  | 256 B | 120 | 732 |
  | 1 KiB | 446 | 2,046 |
  | 4 KiB | 1,075 | 3,839 |
  | 16 KiB | 2,891 | 4,253 |
  | 64 KiB | 3,575 | 4,383 |
  | 1 MiB | 4,177 | 4,555 |

  The one-shot call rebuilds a platform cipher context on every call, about 2 µs.
  Runs go through the cached transform up to 64 KiB, in 4 KiB chunks through scratch
  rented from the array pool for the call. From 64 KiB the one-shot call is used,
  because it avoids the chunk copies. The decryptor is now created on first use, which
  saves 1.8 µs for every cipher that only encrypts - GCM-SIV creates one per message.
- **GHASH and POLYVAL** live in a new internal `Ghash` module instead of a multi-block
  update on `GaloisField128`:
  - A key is prepared once as `Ghash.Key`, holding H to H⁴ for the carry-less kernels.
  - One kernel serves both functions, written against the `IClmulIsa` shim with
    PCLMULQDQ and PMULL implementations.
  - The scalar kernel is BearSSL's `ghash_ctmul64`, which builds 64-bit carry-less
    products from integer multiplications with masked carries: 50 times the old
    byte-wise loop.
  - `GaloisField128` keeps only `Double`. Its multiply moved into the tests as the
    bit-serial reference.
- **GCM-SIV** folds the associated data into the POLYVAL state as it arrives, and
  derives both keys in one multi-block call.
- **ARM64.** Under qemu ARM64 emulation the PMULL shim's operations, the GHASH suite, and
  the GCM and GCM-SIV suites pass; that check is functional only, not timed. The ARM64 CI
  job runs on master pushes and pull requests, not on this branch.
- **The remaining GCM gap.** The platform's `AesGcm` runs at 4.5 GiB/s because it
  interleaves AES-NI and PCLMULQDQ in one pass. This library makes two passes and moves
  the counter blocks through `ICryptoTransform` copies. Closing the gap needs a managed
  AES round, which §9 rules out.

#### W2d - the remaining AES modes

| Measure (AES-128, 1 MiB) | Baseline | Result | Target |
|---|---|---|---|
| EAX | 27.4 MiB/s, 3.67 MB and 15 gen2 per message | 603.1 MiB/s, 112 B | ≥ 5× - met (22×) |
| SIV | 26.0 MiB/s, 3.67 MB and 14 gen2 per message | 611.1 MiB/s, 72 B | ≥ 5× - met (23×) |
| CCM | 26.6 MiB/s | 521.5 MiB/s | ≥ 5× - met (20×) |
| OCB | 49.7 MiB/s, 2,160 B | 1,721.0 MiB/s, 664 B | ≥ 5× - met (35×) |
| XTS | 59.7 MiB/s | 1,384.4 MiB/s | ≥ 5× - met (23×) |
| CBC encryption | 59.5 MiB/s | 966.0 MiB/s | - |
| CBC decryption | 58.2 MiB/s | 2,241.4 MiB/s | - |
| CFB decryption | 57.5 MiB/s | 2,149.2 MiB/s | - |

- **CBC chaining.** CBC encryption and the CBC-MACs inside CMAC and CCM are sequential,
  so `EncryptBlocks` cannot batch them. Chained a block at a time they top out near
  65 MiB/s on AES. The platform chains CBC at about 1.1 GiB/s:

  | Chain | One-shot `EncryptCbc` | Cached transform, reset per call | Per block |
  |---|---|---|---|
  | 64 B | 24 MiB/s | 52 MiB/s | 33 MiB/s |
  | 256 B | 130 MiB/s | 211 MiB/s | 62 MiB/s |
  | 4 KiB | 766 MiB/s | 920 MiB/s | 64 MiB/s |
  | 1 MiB | 1,105 MiB/s | 1,163 MiB/s | 65 MiB/s |

  A new internal `ICbcBlockCipher`, which `AesBlockCipher` implements explicitly, runs a
  chain through a cached zero-IV CBC transform: the caller's chaining value is folded
  into the first block, and the transform is reset after each chain. `CbcChain` uses it
  from six blocks up and chains single blocks otherwise and for every other cipher. The
  public surface is unchanged.
- **CMAC.** A new incremental `Cmac` holds back the last block until it knows whether it
  is the last, and folds the rest through `CbcChain` in whole runs. EAX's
  `OMAC(t, M) = CMAC([t] || M)` and SIV's `Sn xorend D` therefore need no copies of the
  message. The subkeys are derived once per message instead of once per CMAC.
- **Batched modes.** EAX and SIV share `CounterKeystream.TransformBigEndian128`, and CCM
  lays out its 24-bit counter a run at a time. XTS computes a run's tweaks first, OCB a
  run's offsets, and CBC and CFB decryption copy a run's chaining values first. Each then
  makes one multi-block call between two XORs. OCB's 34 key-dependent blocks now sit in
  one table.

Correctness work found on the way, each fixed test-first:

- **SIV empty plaintext.** S2V XORed in `<one>`, the constant reserved for a call with
  no strings, instead of padding the empty final string. Project Wycheproof's vectors
  exposed this, and they are now pinned for SIV, EAX, CCM and GCM-SIV. SIV treats empty
  associated data as no string, a convention its interface forces and the AEAD guide now
  documents; Wycheproof's empty-associated-data rows assume the other convention and are
  excluded.
- **In-place decryption.** CFB, and the CBC helper inside CTS, copied each ciphertext
  block into the chaining value only after writing the plaintext over it, so decrypting
  in place corrupted every block after the first.
- **Long-message coverage.** Every mode is now cross-checked on messages of up to
  20,000 bytes against the platform (`AesGcm`, `AesCcm`, CBC, CFB) or against a
  block-at-a-time reference built from its specification on the platform's AES.

### W3 - scrypt (done)

| Measure | Baseline | Result | Target |
|---|---|---|---|
| N=2¹⁴ r=8 p=1 | 104.3 ms (OpenSSL 51.9 ms) | 32.9 ms | ≤ 50 ms - met |
| Allocation per call, N=2¹⁴ r=8 p=1 | 16.8 MB, 10 gen2 per round | 83 B, no gen2 | ≤ 4 KiB, no gen2 - met |
| N=2¹⁴ r=8 p=4, bound 4 | 374.3 ms (no bound existed) | 41.5 ms | ≤ 120 ms - met |
| N=2¹⁴ r=8 p=4, one thread | 374.3 ms (OpenSSL 204.5 ms) | 122.1 ms | - |
| N=2¹⁷ r=8 p=1 | 800.0 ms, 134 MB (OpenSSL 502.2 ms) | 270.9 ms, 80 B | - |
| N=2¹⁴ r=8 p=1, scalar kernel | - | 52.0 ms | - |

The baseline was measured at the start of the workstream; §1 recorded 112 ms for the
same cost. Each step, at N=2¹⁴ r=8 p=1: the loop rewrite took it to 65 ms, native `V` to
51 ms, and the vector kernel to 33 ms. On .NET 8, N=2¹⁴ r=8 p=1 takes 35.9 ms, p=4 with
bound 4 takes 42.0 ms, and N=2¹⁷ takes 306 ms.

How it was done, and where it departs from the design above:

- **T1, the loops.** Salsa20/8 runs on sixteen locals. BlockMix computes each result in
  its place in the output, and its second form folds `X xor V[j]` into its reads, so
  the XOR is never written out. ROMix writes each link of the chain straight into `V`.
  On one core this alone matches OpenSSL.
- **T2, memory.** `Argon2MatrixPool` is now `NativeBufferPool`. Argon2's behavior is
  unchanged, and its `DisableMatrixReuse` switch keeps its name and now turns retention
  off for every renter. scrypt rents `V` and its scratch as one `ScryptCore.Workspace`,
  which clears every word it spanned before the buffer goes back to the pool. `B` is
  rented from the array pool and read as words in place.
- **T3, threads.** `Scrypt(ScryptParameters, int)` and `Scrypt(int, int, int, int)` take
  the bound, `MaxDegreeOfParallelism` reports it, and a `Verify` overload takes it too.
  Threads claim units from a shared counter and rent a workspace only once they hold a
  unit. Two rules were added to the design:
  - Units under 1 MiB of `V` stay on the calling thread.
  - The threads are capped so their `V`s together stay within the 2 GiB ceiling on one
    `V`. An untrusted encoded hash therefore cannot multiply what a verification
    allocates, whatever the bound.
- **T4, the kernel.** ROMix is generic over `IScryptKernel`, a scalar kernel and one
  `Vector128Kernel<TIsa>`. Its `Sse2Isa` and `AdvSimdIsa` shims supply only the three
  lane rotations (`PSHUFD` and `EXT`). The words are permuted into the diagonal order
  once per ROMix unit, not once per BlockMix call as §4 proposed; `V` holds blocks in
  that order. Word 0 stays first, so the `Integerify` read is the same in both orders.
  AVX-512's `VPROLD` for the four rotations was tried: 9% faster at N=2¹⁴, nothing at
  N=2¹⁷, where memory dominates. It was not worth another kernel.
- **Tests.** RFC 7914's intermediate vectors (§8 Salsa20/8, §9 BlockMix, §10 ROMix)
  now run through every kernel, beside §12's end-to-end vectors. An 80-row corpus
  generated with OpenSSL's `EVP_PBE_scrypt` replaces the recorded corpus §4 planned: it
  is an independent oracle rather than the old code's own output, and the old code
  matched every row. Also added:
  - a kernel sweep against the scalar kernel on seeded units;
  - a thread sweep over the corpus rows with p > 1 at bounds 2, 3, 4 and −1;
  - ROMix over a garbage-filled `V`;
  - the workspace's round trip through the pool.
- **ARM64.** Under qemu ARM64 emulation the AdvSimd shim, every per-kernel vector and
  sweep, and the threaded derivations pass. Long, allocation-heavy runs crash inside
  `libcoreclr` under the emulator. The pre-W3 code crashes the same way on the same
  corpus, so the emulator is at fault; the ARM64 CI job runs the suite on real hardware.

### W4 - BLAKE2b and BLAKE2s (done)

| Measure (1 MiB) | Baseline | Result | Target |
|---|---|---|---|
| BLAKE2b, scalar kernel | 150.6 MiB/s | 512.3 MiB/s | ≥ 450 MiB/s - met |
| BLAKE2b, AVX2 host | 151.5 MiB/s (scalar code) | 693.1 MiB/s | ≥ 650 MiB/s - met |
| BLAKE2b, AVX-512 | 669.0 MiB/s (OpenSSL 664.5) | 830.5 MiB/s | - |
| BLAKE2b, SSSE3 only | 151.5 MiB/s | 571.2 MiB/s | - |
| BLAKE2s, scalar kernel | 96.7 MiB/s | 316.3 MiB/s | ≥ 300 MiB/s - met |
| BLAKE2s, SSSE3 / AVX2 host | 94.4 MiB/s | 411.2 MiB/s | - |
| BLAKE2s, AVX-512 | 518.9 MiB/s (OpenSSL 418.8) | 522.9 MiB/s | - |
| 64-byte message, BLAKE2b / BLAKE2s, scalar | 0.94 / 0.75 µs | 0.35 / 0.29 µs | - |

§1's BLAKE2s baseline of 78 MiB/s came from an earlier run; the table uses this
workstream's own measurements. On .NET 8: BLAKE2b runs at 867 MiB/s with AVX-512,
678 MiB/s on AVX2 and 508 MiB/s scalar; BLAKE2s at 503, 431 and 312 MiB/s.

How it was done, and where it departs from the design above:

- **T1, the scalar kernels.** The compression functions moved into two internal cores,
  `Blake2bCore` and `Blake2sCore`. Their scalar kernels write out every round with σ
  resolved to constant indices; each `G` reads its message words straight from the
  block, and the sixteen working words stay in locals. `Argon2Blake2b` drives
  `Blake2bCore` instead of keeping a second copy of the scalar code.
- **T4, BLAKE2b.** One `Vector256Kernel<TIsa>` replaces the AVX-512 kernel and adds the
  AVX2 tier. Its shim holds only the four rotations of `G`: VPRORQ on AVX-512VL; on
  AVX2, VPSHUFD, VPSHUFB and an add-and-shift, as Argon2 does. Immediate rotations in
  place of variable ones made the AVX-512 path 24% faster. A
  `Vector128Kernel<TIsa>` over Argon2's `IVector128Isa` shims serves SSSE3-only x64 and
  ARM64, splicing row halves with `UpperThenLower`.
- **T4, BLAKE2s.** Its whole state fits four 128-bit rows, so one
  `Vector128Kernel<TIsa>` serves all three tiers. The shim holds the four rotations and
  three lane rotations: VPRORD and PSHUFD on AVX-512VL; PSHUFB, shifts and PSHUFD on
  SSSE3; REV32, TBL, SLI and EXT on ARM64. AVX2 adds nothing to a single BLAKE2s
  state, so AVX2 hosts take the SSSE3 kernel, VEX-encoded.
- **The JIT's inlining budget.** Written out in full, the vector kernels outgrew it:
  the 128-bit ones on .NET 10, and BLAKE2b's 256-bit one on .NET 8, whose budget is
  smaller. The last rounds' `G`, `Round` and `Load` calls stayed calls, the rows spilled
  to memory, and the kernels ran below scalar speed (178, 138 and 224 MiB/s). All three
  now loop over their rounds and read σ from a table, at no measurable cost. The scalar
  kernels stay written out, and the disassembly on both runtimes confirms they inline
  fully.
- **Tests.** Both cores have a minimal BLAKE2 built on the compression function alone,
  so every kernel is driven explicitly, whichever one dispatch picks:
  - RFC 7693's worked examples and every entry of the official blake2-kat.json;
  - a sweep against the scalar kernel over random states, blocks, counters and
    finalization flags;
  - each shim operation checked against its definition.
- **ARM64.** Under qemu ARM64 emulation both AdvSimd kernels pass the RFC examples, the
  blake2-kat.json vectors and the shim tests. The BLAKE2s sweep's
  `CollectionAssert.AreEqual` faults inside MSTest under the emulator. The same sweep
  checked with a span comparison matches the scalar kernel on all 1,000 samples.

### W5 - BLAKE3 (done)

| Measure (1 MiB, one thread) | Baseline | Result | Target |
|---|---|---|---|
| AVX2 host | 86.8 MiB/s (scalar code) | 1,409-1,557 MiB/s | ≥ 1 GiB/s - met |
| AVX-512, eight-way (this host's default) | 280.8 MiB/s | 2,699 MiB/s | - |
| AVX-512, sixteen-way (`DOTNET_PreferredVectorBitWidth=512`) | 280.8 MiB/s | 3,441 MiB/s | - |
| SSSE3 only | 86.8 MiB/s | 885 MiB/s | - |
| Scalar | 86.8 MiB/s | 382 MiB/s | - |
| 64-byte message, AVX-512 / scalar | 0.35 / 0.86 µs | 0.23 / 0.28 µs | - |

| Measure, all four cores (`maxDegreeOfParallelism: -1`) | One thread | Four threads |
|---|---|---|
| 1 MiB, AVX-512 eight-way | 2,699 MiB/s | 7,701 MiB/s |
| 16 MiB, AVX-512 eight-way | 2,145 MiB/s | 9,231 MiB/s |
| 16 MiB, AVX-512 sixteen-way | 2,437 MiB/s | 11,812 MiB/s |
| 16 MiB, AVX2 | 1,237 MiB/s | 4,570 MiB/s |
| 16 MiB, scalar | 404 MiB/s | 1,449 MiB/s |

§1's baseline of 82 MiB/s came from an earlier run; the table uses this workstream's
own measurements. On .NET 8 one thread runs at 2,571 MiB/s with AVX-512 (3,608 MiB/s
sixteen-way), 1,399-1,498 MiB/s on AVX2, 784 MiB/s on SSSE3 and 401 MiB/s scalar. A
hash still allocates only its 32-byte digest on one thread; on several, each write
that divides allocates about 2.4 KB, most of it `Parallel.For`'s own.

How it was done, and where it departs from the design above:

- **T1, the one-block kernels.** The compression function moved into an internal
  `Blake3Core`. Its scalar kernel writes out the seven rounds with the schedule resolved
  to constant indices, and a `Vector128Kernel<TIsa>` runs one block over the BLAKE2s
  shims - BLAKE3's `G` is BLAKE2s's, rotations included - on AVX-512VL, SSSE3 (also on
  AVX2 hosts) and AdvSimd. Blake3 reads blocks straight from the caller's span, and
  builds each parent block on the stack, clearing it after the compression. This alone
  took one thread from 87 to 469 MiB/s on AVX2 and 360 MiB/s scalar.
- **T4, many chunks at once.** Kernels that give each lane its own input compress
  whole chunks, and whole levels of parents, many at a time:
  - eight-way over `Vector256`, with AVX2's byte shuffles or AVX-512VL's `VPRORD`, and
    message words in and chaining values out through 8×8 transposes;
  - four-way over `Vector128` on the BLAKE2s shims, which gained a 4×4 transpose;
  - sixteen-way over `Vector512`, whose 16×16 transpose ends in two rounds of
    `VSHUFI32X4`.
  `CompressSubtree` compresses up to 64 chunks per batch and reduces their chaining
  values level by level in place, so the only copy of state it keeps is 2 KiB on the
  stack, cleared on return. `Blake3.HashCore` brings the stream to a chunk boundary,
  then hashes the largest aligned power-of-two runs of whole chunks as subtrees. It
  leaves at least one byte behind, so the final block stays deferred and the root is
  never compressed early; each subtree's chaining value joins the stack one level up
  per doubling.
- **The runtime's 512-bit preference.** The sixteen-way kernel beats the eight-way one
  by 27-40% on this Cascade Lake host, but the runtime clears
  `Vector512.IsHardwareAccelerated` here, because sustained 512-bit work lowers the
  clock. It therefore runs as its own kind, `Avx512Wide`, selected only where the
  runtime prefers 512-bit vectors; elsewhere AVX-512 hosts keep the eight-way kernel.
  The tests drive it explicitly wherever AVX-512 runs, and the coverage matrix now
  classifies `*.Vector512.cs` with the AVX-512 files.
- **T3, threads.** `Blake3(int maxDegreeOfParallelism)` and `MaxDegreeOfParallelism`
  are the one API addition G5 anticipated. The default is `1`. The hasher plans the
  aligned subtrees of each write, and `Blake3Core.CompressSubtrees` computes them all in
  one parallel job, as scrypt's units are: 64-chunk parts are claimed from a shared
  counter by the calling thread and the workers, and each subtree's parts are joined
  on the calling thread. Writes with fewer than 256 whole chunks stay on the calling
  thread. The parallel branch is a method of its own, because the compiler allocates
  a lambda's captures on entry to the method that declares them, and that had added
  80 bytes to every single-threaded hash.
- **Tests.** The core has a minimal specification-shaped BLAKE3 of its own, so
  every kernel is driven explicitly:
  - the official test_vectors.json in all three modes, keyed hash and key
    derivation included, which `Blake3` does not expose;
  - a sweep of each kernel against the scalar one;
  - the many-input kernels against chunk-by-chunk and parent-by-parent compression,
    over every lane remainder up to 40 and counters whose low word wraps, with parents
    reduced in place;
  - subtrees of 1 to 256 chunks against the specification's tree;
  - the parallel plans against the calling thread's, at every thread bound.

  On the public class, large, ragged and threaded writes are held to small-write
  streaming, which never takes the subtree path. Around subtree and part
  boundaries, every bound's digest is held to the calling thread's.
- **ARM64.** Under qemu the AdvSimd transpose and rotations pass, as do all three
  modes of the official vectors through the AdvSimd kernel and the parallel subtree
  plans. The emulator again faults inside MSTest's `CollectionAssert`, so the
  sweeps were re-checked with a span comparison, and all of them match the scalar
  kernel:
  - 1,000 compressions;
  - every chunk and parent count up to 40, parents in place too;
  - subtrees of 1 to 256 chunks.
- **Found along the way, and fixed.** On .NET 8, BLAKE2b's 128-bit kernel (SSSE3 hosts,
  and the AdvSimd kernel on ARM64) ran at 181 MiB/s, below its own scalar kernel's 507;
  W4 had measured that tier on .NET 10 only, where it runs at 582. Dynamic PGO was
  inlining the hot kernel into `Blake2bCore.Compress` and running out of inlining
  budget inside it, leaving the kernel's `G`, message loads and `Undiagonalize` as
  calls; with `TieredPGO=0` it ran at 554-609 MiB/s. Every BLAKE kernel entry point
  now forbids inlining, so each is compiled on its own whatever the profile: the
  kernel runs at 580 MiB/s on .NET 8, and no other tier moved beyond noise. A
  reflection test per core pins the attribute, committed red before the fix.
  - The same sweep showed AES-GCM 12-20% slower on .NET 8 with PGO than without. The
    GHASH kernel compiles the same either way; the difference lies in how PGO lays out
    and devirtualizes the BCL's OpenSSL AES path under `AesBlockCipher.TransformBlocks`.
    It is not pursued here.

### W6 - ChaCha20, Salsa20, Poly1305 and the Poly1305 AEADs (done)

| Measure (1 MiB unless noted) | Baseline | Result | Target |
|---|---|---|---|
| ChaCha20, AVX2 | 286 MiB/s | 1,163-1,446 MiB/s | ≥ 1 GiB/s - met |
| ChaCha20, AVX-512 eight-way (this host's default) | 286 MiB/s | 3,103 MiB/s | - |
| ChaCha20, AVX-512 sixteen-way (`DOTNET_PreferredVectorBitWidth=512`) | 286 MiB/s | 3,836 MiB/s | - |
| ChaCha20, SSSE3 / scalar | 299 / 295 MiB/s | 774 / 307 MiB/s | - |
| Salsa20, AVX-512 eight-way / sixteen-way | 356 MiB/s | 3,155 / 3,858 MiB/s | - |
| Salsa20, AVX2 / SSSE3 / scalar | 353 / 346 / 351 MiB/s | 729-1,205 / 608 / 398 MiB/s | - |
| XChaCha20-Poly1305 | 141 MiB/s | 761 MiB/s; 561-585 MiB/s on AVX2 | ≥ 500 MiB/s - met |
| XChaCha20-Poly1305, allocation per message | 624 B | 80 B, the instance itself; `Encrypt` / `Decrypt` 0 B | nothing beyond the output - met, see below |
| XChaCha20-Poly1305, 64-byte message | 1.40 µs | 0.82-0.86 µs | - |
| XSalsa20-Poly1305 (secretbox) | 150 MiB/s; 656 B | 689 MiB/s; 80 B | - |
| Poly1305 | 441 MiB/s | 1,110-1,131 MiB/s | ≥ 1 GiB/s - met |
| Poly1305 without BMI2 / scalar | 446 / 438 MiB/s | 600-720 / 618-687 MiB/s | - |

The baselines were measured the same day at `e63fe20`, the commit before W6, whose
stream ciphers, Poly1305 and AEADs are 1.0.0's; they agree with §1. On .NET 8 the
results match within noise: ChaCha20 3,130 MiB/s, Salsa20 3,317 MiB/s,
XChaCha20-Poly1305 807 MiB/s and Poly1305 1,110 MiB/s. The AVX2 tier moved between
processes on this shared host, by up to a quarter for ChaCha20 and more for Salsa20, so
it is given as a range over both runtimes. The sixteen-way figures need
`DOTNET_PreferredVectorBitWidth=512`, because this host does not prefer 512-bit vectors
by default (see W5).

How it was done, and where it departs from the design above:

- **T1, Poly1305 (W6a).** `Poly1305Core` is a struct holding the whole authenticator.
  - `r` is split into limbs of 44, 44 and 42 bits, with 20·r1 and 20·r2 precomputed.
    Blocks are read straight from the caller's span.
  - Each of a block's nine products is split at bit 44 as it is formed, so the limb sums
    stay in 64 bits with no 128-bit carries. The design's `UInt128` sums measured about
    8% slower: .NET routes the low half of a `UInt128` product through memory, and every
    `UInt128` add compiles to add, cmp, setb and movzx.
  - The high half of each product comes from `mulx` on x64 with BMI2 and `umulh` on
    ARM64. Without either, `Math.BigMul` is a call into the BCL's software fallback for
    every product. That path fell to 316-345 MiB/s, below the old 26-bit code, and was
    found while measuring this section (see below).
  - The public `Poly1305` feeds the core from `HashCore`; the AEADs keep one on the
    stack.
- **T4, keystream in bulk (W6b).** `ChaCha20Core` and `Salsa20Core` own the block
  functions: a scalar one, and kernels that keep one state word of 4, 8 or 16
  consecutive blocks in each vector, with the counters in successive lanes.
  - `Vector128Kernel<TIsa>` runs over SSSE3, AdvSimd and AVX-512VL, and
    `Vector256Kernel<TIsa>` over AVX2 and AVX-512VL. `Vector512Kernel` is BLAKE3's
    `Avx512Wide` kind, selected only where the runtime prefers 512-bit vectors.
  - Rotations by 16 and 8 bits are byte shuffles (`rev32` and `tbl` on ARM64) and the
    rest shift pairs; AVX-512 has `vprold`.
  - The words go back into block order through 4×4 transposes, `vperm2i128` for eight
    blocks, and `vshufi32x4` rounds for sixteen.
  - Salsa20 shares the kernel kinds, shims and transposes. Its 64-bit counter carries
    into the high word lane by lane. The scalar quarter rounds use Bodu.Core's internal
    `RotateBitsLeftUnchecked`.
  - Since 1.1.0 the shims are gone: the kernels rotate through Bodu.Core's internal
    `VectorExtensions.RotateBitsLeftUnchecked<TIsa>`, whose `TIsa` is one of its
    `VectorRotation` structs, and share one 4×4 transpose, `ChaCha20Core.Transpose`. The
    kernels' machine code is unchanged.
  - The engines expose the kernels through the internal
    `IBulkStreamCipher.XorKeystreamBlocks`, which `StreamCipherTransform` and the AEADs
    call for whole blocks. A request past the end of the keystream writes the blocks
    that remain, latches exhaustion and throws, as the same run of single-block calls
    would.
- **Allocation-free messages (W6c).** The `Poly1305AeadCore` framings are generic over
  an internal `IKeystreamSource` passed by reference.
  - The sealed AEADs seed a `ChaCha20Core.Keystream` or `Salsa20Core.Keystream` on the
    stack for each message and clear it afterwards. These values hold the state inline
    and draw on the same block functions and kernels as the engines.
  - `Poly1305AeadTransform` keeps its key and nonce in an inline buffer.
  - `Encrypt` and `Decrypt` go through `private protected` hooks. Their default is the
    old path: `CreateEngine`, then `SealCore` / `OpenCore`. A type derived in another
    assembly therefore behaves as before, and the public API is unchanged.
  - A message now allocates nothing but the single-use instance the API requires:
    80 bytes, where it was 624. Making the transforms reusable across nonces would
    remove that too, but it is an API change outside G5, so it is left for a later
    version.
- **Tests.**
  - Every kernel, driven explicitly, is held to the block function one block at a
    time: at every block count from 0 to 40 plus 47, 48, 49, 64 and 100, in place,
    across ChaCha20's counter wrap and Salsa20's carry and wrap, and through each shim's
    rotations and transposes.
  - The block functions are pinned to RFC 8439 2.3.2 and A.1 and to the eSTREAM
    Salsa20 vectors. The RFC 8439 reader now parses A.1's "Block Counter" spelling.
  - `StreamCipherTransform` is checked at split points around block boundaries and
    against an engine with no bulk path.
  - The Poly1305 core is compared with the replaced radix-2^26 code over seeded
    messages, piece-wise feeds, padded segments and limbs at their largest.
  - Each AEAD framing's stack path is held to its engine path over every length up to
    299, plus 1,024, 1,029 and 4,099 bytes.
  - `EngineBackedXChaCha20Poly1305` derives from `Poly1305AeadTransform` the way another
    assembly must, and runs the whole AEAD contract through the base engine path. A
    reflection test holds each sealed AEAD's protected `CreateEngine`, `SealCore` and
    `OpenCore` to `Encrypt` and `Decrypt`.
  - The contract asserts that `Encrypt` and `Decrypt` allocate 0 bytes, checked red
    first.
  - The SIMD-off assembly checks that dispatch falls to the scalar kernel, and now also
    runs the XChaCha draft and libsodium secretbox vectors.
- **ARM64.** Under qemu the AdvSimd rotations, transposes and kernels matched the scalar
  block function over every block count and counter case: 4,052 checks, confirmed with
  a span comparison because the emulator faults inside MSTest's `CollectionAssert`. The
  Poly1305 core's differential and edge tests pass on the `umulh` path.
- **Found along the way, and fixed.** On x64 without BMI2 - pre-Haswell processors, the
  Pentium and Celeron parts that disable VEX encodings, and
  `DOTNET_EnableHWIntrinsic=0` - the core's `Math.BigMul` fallback was nine calls per
  block. `Poly1305Core.SplitProduct` now forms the split inline from three 64-bit
  multiplies:
  - Write `left = a1·2^32 + a0` and `right = b1·2^32 + b0`; the product is
    `(a1·right + a0·b1)·2^32 + a0·b0`.
  - Accumulator limbs stay below 2^46 and limbs of r and 20·r below 2^49, so `a1·right`
    stays below 2^63.
  - That sum, plus the carry out of `a0·b0`, is therefore exactly the product's bits
    from 32 upward.

  Without BMI2 Poly1305 went from 316-345 to 600-720 MiB/s, and XChaCha20-Poly1305
  with neither AVX2 nor BMI2 from 250 to 366-372 MiB/s. The split is held to `UInt128`
  products at its bounds and over 100,000 seeded pairs.
- **Left for later.**
  - Poly1305 is now the AEADs' bottleneck: at 1 MiB the keystream takes about 0.33 ms
    and the MAC about 0.9 ms. OpenSSL and BoringSSL run Poly1305 several blocks at a
    time on 26-bit limbs in AVX2 or AVX-512 registers; that would roughly double the
    AEADs, but it is beyond W6's targets.
  - A 64-byte message costs three scalar block computations: HChaCha20, the Poly1305
    key block and the message block. Producing the key block and the first message
    blocks in one four-way kernel call would shorten it.
  - Salsa20 trails ChaCha20 on AVX2 because none of its rotations (7, 9, 13, 18) is
    byte-aligned, so each is a shift pair. Where AVX-512's `vprold` is available the
    two run at the same speed.

### W7 - Serpent (done)

| Measure (1 MiB unless noted) | Baseline | Result | Target |
|---|---|---|---|
| Serpent-128-CTR, scalar | 3.7 MiB/s | 73-76 MiB/s | ≥ 60 MiB/s - met |
| Serpent-128-CTR, AVX2 eight-way | 3.7 MiB/s | 396-419 MiB/s | ≥ 200 MiB/s - met |
| Serpent-128-CTR, AVX-512VL eight-way (this host's default) | 3.7 MiB/s | 563-579 MiB/s | - |
| Serpent-128-CTR, SSSE3 four-way | 3.7 MiB/s | 206-212 MiB/s | - |
| Serpent-128 `EncryptBlocks` / `DecryptBlocks`, AVX-512VL | - | 730 / 785 MiB/s | - |
| Serpent-128 `EncryptBlocks` / `DecryptBlocks`, AVX2 | - | 487 / 519 MiB/s | - |
| Serpent-128 `Encrypt`, one block per call | 3.9 MiB/s (3.96 µs) | 81 MiB/s (188 ns) | - |
| Serpent-256 / 512 / 1024 `Encrypt`, one block per call | 2.5 / 1.8 / 1.5 MiB/s | 17 / 12.5 / 10.5 MiB/s | - |

The CTR baseline is §1's and was re-measured at `e63fe20`, whose Serpent is 1.0.0's.
The `Encrypt` rows come from a scratch probe that times 20,000 calls on one block, and
the `EncryptBlocks` rows from one that times the explicit `IBlockCipher` members on
1 MiB; both take the best of five or more rounds.

On .NET 8 the scalar and SSSE3 tiers match within noise: 75 and 200 MiB/s. The vector
tiers run 10-20% slower:

- AVX-512 gives 476-484 MiB/s in CTR, and 596 / 639 MiB/s through `EncryptBlocks` /
  `DecryptBlocks`. .NET 10 folds chains of bitwise operations into AVX-512's
  three-input `vpternlogd`: the eight-way kernel has 71 of them against .NET 8's 11, and
  335 instructions per eight rounds against 377.
- AVX2 gives 334-360 MiB/s in CTR and 428 MiB/s through `EncryptBlocks`. Here the two
  runtimes emit nearly the same kernel, .NET 8's 528 instructions against .NET 10's
  522, so the gap lies in register allocation or scheduling; it was not pursued.

How it was done, and where it departs from the design above:

- **T1, the circuits (W7a).** `SerpentCore` holds Osvik's circuits for S0-S7 and their
  inverses, as Crypto++ publishes them in `serpentp.h`, which is in the public domain.
  - Each circuit has 14 to 19 Boolean operations, with no branches and no table reads.
    It leaves its outputs permuted across five registers. The permutations were derived
    by simulating every circuit on truth tables, and the same simulation checked every
    output bit against the S-box tables.
  - Serpent-128's 32 rounds run eight at a time, one per S-box, so each circuit is
    chosen at compile time. The key schedule and the wide-block variants call the
    circuits through an indexed dispatch; the index is a round number, never data.
  - The core works on plain `uint` words, and the rotations use Bodu.Core's
    `RotateBitsLeftUnchecked` / `RotateBitsRightUnchecked`, the prekey recurrence
    included. A version generic over wrapper structs was tried first. The JIT ran out of
    inlining budget and left 154 calls to the wrappers' XOR operator in every block,
    and about 80 to the other operators and rotations.
  - `EncryptBlock` and `DecryptBlock` are `NoInlining | AggressiveOptimization`, as the
    BLAKE kernels are. Without it dynamic PGO inlined them into their caller and ran
    out of budget there, leaving the circuits as calls: 33 MiB/s with PGO against 77
    without.
- **T4, blocks in bulk (W7b).** `Vector128Kernel<TIsa>` and `Vector256Kernel<TIsa>`
  process four or eight blocks at a time. They load the blocks, transpose them so each
  vector holds one word from every block, and run the circuits unchanged.
  - The kernels run over ChaCha20's instruction-set shims, SSSE3, AdvSimd, AVX2 and
    AVX-512VL, and share its 4×4 transposes. The eight-way kernel transposes two
    consecutive blocks per 256-bit load in lane.
  - They are generated per vector type rather than written generically, for the
    wrapper reason above and because .NET 8's `Vector128<T>` and `Vector256<T>` do not
    implement `IBitwiseOperators`. Their entry points are
    `NoInlining | AggressiveOptimization`, and a reflection test pins that.
  - `SerpentCore.EncryptBlocks` / `DecryptBlocks` pick the kernel once per process
    through the `SimdCapabilities` gates. A run goes through in groups of eight, then
    four, and the last few blocks go through the scalar rounds one at a time.
  - `Serpent128Cipher` re-implements `IBlockCipher.EncryptBlocks` / `DecryptBlocks`
    explicitly, so W2's batched modes reach the kernels. The public API is unchanged.
- **Side effect.** Serpent no longer reads tables at secret-dependent indices. The
  remarks on the Serpent types and the security-posture, choosing-a-primitive and
  Serpent guides now say so, and Serpent has moved out of the "data-dependent table
  lookups" group.
- **Tests.**
  - The existing NESSIE vectors and the wide-block variants' vectors still pass.
  - Each circuit is checked against its S-box table for every 4-bit input in every bit
    position. The linear transform and its inverse are checked against the replaced
    code.
  - The table-driven 1.0.0 cipher is kept in the tests as `SerpentReference`. Whole
    blocks are compared with it both ways, over seeded keys of every key size and
    seeded blocks.
  - Every supported kernel, driven explicitly, is held to the scalar path one block at a
    time: at every block count from 0 to 40 plus 47, 48, 49, 64 and 100, in place and
    out of place. The guards and the dispatch are covered, and the SIMD-off assembly
    checks that dispatch falls to the scalar path.
- **ARM64.** Under qemu the AdvSimd kernel matched the scalar path over every block
  count, both ways: 552 checks, confirmed with a span comparison because the emulator
  faults inside MSTest's `CollectionAssert`.
- **Left for later.**
  - The wide-block variants gained 7×, not Serpent-128's 20×. Every round sends each
    four-word group through the indexed dispatch as a call, and the state makes a round
    trip through a span between the key, S-box and linear passes. Unrolling their rounds
    eight at a time, as Serpent-128's are, would remove the dispatch; the cross-group
    word rotation and the tweak injection every fourth round stay.
  - CTR runs at about three quarters of `EncryptBlocks`, the rest going to the counter
    blocks and the XOR. That cost is W2's and applies to every block cipher.

### W8 - Curve25519 and Ed25519 (done)

| Measure | Baseline | Result | Target |
|---|---|---|---|
| X25519 shared secret | 208-239 µs | 90-94 µs | ≤ 150 µs - met |
| X25519 key generation | 204-232 µs | 49-51 µs | ≤ 70 µs - met |
| Ed25519 sign | 87-100 µs | 51-56 µs | ≤ 80 µs - met |
| Ed25519 verify | 363-413 µs | 136-150 µs | ≤ 200 µs - met |
| Ed25519 verify, allocation | 2,720 B | 136 B, the `IncrementalHash` | - |
| Without BMI2: X25519 shared secret / key generation | 367-450 / 371-435 µs | 234-259 / 105-122 µs | - |
| Without BMI2: Ed25519 sign / verify | 139-153 / 681-780 µs | 108-118 / 368-385 µs | - |

The baselines were re-measured the same day at `e63fe20`, whose curve code is 1.0.0's;
they sit a little below §1's 246 / 222 / 105 / 388 µs. Each range spans three runs on
each runtime: .NET 8 and .NET 10 now measure the same within noise. "Without BMI2" is
`DOTNET_EnableBMI2=0` on .NET 8 and `DOTNET_EnableAVX2=0` on .NET 10, which also
disables BMI2 there.

How it was done, and where it departs from the design above:

- **The field multiply (W8a).** The design assumed the multiply was sound and aimed at
  the operation counts around it. In fact the multiply was the main cost.
  - It summed its 25 limb products in `UInt128` and scaled the folded sums by 19 as
    128-bit values. The JIT left 8 calls to `UInt128`'s multiply operator and 6 to its
    addition operator in every multiply.
  - The folded factors are now scaled by 19 before multiplying, so every limb product is
    a single 64 × 64-bit multiplication. Each product is split at bit 51 as it is
    formed, as Poly1305's core splits at bit 44 (W6).
  - Each result limb is summed in two 64-bit parts, the products' low 51 bits and the
    products shifted right 51 bits. The operand bounds keep both below 2^64.
  - The high half comes from `mulx` with BMI2 and from `umulh` on ARM64. Without either,
    `Math.BigMul` would be a call per product, so `SplitProduct` forms the split from
    four 64-bit multiplies inline. A scratch comparison measured the `imul`-plus-`mulx`
    pair faster than `mulx` returning both halves through a pointer, and faster than
    `Math.BigMul`.
  - Squaring is now dedicated: each cross product is formed once from a doubled limb,
    15 limb products instead of 25.
  - The operands are passed by `in`-reference, which measured about 15% faster than by
    value. `Curve25519FieldElement` and `Ed25519Point` became `readonly` structs, so
    that passing them by reference cannot cost a defensive copy.
- **Doubling and key generation (W8b).**
  - `Ed25519Point.Double` delegated to the unified addition, nine multiplications. It is
    now dbl-2008-hwcd for a = −1, four squarings and four multiplications, and it never
    reads T. It is complete, because d is a non-square. It forms −F and −H rather than F
    and H, which negates all four coordinates and leaves the point unchanged.
  - X25519 key generation ran the 255-step ladder on u = 9. It now forms [s]B from
    Ed25519's fixed-base table, in constant time, and maps the result across with
    u = (Z + Y) / (Z − Y), one inversion.
  - The table scan had copied each 160-byte entry by value, 16 per window. It now
    selects the coordinates one field element at a time by reference, which took key
    generation from 54-70 to 46-57 µs in the runs made at the time.
- **Verification (W8c).**
  - The combination [S]B + [k](−A) is compared with R in projective coordinates, four
    multiplications, instead of by encoding both points, two inversions.
  - `IsSmallOrder` recognizes the identity as X = 0 and Y = Z instead of encoding [8]P.
    That removes a third inversion from every verification and from every public-key
    import.
  - The 16-entry table of multiples of the public point is on the stack.
- **Tests.**
  - `Multiply`, `Square`, `MultiplySmall` and `Reduce` are held to `BigInteger`
    arithmetic modulo p, with limbs at and around their bounds and over 10,000 seeded
    values each. The tests also check that results stay loosely reduced, and hold
    `Square` to `Multiply`.
  - `SplitProduct` is held to `UInt128` products at its bounds and over 100,000 seeded
    pairs. The curve tests also pass with BMI2 disabled, which runs it end to end.
  - The doubling is held to self-addition on seeded multiples and on all eight
    small-order points. A point added to a doubled point must give the same sum, which
    checks T. Corrupting T fails ten tests.
  - The map is pinned at the base point and the identity, and held to the ladder on
    seeded multiples. Fixed-base key generation is held to the ladder over 256 boundary
    and seeded scalars.
  - `AreEqual` is tested on one point reached along two routes, on a point and its
    negation (same y), and on the identity and the point of order 2 (same x). Dropping
    either coordinate from the comparison fails those tests.
  - The RFC 7748 and RFC 8032 vectors and the full Wycheproof Ed25519 set still pass.
    The field element, point and Curve25519 tests moved into member partials.
- **ARM64.** Under qemu the `umulh` path matched the replaced `UInt128` arithmetic on
  `Multiply`, `Square` and `MultiplySmall`, and matched the RFC 7748 vectors: 9,002
  checks with no mismatches. The emulator faulted while reading some tests' attributes,
  so the check ran as a temporary probe.
- **Left for later.**
  - **Fixed-base table layout.** Each of its 64 windows costs about 390 ns of addition
    and 220 ns of constant-time scan. Signed radix-16 digits and a table in cached form
    (Y + X, Y − X, 2Z, 2dT) would halve the scan and save a multiplication per addition:
    roughly a quarter off signing and key generation.
  - **Verification's doublings.** The 256 doublings are now about half of verification.
    Three of each window's four doublings feed another doubling and could skip T, saving
    a multiplication each. A width-5 NAF for the public point and a larger table of odd
    base multiples would cut the roughly 120 additions.
  - **Hash allocation.** Signing and verification each allocate an `IncrementalHash`,
    136 bytes. A short message could be hashed from a stack buffer instead.
  - **Register pressure.** The multiply has 14 inputs and 10 partial sums for 15 x64
    registers, which leaves about 130 stack references. Four 64-bit limbs would need
    add-with-carry, which .NET does not expose.

### W9 - ML-KEM and ML-DSA beyond Keccak (done)

| Measure | Baseline | Result | Target |
|---|---|---|---|
| ML-KEM-768 encapsulate / decapsulate | 109-125 / 144-155 µs | 41-51 / 60-80 µs | ≤ 60 / ≤ 80 µs - met |
| ML-KEM-768 key generation | 95-115 µs | 75-80 µs | - |
| ML-DSA-65 sign | 1.82-2.28 ms | 0.79-0.85 ms | ≤ 1 ms - met |
| ML-DSA-65 sign, allocation | about 119 KB | 3,336 B, the signature | ≤ 8 KiB - met |
| ML-DSA-65 verify | 305-391 µs, 54,904 B | 68-73 µs, nothing | - |
| ML-DSA-65 key generation | 322-354 µs | 302-328 µs | - |

The baselines were re-measured the same day at `e7c08bb`, the commit before W9, in runs
alternating with the results. Each range spans three runs on each runtime: .NET 8 and
.NET 10 measure the same within noise. On this machine a single harness row can move by
10-15% from one run to the next, so ranges rather than single figures are recorded.

How it was done, and where it departs from the design above:

- **ML-KEM arithmetic (W9a).**
  - Every butterfly, base-case product and sum reduced with `% Q`. A Reduction partial of
    `MLKemEngine` supplies the reductions FIPS 203 implementations use instead.
    - `MontgomeryReduce`, with R = 2^16, reduces the twiddle products. The twiddles are
      stored multiplied by 2^16, so the reduction gives the plain product.
    - `BarrettReduce` returns the centered representative of the inverse transform's
      sums.
    - `ReduceWide`, a Barrett reduction for values below 2^36, reduces each base-case
      output coefficient once, as a single sum of products.
  - The forward transform leaves its sums unreduced, since seven layers keep them within
    (−8q, 8q), and reduces once at the end.
  - The binomial sampler counted its PRF stream a bit at a time. It now counts a word at
    a time: adding a word to itself shifted right leaves each η-bit field holding the
    count of its own bits.
- **ML-KEM caching (W9b).**
  - Every operation re-derived H(ek), the matrix Â, and the vectors t̂ and ŝ decoded
    from 12-bit packing. `MLKemKeyMaterial` keeps them from when the key is set, as
    FIPS 203 permits.
  - Key generation hands over the matrix and both vectors, and the decapsulation key
    carries H(ek), so a generated key costs nothing extra to cache. An imported key
    derives them once.
  - The caches take about 8, 15 and 24 KiB for ML-KEM-512, 768 and 1024.
- **ML-DSA arithmetic and storage (W9c).**
  - `MLDsaEngine` gained the same kind of Reduction partial, with Montgomery R = 2^32,
    a `Reduce32` for sums below 2^31, and `Freeze`.
  - Of the two factors in each coefficient-wise product, the one fixed for the whole
    operation is held in Montgomery form, so every product needs a single reduction.
    That covers Â, ŝ₁, ŝ₂, t̂₀ and NTT(t₁·2ᵈ).
  - `Decompose` and `HighBits` divided by 2γ₂. They now use the reference
    implementation's multiply-and-shift quotients, so no secret coefficient reaches a
    divider, whose timing on some processors depends on its operands. The signed bit
    packing and `MakeHint` are branch-free.
  - Each operation built jagged arrays of polynomials, and signing cloned y on each
    rejection attempt. Key generation, signing and verification now each rent one
    flat workspace from `ArrayPool<int>`, carve their polynomials from it, and clear it
    on return. Signing allocates only the signature it returns.
- **ML-DSA caching (W9d).**
  - Every signature re-derived Â (about 118 µs for ML-DSA-65) and the 17 transforms of
    s₁, s₂ and t₀. Every verification re-derived Â, tr = H(pk) and the six transforms
    of t₁·2ᵈ. `MLDsaKeyMaterial` keeps them from when the key is set.
  - Key generation and private-key import hand over Â and NTT(s₁), which they compute
    anyway. The transform is linear, so NTT(t₁·2ᵈ) = Â ∘ ŝ₁ + ŝ₂ − t̂₀, formed from
    values already at hand rather than by k more transforms. Every value is brought to
    the representative the `Expand` methods produce, so the caches do not depend on how
    the key was set.
  - The caches take about 32, 53 and 87 KiB for ML-DSA-44, 65 and 87. Building them
    costs key generation about 70 µs, which W9c's arithmetic more than repays.
  - The inverse transform now takes coefficients below q, as the reference
    implementation's does. Only the matrix-vector sums need reducing first, so 17 of
    the 23 inverse transforms in each signing attempt skip that pass. The final scaling
    by 256⁻¹ folds into the last layer.
- **Tests.**
  - The replaced remainder-operator code moved into the tests as `MLKemReference` and
    `MLDsaReference`. The reductions are held to plain integer arithmetic at and
    around their input bounds, and the transforms, products and samplers to the
    references over edge and seeded polynomials.
  - `Decompose` is checked against the division-based reference over every
    coefficient in [0, q), for both values of γ₂, in the Regression tier. The inverse
    transform is checked with inputs that drive its last layer's sum and difference to
    256(q − 1).
  - Key generation's and import's hand-overs are held to what the `Expand` methods
    derive from the encoded keys, the high-order vector formed by linearity included.
  - The `KeyReuse` partials of both contracts hold many operations on one key, and key
    replacement, to the engines' uncached overloads. The `*KeyMaterialTests` check that
    every way of setting a key keeps the same values, and that `Clear` zeroes the
    secret ones.
  - The NIST ACVP and Wycheproof vectors pass unchanged through the cached paths.
- **Left for later (the design's T4).**
  - **A SIMD transform.** The scalar butterfly is 16 instructions around three dependent
    multiplications. A signing attempt runs 29 transforms, about 60% of its time. An
    AVX2 transform over eight 32-bit lanes would cut that several times over, as the
    reference implementation's AVX2 code does. The targets are met without it.
  - **A four-way Keccak.** Â, the mask vector y and the secret vectors are expanded
    from independent SHAKE streams, four or more at a time. A `Vector256<ulong>`
    permutation would run four streams at once.

### W10 - lower priority (done)

| Measure | Baseline | Result |
|---|---|---|
| CubeHash, 1 MiB, AVX-512 | 258 / 390 MiB/s | 382 / 387 MiB/s |
| CubeHash, 1 MiB, AVX-512 off (AVX2) | 24 / 21 MiB/s | 377 / 333 MiB/s |
| CubeHash, 1 MiB, AVX2 off (SSSE3) | 24 / 21 MiB/s | 276 / 227 MiB/s |
| CubeHash, 64-byte message, any vector kernel | 6.5-7.4 µs | 0.60-0.83 µs |
| Whirlpool, 1 MiB | 33.7 / 43.6 MiB/s | 73-80 MiB/s |
| Whirlpool, 64-byte message | 3.40 / 2.80 µs | 1.68-1.74 µs |

Figures are .NET 10 / .NET 8 where they differ. The CubeHash and Whirlpool baselines were
measured the same day at `ee89d80` and `5c7bf86`, the commits before each change. W10
had no targets.

How it was done:

- **CubeHash (W10a).**
  - Without AVX-512 CubeHash ran its rounds one word at a time, through a scratch buffer
    for each exchange. The round function moved into a new `CubeHashCore`, whose kernels
    hold the whole 1024-bit state in registers for a run of rounds: two 512-bit
    registers under AVX-512F, four 256-bit under AVX2, or eight 128-bit over ChaCha20's
    SSSE3 and AdvSimd rotation shims.
  - A round's exchanges pair words 8, 4, 2 and 1 apart. In 128-bit registers the first
    two move whole registers, so the kernel writes them into which register each result
    lands in, at no cost; the 256-bit kernel swaps register halves for the second. The
    exchanges of the upper words, 2 and 1 apart, stay within 128-bit lanes: one
    `pshufd` each.
  - The AVX-512 kernel's permutation indices became constants of the method instead of
    static readonly fields, which took it from 258 to 382 MiB/s on .NET 10.
  - The AVX2 kernel comes close to the AVX-512 one: each round depends on the one
    before, so the dependency chain, not the register width, bounds it.
  - `CubeHashCoreTests` hold each vector kernel to the scalar kernel over seeded states
    and 0 to 160 rounds. The SHA-3 ShortMsgKAT digests pass with AVX-512 disabled and
    with AVX2 disabled, so they run through each x64 kernel. Under qemu the AdvSimd
    kernel matched the scalar kernel, and dispatch there chose it for all 1,024
    ShortMsgKAT digests, which matched; the emulator faulted reading the KAT test's
    attributes, so that check ran as a temporary probe.
- **Whirlpool (W10b).**
  - Each block took five eight-word stack buffers, wrote every round's key and state to
    scratch and copied them back, twenty 64-byte copies a block, and bounds-checked
    every one of its 1,280 table lookups.
  - The key and state now alternate between two buffers each; the ten rounds are an
    even number, so both end where they began. The table is indexed by reference, since
    every index is a column number shifted left by 8 ORed with one byte, below the
    table's 8 × 256 entries. The round keys, zero except for their first word, became
    one array of ten constants.
  - The ISO/IEC 10118-3 and NESSIE vectors for all three revisions and the OpenSSL
    reference set pass unchanged.
- **Camellia and Twofish.** As planned, no kernel work. Their modes gained from W2's
  batching of whole blocks, which W2 records for Twofish-CTR (83.7 to 89.4 MiB/s).
