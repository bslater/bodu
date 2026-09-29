# Implementation plan: the cryptography speed-ups left for later

**Status:** In progress — F1 done (§9) · **Source:** the "Left for later" items in
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

Done: see §9 for the results and where the build departs from this design.

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

---

## 9. Results

Figures are from the `--crypto-harness` of `Bodu.Security.Cryptography.Benchmarks` on the
same 4-vCPU Xeon VM as §1, at 2.8 GHz with AVX-512F/VL but no IFMA. Each is the range over
two runs of the median of five rounds. The baselines were measured the same day on this
branch before the item's code, with the harness change that added the case in place.

### F1 — Poly1305 several blocks at a time (done)

Before and after, on both runtimes, in the harness's five processor configurations.
*Change* is the ratio of the two ranges' midpoints, above 1 where F1 is faster. The
OpenSSL and BCL rows run code F1 does not touch, so they show how far the runs drift on
their own.

| Measure (net10.0) | Before F1 | After F1 | Change |
|---|---|---|---|
| Poly1305 1 MiB, this host's default: the paired AVX2 loop | 1,016–1,054 MiB/s | 4,697–4,877 MiB/s | 4.6× |
| Poly1305 1 MiB, AVX-512 eight lanes (`DOTNET_PreferredVectorBitWidth=512`) | 924–942 MiB/s | 6,264–6,881 MiB/s | 7.0× |
| Poly1305 1 MiB, AVX2 alone (`DOTNET_EnableAVX512=0`): the one-group loop | 1,030–1,052 MiB/s | 3,188–3,193 MiB/s | 3.1× |
| Poly1305 16 KiB, default / 512-bit | 1,038–1,069 MiB/s / 849–918 MiB/s | 4,432–4,604 MiB/s / 5,374–5,497 MiB/s | 4.3× / 6.2× |
| Poly1305 1 KiB, default | 801–818 MiB/s | 1,400–1,458 MiB/s | 1.77× |
| Poly1305 256 B / 64 B, default | 0.53–0.55 µs / 0.34–0.38 µs | 0.53–0.54 µs / 0.36–0.41 µs | 1.01× / 0.94× |
| Poly1305 1 MiB, without AVX2 / without vector code: the scalar loop | 688–709 MiB/s / 641–642 MiB/s | 608–685 MiB/s / 570–699 MiB/s | 0.92× / 0.99× |
| XChaCha20-Poly1305 1 MiB, default / 512-bit | 751–781 MiB/s / 735–743 MiB/s | 1,696–1,726 MiB/s / 2,142–2,164 MiB/s | 2.2× / 2.9× |
| XChaCha20-Poly1305 1 MiB, AVX2 alone | 562–588 MiB/s | 784–827 MiB/s | 1.40× |
| XChaCha20-Poly1305 16 KiB / 1 KiB, default | 745–764 MiB/s / 485–574 MiB/s | 1,621–1,624 MiB/s / 735–760 MiB/s | 2.2× / 1.41× |
| XChaCha20-Poly1305 256 B / 64 B, default | 0.91–0.95 µs / 0.86–0.92 µs | 0.94–0.95 µs / 0.86–0.96 µs | 0.98× / 0.98× |
| XSalsa20-Poly1305 1 MiB, default / 512-bit | 763–764 MiB/s / 688–739 MiB/s | 1,641–1,803 MiB/s / 2,165–2,250 MiB/s | 2.3× / 3.1× |
| XSalsa20-Poly1305 256 B / 64 B, default | 1.31–1.36 µs / 0.66–0.68 µs | 1.38–1.51 µs / 0.68–0.69 µs | 0.92× / 0.98× |
| XChaCha20-Poly1305 256 B, without AVX2 / without vector code | 1.27–1.34 µs / 1.68–1.80 µs | 1.27–1.29 µs / 1.77–1.82 µs | 1.02× / 0.97× |
| OpenSSL Poly1305 1 MiB, for reference | 4,291–4,554 MiB/s / 4,480–4,556 MiB/s | 4,163–4,480 MiB/s / 4,312–4,426 MiB/s | 0.98× / 0.97× |
| BCL ChaCha20-Poly1305 1 MiB (OpenSSL), for reference | 1,779–1,811 MiB/s / 1,697–1,802 MiB/s | 1,617–1,737 MiB/s / 1,664–1,795 MiB/s | 0.93× / 0.99× |

