// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffNumberRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>NUMBER</c> record: a cell holding an IEEE 754 double-precision value.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.Number" />. The payload is fourteen bytes in both versions: the
/// row, column, and extended-format index as 16-bit values, then the value as a little-endian IEEE 754 double.
/// </remarks>
/// <param name="Row">The zero-based row index.</param>
/// <param name="Column">The zero-based column index.</param>
/// <param name="XfIndex">The extended-format index of the cell.</param>
/// <param name="Value">The cell value.</param>
/// <seealso cref="BiffReader.GetNumber" /> <seealso cref="BiffWriter.WriteNumber(int, int, ushort, double)" />
public readonly record struct BiffNumberRecord(int Row, int Column, ushort XfIndex, double Value)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 14;

    /// <summary>
    /// Decodes a <c>NUMBER</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than fourteen bytes.</exception>
    internal static BiffNumberRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Number;
        BiffPayload.RequireLength(payload, Length, type);

        return new BiffNumberRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            BiffPayload.ReadDouble(payload, 6, type));
    }
}
