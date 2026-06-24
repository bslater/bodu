// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Describes a sheet within a BIFF8 workbook: its name, position, visibility, type, and declared used range.
/// </summary>
/// <remarks>
/// An instance is produced from the workbook's bound-sheet record and the <c>DIMENSIONS</c> record of the sheet's
/// substream. Sheets of every <see cref="ExcelSheetType" /> are described; only a
/// <see cref="ExcelSheetType.Worksheet" /> yields cells when opened.
/// </remarks>
public sealed class ExcelWorksheetInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelWorksheetInfo" /> class.
    /// </summary>
    /// <param name="name">The sheet name.</param>
    /// <param name="index">The zero-based position of the sheet within the workbook.</param>
    /// <param name="visibility">The sheet's declared visibility.</param>
    /// <param name="type">The sheet's declared type.</param>
    /// <param name="dimensions">The used range declared by the sheet's <c>DIMENSIONS</c> record.</param>
    internal ExcelWorksheetInfo(string name, int index, ExcelSheetVisibility visibility, ExcelSheetType type, ExcelWorksheetDimensions dimensions)
    {
        Name = name;
        Index = index;
        Visibility = visibility;
        Type = type;
        Dimensions = dimensions;
    }

    /// <summary>
    /// Gets the sheet name.
    /// </summary>
    /// <returns>The sheet name as declared in the workbook globals.</returns>
    public string Name { get; }

    /// <summary>
    /// Gets the zero-based position of the sheet within the workbook.
    /// </summary>
    /// <returns>The sheet index in workbook order.</returns>
    public int Index { get; }

    /// <summary>
    /// Gets the sheet's declared visibility.
    /// </summary>
    /// <returns>The visibility recorded by the sheet's bound-sheet record.</returns>
    public ExcelSheetVisibility Visibility { get; }

    /// <summary>
    /// Gets the sheet's declared type.
    /// </summary>
    /// <returns>The kind of substream the sheet's bound-sheet record points to.</returns>
    public ExcelSheetType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the sheet is visible.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Visibility" /> is <see cref="ExcelSheetVisibility.Visible" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool IsVisible => Visibility == ExcelSheetVisibility.Visible;

    /// <summary>
    /// Gets the used range of the sheet, as declared by its <c>DIMENSIONS</c> record.
    /// </summary>
    /// <returns>
    /// The half-open span of rows and columns that bounds the populated cells; the default value (zero counts) when the
    /// sheet declares no <c>DIMENSIONS</c> record.
    /// </returns>
    public ExcelWorksheetDimensions Dimensions { get; }
}
