// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Core.Radix26.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal partial struct Poly1305Core
{
    /// <summary>
    /// Computes the powers of the key half <c>r</c> that the vector kernels multiply by, each as five 26-bit limbs.
    /// </summary>
    /// <param name="powers">
    /// Receives <c>r</c> to <c>rⁿ</c> in order, five limbs each, for <c>n</c> of 2, 4 or 8, then <c>r²ⁿ</c> where there
    /// is room for it: 10, 15, 20, 25, 40 or 45 limbs in all.
    /// </param>
    /// <remarks>
    /// The powers are formed by the scalar multiplication, from one for <c>r²</c> to eight for <c>r¹⁶</c>, arranged so
    /// that at most four depend on one another: <c>r³</c> and <c>r⁴</c> both come from <c>r²</c>, <c>r⁵</c> to
    /// <c>r⁸</c> from <c>r⁴</c>, and <c>r²ⁿ</c> is <c>rⁿ</c> squared.
    /// </remarks>
    internal readonly void ComputePowers(Span<ulong> powers)
    {
        ulong r0 = _r0;
        ulong r1 = _r1;
        ulong r2 = _r2;
        ulong s1 = _s1;
        ulong s2 = _s2;

        // r².
        ulong a0 = r0;
        ulong a1 = r1;
        ulong a2 = r2;
        Multiply(ref a0, ref a1, ref a2, r0, r1, r2, s1, s2);

        ToRadix26(r0, r1, r2, powers);
        ToRadix26(a0, a1, a2, powers[5..]);

        // The highest power so far, which r²ⁿ squares.
        ulong c0 = a0;
        ulong c1 = a1;
        ulong c2 = a2;
        if (powers.Length >= 20)
        {
            // r³ = r² · r and r⁴ = r² · r².
            ulong b0 = a0;
            ulong b1 = a1;
            ulong b2 = a2;
            Multiply(ref b0, ref b1, ref b2, r0, r1, r2, s1, s2);
            Multiply(ref c0, ref c1, ref c2, a0, a1, a2, a1 * 20, a2 * 20);

            ToRadix26(b0, b1, b2, powers[10..]);
            ToRadix26(c0, c1, c2, powers[15..]);

            if (powers.Length >= 40)
            {
                // r⁵ to r⁸: r, r², r³ and r⁴, each times r⁴.
                ulong u1 = c1 * 20;
                ulong u2 = c2 * 20;
                Multiply(ref r0, ref r1, ref r2, c0, c1, c2, u1, u2);
                Multiply(ref a0, ref a1, ref a2, c0, c1, c2, u1, u2);
                Multiply(ref b0, ref b1, ref b2, c0, c1, c2, u1, u2);
                Multiply(ref c0, ref c1, ref c2, c0, c1, c2, u1, u2);

                ToRadix26(r0, r1, r2, powers[20..]);
                ToRadix26(a0, a1, a2, powers[25..]);
                ToRadix26(b0, b1, b2, powers[30..]);
                ToRadix26(c0, c1, c2, powers[35..]);
            }
        }

        if (powers.Length % 10 == 5)
        {
            // r²ⁿ: the highest power so far, r², r⁴ or r⁸, squared.
            Multiply(ref c0, ref c1, ref c2, c0, c1, c2, c1 * 20, c2 * 20);
            ToRadix26(c0, c1, c2, powers[^5..]);
        }
    }

    /// <summary>
    /// Reads the accumulator as five 26-bit limbs, the form in which it enters a vector kernel.
    /// </summary>
    /// <param name="l0">Receives the limb at 2^0.</param>
    /// <param name="l1">Receives the limb at 2^26.</param>
    /// <param name="l2">Receives the limb at 2^52.</param>
    /// <param name="l3">Receives the limb at 2^78.</param>
    /// <param name="l4">
    /// Receives the limb at 2^104, which may exceed 26 bits by as much as the top limb exceeds 42.
    /// </param>
    private readonly void GetAccumulator(out ulong l0, out ulong l1, out ulong l2, out ulong l3, out ulong l4) =>
        ToRadix26(_h0, _h1, _h2, out l0, out l1, out l2, out l3, out l4);

    /// <summary>
    /// Sets the accumulator from five 26-bit limbs, as a vector kernel leaves it once its lanes are summed: carries
    /// between the limbs, folds what passes 2^130 back in times 5, and splits the result at bits 44 and 88.
    /// </summary>
    /// <param name="l0">The limb at 2^0.</param>
    /// <param name="l1">The limb at 2^26.</param>
    /// <param name="l2">The limb at 2^52.</param>
    /// <param name="l3">The limb at 2^78.</param>
    /// <param name="l4">The limb at 2^104.</param>
    /// <remarks>
    /// Each limb is a sum over at most eight lanes. A kernel's carries leave a lane's limbs at 2^0, 2^52 and 2^78 below
    /// 2^26, and those at 2^26 and 2^104 at most about 2^10 over it, so the limb at 2^78, shifted 34 bits into place,
    /// stays below 2^63 and every sum below 2^64. The result meets the scalar loop's bounds: the first and third limbs
    /// within their widths, and the second at most one over.
    /// </remarks>
    private void SetAccumulator(ulong l0, ulong l1, ulong l2, ulong l3, ulong l4)
    {
        ulong t = l0 + (l1 << 26);
        ulong h0 = t & Mask44;
        t = (t >> 44) + (l2 << 8) + (l3 << 34);
        ulong h1 = t & Mask44;
        t = (t >> 44) + (l4 << 16);
        ulong h2 = t & Mask42;
        h0 += (t >> 42) * 5;
        h1 += h0 >> 44;
        h0 &= Mask44;

        _h0 = h0;
        _h1 = h1;
        _h2 = h2;
    }

    /// <summary>
    /// Splits a value held in limbs of 44, 44 and 42 bits into five limbs of 26 bits, into a span.
    /// </summary>
    /// <param name="h0">The limb at 2^0, below 2^44.</param>
    /// <param name="h1">The limb at 2^44, which may exceed 44 bits by a small carry.</param>
    /// <param name="h2">The limb at 2^88.</param>
    /// <param name="limbs">Receives the five limbs, from 2^0 up, in its first five elements.</param>
    private static void ToRadix26(ulong h0, ulong h1, ulong h2, Span<ulong> limbs)
    {
        ToRadix26(h0, h1, h2, out ulong l0, out ulong l1, out ulong l2, out ulong l3, out ulong l4);
        limbs[0] = l0;
        limbs[1] = l1;
        limbs[2] = l2;
        limbs[3] = l3;
        limbs[4] = l4;
    }

    /// <summary>
    /// Splits a value held in limbs of 44, 44 and 42 bits into five limbs of 26 bits.
    /// </summary>
    /// <param name="h0">The limb at 2^0, below 2^44.</param>
    /// <param name="h1">The limb at 2^44, which may exceed 44 bits by a small carry.</param>
    /// <param name="h2">The limb at 2^88.</param>
    /// <param name="l0">Receives the limb at 2^0.</param>
    /// <param name="l1">Receives the limb at 2^26.</param>
    /// <param name="l2">Receives the limb at 2^52.</param>
    /// <param name="l3">Receives the limb at 2^78.</param>
    /// <param name="l4">
    /// Receives the limb at 2^104, which exceeds 26 bits by as much as the carried top limb exceeds 42.
    /// </param>
    private static void ToRadix26(ulong h0, ulong h1, ulong h2, out ulong l0, out ulong l1, out ulong l2, out ulong l3, out ulong l4)
    {
        // Carry the second limb's excess on, so that each 26-bit limb is a plain slice of the bits.
        h2 += h1 >> 44;
        h1 &= Mask44;

        l0 = h0 & Mask26;
        l1 = ((h0 >> 26) | (h1 << 18)) & Mask26;
        l2 = (h1 >> 8) & Mask26;
        l3 = ((h1 >> 34) | (h2 << 10)) & Mask26;
        l4 = h2 >> 16;
    }
}
