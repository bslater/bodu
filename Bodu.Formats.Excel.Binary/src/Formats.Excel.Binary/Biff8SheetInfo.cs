// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8SheetInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Describes a worksheet within a BIFF8 workbook: its name, position, and visibility.
/// </summary>
public sealed class Biff8SheetInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8SheetInfo" /> class.
    /// </summary>
    /// <param name="name">The sheet name.</param>
    /// <param name="index">The zero-based position of the sheet within the workbook.</param>
    /// <param name="isVisible">Whether the sheet is visible.</param>
    /// <param name="dimensions">The used range declared by the sheet's <c>DIMENSIONS</c> record.</param>
    internal Biff8SheetInfo(string name, int index, bool isVisible, Biff8SheetDimensions dimensions)
    {
        Name = name;
        Index = index;
        IsVisible = isVisible;
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
    /// Gets a value indicating whether the sheet is visible.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the sheet is visible; <see langword="false" /> when it is hidden or very hidden.
    /// </returns>
    public bool IsVisible { get; }

    /// <summary>
    /// Gets the used range of the worksheet, as declared by its <c>DIMENSIONS</c> record.
    /// </summary>
    /// <returns>
    /// The half-open span of rows and columns that bounds the populated cells; the default value (zero counts) when the
    /// sheet declares no <c>DIMENSIONS</c> record.
    /// </returns>
    public Biff8SheetDimensions Dimensions { get; }
}
