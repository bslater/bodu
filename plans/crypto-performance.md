# Implementation plan: faster primitives across Bodu.Security.Cryptography

**Status:** In progress on `claude/argon2-prototype-co27tu` (W0–W2c done, §10) · **Source:** the assessment run on
2026-09-27 after the Argon2 work (§1) · **Target:** `Bodu.Security.Cryptography`, next lock-step release

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
| SHAKE256 | 31 MiB/s | 29 MiB/s | — |
| BLAKE2b-512 | 688 MiB/s | 151 MiB/s | OpenSSL (plain C) 622–673 MiB/s |
| BLAKE2s-256 | 535 MiB/s | 78 MiB/s | OpenSSL (plain C) 394–419 MiB/s |
| BLAKE3 | 278 MiB/s | 82 MiB/s | slower than Bodu's own BLAKE2b |
| CubeHash | 234 MiB/s | 20 MiB/s | — |
| Skein-512 | 301 MiB/s | 293 MiB/s | — |

### 1.2 Ciphers, modes and AEADs (1 MiB input unless noted)

| Primitive | Bodu | Reference |
|---|---|---|
| AES-128-CTR | 59 MiB/s | OpenSSL 4.9 GiB/s; Bodu's own ECB bulk path 3.4 GiB/s |
| AES-128-GCM | 57 MiB/s | BCL `AesGcm` 4.1 GiB/s |
| AES-128-GCM, PCLMULQDQ off (the path ARM64 takes) | 3.9 MiB/s | — |
| ChaCha20 | 290 MiB/s | OpenSSL 3.4 GiB/s |
| XChaCha20-Poly1305 | 145 MiB/s; 624 B allocated per message | BCL ChaCha20-Poly1305 1.7 GiB/s |
| XChaCha20-Poly1305, 64-byte message | 1.33 µs | BCL 1.27 µs |
| Poly1305 | 439 MiB/s | — |
| Serpent-128-CTR | 3.7 MiB/s | — |
| Camellia-128-CTR | 41 MiB/s | OpenSSL CBC 182 MiB/s |
| Twofish-CTR | 83 MiB/s | — |
| Threefish-512-CTR | 240 MiB/s | — |

### 1.3 Key derivation

| Case | Bodu | OpenSSL (plain C) |
|---|---|---|
| scrypt N=2¹⁴ r=8 p=1 (16 MiB) | 112 ms; 16 MiB allocated per call; gen2 collections | 48 ms |
| scrypt N=2¹⁴ r=8 p=4 | 415 ms (units run one at a time) | 201 ms |
| scrypt N=2¹⁷ r=8 p=1 (128 MiB) | 1,430 ms; 128 MiB allocated per call | 530 ms |
| Argon2id m=19 MiB t=2 p=1, for scale | 26 ms | — |

### 1.4 Public-key operations

| Operation | Bodu | Reference |
|---|---|---|
| X25519 shared secret | 246 µs | OpenSSL 40 µs |
| X25519 key generation | 222 µs | — |
| Ed25519 sign / verify | 105 µs / 388 µs (verify allocates 2.7 KB) | OpenSSL 53 µs / 160 µs |
| ML-KEM-768 keygen / encaps / decaps | 301 / 297 / 330 µs | — |
| ML-DSA-65 keygen / sign / verify | 1.16 / 3.44 / 1.06 ms; sign allocates 119 KB | — |

A what-if in a scratch copy replaced only the Keccak permutation with an unrolled one.
SHAKE128 went from 37 to 249 MiB/s, ML-KEM-768 from 297/330 to 130/167 µs, and ML-DSA-65
verify from 1.06 ms to 0.42 ms. That permutation is the basis of W1.

---

## 2. Goals

### 2.1 Principles

- **G1 — identical output.** Every change except W0's GCM-SIV fix is byte-for-byte
  identical to 1.0.0. Published vectors lock it, and so do differential tests against
  the replaced code, which moves into the test project as the oracle.
