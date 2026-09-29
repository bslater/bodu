// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Scalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2bCore
{
    /// <summary>
    /// Compresses one block with the portable scalar kernel, the reference the vector kernels are tested against.
    /// </summary>
    /// <param name="h">The first of the eight chaining-state words, updated in place.</param>
    /// <param name="block">The first byte of the 128-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="finalization">All ones for the final block; otherwise zero.</param>
    /// <remarks>
    /// The twelve rounds are written out with the message schedule σ resolved to constant indices, so each <c>G</c>
    /// reads its two message words straight from the block and the sixteen working words stay in locals.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CompressScalar(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        ulong v0 = h;
        ulong v1 = Unsafe.Add(ref h, 1);
        ulong v2 = Unsafe.Add(ref h, 2);
        ulong v3 = Unsafe.Add(ref h, 3);
        ulong v4 = Unsafe.Add(ref h, 4);
        ulong v5 = Unsafe.Add(ref h, 5);
        ulong v6 = Unsafe.Add(ref h, 6);
        ulong v7 = Unsafe.Add(ref h, 7);
        ulong v8 = Iv0;
        ulong v9 = Iv1;
        ulong v10 = Iv2;
        ulong v11 = Iv3;
        ulong v12 = Iv4 ^ counter;
        ulong v13 = Iv5;
        ulong v14 = Iv6 ^ finalization;
        ulong v15 = Iv7;

        // Round 0: σ0.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 0), M(ref block, 1));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 2), M(ref block, 3));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 4), M(ref block, 5));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 6), M(ref block, 7));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 8), M(ref block, 9));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 10), M(ref block, 11));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 12), M(ref block, 13));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 14), M(ref block, 15));

        // Round 1: σ1.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 14), M(ref block, 10));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 4), M(ref block, 8));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 9), M(ref block, 15));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 13), M(ref block, 6));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 1), M(ref block, 12));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 0), M(ref block, 2));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 11), M(ref block, 7));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 5), M(ref block, 3));

        // Round 2: σ2.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 11), M(ref block, 8));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 12), M(ref block, 0));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 5), M(ref block, 2));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 15), M(ref block, 13));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 10), M(ref block, 14));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 3), M(ref block, 6));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 7), M(ref block, 1));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 9), M(ref block, 4));

        // Round 3: σ3.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 7), M(ref block, 9));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 3), M(ref block, 1));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 13), M(ref block, 12));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 11), M(ref block, 14));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 2), M(ref block, 6));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 5), M(ref block, 10));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 4), M(ref block, 0));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 15), M(ref block, 8));

        // Round 4: σ4.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 9), M(ref block, 0));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 5), M(ref block, 7));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 2), M(ref block, 4));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 10), M(ref block, 15));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 14), M(ref block, 1));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 11), M(ref block, 12));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 6), M(ref block, 8));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 3), M(ref block, 13));

        // Round 5: σ5.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 2), M(ref block, 12));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 6), M(ref block, 10));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 0), M(ref block, 11));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 8), M(ref block, 3));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 4), M(ref block, 13));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 7), M(ref block, 5));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 15), M(ref block, 14));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 1), M(ref block, 9));

        // Round 6: σ6.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 12), M(ref block, 5));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 1), M(ref block, 15));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 14), M(ref block, 13));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 4), M(ref block, 10));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 0), M(ref block, 7));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 6), M(ref block, 3));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 9), M(ref block, 2));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 8), M(ref block, 11));

        // Round 7: σ7.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 13), M(ref block, 11));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 7), M(ref block, 14));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 12), M(ref block, 1));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 3), M(ref block, 9));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 5), M(ref block, 0));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 15), M(ref block, 4));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 8), M(ref block, 6));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 2), M(ref block, 10));

        // Round 8: σ8.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 6), M(ref block, 15));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 14), M(ref block, 9));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 11), M(ref block, 3));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 0), M(ref block, 8));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 12), M(ref block, 2));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 13), M(ref block, 7));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 1), M(ref block, 4));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 10), M(ref block, 5));

        // Round 9: σ9.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 10), M(ref block, 2));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 8), M(ref block, 4));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 7), M(ref block, 6));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 1), M(ref block, 5));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 15), M(ref block, 11));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 9), M(ref block, 14));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 3), M(ref block, 12));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 13), M(ref block, 0));

        // Round 10: σ0.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 0), M(ref block, 1));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 2), M(ref block, 3));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 4), M(ref block, 5));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 6), M(ref block, 7));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 8), M(ref block, 9));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 10), M(ref block, 11));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 12), M(ref block, 13));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 14), M(ref block, 15));

        // Round 11: σ1.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 14), M(ref block, 10));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 4), M(ref block, 8));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 9), M(ref block, 15));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 13), M(ref block, 6));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 1), M(ref block, 12));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 0), M(ref block, 2));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 11), M(ref block, 7));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 5), M(ref block, 3));

        h ^= v0 ^ v8;
        Unsafe.Add(ref h, 1) ^= v1 ^ v9;
        Unsafe.Add(ref h, 2) ^= v2 ^ v10;
        Unsafe.Add(ref h, 3) ^= v3 ^ v11;
        Unsafe.Add(ref h, 4) ^= v4 ^ v12;
        Unsafe.Add(ref h, 5) ^= v5 ^ v13;
        Unsafe.Add(ref h, 6) ^= v6 ^ v14;
        Unsafe.Add(ref h, 7) ^= v7 ^ v15;
    }

    /// <summary>
    /// The BLAKE2b mixing function <c>G</c> (RFC 7693, Section 3.1), applied to four working words in place.
    /// </summary>
    /// <param name="a">The first working word.</param>
    /// <param name="b">The second working word.</param>
    /// <param name="c">The third working word.</param>
    /// <param name="d">The fourth working word.</param>
    /// <param name="x">The first message word.</param>
    /// <param name="y">The second message word.</param>
    /// <remarks>
    /// On .NET 10 the rotations go through Bodu.Core's
    /// <see cref="NumericExtensions.RotateBitsRightUnchecked(ulong, int)" />, which the JIT inlines to the same
    /// instruction as <see cref="BitOperations.RotateRight(ulong, int)" />. On .NET 8 they call
    /// <see cref="BitOperations" /> directly: there the wrapper costs one more inline per rotation, and across the 96
    /// calls to this method that <c>CompressScalar</c> unrolls, that exhausts the JIT's inline budget, so this method
    /// and the message loads stop being inlined and the scalar path runs about 3.4 times slower.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(ref ulong a, ref ulong b, ref ulong c, ref ulong d, ulong x, ulong y)
    {
#if NET10_0_OR_GREATER
        a += b + x;
        d = (d ^ a).RotateBitsRightUnchecked(32);
        c += d;
        b = (b ^ c).RotateBitsRightUnchecked(24);
        a += b + y;
        d = (d ^ a).RotateBitsRightUnchecked(16);
        c += d;
        b = (b ^ c).RotateBitsRightUnchecked(63);
#else
        a += b + x;
        d = BitOperations.RotateRight(d ^ a, 32);
        c += d;
        b = BitOperations.RotateRight(b ^ c, 24);
        a += b + y;
        d = BitOperations.RotateRight(d ^ a, 16);
        c += d;
        b = BitOperations.RotateRight(b ^ c, 63);
#endif
    }

    /// <summary>
    /// Reads message word <paramref name="index" /> of a block, little-endian.
    /// </summary>
    /// <param name="block">The first byte of the block.</param>
    /// <param name="index">The word's index, from 0 to 15.</param>
    /// <returns>The message word.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong M(ref byte block, int index)
    {
        ulong word = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref block, index * sizeof(ulong)));
        return BitConverter.IsLittleEndian ? word : BinaryPrimitives.ReverseEndianness(word);
    }
}
