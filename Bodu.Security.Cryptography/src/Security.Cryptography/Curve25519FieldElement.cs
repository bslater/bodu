// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents an element of the prime field GF(2^255 − 19) used by the Curve25519 and Ed25519 algorithms, stored as
/// five 51-bit limbs in radix 2^51.
/// </summary>
/// <remarks>
/// <para>
/// All arithmetic is branch-free with respect to the element values so that secret-dependent timing variation is
/// avoided. Products are accumulated in <see cref="UInt128" /> before being carried back into 51-bit limbs.
/// </para>
/// <para>
/// <strong>Reduction contract.</strong> An element is <em>loosely reduced</em> when every limb is below 2^52.
/// <see cref="FromBytes" />, <see cref="Multiply" />, <see cref="Square" />, and <see cref="MultiplySmall" /> always
/// return loosely reduced values. <see cref="Add" /> and <see cref="Subtract" /> accept operands with limbs below 2^53
/// and produce limbs below 2^54 without re-reducing; callers must route such sums through a multiplication or
/// <see cref="ToBytes" /> before chaining further additions. The formulas used by the Montgomery ladder and the Edwards
/// point operations never stack more than one addition or subtraction between multiplications, so the limbs stay
/// comfortably within the bound required by <see cref="Multiply" />.
/// </para>
/// </remarks>
internal struct Curve25519FieldElement
{

    /// <summary>Limb 0 of the radix-2^51 representation (bits 0–50 of the element value).</summary>
    internal ulong L0;

    /// <summary>Limb 1 of the radix-2^51 representation (bits 51–101 of the element value).</summary>
    internal ulong L1;

    /// <summary>Limb 2 of the radix-2^51 representation (bits 102–152 of the element value).</summary>
    internal ulong L2;

    /// <summary>Limb 3 of the radix-2^51 representation (bits 153–203 of the element value).</summary>
    internal ulong L3;

    /// <summary>Limb 4 of the radix-2^51 representation (bits 204–254 of the element value).</summary>
    internal ulong L4;

    /// <summary>The number of bytes in the canonical little-endian encoding of a field element.</summary>
    internal const int EncodedSizeInBytes = 32;

    /// <summary>Mask isolating the low 51 bits of a limb.</summary>
    private const ulong LimbMask = (1UL << 51) - 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Curve25519FieldElement" /> struct from explicit limb values.
    /// </summary>
    /// <param name="l0">Limb 0 (bits 0–50).</param>
    /// <param name="l1">Limb 1 (bits 51–101).</param>
    /// <param name="l2">Limb 2 (bits 102–152).</param>
    /// <param name="l3">Limb 3 (bits 153–203).</param>
    /// <param name="l4">Limb 4 (bits 204–254).</param>
    internal Curve25519FieldElement(ulong l0, ulong l1, ulong l2, ulong l3, ulong l4)
    {
        L0 = l0;
        L1 = l1;
        L2 = l2;
        L3 = l3;
        L4 = l4;
    }

    /// <summary>
    /// Gets the multiplicative identity (1) of the field.
    /// </summary>
    /// <returns>A field element whose value is one.</returns>
    internal static Curve25519FieldElement One =>
        new(1, 0, 0, 0, 0);

    /// <summary>
    /// Gets the additive identity (0) of the field.
    /// </summary>
    /// <returns>A field element whose value is zero.</returns>
    internal static Curve25519FieldElement Zero =>
        default;

    /// <summary>
    /// Adds two field elements limb-wise without reducing.
    /// </summary>
    /// <param name="left">The first addend. Limbs must be below 2^53.</param>
    /// <param name="right">The second addend. Limbs must be below 2^53.</param>
    /// <returns>The sum with limbs below 2^54.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Curve25519FieldElement Add(in Curve25519FieldElement left, in Curve25519FieldElement right) =>
        new(left.L0 + right.L0, left.L1 + right.L1, left.L2 + right.L2, left.L3 + right.L3, left.L4 + right.L4);

    /// <summary>
    /// Copies <paramref name="source" /> into <paramref name="destination" /> when <paramref name="condition" /> is 1,
    /// and leaves <paramref name="destination" /> unchanged when it is 0, without a data-dependent branch.
    /// </summary>
    /// <param name="destination">The element conditionally overwritten.</param>
    /// <param name="source">The element conditionally copied.</param>
    /// <param name="condition">The move condition. Must be exactly 0 or 1.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ConditionalMove(ref Curve25519FieldElement destination, in Curve25519FieldElement source, ulong condition)
    {
        ulong mask = 0UL - condition;

        destination.L0 ^= mask & (destination.L0 ^ source.L0);
        destination.L1 ^= mask & (destination.L1 ^ source.L1);
        destination.L2 ^= mask & (destination.L2 ^ source.L2);
        destination.L3 ^= mask & (destination.L3 ^ source.L3);
        destination.L4 ^= mask & (destination.L4 ^ source.L4);
    }

