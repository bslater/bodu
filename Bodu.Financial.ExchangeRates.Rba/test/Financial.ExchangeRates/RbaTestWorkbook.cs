// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestWorkbook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.IO.Biff;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Builds BIFF8 workbooks in the layout of the Reserve Bank of Australia's statistical table F11.1, so the RBA tests
/// read workbooks whose every byte has a known origin.
/// </summary>
/// <remarks>
/// <para>
/// A workbook is built from an <see cref="RbaTestSheet" />. Its records come from <see cref="BiffWriter" /> and its
/// container from <see cref="CompoundStorageBuilder" />: a version 3 compound file holding the <c>Workbook</c> stream
/// with summary and document summary information beside it. The globals declare code page 1200 and the 1900 date
/// system, the number formats and extended formats the cells use, the shared string table, and two visible
/// worksheets: the data sheet and a notes sheet.
/// </para>
/// <para>
/// The cells are encoded as Excel encodes the RBA's own files. Text goes through the shared string table, or is a
/// formula's cached result when <see cref="RbaTestSheet.HeaderFormulas" /> is set. A number with an exact RK form is an
/// <c>RK</c> record, or with RK neighbors one <c>MULRK</c> run (a date in column 0 included), and any other number a
/// <c>NUMBER</c> record. Adjacent blank cells form <c>MULBLANK</c> runs. The data sheet's <c>DIMENSIONS</c> record
/// declares a wider range than its cells use, as Excel's record of every formatted cell does.
/// </para>
/// </remarks>
internal static partial class RbaTestWorkbook
{
    /// <summary>The name of the compound-file stream that holds the workbook.</summary>
    public const string WorkbookStreamName = "Workbook";

    /// <summary>The code of the custom number format of the date cells, the format the RBA gives its dates.</summary>
    public const string DateFormatCode = @"dd\-mmm\-yyyy";

    /// <summary>The code page the globals declare, UTF-16, as BIFF8 workbooks record it.</summary>
    private const ushort CodePage = 1200;

    /// <summary>The index of the custom number format of the date cells.</summary>
    private const ushort DateFormatIndex = 164;

    /// <summary>The index of the custom number format that shows one decimal place.</summary>
    private const ushort OneDecimalFormatIndex = 165;

    /// <summary>The index of the custom number format that shows three decimal places.</summary>
    private const ushort ThreeDecimalFormatIndex = 166;

    /// <summary>The index of the custom number format that shows four decimal places.</summary>
    private const ushort FourDecimalFormatIndex = 167;

    /// <summary>The index of the built-in number format <c>0</c>.</summary>
    private const ushort IntegerFormatIndex = 1;

    /// <summary>The index of the built-in number format <c>0.00</c>.</summary>
    private const ushort TwoDecimalFormatIndex = 2;

    /// <summary>The index of the extended format of text cells and of header blanks: the general format.</summary>
    private const ushort GeneralXf = 0;

    /// <summary>The index of the extended format of date cells.</summary>
    private const ushort DateXf = 1;

    /// <summary>
    /// The index of the extended format that shows no decimal places; those showing one to four follow it.
    /// </summary>
    private const ushort FirstValueXf = 2;

    /// <summary>The largest number of decimal places an extended format of the workbook shows.</summary>
    private const int MaxDecimals = 4;

    /// <summary>The number of rows the data sheet's <c>DIMENSIONS</c> record declares past the last data row.</summary>
    private const int DimensionsRowSlack = 64;

    /// <summary>
    /// The number of columns the data sheet's <c>DIMENSIONS</c> record declares past the last value column.
    /// </summary>
    private const int DimensionsColumnSlack = 8;

    /// <summary>The token that opens a constant string in a parsed formula (<c>ptgStr</c>).</summary>
    private const byte StringToken = 0x17;

    /// <summary>The text of the notes sheet that says where the workbook comes from.</summary>
    private const string OriginNote =
        "Synthetic data: the Bodu tests generate this workbook in the layout of the Reserve Bank of Australia's "
        + "statistical table F11.1, and none of its rates is a rate the RBA published.";

