// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelErrorCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Identifies the spreadsheet error value carried by an <see cref="ExcelCell" /> of kind
/// <see cref="ExcelCellKind.Error" />. The underlying values match the error codes defined by the Excel 97-2003 binary
/// file format.
/// </summary>
/// <remarks>
/// A cell whose stored error code is not one of the documented values is surfaced as that raw numeric value cast to
/// this enumeration; compare against the named members before relying on the symbol.
/// </remarks>
public enum ExcelErrorCode : byte
{
    /// <summary>
    /// The intersection of two ranges that do not intersect (<c>#NULL!</c>).
    /// </summary>
    Null = 0x00,

    /// <summary>
    /// A division by zero (<c>#DIV/0!</c>).
    /// </summary>
    DivideByZero = 0x07,

    /// <summary>
    /// A value of the wrong type for an operation or function (<c>#VALUE!</c>).
    /// </summary>
    Value = 0x0F,

    /// <summary>
    /// A reference to a cell that is not valid (<c>#REF!</c>).
    /// </summary>
    Reference = 0x17,

    /// <summary>
    /// An unrecognized name in a formula (<c>#NAME?</c>).
    /// </summary>
    Name = 0x1D,

    /// <summary>
    /// An invalid numeric value for a function or formula (<c>#NUM!</c>).
    /// </summary>
    Number = 0x24,

    /// <summary>
    /// A value that is not available to a function or formula (<c>#N/A</c>).
    /// </summary>
    NotAvailable = 0x2A,
}
