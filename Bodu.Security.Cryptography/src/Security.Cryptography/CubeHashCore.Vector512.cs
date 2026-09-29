// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCore.Vector512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class CubeHashCore
{
    /// <summary>
    /// Applies rounds of the CubeHash permutation with the state in two 512-bit registers, over AVX-512F.
    /// </summary>
    /// <param name="state">The 32-word state.</param>
    /// <param name="roundCount">The number of rounds.</param>
    /// <remarks>
    /// Each exchange becomes a single <c>VPERMD</c> and each rotation a single <c>VPROLD</c>. The permutation indices
    /// are constants of the method, so the JIT loads them once, ahead of the loop.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void Vector512Rounds(Span<uint> state, int roundCount)
    {
        Vector512<uint> exchange8 = Vector512.Create(8u, 9u, 10u, 11u, 12u, 13u, 14u, 15u, 0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u);
        Vector512<uint> exchange4 = Vector512.Create(4u, 5u, 6u, 7u, 0u, 1u, 2u, 3u, 12u, 13u, 14u, 15u, 8u, 9u, 10u, 11u);
        Vector512<uint> exchange2 = Vector512.Create(2u, 3u, 0u, 1u, 6u, 7u, 4u, 5u, 10u, 11u, 8u, 9u, 14u, 15u, 12u, 13u);
        Vector512<uint> exchange1 = Vector512.Create(1u, 0u, 3u, 2u, 5u, 4u, 7u, 6u, 9u, 8u, 11u, 10u, 13u, 12u, 15u, 14u);

        ref uint words = ref MemoryMarshal.GetReference(state);
        var lower = Vector512.LoadUnsafe(ref words);
        var upper = Vector512.LoadUnsafe(ref words, 16);

        for (int r = 0; r < roundCount; r++)
        {
            upper += lower;
            lower = Avx512F.PermuteVar16x32(lower, exchange8).RotateBitsLeftUnchecked<VectorRotation.Avx512>(7) ^ upper;
            upper = Avx512F.PermuteVar16x32(upper, exchange2) + lower;
            lower = Avx512F.PermuteVar16x32(lower, exchange4).RotateBitsLeftUnchecked<VectorRotation.Avx512>(11) ^ upper;
            upper = Avx512F.PermuteVar16x32(upper, exchange1);
        }

        lower.StoreUnsafe(ref words);
        upper.StoreUnsafe(ref words, 16);
    }
}