    /// <summary>
    /// The zero-based rows of the data sheet that are blank between the units row and the source row.
    /// </summary>
    private static readonly int[] s_blankRows = [6, 7];

    /// <summary>Writes records through a writer passed by reference.</summary>
    /// <param name="writer">The writer.</param>
    private delegate void WriteAction(ref BiffWriter writer);

    /// <summary>
    /// Builds the workbook a sheet describes.
    /// </summary>
    /// <param name="sheet">The content of the workbook.</param>
    /// <returns>The bytes of the <c>.xls</c> compound file.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sheet" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a column of <paramref name="sheet" /> does not have one cell per date, or
    /// <see cref="RbaTestSheet.DateSerials" /> does not have one serial per date.
    /// </exception>
    public static byte[] Build(RbaTestSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (sheet.Columns.Any(column => column.Cells.Count != sheet.Dates.Count)) throw new ArgumentException("Every column must have one cell per date.", nameof(sheet));
        if (sheet.DateSerials is { } serials && serials.Count != sheet.Dates.Count) throw new ArgumentException("The date serials must have one serial per date.", nameof(sheet));

        Dictionary<string, uint> sharedStrings = new(StringComparer.Ordinal);
        byte[] data = Emit((ref BiffWriter w) => WriteDataSheet(ref w, sheet, sharedStrings));
        byte[] notes = Emit((ref BiffWriter w) => WriteNotesSheet(ref w, sheet, sharedStrings));
        string[] strings = [.. sharedStrings.OrderBy(static entry => entry.Value).Select(static entry => entry.Key)];

        // Each BOUNDSHEET record holds the offset of its sheet, which follows the globals. Its length does not depend on
        // the offset, so the globals are measured once with zero offsets and then written with the real ones.
        int globalsLength = EmitGlobals(sheet, strings, 0, 0).Length;
        byte[] globals = EmitGlobals(sheet, strings, (uint)globalsLength, (uint)(globalsLength + data.Length));
        byte[] workbook = [.. globals, .. data, .. notes];

        CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream(WorkbookStreamName, workbook);
        _ = root.AddStream(SummaryInformation.StreamName, new SummaryInformationBuilder { Title = "F11.1 Exchange Rates (synthetic)", Author = "Bodu", ApplicationName = "Bodu tests" }.ToArray());
        _ = root.AddStream(DocumentSummaryInformation.StreamName, new DocumentSummaryInformationBuilder { Company = "Bodu" }.ToArray());
        return root.ToArray();
    }

    /// <summary>
    /// Writes the workbook globals: the code page, the date system, the formats, the shared string table and the sheet
    /// directory.
    /// </summary>
    /// <param name="sheet">The content of the workbook, which names its sheets.</param>
    /// <param name="strings">The shared strings, in index order.</param>
    /// <param name="dataOffset">The offset of the data sheet's <c>BOF</c> record in the workbook stream.</param>
    /// <param name="notesOffset">The offset of the notes sheet's <c>BOF</c> record in the workbook stream.</param>
    /// <returns>The globals substream.</returns>
    private static byte[] EmitGlobals(RbaTestSheet sheet, string[] strings, uint dataOffset, uint notesOffset) =>
        Emit((ref BiffWriter w) =>
        {
            w.WriteBof(BiffSubstreamType.WorkbookGlobals);
            w.WriteCodePage(CodePage);
            w.WriteDateMode(is1904: false);
            w.WriteFormat(DateFormatIndex, DateFormatCode);
            w.WriteFormat(OneDecimalFormatIndex, "0.0");
            w.WriteFormat(ThreeDecimalFormatIndex, "0.000");
            w.WriteFormat(FourDecimalFormatIndex, "0.0000");

            // The extended formats in index order: general, dates, then numbers with zero to four decimal places.
            w.WriteXf(new BiffXfRecord(0, 0, 0));
            w.WriteXf(new BiffXfRecord(0, DateFormatIndex, 0));
            w.WriteXf(new BiffXfRecord(0, IntegerFormatIndex, 0));
            w.WriteXf(new BiffXfRecord(0, OneDecimalFormatIndex, 0));
            w.WriteXf(new BiffXfRecord(0, TwoDecimalFormatIndex, 0));
            w.WriteXf(new BiffXfRecord(0, ThreeDecimalFormatIndex, 0));
            w.WriteXf(new BiffXfRecord(0, FourDecimalFormatIndex, 0));

            w.WriteSst(strings);
            w.WriteBoundSheet(dataOffset, BiffSheetState.Visible, BiffSheetType.Worksheet, sheet.Name);
            w.WriteBoundSheet(notesOffset, BiffSheetState.Visible, BiffSheetType.Worksheet, sheet.NotesName);
            w.WriteEof();
        });

