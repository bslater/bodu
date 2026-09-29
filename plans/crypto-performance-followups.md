# Implementation plan: the cryptography speed-ups left for later

**Status:** Proposed · **Source:** the "Left for later" items in
[`crypto-performance.md`](crypto-performance.md) §10 and the open items in
[`argon2-performance.md`](argon2-performance.md) §10.3, after `Bodu.Security.Cryptography` 1.1.0 ·
**Target:** `Bodu.Security.Cryptography`, next lock-step release

`crypto-performance.md` met every target it set and shipped in 1.1.0. On the way it measured a
set of further gains that no target needed, and left them for later. This plan collects them,
ranks them by value against effort, and says how each would be done and proven.

Every item is optional. None fixes a defect, each stands alone, and the plan can stop after
any of them. Each starts with a scratch spike that measures the gain before any design is
committed to, because an estimate that looked safe has been wrong before: W6's `UInt128`
sums measured 8% slower than the split products that replaced them.

---

## 1. Where things stand

All figures are 1.1.0's, from `crypto-performance.md` §10, on the same machine: a 4-vCPU
Xeon VM at 2.8 GHz with AVX-512, .NET 10, 1 MiB inputs unless noted. Run-to-run noise on
this machine is about ±5–10%.

| Primitive | 1.1.0 | Where the time goes |
|---|---|---|
| Poly1305 | 1,110–1,131 MiB/s | One block at a time through the scalar 44/44/42-bit core |
| XChaCha20-Poly1305 | 761 MiB/s; 561–585 MiB/s on AVX2 | Per MiB, about 0.33 ms of keystream and 0.9 ms of MAC |
| XChaCha20-Poly1305, 64-byte message | 0.82–0.86 µs | Three scalar block computations: HChaCha20, the Poly1305 key block, the message block |
| ML-DSA-65 sign | 0.79–0.85 ms | 29 scalar transforms per attempt, about 60% of the time |
| ML-DSA-65 key generation | 302–328 µs | Expanding Â and the secret vectors, one SHAKE stream at a time |
| ML-KEM-768 encapsulate / decapsulate | 41–51 / 60–80 µs | Scalar transforms and products, one SHAKE stream at a time |
| Ed25519 sign / X25519 key generation | 51–56 / 49–51 µs | 64 windows, each a constant-time scan of 16 entries (about 220 ns) and a unified addition (about 390 ns) |
| Ed25519 verify | 136–150 µs | 256 doublings, about half the time, and about 120 additions; a 136-byte `IncrementalHash` per call |
| Serpent-256 / 512 / 1024 `Encrypt`, one block per call | 17 / 12.5 / 10.5 MiB/s | Every four-word group goes through the S-box dispatch as a call, and the state makes round trips through a span |
| Serpent-128-CTR, AVX-512VL | 563–579 MiB/s, against 730 MiB/s through `EncryptBlocks` | Writing the counter run, and a separate XOR pass |
| Every AdvSimd kernel on ARM64 | not measured | CI and emulation check the output, not the speed |

---

## 2. Principles

`crypto-performance.md` §2.1 applies unchanged:

- **G1:** identical output.
- **G2:** in-box intrinsics only.
- **G3:** every vector path has a scalar twin, tested against it.
- **G4:** constant time is kept.
- **G5:** the public API changes only by addition, and no addition is planned.
- **G6:** everything is measured with the `--crypto-harness` of
  `Bodu.Security.Cryptography.Benchmarks`, before and after, on both runtimes.

What that plan taught adds three rules:

- **Kernel entry points are `NoInlining | AggressiveOptimization`**, so that neither a
  caller's inlining budget nor dynamic PGO can degrade the kernel's code. Without it,
  Serpent's single-block entry points ran at 33 MiB/s under dynamic PGO, against 77 MiB/s
  with it.
- **Generated code is compared on both runtimes.** Capture it with `DOTNET_JitDisasm` and
  `DOTNET_TieredCompilation=0`. The .NET 8 JIT inlines less than .NET 10's, and a helper
  that costs nothing on `net10.0` can exhaust `net8.0`'s budget. Switch on
  `NET10_0_OR_GREATER` only where the two measurably differ, as the scalar BLAKE2 kernels do.
- **Vector rotations go through Bodu.Core.** New kernels rotate through
  `VectorExtensions.RotateBitsLeftUnchecked<TIsa>`, with the instruction set as a type
  argument. One change to a `VectorRotation` implementation then reaches every kernel.

