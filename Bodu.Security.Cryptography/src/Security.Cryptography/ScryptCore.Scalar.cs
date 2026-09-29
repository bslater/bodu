// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.Scalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// The portable scalar implementation of <c>scryptBlockMix</c>. It runs where no vector instruction set is
    /// available or the process has opted out of SIMD, and it is the reference the vector kernels are tested against.
    /// </summary>
    /// <remarks>
    /// The kernel keeps blocks in RFC 7914's word order, so importing and exporting a unit leave it as it is.
    /// </remarks>
    internal readonly struct ScalarKernel
        : IScryptKernel
    {
        /// <summary>
        /// Leaves the blocks as they are: this kernel's word order is RFC 7914's own.
        /// </summary>
        /// <param name="blocks">The first word of the first block.</param>
        /// <param name="count">The number of 64-byte blocks.</param>
        public static void Import(ref uint blocks, int count)
        {
        }

        /// <summary>
        /// Leaves the blocks as they are: this kernel's word order is RFC 7914's own.
        /// </summary>
        /// <param name="blocks">The first word of the first block.</param>
        /// <param name="count">The number of 64-byte blocks.</param>
        public static void Export(ref uint blocks, int count)
        {
        }

        /// <summary>
        /// Applies <c>scryptBlockMix</c> to <paramref name="input" />, writing the shuffled result to
        /// <paramref name="output" />.
        /// </summary>
        /// <param name="input">The first word of the 2·r input blocks, in the kernel's order.</param>
        /// <param name="output">The first word of the destination; it must not overlap the input.</param>
        /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
        /// <remarks>
        /// Each Salsa20/8 result is computed in its place in the output: even-indexed results fill the first half and
        /// odd-indexed ones the second, and each result is the chaining value for the next.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void BlockMix(ref uint input, ref uint output, int blockSizeR)
        {
            int blocks = 2 * blockSizeR;
            ref uint previous = ref Unsafe.Add(ref input, (blocks - 1) * BlockWords);

            for (int i = 0; i < blocks; i++)
            {
                ref uint source = ref Unsafe.Add(ref input, i * BlockWords);
                ref uint destination = ref Unsafe.Add(ref output, ((i >> 1) + ((i & 1) * blockSizeR)) * BlockWords);
                for (int k = 0; k < BlockWords; k++)
                    Unsafe.Add(ref destination, k) = Unsafe.Add(ref previous, k) ^ Unsafe.Add(ref source, k);

                Salsa20_8(ref destination);
                previous = ref destination;
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

            // The first chaining value is the last block of X xor V[j]; every later one is the previous result.
            ref uint destination = ref output;
            for (int k = 0; k < BlockWords; k++)
            {
                Unsafe.Add(ref destination, k) =
                    Unsafe.Add(ref x, last + k) ^ Unsafe.Add(ref v, last + k) ^ Unsafe.Add(ref x, k) ^ Unsafe.Add(ref v, k);
            }

            Salsa20_8(ref destination);
            ref uint previous = ref destination;

            for (int i = 1; i < blocks; i++)
            {
                int offset = i * BlockWords;
                destination = ref Unsafe.Add(ref output, ((i >> 1) + ((i & 1) * blockSizeR)) * BlockWords);
                for (int k = 0; k < BlockWords; k++)
                    Unsafe.Add(ref destination, k) = Unsafe.Add(ref previous, k) ^ Unsafe.Add(ref x, offset + k) ^ Unsafe.Add(ref v, offset + k);

                Salsa20_8(ref destination);
                previous = ref destination;
            }
        }

        /// <summary>
        /// Applies the Salsa20/8 core in place to one 64-byte block held in the kernel's order.
        /// </summary>
        /// <param name="block">The first of the block's sixteen words.</param>
        /// <remarks>
        /// <c>B = B + doubleround^4(B)</c> (RFC 7914, Section 3), with the sixteen words held in locals.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Salsa20_8(ref uint block)
        {
            uint x0 = block;
            uint x1 = Unsafe.Add(ref block, 1);
            uint x2 = Unsafe.Add(ref block, 2);
            uint x3 = Unsafe.Add(ref block, 3);
            uint x4 = Unsafe.Add(ref block, 4);
            uint x5 = Unsafe.Add(ref block, 5);
            uint x6 = Unsafe.Add(ref block, 6);
            uint x7 = Unsafe.Add(ref block, 7);
            uint x8 = Unsafe.Add(ref block, 8);
            uint x9 = Unsafe.Add(ref block, 9);
            uint x10 = Unsafe.Add(ref block, 10);
            uint x11 = Unsafe.Add(ref block, 11);
            uint x12 = Unsafe.Add(ref block, 12);
            uint x13 = Unsafe.Add(ref block, 13);
            uint x14 = Unsafe.Add(ref block, 14);
            uint x15 = Unsafe.Add(ref block, 15);

            for (int i = 0; i < 8; i += 2)
            {
                // Column round.
                QuarterRound(ref x0, ref x4, ref x8, ref x12);
                QuarterRound(ref x5, ref x9, ref x13, ref x1);
                QuarterRound(ref x10, ref x14, ref x2, ref x6);
                QuarterRound(ref x15, ref x3, ref x7, ref x11);

                // Row round.
                QuarterRound(ref x0, ref x1, ref x2, ref x3);
                QuarterRound(ref x5, ref x6, ref x7, ref x4);
                QuarterRound(ref x10, ref x11, ref x8, ref x9);
                QuarterRound(ref x15, ref x12, ref x13, ref x14);
            }

            block += x0;
            Unsafe.Add(ref block, 1) += x1;
            Unsafe.Add(ref block, 2) += x2;
            Unsafe.Add(ref block, 3) += x3;
            Unsafe.Add(ref block, 4) += x4;
            Unsafe.Add(ref block, 5) += x5;
            Unsafe.Add(ref block, 6) += x6;
            Unsafe.Add(ref block, 7) += x7;
            Unsafe.Add(ref block, 8) += x8;
            Unsafe.Add(ref block, 9) += x9;
            Unsafe.Add(ref block, 10) += x10;
            Unsafe.Add(ref block, 11) += x11;
            Unsafe.Add(ref block, 12) += x12;
            Unsafe.Add(ref block, 13) += x13;
            Unsafe.Add(ref block, 14) += x14;
            Unsafe.Add(ref block, 15) += x15;
        }

        /// <summary>
        /// Applies the Salsa20 quarter-round to four state words in place.
        /// </summary>
        /// <param name="a">The first state word, updated in place.</param>
        /// <param name="b">The second state word, updated in place.</param>
        /// <param name="c">The third state word, updated in place.</param>
        /// <param name="d">The fourth state word, updated in place.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            b ^= (a + d).RotateBitsLeftUnchecked(7);
            c ^= (b + a).RotateBitsLeftUnchecked(9);
            d ^= (c + b).RotateBitsLeftUnchecked(13);
            a ^= (d + c).RotateBitsLeftUnchecked(18);
        }
    }
}
