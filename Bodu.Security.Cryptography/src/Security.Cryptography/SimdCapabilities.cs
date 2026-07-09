// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilities.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Gates the library's AVX-512 vectorised code paths, combining the hardware capability check with a process-wide
/// opt-out switch.
/// </summary>
/// <remarks>
/// <para>
/// Several primitives (BLAKE2b, BLAKE2s, BLAKE3, Threefish-256/512/1024, CubeHash) dispatch to an AVX-512 kernel when
/// the host supports it and fall back to a scalar reference implementation otherwise. The properties here replace a
/// bare <see cref="Avx512F.IsSupported" /> / <see cref="Avx512F.VL.IsSupported" /> check at each dispatch site so a
/// caller can force the scalar path for the whole process by enabling the <see cref="DisableSimdSwitchName" /> feature
/// switch.
/// </para>
/// <para>
/// The switch exists for <strong>determinism, reproducibility, and audit</strong> — pinning execution to the single
/// scalar reference implementation — not because the vectorised paths are unsafe. BLAKE2/BLAKE3 and Threefish are ARX
/// constructions (addition, rotation, and XOR only, with no secret-dependent branches or table lookups), so the scalar
/// and AVX-512 paths are inherently constant-time and produce bit-identical output. This type therefore makes no
/// constant-time <em>guarantee</em>; it only lets a caller select which of two equivalent implementations runs.
/// </para>
/// <para>
/// The switch is read once at type initialization via <see cref="AppContext.TryGetSwitch(string, out bool)" />, so it
/// must be set before the first use of any accelerated primitive (for example through <c>runtimeconfig.json</c>, an
/// <c>&lt;RuntimeHostConfigurationOption&gt;</c> MSBuild item, or an early
/// <see cref="AppContext.SetSwitch(string, bool)" /> call). When the switch is off — the default — each property
/// reduces to the underlying hardware intrinsic, which the JIT folds to a compile-time constant on hosts without
/// AVX-512, eliminating the vectorised branch entirely; when AVX-512 is present it reduces to a single cached-boolean
/// load.
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
}