---

## 3. Items

Ranked by value against effort. The targets are indicative until each item's spike has run.

### F1 — Poly1305 several blocks at a time

- **Problem:**
  - Poly1305 is now the Poly1305 AEADs' bottleneck, about three quarters of their time at
    1 MiB.
  - The 1.1.0 core is scalar. It works on three 64-bit limbs and folds one 16-byte block
    per iteration.
- **Design:**
  - **Representation.** Radix 2^26: five limbs, each in a 64-bit lane, as the vector paths
    of OpenSSL and BoringSSL do (Goll and Gueron, 2015). The products come from
    `vpmuludq`: `Avx2.Multiply` over `Vector256<uint>`, and `Avx512F.Multiply`. .NET 10
    exposes no IFMA intrinsic, so the 52-bit `vpmadd52luq` form is not available.
  - **Powers of r.** Computed once per key: r, r², r³ and r⁴, and up to r⁸ for AVX-512.
    Five times each limb is precomputed for the wrap modulo 2^130 − 5.
  - **The lanes.** Each lane accumulates every fourth (or eighth) block, multiplying by r⁴
    (or r⁸). At the end the lanes are multiplied by r⁴, r³, r² and r and summed into the
    scalar state, so the tag is unchanged. Carries between limbs are lazy wherever the
    64-bit lanes leave headroom.
  - **ARM64.** AdvSimd's widening multiplies (`umull` / `umlal`) over two lanes, as
    OpenSSL's NEON path does.
  - **Dispatch.** The vector path runs only above a length threshold, chosen by
    measurement, because computing the powers costs three field multiplications per key
    (seven for AVX-512), and every AEAD message has a fresh key. The 1.1.0 core keeps
    short inputs and the tail. The kernels plug in behind `Poly1305Core`, so `Poly1305`
    and all three AEADs gain together.
- **Target:**
  - Poly1305 ≥ 2 GiB/s with AVX2 and ≥ 3 GiB/s with AVX-512.
  - XChaCha20-Poly1305 ≥ 1.3 GiB/s with AVX-512 and ≥ 800 MiB/s with AVX2.
- **Tests:**
  - Every kernel, driven explicitly, is held to the 1.1.0 core over seeded keys and
    messages: every length from 0 to 1,100 bytes, plus lengths either side of the
    threshold, fed whole and in random chunks.
  - Edge keys: r with every clamped bit set, and s at its maximum.
  - The RFC 8439 vectors, through every kernel and with SIMD off.
- **Constant time:** the only branch is on the message length, which is public.

### F2 — the short AEAD message

- **Problem:** a 64-byte XChaCha20-Poly1305 message costs 0.82–0.86 µs. Three scalar
  ChaCha20 block computations are most of that.
- **Design:**
  - Produce the Poly1305 key block (counter 0) and the first three message blocks
    (counters 1–3) in one four-way kernel call. The `ChaCha20Core.Keystream` value on the
    stack then serves messages up to 192 bytes from that call.
  - HChaCha20 stays scalar, because it derives the key the other blocks use.
  - XSalsa20-Poly1305's framing takes its Poly1305 key from the first 32 bytes of block 0,
    and gets the same change.
- **Target:** a 64-byte XChaCha20-Poly1305 message ≤ 0.6 µs.
- **Tests:**
  - The framing tests already hold the stack path to the engine path at every length up
    to 299 bytes, plus 1,024, 1,029 and 4,099.
  - New rows sit at 192 bytes and at each block boundary around it.

### F3 — ML-DSA and ML-KEM vector transforms, and a four-way SHAKE

- **Problem:**
  - A signing attempt runs 29 scalar transforms, about 60% of its time. Each scalar
    butterfly is 16 instructions around three dependent multiplications.
  - ML-KEM's transforms and products are scalar too.
  - Both expand their matrices, masks and noise from independent SHAKE streams, one at a
    time.
