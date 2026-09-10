// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRkCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents one cell of a <c>MULRK</c> run: its extended-format index and RK-encoded value.
/// </summary>
/// <remarks>
/// Cells are read from a <see cref="BiffMulRkRecord" /> by index and supplied to the writer as a span; <see
/// cref="Value" /> decodes the RK form on access.
/// </remarks>
/// <param name="XfIndex">The extended-format index of the cell.</param>
/// <param name="RawValue">The 32-bit RK value as stored.</param>
/// <seealso cref="BiffMulRkRecord" /> <seealso cref="BiffWriter.WriteMulRk(int, int, ReadOnlySpan{BiffRkCell})" />
public readonly record struct BiffRkCell(ushort XfIndex, uint RawValue)
{
    /// <summary>The encoded size of one cell within the run.</summary>
    internal const int Length = 6;

    /// <summary>
    /// Gets the decoded cell value.
    /// </summary>
    /// <value>The number <see cref="RawValue" /> represents.</value>
    public double Value => BiffRk.Decode(RawValue);
}
