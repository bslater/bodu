// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Hybrid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// The compression function over pairs of rows and pairs of columns: one of each pair in the vector registers, laid
    /// out as <see cref="Vector128Kernel{TIsa}" /> lays out a row, and the other in general registers, as
    /// <see cref="ResidentScalarKernel" /> holds one, their steps interleaved so that the vector pipes and the integer
    /// pipes work at once. Temporary: F13 of the follow-up plan measures it on ARM64 against the scalar and AdvSimd
    /// kernels.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set supplying the vector operations with no portable form.</typeparam>
    /// <remarks>
    /// The eight rows of a block are independent of each other, as are its eight columns, so any two can be computed at
    /// once. Per round of one row, the AdvSimd kernel issues about 150 instructions to the vector pipes and the scalar
    /// kernel about 200 to the integer pipes; on a processor with two vector pipes and four integer pipes neither alone
    /// keeps the other's pipes busy.
    /// </remarks>
    internal readonly struct HybridKernel<TIsa>
        : IArgon2Kernel
        where TIsa : struct, IVector128Isa
    {
        /// <summary>The number of 128-bit vectors in a block.</summary>
        private const int VectorsPerBlock = WordsPerBlock / 2;

        /// <summary>
        /// Computes <c>G(X, Y)</c> for the previous block <c>X</c> held in <paramref name="state" /> and the reference
        /// block <c>Y</c>, stores it in <paramref name="next" />, and leaves it in <paramref name="state" />.
        /// </summary>
        /// <param name="state">128 words holding the previous block on entry and the new block on return.</param>
        /// <param name="scratch">128 words of working space; their contents on entry are ignored.</param>
        /// <param name="reference">
        /// The 128-word reference block <c>Y</c>, read before the destination is written.
        /// </param>
        /// <param name="next">The 128-word destination block.</param>
        /// <param name="withXor">
        /// <see langword="true" /> to XOR the result into the destination's existing contents; otherwise it is
        /// overwritten.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void FillBlock(ref ulong state, ref ulong scratch, ref ulong reference, ref ulong next, bool withXor)
        {
            ref Vector128<ulong> s = ref Unsafe.As<ulong, Vector128<ulong>>(ref state);
            ref Vector128<ulong> xy = ref Unsafe.As<ulong, Vector128<ulong>>(ref scratch);

            if (withXor)
            {
                for (int i = 0; i < VectorsPerBlock; i++)
                {
                    Vector128<ulong> r = Unsafe.Add(ref s, i) ^ Vector128.LoadUnsafe(ref reference, (nuint)(i * 2));
                    Unsafe.Add(ref s, i) = r;
                    Unsafe.Add(ref xy, i) = r ^ Vector128.LoadUnsafe(ref next, (nuint)(i * 2));
                }
            }
            else
            {
                for (int i = 0; i < VectorsPerBlock; i++)
                {
                    Vector128<ulong> r = Unsafe.Add(ref s, i) ^ Vector128.LoadUnsafe(ref reference, (nuint)(i * 2));
                    Unsafe.Add(ref s, i) = r;
                    Unsafe.Add(ref xy, i) = r;
                }
            }

            // Rows 0, 2, 4 and 6 in vector registers, rows 1, 3, 5 and 7 in general registers.
            for (int i = 0; i < 8; i += 2)
                RoundPair(ref Unsafe.Add(ref s, 8 * i), 1, ref Unsafe.Add(ref state, 16 * (i + 1)), 2);

            // Columns 0, 2, 4 and 6 in vector registers, columns 1, 3, 5 and 7 in general registers.
            for (int i = 0; i < 8; i += 2)
                RoundPair(ref Unsafe.Add(ref s, i), 8, ref Unsafe.Add(ref state, 2 * (i + 1)), 16);

            for (int i = 0; i < VectorsPerBlock; i++)
            {
                Vector128<ulong> value = Unsafe.Add(ref s, i) ^ Unsafe.Add(ref xy, i);
                Unsafe.Add(ref s, i) = value;
                value.StoreUnsafe(ref next, (nuint)(i * 2));
            }
        }

        /// <summary>
        /// Applies one BLAKE2b round to two rows or two columns at once: one as eight vector registers, the other as
        /// sixteen words in general registers.
        /// </summary>
        /// <param name="v">The first register of the row or column held in vector registers.</param>
        /// <param name="stride">The distance, in registers, between that row's or column's registers.</param>
        /// <param name="w">The first word of the row or column held in general registers.</param>
        /// <param name="pairStride">
        /// The distance, in words, between that row's or column's consecutive pairs of words: 2 for a row, 16 for a
        /// column.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RoundPair(ref Vector128<ulong> v, int stride, ref ulong w, int pairStride)
        {
            Vector128<ulong> a0 = v;
            Vector128<ulong> a1 = Unsafe.Add(ref v, stride);
            Vector128<ulong> b0 = Unsafe.Add(ref v, 2 * stride);
            Vector128<ulong> b1 = Unsafe.Add(ref v, 3 * stride);
            Vector128<ulong> c0 = Unsafe.Add(ref v, 4 * stride);
            Vector128<ulong> c1 = Unsafe.Add(ref v, 5 * stride);
            Vector128<ulong> d0 = Unsafe.Add(ref v, 6 * stride);
            Vector128<ulong> d1 = Unsafe.Add(ref v, 7 * stride);

            ulong w0 = w;
            ulong w1 = Unsafe.Add(ref w, 1);
            ulong w2 = Unsafe.Add(ref w, pairStride);
            ulong w3 = Unsafe.Add(ref w, pairStride + 1);
            ulong w4 = Unsafe.Add(ref w, 2 * pairStride);
            ulong w5 = Unsafe.Add(ref w, (2 * pairStride) + 1);
            ulong w6 = Unsafe.Add(ref w, 3 * pairStride);
            ulong w7 = Unsafe.Add(ref w, (3 * pairStride) + 1);
            ulong w8 = Unsafe.Add(ref w, 4 * pairStride);
            ulong w9 = Unsafe.Add(ref w, (4 * pairStride) + 1);
            ulong w10 = Unsafe.Add(ref w, 5 * pairStride);
            ulong w11 = Unsafe.Add(ref w, (5 * pairStride) + 1);
            ulong w12 = Unsafe.Add(ref w, 6 * pairStride);
            ulong w13 = Unsafe.Add(ref w, (6 * pairStride) + 1);
            ulong w14 = Unsafe.Add(ref w, 7 * pairStride);
            ulong w15 = Unsafe.Add(ref w, (7 * pairStride) + 1);

            // Each half of each vector step is followed by the same half of the four matching scalar steps, so the two
            // streams of instructions stay close enough together for the processor to issue them side by side.
            Vector128Kernel<TIsa>.G1(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);
            ResidentScalarKernel.GB1(ref w0, ref w4, ref w8, ref w12);
            ResidentScalarKernel.GB1(ref w1, ref w5, ref w9, ref w13);
            ResidentScalarKernel.GB1(ref w2, ref w6, ref w10, ref w14);
            ResidentScalarKernel.GB1(ref w3, ref w7, ref w11, ref w15);
            Vector128Kernel<TIsa>.G2(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);
            ResidentScalarKernel.GB2(ref w0, ref w4, ref w8, ref w12);
            ResidentScalarKernel.GB2(ref w1, ref w5, ref w9, ref w13);
            ResidentScalarKernel.GB2(ref w2, ref w6, ref w10, ref w14);
            ResidentScalarKernel.GB2(ref w3, ref w7, ref w11, ref w15);

            // Shift b, c, and d by one, two, and three words across each register pair, so the diagonals line up.
            Vector128<ulong> t0 = TIsa.UpperThenLower(b0, b1);
            Vector128<ulong> t1 = TIsa.UpperThenLower(b1, b0);
            b0 = t0;
            b1 = t1;
            (c0, c1) = (c1, c0);
            t0 = TIsa.UpperThenLower(d1, d0);
            t1 = TIsa.UpperThenLower(d0, d1);
            d0 = t0;
            d1 = t1;

            Vector128Kernel<TIsa>.G1(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);
            ResidentScalarKernel.GB1(ref w0, ref w5, ref w10, ref w15);
            ResidentScalarKernel.GB1(ref w1, ref w6, ref w11, ref w12);
            ResidentScalarKernel.GB1(ref w2, ref w7, ref w8, ref w13);
            ResidentScalarKernel.GB1(ref w3, ref w4, ref w9, ref w14);
            Vector128Kernel<TIsa>.G2(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);
            ResidentScalarKernel.GB2(ref w0, ref w5, ref w10, ref w15);
            ResidentScalarKernel.GB2(ref w1, ref w6, ref w11, ref w12);
            ResidentScalarKernel.GB2(ref w2, ref w7, ref w8, ref w13);
            ResidentScalarKernel.GB2(ref w3, ref w4, ref w9, ref w14);

            t0 = TIsa.UpperThenLower(b1, b0);
            t1 = TIsa.UpperThenLower(b0, b1);
            b0 = t0;
            b1 = t1;
            (c0, c1) = (c1, c0);
            t0 = TIsa.UpperThenLower(d0, d1);
            t1 = TIsa.UpperThenLower(d1, d0);
            d0 = t0;
            d1 = t1;

            v = a0;
            Unsafe.Add(ref v, stride) = a1;
            Unsafe.Add(ref v, 2 * stride) = b0;
            Unsafe.Add(ref v, 3 * stride) = b1;
            Unsafe.Add(ref v, 4 * stride) = c0;
            Unsafe.Add(ref v, 5 * stride) = c1;
            Unsafe.Add(ref v, 6 * stride) = d0;
            Unsafe.Add(ref v, 7 * stride) = d1;

            w = w0;
            Unsafe.Add(ref w, 1) = w1;
            Unsafe.Add(ref w, pairStride) = w2;
            Unsafe.Add(ref w, pairStride + 1) = w3;
            Unsafe.Add(ref w, 2 * pairStride) = w4;
            Unsafe.Add(ref w, (2 * pairStride) + 1) = w5;
            Unsafe.Add(ref w, 3 * pairStride) = w6;
            Unsafe.Add(ref w, (3 * pairStride) + 1) = w7;
            Unsafe.Add(ref w, 4 * pairStride) = w8;
            Unsafe.Add(ref w, (4 * pairStride) + 1) = w9;
            Unsafe.Add(ref w, 5 * pairStride) = w10;
            Unsafe.Add(ref w, (5 * pairStride) + 1) = w11;
            Unsafe.Add(ref w, 6 * pairStride) = w12;
            Unsafe.Add(ref w, (6 * pairStride) + 1) = w13;
            Unsafe.Add(ref w, 7 * pairStride) = w14;
            Unsafe.Add(ref w, (7 * pairStride) + 1) = w15;
        }
    }
}