- **Design:**
  - **ML-DSA transforms.** An eight-lane AVX2 transform, inverse transform and pointwise
    Montgomery product over the existing `int` coefficients, as the reference
    implementation's AVX2 code does:
    - Montgomery products from `vpmuldq` on the even and odd lanes.
    - The last three layers done with in-register shuffles.
    - A 128-bit kernel serves SSSE3 and AdvSimd through the usual `TIsa` pattern. AVX-512
      only if the spike shows it pays.
  - **ML-KEM transforms.** Its coefficients are also held as `int`, so an eight-lane
    kernel keeps the layout. The reference's sixteen-lane kernel over 16-bit coefficients
    would need 16-bit storage throughout; the spike measures both before choosing.
  - **Four-way SHAKE.** A `Vector256<ulong>` Keccak-f[1600] runs four independent sponges,
    one state per lane. It serves:
    - ML-DSA: `ExpandA` at key set-up, `ExpandMask` on every signing attempt, and
      `ExpandS`.
    - ML-KEM: matrix sampling and the noise streams.
- **Target:**
  - ML-DSA-65 sign ≤ 0.45 ms.
  - ML-DSA-65 key generation ≤ 200 µs.
  - ML-KEM-768 decapsulate ≤ 45 µs.
- **Tests:**
  - The vector transforms and products are held to the scalar ones, and to
    `MLDsaReference` / `MLKemReference`, over edge and seeded polynomials, every kernel
    driven explicitly.
  - The four-way sponge is held to four scalar sponges over seeded inputs, at every squeeze
    length up to three rates.
  - The NIST ACVP and Wycheproof vectors pass through the vector paths, and again with
    SIMD off.

### F4 — Ed25519's fixed-base table and verification

- **Problem:**
  - Signing and X25519 key generation spend each of their 64 windows on a constant-time
    scan of 16 entries in extended coordinates, plus a unified addition.
  - Verification is 256 doublings, about half its time, plus about 120 additions. It uses
    fixed 4-bit windows for both scalars, and every call allocates a 136-byte
    `IncrementalHash`.
- **Design:**
  - **Signed digits.** Signed radix-16 digits (−8 to 8) over a table of 8 entries per
    window, with a branch-free conditional negation. That halves the scan.
  - **Niels form.** The entries are held in affine Niels form, (y + x, y − x, 2d·x·y),
    normalized once when the table is built, as ref10 does. Each entry is then three field
    elements instead of four, and the mixed addition skips the Z multiplication.
  - **Verification.**
    - Doublings whose result is doubled again skip T, one multiplication each.
    - A width-5 NAF covers k·(−A), with its eight odd multiples on the stack.
    - A width-7 NAF covers S·B, over a static table of 32 odd multiples of B in Niels form.
    - Verification may run in variable time, as it does now, because its inputs are public.
  - **Hashing.** Short messages are hashed from a stack buffer with `SHA512.HashData`, so no
    `IncrementalHash` is allocated; longer ones keep it. The threshold is a few hundred
    bytes, chosen by measurement.
- **Target:**
  - Ed25519 sign ≤ 42 µs.
  - X25519 key generation ≤ 38 µs.
  - Ed25519 verify ≤ 110 µs, with nothing allocated below the hashing threshold.
- **Tests:**
  - Fixed-base multiplication is held to the ladder and to the 1.1.0 table over boundary
    and seeded scalars, including every signed digit.
  - Double-scalar multiplication is held to two separate multiplications.
  - The RFC 8032, RFC 7748 and Wycheproof vectors still pass.
  - The table selection is held to a plain lookup for every signed digit of every window.

### F5 — Serpent's wide-block variants

- **Problem:**
  - Serpent-256, 512 and 1024 gained 7× in 1.1.0, against Serpent-128's 20×.
  - Every round sends each four-word group through the indexed S-box dispatch as a call.
  - The state makes a round trip through a span between the key, S-box and linear passes.
- **Design:**
  - Unroll their rounds eight at a time, one per S-box, as Serpent-128's are, with each
    block's groups in locals.
  - The cross-group word rotation, and the tweak injection every fourth round, stay.
  - A many-block vector kernel, with one block per lane, is a possible second step if a
    batched mode needs it.
- **Target:** at least twice each variant's 1.1.0 speed, one block per call.
- **Tests:**
  - The variants' published vectors.
  - A differential test against the 1.1.0 implementation, kept as the oracle, over seeded
    keys, tweaks and blocks of every variant.

### F6 — counter mode without the counter run

- **Problem:** Serpent-128-CTR runs at about three quarters of `EncryptBlocks`.
  `CounterKeystream` writes a 4 KiB run of counter blocks, encrypts it, then XORs it into
  the output in a second pass.
