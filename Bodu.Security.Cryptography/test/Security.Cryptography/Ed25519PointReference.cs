// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the base-point multiplications that the signed-digit fixed-base table and the non-adjacent-form
/// verification of <see cref="Ed25519Point" /> replaced - unsigned 4-bit windows over a table of 64 rows of 16 extended
/// points, joined with the unified addition - kept as an independent oracle that the tests hold the new routines to.
/// </summary>
/// <remarks>
/// The replaced fixed-base routine selected each window's entry with a constant-time scan; this oracle indexes the row
/// directly, which selects the same point.
/// </remarks>
internal static class Ed25519PointReference
{
    /// <summary>The window width, in bits.</summary>
    private const int WindowBits = 4;

    /// <summary>The number of windows covering a 256-bit scalar.</summary>
    private const int WindowCount = 256 / WindowBits;

    /// <summary>The number of entries per window.</summary>
    private const int WindowSize = 1 << WindowBits;

    /// <summary>The table: <c>s_baseTable[i][d] = [d · 2^(4i)] B</c>, built on first use.</summary>
    private static readonly Lazy<Ed25519Point[][]> s_baseTable = new(BuildBaseTable);

    /// <summary>
    /// Multiplies the base point by a 256-bit little-endian scalar with the 1.1.0 fixed-base table: one unified
    /// addition of the window's entry for each 4-bit window.
    /// </summary>
    /// <param name="scalar">The 32-byte little-endian scalar.</param>
    /// <returns>The scalar multiple of the base point.</returns>
    internal static Ed25519Point ScalarMultBase(ReadOnlySpan<byte> scalar)
    {
        Ed25519Point[][] table = s_baseTable.Value;

        Ed25519Point accumulator = Ed25519Point.Identity;
        for (int i = 0; i < WindowCount; i++)
        {
            int digit = (scalar[i >> 1] >> ((i & 1) * WindowBits)) & (WindowSize - 1);
            accumulator = accumulator.Add(table[i][digit]);
        }

        return accumulator;
    }

    /// <summary>
    /// Computes [<paramref name="baseScalar" />]B + [<paramref name="pointScalar" />]<paramref name="point" /> with the
    /// 1.1.0 routine: unsigned 4-bit windows for both scalars, sharing four doublings per window.
    /// </summary>
    /// <param name="baseScalar">The 32-byte little-endian scalar applied to the base point.</param>
    /// <param name="pointScalar">The 32-byte little-endian scalar applied to <paramref name="point" />.</param>
    /// <param name="point">The variable point.</param>
    /// <returns>The combined point.</returns>
    internal static Ed25519Point DoubleScalarMultBaseVartime(
        ReadOnlySpan<byte> baseScalar,
        ReadOnlySpan<byte> pointScalar,
        Ed25519Point point)
    {
        Ed25519Point[] baseTableLow = s_baseTable.Value[0];

        var pointTable = new Ed25519Point[WindowSize];
        pointTable[0] = Ed25519Point.Identity;
        for (int d = 1; d < WindowSize; d++)
            pointTable[d] = pointTable[d - 1].Add(point);

        Ed25519Point accumulator = Ed25519Point.Identity;
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
    /// Builds the 1.1.0 fixed-base table from the base point.
    /// </summary>
    /// <returns>The table, one row of 16 multiples per 4-bit window.</returns>
    private static Ed25519Point[][] BuildBaseTable()
    {
        var table = new Ed25519Point[WindowCount][];

        Ed25519Point windowBase = Ed25519Point.BasePoint;
        for (int i = 0; i < WindowCount; i++)
        {
            var row = new Ed25519Point[WindowSize];
            row[0] = Ed25519Point.Identity;
            for (int d = 1; d < WindowSize; d++)
                row[d] = row[d - 1].Add(windowBase);

            table[i] = row;

            for (int t = 0; t < WindowBits; t++)
                windowBase = windowBase.Double();
        }

        return table;
    }
}
