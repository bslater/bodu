// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Scalar.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides arithmetic modulo the Ed25519 group order L = 2^252 + 27742317777372353535851937790883648493 used by
/// RFC 8032 signing and verification.
/// </summary>
/// <remarks>
/// <para>
/// Scalars are processed as signed 21-bit limbs in 64-bit registers, following the radix and folding identity of the
/// ref10 reference implementation: 2^252 ≡ −δ (mod L), where δ is expressed in signed base 2^21 by the six folding
/// constants below. The carry and fold schedule is fixed and branch-free, so reduction time does not depend on the
/// scalar values.
/// </para>
/// </remarks>
internal static class Ed25519Scalar
{
    /// <summary>
    /// The size, in bytes, of a canonical scalar.
    /// </summary>
    internal const int SizeInBytes = 32;

    /// <summary>
    /// Mask isolating the low 21 bits of a limb.
    /// </summary>
    private const long LimbMask = (1L << 21) - 1;

    /// <summary>
    /// The canonical little-endian encoding of the group order L, used by <see cref="IsCanonical" />.
    /// </summary>
    private static readonly byte[] s_orderBytes = Convert.FromHexString(
        "edd3f55c1a631258d69cf7a2def9de1400000000000000000000000000000010");

    /// <summary>
    /// Reduces a 512-bit little-endian value modulo L, writing the canonical 32-byte result.
    /// </summary>
    /// <param name="source">The 64-byte little-endian value to reduce, typically a SHA-512 digest.</param>
    /// <param name="destination">The 32-byte span that receives the canonical reduced scalar.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> is not exactly 64 bytes or <paramref name="destination" /> is not exactly 32 bytes.
    /// </exception>
    internal static void Reduce(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(source, 64);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, SizeInBytes);

        Span<long> s = stackalloc long[26];
        for (var i = 0; i < 23; i++)
            s[i] = ReadBits(source, 21 * i, 21);

        // Limb 23 absorbs the top 29 bits (483-511) so the full 512-bit value is represented in 24 limbs.
        s[23] = ReadBits(source, 483, 29);

        ReduceLimbs(s);
        WriteLimbs(s, destination);

