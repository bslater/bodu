// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8RecordType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Identifies the BIFF8 record types this reader recognizes. Values match the record identifiers defined by the Excel
/// 97-2003 binary file format; any record whose identifier is not listed here is skipped during reading.
/// </summary>
public enum Biff8RecordType : ushort
{
    /// <summary>
    /// Beginning-of-file record that opens a substream (the workbook globals or a sheet).
    /// </summary>
    Bof = 0x0809,

    /// <summary>
    /// End-of-file record that closes a substream.
    /// </summary>
    Eof = 0x000A,

    /// <summary>
    /// Continuation record carrying the overflow of a preceding record, notably the shared string table.
    /// </summary>
    Continue = 0x003C,

    /// <summary>
    /// Bound-sheet record declaring a sheet's name, visibility, and substream position.
    /// </summary>
    BoundSheet = 0x0085,

    /// <summary>
    /// A cell holding a formula, carrying the cached result of its last calculation.
    /// </summary>
    Formula = 0x0006,

    /// <summary>
    /// The cached string result of a preceding <see cref="Formula" /> cell.
    /// </summary>
    String = 0x0207,

    /// <summary>
    /// File-pass record marking a workbook whose contents are encrypted.
    /// </summary>
    FilePass = 0x002F,

    /// <summary>
    /// Shared string table holding the workbook's deduplicated text values.
    /// </summary>
    Sst = 0x00FC,

    /// <summary>
    /// Worksheet dimensions record describing the used row and column extent.
    /// </summary>
    Dimensions = 0x0200,

    /// <summary>
    /// Extended-format (XF) record describing a cell or style format, including its number-format index.
    /// </summary>
    Xf = 0x00E0,

    /// <summary>
    /// Number-format record mapping a format index to its format code string.
    /// </summary>
    Format = 0x041E,

    /// <summary>
    /// Date-mode record declaring whether the workbook uses the 1900 or 1904 date system.
    /// </summary>
    DateMode = 0x0022,

    /// <summary>
    /// A cell whose value is an index into the shared string table.
    /// </summary>
    LabelSst = 0x00FD,

    /// <summary>
    /// A cell holding an IEEE 754 double-precision number.
    /// </summary>
    Number = 0x0203,

    /// <summary>
    /// A cell holding an RK-encoded number (a compact 30-bit numeric encoding).
    /// </summary>
    Rk = 0x027E,

    /// <summary>
    /// A run of adjacent cells in one row, each holding an RK-encoded number.
    /// </summary>
    MulRk = 0x00BD,

    /// <summary>
    /// A cell holding a boolean value or an error code.
    /// </summary>
    BoolErr = 0x0205,

    /// <summary>
    /// A blank, formatted cell carrying no value.
    /// </summary>
    Blank = 0x0201,

    /// <summary>
    /// A run of adjacent blank cells in one row.
    /// </summary>
    MulBlank = 0x00BE,
}
