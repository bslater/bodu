# Implementation plan: the cryptography speed-ups left for later

**Status:** Done — F1 to F8 (§9), F8 added after F7 · **Source:** the "Left for
later" items in [`crypto-performance.md`](crypto-performance.md) §10 and the open items in
[`argon2-performance.md`](argon2-performance.md) §10.3, after `Bodu.Security.Cryptography` 1.1.0,
and F7's (§9) · **Target:** `Bodu.Security.Cryptography`, next lock-step release

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

Done: see §9 for the results and where the build departs from this design.

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

Done: see §9 for the results and where the build departs from this design.

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

Done: see §9 for the results and where the build departs from this design.

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

Done: see §9 for the results and where the build departs from this design.

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

Done: see §9 for the results and where the build departs from this design.

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

Done: see §9 for the results and where the build departs from this design.

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

### F8 — Poly1305 on ARM64

Added after F7, from the items it left for later (§9). Of those, this is the one with a measured
gain in reach: the gated kernels lose to their layout, so tuning them means redesigning them, and
the other two items are checks rather than speed-ups.

- **Problem:**
  - ARM64 runs Poly1305 one block at a time through the scalar loop. F1's design had a two-lane
    AdvSimd kernel, which waited for F7, since on ARM64 the scalar loop's products come from
    `umulh` and might hold their own.
  - F7 settled it. On the Neoverse N2 the scalar loop took 1 MiB at 1,610–1,653 MiB/s, and
    OpenSSL's NEON code, on the same machine, at 3,156–3,174 MiB/s.
  - Poly1305 is about a third of XChaCha20-Poly1305's time there: 0.61 ms of 1.73 ms per MiB.
- **Design:**
  - A `Vector128Kernel` in `Poly1305Core`, in the AVX2 kernel's shape at 128 bits: a block
    to each 64-bit lane as five 26-bit limbs, two lanes to a vector.
  - Each product is one `UMULL` or `UMLAL`, 32 × 32 → 64 bits, taking the multiplier's limb
    from a lane of another vector (`MultiplyBySelectedScalarWideningLower[AndAdd]`), so that
    a power of r and five times its upper limbs fill three vectors.
  - Each lane takes every second block and multiplies by r². Two groups go per step, as F1's
    `BlocksPaired` does: (h + m)·r⁴ + m′·r², whose second half does not wait on h. After the
    last group the lanes multiply by r² and r, and sum into the scalar state.
  - The powers are formed per run by the scalar multiply, r² and r⁴ alone, one or two
    multiplications, and cleared after it.
  - Dispatch selects the kernel from a threshold, and the paired loop from a second, measured
    on the N2 and the M1. The scalar loop keeps shorter runs and the blocks after the last
    whole group.
  - The measurement uses F7's workflow, dispatched on the branch. The crypto harness gains a
    case for each kernel at each length, which drives `Poly1305Core` with the kernel named,
    so one run places both thresholds; the MAC and AEAD cases at lengths either side of them
    check the choice in use.
- **Target:**
  - Poly1305 over 1 MiB on the N2 at least 1.5 times as fast as the scalar loop. OpenSSL
    reaches 1.9 times.
  - Per F7's rule, the kernel at least as fast as the scalar loop, on every machine
    measured, wherever dispatch selects it.
- **Tests:**
  - As F1's: each loop, driven explicitly, is held to the reference over seeded messages,
    piece-wise feeds, the largest limbs, runs after scalar blocks, and runs either side of
    each threshold.
  - The loops' entry points are checked for `NoInlining | AggressiveOptimization`.
  - Dispatch's ARM64 choices have tests of their own, and the SIMD-off assembly checks the
    scalar loop at the new thresholds.
  - The ARM64 job runs them natively, and qemu in development.
- **Constant time:** the only branches are on the message length, which is public.

---

## 4. Order and method

1. **F7 first, or alongside F1.** It needs no library code, and its results shape the
   AdvSimd halves of F1 and F3.
2. **F1, then F2,** which reuses F1's measurements.
3. **F3, F4, F5 and F6,** in that order.
4. **F8, after F7,** whose run found its gain and whose workflow measures it.

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

Figures are from the `--crypto-harness` of `Bodu.Security.Cryptography.Benchmarks`. F1's and F2's
are from the same 4-vCPU Xeon VM as §1, at 2.8 GHz with AVX-512F/VL but no IFMA; F3's section
describes the machine the work moved to, where F4's, F5's and F6's were measured as well; F7's and F8's come from
GitHub's hosted runners, as F7's section describes. Each is the range over
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

### F2 — the short AEAD message (done)

Before and after, on both runtimes, in the harness's five processor configurations.
*Change* is the ratio of the two ranges' midpoints, above 1 where F2 is faster. The BCL
rows run OpenSSL's code, which F2 does not touch, so they show how far the runs drift on
their own. Messages of up to 960 bytes can now be drawn in one pass. XChaCha20-Poly1305
at 0 bytes, at 512 with AVX-512VL, and at 1 KiB and 16 KiB, whose blocks fill whole
groups, and every message without vector code are drawn as before.

| Measure (net10.0) | Before F2 | After F2 | Change |
|---|---|---|---|
| XChaCha20-Poly1305 64 B / 128 B, this host's default (AVX-512VL) | 0.79–0.82 µs / 1.09–1.17 µs | 0.57–0.70 µs / 0.67–0.68 µs | 1.27× / 1.67× |
| XChaCha20-Poly1305 192 B / 256 B, this host's default (AVX-512VL) | 1.28–1.51 µs / 0.92–1.06 µs | 0.70 µs / 0.77–0.80 µs | 1.99× / 1.26× |
| XChaCha20-Poly1305 448 B / 960 B, this host's default (AVX-512VL) | 1.66–1.79 µs / 2.02–2.26 µs | 0.97–1.12 µs / 1.24–1.27 µs | 1.65× / 1.71× |
| XChaCha20-Poly1305 0 B / 512 B, this host's default (AVX-512VL) | 0.53–0.56 µs / 1.05–1.07 µs | 0.54–0.55 µs / 1.10–1.18 µs | 1.00× / 0.93× |
| XSalsa20-Poly1305 64 B / 256 B, this host's default (AVX-512VL) | 0.63–0.73 µs / 1.33–1.38 µs | 0.55–0.64 µs / 0.76–0.81 µs | 1.14× / 1.73× |
| XSalsa20-Poly1305 512 B / 960 B, this host's default (AVX-512VL) | 1.73–1.74 µs / 2.05–2.08 µs | 1.04–1.09 µs / 1.20–1.32 µs | 1.63× / 1.64× |
| XChaCha20-Poly1305 64 B / 448 B, 512-bit vectors (`DOTNET_PreferredVectorBitWidth=512`) | 1.01–1.06 µs / 1.94–2.14 µs | 0.72–0.79 µs / 1.14–1.37 µs | 1.37× / 1.63× |
| XChaCha20-Poly1305 128 B / 448 B, AVX2 alone | 1.03–1.18 µs / 1.84–1.91 µs | 0.96–1.00 µs / 1.16–1.33 µs | 1.13× / 1.51× |
| XSalsa20-Poly1305 128 B / 448 B, AVX2 alone | 0.97–1.04 µs / 1.83–2.02 µs | 0.90–0.91 µs / 1.25–1.41 µs | 1.11× / 1.45× |
| XChaCha20-Poly1305 64 B / 448 B, SSSE3 (AVX2 disabled) | 0.89 µs / 2.06–2.18 µs | 0.87–0.92 µs / 1.66–1.68 µs | 0.99× / 1.27× |
| XChaCha20-Poly1305 64 B / 448 B, without vector code | 0.95–1.04 µs / 2.59–2.69 µs | 0.88–0.90 µs / 2.51–2.60 µs | 1.12× / 1.03× |
| XChaCha20-Poly1305 1 KiB / 16 KiB, this host's default (AVX-512VL) | 622–782 MiB/s / 1,500–1,573 MiB/s | 680–724 MiB/s / 1,641–1,653 MiB/s | 1.00× / 1.07× |
| BCL ChaCha20-Poly1305 (OpenSSL) 64 B / 1 KiB, this host's default (AVX-512VL), for reference | 1.31–1.50 µs / 2.12 µs | 1.30–1.42 µs / 1.91–1.99 µs | 1.03× / 1.09× |

| Measure (net8.0) | Before F2 | After F2 | Change |
|---|---|---|---|
| XChaCha20-Poly1305 64 B / 128 B, this host's default (AVX-512VL) | 0.86–0.90 µs / 1.12–1.25 µs | 0.63–0.70 µs / 0.69–0.75 µs | 1.32× / 1.65× |
| XChaCha20-Poly1305 192 B / 256 B, this host's default (AVX-512VL) | 1.40–1.42 µs / 0.99 µs | 0.73–0.94 µs / 0.82–0.85 µs | 1.69× / 1.19× |
| XChaCha20-Poly1305 448 B / 960 B, this host's default (AVX-512VL) | 1.70–1.77 µs / 2.26–2.33 µs | 1.00–1.06 µs / 1.35–1.37 µs | 1.68× / 1.69× |
| XChaCha20-Poly1305 0 B / 512 B, this host's default (AVX-512VL) | 0.58–0.60 µs / 1.11–1.19 µs | 0.58–0.59 µs / 1.15–1.17 µs | 1.01× / 0.99× |
| XSalsa20-Poly1305 64 B / 256 B, this host's default (AVX-512VL) | 0.74–0.87 µs / 1.36–1.54 µs | 0.59–0.64 µs / 0.74–0.78 µs | 1.31× / 1.91× |
| XSalsa20-Poly1305 512 B / 960 B, this host's default (AVX-512VL) | 1.84–2.02 µs / 1.80–2.03 µs | 1.15 µs / 1.33–1.42 µs | 1.68× / 1.39× |
| XChaCha20-Poly1305 64 B / 448 B, 512-bit vectors (`DOTNET_PreferredVectorBitWidth=512`) | 0.99–1.00 µs / 2.04–2.19 µs | 0.72–0.78 µs / 1.21–1.27 µs | 1.33× / 1.71× |
| XChaCha20-Poly1305 128 B / 448 B, AVX2 alone | 1.13–1.19 µs / 1.83–1.94 µs | 0.85–1.09 µs / 1.22–1.23 µs | 1.20× / 1.54× |
| XSalsa20-Poly1305 128 B / 448 B, AVX2 alone | 1.02–1.05 µs / 1.94–1.98 µs | 1.05–1.08 µs / 1.38–1.47 µs | 0.97× / 1.38× |
| XChaCha20-Poly1305 64 B / 448 B, SSSE3 (AVX2 disabled) | 0.85–0.90 µs / 1.96–2.08 µs | 0.87–1.09 µs / 1.58 µs | 0.89× / 1.28× |
| XChaCha20-Poly1305 64 B / 448 B, without vector code | 0.89 µs / 2.47–2.67 µs | 0.91–0.95 µs / 2.46–2.65 µs | 0.96× / 1.01× |
| XChaCha20-Poly1305 1 KiB / 16 KiB, this host's default (AVX-512VL) | 679–692 MiB/s / 1,476–1,643 MiB/s | 616–693 MiB/s / 1,525–1,559 MiB/s | 0.95× / 0.99× |
| BCL ChaCha20-Poly1305 (OpenSSL) 64 B / 1 KiB, this host's default (AVX-512VL), for reference | 1.42–1.44 µs / 2.23–2.28 µs | 1.33–1.41 µs / 1.98–2.02 µs | 1.04× / 1.13× |

The harness measures 64 bytes first in each process, before tiering has always settled,
so that row was also taken from alternating A/B runs of the two builds in separate
processes. XChaCha20-Poly1305 seals a 64-byte message in 546–577 ns on .NET 10, over
eleven rounds, against 746–810 before, which meets the target of 0.6 µs; on .NET 8 it
takes 597–630 ns, over six rounds, against 805–900, just above it. The same runs hold the
lengths whose draws F2 leaves as they were to their old cost: with AVX-512VL, 0 bytes at
0.95–1.02× and the 512-byte messages that tie at 0.98–1.03×; without it, 64, 512 and
1,100 bytes at 0.95–1.02× over five rounds, except one secretbox row at 1.09× on .NET 8
that measured 0.94× when 1,100 bytes was the only length its process ran. That, and the
0.93× the harness shows at 512 bytes, is dynamic PGO. The framing is compiled once for
both of its paths and laid out for the one the process has run most, so a message drawn
as it comes costs a little more in a process that has mostly drawn in one pass:
XChaCha20-Poly1305 at 512 bytes on .NET 10 measured 0.99× alone, 1.03× after 64-byte
messages, and 0.93–1.02× in the harness, which runs most one-pass lengths first. Giving
each path a method of its own was tried; it cost one-pass messages 1–5% and did not
remove the effect, so the paths share one method. The .NET 8 row with AVX2 disabled at
64 bytes is one run with a gen-2 collection in it (1.09 µs); the other ran 0.87 µs.

These figures were taken with F2 on ef973018. Master has since routed the keystream
functions' guards through `ThrowHelper` (0e73d4b0); alternating runs of both builds, with
and without F2, measured that change at 0.96–1.02× at 0, 64, 512 and 1,100 bytes on both
runtimes, and F2 on the new master within the same rounds' spread of F2 on ef973018.

How it was done, and where it departs from the design above:

- **The whole short keystream in one pass, where it costs less.** The design drew the
  Poly1305 key block and the first three message blocks in one four-block call, for
  messages of up to 192 bytes. The framings instead plan each message's draws. By
  default a message draws its keystream as it comes: the key block, then its whole blocks
  in one `XorBlocks` call, then its partial last block. Where it is estimated to cost
  less, all of its keystream, the key block included, is drawn in one run of up to
  sixteen blocks through a buffer on the stack, so a message of up to 960 bytes under
  RFC 8439, or 992 under secretbox, can take one or two kernel steps. The run is rounded
  up past the blocks the message needs where a whole kernel group makes it cheaper, and
  the keystream it does not use is discarded. A longer message's blocks after its last
  whole group of four pass through the buffer in one step the same way, which ends the
  unevenness F1 left for later: XChaCha20-Poly1305 at 448 bytes no longer costs more
  than at 512.
