// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.Precompute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the precomputed fixed-base table and the optimized base-point scalar-multiplication routines for
/// <see cref="Ed25519Point" />.
/// </summary>
/// <remarks>
/// The base point B is known at type-initialization time, so its small multiples are precomputed once into a windowed
/// table. <see cref="ScalarMultBaseTable" /> then multiplies in constant time using that table, and
/// <see cref="DoubleScalarMultBaseVartime" /> evaluates [a]B + [b]P with shared doublings for verification, where every
/// input is public and variable-time evaluation is therefore acceptable. The general
/// <see cref="ScalarMult(Ed25519Point, ReadOnlySpan{byte})" /> remains the reference path these routines are validated
/// against.
/// </remarks>
internal partial struct Ed25519Point
{
    /// <summary>The 4-bit window width used by the fixed-base table.</summary>
    private const int WindowBits = 4;

    /// <summary>The number of windows covering a 256-bit scalar at <see cref="WindowBits" /> bits each.</summary>
    private const int WindowCount = 256 / WindowBits;

    /// <summary>The number of entries per window (2^<see cref="WindowBits" />).</summary>
    private const int WindowSize = 1 << WindowBits;

    /// <summary>The fixed-base table: <c>s_baseTable[i][d] = [d · 2^(4i)] B</c>. Built in the static constructor so it observes the already-initialized base point regardless of partial-file field ordering.</summary>
    private static readonly Ed25519Point[][] s_baseTable;

    /// <summary>
    /// Initializes the fixed-base table from the base point.
    /// </summary>
    static Ed25519Point()
    {
        s_baseTable = new Ed25519Point[WindowCount][];

        Ed25519Point windowBase = s_basePoint;
        for (int i = 0; i < WindowCount; i++)
        {
            Ed25519Point[] row = new Ed25519Point[WindowSize];
            row[0] = Identity;
            for (int d = 1; d < WindowSize; d++)
                row[d] = row[d - 1].Add(windowBase);

            s_baseTable[i] = row;

            // Advance the window base by 2^WindowBits for the next window.
            for (int t = 0; t < WindowBits; t++)
                windowBase = windowBase.Double();
        }
    }

    /// <summary>
    /// Multiplies the base point by a 256-bit little-endian scalar in constant time using the precomputed fixed-base
    /// table.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar.</param>
    /// <returns>The scalar multiple of the base point.</returns>
    /// <exception cref="ArgumentException"><paramref name="scalar" /> is not exactly 32 bytes.</exception>
    /// <remarks>
    /// Each window contributes one table entry selected by scanning all <see cref="WindowSize" /> candidates with a
    /// mask-driven conditional move, so neither the access pattern nor the running time depends on the scalar.
    /// </remarks>
    internal static Ed25519Point ScalarMultBaseTable(ReadOnlySpan<byte> scalar)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(scalar, 32);

        Ed25519Point accumulator = Identity;
        for (int i = 0; i < WindowCount; i++)
        {
            int digit = (scalar[i >> 1] >> ((i & 1) * WindowBits)) & (WindowSize - 1);
            accumulator = accumulator.Add(SelectWindowEntry(s_baseTable[i], digit));
        }

        return accumulator;
    }

    /// <summary>
    /// Computes [<paramref name="baseScalar" />]B + [<paramref name="pointScalar" />]<paramref name="point" /> in
    /// variable time, sharing the doublings between the two scalar multiplications (Straus's method).
    /// </summary>
    /// <param name="baseScalar">The 32-byte little-endian scalar applied to the base point.</param>
    /// <param name="pointScalar">The 32-byte little-endian scalar applied to <paramref name="point" />.</param>
    /// <param name="point">The variable point.</param>
    /// <returns>The combined point [baseScalar]B + [pointScalar]·point.</returns>
    /// <exception cref="ArgumentException">A scalar is not exactly 32 bytes.</exception>
    /// <remarks>
    /// This routine is used only by signature verification, where the scalars and points are public, so its
    /// data-dependent branches and timing carry no secret information.
    /// </remarks>
    internal static Ed25519Point DoubleScalarMultBaseVartime(ReadOnlySpan<byte> baseScalar, ReadOnlySpan<byte> pointScalar, Ed25519Point point)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(baseScalar, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(pointScalar, 32);

        // pointTable[d] = [d]·point; the base multiples reuse window 0 of the fixed-base table (s_baseTable[0][d] = d·B).
        Ed25519Point[] pointTable = new Ed25519Point[WindowSize];
        pointTable[0] = Identity;
        for (int d = 1; d < WindowSize; d++)
            pointTable[d] = pointTable[d - 1].Add(point);

        Ed25519Point[] baseTableLow = s_baseTable[0];

        Ed25519Point accumulator = Identity;
        for (int i = WindowCount - 1; i >= 0; i--)
        {
            for (int t = 0; t < WindowBits; t++)
                accumulator = accumulator.Double();

            int baseDigit = (baseScalar[i >> 1] >> ((i & 1) * WindowBits)) & (WindowSize - 1);
            int pointDigit = (pointScalar[i >> 1] >> ((i & 1) * WindowBits)) & (WindowSize - 1);

            if (baseDigit != 0)
                accumulator = accumulator.Add(baseTableLow[baseDigit]);
            if (pointDigit != 0)
                accumulator = accumulator.Add(pointTable[pointDigit]);
        }

        return accumulator;
    }

    /// <summary>
    /// Selects <c>window[index]</c> in constant time by scanning every entry with a mask-driven conditional move.
    /// </summary>
    /// <param name="window">The window's table entries.</param>
    /// <param name="index">The entry to select, in <c>[0, WindowSize)</c>.</param>
    /// <returns>The selected point.</returns>
    private static Ed25519Point SelectWindowEntry(Ed25519Point[] window, int index)
    {
        Ed25519Point selected = Identity;
        for (int d = 0; d < WindowSize; d++)
        {
            // 1 when d == index, else 0, computed without a data-dependent branch.
            uint difference = (uint)(d ^ index);
            ulong match = ((difference - 1) >> 31) & 1;
            ConditionalMove(ref selected, window[d], match);
        }

        return selected;
    }
}