    /// <summary>
    /// Writes the data sheet: the banner, the header rows, the two blank rows and the dated rows.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="sheet">The content of the sheet.</param>
    /// <param name="sharedStrings">The shared string table, which gains each text the sheet uses.</param>
    private static void WriteDataSheet(ref BiffWriter w, RbaTestSheet sheet, Dictionary<string, uint> sharedStrings)
    {
        int width = sheet.Columns.Count + 1;
        int lastRow = RbaTestSheet.FirstDataRow + sheet.Dates.Count - 1;
        RbaTestCell[] cells = new RbaTestCell[width];
        ushort[] xfs = new ushort[width];

        w.WriteBof(BiffSubstreamType.Worksheet);
        w.WriteDimensions(new BiffDimensionsRecord(0, lastRow + 1 + DimensionsRowSlack, 0, width + DimensionsColumnSlack));

        if (sheet.Banner is not null)
        {
            Array.Fill(cells, RbaTestCell.Blank);
            Array.Fill(xfs, GeneralXf);
            cells[0] = RbaTestCell.FromText(sheet.Banner);
            WriteCells(ref w, 0, 0, cells, xfs, sharedStrings, textAsFormulas: false);
        }

        WriteTextRow(ref w, sheet, RbaTestHeaderRows.Title, 1, "Title", static column => column.Title, sheet.HeaderFormulas, sharedStrings);
        WriteTextRow(ref w, sheet, RbaTestHeaderRows.Description, 2, "Description", static column => column.Description, sheet.HeaderFormulas, sharedStrings);
        WriteTextRow(ref w, sheet, RbaTestHeaderRows.Frequency, 3, "Frequency", static column => column.Frequency, textAsFormulas: false, sharedStrings);
        WriteTextRow(ref w, sheet, RbaTestHeaderRows.Type, 4, "Type", static column => column.Type, textAsFormulas: false, sharedStrings);
        WriteTextRow(ref w, sheet, RbaTestHeaderRows.Units, 5, "Units", static column => column.Units, textAsFormulas: false, sharedStrings);

        Array.Fill(cells, RbaTestCell.Blank);
        Array.Fill(xfs, GeneralXf);
        foreach (int row in s_blankRows)
            WriteCells(ref w, row, 0, cells, xfs, sharedStrings, textAsFormulas: false);

        WriteTextRow(ref w, sheet, RbaTestHeaderRows.Source, 8, "Source", static column => column.Source, textAsFormulas: false, sharedStrings);
        if ((sheet.HeaderRows & RbaTestHeaderRows.PublicationDate) != 0)
        {
            RbaTestCell[] dates = [.. sheet.Columns.Select(static column => column.PublicationDate is { } date ? RbaTestCell.FromSerial(RbaTestSheet.ToSerial(date)) : RbaTestCell.Blank)];
            ushort[] dateXfs = [.. dates.Select(static cell => cell.Kind == RbaTestCellKind.Blank ? GeneralXf : DateXf)];
            w.WriteLabelSst(9, 0, GeneralXf, Intern("Publication date", sharedStrings));
            WriteCells(ref w, 9, 1, dates, dateXfs, sharedStrings, textAsFormulas: false);
        }

        WriteTextRow(ref w, sheet, RbaTestHeaderRows.SeriesId, 10, "Series ID", static column => column.SeriesId, textAsFormulas: false, sharedStrings);

        for (int i = 0; i < sheet.Dates.Count; i++)
        {
            cells[0] = RbaTestCell.FromSerial(sheet.GetDateSerial(i));
            xfs[0] = DateXf;
            for (int c = 0; c < sheet.Columns.Count; c++)
            {
                RbaTestColumn column = sheet.Columns[c];
                cells[c + 1] = column.Cells[i];
                xfs[c + 1] = column.Cells[i].Kind == RbaTestCellKind.Text ? GeneralXf : ValueXf(column.Decimals);
            }

            WriteCells(ref w, RbaTestSheet.FirstDataRow + i, 0, cells, xfs, sharedStrings, textAsFormulas: false);
        }

        w.WriteEof();
    }