- **Why the plan is estimated rather than fixed.** The first version rounded every run
  up to a group of four and took the one pass whenever a message fit. Alternating A/B
  runs measured it 3% slower for an empty XChaCha20-Poly1305 message, 9% for an empty
  XSalsa20-Poly1305 one, and 5–7% slower for XChaCha20-Poly1305 at exactly 512 bytes.
  In each case the one pass takes as many kernel steps as drawing as it comes, and adds
  the copies through the buffer. Nor is a kernel step always worth one block. Measured
  against the block function over one block:

  | Step, against the block function over one block (ChaCha20 / Salsa20) | .NET 10 | .NET 8 |
  |---|---|---|
  | 4 or 8 blocks, AVX-512VL | 0.95–0.97 / 1.11–1.14 | 0.93–0.95 / 1.09–1.16 |
  | 4 or 8 blocks, 512-bit vectors | 0.95–0.98 / 1.02–1.06 | 0.96–0.98 / 1.07–1.10 |
  | 16 blocks, 512-bit vectors | 1.43 / 1.49 | 1.40 / 1.56 |
  | 4 or 8 blocks, AVX2 alone | 1.77–2.04 / 2.33–2.73 | 1.88–1.95 / 3.15–3.47 |
  | 4 blocks, SSSE3 (`DOTNET_EnableAVX=0`) | 1.69 / 1.96 | 1.97 / 2.45 |

  `ChaCha20Core.StepCost` rounds these to halves of a block: 2 for the block function,
  2 for four or eight blocks with AVX-512VL and 3 for sixteen, and 4 for four or eight
  blocks on AVX2, SSSE3 and, unmeasured, ARM64's AdvSimd. `CostFor` sums a run's steps
  as the kernel divides it. `Poly1305AeadCore.PlanOnePass` takes the one pass only where
  its estimate is strictly lower than drawing as it comes, which copies nothing. On the
  block function every way costs the same, so a keystream there, and an engine, which
  reports the scalar kernel, is always drawn exactly as before.
- **The plan is kept per kernel.** Planning a message afresh took 75–140 ns, about half
  a block, so `OnePassBlocks` computes the plan for every length up to 1 KiB of
  keystream on a kernel's first message and keeps it, published with
  `Interlocked.CompareExchange`. A lookup takes a few instructions.
- **Generated code, on both runtimes.**
  - With AVX-512, .NET 10's `SealRfc8439` grew from 2,989 to 3,376 bytes of Tier1 code
    and `OpenRfc8439` from 3,486 to 3,927, making the same calls into Poly1305 as
    before: the plan lookup compiles to a few instructions at a constant index, and
    nothing that was inlined stopped being inlined.
  - Without AVX-512, .NET 10 did not inline `LanesFor` into `XorKeystream`, so every
    message drawn as it comes called it twice and divided by the group's width, and
    XChaCha20-Poly1305 ran 6–8% slower at 64 bytes, where its draws had not changed.
    `LanesFor`, `StepCost` and the plan's per-message helpers are now
    `AggressiveInlining`, and for each keystream's kernel, a constant when the framing
    is compiled, the decisions fold away. .NET 8 inlined `LanesFor` but still divided,
    not folding the width into the divisor, so the bytes after the whole groups are
    now taken with a mask. After both changes the 64-byte message is back to its old
    cost without AVX-512: medians of five alternating rounds lie between 0.97× and
    1.02× on both runtimes.
  - The run buffer is zeroed when it is allocated, as every stack buffer in the
    framings is: .NET 10 zeroes it inline and .NET 8 through `Buffer._ZeroMemory`, at a
    cost that grows with the run. `[SkipLocalsInit]` would save that, but an engine
    from a derived `Poly1305AeadTransform` fills the same buffers, and one that left a
    block unfilled would then leak whatever the stack held into the keystream, so the
    zeroing stays.
- **Tests.**
  - A `RecordingKeystream` reports whichever kernel a test names, so every kernel's
    plan runs through the four framings on any processor. At every message length to
    1,299 bytes, and 4,099, it holds the output to the engine's, the draws to the cost
    the plan estimated for them, and every departure from drawing as it comes to a lower
    estimate; on the block function that leaves the draws exactly as they were. Three
    mutations of the planner — one pass on a tie, the rest's step left out of the
    estimate, the rest never rounded — each fail those tests.
  - `StepCost`, `CostFor`, `CheapestRunBlocks`, `KeystreamCost` and `PlanOnePass` are
    pinned at chosen values; `CheapestRunBlocks` is held to every longer run up to
    sixteen blocks, and the plan kept for each kernel to planning afresh. The SIMD-off
    assembly asserts that no message is drawn in one pass there.
  - A tampered tag leaves the output unwritten for messages drawn in one pass and for
    longer ones, and exact in-place encryption matches a separate buffer's for both.
  - The full suite passes on both runtimes, and the AEAD, ChaCha20 and Salsa20 groups
    also without AVX-512 and without AVX2.
  - Under qemu on ARM64, where the keystreams plan for AdvSimd, the plan and keystream
    tests pass, and seal-and-open round trips through all three AEADs at every length to
    1,299 bytes, and 4,099, produce byte for byte the messages x64 does with and without
    vector code, and the library did before F2.
- **Left for later.**
  - HChaCha20 and HSalsa20 are now the largest fixed cost of a short message: about
    170 ns of the 575 a 64-byte XChaCha20-Poly1305 message takes on .NET 10, computed one
    block at a time because they derive the key the other blocks use. A one-block vector
    kernel, as BLAKE2s has, could shorten that.
  - Salsa20's AVX2 and SSSE3 kernels take 3.1–3.5 blocks' time per step on .NET 8,
    against 2.3–2.7 on .NET 10. The estimates follow ChaCha20, so on .NET 8 without
    AVX-512 a three-block XSalsa20-Poly1305 message takes one step where three calls of
    the block function would be about 3% faster (0.97× at 128 bytes). Finding why .NET 8
    compiles those kernels slower would remove that and speed up long XSalsa20 messages
    on such processors.
  - The ARM64 estimate is assumed, not measured: F7 should time the AdvSimd kernel's
    steps against the block function and correct `StepCost` if they differ.

### F3 — ML-DSA and ML-KEM vector transforms, and a four-way SHAKE (done)

F3's figures come from a second machine. Part-way through F3 the work moved from §1's VM to a
4-vCPU Xeon VM at 2.1 GHz (family 6, model 207) with AVX-512F/VL/BW/DQ, IFMA, VBMI and FP16,
which runs every row here faster than the first did. Its runtimes prefer 512-bit vectors by
default, so this host's default and `DOTNET_PreferredVectorBitWidth=512` are the same
configuration and show how far two runs of it drift. The before build is master at 427b804a
with F3's harness rows added; the after build is F3 as merged (#728). Both ran on the second
machine, alternately, in each of the harness's five processor configurations on both
runtimes, in two runs, the second in the opposite order. *Change* is the ratio of the two
ranges' midpoints, above 1 where F3 is faster.

