// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8SheetDirectoryEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Captures the fields of a <c>BOUNDSHEET8</c> record: the stream offset of the sheet's substream and its declared
/// name, type, and visibility.
/// </summary>
/// <remarks>
/// The stream offset is the value of the record's <c>lbPlyPos</c> field, a byte position within the workbook stream
/// pointing at the beginning-of-file record that opens the sheet's substream. Seeking to it allows a sheet to be read
/// directly, without pairing bound sheets with substreams by order.
/// </remarks>
internal readonly struct Biff8SheetDirectoryEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8SheetDirectoryEntry" /> struct.
    /// </summary>
    /// <param name="streamOffset">The byte offset of the sheet's substream within the workbook stream.</param>
    /// <param name="type">The sheet's declared type.</param>
    /// <param name="visibility">The sheet's declared visibility.</param>
    /// <param name="name">The sheet name.</param>
    internal Biff8SheetDirectoryEntry(long streamOffset, ExcelSheetType type, ExcelSheetVisibility visibility, string name)
    {
        StreamOffset = streamOffset;
        Type = type;
        Visibility = visibility;
        Name = name;
    }

    /// <summary>
    /// Gets the byte offset of the sheet's substream within the workbook stream.
    /// </summary>
    /// <value>The <c>lbPlyPos</c> value of the bound-sheet record.</value>
    public long StreamOffset { get; }

    /// <summary>
    /// Gets the sheet's declared type.
    /// </summary>
    /// <value>The kind of substream the bound-sheet record points to.</value>
    public ExcelSheetType Type { get; }

    /// <summary>
    /// Gets the sheet's declared visibility.
    /// </summary>
    /// <value>The visibility recorded by the bound-sheet record.</value>
    public ExcelSheetVisibility Visibility { get; }

    /// <summary>
    /// Gets the sheet name.
    /// </summary>
    /// <value>The decoded sheet name.</value>
    public string Name { get; }
}