    /// <summary>
    /// Writes a labeled header row of text when the sheet carries it: the label in column 0, then each column's text,
    /// or a blank where a column has none.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="sheet">The content of the sheet.</param>
    /// <param name="header">
    /// The header row, which is written only when <see cref="RbaTestSheet.HeaderRows" /> includes it.
    /// </param>
    /// <param name="row">The zero-based row the header occupies.</param>
    /// <param name="label">The label in column 0.</param>
    /// <param name="select">Selects a column's text for the row.</param>
    /// <param name="textAsFormulas">Whether the columns' text is written as formulas' cached results.</param>
    /// <param name="sharedStrings">The shared string table, which gains each text the row uses.</param>
    private static void WriteTextRow(ref BiffWriter w, RbaTestSheet sheet, RbaTestHeaderRows header, int row, string label, Func<RbaTestColumn, string?> select, bool textAsFormulas, Dictionary<string, uint> sharedStrings)
    {
        if ((sheet.HeaderRows & header) == 0)
            return;

        RbaTestCell[] cells = [.. sheet.Columns.Select(column => select(column) is { } text ? RbaTestCell.FromText(text) : RbaTestCell.Blank)];
        ushort[] xfs = new ushort[cells.Length];
        Array.Fill(xfs, GeneralXf);

        w.WriteLabelSst(row, 0, GeneralXf, Intern(label, sharedStrings));
        WriteCells(ref w, row, 1, cells, xfs, sharedStrings, textAsFormulas);
    }

    /// <summary>
    /// Writes a run of cells of one row in column order, grouping adjacent RK numbers into <c>MULRK</c> runs and
    /// adjacent blanks into <c>MULBLANK</c> runs as Excel does.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="row">The zero-based row.</param>
    /// <param name="firstColumn">The zero-based column of the first cell.</param>
    /// <param name="cells">The cells, in column order.</param>
    /// <param name="xfs">The extended format of each cell.</param>
    /// <param name="sharedStrings">The shared string table, which gains each text the cells use.</param>
    /// <param name="textAsFormulas">
    /// Whether text is written as a formula's cached result rather than a shared string.
    /// </param>
    private static void WriteCells(ref BiffWriter w, int row, int firstColumn, ReadOnlySpan<RbaTestCell> cells, ReadOnlySpan<ushort> xfs, Dictionary<string, uint> sharedStrings, bool textAsFormulas)
    {
        int i = 0;
        while (i < cells.Length)
        {
            RbaTestCell cell = cells[i];
            int column = firstColumn + i;
            int end = i + 1;
            switch (cell.Kind)
            {
                case RbaTestCellKind.Text when textAsFormulas:
                    w.WriteFormula(row, column, xfs[i], BiffCachedResultKind.String, 0, StringFormula(cell.Text!));
                    w.WriteString(cell.Text);
                    break;

                case RbaTestCellKind.Text:
                    w.WriteLabelSst(row, column, xfs[i], Intern(cell.Text!, sharedStrings));
                    break;

                case RbaTestCellKind.Number when BiffRk.TryEncode(cell.StoredValue, out uint rk):
                    while (end < cells.Length && cells[end].Kind == RbaTestCellKind.Number && BiffRk.TryEncode(cells[end].StoredValue, out _))
                        end++;

                    if (end - i == 1)
                    {
                        w.WriteRk(row, column, xfs[i], rk);
                        break;
                    }

                    BiffRkCell[] run = new BiffRkCell[end - i];
                    for (int k = 0; k < run.Length; k++)
                    {
                        _ = BiffRk.TryEncode(cells[i + k].StoredValue, out uint value);
                        run[k] = new BiffRkCell(xfs[i + k], value);
                    }

                    w.WriteMulRk(row, column, run);
                    break;

                case RbaTestCellKind.Number:
                    w.WriteNumber(row, column, xfs[i], cell.StoredValue);
                    break;

                case RbaTestCellKind.Blank:
                    while (end < cells.Length && cells[end].Kind == RbaTestCellKind.Blank)
                        end++;

                    if (end - i == 1)
                        w.WriteBlank(row, column, xfs[i]);
                    else
                        w.WriteMulBlank(row, column, xfs[i..end]);

                    break;
            }

            i = end;
        }
    }

