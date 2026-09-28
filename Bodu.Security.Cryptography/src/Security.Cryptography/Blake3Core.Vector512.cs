// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Vector512.cs" company="Bodu Pty. Ltd.">
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
    /// The sixteen-way implementation of the BLAKE3 compression function over <see cref="Vector512{T}" /> with
    /// AVX-512F: up to sixteen inputs at once, one to a lane.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is the eight-way kernel at twice the width: each working-vector word is a vector holding that word for all
    /// sixteen inputs, each input's block arrives as one vector, and a 16×16 transpose of 32-bit words turns the
    /// sixteen blocks into one vector per message word. The chaining values leave through the same transpose, with
    /// eight rows of zeros standing in for the half of the output a 256-bit hash does not read.
    /// </para>
    /// <para>
    /// The transposed message is the only copy the kernel makes of its input; it lives on the stack and is cleared
    /// before the kernel returns.
    /// </para>
    /// </remarks>
    internal readonly struct Vector512Kernel
    {
        /// <summary>The number of inputs one call compresses at most.</summary>
        internal const int Lanes = 16;

        /// <summary>
        /// Compresses up to sixteen equally spaced inputs of whole blocks, each from the key to its chaining value.
        /// </summary>
        /// <param name="input">The first byte of the first input.</param>
        /// <param name="stride">The distance, in bytes, between the starts of consecutive inputs.</param>
        /// <param name="lanes">The number of inputs, from 1 to 16.</param>
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
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
            Span<nint> offsets = stackalloc nint[Lanes];
            for (int lane = 0; lane < Lanes; lane++)
                offsets[lane] = lane < lanes ? lane * stride : 0;

            // Lane i's counter is the first input's plus i, carried into the high word where the low word wraps.
            Vector512<uint> counterBase = Vector512.Create((uint)counter);
            Vector512<uint> counterLow = incrementCounter
                ? counterBase + Vector512.Create(0U, 1U, 2U, 3U, 4U, 5U, 6U, 7U, 8U, 9U, 10U, 11U, 12U, 13U, 14U, 15U)
                : counterBase;
            Vector512<uint> counterHigh = Vector512.Create((uint)(counter >> 32)) - Vector512.LessThan(counterLow, counterBase);

            Vector512<uint> h0 = Vector512.Create(key);
            Vector512<uint> h1 = Vector512.Create(Unsafe.Add(ref key, 1));
            Vector512<uint> h2 = Vector512.Create(Unsafe.Add(ref key, 2));
            Vector512<uint> h3 = Vector512.Create(Unsafe.Add(ref key, 3));
            Vector512<uint> h4 = Vector512.Create(Unsafe.Add(ref key, 4));
            Vector512<uint> h5 = Vector512.Create(Unsafe.Add(ref key, 5));
            Vector512<uint> h6 = Vector512.Create(Unsafe.Add(ref key, 6));
            Vector512<uint> h7 = Vector512.Create(Unsafe.Add(ref key, 7));

            Span<Vector512<uint>> message = stackalloc Vector512<uint>[16];
            ref Vector512<uint> m = ref MemoryMarshal.GetReference(message);
            ref byte schedule = ref MemoryMarshal.GetReference(MessageSchedule);

            for (int block = 0; block < blocks; block++)
            {
                LoadMessage(ref Unsafe.Add(ref input, block * BlockBytes), ref MemoryMarshal.GetReference(offsets), ref m);

                uint blockFlags = flags;
                if (block == 0) blockFlags |= flagsStart;
                if (block == blocks - 1) blockFlags |= flagsEnd;

                Vector512<uint> v0 = h0;
                Vector512<uint> v1 = h1;
                Vector512<uint> v2 = h2;
                Vector512<uint> v3 = h3;
                Vector512<uint> v4 = h4;
                Vector512<uint> v5 = h5;
                Vector512<uint> v6 = h6;
                Vector512<uint> v7 = h7;
                Vector512<uint> v8 = Vector512.Create(Iv0);
                Vector512<uint> v9 = Vector512.Create(Iv1);
                Vector512<uint> v10 = Vector512.Create(Iv2);
                Vector512<uint> v11 = Vector512.Create(Iv3);
                Vector512<uint> v12 = counterLow;
                Vector512<uint> v13 = counterHigh;
                Vector512<uint> v14 = Vector512.Create((uint)BlockBytes);
                Vector512<uint> v15 = Vector512.Create(blockFlags);

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

            // Row i of the transposed chaining values holds lane i's chaining value in its low half; the zero rows fill
            // the high half.
            Vector512<uint> z8 = Vector512<uint>.Zero;
            Vector512<uint> z9 = Vector512<uint>.Zero;
            Vector512<uint> z10 = Vector512<uint>.Zero;
            Vector512<uint> z11 = Vector512<uint>.Zero;
            Vector512<uint> z12 = Vector512<uint>.Zero;
            Vector512<uint> z13 = Vector512<uint>.Zero;
            Vector512<uint> z14 = Vector512<uint>.Zero;
            Vector512<uint> z15 = Vector512<uint>.Zero;
            Transpose(ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref z8, ref z9, ref z10, ref z11, ref z12, ref z13, ref z14, ref z15);

            Store(h0, ref output, 0);
            if (lanes > 1) Store(h1, ref output, 1);
            if (lanes > 2) Store(h2, ref output, 2);
            if (lanes > 3) Store(h3, ref output, 3);
            if (lanes > 4) Store(h4, ref output, 4);
            if (lanes > 5) Store(h5, ref output, 5);
            if (lanes > 6) Store(h6, ref output, 6);
            if (lanes > 7) Store(h7, ref output, 7);
            if (lanes > 8) Store(z8, ref output, 8);
            if (lanes > 9) Store(z9, ref output, 9);
            if (lanes > 10) Store(z10, ref output, 10);
            if (lanes > 11) Store(z11, ref output, 11);
            if (lanes > 12) Store(z12, ref output, 12);
            if (lanes > 13) Store(z13, ref output, 13);
            if (lanes > 14) Store(z14, ref output, 14);
            if (lanes > 15) Store(z15, ref output, 15);
        }

        /// <summary>
        /// Loads one block of each of sixteen inputs and transposes it into sixteen vectors, one per message word.
        /// </summary>
        /// <param name="block">The first byte of the first input's block.</param>
        /// <param name="offsets">The first of the sixteen inputs' offsets from the first input.</param>
        /// <param name="message">The first of the sixteen vectors that receive the message words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadMessage(ref byte block, ref nint offsets, ref Vector512<uint> message)
        {
            Vector512<uint> row0 = Load(ref block, ref offsets, 0);
            Vector512<uint> row1 = Load(ref block, ref offsets, 1);
            Vector512<uint> row2 = Load(ref block, ref offsets, 2);
            Vector512<uint> row3 = Load(ref block, ref offsets, 3);
            Vector512<uint> row4 = Load(ref block, ref offsets, 4);
            Vector512<uint> row5 = Load(ref block, ref offsets, 5);
            Vector512<uint> row6 = Load(ref block, ref offsets, 6);
            Vector512<uint> row7 = Load(ref block, ref offsets, 7);
            Vector512<uint> row8 = Load(ref block, ref offsets, 8);
            Vector512<uint> row9 = Load(ref block, ref offsets, 9);
            Vector512<uint> row10 = Load(ref block, ref offsets, 10);
            Vector512<uint> row11 = Load(ref block, ref offsets, 11);
            Vector512<uint> row12 = Load(ref block, ref offsets, 12);
            Vector512<uint> row13 = Load(ref block, ref offsets, 13);
            Vector512<uint> row14 = Load(ref block, ref offsets, 14);
            Vector512<uint> row15 = Load(ref block, ref offsets, 15);

            Transpose(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7, ref row8, ref row9, ref row10, ref row11, ref row12, ref row13, ref row14, ref row15);

            message = row0;
            Unsafe.Add(ref message, 1) = row1;
            Unsafe.Add(ref message, 2) = row2;
            Unsafe.Add(ref message, 3) = row3;
            Unsafe.Add(ref message, 4) = row4;
            Unsafe.Add(ref message, 5) = row5;
            Unsafe.Add(ref message, 6) = row6;
            Unsafe.Add(ref message, 7) = row7;
            Unsafe.Add(ref message, 8) = row8;
            Unsafe.Add(ref message, 9) = row9;
            Unsafe.Add(ref message, 10) = row10;
            Unsafe.Add(ref message, 11) = row11;
            Unsafe.Add(ref message, 12) = row12;
            Unsafe.Add(ref message, 13) = row13;
            Unsafe.Add(ref message, 14) = row14;
            Unsafe.Add(ref message, 15) = row15;
        }

        /// <summary>
        /// Loads one input's block as a vector of sixteen words.
        /// </summary>
        /// <param name="block">The first byte of the first input's block.</param>
        /// <param name="offsets">The first of the sixteen inputs' offsets from the first input.</param>
        /// <param name="lane">The input's index.</param>
        /// <returns>The block's sixteen words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<uint> Load(ref byte block, ref nint offsets, int lane) =>
            Vector512.LoadUnsafe(ref Unsafe.Add(ref block, Unsafe.Add(ref offsets, lane))).AsUInt32();

        /// <summary>
        /// Transposes sixteen rows of sixteen words: afterwards row <c>i</c> holds what was column <c>i</c>.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        /// <param name="row4">The fifth row, replaced by the fifth column.</param>
        /// <param name="row5">The sixth row, replaced by the sixth column.</param>
        /// <param name="row6">The seventh row, replaced by the seventh column.</param>
        /// <param name="row7">The eighth row, replaced by the eighth column.</param>
        /// <param name="row8">The ninth row, replaced by the ninth column.</param>
        /// <param name="row9">The tenth row, replaced by the tenth column.</param>
        /// <param name="row10">The eleventh row, replaced by the eleventh column.</param>
        /// <param name="row11">The twelfth row, replaced by the twelfth column.</param>
        /// <param name="row12">The thirteenth row, replaced by the thirteenth column.</param>
        /// <param name="row13">The fourteenth row, replaced by the fourteenth column.</param>
        /// <param name="row14">The fifteenth row, replaced by the fifteenth column.</param>
        /// <param name="row15">The sixteenth row, replaced by the sixteenth column.</param>
        /// <remarks>
        /// The unpacks transpose each 4×4 block of words within its 128-bit lane; two rounds of <c>VSHUFI32X4</c> then
        /// move each block to its transposed place.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transpose(
            ref Vector512<uint> row0,
            ref Vector512<uint> row1,
            ref Vector512<uint> row2,
            ref Vector512<uint> row3,
            ref Vector512<uint> row4,
            ref Vector512<uint> row5,
            ref Vector512<uint> row6,
            ref Vector512<uint> row7,
            ref Vector512<uint> row8,
            ref Vector512<uint> row9,
            ref Vector512<uint> row10,
            ref Vector512<uint> row11,
            ref Vector512<uint> row12,
            ref Vector512<uint> row13,
            ref Vector512<uint> row14,
            ref Vector512<uint> row15)
        {
            // Lane k of block[4g + j] holds word 4k + j of rows 4g to 4g + 3.
            Transpose4(row0, row1, row2, row3, out Vector512<uint> block0, out Vector512<uint> block1, out Vector512<uint> block2, out Vector512<uint> block3);
            Transpose4(row4, row5, row6, row7, out Vector512<uint> block4, out Vector512<uint> block5, out Vector512<uint> block6, out Vector512<uint> block7);
            Transpose4(row8, row9, row10, row11, out Vector512<uint> block8, out Vector512<uint> block9, out Vector512<uint> block10, out Vector512<uint> block11);
            Transpose4(row12, row13, row14, row15, out Vector512<uint> block12, out Vector512<uint> block13, out Vector512<uint> block14, out Vector512<uint> block15);

            Gather(block0, block4, block8, block12, out row0, out row4, out row8, out row12);
            Gather(block1, block5, block9, block13, out row1, out row5, out row9, out row13);
            Gather(block2, block6, block10, block14, out row2, out row6, out row10, out row14);
            Gather(block3, block7, block11, block15, out row3, out row7, out row11, out row15);
        }

        /// <summary>
        /// Transposes the 4×4 block of words in each 128-bit lane of four rows.
        /// </summary>
        /// <param name="row0">The first row.</param>
        /// <param name="row1">The second row.</param>
        /// <param name="row2">The third row.</param>
        /// <param name="row3">The fourth row.</param>
        /// <param name="word0">Receives, in lane <c>k</c>, word <c>4k</c> of the four rows.</param>
        /// <param name="word1">Receives, in lane <c>k</c>, word <c>4k + 1</c> of the four rows.</param>
        /// <param name="word2">Receives, in lane <c>k</c>, word <c>4k + 2</c> of the four rows.</param>
        /// <param name="word3">Receives, in lane <c>k</c>, word <c>4k + 3</c> of the four rows.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transpose4(
            Vector512<uint> row0,
            Vector512<uint> row1,
            Vector512<uint> row2,
            Vector512<uint> row3,
            out Vector512<uint> word0,
            out Vector512<uint> word1,
            out Vector512<uint> word2,
            out Vector512<uint> word3)
        {
            Vector512<ulong> low01 = Avx512F.UnpackLow(row0, row1).AsUInt64();
            Vector512<ulong> high01 = Avx512F.UnpackHigh(row0, row1).AsUInt64();
            Vector512<ulong> low23 = Avx512F.UnpackLow(row2, row3).AsUInt64();
            Vector512<ulong> high23 = Avx512F.UnpackHigh(row2, row3).AsUInt64();

            word0 = Avx512F.UnpackLow(low01, low23).AsUInt32();
            word1 = Avx512F.UnpackHigh(low01, low23).AsUInt32();
            word2 = Avx512F.UnpackLow(high01, high23).AsUInt32();
            word3 = Avx512F.UnpackHigh(high01, high23).AsUInt32();
        }

        /// <summary>
        /// Gathers lane <c>k</c> of four 4×4-transposed rows into column <c>4k + j</c>.
        /// </summary>
        /// <param name="rows0">Word <c>4k + j</c> of rows 0 to 3, in lane <c>k</c>.</param>
        /// <param name="rows4">Word <c>4k + j</c> of rows 4 to 7, in lane <c>k</c>.</param>
        /// <param name="rows8">Word <c>4k + j</c> of rows 8 to 11, in lane <c>k</c>.</param>
        /// <param name="rows12">Word <c>4k + j</c> of rows 12 to 15, in lane <c>k</c>.</param>
        /// <param name="column0">Receives column <c>j</c>.</param>
        /// <param name="column4">Receives column <c>4 + j</c>.</param>
        /// <param name="column8">Receives column <c>8 + j</c>.</param>
        /// <param name="column12">Receives column <c>12 + j</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Gather(
            Vector512<uint> rows0,
            Vector512<uint> rows4,
            Vector512<uint> rows8,
            Vector512<uint> rows12,
            out Vector512<uint> column0,
            out Vector512<uint> column4,
            out Vector512<uint> column8,
            out Vector512<uint> column12)
        {
            // 0x88 takes lanes 0 and 2 of each operand, 0xDD lanes 1 and 3.
            Vector512<uint> evenTop = Avx512F.Shuffle4x128(rows0, rows4, 0x88);
            Vector512<uint> oddTop = Avx512F.Shuffle4x128(rows0, rows4, 0xDD);
            Vector512<uint> evenBottom = Avx512F.Shuffle4x128(rows8, rows12, 0x88);
            Vector512<uint> oddBottom = Avx512F.Shuffle4x128(rows8, rows12, 0xDD);

            column0 = Avx512F.Shuffle4x128(evenTop, evenBottom, 0x88);
            column8 = Avx512F.Shuffle4x128(evenTop, evenBottom, 0xDD);
            column4 = Avx512F.Shuffle4x128(oddTop, oddBottom, 0x88);
            column12 = Avx512F.Shuffle4x128(oddTop, oddBottom, 0xDD);
        }

        /// <summary>
        /// The BLAKE3 mixing function <c>G</c>, applied to one word of sixteen inputs at once.
        /// </summary>
        /// <param name="a">The first working word.</param>
        /// <param name="b">The second working word.</param>
        /// <param name="c">The third working word.</param>
        /// <param name="d">The fourth working word.</param>
        /// <param name="x">The first message word.</param>
        /// <param name="y">The second message word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(ref Vector512<uint> a, ref Vector512<uint> b, ref Vector512<uint> c, ref Vector512<uint> d, Vector512<uint> x, Vector512<uint> y)
        {
            a += b + x;
            d = Avx512F.RotateRight(d ^ a, 16);
            c += d;
            b = Avx512F.RotateRight(b ^ c, 12);
            a += b + y;
            d = Avx512F.RotateRight(d ^ a, 8);
            c += d;
            b = Avx512F.RotateRight(b ^ c, 7);
        }

        /// <summary>
        /// Returns the transposed message word a round's schedule names.
        /// </summary>
        /// <param name="message">The first of the sixteen transposed message words.</param>
        /// <param name="schedule">The first of the round's sixteen schedule entries.</param>
        /// <param name="k">The index of the schedule entry.</param>
        /// <returns>The message word, for all sixteen inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<uint> Word(ref Vector512<uint> message, ref byte schedule, int k) =>
            Unsafe.Add(ref message, Unsafe.Add(ref schedule, k));

        /// <summary>
        /// Writes one input's chaining value: the low half of its transposed row.
        /// </summary>
        /// <param name="row">The input's transposed row, its chaining value in the low eight words.</param>
        /// <param name="output">The first byte of the chaining values.</param>
        /// <param name="lane">The input's index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Store(Vector512<uint> row, ref byte output, int lane) =>
            row.GetLower().AsByte().StoreUnsafe(ref Unsafe.Add(ref output, lane * ChainingValueBytes));
    }
}
