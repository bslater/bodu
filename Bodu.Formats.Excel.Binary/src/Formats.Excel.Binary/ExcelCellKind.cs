// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Classifies the value carried by an <see cref="ExcelCell" />.
/// </summary>
public enum ExcelCellKind
{
    /// <summary>
    /// The cell carries no value.
    /// </summary>
    Blank = 0,

    /// <summary>
    /// The cell carries a text value, available from <see cref="ExcelCell.StringValue" />.
    /// </summary>
    String = 1,

    /// <summary>
    /// The cell carries a numeric value, available from <see cref="ExcelCell.NumberValue" />. Dates are stored as
    /// numbers; use <see cref="ExcelSerialDate" /> to interpret a numeric cell as a date when the caller knows it is
    /// one.
    /// </summary>
    Number = 2,

    /// <summary>
    /// The cell carries a boolean value, available from <see cref="ExcelCell.BooleanValue" />.
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// The cell carries a spreadsheet error value (for example, <c>#DIV/0!</c>), available from
    /// <see cref="ExcelCell.ErrorValue" />.
    /// </summary>
    Error = 4,
}
