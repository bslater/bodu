// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the CubeHash round function behind <see cref="CubeHash" />: a portable scalar kernel, and vector kernels
/// that hold the 32-word state in 128-, 256- or 512-bit registers for the whole run of rounds.
/// </summary>
/// <remarks>
/// <para>
/// A round adds the state's lower sixteen words into its upper sixteen, rotates the lower words left by 7, exchanges
/// the lower words 8 apart, XORs the upper words into the lower, and exchanges the upper words 2 apart; then it does
/// the same with a rotation by 11, the lower words 4 apart and the upper words 1 apart. Every kernel performs exactly
/// these operations, so every kernel produces the same state.
/// </para>
/// <para>
/// In registers of four or eight words, the lower words' exchanges move whole registers or register halves: the 128-bit
/// kernel renames registers instead, and the 256-bit kernel swaps halves. The upper words' exchanges stay within
/// 128-bit lanes and take one shuffle per register. The rotations go through Bodu.Core's
/// <see cref="VectorExtensions" />, as ChaCha20's do.
/// </para>
/// </remarks>
internal static partial class CubeHashCore
{
    /// <summary>The number of 32-bit words in the state.</summary>
    internal const int StateWords = 32;

    /// <summary>
    /// Applies rounds of the CubeHash permutation to a state in place, with the kernel dispatch selects.
    /// </summary>
    /// <param name="state">The 32-word state.</param>
    /// <param name="roundCount">The number of rounds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="state" /> holds fewer than 32 words.</exception>
    internal static void PerformRounds(Span<uint> state, int roundCount) =>
        PerformRounds(KernelKind.Auto, state, roundCount);

    /// <summary>
    /// Applies rounds of the CubeHash permutation to a state in place, with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel, which the processor must support, or <see cref="KernelKind.Auto" /> for the one dispatch selects.
    /// </param>
    /// <param name="state">The 32-word state.</param>
    /// <param name="roundCount">The number of rounds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="state" /> holds fewer than 32 words.</exception>
    internal static void PerformRounds(KernelKind kernel, Span<uint> state, int roundCount)
    {
        ThrowHelper.ThrowIfLessThan(state.Length, StateWords, nameof(state));

        if (kernel == KernelKind.Auto)
            kernel = SelectKernel();

        switch (kernel)
        {
            case KernelKind.Avx512:
                Vector512Rounds(state, roundCount);
                break;

            case KernelKind.Avx2:
                Vector256Rounds(state, roundCount);
                break;

            case KernelKind.AdvSimd:
                Vector128Rounds<VectorRotation.AdvSimd>(state, roundCount);
                break;

            case KernelKind.Ssse3:
                Vector128Rounds<VectorRotation.Ssse3>(state, roundCount);
                break;

            default:
                ScalarRounds(state, roundCount);
                break;
        }
    }

    /// <summary>
    /// Selects the widest kernel the processor supports and the process allows.
    /// </summary>
    /// <returns>The kernel dispatch uses.</returns>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.Avx512F)
            return KernelKind.Avx512;

        if (SimdCapabilities.Avx2)
            return KernelKind.Avx2;

        if (SimdCapabilities.AdvSimd)
            return KernelKind.AdvSimd;

        return SimdCapabilities.Ssse3 ? KernelKind.Ssse3 : KernelKind.Scalar;
    }

    /// <summary>
    /// Determines whether the processor can run the specified kernel, whether or not the process allows vector code.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns>
    /// <see langword="true" /> if the processor supports every instruction the kernel uses; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Ssse3 => System.Runtime.Intrinsics.X86.Ssse3.IsSupported,
        KernelKind.AdvSimd => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported,
        KernelKind.Avx2 => System.Runtime.Intrinsics.X86.Avx2.IsSupported,
        KernelKind.Avx512 => System.Runtime.Intrinsics.X86.Avx512F.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Applies rounds of the CubeHash permutation one word at a time: the portable kernel, which runs everywhere.
    /// </summary>
    /// <param name="state">The 32-word state.</param>
    /// <param name="roundCount">The number of rounds.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ScalarRounds(Span<uint> state, int roundCount)
    {
        // temp is used as a scratch permutation buffer; allocated once on the stack for the full call
        Span<uint> temp = stackalloc uint[16];

        // Pre-offset refs for lower and upper halves give the JIT a constant base per half rather than recomputing
        // (16 + i) on every upper access. tempRef bypasses the per-element bounds checks for the non-monotonic XOR
        // scatter indices (i^8, i^2, i^4, i^1) that the JIT cannot prove stay in-range from a loop-bound alone.
        ref uint lowerRef = ref MemoryMarshal.GetReference(state);
        ref uint upperRef = ref Unsafe.Add(ref lowerRef, 16);
        ref uint tempRef = ref MemoryMarshal.GetReference(temp);
        Span<uint> upper = state.Slice(16, 16);

        for (int r = 0; r < roundCount; r++)
        {
            // Steps 1+2: add lower into upper; scatter lower into temp via XOR-8 permutation
            for (int i = 0; i < 16; i++)
            {
                Unsafe.Add(ref upperRef, i) += Unsafe.Add(ref lowerRef, i);
                Unsafe.Add(ref tempRef, i ^ 8) = Unsafe.Add(ref lowerRef, i);
            }

            // Steps 3+4: rotate temp left by 7 into lower; XOR lower with upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref lowerRef, i) = Unsafe.Add(ref tempRef, i).RotateBitsLeftUnchecked(7) ^ Unsafe.Add(ref upperRef, i);

            // Step 5: scatter upper into temp via XOR-2 permutation; copy back to upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref tempRef, i ^ 2) = Unsafe.Add(ref upperRef, i);
            temp.CopyTo(upper);

            // Steps 6+7: add lower into upper; scatter lower into temp via XOR-4 permutation
            for (int i = 0; i < 16; i++)
            {
                Unsafe.Add(ref upperRef, i) += Unsafe.Add(ref lowerRef, i);
                Unsafe.Add(ref tempRef, i ^ 4) = Unsafe.Add(ref lowerRef, i);
            }

            // Steps 8+9: rotate temp left by 11 into lower; XOR lower with upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref lowerRef, i) = Unsafe.Add(ref tempRef, i).RotateBitsLeftUnchecked(11) ^ Unsafe.Add(ref upperRef, i);

            // Step 10: scatter upper into temp via XOR-1 permutation; copy back to upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref tempRef, i ^ 1) = Unsafe.Add(ref upperRef, i);
            temp.CopyTo(upper);
        }
    }
}
