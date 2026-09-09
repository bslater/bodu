// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffDimensionsRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>DIMENSIONS</c> record: the used row and column extent of a sheet.
/// </summary>
/// <remarks>
/// The last row and column are stored one past the end (<c>rwMac</c>, <c>colMac</c>). BIFF8 stores rows as 32-bit
/// values; BIFF5 stores them as 16-bit values.
/// </remarks>
/// <param name="FirstRow">The zero-based index of the first used row.</param>
/// <param name="LastRowExclusive">One past the zero-based index of the last used row.</param>
/// <param name="FirstColumn">The zero-based index of the first used column.</param>
/// <param name="LastColumnExclusive">One past the zero-based index of the last used column.</param>
/// <seealso cref="BiffReader.GetDimensions" /> <seealso cref="BiffWriter.WriteDimensions(in BiffDimensionsRecord)" />
public readonly record struct BiffDimensionsRecord(int FirstRow, int LastRowExclusive, int FirstColumn, int LastColumnExclusive)
{
    /// <summary>The payload length of a BIFF5 record.</summary>
    internal const int Biff5Length = 10;

    /// <summary>The payload length of a BIFF8 record.</summary>
    internal const int Biff8Length = 14;

    /// <summary>
    /// Gets the number of used rows.
    /// </summary>
    /// <value>The row count, never negative.</value>
    public int RowCount => Math.Max(0, LastRowExclusive - FirstRow);

    /// <summary>
    /// Gets the number of used columns.
    /// </summary>
    /// <value>The column count, never negative.</value>
    public int ColumnCount => Math.Max(0, LastColumnExclusive - FirstColumn);

    /// <summary>
    /// Decodes a <c>DIMENSIONS</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the row field width.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffDimensionsRecord Read(ReadOnlySpan<byte> payload, BiffVersion version)
    {
        const BiffRecordType type = BiffRecordType.Dimensions;

        if (version == BiffVersion.Biff8)
        {
            BiffPayload.RequireLength(payload, 12, type);
            uint firstRow = BiffPayload.ReadUInt32(payload, 0, type);
            uint lastRow = BiffPayload.ReadUInt32(payload, 4, type);
            if (firstRow > int.MaxValue || lastRow > int.MaxValue)
                throw BiffPayload.Malformed(type);

            return new BiffDimensionsRecord(
                (int)firstRow,
                (int)lastRow,
                BiffPayload.ReadUInt16(payload, 8, type),
                BiffPayload.ReadUInt16(payload, 10, type));
        }

        BiffPayload.RequireLength(payload, 8, type);
        return new BiffDimensionsRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            BiffPayload.ReadUInt16(payload, 6, type));
    }
}