| Measure (net8.0) | Before F1 | After F1 | Change |
|---|---|---|---|
| Poly1305 1 MiB, this host's default: the paired AVX2 loop | 1,046–1,104 MiB/s | 4,915–4,929 MiB/s | 4.6× |
| Poly1305 1 MiB, AVX-512 eight lanes (`DOTNET_PreferredVectorBitWidth=512`) | 1,123–1,166 MiB/s | 6,133–6,719 MiB/s | 5.6× |
| Poly1305 1 MiB, AVX2 alone (`DOTNET_EnableAVX512=0`): the one-group loop | 1,068–1,168 MiB/s | 2,918–2,999 MiB/s | 2.6× |
| Poly1305 16 KiB, default / 512-bit | 1,082–1,127 MiB/s / 1,088–1,120 MiB/s | 4,567–4,681 MiB/s / 5,216–5,315 MiB/s | 4.2× / 4.8× |
| Poly1305 1 KiB, default | 716–807 MiB/s | 1,112–1,453 MiB/s | 1.68× |
| Poly1305 256 B / 64 B, default | 0.55–0.57 µs / 0.37–0.40 µs | 0.55–0.57 µs / 0.36–0.37 µs | 1.00× / 1.05× |
| Poly1305 1 MiB, without AVX2 / without vector code: the scalar loop | 1,109–1,152 MiB/s / 679–712 MiB/s | 1,005–1,101 MiB/s / 705–713 MiB/s | 0.93× / 1.02× |
| XChaCha20-Poly1305 1 MiB, default / 512-bit | 736–772 MiB/s / 721–760 MiB/s | 1,666–1,699 MiB/s / 2,240 MiB/s | 2.2× / 3.0× |
| XChaCha20-Poly1305 1 MiB, AVX2 alone | 567–620 MiB/s | 801–834 MiB/s | 1.38× |
| XChaCha20-Poly1305 16 KiB / 1 KiB, default | 784–809 MiB/s / 542–545 MiB/s | 1,558–1,623 MiB/s / 637–677 MiB/s | 2.00× / 1.21× |
| XChaCha20-Poly1305 256 B / 64 B, default | 0.96–0.99 µs / 0.84–0.91 µs | 0.97–0.98 µs / 0.85–0.86 µs | 1.00× / 1.02× |
| XSalsa20-Poly1305 1 MiB, default / 512-bit | 762–804 MiB/s / 698–752 MiB/s | 1,726–1,811 MiB/s / 2,223–2,238 MiB/s | 2.3× / 3.1× |
| XSalsa20-Poly1305 256 B / 64 B, default | 1.41–1.46 µs / 0.73–0.75 µs | 1.42–1.50 µs / 0.75–0.79 µs | 0.98× / 0.96× |
| XChaCha20-Poly1305 256 B, without AVX2 / without vector code | 1.22–1.24 µs / 1.76–1.83 µs | 1.19–1.23 µs / 1.76–1.87 µs | 1.02× / 0.99× |
| OpenSSL Poly1305 1 MiB, for reference | 4,369–4,722 MiB/s / 4,488–4,800 MiB/s | 4,286–4,546 MiB/s / 4,205–4,638 MiB/s | 0.97× / 0.95× |
| BCL ChaCha20-Poly1305 1 MiB (OpenSSL), for reference | 1,740–1,840 MiB/s / 1,833–1,906 MiB/s | 1,734–1,793 MiB/s / 1,700–1,726 MiB/s | 0.99× / 0.92× |

