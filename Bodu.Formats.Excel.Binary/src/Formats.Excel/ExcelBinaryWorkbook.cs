// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Formats.Excel.Biff8;
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.Formats.Excel;

/// <summary>
/// Provides a disposable, read-only session over an Excel 97-2003 binary workbook (BIFF8 / <c>.xls</c>), exposing its
/// sheets and the raw cell values of each.
/// </summary>
/// <remarks>
/// <para>
/// Opening a workbook reads its globals once — the date system, the shared string table, the format tables, and the
/// sheet directory — while keeping the compound-file container open. A sheet is read on demand by seeking to the stream
/// offset its bound-sheet record records, so a single sheet can be read without parsing the others and the whole
/// workbook is never materialized.
/// </para>
/// <para>
/// The primary, high-throughput surface is the forward-only <see cref="ExcelWorksheetReader" /> returned by
/// <see cref="OpenWorksheet(int)" />. A materialized, randomly addressable <see cref="ExcelWorksheet" /> is offered as
/// a convenience through <see cref="ReadWorksheet(int)" />.
/// </para>
/// <para>
/// The session owns the underlying container and, unless the caller opts to leave it open, the source stream; dispose
/// the workbook when reading is complete.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Formats.Excel;
///
/// using var workbook = ExcelBinaryWorkbook.OpenRead("report.xls");
/// foreach (ExcelWorksheetInfo info in workbook.Worksheets)
/// {
///     Console.WriteLine($"Sheet '{info.Name}' (#{info.Index})");
///
///     using ExcelWorksheetReader reader = workbook.OpenWorksheet(info.Index);
///     while (reader.TryReadCell(out ExcelCell cell))
///         Console.WriteLine($"  R{cell.RowIndex} C{cell.ColumnIndex}: {cell.Kind}");
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class ExcelBinaryWorkbook
    : IDisposable
{
    /// <summary>The open compound-file container.</summary>
    private readonly CompoundFile _compound;

    /// <summary>The root storage of the compound file.</summary>
    private readonly CompoundStorage _root;

    /// <summary>The name of the workbook stream (<c>Workbook</c> or <c>Book</c>).</summary>
    private readonly string _streamName;

    /// <summary>The parsed workbook globals.</summary>
    private readonly Biff8WorkbookGlobals _globals;

    /// <summary>The worksheet descriptors, in workbook order.</summary>
    private readonly ExcelWorksheetInfo[] _worksheets;

    /// <summary>Whether this workbook has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelBinaryWorkbook" /> class.
    /// </summary>
    /// <param name="compound">The open compound-file container.</param>
    /// <param name="streamName">The name of the workbook stream.</param>
    /// <param name="globals">The parsed workbook globals.</param>
    /// <param name="worksheets">The worksheet descriptors, in workbook order.</param>
    /// <param name="properties">The workbook document properties.</param>
    private ExcelBinaryWorkbook(
        CompoundFile compound,
        string streamName,
        Biff8WorkbookGlobals globals,
        ExcelWorksheetInfo[] worksheets,
        ExcelWorkbookProperties properties)
    {
        _compound = compound;
        _root = compound.RootStorage;
        _streamName = streamName;
        _globals = globals;
        _worksheets = worksheets;
        Properties = properties;
    }

    /// <summary>
    /// Gets the sheets contained in the workbook, in workbook order.
    /// </summary>
    /// <value>A read-only list of <see cref="ExcelWorksheetInfo" /> describing each sheet.</value>
    public IReadOnlyList<ExcelWorksheetInfo> Worksheets => _worksheets;

    /// <summary>
    /// Gets the document properties of the workbook.
    /// </summary>
    /// <value>
    /// The workbook properties; members are <see langword="null" /> when the corresponding property set is absent or
    /// was not read.
    /// </value>
    public ExcelWorkbookProperties Properties { get; }

    /// <summary>
    /// Gets the date system the workbook uses to interpret serial date numbers.
    /// </summary>
    /// <value>
    /// The declared date system; <see cref="ExcelDateSystem.Excel1900" /> when the workbook declares none.
    /// </value>
    public ExcelDateSystem DateSystem => _globals.DateSystem;

    /// <summary>
    /// Opens a workbook from a file path.
    /// </summary>
    /// <param name="path">The path of the <c>.xls</c> file.</param>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the file is not a valid compound file.</exception>
    /// <exception cref="ExcelBinaryWorkbookStreamNotFoundException">
    /// Thrown when the compound file has no workbook stream.
    /// </exception>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the workbook stream is not valid BIFF.</exception>
    /// <exception cref="ExcelBinaryUnsupportedException">Thrown when the workbook is not BIFF8.</exception>
    /// <exception cref="ExcelBinaryEncryptedWorkbookException">Thrown when the workbook is encrypted.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Formats.Excel;
    ///
    /// using var workbook = ExcelBinaryWorkbook.OpenRead("report.xls");
    /// Console.WriteLine($"{workbook.Worksheets.Count} sheet(s), date system: {workbook.DateSystem}");
    ///]]>
    /// </code>
    /// </example>
    public static ExcelBinaryWorkbook OpenRead(string path) =>
        OpenRead(path, ExcelBinaryReaderOptions.Default);

    /// <summary>
    /// Opens a workbook from a file path with the specified options.
    /// </summary>
    /// <param name="path">The path of the <c>.xls</c> file.</param>
    /// <param name="options">The options governing how the workbook is read.</param>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Formats.Excel;
    ///
    /// // Skip document properties and date-format detection for a fast, numeric-only read.
    /// var options = new ExcelBinaryReaderOptions { ReadDocumentProperties = false, DetectDateFormats = false };
    /// using var workbook = ExcelBinaryWorkbook.OpenRead("data.xls", options);
    ///]]>
    /// </code>
    /// </example>
    public static ExcelBinaryWorkbook OpenRead(string path, ExcelBinaryReaderOptions options)
    {
        ThrowHelper.ThrowIfNull(path);
        ThrowHelper.ThrowIfNull(options);

        FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            return OpenCore(stream, leaveOpen: false, options);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Opens a workbook from a file.
    /// </summary>
    /// <param name="file">The <c>.xls</c> file to open.</param>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="file" /> is <see langword="null" />.
    /// </exception>
    public static ExcelBinaryWorkbook OpenRead(FileInfo file)
    {
        ThrowHelper.ThrowIfNull(file);

        return OpenRead(file.FullName, ExcelBinaryReaderOptions.Default);
    }

    /// <summary>
    /// Opens a workbook over the supplied stream.
    /// </summary>
    /// <param name="stream">The stream containing the <c>.xls</c> file; read from its current position.</param>
    /// <param name="leaveOpen">
    /// <see langword="true" /> to leave <paramref name="stream" /> open when the workbook is disposed; otherwise
    /// <see langword="false" />.
    /// </param>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream is not a valid compound file.</exception>
    /// <exception cref="ExcelBinaryWorkbookStreamNotFoundException">
    /// Thrown when the compound file has no workbook stream.
    /// </exception>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the workbook stream is not valid BIFF.</exception>
    /// <exception cref="ExcelBinaryUnsupportedException">Thrown when the workbook is not BIFF8.</exception>
    /// <exception cref="ExcelBinaryEncryptedWorkbookException">Thrown when the workbook is encrypted.</exception>
    public static ExcelBinaryWorkbook OpenRead(Stream stream, bool leaveOpen = false) =>
        OpenCore(stream, leaveOpen, ExcelBinaryReaderOptions.Default);

    /// <summary>
    /// Opens a workbook over the supplied stream with the specified options.
    /// </summary>
    /// <param name="stream">The stream containing the <c>.xls</c> file; read from its current position.</param>
    /// <param name="options">The options governing how the workbook is read.</param>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Formats.Excel;
    ///
    /// // Read from a stream that the caller continues to own.
    /// using var source = File.OpenRead("report.xls");
    /// var options = new ExcelBinaryReaderOptions { LeaveOpen = true };
    /// using var workbook = ExcelBinaryWorkbook.Open(source, options);
    ///]]>
    /// </code>
    /// </example>
    public static ExcelBinaryWorkbook Open(Stream stream, ExcelBinaryReaderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        return OpenCore(stream, options.LeaveOpen, options);
    }

    /// <summary>
    /// Opens a forward-only reader over the sheet at the specified index.
    /// </summary>
    /// <param name="worksheetIndex">The zero-based sheet index.</param>
    /// <returns>A forward-only <see cref="ExcelWorksheetReader" /> over the sheet's value records.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="worksheetIndex" /> is out of range.
    /// </exception>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the sheet substream is malformed.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the workbook has been disposed.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Formats.Excel;
    ///
    /// using var workbook = ExcelBinaryWorkbook.OpenRead("report.xls");
    /// using ExcelWorksheetReader reader = workbook.OpenWorksheet(0);
    /// while (reader.TryReadCell(out ExcelCell cell))
    /// {
    ///     if (cell.Kind == ExcelCellKind.Number)
    ///         Console.WriteLine($"R{cell.RowIndex} C{cell.ColumnIndex} = {cell.NumberValue}");
    /// }
    ///]]>
    /// </code>
    /// </example>
    public ExcelWorksheetReader OpenWorksheet(int worksheetIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if ((uint)worksheetIndex >= (uint)_worksheets.Length)
            throw new ArgumentOutOfRangeException(nameof(worksheetIndex));

        byte[] substream = ReadSheetSubstream(worksheetIndex);
        return new ExcelWorksheetReader(_worksheets[worksheetIndex], substream, _globals.SharedStrings, _globals.Formats);
    }

    /// <summary>
    /// Opens a forward-only reader over the sheet with the specified name.
    /// </summary>
    /// <param name="worksheetName">The sheet name, compared using ordinal equality.</param>
    /// <returns>A forward-only <see cref="ExcelWorksheetReader" /> over the sheet's value records.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="worksheetName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown when no sheet with the given name exists.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the workbook has been disposed.</exception>
    public ExcelWorksheetReader OpenWorksheet(string worksheetName) =>
        OpenWorksheet(IndexOf(worksheetName));

    /// <summary>
    /// Reads the sheet at the specified index into a materialized, randomly addressable view.
    /// </summary>
    /// <param name="worksheetIndex">The zero-based sheet index.</param>
    /// <returns>A materialized <see cref="ExcelWorksheet" /> buffering the sheet's populated cells.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="worksheetIndex" /> is out of range.
    /// </exception>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the sheet substream is malformed.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the workbook has been disposed.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Formats.Excel;
    ///
    /// using var workbook = ExcelBinaryWorkbook.OpenRead("report.xls");
    /// ExcelWorksheet sheet = workbook.ReadWorksheet(0);
    /// if (sheet.TryGetCell(0, 0, out ExcelCell a1))
    ///     Console.WriteLine($"A1 = {a1.StringValue ?? a1.NumberValue?.ToString()}");
    ///]]>
    /// </code>
    /// </example>
    public ExcelWorksheet ReadWorksheet(int worksheetIndex)
    {
        using ExcelWorksheetReader reader = OpenWorksheet(worksheetIndex);
        return new ExcelWorksheet(reader.Worksheet, reader.ReadCells());
    }

    /// <summary>
    /// Reads the sheet with the specified name into a materialized, randomly addressable view.
    /// </summary>
    /// <param name="worksheetName">The sheet name, compared using ordinal equality.</param>
    /// <returns>A materialized <see cref="ExcelWorksheet" /> buffering the sheet's populated cells.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="worksheetName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown when no sheet with the given name exists.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the workbook has been disposed.</exception>
    public ExcelWorksheet ReadWorksheet(string worksheetName) =>
        ReadWorksheet(IndexOf(worksheetName));

    /// <summary>
    /// Gets the format code for a number-format index, including the well-known built-in formats.
    /// </summary>
    /// <param name="formatIndex">The number-format index, as carried by <see cref="ExcelCell.FormatIndex" />.</param>
    /// <returns>The format code, or <see langword="null" /> when none is known for the index.</returns>
    public string? GetNumberFormatCode(ushort formatIndex) =>
        _globals.Formats.GetFormatCode(formatIndex);

    /// <summary>
    /// Converts a numeric cell to a <see cref="DateTime" /> using the workbook's date system.
    /// </summary>
    /// <param name="cell">The cell to convert.</param>
    /// <returns>
    /// The date and time represented by the cell's value, or <see langword="null" /> when the cell is not numeric.
    /// </returns>
    /// <remarks>
    /// The conversion is applied to any numeric cell; inspect <see cref="ExcelCell.IsDateFormatted" /> first when only
    /// date-formatted cells should be treated as dates.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the cell's value is outside the range of representable OLE Automation dates.
    /// </exception>
    public DateTime? GetDateTime(ExcelCell cell) =>
        cell.Kind == ExcelCellKind.Number && cell.NumberValue.HasValue
            ? ExcelSerialDate.ToDateTime(cell.NumberValue.Value, DateSystem)
            : null;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _compound.Dispose();
    }

    /// <summary>
    /// Opens a workbook over a stream, parsing its globals and describing its sheets.
    /// </summary>
    /// <param name="stream">The stream containing the <c>.xls</c> file.</param>
    /// <param name="leaveOpen">Whether to leave the stream open when the workbook is disposed.</param>
    /// <param name="options">The options governing how the workbook is read.</param>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" />.</returns>
    private static ExcelBinaryWorkbook OpenCore(Stream stream, bool leaveOpen, ExcelBinaryReaderOptions options)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);

        CompoundFile compound = CompoundFile.Open(stream, leaveOpen: leaveOpen, buffered: false);
        try
        {
            CompoundStorage root = compound.RootStorage;
            string streamName = ResolveWorkbookStreamName(root);

            byte[] globalsBytes = ReadGlobalsSubstream(root, streamName);
            Biff8WorkbookGlobals globals = Biff8WorkbookGlobals.Parse(globalsBytes, options);

            ExcelWorksheetInfo[] worksheets = DescribeWorksheets(root, streamName, globals);
            ExcelWorkbookProperties properties = options.ReadDocumentProperties
                ? ReadProperties(compound)
                : ExcelWorkbookProperties.Empty;

            return new ExcelBinaryWorkbook(compound, streamName, globals, worksheets, properties);
        }
        catch
        {
            compound.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Resolves the name of the workbook stream within the compound file.
    /// </summary>
    /// <param name="root">The root storage of the compound file.</param>
    /// <returns>The name of the workbook stream.</returns>
    /// <exception cref="ExcelBinaryWorkbookStreamNotFoundException">
    /// Thrown when neither a <c>Workbook</c> nor a <c>Book</c> stream is present.
    /// </exception>
    private static string ResolveWorkbookStreamName(CompoundStorage root)
    {
        if (root.TryOpenStream("Workbook", out CompoundStream? workbook))
        {
            workbook.Dispose();
            return "Workbook";
        }

        if (root.TryOpenStream("Book", out CompoundStream? book))
        {
            book.Dispose();
            return "Book";
        }

        throw new ExcelBinaryWorkbookStreamNotFoundException(ExcelBinaryResourceStrings.IO_KeyNotFound_Biff8Workbook);
    }

    /// <summary>
    /// Reads the workbook globals substream from the workbook stream.
    /// </summary>
    /// <param name="root">The root storage of the compound file.</param>
    /// <param name="streamName">The name of the workbook stream.</param>
    /// <returns>The bytes of the globals substream.</returns>
    private static byte[] ReadGlobalsSubstream(CompoundStorage root, string streamName)
    {
        using CompoundStream stream = root.OpenStream(streamName);
        return Biff8SubstreamLoader.ReadSubstream(stream);
    }

    /// <summary>
    /// Describes every sheet declared by the workbook, reading each sheet's dimensions without parsing its body.
    /// </summary>
    /// <param name="root">The root storage of the compound file.</param>
    /// <param name="streamName">The name of the workbook stream.</param>
    /// <param name="globals">The parsed workbook globals.</param>
    /// <returns>The worksheet descriptors, in workbook order.</returns>
    private static ExcelWorksheetInfo[] DescribeWorksheets(CompoundStorage root, string streamName, Biff8WorkbookGlobals globals)
    {
        IReadOnlyList<Biff8SheetDirectoryEntry> entries = globals.Sheets;
        var worksheets = new ExcelWorksheetInfo[entries.Count];
        if (entries.Count == 0)
            return worksheets;

        using CompoundStream stream = root.OpenStream(streamName);
        for (int i = 0; i < entries.Count; i++)
        {
            Biff8SheetDirectoryEntry entry = entries[i];
            ExcelWorksheetDimensions dimensions = entry.Type == ExcelSheetType.Worksheet
                ? Biff8DimensionsReader.Read(stream, entry.StreamOffset)
                : default;

            worksheets[i] = new ExcelWorksheetInfo(entry.Name, i, entry.Visibility, entry.Type, dimensions);
        }

        return worksheets;
    }

    /// <summary>
    /// Reads the workbook's document-property sets from the compound file, tolerating their absence or corruption.
    /// </summary>
    /// <param name="compound">The open compound file.</param>
    /// <returns>
    /// The workbook properties; absent or unparsable property sets yield <see langword="null" /> members.
    /// </returns>
    private static ExcelWorkbookProperties ReadProperties(CompoundFile compound)
    {
        SummaryInformation? summary = TryReadSummary(compound, SummaryInformation.StreamName, SummaryInformation.Read);
        DocumentSummaryInformation? document =
            TryReadSummary(compound, DocumentSummaryInformation.StreamName, DocumentSummaryInformation.Read);

        return new ExcelWorkbookProperties(summary, document);
    }

    /// <summary>
    /// Reads and parses a property-set stream, returning <see langword="null" /> when the stream is absent or
    /// malformed.
    /// </summary>
    /// <typeparam name="T">The property-set view type.</typeparam>
    /// <param name="compound">The open compound file.</param>
    /// <param name="streamName">The name of the property-set stream.</param>
    /// <param name="parse">The parser that materializes the view from the stream.</param>
    /// <returns>The parsed property set, or <see langword="null" /> when it is absent or cannot be parsed.</returns>
    private static T? TryReadSummary<T>(CompoundFile compound, string streamName, Func<Stream, T> parse)
        where T : class
    {
        if (!compound.RootStorage.TryOpenStream(streamName, out CompoundStream? stream))
            return null;

        using (stream)
        {
            try
            {
                // Property metadata is auxiliary; a malformed set must never prevent the workbook from loading.
                return parse(stream);
            }
            catch (CompoundFileFormatException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Reads the substream of the sheet at the specified index from the workbook stream.
    /// </summary>
    /// <param name="worksheetIndex">The zero-based sheet index.</param>
    /// <returns>The sheet substream bytes, from its BOF record through its EOF record.</returns>
    private byte[] ReadSheetSubstream(int worksheetIndex)
    {
        long offset = _globals.Sheets[worksheetIndex].StreamOffset;
        using CompoundStream stream = _root.OpenStream(_streamName);
        stream.Seek(offset, SeekOrigin.Begin);

        return Biff8SubstreamLoader.ReadSubstream(stream);
    }

    /// <summary>
    /// Finds the index of the sheet with the specified name.
    /// </summary>
    /// <param name="worksheetName">The sheet name, compared using ordinal equality.</param>
    /// <returns>The zero-based index of the matching sheet.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="worksheetName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown when no sheet with the given name exists.</exception>
    private int IndexOf(string worksheetName)
    {
        ThrowHelper.ThrowIfNull(worksheetName);

        for (int i = 0; i < _worksheets.Length; i++)
        {
            if (string.Equals(_worksheets[i].Name, worksheetName, StringComparison.Ordinal))
                return i;
        }

        throw new KeyNotFoundException(
            string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.IO_KeyNotFound_Biff8Sheet, worksheetName));
    }
}
