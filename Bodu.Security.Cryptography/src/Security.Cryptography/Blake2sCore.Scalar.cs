// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.Scalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2sCore
{
    /// <summary>
    /// Compresses one block with the portable scalar kernel, the reference the vector kernels are tested against.
    /// </summary>
    /// <param name="h">The first of the eight chaining-state words, updated in place.</param>
    /// <param name="block">The first byte of the 64-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="finalization">All ones for the final block; otherwise zero.</param>
    /// <remarks>
    /// The ten rounds are written out with the message schedule σ resolved to constant indices, so each <c>G</c> reads
    /// its two message words straight from the block and the sixteen working words stay in locals.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CompressScalar(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        uint v0 = h;
        uint v1 = Unsafe.Add(ref h, 1);
        uint v2 = Unsafe.Add(ref h, 2);
        uint v3 = Unsafe.Add(ref h, 3);
        uint v4 = Unsafe.Add(ref h, 4);
        uint v5 = Unsafe.Add(ref h, 5);
        uint v6 = Unsafe.Add(ref h, 6);
        uint v7 = Unsafe.Add(ref h, 7);
        uint v8 = Iv0;
        uint v9 = Iv1;
        uint v10 = Iv2;
        uint v11 = Iv3;
        uint v12 = Iv4 ^ (uint)counter;
        uint v13 = Iv5 ^ (uint)(counter >> 32);
        uint v14 = Iv6 ^ finalization;
        uint v15 = Iv7;

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
    /// The BLAKE2s mixing function <c>G</c> (RFC 7693, Section 3.1), applied to four working words in place.
    /// </summary>
    /// <param name="a">The first working word.</param>
    /// <param name="b">The second working word.</param>
    /// <param name="c">The third working word.</param>
    /// <param name="d">The fourth working word.</param>
    /// <param name="x">The first message word.</param>
    /// <param name="y">The second message word.</param>
    /// <remarks>
    /// On .NET 10 the rotations go through Bodu.Core's
    /// <see cref="NumericExtensions.RotateBitsRightUnchecked(uint, int)" />, which the JIT inlines to the same
    /// instruction as <see cref="BitOperations.RotateRight(uint, int)" />. On .NET 8 they call
    /// <see cref="BitOperations" /> directly: there the wrapper costs one more inline per rotation, and across the 80
    /// calls to this method that <c>CompressScalar</c> unrolls, that exhausts the JIT's inline budget, so this method
    /// and the message loads stop being inlined and the scalar path runs about 3.5 times slower.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(ref uint a, ref uint b, ref uint c, ref uint d, uint x, uint y)
    {
#if NET10_0_OR_GREATER
        a += b + x;
        d = (d ^ a).RotateBitsRightUnchecked(16);
        c += d;
        b = (b ^ c).RotateBitsRightUnchecked(12);
        a += b + y;
        d = (d ^ a).RotateBitsRightUnchecked(8);
        c += d;
        b = (b ^ c).RotateBitsRightUnchecked(7);
#else
        a += b + x;
        d = BitOperations.RotateRight(d ^ a, 16);
        c += d;
        b = BitOperations.RotateRight(b ^ c, 12);
        a += b + y;
        d = BitOperations.RotateRight(d ^ a, 8);
        c += d;
        b = BitOperations.RotateRight(b ^ c, 7);
#endif
    }

    /// <summary>
    /// Reads message word <paramref name="index" /> of a block, little-endian.
    /// </summary>
    /// <param name="block">The first byte of the block.</param>
    /// <param name="index">The word's index, from 0 to 15.</param>
    /// <returns>The message word.</returns>
    /// <remarks>
    /// The index is native-sized so that a vector kernel's gather, which reads it from σ, scales it within the address
    /// rather than in a register of its own, and the load can fold into the instruction that inserts the word.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint M(ref byte block, nuint index)
    {
        uint word = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref block, index * sizeof(uint)));
        return BitConverter.IsLittleEndian ? word : BinaryPrimitives.ReverseEndianness(word);
    }
}
