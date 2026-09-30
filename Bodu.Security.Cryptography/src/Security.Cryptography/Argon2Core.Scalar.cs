// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Scalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// The portable scalar implementation of the compression function. It runs where no vector instruction set is
    /// available or the process has opted out of SIMD, and it is the reference the vector kernels are tested against.
    /// </summary>
    internal readonly struct ScalarKernel
        : IArgon2Kernel
    {
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
            // R = X xor Y replaces X in the state. The scratch keeps R for the final combination - or R xor the
            // destination's old contents, on passes that XOR - so the destination can be overwritten in one sweep.
            if (withXor)
            {
                for (int k = 0; k < WordsPerBlock; k++)
                {
                    ulong r = Unsafe.Add(ref state, k) ^ Unsafe.Add(ref reference, k);
                    Unsafe.Add(ref state, k) = r;
                    Unsafe.Add(ref scratch, k) = r ^ Unsafe.Add(ref next, k);
                }
            }
            else
            {
                for (int k = 0; k < WordsPerBlock; k++)
                {
                    ulong r = Unsafe.Add(ref state, k) ^ Unsafe.Add(ref reference, k);
                    Unsafe.Add(ref state, k) = r;
                    Unsafe.Add(ref scratch, k) = r;
                }
            }

            // P rowwise, then columnwise (RFC 9106, Figures 15 and 18).
            for (int row = 0; row < 8; row++)
                RoundRow(ref Unsafe.Add(ref state, row * 16));

            for (int column = 0; column < 8; column++)
                RoundColumn(ref Unsafe.Add(ref state, column * 2));

            // The new block is P(R) xor R (xor the old contents), and becomes the state for the following block.
            for (int k = 0; k < WordsPerBlock; k++)
            {
                ulong value = Unsafe.Add(ref state, k) ^ Unsafe.Add(ref scratch, k);
                Unsafe.Add(ref state, k) = value;
                Unsafe.Add(ref next, k) = value;
            }
        }

        /// <summary>
        /// Applies one BLAKE2b round (eight <c>GB</c> calls) to a row: the sixteen consecutive words from
        /// <paramref name="v" />.
        /// </summary>
        /// <param name="v">The row's first word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RoundRow(ref ulong v)
        {
            GB(ref v, 0, 4, 8, 12);
            GB(ref v, 1, 5, 9, 13);
            GB(ref v, 2, 6, 10, 14);
            GB(ref v, 3, 7, 11, 15);

            GB(ref v, 0, 5, 10, 15);
            GB(ref v, 1, 6, 11, 12);
            GB(ref v, 2, 7, 8, 13);
            GB(ref v, 3, 4, 9, 14);
        }

        /// <summary>
        /// Applies one BLAKE2b round (eight <c>GB</c> calls) to a column: the word pair at <paramref name="v" /> and
        /// the same pair in each of the seven rows below it.
        /// </summary>
        /// <param name="v">The column's first word.</param>
        /// <remarks>
        /// Column word <c>i</c> of the round is block word <c>16 * (i / 2) + i % 2</c> from the column's start, so the
        /// round's <c>GB(v0, v4, v8, v12)</c> reads words 0, 32, 64, and 96.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RoundColumn(ref ulong v)
        {
            GB(ref v, 0, 32, 64, 96);
            GB(ref v, 1, 33, 65, 97);
            GB(ref v, 16, 48, 80, 112);
            GB(ref v, 17, 49, 81, 113);

            GB(ref v, 0, 33, 80, 113);
            GB(ref v, 1, 48, 81, 96);
            GB(ref v, 16, 49, 64, 97);
            GB(ref v, 17, 32, 65, 112);
        }

        /// <summary>
        /// The Argon2 mixing function <c>GB</c> - the BLAKE2b round function augmented with 64-bit multiplications (RFC
        /// 9106, Section 3.6).
        /// </summary>
        /// <param name="v">The word the offsets are relative to.</param>
        /// <param name="a">The offset of the first state word, combined and updated in place.</param>
        /// <param name="b">The offset of the second state word, combined and updated in place.</param>
        /// <param name="c">The offset of the third state word, combined and updated in place.</param>
        /// <param name="d">The offset of the fourth state word, combined and updated in place.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GB(ref ulong v, int a, int b, int c, int d)
        {
            ref ulong wa = ref Unsafe.Add(ref v, a);
            ref ulong wb = ref Unsafe.Add(ref v, b);
            ref ulong wc = ref Unsafe.Add(ref v, c);
            ref ulong wd = ref Unsafe.Add(ref v, d);

            // Working on locals lets the JIT keep all four words in registers across the eight steps.
            ulong va = wa, vb = wb, vc = wc, vd = wd;

            va = FBlaMka(va, vb);
            vd = (vd ^ va).RotateBitsRightUnchecked(32);
            vc = FBlaMka(vc, vd);
            vb = (vb ^ vc).RotateBitsRightUnchecked(24);
            va = FBlaMka(va, vb);
            vd = (vd ^ va).RotateBitsRightUnchecked(16);
            vc = FBlaMka(vc, vd);
            vb = (vb ^ vc).RotateBitsRightUnchecked(63);

            wa = va;
            wb = vb;
            wc = vc;
            wd = vd;
        }

        /// <summary>
        /// Computes <c>x + y + 2 * trunc(x) * trunc(y)</c> modulo 2^64, where <c>trunc(x)</c> is the low 32 bits of x.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The value <c>x + y + 2·trunc(x)·trunc(y)</c> modulo 2^64.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FBlaMka(ulong x, ulong y)
        {
            ulong xy = (ulong)(uint)x * (uint)y;
            return x + y + (2 * xy);
        }
    }
}