- **G2 — in-box only.** Changes use `System.Runtime.Intrinsics`, `NativeMemory`,
  `Parallel` and `TimeProvider`, with no new package or native dependency. The target
  frameworks stay `net8.0;net10.0`.
- **G3 — every vector path has a scalar twin, tested against it.**
  - `DisableSimd` and the `Bodu.Security.Cryptography.Simd.Test` assembly keep the
    scalar paths on every vector.
  - The ARM64 job runs both suites natively.
- **G4 — constant time is kept, and improved where it can be.**
  - No new branch or memory access depends on a secret.
  - Replacing per-bit lookups (Serpent) and tweak branches (XTS) removes existing ones.
- **G5 — additive public API.** The only planned additions are a thread bound for scrypt
  (W3) and, if W5 needs it, for BLAKE3.
- **G6 — measured.** Each workstream records before and after in §10, with the harness
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
| W4 BLAKE2 | BLAKE2b scalar / AVX2 | 151 MiB/s / — | ≥ 450 / ≥ 650 MiB/s |
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
| **T5** | Validation: every-kernel-against-scalar sweeps, the `DisableSimd` assembly, the ARM64 job, and hardware-gated coverage classification | 5–6 |

---

## 4. Workstreams

Each workstream is one or more commits on the branch, and each leaves
`dotnet test bodu.slnx --settings test.runsettings` green, CI's tier including
Regression, on `net8.0` and `net10.0`. They are ordered by value against effort, and
none depends on a later one.

### W0 — correctness and hygiene

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

### W1 — Keccak (SHAKE, ML-KEM, ML-DSA)

- **Problem:**
  - `KeccakPermutation.Permute` is the textbook loop: `% 5` indexing, rho and pi through
    tables, bounds-checked spans, and a `stackalloc` plus clear on every permutation.
  - It runs at about 4.3 µs per permutation, and it dominates ML-KEM and ML-DSA.
- **Design:**
  - **T1.** The unrolled, register-resident permutation from the what-if: 25 lane
    locals, with theta, rho and pi folded into one pass and chi applied per row.
  - The samplers:
    - ML-KEM `SampleNtt`, and ML-DSA `RejNttPoly` / `RejBoundedPoly` / `SampleInBall`,
      squeeze whole rate blocks and parse them, instead of pulling 1–3 bytes per call.
    - The public `Shake` absorbs full blocks straight from the caller's span.
- **Tests:**
  - The loop permutation moves into the test project as `KeccakReference`.
  - A BVT test compares the two over 1,000 seeded random states.
  - The FIPS 202 vectors and the ML-KEM/ML-DSA ACVP vectors already in the suite pin
    the end-to-end output.
- **Later, in W9:** a four-way permutation over `Vector256<ulong>` for the independent
  XOFs.

### W2 — AES modes, GHASH and POLYVAL

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
  - **T1 — one keystream core for the counter modes.**
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
  - **T4 — GHASH.**
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

### W3 — scrypt

- **Problem:**
  - `ScryptCore` allocates the whole `V` (`new uint[N·32r]`) on every call, plus
    working buffers.
  - It runs the p independent ROMix units one after another.
  - Salsa20/8 is scalar, with a `stackalloc` and copy on every call.
  - BlockMix copies each sub-block.
  - The result is 2.3–2.7× slower than OpenSSL's plain C.
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

### W4 — BLAKE2b and BLAKE2s

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

### W5 — BLAKE3

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

### W6 — ChaCha20, Salsa20, Poly1305 and the Poly1305 AEADs

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

### W7 — Serpent

- **Problem:**
  - The bitsliced layout applies each S-box through a 32-iteration loop: gather one bit
    per word, look it up in a 16-entry table, scatter the result.
  - That costs about 4 µs per block.
- **Design:**
  - **T1:** Osvik's Boolean S-box circuits (S0–S7 and their inverses) replace the loop.
    They are constant time by construction.
  - **T4:** `EncryptBlocks` / `DecryptBlocks` process 4 or 8 blocks through
    `Vector128<uint>` / `Vector256<uint>` for the batched modes from W2.
