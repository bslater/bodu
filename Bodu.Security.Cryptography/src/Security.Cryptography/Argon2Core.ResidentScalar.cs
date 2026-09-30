// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.ResidentScalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// The scalar compression function holding each row or column in sixteen locals across its round, where
    /// <see cref="ScalarKernel" /> loads and stores four words for every <c>GB</c>. Temporary: F13 of the follow-up
    /// plan measures it against <see cref="ScalarKernel" /> on ARM64, whose thirty-one general registers hold the
    /// sixteen words.
    /// </summary>
    internal readonly struct ResidentScalarKernel
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

            // A row's word pairs are consecutive; a column's are sixteen words apart.
            for (int row = 0; row < 8; row++)
                Round(ref Unsafe.Add(ref state, row * 16), 2);

            for (int column = 0; column < 8; column++)
                Round(ref Unsafe.Add(ref state, column * 2), 16);

            for (int k = 0; k < WordsPerBlock; k++)
            {
                ulong value = Unsafe.Add(ref state, k) ^ Unsafe.Add(ref scratch, k);
                Unsafe.Add(ref state, k) = value;
                Unsafe.Add(ref next, k) = value;
            }
        }

        /// <summary>
        /// Applies one BLAKE2b round (eight <c>GB</c> calls) to the sixteen words of a row or a column, held in locals
        /// from the first load to the last store.
        /// </summary>
        /// <param name="v">The first word.</param>
        /// <param name="pairStride">
        /// The distance, in words, between consecutive pairs of words: 2 for a row, 16 for a column.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Round(ref ulong v, int pairStride)
        {
            ulong v0 = v;
            ulong v1 = Unsafe.Add(ref v, 1);
            ulong v2 = Unsafe.Add(ref v, pairStride);
            ulong v3 = Unsafe.Add(ref v, pairStride + 1);
            ulong v4 = Unsafe.Add(ref v, 2 * pairStride);
            ulong v5 = Unsafe.Add(ref v, (2 * pairStride) + 1);
            ulong v6 = Unsafe.Add(ref v, 3 * pairStride);
            ulong v7 = Unsafe.Add(ref v, (3 * pairStride) + 1);
            ulong v8 = Unsafe.Add(ref v, 4 * pairStride);
            ulong v9 = Unsafe.Add(ref v, (4 * pairStride) + 1);
            ulong v10 = Unsafe.Add(ref v, 5 * pairStride);
            ulong v11 = Unsafe.Add(ref v, (5 * pairStride) + 1);
            ulong v12 = Unsafe.Add(ref v, 6 * pairStride);
            ulong v13 = Unsafe.Add(ref v, (6 * pairStride) + 1);
            ulong v14 = Unsafe.Add(ref v, 7 * pairStride);
            ulong v15 = Unsafe.Add(ref v, (7 * pairStride) + 1);

            GB1(ref v0, ref v4, ref v8, ref v12);
            GB2(ref v0, ref v4, ref v8, ref v12);
            GB1(ref v1, ref v5, ref v9, ref v13);
            GB2(ref v1, ref v5, ref v9, ref v13);
            GB1(ref v2, ref v6, ref v10, ref v14);
            GB2(ref v2, ref v6, ref v10, ref v14);
            GB1(ref v3, ref v7, ref v11, ref v15);
            GB2(ref v3, ref v7, ref v11, ref v15);

            GB1(ref v0, ref v5, ref v10, ref v15);
            GB2(ref v0, ref v5, ref v10, ref v15);
            GB1(ref v1, ref v6, ref v11, ref v12);
            GB2(ref v1, ref v6, ref v11, ref v12);
            GB1(ref v2, ref v7, ref v8, ref v13);
            GB2(ref v2, ref v7, ref v8, ref v13);
            GB1(ref v3, ref v4, ref v9, ref v14);
            GB2(ref v3, ref v4, ref v9, ref v14);

            v = v0;
            Unsafe.Add(ref v, 1) = v1;
            Unsafe.Add(ref v, pairStride) = v2;
            Unsafe.Add(ref v, pairStride + 1) = v3;
            Unsafe.Add(ref v, 2 * pairStride) = v4;
            Unsafe.Add(ref v, (2 * pairStride) + 1) = v5;
            Unsafe.Add(ref v, 3 * pairStride) = v6;
            Unsafe.Add(ref v, (3 * pairStride) + 1) = v7;
            Unsafe.Add(ref v, 4 * pairStride) = v8;
            Unsafe.Add(ref v, (4 * pairStride) + 1) = v9;
            Unsafe.Add(ref v, 5 * pairStride) = v10;
            Unsafe.Add(ref v, (5 * pairStride) + 1) = v11;
            Unsafe.Add(ref v, 6 * pairStride) = v12;
            Unsafe.Add(ref v, (6 * pairStride) + 1) = v13;
            Unsafe.Add(ref v, 7 * pairStride) = v14;
            Unsafe.Add(ref v, (7 * pairStride) + 1) = v15;
        }

        /// <summary>
        /// Applies the first half of <c>GB</c> - the steps that rotate by 32 and by 24 - to four words held in locals.
        /// </summary>
        /// <param name="a">The <c>a</c> word.</param>
        /// <param name="b">The <c>b</c> word.</param>
        /// <param name="c">The <c>c</c> word.</param>
        /// <param name="d">The <c>d</c> word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GB1(ref ulong a, ref ulong b, ref ulong c, ref ulong d)
        {
            a = FBlaMka(a, b);
            d = (d ^ a).RotateBitsRightUnchecked(32);
            c = FBlaMka(c, d);
            b = (b ^ c).RotateBitsRightUnchecked(24);
        }

        /// <summary>
        /// Applies the second half of <c>GB</c> - the steps that rotate by 16 and by 63 - to four words held in locals.
        /// </summary>
        /// <param name="a">The <c>a</c> word.</param>
        /// <param name="b">The <c>b</c> word.</param>
        /// <param name="c">The <c>c</c> word.</param>
        /// <param name="d">The <c>d</c> word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GB2(ref ulong a, ref ulong b, ref ulong c, ref ulong d)
        {
            a = FBlaMka(a, b);
            d = (d ^ a).RotateBitsRightUnchecked(16);
            c = FBlaMka(c, d);
            b = (b ^ c).RotateBitsRightUnchecked(63);
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
