// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelDateSystem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Identifies the date base (epoch) a workbook uses to interpret serial date numbers.
/// </summary>
/// <remarks>
/// A workbook declares its date system through the date-mode record. The two systems differ by 1,462 days, so a serial
/// number must be paired with the correct system to recover the intended calendar date.
/// </remarks>
public enum ExcelDateSystem
{
    /// <summary>
    /// The 1900 date system, whose epoch is <c>1899-12-30</c> (the default for workbooks authored on Windows).
    /// </summary>
    Excel1900 = 0,

    /// <summary>
    /// The 1904 date system, whose epoch is <c>1904-01-01</c> (historically the default for workbooks authored on the
    /// Macintosh).
    /// </summary>
    Excel1904 = 1,
}
