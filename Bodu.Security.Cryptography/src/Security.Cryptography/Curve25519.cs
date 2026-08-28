// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the X25519 scalar multiplication function over Curve25519 as defined in RFC 7748 §5, implemented with a
/// constant-time Montgomery ladder.
/// </summary>
/// <remarks>
/// <para>
/// The ladder processes the 255 scalar bits from most to least significant, using a single mask-driven conditional swap
/// per bit so that the sequence of field operations is independent of the scalar value. The scalar supplied by the
/// caller is never modified: clamping is applied to a stack copy that is zeroed before returning.
/// </para>
/// </remarks>
internal static class Curve25519
{
    /// <summary>The size, in bytes, of a Curve25519 scalar, u-coordinate, and scalar multiplication result.</summary>
    internal const int PointSizeInBytes = 32;

    /// <summary>The curve constant (A − 2) / 4 = 121665 used by the Montgomery ladder step.</summary>
    private const uint A24 = 121665;

    /// <summary>
    /// Computes the X25519 function <c>X25519(scalar, u)</c>, writing the resulting u-coordinate into
    /// <paramref name="destination" />.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar. Clamped per RFC 7748 §5 on a private copy.</param>
    /// <param name="u">
    /// The 32-byte little-endian u-coordinate; bit 255 is masked before decoding, as the RFC requires.
    /// </param>
    /// <param name="destination">The 32-byte span that receives the resulting u-coordinate.</param>
    /// <returns>
    /// <see langword="true" /> when the result is the all-zero output produced by low-order input points; otherwise,
    /// <see langword="false" />. The flag is accumulated over every output byte so the check itself is constant-time.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="scalar" />, <paramref name="u" />, or <paramref name="destination" /> is not exactly 32 bytes
    /// long.
    /// </exception>
    internal static bool ScalarMult(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> u, Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(scalar, PointSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(u, PointSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, PointSizeInBytes);

        // Clamp a copy of the scalar (clear bits 0-2 and 255, set bit 254); the caller's key bytes are preserved.
        Span<byte> e = stackalloc byte[PointSizeInBytes];

        var x1 = default(Curve25519FieldElement);
        var x2 = default(Curve25519FieldElement);
        var z2 = default(Curve25519FieldElement);
        var x3 = default(Curve25519FieldElement);
        var z3 = default(Curve25519FieldElement);

        try
        {
            scalar.CopyTo(e);
            e[0] &= 248;
            e[31] &= 127;
            e[31] |= 64;

            x1 = Curve25519FieldElement.FromBytes(u);
            x2 = Curve25519FieldElement.One;
            z2 = Curve25519FieldElement.Zero;
            x3 = x1;
            z3 = Curve25519FieldElement.One;

            // RFC 7748 §5 ladder: one conditional swap pair per bit keeps the operation sequence scalar-independent.
            ulong swap = 0UL;
            for (int t = 254; t >= 0; t--)
            {
                ulong bit = (ulong)((e[t >> 3] >> (t & 7)) & 1);
                swap ^= bit;
                Curve25519FieldElement.ConditionalSwap(ref x2, ref x3, swap);
                Curve25519FieldElement.ConditionalSwap(ref z2, ref z3, swap);
                swap = bit;

                var a = Curve25519FieldElement.Add(x2, z2);
                var aa = Curve25519FieldElement.Square(a);
                var b = Curve25519FieldElement.Subtract(x2, z2);
                var bb = Curve25519FieldElement.Square(b);
                var eDiff = Curve25519FieldElement.Subtract(aa, bb);
                var c = Curve25519FieldElement.Add(x3, z3);
                var d = Curve25519FieldElement.Subtract(x3, z3);
                var da = Curve25519FieldElement.Multiply(d, a);
                var cb = Curve25519FieldElement.Multiply(c, b);

                x3 = Curve25519FieldElement.Square(Curve25519FieldElement.Add(da, cb));
                z3 = Curve25519FieldElement.Multiply(x1, Curve25519FieldElement.Square(Curve25519FieldElement.Subtract(da, cb)));
                x2 = Curve25519FieldElement.Multiply(aa, bb);
                z2 = Curve25519FieldElement.Multiply(
                    eDiff,
                    Curve25519FieldElement.Add(aa, Curve25519FieldElement.MultiplySmall(eDiff, A24)));
            }

            Curve25519FieldElement.ConditionalSwap(ref x2, ref x3, swap);
            Curve25519FieldElement.ConditionalSwap(ref z2, ref z3, swap);

            var result = Curve25519FieldElement.Multiply(x2, Curve25519FieldElement.Invert(z2));
            result.ToBytes(destination);
        }
        finally
        {
            // Best-effort scrub of the clamped scalar and the named ladder coordinates. Per-iteration field-element
            // temporaries are returned by value through the arithmetic helpers and land in registers or stack slots
            // managed code cannot reliably scrub; those are out of reach by design.
            CryptographyHelper.Clear(e);
            CryptographyHelper.Clear(ref x1);
            CryptographyHelper.Clear(ref x2);
            CryptographyHelper.Clear(ref z2);
            CryptographyHelper.Clear(ref x3);
            CryptographyHelper.Clear(ref z3);
        }

        // Accumulate over all output bytes; a single data-dependent branch on the aggregate is unavoidable and
        // reveals only what the caller's subsequent rejection of the all-zero result reveals anyway.
        int accumulator = 0;
        for (int i = 0; i < destination.Length; i++)
            accumulator |= destination[i];

        return accumulator == 0;
    }

    /// <summary>
    /// Computes <c>X25519(scalar, 9)</c>, multiplying the Curve25519 base point by the supplied scalar.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar. Clamped per RFC 7748 §5 on a private copy.</param>
    /// <param name="destination">The 32-byte span that receives the resulting u-coordinate.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="scalar" /> or <paramref name="destination" /> is not exactly 32 bytes long.
    /// </exception>
    /// <remarks>
    /// The base point has order 8 × L, so a clamped scalar can never produce the all-zero output; the low-order result
    /// flag returned by <see cref="ScalarMult" /> is therefore discarded.
    /// </remarks>
    internal static void ScalarMultBase(ReadOnlySpan<byte> scalar, Span<byte> destination)
    {
        Span<byte> basePoint = stackalloc byte[PointSizeInBytes];
        basePoint[0] = 9;

        _ = ScalarMult(scalar, basePoint, destination);
    }
}
