// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Core.Vector256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal partial struct Poly1305Core
{
    /// <summary>
    /// Absorbs groups of four blocks with AVX2: one block in each 64-bit lane of 256-bit vectors, as five 26-bit limbs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unpacking a group's 64 bytes leaves its blocks in the lanes in the order b1, b3, b2, b4. After every group but
    /// the last, each lane is multiplied by <c>r⁴</c>; after the last, the lanes are multiplied by <c>r⁴</c>, <c>r²</c>,
    /// <c>r³</c> and <c>r</c> in turn, so that every block ends up multiplied by the power of <c>r</c> the scalar loop
    /// would have given it. The accumulator enters in the first lane, with the first block, and the four lanes are
    /// summed back into it.
    /// </para>
    /// <para>
    /// A product of two limbs is one <c>vpmuludq</c>, 32 × 32 → 64 bits. The limb sums stay far below 2^64, even with
    /// two groups' products added together, so one round of carries is enough to bring every limb back within a few
    /// bits of 26.
    /// </para>
    /// <para>
    /// There are two loops. <see cref="Blocks" /> takes one group at a time, and each group's multiplication waits on
    /// the carries of the one before. <see cref="BlocksPaired" /> takes two at a time, as <c>(h + m)·r⁸ + m′·r⁴</c>:
    /// the second group's products depend only on the message, so they fill the multiplier while the first group's
    /// wait, and the two share one round of carries. The paired loop holds about twice as many vectors, which fit in
    /// the 32 registers AVX-512 provides but spill from AVX2's 16, so dispatch takes it only where AVX-512VL is
    /// available.
    /// </para>
    /// </remarks>
    private static class Vector256Kernel
    {
        /// <summary>The number of blocks in a group: one for each 64-bit lane.</summary>
        private const int Lanes = 4;

        /// <summary>The number of bytes in a group.</summary>
        private const int GroupBytes = Lanes * BlockBytes;

        /// <summary>
        /// Absorbs whole groups of four blocks into a core's accumulator, one group at a time.
        /// </summary>
        /// <param name="core">The core whose accumulator absorbs the blocks, under its key half <c>r</c>.</param>
        /// <param name="message">The first byte of the first group.</param>
        /// <param name="groups">The number of groups; at least one.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Blocks(ref Poly1305Core core, ref byte message, int groups)
        {
            // r to r⁴; cleared before returning.
            Span<ulong> powers = stackalloc ulong[Lanes * 5];
            core.ComputePowers(powers);

            Vector256<ulong> mask = Vector256.Create(Mask26);

            // r⁴ in every lane, and five times its upper limbs.
            Vector256<ulong> r0 = Vector256.Create(powers[15]);
            Vector256<ulong> r1 = Vector256.Create(powers[16]);
            Vector256<ulong> r2 = Vector256.Create(powers[17]);
            Vector256<ulong> r3 = Vector256.Create(powers[18]);
            Vector256<ulong> r4 = Vector256.Create(powers[19]);
            Vector256<ulong> s1 = Times5(r1);
            Vector256<ulong> s2 = Times5(r2);
            Vector256<ulong> s3 = Times5(r3);
            Vector256<ulong> s4 = Times5(r4);

            core.GetAccumulator(out ulong l0, out ulong l1, out ulong l2, out ulong l3, out ulong l4);
            Vector256<ulong> h0 = Vector256.CreateScalar(l0);
            Vector256<ulong> h1 = Vector256.CreateScalar(l1);
            Vector256<ulong> h2 = Vector256.CreateScalar(l2);
            Vector256<ulong> h3 = Vector256.CreateScalar(l3);
            Vector256<ulong> h4 = Vector256.CreateScalar(l4);

            nint offset = 0;
            for (int group = 1; group < groups; group++)
            {
                AddGroup(ref message, offset, ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                Multiply(ref h0, ref h1, ref h2, ref h3, ref h4, r0, r1, r2, r3, r4, s1, s2, s3, s4, mask);
                offset += GroupBytes;
            }

            AddLastGroup(ref core, ref message, offset, powers, h0, h1, h2, h3, h4, mask);
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(powers));
        }

        /// <summary>
        /// Absorbs whole groups of four blocks into a core's accumulator, two groups at a time while more than two
        /// remain.
        /// </summary>
        /// <param name="core">The core whose accumulator absorbs the blocks, under its key half <c>r</c>.</param>
        /// <param name="message">The first byte of the first group.</param>
        /// <param name="groups">The number of groups; at least one.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void BlocksPaired(ref Poly1305Core core, ref byte message, int groups)
        {
            // r to r⁴, then r⁸; cleared before returning.
            Span<ulong> powers = stackalloc ulong[(Lanes + 1) * 5];
            core.ComputePowers(powers);

            Vector256<ulong> mask = Vector256.Create(Mask26);

            // r⁴ and r⁸ in every lane, and five times their upper limbs.
            Vector256<ulong> r0 = Vector256.Create(powers[15]);
            Vector256<ulong> r1 = Vector256.Create(powers[16]);
            Vector256<ulong> r2 = Vector256.Create(powers[17]);
            Vector256<ulong> r3 = Vector256.Create(powers[18]);
            Vector256<ulong> r4 = Vector256.Create(powers[19]);
            Vector256<ulong> s1 = Times5(r1);
            Vector256<ulong> s2 = Times5(r2);
            Vector256<ulong> s3 = Times5(r3);
            Vector256<ulong> s4 = Times5(r4);
            Vector256<ulong> q0 = Vector256.Create(powers[20]);
            Vector256<ulong> q1 = Vector256.Create(powers[21]);
            Vector256<ulong> q2 = Vector256.Create(powers[22]);
            Vector256<ulong> q3 = Vector256.Create(powers[23]);
            Vector256<ulong> q4 = Vector256.Create(powers[24]);
            Vector256<ulong> t1 = Times5(q1);
            Vector256<ulong> t2 = Times5(q2);
            Vector256<ulong> t3 = Times5(q3);
            Vector256<ulong> t4 = Times5(q4);

            core.GetAccumulator(out ulong l0, out ulong l1, out ulong l2, out ulong l3, out ulong l4);
            Vector256<ulong> h0 = Vector256.CreateScalar(l0);
            Vector256<ulong> h1 = Vector256.CreateScalar(l1);
            Vector256<ulong> h2 = Vector256.CreateScalar(l2);
            Vector256<ulong> h3 = Vector256.CreateScalar(l3);
            Vector256<ulong> h4 = Vector256.CreateScalar(l4);

            // (h + m)·r⁸ + m′·r⁴, whose second half does not wait on h.
            nint offset = 0;
            int group = 0;
            for (; groups - group > 2; group += 2)
            {
                AddGroup(ref message, offset, ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                SplitGroup(ref message, offset + GroupBytes, out Vector256<ulong> m0, out Vector256<ulong> m1, out Vector256<ulong> m2, out Vector256<ulong> m3, out Vector256<ulong> m4, mask);
                Products(h0, h1, h2, h3, h4, q0, q1, q2, q3, q4, t1, t2, t3, t4, out Vector256<ulong> d0, out Vector256<ulong> d1, out Vector256<ulong> d2, out Vector256<ulong> d3, out Vector256<ulong> d4);
                Products(m0, m1, m2, m3, m4, r0, r1, r2, r3, r4, s1, s2, s3, s4, out Vector256<ulong> e0, out Vector256<ulong> e1, out Vector256<ulong> e2, out Vector256<ulong> e3, out Vector256<ulong> e4);
                h0 = d0 + e0;
                h1 = d1 + e1;
                h2 = d2 + e2;
                h3 = d3 + e3;
                h4 = d4 + e4;
                Carry(ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                offset += 2 * GroupBytes;
            }

            // One group left before the last, or none.
            if (groups - group == 2)
            {
                AddGroup(ref message, offset, ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                Multiply(ref h0, ref h1, ref h2, ref h3, ref h4, r0, r1, r2, r3, r4, s1, s2, s3, s4, mask);
                offset += GroupBytes;
            }

            AddLastGroup(ref core, ref message, offset, powers, h0, h1, h2, h3, h4, mask);
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(powers));
        }

        /// <summary>
        /// Absorbs the last group, multiplying each lane by the power of <c>r</c> its block needs, and sums the lanes
        /// back into the core's accumulator.
        /// </summary>
        /// <param name="core">The core whose accumulator receives the lanes' sum.</param>
        /// <param name="message">The first byte of the message.</param>
        /// <param name="offset">The offset of the last group's 64 bytes.</param>
        /// <param name="powers">The powers <c>r</c> to <c>r⁴</c>, five limbs each.</param>
        /// <param name="h0">The lanes' limbs at 2^0.</param>
        /// <param name="h1">The lanes' limbs at 2^26.</param>
        /// <param name="h2">The lanes' limbs at 2^52.</param>
        /// <param name="h3">The lanes' limbs at 2^78.</param>
        /// <param name="h4">The lanes' limbs at 2^104.</param>
        /// <param name="mask">The 26-bit mask in every lane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddLastGroup(
            ref Poly1305Core core,
            ref byte message,
            nint offset,
            ReadOnlySpan<ulong> powers,
            Vector256<ulong> h0,
            Vector256<ulong> h1,
            Vector256<ulong> h2,
            Vector256<ulong> h3,
            Vector256<ulong> h4,
            Vector256<ulong> mask)
        {
            // In the lane order b1, b3, b2, b4: r⁴, r², r³ and r.
            Vector256<ulong> r0 = Vector256.Create(powers[15], powers[5], powers[10], powers[0]);
            Vector256<ulong> r1 = Vector256.Create(powers[16], powers[6], powers[11], powers[1]);
            Vector256<ulong> r2 = Vector256.Create(powers[17], powers[7], powers[12], powers[2]);
            Vector256<ulong> r3 = Vector256.Create(powers[18], powers[8], powers[13], powers[3]);
            Vector256<ulong> r4 = Vector256.Create(powers[19], powers[9], powers[14], powers[4]);
            AddGroup(ref message, offset, ref h0, ref h1, ref h2, ref h3, ref h4, mask);
            Multiply(ref h0, ref h1, ref h2, ref h3, ref h4, r0, r1, r2, r3, r4, Times5(r1), Times5(r2), Times5(r3), Times5(r4), mask);

            core.SetAccumulator(Vector256.Sum(h0), Vector256.Sum(h1), Vector256.Sum(h2), Vector256.Sum(h3), Vector256.Sum(h4));
        }

        /// <summary>
        /// Adds a group of four blocks to the lanes, each block as five 26-bit limbs with the 2^128 bit a full block
        /// sets.
        /// </summary>
        /// <param name="message">The first byte of the message.</param>
        /// <param name="offset">The offset of the group's 64 bytes.</param>
        /// <param name="h0">The lanes' limbs at 2^0.</param>
        /// <param name="h1">The lanes' limbs at 2^26.</param>
        /// <param name="h2">The lanes' limbs at 2^52.</param>
        /// <param name="h3">The lanes' limbs at 2^78.</param>
        /// <param name="h4">The lanes' limbs at 2^104.</param>
        /// <param name="mask">The 26-bit mask in every lane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddGroup(
            ref byte message,
            nint offset,
            ref Vector256<ulong> h0,
            ref Vector256<ulong> h1,
            ref Vector256<ulong> h2,
            ref Vector256<ulong> h3,
            ref Vector256<ulong> h4,
            Vector256<ulong> mask)
        {
            SplitGroup(ref message, offset, out Vector256<ulong> m0, out Vector256<ulong> m1, out Vector256<ulong> m2, out Vector256<ulong> m3, out Vector256<ulong> m4, mask);
            h0 += m0;
            h1 += m1;
            h2 += m2;
            h3 += m3;
            h4 += m4;
        }

        /// <summary>
        /// Splits a group of four blocks into five 26-bit limbs each, one block to a lane, with the 2^128 bit a full
        /// block sets.
        /// </summary>
        /// <param name="message">The first byte of the message.</param>
        /// <param name="offset">The offset of the group's 64 bytes.</param>
        /// <param name="m0">Receives the blocks' limbs at 2^0.</param>
        /// <param name="m1">Receives the blocks' limbs at 2^26.</param>
        /// <param name="m2">Receives the blocks' limbs at 2^52.</param>
        /// <param name="m3">Receives the blocks' limbs at 2^78.</param>
        /// <param name="m4">Receives the blocks' limbs at 2^104.</param>
        /// <param name="mask">The 26-bit mask in every lane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SplitGroup(
            ref byte message,
            nint offset,
            out Vector256<ulong> m0,
            out Vector256<ulong> m1,
            out Vector256<ulong> m2,
            out Vector256<ulong> m3,
            out Vector256<ulong> m4,
            Vector256<ulong> mask)
        {
            Vector256<ulong> first = Vector256.LoadUnsafe(ref Unsafe.Add(ref message, offset)).AsUInt64();
            Vector256<ulong> second = Vector256.LoadUnsafe(ref Unsafe.Add(ref message, offset + 32)).AsUInt64();

            // The low and the high eight bytes of each block, within each 128-bit half: b1, b3, then b2, b4.
            Vector256<ulong> low = Avx2.UnpackLow(first, second);
            Vector256<ulong> high = Avx2.UnpackHigh(first, second);

            m0 = low & mask;
            m1 = (low >>> 26) & mask;
            m2 = ((low >>> 52) | (high << 12)) & mask;
            m3 = (high >>> 14) & mask;
            m4 = (high >>> 40) | Vector256.Create(FullBlockBit26);
        }

        /// <summary>
        /// Multiplies each lane by its lane of a multiplier modulo 2^130 − 5, and brings every limb back within a few
        /// bits of 26 with one round of carries.
        /// </summary>
        /// <param name="h0">The lanes' limbs at 2^0, replaced by the product's.</param>
        /// <param name="h1">The lanes' limbs at 2^26, replaced by the product's.</param>
        /// <param name="h2">The lanes' limbs at 2^52, replaced by the product's.</param>
        /// <param name="h3">The lanes' limbs at 2^78, replaced by the product's.</param>
        /// <param name="h4">The lanes' limbs at 2^104, replaced by the product's.</param>
        /// <param name="r0">The multiplier's limbs at 2^0.</param>
        /// <param name="r1">The multiplier's limbs at 2^26.</param>
        /// <param name="r2">The multiplier's limbs at 2^52.</param>
        /// <param name="r3">The multiplier's limbs at 2^78.</param>
        /// <param name="r4">The multiplier's limbs at 2^104.</param>
        /// <param name="s1">Five times <paramref name="r1" />.</param>
        /// <param name="s2">Five times <paramref name="r2" />.</param>
        /// <param name="s3">Five times <paramref name="r3" />.</param>
        /// <param name="s4">Five times <paramref name="r4" />.</param>
        /// <param name="mask">The 26-bit mask in every lane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Multiply(
            ref Vector256<ulong> h0,
            ref Vector256<ulong> h1,
            ref Vector256<ulong> h2,
            ref Vector256<ulong> h3,
            ref Vector256<ulong> h4,
            Vector256<ulong> r0,
            Vector256<ulong> r1,
            Vector256<ulong> r2,
            Vector256<ulong> r3,
            Vector256<ulong> r4,
            Vector256<ulong> s1,
            Vector256<ulong> s2,
            Vector256<ulong> s3,
            Vector256<ulong> s4,
            Vector256<ulong> mask)
        {
            Products(h0, h1, h2, h3, h4, r0, r1, r2, r3, r4, s1, s2, s3, s4, out h0, out h1, out h2, out h3, out h4);
            Carry(ref h0, ref h1, ref h2, ref h3, ref h4, mask);
        }

        /// <summary>
        /// Forms the five limb sums of each lane's product with its lane of a multiplier, before any carry.
        /// </summary>
        /// <param name="h0">The lanes' limbs at 2^0.</param>
        /// <param name="h1">The lanes' limbs at 2^26.</param>
        /// <param name="h2">The lanes' limbs at 2^52.</param>
        /// <param name="h3">The lanes' limbs at 2^78.</param>
        /// <param name="h4">The lanes' limbs at 2^104.</param>
        /// <param name="r0">The multiplier's limbs at 2^0.</param>
        /// <param name="r1">The multiplier's limbs at 2^26.</param>
        /// <param name="r2">The multiplier's limbs at 2^52.</param>
        /// <param name="r3">The multiplier's limbs at 2^78.</param>
        /// <param name="r4">The multiplier's limbs at 2^104.</param>
        /// <param name="s1">Five times <paramref name="r1" />.</param>
        /// <param name="s2">Five times <paramref name="r2" />.</param>
        /// <param name="s3">Five times <paramref name="r3" />.</param>
        /// <param name="s4">Five times <paramref name="r4" />.</param>
        /// <param name="d0">Receives the sums at 2^0.</param>
        /// <param name="d1">Receives the sums at 2^26.</param>
        /// <param name="d2">Receives the sums at 2^52.</param>
        /// <param name="d3">Receives the sums at 2^78.</param>
        /// <param name="d4">Receives the sums at 2^104.</param>
        /// <remarks>
        /// A limb product that lands at 2^130 or above folds back times 5, so the multiplier's upper limbs also serve
        /// five times over, as <paramref name="s1" /> to <paramref name="s4" />. With limbs below 2^27 and multipliers
        /// below 2^29, each sum of five products stays below 2^59, so two such sums can be added before the carries.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Products(
            Vector256<ulong> h0,
            Vector256<ulong> h1,
            Vector256<ulong> h2,
            Vector256<ulong> h3,
            Vector256<ulong> h4,
            Vector256<ulong> r0,
            Vector256<ulong> r1,
            Vector256<ulong> r2,
            Vector256<ulong> r3,
            Vector256<ulong> r4,
            Vector256<ulong> s1,
            Vector256<ulong> s2,
            Vector256<ulong> s3,
            Vector256<ulong> s4,
            out Vector256<ulong> d0,
            out Vector256<ulong> d1,
            out Vector256<ulong> d2,
            out Vector256<ulong> d3,
            out Vector256<ulong> d4)
        {
            d0 = Product(h0, r0) + Product(h1, s4) + Product(h2, s3) + Product(h3, s2) + Product(h4, s1);
            d1 = Product(h0, r1) + Product(h1, r0) + Product(h2, s4) + Product(h3, s3) + Product(h4, s2);
            d2 = Product(h0, r2) + Product(h1, r1) + Product(h2, r0) + Product(h3, s4) + Product(h4, s3);
            d3 = Product(h0, r3) + Product(h1, r2) + Product(h2, r1) + Product(h3, r0) + Product(h4, s4);
            d4 = Product(h0, r4) + Product(h1, r3) + Product(h2, r2) + Product(h3, r1) + Product(h4, r0);
        }

        /// <summary>
        /// Brings every limb of each lane back within a few bits of 26 with one round of carries, folding what passes
        /// 2^130 back in times 5.
        /// </summary>
        /// <param name="d0">The lanes' limbs at 2^0.</param>
        /// <param name="d1">The lanes' limbs at 2^26.</param>
        /// <param name="d2">The lanes' limbs at 2^52.</param>
        /// <param name="d3">The lanes' limbs at 2^78.</param>
        /// <param name="d4">The lanes' limbs at 2^104.</param>
        /// <param name="mask">The 26-bit mask in every lane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Carry(
            ref Vector256<ulong> d0,
            ref Vector256<ulong> d1,
            ref Vector256<ulong> d2,
            ref Vector256<ulong> d3,
            ref Vector256<ulong> d4,
            Vector256<ulong> mask)
        {
            // Two chains run side by side: 0 → 1 → 2 → 3 → 4, and 3 → 4 → 0 (times 5) → 1.
            Vector256<ulong> c = d0 >>> 26;
            Vector256<ulong> e = d3 >>> 26;
            d0 &= mask;
            d1 += c;
            d3 &= mask;
            d4 += e;

            c = d1 >>> 26;
            e = d4 >>> 26;
            d1 &= mask;
            d2 += c;
            d4 &= mask;
            d0 += Times5(e);

            c = d2 >>> 26;
            e = d0 >>> 26;
            d2 &= mask;
            d3 += c;
            d0 &= mask;
            d1 += e;

            c = d3 >>> 26;
            d3 &= mask;
            d4 += c;
        }

        /// <summary>
        /// Multiplies each 64-bit lane by 5, with a shift and an add: AVX2 has no 64-bit multiply.
        /// </summary>
        /// <param name="value">The lanes.</param>
        /// <returns>Five times each lane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Times5(Vector256<ulong> value) =>
            value + (value << 2);

        /// <summary>
        /// Multiplies the low 32 bits of each 64-bit lane of two vectors into a 64-bit product: <c>vpmuludq</c>.
        /// </summary>
        /// <param name="left">The first factors.</param>
        /// <param name="right">The second factors.</param>
        /// <returns>The products.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Product(Vector256<ulong> left, Vector256<ulong> right) =>
            Avx2.Multiply(left.AsUInt32(), right.AsUInt32());
    }
}
