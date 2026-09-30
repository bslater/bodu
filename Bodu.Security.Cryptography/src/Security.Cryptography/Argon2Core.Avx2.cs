// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Avx2.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// The AVX2 implementation of the compression function, in the shape of the reference implementation's
    /// <c>blamka-round-opt.h</c>: each 256-bit register holds four words, and each round works on two rows or two
    /// columns of the block at once.
    /// </summary>
    /// <remarks>
    /// The multiply is <c>VPMULUDQ</c>, which takes the low 32 bits of each word, exactly the truncation BlaMka calls
    /// for. Rotating by 32 is <c>VPSHUFD</c>, by 24 and 16 a <c>VPSHUFB</c> over constant indices, and by 63 an add, a
    /// shift, and an XOR. Rows and columns are brought into diagonal position with <c>VPERMQ</c> and <c>VPBLENDD</c>.
    /// Nothing branches on, or is indexed by, the block's contents.
    /// </remarks>
    internal readonly struct Avx2Kernel
        : IArgon2Kernel
    {
        /// <summary>The number of 256-bit vectors in a block.</summary>
        private const int VectorsPerBlock = WordsPerBlock / 4;

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
            ref Vector256<ulong> s = ref Unsafe.As<ulong, Vector256<ulong>>(ref state);
            ref Vector256<ulong> xy = ref Unsafe.As<ulong, Vector256<ulong>>(ref scratch);

            if (withXor)
            {
                for (int i = 0; i < VectorsPerBlock; i++)
                {
                    Vector256<ulong> r = Unsafe.Add(ref s, i) ^ Vector256.LoadUnsafe(ref reference, (nuint)(i * 4));
                    Unsafe.Add(ref s, i) = r;
                    Unsafe.Add(ref xy, i) = r ^ Vector256.LoadUnsafe(ref next, (nuint)(i * 4));
                }
            }
            else
            {
                for (int i = 0; i < VectorsPerBlock; i++)
                {
                    Vector256<ulong> r = Unsafe.Add(ref s, i) ^ Vector256.LoadUnsafe(ref reference, (nuint)(i * 4));
                    Unsafe.Add(ref s, i) = r;
                    Unsafe.Add(ref xy, i) = r;
                }
            }

            for (int i = 0; i < 4; i++)
                RoundRows(ref Unsafe.Add(ref s, 8 * i));

            for (int i = 0; i < 4; i++)
                RoundColumns(ref Unsafe.Add(ref s, i));

            for (int i = 0; i < VectorsPerBlock; i++)
            {
                Vector256<ulong> value = Unsafe.Add(ref s, i) ^ Unsafe.Add(ref xy, i);
                Unsafe.Add(ref s, i) = value;
                value.StoreUnsafe(ref next, (nuint)(i * 4));
            }
        }

        /// <summary>
        /// Applies one BLAKE2b round to each of two consecutive rows: the eight vectors from <paramref name="v" /> (<c>BLAKE2_ROUND_1</c>).
        /// </summary>
        /// <param name="v">The first vector of the two rows.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RoundRows(ref Vector256<ulong> v)
        {
            Vector256<ulong> a0 = v;
            Vector256<ulong> a1 = Unsafe.Add(ref v, 4);
            Vector256<ulong> b0 = Unsafe.Add(ref v, 1);
            Vector256<ulong> b1 = Unsafe.Add(ref v, 5);
            Vector256<ulong> c0 = Unsafe.Add(ref v, 2);
            Vector256<ulong> c1 = Unsafe.Add(ref v, 6);
            Vector256<ulong> d0 = Unsafe.Add(ref v, 3);
            Vector256<ulong> d1 = Unsafe.Add(ref v, 7);

            G1(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);
            G2(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);

            // Rotate b, c, and d left by one, two, and three words, so the diagonals line up as columns.
            b0 = Avx2.Permute4x64(b0, 0x39);
            c0 = Avx2.Permute4x64(c0, 0x4E);
            d0 = Avx2.Permute4x64(d0, 0x93);
            b1 = Avx2.Permute4x64(b1, 0x39);
            c1 = Avx2.Permute4x64(c1, 0x4E);
            d1 = Avx2.Permute4x64(d1, 0x93);

            G1(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);
            G2(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);

            b0 = Avx2.Permute4x64(b0, 0x93);
            c0 = Avx2.Permute4x64(c0, 0x4E);
            d0 = Avx2.Permute4x64(d0, 0x39);
            b1 = Avx2.Permute4x64(b1, 0x93);
            c1 = Avx2.Permute4x64(c1, 0x4E);
            d1 = Avx2.Permute4x64(d1, 0x39);

            v = a0;
            Unsafe.Add(ref v, 4) = a1;
            Unsafe.Add(ref v, 1) = b0;
            Unsafe.Add(ref v, 5) = b1;
            Unsafe.Add(ref v, 2) = c0;
            Unsafe.Add(ref v, 6) = c1;
            Unsafe.Add(ref v, 3) = d0;
            Unsafe.Add(ref v, 7) = d1;
        }

        /// <summary>
        /// Applies one BLAKE2b round to each of two columns: the vectors at <paramref name="v" /> and every fourth
        /// vector after it (<c>BLAKE2_ROUND_2</c>).
        /// </summary>
        /// <param name="v">The first vector of the two columns.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RoundColumns(ref Vector256<ulong> v)
        {
            Vector256<ulong> a0 = v;
            Vector256<ulong> a1 = Unsafe.Add(ref v, 4);
            Vector256<ulong> b0 = Unsafe.Add(ref v, 8);
            Vector256<ulong> b1 = Unsafe.Add(ref v, 12);
            Vector256<ulong> c0 = Unsafe.Add(ref v, 16);
            Vector256<ulong> c1 = Unsafe.Add(ref v, 20);
            Vector256<ulong> d0 = Unsafe.Add(ref v, 24);
            Vector256<ulong> d1 = Unsafe.Add(ref v, 28);

            G1(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);
            G2(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);

            // A column's words sit two to a register across a pair of registers, so its diagonals are formed by
            // blending the pair's halves and swapping each half's words.
            Vector256<ulong> t1 = Avx2.Blend(b0.AsUInt32(), b1.AsUInt32(), 0xCC).AsUInt64();
            Vector256<ulong> t2 = Avx2.Blend(b0.AsUInt32(), b1.AsUInt32(), 0x33).AsUInt64();
            b1 = Avx2.Permute4x64(t1, 0xB1);
            b0 = Avx2.Permute4x64(t2, 0xB1);
            (c0, c1) = (c1, c0);
            t1 = Avx2.Blend(d0.AsUInt32(), d1.AsUInt32(), 0xCC).AsUInt64();
            t2 = Avx2.Blend(d0.AsUInt32(), d1.AsUInt32(), 0x33).AsUInt64();
            d0 = Avx2.Permute4x64(t1, 0xB1);
            d1 = Avx2.Permute4x64(t2, 0xB1);

            G1(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);
            G2(ref a0, ref a1, ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);

            t1 = Avx2.Blend(b0.AsUInt32(), b1.AsUInt32(), 0xCC).AsUInt64();
            t2 = Avx2.Blend(b0.AsUInt32(), b1.AsUInt32(), 0x33).AsUInt64();
            b0 = Avx2.Permute4x64(t1, 0xB1);
            b1 = Avx2.Permute4x64(t2, 0xB1);
            (c0, c1) = (c1, c0);
            t1 = Avx2.Blend(d0.AsUInt32(), d1.AsUInt32(), 0x33).AsUInt64();
            t2 = Avx2.Blend(d0.AsUInt32(), d1.AsUInt32(), 0xCC).AsUInt64();
            d0 = Avx2.Permute4x64(t1, 0xB1);
            d1 = Avx2.Permute4x64(t2, 0xB1);

            v = a0;
            Unsafe.Add(ref v, 4) = a1;
            Unsafe.Add(ref v, 8) = b0;
            Unsafe.Add(ref v, 12) = b1;
            Unsafe.Add(ref v, 16) = c0;
            Unsafe.Add(ref v, 20) = c1;
            Unsafe.Add(ref v, 24) = d0;
            Unsafe.Add(ref v, 28) = d1;
        }

        /// <summary>
        /// Applies the first half of <c>GB</c> - the steps that rotate by 32 and by 24 - to two sets of four words.
        /// </summary>
        /// <param name="a0">The first set's <c>a</c> words.</param>
        /// <param name="a1">The second set's <c>a</c> words.</param>
        /// <param name="b0">The first set's <c>b</c> words.</param>
        /// <param name="b1">The second set's <c>b</c> words.</param>
        /// <param name="c0">The first set's <c>c</c> words.</param>
        /// <param name="c1">The second set's <c>c</c> words.</param>
        /// <param name="d0">The first set's <c>d</c> words.</param>
        /// <param name="d1">The second set's <c>d</c> words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G1(
            ref Vector256<ulong> a0,
            ref Vector256<ulong> a1,
            ref Vector256<ulong> b0,
            ref Vector256<ulong> b1,
            ref Vector256<ulong> c0,
            ref Vector256<ulong> c1,
            ref Vector256<ulong> d0,
            ref Vector256<ulong> d1)
        {
            a0 = BlaMka(a0, b0);
            d0 = RotateRight32(d0 ^ a0);
            c0 = BlaMka(c0, d0);
            b0 = RotateRight24(b0 ^ c0);

            a1 = BlaMka(a1, b1);
            d1 = RotateRight32(d1 ^ a1);
            c1 = BlaMka(c1, d1);
            b1 = RotateRight24(b1 ^ c1);
        }

        /// <summary>
        /// Applies the second half of <c>GB</c> - the steps that rotate by 16 and by 63 - to two sets of four words.
        /// </summary>
        /// <param name="a0">The first set's <c>a</c> words.</param>
        /// <param name="a1">The second set's <c>a</c> words.</param>
        /// <param name="b0">The first set's <c>b</c> words.</param>
        /// <param name="b1">The second set's <c>b</c> words.</param>
        /// <param name="c0">The first set's <c>c</c> words.</param>
        /// <param name="c1">The second set's <c>c</c> words.</param>
        /// <param name="d0">The first set's <c>d</c> words.</param>
        /// <param name="d1">The second set's <c>d</c> words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G2(
            ref Vector256<ulong> a0,
            ref Vector256<ulong> a1,
            ref Vector256<ulong> b0,
            ref Vector256<ulong> b1,
            ref Vector256<ulong> c0,
            ref Vector256<ulong> c1,
            ref Vector256<ulong> d0,
            ref Vector256<ulong> d1)
        {
            a0 = BlaMka(a0, b0);
            d0 = RotateRight16(d0 ^ a0);
            c0 = BlaMka(c0, d0);
            b0 = RotateRight63(b0 ^ c0);

            a1 = BlaMka(a1, b1);
            d1 = RotateRight16(d1 ^ a1);
            c1 = BlaMka(c1, d1);
            b1 = RotateRight63(b1 ^ c1);
        }

        /// <summary>
        /// Computes <c>x + y + 2 * trunc(x) * trunc(y)</c> in each of four words.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>The BlaMka combination of each pair of words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> BlaMka(Vector256<ulong> x, Vector256<ulong> y)
        {
            Vector256<ulong> product = Avx2.Multiply(x.AsUInt32(), y.AsUInt32());
            return x + y + product + product;
        }

        /// <summary>
        /// Rotates each word right by 32 bits, by swapping its halves.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> RotateRight32(Vector256<ulong> x) =>
            Avx2.Shuffle(x.AsUInt32(), 0xB1).AsUInt64();

        /// <summary>
        /// Rotates each word right by 24 bits, with a byte shuffle over constant indices.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> RotateRight24(Vector256<ulong> x) =>
            Avx2.Shuffle(
                x.AsByte(),
                Vector256.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10, 3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10))
            .AsUInt64();

        /// <summary>
        /// Rotates each word right by 16 bits, with a byte shuffle over constant indices.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> RotateRight16(Vector256<ulong> x) =>
            Avx2.Shuffle(
                x.AsByte(),
                Vector256.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9, 2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9))
            .AsUInt64();

        /// <summary>
        /// Rotates each word right by 63 bits: left by one, as the sum of the word with itself, with the top bit
        /// brought round.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> RotateRight63(Vector256<ulong> x) =>
            Avx2.ShiftRightLogical(x, 63) ^ (x + x);
    }
}
