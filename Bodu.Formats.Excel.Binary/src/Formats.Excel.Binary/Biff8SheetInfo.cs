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
    internal Biff8SheetInfo(string name, int index, bool isVisible)
    {
        Name = name;
        Index = index;
        IsVisible = isVisible;
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
}
