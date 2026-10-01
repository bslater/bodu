// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.Vector128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2sCore
{
    /// <summary>
    /// The 128-bit implementation of the BLAKE2s compression function, written once over <see cref="Vector128{T}" />
    /// and specialized for an instruction set by <typeparamref name="TIsa" />.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set supplying the rotations of <c>G</c> and the lane rotations.
    /// </typeparam>
    /// <remarks>
    /// The working vector is held as four rows of four words, so lane <c>i</c> of the rows is column <c>i</c> and one
    /// vector <c>G</c> mixes all four columns. Rotating the second, third and fourth rows by one, two and three lanes
    /// turns the diagonals into columns for the second half of a round, and rotating them back restores the rows. Each
    /// round's message words are gathered straight from the block as the schedule σ names them.
    /// </remarks>
    internal readonly struct Vector128Kernel<TIsa>
        where TIsa : struct, IVector128Isa
    {
        /// <summary>
        /// Compresses one block into the chaining state.
        /// </summary>
        /// <param name="h">The first of the eight chaining-state words, updated in place.</param>
        /// <param name="block">The first byte of the 64-byte block.</param>
        /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
        /// <param name="finalization">All ones for the final block; otherwise zero.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
        {
            Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
            Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
            Vector128<uint> a = h0;
            Vector128<uint> b = h1;
            Vector128<uint> c = Vector128.Create(Iv0, Iv1, Iv2, Iv3);
            Vector128<uint> d = Vector128.Create(Iv4 ^ (uint)counter, Iv5 ^ (uint)(counter >> 32), Iv6 ^ finalization, Iv7);

            // A loop over the rounds keeps the method within the JIT's inlining budget: with every round written out, the
            // later rounds' Round and Load calls stay calls, and the rows spill.
            ref byte sigma = ref MemoryMarshal.GetReference(Sigma);
            for (int round = 0; round < 10; round++)
            {
                ref byte s = ref Unsafe.Add(ref sigma, round * 16);

                Round(
                    ref a,
                    ref b,
                    ref c,
                    ref d,
                    Load(ref block, ref s, 0, 2, 4, 6),
                    Load(ref block, ref s, 1, 3, 5, 7),
                    Load(ref block, ref s, 8, 10, 12, 14),
                    Load(ref block, ref s, 9, 11, 13, 15));
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
            ref Vector128<uint> a,
            ref Vector128<uint> b,
            ref Vector128<uint> c,
            ref Vector128<uint> d,
            Vector128<uint> columnX,
            Vector128<uint> columnY,
            Vector128<uint> diagonalX,
            Vector128<uint> diagonalY)
        {
            G(ref a, ref b, ref c, ref d, columnX, columnY);

            b = TIsa.RotateLanes1(b);
            c = TIsa.RotateLanes2(c);
            d = TIsa.RotateLanes3(d);

            G(ref a, ref b, ref c, ref d, diagonalX, diagonalY);

            b = TIsa.RotateLanes3(b);
            c = TIsa.RotateLanes2(c);
            d = TIsa.RotateLanes1(d);
        }

        /// <summary>
        /// The BLAKE2s mixing function <c>G</c>, applied to four columns (or diagonals) at once.
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
        private static void G(ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d, Vector128<uint> x, Vector128<uint> y)
        {
            a = a + x + b;
            d = TIsa.RotateRight16(d ^ a);
            c += d;
            b = TIsa.RotateRight12(b ^ c);
            a = a + y + b;
            d = TIsa.RotateRight8(d ^ a);
            c += d;
            b = TIsa.RotateRight7(b ^ c);
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
        /// Where SSE4.1 is available the vector is built a word at a time, so that each word's load folds into the
        /// <c>vmovd</c> or <c>vpinsrd</c> that places it. Built from four arguments at once, the JIT loaded the first
        /// three words into general registers, each with its own index arithmetic, and then moved them across to the
        /// vector registers.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<uint> Load(ref byte block, ref byte schedule, int k0, int k1, int k2, int k3)
        {
            if (Sse41.IsSupported)
            {
                return Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref schedule, k0)))
                    .WithElement(1, M(ref block, Unsafe.Add(ref schedule, k1)))
                    .WithElement(2, M(ref block, Unsafe.Add(ref schedule, k2)))
                    .WithElement(3, M(ref block, Unsafe.Add(ref schedule, k3)));
            }

            return Vector128.Create(
                M(ref block, Unsafe.Add(ref schedule, k0)),
                M(ref block, Unsafe.Add(ref schedule, k1)),
                M(ref block, Unsafe.Add(ref schedule, k2)),
                M(ref block, Unsafe.Add(ref schedule, k3)));
        }
    }
}
