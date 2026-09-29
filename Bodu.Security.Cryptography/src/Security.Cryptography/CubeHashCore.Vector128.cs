// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCore.Vector128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class CubeHashCore
{
    /// <summary>
    /// Applies rounds of the CubeHash permutation with the state in eight 128-bit registers, four words each.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set that performs the rotations: <see cref="VectorRotation.Ssse3" /> or
    /// <see cref="VectorRotation.AdvSimd" />.
    /// </typeparam>
    /// <param name="state">The 32-word state.</param>
    /// <param name="roundCount">The number of rounds.</param>
    /// <remarks>
    /// Register j of each half holds words 4j to 4j + 3. Exchanging the lower words 8 apart pairs register j with
    /// register j XOR 2, and 4 apart with register j XOR 1, so those exchanges are written into which register each
    /// result lands in. Exchanging the upper words 2 or 1 apart stays within each register: one shuffle.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void Vector128Rounds<TIsa>(Span<uint> state, int roundCount)
        where TIsa : struct, IVector128Rotation
    {
        ref uint words = ref MemoryMarshal.GetReference(state);
        var lower0 = Vector128.LoadUnsafe(ref words);
        var lower1 = Vector128.LoadUnsafe(ref words, 4);
        var lower2 = Vector128.LoadUnsafe(ref words, 8);
        var lower3 = Vector128.LoadUnsafe(ref words, 12);
        var upper0 = Vector128.LoadUnsafe(ref words, 16);
        var upper1 = Vector128.LoadUnsafe(ref words, 20);
        var upper2 = Vector128.LoadUnsafe(ref words, 24);
        var upper3 = Vector128.LoadUnsafe(ref words, 28);

        for (int r = 0; r < roundCount; r++)
        {
            upper0 += lower0;
            upper1 += lower1;
            upper2 += lower2;
            upper3 += lower3;

            // Rotate the lower words by 7 and exchange those 8 apart, registers 0 and 2, 1 and 3; XOR in the upper words.
            Vector128<uint> next0 = lower2.RotateBitsLeftUnchecked<TIsa>(7) ^ upper0;
            Vector128<uint> next1 = lower3.RotateBitsLeftUnchecked<TIsa>(7) ^ upper1;
            Vector128<uint> next2 = lower0.RotateBitsLeftUnchecked<TIsa>(7) ^ upper2;
            Vector128<uint> next3 = lower1.RotateBitsLeftUnchecked<TIsa>(7) ^ upper3;

            // Exchange the upper words 2 apart, the halves of each register, and add the lower words in.
            upper0 = SwapHalves(upper0) + next0;
            upper1 = SwapHalves(upper1) + next1;
            upper2 = SwapHalves(upper2) + next2;
            upper3 = SwapHalves(upper3) + next3;

            // Rotate by 11 and exchange the lower words 4 apart, registers 0 and 1, 2 and 3; XOR in the upper words.
            lower0 = next1.RotateBitsLeftUnchecked<TIsa>(11) ^ upper0;
            lower1 = next0.RotateBitsLeftUnchecked<TIsa>(11) ^ upper1;
            lower2 = next3.RotateBitsLeftUnchecked<TIsa>(11) ^ upper2;
            lower3 = next2.RotateBitsLeftUnchecked<TIsa>(11) ^ upper3;

            // Exchange the upper words 1 apart, adjacent within each register.
            upper0 = SwapPairs(upper0);
            upper1 = SwapPairs(upper1);
            upper2 = SwapPairs(upper2);
            upper3 = SwapPairs(upper3);
        }

        lower0.StoreUnsafe(ref words);
        lower1.StoreUnsafe(ref words, 4);
        lower2.StoreUnsafe(ref words, 8);
        lower3.StoreUnsafe(ref words, 12);
        upper0.StoreUnsafe(ref words, 16);
        upper1.StoreUnsafe(ref words, 20);
        upper2.StoreUnsafe(ref words, 24);
        upper3.StoreUnsafe(ref words, 28);
    }

    /// <summary>
    /// Exchanges the two 64-bit halves of a register: words (0, 1, 2, 3) become (2, 3, 0, 1).
    /// </summary>
    /// <param name="value">The four words.</param>
    /// <returns>The words with each exchanged for the one 2 apart.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> SwapHalves(Vector128<uint> value) =>
        Vector128.Shuffle(value, Vector128.Create(2u, 3u, 0u, 1u));

    /// <summary>
    /// Exchanges adjacent words of a register: words (0, 1, 2, 3) become (1, 0, 3, 2).
    /// </summary>
    /// <param name="value">The four words.</param>
    /// <returns>The words with each exchanged for the one 1 apart.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> SwapPairs(Vector128<uint> value) =>
        Vector128.Shuffle(value, Vector128.Create(1u, 0u, 3u, 2u));
}
