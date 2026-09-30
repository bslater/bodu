# Implementation plan: a faster Argon2id

**Status:** Done and released: merged in #710 and shipped in `Bodu.Security.Cryptography` 1.1.0,
released out of band (#711); measured on one x64 machine (§10), then on ARM64 and a small-L3 x64 machine (§10.3) · **Source:** FallbackPlan requirements document "a faster
Argon2id in Bodu" (`ARG-F-*` / `ARG-N-*`, raised 2026-09-27 against 1.0.0) ·
**Target:** `Bodu.Security.Cryptography` 1.1.0

This plan maps the requirements statement onto the repository at `40068ba`. That is
the commit the requirements' source links point at, and the current `master`, whose
Argon2 sources have not changed since the 1.0.0 cut. The plan answers the three open
questions and breaks the work into ordered phases that can each be committed on its
own. Its decisions rest on a throwaway feasibility spike (§2), run on a machine that
matches the requirements' §2.1 description. Nothing from the spike is committed.

---

## 1. Verified current state

Every claim in the requirements' §2.2 holds at `40068ba`. The table checks each
against the code and adds what the requirements did not know.

| Requirements claim | Repository reality |
|---|---|
| Lanes are filled one after another (`Argon2Core.DeriveTag` L66–73; the `Argon2` remarks) | Confirmed. The user guide says so too (`docs/guides/cryptography/argon2.md`, "Not multi-threaded here"). |
| Every call allocates the matrix (L58) and clears it after use (L79) | Confirmed. The spike prices it at about 20 ms of a warm derivation once the kernel is vectorised (§2). |
| The compression function is scalar, while BLAKE2, BLAKE3, Threefish and CubeHash already dispatch to AVX-512 | Confirmed, and the gap is wider than stated: **the package has no AVX2 or ARM64 path anywhere.** Every existing kernel is AVX-512, apart from PCLMULQDQ for GHASH. `SimdCapabilities` exposes only `Avx512F`, `Avx512FVL` and `Pclmulqdq`. Nothing in the repository references `System.Runtime.Intrinsics.Arm`, and CI runs only on `ubuntu-latest` (x64). ARG-N-002 therefore brings the package's first AVX2 gate, its first ARM64 gate, and the repository's first ARM64 CI run. |
| Stack scratch is zeroed for every block; the project does not set `SkipLocalsInit` | Confirmed. `SkipLocalsInit` also needs `AllowUnsafeBlocks`, which the project does not set; `bld/Compiler.props` makes it an explicit per-project opt-in. On its own, `[SkipLocalsInit]` saved about 6 % of 1.0.0's CPU in the spike. The restructured core removes per-block scratch altogether. |
| ARG-N-006: "as 1.0.0 clears the matrix and H0 today" | **Partly.** 1.0.0 clears the matrix, H0 and its two heap buffers, but not its password-derived **stack** scratch. Uncleared: `FillBlock`'s two working blocks, `Permute`'s column, `InitializeBlocks`' input (a copy of H0) and output block, `Finalize`'s final block, and the bundled BLAKE2b's state, working vector, message words and final block. The last two hold raw password bytes while H0 is computed. They are released uncleared when each frame returns. The H0 input is also a heap array that a compacting collection may move before it is cleared. The new code has to close these gaps, not just preserve 1.0.0's clearing. |
| ARG-F-001: RFC 9106's vectors and Bodu's own tests prove the output unchanged | **Not yet provable.** The suite holds three vectors, all from RFC 9106 §5 (m = 32 KiB, t = 3, p = 4, version 0x13, two-block segments). It has no vector for version 0x10, for a tag over 64 bytes, or for p = 1, and none with a segment longer than 128 blocks — so the address-block regeneration inside a segment is never checked. Phase 0 closes this before any code moves. The spike checked 1.0.0 against the reference implementation's version 0x10 tags (`kats/argon2{d,i,id}_v16`) and two rows from its `src/test.c`: all match, so importing them is green today. |
| ARG-N-008: the scalar path held to the vectors "as the package already does for BLAKE2, BLAKE3 and Threefish" | Confirmed: `Bodu.Security.Cryptography.Simd.Test` links their KAT suites and sets the switch in `runtimeconfig.template.json`. The coverage tooling knows only AVX-512, though: `tools/New-CoverageMatrix.ps1` treats `*.Avx512.cs` as hardware-gated, so an ARM64-only file would read 0 % on x64 CI. |
| §2.1's figures | Reproduced within noise on a 4-vCPU Intel Xeon at 2.1 GHz with AVX2 and AVX-512, on .NET 10.0.0: 236 ms wall and 232 ms CPU at p = 4, and 215 ms at p = 1. |

Four further findings:

- **The first call in a process costs about 1.6 times a warm call.** In a fresh
  process, 1.0.0's first derivation took 347–407 ms against 221–262 ms for the
  second, because the fill loop starts on unoptimised tier-0 JIT code (Table C).
  FallbackPlan's CLI unlock is such a first call, and §2.1 measured only warm calls.
- **The spike's host holds the whole matrix in cache.** It matches §2.1's
  description, and its L3 is 260 MiB, so one 64 MiB matrix fits and four (256 MiB)
  do not. If §2.1's machine is the same class, that is the likelier reading of its
  "four at once is limited by the memory system". A desktop or laptop typically has
  8–36 MiB of last-level cache, so its single-derivation numbers will be less
  flattering. Phase 7 measures on one.
- **The Argon2 tests do not follow the repository's layout rules.**
  `Argon2CoverageTests` is the catch-all name `CLAUDE.md` rules out, there is no
  member-named backbone, and the RFC runner hand-rolls its display name instead of
  binding `KatDisplayName`.
- **The package README does not mention Argon2**, and neither does the cryptography
  row in `CLAUDE.md`.

## 2. Feasibility spike

A throwaway console application, not committed, copied the 1.0.0 core verbatim and
built alternatives beside it:

- a restructured scalar kernel;
- a 128-bit kernel in the reference implementation's SSSE3 shape;
- an AVX2 kernel in its AVX2 shape;
- lanes on threads, one `Parallel.For` per slice;
- three ways to hold the matrix: managed per call (1.0.0), native per call, and
  native and reused.

Every configuration reproduced the RFC 9106 §5 vectors. For all three variants, each
also matched 1.0.0's tags bit for bit on shapes the RFC never reaches: version 0x10,
100-byte tags, m not a multiple of 4p, p ∈ {1, 3, 4, 8}, and 64 MiB. Measurements
follow the requirements' appendix method (six warm-ups, then ten calls) on the host in
§1. Each figure is a single run, so allow ±10 %.

**Table A — one derivation (Argon2id, 64 MiB, t = 3, p = 4, warm).** "In OS" is the
share of CPU spent in the operating system, mostly on page faults.

| Configuration | Wall | CPU (in OS) | CPU vs 1.0.0 | Allocated | Gen2 per call |
|---|--:|--:|--:|--:|--:|
| 1.0.0 | 236 ms | 232 ms (6) | 100 % | 64 MiB | 0.5 |
| 1.0.0 with `[SkipLocalsInit]` | 217 | 218 (3) | 94 % | 64 MiB | 0.5 |
| Scalar, 1 thread, managed per call | 174 | 178 (6) | 77 % | 64 MiB | 0.5 |
| Scalar, 1 thread, reused | 146 | 147 (0) | 64 % | 2 KiB | 0 |
| Scalar, 4 threads, reused | 55 | 166 (3) | 72 % | 28 KiB | 0 |
| 128-bit, 1 thread, native per call | 136 | 135 (18) | 58 % | 2 KiB | 0 |
| 128-bit, 1 thread, reused | 113 | 114 (0) | 49 % | 2 KiB | 0 |
| 128-bit, 4 threads, native per call | 50 | 157 (27) | 68 % | 28 KiB | 0 |
| 128-bit, 4 threads, reused | 36 | 123 (0) | 53 % | 28 KiB | 0 |
| AVX2, 1 thread, managed per call | 88 | 91 (4) | 39 % | 64 MiB | 0.5 |
| AVX2, 1 thread, native per call | 90 | 89 (17) | 38 % | 2 KiB | 0 |
| AVX2, 1 thread, reused | 69 | 69 (0) | 30 % | 2 KiB | 0 |
| AVX2, 4 threads, managed per call | 35 | 101 (4) | 44 % | 64 MiB | 0.5 |
| AVX2, 4 threads, native per call | 37 | 117 (33) | 50 % | 28 KiB | 0 |
| AVX2, 4 threads, reused | 27 | 80 (1) | 35 % | 28 KiB | 0 |

**Table B — four at once (four dedicated threads, each deriving four times).**

| Configuration | All 16 | Each | CPU each | Throughput vs 1.0.0 |
|---|--:|--:|--:|--:|
| 1.0.0 | 2,145 ms | 134 ms | 493 ms | 1.0× |
| AVX2, 1 thread each, native per call | 785 | 49 | 169 | 2.7× |
| AVX2, 1 thread each, reused | 416 | 26 | 95 | 5.2× |
| AVX2, 4 threads each, native per call | 577 | 36 | 134 | 3.7× |
| AVX2, 4 threads each, reused | 377 | 24 | 89 | 5.7× |
| Scalar, 4 threads each, reused | 746 | 47 | 180 | 2.9× |

With the callers on thread-pool threads instead (`Task.Run`, three runs each),
AVX2 with reuse gave these totals:

- Four callers × four derivations: 630–697 ms at one thread each, 360–408 ms at
  four. 1.0.0: 1,182–1,698 ms.
- Eight callers × four on the four cores: 779–1,008 ms at one thread each,
  777–834 ms at four. 1.0.0: 2,758–2,787 ms.

**Table C — the first call in a fresh process (64 MiB, p = 4, native per call; three
processes each).** AO is `[MethodImpl(MethodImplOptions.AggressiveOptimization)]` on
the fill loop and the kernels.

| Configuration | First call | Second call |
|---|--:|--:|
| 1.0.0 | 347–407 ms | 221–262 ms |
| AVX2, 1 thread | 257–269 | 100–108 |
| AVX2, 1 thread, with AO | 120–130 | 95–106 |
| AVX2, 4 threads | 189–207 | 58–147 |
| AVX2, 4 threads, with AO | 76–81 | 44–51 |

**Table D — where threads start to pay (AVX2, reused, p = 4, t = 3).** The last
column is the CPU with four threads divided by the CPU with one.

| m | Blocks per segment | 1 thread | 4 threads | CPU cost of threads |
|---|--:|--:|--:|--:|
| 256 KiB | 16 | 0.39 ms | 0.52 ms | 3.3× |
| 1 MiB | 64 | 1.09 | 0.95 | 2.5× |
| 4 MiB | 256 | 3.96 | 2.62 | 2.1× |
| 16 MiB | 1,024 | 17.0 | 7.3 | 1.4× |
| 64 MiB | 4,096 | 69 | 27 | 1.17× |

What the spike shows:

1. **Every x64 target is reachable, with margin.**
   - ARG-N-001 asks for at most 40 % of 1.0.0's wall time. Threads alone reach 23 %
     on the scalar kernel, and 11 % with AVX2.
   - ARG-N-002 asks for at most 60 % of its CPU. AVX2 reaches 30–50 % in every
     memory mode.
   - ARG-N-003 asks that scalar be no slower. The restructured scalar kernel costs
     64–77 %.
   - ARG-N-005 asks four at once to keep 1.0.0's throughput. Every configuration
     reaches 2.7–5.7 times it.
2. **Once the kernel is vectorised, getting the matrix is a fifth of the cost, and
   native allocation per call does not remove it.** The operating system zero-fills
   fresh pages on first touch. So the ~20 ms the runtime spent zeroing and
   collecting moves into operating-system time (17–33 ms per call), and it contends when
   derivations overlap: in Table B, native per call took 577 ms where reuse took
   377 ms. Only reuse removes it.
3. **Reuse is what gives the ARM64 target its margin.** The 128-bit kernel has the
   shape an AdvSimd kernel will have. It costs 49–53 % with reuse but 68 % with
   per-call native memory and threads. Only an ARM64 run can confirm this, and it is
   the riskiest target in the requirements.
4. **Threads by default cost nothing when derivations are already concurrent.** That
   held with dedicated and thread-pool callers, and with twice as many callers as
   cores. Threads pay from about 64 blocks per segment (Table D).
5. **The first call is dominated by the JIT, not the algorithm.** AO roughly halves
   it. With threads, a fresh process's first 64 MiB derivation drops from 1.0.0's
   ~350–400 ms to ~80 ms.

## 3. Decisions

### 3.1 Threads: by default (open question 1)

The existing constructors and one-shot methods fill lanes on up to
min(p, `Environment.ProcessorCount`) threads, the calling thread included. They do so
whenever a segment is at least **256 blocks**, the point in Table D where four threads
first cut the wall time by a third; Phase 7 settles the number. Smaller memory stays
on the calling thread, as do platforms without threads (browser, WASI). The reasons:

- p is RFC 9106's declared degree of parallelism. The reference implementation's
  high-level API, Konscious and Go's `x/crypto/argon2` all run p lanes on up to p
  threads.
- It is FallbackPlan's stated preference and the requirements' headline latency win.
- The spike found no throughput cost when derivations were already concurrent
  (finding 4).
- The fork/join cannot deadlock or wait on a starved thread pool. The caller fills
  lanes itself, and if no worker ever arrives it fills them all, which is exactly
  1.0.0's behaviour.

`MerkleTree` defaults to one thread, and the difference is deliberate. Its parallel
mode was an opt-in trade that costs a hash algorithm and a buffer per worker. p, by
contrast, is the parameter through which the caller has already said how many lanes
may run at once. Callers who know better have ARG-F-003's bound (§4.1).

### 3.2 The matrix: native memory, reused through a small bounded pool (open question 2)

Each derivation takes its matrix from a process-wide pool of 64-byte-aligned native
buffers (`NativeMemory.AlignedAlloc`):

- A buffer is **cleared every time it is returned**.
- It is then kept only if the pool holds fewer than `Environment.ProcessorCount`
  buffers and the buffer is at most 256 MiB.
- A timer frees buffers left idle for 30 seconds, and stops when the pool is empty.
- The `AppContext` switch `Bodu.Security.Cryptography.Argon2.DisableMatrixReuse`
  turns retention off.
- Above the size cap, or with the switch set, the buffer is freed straight away. It
  is still native memory, so it still causes no collection.

The requirements offered three options, and the spike measured them:

- *Native memory, freed per call*, meets the letter of ARG-N-004 but not its
  purpose. The operating system's page faults replace the runtime's zeroing
  (finding 2) and cost more when derivations overlap.
- *A buffer per thread* keeps memory resident for every thread that ever derived.
  Rejected, as the requirements expected.
- *An uninitialised managed array* is still one large-object allocation per call,
  followed by the gen2 collections. It fails ARG-N-004.

Native memory is preferred to a pooled managed array for three reasons:

- A trimmed native buffer is released at once. A dropped array waits for a gen2
  collection, which is exactly what ARG-N-004 removes.
- It is 64-byte aligned, so no 32-byte load straddles a cache line.
- It never moves.

The price is `AllowUnsafeBlocks` for the project, with the pointer code confined to
one small type. The same switch also enables `[SkipLocalsInit]`.

The docs state the residency: at most `ProcessorCount` × m, for 30 seconds after the
last derivation, all of it zero. Because a buffer is cleared before it is retained,
ARG-N-006 holds for the pool by construction.

### 3.3 Version: 1.1.0 (open question 3)

The API change is additive (§4.1), so it is a SemVer minor in the Stable stream.
Under lock-step it ships at the next `BoduBaseVersion` bump; `bld/RELEASING.md`'s
"Next waves" already anticipates 1.1.0 for the next coordinated wave. Package
validation against the published baseline enforces ARG-F-002 at pack time. If
FallbackPlan cannot wait for the wave, RELEASING.md's "Out-of-band single-package
fix" route can ship this package alone at 1.1.0 and let the wave catch up.

### 3.4 Also decided here

- **Lock the output before changing it** (Phase 0). The current suite cannot prove
  ARG-F-001 (§1).
- **The thread bound lives on the instance, not in `Argon2Parameters`.** The record's
  equality and `ToString` would pick it up, and a PHC string cannot carry it.
  ARG-F-003 also separates p, which is part of the result, from the bound, which is
  not.
- **No AVX-512 kernel in this work.** AVX2 already costs 30–35 % of 1.0.0's CPU.
  What remains is the matrix and memory latency, which wider registers do not touch.
  Phase 7 revisits it only if a prototype gains at least 15 % on the §2.1 machine.
- **AO on the fill loop and the kernels**, for the first call (finding 5).

## 4. Design

### 4.1 Public API (ARG-F-002, ARG-F-003)

```csharp
public abstract class Argon2
{
    // New: the greatest number of threads one derivation may use; -1 lets the library choose.
    public int MaxDegreeOfParallelism { get; }

    // New: verify with a bound; dispatches on the variant named in the string, as Verify does.
    public static bool Verify(
        string encoded, ReadOnlySpan<byte> password, ReadOnlySpan<byte> secret, int maxDegreeOfParallelism);

    // New, but not public: the constructors below delegate to it.
    private protected Argon2(Argon2Parameters parameters, int maxDegreeOfParallelism);
}

public sealed class Argon2id : Argon2   // and likewise Argon2i and Argon2d
{
    public Argon2id(Argon2Parameters parameters);                              // unchanged; the bound is -1
    public Argon2id(Argon2Parameters parameters, int maxDegreeOfParallelism);  // new
}
```

- **Values.** `−1`, the default, lets the library choose (§3.1). `1` confines the
  derivation to the calling thread, which is how 1.0.0 runs. `n ≥ 2` allows at most n
  threads, the caller's included. The value is always an upper bound: a derivation
  never uses more threads than lanes, and uses fewer when segments are small. The tag
  never depends on it.
- **Validation.** `0`, or anything below `−1`, throws `ArgumentOutOfRangeException`.
  `MerkleTree` already enforces the same rule. With a second call site, the
  helper rule in `CLAUDE.md` applies: its private guard is promoted to
  `CryptographyThrowHelper.ThrowIfDegreeOfParallelismInvalid`, with a neutral resx
  message.
- **No new one-shot overloads.** `new Argon2id(parameters, 1).GetBytes(password,
  salt)` is the same single line a `DeriveKey` overload would be. If someone asks for
  the overloads later, adding them is also additive.
- **Everything else is unchanged.** `Argon2id.DeriveKey`, `GetBytes`, `Hash`,
  `Verify`, `Argon2Parameters` and the PHC encoding keep their signatures and meaning,
  so FallbackPlan adopts the new version with a version bump alone.

### 4.2 Execution: lanes on threads

- **Each slice is a fork/join over the lanes.** The fork is
  `Parallel.For(0, lanes, …)`, with `MaxDegreeOfParallelism = n` and
  `TaskScheduler = TaskScheduler.Default` set explicitly, so a caller's constrained
  scheduler is never used. The calling thread fills lanes too.
- **Each join is RFC 9106 §3.4's synchronisation point.** There are 4t joins per
  derivation, twelve at t = 3. A join publishes one slice's blocks to the next
  slice's readers. That is also what makes references across lanes safe under
  ARM64's weaker memory model.
- The threshold and platforms without threads route to the calling thread (§3.1).
- A worker's fault surfaces as itself, not wrapped in an `AggregateException`,
  following `MerkleTree.RunParallel`.
- There is no cancellation: a derivation lasts tens of milliseconds.
- The matrix is cleared and returned only after the final join, never while a worker
  could still write to it.
- **Per-worker scratch is allocated per segment, not per block.** It holds the carried
  block, the XOR copy, and the address, input and zero blocks, about 7 KiB in all. It
  is allocated on the stack and cleared when the segment ends.
- If Phase 7 shows that the parallel CPU overhead (+17 % at 64 MiB) matters, persistent
  workers that claim lanes from a per-slice counter can replace `Parallel.For`.
  Whatever replaces it must keep the caller participating.

### 4.3 Compression kernels

- **The previous block is carried across a segment**, in registers and scratch, as the
  reference implementation's optimised fill does. Each block then reads only its
  reference block (and, on XOR passes, its destination). Nothing is copied or zeroed
  per block.
- **The fill loop is generic over a struct kernel**: `FillSegment<TKernel>`, using
  static abstract members. The JIT emits one specialised loop per kernel, with no
  dispatch per block.
- **Scalar.** 1.0.0's arithmetic, restructured: carried state, refs rather than span
  slices, and no column copy. In the spike it was 23 % cheaper than 1.0.0 with
  1.0.0's allocation, and 36 % cheaper with reuse (ARG-N-003).
- **AVX2.** `Vector256<ulong>`, in the AVX2 shape of the reference implementation's
  `blamka-round-opt.h`. The multiply is `VPMULUDQ`. Rotate-32 is `VPSHUFD`,
  rotate-24 and rotate-16 are `VPSHUFB`, and rotate-63 is an add, a shift and an xor.
  Diagonalising uses `VPERMQ` and `VPBLENDD`. Each round handles two rows or two
  columns.
- **128-bit.** `Vector128<ulong>`, in the reference implementation's SSSE3 shape. The
  kernel is written once and specialised by a struct *ISA shim* of five operations.
  Everything else is portable `Vector128` arithmetic.

  | Operation | x64 (`Ssse3Isa`) | ARM64 (`AdvSimdIsa`) |
  |---|---|---|
  | Multiply the low 32 bits of each lane into 64 | `PMULUDQ` | `XTN` twice, then `UMULL` |
  | Rotate right by 32 | `PSHUFD` | `REV64` on 32-bit elements |
  | Rotate right by 24 or 16 | `PSHUFB` | `TBL` |
  | Take one half from each of two registers | `PALIGNR` | `EXT` |

  The payoff is testability: the kernel's logic runs on every x64 CI host, and only
  five one-line members run on ARM64 alone.
- **Dispatch** happens once per derivation, in the order AVX2, ARM64, SSSE3, scalar.
  Each is checked through `SimdCapabilities`, which gains `Avx2`, `Ssse3` and
  `AdvSimd` gates that honour the existing `DisableSimd` switch.
- **Attributes.** The fill loop and the kernels carry AO. The Argon2 types carry
  `[SkipLocalsInit]` once every `stackalloc` has been shown to be written before it
  is read.

### 4.4 Matrix ownership

- **`Argon2Matrix`** owns one derivation's native region and hands out block
  references as `ref ulong`; this is the only pointer code. On disposal it clears the
  region with `CryptographicOperations.ZeroMemory`, then returns it to the pool. The
  clear runs in chunks, because the 2 GiB ceiling exceeds one span.
- **`Argon2MatrixPool`** is lock-protected. Renting takes the smallest retained buffer
  that fits, or allocates one. Returning clears the buffer, then retains or frees it
  per §3.2. The idle timer takes a `TimeProvider`, so tests can drive it.
- **No block is read before it is written.** Pass 0 writes every block before
  anything references it, and columns 0 and 1 come from H'. A pooled buffer is all
  zero, and so is a fresh page, so a read-before-write bug would go unnoticed. A test
  pins the invariant by running the fill over a deliberately poisoned matrix
  (Phase 3).
- **H0 and H' stop copying their inputs.** `Argon2Blake2b` gains an incremental state
  (initialise, update, finalise), so neither the password nor H0 is ever copied into
  a heap array.

### 4.5 Clearing and data independence (ARG-N-006, ARG-N-007)

- **Cleared on every derivation:** the matrix, before it is retained or freed; H0;
  each worker's per-segment scratch; and the initial-block, final-block and BLAKE2b
  buffers when their methods exit. Together these close 1.0.0's stack gaps (§1).
- **Out of reach:** values the JIT holds in registers or spills to its own stack
  slots. The `Argon2` remarks say so, as the README already does for memory the JIT
  and GC control.
- **The kernels have no branches and no secret-indexed tables.** `PSHUFB` and `TBL`
  apply constant indices: a fixed byte permutation, not a lookup. The 32-bit multiply
  has a fixed latency on every targeted ISA.
- **The data-independent segments stay data-independent.** They take reference
  positions only from the address block, which is computed from public inputs. Which
  thread fills which lane depends on timing, never on data.
- A review checklist item records all of this for every kernel.

### 4.6 Files

All files sit flat in `src/Security.Cryptography/`, one type per file, with partials
nested by `.filenesting.json`:

```text
Argon2Core.cs              H0, initial blocks, pass/slice loop, finalisation, dispatch
Argon2Core.Segment.cs      FillSegment<TKernel>, reference indexing, address blocks
Argon2Core.Kernel.cs       IArgon2Kernel (static abstract compression)
Argon2Core.Scalar.cs       ScalarKernel
Argon2Core.Avx2.cs         Avx2Kernel
Argon2Core.Vector128.cs    Vector128Kernel<TIsa>, IVector128Isa
Argon2Core.Ssse3.cs        Ssse3Isa
Argon2Core.AdvSimd.cs      AdvSimdIsa (runs only on ARM64; hardware-gated for coverage)
Argon2Matrix.cs            one derivation's native matrix
Argon2MatrixPool.cs        bounded, idle-trimmed reuse
Argon2Blake2b.State.cs     incremental BLAKE2b
```

## 5. Work phases

Each phase is one or more commits on the session branch. Every phase leaves
`dotnet test bodu.slnx --settings test.runsettings` green; that is CI's tier, and it
includes Regression. Every commit after Phase 0 is checked against the three output
oracles Phase 0 establishes.

### Phase 0 — lock the output (ARG-F-001); no production change

- **Reorganise the Argon2 tests** into member-named backbone partials:
  `Argon2Tests.Ctor`, `.DeriveKey`, `.GetBytes`, `.Hash` and `.Verify`, plus
  `.KnownAnswers`. Fold `Argon2CoverageTests` into them, and bind the vector runner
  through `KatDisplayName`. No test is added or removed.
- **Import the reference implementation's vectors**, each file with a provenance
  header:
  - `kats/argon2{d,i,id}_v16`: version 0x10 for all three variants, on RFC 9106 §5's
    inputs.
  - `src/test.c`: Argon2i at versions 0x10 and 0x13, and Argon2id at 0x13, over
    t ∈ {1, 2, 4}, m ∈ {2⁸, 2¹⁶, 2¹⁸} KiB, p ∈ {1, 2}, and varied password and salt.
    The 256 MiB rows go in Regression. Argon2i's 1 GiB `TEST_LARGE_RAM` rows go in
    Stress.
- **Record 1.0.0's own output** as an embedded corpus with provenance, generated once
  from the published 1.0.0 package. Its Argon2 sources are the same as at `40068ba`.
  The grid covers:
  - all three variants and both versions;
  - p ∈ {1, 2, 3, 4, 5, 8};
  - m at the 8p minimum, at values that are not multiples of 4p, and large enough for
    segments over 128 blocks;
  - t ∈ {1, 2, 3} and T ∈ {4, 32, 64, 65, 128, 1024};
  - with and without secret and associated data.

  BVT cost stays within seconds, with heavy rows in Regression. Regenerate
  `kat-census.txt`.

### Phase 1 — measure first (ARG-N-009)

- **`Argon2Benchmarks`**, using BenchmarkDotNet with `[MemoryDiagnoser]`. It runs
  Argon2id at 64 MiB and at 256 KiB, with t = 3 and p = 4, one at a time and four at
  once. Its jobs:
  - the default;
  - `DOTNET_EnableAVX2=0`, which selects the 128-bit path;
  - `DOTNET_EnableHWIntrinsic=0`, which selects scalar;
  - a cold-start job, for the first call in a fresh process.

  The thread-bound axis joins in Phase 4.
- **A harness mode that reproduces the requirements' appendix `Measure`**: wall, CPU,
  cores used, allocation, gen2 and pause, so results line up with their §2.1 table.
  An MSBuild property builds it against either the project or the published 1.0.0
  package, so both baselines run from one source.
- **Record the 1.0.0 baselines on three machines**: the §2.1 class (4 vCPU, AVX-512,
  large L3); an x64 AVX2 desktop or laptop with a small L3; and Apple silicon.

### Phase 2 — restructure without changing the arithmetic (ARG-N-003, ARG-N-006)

- The carried-state scalar kernel, `FillSegment<TKernel>`, per-segment scratch, and
  AO.
- `Argon2Blake2b`'s incremental state for H0 and H', and clearing every scratch buffer
  on exit.
- **Exit:** all oracles pass, and scalar CPU is no more than 1.0.0's. The spike's
  equivalent measured 77 %. The first call is measured.

### Phase 3 — matrix ownership (ARG-N-004, ARG-N-006)

- Set `AllowUnsafeBlocks` for the project, which also defines `ALLOW_UNSAFE`. Add
  `Argon2Matrix`, and `Argon2MatrixPool` with its retention switch. Put
  `[SkipLocalsInit]` on the Argon2 types.
- Tests:
  - **The poisoned matrix.** An internal seam lends the core a matrix pre-filled with
    garbage, and every vector must still match.
  - **The pool.** Renting returns a buffer at least as large as asked for, and a
    returned buffer reads all zero. The count and size caps hold. A fake
    `TimeProvider` drives the idle release. The switch turns retention off.
  - **Allocation.** A loop of derivations with the bound at 1 allocates under 1 MiB
    per call, measured with `GC.GetAllocatedBytesForCurrentThread`. Process-wide GC
    counters are left to the benchmark, because MSTest runs tests in parallel.
- **Exit:** in the benchmark, no gen2 collections, under 1 MiB allocated per call, and
  no operating-system time per call once warm.

### Phase 4 — lanes on threads (ARG-N-001, ARG-F-003)

- The API in §4.1; the promoted guard and its resx message; the per-slice fork/join;
  the threshold; the fallback for platforms without threads.
- Tests:
  - **Validation.** `0` and `−2` throw with the parameter name, and the property
    echoes the constructor.
  - **Output independence.** The tag is identical for bounds {1, 2, 3, 4, −1, 64} ×
    p ∈ {1, 3, 4, 8}, across the Phase 0 corpus.
  - **`Verify` with a bound.**
  - **Concurrency.** Many concurrent derivations all produce correct tags
    (Regression).
- **Exit:** on the §2.1 machine, wall time at most 40 % of 1.0.0's (ARG-N-001), and
  four at once at no less than 1.0.0's throughput (ARG-N-005).

### Phase 5 — vector kernels (ARG-N-002, ARG-N-007)

- The `Avx2`, `Ssse3` and `AdvSimd` gates on `SimdCapabilities`. Its remarks are
  rewritten, since it is no longer AVX-512-only.
- The AVX2 kernel, the 128-bit kernel with its two shims, and dispatch.
- Tests:
  - **Differential.** Every kernel the host supports is checked against the scalar
    kernel on seeded random blocks, in both XOR modes. Unsupported kernels report
    `Assert.Inconclusive`, as `GaloisField128Tests` does.
  - **Shims.** Each shim operation is checked against its scalar definition.
  - **Vectors per kernel.** An internal seam runs the whole vector corpus through
    each supported kernel explicitly. One AVX2 host thereby covers the AVX2, SSSE3
    and scalar kernels, whichever one dispatch picks.
- `Bodu.Security.Cryptography.Simd.Test` links the `KnownAnswers` partial and its
  fixtures, using the `LogicalName` indirection it already uses for BLAKE2.
  `SimdOptOutTests` asserts that the new gates are off.
- **Exit:** on both x64 machines, CPU at most 60 % of 1.0.0's with AVX2 (ARG-N-002).

### Phase 6 — ARM64 in CI, and coverage tooling (ARG-N-008)

- **A job on `ubuntu-24.04-arm`**, GitHub's hosted ARM64 runner, which is free for
  public repositories. It builds and runs both cryptography test projects on both
  frameworks with `test.runsettings`, following `build-test.yml`'s steps. A test
  asserts that `SimdCapabilities.AdvSimd` is on when running on ARM64, so the job
  cannot silently fall back to scalar. The job proves correctness; Phase 7 measures
  performance.
- **Coverage.** `tools/New-CoverageMatrix.ps1` treats `*.AdvSimd.cs` as
  hardware-gated, as it does `*.Avx512.cs`. The header of `bld/collect-coverage.sh`
  and `docs/articles/code-coverage.md` are updated to match.

### Phase 7 — measure against the targets and settle the tunables

- Run Phase 1's harness and benchmarks on the three machines, 1.0.0 against the new
  build. Record the results in a Results section of this plan, for FallbackPlan's §8.
- **Gate:** on Apple silicon, CPU at most 60 % of 1.0.0's. This is the riskiest
  target. If it is missed, tune the shim first (for example `SHL`/`SRI` in place of
  `TBL`), before re-planning.
- Settle from the data: the thread threshold, and the pool's count cap and idle
  period.
- Decide two deferred items, also from the data:
  - an AVX-512 kernel, only if it gains at least 15 %;
  - a process-wide default for the bound through `AppContext`, for test hosts that
    cannot change code, only if four at once regresses under the default on any
    machine.

### Phase 8 — documentation and release

- **XML documentation**: the `Argon2` remarks (threads, the bound, reuse and
  residency, the first call, and the limits of clearing), the new members, and
  `SimdCapabilities`.
- **`docs/guides/cryptography/argon2.md`**: replace "Not multi-threaded here" with a
  performance section. It covers the bound, residency (concurrency × m), both
  switches, and sizing.
- **Other docs**: add Argon2 to the package README's matrix; update `CLAUDE.md`'s
  cryptography row, its Key Types and its Simd.Test description; add a `ROADMAP.md`
  entry.
- **Release** 1.1.0 at the next lock-step bump. The release notes lead with the two
  behaviour changes and how to turn each off: threads by default, and retained
  (cleared) matrices.

## 6. Traceability

| Requirement | Disposition |
|---|---|
| ARG-F-001 | Phase 0 (three oracles); every later phase runs them |
| ARG-F-002 | §4.1 (additive only); package validation at pack |
| ARG-F-003 | Phase 4 (`maxDegreeOfParallelism`) |
| ARG-N-001 | Phase 4; spike: 11–23 % of 1.0.0's wall |
| ARG-N-002 | Phase 5 (x64), Phases 6–7 (ARM64); spike: AVX2 30–50 %, 128-bit 49–53 % with reuse |
| ARG-N-003 | Phase 2; spike: 64–77 % |
| ARG-N-004 | Phase 3; spike: 2–28 KiB per call, no gen2 |
| ARG-N-005 | Phase 4 exit, Phase 7; spike: 2.7–5.7× |
| ARG-N-006 | Phases 2–3 (§4.5), including 1.0.0's stack gaps |
| ARG-N-007 | Phase 5 (§4.5); review checklist |
| ARG-N-008 | Phases 5–6 |
| ARG-N-009 | Phase 1, extended in Phase 4, run in Phase 7 |
| ARG-N-010 | In-box APIs only (`System.Runtime.Intrinsics`, `NativeMemory`, `Parallel`, `TimeProvider`). Target frameworks unchanged. `IsAotCompatible` kept, with a NativeAOT smoke check of which kernel an AOT build selects |

## 7. Risks

| Risk | Mitigation |
|---|---|
| ARM64 misses the 60 % CPU target; only an x64 stand-in has been measured | Reuse gives margin; Phase 7 measures on Apple silicon before release; shim tuning |
| Machines with a small L3 are bound by memory latency, so the §2.1 machine flatters the numbers | Phase 7 measures on a small-L3 desktop; every target is relative to 1.0.0 on the same machine |
| The pool's residency surprises a host | Only cleared buffers are held; count and size caps; release after 30 s idle; a switch; residency documented |
| Threads by default hurt a saturated server | The caller participates, so no starvation; the threshold; the bound. Phase 7's concurrency runs decide the `AppContext` default |
| NativeAOT's default instruction set does not select AVX2 | The AOT smoke check reports the kernel; the docs name `IlcInstructionSet` if it is needed; scalar stays correct everywhere |
| A collectible `AssemblyLoadContext` cannot unload until the idle timer empties the pool | A delay of 30 s at most; documented |
| Pointer code | Confined to `Argon2Matrix`; the project opt-in is explicit; covered by the poisoned-matrix and differential tests |

## 8. Out of scope

Everything in the requirements' §6 (lower parameters, a native implementation or a
platform call, caching derivations), and also:

