// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilities.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Gates the library's vectorized code paths, combining the hardware capability check with a process-wide opt-out
/// switch.
/// </summary>
/// <remarks>
/// <para>
/// Several primitives dispatch to a vector kernel when the host supports it and fall back to a scalar reference
/// implementation otherwise: among them BLAKE2b, BLAKE2s, BLAKE3, Threefish-256/512/1024, and CubeHash to AVX-512;
/// GHASH and POLYVAL to the carry-less multiply on x64 or the polynomial multiply on ARM64; Argon2 to AVX2, or to SSSE3
/// on other x64 hosts, and on ARM64 to a kernel that pairs AdvSimd with the general registers; and scrypt's Salsa20/8
/// to SSE2 on x64. The properties here replace a bare <c>IsSupported</c> check at each dispatch site so a caller can
/// force the scalar paths for the whole process by enabling the <see cref="DisableSimdSwitchName" /> feature switch.
/// </para>
/// <para>
/// On ARM64, <see cref="AdvSimdSingleState" /> holds back the AdvSimd kernels of BLAKE2b, BLAKE2s, and scrypt, and
/// BLAKE3's for a single block, which ran slower than the scalar kernels on the processors measured.
/// </para>
/// <para>
/// The switch exists for <strong>determinism, reproducibility, and audit</strong> - pinning execution to the single
/// scalar reference implementation - not because the vectorized paths are unsafe. Every gated kernel uses only
/// arithmetic, rotations, XORs, and shuffles by constant indices, with no secret-dependent branches or table lookups,
/// so the scalar and vector paths produce bit-identical output. This type therefore makes no constant-time <em>guarantee</em>;
/// it only lets a caller select which of two equivalent implementations runs.
/// </para>
/// <para>
/// The switch is read once at type initialization via <see cref="AppContext.TryGetSwitch(string, out bool)" />, so it
/// must be set before the first use of any accelerated primitive (for example through <c>runtimeconfig.json</c>, an
/// <c>&lt;RuntimeHostConfigurationOption&gt;</c> MSBuild item, or an early
/// <see cref="AppContext.SetSwitch(string, bool)" /> call). When the switch is off - the default - each property
/// reduces to the underlying hardware intrinsic, which the JIT folds to a compile-time constant on hosts without the
/// instruction set, eliminating the vectorized branch entirely; when the instruction set is present it reduces to a
/// single cached-boolean load.
/// </para>
/// </remarks>
internal static class SimdCapabilities
{
    /// <summary>The name of the <see cref="AppContext" /> feature switch that, when enabled, forces the scalar code paths.</summary>
    internal const string DisableSimdSwitchName = "Bodu.Security.Cryptography.DisableSimd";

    /// <summary>Whether SIMD dispatch has been disabled process-wide via the <see cref="DisableSimdSwitchName" /> switch. Read once at type initialization.</summary>
    private static readonly bool s_disabled = AppContext.TryGetSwitch(DisableSimdSwitchName, out bool disabled) && disabled;

