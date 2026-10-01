// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.Vector128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class SerpentCore
{
    /// <summary>
    /// Provides the Serpent-128 rounds over four blocks at once, one block to each lane of a 128-bit vector, generic
    /// over the instruction set that performs the rotations.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set that performs the rotations: <see cref="VectorRotation.Ssse3" /> or
    /// <see cref="VectorRotation.Avx512" /> on x64, <see cref="VectorRotation.AdvSimd" /> on ARM64.
    /// </typeparam>
    /// <remarks>
    /// Four blocks load as four vectors, one block each, and a 4×4 transpose turns them into four vectors that each
    /// hold one word of all four blocks: the bitsliced form the S-box circuits and the linear transform work on, with
    /// each round-key word broadcast to every lane. A second transpose returns the blocks for storing. The rounds and
    /// circuits repeat the scalar ones in <see cref="SerpentCore" /> over vectors; the language offers no way to write
    /// them once for both that the JIT keeps inlined.
    /// </remarks>
    internal static class Vector128Kernel<TIsa>
        where TIsa : struct, IVector128Rotation
    {
        /// <summary>The number of bytes in a group of four blocks.</summary>
        private const int GroupBytes = 4 * BlockBytes;

        /// <summary>
        /// Encrypts groups of four blocks.
        /// </summary>
        /// <param name="roundKeys">The first of the 132 round-key words.</param>
        /// <param name="input">The first byte of the plaintext.</param>
        /// <param name="output">The first byte of the destination; it may be the input's first byte.</param>
        /// <param name="groups">The number of groups of four blocks.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void EncryptBlocks(ref uint roundKeys, ref byte input, ref byte output, int groups)
        {
            for (nint offset = 0, end = (nint)groups * GroupBytes; offset < end; offset += GroupBytes)
            {
                Load(ref input, offset, out Vector128<uint> x0, out Vector128<uint> x1, out Vector128<uint> x2, out Vector128<uint> x3);
                EncryptRounds(ref x0, ref x1, ref x2, ref x3, ref roundKeys);
                Store(x0, x1, x2, x3, ref output, offset);
            }
        }

        /// <summary>
        /// Decrypts groups of four blocks.
        /// </summary>
        /// <param name="roundKeys">The first of the 132 round-key words.</param>
        /// <param name="input">The first byte of the ciphertext.</param>
        /// <param name="output">The first byte of the destination; it may be the input's first byte.</param>
        /// <param name="groups">The number of groups of four blocks.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void DecryptBlocks(ref uint roundKeys, ref byte input, ref byte output, int groups)
        {
            for (nint offset = 0, end = (nint)groups * GroupBytes; offset < end; offset += GroupBytes)
            {
                Load(ref input, offset, out Vector128<uint> x0, out Vector128<uint> x1, out Vector128<uint> x2, out Vector128<uint> x3);
                DecryptRounds(ref x0, ref x1, ref x2, ref x3, ref roundKeys);
                Store(x0, x1, x2, x3, ref output, offset);
            }
        }

        /// <summary>
        /// Combines groups of four blocks of input by XOR with the keystream of four successive counter blocks each,
        /// and advances the counter past them.
        /// </summary>
        /// <param name="roundKeys">The first of the 132 round-key words.</param>
        /// <param name="counterHigh">The counter's high 64 bits; advanced past the blocks used.</param>
        /// <param name="counterLow">The counter's low 64 bits; advanced past the blocks used.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination; it may be the input's first byte.</param>
        /// <param name="groups">The number of groups of four blocks.</param>
        /// <remarks>
        /// The counter blocks never reach memory. Each lane's counter is held as four words in native order, one vector
        /// per word, and a byte reversal turns them into the words Serpent reads from the big-endian block. The
        /// counters advance by four a group with the carries between words propagated by masks, not branches, so the
        /// time taken does not depend on the counter.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void XorCounterBlocks(ref uint roundKeys, ref ulong counterHigh, ref ulong counterLow, ref byte input, ref byte output, int groups)
        {
            Vector128<uint> c0 = Vector128.Create((uint)(counterHigh >> 32));
            Vector128<uint> c1 = Vector128.Create((uint)counterHigh);
            Vector128<uint> c2 = Vector128.Create((uint)(counterLow >> 32));
            Vector128<uint> c3 = Vector128.Create((uint)counterLow);
            AddToCounters(ref c0, ref c1, ref c2, ref c3, Vector128.Create(0u, 1u, 2u, 3u));

            for (nint offset = 0, end = (nint)groups * GroupBytes; offset < end; offset += GroupBytes)
            {
                Vector128<uint> x0 = ReverseBytes(c0);
                Vector128<uint> x1 = ReverseBytes(c1);
                Vector128<uint> x2 = ReverseBytes(c2);
                Vector128<uint> x3 = ReverseBytes(c3);

                EncryptRounds(ref x0, ref x1, ref x2, ref x3, ref roundKeys);
                XorStore(x0, x1, x2, x3, ref input, ref output, offset);
                AddToCounters(ref c0, ref c1, ref c2, ref c3, Vector128.Create(4u));
            }

            AdvanceCounter(ref counterHigh, ref counterLow, (ulong)groups * 4);
        }

        /// <summary>
        /// Adds a value to every lane's 128-bit counter, held as four words in native order, one vector per word from
        /// the most significant, propagating each carry by masks.
        /// </summary>
        /// <param name="c0">The counters' most significant words.</param>
        /// <param name="c1">The counters' second words.</param>
        /// <param name="c2">The counters' third words.</param>
        /// <param name="c3">The counters' least significant words.</param>
        /// <param name="addend">The value added to each lane's counter.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddToCounters(ref Vector128<uint> c0, ref Vector128<uint> c1, ref Vector128<uint> c2, ref Vector128<uint> c3, Vector128<uint> addend)
        {
            c3 += addend;

            // A lane's least significant word carried if its sum is below the addend. A carry is all ones, so it adds
            // one by subtraction, and it carries on from a word it wraps to zero.
            Vector128<uint> carry = Vector128.LessThan(c3, addend);
            c2 -= carry;
            carry &= Vector128.Equals(c2, Vector128<uint>.Zero);
            c1 -= carry;
            carry &= Vector128.Equals(c1, Vector128<uint>.Zero);
            c0 -= carry;
        }

        /// <summary>
        /// Reverses the order of the bytes in each 32-bit lane: one byte shuffle on x64, and one <c>REV32</c> on ARM64.
        /// </summary>
        /// <param name="value">The words.</param>
        /// <returns>The words with their bytes reversed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<uint> ReverseBytes(Vector128<uint> value) =>
            AdvSimd.IsSupported
                ? AdvSimd.ReverseElement8(value)
                : Ssse3.Shuffle(value.AsByte(), Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)).AsUInt32();

        /// <summary>
        /// Transposes four vectors of words back into four blocks and combines them with four blocks of input by XOR.
        /// </summary>
        /// <param name="x0">The first word of each block.</param>
        /// <param name="x1">The second word of each block.</param>
        /// <param name="x2">The third word of each block.</param>
        /// <param name="x3">The fourth word of each block.</param>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the first block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void XorStore(Vector128<uint> x0, Vector128<uint> x1, Vector128<uint> x2, Vector128<uint> x3, ref byte input, ref byte output, nint offset)
        {
            ChaCha20Core.Transpose(ref x0, ref x1, ref x2, ref x3);
            (Vector128.LoadUnsafe(ref input, (nuint)offset) ^ x0.AsByte()).StoreUnsafe(ref output, (nuint)offset);
            (Vector128.LoadUnsafe(ref input, (nuint)(offset + BlockBytes)) ^ x1.AsByte()).StoreUnsafe(ref output, (nuint)(offset + BlockBytes));
            (Vector128.LoadUnsafe(ref input, (nuint)(offset + (2 * BlockBytes))) ^ x2.AsByte()).StoreUnsafe(ref output, (nuint)(offset + (2 * BlockBytes)));
            (Vector128.LoadUnsafe(ref input, (nuint)(offset + (3 * BlockBytes))) ^ x3.AsByte()).StoreUnsafe(ref output, (nuint)(offset + (3 * BlockBytes)));
        }

        /// <summary>
        /// Loads four blocks and transposes them into four vectors of words.
        /// </summary>
        /// <param name="input">The first byte of the input.</param>
        /// <param name="offset">The offset of the first block.</param>
        /// <param name="x0">Receives the first word of each block.</param>
        /// <param name="x1">Receives the second word of each block.</param>
        /// <param name="x2">Receives the third word of each block.</param>
        /// <param name="x3">Receives the fourth word of each block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Load(ref byte input, nint offset, out Vector128<uint> x0, out Vector128<uint> x1, out Vector128<uint> x2, out Vector128<uint> x3)
        {
            x0 = Vector128.LoadUnsafe(ref input, (nuint)offset).AsUInt32();
            x1 = Vector128.LoadUnsafe(ref input, (nuint)(offset + BlockBytes)).AsUInt32();
            x2 = Vector128.LoadUnsafe(ref input, (nuint)(offset + (2 * BlockBytes))).AsUInt32();
            x3 = Vector128.LoadUnsafe(ref input, (nuint)(offset + (3 * BlockBytes))).AsUInt32();
            ChaCha20Core.Transpose(ref x0, ref x1, ref x2, ref x3);
        }

        /// <summary>
        /// Transposes four vectors of words back into four blocks and stores them.
        /// </summary>
        /// <param name="x0">The first word of each block.</param>
        /// <param name="x1">The second word of each block.</param>
        /// <param name="x2">The third word of each block.</param>
        /// <param name="x3">The fourth word of each block.</param>
        /// <param name="output">The first byte of the destination.</param>
        /// <param name="offset">The offset of the first block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Store(Vector128<uint> x0, Vector128<uint> x1, Vector128<uint> x2, Vector128<uint> x3, ref byte output, nint offset)
        {
            ChaCha20Core.Transpose(ref x0, ref x1, ref x2, ref x3);
            x0.AsByte().StoreUnsafe(ref output, (nuint)offset);
            x1.AsByte().StoreUnsafe(ref output, (nuint)(offset + BlockBytes));
            x2.AsByte().StoreUnsafe(ref output, (nuint)(offset + (2 * BlockBytes)));
            x3.AsByte().StoreUnsafe(ref output, (nuint)(offset + (3 * BlockBytes)));
        }

        /// <summary>
        /// Runs the 32 Serpent-128 encryption rounds over the words of every block in the vectors.
        /// </summary>
        /// <param name="x0">The first word of each block.</param>
        /// <param name="x1">The second word of each block.</param>
        /// <param name="x2">The third word of each block.</param>
        /// <param name="x3">The fourth word of each block.</param>
        /// <param name="roundKeys">The first of the 132 round-key words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncryptRounds(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3, ref uint roundKeys)
        {
            for (int r = 0; r < RoundCount; r += 8)
            {
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r);
                S0(ref x0, ref x1, ref x2, ref x3);
                LinearTransform(ref x0, ref x1, ref x2, ref x3);

                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 1);
                S1(ref x0, ref x1, ref x2, ref x3);
                LinearTransform(ref x0, ref x1, ref x2, ref x3);

                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 2);
                S2(ref x0, ref x1, ref x2, ref x3);
                LinearTransform(ref x0, ref x1, ref x2, ref x3);

                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 3);
                S3(ref x0, ref x1, ref x2, ref x3);
                LinearTransform(ref x0, ref x1, ref x2, ref x3);

                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 4);
                S4(ref x0, ref x1, ref x2, ref x3);
                LinearTransform(ref x0, ref x1, ref x2, ref x3);

                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 5);
                S5(ref x0, ref x1, ref x2, ref x3);
                LinearTransform(ref x0, ref x1, ref x2, ref x3);

                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 6);
                S6(ref x0, ref x1, ref x2, ref x3);
                LinearTransform(ref x0, ref x1, ref x2, ref x3);

                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 7);
                S7(ref x0, ref x1, ref x2, ref x3);

                if (r + 8 < RoundCount)
                    LinearTransform(ref x0, ref x1, ref x2, ref x3);
            }

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, RoundCount);
        }

        /// <summary>
        /// Runs the 32 Serpent-128 rounds backwards over the words of every block in the vectors.
        /// </summary>
        /// <param name="x0">The first word of each block.</param>
        /// <param name="x1">The second word of each block.</param>
        /// <param name="x2">The third word of each block.</param>
        /// <param name="x3">The fourth word of each block.</param>
        /// <param name="roundKeys">The first of the 132 round-key words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DecryptRounds(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3, ref uint roundKeys)
        {
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, RoundCount);

            for (int r = RoundCount - 8; r >= 0; r -= 8)
            {
                if (r + 8 < RoundCount)
                    InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);

                I7(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 7);

                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
                I6(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 6);

                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
                I5(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 5);

                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
                I4(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 4);

                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
                I3(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 3);

                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
                I2(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 2);

                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
                I1(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 1);

                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
                I0(ref x0, ref x1, ref x2, ref x3);
                AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r);
            }
        }

        /// <summary>
        /// Applies the Serpent linear transform to the words of every block in the vectors.
        /// </summary>
        /// <param name="x0">The first word of each block.</param>
        /// <param name="x1">The second word of each block.</param>
        /// <param name="x2">The third word of each block.</param>
        /// <param name="x3">The fourth word of each block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LinearTransform(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            x0 = x0.RotateBitsLeftUnchecked<TIsa>(13);
            x2 = x2.RotateBitsLeftUnchecked<TIsa>(3);
            x1 ^= x0 ^ x2;
            x3 ^= x2 ^ Vector128.ShiftLeft(x0, 3);
            x1 = x1.RotateBitsLeftUnchecked<TIsa>(1);
            x3 = x3.RotateBitsLeftUnchecked<TIsa>(7);
            x0 ^= x1 ^ x3;
            x2 ^= x3 ^ Vector128.ShiftLeft(x1, 7);
            x0 = x0.RotateBitsLeftUnchecked<TIsa>(5);
            x2 = x2.RotateBitsLeftUnchecked<TIsa>(22);
        }

        /// <summary>
        /// Applies the inverse of the Serpent linear transform to the words of every block in the vectors.
        /// </summary>
        /// <param name="x0">The first word of each block.</param>
        /// <param name="x1">The second word of each block.</param>
        /// <param name="x2">The third word of each block.</param>
        /// <param name="x3">The fourth word of each block.</param>
        /// <remarks>
        /// Each right rotation by <c>n</c> is a left rotation by <c>32 − n</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InverseLinearTransform(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            x2 = x2.RotateBitsLeftUnchecked<TIsa>(32 - 22);
            x0 = x0.RotateBitsLeftUnchecked<TIsa>(32 - 5);
            x2 ^= x3 ^ Vector128.ShiftLeft(x1, 7);
            x0 ^= x1 ^ x3;
            x3 = x3.RotateBitsLeftUnchecked<TIsa>(32 - 7);
            x1 = x1.RotateBitsLeftUnchecked<TIsa>(32 - 1);
            x3 ^= x2 ^ Vector128.ShiftLeft(x0, 3);
            x1 ^= x0 ^ x2;
            x2 = x2.RotateBitsLeftUnchecked<TIsa>(32 - 3);
            x0 = x0.RotateBitsLeftUnchecked<TIsa>(32 - 13);
        }

        /// <summary>
        /// Adds a round key to the words of every block in the vectors by XOR, each key word broadcast to every lane.
        /// </summary>
        /// <param name="x0">The first word of each block.</param>
        /// <param name="x1">The second word of each block.</param>
        /// <param name="x2">The third word of each block.</param>
        /// <param name="x3">The fourth word of each block.</param>
        /// <param name="roundKeys">The first of the round-key words.</param>
        /// <param name="round">The round whose key is added: 0 to 32.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddRoundKey(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3, ref uint roundKeys, int round)
        {
            ref uint key = ref Unsafe.Add(ref roundKeys, round * 4);
            x0 ^= Vector128.Create(key);
            x1 ^= Vector128.Create(Unsafe.Add(ref key, 1));
            x2 ^= Vector128.Create(Unsafe.Add(ref key, 2));
            x3 ^= Vector128.Create(Unsafe.Add(ref key, 3));
        }

        /// <summary>
        /// Applies the Serpent S-box S0 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S0(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r3 ^= r0;
            r4 = r1;
            r1 &= r3;
            r4 ^= r2;
            r1 ^= r0;
            r0 |= r3;
            r0 ^= r4;
            r4 ^= r3;
            r3 ^= r2;
            r2 |= r1;
            r2 ^= r4;
            r4 = ~r4;
            r4 |= r1;
            r1 ^= r3;
            r1 ^= r4;
            r3 |= r0;
            r1 ^= r3;
            r4 ^= r3;

            x0 = r1;
            x1 = r4;
            x2 = r2;
            x3 = r0;
        }

        /// <summary>
        /// Applies the Serpent S-box S1 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S1(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r0 = ~r0;
            r2 = ~r2;
            r4 = r0;
            r0 &= r1;
            r2 ^= r0;
            r0 |= r3;
            r3 ^= r2;
            r1 ^= r0;
            r0 ^= r4;
            r4 |= r1;
            r1 ^= r3;
            r2 |= r0;
            r2 &= r4;
            r0 ^= r1;
            r1 &= r2;
            r1 ^= r0;
            r0 &= r2;
            r0 ^= r4;

            x0 = r2;
            x1 = r0;
            x2 = r3;
            x3 = r1;
        }

        /// <summary>
        /// Applies the Serpent S-box S2 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of fourteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S2(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r4 = r0;
            r0 &= r2;
            r0 ^= r3;
            r2 ^= r1;
            r2 ^= r0;
            r3 |= r4;
            r3 ^= r1;
            r4 ^= r2;
            r1 = r3;
            r3 |= r4;
            r3 ^= r0;
            r0 &= r1;
            r4 ^= r0;
            r1 ^= r3;
            r1 ^= r4;
            r4 = ~r4;

            x0 = r2;
            x1 = r3;
            x2 = r1;
            x3 = r4;
        }

        /// <summary>
        /// Applies the Serpent S-box S3 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S3(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r4 = r0;
            r0 |= r3;
            r3 ^= r1;
            r1 &= r4;
            r4 ^= r2;
            r2 ^= r3;
            r3 &= r0;
            r4 |= r1;
            r3 ^= r4;
            r0 ^= r1;
            r4 &= r0;
            r1 ^= r3;
            r4 ^= r2;
            r1 |= r0;
            r1 ^= r2;
            r0 ^= r3;
            r2 = r1;
            r1 |= r3;
            r1 ^= r0;

            x0 = r1;
            x1 = r2;
            x2 = r3;
            x3 = r4;
        }

        /// <summary>
        /// Applies the Serpent S-box S4 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of nineteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S4(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r1 ^= r3;
            r3 = ~r3;
            r2 ^= r3;
            r3 ^= r0;
            r4 = r1;
            r1 &= r3;
            r1 ^= r2;
            r4 ^= r3;
            r0 ^= r4;
            r2 &= r4;
            r2 ^= r0;
            r0 &= r1;
            r3 ^= r0;
            r4 |= r1;
            r4 ^= r0;
            r0 |= r3;
            r0 ^= r2;
            r2 &= r3;
            r0 = ~r0;
            r4 ^= r2;

            x0 = r1;
            x1 = r4;
            x2 = r0;
            x3 = r3;
        }

        /// <summary>
        /// Applies the Serpent S-box S5 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S5(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r0 ^= r1;
            r1 ^= r3;
            r3 = ~r3;
            r4 = r1;
            r1 &= r0;
            r2 ^= r3;
            r1 ^= r2;
            r2 |= r4;
            r4 ^= r3;
            r3 &= r1;
            r3 ^= r0;
            r4 ^= r1;
            r4 ^= r2;
            r2 ^= r0;
            r0 &= r3;
            r2 = ~r2;
            r0 ^= r4;
            r4 |= r3;
            r2 ^= r4;

            x0 = r1;
            x1 = r3;
            x2 = r0;
            x3 = r2;
        }

        /// <summary>
        /// Applies the Serpent S-box S6 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S6(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r2 = ~r2;
            r4 = r3;
            r3 &= r0;
            r0 ^= r4;
            r3 ^= r2;
            r2 |= r4;
            r1 ^= r3;
            r2 ^= r0;
            r0 |= r1;
            r2 ^= r1;
            r4 ^= r0;
            r0 |= r3;
            r0 ^= r2;
            r4 ^= r3;
            r4 ^= r0;
            r3 = ~r3;
            r2 &= r4;
            r2 ^= r3;

            x0 = r0;
            x1 = r1;
            x2 = r4;
            x3 = r2;
        }

        /// <summary>
        /// Applies the Serpent S-box S7 to the same four words of every block in the vectors in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of nineteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void S7(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r4 = r2;
            r2 &= r1;
            r2 ^= r3;
            r3 &= r1;
            r4 ^= r2;
            r2 ^= r1;
            r1 ^= r0;
            r0 |= r4;
            r0 ^= r2;
            r3 ^= r1;
            r2 ^= r3;
            r3 &= r0;
            r3 ^= r4;
            r4 ^= r2;
            r2 &= r0;
            r4 = ~r4;
            r2 ^= r4;
            r4 &= r0;
            r1 ^= r3;
            r4 ^= r1;

            x0 = r2;
            x1 = r4;
            x2 = r3;
            x3 = r0;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S0 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I0(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r2 = ~r2;
            r4 = r1;
            r1 |= r0;
            r4 = ~r4;
            r1 ^= r2;
            r2 |= r4;
            r1 ^= r3;
            r0 ^= r4;
            r2 ^= r0;
            r0 &= r3;
            r4 ^= r0;
            r0 |= r1;
            r0 ^= r2;
            r3 ^= r4;
            r2 ^= r1;
            r3 ^= r0;
            r3 ^= r1;
            r2 &= r3;
            r4 ^= r2;

            x0 = r0;
            x1 = r4;
            x2 = r1;
            x3 = r3;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S1 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I1(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r4 = r1;
            r1 ^= r3;
            r3 &= r1;
            r4 ^= r2;
            r3 ^= r0;
            r0 |= r1;
            r2 ^= r3;
            r0 ^= r4;
            r0 |= r2;
            r1 ^= r3;
            r0 ^= r1;
            r1 |= r3;
            r1 ^= r0;
            r4 = ~r4;
            r4 ^= r1;
            r1 |= r0;
            r1 ^= r0;
            r1 |= r4;
            r3 ^= r1;

            x0 = r4;
            x1 = r0;
            x2 = r3;
            x3 = r2;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S2 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I2(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r2 ^= r3;
            r3 ^= r0;
            r4 = r3;
            r3 &= r2;
            r3 ^= r1;
            r1 |= r2;
            r1 ^= r4;
            r4 &= r3;
            r2 ^= r3;
            r4 &= r0;
            r4 ^= r2;
            r2 &= r1;
            r2 |= r0;
            r3 = ~r3;
            r2 ^= r3;
            r0 ^= r3;
            r0 &= r1;
            r3 ^= r4;
            r3 ^= r0;

            x0 = r1;
            x1 = r4;
            x2 = r2;
            x3 = r3;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S3 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I3(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r4 = r2;
            r2 ^= r1;
            r1 &= r2;
            r1 ^= r0;
            r0 &= r4;
            r4 ^= r3;
            r3 |= r1;
            r3 ^= r2;
            r0 ^= r4;
            r2 ^= r0;
            r0 |= r3;
            r0 ^= r1;
            r4 ^= r2;
            r2 &= r3;
            r1 |= r3;
            r1 ^= r2;
            r4 ^= r0;
            r2 ^= r4;

            x0 = r3;
            x1 = r0;
            x2 = r2;
            x3 = r1;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S4 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of nineteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I4(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r4 = r2;
            r2 &= r3;
            r2 ^= r1;
            r1 |= r3;
            r1 &= r0;
            r4 ^= r2;
            r4 ^= r1;
            r1 &= r2;
            r0 = ~r0;
            r3 ^= r4;
            r1 ^= r3;
            r3 &= r0;
            r3 ^= r2;
            r0 ^= r1;
            r2 &= r0;
            r3 ^= r0;
            r2 ^= r4;
            r2 |= r3;
            r3 ^= r0;
            r2 ^= r1;

            x0 = r0;
            x1 = r3;
            x2 = r2;
            x3 = r4;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S5 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I5(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r1 = ~r1;
            r4 = r3;
            r2 ^= r1;
            r3 |= r0;
            r3 ^= r2;
            r2 |= r1;
            r2 &= r0;
            r4 ^= r3;
            r2 ^= r4;
            r4 |= r0;
            r4 ^= r1;
            r1 &= r2;
            r1 ^= r3;
            r4 ^= r2;
            r3 &= r4;
            r4 ^= r1;
            r3 ^= r0;
            r3 ^= r4;
            r4 = ~r4;

            x0 = r1;
            x1 = r4;
            x2 = r3;
            x3 = r2;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S6 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of sixteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I6(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r0 ^= r2;
            r4 = r2;
            r2 &= r0;
            r4 ^= r3;
            r2 = ~r2;
            r3 ^= r1;
            r2 ^= r3;
            r4 |= r0;
            r0 ^= r2;
            r3 ^= r4;
            r4 ^= r1;
            r1 &= r3;
            r1 ^= r0;
            r0 ^= r3;
            r0 |= r2;
            r3 ^= r1;
            r4 ^= r0;

            x0 = r1;
            x1 = r2;
            x2 = r4;
            x3 = r3;
        }

        /// <summary>
        /// Applies the inverse of the Serpent S-box S7 to the same four words of every block in the vectors in
        /// bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        /// <remarks>
        /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void I7(ref Vector128<uint> x0, ref Vector128<uint> x1, ref Vector128<uint> x2, ref Vector128<uint> x3)
        {
            Vector128<uint> r0 = x0;
            Vector128<uint> r1 = x1;
            Vector128<uint> r2 = x2;
            Vector128<uint> r3 = x3;
            Vector128<uint> r4;

            r4 = r2;
            r2 ^= r0;
            r0 &= r3;
            r2 = ~r2;
            r4 |= r3;
            r3 ^= r1;
            r1 |= r0;
            r0 ^= r2;
            r2 &= r4;
            r1 ^= r2;
            r2 ^= r0;
            r0 |= r2;
            r3 &= r4;
            r0 ^= r3;
            r4 ^= r1;
            r3 ^= r4;
            r4 |= r0;
            r3 ^= r2;
            r4 ^= r2;

            x0 = r3;
            x1 = r0;
            x2 = r1;
            x3 = r4;
        }
    }
}
