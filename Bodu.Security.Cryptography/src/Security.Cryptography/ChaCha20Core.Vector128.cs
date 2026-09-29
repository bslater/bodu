// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Vector128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Produces the ChaCha20 keystream four blocks at a time over 128-bit vectors: lane <c>i</c> of vector <c>w</c>
    /// holds word <c>w</c> of the run's <c>i</c>-th block, whose counter is the run's first counter plus <c>i</c>.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set that performs the rotations: <see cref="VectorRotation.Ssse3" />,
    /// <see cref="VectorRotation.AdvSimd" /> or <see cref="VectorRotation.Avx512" />.
    /// </typeparam>
    internal static class Vector128Kernel<TIsa>
        where TIsa : struct, IVector128Rotation
    {
        /// <summary>
        /// Combines runs of four blocks of input with the keystream by XOR.
        /// </summary>
        /// <param name="state">The first of the sixteen state words; the counter word is ignored.</param>
        /// <param name="counter">The block counter of the first block.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination, which may be the first byte of the input.</param>
        /// <param name="groups">The number of runs of four blocks.</param>
        /// <remarks>
        /// The kernel is compiled on its own, never inlined into its caller, so that the caller's inlining budget and
        /// profile cannot degrade its code.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void XorBlocks(ref uint state, uint counter, ref byte input, ref byte output, int groups)
        {
            Vector128<uint> j0 = Vector128.Create(state);
            Vector128<uint> j1 = Vector128.Create(Unsafe.Add(ref state, 1));
            Vector128<uint> j2 = Vector128.Create(Unsafe.Add(ref state, 2));
            Vector128<uint> j3 = Vector128.Create(Unsafe.Add(ref state, 3));
            Vector128<uint> j4 = Vector128.Create(Unsafe.Add(ref state, 4));
            Vector128<uint> j5 = Vector128.Create(Unsafe.Add(ref state, 5));
            Vector128<uint> j6 = Vector128.Create(Unsafe.Add(ref state, 6));
            Vector128<uint> j7 = Vector128.Create(Unsafe.Add(ref state, 7));
            Vector128<uint> j8 = Vector128.Create(Unsafe.Add(ref state, 8));
            Vector128<uint> j9 = Vector128.Create(Unsafe.Add(ref state, 9));
            Vector128<uint> j10 = Vector128.Create(Unsafe.Add(ref state, 10));
            Vector128<uint> j11 = Vector128.Create(Unsafe.Add(ref state, 11));
            Vector128<uint> j12 = Vector128.Create(counter) + Vector128.Create(0u, 1u, 2u, 3u);
            Vector128<uint> j13 = Vector128.Create(Unsafe.Add(ref state, 13));
            Vector128<uint> j14 = Vector128.Create(Unsafe.Add(ref state, 14));
            Vector128<uint> j15 = Vector128.Create(Unsafe.Add(ref state, 15));
            nint offset = 0;

            for (int group = 0; group < groups; group++)
            {
                Vector128<uint> x0 = j0, x1 = j1, x2 = j2, x3 = j3;
                Vector128<uint> x4 = j4, x5 = j5, x6 = j6, x7 = j7;
                Vector128<uint> x8 = j8, x9 = j9, x10 = j10, x11 = j11;
                Vector128<uint> x12 = j12, x13 = j13, x14 = j14, x15 = j15;

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

                XorWords(x0 + j0, x1 + j1, x2 + j2, x3 + j3, ref input, ref output, offset);
                XorWords(x4 + j4, x5 + j5, x6 + j6, x7 + j7, ref input, ref output, offset + 16);
                XorWords(x8 + j8, x9 + j9, x10 + j10, x11 + j11, ref input, ref output, offset + 32);
                XorWords(x12 + j12, x13 + j13, x14 + j14, x15 + j15, ref input, ref output, offset + 48);

                j12 += Vector128.Create(4u);
                offset += 4 * BlockBytes;
            }
        }

        /// <summary>
        /// Combines four consecutive words of each of four consecutive blocks with the input by XOR: transposed into
        /// block order, each block's four words are sixteen contiguous keystream bytes.
        /// </summary>
        /// <param name="w0">The first of the four words, one block per lane.</param>
        /// <param name="w1">The second word, one block per lane.</param>
        /// <param name="w2">The third word, one block per lane.</param>
        /// <param name="w3">The fourth word, one block per lane.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the four words in the first block of the run.</param>
        /// <remarks>
        /// <see cref="Salsa20Core" />'s 128-bit kernel shares this step, since both ciphers emit their sixteen words in
        /// order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void XorWords(Vector128<uint> w0, Vector128<uint> w1, Vector128<uint> w2, Vector128<uint> w3, ref byte input, ref byte output, nint offset)
        {
            Transpose(ref w0, ref w1, ref w2, ref w3);

            Xor(w0, ref input, ref output, offset);
            Xor(w1, ref input, ref output, offset + BlockBytes);
            Xor(w2, ref input, ref output, offset + (2 * BlockBytes));
            Xor(w3, ref input, ref output, offset + (3 * BlockBytes));
        }

        /// <summary>
        /// Combines sixteen keystream bytes with the input at an offset by XOR.
        /// </summary>
        /// <param name="keystream">The keystream bytes, as four little-endian words.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the sixteen bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Xor(Vector128<uint> keystream, ref byte input, ref byte output, nint offset) =>
            (Vector128.LoadUnsafe(ref input, (nuint)offset) ^ keystream.AsByte()).StoreUnsafe(ref output, (nuint)offset);

        /// <summary>
        /// Applies the ChaCha20 quarter round to four words of every lane.
        /// </summary>
        /// <param name="a">The first word.</param>
        /// <param name="b">The second word.</param>
        /// <param name="c">The third word.</param>
        /// <param name="d">The fourth word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / XOR / rotate steps mirror the RFC 8439 quarter-round definition, as the scalar quarter round does.")]
        private static void QuarterRound(ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d)
        {
            a += b; d ^= a; d = d.RotateBitsLeftUnchecked<TIsa>(16);
            c += d; b ^= c; b = b.RotateBitsLeftUnchecked<TIsa>(12);
            a += b; d ^= a; d = d.RotateBitsLeftUnchecked<TIsa>(8);
            c += d; b ^= c; b = b.RotateBitsLeftUnchecked<TIsa>(7);
        }
    }
}