The scalar rows, and the messages of 256 bytes and less, move by no more than the
reference rows drift, so they were also compared in alternating A/B runs of the two
builds, three rounds each on both runtimes. For Poly1305 at 1 MiB and 256 bytes, and
both AEADs at 256 bytes, with AVX2 disabled and without vector code, the medians lie
between 0.92× and 1.15×, scattered both ways. Where F1 left the code unchanged, the
machine code shows it: the scalar loop compiles to exactly what it did before on .NET 10
in every configuration, and on .NET 8 without vector code, where that A/B still read
0.92× as one build's runs drifted from 1.49 to 1.68 ms.

How it was done, and where it departs from the design above:

- **Kernels.** `Poly1305Core` gained two nested kernels behind its block loop, both
  giving each 64-bit lane a block of its own as five 26-bit limbs, multiplied with
  `vpmuludq`:
  - `Vector256Kernel`, four lanes on AVX2, with two loops. `Blocks` takes one group of
    four blocks at a time, multiplying the lanes by r⁴. `BlocksPaired` takes two, as
    (h + m)·r⁸ + m′·r⁴: the second group's products depend only on the message, so they
    fill the multiplier while the first group's carries run, and the two share one round
    of carries.
  - `Vector512Kernel`, eight lanes on AVX-512F, paired the same way with r¹⁶ and r⁸.
  - After the last group each lane multiplies by the power of r its last block needs —
    r⁴, r², r³ and r in the AVX2 lane order b1, b3, b2, b4 that `vpunpcklqdq` leaves —
    and the lanes are summed back into the 44/44/42-bit accumulator, so the tag is
    unchanged. The blocks after the last whole group take the scalar loop.
- **The paired loop is a finding, not the design.** The first version took one group at a
  time, and its loop was bound by the chain from one group's carries to the next group's
  products. In the per-kernel sweep of `Poly1305Core` below, at 1 MiB, that gave
  3,347–3,454 MiB/s with AVX2 and 5,579 MiB/s with AVX-512; pairing the groups raised
  them to 4,993–5,280 and 7,171–7,229 MiB/s. But the paired loop holds about twice as
  many live vectors, which fit in the 32 registers AVX-512VL gives 256-bit code and
  spill from AVX2's 16. With AVX-512 disabled it measured 2,323–2,784 MiB/s on .NET 8,
  below the one-group loop's 2,712–3,389. So dispatch takes the paired AVX2 loop only
  where AVX-512VL is available, and the kinds are `Avx2`, `Avx2Paired` and `Avx512`.
- **Powers of r.** Computed per run rather than once per key, since the core is a struct
  kept on the stack and every AEAD message has a fresh key. They come from the scalar
  multiply, factored out of the block loop as `Poly1305Core.Multiply`, arranged so that at
  most four depend on one another: three multiplications for the one-group loop, four
  for the paired one, eight for AVX-512. They live in a stack buffer cleared when the
  run ends.
