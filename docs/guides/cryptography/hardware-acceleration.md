---
title: Hardware acceleration and the SIMD opt-out
---

# Hardware acceleration and the SIMD opt-out

Several primitives in `Bodu.Security.Cryptography` ship an AVX-512 vectorised kernel alongside their scalar reference implementation, and dispatch to it automatically when the host CPU supports the required instructions. This page documents exactly *which* algorithms are accelerated, *when* the fast path engages, and *how* to force the scalar path process-wide.

## Which algorithms are accelerated

Every accelerated primitive has a scalar reference implementation and an AVX-512 kernel in a sibling `*.Avx512.cs` file. The dispatch is per-operation and transparent — the public API and output are identical either way.

| Primitive | Accelerated operation | Instruction set gate |
|---|---|---|
| `Blake2b` | Block compression | AVX-512F + VL |
| `Blake2s` | Block compression | AVX-512F + VL |
| `Blake3` | Compression function | AVX-512F + VL |
| `Threefish256` | Encrypt / decrypt block | AVX-512F + VL |
| `Threefish512` | Encrypt / decrypt block | AVX-512F |
| `Threefish1024` | Encrypt / decrypt block | AVX-512F |
| `CubeHash` | Round permutation | AVX-512F |

There are two gate forms. The 128- and 256-bit-lane kernels (BLAKE2b, BLAKE2s, BLAKE3, Threefish-256) require the AVX-512 **Vector Length** extension (`Avx512F.VL.IsSupported`); the 512-bit-lane kernels (Threefish-512/1024, CubeHash) require only AVX-512 **Foundation** (`Avx512F.IsSupported`). No other SIMD instruction set (AVX2, SSE, ARM AdvSimd) is used as a standalone fast path.

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

  ```csharp
  AppContext.SetSwitch("Bodu.Security.Cryptography.DisableSimd", true);
  ```

As a coarser alternative, the .NET runtime's own knob `DOTNET_EnableAVX512F=0` (or `DOTNET_EnableHWIntrinsic=0`) makes `Avx512F.IsSupported` report `false`, which also forces the scalar path — but it affects the whole process including the BCL, not just this library. Prefer the library switch when you only want to pin *these* primitives.

> [!NOTE]
> The opt-out exists for **determinism, reproducibility, and audit**, not because the vectorised paths are unsafe. BLAKE2, BLAKE3, and Threefish are ARX constructions — addition, rotation, and XOR only, with no secret-dependent branches or table lookups — so the scalar and AVX-512 paths are both inherently constant-time and produce bit-identical output. The switch only selects which of two equivalent implementations runs; it makes no additional constant-time *guarantee* beyond what the algorithms already provide, and this library is not independently audited.

## Where to go next

- [Using BLAKE2 and BLAKE3](blake.md)
- [Using Threefish-256](threefish-256.md) · [Threefish-512](threefish-512.md) · [Threefish-1024](threefish-1024.md)
- [Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography)
