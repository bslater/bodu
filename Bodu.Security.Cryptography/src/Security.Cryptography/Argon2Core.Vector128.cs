// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Vector128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// The 128-bit implementation of the compression function, in the shape of the reference implementation's SSSE3
    /// <c>blamka-round-opt.h</c>: each register holds two words, and each round works on one row or one column of the
    /// block as eight registers.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set supplying the operations with no portable form.</typeparam>
    /// <remarks>
    /// Additions, XORs, and the rotation by 63 are portable <see cref="Vector128" /> arithmetic; the multiply, the
    /// other three rotations, and the word exchange that forms the diagonals come from <typeparamref name="TIsa" />.
    /// Nothing branches on, or is indexed by, the block's contents.
    /// </remarks>
    internal readonly struct Vector128Kernel<TIsa>
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

            // A row is eight consecutive registers; a column is one register from each row.
            for (int i = 0; i < 8; i++)
                Round(ref Unsafe.Add(ref s, 8 * i), 1);

            for (int i = 0; i < 8; i++)
                Round(ref Unsafe.Add(ref s, i), 8);

            for (int i = 0; i < VectorsPerBlock; i++)
            {
                Vector128<ulong> value = Unsafe.Add(ref s, i) ^ Unsafe.Add(ref xy, i);
                Unsafe.Add(ref s, i) = value;
                value.StoreUnsafe(ref next, (nuint)(i * 2));
            }
        }

        /// <summary>
        /// Applies one BLAKE2b round (<c>BLAKE2_ROUND</c>) to the sixteen words held in the eight registers from
        /// <paramref name="v" />, <paramref name="stride" /> registers apart.
        /// </summary>
        /// <param name="v">The first register of the row or column.</param>
        /// <param name="stride">The distance, in registers, between the row's or column's registers.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Round(ref Vector128<ulong> v, int stride)
        {
            Vector128<ulong> a0 = v;
            Vector128<ulong> a1 = Unsafe.Add(ref v, stride);
            Vector128<ulong> b0 = Unsafe.Add(ref v, 2 * stride);
            Vector128<ulong> b1 = Unsafe.Add(ref v, 3 * stride);
            Vector128<ulong> c0 = Unsafe.Add(ref v, 4 * stride);
            Vector128<ulong> c1 = Unsafe.Add(ref v, 5 * stride);
            Vector128<ulong> d0 = Unsafe.Add(ref v, 6 * stride);
            Vector128<ulong> d1 = Unsafe.Add(ref v, 7 * stride);

            G1(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);
            G2(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);

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

            G1(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);
            G2(ref a0, ref b0, ref c0, ref d0, ref a1, ref b1, ref c1, ref d1);

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
        }

        /// <summary>
        /// Applies the first half of <c>GB</c> - the steps that rotate by 32 and by 24 - to two pairs of four words.
        /// </summary>
        /// <param name="a0">The first pair's <c>a</c> words.</param>
        /// <param name="b0">The first pair's <c>b</c> words.</param>
        /// <param name="c0">The first pair's <c>c</c> words.</param>
        /// <param name="d0">The first pair's <c>d</c> words.</param>
        /// <param name="a1">The second pair's <c>a</c> words.</param>
        /// <param name="b1">The second pair's <c>b</c> words.</param>
        /// <param name="c1">The second pair's <c>c</c> words.</param>
        /// <param name="d1">The second pair's <c>d</c> words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G1(
            ref Vector128<ulong> a0,
            ref Vector128<ulong> b0,
            ref Vector128<ulong> c0,
            ref Vector128<ulong> d0,
            ref Vector128<ulong> a1,
            ref Vector128<ulong> b1,
            ref Vector128<ulong> c1,
            ref Vector128<ulong> d1)
        {
            a0 = BlaMka(a0, b0);
            a1 = BlaMka(a1, b1);
            d0 = TIsa.RotateRight32(d0 ^ a0);
            d1 = TIsa.RotateRight32(d1 ^ a1);
            c0 = BlaMka(c0, d0);
            c1 = BlaMka(c1, d1);
            b0 = TIsa.RotateRight24(b0 ^ c0);
            b1 = TIsa.RotateRight24(b1 ^ c1);
        }

        /// <summary>
        /// Applies the second half of <c>GB</c> - the steps that rotate by 16 and by 63 - to two pairs of four words.
        /// </summary>
        /// <param name="a0">The first pair's <c>a</c> words.</param>
        /// <param name="b0">The first pair's <c>b</c> words.</param>
        /// <param name="c0">The first pair's <c>c</c> words.</param>
        /// <param name="d0">The first pair's <c>d</c> words.</param>
        /// <param name="a1">The second pair's <c>a</c> words.</param>
        /// <param name="b1">The second pair's <c>b</c> words.</param>
        /// <param name="c1">The second pair's <c>c</c> words.</param>
        /// <param name="d1">The second pair's <c>d</c> words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G2(
            ref Vector128<ulong> a0,
            ref Vector128<ulong> b0,
            ref Vector128<ulong> c0,
            ref Vector128<ulong> d0,
            ref Vector128<ulong> a1,
            ref Vector128<ulong> b1,
            ref Vector128<ulong> c1,
            ref Vector128<ulong> d1)
        {
            a0 = BlaMka(a0, b0);
            a1 = BlaMka(a1, b1);
            d0 = TIsa.RotateRight16(d0 ^ a0);
            d1 = TIsa.RotateRight16(d1 ^ a1);
            c0 = BlaMka(c0, d0);
            c1 = BlaMka(c1, d1);
            b0 = RotateRight63(b0 ^ c0);
            b1 = RotateRight63(b1 ^ c1);
        }

        /// <summary>
        /// Computes <c>x + y + 2 * trunc(x) * trunc(y)</c> in each of two words.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The BlaMka combination of each pair of words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> BlaMka(Vector128<ulong> x, Vector128<ulong> y)
        {
            Vector128<ulong> product = TIsa.MultiplyLow(x, y);
            return x + y + product + product;
        }

        /// <summary>
        /// Rotates each word right by 63 bits: left by one, as the sum of the word with itself, with the top bit
        /// brought round.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> RotateRight63(Vector128<ulong> x) =>
            Vector128.ShiftRightLogical(x, 63) ^ (x + x);
    }
}
