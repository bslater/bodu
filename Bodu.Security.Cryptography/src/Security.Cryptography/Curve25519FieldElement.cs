// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents an element of the prime field GF(2^255 − 19) used by the Curve25519 and Ed25519 algorithms, stored as
/// five 51-bit limbs in radix 2^51.
/// </summary>
/// <remarks>
/// <para>
/// All arithmetic is branch-free with respect to the element values so that secret-dependent timing variation is
/// avoided.
/// </para>
/// <para>
/// A product is formed from 25 limb products, a square from 15. Limb products that land at or above 2^255 fold back
/// through 2^255 ≡ 19 (mod p); the factor is scaled by 19 beforehand, so every limb product is a single 64 × 64-bit
/// multiplication. Each is split at bit 51 as it is formed, and each result limb is summed in two 64-bit parts, the
/// products' low 51 bits and the products shifted right 51 bits, so no 128-bit arithmetic is needed.
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
internal readonly struct Curve25519FieldElement
{
    /// <summary>The number of bytes in the canonical little-endian encoding of a field element.</summary>
    internal const int EncodedSizeInBytes = 32;

    /// <summary>Limb 0 of the radix-2^51 representation (bits 0-50 of the element value).</summary>
    internal readonly ulong _l0;

    /// <summary>Limb 1 of the radix-2^51 representation (bits 51-101 of the element value).</summary>
    internal readonly ulong _l1;

    /// <summary>Limb 2 of the radix-2^51 representation (bits 102-152 of the element value).</summary>
    internal readonly ulong _l2;

    /// <summary>Limb 3 of the radix-2^51 representation (bits 153-203 of the element value).</summary>
    internal readonly ulong _l3;

    /// <summary>Limb 4 of the radix-2^51 representation (bits 204-254 of the element value).</summary>
    internal readonly ulong _l4;

    /// <summary>Mask isolating the low 51 bits of a limb.</summary>
    private const ulong LimbMask = (1UL << 51) - 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Curve25519FieldElement" /> struct from explicit limb values.
    /// </summary>
    /// <param name="l0">Limb 0 (bits 0-50).</param>
    /// <param name="l1">Limb 1 (bits 51-101).</param>
    /// <param name="l2">Limb 2 (bits 102-152).</param>
    /// <param name="l3">Limb 3 (bits 153-203).</param>
    /// <param name="l4">Limb 4 (bits 204-254).</param>
    internal Curve25519FieldElement(ulong l0, ulong l1, ulong l2, ulong l3, ulong l4)
    {
        _l0 = l0;
        _l1 = l1;
        _l2 = l2;
        _l3 = l3;
        _l4 = l4;
    }

    /// <summary>
    /// Gets the multiplicative identity (1) of the field.
    /// </summary>
    /// <value>A field element whose value is one.</value>
    internal static Curve25519FieldElement One =>
        new(1, 0, 0, 0, 0);

    /// <summary>
    /// Gets the additive identity (0) of the field.
    /// </summary>
    /// <value>A field element whose value is zero.</value>
    internal static Curve25519FieldElement Zero =>
        default;

    /// <summary>
    /// Adds two field elements limb-wise without reducing.
    /// </summary>
    /// <param name="left">The first addend. Limbs must be below 2^53.</param>
    /// <param name="right">The second addend. Limbs must be below 2^53.</param>
    /// <returns>The sum with limbs below 2^54.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Curve25519FieldElement Add(Curve25519FieldElement left, Curve25519FieldElement right) =>
        new(left._l0 + right._l0, left._l1 + right._l1, left._l2 + right._l2, left._l3 + right._l3, left._l4 + right._l4);

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

        destination = new Curve25519FieldElement(
            destination._l0 ^ (mask & (destination._l0 ^ source._l0)),
            destination._l1 ^ (mask & (destination._l1 ^ source._l1)),
            destination._l2 ^ (mask & (destination._l2 ^ source._l2)),
            destination._l3 ^ (mask & (destination._l3 ^ source._l3)),
            destination._l4 ^ (mask & (destination._l4 ^ source._l4)));
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

        ulong x0 = mask & (left._l0 ^ right._l0);
        ulong x1 = mask & (left._l1 ^ right._l1);
        ulong x2 = mask & (left._l2 ^ right._l2);
        ulong x3 = mask & (left._l3 ^ right._l3);
        ulong x4 = mask & (left._l4 ^ right._l4);

        left = new Curve25519FieldElement(left._l0 ^ x0, left._l1 ^ x1, left._l2 ^ x2, left._l3 ^ x3, left._l4 ^ x4);
        right = new Curve25519FieldElement(right._l0 ^ x0, right._l1 ^ x1, right._l2 ^ x2, right._l3 ^ x3, right._l4 ^ x4);
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
    internal static Curve25519FieldElement Invert(Curve25519FieldElement value)
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
    /// The operand bounds keep every limb product below 2^113 and every part of a result limb's sum below 2^64.
    /// </remarks>
    internal static Curve25519FieldElement Multiply(in Curve25519FieldElement left, in Curve25519FieldElement right)
    {
        ulong f0 = left._l0, f1 = left._l1, f2 = left._l2, f3 = left._l3, f4 = left._l4;
        ulong g0 = right._l0, g1 = right._l1, g2 = right._l2, g3 = right._l3, g4 = right._l4;
        ulong g1By19 = 19 * g1, g2By19 = 19 * g2, g3By19 = 19 * g3, g4By19 = 19 * g4;

        ulong low0 = 0, high0 = 0;
        AddProduct(f0, g0, ref low0, ref high0);
        AddProduct(f1, g4By19, ref low0, ref high0);
        AddProduct(f2, g3By19, ref low0, ref high0);
        AddProduct(f3, g2By19, ref low0, ref high0);
        AddProduct(f4, g1By19, ref low0, ref high0);

        ulong low1 = 0, high1 = 0;
        AddProduct(f0, g1, ref low1, ref high1);
        AddProduct(f1, g0, ref low1, ref high1);
        AddProduct(f2, g4By19, ref low1, ref high1);
        AddProduct(f3, g3By19, ref low1, ref high1);
        AddProduct(f4, g2By19, ref low1, ref high1);

        ulong low2 = 0, high2 = 0;
        AddProduct(f0, g2, ref low2, ref high2);
        AddProduct(f1, g1, ref low2, ref high2);
        AddProduct(f2, g0, ref low2, ref high2);
        AddProduct(f3, g4By19, ref low2, ref high2);
        AddProduct(f4, g3By19, ref low2, ref high2);

        ulong low3 = 0, high3 = 0;
        AddProduct(f0, g3, ref low3, ref high3);
        AddProduct(f1, g2, ref low3, ref high3);
        AddProduct(f2, g1, ref low3, ref high3);
        AddProduct(f3, g0, ref low3, ref high3);
        AddProduct(f4, g4By19, ref low3, ref high3);

        ulong low4 = 0, high4 = 0;
        AddProduct(f0, g4, ref low4, ref high4);
        AddProduct(f1, g3, ref low4, ref high4);
        AddProduct(f2, g2, ref low4, ref high4);
        AddProduct(f3, g1, ref low4, ref high4);
        AddProduct(f4, g0, ref low4, ref high4);

        return Carry(low0, high0, low1, high1, low2, high2, low3, high3, low4, high4);
    }

    /// <summary>
    /// Multiplies a field element by a small unsigned constant modulo p.
    /// </summary>
    /// <param name="value">The element to scale. Limbs must be below 2^54.</param>
    /// <param name="factor">The small constant factor, such as the curve constant 121665.</param>
    /// <returns>The scaled element with all limbs below 2^52.</returns>
    internal static Curve25519FieldElement MultiplySmall(in Curve25519FieldElement value, uint factor)
    {
        ulong low0 = 0, high0 = 0, low1 = 0, high1 = 0, low2 = 0, high2 = 0, low3 = 0, high3 = 0, low4 = 0, high4 = 0;
        AddProduct(value._l0, factor, ref low0, ref high0);
        AddProduct(value._l1, factor, ref low1, ref high1);
        AddProduct(value._l2, factor, ref low2, ref high2);
        AddProduct(value._l3, factor, ref low3, ref high3);
        AddProduct(value._l4, factor, ref low4, ref high4);

        return Carry(low0, high0, low1, high1, low2, high2, low3, high3, low4, high4);
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
    internal static Curve25519FieldElement Pow22523(Curve25519FieldElement value)
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
    internal static Curve25519FieldElement Reduce(Curve25519FieldElement value) =>
        Carry(value._l0, 0, value._l1, 0, value._l2, 0, value._l3, 0, value._l4, 0);

    /// <summary>
    /// Squares a field element modulo p, returning a loosely reduced result.
    /// </summary>
    /// <param name="value">The element to square. Limbs must be below 2^54.</param>
    /// <returns>The square with all limbs below 2^52.</returns>
    /// <remarks>
    /// Each cross product appears twice in a square, so it is formed once from a doubled limb: 15 limb products instead
    /// of <see cref="Multiply" />'s 25. The operand bounds keep every limb product below 2^114 and every part of a
    /// result limb's sum below 2^64.
    /// </remarks>
    internal static Curve25519FieldElement Square(in Curve25519FieldElement value)
    {
        ulong f0 = value._l0, f1 = value._l1, f2 = value._l2, f3 = value._l3, f4 = value._l4;
        ulong f0By2 = 2 * f0, f1By2 = 2 * f1, f2By2 = 2 * f2, f3By2 = 2 * f3;
        ulong f3By19 = 19 * f3, f4By19 = 19 * f4;

        // Limb 0: f0² + 19 · 2 · (f1 · f4 + f2 · f3).
        ulong low0 = 0, high0 = 0;
        AddProduct(f0, f0, ref low0, ref high0);
        AddProduct(f1By2, f4By19, ref low0, ref high0);
        AddProduct(f2By2, f3By19, ref low0, ref high0);

        // Limb 1: 2 · f0 · f1 + 19 · (2 · f2 · f4 + f3²).
        ulong low1 = 0, high1 = 0;
        AddProduct(f0By2, f1, ref low1, ref high1);
        AddProduct(f2By2, f4By19, ref low1, ref high1);
        AddProduct(f3, f3By19, ref low1, ref high1);

        // Limb 2: 2 · f0 · f2 + f1² + 19 · 2 · f3 · f4.
        ulong low2 = 0, high2 = 0;
        AddProduct(f0By2, f2, ref low2, ref high2);
        AddProduct(f1, f1, ref low2, ref high2);
        AddProduct(f3By2, f4By19, ref low2, ref high2);

        // Limb 3: 2 · f0 · f3 + 2 · f1 · f2 + 19 · f4².
        ulong low3 = 0, high3 = 0;
        AddProduct(f0By2, f3, ref low3, ref high3);
        AddProduct(f1By2, f2, ref low3, ref high3);
        AddProduct(f4, f4By19, ref low3, ref high3);

        // Limb 4: 2 · f0 · f4 + 2 · f1 · f3 + f2².
        ulong low4 = 0, high4 = 0;
        AddProduct(f0By2, f4, ref low4, ref high4);
        AddProduct(f1By2, f3, ref low4, ref high4);
        AddProduct(f2, f2, ref low4, ref high4);

        return Carry(low0, high0, low1, high1, low2, high2, low3, high3, low4, high4);
    }

    /// <summary>
    /// Subtracts <paramref name="right" /> from <paramref name="left" /> limb-wise, biasing by 4p to keep every limb
    /// non-negative without branching.
    /// </summary>
    /// <param name="left">The minuend. Limbs must be below 2^53.</param>
    /// <param name="right">The subtrahend. Limbs must be below 2^53.</param>
    /// <returns>The difference with limbs below 2^54.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Curve25519FieldElement Subtract(Curve25519FieldElement left, Curve25519FieldElement right)
    {
        // 4p in radix 2^51: (2^53 - 76, 2^53 - 4, 2^53 - 4, 2^53 - 4, 2^53 - 4). Adding it keeps each limb
        // non-negative for any subtrahend limb below 2^53, so the subtraction never borrows.
        const ulong Four0 = (1UL << 53) - 76;
        const ulong FourN = (1UL << 53) - 4;

        return new Curve25519FieldElement(
            left._l0 + Four0 - right._l0,
            left._l1 + FourN - right._l1,
            left._l2 + FourN - right._l2,
            left._l3 + FourN - right._l3,
            left._l4 + FourN - right._l4);
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

        ulong t0 = _l0, t1 = _l1, t2 = _l2, t3 = _l3, t4 = _l4;

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
    /// Adds a 64 × 64-bit limb product to a result limb's sum held as two parts: the product's low 51 bits to one, and
    /// the rest, the product shifted right 51 bits, to the other.
    /// </summary>
    /// <param name="left">The first factor, below 2^55.</param>
    /// <param name="right">The second factor, below 2^59.</param>
    /// <param name="low">The sum of the products' low 51 bits.</param>
    /// <param name="high">The sum of the products shifted right 51 bits.</param>
    /// <remarks>
    /// The high half of the product comes from <c>mulx</c> on x64 with BMI2 and from <c>umulh</c> on ARM64. Without
    /// either, <see cref="Math.BigMul(ulong, ulong, out ulong)" /> is a call per product, so
    /// <see cref="SplitProduct" /> forms the split from four 64-bit multiplies inline instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddProduct(ulong left, ulong right, ref ulong low, ref ulong high)
    {
        if (Bmi2.X64.IsSupported || ArmBase.Arm64.IsSupported)
        {
            ulong product = left * right;
            low += product & LimbMask;
            high += (MultiplyHigh(left, right) << 13) | (product >> 51);
        }
        else
        {
            high += SplitProduct(left, right, out ulong productLow);
            low += productLow;
        }
    }

    /// <summary>
    /// Carries the two-part sums of a product's result limbs back into a loosely reduced element, folding the overflow
    /// above bit 254 through 2^255 ≡ 19 (mod p).
    /// </summary>
    /// <param name="low0">The sum of the low 51 bits of limb 0's products.</param>
    /// <param name="high0">The sum of limb 0's products shifted right 51 bits.</param>
    /// <param name="low1">The sum of the low 51 bits of limb 1's products.</param>
    /// <param name="high1">The sum of limb 1's products shifted right 51 bits.</param>
    /// <param name="low2">The sum of the low 51 bits of limb 2's products.</param>
    /// <param name="high2">The sum of limb 2's products shifted right 51 bits.</param>
    /// <param name="low3">The sum of the low 51 bits of limb 3's products.</param>
    /// <param name="high3">The sum of limb 3's products shifted right 51 bits.</param>
    /// <param name="low4">The sum of the low 51 bits of limb 4's products.</param>
    /// <param name="high4">The sum of limb 4's products shifted right 51 bits.</param>
    /// <returns>The reduced element with all limbs below 2^52.</returns>
    /// <remarks>
    /// Limb i's value is <c>low_i + high_i · 2^51</c>. Its carry into limb i + 1 is <c>high_i</c> plus whatever
    /// <c>low_i</c> holds above bit 50. Limb 4 has no products scaled by 19, so its carry stays below 2^60 and its
    /// fold, multiplied by 19, still fits in 64 bits.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Curve25519FieldElement Carry(
        ulong low0,
        ulong high0,
        ulong low1,
        ulong high1,
        ulong low2,
        ulong high2,
        ulong low3,
        ulong high3,
        ulong low4,
        ulong high4)
    {
        ulong r0 = low0 & LimbMask;
        low1 += (low0 >> 51) + high0;

        ulong r1 = low1 & LimbMask;
        low2 += (low1 >> 51) + high1;

        ulong r2 = low2 & LimbMask;
        low3 += (low2 >> 51) + high2;

        ulong r3 = low3 & LimbMask;
        low4 += (low3 >> 51) + high3;

        ulong r4 = low4 & LimbMask;
        ulong carry = (low4 >> 51) + high4;

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

    /// <summary>
    /// Returns the high 64 bits of the 128-bit product of two 64-bit values, through <c>mulx</c> on x64 with BMI2 or
    /// <c>umulh</c> on ARM64.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The high half of the product.</returns>
    /// <remarks>
    /// Callers check that one of the two instructions is available; <see cref="SplitProduct" /> serves processors with
    /// neither.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MultiplyHigh(ulong left, ulong right) =>
        Bmi2.X64.IsSupported ? Bmi2.X64.MultiplyNoFlags(left, right) : ArmBase.Arm64.MultiplyHigh(left, right);

    /// <summary>
    /// Splits the product of two limb factors at bit 51 with four 64-bit multiplies: for processors without an
    /// instruction for the high half of a 64 × 64-bit product.
    /// </summary>
    /// <param name="left">The first factor, below 2^55.</param>
    /// <param name="right">The second factor, below 2^59.</param>
    /// <param name="low">Receives the product's low 51 bits.</param>
    /// <returns>The product shifted right 51 bits.</returns>
    /// <remarks>
    /// With <c>left = a1 · 2^32 + a0</c> and <c>right = b1 · 2^32 + b0</c>, the product is
    /// <c>a1 · b1 · 2^64 + m · 2^32 + (a0 · b0 mod 2^32)</c>, where <c>m = a1 · b0 + a0 · b1 + ⌊a0 · b0 / 2^32⌋</c>.
    /// The bounds keep <c>m</c> below 2^60 and <c>a1 · b1</c> below 2^50, so the product shifted right 51 bits is
    /// <c>a1 · b1 · 2^13 + ⌊m / 2^19⌋</c> and fits in 64 bits.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong SplitProduct(ulong left, ulong right, out ulong low)
    {
        ulong leftLow = (uint)left, leftHigh = left >> 32;
        ulong rightLow = (uint)right, rightHigh = right >> 32;

        ulong lowProduct = leftLow * rightLow;
        ulong middle = (leftHigh * rightLow) + (leftLow * rightHigh) + (lowProduct >> 32);

        low = ((middle << 32) | (uint)lowProduct) & LimbMask;
        return ((leftHigh * rightHigh) << 13) + (middle >> 19);
    }
}