    /// <summary>
    /// Writes the notes sheet, which says the workbook is synthetic and counts what the data sheet holds.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="sheet">The content of the workbook.</param>
    /// <param name="sharedStrings">The shared string table, which gains each text the sheet uses.</param>
    private static void WriteNotesSheet(ref BiffWriter w, RbaTestSheet sheet, Dictionary<string, uint> sharedStrings)
    {
        w.WriteBof(BiffSubstreamType.Worksheet);
        w.WriteDimensions(new BiffDimensionsRecord(0, 6, 0, 2));
        w.WriteLabelSst(0, 0, GeneralXf, Intern("Notes", sharedStrings));
        w.WriteLabelSst(2, 0, GeneralXf, Intern(OriginNote, sharedStrings));
        w.WriteLabelSst(4, 0, GeneralXf, Intern("Observation dates", sharedStrings));
        w.WriteRk(4, 1, GeneralXf, BiffRk.TryEncode(sheet.Dates.Count, out uint rk) ? rk : throw new InvalidOperationException("A row count has an RK form."));
        w.WriteLabelSst(5, 0, GeneralXf, Intern("Value columns", sharedStrings));
        w.WriteNumber(5, 1, GeneralXf, sheet.Columns.Count);
        w.WriteEof();
    }

    /// <summary>
    /// Returns the extended format of number cells that show a number of decimal places.
    /// </summary>
    /// <param name="decimals">The number of decimal places.</param>
    /// <returns>The index of the extended format, which shows at most <see cref="MaxDecimals" /> places.</returns>
    private static ushort ValueXf(int decimals) =>
        (ushort)(FirstValueXf + Math.Clamp(decimals, 0, MaxDecimals));

    /// <summary>
    /// Returns the parsed tokens of a formula that evaluates to a constant string.
    /// </summary>
    /// <param name="text">The string, of at most 255 characters each below U+0100.</param>
    /// <returns>A <c>ptgStr</c> token holding <paramref name="text" /> as 8-bit characters.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text" /> cannot be held as 8-bit characters.
    /// </exception>
    private static byte[] StringFormula(string text)
    {
        if (text.Length > byte.MaxValue || text.Any(static ch => ch > 'ÿ')) throw new ArgumentException("The formula text must fit an 8-bit short string.", nameof(text));

        byte[] tokens = new byte[3 + text.Length];
        tokens[0] = StringToken;
        tokens[1] = (byte)text.Length;
        for (int i = 0; i < text.Length; i++)
            tokens[3 + i] = (byte)text[i];

        return tokens;
    }

    /// <summary>
    /// Returns the index of a text in the shared string table, adding it when it is not there yet.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="sharedStrings">The shared string table.</param>
    /// <returns>The zero-based index of <paramref name="text" /> in the table.</returns>
    private static uint Intern(string text, Dictionary<string, uint> sharedStrings)
    {
        if (!sharedStrings.TryGetValue(text, out uint index))
        {
            index = (uint)sharedStrings.Count;
            sharedStrings.Add(text, index);
        }

        return index;
    }

    /// <summary>
    /// Runs a writer callback against a fresh BIFF8 writer and returns the records it wrote.
    /// </summary>
    /// <param name="write">The callback.</param>
    /// <returns>The records.</returns>
    private static byte[] Emit(WriteAction write)
    {
        ArrayBufferWriter<byte> output = new();
        BiffWriter writer = new(output, BiffVersion.Biff8);
        write(ref writer);
        return output.WrittenSpan.ToArray();
    }
}
