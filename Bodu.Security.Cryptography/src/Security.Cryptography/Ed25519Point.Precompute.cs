// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.Precompute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the precomputed multiples of the base point and the scalar multiplications that use them for
/// <see cref="Ed25519Point" />.
/// </summary>
/// <remarks>
/// <para>
/// The base point B is known at type-initialization time, so its multiples are precomputed once, in affine Niels form.
/// <see cref="ScalarMultBase" /> multiplies it in constant time over a table of 32 rows, and
/// <see cref="DoubleScalarMultBaseVartime" /> evaluates [a]B + [b]P for verification, where every input is public and
/// variable-time evaluation is therefore acceptable, over a table of odd multiples.
/// </para>
/// <para>
/// The general <see cref="ScalarMult(Ed25519Point, ReadOnlySpan{byte})" /> remains the reference these routines are
/// validated against.
/// </para>
/// </remarks>
internal readonly partial struct Ed25519Point
{
    /// <summary>The number of rows in the fixed-base table: one for each byte of a scalar, row i holding multiples of 256^i·B.</summary>
    private const int BaseTableRows = 32;

    /// <summary>The number of multiples in each row of the fixed-base table: 1 to 8, the magnitudes of a signed radix-16 digit.</summary>
    private const int BaseTableRowLength = 8;

    /// <summary>The number of signed radix-16 digits of a 256-bit scalar.</summary>
    private const int RadixDigitCount = 64;

    /// <summary>The width of the non-adjacent form of the scalar that verification applies to the base point.</summary>
    private const int BaseNafWidth = 7;

    /// <summary>The width of the non-adjacent form of the scalar that verification applies to the public point.</summary>
    private const int PointNafWidth = 5;

    /// <summary>The number of digits a non-adjacent form holds: 256, and past them the carry a scalar at or above 2^255 can leave above its top window.</summary>
    internal const int NafLength = 256 + 8;

    /// <summary>The fixed-base table, row by row: <c>s_baseTable[8i + m − 1] = m · 256^i · B</c> for m from 1 to 8. Built in the static constructor, after the curve constants and the base point.</summary>
    private static readonly AffineNielsPoint[] s_baseTable;

    /// <summary>The odd multiples of the base point that verification's width-7 digits select: <c>s_baseOddMultiples[k] = (2k + 1) · B</c>.</summary>
    private static readonly AffineNielsPoint[] s_baseOddMultiples;

    /// <summary>The multiple 2^252·B, which the carry out of a scalar's top radix-16 digit becomes, before the four doublings of <see cref="ScalarMultBase" />.</summary>
    private static readonly Ed25519Point s_base2Pow252;

    /// <summary>
    /// Initializes static members of the <see cref="Ed25519Point" /> struct with the multiples of the base point.
    /// </summary>
    /// <remarks>
    /// The multiples are formed with the unified addition and the doubling, then converted to affine Niels form
    /// together, with one inversion for each table.
    /// </remarks>
    static Ed25519Point()
    {
        var multiples = new Ed25519Point[BaseTableRows * BaseTableRowLength];

        Ed25519Point rowBase = s_basePoint;
        for (int row = 0; row < BaseTableRows; row++)
        {
            Ed25519Point multiple = rowBase;
            multiples[row * BaseTableRowLength] = multiple;
            for (int m = 1; m < BaseTableRowLength; m++)
            {
                multiple = multiple.Add(rowBase);
                multiples[(row * BaseTableRowLength) + m] = multiple;
            }

            // Advance the row base by a byte: 256^(i + 1)·B = 2^8 · 256^i·B.
            for (int t = 0; t < 8; t++)
                rowBase = rowBase.Double();
        }

        s_baseTable = ToAffineNiels(multiples);

        // 2^252·B = 2^4 · 256^31·B, the first multiple in the last row.
        s_base2Pow252 = multiples[(BaseTableRows - 1) * BaseTableRowLength].Double().Double().Double().Double();

        var oddMultiples = new Ed25519Point[1 << (BaseNafWidth - 2)];
        Ed25519Point twice = s_basePoint.Double();
        oddMultiples[0] = s_basePoint;
        for (int k = 1; k < oddMultiples.Length; k++)
            oddMultiples[k] = oddMultiples[k - 1].Add(twice);

        s_baseOddMultiples = ToAffineNiels(oddMultiples);
    }

    /// <summary>
    /// Multiplies the base point by a 256-bit little-endian scalar in constant time, using the precomputed fixed-base
    /// table.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar.</param>
    /// <returns>The scalar multiple of the base point.</returns>
    /// <exception cref="ArgumentException"><paramref name="scalar" /> is not exactly 32 bytes.</exception>
    /// <remarks>
    /// <para>
    /// The scalar is recoded into 64 signed radix-16 digits e_j, from −8 to 7, and a carry c, so that it is the sum of
    /// the e_j·16^j and c·2^256. The odd digits are added first, each from the row of the byte it belongs to, which
    /// gives their sum divided by 16; four doublings restore the factor, and the even digits are added last. Each digit
    /// costs one mixed addition, and each selection scans the 8 entries of its row, so the table needs only 32 rows.
    /// The carry enters the sum before the doublings, as 2^252·B.
    /// </para>
    /// <para>
    /// Every selection reads each entry of its row and negates the result with conditional moves, and the carry is
    /// taken with a conditional move, so neither the memory access pattern nor the sequence of operations depends on
    /// the scalar. The digits are cleared before the method returns.
    /// </para>
    /// </remarks>
    internal static Ed25519Point ScalarMultBase(ReadOnlySpan<byte> scalar)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(scalar, 32);

        Span<sbyte> digits = stackalloc sbyte[RadixDigitCount];

        try
        {
            int carry = RecodeSignedRadix16(scalar, digits);

            Ed25519Point accumulator = Identity;
            ConditionalMove(ref accumulator, s_base2Pow252, (ulong)carry);

            for (int j = 1; j < RadixDigitCount; j += 2)
                accumulator = accumulator.Add(SelectBaseMultiple(j >> 1, digits[j])).ToExtended();

            CompletedPoint doubled = accumulator.ToProjective().Double();
            doubled = doubled.ToProjective().Double();
            doubled = doubled.ToProjective().Double();
            accumulator = doubled.ToProjective().Double().ToExtended();

            for (int j = 0; j < RadixDigitCount; j += 2)
                accumulator = accumulator.Add(SelectBaseMultiple(j >> 1, digits[j])).ToExtended();

            return accumulator;
        }
        finally
        {
            CryptographyHelper.Clear(digits);
        }
    }

    /// <summary>
    /// Computes [<paramref name="baseScalar" />]B + [<paramref name="pointScalar" />]<paramref name="point" /> in
    /// variable time, sharing the doublings between the two scalar multiplications.
    /// </summary>
    /// <param name="baseScalar">The 32-byte little-endian scalar applied to the base point.</param>
    /// <param name="pointScalar">The 32-byte little-endian scalar applied to <paramref name="point" />.</param>
    /// <param name="point">The variable point.</param>
    /// <returns>The combined point [baseScalar]B + [pointScalar]·point.</returns>
    /// <exception cref="ArgumentException">A scalar is not exactly 32 bytes.</exception>
    /// <remarks>
    /// <para>
    /// Each scalar is recoded into its non-adjacent form, of width 7 for the base point and width 5 for the variable
    /// point, whose nonzero digits are odd and at least that many positions apart: about 32 and 43 additions for
    /// scalars of 253 bits. The base point's digits select from the 32 odd multiples precomputed in affine Niels form,
    /// and the variable point's from its 8 odd multiples, formed on the stack in projective Niels form. A digit is
    /// negated by subtracting its multiple. The doublings stay in projective coordinates, without the extended
    /// coordinate, unless an addition follows.
    /// </para>
    /// <para>
    /// This routine is used only by signature verification, where the scalars and points are public, so its
    /// data-dependent branches and timing carry no secret information.
    /// </para>
    /// </remarks>
    internal static Ed25519Point DoubleScalarMultBaseVartime(ReadOnlySpan<byte> baseScalar, ReadOnlySpan<byte> pointScalar, Ed25519Point point)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(baseScalar, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(pointScalar, 32);

        Span<sbyte> baseDigits = stackalloc sbyte[NafLength];
        Span<sbyte> pointDigits = stackalloc sbyte[NafLength];
        int top = Math.Max(
            ComputeNonAdjacentForm(baseScalar, BaseNafWidth, baseDigits),
            ComputeNonAdjacentForm(pointScalar, PointNafWidth, pointDigits));

        if (top < 0)
            return Identity;

        // pointMultiples[k] = (2k + 1)·point.
        Span<ProjectiveNielsPoint> pointMultiples = stackalloc ProjectiveNielsPoint[1 << (PointNafWidth - 2)];
        ProjectiveNielsPoint twice = point.Double().ToProjectiveNiels();
        Ed25519Point multiple = point;
        pointMultiples[0] = point.ToProjectiveNiels();
        for (int k = 1; k < pointMultiples.Length; k++)
        {
            multiple = multiple.Add(twice).ToExtended();
            pointMultiples[k] = multiple.ToProjectiveNiels();
        }

        ProjectivePoint accumulator = ProjectivePoint.Identity;
        for (int i = top; ; i--)
        {
            CompletedPoint sum = accumulator.Double();

            int pointDigit = pointDigits[i];
            if (pointDigit > 0)
                sum = sum.ToExtended().Add(pointMultiples[pointDigit >> 1]);
            else if (pointDigit < 0)
                sum = sum.ToExtended().Subtract(pointMultiples[-pointDigit >> 1]);

            int baseDigit = baseDigits[i];
            if (baseDigit > 0)
                sum = sum.ToExtended().Add(s_baseOddMultiples[baseDigit >> 1]);
            else if (baseDigit < 0)
                sum = sum.ToExtended().Subtract(s_baseOddMultiples[-baseDigit >> 1]);

            if (i == 0)
                return sum.ToExtended();

            accumulator = sum.ToProjective();
        }
    }

    /// <summary>
    /// Selects <c>digit · 256^row · B</c> from the fixed-base table in constant time.
    /// </summary>
    /// <param name="row">The table row: the index of the scalar byte the digit belongs to.</param>
    /// <param name="digit">The signed radix-16 digit.</param>
    /// <returns>The selected multiple in affine Niels form; the identity when the digit is zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="row" /> is below 0 or above 31.</exception>
    /// <remarks>
    /// <para>
    /// Every entry of the row is read and conditionally moved, and the entry for the digit's magnitude is negated with
    /// a conditional swap of y + x and y − x and a conditional negation of 2d·x·y, so neither the memory access pattern
    /// nor the running time depends on the digit.
    /// </para>
    /// <para>
    /// The digit must be from −8 to 8, as <see cref="RecodeSignedRadix16" /> produces it. It is not checked, since the
    /// check would branch on it.
    /// </para>
    /// </remarks>
    internal static AffineNielsPoint SelectBaseMultiple(int row, int digit)
    {
        // 1 when the digit is negative, then its magnitude, without a data-dependent branch.
        ulong negative = (uint)digit >> 31;
        int magnitude = digit - ((-(int)negative & digit) << 1);

        Curve25519FieldElement yPlusX = Curve25519FieldElement.One;
        Curve25519FieldElement yMinusX = Curve25519FieldElement.One;
        Curve25519FieldElement xy2d = Curve25519FieldElement.Zero;

        ReadOnlySpan<AffineNielsPoint> entries = s_baseTable.AsSpan(row * BaseTableRowLength, BaseTableRowLength);
        for (int m = 0; m < entries.Length; m++)
        {
            // 1 when the entry holds the magnitude's multiple, else 0.
            uint difference = (uint)(magnitude ^ (m + 1));
            ulong match = (difference - 1) >> 31;

            ref readonly AffineNielsPoint entry = ref entries[m];
            Curve25519FieldElement.ConditionalMove(ref yPlusX, entry.YPlusX, match);
            Curve25519FieldElement.ConditionalMove(ref yMinusX, entry.YMinusX, match);
            Curve25519FieldElement.ConditionalMove(ref xy2d, entry.XY2d, match);
        }

        Curve25519FieldElement.ConditionalSwap(ref yPlusX, ref yMinusX, negative);
        Curve25519FieldElement.ConditionalMove(
            ref xy2d,
            Curve25519FieldElement.Subtract(Curve25519FieldElement.Zero, xy2d),
            negative);

        return new AffineNielsPoint(yPlusX, yMinusX, xy2d);
    }

    /// <summary>
    /// Recodes a 256-bit little-endian scalar into 64 signed radix-16 digits, each from −8 to 7, in constant time.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar.</param>
    /// <param name="digits">The 64-digit span that receives the digits, least significant first.</param>
    /// <returns>
    /// The carry out of the top digit, 0 or 1: the scalar equals the sum of <c>digits[j] · 16^j</c> plus the carry
    /// times 2^256.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="scalar" /> is not exactly 32 bytes, or <paramref name="digits" /> is not exactly 64 digits.
    /// </exception>
    /// <remarks>
    /// Each nibble takes the carry from the one below it and passes one on when it reaches 8, so the recoding is the
    /// same sequence of arithmetic for every scalar.
    /// </remarks>
    internal static int RecodeSignedRadix16(ReadOnlySpan<byte> scalar, Span<sbyte> digits)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(scalar, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(digits, RadixDigitCount);

        int carry = 0;
        for (int j = 0; j < RadixDigitCount; j++)
        {
            int digit = ((scalar[j >> 1] >> ((j & 1) << 2)) & 15) + carry;
            carry = (digit + 8) >> 4;
            digits[j] = (sbyte)(digit - (carry << 4));
        }

        return carry;
    }

    /// <summary>
    /// Recodes a 256-bit little-endian scalar into its width-<paramref name="width" /> non-adjacent form, in variable
    /// time.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar.</param>
    /// <param name="width">The window width.</param>
    /// <param name="digits">The span that receives the form, least significant digit first.</param>
    /// <returns>The position of the most significant nonzero digit, or −1 when the scalar is zero.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="scalar" /> is not exactly 32 bytes, or <paramref name="digits" /> is not exactly
    /// <see cref="NafLength" /> digits.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width" /> is below 2 or above 8.</exception>
    /// <remarks>
    /// <para>
    /// The scalar equals the sum of <c>digits[i] · 2^i</c>. Every nonzero digit is odd and below 2^(width − 1) in
    /// magnitude, and any <paramref name="width" /> consecutive positions hold at most one of them. A scalar below
    /// 2^255 needs no digit past position 255; the positions after it hold the carry a larger scalar leaves above its
    /// top window.
    /// </para>
    /// <para>
    /// The recoding branches on the scalar's bits, so it serves only public scalars.
    /// </para>
    /// </remarks>
    internal static int ComputeNonAdjacentForm(ReadOnlySpan<byte> scalar, int width, Span<sbyte> digits)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(scalar, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(digits, NafLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, 8);

        digits.Clear();

        // The scalar's words, and a zero word above them for windows that run past bit 255.
        Span<ulong> words = stackalloc ulong[5];
        for (int w = 0; w < 4; w++)
            words[w] = BinaryPrimitives.ReadUInt64LittleEndian(scalar.Slice(8 * w, 8));
        words[4] = 0;

        ulong windowSize = 1UL << width;
        ulong windowMask = windowSize - 1;

        int position = 0;
        int top = -1;
        ulong carry = 0;
        while (position < 256)
        {
            int index = position >> 6;
            int bit = position & 63;
            ulong bits = words[index] >> bit;
            if (bit > 64 - width)
                bits |= words[index + 1] << (64 - bit);

            ulong window = carry + (bits & windowMask);

            // An even window places no digit; its lowest bit equals the carry, which therefore passes up unchanged.
            if ((window & 1) == 0)
            {
                position++;
                continue;
            }

            if (window < windowSize / 2)
            {
                carry = 0;
                digits[position] = (sbyte)window;
            }
            else
            {
                carry = 1;
                digits[position] = (sbyte)((long)window - (long)windowSize);
            }

            top = position;
            position += width;
        }

        if (carry != 0)
        {
            digits[position] = 1;
            top = position;
        }

        return top;
    }

    /// <summary>
    /// Converts extended points to affine Niels form, inverting all their Z coordinates with one inversion.
    /// </summary>
    /// <param name="points">The points to convert.</param>
    /// <returns>The points in affine Niels form, in the same order.</returns>
    /// <remarks>
    /// Montgomery's trick: the running products of the Z coordinates are inverted once, and each Z's inverse is peeled
    /// off the inverted product with two multiplications. Each sum and difference is re-reduced, so every coordinate of
    /// the table is loosely reduced.
    /// </remarks>
    private static AffineNielsPoint[] ToAffineNiels(Ed25519Point[] points)
    {
        var products = new Curve25519FieldElement[points.Length];
        Curve25519FieldElement product = Curve25519FieldElement.One;
        for (int k = 0; k < points.Length; k++)
        {
            product = Curve25519FieldElement.Multiply(product, points[k]._z);
            products[k] = product;
        }

        // inverse holds (Z_0 ⋯ Z_k)^−1 at the top of each iteration.
        Curve25519FieldElement inverse = Curve25519FieldElement.Invert(product);
        var niels = new AffineNielsPoint[points.Length];
        for (int k = points.Length - 1; k >= 0; k--)
        {
            Curve25519FieldElement zInverse = k == 0 ? inverse : Curve25519FieldElement.Multiply(inverse, products[k - 1]);
            inverse = Curve25519FieldElement.Multiply(inverse, points[k]._z);

            var x = Curve25519FieldElement.Multiply(points[k]._x, zInverse);
            var y = Curve25519FieldElement.Multiply(points[k]._y, zInverse);

            niels[k] = new AffineNielsPoint(
                Curve25519FieldElement.Reduce(Curve25519FieldElement.Add(y, x)),
                Curve25519FieldElement.Reduce(Curve25519FieldElement.Subtract(y, x)),
                Curve25519FieldElement.Multiply(Curve25519FieldElement.Multiply(x, y), s_d2));
        }

        return niels;
    }
}
