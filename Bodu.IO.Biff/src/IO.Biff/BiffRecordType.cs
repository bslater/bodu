// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRecordType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the BIFF record types the codec names. Values are the 16-bit record identifiers defined by the Excel
/// binary file format and are shared by BIFF5 and BIFF8 except where a member's documentation says otherwise.
/// </summary>
/// <remarks>
/// The enumeration is a curated catalogue, not the full record set: a record whose identifier is not listed here is
/// still readable through <see cref="BiffReader.RecordId" /> and <see cref="BiffReader.ValueSpan" />, and can be
/// written as a raw record from its identifier and payload. Encountering an unlisted identifier is never an error.
/// </remarks>
public enum BiffRecordType : ushort
{
    /// <summary>
    /// The value reported when no record is current.
    /// </summary>
    None = 0x0000,

    /// <summary>
    /// Formula cell: row, column, format index, the cached result, and the parsed-expression tokens.
    /// </summary>
    Formula = 0x0006,

    /// <summary>
    /// End of a substream.
    /// </summary>
    Eof = 0x000A,

    /// <summary>
    /// Iteration count for circular references.
    /// </summary>
    CalcCount = 0x000C,

    /// <summary>
    /// Calculation mode.
    /// </summary>
    CalcMode = 0x000D,

    /// <summary>
    /// Whether values are calculated with displayed precision.
    /// </summary>
    Precision = 0x000E,

    /// <summary>
    /// Reference style (A1 or R1C1).
    /// </summary>
    RefMode = 0x000F,

    /// <summary>
    /// Iteration convergence delta.
    /// </summary>
    Delta = 0x0010,

    /// <summary>
    /// Whether iteration is enabled.
    /// </summary>
    Iteration = 0x0011,

    /// <summary>
    /// Sheet or workbook protection flag.
    /// </summary>
    Protect = 0x0012,

    /// <summary>
    /// Protection password hash.
    /// </summary>
    Password = 0x0013,

    /// <summary>
    /// Print header text.
    /// </summary>
    Header = 0x0014,

    /// <summary>
    /// Print footer text.
    /// </summary>
    Footer = 0x0015,

    /// <summary>
    /// External sheet references.
    /// </summary>
    ExternSheet = 0x0017,

    /// <summary>
    /// Defined name (<c>Lbl</c>).
    /// </summary>
    Name = 0x0018,

    /// <summary>
    /// Window protection flag.
    /// </summary>
    WindowProtect = 0x0019,

    /// <summary>
    /// Manual vertical page breaks.
    /// </summary>
    VerticalPageBreaks = 0x001A,

    /// <summary>
    /// Manual horizontal page breaks.
    /// </summary>
    HorizontalPageBreaks = 0x001B,

    /// <summary>
    /// Cell note (comment).
    /// </summary>
    Note = 0x001C,

    /// <summary>
    /// Current selection.
    /// </summary>
    Selection = 0x001D,

    /// <summary>
    /// Date system: whether serial dates count from 1904 rather than 1900.
    /// </summary>
    DateMode = 0x0022,

    /// <summary>
    /// External name.
    /// </summary>
    ExternName = 0x0023,

    /// <summary>
    /// Whether row and column headers are printed.
    /// </summary>
    PrintHeaders = 0x002A,

    /// <summary>
    /// Whether gridlines are printed.
    /// </summary>
    PrintGridlines = 0x002B,

    /// <summary>
    /// File-pass record: the workbook is encrypted or obfuscated.
    /// </summary>
    FilePass = 0x002F,

    /// <summary>
    /// Font description.
    /// </summary>
    Font = 0x0031,

    /// <summary>
    /// Continuation of the preceding record's payload.
    /// </summary>
    Continue = 0x003C,

    /// <summary>
    /// Workbook window position and options.
    /// </summary>
    Window1 = 0x003D,

    /// <summary>
    /// Whether a backup is written on save.
    /// </summary>
    Backup = 0x0040,

    /// <summary>
    /// Window pane split positions.
    /// </summary>
    Pane = 0x0041,

    /// <summary>
    /// Code page used by byte strings in the stream.
    /// </summary>
    CodePage = 0x0042,

    /// <summary>
    /// Default column width.
    /// </summary>
    DefColWidth = 0x0055,

    /// <summary>
    /// Drawing object.
    /// </summary>
    Obj = 0x005D,

    /// <summary>
    /// Name of the user who last saved the workbook.
    /// </summary>
    WriteAccess = 0x005C,

    /// <summary>
    /// Whether the workbook is recalculated before saving.
    /// </summary>
    SaveRecalc = 0x005F,

    /// <summary>
    /// Column width, format, and outline settings for a run of columns.
    /// </summary>
    ColInfo = 0x007D,

    /// <summary>
    /// Outline guttering.
    /// </summary>
    Guts = 0x0080,

    /// <summary>
    /// Sheet-level option flags.
    /// </summary>
    WsBool = 0x0081,

    /// <summary>
    /// Whether the print-gridlines option has been changed.
    /// </summary>
    GridSet = 0x0082,

    /// <summary>
    /// Whether the sheet is centered horizontally when printed.
    /// </summary>
    HCenter = 0x0083,

    /// <summary>
    /// Whether the sheet is centered vertically when printed.
    /// </summary>
    VCenter = 0x0084,

    /// <summary>
    /// Bound-sheet directory entry: a sheet's name, visibility, type, and substream position.
    /// </summary>
    BoundSheet = 0x0085,

    /// <summary>
    /// Locale and default country codes.
    /// </summary>
    Country = 0x008C,

    /// <summary>
    /// Object display mode.
    /// </summary>
    HideObj = 0x008D,