- **Tests:** the NESSIE and AES-submission vectors, the wide-block variants'
  existing vectors, and a differential test of the multi-block path against single
  blocks.

### W8 — Curve25519 and Ed25519

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

### W9 — ML-KEM and ML-DSA beyond Keccak

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

### W10 — lower priority

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

- Output is unchanged everywhere except AES-256-GCM-SIV. That change is a conformance
  fix, but it breaks stored data sealed under a 256-bit key, and the release notes must
  say so.
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
this machine is about ±5–10%.

### W0 — correctness and hygiene (done)

- AES-256-GCM-SIV derives a 256-bit message-encryption key, as RFC 8452 §4 specifies.
  The C.2 and C.3 vectors are pinned, and the test that proved the bug was committed
  first. Output made by earlier AES-256-GCM-SIV versions does not decrypt (§7).
- The coverage script and the docs name both runtimes' AVX-512 switches:
  `DOTNET_EnableAVX512F` on .NET 8 and `DOTNET_EnableAVX512` on .NET 10.
- XTS doubles its tweak with a mask instead of a branch. Fifteen IEEE 1619-2007
  whole-block vectors are pinned; the previous vectors never doubled the tweak.
- Threefish-512's kernel is gated on AVX-512VL, which it uses.

### W1 — Keccak (done)

| Measure | Baseline | Result | Target |
|---|---|---|---|
| SHAKE128, 1 MiB | 35.2 MiB/s | 263.1 MiB/s | ≥ 200 MiB/s — met |
| SHAKE256, 1 MiB | 29.6 MiB/s | 178.0 MiB/s | — |
| SHAKE128, 64-byte input | 4.59 µs | 0.70 µs (BCL: 1.21 µs) | — |
| ML-KEM-768 keygen / encaps / decaps | 289 / 306 / 326 µs | 103 / 138 / 139 µs | ≤ 150 / ≤ 180 µs — met |
| ML-DSA-65 keygen / sign / verify | 1.14 / 3.31 / 1.04 ms | 0.33 / 2.24 / 0.30 ms | verify ≤ 0.5 ms — met |

On .NET 8: SHAKE128 goes from 36.4 to 269.9 MiB/s, ML-KEM-768 encaps / decaps from
290 / 416 to 114 / 162 µs, and ML-DSA-65 verify from 1.01 to 0.33 ms.

### W2 — AES modes, GHASH and POLYVAL (W2a–c done; W2d next)

| Measure | Baseline | Result | Target |
|---|---|---|---|
| AES-128-CTR, 1 MiB | 54.8 MiB/s | 1,665.8 MiB/s | ≥ 1 GiB/s — met |
| AES-128-GCM, 1 MiB | 54.9 MiB/s | 1,095.2 MiB/s, 240 B allocated | ≥ 800 MiB/s — met |
| AES-128-GCM, 64 bytes | 1.81 µs | 1.04 µs | — |
| AES-128-GCM, scalar GHASH kernel | 4.1 MiB/s | 208.9 MiB/s | ≥ 40 MiB/s — met |
| AES-128-GCM-SIV, 1 MiB | 38.8 MiB/s, 1,496 B | 1,154.5 MiB/s, 848 B | ≥ 5× — met (30×) |
| AES-128-GCM-SIV, 64 bytes | 10.47 µs | 5.09 µs | — |
| Threefish-512-CTR / Twofish-CTR | 244 / 83.7 MiB/s | 338 / 89.4 MiB/s | — |

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
  saves 1.8 µs for every cipher that only encrypts — GCM-SIV creates one per message.
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

W2d baselines, measured after W2c: CCM 26.6, EAX 27.4 (3.67 MB and 15 gen2 collections
per message), OCB 49.7, SIV 26.0 (3.67 MB), CBC decrypt 50.4, and XTS 59.7 MiB/s.