- **Dispatch.** `SelectKernel(length)` places each kernel where it was measured to win on
  both runtimes: in a per-kernel sweep of `Poly1305Core` (initialize, update, finish), and,
  for the AVX2 threshold, in the AEADs themselves:

  | Kernel | From | Crossover measured (.NET 8 / .NET 10) |
  |---|---|---|
  | `Avx2` over the scalar loop | 512 bytes | 192–256 / 192–256 bytes for the MAC alone; about 512 bytes inside an AEAD |
  | `Avx2Paired` over `Avx2`, with AVX-512VL | 1 KiB | 512 bytes–1 KiB / 1 KiB |
  | `Avx512` over `Avx2Paired`, where the runtime prefers 512-bit vectors | 4 KiB | 4 KiB / 2–3 KiB |

  This host does not prefer 512-bit vectors by default, so the eight-lane kernel needs
  `DOTNET_PreferredVectorBitWidth=512` here, as ChaCha20's sixteen-block kernel does.

  The sweep, in nanoseconds per message on .NET 10 with 512-bit vectors preferred (the
  bold figure is the kernel dispatch picks):

  | Bytes | Scalar | `Avx2` | `Avx2Paired` | `Avx512` |
  |---|---|---|---|---|
  | 128 | **193** | 197 | 212 | 293 |
  | 256 | **311** | 240 | 255 | 310 |
  | 512 | 544 | **284** | 334 | 416 |
  | 1,024 | 1,043 | 408 | **397** | 459 |
  | 2,048 | 1,981 | 666 | **602** | 590 |
  | 4,096 | 3,879 | 1,155 | 952 | **838** |
  | 16,384 | 15,044 | 4,170 | 3,082 | **2,397** |
  | 1 MiB | 939,903 | 298,797 | 200,279 | **138,332** |

  At 1 MiB that is 1,064, 3,347, 4,993 and 7,229 MiB/s. On .NET 8 the figures are within
  a few percent, except that the eight-lane kernel overtakes the paired loop only from
  4 KiB (881 against 901 ns there, 619 against 570 at 2 KiB).
- **The AEADs set the AVX2 threshold, not the MAC.** The first cut took the AVX2 kernel
  from 256 bytes, where the sweep showed it winning. The harness then showed
  XChaCha20-Poly1305 and XSalsa20-Poly1305 messages of 256 bytes running slower than
  before. An A/B of the previous library, the 256-byte threshold and a 512-byte one, in
  alternating processes on .NET 10, in nanoseconds per message:

  | Bytes | XChaCha20-Poly1305: before / 256 / 512 | XSalsa20-Poly1305: before / 256 / 512 | Poly1305 alone: before / 256 |
  |---|---|---|---|
  | 256 | 903–962 / 972–977 / 933–934 | 1,302–1,308 / 1,429–1,454 / 1,272–1,296 | 511–513 / 512–523 |
  | 320 | 1,162–1,175 / 1,267–1,272 / 1,200–1,211 | 1,023–1,035 / 1,053–1,075 / 1,016–1,023 | 552–567 / 498–502 |
  | 512 | 1,133–1,160 / 1,030–1,032 / 1,050–1,070 | 1,676–1,689 / 1,631–1,641 / 1,656–1,714 | 735–736 / 549–564 |
  | 768 | 1,470–1,482 / 1,279–1,294 / 1,291–1,302 | 1,857–1,907 / 1,701–1,738 / 1,704–1,706 | 941–951 / 592–620 |

  Below 512 bytes the kernel saves the MAC too little to cover what it costs the rest of
  the message — most likely the lower clock this Xeon runs at once 256-bit multiplies
  are in flight, which the keystream then runs at too. So the AVX2 kernel starts at
  512 bytes, and the 256- and 320-byte messages are back to their old cost.
