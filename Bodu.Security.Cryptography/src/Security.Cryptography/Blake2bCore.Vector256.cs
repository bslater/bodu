// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Vector256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    /// vector <c>G</c> mixes all four columns. Rotating the first row by three lanes and the third and fourth by one
    /// and two turns the diagonals into columns for the second half of a round, lane <c>i</c> holding the diagonal
    /// through word <c>i</c> of the second row, and rotating them back restores the rows. The second row stays in
    /// place: it is the row that each half of a round computes last and that the next half reads first, so no rotation
    /// stands between the two, and the latency of the lane rotations, AVX2's <c>VPERMQ</c> on every host that runs this
    /// kernel, overlaps the end of each half.
    /// </para>
    /// <para>
    /// Each round's message words are gathered straight from the block as the schedule σ names them, the diagonal
    /// half's in the order its lanes hold the diagonals.
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
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
        {
            Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
            Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
            Vector256<ulong> a = h0;
            Vector256<ulong> b = h1;
            Vector256<ulong> c = Vector256.Create(Iv0, Iv1, Iv2, Iv3);
            Vector256<ulong> d = Vector256.Create(Iv4 ^ counter, Iv5, Iv6 ^ finalization, Iv7);

            // A loop over the rounds keeps the method within the JIT's inlining budget on every supported runtime: with
            // every round written out, .NET 8 leaves the later rounds' Round and Load calls as calls, and the rows spill.
            ref byte sigma = ref MemoryMarshal.GetReference(Sigma);
            for (int round = 0; round < 12; round++)
            {
                ref byte s = ref Unsafe.Add(ref sigma, round * 16);

                Round(
                    ref a,
                    ref b,
                    ref c,
                    ref d,
                    Load(ref block, ref s, 0, 2, 4, 6),
                    Load(ref block, ref s, 1, 3, 5, 7),
                    Load(ref block, ref s, 14, 8, 10, 12),
                    Load(ref block, ref s, 15, 9, 11, 13));
            }

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

            a = Avx2.Permute4x64(a, 0b10_01_00_11);
            c = Avx2.Permute4x64(c, 0b00_11_10_01);
            d = Avx2.Permute4x64(d, 0b01_00_11_10);

            G(ref a, ref b, ref c, ref d, diagonalX, diagonalY);

            a = Avx2.Permute4x64(a, 0b00_11_10_01);
            c = Avx2.Permute4x64(c, 0b10_01_00_11);
            d = Avx2.Permute4x64(d, 0b01_00_11_10);
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
        /// <remarks>
        /// Each message word is added to <paramref name="a" /> before <paramref name="b" /> is. The row
        /// <paramref name="b" /> is the last that the step before computes, so adding it last leaves one addition, not
        /// two, waiting on it.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d, Vector256<ulong> x, Vector256<ulong> y)
        {
            a = a + x + b;
            d = TIsa.RotateRight32(d ^ a);
            c += d;
            b = TIsa.RotateRight24(b ^ c);
            a = a + y + b;
            d = TIsa.RotateRight16(d ^ a);
            c += d;
            b = TIsa.RotateRight63(b ^ c);
        }

        /// <summary>
        /// Gathers four message words of a block into one vector, little-endian, as a round's schedule names them.
        /// </summary>
        /// <param name="block">The first byte of the block.</param>
        /// <param name="schedule">The first of the round's sixteen σ entries.</param>
        /// <param name="k0">The σ entry naming the word for lane 0.</param>
        /// <param name="k1">The σ entry naming the word for lane 1.</param>
        /// <param name="k2">The σ entry naming the word for lane 2.</param>
        /// <param name="k3">The σ entry naming the word for lane 3.</param>
        /// <returns>The four words.</returns>
        /// <remarks>
        /// Each half is built a word at a time, so that each word's load folds into the <c>vmovq</c> or <c>vpinsrq</c>
        /// that places it. Built from four arguments at once, the JIT loaded three of the words into general registers,
        /// each with its own index arithmetic, and then moved them across to the vector registers.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Load(ref byte block, ref byte schedule, int k0, int k1, int k2, int k3) =>
            Vector256.Create(
                Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref schedule, k0))).WithElement(1, M(ref block, Unsafe.Add(ref schedule, k1))),
                Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref schedule, k2))).WithElement(1, M(ref block, Unsafe.Add(ref schedule, k3))));
    }
}
