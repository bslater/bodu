// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Vector256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>
    /// The eight-way implementation of the BLAKE3 compression function over <see cref="Vector256{T}" />: up to eight
    /// inputs at once, one to a lane, specialized for an instruction set by <typeparamref name="TIsa" />.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set supplying the rotations of <c>G</c>.</typeparam>
    /// <remarks>
    /// <para>
    /// Each of the sixteen working-vector words is a vector holding that word for all eight inputs, so every round runs
    /// exactly as the scalar kernel's does, one lane per input, and no lane ever needs another's words. The message
    /// arrives in the same form: each input's block is loaded as two vectors, and an 8×8 transpose of 32-bit words
    /// turns them into one vector per message word, which the rounds read as the schedule names them. The chaining
    /// values leave through the same transpose.
    /// </para>
    /// <para>
    /// The transposed message is the only copy the kernel makes of its input; it lives on the stack and is cleared
    /// before the kernel returns.
    /// </para>
    /// </remarks>
    internal readonly struct Vector256Kernel<TIsa>
        where TIsa : struct, IVector256Isa
    {
        /// <summary>The number of inputs one call compresses at most.</summary>
        internal const int Lanes = 8;

        /// <summary>
        /// Compresses up to eight equally spaced inputs of whole blocks, each from the key to its chaining value.
        /// </summary>
        /// <param name="input">The first byte of the first input.</param>
        /// <param name="stride">The distance, in bytes, between the starts of consecutive inputs.</param>
        /// <param name="lanes">The number of inputs, from 1 to 8.</param>
        /// <param name="blocks">The number of 64-byte blocks in every input.</param>
        /// <param name="key">The first of the eight key words every input starts from.</param>
        /// <param name="counter">The counter of the first input.</param>
        /// <param name="incrementCounter">
        /// <see langword="true" /> to give input <c>i</c> the counter <paramref name="counter" /> + <c>i</c>;
        /// <see langword="false" /> to give every input <paramref name="counter" />.
        /// </param>
        /// <param name="flags">The flags on every block.</param>
        /// <param name="flagsStart">The flags added to every input's first block.</param>
        /// <param name="flagsEnd">The flags added to every input's last block.</param>
        /// <param name="output">The first byte of the chaining values, 32 bytes to an input.</param>
        /// <remarks>
        /// Each block's message is read in full before the rounds, and the chaining values are written only after the
        /// last block, so the output may overlay the inputs of a one-block call.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void HashMany(
            ref byte input,
            nint stride,
            int lanes,
            int blocks,
            ref uint key,
            ulong counter,
            bool incrementCounter,
            uint flags,
            uint flagsStart,
            uint flagsEnd,
            ref byte output)
        {
            // Lanes past the inputs repeat the first input, so every load stays inside the inputs; their results are
            // dropped.
            nint offset1 = lanes > 1 ? stride : 0;
            nint offset2 = lanes > 2 ? 2 * stride : 0;
            nint offset3 = lanes > 3 ? 3 * stride : 0;
            nint offset4 = lanes > 4 ? 4 * stride : 0;
            nint offset5 = lanes > 5 ? 5 * stride : 0;
            nint offset6 = lanes > 6 ? 6 * stride : 0;
            nint offset7 = lanes > 7 ? 7 * stride : 0;

            ulong step = incrementCounter ? 1UL : 0UL;
            Vector256<uint> counterLow = Vector256.Create(
                (uint)counter,
                (uint)(counter + step),
                (uint)(counter + (2 * step)),
                (uint)(counter + (3 * step)),
                (uint)(counter + (4 * step)),
                (uint)(counter + (5 * step)),
                (uint)(counter + (6 * step)),
                (uint)(counter + (7 * step)));
            Vector256<uint> counterHigh = Vector256.Create(
                (uint)(counter >> 32),
                (uint)((counter + step) >> 32),
                (uint)((counter + (2 * step)) >> 32),
                (uint)((counter + (3 * step)) >> 32),
                (uint)((counter + (4 * step)) >> 32),
                (uint)((counter + (5 * step)) >> 32),
                (uint)((counter + (6 * step)) >> 32),
                (uint)((counter + (7 * step)) >> 32));

            Vector256<uint> h0 = Vector256.Create(key);
            Vector256<uint> h1 = Vector256.Create(Unsafe.Add(ref key, 1));
            Vector256<uint> h2 = Vector256.Create(Unsafe.Add(ref key, 2));
            Vector256<uint> h3 = Vector256.Create(Unsafe.Add(ref key, 3));
            Vector256<uint> h4 = Vector256.Create(Unsafe.Add(ref key, 4));
            Vector256<uint> h5 = Vector256.Create(Unsafe.Add(ref key, 5));
            Vector256<uint> h6 = Vector256.Create(Unsafe.Add(ref key, 6));
            Vector256<uint> h7 = Vector256.Create(Unsafe.Add(ref key, 7));

            Span<Vector256<uint>> message = stackalloc Vector256<uint>[16];
            ref Vector256<uint> m = ref MemoryMarshal.GetReference(message);
            ref byte schedule = ref MemoryMarshal.GetReference(MessageSchedule);

            for (int block = 0; block < blocks; block++)
            {
                LoadMessage(ref Unsafe.Add(ref input, block * BlockBytes), offset1, offset2, offset3, offset4, offset5, offset6, offset7, ref m);

                uint blockFlags = flags;
                if (block == 0) blockFlags |= flagsStart;
                if (block == blocks - 1) blockFlags |= flagsEnd;

                Vector256<uint> v0 = h0;
                Vector256<uint> v1 = h1;
                Vector256<uint> v2 = h2;
                Vector256<uint> v3 = h3;
                Vector256<uint> v4 = h4;
                Vector256<uint> v5 = h5;
                Vector256<uint> v6 = h6;
                Vector256<uint> v7 = h7;
                Vector256<uint> v8 = Vector256.Create(Iv0);
                Vector256<uint> v9 = Vector256.Create(Iv1);
                Vector256<uint> v10 = Vector256.Create(Iv2);
                Vector256<uint> v11 = Vector256.Create(Iv3);
                Vector256<uint> v12 = counterLow;
                Vector256<uint> v13 = counterHigh;
                Vector256<uint> v14 = Vector256.Create((uint)BlockBytes);
                Vector256<uint> v15 = Vector256.Create(blockFlags);

                // A loop over the rounds keeps the method within the JIT's inlining budget: with every round written
                // out, the later rounds' G calls stay calls.
                for (int round = 0; round < 7; round++)
                {
                    ref byte s = ref Unsafe.Add(ref schedule, round * 16);

                    G(ref v0, ref v4, ref v8, ref v12, Word(ref m, ref s, 0), Word(ref m, ref s, 1));
                    G(ref v1, ref v5, ref v9, ref v13, Word(ref m, ref s, 2), Word(ref m, ref s, 3));
                    G(ref v2, ref v6, ref v10, ref v14, Word(ref m, ref s, 4), Word(ref m, ref s, 5));
                    G(ref v3, ref v7, ref v11, ref v15, Word(ref m, ref s, 6), Word(ref m, ref s, 7));
                    G(ref v0, ref v5, ref v10, ref v15, Word(ref m, ref s, 8), Word(ref m, ref s, 9));
                    G(ref v1, ref v6, ref v11, ref v12, Word(ref m, ref s, 10), Word(ref m, ref s, 11));
                    G(ref v2, ref v7, ref v8, ref v13, Word(ref m, ref s, 12), Word(ref m, ref s, 13));
                    G(ref v3, ref v4, ref v9, ref v14, Word(ref m, ref s, 14), Word(ref m, ref s, 15));
                }

                h0 = v0 ^ v8;
                h1 = v1 ^ v9;
                h2 = v2 ^ v10;
                h3 = v3 ^ v11;
                h4 = v4 ^ v12;
                h5 = v5 ^ v13;
                h6 = v6 ^ v14;
                h7 = v7 ^ v15;
            }

            message.Clear();

            // Row i of the transposed chaining values is lane i's chaining value.
            Transpose(ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7);

            Store(h0, ref output, 0);
            if (lanes > 1) Store(h1, ref output, 1);
            if (lanes > 2) Store(h2, ref output, 2);
            if (lanes > 3) Store(h3, ref output, 3);
            if (lanes > 4) Store(h4, ref output, 4);
            if (lanes > 5) Store(h5, ref output, 5);
            if (lanes > 6) Store(h6, ref output, 6);
            if (lanes > 7) Store(h7, ref output, 7);
        }

        /// <summary>
        /// Loads one block of each of eight inputs and transposes it into sixteen vectors, one per message word.
        /// </summary>
        /// <param name="block">The first byte of the first input's block.</param>
        /// <param name="offset1">The offset of the second input's block from the first's.</param>
        /// <param name="offset2">The offset of the third input's block from the first's.</param>
        /// <param name="offset3">The offset of the fourth input's block from the first's.</param>
        /// <param name="offset4">The offset of the fifth input's block from the first's.</param>
        /// <param name="offset5">The offset of the sixth input's block from the first's.</param>
        /// <param name="offset6">The offset of the seventh input's block from the first's.</param>
        /// <param name="offset7">The offset of the eighth input's block from the first's.</param>
        /// <param name="message">The first of the sixteen vectors that receive the message words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadMessage(
            ref byte block,
            nint offset1,
            nint offset2,
            nint offset3,
            nint offset4,
            nint offset5,
            nint offset6,
            nint offset7,
            ref Vector256<uint> message)
        {
            for (nint half = 0; half < BlockBytes; half += 32)
            {
                Vector256<uint> row0 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, half)).AsUInt32();
                Vector256<uint> row1 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, offset1 + half)).AsUInt32();
                Vector256<uint> row2 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, offset2 + half)).AsUInt32();
                Vector256<uint> row3 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, offset3 + half)).AsUInt32();
                Vector256<uint> row4 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, offset4 + half)).AsUInt32();
                Vector256<uint> row5 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, offset5 + half)).AsUInt32();
                Vector256<uint> row6 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, offset6 + half)).AsUInt32();
                Vector256<uint> row7 = Vector256.LoadUnsafe(ref Unsafe.Add(ref block, offset7 + half)).AsUInt32();

                Transpose(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7);

                ref Vector256<uint> words = ref Unsafe.Add(ref message, half / sizeof(uint));
                words = row0;
                Unsafe.Add(ref words, 1) = row1;
                Unsafe.Add(ref words, 2) = row2;
                Unsafe.Add(ref words, 3) = row3;
                Unsafe.Add(ref words, 4) = row4;
                Unsafe.Add(ref words, 5) = row5;
                Unsafe.Add(ref words, 6) = row6;
                Unsafe.Add(ref words, 7) = row7;
            }
        }

        /// <summary>
        /// Transposes eight rows of eight words: afterwards row <c>i</c> holds what was column <c>i</c>.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        /// <param name="row4">The fifth row, replaced by the fifth column.</param>
        /// <param name="row5">The sixth row, replaced by the sixth column.</param>
        /// <param name="row6">The seventh row, replaced by the seventh column.</param>
        /// <param name="row7">The eighth row, replaced by the eighth column.</param>
        /// <remarks>
        /// The unpacks work within each 128-bit half, transposing the four 4×4 quarters in place; the final
        /// <c>VPERM2I128</c>s exchange the two off-diagonal quarters.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transpose(
            ref Vector256<uint> row0,
            ref Vector256<uint> row1,
            ref Vector256<uint> row2,
            ref Vector256<uint> row3,
            ref Vector256<uint> row4,
            ref Vector256<uint> row5,
            ref Vector256<uint> row6,
            ref Vector256<uint> row7)
        {
            Vector256<ulong> low01 = Avx2.UnpackLow(row0, row1).AsUInt64();
            Vector256<ulong> high01 = Avx2.UnpackHigh(row0, row1).AsUInt64();
            Vector256<ulong> low23 = Avx2.UnpackLow(row2, row3).AsUInt64();
            Vector256<ulong> high23 = Avx2.UnpackHigh(row2, row3).AsUInt64();
            Vector256<ulong> low45 = Avx2.UnpackLow(row4, row5).AsUInt64();
            Vector256<ulong> high45 = Avx2.UnpackHigh(row4, row5).AsUInt64();
            Vector256<ulong> low67 = Avx2.UnpackLow(row6, row7).AsUInt64();
            Vector256<ulong> high67 = Avx2.UnpackHigh(row6, row7).AsUInt64();

            // Columns 0 and 4, 1 and 5, 2 and 6, 3 and 7 of rows 0-3, then of rows 4-7, one 128-bit half each.
            Vector256<uint> columns04Top = Avx2.UnpackLow(low01, low23).AsUInt32();
            Vector256<uint> columns15Top = Avx2.UnpackHigh(low01, low23).AsUInt32();
            Vector256<uint> columns26Top = Avx2.UnpackLow(high01, high23).AsUInt32();
            Vector256<uint> columns37Top = Avx2.UnpackHigh(high01, high23).AsUInt32();
            Vector256<uint> columns04Bottom = Avx2.UnpackLow(low45, low67).AsUInt32();
            Vector256<uint> columns15Bottom = Avx2.UnpackHigh(low45, low67).AsUInt32();
            Vector256<uint> columns26Bottom = Avx2.UnpackLow(high45, high67).AsUInt32();
            Vector256<uint> columns37Bottom = Avx2.UnpackHigh(high45, high67).AsUInt32();

            row0 = Avx2.Permute2x128(columns04Top, columns04Bottom, 0x20);
            row1 = Avx2.Permute2x128(columns15Top, columns15Bottom, 0x20);
            row2 = Avx2.Permute2x128(columns26Top, columns26Bottom, 0x20);
            row3 = Avx2.Permute2x128(columns37Top, columns37Bottom, 0x20);
            row4 = Avx2.Permute2x128(columns04Top, columns04Bottom, 0x31);
            row5 = Avx2.Permute2x128(columns15Top, columns15Bottom, 0x31);
            row6 = Avx2.Permute2x128(columns26Top, columns26Bottom, 0x31);
            row7 = Avx2.Permute2x128(columns37Top, columns37Bottom, 0x31);
        }

        /// <summary>
        /// The BLAKE3 mixing function <c>G</c>, applied to one word of eight inputs at once.
        /// </summary>
        /// <param name="a">The first working word.</param>
        /// <param name="b">The second working word.</param>
        /// <param name="c">The third working word.</param>
        /// <param name="d">The fourth working word.</param>
        /// <param name="x">The first message word.</param>
        /// <param name="y">The second message word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(ref Vector256<uint> a, ref Vector256<uint> b, ref Vector256<uint> c, ref Vector256<uint> d, Vector256<uint> x, Vector256<uint> y)
        {
            a += b + x;
            d = TIsa.RotateRight16(d ^ a);
            c += d;
            b = TIsa.RotateRight12(b ^ c);
            a += b + y;
            d = TIsa.RotateRight8(d ^ a);
            c += d;
            b = TIsa.RotateRight7(b ^ c);
        }

        /// <summary>
        /// Returns the transposed message word a round's schedule names.
        /// </summary>
        /// <param name="message">The first of the sixteen transposed message words.</param>
        /// <param name="schedule">The first of the round's sixteen schedule entries.</param>
        /// <param name="k">The index of the schedule entry.</param>
        /// <returns>The message word, for all eight inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> Word(ref Vector256<uint> message, ref byte schedule, int k) =>
            Unsafe.Add(ref message, Unsafe.Add(ref schedule, k));

        /// <summary>
        /// Writes one input's chaining value.
        /// </summary>
        /// <param name="chainingValue">The chaining value's eight words.</param>
        /// <param name="output">The first byte of the chaining values.</param>
        /// <param name="lane">The input's index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Store(Vector256<uint> chainingValue, ref byte output, int lane) =>
            chainingValue.AsByte().StoreUnsafe(ref Unsafe.Add(ref output, lane * ChainingValueBytes));
    }
}
