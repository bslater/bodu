// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.Vector128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// The 128-bit implementation of <c>scryptBlockMix</c>, written once over <see cref="Vector128{T}" /> and
    /// specialized for an instruction set by <typeparamref name="TIsa" />.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set supplying the lane rotations.</typeparam>
    /// <remarks>
    /// <para>
    /// The kernel keeps each block in the diagonal order that suits four-lane vectors: word <c>5i mod 16</c> sits at
    /// position <c>i</c>, so the block's four vectors are the diagonals <c>(0, 5, 10, 15)</c>, <c>(4, 9, 14, 3)</c>,
    /// <c>(8, 13, 2, 7)</c> and <c>(12, 1, 6, 11)</c>. In that order the four quarter-rounds of a column round are the
    /// four lanes of one vector operation; rotating three of the vectors by one, two and three lanes lines up the row
    /// round the same way, and rotating them back restores the order.
    /// </para>
    /// <para>
    /// BlockMix carries the chaining value in four registers across the 2·r blocks, so each block costs four loads,
    /// four stores and the core. Rotations by 7, 9, 13 and 18 bits are pairs of shifts.
    /// </para>
    /// </remarks>
    internal readonly struct Vector128Kernel<TIsa>
        : IScryptKernel
        where TIsa : struct, IVector128Isa
    {
        /// <summary>
        /// Rearranges the words of each 64-byte block from RFC 7914's order into the kernel's, in place.
        /// </summary>
        /// <param name="blocks">The first word of the first block.</param>
        /// <param name="count">The number of 64-byte blocks.</param>
        public static void Import(ref uint blocks, int count)
        {
            for (int b = 0; b < count; b++)
            {
                ref uint block = ref Unsafe.Add(ref blocks, b * BlockWords);
                uint w1 = Unsafe.Add(ref block, 1);
                uint w2 = Unsafe.Add(ref block, 2);
                uint w3 = Unsafe.Add(ref block, 3);
                uint w5 = Unsafe.Add(ref block, 5);
                uint w6 = Unsafe.Add(ref block, 6);
                uint w7 = Unsafe.Add(ref block, 7);
                uint w9 = Unsafe.Add(ref block, 9);
                uint w10 = Unsafe.Add(ref block, 10);
                uint w11 = Unsafe.Add(ref block, 11);
                uint w13 = Unsafe.Add(ref block, 13);
                uint w14 = Unsafe.Add(ref block, 14);
                uint w15 = Unsafe.Add(ref block, 15);

                // Position i takes word 5i mod 16; words 0, 4, 8 and 12 stay where they are.
                Unsafe.Add(ref block, 1) = w5;
                Unsafe.Add(ref block, 2) = w10;
                Unsafe.Add(ref block, 3) = w15;
                Unsafe.Add(ref block, 5) = w9;
                Unsafe.Add(ref block, 6) = w14;
                Unsafe.Add(ref block, 7) = w3;
                Unsafe.Add(ref block, 9) = w13;
                Unsafe.Add(ref block, 10) = w2;
                Unsafe.Add(ref block, 11) = w7;
                Unsafe.Add(ref block, 13) = w1;
                Unsafe.Add(ref block, 14) = w6;
                Unsafe.Add(ref block, 15) = w11;
            }
        }

        /// <summary>
        /// Rearranges the words of each 64-byte block from the kernel's order back into RFC 7914's, in place.
        /// </summary>
        /// <param name="blocks">The first word of the first block.</param>
        /// <param name="count">The number of 64-byte blocks.</param>
        public static void Export(ref uint blocks, int count)
        {
            for (int b = 0; b < count; b++)
            {
                ref uint block = ref Unsafe.Add(ref blocks, b * BlockWords);
                uint p1 = Unsafe.Add(ref block, 1);
                uint p2 = Unsafe.Add(ref block, 2);
                uint p3 = Unsafe.Add(ref block, 3);
                uint p5 = Unsafe.Add(ref block, 5);
                uint p6 = Unsafe.Add(ref block, 6);
                uint p7 = Unsafe.Add(ref block, 7);
                uint p9 = Unsafe.Add(ref block, 9);
                uint p10 = Unsafe.Add(ref block, 10);
                uint p11 = Unsafe.Add(ref block, 11);
                uint p13 = Unsafe.Add(ref block, 13);
                uint p14 = Unsafe.Add(ref block, 14);
                uint p15 = Unsafe.Add(ref block, 15);

                // Word j returns from position 13j mod 16, the inverse of Import's 5i mod 16.
                Unsafe.Add(ref block, 1) = p13;
                Unsafe.Add(ref block, 2) = p10;
                Unsafe.Add(ref block, 3) = p7;
                Unsafe.Add(ref block, 5) = p1;
                Unsafe.Add(ref block, 6) = p14;
                Unsafe.Add(ref block, 7) = p11;
                Unsafe.Add(ref block, 9) = p5;
                Unsafe.Add(ref block, 10) = p2;
                Unsafe.Add(ref block, 11) = p15;
                Unsafe.Add(ref block, 13) = p9;
                Unsafe.Add(ref block, 14) = p6;
                Unsafe.Add(ref block, 15) = p3;
            }
        }

        /// <summary>
        /// Applies the Salsa20/8 core in place to one 64-byte block held in the kernel's order.
        /// </summary>
        /// <param name="block">The first of the block's sixteen words.</param>
        public static void Salsa20_8(ref uint block)
        {
            Vector128<uint> b0 = Vector128.LoadUnsafe(ref block);
            Vector128<uint> b1 = Vector128.LoadUnsafe(ref block, 4);
            Vector128<uint> b2 = Vector128.LoadUnsafe(ref block, 8);
            Vector128<uint> b3 = Vector128.LoadUnsafe(ref block, 12);

            Salsa20_8(ref b0, ref b1, ref b2, ref b3);

            b0.StoreUnsafe(ref block);
            b1.StoreUnsafe(ref block, 4);
            b2.StoreUnsafe(ref block, 8);
            b3.StoreUnsafe(ref block, 12);
        }

        /// <summary>
        /// Applies <c>scryptBlockMix</c> to <paramref name="input" />, writing the shuffled result to
        /// <paramref name="output" />.
        /// </summary>
        /// <param name="input">The first word of the 2·r input blocks, in the kernel's order.</param>
        /// <param name="output">The first word of the destination; it must not overlap the input.</param>
        /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void BlockMix(ref uint input, ref uint output, int blockSizeR)
        {
            int blocks = 2 * blockSizeR;
            ref uint last = ref Unsafe.Add(ref input, (blocks - 1) * BlockWords);
            Vector128<uint> x0 = Vector128.LoadUnsafe(ref last);
            Vector128<uint> x1 = Vector128.LoadUnsafe(ref last, 4);
            Vector128<uint> x2 = Vector128.LoadUnsafe(ref last, 8);
            Vector128<uint> x3 = Vector128.LoadUnsafe(ref last, 12);

            for (int i = 0; i < blocks; i++)
            {
                ref uint source = ref Unsafe.Add(ref input, i * BlockWords);
                x0 ^= Vector128.LoadUnsafe(ref source);
                x1 ^= Vector128.LoadUnsafe(ref source, 4);
                x2 ^= Vector128.LoadUnsafe(ref source, 8);
                x3 ^= Vector128.LoadUnsafe(ref source, 12);

                Salsa20_8(ref x0, ref x1, ref x2, ref x3);

                ref uint destination = ref Unsafe.Add(ref output, ((i >> 1) + ((i & 1) * blockSizeR)) * BlockWords);
                x0.StoreUnsafe(ref destination);
                x1.StoreUnsafe(ref destination, 4);
                x2.StoreUnsafe(ref destination, 8);
                x3.StoreUnsafe(ref destination, 12);
            }
        }

        /// <summary>
        /// Applies <c>scryptBlockMix</c> to <c><paramref name="x" /> xor <paramref name="v" /></c>, writing the
        /// shuffled result to <paramref name="output" />, without writing the XOR out first.
        /// </summary>
        /// <param name="x">The first word of the ROMix state <c>X</c>, in the kernel's order.</param>
        /// <param name="v">The first word of the chain unit <c>V[j]</c>, in the kernel's order.</param>
        /// <param name="output">The first word of the destination; it must overlap neither input.</param>
        /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void BlockMixXor(ref uint x, ref uint v, ref uint output, int blockSizeR)
        {
            int blocks = 2 * blockSizeR;
            int last = (blocks - 1) * BlockWords;
            Vector128<uint> x0 = Vector128.LoadUnsafe(ref x, (nuint)last) ^ Vector128.LoadUnsafe(ref v, (nuint)last);
            Vector128<uint> x1 = Vector128.LoadUnsafe(ref x, (nuint)(last + 4)) ^ Vector128.LoadUnsafe(ref v, (nuint)(last + 4));
            Vector128<uint> x2 = Vector128.LoadUnsafe(ref x, (nuint)(last + 8)) ^ Vector128.LoadUnsafe(ref v, (nuint)(last + 8));
            Vector128<uint> x3 = Vector128.LoadUnsafe(ref x, (nuint)(last + 12)) ^ Vector128.LoadUnsafe(ref v, (nuint)(last + 12));

            for (int i = 0; i < blocks; i++)
            {
                ref uint xi = ref Unsafe.Add(ref x, i * BlockWords);
                ref uint vi = ref Unsafe.Add(ref v, i * BlockWords);
                x0 ^= Vector128.LoadUnsafe(ref xi) ^ Vector128.LoadUnsafe(ref vi);
                x1 ^= Vector128.LoadUnsafe(ref xi, 4) ^ Vector128.LoadUnsafe(ref vi, 4);
                x2 ^= Vector128.LoadUnsafe(ref xi, 8) ^ Vector128.LoadUnsafe(ref vi, 8);
                x3 ^= Vector128.LoadUnsafe(ref xi, 12) ^ Vector128.LoadUnsafe(ref vi, 12);

                Salsa20_8(ref x0, ref x1, ref x2, ref x3);

                ref uint destination = ref Unsafe.Add(ref output, ((i >> 1) + ((i & 1) * blockSizeR)) * BlockWords);
                x0.StoreUnsafe(ref destination);
                x1.StoreUnsafe(ref destination, 4);
                x2.StoreUnsafe(ref destination, 8);
                x3.StoreUnsafe(ref destination, 12);
            }
        }

        /// <summary>
        /// Applies the Salsa20/8 core to one block held as its four diagonals.
        /// </summary>
        /// <param name="b0">The diagonal <c>(0, 5, 10, 15)</c>, transformed in place.</param>
        /// <param name="b1">The diagonal <c>(4, 9, 14, 3)</c>, transformed in place.</param>
        /// <param name="b2">The diagonal <c>(8, 13, 2, 7)</c>, transformed in place.</param>
        /// <param name="b3">The diagonal <c>(12, 1, 6, 11)</c>, transformed in place.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Salsa20_8(ref Vector128<uint> b0, ref Vector128<uint> b1, ref Vector128<uint> b2, ref Vector128<uint> b3)
        {
            Vector128<uint> x0 = b0;
            Vector128<uint> x1 = b1;
            Vector128<uint> x2 = b2;
            Vector128<uint> x3 = b3;

            for (int i = 0; i < 8; i += 2)
            {
                // Column round: lane k of (x0, x1, x2, x3) is the k-th column quarter-round's (a, b, c, d).
                x1 ^= RotateLeft(x0 + x3, 7);
                x2 ^= RotateLeft(x1 + x0, 9);
                x3 ^= RotateLeft(x2 + x1, 13);
                x0 ^= RotateLeft(x3 + x2, 18);

                // Line up the row round: lane k of (x0, x3, x2, x1) becomes the k-th row quarter-round's (a, b, c, d).
                x1 = TIsa.RotateLanes3(x1);
                x2 = TIsa.RotateLanes2(x2);
                x3 = TIsa.RotateLanes1(x3);

                x3 ^= RotateLeft(x0 + x1, 7);
                x2 ^= RotateLeft(x3 + x0, 9);
                x1 ^= RotateLeft(x2 + x3, 13);
                x0 ^= RotateLeft(x1 + x2, 18);

                // Restore the diagonal order for the next column round.
                x1 = TIsa.RotateLanes1(x1);
                x2 = TIsa.RotateLanes2(x2);
                x3 = TIsa.RotateLanes3(x3);
            }

            b0 += x0;
            b1 += x1;
            b2 += x2;
            b3 += x3;
        }

        /// <summary>
        /// Rotates each 32-bit lane left by a constant number of bits, as a pair of shifts.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits, from 1 to 31.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<uint> RotateLeft(Vector128<uint> value, int count) =>
            Vector128.ShiftLeft(value, count) | Vector128.ShiftRightLogical(value, 32 - count);
    }
}
