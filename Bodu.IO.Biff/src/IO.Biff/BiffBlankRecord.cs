// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffBlankRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>BLANK</c> record: a formatted cell carrying no value.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.Blank" />. The payload is the six-byte cell prefix alone, in both
/// versions. A run of adjacent blank cells is written as a <see cref="BiffMulBlankRecord" /> instead.
/// </remarks>
/// <param name="Row">The zero-based row index.</param>
/// <param name="Column">The zero-based column index.</param>
/// <param name="XfIndex">The extended-format index of the cell.</param>
/// <seealso cref="BiffReader.GetBlank" /> <seealso cref="BiffWriter.WriteBlank(int, int, ushort)" />
public readonly record struct BiffBlankRecord(int Row, int Column, ushort XfIndex)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 6;

    /// <summary>
    /// Decodes a <c>BLANK</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than six bytes.</exception>
    internal static BiffBlankRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Blank;
        BiffPayload.RequireLength(payload, Length, type);

        return new BiffBlankRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type));
    }
}
