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
/// pair, including doubling and the identity, so no secret-dependent branching is required. This trades the peak speed
/// of the ref10 formula set for a substantially smaller and more reviewable implementation.
/// </para>
/// </remarks>
internal partial struct Ed25519Point
{
    /// <summary>
    /// The size, in bytes, of an encoded point.
    /// </summary>
    internal const int EncodedSizeInBytes = 32;

    /// <summary>
    /// The curve constant d = −121665/121666 mod p, decoded from its canonical encoding at type initialization.
    /// </summary>
    private static readonly Curve25519FieldElement s_d = Curve25519FieldElement.FromBytes(
        Convert.FromHexString("a3785913ca4deb75abd841414d0a700098e879777940c78c73fe6f2bee6c0352"));

    /// <summary>
    /// The doubled curve constant 2d used by the unified addition formula.
    /// </summary>
    private static readonly Curve25519FieldElement s_d2 = Curve25519FieldElement.Add(s_d, s_d);

    /// <summary>
    /// The square root of −1 modulo p, used to correct the candidate root during point decompression.
    /// </summary>
    private static readonly Curve25519FieldElement s_sqrtMinusOne = Curve25519FieldElement.FromBytes(
        Convert.FromHexString("b0a00e4a271beec478e42fad0618432fa7d7fb3d99004d2b0bdfc14f8024832b"));

    /// <summary>
    /// The Ed25519 base point B = (x, 4/5) with x positive, decoded from its canonical encoding. Declared after the
    /// curve constants in this file because static field initializers run in declaration order and the decoder consumes
    /// <see cref="s_d" /> and <see cref="s_sqrtMinusOne" />.
    /// </summary>
    private static readonly Ed25519Point s_basePoint = DecodeConstant(
        "5866666666666666666666666666666666666666666666666666666666666666");

    /// <summary>
    /// The X coordinate of the extended representation.
    /// </summary>
    private Curve25519FieldElement _x;

    /// <summary>
    /// The Y coordinate of the extended representation.
    /// </summary>
    private Curve25519FieldElement _y;

    /// <summary>
    /// The Z coordinate of the extended representation.
    /// </summary>
    private Curve25519FieldElement _z;

    /// <summary>
    /// The extended coordinate T = XY/Z.
    /// </summary>
    private Curve25519FieldElement _t;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ed25519Point" /> struct from explicit extended coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="t">The extended coordinate T = XY/Z.</param>
    private Ed25519Point(
        in Curve25519FieldElement x,
        in Curve25519FieldElement y,
        in Curve25519FieldElement z,
        in Curve25519FieldElement t)
    {
        _x = x;
        _y = y;
        _z = z;
        _t = t;
    }

    /// <summary>
    /// Gets the identity element (0, 1) of the curve group.
    /// </summary>
    /// <returns>The neutral point in extended coordinates (0 : 1 : 1 : 0).</returns>
    internal static Ed25519Point Identity =>
        new(Curve25519FieldElement.Zero, Curve25519FieldElement.One, Curve25519FieldElement.One, Curve25519FieldElement.Zero);

    /// <summary>
    /// Gets the Ed25519 base point B defined by RFC 8032.
    /// </summary>
    /// <returns>The generator of the prime-order subgroup.</returns>
    internal static Ed25519Point BasePoint =>
        s_basePoint;

    /// <summary>
    /// Adds this point to <paramref name="other" /> using the complete unified extended-coordinate formula.
    /// </summary>
    /// <param name="other">The point to add.</param>
    /// <returns>The sum of the two points.</returns>
    internal readonly Ed25519Point Add(in Ed25519Point other)
    {
        Curve25519FieldElement a = Curve25519FieldElement.Multiply(
            Curve25519FieldElement.Subtract(_y, _x),
            Curve25519FieldElement.Subtract(other._y, other._x));
        Curve25519FieldElement b = Curve25519FieldElement.Multiply(
            Curve25519FieldElement.Add(_y, _x),
            Curve25519FieldElement.Add(other._y, other._x));
        Curve25519FieldElement c = Curve25519FieldElement.Multiply(Curve25519FieldElement.Multiply(_t, s_d2), other._t);
        Curve25519FieldElement zz = Curve25519FieldElement.Multiply(_z, other._z);
        Curve25519FieldElement d = Curve25519FieldElement.Add(zz, zz);

        Curve25519FieldElement e = Curve25519FieldElement.Subtract(b, a);
        Curve25519FieldElement f = Curve25519FieldElement.Subtract(d, c);
        Curve25519FieldElement g = Curve25519FieldElement.Add(d, c);
        Curve25519FieldElement h = Curve25519FieldElement.Add(b, a);

        return new Ed25519Point(
            Curve25519FieldElement.Multiply(e, f),
            Curve25519FieldElement.Multiply(g, h),
            Curve25519FieldElement.Multiply(f, g),
            Curve25519FieldElement.Multiply(e, h));
    }

