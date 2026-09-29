// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20Core.Vector256.cs" company="Bodu Pty. Ltd.">
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
    /// Produces the Salsa20 keystream eight blocks at a time over 256-bit vectors: lane <c>i</c> of vector <c>w</c>
    /// holds word <c>w</c> of the run's <c>i</c>-th block, whose counter is the run's first counter plus <c>i</c>.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set that performs the rotations: <see cref="VectorRotation.Avx2" /> or
    /// <see cref="VectorRotation.Avx512" />.
    /// </typeparam>
    internal static class Vector256Kernel<TIsa>
        where TIsa : struct, IVector256Rotation
    {
        /// <summary>
        /// Combines runs of eight blocks of input with the keystream by XOR.
        /// </summary>
        /// <param name="state">The first of the sixteen state words; the counter words are ignored.</param>
        /// <param name="counter">The 64-bit block counter of the first block.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination, which may be the first byte of the input.</param>
        /// <param name="groups">The number of runs of eight blocks.</param>
        /// <remarks>
        /// The kernel is compiled on its own, never inlined into its caller, so that the caller's inlining budget and
        /// profile cannot degrade its code.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void XorBlocks(ref uint state, ulong counter, ref byte input, ref byte output, int groups)
        {
            Vector256<uint> j0 = Vector256.Create(state);
            Vector256<uint> j1 = Vector256.Create(Unsafe.Add(ref state, 1));
            Vector256<uint> j2 = Vector256.Create(Unsafe.Add(ref state, 2));
            Vector256<uint> j3 = Vector256.Create(Unsafe.Add(ref state, 3));
            Vector256<uint> j4 = Vector256.Create(Unsafe.Add(ref state, 4));
            Vector256<uint> j5 = Vector256.Create(Unsafe.Add(ref state, 5));
            Vector256<uint> j6 = Vector256.Create(Unsafe.Add(ref state, 6));
            Vector256<uint> j7 = Vector256.Create(Unsafe.Add(ref state, 7));
            Vector256<uint> j10 = Vector256.Create(Unsafe.Add(ref state, 10));
            Vector256<uint> j11 = Vector256.Create(Unsafe.Add(ref state, 11));
            Vector256<uint> j12 = Vector256.Create(Unsafe.Add(ref state, 12));
            Vector256<uint> j13 = Vector256.Create(Unsafe.Add(ref state, 13));
            Vector256<uint> j14 = Vector256.Create(Unsafe.Add(ref state, 14));
            Vector256<uint> j15 = Vector256.Create(Unsafe.Add(ref state, 15));
            nint offset = 0;

            for (int group = 0; group < groups; group++)
            {
                // The counter's low word counts up across the lanes; a lane where it wrapped carries into the high word.
                Vector256<uint> low = Vector256.Create((uint)counter);
                Vector256<uint> j8 = low + Vector256.Create(0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u);
                Vector256<uint> j9 = Vector256.Create((uint)(counter >> 32)) - Vector256.LessThan(j8, low);

                Vector256<uint> x0 = j0, x1 = j1, x2 = j2, x3 = j3;
                Vector256<uint> x4 = j4, x5 = j5, x6 = j6, x7 = j7;
                Vector256<uint> x8 = j8, x9 = j9, x10 = j10, x11 = j11;
                Vector256<uint> x12 = j12, x13 = j13, x14 = j14, x15 = j15;

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

                ChaCha20Core.Vector256Kernel<TIsa>.XorWords(x0 + j0, x1 + j1, x2 + j2, x3 + j3, x4 + j4, x5 + j5, x6 + j6, x7 + j7, ref input, ref output, offset);
                ChaCha20Core.Vector256Kernel<TIsa>.XorWords(x8 + j8, x9 + j9, x10 + j10, x11 + j11, x12 + j12, x13 + j13, x14 + j14, x15 + j15, ref input, ref output, offset + 32);

                counter += 8;
                offset += 8 * BlockBytes;
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
        private static void QuarterRound(ref Vector256<uint> a, ref Vector256<uint> b, ref Vector256<uint> c, ref Vector256<uint> d)
        {
            b ^= (a + d).RotateBitsLeftUnchecked<TIsa>(7);
            c ^= (b + a).RotateBitsLeftUnchecked<TIsa>(9);
            d ^= (c + b).RotateBitsLeftUnchecked<TIsa>(13);
            a ^= (d + c).RotateBitsLeftUnchecked<TIsa>(18);
        }
    }
}