| Operation (net10.0, this host's default) | Before F3 (µs) | After F3 (µs) | Change |
|---|---|---|---|
| ML-KEM-512 key generation | 31.7–32.1 | 17.4–17.9 | 1.81× |
| ML-KEM-512 encapsulate | 20.6–21.3 | 10.0–10.6 | 2.04× |
| ML-KEM-512 decapsulate | 29.1–30.3 | 15.4–15.6 | 1.91× |
| ML-KEM-512 encapsulate to an imported key | 34.3–35.9 | 17.9–18.2 | 1.94× |
| ML-KEM-768 key generation | 50.5–50.6 | 28.3–29.4 | 1.75× |
| ML-KEM-768 encapsulate | 27.4–29.4 | 13.5 | 2.10× |
| ML-KEM-768 decapsulate | 43.1–44.5 | 19.5–20.2 | 2.21× |
| ML-KEM-768 encapsulate to an imported key | 54.2–67.8 | 27.9–28.6 | 2.16× |
| ML-KEM-1024 key generation | 84.8–86.6 | 41.7 | 2.05× |
| ML-KEM-1024 encapsulate | 39.3–44.0 | 18.5–19.3 | 2.21× |
| ML-KEM-1024 decapsulate | 57.0–58.7 | 27.3–27.7 | 2.10× |
| ML-KEM-1024 encapsulate to an imported key | 83.0–87.2 | 39.1–41.6 | 2.11× |
| ML-DSA-44 key generation | 109–120 | 51.9–53.4 | 2.17× |
| ML-DSA-44 sign | 339–369 | 98–113 | 3.36× |
| ML-DSA-44 verify | 32.3–32.6 | 16.7–16.9 | 1.93× |
| ML-DSA-44 verify with an imported key | 106–111 | 43.0–46.4 | 2.43× |
| ML-DSA-65 key generation | 243–434 | 105 | 3.23× |
| ML-DSA-65 sign | 579–581 | 156–168 | 3.58× |
| ML-DSA-65 verify | 45.9–55.3 | 20.7–22.8 | 2.32× |
| ML-DSA-65 verify with an imported key | 164–174 | 72.6–80.0 | 2.22× |
| ML-DSA-87 key generation | 331–346 | 125–126 | 2.70× |
| ML-DSA-87 sign | 718–812 | 160–173 | 4.60× |
| ML-DSA-87 verify | 58.7–72.1 | 30.4–32.6 | 2.08× |
| ML-DSA-87 verify with an imported key | 271–274 | 115–124 | 2.28× |

| Operation (net8.0, this host's default) | Before F3 (µs) | After F3 (µs) | Change |
|---|---|---|---|
| ML-KEM-512 key generation | 32.6–36.5 | 19.6–20.1 | 1.74× |
| ML-KEM-512 encapsulate | 24.5–24.7 | 11.1–11.2 | 2.21× |
| ML-KEM-512 decapsulate | 33.9–34.1 | 16.8–19.3 | 1.88× |
| ML-KEM-512 encapsulate to an imported key | 38.9–40.6 | 19.5–21.1 | 1.96× |
| ML-KEM-768 key generation | 54.0–57.5 | 32.2–35.0 | 1.66× |
| ML-KEM-768 encapsulate | 31.7–32.4 | 13.2–15.6 | 2.22× |
| ML-KEM-768 decapsulate | 46.9–48.1 | 22.0–24.4 | 2.05× |
| ML-KEM-768 encapsulate to an imported key | 61.9–62.0 | 31.0–31.5 | 1.98× |
| ML-KEM-1024 key generation | 90.3–91.7 | 43.4–46.4 | 2.03× |
| ML-KEM-1024 encapsulate | 46.2–46.4 | 19.4–20.4 | 2.32× |
| ML-KEM-1024 decapsulate | 63.5–66.1 | 28.8–30.0 | 2.20× |
| ML-KEM-1024 encapsulate to an imported key | 90.2–90.3 | 41.2–41.3 | 2.19× |
| ML-DSA-44 key generation | 133–139 | 57.9–58.9 | 2.32× |
| ML-DSA-44 sign | 395–404 | 102–110 | 3.78× |
| ML-DSA-44 verify | 39.9–45.0 | 18.4–19.6 | 2.24× |
| ML-DSA-44 verify with an imported key | 115–132 | 46.3–47.7 | 2.62× |
| ML-DSA-65 key generation | 243–253 | 108–110 | 2.27× |
| ML-DSA-65 sign | 607–650 | 168–172 | 3.69× |
| ML-DSA-65 verify | 50.8–54.3 | 24.2–25.1 | 2.13× |
| ML-DSA-65 verify with an imported key | 174–197 | 78.9–86.6 | 2.24× |
| ML-DSA-87 key generation | 336–360 | 134–139 | 2.55× |
| ML-DSA-87 sign | 674–697 | 155–167 | 4.25× |
| ML-DSA-87 verify | 72.0–78.6 | 31.7 | 2.38× |
| ML-DSA-87 verify with an imported key | 281–297 | 131–132 | 2.20× |

The ML-DSA-65 key generation range before F3 on .NET 10, 243–434 µs, holds one outlying run.
Alternating A/B runs of the three target rows, five rounds of each build in separate
processes, put it at 238–266 µs:

| Operation | Configuration | Runtime | Before F3 (µs) | After F3 (µs) | Change (medians) |
|---|---|---|---|---|---|
| ML-DSA-65 key generation | this host's default | net10.0 | 238–266 | 95–115 | 2.49× |
| ML-DSA-65 key generation | this host's default | net8.0 | 242–311 | 107–120 | 2.26× |
| ML-DSA-65 key generation | no vector instructions | net10.0 | 235–270 | 227–260 | 1.08× |
| ML-DSA-65 key generation | no vector instructions | net8.0 | 250–292 | 231–242 | 1.12× |
| ML-DSA-65 sign | this host's default | net10.0 | 593–690 | 151–167 | 3.91× |
| ML-DSA-65 sign | this host's default | net8.0 | 660–718 | 155–175 | 4.38× |
| ML-DSA-65 sign | no vector instructions | net10.0 | 566–794 | 509–621 | 1.15× |
| ML-DSA-65 sign | no vector instructions | net8.0 | 624–712 | 503–720 | 1.26× |
| ML-KEM-768 decapsulate | this host's default | net10.0 | 41.7–48.0 | 21.9–31.4 | 1.87× |
| ML-KEM-768 decapsulate | this host's default | net8.0 | 44.3–54.1 | 22.6–27.0 | 2.09× |
| ML-KEM-768 decapsulate | no vector instructions | net10.0 | 43.0–52.7 | 45.4–48.5 | 0.98× |
| ML-KEM-768 decapsulate | no vector instructions | net8.0 | 46.6–57.6 | 46.3–56.3 | 1.00× |

The plan's targets are met with a wide margin. They were set on the first machine: 0.45 ms,
200 µs and 45 µs against 1.1.0's 0.79–0.85 ms, 302–328 µs and 60–80 µs. On that machine, during
development, the F3 build signed with ML-DSA-65 in 258–281 µs, generated an ML-DSA-65 key in
172–186 µs and decapsulated with ML-KEM-768 in 38–41 µs. Scaled to the before figures here,
the targets come to about 350 µs, 160 µs and 29 µs, against 151–175 µs, 95–120 µs and 19.5–24.4 µs.

The three targets in each configuration:

| net10.0 | ML-DSA-65 sign | ML-DSA-65 key generation | ML-KEM-768 decapsulate |
|---|---|---|---|
| this host's default | 579–581 → 156–168 (3.58×) | 243–434 → 105 (3.23×) | 43.1–44.5 → 19.5–20.2 (2.21×) |
| `DOTNET_PreferredVectorBitWidth=512` | 593–602 → 150–163 (3.82×) | 247–250 → 100 (2.48×) | 41.7–42.4 → 19.2–20.7 (2.11×) |
| AVX-512 off | 577–596 → 183–208 (3.00×) | 235–256 → 132–144 (1.78×) | 41.3–42.8 → 21.5–22.6 (1.91×) |
| AVX2 off | 626–637 → 479–546 (1.23×) | 228–238 → 221–227 (1.04×) | 43.7–50.7 → 43.5–44.5 (1.07×) |
| no vector instructions | 621–707 → 536–545 (1.23×) | 238–268 → 231 (1.09×) | 45.8–46.0 → 44.2–48.7 (0.99×) |

| net8.0 | ML-DSA-65 sign | ML-DSA-65 key generation | ML-KEM-768 decapsulate |
|---|---|---|---|
| this host's default | 607–650 → 168–172 (3.69×) | 243–253 → 108–110 (2.27×) | 46.9–48.1 → 22.0–24.4 (2.05×) |
| `DOTNET_PreferredVectorBitWidth=512` | 626–665 → 162–163 (3.97×) | 246 → 107–135 (2.03×) | 45.2–54.3 → 21.9–22.2 (2.26×) |
| AVX-512 off | 634–717 → 204–207 (3.28×) | 239–251 → 138–163 (1.62×) | 46.0–49.6 → 23.8–26.4 (1.90×) |
| AVX2 off | 679–739 → 526–599 (1.26×) | 246–256 → 216–249 (1.08×) | 47.2–55.7 → 47.6–48.4 (1.07×) |
| no vector instructions | 670–742 → 517–637 (1.22×) | 244–256 → 221–231 (1.11×) | 46.5–50.9 → 49.0–50.6 (0.98×) |

Every row's change, in every configuration:

| Operation (net10.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| ML-KEM-512 key generation | 1.81× | 1.81× | 1.52× | 1.04× | 0.91× |
| ML-KEM-512 encapsulate | 2.04× | 2.01× | 1.90× | 0.99× | 0.97× |
| ML-KEM-512 decapsulate | 1.91× | 2.02× | 1.71× | 1.02× | 1.03× |
| ML-KEM-512 encapsulate to an imported key | 1.94× | 1.91× | 1.78× | 0.95× | 0.90× |
| ML-KEM-768 key generation | 1.75× | 1.96× | 1.42× | 0.90× | 0.88× |
| ML-KEM-768 encapsulate | 2.10× | 2.20× | 2.00× | 1.09× | 0.97× |
| ML-KEM-768 decapsulate | 2.21× | 2.11× | 1.91× | 1.07× | 0.99× |
| ML-KEM-768 encapsulate to an imported key | 2.16× | 1.90× | 1.62× | 1.00× | 1.00× |
| ML-KEM-1024 key generation | 2.05× | 1.83× | 1.64× | 1.04× | 0.98× |
| ML-KEM-1024 encapsulate | 2.21× | 2.37× | 2.12× | 1.03× | 1.14× |
| ML-KEM-1024 decapsulate | 2.10× | 2.23× | 1.89× | 0.95× | 0.94× |
| ML-KEM-1024 encapsulate to an imported key | 2.11× | 2.20× | 1.62× | 1.03× | 0.98× |
| ML-DSA-44 key generation | 2.17× | 2.20× | 1.75× | 1.12× | 1.04× |
| ML-DSA-44 sign | 3.36× | 3.56× | 2.69× | 1.18× | 1.16× |
| ML-DSA-44 verify | 1.93× | 2.10× | 1.88× | 0.99× | 1.02× |
| ML-DSA-44 verify with an imported key | 2.43× | 2.59× | 1.73× | 0.98× | 1.06× |
| ML-DSA-65 key generation | 3.23× | 2.48× | 1.78× | 1.04× | 1.09× |
| ML-DSA-65 sign | 3.58× | 3.82× | 3.00× | 1.23× | 1.23× |
| ML-DSA-65 verify | 2.32× | 1.97× | 2.08× | 1.02× | 1.05× |
| ML-DSA-65 verify with an imported key | 2.22× | 2.68× | 1.91× | 1.01× | 1.02× |
| ML-DSA-87 key generation | 2.70× | 2.48× | 2.07× | 0.94× | 1.14× |
| ML-DSA-87 sign | 4.60× | 3.99× | 3.64× | 1.25× | 1.22× |
| ML-DSA-87 verify | 2.08× | 1.99× | 2.01× | 0.90× | 0.94× |
| ML-DSA-87 verify with an imported key | 2.28× | 2.46× | 1.88× | 1.02× | 0.91× |

| Operation (net8.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| ML-KEM-512 key generation | 1.74× | 1.85× | 1.53× | 1.03× | 1.07× |
| ML-KEM-512 encapsulate | 2.21× | 2.01× | 1.82× | 1.12× | 0.94× |
| ML-KEM-512 decapsulate | 1.88× | 2.02× | 1.72× | 1.02× | 1.03× |
| ML-KEM-512 encapsulate to an imported key | 1.96× | 2.08× | 1.63× | 0.96× | 1.17× |
| ML-KEM-768 key generation | 1.66× | 1.84× | 1.38× | 0.96× | 1.07× |
| ML-KEM-768 encapsulate | 2.22× | 2.36× | 2.02× | 1.09× | 1.01× |
| ML-KEM-768 decapsulate | 2.05× | 2.26× | 1.90× | 1.07× | 0.98× |
| ML-KEM-768 encapsulate to an imported key | 1.98× | 2.06× | 1.63× | 1.07× | 0.96× |
| ML-KEM-1024 key generation | 2.03× | 1.88× | 1.57× | 1.11× | 1.13× |
| ML-KEM-1024 encapsulate | 2.32× | 2.17× | 2.10× | 1.07× | 0.96× |
| ML-KEM-1024 decapsulate | 2.20× | 2.01× | 1.93× | 1.07× | 0.92× |
| ML-KEM-1024 encapsulate to an imported key | 2.19× | 2.11× | 1.72× | 1.00× | 1.23× |
| ML-DSA-44 key generation | 2.32× | 2.37× | 1.71× | 0.97× | 1.07× |
| ML-DSA-44 sign | 3.78× | 3.59× | 3.20× | 1.29× | 1.30× |
| ML-DSA-44 verify | 2.24× | 2.06× | 1.96× | 1.02× | 1.19× |
| ML-DSA-44 verify with an imported key | 2.62× | 2.29× | 1.78× | 1.02× | 1.11× |
| ML-DSA-65 key generation | 2.27× | 2.03× | 1.62× | 1.08× | 1.11× |
| ML-DSA-65 sign | 3.69× | 3.97× | 3.28× | 1.26× | 1.22× |
| ML-DSA-65 verify | 2.13× | 2.04× | 2.18× | 1.07× | 0.96× |
| ML-DSA-65 verify with an imported key | 2.24× | 2.35× | 1.72× | 1.03× | 1.01× |
| ML-DSA-87 key generation | 2.55× | 2.46× | 1.79× | 1.14× | 1.14× |
| ML-DSA-87 sign | 4.25× | 3.82× | 3.62× | 1.34× | 1.28× |
| ML-DSA-87 verify | 2.38× | 1.98× | 2.43× | 1.08× | 1.14× |
| ML-DSA-87 verify with an imported key | 2.20× | 2.64× | 1.76× | 1.05× | 1.01× |

Without AVX2, dispatch selects the scalar code. ML-DSA signing still runs 1.15–1.34× faster
there, and key generation 0.94–1.14×, from the scalar twins of the polynomial passes described
below. The ML-KEM rows scatter from 0.88× to 1.23× between the two runs. Alternating runs, three
rounds of each build, of ML-KEM-768 key generation and its parts through the engine measured the
whole operation at 0.99–1.03× on both runtimes, and the A/B runs above put decapsulation at
0.98–1.00×. Of the parts, only the scalar base-case product measured slower, on .NET 10 with AVX2
disabled: 379 ns against 284, about 2% of a key generation.

How it was done, and where it departs from the design above:

- **ML-DSA's kernel.** `MLDsaEngine.Vector256Kernel` works on eight `int` coefficients per
  vector, as the design and the reference implementation's AVX2 code do.
  - Each Montgomery product multiplies the even and the odd lanes with `vpmuldq`, forms each
    lane's multiple m from a second product with ζ·q⁻¹, precomputed for every twiddle, and
    subtracts the high halves of m·q from those of a·ζ: `MontgomeryReduce` bit for bit.
  - The forward transform makes two passes over memory: the first three layers on eight
    vectors 32 coefficients apart, held in registers, and the other five on each block of 32
    coefficients, the last three after an in-register rearrangement that reads lane-ordered
    twiddle tables. The inverse runs the same passes in the opposite order.
  - Every layer adds, subtracts and reduces in the scalar code's order, so the output equals
    the scalar code's exactly, not only modulo q.
  - In the spike, on the second machine, a forward transform took 346–374 ns against
    1,226–1,461 scalar, an inverse 325–328 against 1,331–1,384, and a pointwise product
    53–74 ns against 231–249.
- **ML-KEM's kernel: sixteen 16-bit lanes over a copy.** The design left open whether to keep
  eight `int` lanes, and the layout, or take the reference's sixteen 16-bit lanes, which it
  expected to need 16-bit storage throughout. The spike measured the forward transform at
  376–402 ns with eight `int` lanes and 149–162 ns with sixteen 16-bit ones, against
  1,285–1,323 ns scalar. Every value the scalar code forms fits in 16 bits: its transforms
  keep each coefficient below 8q = 26,632 in magnitude. So `MLKemEngine.Vector256Kernel` packs
  the `int` coefficients into a 16-bit copy on the stack as it loads them (`vpackssdw`, then
  `vpermq` to restore the order), and sign-extends them as it stores them (`vpmovsxwd`).
  Storage stays `int` everywhere else, and the copy is cleared before the kernel returns.
  Montgomery products use `vpmullw` and `vpmulhw`, and Barrett reduction takes its quotient
  from `vpmulhw` by 20,159, rounded and shifted exactly as `BarrettReduce` does. Four layers run
  over the whole copy and the last three on each block of 32, after the in-register
  rearrangement; the base-case products take sixteen pairs per vector. The inverse transform
  took 152–161 ns against 1,675–1,777, and a product 91–93 ns against 346–410.
- **The four-way Keccak.** `KeccakPermutation.Permute4` advances four independent states, one
  per 64-bit lane of 25 `Vector256<ulong>`s, through `Vector256Kernel<TIsa>` over
  `VectorRotation.Avx512` or `.Avx2`, or as four scalar permutations.
  - Its rotations go through Bodu.Core, which gained 64-bit lanes for `Vector256`: `vprolq` on
    AVX-512VL, and on AVX2 a doubleword shuffle for 32 bits, an in-lane byte shuffle for the
    other multiples of 8, and a pair of shifts otherwise.
  - With AVX-512VL, θ's five-way parity takes two `vpternlogq` (0x96) and χ one (0xD2).
  - `KeccakSponge4` runs four SHAKE128 or SHAKE256 sponges over it. Each absorbs one message
    shorter than the rate, which every lattice stream's seed and index is, and squeezes whole
    blocks.
  - In the library the kernel permutes four states in 0.26–0.27 µs with AVX-512VL on both
    runtimes, and, with AVX-512 disabled, in 0.62 µs over AVX2 on .NET 10 and 0.77 on .NET 8,
    against 0.38–0.40 µs for one scalar permutation.
- **No eight-way Keccak, and no AVX-512 kernels.** The spike's eight-way permutation over
  `Vector512<ulong>` took 374–395 ns for eight states on the second machine, 47–49 ns each
  against the four-way kernel's 64. But it needs eight streams at once, which only matrix
  expansion supplies: ExpandMask draws four, five or seven per signing attempt, and ML-KEM's
  noise four to nine per operation. And on the first machine, which does not prefer 512-bit
  vectors, it gained only 20% per state. The ML-DSA pointwise product over 512-bit vectors
  took 42–56 ns against AVX2's 53–74, on a product that is a small share of signing, and the
  transforms would need a fourth in-register layer. Every kernel is 256-bit.
- **Where the four-way sponge serves.**
  - ML-DSA: `SampleMatrix` (ExpandA) when a key is set, `SampleSecretVector` (ExpandS) at key
    generation, and `ExpandMaskVector` (ExpandMask) on every signing attempt.
  - ML-KEM: `SampleMatrix` and `SampleNoiseVector`.
  - A batch of fewer than four streams gives its idle sponges a copy of an active one's
    message and never reads their output. Each batch squeezes until every stream in it is
    full, parsing each block as the one-stream code did, so the coefficients do not depend on
    the batching.
  - The four-way sponge runs only where `KeccakPermutation.IsFourWayAccelerated`, that is,
    with AVX2: four scalar permutations cost what one stream at a time does.
- **The per-coefficient signing passes are a finding, not the design.** With the transforms,
  products and expansions vectorized, ML-DSA-65 signing still took 511–581 µs on the first
  machine, above the target. `Sign` compiles differently from the methods it calls: its
  `stackalloc` buffers rule out on-stack replacement, so the JIT compiles it once, fully
  optimized but without the profile dynamic PGO collects (its disassembly reads
  `Tier0-FullOpts`), and that compilation left `HighBits`, `MakeHint` and `InfinityNorm` as
  calls on every coefficient. A profile of one ML-DSA-65 signing attempt put the pass that
  forms r₀ at 16 µs of the attempt's 73 on .NET 10, and the hint pass at 14, against half a
  microsecond for a transform. Each per-coefficient pass is now a method over whole
  polynomials, with an AVX2 kernel and a scalar twin: `HighBits`, `LowBitsNorm`, `MakeHints`,
  `InfinityNorm`, `AddModQ` and `SubtractModQ` for signing, and `ToMontgomery` and `Reduce32`
  for setting a key. They compile, and tier up, on their own, whatever `Sign` does. Signing
  then took 258–281 µs on the first machine. On the second, one attempt takes about 25 µs on
  both runtimes, of which ExpandMask is 5.7–6.7 µs and w1Encode with its SHAKE256 4.2.
- **Generated code, on both runtimes.**
  - **.NET 10 kept the AVX-512 Keccak kernel's state on the stack**, with 52 stack references
    in its round loop against .NET 8's 2. Two things caused it:
    - The helpers that choose between `vpternlogq` and plain operators were written with `?:`.
      Inlined, each left a join temporary that the register allocator would not keep in a
      register while 25 state vectors were live. In a scratch copy of the kernel on the
      second machine, .NET 10 ran that form in 449 ns and the same helpers written with
      `if` / `return` in 268; .NET 8 ran both in 210–264.
    - The kernel loaded the state and then, before its round loop, checked that
      `KeccakPermutation`'s static fields were initialized, calling the runtime's
      static-base helper if not: it read the round constants from a `static readonly` array.
      No vector register survives a call under the System V ABI, so the 25 state vectors,
      live across that call, were given stack homes for the whole loop. The constants are now
      a `ReadOnlySpan<ulong>` over the assembly's data, which needs no initialization. A
      build that keeps everything else but reads the constants from the array ran the
      AVX-512 kernel in 0.39 µs on .NET 10 against 0.26, and the four-way sponge's absorb and
      five squeezes in 2.33 µs against 1.49.
  - With both changes the AVX-512 kernel has 2 stack references in its loop on both runtimes.
    Over AVX2's sixteen registers the 25 state vectors cannot stay in registers, and the kernel
    spills on both runtimes whatever the round constants or the shifts.
  - **F3's change to Bodu.Core's shift pairs is undone.** On the first machine the four-way
    AVX2 kernel ran about twice as fast on .NET 10 as on .NET 8, and .NET 8 compiled the right
    half of every shift-pair rotation, `Vector256.ShiftRightLogical(value, 64 - count)`, to a
    count moved into a register on each call. F3 therefore wrote every shift pair, 32- and
    64-bit, over SSSE3, AVX2 and AdvSimd, with the instruction set's intrinsics,
    `X86.Avx2.ShiftRightLogical(value, (byte)(64 - count))`. That was measured in a process with
    AVX-512 enabled, where the AVX2 kernel runs only when a test names it. On the second machine
    the change proved a mistake:
    - .NET 8 does not fold `(byte)(32 - count)` into the intrinsic's immediate: it passes the
      count from memory. In the ChaCha20 and Salsa20 kernels that form spilled more. Over SSSE3
      the Salsa20 kernel grew from 2,987 to 3,387 bytes, with 251 stack references against 208.
      .NET 10 compiles the SSSE3 kernels identically in every build below.
    - Alternating runs of four builds, three rounds each, with AVX-512 or AVX2 disabled:

      | Row (MiB/s, median of three) | Configuration | Runtime | Before F3 | F3 as merged | 32-bit shifts portable | Literal immediates |
      |---|---|---|---|---|---|---|
      | ChaCha20 1 MiB | AVX2 alone | net8.0 | 1,740 | 1,462 (0.84×) | 1,541 (0.89×) | 1,482 (0.85×) |
      | ChaCha20 1 MiB | SSSE3 (AVX2 disabled) | net8.0 | 977 | 861 (0.88×) | 959 (0.98×) | 883 (0.90×) |
      | Salsa20 1 MiB | AVX2 alone | net8.0 | 943 | 858 (0.91×) | 1,047 (1.11×) | 749 (0.79×) |
      | Salsa20 1 MiB | SSSE3 (AVX2 disabled) | net8.0 | 716 | 589 (0.82×) | 704 (0.98×) | 559 (0.78×) |
      | Serpent-128-CTR 1 MiB | AVX2 alone | net8.0 | 357 | 419 (1.17×) | 370 (1.04×) | 417 (1.17×) |
      | Serpent-128-CTR 1 MiB | SSSE3 (AVX2 disabled) | net8.0 | 215 | 218 (1.01×) | 218 (1.01×) | 207 (0.96×) |
      | CubeHash 1 MiB | AVX2 alone | net8.0 | 359 | 366 (1.02×) | 357 (0.99×) | 359 (1.00×) |
      | CubeHash 1 MiB | AVX2 alone | net10.0 | 353 | 350 (0.99×) | 350 (0.99×) | 119 (0.34×) |
      | CubeHash 1 MiB | SSSE3 (AVX2 disabled) | net8.0 | 299 | 330 (1.11×) | 284 (0.95×) | 319 (1.07×) |
      | CubeHash 1 MiB | SSSE3 (AVX2 disabled) | net10.0 | 308 | 317 (1.03×) | 315 (1.02×) | 84 (0.27×) |

    - A switch over every count with literal immediates gave .NET 8 immediate shifts but the
      same spills, and cost CubeHash about two thirds of its speed on .NET 10, whose inliner gave
      up on the larger rotation. With the 64-bit shifts portable as well, the four-way Keccak
      over AVX2 took 0.79 µs on .NET 8 against 0.77, and .NET 10 compiled it identically.
    - So every shift pair is the portable operators again, as in 1.1.0. ChaCha20 and Salsa20 are
      back to their earlier speed on .NET 8, and F3's gains there for Serpent-128-CTR with AVX2
      alone (1.17×) and CubeHash over SSSE3 (1.11×) go with the change. ChaCha20 with AVX2 alone
      varies by about ±10% between runs of one build: the portable build compiles it as before
      F3 and still read 0.89×.
  - The ML-DSA and ML-KEM kernels compile with no calls in their loops on either runtime;
    ML-KEM's only calls, which clear its copy, follow them. Every kernel entry point is
    `NoInlining | AggressiveOptimization`, per §2, and a reflection test checks each.
- **Tests.**
  - Each kernel is driven explicitly through a `KernelKind` overload, whatever dispatch
    would pick, and held bit for bit to the scalar kernel and to `MLDsaReference` /
    `MLKemReference`, the remainder-operator code W9 replaced. The inputs are edge
    polynomials (zeros, q − 1 and −(q − 1) throughout, ML-KEM's twelve-bit maximum), each
    sign or value alternating in runs of every power of two from 1 to 128, which pair
    differently in each layer, and seeded polynomials, signed wherever the scalar code
    accepts signed input. A kernel the processor lacks reports inconclusive.
  - `HighBits` and `LowBitsNorm` are checked over every coefficient from 0 to q − 1 in the
    Regression tier, and at the decomposition's boundaries in BVT. `MakeHints`,
    `InfinityNorm`, `AddModQ`, `SubtractModQ`, `ToMontgomery` and `Reduce32` are checked over
    boundary and seeded polynomials, the norms with the largest value in every position, and
    in place.
  - The four-way samplers are held to one stream at a time over every batch remainder and
    across nonce and counter carries.
  - `Permute4` is held to the scalar permutation over seeded states and uniform but distinct
    ones, per kernel. `KeccakSponge4` is held to four scalar sponges at every message length
    below the rate and every split of up to three whole squeezed blocks, and to the FIPS 202
    SHAKE128 and SHAKE256 outputs for four published messages. The design asked for every
    squeeze length up to three rates; the sponge squeezes whole blocks only, because every
    sampler parses whole blocks.
  - Bodu.Core's 64-bit rotation is held to the scalar rotation for every count on each
    instruction set.
  - The SIMD-off assembly links the ML-KEM and ML-DSA ACVP suites and asserts that every one
    of these dispatches takes its scalar path. The repository holds no Wycheproof vectors
    for ML-KEM or ML-DSA, so the NIST ACVP vectors are the published ones run through both
    paths: key generation, encapsulation and decapsulation, key checks, and signature
    generation and verification.
  - The full suite passes on both runtimes, and the ML-DSA engine tests also with AVX2
    disabled.
- **ARM64 and SSSE3 keep the scalar lattice code.** The design's 128-bit kernel for SSSE3 and
  AdvSimd waits for F7, as F1's AdvSimd kernel does: nothing here can measure it on ARM64.
- **Left for later.**
  - Key generation now spends most of its time outside the transforms. Of ML-DSA-65's
    76–93 µs in the profiler on the second machine, the matrix's rejection sampling takes
    about 25, encoding the keys and hashing the public key 18–21, and keeping the key's
    values 8–10. Vectorized rejection parsing and bit packing are the next steps.
  - The eight-way Keccak would serve matrix expansion where 512-bit vectors are preferred,
    as they are on the second machine, at 47–49 ns per state against 64.
  - Verification gained 1.9–2.6×, less than signing: its `UseHint` pass is still per
    coefficient.
  - ML-KEM's 16-bit copy is zeroed when the JIT allocates it, as every stack buffer is, and
    cleared again after use; the kernel writes every element before reading any, so
    `[SkipLocalsInit]` could save the first.
  - On .NET 8 the intrinsic shifts that slowed ChaCha20 and Salsa20 ran Serpent-128-CTR with
    AVX2 alone 1.17× faster and CubeHash over SSSE3 1.11× faster. Stream-cipher kernels that do
    not spill on that form would let every kernel have it.
  - ML-KEM's scalar base-case product runs 0.75× on .NET 10 with AVX2 disabled, now that it
    shares a method with the dispatch to its kernel.

### F4 — Ed25519's fixed-base table and verification (done)

F4's figures come from the second machine, F3's. The before build is this branch at 95dc3aef, which
adds F4's harness rows to master at 6b341cd3 (#728) and the rotation change F3's section describes;
the after build is at 8c24adf5, with F4's code. Both ran alternately, in each of the harness's five
processor configurations on both runtimes, in two runs, the second in the opposite order. *Change*
is the ratio of the two ranges' midpoints, above 1 where F4 is faster; *allocated* is the harness's
managed bytes per operation.

| Operation (net10.0, this host's default) | Before F4 (µs) | After F4 (µs) | Change | Allocated before → after (B/op) |
|---|---|---|---|---|
| X25519 shared secret | 67.8–70.9 | 67.2–68.3 | 1.02× | 56 → 56 |
| X25519 key generation | 36.8–37.1 | 24.4–26.6 | 1.45× | 144 → 144 |
| Ed25519 sign | 36.3 | 29.8–31.0 | 1.19× | 224 → 88 |
| Ed25519 verify | 98.8–98.9 | 79.3–79.6 | 1.24× | 136 → 0 |
| Ed25519 sign into a span | 35.1–35.6 | 28.6–29.3 | 1.22× | 136 → 0 |
| Ed25519 sign 1 KiB into a span | 40.7–41.8 | 31.6–33.4 | 1.27× | 136 → 0 |
| Ed25519 verify 1 KiB | 108 | 77.2–78.9 | 1.38× | 136 → 0 |
| Ed25519 sign 16 KiB into a span | 87.5–89.2 | 80.0–83.7 | 1.08× | 136 → 136 |
| Ed25519 verify 16 KiB | 122–125 | 106–116 | 1.11× | 136 → 136 |

| Operation (net8.0, this host's default) | Before F4 (µs) | After F4 (µs) | Change | Allocated before → after (B/op) |
|---|---|---|---|---|
| X25519 shared secret | 68.2–71.0 | 64.9–71.5 | 1.02× | 56 → 56 |
| X25519 key generation | 33.1–36.1 | 26.6–28.6 | 1.25× | 144 → 144 |
| Ed25519 sign | 38.0–40.6 | 30.5–31.7 | 1.26× | 224 → 88 |
| Ed25519 verify | 97.9–98.1 | 76.9–82.5 | 1.23× | 136 → 0 |
| Ed25519 sign into a span | 37.1–40.3 | 30.4–31.4 | 1.25× | 136 → 0 |
| Ed25519 sign 1 KiB into a span | 43.0–48.8 | 32.9–34.7 | 1.36× | 136 → 0 |
| Ed25519 verify 1 KiB | 99–110 | 79.8–87.0 | 1.25× | 136 → 0 |
| Ed25519 sign 16 KiB into a span | 88.3–90.9 | 82.7–86.1 | 1.06× | 136 → 136 |
| Ed25519 verify 16 KiB | 122–132 | 104–107 | 1.21× | 136 → 136 |

This machine's runs vary by 10–15% between processes, so alternating A/B runs of the three target
rows, five rounds of each build in separate processes, give a steadier reading:

| Operation | Configuration | Runtime | Before F4 (µs) | After F4 (µs) | Change (medians) |
|---|---|---|---|---|---|
| X25519 key generation | this host's default | net10.0 | 34.4–37.7 | 25.0–28.2 | 1.33× |
| X25519 key generation | this host's default | net8.0 | 34.5–38.7 | 25.5–27.5 | 1.37× |
| X25519 key generation | no vector instructions | net10.0 | 56.4–62.0 | 46.4–51.6 | 1.21× |
| X25519 key generation | no vector instructions | net8.0 | 60.7–66.7 | 45.8–56.6 | 1.33× |
| Ed25519 sign | this host's default | net10.0 | 34.8–38.3 | 28.4–29.8 | 1.24× |
| Ed25519 sign | this host's default | net8.0 | 36.8–44.3 | 28.7–33.1 | 1.20× |
| Ed25519 sign | no vector instructions | net10.0 | 61.0–66.0 | 48.7–56.4 | 1.26× |
| Ed25519 sign | no vector instructions | net8.0 | 60.9–69.2 | 50.8–54.0 | 1.23× |
| Ed25519 verify | this host's default | net10.0 | 97–119 | 75.2–82.2 | 1.36× |
| Ed25519 verify | this host's default | net8.0 | 94–121 | 79.3–82.8 | 1.28× |
| Ed25519 verify | no vector instructions | net10.0 | 192–208 | 138–150 | 1.41× |
| Ed25519 verify | no vector instructions | net8.0 | 190–234 | 143–173 | 1.41× |

The best of fifteen timed rounds, in two processes per build run alternately, shows where each
operation's time goes. The parts F4 does not touch cost the same before and after:

| Part (µs, best of 15 rounds) | net10.0 before | net10.0 after | net8.0 before | net8.0 after |
|---|---|---|---|---|
| sign into a span, whole | 33.0 | 25.8 (1.28×) | 34.4–36.8 | 27.2–27.3 (1.31×) |
| verify, whole | 88.8–89.3 | 69.0–69.6 (1.29×) | 88.8 | 69.5–70.8 (1.27×) |
| fixed-base multiplication | 23.2 | 16.8 (1.38×) | 24.2 | 17.1–17.2 (1.41×) |
| double-scalar multiplication | 80.5–81.1 | 62.4–62.6 (1.29×) | 79.3–81.2 | 63.2–64.0 (1.26×) |
| encoding a point (one inversion) | 4.8 | 4.7–4.8 | 4.8 | 4.8–4.9 |
| decoding R (a square root) | 5.1 | 5.1–5.3 | 5.2–5.4 | 5.1–5.2 |
| R's small-order check | 0.6–0.7 | 0.6 | 0.6 | 0.6 |
| SHA-512 of the seed / of 128 bytes | 0.8 / 0.9–1.0 | 0.8 / 1.0 | 1.0–1.1 / 1.2–1.3 | 1.0–1.7 / 1.2 |
| reducing a digest / the multiply-add mod L | 0.3 / 0.5–0.6 | 0.3 / 0.5 | 0.3 / 0.6 | 0.3 / 0.6 |

The plan's targets are met on the machine they were set on. They were 42 µs for signing, 38 µs for
X25519 key generation and 110 µs for verification, against 1.1.0's 51–56, 49–51 and 136–150 µs:
gains of about 1.27×, 1.32× and 1.30×. On that machine, before the work moved, one harness run of
each build signed in 48.5 → 38.0 µs on .NET 10 and 50.9 → 39.2 µs on .NET 8, generated an X25519
key in 48.4 → 35.2 and 45.6 → 34.3 µs, and verified in 131 → 99 and 132 → 102 µs. On the second
machine, key generation meets the target's ratio by every measure (1.25–1.45×), and verification
by the A/B medians and the best rounds on .NET 10 (1.29–1.36×) and within 2–3% of it on .NET 8
(1.23–1.28×). Signing reaches it by the best rounds (1.28–1.31×) but not by the harness's medians
(1.19–1.26×). The multiplications F4 changed gained 1.38–1.41× (fixed base) and 1.26–1.29×
(double-scalar), which matches their operation counts; about 7.5 µs of signing and 7 µs of
verification on this machine lie outside them and are unchanged.

The three targets in each configuration:

| net10.0 | Ed25519 sign | X25519 key generation | Ed25519 verify |
|---|---|---|---|
| this host's default | 36.3 → 29.8–31.0 (1.19×) | 36.8–37.1 → 24.4–26.6 (1.45×) | 98.8–98.9 → 79.3–79.6 (1.24×) |
| `DOTNET_PreferredVectorBitWidth=512` | 35.8–37.3 → 30.3–31.8 (1.18×) | 35.3–37.5 → 27.3–27.4 (1.33×) | 95.6–99.7 → 79.7–83.9 (1.19×) |
| AVX-512 off | 35.4–41.9 → 33.1–34.3 (1.15×) | 38.4–41.9 → 27.4–31.6 (1.36×) | 104–106 → 76.4–82.5 (1.32×) |
| AVX2 off | 62.4–63.8 → 48.1–51.1 (1.27×) | 61.3–62.9 → 45.3–55.0 (1.24×) | 201–234 → 135–150 (1.53×) |
| no vector instructions | 62.3–63.0 → 50.9–54.7 (1.19×) | 59.5–61.7 → 45.8–50.0 (1.26×) | 202–209 → 146–150 (1.39×) |

| net8.0 | Ed25519 sign | X25519 key generation | Ed25519 verify |
|---|---|---|---|
| this host's default | 38.0–40.6 → 30.5–31.7 (1.26×) | 33.1–36.1 → 26.6–28.6 (1.25×) | 97.9–98.1 → 76.9–82.5 (1.23×) |
| `DOTNET_PreferredVectorBitWidth=512` | 35.6–36.9 → 31.9–32.7 (1.12×) | 32.7–34.9 → 26.1–26.9 (1.27×) | 95.8–98.5 → 79.2–83.1 (1.20×) |
| AVX-512 off | 38.2–39.5 → 29.4–31.5 (1.28×) | 33.4–33.8 → 27.0–28.2 (1.22×) | 97–108 → 77.2–88.8 (1.23×) |
| AVX2 off | 40.7–41.0 → 32.0–32.3 (1.27×) | 34.0–34.5 → 26.0–28.1 (1.27×) | 100–111 → 72.7–89.9 (1.30×) |
| no vector instructions | 65.0–66.3 → 53.3–56.6 (1.19×) | 56.7–64.8 → 51.5–64.2 (1.05×) | 191–204 → 154–155 (1.28×) |

Every row's change, in every configuration:

| Operation (net10.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| X25519 shared secret | 1.02× | 0.99× | 0.94× | 0.95× | 1.00× |
| X25519 key generation | 1.45× | 1.33× | 1.36× | 1.24× | 1.26× |
| Ed25519 sign | 1.19× | 1.18× | 1.15× | 1.27× | 1.19× |
| Ed25519 verify | 1.24× | 1.19× | 1.32× | 1.53× | 1.39× |
| Ed25519 sign into a span | 1.22× | 1.31× | 1.16× | 1.21× | 1.26× |
| Ed25519 sign 1 KiB into a span | 1.27× | 1.30× | 1.30× | 1.15× | 1.04× |
| Ed25519 verify 1 KiB | 1.38× | 1.19× | 1.26× | 1.38× | 1.34× |
| Ed25519 sign 16 KiB into a span | 1.08× | 0.97× | 1.03× | 1.20× | 1.14× |
| Ed25519 verify 16 KiB | 1.11× | 1.15× | 1.11× | 1.23× | 1.33× |

| Operation (net8.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| X25519 shared secret | 1.02× | 0.92× | 0.99× | 1.04× | 0.89× |
| X25519 key generation | 1.25× | 1.27× | 1.22× | 1.27× | 1.05× |
| Ed25519 sign | 1.26× | 1.12× | 1.28× | 1.27× | 1.19× |
| Ed25519 verify | 1.23× | 1.20× | 1.23× | 1.30× | 1.28× |
| Ed25519 sign into a span | 1.25× | 1.32× | 1.30× | 1.14× | 1.16× |
| Ed25519 sign 1 KiB into a span | 1.36× | 1.36× | 1.23× | 1.17× | 1.21× |
| Ed25519 verify 1 KiB | 1.25× | 1.29× | 1.41× | 1.31× | 1.50× |
| Ed25519 sign 16 KiB into a span | 1.06× | 1.07× | 1.04× | 1.03× | 1.06× |
| Ed25519 verify 16 KiB | 1.21× | 1.13× | 1.24× | 1.18× | 1.29× |

On .NET 10, disabling AVX2 disables BMI2 with it, so the field multiplication loses `mulx` and runs
at the speed it has with no intrinsics at all; on .NET 8 it keeps BMI2, and AVX2 off matches the
default. The long-message rows gain least: at 16 KiB the hash is most of the time.

How it was done, and where it departs from the design above:

- **Signed digits over a table of 32 rows.** `Ed25519Point.ScalarMultBase` recodes the scalar into
  64 signed radix-16 digits from −8 to 7 (`RecodeSignedRadix16`) and adds one multiple per digit
  from a table of 32 rows of 8, row i holding 1 to 8 times 256^i·B: the odd digits first, then four
  doublings, then the even digits, as ref10 does.
  - The table is held in affine Niels form (y + x, y − x, 2d·x·y) as the design asked, normalized
    once with one inversion for all 256 entries (Montgomery's trick). An addition from it costs
    three multiplications before the four that convert the sum out of completed coordinates, where
    1.1.0's unified addition cost nine.
  - `SelectBaseMultiple` reads all 8 entries of the row and keeps the one for the digit's magnitude
    by masks, then negates it with a conditional swap of y + x and y − x and a conditional negation
    of 2d·x·y.
  - The table is 30 KB, where 1.1.0's 64 rows of 16 extended points were 160 KB.
  - Departure: ref10's recoding needs the scalar's top bit clear, leaving the top digit from 0 to 8.
    Every caller passes such a scalar, but 1.1.0's routine served every 256-bit scalar and the
    tests pass larger ones. So the top digit is recoded too, and its carry, worth 2^256·B, enters
    before the doublings as 2^252·B through a conditional move.
  - In the spike, 64 rows without the doublings ran within noise of 32 rows on both machines
    (16.2 against 16.7 µs on .NET 10 and 16.4 against 15.4 on .NET 8 on the second; 24.2 against
    25.4 and 24.7 against 26.0 on the first), at twice the size.
- **Verification over non-adjacent forms.** `DoubleScalarMultBaseVartime` recodes S into a width-7
  form over 32 precomputed odd multiples of B, in affine Niels form, and k into a width-5 form over
  the 8 odd multiples of −A, formed on the stack in projective Niels form, as designed
  (`ComputeNonAdjacentForm`). A negative digit subtracts its multiple. The doublings stay in
  projective coordinates, and T is formed only when an addition follows. For scalars of 253 bits
  that is about 253 doublings of seven multiplications, 43 additions of eight and 32 of seven, where
  1.1.0 took 256 doublings of eight and about 120 additions of nine: the 1.26–1.29× measured.
  - The recoding places a digit past bit 255 when a scalar at or above 2^255 carries out of its top
    window, so this routine too serves every 256-bit scalar; verification's are below the group
    order.
  - The spike measured width 7 for B within noise of width 5 (58.0 against 54.0 µs on .NET 10 and
    55.4 against 62.0 on .NET 8 on the second machine, 83.7 against 86.2 and 85.8 against 88.0 on the
    first); width 7 is kept, as designed, for its fewer additions.
- **Hashing.** A message of up to `Ed25519.StackHashMaximumMessageLength` (1 KiB) is copied after
  its prefix into a buffer on the stack and hashed with one `SHA512.HashData` call. The prefix's
  copy, the secret nonce prefix when signing, is cleared before returning.
  - The design expected a limit of a few hundred bytes. On both machines and runtimes the one-call
    hash measured faster than `IncrementalHash` at every length up to 4 KiB: at 1 KiB, 2.6 against
    3.2 µs on .NET 10 and 3.1 against 3.9 on .NET 8 on the first machine. The limit is 1 KiB to keep
    the copy's stack use small.
  - A longer message is hashed incrementally, and signing's two hashes of it share one
    `IncrementalHash`. They did not at first: the harness showed signing 16 KiB allocating 272
    bytes, and a test added first (`SignData_WhenMessageExceedsTheStackHash_ForSpanOverload_*`)
    failed until they did.
  - Signing into a span, and verifying, allocate nothing below the limit; the allocating
    `SignData` allocates only its 64-byte signature.
- **Generated code, on both runtimes.**
  - `ScalarMultBase` and `DoubleScalarMultBaseVartime` compile once, fully optimized (their
    `stackalloc` rules out on-stack replacement), and call the point formulas, which tier up on
    their own.
  - `SelectBaseMultiple`'s only conditional jumps are the row's bounds check and its 8-entry loop.
    `RecodeSignedRadix16`'s are the length and index checks and its 64-digit loop, and
    `ScalarMultBase`'s the class-initialization and length checks, its two digit loops, the frame
    zeroing and the stack cookie. None depends on the scalar. The selection keeps its three
    accumulators' limbs on the stack, at fixed offsets.
- **Tests.**
  - The replaced routines stay in the tests as the oracle, `Ed25519PointReference`: the 1.1.0 table
    of 64 rows of 16 extended points and its unsigned 4-bit windows. It landed, with the tests
    below that need no new API, before the change and passed against 1.1.0's code.
  - `ScalarMultBase` is held to the ladder and to the oracle over scalars that give every window
    each digit from −8 to 7 (a byte of n, or of 16n, in every position), the recoding's bounds (the
    group order and its neighbours, 2^255, 2^256 − 1, a top nibble of 7 that carries), and seeded
    unreduced, clamped and reduced scalars.
  - `DoubleScalarMultBaseVartime` is held to two separate ladders and to the oracle over boundary
    scalars, with a multiple of B, a decoded and negated point, a point with a component of order 8
    and the identity, and over seeded inputs of the shape verification passes.
  - `SelectBaseMultiple` is held to a plain lookup, the multiple formed by doublings and additions
    without the table, for every digit from −8 to 8 of every row. Both recodings are held to their
    values as integers, and the non-adjacent form's digits to their bounds and spacing, at every
    width from 2 to 8.
  - The mixed additions and subtractions, and the projective doubling and its chains, are held to
    the unified addition and doubling, the eight points of small order included.
  - RFC 8032's TEST 1024, whose message is 1023 bytes, joins the signing vectors, built from the
    embedded Wycheproof row that carries it and the RFC's private seed. Signing and verification
    either side of the limit, and far beyond it, are held to an RFC 8032 signer built from one-call
    hashes and the oracle, and a changed last byte fails verification on both sides.
  - The full suite passes on both runtimes.
- **Left for later.**
  - The inversion that encodes R when signing (4.8 µs) and the square root that decodes it when
    verifying (5.1 µs) are now the largest costs outside the multiplications. A constant-time
    inversion by divsteps (Bernstein–Yang) is the known faster alternative to the exponentiation.
  - Signing recomputes the expanded key, SHA-512 of the seed, on every call (0.8–1.1 µs). The key
    material could keep it, as it keeps the decoded public point, at the price of holding more
    secret material.
  - Verification forms the 8 odd multiples of −A on every call, about 70 field multiplications,
    which the key material could keep as well.
  - X25519's shared secret, the Montgomery ladder, is unchanged.

### F5 — Serpent's wide-block variants (done)

F5's figures come from the second machine, F3's. The before build is this branch at 6ef77d8f, which adds the harness's
block rows to master at 57c0ab26 (#729); the after build is at 9182607e, with F5's code. Both ran alternately, in each
of the harness's five processor configurations on both runtimes, in two runs, the second in the opposite order. The rows
encrypt and decrypt 64 KiB one block per call, through `IBlockCipher.Encrypt` and `Decrypt`. Serpent-128's rows run
code F5 does not touch, so they show how far the runs drift on their own. *Change* is the ratio of the two ranges'
midpoints, above 1 where F5 is faster. Nothing is allocated before or after.

| One block per call (net10.0, this host's default) | Before F5 (MiB/s) | After F5 (MiB/s) | Change |
|---|---|---|---|
| Serpent-128 encrypt | 81.0–85.7 | 87.1–88.6 | 1.05× |
| Serpent-128 decrypt | 82.9–86.6 | 82.7–88.4 | 1.01× |
| Serpent-256 encrypt | 22.9–26.1 | 78.7–81.2 | 3.26× |
| Serpent-256 decrypt | 21.6–22.9 | 79.4–81.5 | 3.62× |
| Serpent-512 encrypt | 19.2–22.0 | 51.2–51.8 | 2.50× |
| Serpent-512 decrypt | 19.4 | 47.9–50.6 | 2.54× |
| Serpent-1024 encrypt | 16.9–17.7 | 40.1–41.9 | 2.37× |
| Serpent-1024 decrypt | 15.7–16.3 | 40.7–41.7 | 2.58× |

| One block per call (net8.0, this host's default) | Before F5 (MiB/s) | After F5 (MiB/s) | Change |
|---|---|---|---|
| Serpent-128 encrypt | 83.0–86.4 | 83.8–84.2 | 0.99× |
| Serpent-128 decrypt | 88.3–88.4 | 78.7–88.1 | 0.94× |
| Serpent-256 encrypt | 23.5–24.6 | 70.0–77.9 | 3.07× |
| Serpent-256 decrypt | 20.0–20.6 | 76.3–82.4 | 3.91× |
| Serpent-512 encrypt | 19.0–21.3 | 50.2–55.0 | 2.61× |
| Serpent-512 decrypt | 16.4–17.9 | 49.0–50.2 | 2.89× |
| Serpent-1024 encrypt | 15.5–17.6 | 39.1–43.5 | 2.50× |
| Serpent-1024 decrypt | 13.9–14.3 | 41.3–43.0 | 2.99× |

Five alternating A/B rounds of the wide-block rows, each build in its own process, give the same picture:

| One block per call | Runtime | Before F5 (MiB/s) | After F5 (MiB/s) | Change (medians) |
|---|---|---|---|---|
| Serpent-256 encrypt | net10.0 | 24.5–25.8 | 71.7–79.5 | 3.06× |
| Serpent-256 encrypt | net8.0 | 20.4–24.9 | 77.5–81.3 | 3.32× |
| Serpent-256 decrypt | net10.0 | 23.0–24.5 | 76.1–86.9 | 3.31× |
| Serpent-256 decrypt | net8.0 | 19.5–24.1 | 76.2–83.4 | 3.68× |
| Serpent-512 encrypt | net10.0 | 18.7–21.7 | 44.2–51.9 | 2.52× |
| Serpent-512 encrypt | net8.0 | 14.2–19.8 | 44.8–54.9 | 2.61× |
| Serpent-512 decrypt | net10.0 | 16.5–20.6 | 45.2–48.9 | 2.54× |
| Serpent-512 decrypt | net8.0 | 16.7–19.1 | 44.7–49.4 | 2.64× |
| Serpent-1024 encrypt | net10.0 | 16.0–17.6 | 36.2–42.9 | 2.48× |
| Serpent-1024 encrypt | net8.0 | 10.5–17.2 | 39.1–44.3 | 2.60× |
| Serpent-1024 decrypt | net10.0 | 15.1–17.0 | 32.3–40.0 | 2.25× |
| Serpent-1024 decrypt | net8.0 | 12.2–15.3 | 35.1–41.0 | 2.89× |

The target, at least twice each variant's 1.1.0 speed one block per call, is met by every wide-block row in every
configuration on both runtimes: Serpent-256 gains 3.0–4.1×, Serpent-512 2.3–3.1× and Serpent-1024 2.25–3.0×, while
the Serpent-128 rows move 0.94–1.05×. Serpent-256 now runs about as fast per byte as Serpent-128 does one block per call,
although a block of it takes 48 rounds of two groups where Serpent-128's takes 32 rounds of one.

Every row's change, in every configuration:

| One block per call (net10.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| Serpent-128 encrypt | 1.05× | 0.95× | 1.01× | 1.01× | 1.00× |
| Serpent-128 decrypt | 1.01× | 0.98× | 0.99× | 0.97× | 0.97× |
| Serpent-256 encrypt | 3.26× | 3.25× | 3.16× | 3.32× | 3.25× |
| Serpent-256 decrypt | 3.62× | 3.33× | 3.53× | 3.56× | 3.64× |
| Serpent-512 encrypt | 2.50× | 3.09× | 2.42× | 2.65× | 2.61× |
| Serpent-512 decrypt | 2.54× | 2.88× | 2.33× | 2.50× | 2.60× |
| Serpent-1024 encrypt | 2.37× | 2.87× | 2.53× | 2.54× | 2.35× |
| Serpent-1024 decrypt | 2.58× | 2.82× | 2.38× | 2.52× | 2.57× |

| One block per call (net8.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| Serpent-128 encrypt | 0.99× | 1.03× | 0.98× | 1.00× | 1.00× |
| Serpent-128 decrypt | 0.94× | 0.99× | 1.00× | 0.95× | 0.98× |
| Serpent-256 encrypt | 3.07× | 3.30× | 3.02× | 3.40× | 3.18× |
| Serpent-256 decrypt | 3.91× | 3.82× | 4.11× | 3.62× | 3.61× |
| Serpent-512 encrypt | 2.61× | 2.52× | 2.71× | 2.64× | 2.60× |
| Serpent-512 decrypt | 2.89× | 2.63× | 2.88× | 3.06× | 2.94× |
| Serpent-1024 encrypt | 2.50× | 2.66× | 2.42× | 2.56× | 2.53× |
| Serpent-1024 decrypt | 2.99× | 2.88× | 2.98× | 2.70× | 2.75× |

The rounds are scalar, so the vector configurations change nothing but the drift between runs.

How it was done, and where it departs from the design above:

- **Eight rounds at a time, the S-box a type argument.** The rounds run in `SerpentCore`, eight at a time, one per
  S-box, as the design asked. Each round names its S-box as a type argument, one of eight structs behind
  `IRoundSBox`, so every circuit is inlined where the round is compiled.
  - W7 had found a version generic over wrappers of the *words* running out of inlining budget. Here the type argument
    only selects the circuit, and the words stay plain `uint`.
  - The generated code on both runtimes calls nothing inside the rounds.
- **Two paths.**
  - Serpent-256's eight words fit in the registers. `EncryptResidentWideBlock` / `DecryptResidentWideBlock` hold
    them in locals for the whole block, so the word rotation between rounds is a renaming of the locals, which returns
    them to their places every eight rounds, and each pass of eight rounds is written out once. The generated code makes
    three stack references on either runtime.
  - Serpent-512's sixteen words and Serpent-1024's thirty-two do not fit in x64's sixteen registers.
    `EncryptStreamedWideBlock` / `DecryptStreamedWideBlock` pass each round over the state a four-word group at a time.
    The group is read into locals, takes its round key, S-box and linear transform, and is written to a second copy of
    the state one word to the left, which is the rotation. The two copies take 256 bytes of stack and are cleared after
    the block.
  - In the spike, the resident rounds ran Serpent-256 1.27–1.30× faster than streaming it, on both runtimes. A width
    fixed at compile time made no measurable difference to the streamed rounds.
- **Departure: the tweak is folded into the round keys.** The design kept the tweak injection every fourth round.
  Each injection is followed at once by the next round's key, so `SerpentBlockCipher` adds the injected material to
  that key when the key is set, and keeps no tweak schedule. The construction and every output are unchanged. In the
  spike, folding ran 2–13% faster than injecting.
- **Removed code.** The per-pass helpers went, with the base class's inverse S-box and linear-transform wrappers, which
  nothing else called. The indexed `SerpentCore.SBox` stays for the key schedules, and `InverseSBox` for the circuit
  tests.
- **Generated code, on both runtimes.** Each of the four entry points is `NoInlining | AggressiveOptimization`, so it
  compiles once, fully optimized, with its own inlining budget: the resident ones to 3.1–3.3 KB and the streamed ones to
  2.7–2.8 KB. They call only the throw helpers, and the streamed ones the argument check and the clearing of the
  copies as well.
- **Tests.**
  - The replaced rounds stay in the tests as the oracle, `SerpentWideReference`, which applies `SerpentReference`'s
    table-driven S-boxes. It landed before the change, with tests that hold `Serpent256/512/1024Cipher` to it in and
    out of place over a key, tweak and blocks of all-ones and all-zero bytes and six seeded keys and tweaks, and passed
    against 1.1.0's rounds.
  - `SerpentCoreTests` hold both paths to the oracle, given the round keys with the tweak folded in: at every width, at
    one, two and the variant's own number of eight-round passes, and in and out of place. They also check the guards
    and that every entry point forbids inlining.
  - The variants' pinned vectors still pass, and the full suite passes on both runtimes.
- **Left for later.**
  - A kernel with one block per vector lane, which the design left as a second step. The wide variants'
    `EncryptBlocks` is still the default, one `Encrypt` per block, so their ECB and CTR would gain from it.
  - ARM64's thirty-one general registers could hold Serpent-512's sixteen words too. F7's hardware run would show
    whether a resident path pays there.

### F6 — counter mode without the counter run (done)

F6's figures come from the second machine, F3's. The before build is this branch at a88da7a7, which adds the harness's
ECB and 64-byte CTR rows beside each block cipher's CTR row, on master at 5f5a692e (#732); the after build is at
50de4381, with F6's code. Both ran alternately, in each of the harness's five processor configurations on both runtimes,
in two runs, the second in the opposite order. *CTR 1 MiB* and *CTR 64 B* run a new `CtrModeTransform` over
`Serpent128Cipher` per call. *ECB bulk 1 MiB* encrypts the same number of blocks through `EncryptBlocks`, which F6 does
not touch, and *Twofish-CTR 1 MiB* runs CTR's unchanged path, so the two show how far the runs drift on their own.
*Change* is the ratio of the two ranges' midpoints, above 1 where F6 is faster. The CTR rows allocate the transform's 80
bytes per call, before and after, and the ECB row nothing.

| Serpent-128 (net10.0, this host's default) | Before F6 (MiB/s) | After F6 (MiB/s) | Change |
|---|---|---|---|
| CTR 1 MiB | 644–646 | 725–732 | 1.13× |
| CTR 64 B | 195–196 | 201–218 | 1.07× |
| ECB bulk 1 MiB | 672–682 | 731–736 | 1.08× |
| Twofish-CTR 1 MiB | 128–144 | 142–144 | 1.05× |

| Serpent-128 (net8.0, this host's default) | Before F6 (MiB/s) | After F6 (MiB/s) | Change |
|---|---|---|---|
| CTR 1 MiB | 547–556 | 545–597 | 1.04× |
| CTR 64 B | 156–176 | 181–190 | 1.12× |
| ECB bulk 1 MiB | 578–602 | 584–600 | 1.00× |
| Twofish-CTR 1 MiB | 146–147 | 136–142 | 0.95× |

The ECB and Twofish rows moved 0.95–1.08× on their own, so the two runs alone cannot separate a few percent from the
drift. Five alternating A/B rounds of the Serpent-128 rows, each build in its own process, can:

| Serpent-128 | Runtime | Before F6 (MiB/s) | After F6 (MiB/s) | Change (medians) |
|---|---|---|---|---|
| CTR 1 MiB | net10.0 | 609–652 | 708–725 | 1.14× |
| CTR 1 MiB | net8.0 | 525–568 | 579–600 | 1.06× |
| CTR 64 B | net10.0 | 182–203 | 193–223 | 1.09× |
| CTR 64 B | net8.0 | 147–181 | 182–186 | 1.09× |
| ECB bulk 1 MiB | net10.0 | 691–741 | 694–742 | 1.01× |
| ECB bulk 1 MiB | net8.0 | 597–616 | 557–611 | 0.98× |

The target, Serpent-128-CTR within 10% of `EncryptBlocks`, is met in every configuration on both runtimes. Over the A/B
rounds CTR runs at 1.00 of ECB's median rate on .NET 10 and 0.98 on .NET 8, where it ran at 0.89 and 0.91. Over the two
runs of every configuration it runs at 0.94–1.02, where it ran at 0.86–1.03:

| CTR 1 MiB ÷ ECB 1 MiB | Runtime | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|---|
| Before F6 | net10.0 | 0.95 | 0.86 | 0.93 | 0.91 | 1.03 |
| After F6 | net10.0 | 0.99 | 0.97 | 1.02 | 1.00 | 0.94 |
| Before F6 | net8.0 | 0.93 | 0.91 | 0.93 | 1.02 | 1.03 |
| After F6 | net8.0 | 0.97 | 0.95 | 1.02 | 0.98 | 0.98 |

Every row's change, in every configuration:

| Change (net10.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| Serpent-128-CTR 1 MiB | 1.13× | 1.14× | 1.08× | 1.10× | 0.97× |
| Serpent-128-CTR 64 B | 1.07× | 1.05× | 1.02× | 1.05× | 1.04× |
| Serpent-128-ECB bulk 1 MiB | 1.08× | 1.02× | 0.98× | 1.00× | 1.06× |
| Twofish-CTR 1 MiB | 1.05× | 0.95× | 1.02× | 1.01× | 1.01× |

| Change (net8.0) | this host's default | `DOTNET_PreferredVectorBitWidth=512` | AVX-512 off | AVX2 off | no vector instructions |
|---|---|---|---|---|---|
| Serpent-128-CTR 1 MiB | 1.04× | 1.03× | 1.06× | 1.03× | 0.98× |
| Serpent-128-CTR 64 B | 1.12× | 0.98× | 1.11× | 1.08× | 1.07× |
| Serpent-128-ECB bulk 1 MiB | 1.00× | 0.99× | 0.97× | 1.07× | 1.02× |
| Twofish-CTR 1 MiB | 0.95× | 0.95× | 0.94× | 1.03× | 1.01× |

With no vector instructions, the two runs put CTR at 0.97–0.98× of its old rate, inside the ECB row's own drift there.
Five alternating A/B rounds in that configuration settle it: the fused scalar loop runs a little faster than encrypting a
run of counter blocks and XORing it in a second pass.

| No vector instructions | Runtime | Before F6 (MiB/s) | After F6 (MiB/s) | Change (medians) |
|---|---|---|---|---|
| CTR 1 MiB | net10.0 | 82.5–86.4 | 83.3–88.6 | 1.04× |
| CTR 1 MiB | net8.0 | 78.2–84.5 | 82.2–87.9 | 1.04× |
| ECB bulk 1 MiB | net10.0 | 83.6–89.2 | 85.7–87.6 | 1.00× |
| ECB bulk 1 MiB | net8.0 | 80.4–86.9 | 75.7–87.5 | 1.01× |

There CTR runs at 1.00 of ECB's median rate on both runtimes, where it ran at 0.96–0.97.

How it was done, and where it departs from the design above:

- **An internal counter-mode entry.** `ICounterModeBlockCipher` extends `IBlockCipher` with
  `XorCounterKeystream(ref counterHigh, ref counterLow, input, output)`. It combines the input with the keystream of
  successive counter blocks, the counter a big-endian 128-bit integer held as two 64-bit halves that advances by one
  per whole or partial block, modulo 2¹²⁸. `Serpent128Cipher` implements it over `SerpentCore.XorCounterKeystream`,
  which dispatches as `EncryptBlocks` does: groups of eight blocks, then four, then single blocks, then a partial last
  block.
- **The counters in registers.** The eight- and four-block kernels gained `XorCounterBlocks`.
  - Each lane's counter is four words in native order, one vector per word. The eight-block kernel's lanes follow the
    order `Load`'s transpose leaves: the group's even-numbered blocks in the low 128-bit lane, its odd-numbered blocks
    in the high.
  - A byte reversal per word, one `vpshufb`, `pshufb` or `REV32`, gives the words Serpent reads from a big-endian
    block. The rounds run, and the transposed keystream is XORed with the input as it is stored.
  - Between groups the counters advance by eight or four. Each carry between words is formed by an unsigned compare and
    applied by masks.
  - The scalar path forms one block's words the same way and adds the carry out of the low half arithmetically. A
    partial last block goes through a 16-byte stack buffer, cleared afterwards, because its bytes past the input hold
    bare keystream.
- **Departure: no branch on the counter.** The design did not ask for it. EAX's counter, though, is `N′ = OMAC(0, N)`
  under the key. The run of counter blocks branched only on a carry out of the low 64 bits, which a random counter meets
  once in 2⁶⁴ blocks. The kernels carry out of each 32-bit word, once in 2³² blocks, well within one long message. So
  they carry by masks, and their time does not depend on the counter.
- **The modes.**
  - `CtrModeTransform` hands such a cipher the input up to the counter's wrap, which it computes from the two halves.
    The block that takes the counter's last value is produced, and a call with input left over throws after writing it,
    as the run of counter blocks did.
  - `CounterKeystream.TransformBigEndian128`, the counter EAX and SIV share, hands it the whole input, wrapping silently
    as before.
  - Every other cipher keeps the run of counter blocks, as does every mode with another counter rule: GCM's `inc32`,
    GCM-SIV's little-endian counter, and CCM's.
- **Departure: EAX and SIV as well.** The design named CTR. EAX and SIV share the same big-endian counter through
  `CounterKeystream.TransformBigEndian128`, so they take the entry at no extra cost. Neither is measured here: with
  Serpent, both spend most of their time in CMAC, one block at a time.
- **Generated code, on both runtimes.**
  - Each counter kernel and the scalar loop is `NoInlining | AggressiveOptimization`.
  - The kernels call nothing. Their only conditional jumps are the loops', and the one that skips the last round's
    linear transform.
  - With AVX-512VL's 32 registers nothing spills. With AVX2's 16, the .NET 8 eight-block kernel reloads two constant
    vectors once per group.
  - The scalar loop spills its loop state at block boundaries, not inside the rounds.
- **Tests.**
  - The mode tests landed first, and passed against the run of counter blocks. CTR's batching tests gained Serpent-128,
    a message split into two calls at every byte, and counters that carry out of their last four and last eight bytes.
    The wrap tests gained a wrap partway through a group, and a partial last block that takes the counter's last value.
    `CounterKeystreamTests` hold EAX's and SIV's counter to a block-at-a-time reference: over lengths either side of a
    block, a group and a run, from counters below each carry and the wrap, and in place.
  - `SerpentCoreTests.XorCounterKeystream` drives every kernel explicitly against encrypting each counter block on its
    own. It covers every length to forty blocks and a few past, each whole and with partial blocks of 1, 7 and 15 bytes.
    It starts counters 1 to 17 blocks below a carry out of 32, 64 and 96 bits, and below the wrap, so each carry falls
    in every lane of a group. It runs in place, and into a longer output that it must leave untouched past the input.
    It checks the counter each kernel leaves, and each kernel's attributes.
  - `Serpent128CipherTests.XorCounterKeystream` holds the cipher's entry to `Encrypt` of each counter block for every
    key size, and checks the disposal and length guards.
  - The AdvSimd kernel ran on ARM64 under qemu. `SerpentCoreTests`' counter tests pass there on .NET 8. On both
    runtimes, a scratch check held CTR to `Encrypt` of each counter block over carries, in place and out. Under the
    emulator, .NET 10 aborts inside MSTest's `CollectionAssert`, after the kernel has returned.
  - The full suite passes on both runtimes.
- **Left for later.**
  - GCM's `inc32` counter, GCM-SIV's little-endian counter and CCM's counter each need their own increment rule in the
    same kernels.
  - The wide-block variants' CTR still encrypts one counter block per `Encrypt` call. F5 left their many-block kernel for
    later.
  - AES cannot fuse, because its rounds run in the BCL.

### F7 — ARM64 on real hardware, and a small-L3 desktop (done)

F7's figures come from GitHub's hosted runners, through a new workflow, *Cryptography benchmarks*
(`.github/workflows/crypto-benchmarks.yml`). It runs on manual dispatch, and on any pull request that changes it, on
three machines:

| Runner | Processor | Cores | Cache, as the VM reports it |
|---|---|---|---|
| `ubuntu-24.04-arm` | Arm Neoverse N2 | 4 | 1 MiB L2 per core, 128 MiB L3 |
| `macos-15` | Apple M1, virtualized | 3 | 12 MiB L2 |
| `ubuntu-24.04` | AMD EPYC 7763 (Zen 3): AVX2, no AVX-512 | 2, with 2 threads each | 32 MiB L3 |

Each machine builds the harness twice, from the pull request and against the published 1.0.0 package
(`-p:BoduCryptoBaseline=1.0.0`). `Bodu.Security.Cryptography/bench/harness_matrix.py` then runs each suite in three
configurations, twice, the second time in the opposite order:

- *vector*: the kernels dispatch selects.
- *scalar*: the same build with `--disable-simd`, a new harness option that sets the library's `DisableSimd` switch.
  Every library kernel gives way to its scalar path, while the runtime, the BCL and the `umulh` products keep their
  own code.
- *1.0.0*: the published package.

The suites are the crypto harness, over the rows with an ARM64 path of their own; the Argon2 harness (m = 64 MiB,
t = 3, at p = 4 and p = 1); and a new sweep, `--argon2-harness --sweep`, of m from 1 to 32 MiB at p = 4, with threads
and without. The x64 machine runs only the Argon2 suites. The N2's two runs mostly agreed within 2%. The M1 is
a three-core virtual machine whose runs spread by up to ±30%, so its ratios are rougher.

**The AdvSimd kernels against their scalar paths.** Vector ÷ scalar, by the medians of the two runs, before F7's
gate. Below 1, the AdvSimd kernel ran slower:

| Case | N2, .NET 8 | N2, .NET 10 | M1, .NET 8 | M1, .NET 10 |
|---|---|---|---|---|
| BLAKE2b-512, 1 MiB | 0.32 | 0.32 | 0.33 | 0.30 |
| BLAKE2b-512, 64 B | 0.42 | 0.42 | 0.39 | 0.37 |
| BLAKE2s-256, 1 MiB | 0.50 | 0.50 | 0.56 | 0.52 |
| BLAKE2s-256, 64 B | 0.64 | 0.63 | 0.63 | 0.60 |
| BLAKE3, 64 B (one block) | 0.68 | 0.69 | 0.73 | 0.62 |
| scrypt N = 2¹⁴, r = 8, p = 1 | 0.86 | 0.48 | 0.99 | 0.63 |
| scrypt N = 2¹⁷, r = 8, p = 1 | 0.86 | 0.50 | 1.07 | 0.64 |
| scrypt N = 2¹⁴, r = 8, p = 4, four threads | 0.62 | 0.48 | 0.81 | 0.56 |
| Argon2id m = 19 MiB, t = 2, p = 1 | 0.77 | 0.76 | 1.63 | 1.22 |
| Argon2id m = 64 MiB, t = 3, p = 1 (CPU) | 0.79 | 0.80 | 1.49 | 0.96 |
| Argon2id m = 64 MiB, t = 3, p = 4 (CPU) | 0.80 | 0.77 | 1.60 | 0.91 |
| BLAKE3, 1 MiB | 1.48 | 1.49 | 2.53 | 2.13 |
| BLAKE3, 16 MiB, all cores | 1.49 | 1.51 | 3.15 | 2.15 |
| CubeHash, 1 MiB | 7.24 | 6.91 | 9.00 | 8.77 |
| ChaCha20, 1 MiB | 1.64 | 1.63 | 2.53 | 1.99 |
| Salsa20, 1 MiB | 2.10 | 1.51 | 2.18 | 1.97 |
| XChaCha20-Poly1305, 1 MiB | 1.42 | 1.40 | 1.84 | 1.63 |
| XChaCha20-Poly1305, 64 B | 1.00 | 1.00 | 1.09 | 0.97 |
| AES-128-GCM, 1 MiB (PMULL) | 3.67 | 3.62 | 5.41 | 4.99 |
| AES-128-GCM-SIV, 1 MiB (PMULL) | 3.71 | 3.76 | 5.48 | 5.25 |
| Serpent-128-CTR, 1 MiB | 1.44 | 1.43 | 2.19 | 1.92 |
| Serpent-128-ECB, 1 MiB | 1.46 | 1.45 | 2.21 | 1.91 |
| Poly1305, 1 MiB (`umulh`; no library vector kernel) | 1.00 | 1.00 | 1.06 | 1.00 |
| Ed25519 verify (`umulh`) | 1.01 | 1.01 | 1.11 | 1.02 |

Every kernel that gives each lane a state of its own won on both processors and both runtimes: ChaCha20 and Salsa20
over four blocks, BLAKE3 over four chunks or parents, and Serpent-128 over four blocks. So did CubeHash's, whose
1024-bit state fills eight vectors, and PMULL's GHASH and POLYVAL, by 3.6–5.5×. The kernels that spread one state
across a vector's lanes lost on the N2, on both runtimes:

- BLAKE2b ran at 0.32–0.42 of its scalar path, and BLAKE2s at 0.50–0.64.
- A single BLAKE3 block ran at 0.68–0.69.
- scrypt ran at 0.48–0.86.
- Argon2 ran at 0.76–0.80.

The M1 agreed for BLAKE2 and the single BLAKE3 block on both runtimes, and for scrypt on .NET 10. Under .NET 8 its
scrypt kernel ran about as fast as the scalar path, and its Argon2 kernel 1.5–1.6 times as fast; under .NET 10 its
Argon2 kernel ran about as fast.

The rotations are not what loses. BLAKE3's four-input kernel and its one-block kernel rotate through the same shim,
`Blake2sCore.AdvSimdIsa`, whose 12- and 7-bit rotations already take `SHL` and `SRI`. On the N2 the first ran 1.5
times as fast as the scalar kernel and the second at 0.68 of it. What the losers share is their layout: one state
across a vector's four 32-bit or two 64-bit lanes, with the lanes rearranged between the column and diagonal steps of
every round. On x64 that layout wins, because sixteen general registers cannot hold the state. ARM64's thirty-one
can, and rotate a word in one instruction, so its scalar kernels have little left to gain. The design's first
tuning candidate, `SHL` / `SRI` in place of `TBL` in `VectorRotation.AdvSimd`, was therefore not tried: every kernel
that rotates through `VectorRotation.AdvSimd` won.

.NET 10's scalar paths gained the most. On the N2, scrypt's scalar path ran N = 2¹⁴ in 40.8–41.1 ms under .NET 8
and 22.8–22.9 ms under .NET 10, while its AdvSimd kernel took 47.3–47.8 ms under both. Under .NET 10 the scalar path
beat OpenSSL's `EVP_PBE_scrypt` on the same machine, which took 23.8–24.2 ms.

**F2's ARM64 estimate.** F2 left `StepCost`'s cost for an AdvSimd step of four blocks, twice the block function's, for
this run to check. On the N2 the kernels' rates over 1 MiB put the step at 2.5 block-function times for ChaCha20, and
1.9–2.7 for Salsa20. With the step at 2.5 times, every one-pass plan up to 1 KiB comes out the same as with 2, and it
would take 3 to change any. The M1's rates put the step at 1.6–2.0, noisily. A step under 2 would draw keystreams of
65–128 bytes, and the same window past each further 256 bytes, in one pass rounded up to whole steps, where they are
now drawn as they come. The estimate stays. At 64 bytes the AEADs sealed as fast under the AdvSimd kernel's plan as
under the scalar kernel's: 0.99–1.00 on the N2, and 0.95–1.09 on the M1.

**The gate.** As the target asks, dispatch no longer selects the losing kernels.
`SimdCapabilities.AdvSimdSingleState` holds back every AdvSimd kernel that spreads one state across its lanes, and it
is closed. BLAKE2b, BLAKE2s and scrypt select their scalar kernels on ARM64. BLAKE3 keeps its four-input AdvSimd kernel,
and compresses a single block, or a lone input among several, with the scalar kernel. Argon2 selects its scalar kernel
under .NET 10 and keeps its AdvSimd kernel under .NET 8, for the reason ARG-N-002 below gives.

The workflow ran again on the gated build, in which Argon2 took its scalar kernel under both runtimes. On the N2 every
gated row now runs at 0.95–1.00 of its scalar path, which is the same code, and its gain over 1.0.0 grew:

| Case, vector ÷ 1.0.0 on the N2 | .NET 8 before the gate | .NET 8 gated | .NET 10 before the gate | .NET 10 gated |
|---|---|---|---|---|
| BLAKE2b-512, 1 MiB | 1.37 | 4.25 | 1.35 | 4.23 |
| BLAKE2b-512, 64 B | 1.32 | 3.23 | 1.38 | 3.29 |
| BLAKE2s-256, 1 MiB | 2.17 | 4.35 | 2.20 | 4.34 |
| BLAKE2s-256, 64 B | 2.03 | 3.31 | 2.50 | 3.92 |
| BLAKE3, 64 B (one block) | 2.15 | 3.17 | 2.08 | 2.98 |
| scrypt N = 2¹⁴, r = 8, p = 1 | 1.45 | 1.64 | 1.25 | 2.56 |
| scrypt N = 2¹⁷, r = 8, p = 1 | 1.36 | 1.54 | 1.22 | 2.37 |
| Argon2id m = 19 MiB, t = 2, p = 1 | 2.07 | 2.58 | 1.99 | 2.35 |
| Argon2id m = 64 MiB, t = 3, p = 4 (CPU) | 1.80 | 2.12 | 1.70 | 2.02 |

Each run measures 1.0.0 afresh, on the runner it was given, so each ratio holds within its run. The kernels that were
not gated ran as before, within 1–2%. On the M1 the gated rows read 0.80–1.26 of the scalar path, the same code, which
is how far that machine drifts within one run.

**ARG-N-002 on ARM64.** `argon2-performance.md` asks that a 64 MiB derivation at p = 4 take at most 60% of 1.0.0's
CPU with vector code, on Apple silicon above all. The share of 1.0.0's CPU and wall time, by the midpoints of the two
runs before the gate:

| 64 MiB, t = 3, p = 4 | N2, .NET 8 | N2, .NET 10 | M1, .NET 8 | M1, .NET 10 | EPYC, .NET 8 | EPYC, .NET 10 |
|---|---|---|---|---|---|---|
| CPU, the vector kernel | 56% | 59% | 36% | 55% | 21% | 23% |
| CPU, the scalar kernel | 44% | 46% | 58% | 50% | 63% | 69% |
| Wall, the vector kernel | 16% | 21% | 16% | 25% | 7% | 9% |
| Wall, the scalar kernel | 12% | 16% | 26% | 30% | 19% | 24% |

In the gated run the scalar kernel took 46–50% on the N2, but 66–67% on the M1 under .NET 8, past the target, and
55–59% under .NET 10. The M1's rows drift between runs, but under .NET 8 its scalar kernel took 58–67% in both, against
36% for the AdvSimd kernel, which also ran 1.5–1.6 times as fast there. So Argon2 keeps the AdvSimd kernel under
.NET 8, at 0.8 of the scalar kernel's speed on the N2, and selects the scalar kernel under .NET 10, where it runs at
least as fast as the AdvSimd kernel.

A third run, on the build as it merges, held both choices:

| 64 MiB, t = 3, p = 4, share of 1.0.0's CPU | N2, .NET 8 | N2, .NET 10 | M1, .NET 8 | M1, .NET 10 |
|---|---|---|---|---|
| *vector*: AdvSimd under .NET 8, scalar under .NET 10 | 56% | 47% | 48% | 67% |
| *scalar* | 45% | 45% | 63% | 47% |

Under .NET 8 the AdvSimd kernel meets the target on both processors, and on the M1 ran 1.3–1.4 times as fast as the
scalar kernel, which missed it again. Under .NET 10 both configurations run the scalar kernel. It meets the target on
the N2, but the M1 read the same code at 47% and 67% within one job, and at 47–67% over the three runs, so that virtual
machine cannot place it either side. The AdvSimd kernel read 55% there in the one run that timed it under .NET 10. On
the EPYC the AVX2 kernel takes 21–23%.

**The small-L3 machine.** The EPYC's 32 MiB of L3 holds half of the harness's 64 MiB matrix, where the machine
Argon2's thread threshold was set on held all of it. The sweep compares the default, which runs lanes on threads from
256 blocks per segment (4 MiB at p = 4), with one thread. One thread's wall time over the default's, by the midpoints:

| m, t = 3, p = 4 | EPYC, .NET 8 | EPYC, .NET 10 | N2, .NET 8 | N2, .NET 10 | M1, .NET 8 | M1, .NET 10 |
|---|---|---|---|---|---|---|
| 1 MiB | 1.00 | 1.00 | 1.00 | 1.00 | 1.03 | 1.13 |
| 2 MiB | 1.00 | 1.00 | 1.00 | 0.98 | 1.02 | 0.96 |
| 4 MiB | 2.06 | 1.65 | 2.85 | 2.20 | 1.44 | 1.51 |
| 8 MiB | 2.23 | 1.64 | 2.90 | 2.65 | 1.66 | 1.46 |
| 16 MiB | 2.01 | 1.84 | 3.06 | 2.82 | 2.14 | 0.92 |
| 32 MiB | 1.98 | 1.72 | 2.92 | 2.64 | 1.68 | 0.77 |
| 64 MiB (the harness) | 2.39 | 1.78 | 3.55 | 2.68 | 1.92 | 2.01 |

Below the threshold both columns run one thread, so the ratio is 1 but for noise. From the threshold up the threads
cut the wall time on every machine, the small-L3 EPYC included, by 1.6–2.4 times there. The M1's two readings below 1,
at 16 and 32 MiB under .NET 10, come from runs whose threaded times spread from 13 to 60 ms and from 35 to 135 ms. The
threshold stands. Whether it could be lower cannot be read here: the harness cannot run threads below it, since the
threshold is internal.

With AVX2 the EPYC runs Argon2 2.6–3.0 times as fast as its scalar kernel, on both runtimes, and at 64 MiB and p = 4
in 7–9% of 1.0.0's wall time.

**x64's own single-state kernels.** The ARM64 result raised the question for x64, where the SSE2, SSSE3 and AVX2
kernels of BLAKE2b, BLAKE2s, BLAKE3's single block, scrypt and Argon2 share the layout. On the second machine, F3's,
vector ÷ scalar in two alternating runs, in three of its configurations:

| .NET 10 / .NET 8 | This host's default (AVX-512) | AVX-512 off | AVX2 off |
|---|---|---|---|
| BLAKE2b-512, 1 MiB | 1.21 / 1.03 | 1.08 / 0.95 | 0.96 / 0.88 |
| BLAKE2b-512, 64 B | 1.09 / 1.00 | 1.05 / 0.96 | 0.92 / 0.90 |
| BLAKE2s-256, 1 MiB | 1.29 / 1.33 | 1.30 / 0.99 | 1.04 / 1.06 |
| BLAKE2s-256, 64 B | 1.17 / 1.19 | 1.23 / 1.07 | 1.02 / 1.03 |
| BLAKE3, 64 B (one block) | 1.25 / 1.07 | 1.24 / 0.98 | 1.15 / 1.29 |
| scrypt N = 2¹⁴, r = 8, p = 1 | 1.54 / 1.47 | 1.58 / 1.51 | 1.25 / 1.64 |
| scrypt N = 2¹⁷, r = 8, p = 1 | 1.26 / 1.27 | 1.23 / 1.32 | 1.12 / 1.29 |
| Argon2id m = 19 MiB, t = 2, p = 1 | 2.38 / 2.03 | 2.31 / 2.14 | 1.38 / 1.37 |

Everything wins with AVX-512, and scrypt, Argon2 and BLAKE3's single block win everywhere. BLAKE2b's kernels are the
exception. With AVX-512 off, its AVX2 kernel runs at 0.95–1.08 of the scalar path, and with AVX2 off, its 128-bit
SSSE3 kernel at 0.88–0.96. F7's target covers only the AdvSimd kernels, so x64 dispatch is unchanged, and BLAKE2b is
left for later.

How it was done, and where it departs from the design above:

- **The harness.**
  - `-p:BoduCryptoBaseline=1.0.0` builds against the published package again, which `#if !BODU_CRYPTO_BASELINE`
    now allows by leaving out the rows 1.0.0 cannot run: BLAKE3 over all cores, and scrypt with a thread bound.
  - `--disable-simd` sets the library's switch before the first use of any primitive, and every header names the
    state of the library's SIMD, so each result says what it measured.
  - `--argon2-harness --sweep` runs m from 1 to 32 MiB at p = 4, by default and on one thread.
- **The workflow and `harness_matrix.py`.**
  - `harness_matrix.py run` builds both builds for each framework, then runs each suite in each configuration in
    alternating order, one output file per run.
  - `harness_matrix.py summarize` prints a table per suite and framework, with each row's range over the runs and the
    ratios of the medians. Rates divide one way and times the other, and a per-operation row reads its time, since
    whole operations a second are too coarse for the key derivations.
  - The workflow runs it on the three machines, reports each processor, writes the tables into the job summary and
    uploads the raw outputs. It runs on manual dispatch, and on a pull request that changes the workflow, which is how
    this one ran.
- **Departure: Apple silicon was available.** The design allowed for it where it could be had. GitHub's `macos-15`
  runner is an M1, so the workflow runs there too. No Graviton runner is hosted.
- **Departure: gating, not tuning.** The target gates a losing kernel until it is tuned. The measurements say the
  rotations were not the cause, so nothing was tuned. `SimdCapabilities.AdvSimdSingleState`, which is closed, holds
  back the losing kernels; `SimdCapabilities.AdvSimd` still gates the others.
  - `Blake2bCore`, `Blake2sCore` and `ScryptCore` select their AdvSimd kernel only through the new gate, so on ARM64
    they select the scalar kernel. `Argon2Core` does so under .NET 10, and under .NET 8 selects the AdvSimd kernel
    through `SimdCapabilities.AdvSimd`, as before.
  - `Blake3Core.SelectSingleBlockKernel` gives the kernel for a single block: `SelectKernel`'s, except the scalar
    kernel in place of AdvSimd. The kindless `Compress`, `Compress(KernelKind.Auto, …)` and `HashOne`, which
    compresses a lone input among several, go through it. The four-input AdvSimd kernel is unchanged.
  - Every AdvSimd kernel stays in place, and the tests still drive each one explicitly on the ARM64 job and hold it
    to the scalar kernel.
- **Departure: test-first commits.** The design expected no new tests. Dispatch changing on ARM64 is a behaviour
  change, so the ARM64 selection tests of BLAKE2b, BLAKE2s, scrypt and Argon2 were changed first to expect the scalar
  kernel. Under qemu they failed against the ungated build, with `AdvSimd` selected, and passed against the gated one.
  Argon2's test was then split by runtime, to expect the AdvSimd kernel under .NET 8, and failed under qemu's .NET 8
  until dispatch followed the runtime.
  - The new members have tests of their own: the gate is closed, BLAKE3 selects the scalar kernel for a single block
    on ARM64 and the kernel `SelectKernel` selects everywhere else, and the SIMD-off assembly checks both.
  - The full suites pass on both runtimes, and on GitHub's ARM64 runners. On .NET 8 under qemu, the BLAKE2, BLAKE3
    and scrypt test classes pass with the gate in place, and Argon2's selection follows the runtime under .NET 8 and
    .NET 10.
- **The documentation.** The package README's acceleration table, the hardware-acceleration guide and CLAUDE.md name
  the scalar kernels as ARM64's for these primitives, and say why. The cost remark on `ChaCha20Core.StepCost` now
  cites F7: on the N2, a 64-byte XChaCha20-Poly1305 message sealed as fast under the AdvSimd kernel's plan as under
  the scalar kernel's, 1.00 on both runtimes.
- **Left for later.**
  - Tuning the gated kernels. The losing layout, not the rotations, is the cause, and the M1 hints that a core with
    more vector pipes can win: its Argon2 kernel ran 1.3–1.6 times as fast as its scalar path under .NET 8. Dispatch
    cannot tell an M1 from an N2, so under .NET 8 the N2 runs Argon2's AdvSimd kernel at 0.8 of its scalar path's
    speed. BLAKE2's parallel variants, BLAKE2bp and BLAKE2sp, would give each lane a state of its own, but they are
    different functions.
  - A Poly1305 AdvSimd kernel. F1's design had a two-lane one, which waited for this run. On the N2, OpenSSL's NEON
    code took 1 MiB at 3,156–3,174 MiB/s against the scalar loop's 1,610–1,652.
  - BLAKE2b on x64 without AVX-512: its AVX2 kernel ran at 0.95–1.08 of the scalar path, and its SSSE3 kernel at
    0.88–0.96. A five-round A/B would settle whether x64 should gate them too.
  - Argon2's thread threshold below 4 MiB. At the threshold the threads already cut the wall time 1.4–2.9 times on
    all three machines, so it may sit too high. Measuring below it needs the harness to reach the internal
    `FillOptions`.

### F8 — Poly1305 on ARM64 (done)

F8's figures come from F7's workflow, on the Neoverse N2 and the Apple M1; the EPYC runs only the Argon2 suites. The
crypto harness gained a case for each Poly1305 kernel at each length from 64 bytes to 1 MiB, which calls the library's
core with the kernel named, whatever dispatch would select, so one run places every threshold. The workflow ran on
three builds: the design's, with its two loops as separate kinds; one that splits a step's four blocks together; and
the build as it merges, which also carries with `USRA`.

**The kernel against the scalar loop.** The scalar loop's time over the kernel's, by the medians of the final build's
two runs; above 1, the kernel ran faster:

| Bytes | N2, .NET 8 | N2, .NET 10 | M1, .NET 8 | M1, .NET 10 |
|---|---|---|---|---|
| 64 | 0.75 | 0.75 | 0.92 | 0.89 |
| 128 | 0.88 | 0.88 | 1.18 | 1.14 |
| 192 | 0.98 | 0.98 | 1.41 | 1.39 |
| 256 | 1.06 | 1.05 | 1.69 | 1.57 |
| 384 | 1.18 | 1.17 | 1.83 | 1.83 |
| 512 | 1.27 | 1.24 | 2.11 | 2.13 |
| 1 KiB | 1.47 | 1.38 | 2.74 | 2.65 |
| 4 KiB | 1.70 | 1.55 | 3.21 | 3.48 |
| 16 KiB | 1.77 | 1.60 | 3.86 | 3.76 |
| 1 MiB | 1.80 | 1.62 | 3.77 | 3.96 |

Dispatch gives the kernel runs of 256 bytes or more, where it ran faster than the scalar loop on both processors and
both runtimes. The M1's kernel caught up at 128 bytes and the N2's at 256: below that the N2's scalar loop, whose
`umulh` multiplies 64 by 64 bits, finished first. Dispatch cannot tell the two processors apart, so the N2's threshold
serves both.

Over 1 MiB on the N2 the kernel ran Poly1305 1.80 times as fast as the scalar loop under .NET 8 and 1.62 times under
.NET 10, against the target's 1.5. That is 2,898 and 2,674 MiB/s, where OpenSSL's NEON code, on the same machine, ran
at 3,161–3,163 MiB/s.

**The three builds.** The scalar loop's time over each loop's for 1 MiB on the N2, by the medians of each run, with the
vector instructions a step of four blocks takes under .NET 10:

| Build | Instructions | .NET 8 | .NET 10 |
|---|---|---|---|
| The design's one-group loop: one group of two per step | — | 1.29 | 1.27 |
| The design's paired loop: two groups per step, each split into limbs on its own | 118 | 1.52 | 1.49 |
| The step's four blocks split together, one to each 32-bit lane | 109 | 1.70 | 1.62 |
| The same, carrying with `USRA` (the build as it merges) | 100 | 1.80 | 1.62 |

The first build ran the design's two loops as two kinds. On both processors the paired loop ran faster than the
one-group loop at every length where either beat the scalar loop, 256 bytes and up on the N2 and 192 and up on the
M1, so the one-group loop and its threshold went. Its 1.49–1.52 times sat at the target's edge, which the next two
builds cleared by cutting the instructions around the step's 50 multiplies. The carry's change registered under .NET 8
alone: the kernel took 364 µs per MiB before it and 345 after, while under .NET 10 it took 373 both times.

**The MAC and the AEADs.** On the N2, vector ÷ 1.0.0 for 1 MiB, in F7's last run, before F8, and in F8's last. Each
run measures 1.0.0 afresh, so each ratio holds within its run:

| 1 MiB on the N2 | .NET 8, before | .NET 8, after | .NET 10, before | .NET 10, after |
|---|---|---|---|---|
| Poly1305 | 1.63 | 2.93 | 1.50 | 2.43 |
| XChaCha20-Poly1305 | 2.07 | 2.47 | 1.97 | 2.27 |
| XSalsa20-Poly1305 | 2.45 | 3.01 | 2.01 | 2.39 |

In MiB/s, XChaCha20-Poly1305 went from 574–579 to 668–681, and XSalsa20-Poly1305 from 677–688 to 817–832. At 256
bytes, the shortest run the kernel takes, the MAC ran 1.03–1.04 times as fast as the scalar configuration on the N2,
and at 1 KiB 1.31–1.38 times.

How it was done, and where it departs from the design above:

- **The harness.**
  - `Poly1305KernelDriver` calls `Poly1305Core`'s `Initialize`, `Update(KernelKind, …)` and `Finish` through a
    dynamic method bound to the library's module, since the core is internal to the library; the kernels are the
    library's own compiled code.
  - It lists the kernels the processor supports, and the crypto harness times each at twelve lengths,
    `kernel/Poly1305 <kernel> <length>`.
  - `harness_matrix.py` runs those rows and the MAC and AEAD rows at 64, 256 and 512 bytes, 1 KiB and 1 MiB.
- **The kernel.** `Poly1305Core.Vector128Kernel.Blocks` takes two groups of two blocks per step while more than two
  groups remain, then one group, then the last, whose lanes it multiplies by `r²` and `r` and sums into the scalar
  state. A step:
  - loads its four blocks and gathers each 32-bit word of all four into one vector with two rounds of `UZP1` and
    `UZP2`, then forms the five limbs with shifts, `SLI` for a limb that straddles two words, masks, and `USRA` to add
    the top limb to the bit a full block sets: one block to each 32-bit lane, the first group in the lower halves and
    the second in the upper;
  - multiplies the second group by `r²` in place with `UMULL2` and `UMLAL2`
    (`MultiplyBySelectedScalarWideningUpper[AndAdd]`), taking each of the multiplier's limbs from a lane of another
    register, so that a power of `r` and five times its upper limbs fill three registers;
  - adds the first group to the accumulator, narrowed with `XTN`, and multiplies the sum by `r⁴` with `UMULL` and
    `UMLAL` into the same sums;
  - carries once, in two chains side by side, each carry one `USRA` and a mask. The carry out of 2^130, below 2^31, is
    narrowed by `SHRN` and folded back in times 5 by one `UMLAL`.

  `ComputePowers` forms `r²` and `r⁴`, two scalar multiplications, and the kernel clears them before it returns.
  `AdvSimdMinimumBytes` is 256. `KernelMinimumBytes`, the shortest run any kernel takes on the processor at hand, lets
  the block loop send shorter runs to the scalar loop without consulting dispatch.
- **The generated code.** Under qemu the step compiles without spills on both runtimes, the .NET 10.0.12 that the
  runners used included.
  - The two runtimes emit the same instructions, except that .NET 8 loads the masks and the full-block bit from the
    constant pool at each use, twelve loads a step, where .NET 10 keeps the 64-bit mask in a register and forms the
    32-bit one, the full-block bit and the 5 once a step.
  - Both copy three product sums into the accumulator's registers, for the `USRA`s that write them.
  - Taking the lower halves before the upper halves' products made both runtimes copy the message limbs, so the upper
    halves go first.
- **CA1857.** .NET 8's reference assembly bounds the shift counts of the 64-bit `USRA` and `SHRN` overloads at 16,
  though the instructions take up to 64 and 32. .NET 8's JIT emits them with 26, and .NET 10's annotations allow it,
  so the carry suppresses the analyzer with that reason.
- **Tests.** As the design has them, with one AdvSimd kind:
  - Every Update test that drives each kernel drives it.
  - `ComputePowers`, now internal, is held to `BigInteger` powers of `r` in every layout it fills.
  - Dispatch's ARM64 choices, the kernel minimum and `IsSupported` have tests of their own, and the SIMD-off assembly
    checks the scalar loop at every threshold.
  - The ARM64 jobs run them natively. Under qemu, the Poly1305 core, MAC and AEAD classes passed on both runtimes.
  - The kernel is new code, so its tests came with it rather than first.
- **Departure: one kind, not two.** The design selected the paired loop from a second threshold. The first run found
  it faster wherever the one-group loop ran, so there is one `AdvSimd` kind, and the one-group loop is gone.
- **Departure: the workflow ran on the pull request.** The design dispatched F7's workflow on the branch, but the
  integration that opened the pull request may not dispatch workflows. The workflow already ran on a pull request that
  changes it; it now also runs on one that changes `harness_matrix.py`, whose filters choose the cases and whose parser
  reads them.
- **Constant time.** The kernel branches only on the run's length, which is public.
- **Left for later.**
  - .NET 10's step. On the N2 it ran 8% slower than .NET 8's, and the carry's change did not register there, though
    the two differ only in how they form constants.
  - `LD4`, which .NET 10 exposes as `AdvSimd.Arm64.Load4xVector128AndUnzip`, would load a step's four blocks with
    their words already gathered, in place of four loads and eight `UZP`s.
  - The M1's crossover. Its kernel caught the scalar loop at 128 bytes, but runs of 128 to 255 bytes take the scalar
    loop, because on the N2 that loop is faster there.
  - OpenSSL splits one group's blocks with integer instructions, whose pipes the vector code leaves idle.