        CryptographyHelper.Clear(s);
    }

    /// <summary>
    /// Computes (<paramref name="a" /> · <paramref name="b" /> + <paramref name="c" />) mod L, writing the canonical
    /// 32-byte result.
    /// </summary>
    /// <param name="a">The first 32-byte little-endian factor.</param>
    /// <param name="b">The second 32-byte little-endian factor.</param>
    /// <param name="c">The 32-byte little-endian addend.</param>
    /// <param name="destination">The 32-byte span that receives the canonical result.</param>
    /// <exception cref="ArgumentException">Any span does not have its exact required length.</exception>
    /// <remarks>
    /// Inputs need not be reduced; any 256-bit values are accepted, which lets the signing path pass the clamped
    /// secret scalar directly.
    /// </remarks>
    internal static void MulAdd(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, ReadOnlySpan<byte> c, Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(a, SizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(b, SizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(c, SizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, SizeInBytes);

        Span<long> al = stackalloc long[13];
        Span<long> bl = stackalloc long[13];
        Span<long> s = stackalloc long[26];

        for (var i = 0; i < 13; i++)
        {
            al[i] = ReadBits(a, 21 * i, Math.Min(21, 256 - (21 * i)));
            bl[i] = ReadBits(b, 21 * i, Math.Min(21, 256 - (21 * i)));
        }

        // Schoolbook product into 25 partial limbs; magnitudes stay below 2^46 (13 products of at most 2^42).
        for (var i = 0; i < 13; i++)
        {
            for (var j = 0; j < 13; j++)
                s[i + j] += al[i] * bl[j];
        }

        for (var i = 0; i < 13; i++)
            s[i] += ReadBits(c, 21 * i, Math.Min(21, 256 - (21 * i)));

        // Normalize so every limb fits the fold-schedule entry bound, then fold the two extra limbs down before
        // running the shared 24-limb reduction.
        for (var i = 0; i < 25; i++)
            CarryRound(s, i);

        Fold(s, 25);
        Fold(s, 24);
        ReduceLimbs(s);
        WriteLimbs(s, destination);

        CryptographyHelper.Clear(al);
        CryptographyHelper.Clear(bl);
        CryptographyHelper.Clear(s);
    }

    /// <summary>
    /// Determines whether a 32-byte little-endian scalar is canonical, that is, strictly less than the group order
    /// L. RFC 8032 verification rejects signatures whose S component is non-canonical to prevent malleability.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar to test.</param>
    /// <returns><see langword="true" /> when the scalar is strictly less than L; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="scalar" /> is not exactly 32 bytes.</exception>
    internal static bool IsCanonical(ReadOnlySpan<byte> scalar)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(scalar, SizeInBytes);

        // Verification inputs are public, so a short-circuiting big-endian comparison is acceptable here.
        for (var i = SizeInBytes - 1; i >= 0; i--)
        {
            if (scalar[i] < s_orderBytes[i]) return true;
            if (scalar[i] > s_orderBytes[i]) return false;
        }

        return false;
    }

    /// <summary>
    /// Runs the fixed fold-and-carry schedule that maps 24 partial limbs onto the canonical 12-limb representative
    /// modulo L. On return limbs 0–11 hold the canonical value and all higher limbs are zero.
    /// </summary>
    /// <param name="s">The limb workspace. Limbs 0–22 must be below 2^21 in magnitude and limb 23 below 2^30.</param>
    /// <remarks>
    /// The schedule was validated exhaustively against direct big-integer reduction; the worst intermediate limb
    /// magnitude stays below 2^50, well inside the 64-bit signed range.
    /// </remarks>
    private static void ReduceLimbs(Span<long> s)
    {
        for (var i = 23; i >= 18; i--)
            Fold(s, i);

        for (var i = 6; i < 18; i++)
            CarryRound(s, i);

        for (var i = 18; i >= 12; i--)
            Fold(s, i);

        for (var i = 0; i < 12; i++)
            CarryRound(s, i);

        Fold(s, 12);

        for (var i = 0; i < 12; i++)
            CarryFloor(s, i);

        Fold(s, 12);

        for (var i = 0; i < 11; i++)
            CarryFloor(s, i);
    }

    /// <summary>
    /// Folds limb <paramref name="i" /> down twelve positions through the identity 2^252 ≡ −δ (mod L), zeroing the
    /// source limb.
    /// </summary>
    /// <param name="s">The limb workspace.</param>
    /// <param name="i">The index of the limb to fold. Must be at least 12.</param>
    private static void Fold(Span<long> s, int i)
    {
        var v = s[i];

        s[i - 12] += v * 666643;
        s[i - 11] += v * 470296;
        s[i - 10] += v * 654183;
        s[i - 9] -= v * 997805;
        s[i - 8] += v * 136657;
        s[i - 7] -= v * 683901;
        s[i] = 0;
    }

    /// <summary>
    /// Performs a round-to-nearest carry from limb <paramref name="i" /> into limb <c>i + 1</c>, leaving the source
    /// limb in the balanced range [−2^20, 2^20].
    /// </summary>
    /// <param name="s">The limb workspace.</param>
    /// <param name="i">The index of the limb to normalize.</param>
    private static void CarryRound(Span<long> s, int i)
    {
        var carry = (s[i] + (1L << 20)) >> 21;

        s[i + 1] += carry;
        s[i] -= carry << 21;
    }

    /// <summary>
    /// Performs a floor carry from limb <paramref name="i" /> into limb <c>i + 1</c>, leaving the source limb in the
    /// range [0, 2^21).
    /// </summary>
    /// <param name="s">The limb workspace.</param>
    /// <param name="i">The index of the limb to normalize.</param>
    private static void CarryFloor(Span<long> s, int i)
    {
        var carry = s[i] >> 21;

        s[i + 1] += carry;
        s[i] -= carry << 21;
    }

    /// <summary>
    /// Reads up to 29 bits from a little-endian byte span starting at an arbitrary bit offset.
    /// </summary>
    /// <param name="source">The little-endian source bytes.</param>
    /// <param name="bitOffset">The zero-based bit position of the least significant bit to read.</param>
    /// <param name="bitCount">The number of bits to read (at most 29).</param>
    /// <returns>The extracted bits as a non-negative value.</returns>
    private static long ReadBits(ReadOnlySpan<byte> source, int bitOffset, int bitCount)
    {
        var byteIndex = bitOffset >> 3;
        var shift = bitOffset & 7;

        // Gather up to five bytes so that shift + bitCount (at most 7 + 29 = 36 bits) is always covered.
        ulong window = 0;
        var available = Math.Min(5, source.Length - byteIndex);
        for (var i = 0; i < available; i++)
            window |= (ulong)source[byteIndex + i] << (8 * i);

        return (long)((window >> shift) & ((1UL << bitCount) - 1));
    }

    /// <summary>
    /// Packs the canonical limbs 0–11 (21 bits each, 252 bits total) into a 32-byte little-endian encoding.
    /// </summary>
    /// <param name="s">The limb workspace holding the canonical value in limbs 0–11.</param>
    /// <param name="destination">The 32-byte span that receives the encoding.</param>
    private static void WriteLimbs(ReadOnlySpan<long> s, Span<byte> destination)
    {
        ulong accumulator = 0;
        var bits = 0;
        var index = 0;

        for (var i = 0; i < 12; i++)
        {
            accumulator |= (ulong)s[i] << bits;
            bits += 21;

            while (bits >= 8)
            {
                destination[index++] = (byte)accumulator;
                accumulator >>= 8;
                bits -= 8;
            }
        }

        while (index < SizeInBytes)
        {
            destination[index++] = (byte)accumulator;
            accumulator >>= 8;
        }
    }
}
