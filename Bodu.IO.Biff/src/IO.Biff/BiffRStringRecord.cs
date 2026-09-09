// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRStringRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>RSTRING</c> record: a BIFF5 rich-text label cell — a 16-bit-length code-page byte string
/// followed by a run table of two-byte entries (character index, font index).
/// </summary>
/// <remarks>
/// BIFF8 superseded the record with rich runs inside the shared string table; a BIFF8 stream may still carry it for
/// compatibility, in which case the text is still a byte string in the stream's code page.
/// </remarks>
public readonly ref struct BiffRStringRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffRStringRecord" /> struct.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="text">The cell text.</param>
    /// <param name="runs">The run table bytes.</param>
    private BiffRStringRecord(int row, int column, ushort xfIndex, BiffString text, ReadOnlySpan<byte> runs)
    {
        Row = row;
        Column = column;
        XfIndex = xfIndex;
        Text = text;
        Runs = runs;
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
    /// <value>The text as a span-backed byte-string view.</value>
    public BiffString Text { get; }

    /// <summary>
    /// Gets the formatting runs as raw bytes: two bytes per run, the first character index of the run followed by its
    /// font index.
    /// </summary>
    /// <value>The run bytes.</value>
    public ReadOnlySpan<byte> Runs { get; }

    /// <summary>
    /// Gets the number of formatting runs.
    /// </summary>
    /// <value>The run count.</value>
    public int RunCount => Runs.Length / 2;

    /// <summary>
    /// Decodes an <c>RSTRING</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="codePage">The code page the text is encoded in.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffRStringRecord Read(ReadOnlySpan<byte> payload, int codePage)
    {
        const BiffRecordType type = BiffRecordType.RString;
        BiffPayload.RequireLength(payload, 8, type);

        int row = BiffPayload.ReadUInt16(payload, 0, type);
        int column = BiffPayload.ReadUInt16(payload, 2, type);
        ushort xfIndex = BiffPayload.ReadUInt16(payload, 4, type);
        BiffString text = BiffString.ReadByteString(payload, 6, wideLength: true, codePage, type);

        int runsOffset = 6 + text.EncodedLength;
        int runCount = BiffPayload.ReadByte(payload, runsOffset, type);
        if (runsOffset + 1 + (runCount * 2) > payload.Length)
            throw BiffPayload.Malformed(type);

        return new BiffRStringRecord(row, column, xfIndex, text, payload.Slice(runsOffset + 1, runCount * 2));
    }
}
