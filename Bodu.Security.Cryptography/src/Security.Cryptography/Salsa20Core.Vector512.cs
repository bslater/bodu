// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20Core.Vector512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class Salsa20Core
{
    /// <summary>
    /// Produces the Salsa20 keystream sixteen blocks at a time over 512-bit vectors with AVX-512F: lane <c>i</c> of
    /// vector <c>w</c> holds word <c>w</c> of the run's <c>i</c>-th block, whose counter is the run's first counter
    /// plus <c>i</c>.
    /// </summary>
    internal static class Vector512Kernel
    {
        /// <summary>
        /// Combines runs of sixteen blocks of input with the keystream by XOR.
        /// </summary>
        /// <param name="state">The first of the sixteen state words; the counter words are ignored.</param>
        /// <param name="counter">The 64-bit block counter of the first block.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination, which may be the first byte of the input.</param>
        /// <param name="groups">The number of runs of sixteen blocks.</param>
        /// <remarks>
        /// The kernel is compiled on its own, never inlined into its caller, so that the caller's inlining budget and
        /// profile cannot degrade its code.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void XorBlocks(ref uint state, ulong counter, ref byte input, ref byte output, int groups)
        {
            Vector512<uint> j0 = Vector512.Create(state);
            Vector512<uint> j1 = Vector512.Create(Unsafe.Add(ref state, 1));
            Vector512<uint> j2 = Vector512.Create(Unsafe.Add(ref state, 2));
            Vector512<uint> j3 = Vector512.Create(Unsafe.Add(ref state, 3));
            Vector512<uint> j4 = Vector512.Create(Unsafe.Add(ref state, 4));
            Vector512<uint> j5 = Vector512.Create(Unsafe.Add(ref state, 5));
            Vector512<uint> j6 = Vector512.Create(Unsafe.Add(ref state, 6));
            Vector512<uint> j7 = Vector512.Create(Unsafe.Add(ref state, 7));
            Vector512<uint> j10 = Vector512.Create(Unsafe.Add(ref state, 10));
            Vector512<uint> j11 = Vector512.Create(Unsafe.Add(ref state, 11));
            Vector512<uint> j12 = Vector512.Create(Unsafe.Add(ref state, 12));
            Vector512<uint> j13 = Vector512.Create(Unsafe.Add(ref state, 13));
            Vector512<uint> j14 = Vector512.Create(Unsafe.Add(ref state, 14));
            Vector512<uint> j15 = Vector512.Create(Unsafe.Add(ref state, 15));
            nint offset = 0;

            for (int group = 0; group < groups; group++)
            {
                // The counter's low word counts up across the lanes; a lane where it wrapped carries into the high word.
                Vector512<uint> low = Vector512.Create((uint)counter);
                Vector512<uint> j8 = low + Vector512.Create(0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u, 9u, 10u, 11u, 12u, 13u, 14u, 15u);
                Vector512<uint> j9 = Vector512.Create((uint)(counter >> 32)) - Vector512.LessThan(j8, low);

                Vector512<uint> x0 = j0, x1 = j1, x2 = j2, x3 = j3;
                Vector512<uint> x4 = j4, x5 = j5, x6 = j6, x7 = j7;
                Vector512<uint> x8 = j8, x9 = j9, x10 = j10, x11 = j11;
                Vector512<uint> x12 = j12, x13 = j13, x14 = j14, x15 = j15;

                for (int round = 0; round < DoubleRounds; round++)
                {
                    QuarterRound(ref x0, ref x4, ref x8, ref x12);
                    QuarterRound(ref x5, ref x9, ref x13, ref x1);
                    QuarterRound(ref x10, ref x14, ref x2, ref x6);
                    QuarterRound(ref x15, ref x3, ref x7, ref x11);

                    QuarterRound(ref x0, ref x1, ref x2, ref x3);
                    QuarterRound(ref x5, ref x6, ref x7, ref x4);
                    QuarterRound(ref x10, ref x11, ref x8, ref x9);
                    QuarterRound(ref x15, ref x12, ref x13, ref x14);
                }

                x0 += j0;
                x1 += j1;
                x2 += j2;
                x3 += j3;
                x4 += j4;
                x5 += j5;
                x6 += j6;
                x7 += j7;
                x8 += j8;
                x9 += j9;
                x10 += j10;
                x11 += j11;
                x12 += j12;
                x13 += j13;
                x14 += j14;
                x15 += j15;

                ChaCha20Core.Vector512Kernel.Transpose(ref x0, ref x1, ref x2, ref x3);
                ChaCha20Core.Vector512Kernel.Transpose(ref x4, ref x5, ref x6, ref x7);
                ChaCha20Core.Vector512Kernel.Transpose(ref x8, ref x9, ref x10, ref x11);
                ChaCha20Core.Vector512Kernel.Transpose(ref x12, ref x13, ref x14, ref x15);

                ChaCha20Core.Vector512Kernel.XorQuarters(x0, x4, x8, x12, ref input, ref output, offset);
                ChaCha20Core.Vector512Kernel.XorQuarters(x1, x5, x9, x13, ref input, ref output, offset + BlockBytes);
                ChaCha20Core.Vector512Kernel.XorQuarters(x2, x6, x10, x14, ref input, ref output, offset + (2 * BlockBytes));
                ChaCha20Core.Vector512Kernel.XorQuarters(x3, x7, x11, x15, ref input, ref output, offset + (3 * BlockBytes));

                counter += 16;
                offset += 16 * BlockBytes;
            }
        }

        /// <summary>
        /// Applies the Salsa20 quarter round to four words of every lane.
        /// </summary>
        /// <param name="a">The first word.</param>
        /// <param name="b">The second word.</param>
        /// <param name="c">The third word.</param>
        /// <param name="d">The fourth word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void QuarterRound(ref Vector512<uint> a, ref Vector512<uint> b, ref Vector512<uint> c, ref Vector512<uint> d)
        {
            b ^= (a + d).RotateBitsLeftUnchecked<VectorRotation.Avx512>(7);
            c ^= (b + a).RotateBitsLeftUnchecked<VectorRotation.Avx512>(9);
            d ^= (c + b).RotateBitsLeftUnchecked<VectorRotation.Avx512>(13);
            a ^= (d + c).RotateBitsLeftUnchecked<VectorRotation.Avx512>(18);
        }
    }
}
