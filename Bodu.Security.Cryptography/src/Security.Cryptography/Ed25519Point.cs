// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a point on the twisted Edwards curve edwards25519 (−x² + y² = 1 + d·x²·y²) in extended homogeneous
/// coordinates (X : Y : Z : T) with x = X/Z, y = Y/Z, and T = XY/Z, as used by Ed25519 (RFC 8032).
/// </summary>
/// <remarks>
/// <para>
/// Point addition uses the unified extended-coordinate formula of Hisil, Wong, Carter, and Dawson. Because the curve
/// parameter a = −1 is a square modulo p and d is a non-square, the formula is complete: it is correct for every input
/// pair, including doubling and the identity, so no secret-dependent branching is required. Doubling uses the same
/// paper's dedicated formula, which is complete as well and needs four squarings and four multiplications instead of
/// nine multiplications. This trades the peak speed of the ref10 formula set for a substantially smaller and more
/// reviewable implementation.
/// </para>
/// </remarks>
internal readonly partial struct Ed25519Point
{
    /// <summary>The curve constant d = −121665/121666 mod p, decoded from its canonical encoding at type initialization.</summary>
    private static readonly Curve25519FieldElement s_d = Curve25519FieldElement.FromBytes(
        Convert.FromHexString("a3785913ca4deb75abd841414d0a700098e879777940c78c73fe6f2bee6c0352"));

    /// <summary>The doubled curve constant 2d used by the unified addition formula.</summary>
    private static readonly Curve25519FieldElement s_d2 = Curve25519FieldElement.Add(s_d, s_d);

    /// <summary>The square root of −1 modulo p, used to correct the candidate root during point decompression.</summary>
    private static readonly Curve25519FieldElement s_sqrtMinusOne = Curve25519FieldElement.FromBytes(
        Convert.FromHexString("b0a00e4a271beec478e42fad0618432fa7d7fb3d99004d2b0bdfc14f8024832b"));

    /// <summary>The Ed25519 base point B = (x, 4/5) with x positive, decoded from its canonical encoding. Declared after the curve constants in this file because static field initializers run in declaration order and the decoder consumes <see cref="s_d" /> and <see cref="s_sqrtMinusOne" />.</summary>
    private static readonly Ed25519Point s_basePoint = DecodeConstant(
        "5866666666666666666666666666666666666666666666666666666666666666");

    /// <summary>The X coordinate of the extended representation.</summary>
    private readonly Curve25519FieldElement _x;

    /// <summary>The Y coordinate of the extended representation.</summary>
    private readonly Curve25519FieldElement _y;

    /// <summary>The Z coordinate of the extended representation.</summary>
    private readonly Curve25519FieldElement _z;

    /// <summary>The extended coordinate T = XY/Z.</summary>
    private readonly Curve25519FieldElement _t;

    /// <summary>The size, in bytes, of an encoded point.</summary>
    internal const int EncodedSizeInBytes = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ed25519Point" /> struct from explicit extended coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="t">The extended coordinate T = XY/Z.</param>
    private Ed25519Point(
        Curve25519FieldElement x,
        Curve25519FieldElement y,
        Curve25519FieldElement z,
        Curve25519FieldElement t)
    {
        _x = x;
        _y = y;
        _z = z;
        _t = t;
    }

    /// <summary>
    /// Gets the Ed25519 base point B defined by RFC 8032.
    /// </summary>
    /// <value>The generator of the prime-order subgroup.</value>
    internal static Ed25519Point BasePoint =>
        s_basePoint;

    /// <summary>
    /// Gets the identity element (0, 1) of the curve group.
    /// </summary>
    /// <value>The neutral point in extended coordinates (0 : 1 : 1 : 0).</value>
    internal static Ed25519Point Identity =>
        new(Curve25519FieldElement.Zero, Curve25519FieldElement.One, Curve25519FieldElement.One, Curve25519FieldElement.Zero);

    /// <summary>
    /// Attempts to decode a 32-byte RFC 8032 point encoding, rejecting non-canonical y values and encodings that do not
    /// correspond to a curve point.
    /// </summary>
    /// <param name="encoded">The 32-byte encoding to decode.</param>
    /// <param name="point">When the method returns <see langword="true" />, the decoded point.</param>
    /// <returns><see langword="true" /> when the encoding is valid; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="encoded" /> is not exactly 32 bytes.</exception>
    /// <remarks>
    /// The decoder enforces the RFC 8032 §5.1.3 rules strictly: a y coordinate at or above p, a non-square x²
    /// candidate, or the invalid combination x = 0 with sign bit 1 all fail. Strict canonicality matters for
    /// interoperable signature verification, where implementations that accept non-canonical encodings disagree with
    /// those that reject them.
    /// </remarks>
    internal static bool TryDecode(ReadOnlySpan<byte> encoded, out Ed25519Point point)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(encoded, EncodedSizeInBytes);

        point = Identity;
        bool sign = (encoded[31] & 0x80) != 0;

        // FromBytes ignores bit 255; a canonical y must round-trip to the encoding with the sign bit cleared.
        var y = Curve25519FieldElement.FromBytes(encoded);
        Span<byte> canonical = stackalloc byte[EncodedSizeInBytes];
        y.ToBytes(canonical);

        int mismatch = 0;
        for (int i = 0; i < EncodedSizeInBytes - 1; i++)
            mismatch |= canonical[i] ^ encoded[i];
        mismatch |= canonical[31] ^ (encoded[31] & 0x7F);

        if (mismatch != 0)
            return false;

        // Solve x² = (y² − 1) / (d·y² + 1) via the combined exponentiation x = u·v³·(u·v⁷)^((p−5)/8). The
        // subtraction result is re-reduced because u is later negated, and Subtract requires tight operands.
        var y2 = Curve25519FieldElement.Square(y);
        var u = Curve25519FieldElement.Reduce(
            Curve25519FieldElement.Subtract(y2, Curve25519FieldElement.One));
        var v = Curve25519FieldElement.Add(Curve25519FieldElement.Multiply(s_d, y2), Curve25519FieldElement.One);

        var v3 = Curve25519FieldElement.Multiply(Curve25519FieldElement.Square(v), v);
        var v7 = Curve25519FieldElement.Multiply(Curve25519FieldElement.Square(v3), v);
        var x = Curve25519FieldElement.Multiply(
            Curve25519FieldElement.Multiply(u, v3),
            Curve25519FieldElement.Pow22523(Curve25519FieldElement.Multiply(u, v7)));

        var vx2 = Curve25519FieldElement.Multiply(v, Curve25519FieldElement.Square(x));

        if (!AreEqual(vx2, u))
        {
            // v·x² = −u means the candidate is off by a factor of sqrt(−1); anything else means x² has no root.
            if (!AreEqual(vx2, Curve25519FieldElement.Subtract(Curve25519FieldElement.Zero, u)))
                return false;

            x = Curve25519FieldElement.Multiply(x, s_sqrtMinusOne);
        }

        if (x.IsZeroConstantTime() && sign)
            return false;

        if (x.IsNegative() != sign)
            x = Curve25519FieldElement.Subtract(Curve25519FieldElement.Zero, x);

        point = new Ed25519Point(x, y, Curve25519FieldElement.One, Curve25519FieldElement.Multiply(x, y));

        return true;
    }

    /// <summary>
    /// Adds this point to <paramref name="other" /> using the complete unified extended-coordinate formula.
    /// </summary>
    /// <param name="other">The point to add.</param>
    /// <returns>The sum of the two points.</returns>
    internal readonly Ed25519Point Add(in Ed25519Point other)
    {
        var a = Curve25519FieldElement.Multiply(
            Curve25519FieldElement.Subtract(_y, _x),
            Curve25519FieldElement.Subtract(other._y, other._x));
        var b = Curve25519FieldElement.Multiply(
            Curve25519FieldElement.Add(_y, _x),
            Curve25519FieldElement.Add(other._y, other._x));
        var c = Curve25519FieldElement.Multiply(Curve25519FieldElement.Multiply(_t, s_d2), other._t);
        var zz = Curve25519FieldElement.Multiply(_z, other._z);
        var d = Curve25519FieldElement.Add(zz, zz);

        var e = Curve25519FieldElement.Subtract(b, a);
        var f = Curve25519FieldElement.Subtract(d, c);
        var g = Curve25519FieldElement.Add(d, c);
        var h = Curve25519FieldElement.Add(b, a);

        return new Ed25519Point(
            Curve25519FieldElement.Multiply(e, f),
            Curve25519FieldElement.Multiply(g, h),
            Curve25519FieldElement.Multiply(f, g),
            Curve25519FieldElement.Multiply(e, h));
    }

    /// <summary>
    /// Adds a point in affine Niels form to this point, leaving the sum in completed coordinates.
    /// </summary>
    /// <param name="other">The point to add, in affine Niels form.</param>
    /// <returns>The sum of the two points, in completed coordinates.</returns>
    /// <remarks>
    /// The mixed addition of Hisil, Wong, Carter, and Dawson for a = −1 with the second point's Z one
    /// (madd-2008-hwcd-3), before its final multiplications, which the conversion out of completed coordinates
    /// performs: three multiplications. Like <see cref="Add(in Ed25519Point)" />, it is complete.
    /// </remarks>
    internal readonly CompletedPoint Add(in AffineNielsPoint other)
    {
        var a = Curve25519FieldElement.Multiply(Curve25519FieldElement.Add(_y, _x), other.YPlusX);
        var b = Curve25519FieldElement.Multiply(Curve25519FieldElement.Subtract(_y, _x), other.YMinusX);
        var c = Curve25519FieldElement.Multiply(other.XY2d, _t);
        var d = Curve25519FieldElement.Add(_z, _z);

        return new CompletedPoint(
            Curve25519FieldElement.Subtract(a, b),
            Curve25519FieldElement.Add(a, b),
            Curve25519FieldElement.Add(d, c),
            Curve25519FieldElement.Subtract(d, c));
    }

    /// <summary>
    /// Subtracts a point in affine Niels form from this point, leaving the difference in completed coordinates.
    /// </summary>
    /// <param name="other">The point to subtract, in affine Niels form.</param>
    /// <returns>The difference of the two points, in completed coordinates.</returns>
    /// <remarks>
    /// Adds the negation (y − x, y + x, −2d·x·y) of <paramref name="other" />, reading its coordinates in the
    /// negation's order rather than forming it.
    /// </remarks>
    internal readonly CompletedPoint Subtract(in AffineNielsPoint other)
    {
        var a = Curve25519FieldElement.Multiply(Curve25519FieldElement.Add(_y, _x), other.YMinusX);
        var b = Curve25519FieldElement.Multiply(Curve25519FieldElement.Subtract(_y, _x), other.YPlusX);
        var c = Curve25519FieldElement.Multiply(other.XY2d, _t);
        var d = Curve25519FieldElement.Add(_z, _z);

        return new CompletedPoint(
            Curve25519FieldElement.Subtract(a, b),
            Curve25519FieldElement.Add(a, b),
            Curve25519FieldElement.Subtract(d, c),
            Curve25519FieldElement.Add(d, c));
    }

    /// <summary>
    /// Adds a point in projective Niels form to this point, leaving the sum in completed coordinates.
    /// </summary>
    /// <param name="other">The point to add, in projective Niels form.</param>
    /// <returns>The sum of the two points, in completed coordinates.</returns>
    /// <remarks>
    /// The unified addition of <see cref="Add(in Ed25519Point)" /> before its final multiplications, with the second
    /// point's sum, difference and product with 2d formed beforehand: four multiplications.
    /// </remarks>
    internal readonly CompletedPoint Add(in ProjectiveNielsPoint other)
    {
        var a = Curve25519FieldElement.Multiply(Curve25519FieldElement.Add(_y, _x), other.YPlusX);
        var b = Curve25519FieldElement.Multiply(Curve25519FieldElement.Subtract(_y, _x), other.YMinusX);
        var c = Curve25519FieldElement.Multiply(other.T2d, _t);
        var zz = Curve25519FieldElement.Multiply(_z, other.Z);
        var d = Curve25519FieldElement.Add(zz, zz);

        return new CompletedPoint(
            Curve25519FieldElement.Subtract(a, b),
            Curve25519FieldElement.Add(a, b),
            Curve25519FieldElement.Add(d, c),
            Curve25519FieldElement.Subtract(d, c));
    }

    /// <summary>
    /// Subtracts a point in projective Niels form from this point, leaving the difference in completed coordinates.
    /// </summary>
    /// <param name="other">The point to subtract, in projective Niels form.</param>
    /// <returns>The difference of the two points, in completed coordinates.</returns>
    /// <remarks>
    /// Adds the negation (Y − X, Y + X, Z, −2d·T) of <paramref name="other" />, reading its coordinates in the
    /// negation's order rather than forming it.
    /// </remarks>
    internal readonly CompletedPoint Subtract(in ProjectiveNielsPoint other)
    {
        var a = Curve25519FieldElement.Multiply(Curve25519FieldElement.Add(_y, _x), other.YMinusX);
        var b = Curve25519FieldElement.Multiply(Curve25519FieldElement.Subtract(_y, _x), other.YPlusX);
        var c = Curve25519FieldElement.Multiply(other.T2d, _t);
        var zz = Curve25519FieldElement.Multiply(_z, other.Z);
        var d = Curve25519FieldElement.Add(zz, zz);

        return new CompletedPoint(
            Curve25519FieldElement.Subtract(a, b),
            Curve25519FieldElement.Add(a, b),
            Curve25519FieldElement.Subtract(d, c),
            Curve25519FieldElement.Add(d, c));
    }

    /// <summary>
    /// Converts this point to projective Niels form (Y + X, Y − X, Z, 2d·T).
    /// </summary>
    /// <returns>The same point in projective Niels form.</returns>
    internal readonly ProjectiveNielsPoint ToProjectiveNiels() =>
        new(
            Curve25519FieldElement.Add(_y, _x),
            Curve25519FieldElement.Subtract(_y, _x),
            _z,
            Curve25519FieldElement.Multiply(_t, s_d2));

    /// <summary>
    /// Converts this point to projective coordinates by dropping the extended coordinate.
    /// </summary>
    /// <returns>The same point in projective coordinates (X : Y : Z).</returns>
    internal readonly ProjectivePoint ToProjective() =>
        new(_x, _y, _z);

    /// <summary>
    /// Doubles this point.
    /// </summary>
    /// <returns>The point added to itself.</returns>
    /// <remarks>
    /// <para>
    /// Uses the dedicated extended-coordinate doubling of Hisil, Wong, Carter, and Dawson (dbl-2008-hwcd) for a = −1:
    /// four squarings and four multiplications, where <see cref="Add(in Ed25519Point)" /> needs nine multiplications.
    /// It does not read T, and it is complete: the new Z is (y² − x²)(y² − x² − 2) up to a factor, and since d is a
    /// non-square neither factor can vanish for a point on the curve, so it is correct for every point, the identity
    /// and the points of small order included.
    /// </para>
    /// <para>
    /// The formula gives the point (E·F : G·H : F·G : E·H), where E = (X + Y)² − X² − Y², G = Y² − X², F = G − 2Z², and
    /// H = −(X² + Y²). This computes −F and −H instead, which negates all four coordinates and leaves the point
    /// unchanged.
    /// </para>
    /// </remarks>
    internal readonly Ed25519Point Double()
    {
        var xx = Curve25519FieldElement.Square(_x);
        var yy = Curve25519FieldElement.Square(_y);
        var zz = Curve25519FieldElement.Square(_z);
        var sum = Curve25519FieldElement.Square(Curve25519FieldElement.Add(_x, _y));

        var negatedH = Curve25519FieldElement.Add(xx, yy);
        var e = Curve25519FieldElement.Subtract(sum, negatedH);
        var g = Curve25519FieldElement.Subtract(yy, xx);

        // G is re-reduced because it is subtracted in turn, and Subtract requires tight operands.
        var negatedF = Curve25519FieldElement.Subtract(Curve25519FieldElement.Add(zz, zz), Curve25519FieldElement.Reduce(g));

        return new Ed25519Point(
            Curve25519FieldElement.Multiply(e, negatedF),
            Curve25519FieldElement.Multiply(g, negatedH),
            Curve25519FieldElement.Multiply(negatedF, g),
            Curve25519FieldElement.Multiply(e, negatedH));
    }

    /// <summary>
    /// Writes the canonical 32-byte RFC 8032 encoding of this point: the y coordinate in little-endian order with the
    /// sign of x stored in bit 255.
    /// </summary>
    /// <param name="destination">The 32-byte span that receives the encoding.</param>
    /// <exception cref="ArgumentException"><paramref name="destination" /> is not exactly 32 bytes.</exception>
    internal readonly void Encode(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, EncodedSizeInBytes);

        var zInverse = Curve25519FieldElement.Invert(_z);
        var x = Curve25519FieldElement.Multiply(_x, zInverse);
        var y = Curve25519FieldElement.Multiply(_y, zInverse);

        y.ToBytes(destination);

        if (x.IsNegative())
            destination[31] |= 0x80;
    }

    /// <summary>
    /// Writes the u-coordinate of this point's image on Curve25519 under the birational map u = (1 + y) / (1 − y), in
    /// the 32-byte little-endian encoding X25519 uses.
    /// </summary>
    /// <param name="destination">The 32-byte span that receives the encoding.</param>
    /// <exception cref="ArgumentException"><paramref name="destination" /> is not exactly 32 bytes.</exception>
    /// <remarks>
    /// In projective coordinates u = (Z + Y) / (Z − Y), so the map costs one inversion and one multiplication. The map
    /// carries the Ed25519 base point to the X25519 base point u = 9 and respects scalar multiplication. The identity,
    /// where Z = Y, has no image; it encodes as zero.
    /// </remarks>
    internal readonly void EncodeMontgomeryU(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, EncodedSizeInBytes);

        var u = Curve25519FieldElement.Multiply(
            Curve25519FieldElement.Add(_z, _y),
            Curve25519FieldElement.Invert(Curve25519FieldElement.Subtract(_z, _y)));

        u.ToBytes(destination);
    }

    /// <summary>
    /// Determines whether this point has small order, that is, whether it lies in the order-8 cofactor subgroup rather
    /// than the prime-order subgroup generated by the base point.
    /// </summary>
    /// <returns><see langword="true" /> when [8]P is the identity; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// The eight points whose order divides the cofactor satisfy [8]P = O. Rejecting them on key import and on the
    /// signature commitment R keeps Bodu's cofactorless verification self-consistent: a malleable small-order component
    /// can no longer be added to either input without changing the accepted-signature set.
    /// </remarks>
    internal readonly bool IsSmallOrder()
    {
        // [8]P via three doublings; a point of order dividing 8 collapses to the identity (0, 1), which in projective
        // coordinates is X = 0 with Y = Z, so no inversion is needed to recognize it.
        Ed25519Point multiple = Double().Double().Double();

        bool xIsZero = multiple._x.IsZeroConstantTime();
        bool yEqualsZ = AreEqual(multiple._y, multiple._z);

        return xIsZero & yEqualsZ;
    }

    /// <summary>
    /// Determines whether two points are the same point, comparing their projective coordinates without inverting.
    /// </summary>
    /// <param name="left">The first point.</param>
    /// <param name="right">The second point.</param>
    /// <returns><see langword="true" /> when the points are equal; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// Two points are equal when x₁ = x₂ and y₁ = y₂, that is, when X₁·Z₂ = X₂·Z₁ and Y₁·Z₂ = Y₂·Z₁: four
    /// multiplications, where comparing encodings would cost an inversion for each point. Both coordinates are compared
    /// on every call.
    /// </remarks>
    internal static bool AreEqual(in Ed25519Point left, in Ed25519Point right)
    {
        bool xEqual = AreEqual(
            Curve25519FieldElement.Multiply(left._x, right._z),
            Curve25519FieldElement.Multiply(right._x, left._z));
        bool yEqual = AreEqual(
            Curve25519FieldElement.Multiply(left._y, right._z),
            Curve25519FieldElement.Multiply(right._y, left._z));

        return xEqual & yEqual;
    }

    /// <summary>
    /// Negates this point, mapping (x, y) to (−x, y).
    /// </summary>
    /// <returns>The additive inverse of this point.</returns>
    internal readonly Ed25519Point Negate() =>
        new(
            Curve25519FieldElement.Subtract(Curve25519FieldElement.Zero, _x),
            _y,
            _z,
            Curve25519FieldElement.Subtract(Curve25519FieldElement.Zero, _t));

    /// <summary>
    /// Determines whether two field elements represent the same value modulo p by comparing their canonical encodings.
    /// </summary>
    /// <param name="left">The first element.</param>
    /// <param name="right">The second element.</param>
    /// <returns>
    /// <see langword="true" /> when the elements are congruent modulo p; otherwise, <see langword="false" />.
    /// </returns>
    private static bool AreEqual(Curve25519FieldElement left, Curve25519FieldElement right)
    {
        Span<byte> leftBytes = stackalloc byte[EncodedSizeInBytes];
        Span<byte> rightBytes = stackalloc byte[EncodedSizeInBytes];
        left.ToBytes(leftBytes);
        right.ToBytes(rightBytes);

        int difference = 0;
        for (int i = 0; i < EncodedSizeInBytes; i++)
            difference |= leftBytes[i] ^ rightBytes[i];

        return difference == 0;
    }

    /// <summary>
    /// Decodes a known-valid canonical point encoding during type initialization.
    /// </summary>
    /// <param name="hex">The canonical 32-byte encoding as a hex string.</param>
    /// <returns>The decoded point.</returns>
    /// <exception cref="InvalidOperationException">
    /// The constant fails to decode, indicating a build-time defect.
    /// </exception>
    private static Ed25519Point DecodeConstant(string hex) =>
        TryDecode(Convert.FromHexString(hex), out Ed25519Point point)
            ? point
            : throw new InvalidOperationException($"The built-in point constant '{hex}' failed to decode.");
}
