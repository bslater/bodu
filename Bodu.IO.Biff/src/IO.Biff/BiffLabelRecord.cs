// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffLabelRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>LABEL</c> record: a cell holding an inline text value. The text is a 16-bit-length Unicode
/// string in BIFF8 and a 16-bit-length code-page byte string in BIFF5.
/// </summary>
public readonly ref struct BiffLabelRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffLabelRecord" /> struct.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="text">The cell text.</param>
    private BiffLabelRecord(int row, int column, ushort xfIndex, BiffString text)
    {
        Row = row;
        Column = column;
        XfIndex = xfIndex;
        Text = text;
    }

    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    /// <value>The row index.</value>
    public int Row { get; }

    /// <summary>
    /// Gets the zero-based column index.
    /// </summary>
    /// <value>The column index.</value>
    public int Column { get; }

    /// <summary>
    /// Gets the extended-format index of the cell.
    /// </summary>
    /// <value>The XF index.</value>
    public ushort XfIndex { get; }

    /// <summary>
    /// Gets the cell text.
    /// </summary>
    /// <value>The text as a span-backed view.</value>
    public BiffString Text { get; }

    /// <summary>
    /// Decodes a <c>LABEL</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the text representation.</param>
    /// <param name="codePage">The code page for BIFF5 text.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffLabelRecord Read(ReadOnlySpan<byte> payload, BiffVersion version, int codePage)
    {
        const BiffRecordType type = BiffRecordType.Label;
        BiffPayload.RequireLength(payload, 8, type);

        return new BiffLabelRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            BiffString.Read(payload, 6, wideLength: true, version, codePage, type));
    }
}