- **Design:**
  - An internal counter-mode entry on managed kernels that can take one, starting with
    Serpent.
  - The counters are formed in registers and the keystream is XORed into the output inside
    the kernel, as ChaCha20's `XorBlocks` does.
  - `CounterKeystream` stays for the other ciphers. AES cannot fuse, because its rounds run
    in the BCL.
- **Target:** Serpent-128-CTR within 10% of `EncryptBlocks`.
- **Tests:**
  - CTR is held to the existing block-at-a-time reference at every split point.
  - A counter wrap must leave exactly the output the old loop left.

### F7 — ARM64 on real hardware, and a small-L3 desktop

- **Problem:**
  - CI's hosted ARM64 runner and qemu prove the AdvSimd kernels correct, but nothing has
    measured their speed. `argon2-performance.md` calls this the riskiest target.
  - The small-L3 desktop run, which would validate Argon2's thread heuristic on the second
    machine it names, is also outstanding.
- **Design:**
  - A manual-dispatch workflow job runs `--crypto-harness` and `--argon2-harness` on the
    hosted ARM64 runner and uploads the results, repeated to give ranges.
  - Where it is available, a run on Apple silicon or Graviton.
  - The AdvSimd kernels are compared with the scalar paths and with 1.0.0 on the same
    machine.
  - Byte rotations are the first candidate for tuning, for example `SHL` / `SRI` in place of
    `TBL`. They now live in `VectorRotation.AdvSimd`, so one change reaches every kernel.
- **Target:** every AdvSimd kernel at least as fast as its scalar path on the same machine.
  A kernel that is not is gated off through `SimdCapabilities` until it is tuned.
- **Tests:** none new. The results are recorded here, with the machine and runtime.

---

## 4. Order and method

1. **F7 first, or alongside F1.** It needs no library code, and its results shape the
   AdvSimd halves of F1 and F3.
2. **F1, then F2,** which reuses F1's measurements.
3. **F3, F4, F5 and F6,** in that order.

Each item follows the same sequence:

1. A scratch spike measures the gain.
2. The implementation lands test first where it replaces behaviour, with the replaced code
   kept in the test project as the oracle.
3. The generated code is compared on both runtimes.
4. The before and after figures are recorded in a results section added to this plan.

Each item is its own pull request.

## 5. Validation

As `crypto-performance.md` §6:

- The published vectors pin the output. CI runs them on `net8.0` and `net10.0` under
  `test.runsettings`.
- Each replaced routine stays in the test project as a differential oracle.
- Every vector kernel is swept against scalar.
- `Bodu.Security.Cryptography.Simd.Test` replays the vectors with SIMD off, and the ARM64 job
  runs both suites natively.
- Each new kernel is reviewed against `SECURITY-CHECKLIST.md` §7, "Vector kernels".

## 6. Compatibility and version

- Output is unchanged, and no public API change is planned.
- The work ships with the next lock-step `BoduBaseVersion` bump. For this package that bump
  must move past the out-of-band 1.1.0 (`bld/RELEASING.md`).

## 7. Risks

| Risk | Mitigation |
|---|---|
| F1's powers of r cost more than the lanes save on short messages | A length threshold chosen by measurement; the 1.1.0 core keeps everything below it |
| .NET 8 inlines less than .NET 10 | `NoInlining \| AggressiveOptimization` kernel entry points, and the generated code compared on both runtimes |
| F4's signed digits add a timing leak | A branch-free conditional negation and conditional moves over the whole window; reviewed per the checklist |
| An AdvSimd kernel turns out slower than scalar | Gate it off through `SimdCapabilities` until tuned |
| Hosted ARM64 runners are shared and noisy | Repeated runs, recorded as ranges; A/B comparisons within one run |

## 8. Out of scope

- **Salsa20 trailing ChaCha20 on AVX2.** None of Salsa20's rotations is byte-aligned, so
  each is a shift pair; AVX-512's rotate instruction already closes the gap.
- **The field multiply's register pressure.** Four 64-bit limbs would need add-with-carry,
  which .NET does not expose.
- **IFMA arithmetic for Poly1305 and Curve25519.** .NET 10 exposes no IFMA intrinsic.
- **Moving the remaining AVX-512 rotations onto Bodu.Core** (BLAKE2, BLAKE3, Threefish).
  That is consistency rather than speed, and is decided separately.
- **Recurrence's period fast-forward.** It is not a cryptography item; it is Phase 7 of
  [`recurrence-scheduling.md`](recurrence-scheduling.md).
