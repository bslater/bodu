// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWorkbookGlobals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.IO.Biff;

namespace Bodu.Formats.Excel.Biff;

/// <summary>
/// Parses the workbook globals substream: the beginning-of-file marker, the date system, the bound sheets and their
/// stream offsets, the shared string table, and the extended-format and number-format records.
/// </summary>
/// <remarks>
/// The globals substream is read once when a workbook is opened. It validates that the stream is an unencrypted
/// workbook in a supported BIFF version and produces the workbook-wide tables every sheet read depends on.
/// </remarks>
internal sealed class BiffWorkbookGlobals
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffWorkbookGlobals" /> class.
    /// </summary>
    /// <param name="version">The BIFF version of the workbook.</param>
    /// <param name="codePage">The code page in effect at the end of the globals.</param>
    /// <param name="sheets">The bound sheets, in workbook order.</param>
    /// <param name="sharedStrings">The decoded shared string table.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <param name="dateSystem">The workbook's declared date system.</param>
    private BiffWorkbookGlobals(
        BiffVersion version,
        int codePage,
        IReadOnlyList<BiffSheetDirectoryEntry> sheets,
        string[] sharedStrings,
        BiffFormatTable formats,
        ExcelDateSystem dateSystem)
    {
        Version = version;
        CodePage = codePage;
        Sheets = sheets;
        SharedStrings = sharedStrings;
        Formats = formats;
        DateSystem = dateSystem;
    }

    /// <summary>
    /// Gets the BIFF version of the workbook.
    /// </summary>
    /// <value>The version established from the globals BOF record.</value>
    public BiffVersion Version { get; }

    /// <summary>
    /// Gets the code page in effect for byte strings at the end of the globals.
    /// </summary>
    /// <value>The Windows code page number.</value>
    public int CodePage { get; }

    /// <summary>
    /// Gets the bound sheets, in workbook order.
    /// </summary>
    /// <value>The sheet directory.</value>
    public IReadOnlyList<BiffSheetDirectoryEntry> Sheets { get; }

    /// <summary>
    /// Gets the decoded shared string table.
    /// </summary>
    /// <value>The unique strings, indexed as cells reference them; empty when the workbook has no table.</value>
    public string[] SharedStrings { get; }

    /// <summary>
    /// Gets the workbook format table.
    /// </summary>
    /// <value>The XF-to-format resolution table.</value>
    public BiffFormatTable Formats { get; }

    /// <summary>
    /// Gets the workbook's declared date system.
    /// </summary>
    /// <value>The date system; 1900 when no <c>DATEMODE</c> record is present.</value>
    public ExcelDateSystem DateSystem { get; }

    /// <summary>
    /// Gets the reader options that let a sheet substream be read with the version and code page the globals
    /// established.
    /// </summary>
    /// <value>The options seeding a <see cref="BiffReader" />.</value>
    public BiffReaderOptions ReaderOptions => new() { Version = Version, CodePage = CodePage };

    /// <summary>
    /// Parses the globals substream.
    /// </summary>
    /// <param name="globals">The substream bytes, from its BOF record through its EOF record.</param>
    /// <param name="options">The reader options controlling date-format detection.</param>
    /// <returns>The parsed globals.</returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the substream does not begin with a BOF record, the BOF does not open the workbook globals, or a
    /// record the globals depend on is malformed.
    /// </exception>
    /// <exception cref="ExcelBinaryUnsupportedException">
    /// Thrown when the workbook is encoded in a BIFF version the reader does not support.
    /// </exception>
    /// <exception cref="ExcelBinaryEncryptedWorkbookException">
    /// Thrown when the workbook carries a <c>FILEPASS</c> record.
    /// </exception>
    public static BiffWorkbookGlobals Parse(byte[] globals, ExcelBinaryReaderOptions options)
    {
        try
        {
            return ParseCore(globals, options);
        }
        catch (BiffUnsupportedVersionException ex)
        {
            throw new ExcelBinaryUnsupportedException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Op_NotSupported_Biff8Version, ex.RawVersion ?? 0),
                ex);
        }
        catch (BiffFormatException ex)
        {
            throw new ExcelBinaryFormatException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Parses the globals substream, letting codec exceptions propagate for the caller to translate.
    /// </summary>
    /// <param name="globals">The substream bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The parsed globals.</returns>
    private static BiffWorkbookGlobals ParseCore(byte[] globals, ExcelBinaryReaderOptions options)
    {
        var reader = new BiffReader(globals);
        if (!reader.Read() || reader.RecordType != BiffRecordType.Bof)
            throw new ExcelBinaryFormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        BiffBofRecord bof = reader.GetBof();
        ValidateGlobalsBof(bof);

        List<BiffSheetDirectoryEntry> sheets = new();
        List<ushort> xfFormatIndex = new();
        Dictionary<ushort, string> formatCodes = new();
        ExcelDateSystem dateSystem = ExcelDateSystem.Excel1900;
        string[] sharedStrings = [];

        while (reader.Read())
        {
            switch (reader.RecordType)
            {
                case BiffRecordType.FilePass:
                    throw new ExcelBinaryEncryptedWorkbookException(ExcelBinaryResourceStrings.Op_NotSupported_Biff8Encrypted);

                case BiffRecordType.BoundSheet:
                    sheets.Add(ReadBoundSheet(reader.GetBoundSheet()));
                    break;

                case BiffRecordType.DateMode:
                    dateSystem = reader.GetDateMode().Is1904 ? ExcelDateSystem.Excel1904 : ExcelDateSystem.Excel1900;
                    break;

                // A record too short even for its two indices is skipped, as the pre-codec reader did.
                case BiffRecordType.Xf when reader.RecordLength >= 4:
                    xfFormatIndex.Add(reader.GetXf().FormatIndex);
                    break;

                case BiffRecordType.Format:
                    BiffFormatRecord format = reader.GetFormat();
                    formatCodes[format.FormatIndex] = format.Code.GetString();
                    break;

                case BiffRecordType.Sst:
                    sharedStrings = ReadSharedStrings(ref reader);
                    break;

                default:
                    break;
            }
        }

        var formats = new BiffFormatTable([.. xfFormatIndex], formatCodes, options.DetectDateFormats);
        return new BiffWorkbookGlobals(reader.Version, reader.CodePage, sheets, sharedStrings, formats, dateSystem);
    }

    /// <summary>
    /// Validates that the BOF record opens a workbook-globals substream in a supported version.
    /// </summary>
    /// <param name="bof">The decoded BOF record.</param>
    /// <exception cref="ExcelBinaryUnsupportedException">
    /// Thrown when the version is neither BIFF5 nor BIFF8.
    /// </exception>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the substream is not the workbook globals.</exception>
    private static void ValidateGlobalsBof(BiffBofRecord bof)
    {
        if (bof.Version == BiffVersion.Unknown)
        {
            throw new ExcelBinaryUnsupportedException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Op_NotSupported_Biff8Version, bof.RawVersion));
        }

        if (bof.SubstreamType != BiffSubstreamType.WorkbookGlobals)
        {
            throw new ExcelBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8SubstreamType, (ushort)bof.SubstreamType));
        }
    }

    /// <summary>
    /// Projects a decoded bound-sheet record onto a directory entry.
    /// </summary>
    /// <param name="record">The decoded record.</param>
    /// <returns>The directory entry.</returns>
    private static BiffSheetDirectoryEntry ReadBoundSheet(in BiffBoundSheetRecord record) =>
        new(record.StreamOffset, MapSheetType(record.SheetType), MapVisibility(record.State), record.Name.GetString());

    /// <summary>
    /// Maps a bound-sheet visibility state onto the public enumeration.
    /// </summary>
    /// <param name="state">The decoded state.</param>
    /// <returns>The visibility.</returns>
    private static ExcelSheetVisibility MapVisibility(BiffSheetState state) =>
        state switch
        {
            BiffSheetState.Visible => ExcelSheetVisibility.Visible,
            BiffSheetState.Hidden => ExcelSheetVisibility.Hidden,
            _ => ExcelSheetVisibility.VeryHidden,
        };

    /// <summary>
    /// Maps a bound-sheet type onto the public enumeration.
    /// </summary>
    /// <param name="sheetType">The decoded type.</param>
    /// <returns>The sheet type.</returns>
    private static ExcelSheetType MapSheetType(BiffSheetType sheetType) =>
        sheetType switch
        {
            BiffSheetType.Worksheet => ExcelSheetType.Worksheet,
            BiffSheetType.MacroSheet => ExcelSheetType.MacroSheet,
            BiffSheetType.Chart => ExcelSheetType.Chart,
            BiffSheetType.VisualBasicModule => ExcelSheetType.VbaModule,
            _ => ExcelSheetType.Unknown,
        };

    /// <summary>
    /// Materializes the shared string table the reader is positioned on, consuming its continuation records.
    /// </summary>
    /// <param name="reader">The reader, positioned on the SST record.</param>
    /// <returns>The unique strings in table order.</returns>
    private static string[] ReadSharedStrings(ref BiffReader reader)
    {
        var strings = new BiffSstReader(ref reader);
        List<string> result = new((int)Math.Min(strings.Header.UniqueCount, 1024));

        while (strings.Read(ref reader))
            result.Add(strings.GetString());

        return [.. result];
    }
}
