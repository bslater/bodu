// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Scalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>
    /// Compresses one block with the portable scalar kernel, the reference the vector kernels are tested against.
    /// </summary>
    /// <param name="cv">The first of the eight chaining-value words, replaced in place.</param>
    /// <param name="block">The first byte of the 64-byte block.</param>
    /// <param name="counter">The chunk counter.</param>
    /// <param name="blockLength">The number of message bytes in the block.</param>
    /// <param name="flags">The domain-separation flags.</param>
    /// <remarks>
    /// The seven rounds are written out with the message schedule resolved to constant indices, so each <c>G</c> reads
    /// its two message words straight from the block and the sixteen working words stay in locals.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CompressScalar(ref uint cv, ref byte block, ulong counter, uint blockLength, uint flags)
    {
        uint v0 = cv;
        uint v1 = Unsafe.Add(ref cv, 1);
        uint v2 = Unsafe.Add(ref cv, 2);
        uint v3 = Unsafe.Add(ref cv, 3);
        uint v4 = Unsafe.Add(ref cv, 4);
        uint v5 = Unsafe.Add(ref cv, 5);
        uint v6 = Unsafe.Add(ref cv, 6);
        uint v7 = Unsafe.Add(ref cv, 7);
        uint v8 = Iv0;
        uint v9 = Iv1;
        uint v10 = Iv2;
        uint v11 = Iv3;
        uint v12 = (uint)counter;
        uint v13 = (uint)(counter >> 32);
        uint v14 = blockLength;
        uint v15 = flags;

        // Round 0: the block as given.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 0), M(ref block, 1));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 2), M(ref block, 3));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 4), M(ref block, 5));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 6), M(ref block, 7));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 8), M(ref block, 9));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 10), M(ref block, 11));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 12), M(ref block, 13));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 14), M(ref block, 15));

        // Round 1: the block permuted once.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 2), M(ref block, 6));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 3), M(ref block, 10));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 7), M(ref block, 0));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 4), M(ref block, 13));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 1), M(ref block, 11));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 12), M(ref block, 5));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 9), M(ref block, 14));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 15), M(ref block, 8));

        // Round 2: permuted twice.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 3), M(ref block, 4));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 10), M(ref block, 12));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 13), M(ref block, 2));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 7), M(ref block, 14));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 6), M(ref block, 5));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 9), M(ref block, 0));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 11), M(ref block, 15));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 8), M(ref block, 1));

        // Round 3: permuted three times.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 10), M(ref block, 7));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 12), M(ref block, 9));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 14), M(ref block, 3));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 13), M(ref block, 15));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 4), M(ref block, 0));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 11), M(ref block, 2));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 5), M(ref block, 8));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 1), M(ref block, 6));

        // Round 4: permuted four times.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 12), M(ref block, 13));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 9), M(ref block, 11));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 15), M(ref block, 10));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 14), M(ref block, 8));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 7), M(ref block, 2));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 5), M(ref block, 3));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 0), M(ref block, 1));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 6), M(ref block, 4));

        // Round 5: permuted five times.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 9), M(ref block, 14));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 11), M(ref block, 5));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 8), M(ref block, 12));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 15), M(ref block, 1));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 13), M(ref block, 3));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 0), M(ref block, 10));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 2), M(ref block, 6));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 4), M(ref block, 7));

        // Round 6: permuted six times.
        G(ref v0, ref v4, ref v8, ref v12, M(ref block, 11), M(ref block, 15));
        G(ref v1, ref v5, ref v9, ref v13, M(ref block, 5), M(ref block, 0));
        G(ref v2, ref v6, ref v10, ref v14, M(ref block, 1), M(ref block, 9));
        G(ref v3, ref v7, ref v11, ref v15, M(ref block, 8), M(ref block, 6));
        G(ref v0, ref v5, ref v10, ref v15, M(ref block, 14), M(ref block, 10));
        G(ref v1, ref v6, ref v11, ref v12, M(ref block, 2), M(ref block, 12));
        G(ref v2, ref v7, ref v8, ref v13, M(ref block, 3), M(ref block, 4));
        G(ref v3, ref v4, ref v9, ref v14, M(ref block, 7), M(ref block, 13));

        cv = v0 ^ v8;
        Unsafe.Add(ref cv, 1) = v1 ^ v9;
        Unsafe.Add(ref cv, 2) = v2 ^ v10;
        Unsafe.Add(ref cv, 3) = v3 ^ v11;
        Unsafe.Add(ref cv, 4) = v4 ^ v12;
        Unsafe.Add(ref cv, 5) = v5 ^ v13;
        Unsafe.Add(ref cv, 6) = v6 ^ v14;
        Unsafe.Add(ref cv, 7) = v7 ^ v15;
    }

    /// <summary>
    /// The BLAKE3 mixing function <c>G</c>, applied to four working words in place: BLAKE2s's <c>G</c>, rotations
    /// included.
    /// </summary>
    /// <param name="a">The first working word.</param>
    /// <param name="b">The second working word.</param>
    /// <param name="c">The third working word.</param>
    /// <param name="d">The fourth working word.</param>
    /// <param name="x">The first message word.</param>
    /// <param name="y">The second message word.</param>
    /// <remarks>
    /// Each message word is added to <paramref name="a" /> before <paramref name="b" /> is. The word
    /// <paramref name="b" /> is the last that the step before computes, so adding it last leaves one addition, not two,
    /// waiting on it.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(ref uint a, ref uint b, ref uint c, ref uint d, uint x, uint y)
    {
        a = a + x + b;
        d = (d ^ a).RotateBitsRightUnchecked(16);
        c += d;
        b = (b ^ c).RotateBitsRightUnchecked(12);
        a = a + y + b;
        d = (d ^ a).RotateBitsRightUnchecked(8);
        c += d;
        b = (b ^ c).RotateBitsRightUnchecked(7);
    }

    /// <summary>
    /// Reads message word <paramref name="index" /> of a block, little-endian.
    /// </summary>
    /// <param name="block">The first byte of the block.</param>
    /// <param name="index">The word's index, from 0 to 15.</param>
    /// <returns>The message word.</returns>
    /// <remarks>
    /// The index is native-sized so that a vector kernel's gather, which reads it from the message schedule, scales it
    /// within the address rather than in a register of its own, and the load can fold into the instruction that inserts
    /// the word.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint M(ref byte block, nuint index)
    {
        uint word = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref block, index * sizeof(uint)));
        return BitConverter.IsLittleEndian ? word : BinaryPrimitives.ReverseEndianness(word);
    }
}
