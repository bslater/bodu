// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Core.Vector128.Variants.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal partial struct Poly1305Core
{
    /// <content> The variants of the AdvSimd kernel's step that F11 of <c>plans/crypto-performance-followups.md</c>
    /// measures against it on hosted ARM64 hardware: the step's constants formed once, before the loop, with the 5 that
    /// folds the carry out of 2^130 back in read from a spare lane of a multiplier's register; and, on top of that, the
    /// blocks split into limbs in general registers. Each runs through its own kernel kind until the measurement picks
    /// one. </content>
    private static partial class Vector128Kernel
    {
        /// <summary>
        /// Splits the four blocks of a step into five 26-bit limbs each, one block to each 32-bit lane, with the 2^128
        /// bit a full block sets: the first group's two blocks in the lower halves and the second group's in the upper.
        /// </summary>
        internal unsafe interface IStepSplit
        {
            /// <summary>
            /// Splits the four blocks at <paramref name="blocks" />.
            /// </summary>
            /// <param name="blocks">The first byte of the step's 64 bytes, pinned.</param>
            /// <param name="mask">The 26-bit mask in every 32-bit lane.</param>
            /// <param name="fullBlockBit">The 2^128 bit, as it falls in the top limb, in every 32-bit lane.</param>
            /// <param name="m0">Receives the blocks' limbs at 2^0.</param>
            /// <param name="m1">Receives the blocks' limbs at 2^26.</param>
            /// <param name="m2">Receives the blocks' limbs at 2^52.</param>
            /// <param name="m3">Receives the blocks' limbs at 2^78.</param>
            /// <param name="m4">Receives the blocks' limbs at 2^104.</param>
            static abstract void Split(
                byte* blocks,
                Vector128<uint> mask,
                Vector128<uint> fullBlockBit,
                out Vector128<uint> m0,
                out Vector128<uint> m1,
                out Vector128<uint> m2,
                out Vector128<uint> m3,
                out Vector128<uint> m4);
        }

        /// <summary>
        /// Absorbs whole groups of two blocks into a core's accumulator as <see cref="Blocks" /> does, with the step's
        /// constants formed once, before the loop, and its blocks split by <typeparamref name="TSplit" />.
        /// </summary>
        /// <typeparam name="TSplit">The split the step uses.</typeparam>
        /// <param name="core">The core whose accumulator absorbs the blocks, under its key half <c>r</c>.</param>
        /// <param name="message">The first byte of the first group.</param>
        /// <param name="groups">The number of groups; at least one.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void BlocksWithSplit<TSplit>(ref Poly1305Core core, ref byte message, int groups)
            where TSplit : struct, IStepSplit
        {
            // r, r² and r⁴; cleared before returning.
            Span<ulong> powers = stackalloc ulong[3 * 5];
            core.ComputePowers(powers);

            // Formed here, outside the loop, so that the step holds them rather than forming them each time. The products
            // read c's first lane alone, so its second holds the 5 the carry multiplies by, which then needs no register
            // of its own.
            Vector128<ulong> mask = Vector128.Create(Mask26);
            Vector128<uint> mask32 = Vector128.Create((uint)Mask26);
            Vector128<uint> fullBlockBit = Vector128.Create((uint)FullBlockBit26);
            Pack(powers[5..], out Vector128<uint> a, out Vector128<uint> b, out Vector128<uint> c);
            Pack(powers[10..], out Vector128<uint> d, out Vector128<uint> e, out Vector128<uint> f);
            c = c.WithElement(1, 5u);
            LoadAccumulator(ref core, out Vector128<ulong> h0, out Vector128<ulong> h1, out Vector128<ulong> h2, out Vector128<ulong> h3, out Vector128<ulong> h4);

            nint offset = 0;
            int group = 0;
            fixed (byte* start = &message)
            {
                for (; groups - group > 2; group += 2)
                {
                    TSplit.Split(start + offset, mask32, fullBlockBit, out Vector128<uint> m0, out Vector128<uint> m1, out Vector128<uint> m2, out Vector128<uint> m3, out Vector128<uint> m4);
                    UpperHalfProducts(m0, m1, m2, m3, m4, a, b, c, out Vector128<ulong> p0, out Vector128<ulong> p1, out Vector128<ulong> p2, out Vector128<ulong> p3, out Vector128<ulong> p4);
                    AddProducts(
                        Narrow(h0) + m0.GetLower(),
                        Narrow(h1) + m1.GetLower(),
                        Narrow(h2) + m2.GetLower(),
                        Narrow(h3) + m3.GetLower(),
                        Narrow(h4) + m4.GetLower(),
                        d,
                        e,
                        f,
                        ref p0,
                        ref p1,
                        ref p2,
                        ref p3,
                        ref p4);
                    h0 = p0;
                    h1 = p1;
                    h2 = p2;
                    h3 = p3;
                    h4 = p4;
                    CarryWithFive(ref h0, ref h1, ref h2, ref h3, ref h4, mask, c);
                    offset += 2 * GroupBytes;
                }
            }

            // One group left before the last, or none.
            if (groups - group == 2)
            {
                AddGroup(ref message, offset, ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                Products(Narrow(h0), Narrow(h1), Narrow(h2), Narrow(h3), Narrow(h4), a, b, c, out h0, out h1, out h2, out h3, out h4);
                Carry(ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                offset += GroupBytes;
            }

            AddLastGroup(ref core, ref message, offset, powers, h0, h1, h2, h3, h4, mask);
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(powers));
        }

        /// <summary>
        /// Brings every limb of each lane back within a few bits of 26 as
        /// <see cref="Carry(ref Vector128{ulong}, ref Vector128{ulong}, ref Vector128{ulong}, ref Vector128{ulong}, ref Vector128{ulong}, Vector128{ulong})" />
        /// does, with the 5 that folds the carry out of 2^130 back in read from a lane of a register the step already
        /// holds, rather than formed.
        /// </summary>
        /// <param name="d0">The lanes' limbs at 2^0.</param>
        /// <param name="d1">The lanes' limbs at 2^26.</param>
        /// <param name="d2">The lanes' limbs at 2^52.</param>
        /// <param name="d3">The lanes' limbs at 2^78.</param>
        /// <param name="d4">The lanes' limbs at 2^104.</param>
        /// <param name="mask">The 26-bit mask in every lane.</param>
        /// <param name="five">A vector whose second 32-bit lane holds 5.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("Performance", "CA1857:A constant is expected for the parameter", Justification = ".NET 8's reference assembly bounds the counts of the 64-bit USRA and SHRN overloads at 16, where the instructions take up to 64 and 32; .NET 8's JIT emits them with 26, and .NET 10's annotations allow it.")]
        private static void CarryWithFive(
            ref Vector128<ulong> d0,
            ref Vector128<ulong> d1,
            ref Vector128<ulong> d2,
            ref Vector128<ulong> d3,
            ref Vector128<ulong> d4,
            Vector128<ulong> mask,
            Vector128<uint> five)
        {
            d1 = AdvSimd.ShiftRightLogicalAdd(d1, d0, 26);
            d4 = AdvSimd.ShiftRightLogicalAdd(d4, d3, 26);
            d0 &= mask;
            d3 &= mask;

            d2 = AdvSimd.ShiftRightLogicalAdd(d2, d1, 26);
            d0 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d0, AdvSimd.ShiftRightLogicalNarrowingLower(d4, 26), five, 1);
            d1 &= mask;
            d4 &= mask;

            d3 = AdvSimd.ShiftRightLogicalAdd(d3, d2, 26);
            d1 = AdvSimd.ShiftRightLogicalAdd(d1, d0, 26);
            d2 &= mask;
            d0 &= mask;

            d4 = AdvSimd.ShiftRightLogicalAdd(d4, d3, 26);
            d3 &= mask;
        }

        /// <summary>
        /// Splits a step's blocks as <see cref="SplitFourBlocks" /> does: four loads, two rounds of <c>UZP1</c> and
        /// <c>UZP2</c>, and the limbs formed with shifts, <c>SLI</c>, masks and <c>USRA</c>, with the constants passed
        /// in.
        /// </summary>
        internal readonly struct UnzipSplit : IStepSplit
        {
            /// <inheritdoc />
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void Split(
                byte* blocks,
                Vector128<uint> mask,
                Vector128<uint> fullBlockBit,
                out Vector128<uint> m0,
                out Vector128<uint> m1,
                out Vector128<uint> m2,
                out Vector128<uint> m3,
                out Vector128<uint> m4)
            {
                Vector128<uint> first = Vector128.Load((uint*)blocks);
                Vector128<uint> second = Vector128.Load((uint*)(blocks + BlockBytes));
                Vector128<uint> third = Vector128.Load((uint*)(blocks + (2 * BlockBytes)));
                Vector128<uint> fourth = Vector128.Load((uint*)(blocks + (3 * BlockBytes)));

                Vector128<uint> even = AdvSimd.Arm64.UnzipEven(first, second);
                Vector128<uint> odd = AdvSimd.Arm64.UnzipOdd(first, second);
                Vector128<uint> evenNext = AdvSimd.Arm64.UnzipEven(third, fourth);
                Vector128<uint> oddNext = AdvSimd.Arm64.UnzipOdd(third, fourth);
                Vector128<uint> w0 = AdvSimd.Arm64.UnzipEven(even, evenNext);
                Vector128<uint> w1 = AdvSimd.Arm64.UnzipEven(odd, oddNext);
                Vector128<uint> w2 = AdvSimd.Arm64.UnzipOdd(even, evenNext);
                Vector128<uint> w3 = AdvSimd.Arm64.UnzipOdd(odd, oddNext);

                Limbs(w0, w1, w2, w3, mask, fullBlockBit, out m0, out m1, out m2, out m3, out m4);
            }
        }

        /// <summary>
        /// Splits a step's blocks in general registers, whose pipes the vector code leaves idle, as OpenSSL does: each
        /// limb of two blocks packed into one 64-bit value, the earlier block in its low half, and two such values
        /// moved into a vector together.
        /// </summary>
        internal readonly struct IntegerSplit : IStepSplit
        {
            /// <inheritdoc />
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void Split(
                byte* blocks,
                Vector128<uint> mask,
                Vector128<uint> fullBlockBit,
                out Vector128<uint> m0,
                out Vector128<uint> m1,
                out Vector128<uint> m2,
                out Vector128<uint> m3,
                out Vector128<uint> m4)
            {
                ulong a0 = Unsafe.ReadUnaligned<ulong>(blocks);
                ulong a1 = Unsafe.ReadUnaligned<ulong>(blocks + 8);
                ulong b0 = Unsafe.ReadUnaligned<ulong>(blocks + BlockBytes);
                ulong b1 = Unsafe.ReadUnaligned<ulong>(blocks + BlockBytes + 8);
                ulong c0 = Unsafe.ReadUnaligned<ulong>(blocks + (2 * BlockBytes));
                ulong c1 = Unsafe.ReadUnaligned<ulong>(blocks + (2 * BlockBytes) + 8);
                ulong e0 = Unsafe.ReadUnaligned<ulong>(blocks + (3 * BlockBytes));
                ulong e1 = Unsafe.ReadUnaligned<ulong>(blocks + (3 * BlockBytes) + 8);

                m0 = Pack(a0 & Mask26, b0 & Mask26, c0 & Mask26, e0 & Mask26);
                m1 = Pack((a0 >> 26) & Mask26, (b0 >> 26) & Mask26, (c0 >> 26) & Mask26, (e0 >> 26) & Mask26);
                m2 = Pack(((a0 >> 52) | (a1 << 12)) & Mask26, ((b0 >> 52) | (b1 << 12)) & Mask26, ((c0 >> 52) | (c1 << 12)) & Mask26, ((e0 >> 52) | (e1 << 12)) & Mask26);
                m3 = Pack((a1 >> 14) & Mask26, (b1 >> 14) & Mask26, (c1 >> 14) & Mask26, (e1 >> 14) & Mask26);
                m4 = Pack((a1 >> 40) | FullBlockBit26, (b1 >> 40) | FullBlockBit26, (c1 >> 40) | FullBlockBit26, (e1 >> 40) | FullBlockBit26);
            }

            /// <summary>
            /// Packs one limb of each of four blocks into the four 32-bit lanes of a vector, in block order.
            /// </summary>
            /// <param name="first">The first block's limb, below 2^32.</param>
            /// <param name="second">The second block's limb, below 2^32.</param>
            /// <param name="third">The third block's limb, below 2^32.</param>
            /// <param name="fourth">The fourth block's limb, below 2^32.</param>
            /// <returns>The four limbs, one to a lane.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<uint> Pack(ulong first, ulong second, ulong third, ulong fourth) =>
                Vector128.Create(first | (second << 32), third | (fourth << 32)).AsUInt32();
        }

        /// <summary>
        /// Forms five 26-bit limbs from the four 32-bit words of each lane's block, with the 2^128 bit a full block
        /// sets.
        /// </summary>
        /// <param name="w0">The blocks' words at 2^0.</param>
        /// <param name="w1">The blocks' words at 2^32.</param>
        /// <param name="w2">The blocks' words at 2^64.</param>
        /// <param name="w3">The blocks' words at 2^96.</param>
        /// <param name="mask">The 26-bit mask in every 32-bit lane.</param>
        /// <param name="fullBlockBit">The 2^128 bit, as it falls in the top limb, in every 32-bit lane.</param>
        /// <param name="m0">Receives the limbs at 2^0.</param>
        /// <param name="m1">Receives the limbs at 2^26.</param>
        /// <param name="m2">Receives the limbs at 2^52.</param>
        /// <param name="m3">Receives the limbs at 2^78.</param>
        /// <param name="m4">Receives the limbs at 2^104.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Limbs(
            Vector128<uint> w0,
            Vector128<uint> w1,
            Vector128<uint> w2,
            Vector128<uint> w3,
            Vector128<uint> mask,
            Vector128<uint> fullBlockBit,
            out Vector128<uint> m0,
            out Vector128<uint> m1,
            out Vector128<uint> m2,
            out Vector128<uint> m3,
            out Vector128<uint> m4)
        {
            m0 = w0 & mask;
            m1 = AdvSimd.ShiftLeftAndInsert(w0 >>> 26, w1, 6) & mask;
            m2 = AdvSimd.ShiftLeftAndInsert(w1 >>> 20, w2, 12) & mask;
            m3 = AdvSimd.ShiftLeftAndInsert(w2 >>> 14, w3, 18) & mask;
            m4 = AdvSimd.ShiftRightLogicalAdd(fullBlockBit, w3, 8);
        }
    }
}
