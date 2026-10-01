// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Vector128.cs" company="Bodu Pty. Ltd.">
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
    /// The 128-bit implementation of the BLAKE2b compression function, written once over <see cref="Vector128{T}" />
    /// and specialized for an instruction set by <typeparamref name="TIsa" /> - the shim Argon2's 128-bit kernel uses.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set supplying the rotations by 32, 24 and 16 bits, and the splice.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Each row of the working vector is held as two vectors of two words, so each <c>G</c> call mixes two columns at
    /// once and a round's four columns take two. Turning the diagonals into columns rotates the second row by one word
    /// and the fourth by three, which splice the halves with <see cref="Argon2Core.IVector128Isa.UpperThenLower" />,
    /// and the third by two, which only swaps them.
    /// </para>
    /// <para>
    /// This kernel serves x64 processors with SSSE3 but not AVX2, and ARM64. Additions, XORs and the rotation by 63
    /// bits are portable <see cref="Vector128" /> arithmetic; the shim's multiply, which Argon2 needs, is unused.
    /// </para>
    /// </remarks>
    internal readonly struct Vector128Kernel<TIsa>
        where TIsa : struct, Argon2Core.IVector128Isa
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
            Vector128<ulong> h0 = Vector128.LoadUnsafe(ref h);
            Vector128<ulong> h1 = Vector128.LoadUnsafe(ref h, 2);
            Vector128<ulong> h2 = Vector128.LoadUnsafe(ref h, 4);
            Vector128<ulong> h3 = Vector128.LoadUnsafe(ref h, 6);
            Vector128<ulong> a0 = h0;
            Vector128<ulong> a1 = h1;
            Vector128<ulong> b0 = h2;
            Vector128<ulong> b1 = h3;
            Vector128<ulong> c0 = Vector128.Create(Iv0, Iv1);
            Vector128<ulong> c1 = Vector128.Create(Iv2, Iv3);
            Vector128<ulong> d0 = Vector128.Create(Iv4 ^ counter, Iv5);
            Vector128<ulong> d1 = Vector128.Create(Iv6 ^ finalization, Iv7);

            // A loop over the rounds keeps the method within the JIT's inlining budget: with every round written out, the
            // last rounds' G calls stay calls, and the rows spill.
            ref byte sigma = ref MemoryMarshal.GetReference(Sigma);
            for (int round = 0; round < 12; round++)
            {
                ref byte s = ref Unsafe.Add(ref sigma, round * 16);

                G(ref a0, ref b0, ref c0, ref d0, Load(ref block, ref s, 0, 2), Load(ref block, ref s, 1, 3));
                G(ref a1, ref b1, ref c1, ref d1, Load(ref block, ref s, 4, 6), Load(ref block, ref s, 5, 7));
                Diagonalize(ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);
                G(ref a0, ref b0, ref c0, ref d0, Load(ref block, ref s, 8, 10), Load(ref block, ref s, 9, 11));
                G(ref a1, ref b1, ref c1, ref d1, Load(ref block, ref s, 12, 14), Load(ref block, ref s, 13, 15));
                Undiagonalize(ref b0, ref b1, ref c0, ref c1, ref d0, ref d1);
            }

            (h0 ^ a0 ^ c0).StoreUnsafe(ref h);
            (h1 ^ a1 ^ c1).StoreUnsafe(ref h, 2);
            (h2 ^ b0 ^ d0).StoreUnsafe(ref h, 4);
            (h3 ^ b1 ^ d1).StoreUnsafe(ref h, 6);
        }

        /// <summary>
        /// The BLAKE2b mixing function <c>G</c>, applied to two columns (or diagonals) at once.
        /// </summary>
        /// <param name="a">Two words of the first row.</param>
        /// <param name="b">Two words of the second row.</param>
        /// <param name="c">Two words of the third row.</param>
        /// <param name="d">Two words of the fourth row.</param>
        /// <param name="x">The first message word of each <c>G</c>.</param>
        /// <param name="y">The second message word of each <c>G</c>.</param>
        /// <remarks>
        /// Each message word is added to <paramref name="a" /> before <paramref name="b" /> is. The row
        /// <paramref name="b" /> is the last that the step before computes, so adding it last leaves one addition, not
        /// two, waiting on it.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(ref Vector128<ulong> a, ref Vector128<ulong> b, ref Vector128<ulong> c, ref Vector128<ulong> d, Vector128<ulong> x, Vector128<ulong> y)
        {
            a = a + x + b;
            d = TIsa.RotateRight32(d ^ a);
            c += d;
            b = TIsa.RotateRight24(b ^ c);
            a = a + y + b;
            d = TIsa.RotateRight16(d ^ a);
            c += d;
            b = b ^ c;
            b = Vector128.ShiftRightLogical(b, 63) ^ (b + b);
        }

        /// <summary>
        /// Rotates the second row one word left, the third two, and the fourth three, so the diagonals become columns.
        /// </summary>
        /// <param name="b0">The first half of the second row.</param>
        /// <param name="b1">The second half of the second row.</param>
        /// <param name="c0">The first half of the third row.</param>
        /// <param name="c1">The second half of the third row.</param>
        /// <param name="d0">The first half of the fourth row.</param>
        /// <param name="d1">The second half of the fourth row.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Diagonalize(
            ref Vector128<ulong> b0,
            ref Vector128<ulong> b1,
            ref Vector128<ulong> c0,
            ref Vector128<ulong> c1,
            ref Vector128<ulong> d0,
            ref Vector128<ulong> d1)
        {
            Vector128<ulong> t = TIsa.UpperThenLower(b0, b1);
            b1 = TIsa.UpperThenLower(b1, b0);
            b0 = t;

            (c0, c1) = (c1, c0);

            t = TIsa.UpperThenLower(d1, d0);
            d1 = TIsa.UpperThenLower(d0, d1);
            d0 = t;
        }

        /// <summary>
        /// Undoes <see cref="Diagonalize" />, restoring the rows.
        /// </summary>
        /// <param name="b0">The first half of the second row.</param>
        /// <param name="b1">The second half of the second row.</param>
        /// <param name="c0">The first half of the third row.</param>
        /// <param name="c1">The second half of the third row.</param>
        /// <param name="d0">The first half of the fourth row.</param>
        /// <param name="d1">The second half of the fourth row.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Undiagonalize(
            ref Vector128<ulong> b0,
            ref Vector128<ulong> b1,
            ref Vector128<ulong> c0,
            ref Vector128<ulong> c1,
            ref Vector128<ulong> d0,
            ref Vector128<ulong> d1)
        {
            Vector128<ulong> t = TIsa.UpperThenLower(b1, b0);
            b1 = TIsa.UpperThenLower(b0, b1);
            b0 = t;

            (c0, c1) = (c1, c0);

            t = TIsa.UpperThenLower(d0, d1);
            d1 = TIsa.UpperThenLower(d1, d0);
            d0 = t;
        }

        /// <summary>
        /// Gathers two message words of a block into one vector, little-endian, as a round's schedule names them.
        /// </summary>
        /// <param name="block">The first byte of the block.</param>
        /// <param name="schedule">The first of the round's sixteen σ entries.</param>
        /// <param name="k0">The σ entry naming the word for lane 0.</param>
        /// <param name="k1">The σ entry naming the word for lane 1.</param>
        /// <returns>The two words.</returns>
        /// <remarks>
        /// Where SSE4.1 is available the vector is built a word at a time, so that each word's load folds into the
        /// <c>vmovq</c> or <c>vpinsrq</c> that places it, rather than passing through a general register.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> Load(ref byte block, ref byte schedule, int k0, int k1)
        {
            if (Sse41.IsSupported)
                return Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref schedule, k0))).WithElement(1, M(ref block, Unsafe.Add(ref schedule, k1)));

            return Vector128.Create(M(ref block, Unsafe.Add(ref schedule, k0)), M(ref block, Unsafe.Add(ref schedule, k1)));
        }
    }
}
