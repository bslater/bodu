// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Vector128.cs" company="Bodu Pty. Ltd.">
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
    /// The 128-bit implementation of the BLAKE3 compression function, written once over <see cref="Vector128{T}" /> and
    /// specialized for an instruction set by <typeparamref name="TIsa" />.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set supplying the rotations of <c>G</c> and the lane rotations. BLAKE3's <c>G</c> is BLAKE2s's,
    /// so the BLAKE2s shims serve both.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="Compress" /> compresses one block: the working vector is held as four rows of four words, so lane
    /// <c>i</c> of the rows is column <c>i</c> and one vector <c>G</c> mixes all four columns. Rotating the first row
    /// by three lanes and the third and fourth by one and two turns the diagonals into columns for the second half of a
    /// round, lane <c>i</c> holding the diagonal through word <c>i</c> of the second row, and rotating them back
    /// restores the rows. The second row stays in place: it is the row that each half of a round computes last and that
    /// the next half reads first, so no rotation stands between the two. Each round's message words are gathered
    /// straight from the block as the schedule names them, the diagonal half's in the order its lanes hold the
    /// diagonals.
    /// </para>
    /// <para>
    /// <see cref="HashMany" /> compresses up to four inputs at once, one to a lane, as the eight-way kernel does eight:
    /// each working-vector word is a vector holding that word for all four inputs, and 4×4 transposes carry the message
    /// words in and the chaining values out. The transposed message is its only copy of the input; it lives on the
    /// stack and is cleared before the method returns.
    /// </para>
    /// </remarks>
    internal readonly struct Vector128Kernel<TIsa>
        where TIsa : struct, Blake2sCore.IVector128Isa
    {
        /// <summary>
        /// Compresses one block into a chaining value.
        /// </summary>
        /// <param name="cv">The first of the eight chaining-value words, replaced in place.</param>
        /// <param name="block">The first byte of the 64-byte block.</param>
        /// <param name="counter">The chunk counter.</param>
        /// <param name="blockLength">The number of message bytes in the block.</param>
        /// <param name="flags">The domain-separation flags.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Compress(ref uint cv, ref byte block, ulong counter, uint blockLength, uint flags)
        {
            Vector128<uint> a = Vector128.LoadUnsafe(ref cv);
            Vector128<uint> b = Vector128.LoadUnsafe(ref cv, 4);
            Vector128<uint> c = Vector128.Create(Iv0, Iv1, Iv2, Iv3);
            Vector128<uint> d = Vector128.Create((uint)counter, (uint)(counter >> 32), blockLength, flags);

            // A loop over the rounds keeps the method within the JIT's inlining budget: with every round written out, the
            // later rounds' Round and Load calls stay calls, and the rows spill.
            ref byte schedule = ref MemoryMarshal.GetReference(MessageSchedule);
            for (int round = 0; round < 7; round++)
            {
                ref byte s = ref Unsafe.Add(ref schedule, round * 16);

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

            (a ^ c).StoreUnsafe(ref cv);
            (b ^ d).StoreUnsafe(ref cv, 4);
        }

        /// <summary>
        /// Compresses up to four equally spaced inputs of whole blocks, each from the key to its chaining value.
        /// </summary>
        /// <param name="input">The first byte of the first input.</param>
        /// <param name="stride">The distance, in bytes, between the starts of consecutive inputs.</param>
        /// <param name="lanes">The number of inputs, from 1 to 4.</param>
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

            ulong step = incrementCounter ? 1UL : 0UL;
            Vector128<uint> counterLow = Vector128.Create(
                (uint)counter,
                (uint)(counter + step),
                (uint)(counter + (2 * step)),
                (uint)(counter + (3 * step)));
            Vector128<uint> counterHigh = Vector128.Create(
                (uint)(counter >> 32),
                (uint)((counter + step) >> 32),
                (uint)((counter + (2 * step)) >> 32),
                (uint)((counter + (3 * step)) >> 32));

            Vector128<uint> h0 = Vector128.Create(key);
            Vector128<uint> h1 = Vector128.Create(Unsafe.Add(ref key, 1));
            Vector128<uint> h2 = Vector128.Create(Unsafe.Add(ref key, 2));
            Vector128<uint> h3 = Vector128.Create(Unsafe.Add(ref key, 3));
            Vector128<uint> h4 = Vector128.Create(Unsafe.Add(ref key, 4));
            Vector128<uint> h5 = Vector128.Create(Unsafe.Add(ref key, 5));
            Vector128<uint> h6 = Vector128.Create(Unsafe.Add(ref key, 6));
            Vector128<uint> h7 = Vector128.Create(Unsafe.Add(ref key, 7));

            Span<Vector128<uint>> message = stackalloc Vector128<uint>[16];
            ref Vector128<uint> m = ref MemoryMarshal.GetReference(message);
            ref byte schedule = ref MemoryMarshal.GetReference(MessageSchedule);

            for (int block = 0; block < blocks; block++)
            {
                LoadMessage(ref Unsafe.Add(ref input, block * BlockBytes), offset1, offset2, offset3, ref m);

                uint blockFlags = flags;
                if (block == 0) blockFlags |= flagsStart;
                if (block == blocks - 1) blockFlags |= flagsEnd;

                Vector128<uint> v0 = h0;
                Vector128<uint> v1 = h1;
                Vector128<uint> v2 = h2;
                Vector128<uint> v3 = h3;
                Vector128<uint> v4 = h4;
                Vector128<uint> v5 = h5;
                Vector128<uint> v6 = h6;
                Vector128<uint> v7 = h7;
                Vector128<uint> v8 = Vector128.Create(Iv0);
                Vector128<uint> v9 = Vector128.Create(Iv1);
                Vector128<uint> v10 = Vector128.Create(Iv2);
                Vector128<uint> v11 = Vector128.Create(Iv3);
                Vector128<uint> v12 = counterLow;
                Vector128<uint> v13 = counterHigh;
                Vector128<uint> v14 = Vector128.Create((uint)BlockBytes);
                Vector128<uint> v15 = Vector128.Create(blockFlags);

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

            // Row i of each transposed half is the matching half of lane i's chaining value.
            TIsa.Transpose(ref h0, ref h1, ref h2, ref h3);
            TIsa.Transpose(ref h4, ref h5, ref h6, ref h7);

            Store(h0, h4, ref output, 0);
            if (lanes > 1) Store(h1, h5, ref output, 1);
            if (lanes > 2) Store(h2, h6, ref output, 2);
            if (lanes > 3) Store(h3, h7, ref output, 3);
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

            a = TIsa.RotateLanes3(a);
            c = TIsa.RotateLanes1(c);
            d = TIsa.RotateLanes2(d);

            G(ref a, ref b, ref c, ref d, diagonalX, diagonalY);

            a = TIsa.RotateLanes1(a);
            c = TIsa.RotateLanes3(c);
            d = TIsa.RotateLanes2(d);
        }

        /// <summary>
        /// The BLAKE3 mixing function <c>G</c>, applied to four columns (or diagonals) at once.
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
        /// <param name="schedule">The first of the round's sixteen schedule entries.</param>
        /// <param name="k0">The schedule entry naming the word for lane 0.</param>
        /// <param name="k1">The schedule entry naming the word for lane 1.</param>
        /// <param name="k2">The schedule entry naming the word for lane 2.</param>
        /// <param name="k3">The schedule entry naming the word for lane 3.</param>
        /// <returns>The four words.</returns>
        /// <remarks>
        /// Where SSE4.1 is available the vector is built a word at a time, so that each word's load folds into the
        /// <c>vmovd</c> or <c>vpinsrd</c> that places it. Built from four arguments at once, the JIT loads the first
        /// three words into general registers, each with its own index arithmetic, and then moves them across to the
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

        /// <summary>
        /// Loads one block of each of four inputs and transposes it into sixteen vectors, one per message word.
        /// </summary>
        /// <param name="block">The first byte of the first input's block.</param>
        /// <param name="offset1">The offset of the second input's block from the first's.</param>
        /// <param name="offset2">The offset of the third input's block from the first's.</param>
        /// <param name="offset3">The offset of the fourth input's block from the first's.</param>
        /// <param name="message">The first of the sixteen vectors that receive the message words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadMessage(ref byte block, nint offset1, nint offset2, nint offset3, ref Vector128<uint> message)
        {
            for (nint quarter = 0; quarter < BlockBytes; quarter += 16)
            {
                Vector128<uint> row0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref block, quarter)).AsUInt32();
                Vector128<uint> row1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref block, offset1 + quarter)).AsUInt32();
                Vector128<uint> row2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref block, offset2 + quarter)).AsUInt32();
                Vector128<uint> row3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref block, offset3 + quarter)).AsUInt32();

                TIsa.Transpose(ref row0, ref row1, ref row2, ref row3);

                ref Vector128<uint> words = ref Unsafe.Add(ref message, quarter / sizeof(uint));
                words = row0;
                Unsafe.Add(ref words, 1) = row1;
                Unsafe.Add(ref words, 2) = row2;
                Unsafe.Add(ref words, 3) = row3;
            }
        }

        /// <summary>
        /// Returns the transposed message word a round's schedule names.
        /// </summary>
        /// <param name="message">The first of the sixteen transposed message words.</param>
        /// <param name="schedule">The first of the round's sixteen schedule entries.</param>
        /// <param name="k">The index of the schedule entry.</param>
        /// <returns>The message word, for all four inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<uint> Word(ref Vector128<uint> message, ref byte schedule, int k) =>
            Unsafe.Add(ref message, Unsafe.Add(ref schedule, k));

        /// <summary>
        /// Writes one input's chaining value.
        /// </summary>
        /// <param name="low">The chaining value's first four words.</param>
        /// <param name="high">The chaining value's last four words.</param>
        /// <param name="output">The first byte of the chaining values.</param>
        /// <param name="lane">The input's index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Store(Vector128<uint> low, Vector128<uint> high, ref byte output, int lane)
        {
            ref byte destination = ref Unsafe.Add(ref output, lane * ChainingValueBytes);
            low.AsByte().StoreUnsafe(ref destination);
            high.AsByte().StoreUnsafe(ref Unsafe.Add(ref destination, 16));
        }
    }
}
