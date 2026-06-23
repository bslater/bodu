// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSheetVisibility.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Identifies the visibility a workbook declares for a sheet through the hidden-state field of its bound-sheet record.
/// </summary>
/// <remarks>
/// A very hidden sheet is concealed from the Excel user interface and can be revealed only through the object model, so
/// it is distinguished from an ordinarily hidden sheet rather than collapsed into a single hidden state.
/// </remarks>
public enum ExcelSheetVisibility
{
    /// <summary>
    /// The sheet is visible in the workbook.
    /// </summary>
    Visible = 0,

    /// <summary>
    /// The sheet is hidden but can be unhidden through the Excel user interface.
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// The sheet is very hidden and can be revealed only programmatically.
    /// </summary>
    VeryHidden = 2,
}