    /// <summary>
    /// Swaps two field elements in place when <paramref name="condition" /> is 1, and leaves them unchanged when it is
    /// 0, without a data-dependent branch.
    /// </summary>
    /// <param name="left">The first element, swapped with <paramref name="right" /> when the condition is set.</param>
    /// <param name="right">The second element, swapped with <paramref name="left" /> when the condition is set.</param>
    /// <param name="condition">The swap condition. Must be exactly 0 or 1.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ConditionalSwap(ref Curve25519FieldElement left, ref Curve25519FieldElement right, ulong condition)
    {
        ulong mask = 0UL - condition;

        ulong x0 = mask & (left.L0 ^ right.L0);
        ulong x1 = mask & (left.L1 ^ right.L1);
        ulong x2 = mask & (left.L2 ^ right.L2);
        ulong x3 = mask & (left.L3 ^ right.L3);
        ulong x4 = mask & (left.L4 ^ right.L4);

        left.L0 ^= x0;
        left.L1 ^= x1;
        left.L2 ^= x2;
        left.L3 ^= x3;
        left.L4 ^= x4;

        right.L0 ^= x0;
        right.L1 ^= x1;
        right.L2 ^= x2;
        right.L3 ^= x3;
        right.L4 ^= x4;
    }

    /// <summary>
    /// Decodes a 32-byte little-endian encoding into a loosely reduced field element, ignoring the most significant bit
    /// of the final byte as required by RFC 7748 and RFC 8032.
    /// </summary>
    /// <param name="source">The 32-byte little-endian encoding to decode.</param>
    /// <returns>The decoded field element with all limbs below 2^51.</returns>
    /// <exception cref="ArgumentException"><paramref name="source" /> is not exactly 32 bytes long.</exception>
    /// <remarks>
    /// The decoded value may be non-canonical (in the range [p, 2^255)); subsequent arithmetic and
    /// <see cref="ToBytes" /> reduce it correctly modulo p.
    /// </remarks>
    internal static Curve25519FieldElement FromBytes(ReadOnlySpan<byte> source)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(source, EncodedSizeInBytes);

