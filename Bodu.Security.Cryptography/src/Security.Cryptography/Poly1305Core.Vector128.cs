// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Core.Vector128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal partial struct Poly1305Core
{
    /// <summary>
    /// Absorbs groups of two blocks with AdvSimd on ARM64: one block in each 64-bit lane of 128-bit vectors, as five
    /// 26-bit limbs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After every group but the last, each lane is multiplied by <c>r²</c>; after the last, the two lanes are
    /// multiplied by <c>r²</c> and <c>r</c>, so that every block ends up multiplied by the power of <c>r</c> the scalar
    /// loop would have given it. The accumulator enters in the first lane, with the first block, and the two lanes are
    /// summed back into it.
    /// </para>
    /// <para>
    /// A product of two limbs is one <c>UMULL</c> or <c>UMLAL</c>, 32 × 32 → 64 bits. They take each lane's limb from
    /// the lower half of its 64-bit lane, where <c>XTN</c> narrows the limbs once per group after the carries, and the
    /// multiplier's limb from one lane of another vector, so that a multiplier's five limbs and five times its upper
    /// four fill three vectors. <see cref="BlocksPaired" /> takes two groups at a time, as <c>(h + m)·r⁴ + m′·r²</c>,
    /// whose second half does not wait on <c>h</c>; with its two multipliers in six vectors, its state fits in ARM64's
    /// 32 vector registers.
    /// </para>
    /// </remarks>
    private static class Vector128Kernel
    {
        /// <summary>The number of blocks in a group: one for each 64-bit lane.</summary>
        private const int Lanes = 2;

        /// <summary>The number of bytes in a group.</summary>
        private const int GroupBytes = Lanes * BlockBytes;

        /// <summary>
        /// Absorbs whole groups of two blocks into a core's accumulator, one group at a time.
        /// </summary>
        /// <param name="core">The core whose accumulator absorbs the blocks, under its key half <c>r</c>.</param>
        /// <param name="message">The first byte of the first group.</param>
        /// <param name="groups">The number of groups; at least one.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Blocks(ref Poly1305Core core, ref byte message, int groups)
        {
            // r and r²; cleared before returning.
            Span<ulong> powers = stackalloc ulong[2 * 5];
            core.ComputePowers(powers);

            Vector128<ulong> mask = Vector128.Create(Mask26);
            Pack(powers[5..], out Vector128<uint> a, out Vector128<uint> b, out Vector128<uint> c);
            LoadAccumulator(ref core, out Vector128<ulong> h0, out Vector128<ulong> h1, out Vector128<ulong> h2, out Vector128<ulong> h3, out Vector128<ulong> h4);

            nint offset = 0;
            for (int group = 1; group < groups; group++)
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
        /// Absorbs whole groups of two blocks into a core's accumulator, two groups at a time while more than two
        /// remain.
        /// </summary>
        /// <param name="core">The core whose accumulator absorbs the blocks, under its key half <c>r</c>.</param>
        /// <param name="message">The first byte of the first group.</param>
        /// <param name="groups">The number of groups; at least one.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void BlocksPaired(ref Poly1305Core core, ref byte message, int groups)
        {
            // r, r² and r⁴; cleared before returning.
            Span<ulong> powers = stackalloc ulong[3 * 5];
            core.ComputePowers(powers);

            Vector128<ulong> mask = Vector128.Create(Mask26);
            Pack(powers[5..], out Vector128<uint> a, out Vector128<uint> b, out Vector128<uint> c);
            Pack(powers[10..], out Vector128<uint> d, out Vector128<uint> e, out Vector128<uint> f);
            LoadAccumulator(ref core, out Vector128<ulong> h0, out Vector128<ulong> h1, out Vector128<ulong> h2, out Vector128<ulong> h3, out Vector128<ulong> h4);

            nint offset = 0;
            int group = 0;
            for (; groups - group > 2; group += 2)
            {
                // m′·r² first, which needs nothing of h, then (h + m)·r⁴ into the same sums.
                SplitGroup(ref message, offset + GroupBytes, out Vector128<ulong> m0, out Vector128<ulong> m1, out Vector128<ulong> m2, out Vector128<ulong> m3, out Vector128<ulong> m4, mask);
                Products(Narrow(m0), Narrow(m1), Narrow(m2), Narrow(m3), Narrow(m4), a, b, c, out Vector128<ulong> p0, out Vector128<ulong> p1, out Vector128<ulong> p2, out Vector128<ulong> p3, out Vector128<ulong> p4);
                AddGroup(ref message, offset, ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                AddProducts(Narrow(h0), Narrow(h1), Narrow(h2), Narrow(h3), Narrow(h4), d, e, f, ref p0, ref p1, ref p2, ref p3, ref p4);
                h0 = p0;
                h1 = p1;
                h2 = p2;
                h3 = p3;
                h4 = p4;
                Carry(ref h0, ref h1, ref h2, ref h3, ref h4, mask);
                offset += 2 * GroupBytes;
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
        /// Packs a multiplier's five 26-bit limbs, and five times its upper four, into the lanes the products read them
        /// from.
        /// </summary>
        /// <param name="limbs">The multiplier's limbs, from 2^0 up, in its first five elements.</param>
        /// <param name="a">Receives the limbs at 2^0, 2^26, 2^52 and 2^78.</param>
        /// <param name="b">Receives the limb at 2^104, then five times the limbs at 2^26, 2^52 and 2^78.</param>
        /// <param name="c">Receives five times the limb at 2^104 in its first lane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Pack(ReadOnlySpan<ulong> limbs, out Vector128<uint> a, out Vector128<uint> b, out Vector128<uint> c)
        {
            a = Vector128.Create((uint)limbs[0], (uint)limbs[1], (uint)limbs[2], (uint)limbs[3]);
            b = Vector128.Create((uint)limbs[4], (uint)limbs[1] * 5, (uint)limbs[2] * 5, (uint)limbs[3] * 5);
            c = Vector128.CreateScalar((uint)limbs[4] * 5);
        }

        /// <summary>
        /// Reads a core's accumulator into the first lane of five vectors, as five 26-bit limbs, with zeros in the
        /// second.
        /// </summary>
        /// <param name="core">The core whose accumulator is read.</param>
        /// <param name="h0">Receives the limbs at 2^0.</param>
        /// <param name="h1">Receives the limbs at 2^26.</param>
        /// <param name="h2">Receives the limbs at 2^52.</param>
        /// <param name="h3">Receives the limbs at 2^78.</param>
        /// <param name="h4">Receives the limbs at 2^104.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadAccumulator(
            ref Poly1305Core core,
            out Vector128<ulong> h0,
            out Vector128<ulong> h1,
            out Vector128<ulong> h2,
            out Vector128<ulong> h3,
            out Vector128<ulong> h4)
        {
            core.GetAccumulator(out ulong l0, out ulong l1, out ulong l2, out ulong l3, out ulong l4);
            h0 = Vector128.CreateScalar(l0);
            h1 = Vector128.CreateScalar(l1);
            h2 = Vector128.CreateScalar(l2);
            h3 = Vector128.CreateScalar(l3);
            h4 = Vector128.CreateScalar(l4);
        }

        /// <summary>
        /// Absorbs the last group, multiplying the two lanes by <c>r²</c> and <c>r</c>, and sums the lanes back into
        /// the core's accumulator.
        /// </summary>
        /// <param name="core">The core whose accumulator receives the lanes' sum.</param>
        /// <param name="message">The first byte of the message.</param>
        /// <param name="offset">The offset of the last group's 32 bytes.</param>
        /// <param name="powers">The powers <c>r</c> and <c>r²</c>, five limbs each, in its first ten elements.</param>
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
            Vector128<ulong> h0,
            Vector128<ulong> h1,
            Vector128<ulong> h2,
            Vector128<ulong> h3,
            Vector128<ulong> h4,
            Vector128<ulong> mask)
        {
            AddGroup(ref message, offset, ref h0, ref h1, ref h2, ref h3, ref h4, mask);

            // r² in the first lane and r in the second, so a multiplier's limb differs by lane: one vector per limb.
            Vector64<uint> r0 = Vector64.Create((uint)powers[5], (uint)powers[0]);
            Vector64<uint> r1 = Vector64.Create((uint)powers[6], (uint)powers[1]);
            Vector64<uint> r2 = Vector64.Create((uint)powers[7], (uint)powers[2]);
            Vector64<uint> r3 = Vector64.Create((uint)powers[8], (uint)powers[3]);
            Vector64<uint> r4 = Vector64.Create((uint)powers[9], (uint)powers[4]);
            Vector64<uint> s1 = r1 * 5;
            Vector64<uint> s2 = r2 * 5;
            Vector64<uint> s3 = r3 * 5;
            Vector64<uint> s4 = r4 * 5;

            Vector64<uint> n0 = Narrow(h0);
            Vector64<uint> n1 = Narrow(h1);
            Vector64<uint> n2 = Narrow(h2);
            Vector64<uint> n3 = Narrow(h3);
            Vector64<uint> n4 = Narrow(h4);
            h0 = AdvSimd.MultiplyWideningLower(n0, r0);
            h1 = AdvSimd.MultiplyWideningLower(n0, r1);
            h2 = AdvSimd.MultiplyWideningLower(n0, r2);
            h3 = AdvSimd.MultiplyWideningLower(n0, r3);
            h4 = AdvSimd.MultiplyWideningLower(n0, r4);
            h0 = AdvSimd.MultiplyWideningLowerAndAdd(h0, n1, s4);
            h1 = AdvSimd.MultiplyWideningLowerAndAdd(h1, n1, r0);
            h2 = AdvSimd.MultiplyWideningLowerAndAdd(h2, n1, r1);
            h3 = AdvSimd.MultiplyWideningLowerAndAdd(h3, n1, r2);
            h4 = AdvSimd.MultiplyWideningLowerAndAdd(h4, n1, r3);
            h0 = AdvSimd.MultiplyWideningLowerAndAdd(h0, n2, s3);
            h1 = AdvSimd.MultiplyWideningLowerAndAdd(h1, n2, s4);
            h2 = AdvSimd.MultiplyWideningLowerAndAdd(h2, n2, r0);
            h3 = AdvSimd.MultiplyWideningLowerAndAdd(h3, n2, r1);
            h4 = AdvSimd.MultiplyWideningLowerAndAdd(h4, n2, r2);
            h0 = AdvSimd.MultiplyWideningLowerAndAdd(h0, n3, s2);
            h1 = AdvSimd.MultiplyWideningLowerAndAdd(h1, n3, s3);
            h2 = AdvSimd.MultiplyWideningLowerAndAdd(h2, n3, s4);
            h3 = AdvSimd.MultiplyWideningLowerAndAdd(h3, n3, r0);
            h4 = AdvSimd.MultiplyWideningLowerAndAdd(h4, n3, r1);
            h0 = AdvSimd.MultiplyWideningLowerAndAdd(h0, n4, s1);
            h1 = AdvSimd.MultiplyWideningLowerAndAdd(h1, n4, s2);
            h2 = AdvSimd.MultiplyWideningLowerAndAdd(h2, n4, s3);
            h3 = AdvSimd.MultiplyWideningLowerAndAdd(h3, n4, s4);
            h4 = AdvSimd.MultiplyWideningLowerAndAdd(h4, n4, r0);
            Carry(ref h0, ref h1, ref h2, ref h3, ref h4, mask);

            core.SetAccumulator(Vector128.Sum(h0), Vector128.Sum(h1), Vector128.Sum(h2), Vector128.Sum(h3), Vector128.Sum(h4));
        }

        /// <summary>
        /// Adds a group of two blocks to the lanes, each block as five 26-bit limbs with the 2^128 bit a full block
        /// sets.
        /// </summary>
        /// <param name="message">The first byte of the message.</param>
        /// <param name="offset">The offset of the group's 32 bytes.</param>
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
            ref Vector128<ulong> h0,
            ref Vector128<ulong> h1,
            ref Vector128<ulong> h2,
            ref Vector128<ulong> h3,
            ref Vector128<ulong> h4,
            Vector128<ulong> mask)
        {
            SplitGroup(ref message, offset, out Vector128<ulong> m0, out Vector128<ulong> m1, out Vector128<ulong> m2, out Vector128<ulong> m3, out Vector128<ulong> m4, mask);
            h0 += m0;
            h1 += m1;
            h2 += m2;
            h3 += m3;
            h4 += m4;
        }

        /// <summary>
        /// Splits a group of two blocks into five 26-bit limbs each, one block to a lane, with the 2^128 bit a full
        /// block sets.
        /// </summary>
        /// <param name="message">The first byte of the message.</param>
        /// <param name="offset">The offset of the group's 32 bytes.</param>
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
            out Vector128<ulong> m0,
            out Vector128<ulong> m1,
            out Vector128<ulong> m2,
            out Vector128<ulong> m3,
            out Vector128<ulong> m4,
            Vector128<ulong> mask)
        {
            Vector128<ulong> first = Vector128.LoadUnsafe(ref Unsafe.Add(ref message, offset)).AsUInt64();
            Vector128<ulong> second = Vector128.LoadUnsafe(ref Unsafe.Add(ref message, offset + BlockBytes)).AsUInt64();

            // The low and the high eight bytes of each block, one block to a lane: ZIP1 and ZIP2.
            Vector128<ulong> low = AdvSimd.Arm64.ZipLow(first, second);
            Vector128<ulong> high = AdvSimd.Arm64.ZipHigh(first, second);

            m0 = low & mask;
            m1 = (low >>> 26) & mask;
            m2 = ((low >>> 52) | (high << 12)) & mask;
            m3 = (high >>> 14) & mask;
            m4 = (high >>> 40) | Vector128.Create(FullBlockBit26);
        }

        /// <summary>
        /// Forms the five limb sums of each lane's product with a multiplier, before any carry.
        /// </summary>
        /// <param name="h0">The lanes' limbs at 2^0, narrowed to 32 bits.</param>
        /// <param name="h1">The lanes' limbs at 2^26, narrowed to 32 bits.</param>
        /// <param name="h2">The lanes' limbs at 2^52, narrowed to 32 bits.</param>
        /// <param name="h3">The lanes' limbs at 2^78, narrowed to 32 bits.</param>
        /// <param name="h4">The lanes' limbs at 2^104, narrowed to 32 bits.</param>
        /// <param name="a">
        /// The multiplier's limbs at 2^0, 2^26, 2^52 and 2^78, as <see cref="Pack" /> leaves them.
        /// </param>
        /// <param name="b">The multiplier's limb at 2^104, then five times those at 2^26, 2^52 and 2^78.</param>
        /// <param name="c">Five times the multiplier's limb at 2^104, in the first lane.</param>
        /// <param name="d0">Receives the sums at 2^0.</param>
        /// <param name="d1">Receives the sums at 2^26.</param>
        /// <param name="d2">Receives the sums at 2^52.</param>
        /// <param name="d3">Receives the sums at 2^78.</param>
        /// <param name="d4">Receives the sums at 2^104.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Products(
            Vector64<uint> h0,
            Vector64<uint> h1,
            Vector64<uint> h2,
            Vector64<uint> h3,
            Vector64<uint> h4,
            Vector128<uint> a,
            Vector128<uint> b,
            Vector128<uint> c,
            out Vector128<ulong> d0,
            out Vector128<ulong> d1,
            out Vector128<ulong> d2,
            out Vector128<ulong> d3,
            out Vector128<ulong> d4)
        {
            d0 = AdvSimd.MultiplyBySelectedScalarWideningLower(h0, a, 0);
            d1 = AdvSimd.MultiplyBySelectedScalarWideningLower(h0, a, 1);
            d2 = AdvSimd.MultiplyBySelectedScalarWideningLower(h0, a, 2);
            d3 = AdvSimd.MultiplyBySelectedScalarWideningLower(h0, a, 3);
            d4 = AdvSimd.MultiplyBySelectedScalarWideningLower(h0, b, 0);
            AddUpperProducts(h1, h2, h3, h4, a, b, c, ref d0, ref d1, ref d2, ref d3, ref d4);
        }

        /// <summary>
        /// Adds each lane's product with a multiplier to five limb sums.
        /// </summary>
        /// <param name="h0">The lanes' limbs at 2^0, narrowed to 32 bits.</param>
        /// <param name="h1">The lanes' limbs at 2^26, narrowed to 32 bits.</param>
        /// <param name="h2">The lanes' limbs at 2^52, narrowed to 32 bits.</param>
        /// <param name="h3">The lanes' limbs at 2^78, narrowed to 32 bits.</param>
        /// <param name="h4">The lanes' limbs at 2^104, narrowed to 32 bits.</param>
        /// <param name="a">
        /// The multiplier's limbs at 2^0, 2^26, 2^52 and 2^78, as <see cref="Pack" /> leaves them.
        /// </param>
        /// <param name="b">The multiplier's limb at 2^104, then five times those at 2^26, 2^52 and 2^78.</param>
        /// <param name="c">Five times the multiplier's limb at 2^104, in the first lane.</param>
        /// <param name="d0">The sums at 2^0.</param>
        /// <param name="d1">The sums at 2^26.</param>
        /// <param name="d2">The sums at 2^52.</param>
        /// <param name="d3">The sums at 2^78.</param>
        /// <param name="d4">The sums at 2^104.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddProducts(
            Vector64<uint> h0,
            Vector64<uint> h1,
            Vector64<uint> h2,
            Vector64<uint> h3,
            Vector64<uint> h4,
            Vector128<uint> a,
            Vector128<uint> b,
            Vector128<uint> c,
            ref Vector128<ulong> d0,
            ref Vector128<ulong> d1,
            ref Vector128<ulong> d2,
            ref Vector128<ulong> d3,
            ref Vector128<ulong> d4)
        {
            d0 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d0, h0, a, 0);
            d1 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d1, h0, a, 1);
            d2 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d2, h0, a, 2);
            d3 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d3, h0, a, 3);
            d4 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d4, h0, b, 0);
            AddUpperProducts(h1, h2, h3, h4, a, b, c, ref d0, ref d1, ref d2, ref d3, ref d4);
        }

        /// <summary>
        /// Adds the products of each lane's limbs at 2^26 and up with a multiplier to five limb sums: those that pass
        /// 2^130 fold back in times 5, through the multiplier's limbs times 5.
        /// </summary>
        /// <param name="h1">The lanes' limbs at 2^26, narrowed to 32 bits.</param>
        /// <param name="h2">The lanes' limbs at 2^52, narrowed to 32 bits.</param>
        /// <param name="h3">The lanes' limbs at 2^78, narrowed to 32 bits.</param>
        /// <param name="h4">The lanes' limbs at 2^104, narrowed to 32 bits.</param>
        /// <param name="a">
        /// The multiplier's limbs at 2^0, 2^26, 2^52 and 2^78, as <see cref="Pack" /> leaves them.
        /// </param>
        /// <param name="b">The multiplier's limb at 2^104, then five times those at 2^26, 2^52 and 2^78.</param>
        /// <param name="c">Five times the multiplier's limb at 2^104, in the first lane.</param>
        /// <param name="d0">The sums at 2^0.</param>
        /// <param name="d1">The sums at 2^26.</param>
        /// <param name="d2">The sums at 2^52.</param>
        /// <param name="d3">The sums at 2^78.</param>
        /// <param name="d4">The sums at 2^104.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddUpperProducts(
            Vector64<uint> h1,
            Vector64<uint> h2,
            Vector64<uint> h3,
            Vector64<uint> h4,
            Vector128<uint> a,
            Vector128<uint> b,
            Vector128<uint> c,
            ref Vector128<ulong> d0,
            ref Vector128<ulong> d1,
            ref Vector128<ulong> d2,
            ref Vector128<ulong> d3,
            ref Vector128<ulong> d4)
        {
            d0 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d0, h1, c, 0);
            d1 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d1, h1, a, 0);
            d2 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d2, h1, a, 1);
            d3 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d3, h1, a, 2);
            d4 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d4, h1, a, 3);

            d0 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d0, h2, b, 3);
            d1 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d1, h2, c, 0);
            d2 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d2, h2, a, 0);
            d3 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d3, h2, a, 1);
            d4 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d4, h2, a, 2);

            d0 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d0, h3, b, 2);
            d1 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d1, h3, b, 3);
            d2 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d2, h3, c, 0);
            d3 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d3, h3, a, 0);
            d4 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d4, h3, a, 1);

            d0 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d0, h4, b, 1);
            d1 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d1, h4, b, 2);
            d2 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d2, h4, b, 3);
            d3 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d3, h4, c, 0);
            d4 = AdvSimd.MultiplyBySelectedScalarWideningLowerAndAdd(d4, h4, a, 0);
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
            ref Vector128<ulong> d0,
            ref Vector128<ulong> d1,
            ref Vector128<ulong> d2,
            ref Vector128<ulong> d3,
            ref Vector128<ulong> d4,
            Vector128<ulong> mask)
        {
            // Two chains run side by side: 0 → 1 → 2 → 3 → 4, and 3 → 4 → 0 (times 5) → 1.
            Vector128<ulong> c = d0 >>> 26;
            Vector128<ulong> e = d3 >>> 26;
            d0 &= mask;
            d1 += c;
            d3 &= mask;
            d4 += e;

            c = d1 >>> 26;
            e = d4 >>> 26;
            d1 &= mask;
            d2 += c;
            d4 &= mask;
            d0 += e + (e << 2);

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
        /// Narrows each 64-bit lane to its low 32 bits, the form <c>UMULL</c> takes its factors in: <c>XTN</c>.
        /// </summary>
        /// <param name="value">The lanes, each below 2^32.</param>
        /// <returns>The lanes' low halves.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector64<uint> Narrow(Vector128<ulong> value) =>
            AdvSimd.ExtractNarrowingLower(value);
    }
}
