// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Vector512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Produces the ChaCha20 keystream sixteen blocks at a time over 512-bit vectors with AVX-512F: lane <c>i</c> of
    /// vector <c>w</c> holds word <c>w</c> of the run's <c>i</c>-th block, whose counter is the run's first counter
    /// plus <c>i</c>.
    /// </summary>
    internal static class Vector512Kernel
    {
        /// <summary>
        /// Combines runs of sixteen blocks of input with the keystream by XOR.
        /// </summary>
        /// <param name="state">The first of the sixteen state words; the counter word is ignored.</param>
        /// <param name="counter">The block counter of the first block.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination, which may be the first byte of the input.</param>
        /// <param name="groups">The number of runs of sixteen blocks.</param>
        /// <remarks>
        /// The kernel is compiled on its own, never inlined into its caller, so that the caller's inlining budget and
        /// profile cannot degrade its code.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void XorBlocks(ref uint state, uint counter, ref byte input, ref byte output, int groups)
        {
            Vector512<uint> j0 = Vector512.Create(state);
            Vector512<uint> j1 = Vector512.Create(Unsafe.Add(ref state, 1));
            Vector512<uint> j2 = Vector512.Create(Unsafe.Add(ref state, 2));
            Vector512<uint> j3 = Vector512.Create(Unsafe.Add(ref state, 3));
            Vector512<uint> j4 = Vector512.Create(Unsafe.Add(ref state, 4));
            Vector512<uint> j5 = Vector512.Create(Unsafe.Add(ref state, 5));
            Vector512<uint> j6 = Vector512.Create(Unsafe.Add(ref state, 6));
            Vector512<uint> j7 = Vector512.Create(Unsafe.Add(ref state, 7));
            Vector512<uint> j8 = Vector512.Create(Unsafe.Add(ref state, 8));
            Vector512<uint> j9 = Vector512.Create(Unsafe.Add(ref state, 9));
            Vector512<uint> j10 = Vector512.Create(Unsafe.Add(ref state, 10));
            Vector512<uint> j11 = Vector512.Create(Unsafe.Add(ref state, 11));
            Vector512<uint> j12 = Vector512.Create(counter) + Vector512.Create(0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u, 9u, 10u, 11u, 12u, 13u, 14u, 15u);
            Vector512<uint> j13 = Vector512.Create(Unsafe.Add(ref state, 13));
            Vector512<uint> j14 = Vector512.Create(Unsafe.Add(ref state, 14));
            Vector512<uint> j15 = Vector512.Create(Unsafe.Add(ref state, 15));
            nint offset = 0;

            for (int group = 0; group < groups; group++)
            {
                Vector512<uint> x0 = j0, x1 = j1, x2 = j2, x3 = j3;
                Vector512<uint> x4 = j4, x5 = j5, x6 = j6, x7 = j7;
                Vector512<uint> x8 = j8, x9 = j9, x10 = j10, x11 = j11;
                Vector512<uint> x12 = j12, x13 = j13, x14 = j14, x15 = j15;

                for (int round = 0; round < DoubleRounds; round++)
                {
                    QuarterRound(ref x0, ref x4, ref x8, ref x12);
                    QuarterRound(ref x1, ref x5, ref x9, ref x13);
                    QuarterRound(ref x2, ref x6, ref x10, ref x14);
                    QuarterRound(ref x3, ref x7, ref x11, ref x15);

                    QuarterRound(ref x0, ref x5, ref x10, ref x15);
                    QuarterRound(ref x1, ref x6, ref x11, ref x12);
                    QuarterRound(ref x2, ref x7, ref x8, ref x13);
                    QuarterRound(ref x3, ref x4, ref x9, ref x14);
                }

                x0 += j0;
                x1 += j1;
                x2 += j2;
                x3 += j3;
                x4 += j4;
                x5 += j5;
                x6 += j6;
                x7 += j7;
                x8 += j8;
                x9 += j9;
                x10 += j10;
                x11 += j11;
                x12 += j12;
                x13 += j13;
                x14 += j14;
                x15 += j15;

                // Each group of four words, transposed within every 128-bit lane, leaves lane k of vector 4g + b
                // holding words 4g to 4g + 3 of block 4k + b; each block's four such lanes are then gathered.
                Transpose(ref x0, ref x1, ref x2, ref x3);
                Transpose(ref x4, ref x5, ref x6, ref x7);
                Transpose(ref x8, ref x9, ref x10, ref x11);
                Transpose(ref x12, ref x13, ref x14, ref x15);

                XorQuarters(x0, x4, x8, x12, ref input, ref output, offset);
                XorQuarters(x1, x5, x9, x13, ref input, ref output, offset + BlockBytes);
                XorQuarters(x2, x6, x10, x14, ref input, ref output, offset + (2 * BlockBytes));
                XorQuarters(x3, x7, x11, x15, ref input, ref output, offset + (3 * BlockBytes));

                j12 += Vector512.Create(16u);
                offset += 16 * BlockBytes;
            }
        }

        /// <summary>
        /// Transposes four rows of sixteen words as four independent 4×4 transposes, one in each 128-bit lane.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column of each lane.</param>
        /// <param name="row1">The second row, replaced by the second column of each lane.</param>
        /// <param name="row2">The third row, replaced by the third column of each lane.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column of each lane.</param>
        /// <remarks>
        /// <see cref="Salsa20Core" />'s 512-bit kernel shares this step, since both ciphers emit their sixteen words in
        /// order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Transpose(ref Vector512<uint> row0, ref Vector512<uint> row1, ref Vector512<uint> row2, ref Vector512<uint> row3)
        {
            Vector512<ulong> low01 = Avx512F.UnpackLow(row0, row1).AsUInt64();
            Vector512<ulong> high01 = Avx512F.UnpackHigh(row0, row1).AsUInt64();
            Vector512<ulong> low23 = Avx512F.UnpackLow(row2, row3).AsUInt64();
            Vector512<ulong> high23 = Avx512F.UnpackHigh(row2, row3).AsUInt64();

            row0 = Avx512F.UnpackLow(low01, low23).AsUInt32();
            row1 = Avx512F.UnpackHigh(low01, low23).AsUInt32();
            row2 = Avx512F.UnpackLow(high01, high23).AsUInt32();
            row3 = Avx512F.UnpackHigh(high01, high23).AsUInt32();
        }

        /// <summary>
        /// Combines four blocks with the input by XOR - the first and the blocks four, eight and twelve after it -
        /// gathering each from the same 128-bit lane of the vectors of its words 0-3, 4-7, 8-11 and 12-15.
        /// </summary>
        /// <param name="quarter0">Words 0-3 of the four blocks, one block per 128-bit lane.</param>
        /// <param name="quarter1">Words 4-7 of the four blocks.</param>
        /// <param name="quarter2">Words 8-11 of the four blocks.</param>
        /// <param name="quarter3">Words 12-15 of the four blocks.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the first of the four blocks.</param>
        /// <remarks>
        /// <see cref="Salsa20Core" />'s 512-bit kernel shares this step, since both ciphers emit their sixteen words in
        /// order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void XorQuarters(Vector512<uint> quarter0, Vector512<uint> quarter1, Vector512<uint> quarter2, Vector512<uint> quarter3, ref byte input, ref byte output, nint offset)
        {
            // Lanes 0 and 1, then lanes 2 and 3, of each pair of quarters; then the lanes of each block side by side.
            Vector512<uint> low01 = Avx512F.Shuffle4x128(quarter0, quarter1, 0x44);
            Vector512<uint> high01 = Avx512F.Shuffle4x128(quarter0, quarter1, 0xEE);
            Vector512<uint> low23 = Avx512F.Shuffle4x128(quarter2, quarter3, 0x44);
            Vector512<uint> high23 = Avx512F.Shuffle4x128(quarter2, quarter3, 0xEE);

            Xor(Avx512F.Shuffle4x128(low01, low23, 0x88), ref input, ref output, offset);
            Xor(Avx512F.Shuffle4x128(low01, low23, 0xDD), ref input, ref output, offset + (4 * BlockBytes));
            Xor(Avx512F.Shuffle4x128(high01, high23, 0x88), ref input, ref output, offset + (8 * BlockBytes));
            Xor(Avx512F.Shuffle4x128(high01, high23, 0xDD), ref input, ref output, offset + (12 * BlockBytes));
        }

        /// <summary>
        /// Combines a 64-byte keystream block with the input at an offset by XOR.
        /// </summary>
        /// <param name="keystream">The keystream block, as sixteen little-endian words.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Xor(Vector512<uint> keystream, ref byte input, ref byte output, nint offset) =>
            (Vector512.LoadUnsafe(ref input, (nuint)offset) ^ keystream.AsByte()).StoreUnsafe(ref output, (nuint)offset);

        /// <summary>
        /// Applies the ChaCha20 quarter round to four words of every lane.
        /// </summary>
        /// <param name="a">The first word.</param>
        /// <param name="b">The second word.</param>
        /// <param name="c">The third word.</param>
        /// <param name="d">The fourth word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / XOR / rotate steps mirror the RFC 8439 quarter-round definition, as the scalar quarter round does.")]
        private static void QuarterRound(ref Vector512<uint> a, ref Vector512<uint> b, ref Vector512<uint> c, ref Vector512<uint> d)
        {
            a += b; d ^= a; d = d.RotateBitsLeftUnchecked<VectorRotation.Avx512>(16);
            c += d; b ^= c; b = b.RotateBitsLeftUnchecked<VectorRotation.Avx512>(12);
            a += b; d ^= a; d = d.RotateBitsLeftUnchecked<VectorRotation.Avx512>(8);
            c += d; b ^= c; b = b.RotateBitsLeftUnchecked<VectorRotation.Avx512>(7);
        }
    }
}