        // Overlapping 64-bit reads shifted into place; the final shift-and-mask discards bit 255.
        return new Curve25519FieldElement(
            BinaryPrimitives.ReadUInt64LittleEndian(source[..8]) & LimbMask,
            (BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(6, 8)) >> 3) & LimbMask,
            (BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(12, 8)) >> 6) & LimbMask,
            (BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(19, 8)) >> 1) & LimbMask,
            (BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(24, 8)) >> 12) & LimbMask);
    }

    /// <summary>
    /// Computes the multiplicative inverse of a field element by raising it to p − 2.
    /// </summary>
    /// <param name="value">The element to invert. Limbs must be below 2^54.</param>
    /// <returns>The inverse of <paramref name="value" />, or zero when <paramref name="value" /> is zero.</returns>
    /// <remarks>
    /// Uses the standard 254-squaring addition chain for 2^255 − 21 from the ref10 implementation. The exponent is
    /// fixed, so the operation is constant-time regardless of the element value.
    /// </remarks>
    internal static Curve25519FieldElement Invert(in Curve25519FieldElement value)
    {
        Curve25519FieldElement z = value;

        Curve25519FieldElement t0 = Square(z);                       // z^2
        Curve25519FieldElement t1 = Square(t0);
        t1 = Square(t1);                                              // z^8
        t1 = Multiply(z, t1);                                         // z^9
        t0 = Multiply(t0, t1);                                        // z^11
        Curve25519FieldElement t2 = Square(t0);                       // z^22
        t1 = Multiply(t1, t2);                                        // z^31 = z^(2^5 - 1)

        t2 = Square(t1);
        for (int i = 1; i < 5; i++)
            t2 = Square(t2);
        t1 = Multiply(t2, t1);                                        // z^(2^10 - 1)

        t2 = Square(t1);
        for (int i = 1; i < 10; i++)
            t2 = Square(t2);
        t2 = Multiply(t2, t1);                                        // z^(2^20 - 1)

        Curve25519FieldElement t3 = Square(t2);
        for (int i = 1; i < 20; i++)
            t3 = Square(t3);
        t2 = Multiply(t3, t2);                                        // z^(2^40 - 1)

        t2 = Square(t2);
        for (int i = 1; i < 10; i++)
            t2 = Square(t2);
        t1 = Multiply(t2, t1);                                        // z^(2^50 - 1)

        t2 = Square(t1);
        for (int i = 1; i < 50; i++)
            t2 = Square(t2);
        t2 = Multiply(t2, t1);                                        // z^(2^100 - 1)

        t3 = Square(t2);
        for (int i = 1; i < 100; i++)
            t3 = Square(t3);
        t2 = Multiply(t3, t2);                                        // z^(2^200 - 1)

        t2 = Square(t2);
        for (int i = 1; i < 50; i++)
            t2 = Square(t2);
        t1 = Multiply(t2, t1);                                        // z^(2^250 - 1)

        t1 = Square(t1);
        for (int i = 1; i < 5; i++)
            t1 = Square(t1);

        return Multiply(t1, t0);                                      // z^(2^255 - 21) = z^(p - 2)
    }

    /// <summary>
    /// Multiplies two field elements modulo p, returning a loosely reduced product.
    /// </summary>
    /// <param name="left">The first factor. Limbs must be below 2^54.</param>
    /// <param name="right">The second factor. Limbs must be below 2^54.</param>
    /// <returns>The product with all limbs below 2^52.</returns>
    /// <remarks>
    /// Uses the schoolbook 5×5 limb product with the high limbs folded back through the identity 2^255 ≡ 19 (mod p).
    /// Partial products are accumulated in <see cref="UInt128" />, which cannot overflow for the permitted operand
    /// bounds.
    /// </remarks>
    internal static Curve25519FieldElement Multiply(in Curve25519FieldElement left, in Curve25519FieldElement right)
    {
        ulong f0 = left.L0, f1 = left.L1, f2 = left.L2, f3 = left.L3, f4 = left.L4;
        ulong g0 = right.L0, g1 = right.L1, g2 = right.L2, g3 = right.L3, g4 = right.L4;

        UInt128 t0 = ((UInt128)f0 * g0)
                   + (UInt128)19 * (((UInt128)f1 * g4) + ((UInt128)f2 * g3) + ((UInt128)f3 * g2) + ((UInt128)f4 * g1));
        UInt128 t1 = ((UInt128)f0 * g1) + ((UInt128)f1 * g0)
                   + (UInt128)19 * (((UInt128)f2 * g4) + ((UInt128)f3 * g3) + ((UInt128)f4 * g2));
        UInt128 t2 = ((UInt128)f0 * g2) + ((UInt128)f1 * g1) + ((UInt128)f2 * g0)
                   + (UInt128)19 * (((UInt128)f3 * g4) + ((UInt128)f4 * g3));
        UInt128 t3 = ((UInt128)f0 * g3) + ((UInt128)f1 * g2) + ((UInt128)f2 * g1) + ((UInt128)f3 * g0)
                   + (UInt128)19 * ((UInt128)f4 * g4);
        UInt128 t4 = ((UInt128)f0 * g4) + ((UInt128)f1 * g3) + ((UInt128)f2 * g2) + ((UInt128)f3 * g1) + ((UInt128)f4 * g0);

        return CarryReduce(t0, t1, t2, t3, t4);
    }

    /// <summary>
    /// Multiplies a field element by a small unsigned constant modulo p.
    /// </summary>
    /// <param name="value">The element to scale. Limbs must be below 2^54.</param>
    /// <param name="factor">The small constant factor, such as the curve constant 121665.</param>
    /// <returns>The scaled element with all limbs below 2^52.</returns>
    internal static Curve25519FieldElement MultiplySmall(in Curve25519FieldElement value, uint factor)
    {
        UInt128 t0 = (UInt128)value.L0 * factor;
        UInt128 t1 = (UInt128)value.L1 * factor;
        UInt128 t2 = (UInt128)value.L2 * factor;
        UInt128 t3 = (UInt128)value.L3 * factor;
        UInt128 t4 = (UInt128)value.L4 * factor;

        return CarryReduce(t0, t1, t2, t3, t4);
    }

    /// <summary>
    /// Raises a field element to the power (p − 5) / 8 = 2^252 − 3, the exponent used to compute square roots during
    /// Ed25519 point decompression.
    /// </summary>
    /// <param name="value">The base element. Limbs must be below 2^54.</param>
    /// <returns><paramref name="value" /> raised to 2^252 − 3.</returns>
    /// <remarks>
    /// Uses the fixed ref10 addition chain; the operation is constant-time regardless of the element value.
    /// </remarks>
    internal static Curve25519FieldElement Pow22523(in Curve25519FieldElement value)
    {
        Curve25519FieldElement z = value;

        Curve25519FieldElement t0 = Square(z);                        // z^2
        Curve25519FieldElement t1 = Square(t0);
        t1 = Square(t1);                                              // z^8
        t1 = Multiply(z, t1);                                         // z^9
        t0 = Multiply(t0, t1);                                        // z^11
        t0 = Square(t0);                                              // z^22
        t0 = Multiply(t1, t0);                                        // z^31 = z^(2^5 - 1)

        t1 = Square(t0);
        for (int i = 1; i < 5; i++)
            t1 = Square(t1);
        t0 = Multiply(t1, t0);                                        // z^(2^10 - 1)

        t1 = Square(t0);
        for (int i = 1; i < 10; i++)
            t1 = Square(t1);
        t1 = Multiply(t1, t0);                                        // z^(2^20 - 1)

        Curve25519FieldElement t2 = Square(t1);
        for (int i = 1; i < 20; i++)
            t2 = Square(t2);
        t1 = Multiply(t2, t1);                                        // z^(2^40 - 1)

        t1 = Square(t1);
        for (int i = 1; i < 10; i++)
            t1 = Square(t1);
        t0 = Multiply(t1, t0);                                        // z^(2^50 - 1)

        t1 = Square(t0);
        for (int i = 1; i < 50; i++)
            t1 = Square(t1);
        t1 = Multiply(t1, t0);                                        // z^(2^100 - 1)

        t2 = Square(t1);
        for (int i = 1; i < 100; i++)
            t2 = Square(t2);
        t1 = Multiply(t2, t1);                                        // z^(2^200 - 1)

        t1 = Square(t1);
        for (int i = 1; i < 50; i++)
            t1 = Square(t1);
        t0 = Multiply(t1, t0);                                        // z^(2^250 - 1)

        t0 = Square(t0);
        t0 = Square(t0);                                              // z^(2^252 - 4)

        return Multiply(t0, z);                                       // z^(2^252 - 3)
    }

    /// <summary>
    /// Carries a loosely accumulated element back into tight reduction, bringing every limb below 2^52.
    /// </summary>
    /// <param name="value">The element to normalize. Limbs may be as large as 2^63.</param>
    /// <returns>An equivalent element with all limbs below 2^52.</returns>
    /// <remarks>
    /// Use this before handing an <see cref="Add" /> or <see cref="Subtract" /> result back into another addition or
    /// subtraction, whose operand bound (limbs below 2^53) a chained loose value would otherwise violate.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Curve25519FieldElement Reduce(in Curve25519FieldElement value) =>
        CarryReduce(value.L0, value.L1, value.L2, value.L3, value.L4);

    /// <summary>
    /// Squares a field element modulo p, returning a loosely reduced result.
    /// </summary>
    /// <param name="value">The element to square. Limbs must be below 2^54.</param>
    /// <returns>The square with all limbs below 2^52.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Curve25519FieldElement Square(in Curve25519FieldElement value) =>
        Multiply(value, value);

    /// <summary>
    /// Subtracts <paramref name="right" /> from <paramref name="left" /> limb-wise, biasing by 4p to keep every limb
    /// non-negative without branching.
    /// </summary>
    /// <param name="left">The minuend. Limbs must be below 2^53.</param>
    /// <param name="right">The subtrahend. Limbs must be below 2^53.</param>
    /// <returns>The difference with limbs below 2^54.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Curve25519FieldElement Subtract(in Curve25519FieldElement left, in Curve25519FieldElement right)
    {
        // 4p in radix 2^51: (2^53 - 76, 2^53 - 4, 2^53 - 4, 2^53 - 4, 2^53 - 4). Adding it keeps each limb
        // non-negative for any subtrahend limb below 2^53, so the subtraction never borrows.
        const ulong Four0 = (1UL << 53) - 76;
        const ulong FourN = (1UL << 53) - 4;

        return new Curve25519FieldElement(
            left.L0 + Four0 - right.L0,
            left.L1 + FourN - right.L1,
            left.L2 + FourN - right.L2,
            left.L3 + FourN - right.L3,
            left.L4 + FourN - right.L4);
    }

    /// <summary>
    /// Determines whether the canonical representative of this element is negative, defined by RFC 8032 as having its
    /// least significant bit set.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the canonical encoding is odd; otherwise, <see langword="false" />.
    /// </returns>
    internal readonly bool IsNegative()
    {
        Span<byte> encoded = stackalloc byte[EncodedSizeInBytes];
        ToBytes(encoded);

        bool negative = (encoded[0] & 1) == 1;
        CryptographyHelper.Clear(encoded);

        return negative;
    }

    /// <summary>
    /// Determines whether this element is zero modulo p, accumulating over the full canonical encoding so that the
    /// comparison time does not depend on the value.
    /// </summary>
    /// <returns><see langword="true" /> when the element is zero; otherwise, <see langword="false" />.</returns>
    internal readonly bool IsZeroConstantTime()
    {
        Span<byte> encoded = stackalloc byte[EncodedSizeInBytes];
        ToBytes(encoded);

        int accumulator = 0;
        for (int i = 0; i < encoded.Length; i++)
            accumulator |= encoded[i];

        CryptographyHelper.Clear(encoded);

        return accumulator == 0;
    }

    /// <summary>
    /// Writes the canonical 32-byte little-endian encoding of this element, fully reduced modulo p, into
    /// <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">The 32-byte span that receives the canonical encoding.</param>
    /// <exception cref="ArgumentException"><paramref name="destination" /> is not exactly 32 bytes long.</exception>
    /// <remarks>
    /// The encoding always has its most significant bit clear because the canonical representative is below 2^255 − 19.
    /// The reduction is branch-free.
    /// </remarks>
    internal readonly void ToBytes(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, EncodedSizeInBytes);

        ulong t0 = L0, t1 = L1, t2 = L2, t3 = L3, t4 = L4;

        // Two carry passes bring every limb below 2^51 (plus a tiny excess on t0), so the value is below 2p.
        for (int pass = 0; pass < 2; pass++)
        {
            t1 += t0 >> 51;
            t0 &= LimbMask;
            t2 += t1 >> 51;
            t1 &= LimbMask;
            t3 += t2 >> 51;
            t2 &= LimbMask;
            t4 += t3 >> 51;
            t3 &= LimbMask;
            t0 += 19 * (t4 >> 51);
            t4 &= LimbMask;
        }

        // Compute q = 1 when the value is >= p, else 0, by propagating the carry of (value + 19) past bit 254;
        // adding 19q then dropping bit 255 subtracts q * p without a data-dependent branch.
        ulong q = (t0 + 19) >> 51;
        q = (t1 + q) >> 51;
        q = (t2 + q) >> 51;
        q = (t3 + q) >> 51;
        q = (t4 + q) >> 51;

        t0 += 19 * q;
        t1 += t0 >> 51;
        t0 &= LimbMask;
        t2 += t1 >> 51;
        t1 &= LimbMask;
        t3 += t2 >> 51;
        t2 &= LimbMask;
        t4 += t3 >> 51;
        t3 &= LimbMask;
        t4 &= LimbMask;

        BinaryPrimitives.WriteUInt64LittleEndian(destination[..8], t0 | (t1 << 51));
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8, 8), (t1 >> 13) | (t2 << 38));
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(16, 8), (t2 >> 26) | (t3 << 25));
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(24, 8), (t3 >> 39) | (t4 << 12));
    }

    /// <summary>
    /// Carries the wide partial sums of a limb product back into a loosely reduced element, folding the bit-255
    /// overflow through 2^255 ≡ 19 (mod p).
    /// </summary>
    /// <param name="t0">Wide partial sum for limb 0.</param>
    /// <param name="t1">Wide partial sum for limb 1.</param>
    /// <param name="t2">Wide partial sum for limb 2.</param>
    /// <param name="t3">Wide partial sum for limb 3.</param>
    /// <param name="t4">Wide partial sum for limb 4.</param>
    /// <returns>The reduced element with all limbs below 2^52.</returns>
    private static Curve25519FieldElement CarryReduce(UInt128 t0, UInt128 t1, UInt128 t2, UInt128 t3, UInt128 t4)
    {
        ulong r0 = (ulong)t0 & LimbMask;
        ulong carry = (ulong)(t0 >> 51);

        t1 += carry;
        ulong r1 = (ulong)t1 & LimbMask;
        carry = (ulong)(t1 >> 51);

        t2 += carry;
        ulong r2 = (ulong)t2 & LimbMask;
        carry = (ulong)(t2 >> 51);

        t3 += carry;
        ulong r3 = (ulong)t3 & LimbMask;
        carry = (ulong)(t3 >> 51);

        t4 += carry;
        ulong r4 = (ulong)t4 & LimbMask;
        carry = (ulong)(t4 >> 51);

        // Fold the overflow above bit 254 back into limb 0 (2^255 ≡ 19), then settle the remaining small carries.
        r0 += carry * 19;
        carry = r0 >> 51;
        r0 &= LimbMask;

        r1 += carry;
        carry = r1 >> 51;
        r1 &= LimbMask;

        r2 += carry;

        return new Curve25519FieldElement(r0, r1, r2, r3, r4);
    }
}
