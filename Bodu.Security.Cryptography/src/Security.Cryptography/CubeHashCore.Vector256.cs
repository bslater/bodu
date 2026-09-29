// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCore.Vector256.cs" company="Bodu Pty. Ltd.">
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
    /// Applies rounds of the CubeHash permutation with the state in four 256-bit registers, eight words each, over
    /// AVX2.
    /// </summary>
    /// <param name="state">The 32-word state.</param>
    /// <param name="roundCount">The number of rounds.</param>
    /// <remarks>
    /// Exchanging the lower words 8 apart pairs the two lower registers, so it is written into which register each
    /// result lands in; exchanging them 4 apart swaps the 128-bit halves of each register. Exchanging the upper words 2
    /// or 1 apart stays within each 128-bit lane: one shuffle.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void Vector256Rounds(Span<uint> state, int roundCount)
    {
        const byte SwapHalvesControl = 0b01_00_11_10;
        const byte SwapPairsControl = 0b10_11_00_01;

        ref uint words = ref MemoryMarshal.GetReference(state);
        var lower0 = Vector256.LoadUnsafe(ref words);
        var lower1 = Vector256.LoadUnsafe(ref words, 8);
        var upper0 = Vector256.LoadUnsafe(ref words, 16);
        var upper1 = Vector256.LoadUnsafe(ref words, 24);

        for (int r = 0; r < roundCount; r++)
        {
            upper0 += lower0;
            upper1 += lower1;

            // Rotate the lower words by 7 and exchange those 8 apart, the two registers; XOR in the upper words.
            Vector256<uint> next0 = lower1.RotateBitsLeftUnchecked<VectorRotation.Avx2>(7) ^ upper0;
            Vector256<uint> next1 = lower0.RotateBitsLeftUnchecked<VectorRotation.Avx2>(7) ^ upper1;

            // Exchange the upper words 2 apart, within each 128-bit lane, and add the lower words in.
            upper0 = Avx2.Shuffle(upper0, SwapHalvesControl) + next0;
            upper1 = Avx2.Shuffle(upper1, SwapHalvesControl) + next1;

            // Rotate by 11 and exchange the lower words 4 apart, the halves of each register; XOR in the upper words.
            lower0 = Avx2.Permute2x128(next0, next0, 0x01).RotateBitsLeftUnchecked<VectorRotation.Avx2>(11) ^ upper0;
            lower1 = Avx2.Permute2x128(next1, next1, 0x01).RotateBitsLeftUnchecked<VectorRotation.Avx2>(11) ^ upper1;

            // Exchange the upper words 1 apart, adjacent within each register.
            upper0 = Avx2.Shuffle(upper0, SwapPairsControl);
            upper1 = Avx2.Shuffle(upper1, SwapPairsControl);
        }

        lower0.StoreUnsafe(ref words);
        lower1.StoreUnsafe(ref words, 8);
        upper0.StoreUnsafe(ref words, 16);
        upper1.StoreUnsafe(ref words, 24);
    }
}
