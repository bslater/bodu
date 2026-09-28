---
title: Hardware acceleration and the SIMD opt-out
---

# Hardware acceleration and the SIMD opt-out

Several primitives in `Bodu.Security.Cryptography` ship a vectorised kernel alongside their scalar reference implementation, and dispatch to it automatically when the host CPU supports the required instructions. This page documents exactly *which* algorithms are accelerated, *when* the fast path engages, and *how* to force the scalar path process-wide.

## Which algorithms are accelerated

Every accelerated primitive has a scalar reference implementation and one or more vector kernels in sibling files — `*.Avx512.cs` for the AVX-512 kernels, `Argon2Core.Avx2.cs`, `Argon2Core.Vector128.cs` with its `Ssse3` and `AdvSimd` shims for Argon2, `ScryptCore.Vector128.cs` with its `Sse2` and `AdvSimd` shims for scrypt, and `Ghash.Clmul.cs` with its `PclmulqdqIsa` and `PmullIsa` shims for GHASH and POLYVAL. The dispatch is per-operation and transparent — the public API and output are identical either way.

| Primitive | Accelerated operation | Instruction set gate |
|---|---|---|
| `Blake2b` | Block compression | AVX-512F + VL |
| `Blake2s` | Block compression | AVX-512F + VL |
| `Blake3` | Compression function | AVX-512F + VL |
| `Threefish256` | Encrypt / decrypt block | AVX-512F + VL |
| `Threefish512` | Encrypt / decrypt block | AVX-512F |
| `Threefish1024` | Encrypt / decrypt block | AVX-512F |
| `CubeHash` | Round permutation | AVX-512F |
| `Argon2d` / `Argon2i` / `Argon2id` | Compression function `G` | AVX2, else SSSE3 (x64); AdvSimd (ARM64) |
| `Scrypt` | `scryptBlockMix` (Salsa20/8) | SSE2 (x64); AdvSimd (ARM64) |
| `GcmModeTransform` / `GcmSivModeTransform` | GHASH / POLYVAL | PCLMULQDQ + SSSE3 (x64); PMULL (ARM64) |

There are two gate forms. The 128- and 256-bit-lane kernels (BLAKE2b, BLAKE2s, BLAKE3, Threefish-256) require the AVX-512 **Vector Length** extension (`Avx512F.VL.IsSupported`); the 512-bit-lane kernels (Threefish-512/1024, CubeHash) require only AVX-512 **Foundation** (`Avx512F.IsSupported`). GHASH, which `GcmModeTransform` authenticates with, and POLYVAL, which `GcmSivModeTransform` does, share one carry-less kernel that folds four blocks into each reduction. It runs on **PCLMULQDQ** with SSSE3 for the byte shuffles on x64 (`Pclmulqdq.IsSupported && Ssse3.IsSupported`) and on the cryptography extension's **PMULL** on ARM64 (`Aes.IsSupported`, which .NET uses to expose it); like Argon2's kernel it is written once and differs between the two architectures only in a small shim. Elsewhere, including ARM64 processors without the extension, a constant-time scalar multiply built from integer multiplications runs instead. Argon2 takes the widest of three kernels the processor offers: a 256-bit **AVX2** kernel, else a 128-bit kernel over **SSSE3** on x64 or **AdvSimd** on ARM64 (`AdvSimd.Arm64.IsSupported`); the 128-bit kernel is written once over portable `Vector128` arithmetic and differs between the two architectures only in five shim operations. Argon2 uses no AVX-512 kernel: its cost is dominated by memory, which wider registers do not help. scrypt's `scryptBlockMix` runs the Salsa20/8 core on four 128-bit vectors, one per diagonal of its state, over **SSE2** on x64 — part of the x64 baseline, so every x64 processor takes it — or **AdvSimd** on ARM64; the two differ only in the three lane rotations the shim supplies. A BlockMix chain is sequential, so the kernel stays at 128 bits: one Salsa20/8 state fills a vector exactly, and nothing wider would have independent work to fill it.

The gates are evaluated where the code is compiled. Under the JIT that is the running processor; a NativeAOT application is compiled ahead of time for a baseline instruction set, which on x64 excludes AVX2 and AVX-512, so it takes the narrower kernels unless the project raises the target with `<IlcInstructionSet>` (for example `x86-64-v3` for AVX2) — only where every machine it will run on supports it.

## When the fast path engages

By default the vectorised path is taken whenever the CPU reports the required instruction set. The check is a JIT intrinsic: on a host without AVX-512 it folds to a compile-time constant and the vectorised branch is eliminated entirely, so there is no runtime probing cost.

## Forcing the scalar path

For scenarios that need a single, deterministic code path — reproducing a result bit-for-bit across heterogeneous hardware, differential testing against the scalar reference, or an audit that wants one implementation to reason about — the vectorised paths can be disabled process-wide with the feature switch:

```
Bodu.Security.Cryptography.DisableSimd = true
```

When set, every accelerated primitive runs its scalar reference implementation regardless of the host CPU. The switch is read **once**, the first time any accelerated primitive is used, so it must be set before that point. Any of the standard mechanisms works:

- **`runtimeconfig.json` / project file** (recommended — applied before any managed code runs):

  ```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="Bodu.Security.Cryptography.DisableSimd" Value="true" Trim="false" />
  </ItemGroup>
  ```

- **In code, at startup**, before touching any hashing or cipher type:

<!-- compile -->
  ```csharp
  AppContext.SetSwitch("Bodu.Security.Cryptography.DisableSimd", true);
  ```

As a coarser alternative, the .NET runtime's own knobs make the intrinsics report `false`, which also forces the scalar paths — `DOTNET_EnableAVX512F=0` on .NET 8 or `DOTNET_EnableAVX512=0` on .NET 10 for the AVX-512 kernels (each runtime ignores the other's name, so set both where either may run), `DOTNET_EnableAVX2=0` to move Argon2 from AVX2 to SSSE3, or `DOTNET_EnableHWIntrinsic=0` for everything — but they affect the whole process including the BCL, not just this library. Prefer the library switch when you only want to pin *these* primitives.

> [!NOTE]
> The opt-out exists for **determinism, reproducibility, and audit**, not because the vectorised paths are unsafe. BLAKE2, BLAKE3, and Threefish are ARX constructions — addition, rotation, and XOR only, with no secret-dependent branches or table lookups — so the scalar and AVX-512 paths are both inherently constant-time and produce bit-identical output. Argon2's kernels add a 32-bit multiply, which has a fixed latency on every targeted processor, and byte shuffles whose indices are constants, not data; which blocks a derivation reads is decided outside the kernels, from public inputs in Argon2i and the first half of Argon2id's first pass. scrypt's kernels are Salsa20/8, again addition, rotation, and XOR, with lane rotations by constants; scrypt's own data-dependent read of `V[j]` is part of its design, made outside the kernels, and identical on both paths. The GHASH and POLYVAL kernels are carry-less or integer multiplications, shifts, and XORs, which likewise run in fixed time, with no branch on or index by the key or the data. The switch only selects which of two equivalent implementations runs; it makes no additional constant-time *guarantee* beyond what the algorithms already provide, and this library is not independently audited.

## Where to go next

- [Using BLAKE2 and BLAKE3](blake.md)
- [Using Argon2](argon2.md)
- [Using scrypt](scrypt.md)
- [Using Threefish-256](threefish-256.md) · [Threefish-512](threefish-512.md) · [Threefish-1024](threefish-1024.md)
- [Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography)
