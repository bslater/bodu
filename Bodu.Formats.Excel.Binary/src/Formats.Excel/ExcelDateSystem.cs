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
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The same serial number names two different days depending on the workbook's system.
/// DateOnly windows = ExcelSerialDate.FromSerialDate(45000.0, ExcelDateSystem.Excel1900); // 2023-03-15
/// DateOnly mac     = ExcelSerialDate.FromSerialDate(45000.0, ExcelDateSystem.Excel1904); // 2027-03-16
///]]>
/// </code>
/// </example>
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
