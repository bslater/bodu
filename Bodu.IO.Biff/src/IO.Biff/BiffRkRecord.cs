// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRkRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>RK</c> record: a cell holding a number in the compact RK encoding.
/// </summary>
/// <param name="Row">The zero-based row index.</param>
/// <param name="Column">The zero-based column index.</param>
/// <param name="XfIndex">The extended-format index of the cell.</param>
/// <param name="RawValue">The 32-bit RK value as stored.</param>
public readonly record struct BiffRkRecord(int Row, int Column, ushort XfIndex, uint RawValue)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 10;

    /// <summary>
    /// Gets the decoded cell value.
    /// </summary>
    /// <value>The number <see cref="RawValue" /> represents.</value>
    public double Value => BiffRk.Decode(RawValue);

    /// <summary>
    /// Decodes an <c>RK</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than ten bytes.</exception>
    internal static BiffRkRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Rk;
        BiffPayload.RequireLength(payload, Length, type);

        return new BiffRkRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            BiffPayload.ReadUInt32(payload, 6, type));
    }
}