- **Generated code, on both runtimes.**
  - With AVX-512, every kernel compiles to all of its `vpmuludq`s inline, with no call in
    its loop, on .NET 8 and .NET 10.
  - With AVX2 alone, .NET 8 did not expand `>>` on `Vector256<ulong>`: it treats the
    operator as a signed shift, which has no 64-bit form before AVX-512, and called the
    software fallback — which also used up the inlining budget, so `Multiply`,
    `AddGroup` and `Times5` became calls in the loop. The kernels now write their right
    shifts `>>>`, which .NET 8 expands to `vpsrlq` on AVX2. .NET 10 expanded both.
  - The scalar block loop, now calling `Multiply`, compiles to the same machine code as
    before on .NET 10 in every configuration. On .NET 8 the register allocator keeps the
    loop bound in a 32-bit stack slot, two instructions more per block; A/B runs of the
    scalar path measured 1,056–1,141 MiB/s before and 1,006–1,158 after, so no framework
    switch is warranted.
  - Compiled whole, as `DOTNET_TieredCompilation=0` does, none of that shows what dynamic
    PGO does on .NET 10. The first full measurement did: Poly1305 at 1 MiB without AVX2
    ran at 614–616 MiB/s against 688–709 before. The tiered code showed why. Through the
    new `FullBlocks` call site, PGO inlined the scalar loop into `Poly1305.HashCore` and
    `Poly1305Core.Update`, ran out of budget part-way, and left `AddProduct`,
    `SplitProduct` and `Multiply` as calls in the loop. The loop had only
    `AggressiveOptimization`; it now has `NoInlining` as well, as §2's first rule requires
    of every kernel entry point, and alternating A/B runs of the final build measured
    649–730 MiB/s against 625–699 before.
  - The dispatch did the same to the AEADs. `Poly1305AeadCore.SealRfc8439`, which
    inlines `Update` for the associated data, the ciphertext and the lengths block, grew
    from 3,545 to 4,449 bytes of Tier1 code with the three kernels' calls inlined in it.
    That exhausted its budget, so `WriteUInt64LittleEndian`, `Poly1305Core.Clear` and
    `ThrowIfLessThan` became calls on every message, and XChaCha20-Poly1305 at 64 bytes
    took 807–834 ns against 785–796 before. `FullBlocks` now sends runs shorter than
    512 bytes straight to the scalar loop and dispatches longer ones out of line in
    `KernelBlocks`. The framing method is back to 3,692 bytes, making the same calls as
    before plus the out-of-line one. In three alternating runs, the medians for
    messages of 64 to 320 bytes, across Poly1305 and both AEADs, lie between 1.8%
    faster and 2.8% slower than before.
- **Tests.**
  - `Update(KernelKind, data)` drives each loop explicitly, whatever dispatch would pick.
    Every loop — scalar, `Avx2`, `Avx2Paired` and `Avx512` — is held to
    `Poly1305Reference` at every length from 0 to 1,100 bytes, over 300 seeded messages
    fed in pieces of up to 300 bytes, with r at its largest clamped value and s and the
    message at all ones or all zeros, entered after one to eight scalar blocks of all
    ones, and over the RFC 8439 vectors.
  - Dispatch is held to the reference either side of each threshold, and
    `SelectKernel`, `IsSupported` and `LanesFor` have their own tests. The SIMD-off
    assembly asserts that every run takes the scalar loop.
  - A reflection test checks that the scalar loop, the dispatch and every kernel are
    marked `NoInlining`. It was committed first and seen to fail on the scalar loop
    before the two inlining fixes above.
  - The suite passes on both runtimes with AVX-512 on, with it off, and with AVX2 off
    (where the vector kernels report inconclusive). Under qemu on ARM64, every
    `Poly1305Core` test passes on the `umulh` path, and the x64 kernels report
    inconclusive.
- **ARM64 keeps the scalar loop.** The design's two-lane AdvSimd kernel waits for F7: on
  cores where `umulh` is cheap the scalar loop may well win, and nothing here can measure
  it. The Multiply refactor runs on the `umulh` path there.
- **Left for later.**
  - The fixed cost of a kernel run, about 100 ns for the powers, the conversions and the
    lane sums, is part of what sets the thresholds. Caching the powers in the core would
    lower it for streams of small updates, at the price of a larger struct to clear.
  - For F2: the AEADs' cost is not monotonic in the message length below 1 KiB.
    XChaCha20-Poly1305 takes about 1.66 µs at 448 bytes but 1.13 µs at 512, and
    XSalsa20-Poly1305 1.30 µs at 256 bytes but 1.02 µs at 320, before and after F1 alike:
    keystream blocks left over after the widest whole kernel run go through the scalar
    block function one at a time.
  - Poly1305 no longer bounds the AEADs: at 1 MiB with AVX-512 the MAC takes about 0.15 ms
    of XChaCha20-Poly1305's 0.45 ms, and the keystream the rest.