    /// <summary>
    /// Gets a value indicating whether the AVX-512 Foundation (512-bit lane) code paths should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="Avx512F.IsSupported" /> and the disable switch is off; otherwise,
    /// <see langword="false" />.
    /// </value>
    internal static bool Avx512F
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.X86.Avx512F.IsSupported && !s_disabled;
    }

    /// <summary>
    /// Gets a value indicating whether the AVX-512 Vector Length (128/256-bit lane) code paths should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="Avx512F.VL.IsSupported" /> and the disable switch is off; otherwise,
    /// <see langword="false" />.
    /// </value>
    internal static bool Avx512FVL
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported && !s_disabled;
    }

    /// <summary>
    /// Gets a value indicating whether the carry-less-multiply <c>GF(2¹²⁸)</c> code paths (GHASH / POLYVAL) should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported" /> and
    /// <see cref="System.Runtime.Intrinsics.X86.Ssse3.IsSupported" /> and the disable switch is off; otherwise,
    /// <see langword="false" />.
    /// </value>
    /// <remarks>
    /// The carry-less multiply requires <see cref="System.Runtime.Intrinsics.X86.Pclmulqdq" /> for the field
    /// multiplication and <see cref="System.Runtime.Intrinsics.X86.Ssse3" /> for the byte-reversal shuffle that maps
    /// the GHASH block layout to the reflected polynomial order. Both produce output bit-identical to the scalar
    /// reference, so this gate only selects which of two equivalent implementations runs.
    /// </remarks>
    internal static bool Pclmulqdq
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported && System.Runtime.Intrinsics.X86.Ssse3.IsSupported && !s_disabled;
    }

    /// <summary>
    /// Gets a value indicating whether the AVX2 (256-bit integer) code paths should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="System.Runtime.Intrinsics.X86.Avx2.IsSupported" /> and the disable switch
    /// is off; otherwise, <see langword="false" />.
    /// </value>
    internal static bool Avx2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.X86.Avx2.IsSupported && !s_disabled;
    }

    /// <summary>
    /// Gets a value indicating whether the SSSE3 (128-bit integer, x64) code paths should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="System.Runtime.Intrinsics.X86.Ssse3.IsSupported" /> and the disable switch
    /// is off; otherwise, <see langword="false" />.
    /// </value>
    internal static bool Ssse3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.X86.Ssse3.IsSupported && !s_disabled;
    }

    /// <summary>
    /// Gets a value indicating whether the SSE2 (128-bit integer, x64) code paths should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="System.Runtime.Intrinsics.X86.Sse2.IsSupported" /> and the disable switch
    /// is off; otherwise, <see langword="false" />.
    /// </value>
    /// <remarks>
    /// SSE2 belongs to the x64 baseline, so on x64 only the disable switch closes this gate.
    /// </remarks>
    internal static bool Sse2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.X86.Sse2.IsSupported && !s_disabled;
    }

    /// <summary>
    /// Gets a value indicating whether the AdvSimd (128-bit, ARM64) code paths should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported" /> and the
    /// disable switch is off; otherwise, <see langword="false" />.
    /// </value>
    /// <remarks>
    /// The gate requires the 64-bit instruction set, not just AdvSimd, because the kernels it guards use the ARM64-only
    /// full-width table lookup.
    /// </remarks>
    internal static bool AdvSimd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported && !s_disabled;
    }

    /// <summary>
    /// Gets a value indicating whether dispatch should select the AdvSimd kernels that spread a single state across
    /// their vectors' lanes: those of BLAKE2b, BLAKE2s, and scrypt, and BLAKE3's for a single block.
    /// </summary>
    /// <value>
    /// <see langword="false" />: on a Neoverse N2, the scalar kernels ran every one of these primitives faster.
    /// </value>
    /// <remarks>
    /// <para>
    /// Each of these kernels holds one state in its vectors and rearranges the lanes between the column and diagonal
    /// steps of every round. Built the same way over SSE2, SSSE3, or AVX2, they run faster than the scalar kernels on
    /// x64, whose sixteen general registers cannot hold the state. ARM64 has thirty-one and rotates a register in one
    /// instruction; on a Neoverse N2 the scalar kernels ran every one of these primitives faster, on .NET 8 and .NET 10
    /// alike, and an Apple M1 agreed but for scrypt under .NET 8. A count of each round's instructions in the JIT's
    /// output found that no layout of one state can beat the scalar code on the N2's two vector pipes, so dispatch runs
    /// the scalar kernels in their place on ARM64. The kernels run only where a caller names one, as the tests do to
    /// hold each to the scalar kernel.
    /// </para>
    /// <para>
    /// Argon2's AdvSimd kernel, which holds one row in eight vector registers, lost to the scalar kernel on the N2 as
    /// these do. Argon2 compresses eight independent rows, then eight independent columns, so on ARM64 it runs a kernel
    /// that pairs one row or column in vector registers with another in general registers, which ran faster than both
    /// on both processors; this gate does not govern it.
    /// </para>
    /// <para>
    /// The AdvSimd kernels that give each lane a state of its own (ChaCha20 and Salsa20 over several blocks, BLAKE3
    /// over several chunks or parents, Serpent-128 over several blocks) and CubeHash's ran faster than the scalar
    /// kernels on both processors, and <see cref="AdvSimd" /> alone gates them.
    /// </para>
    /// </remarks>
    internal static bool AdvSimdSingleState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => false;
    }

    /// <summary>
    /// Gets a value indicating whether the process runs on ARM64 under one of Apple's operating systems, and so on
    /// Apple's own cores.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported" /> and the
    /// process runs on macOS, iOS, tvOS, or Mac Catalyst; otherwise, <see langword="false" />.
    /// </value>
    /// <remarks>
    /// <para>
    /// Where an Apple M1 and a Neoverse N2 disagreed on a kernel, dispatch passes this value to choose between them:
    /// Poly1305's AdvSimd kernel takes runs from 128 bytes on Apple's cores and from 256 elsewhere. Apple's cores have
    /// four 128-bit vector pipes, where the N2 has two.
    /// </para>
    /// <para>
    /// The value describes the platform rather than gating a code path, so the disable switch does not close it; the
    /// gates it is combined with do that. Linux on Apple's cores is reported as any other ARM64 processor. Each
    /// operating-system check is a constant in its platform's runtime, so the JIT folds the whole property.
    /// </para>
    /// </remarks>
    internal static bool AppleSilicon
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported
            && (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst());
    }

    /// <summary>
    /// Gets a value indicating whether the ARM64 polynomial-multiply <c>GF(2¹²⁸)</c> code paths (GHASH / POLYVAL)
    /// should run.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if <see cref="System.Runtime.Intrinsics.Arm.Aes.IsSupported" /> and
    /// <see cref="System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported" /> and the disable switch is off; otherwise,
    /// <see langword="false" />.
    /// </value>
    /// <remarks>
    /// The 64 × 64-bit polynomial multiply (<c>PMULL</c> / <c>PMULL2</c>) belongs to the ARMv8 cryptography extension,
    /// which .NET exposes on <see cref="System.Runtime.Intrinsics.Arm.Aes" />. Most ARM64 processors have it; some that
    /// do not, such as the Cortex-A72 in the Raspberry Pi 4, run the scalar kernel.
    /// </remarks>
    internal static bool Pmull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Runtime.Intrinsics.Arm.Aes.IsSupported && System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported && !s_disabled;
    }
}
