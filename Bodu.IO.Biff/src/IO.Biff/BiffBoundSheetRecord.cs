// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffBoundSheetRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>BOUNDSHEET</c> record: one entry of the sheet directory in the workbook globals, giving a
/// sheet's name, visibility, kind, and the absolute stream offset of its substream.
/// </summary>
/// <remarks>
/// The name is an 8-bit-length string in both versions — a Unicode string in BIFF8, a code-page byte string in BIFF5.
/// </remarks>
/// <seealso cref="BiffReader.GetBoundSheet" />
/// <seealso cref="BiffWriter.WriteBoundSheet(uint, BiffSheetState, BiffSheetType, ReadOnlySpan{char})" />
/// <seealso cref="BiffSheetState" /> <seealso cref="BiffSheetType" />
public readonly ref struct BiffBoundSheetRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffBoundSheetRecord" /> struct.
    /// </summary>
    /// <param name="streamOffset">
    /// The absolute offset of the sheet's <c>BOF</c> record within the workbook stream.
    /// </param>
    /// <param name="state">The sheet's visibility.</param>
    /// <param name="sheetType">The kind of sheet.</param>
    /// <param name="name">The sheet name.</param>
    private BiffBoundSheetRecord(uint streamOffset, BiffSheetState state, BiffSheetType sheetType, BiffString name)
    {
        StreamOffset = streamOffset;
        State = state;
        SheetType = sheetType;
        Name = name;
    }

    /// <summary>
    /// Gets the absolute offset of the sheet's <c>BOF</c> record within the workbook stream.
    /// </summary>
    /// <value>The <c>lbPlyPos</c> field.</value>
    public uint StreamOffset { get; }

    /// <summary>
    /// Gets the sheet's visibility.
    /// </summary>
    /// <value>The visibility state.</value>
    public BiffSheetState State { get; }

    /// <summary>
    /// Gets the kind of sheet.
    /// </summary>
    /// <value>The sheet type; the value may not correspond to a defined member for an unrecognized kind.</value>
    public BiffSheetType SheetType { get; }

    /// <summary>
    /// Gets the sheet name.
    /// </summary>
    /// <value>The name as a span-backed view.</value>
    public BiffString Name { get; }

    /// <summary>
    /// Decodes a <c>BOUNDSHEET</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the name's representation.</param>
    /// <param name="codePage">The code page for a BIFF5 name.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffBoundSheetRecord Read(ReadOnlySpan<byte> payload, BiffVersion version, int codePage)
    {
        const BiffRecordType type = BiffRecordType.BoundSheet;
        BiffPayload.RequireLength(payload, 7, type);

        uint offset = BiffPayload.ReadUInt32(payload, 0, type);
        var state = (BiffSheetState)(payload[4] & 0x03);
        var sheetType = (BiffSheetType)payload[5];
        BiffString name = BiffString.Read(payload, 6, wideLength: false, version, codePage, type);

        return new BiffBoundSheetRecord(offset, state, sheetType, name);
    }
}