- cancellation;
- a public pool API;
- huge pages, which need a platform call;
- an AVX-512 kernel, unless Phase 7 justifies one;
- scrypt, which would gain from the same techniques but needs its own plan.

## 9. What FallbackPlan gets

The open questions, answered:

- **Threads:** used by default, and bounded by `maxDegreeOfParallelism`.
- **The matrix:** native memory, reused through a bounded pool that frees idle
  buffers.
- **Version:** 1.1.0, with the next lock-step wave.

FallbackPlan's §5 needs no change: take the version, and let the cross-verification
and committed vectors prove the output. The spike measured these on the §2.1 machine
class:

| Measure | 1.0.0 | Spike |
|---|--:|--:|
| Warm 64 MiB derivation | 209–236 ms | 27–37 ms, using 35–50 % of 1.0.0's CPU |
| First derivation in a fresh process | ~350–400 ms | ~80 ms |
| Four at once | 1× | 3.7–5.7× the throughput |

Phase 7 confirms these on the release build, and on the kinds of machines
FallbackPlan's users run. §10 records the implementation's measurements so far.

## 10. Results

The phases are implemented on `claude/argon2-prototype-co27tu`, one commit or more
each: Phase 0 (`1af9d09`, `311aa11`), 1 (`a0eb574`), 2 (`b0e244d`), 3 (`cfef00f`),
4 (`089c541`), 5 (`9b8def8`), 6 (`11373cf`, `2e6a5b3`) and 8 (`7bcb9e4`). Phase 7's
measurements follow, from one machine: the 4-vCPU Xeon (AVX-512) of §2, .NET 10.0.0,
Release, using the harness (`--argon2-harness`, the requirements' appendix method). Each
row is the mean of two rounds, with 1.0.0 measured in the same session. The VM's speed
drifted by 10–15 % between sessions, so compare figures within a table, not with §2.

**Table R1 — one derivation (Argon2id, 64 MiB, t = 3, p = 4, warm).** The kernel is
selected with the runtime's switches: the default takes AVX2, `DOTNET_EnableAVX2=0`
SSSE3, and `DOTNET_EnableHWIntrinsic=0` the scalar kernel.

| Build | Wall | vs 1.0.0 | CPU | vs 1.0.0 | Allocated | Gen2 per call |
|---|--:|--:|--:|--:|--:|--:|
| 1.0.0 | 247.5 ms | 100 % | 246.8 ms | 100 % | 64 MiB | 0.5 |
| AVX2, threads (default) | 30.7 | 12 % | 91.3 | 37 % | 26 KiB | 0 |
| AVX2, one thread | 90.2 | 36 % | 91.5 | 37 % | 0.2 KiB | 0 |
| SSSE3, threads | 41.4 | 17 % | 131.5 | 53 % | 26 KiB | 0 |
| SSSE3, one thread | 133.3 | 54 % | 134.0 | 54 % | 0.2 KiB | 0 |
| Scalar, threads | 62.2 | 25 % | 191.2 | 77 % | 26 KiB | 0 |
| Scalar, one thread | 193.6 | 78 % | 195.9 | 79 % | 0.2 KiB | 0 |

**Table R2 — four at once (four dedicated threads, each deriving four times).**

| Build | All 16, threads | All 16, one thread each | Throughput vs 1.0.0 |
|---|--:|--:|--:|
| 1.0.0 | 1,096 ms | — | 1.0× |
| AVX2 | 462 | 450 | 2.4× |
| SSSE3 | 655 | 619 | 1.7–1.8× |
| Scalar | 929 | 872 | 1.2–1.3× |

**Table R3 — the first derivation in a fresh process (64 MiB, p = 4; three processes
each).** 1.0.0: 363–385 ms. This build: 96–115 ms.

**Table R4 — where threads pay (AVX2, p = 4, t = 3).** Measured on a scratch build with
the threshold lowered to one block, so four threads engage at every size; three runs,
each the median of repeated derivations. The ratios divide four threads by one.

| m | Blocks per segment | Wall ratio | CPU ratio |
|---|--:|--:|--:|
| 1 MiB | 64 | 0.95–1.11 | 1.9–2.5 |
| 2 MiB | 128 | 0.66–1.03 | 1.2–2.2 |
| 4 MiB | 256 | 0.59–0.72 | 0.9–1.7 |
| 8 MiB | 512 | 0.51–0.68 | 1.6–1.7 |
| 16 MiB | 1,024 | 0.41–0.48 | 1.4–1.5 |
| 32 MiB | 2,048 | 0.42–0.49 | 1.3–1.5 |

An earlier single run read 1.01 at 4 MiB; the three repeats did not reproduce it.

**NativeAOT (linux-x64, same host).** A default AOT build compiles against a baseline
instruction set without AVX2, so dispatch selects the SSSE3 kernel: 116 ms for the
64 MiB derivation on one thread, with the RFC 9106 tag reproduced. Built with
`IlcInstructionSet=x86-64-v3`, it selects AVX2: 73 ms.

**ARM64.** Correctness only, so far, under qemu-user with the linux-arm64 runtime.
Dispatch selects AdvSimd; 10,000 shim checks against their scalar definitions, 300
differential blocks against the scalar kernel, RFC 9106 and all 132 recorded rows pass.
qemu occasionally crashes the runtime itself when tiered compilation is on, including in
a control program with no Argon2 code; with tiering off every run passed, and no run ever
produced a wrong tag. The ARM64 CI job (Phase 6) runs both suites on hardware.

**Coverage.** Both cryptography suites, merged, on the host above: the package at 98.3 %
of lines, every kernel file at 100 %, and the AdvSimd shim n/a (hardware-gated). The
collection took about 8 minutes for the main suite and 7 for the SIMD-off assembly,
within coverage.runsettings' 30-minute limit, once the kernel and thread sweeps were
capped at 16 MiB vectors.

### 10.1 Against the requirements

| Requirement | Target | Result on the machine above |
|---|---|---|
| ARG-F-001 | Tags unchanged | RFC 9106, the reference implementation's vectors, and the 132 rows recorded from 1.0.0 reproduce on every kernel and at every thread bound |
| ARG-F-002 | Additive API only | `MaxDegreeOfParallelism`, a bounded constructor per variant, and a bounded `Verify`; nothing changed or removed |
| ARG-F-003 | A bound on threads | Implemented; `1` is 1.0.0's behaviour |
| ARG-N-001 | Wall ≤ 40 % | 12 % (AVX2), 17 % (SSSE3), 25 % (scalar) |
| ARG-N-002 | CPU ≤ 60 % with vector code | 37 % (AVX2), 53 % (SSSE3); on ARM64 (§10.3), with the kernels dispatch now selects, 56 % on a Neoverse N2 and 36 % on an Apple M1 under .NET 8 (AdvSimd), and 46–50 % and 50–59 % under .NET 10 (scalar) |
| ARG-N-003 | Scalar no slower | 77–79 % |
| ARG-N-004 | < 1 MiB allocated, no gen2 | 26 KiB with threads, 0.2–0.3 KiB without; gen2 0 |
| ARG-N-005 | Four at once ≥ 1.0.0 | 2.4× (AVX2), 1.7× (SSSE3), 1.2× (scalar) |
| ARG-N-006 | Clearing | The matrix, H0, per-segment scratch and the first- and last-block buffers are cleared |
| ARG-N-007 | Data independence | Kernels branch-free, shuffles by constant indices; checklist §7 in `SECURITY-CHECKLIST.md` |
| ARG-N-008 | Scalar held to the vectors; ARM64 in CI | The SIMD-off assembly runs the Argon2 vectors; the ARM64 job runs both suites on every pull request |
| ARG-N-009 | Measurement | The harness gained one-thread rows and was run here |
| ARG-N-010 | In-box APIs; AOT | In-box only; AOT selects SSSE3 by default and AVX2 with `IlcInstructionSet` |

### 10.2 Tunables and deferred decisions

- **Thread threshold: 256 blocks per segment, unchanged.** It is the first size in
  Table R4 where four threads reliably cut the wall time, by 28–41 %; at 128 blocks
  the gain is inconsistent and at 64 there is none.
- **Pool: `ProcessorCount` buffers, 256 MiB each, 30 s idle, unchanged.** Four at once
  shows no gen2 collections and no throughput loss, and the residency is documented.
- **No AVX-512 kernel.** None was prototyped. AVX2 already meets ARG-N-002 at 37 %, and
  the plan's 15 % bar needs a prototype to clear.
- **No `AppContext` default for the bound.** Four at once never falls below 1.0.0. When
  the machine is already saturated, threads by default cost 3–7 % of throughput against
  one thread each (AVX2 3 %, SSSE3 6 %, scalar 7 %), which the per-instance bound
  recovers.

### 10.3 Measured since

Both items this section left open were measured by F7 of
[`crypto-performance-followups.md`](crypto-performance-followups.md), whose §9 has the
figures, on GitHub's hosted runners:

- **ARM64 performance.** On a Neoverse N2 the AdvSimd kernel ran at 0.76–0.80 of the
  scalar kernel's speed, on .NET 8 and .NET 10, and took 56–59 % of 1.0.0's CPU. On an
  Apple M1 it ran 1.5–1.6 times as fast as the scalar kernel under .NET 8, taking 36 %
  of 1.0.0's CPU where the scalar kernel took 58–67 %, and about as fast under .NET 10.
  BLAKE2b's and BLAKE2s's AdvSimd kernels lost by more, so the layout they share with
  Argon2's, one state across a vector's lanes, is the cause rather than the shim's
  rotations. Dispatch therefore follows the runtime: under .NET 8 it keeps the AdvSimd
  kernel, which meets ARG-N-002 on both processors, and under .NET 10 it runs the scalar
  kernel (`SimdCapabilities.AdvSimdSingleState`), which takes 46–50 % of 1.0.0's CPU on
  the N2 and 50–59 % on the M1.
- **A small-L3 machine.** An AMD EPYC 7763 whose 32 MiB of L3 holds half of the 64 MiB
  matrix. From the 256-block threshold the threads cut the wall time by 1.6–2.4 times, on
  both runtimes, so the threshold stands; below it both runs take one thread. The AVX2
  kernel takes 21–23 % of 1.0.0's CPU and 7–9 % of its wall time.

Closed since this section was written:

- **The ARM64 CI job** has run on every pull request since #710, and passes.
- **Release.** The work shipped in `Bodu.Security.Cryptography` 1.1.0, released out of
  band (#711) ahead of the next lock-step bump, with notes along these lines:

  > **Argon2 is faster.** Derivations now divide their lanes among threads once each lane
  > is about 1 MiB, compress blocks with AVX2, SSSE3 or AdvSimd, and hold the matrix in
  > native memory: a 64 MiB derivation takes about an eighth of the time and allocates
  > almost nothing on the managed heap. Tags are unchanged. Two behaviours are new, and
  > each can be turned off: **threads by default** — construct with
  > `maxDegreeOfParallelism: 1`, or pass it to `Argon2.Verify`, to keep each derivation on
  > its calling thread; and **retained matrices** — up to one cleared matrix per processor
  > stays reserved for 30 seconds after the last derivation, unless the `AppContext` switch
  > `Bodu.Security.Cryptography.Argon2.DisableMatrixReuse` is set. NativeAOT applications
  > get the SSSE3 kernel on x64 unless they set `IlcInstructionSet` (for example
  > `x86-64-v3`).
