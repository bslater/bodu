// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.ScalarMult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides scalar multiplication for <see cref="Ed25519Point" />.
/// </summary>
internal partial struct Ed25519Point
{
    /// <summary>
    /// Multiplies <paramref name="point" /> by a 256-bit little-endian scalar using a constant-time binary ladder.
    /// </summary>
    /// <param name="point">The point to multiply.</param>
    /// <param name="scalar">The 32-byte little-endian scalar.</param>
    /// <returns>The scalar multiple of <paramref name="point" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="scalar" /> is not exactly 32 bytes.</exception>
    /// <remarks>
    /// Every iteration performs one complete addition, one doubling, and one mask-driven conditional move, so the
    /// operation sequence and memory access pattern are independent of the scalar value. The completeness of the
    /// unified addition formula makes the identity-accumulator start safe without special cases.
    /// </remarks>
    internal static Ed25519Point ScalarMult(Ed25519Point point, ReadOnlySpan<byte> scalar)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(scalar, 32);

        Ed25519Point accumulator = Identity;
        Ed25519Point addend = point;

        for (int i = 0; i < 256; i++)
        {
            ulong bit = (ulong)((scalar[i >> 3] >> (i & 7)) & 1);

            Ed25519Point sum = accumulator.Add(addend);
            ConditionalMove(ref accumulator, sum, bit);
            addend = addend.Double();
        }

        return accumulator;
    }

    /// <summary>
    /// Multiplies the Ed25519 base point by a 256-bit little-endian scalar in constant time, using the precomputed
    /// fixed-base table.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar.</param>
    /// <returns>The scalar multiple of the base point.</returns>
    /// <exception cref="ArgumentException"><paramref name="scalar" /> is not exactly 32 bytes.</exception>
    /// <remarks>
    /// Delegates to <see cref="ScalarMultBaseTable" />; the general
    /// <see cref="ScalarMult(Ed25519Point, ReadOnlySpan{byte})" /> remains available as the reference implementation
    /// that the table-based path is validated against.
    /// </remarks>
    internal static Ed25519Point ScalarMultBase(ReadOnlySpan<byte> scalar) =>
        ScalarMultBaseTable(scalar);

    /// <summary>
    /// Copies <paramref name="source" /> into <paramref name="destination" /> when <paramref name="condition" /> is 1,
    /// and leaves <paramref name="destination" /> unchanged when it is 0, without a data-dependent branch.
    /// </summary>
    /// <param name="destination">The point conditionally overwritten.</param>
    /// <param name="source">The point conditionally copied.</param>
    /// <param name="condition">The move condition. Must be exactly 0 or 1.</param>
    private static void ConditionalMove(ref Ed25519Point destination, Ed25519Point source, ulong condition)
    {
        Curve25519FieldElement.ConditionalMove(ref destination._x, source._x, condition);
        Curve25519FieldElement.ConditionalMove(ref destination._y, source._y, condition);
        Curve25519FieldElement.ConditionalMove(ref destination._z, source._z, condition);
        Curve25519FieldElement.ConditionalMove(ref destination._t, source._t, condition);
    }
}
