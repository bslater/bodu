// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffLabelSstRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>LABELSST</c> record: a cell whose text is an entry of the shared string table (BIFF8).
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.LabelSst" />. The payload is ten bytes: the cell prefix and the
/// 32-bit index into the shared string table that <see cref="BiffSstReader" /> materializes. The record exists only in
/// BIFF8; the writer rejects it under BIFF5, and the reader decodes it by layout in either version.
/// </remarks>
/// <param name="Row">The zero-based row index.</param>
/// <param name="Column">The zero-based column index.</param>
/// <param name="XfIndex">The extended-format index of the cell.</param>
/// <param name="SstIndex">The zero-based index of the cell's text in the shared string table.</param>
/// <seealso cref="BiffReader.GetLabelSst" /> <seealso cref="BiffWriter.WriteLabelSst(int, int, ushort, uint)" />
/// <seealso cref="BiffSstReader" />
public readonly record struct BiffLabelSstRecord(int Row, int Column, ushort XfIndex, uint SstIndex)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 10;

    /// <summary>
    /// Decodes a <c>LABELSST</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than ten bytes.</exception>
    internal static BiffLabelSstRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.LabelSst;
        BiffPayload.RequireLength(payload, Length, type);

        return new BiffLabelSstRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            BiffPayload.ReadUInt32(payload, 6, type));
    }
}