    /// <summary>
    /// Workbook color palette.
    /// </summary>
    Palette = 0x0092,

    /// <summary>
    /// Sheet zoom level.
    /// </summary>
    Scl = 0x00A0,

    /// <summary>
    /// Page setup.
    /// </summary>
    Setup = 0x00A1,

    /// <summary>
    /// A run of adjacent RK-encoded number cells in one row.
    /// </summary>
    MulRk = 0x00BD,

    /// <summary>
    /// A run of adjacent blank (formatted, valueless) cells in one row.
    /// </summary>
    MulBlank = 0x00BE,

    /// <summary>
    /// Add-in manager record count.
    /// </summary>
    Mms = 0x00C1,

    /// <summary>
    /// Rich-text label cell (BIFF5; superseded by the shared string table in BIFF8).
    /// </summary>
    RString = 0x00D6,

    /// <summary>
    /// Row-block cell offsets used for random access.
    /// </summary>
    DbCell = 0x00D7,

    /// <summary>
    /// Workbook boolean options.
    /// </summary>
    BookBool = 0x00DA,

    /// <summary>
    /// Extended format (XF): a cell or style format, including its number-format and font indices.
    /// </summary>
    Xf = 0x00E0,

    /// <summary>
    /// Start of the user-interface header block.
    /// </summary>
    InterfaceHdr = 0x00E1,

    /// <summary>
    /// End of the user-interface header block.
    /// </summary>
    InterfaceEnd = 0x00E2,

    /// <summary>
    /// Merged cell ranges.
    /// </summary>
    MergeCells = 0x00E5,

    /// <summary>
    /// Shared string table (BIFF8 only): the workbook's pooled, deduplicated text values.
    /// </summary>
    Sst = 0x00FC,

    /// <summary>
    /// Label cell whose value is an index into the shared string table (BIFF8 only).
    /// </summary>
    LabelSst = 0x00FD,

    /// <summary>
    /// Hash table over the shared string table (BIFF8 only).
    /// </summary>
    ExtSst = 0x00FF,

    /// <summary>
    /// Sheet dimensions: the used row and column extent.
    /// </summary>
    Dimensions = 0x0200,

    /// <summary>
    /// Blank cell: a formatted cell carrying no value.
    /// </summary>
    Blank = 0x0201,

    /// <summary>
    /// Number cell: an IEEE 754 double-precision value.
    /// </summary>
    Number = 0x0203,

    /// <summary>
    /// Label cell: an inline (non-shared) text value.
    /// </summary>
    Label = 0x0204,

    /// <summary>
    /// Boolean or error cell.
    /// </summary>
    BoolErr = 0x0205,

    /// <summary>
    /// The cached string result of the preceding <see cref="Formula" /> record.
    /// </summary>
    String = 0x0207,

    /// <summary>
    /// Row properties: extent, height, format, and outline settings.
    /// </summary>
    Row = 0x0208,

    /// <summary>
    /// Beginning-of-file record of a BIFF3 stream; not supported, recognized so the failure is reported as a version
    /// mismatch rather than malformed data.
    /// </summary>
    Biff3Bof = 0x0209,

    /// <summary>
    /// Row-block index used for random access.
    /// </summary>
    Index = 0x020B,

    /// <summary>
    /// Array formula.
    /// </summary>
    Array = 0x0221,

    /// <summary>
    /// Default row height.
    /// </summary>
    DefaultRowHeight = 0x0225,

    /// <summary>
    /// Data table (what-if) formula.
    /// </summary>
    Table = 0x0236,

    /// <summary>
    /// Sheet window options.
    /// </summary>
    Window2 = 0x023E,

    /// <summary>
    /// RK cell: a number in the compact 30-bit RK encoding.
    /// </summary>
    Rk = 0x027E,

    /// <summary>
    /// Cell style.
    /// </summary>
    Style = 0x0293,

    /// <summary>
    /// Beginning-of-file record of a BIFF4 stream; not supported, recognized so the failure is reported as a version
    /// mismatch rather than malformed data.
    /// </summary>
    Biff4Bof = 0x0409,

    /// <summary>
    /// Number format: a format index and its format code string.
    /// </summary>
    Format = 0x041E,

    /// <summary>
    /// Shared formula.
    /// </summary>
    ShrFmla = 0x04BC,

    /// <summary>
    /// Beginning of a substream: BIFF version, substream type, and build information.
    /// </summary>
    Bof = 0x0809,

    /// <summary>
    /// Whether the workbook uses natural-language formulas (BIFF8 only).
    /// </summary>
    UsesElfs = 0x0160,

    /// <summary>
    /// External workbook link (BIFF8 only).
    /// </summary>
    SupBook = 0x01AE,

    /// <summary>
    /// Shared-workbook protection flag (BIFF8 only).
    /// </summary>
    Prot4Rev = 0x01AF,

    /// <summary>
    /// Text object (BIFF8 only).
    /// </summary>
    Txo = 0x01B6,

    /// <summary>
    /// Whether external links are refreshed on open (BIFF8 only).
    /// </summary>
    RefreshAll = 0x01B7,

    /// <summary>
    /// Hyperlink (BIFF8 only).
    /// </summary>
    HLink = 0x01B8,

    /// <summary>
    /// Sheet or workbook code name (BIFF8 only).
    /// </summary>
    CodeName = 0x01BA,

    /// <summary>
    /// Shared-workbook protection password (BIFF8 only).
    /// </summary>
    Prot4RevPass = 0x01BC,

    /// <summary>
    /// Beginning-of-file record of a BIFF2 stream; not supported, recognized so the failure is reported as a version
    /// mismatch rather than malformed data.
    /// </summary>
    Biff2Bof = 0x0009,
}