    /// <summary>
    /// Doubles this point.
    /// </summary>
    /// <returns>The point added to itself.</returns>
    /// <remarks>
    /// Delegates to the complete <see cref="Add" /> formula, which is valid for equal operands; the dedicated doubling
    /// formula is omitted in favor of a single audited code path.
    /// </remarks>
    internal readonly Ed25519Point Double() =>
        Add(this);

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
    /// Writes the canonical 32-byte RFC 8032 encoding of this point: the y coordinate in little-endian order with the
    /// sign of x stored in bit 255.
    /// </summary>
    /// <param name="destination">The 32-byte span that receives the encoding.</param>
    /// <exception cref="ArgumentException"><paramref name="destination" /> is not exactly 32 bytes.</exception>
    internal readonly void Encode(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, EncodedSizeInBytes);

        Curve25519FieldElement zInverse = Curve25519FieldElement.Invert(_z);
        Curve25519FieldElement x = Curve25519FieldElement.Multiply(_x, zInverse);
        Curve25519FieldElement y = Curve25519FieldElement.Multiply(_y, zInverse);

        y.ToBytes(destination);

        if (x.IsNegative())
            destination[31] |= 0x80;
    }

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
        var sign = (encoded[31] & 0x80) != 0;

        // FromBytes ignores bit 255; a canonical y must round-trip to the encoding with the sign bit cleared.
        Curve25519FieldElement y = Curve25519FieldElement.FromBytes(encoded);
        Span<byte> canonical = stackalloc byte[EncodedSizeInBytes];
        y.ToBytes(canonical);

        var mismatch = 0;
        for (var i = 0; i < EncodedSizeInBytes - 1; i++)
            mismatch |= canonical[i] ^ encoded[i];
        mismatch |= canonical[31] ^ (encoded[31] & 0x7F);

        if (mismatch != 0)
            return false;

        // Solve x² = (y² − 1) / (d·y² + 1) via the combined exponentiation x = u·v³·(u·v⁷)^((p−5)/8). The
        // subtraction result is re-reduced because u is later negated, and Subtract requires tight operands.
        Curve25519FieldElement y2 = Curve25519FieldElement.Square(y);
        Curve25519FieldElement u = Curve25519FieldElement.Reduce(
            Curve25519FieldElement.Subtract(y2, Curve25519FieldElement.One));
        Curve25519FieldElement v = Curve25519FieldElement.Add(Curve25519FieldElement.Multiply(s_d, y2), Curve25519FieldElement.One);

        Curve25519FieldElement v3 = Curve25519FieldElement.Multiply(Curve25519FieldElement.Square(v), v);
        Curve25519FieldElement v7 = Curve25519FieldElement.Multiply(Curve25519FieldElement.Square(v3), v);
        Curve25519FieldElement x = Curve25519FieldElement.Multiply(
            Curve25519FieldElement.Multiply(u, v3),
            Curve25519FieldElement.Pow22523(Curve25519FieldElement.Multiply(u, v7)));

        Curve25519FieldElement vx2 = Curve25519FieldElement.Multiply(v, Curve25519FieldElement.Square(x));

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
    /// Determines whether two field elements represent the same value modulo p by comparing their canonical encodings.
    /// </summary>
    /// <param name="left">The first element.</param>
    /// <param name="right">The second element.</param>
    /// <returns>
    /// <see langword="true" /> when the elements are congruent modulo p; otherwise, <see langword="false" />.
    /// </returns>
    private static bool AreEqual(in Curve25519FieldElement left, in Curve25519FieldElement right)
    {
        Span<byte> leftBytes = stackalloc byte[EncodedSizeInBytes];
        Span<byte> rightBytes = stackalloc byte[EncodedSizeInBytes];
        left.ToBytes(leftBytes);
        right.ToBytes(rightBytes);

        var difference = 0;
        for (var i = 0; i < EncodedSizeInBytes; i++)
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
