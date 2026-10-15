// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using Bodu.IO.Biff;
using Bodu.IO.Compound;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Excel;

/// <summary>
/// Holds Bodu.Formats.Excel.Binary and Bodu.IO.Biff to the defect fixes other <c>.xls</c> readers list in their release
/// notes, as cataloged in <c>corpus/biff/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A row's input is a generator expression, <c>gen:Stream(...)</c> or <c>gen:Sheet(...)</c>, that lays out a BIFF
/// record stream byte by byte from [MS-XLS], independently of <see cref="BiffWriter" />; <c>corpus/biff/README.md</c>
/// defines the notation. The Workbook surface stores the stream in a compound file that <see cref="CompoundFile" />
/// writes, opens it with <see cref="ExcelBinaryWorkbook.Open" />, and renders a line for each sheet and for each of its
/// cells; the Records surface reads the stream with <see cref="BiffReader" /> and renders a line for each record.
/// </para>
/// <para>
/// A <c>parse</c> row compares the rendering with its expected text, a <c>reject</c> row expects the exception it
/// names, and a <c>roundtrip</c> row writes every record back through <see cref="BiffWriter" /> and compares the record
/// listings of the two streams. Every row runs under a timeout, so an input that makes a reader loop forever fails
/// rather than hangs the run.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogs this class runs.</summary>
    private const string Area = "biff";

    /// <summary>The time, in milliseconds, that one row may run before it fails.</summary>
    /// <remarks>
    /// The limit catches a reader that loops forever, not a slow one, so it leaves room for a Debug build under an IDE's
    /// debugger: the slowest row, an SST of 1,048,577 strings, runs in about 0.3 s in CI's Release build and about 1 s in
    /// a Debug build. Each row runs as its own test, so a row that passes the limit fails alone.
    /// </remarks>
    private const int RowTimeout = 60_000;

    /// <summary>The option names a catalog row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames = ["Surface", "Version", "Book", "Formats", "DetectDates"];

    /// <summary>The exceptions a <c>reject</c> row on the Workbook surface may name.</summary>
    private static readonly string[] s_workbookExceptions =
    [
        nameof(ExcelBinaryFormatException),
        nameof(ExcelBinaryUnsupportedException),
        nameof(ExcelBinaryEncryptedWorkbookException),
        nameof(ExcelBinaryWorkbookStreamNotFoundException),
        nameof(CompoundFileFormatException),
    ];

    /// <summary>The exceptions a <c>reject</c> row on the Records surface may name.</summary>
    private static readonly string[] s_recordsExceptions = [nameof(BiffFormatException), nameof(BiffUnsupportedVersionException)];

    /// <summary>The catalogs embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(ExcelBinaryReleaseNoteCorpusTests).Assembly));

    /// <summary>
    /// Gets the runnable rows of kind <c>parse</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> ParseRows =>
        RunnableRows("parse");

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RejectRows =>
        RunnableRows("reject");

    /// <summary>
    /// Gets the runnable rows of kind <c>roundtrip</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RoundTripRows =>
        RunnableRows("roundtrip");

    /// <summary>
    /// Gets every row of every embedded catalog.
    /// </summary>
    /// <value>The rows, catalog by catalog in file order.</value>
    private static IEnumerable<ReleaseNoteFix> AllRows =>
        s_catalogs.Value.SelectMany(catalog => catalog.Rows);

    /// <summary>
    /// Selects the runnable rows of one kind as test data.
    /// </summary>
    /// <param name="kind">The row kind.</param>
    /// <returns>
    /// One single-element argument array per <c>applies</c> or <c>dialect</c> row of <paramref name="kind" />.
    /// </returns>
    private static IEnumerable<object[]> RunnableRows(string kind) =>
        AllRows.Where(fix => fix.IsRunnable && fix.Kind == kind).Select(fix => new object[] { fix });

    /// <summary>
    /// Renders a generated input through the surface the row's options select.
    /// </summary>
    /// <param name="input">The generated input.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering, its lines separated by line feeds.</returns>
    private static string Render(GeneratedInput input, RowOptions options) =>
        options.ReadsRecords
            ? RenderRecords(input.Stream, options, includeLengths: true)
            : RenderWorkbook(input.CreateFile(), options);

    /// <summary>
    /// Renders a workbook through <see cref="ExcelBinaryWorkbook" />: an optional <c>#book</c> line, then for each
    /// sheet a <c>#sheet</c> line followed by one line per cell in row-major order.
    /// </summary>
    /// <param name="file">The bytes of the file to open.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering, its lines separated by line feeds.</returns>
    /// <remarks>
    /// The cells of a sheet are those <see cref="ExcelWorksheetReader.ReadCells" /> returns, sorted by row and then by
    /// column; the sort is stable, so cells at the same position keep their record order.
    /// </remarks>
    private static string RenderWorkbook(byte[] file, RowOptions options)
    {
        var lines = new List<string>();
        using var stream = new MemoryStream(file, writable: false);
        using var workbook = ExcelBinaryWorkbook.Open(stream, options.ToWorkbookOptions());
        if (options.Book)
            lines.Add($"#book\t{workbook.BiffVersion}\t{workbook.DateSystem}");

        foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
        {
            ExcelWorksheetDimensions dimensions = sheet.Dimensions;
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"#sheet\t{sheet.Index}\t{EscapeValue(sheet.Name)}\t{sheet.Visibility}\t{sheet.Type}\tr{dimensions.FirstRowIndex}+{dimensions.RowCount}:c{dimensions.FirstColumnIndex}+{dimensions.ColumnCount}"));

            List<ExcelCell> cells;
            using (ExcelWorksheetReader reader = workbook.OpenWorksheet(sheet.Index))
                cells = [.. reader.ReadCells()];

            foreach (ExcelCell cell in cells.OrderBy(cell => cell.RowIndex).ThenBy(cell => cell.ColumnIndex))
                lines.Add(DescribeCell(workbook, cell, options));
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Describes one cell as a Workbook rendering line.
    /// </summary>
    /// <param name="workbook">The workbook the cell was read from.</param>
    /// <param name="cell">The cell.</param>
    /// <param name="options">The row options.</param>
    /// <returns>
    /// The cell's A1 reference, kind and value, tab-separated; with <see cref="RowOptions.Formats" />, also its format
    /// index, its format code (<c>(null)</c> when there is none) and <c>date</c> or <c>-</c>.
    /// </returns>
    private static string DescribeCell(ExcelBinaryWorkbook workbook, ExcelCell cell, RowOptions options)
    {
        var line = new StringBuilder();
        line.Append(ExcelCellReference.ToA1(cell.RowIndex, cell.ColumnIndex)).Append('\t').Append(cell.Kind).Append('\t');
        line.Append(cell.Kind switch
        {
            ExcelCellKind.String => EscapeValue(cell.StringValue!),
            ExcelCellKind.Number => FormatNumber(cell.NumberValue!.Value),
            ExcelCellKind.Boolean => cell.BooleanValue!.Value ? "true" : "false",
            ExcelCellKind.Error => FormatError(cell.ErrorValue!.Value),
            _ => string.Empty,
        });

        if (options.Formats)
        {
            string? code = workbook.GetNumberFormatCode(cell.FormatIndex);
            line.Append('\t').Append(FormatInteger(cell.FormatIndex));
            line.Append('\t').Append(code is null ? "(null)" : EscapeValue(code));
            line.Append('\t').Append(cell.IsDateFormatted ? "date" : "-");
        }

        return line.ToString();
    }

    /// <summary>
    /// Renders a record stream through <see cref="BiffReader" />, one line per record and one more per shared string.
    /// </summary>
    /// <param name="stream">The record stream.</param>
    /// <param name="options">The row options.</param>
    /// <param name="includeLengths">Whether each record line carries the record length after the record name.</param>
    /// <returns>The rendering, its lines separated by line feeds.</returns>
    private static string RenderRecords(byte[] stream, RowOptions options, bool includeLengths)
    {
        var lines = new List<string>();
        var reader = new BiffReader(stream, isFinalBlock: true, options.ToReaderState());
        while (reader.Read())
        {
            if (reader.RecordType == BiffRecordType.Sst)
                _ = ReadSharedStrings(ref reader, lines, includeLengths);
            else
                lines.Add(DescribeRecord(ref reader, includeLengths));
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Reads a record stream and writes every record back through <see cref="BiffWriter" />.
    /// </summary>
    /// <param name="stream">The record stream.</param>
    /// <param name="options">The row options.</param>
    /// <returns>
    /// The record listing of <paramref name="stream" />, without record lengths, and the bytes written.
    /// </returns>
    /// <remarks>
    /// A record the Records surface decodes is written through its typed writer, a shared string table and its CONTINUE
    /// records through <see cref="BiffWriter.WriteSst(ReadOnlySpan{string}, uint)" />, and every other record through
    /// <see cref="BiffWriter.WriteRecord(ushort, ReadOnlySpan{byte})" />.
    /// </remarks>
    private static (string Listing, byte[] Written) Rewrite(byte[] stream, RowOptions options)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, FindWriterOptions(stream, options));
        var lines = new List<string>();
        var reader = new BiffReader(stream, isFinalBlock: true, options.ToReaderState());
        while (reader.Read())
        {
            if (reader.RecordType == BiffRecordType.Sst)
            {
                (string[] strings, uint totalCount) = ReadSharedStrings(ref reader, lines, includeLengths: false);
                writer.WriteSst(strings, totalCount);
                continue;
            }

            lines.Add(DescribeRecord(ref reader, includeLengths: false));
            RewriteRecord(ref reader, ref writer);
        }

        return (string.Join('\n', lines), output.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Chooses the writer options for a round trip: the version of the first BOF record and the first CODEPAGE value.
    /// </summary>
    /// <param name="stream">The record stream.</param>
    /// <param name="options">The row options, whose version, when named, takes precedence over the first BOF's.</param>
    /// <returns>
    /// The writer options; BIFF8 when no version is found, and code page zero when the stream has no CODEPAGE.
    /// </returns>
    /// <remarks>
    /// The scan walks the record headers directly and stops at the first record that runs past the end of the stream,
    /// so it never throws on a malformed stream; the reader reports such a stream when the round trip reads it.
    /// </remarks>
    private static BiffWriterOptions FindWriterOptions(byte[] stream, RowOptions options)
    {
        BiffVersion version = options.Version ?? BiffVersion.Unknown;
        var codePage = 0;
        var position = 0;
        while (position + 4 <= stream.Length)
        {
            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(stream.AsSpan(position));
            int length = BinaryPrimitives.ReadUInt16LittleEndian(stream.AsSpan(position + 2));
            if (position + 4 + length > stream.Length)
                break;

            ReadOnlySpan<byte> payload = stream.AsSpan(position + 4, length);
            if (id == (ushort)BiffRecordType.Bof && version == BiffVersion.Unknown && length >= 2)
                version = BinaryPrimitives.ReadUInt16LittleEndian(payload) == 0x0500 ? BiffVersion.Biff5 : BiffVersion.Biff8;

            if (id == (ushort)BiffRecordType.CodePage && codePage == 0 && length >= 2)
                codePage = BinaryPrimitives.ReadUInt16LittleEndian(payload);

            position += 4 + length;
        }

        return new BiffWriterOptions
        {
            Version = version == BiffVersion.Unknown ? BiffVersion.Biff8 : version,
            CodePage = codePage,
        };
    }

    /// <summary>
    /// Reads the shared string table that starts at the current record, across its CONTINUE records, and adds its
    /// lines.
    /// </summary>
    /// <param name="reader">The reader, on an SST record; it is left on the last record the table spans.</param>
    /// <param name="lines">Receives the SST line, then one <c>#sst</c> line per string.</param>
    /// <param name="includeLengths">Whether the SST line carries the record length after the record name.</param>
    /// <returns>The strings, in table order, and the total reference count the table declares.</returns>
    /// <exception cref="BiffFormatException">The table is malformed.</exception>
    private static (string[] Strings, uint TotalCount) ReadSharedStrings(ref BiffReader reader, List<string> lines, bool includeLengths)
    {
        var line = new StringBuilder(NameRecord(reader.RecordId));
        if (includeLengths)
            line.Append('\t').Append(FormatInteger(reader.RecordLength));

        var table = new BiffSstReader(ref reader);
        line.Append('\t').Append(FormatInteger(table.Header.TotalCount)).Append('\t').Append(FormatInteger(table.Header.UniqueCount));
        lines.Add(line.ToString());

        var strings = new List<string>();
        while (table.Read(ref reader))
        {
            string text = table.GetString();
            strings.Add(text);
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"#sst\t{table.Index}\t{EscapeValue(text)}"));
        }

        return ([.. strings], table.Header.TotalCount);
    }

    /// <summary>
    /// Describes the current record as a Records rendering line.
    /// </summary>
    /// <param name="reader">The reader, on a record other than SST.</param>
    /// <param name="includeLengths">Whether the line carries the record length after the record name.</param>
    /// <returns>
    /// The record name (or <c>0xNNNN</c>), then, for the records the Records surface decodes, their fields,
    /// tab-separated, as <c>corpus/biff/README.md</c> lists them.
    /// </returns>
    /// <exception cref="BiffFormatException">A decoded record is malformed.</exception>
    private static string DescribeRecord(ref BiffReader reader, bool includeLengths)
    {
        var line = new StringBuilder(NameRecord(reader.RecordId));
        if (includeLengths)
            line.Append('\t').Append(FormatInteger(reader.RecordLength));

        switch (reader.RecordType)
        {
            case BiffRecordType.Bof:
            {
                BiffBofRecord bof = reader.GetBof();
                line.Append('\t').Append(bof.Version).Append('\t').Append(NameSubstream(bof.SubstreamType));
                break;
            }

            case BiffRecordType.CodePage:
            {
                BiffCodePageRecord codePage = reader.GetCodePage();
                line.Append('\t').Append(FormatInteger(codePage.RawValue)).Append('\t').Append(FormatInteger(codePage.CodePage));
                break;
            }

            case BiffRecordType.DateMode:
                line.Append('\t').Append(reader.GetDateMode().Is1904 ? "1904" : "1900");
                break;

            case BiffRecordType.BoundSheet:
            {
                BiffBoundSheetRecord sheet = reader.GetBoundSheet();
                line.Append('\t').Append(FormatInteger(sheet.StreamOffset)).Append('\t').Append(sheet.State);
                line.Append('\t').Append(NameSheetType(sheet.SheetType)).Append('\t').Append(EscapeValue(sheet.Name.GetString()));
                break;
            }

            case BiffRecordType.Dimensions:
            {
                BiffDimensionsRecord dimensions = reader.GetDimensions();
                line.Append('\t').Append(FormatInteger(dimensions.FirstRow)).Append('\t').Append(FormatInteger(dimensions.LastRowExclusive));
                line.Append('\t').Append(FormatInteger(dimensions.FirstColumn)).Append('\t').Append(FormatInteger(dimensions.LastColumnExclusive));
                break;
            }

            case BiffRecordType.Row:
            {
                BiffRowRecord row = reader.GetRow();
                line.Append('\t').Append(FormatInteger(row.Row)).Append('\t').Append(FormatInteger(row.FirstColumn));
                line.Append('\t').Append(FormatInteger(row.LastColumnExclusive));
                break;
            }

            case BiffRecordType.Number:
            {
                BiffNumberRecord number = reader.GetNumber();
                line.Append('\t').Append(FormatCell(number.Row, number.Column, number.XfIndex)).Append('\t').Append(FormatNumber(number.Value));
                break;
            }

            case BiffRecordType.Rk:
            {
                BiffRkRecord rk = reader.GetRk();
                line.Append('\t').Append(FormatCell(rk.Row, rk.Column, rk.XfIndex)).Append('\t').Append(FormatNumber(rk.Value));
                break;
            }

            case BiffRecordType.MulRk:
            {
                BiffMulRkRecord cells = reader.GetMulRk();
                line.Append('\t').Append(FormatInteger(cells.Row)).Append('\t').Append(FormatInteger(cells.FirstColumn));
                line.Append('\t').Append(FormatInteger(cells.LastColumn));
                for (var i = 0; i < cells.Count; i++)
                {
                    BiffRkCell cell = cells[i];
                    line.Append('\t').Append(FormatInteger(cell.XfIndex)).Append(':').Append(FormatNumber(cell.Value));
                }

                break;
            }

            case BiffRecordType.Blank:
            {
                BiffBlankRecord blank = reader.GetBlank();
                line.Append('\t').Append(FormatCell(blank.Row, blank.Column, blank.XfIndex));
                break;
            }

            case BiffRecordType.MulBlank:
            {
                BiffMulBlankRecord blanks = reader.GetMulBlank();
                line.Append('\t').Append(FormatInteger(blanks.Row)).Append('\t').Append(FormatInteger(blanks.FirstColumn));
                line.Append('\t').Append(FormatInteger(blanks.LastColumn)).Append('\t').Append(FormatInteger(blanks.Count));
                break;
            }

            case BiffRecordType.BoolErr:
            {
                BiffBoolErrRecord value = reader.GetBoolErr();
                line.Append('\t').Append(FormatCell(value.Row, value.Column, value.XfIndex)).Append('\t');
                line.Append(value.IsError ? "error:" + FormatByte(value.ErrorCode) : value.BooleanValue ? "true" : "false");
                break;
            }

            case BiffRecordType.Label:
            {
                BiffLabelRecord label = reader.GetLabel();
                line.Append('\t').Append(FormatCell(label.Row, label.Column, label.XfIndex)).Append('\t').Append(EscapeValue(label.Text.GetString()));
                break;
            }

            case BiffRecordType.LabelSst:
            {
                BiffLabelSstRecord label = reader.GetLabelSst();
                line.Append('\t').Append(FormatCell(label.Row, label.Column, label.XfIndex)).Append('\t').Append(FormatInteger(label.SstIndex));
                break;
            }

            case BiffRecordType.RString:
            {
                BiffRStringRecord label = reader.GetRString();
                line.Append('\t').Append(FormatCell(label.Row, label.Column, label.XfIndex)).Append('\t').Append(EscapeValue(label.Text.GetString()));
                line.Append('\t').Append(FormatInteger(label.RunCount));
                break;
            }

            case BiffRecordType.Formula:
            {
                BiffFormulaRecord formula = reader.GetFormula();
                line.Append('\t').Append(FormatCell(formula.Row, formula.Column, formula.XfIndex)).Append('\t').Append(formula.CachedResultKind);
                switch (formula.CachedResultKind)
                {
                    case BiffCachedResultKind.Number:
                        line.Append('\t').Append(FormatNumber(formula.NumberValue));
                        break;

                    case BiffCachedResultKind.Boolean:
                        line.Append('\t').Append(formula.BooleanValue ? "true" : "false");
                        break;

                    case BiffCachedResultKind.Error:
                        line.Append('\t').Append(FormatByte(formula.ErrorCode));
                        break;
                }

                break;
            }

            case BiffRecordType.String:
                line.Append('\t').Append(EscapeValue(reader.GetString().Text.GetString()));
                break;

            case BiffRecordType.Xf:
            {
                BiffXfRecord format = reader.GetXf();
                line.Append('\t').Append(FormatInteger(format.FontIndex)).Append('\t').Append(FormatInteger(format.FormatIndex));
                break;
            }

            case BiffRecordType.Format:
            {
                BiffFormatRecord format = reader.GetFormat();
                line.Append('\t').Append(FormatInteger(format.FormatIndex)).Append('\t').Append(EscapeValue(format.Code.GetString()));
                break;
            }

            case BiffRecordType.Font:
            {
                BiffFontRecord font = reader.GetFont();
                line.Append('\t').Append(FormatInteger(font.Height)).Append('\t').Append(FormatInteger(font.Weight));
                line.Append('\t').Append(FormatInteger(font.Escapement)).Append('\t').Append(EscapeValue(font.Name.GetString()));
                break;
            }

            case BiffRecordType.FilePass:
                line.Append('\t').Append(reader.GetFilePass().EncryptionType);
                break;
        }

        return line.ToString();
    }

    /// <summary>
    /// Writes the current record back through a writer.
    /// </summary>
    /// <param name="reader">The reader, on a record other than SST.</param>
    /// <param name="writer">The writer.</param>
    /// <remarks>
    /// Each record the Records surface decodes, except RSTRING and FILEPASS, which have no typed writer, is decoded and
    /// written through its typed writer; every other record is written as it was read.
    /// </remarks>
    /// <exception cref="BiffFormatException">A decoded record is malformed.</exception>
    private static void RewriteRecord(ref BiffReader reader, ref BiffWriter writer)
    {
        switch (reader.RecordType)
        {
            case BiffRecordType.Bof:
            {
                BiffBofRecord bof = reader.GetBof();
                writer.WriteBof(bof.SubstreamType, bof.Build, bof.Year);
                break;
            }

            case BiffRecordType.Eof:
                writer.WriteEof();
                break;

            case BiffRecordType.CodePage:
                writer.WriteCodePage(reader.GetCodePage().RawValue);
                break;

            case BiffRecordType.DateMode:
                writer.WriteDateMode(reader.GetDateMode().Is1904);
                break;

            case BiffRecordType.BoundSheet:
            {
                BiffBoundSheetRecord sheet = reader.GetBoundSheet();
                writer.WriteBoundSheet(sheet.StreamOffset, sheet.State, sheet.SheetType, sheet.Name.GetString());
                break;
            }

            case BiffRecordType.Dimensions:
                writer.WriteDimensions(reader.GetDimensions());
                break;

            case BiffRecordType.Row:
                writer.WriteRow(reader.GetRow());
                break;

            case BiffRecordType.Number:
            {
                BiffNumberRecord number = reader.GetNumber();
                writer.WriteNumber(number.Row, number.Column, number.XfIndex, number.Value);
                break;
            }

            case BiffRecordType.Rk:
            {
                BiffRkRecord rk = reader.GetRk();
                writer.WriteRk(rk.Row, rk.Column, rk.XfIndex, rk.RawValue);
                break;
            }

            case BiffRecordType.MulRk:
            {
                BiffMulRkRecord cells = reader.GetMulRk();
                var values = new BiffRkCell[cells.Count];
                for (var i = 0; i < values.Length; i++)
                    values[i] = cells[i];

                writer.WriteMulRk(cells.Row, cells.FirstColumn, values);
                break;
            }

            case BiffRecordType.Blank:
            {
                BiffBlankRecord blank = reader.GetBlank();
                writer.WriteBlank(blank.Row, blank.Column, blank.XfIndex);
                break;
            }

            case BiffRecordType.MulBlank:
            {
                BiffMulBlankRecord blanks = reader.GetMulBlank();
                var formats = new ushort[blanks.Count];
                for (var i = 0; i < formats.Length; i++)
                    formats[i] = blanks[i];

                writer.WriteMulBlank(blanks.Row, blanks.FirstColumn, formats);
                break;
            }

            case BiffRecordType.BoolErr:
            {
                BiffBoolErrRecord value = reader.GetBoolErr();
                writer.WriteBoolErr(value.Row, value.Column, value.XfIndex, value.RawValue, value.IsError);
                break;
            }

            case BiffRecordType.Label:
            {
                BiffLabelRecord label = reader.GetLabel();
                writer.WriteLabel(label.Row, label.Column, label.XfIndex, label.Text.GetString());
                break;
            }

            case BiffRecordType.LabelSst:
            {
                BiffLabelSstRecord label = reader.GetLabelSst();
                writer.WriteLabelSst(label.Row, label.Column, label.XfIndex, label.SstIndex);
                break;
            }

            case BiffRecordType.Formula:
            {
                BiffFormulaRecord formula = reader.GetFormula();
                if (formula.CachedResultKind == BiffCachedResultKind.Number)
                    writer.WriteFormula(formula.Row, formula.Column, formula.XfIndex, formula.NumberValue, formula.Tokens, formula.Flags);
                else
                    writer.WriteFormula(formula.Row, formula.Column, formula.XfIndex, formula.CachedResultKind, formula.RawValue, formula.Tokens, formula.Flags);

                break;
            }

            case BiffRecordType.String:
                writer.WriteString(reader.GetString().Text.GetString());
                break;

            case BiffRecordType.Xf:
                writer.WriteXf(reader.GetXf());
                break;

            case BiffRecordType.Format:
            {
                BiffFormatRecord format = reader.GetFormat();
                writer.WriteFormat(format.FormatIndex, format.Code.GetString());
                break;
            }

            case BiffRecordType.Font:
            {
                BiffFontRecord font = reader.GetFont();
                writer.WriteFont(
                    font.Height,
                    font.Attributes,
                    font.ColorIndex,
                    font.Weight,
                    font.Escapement,
                    font.Underline,
                    font.Family,
                    font.CharacterSet,
                    font.Name.GetString());
                break;
            }

            default:
                writer.WriteRecord(reader.RecordId, reader.ValueSpan);
                break;
        }
    }

    /// <summary>
    /// Names a record identifier.
    /// </summary>
    /// <param name="id">The record identifier.</param>
    /// <returns>The <see cref="BiffRecordType" /> name, or <c>0xNNNN</c> for an identifier it does not name.</returns>
    private static string NameRecord(ushort id) =>
        Enum.IsDefined((BiffRecordType)id) ? ((BiffRecordType)id).ToString() : string.Create(CultureInfo.InvariantCulture, $"0x{id:X4}");

    /// <summary>
    /// Names a BOF substream type.
    /// </summary>
    /// <param name="type">The substream type.</param>
    /// <returns>The <see cref="BiffSubstreamType" /> name, or <c>0xNNNN</c> for a value it does not name.</returns>
    private static string NameSubstream(BiffSubstreamType type) =>
        Enum.IsDefined(type) ? type.ToString() : string.Create(CultureInfo.InvariantCulture, $"0x{(ushort)type:X4}");

    /// <summary>
    /// Names a BOUNDSHEET sheet type.
    /// </summary>
    /// <param name="type">The sheet type.</param>
    /// <returns>The <see cref="BiffSheetType" /> name, or <c>0xNN</c> for a value it does not name.</returns>
    private static string NameSheetType(BiffSheetType type) =>
        Enum.IsDefined(type) ? type.ToString() : FormatByte((byte)type);

    /// <summary>
    /// Formats a cell position and XF index for a Records rendering line.
    /// </summary>
    /// <param name="row">The zero-based row.</param>
    /// <param name="column">The zero-based column.</param>
    /// <param name="xfIndex">The XF index.</param>
    /// <returns>The row, column and XF index, tab-separated.</returns>
    private static string FormatCell(int row, int column, ushort xfIndex) =>
        string.Create(CultureInfo.InvariantCulture, $"{row}\t{column}\t{xfIndex}");

    /// <summary>
    /// Formats an integer in invariant decimal form.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The text.</returns>
    private static string FormatInteger(long value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a byte as <c>0xHH</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The text, with two upper-case hexadecimal digits.</returns>
    private static string FormatByte(byte value) =>
        string.Create(CultureInfo.InvariantCulture, $"0x{value:X2}");

    /// <summary>
    /// Formats a number in its shortest round-trippable invariant form.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The text, such as <c>1.5</c>, <c>-0</c>, <c>NaN</c> or <c>Infinity</c>.</returns>
    private static string FormatNumber(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats an error code.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <returns>The <see cref="ExcelErrorCode" /> name, or <c>0xHH</c> for a value it does not name.</returns>
    private static string FormatError(ExcelErrorCode code) =>
        Enum.IsDefined(code) ? code.ToString() : FormatByte((byte)code);

    /// <summary>
    /// Escapes a value inside a rendering line.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The value with a backslash, a tab, a line feed and a carriage return written <c>\\</c>, <c>\t</c>, <c>\n</c> and
    /// <c>\r</c>, and an unpaired surrogate written <c>\u{XXXX}</c>; every other character stays as it is.
    /// </returns>
    private static string EscapeValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                builder.Append(c).Append(value[++i]);
                continue;
            }

            switch (c)
            {
                case '\\':
                    builder.Append(@"\\");
                    break;

                case '\t':
                    builder.Append(@"\t");
                    break;

                case '\n':
                    builder.Append(@"\n");
                    break;

                case '\r':
                    builder.Append(@"\r");
                    break;

                default:
                    if (char.IsSurrogate(c))
                        builder.Append(@"\u{").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)).Append('}');
                    else
                        builder.Append(c);

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes a rendering in the catalog's escaped notation, for a failure message.
    /// </summary>
    /// <param name="rendering">The rendering.</param>
    /// <returns>
    /// The UTF-8 bytes of <paramref name="rendering" />, escaped as <see cref="CorpusEscapes.Encode" /> writes them.
    /// </returns>
    private static string EncodeRendering(string rendering) =>
        CorpusEscapes.Encode(Encoding.UTF8.GetBytes(rendering));

    /// <summary>
    /// Asserts that rendering a generated input throws exactly the exception a <c>reject</c> row names.
    /// </summary>
    /// <typeparam name="TException">The exception the row names.</typeparam>
    /// <param name="fix">The catalog row.</param>
    /// <param name="input">The generated input.</param>
    /// <param name="options">The row options.</param>
    private static void AssertRejects<TException>(ReleaseNoteFix fix, GeneratedInput input, RowOptions options)
        where TException : Exception
    {
        string? rendering = null;
        _ = Assert.ThrowsExactly<TException>(
            () =>
            {
                rendering = Render(input, options);
            },
            exception => exception is null ? $"{fix} rendered {EncodeRendering(rendering ?? string.Empty)}." : $"{fix} threw {Describe(exception)}");
    }

    /// <summary>
    /// Describes an exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Its type name and message, followed by those of its inner exception when it has one.</returns>
    private static string Describe(Exception exception) =>
        exception.InnerException is { } inner
            ? $"{exception.GetType().Name}: {exception.Message} (inner {inner.GetType().Name}: {inner.Message})"
            : $"{exception.GetType().Name}: {exception.Message}";
}
