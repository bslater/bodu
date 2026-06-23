// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSheetType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Identifies the kind of substream a bound-sheet record points to, as declared by its sheet-type field.
/// </summary>
/// <remarks>
/// Only a sheet of kind <see cref="Worksheet" /> carries the tabular cell records this reader surfaces. Dialog, macro,
/// chart, and Visual Basic module substreams are reported for completeness but yield no cells.
/// </remarks>
public enum ExcelSheetType
{
    /// <summary>
    /// A worksheet (or, in some producers, a dialog sheet) holding cell records.
    /// </summary>
    Worksheet = 0,

    /// <summary>
    /// An Excel 4.0 macro sheet.
    /// </summary>
    MacroSheet = 1,

    /// <summary>
    /// A chart sheet.
    /// </summary>
    Chart = 2,

    /// <summary>
    /// A Visual Basic for Applications module.
    /// </summary>
    VbaModule = 3,

    /// <summary>
    /// A sheet whose declared type is not one this reader recognizes.
    /// </summary>
    Unknown = 255,
}
