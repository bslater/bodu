// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Vector256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2bCore
{
    /// <summary>
    /// The 256-bit implementation of the BLAKE2b compression function, written once over <see cref="Vector256{T}" />
    /// and specialized for an instruction set by <typeparamref name="TIsa" />.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set supplying the four rotations of <c>G</c>.</typeparam>
    /// <remarks>
    /// <para>
    /// The working vector is held as four rows of four words, so lane <c>i</c> of the rows is column <c>i</c> and one
    /// vector <c>G</c> mixes all four columns. Rotating the second, third and fourth rows by one, two and three lanes
    /// turns the diagonals into columns for the second half of a round, and rotating them back restores the rows.
    /// </para>
    /// <para>
    /// Each round's message words are gathered with the schedule σ resolved to constant indices, straight from the
    /// block. The lane rotations are AVX2's <c>VPERMQ</c> on every host that runs this kernel.
    /// </para>
    /// </remarks>
    internal readonly struct Vector256Kernel<TIsa>
        where TIsa : struct, IVector256Isa
    {
        /// <summary>
        /// Compresses one block into the chaining state.
        /// </summary>
        /// <param name="h">The first of the eight chaining-state words, updated in place.</param>
        /// <param name="block">The first byte of the 128-byte block.</param>
        /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
        /// <param name="finalization">All ones for the final block; otherwise zero.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
        {
            Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
            Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
            Vector256<ulong> a = h0;
            Vector256<ulong> b = h1;
            Vector256<ulong> c = Vector256.Create(Iv0, Iv1, Iv2, Iv3);
            Vector256<ulong> d = Vector256.Create(Iv4 ^ counter, Iv5, Iv6 ^ finalization, Iv7);

            // Round 0: σ0.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 0, 2, 4, 6), Load(ref block, 1, 3, 5, 7), Load(ref block, 8, 10, 12, 14), Load(ref block, 9, 11, 13, 15));

            // Round 1: σ1.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 14, 4, 9, 13), Load(ref block, 10, 8, 15, 6), Load(ref block, 1, 0, 11, 5), Load(ref block, 12, 2, 7, 3));

            // Round 2: σ2.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 11, 12, 5, 15), Load(ref block, 8, 0, 2, 13), Load(ref block, 10, 3, 7, 9), Load(ref block, 14, 6, 1, 4));

            // Round 3: σ3.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 7, 3, 13, 11), Load(ref block, 9, 1, 12, 14), Load(ref block, 2, 5, 4, 15), Load(ref block, 6, 10, 0, 8));

            // Round 4: σ4.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 9, 5, 2, 10), Load(ref block, 0, 7, 4, 15), Load(ref block, 14, 11, 6, 3), Load(ref block, 1, 12, 8, 13));

            // Round 5: σ5.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 2, 6, 0, 8), Load(ref block, 12, 10, 11, 3), Load(ref block, 4, 7, 15, 1), Load(ref block, 13, 5, 14, 9));

            // Round 6: σ6.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 12, 1, 14, 4), Load(ref block, 5, 15, 13, 10), Load(ref block, 0, 6, 9, 8), Load(ref block, 7, 3, 2, 11));

            // Round 7: σ7.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 13, 7, 12, 3), Load(ref block, 11, 14, 1, 9), Load(ref block, 5, 15, 8, 2), Load(ref block, 0, 4, 6, 10));

            // Round 8: σ8.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 6, 14, 11, 0), Load(ref block, 15, 9, 3, 8), Load(ref block, 12, 13, 1, 10), Load(ref block, 2, 7, 4, 5));

            // Round 9: σ9.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 10, 8, 7, 1), Load(ref block, 2, 4, 6, 5), Load(ref block, 15, 9, 3, 13), Load(ref block, 11, 14, 12, 0));

            // Round 10: σ0.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 0, 2, 4, 6), Load(ref block, 1, 3, 5, 7), Load(ref block, 8, 10, 12, 14), Load(ref block, 9, 11, 13, 15));

            // Round 11: σ1.
            Round(ref a, ref b, ref c, ref d, Load(ref block, 14, 4, 9, 13), Load(ref block, 10, 8, 15, 6), Load(ref block, 1, 0, 11, 5), Load(ref block, 12, 2, 7, 3));

            (h0 ^ a ^ c).StoreUnsafe(ref h);
            (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
        }

        /// <summary>
        /// Applies one round: <c>G</c> over the four columns, then over the four diagonals.
        /// </summary>
        /// <param name="a">The first row.</param>
        /// <param name="b">The second row.</param>
        /// <param name="c">The third row.</param>
        /// <param name="d">The fourth row.</param>
        /// <param name="columnX">The first message word of each column's <c>G</c>.</param>
        /// <param name="columnY">The second message word of each column's <c>G</c>.</param>
        /// <param name="diagonalX">The first message word of each diagonal's <c>G</c>.</param>
        /// <param name="diagonalY">The second message word of each diagonal's <c>G</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Round(
            ref Vector256<ulong> a,
            ref Vector256<ulong> b,
            ref Vector256<ulong> c,
            ref Vector256<ulong> d,
            Vector256<ulong> columnX,
            Vector256<ulong> columnY,
            Vector256<ulong> diagonalX,
            Vector256<ulong> diagonalY)
        {
            G(ref a, ref b, ref c, ref d, columnX, columnY);

            b = Avx2.Permute4x64(b, 0b00_11_10_01);
            c = Avx2.Permute4x64(c, 0b01_00_11_10);
            d = Avx2.Permute4x64(d, 0b10_01_00_11);

            G(ref a, ref b, ref c, ref d, diagonalX, diagonalY);

            b = Avx2.Permute4x64(b, 0b10_01_00_11);
            c = Avx2.Permute4x64(c, 0b01_00_11_10);
            d = Avx2.Permute4x64(d, 0b00_11_10_01);
        }

        /// <summary>
        /// The BLAKE2b mixing function <c>G</c>, applied to four columns (or diagonals) at once.
        /// </summary>
        /// <param name="a">The first row.</param>
        /// <param name="b">The second row.</param>
        /// <param name="c">The third row.</param>
        /// <param name="d">The fourth row.</param>
        /// <param name="x">The first message word of each <c>G</c>.</param>
        /// <param name="y">The second message word of each <c>G</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d, Vector256<ulong> x, Vector256<ulong> y)
        {
            a += b + x;
            d = TIsa.RotateRight32(d ^ a);
            c += d;
            b = TIsa.RotateRight24(b ^ c);
            a += b + y;
            d = TIsa.RotateRight16(d ^ a);
            c += d;
            b = TIsa.RotateRight63(b ^ c);
        }

        /// <summary>
        /// Gathers four message words of a block into one vector, little-endian.
        /// </summary>
        /// <param name="block">The first byte of the block.</param>
        /// <param name="i0">The index of the word for lane 0.</param>
        /// <param name="i1">The index of the word for lane 1.</param>
        /// <param name="i2">The index of the word for lane 2.</param>
        /// <param name="i3">The index of the word for lane 3.</param>
        /// <returns>The four words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Load(ref byte block, int i0, int i1, int i2, int i3) =>
            Vector256.Create(M(ref block, i0), M(ref block, i1), M(ref block, i2), M(ref block, i3));
    }
}
