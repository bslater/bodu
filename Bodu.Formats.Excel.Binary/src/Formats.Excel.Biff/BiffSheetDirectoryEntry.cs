// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSheetDirectoryEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Biff;

/// <summary>
/// Describes one bound-sheet entry of the workbook globals: where a sheet's substream begins and what the sheet is.
/// </summary>
internal readonly struct BiffSheetDirectoryEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffSheetDirectoryEntry" /> struct.
    /// </summary>
    /// <param name="streamOffset">The absolute offset of the sheet's BOF record within the workbook stream.</param>
    /// <param name="type">The kind of sheet.</param>
    /// <param name="visibility">The sheet's visibility.</param>
    /// <param name="name">The sheet name.</param>
    internal BiffSheetDirectoryEntry(long streamOffset, ExcelSheetType type, ExcelSheetVisibility visibility, string name)
    {
        StreamOffset = streamOffset;
        Type = type;
        Visibility = visibility;
        Name = name;
    }

    /// <summary>
    /// Gets the absolute offset of the sheet's BOF record within the workbook stream.
    /// </summary>
    /// <value>The <c>lbPlyPos</c> field of the bound-sheet record.</value>
    public long StreamOffset { get; }

    /// <summary>
    /// Gets the kind of sheet.
    /// </summary>
    /// <value>The sheet type.</value>
    public ExcelSheetType Type { get; }

    /// <summary>
    /// Gets the sheet's visibility.
    /// </summary>
    /// <value>The visibility state.</value>
    public ExcelSheetVisibility Visibility { get; }

    /// <summary>
    /// Gets the sheet name.
    /// </summary>
    /// <value>The name as declared in the bound-sheet record.</value>
    public string Name { get; }
}
