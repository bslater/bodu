// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Vector256.cs" company="Bodu Pty. Ltd.">
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
    /// Produces the ChaCha20 keystream eight blocks at a time over 256-bit vectors: lane <c>i</c> of vector <c>w</c>
    /// holds word <c>w</c> of the run's <c>i</c>-th block, whose counter is the run's first counter plus <c>i</c>.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set that performs the rotations: <see cref="VectorRotation.Avx2" /> or
    /// <see cref="VectorRotation.Avx512" />.
    /// </typeparam>
    internal static class Vector256Kernel<TIsa>
        where TIsa : struct, IVector256Rotation
    {
        /// <summary>
        /// Combines runs of eight blocks of input with the keystream by XOR.
        /// </summary>
        /// <param name="state">The first of the sixteen state words; the counter word is ignored.</param>
        /// <param name="counter">The block counter of the first block.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination, which may be the first byte of the input.</param>
        /// <param name="groups">The number of runs of eight blocks.</param>
        /// <remarks>
        /// The kernel is compiled on its own, never inlined into its caller, so that the caller's inlining budget and
        /// profile cannot degrade its code.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void XorBlocks(ref uint state, uint counter, ref byte input, ref byte output, int groups)
        {
            Vector256<uint> j0 = Vector256.Create(state);
            Vector256<uint> j1 = Vector256.Create(Unsafe.Add(ref state, 1));
            Vector256<uint> j2 = Vector256.Create(Unsafe.Add(ref state, 2));
            Vector256<uint> j3 = Vector256.Create(Unsafe.Add(ref state, 3));
            Vector256<uint> j4 = Vector256.Create(Unsafe.Add(ref state, 4));
            Vector256<uint> j5 = Vector256.Create(Unsafe.Add(ref state, 5));
            Vector256<uint> j6 = Vector256.Create(Unsafe.Add(ref state, 6));
            Vector256<uint> j7 = Vector256.Create(Unsafe.Add(ref state, 7));
            Vector256<uint> j8 = Vector256.Create(Unsafe.Add(ref state, 8));
            Vector256<uint> j9 = Vector256.Create(Unsafe.Add(ref state, 9));
            Vector256<uint> j10 = Vector256.Create(Unsafe.Add(ref state, 10));
            Vector256<uint> j11 = Vector256.Create(Unsafe.Add(ref state, 11));
            Vector256<uint> j12 = Vector256.Create(counter) + Vector256.Create(0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u);
            Vector256<uint> j13 = Vector256.Create(Unsafe.Add(ref state, 13));
            Vector256<uint> j14 = Vector256.Create(Unsafe.Add(ref state, 14));
            Vector256<uint> j15 = Vector256.Create(Unsafe.Add(ref state, 15));
            nint offset = 0;

            for (int group = 0; group < groups; group++)
            {
                Vector256<uint> x0 = j0, x1 = j1, x2 = j2, x3 = j3;
                Vector256<uint> x4 = j4, x5 = j5, x6 = j6, x7 = j7;
                Vector256<uint> x8 = j8, x9 = j9, x10 = j10, x11 = j11;
                Vector256<uint> x12 = j12, x13 = j13, x14 = j14, x15 = j15;

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

                XorWords(x0 + j0, x1 + j1, x2 + j2, x3 + j3, x4 + j4, x5 + j5, x6 + j6, x7 + j7, ref input, ref output, offset);
                XorWords(x8 + j8, x9 + j9, x10 + j10, x11 + j11, x12 + j12, x13 + j13, x14 + j14, x15 + j15, ref input, ref output, offset + 32);

                j12 += Vector256.Create(8u);
                offset += 8 * BlockBytes;
            }
        }

        /// <summary>
        /// Combines eight consecutive words of each of eight consecutive blocks with the input by XOR: transposed into
        /// block order, each block's eight words are 32 contiguous keystream bytes.
        /// </summary>
        /// <param name="w0">The first of the eight words, one block per lane.</param>
        /// <param name="w1">The second word, one block per lane.</param>
        /// <param name="w2">The third word, one block per lane.</param>
        /// <param name="w3">The fourth word, one block per lane.</param>
        /// <param name="w4">The fifth word, one block per lane.</param>
        /// <param name="w5">The sixth word, one block per lane.</param>
        /// <param name="w6">The seventh word, one block per lane.</param>
        /// <param name="w7">The eighth word, one block per lane.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the eight words in the first block of the run.</param>
        /// <remarks>
        /// <para>
        /// The transposes work within each 128-bit lane, which leaves the first four blocks' words in the low halves
        /// and the last four blocks' in the high halves; a lane permute then joins each block's two groups of four
        /// words.
        /// </para>
        /// <para>
        /// <see cref="Salsa20Core" />'s 256-bit kernel shares this step, since both ciphers emit their sixteen words in
        /// order.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void XorWords(
            Vector256<uint> w0,
            Vector256<uint> w1,
            Vector256<uint> w2,
            Vector256<uint> w3,
            Vector256<uint> w4,
            Vector256<uint> w5,
            Vector256<uint> w6,
            Vector256<uint> w7,
            ref byte input,
            ref byte output,
            nint offset)
        {
            Transpose(ref w0, ref w1, ref w2, ref w3);
            Transpose(ref w4, ref w5, ref w6, ref w7);

            Xor(Avx2.Permute2x128(w0, w4, 0x20), ref input, ref output, offset);
            Xor(Avx2.Permute2x128(w1, w5, 0x20), ref input, ref output, offset + BlockBytes);
            Xor(Avx2.Permute2x128(w2, w6, 0x20), ref input, ref output, offset + (2 * BlockBytes));
            Xor(Avx2.Permute2x128(w3, w7, 0x20), ref input, ref output, offset + (3 * BlockBytes));
            Xor(Avx2.Permute2x128(w0, w4, 0x31), ref input, ref output, offset + (4 * BlockBytes));
            Xor(Avx2.Permute2x128(w1, w5, 0x31), ref input, ref output, offset + (5 * BlockBytes));
            Xor(Avx2.Permute2x128(w2, w6, 0x31), ref input, ref output, offset + (6 * BlockBytes));
            Xor(Avx2.Permute2x128(w3, w7, 0x31), ref input, ref output, offset + (7 * BlockBytes));
        }

        /// <summary>
        /// Transposes four rows of eight words as two independent 4×4 transposes, one in each 128-bit lane.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column of each lane.</param>
        /// <param name="row1">The second row, replaced by the second column of each lane.</param>
        /// <param name="row2">The third row, replaced by the third column of each lane.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column of each lane.</param>
        /// <remarks>
        /// <see cref="SerpentCore" />'s eight-block kernel shares this step to move blocks in and out of word order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Transpose(ref Vector256<uint> row0, ref Vector256<uint> row1, ref Vector256<uint> row2, ref Vector256<uint> row3)
        {
            Vector256<ulong> low01 = Avx2.UnpackLow(row0, row1).AsUInt64();
            Vector256<ulong> high01 = Avx2.UnpackHigh(row0, row1).AsUInt64();
            Vector256<ulong> low23 = Avx2.UnpackLow(row2, row3).AsUInt64();
            Vector256<ulong> high23 = Avx2.UnpackHigh(row2, row3).AsUInt64();

            row0 = Avx2.UnpackLow(low01, low23).AsUInt32();
            row1 = Avx2.UnpackHigh(low01, low23).AsUInt32();
            row2 = Avx2.UnpackLow(high01, high23).AsUInt32();
            row3 = Avx2.UnpackHigh(high01, high23).AsUInt32();
        }

        /// <summary>
        /// Combines 32 keystream bytes with the input at an offset by XOR.
        /// </summary>
        /// <param name="keystream">The keystream bytes, as eight little-endian words.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the 32 bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Xor(Vector256<uint> keystream, ref byte input, ref byte output, nint offset) =>
            (Vector256.LoadUnsafe(ref input, (nuint)offset) ^ keystream.AsByte()).StoreUnsafe(ref output, (nuint)offset);

        /// <summary>
        /// Applies the ChaCha20 quarter round to four words of every lane.
        /// </summary>
        /// <param name="a">The first word.</param>
        /// <param name="b">The second word.</param>
        /// <param name="c">The third word.</param>
        /// <param name="d">The fourth word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / XOR / rotate steps mirror the RFC 8439 quarter-round definition, as the scalar quarter round does.")]
        private static void QuarterRound(ref Vector256<uint> a, ref Vector256<uint> b, ref Vector256<uint> c, ref Vector256<uint> d)
        {
            a += b; d ^= a; d = d.RotateBitsLeftUnchecked<TIsa>(16);
            c += d; b ^= c; b = b.RotateBitsLeftUnchecked<TIsa>(12);
            a += b; d ^= a; d = d.RotateBitsLeftUnchecked<TIsa>(8);
            c += d; b ^= c; b = b.RotateBitsLeftUnchecked<TIsa>(7);
        }
    }
}
