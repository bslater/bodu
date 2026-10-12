// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.IO.Biff;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.Formats.Excel;

/// <summary>
/// Builds the sample BIFF8 workbook the <c>Bodu.Formats.Excel</c> test suite opens.
/// </summary>
/// <remarks>
/// <para>
/// The tests build the sample rather than commit a real workbook, so that every byte of it has a known origin: its
/// records come from <see cref="BiffWriter" />, its container from <see cref="CompoundStorageBuilder" />.
/// </para>
/// <para>
/// The workbook has two visible worksheets. <c>Data</c> declares the used range A1:E111. A1 holds an inline title;
/// row 2 names the series and row 11 identifies them with shared strings, and row 6 gives their units with inline
/// labels. Rows 12 to 111 hold a value of each series a day from 2024-01-01, the date in column A formatted as a date:
/// the first data row's values are <c>NUMBER</c> records, the last data row is one <c>MULRK</c> run, and the rows
/// between are <c>RK</c> records. <c>Notes</c> declares A1:B3 and holds text, a Boolean and an error value. The
/// workbook uses the 1900 date system and carries summary and document summary information.
/// </para>
/// </remarks>
internal static class ExcelBinaryFixtures
{
    /// <summary>The zero-based row of the <c>Data</c> sheet that identifies the series.</summary>
    public const int SeriesIdRow = 10;

    /// <summary>The zero-based row of the <c>Data</c> sheet that holds the first data row.</summary>
    public const int FirstDataRow = 11;

    /// <summary>The number of data rows of the <c>Data</c> sheet.</summary>
    public const int DataRowCount = 100;

    /// <summary>The zero-based row of the <c>Data</c> sheet that holds the last data row.</summary>
    public const int LastDataRow = FirstDataRow + DataRowCount - 1;

    /// <summary>The serial date of the first data row, 2024-01-01 in the 1900 date system.</summary>
    public const int FirstSerialDate = 45292;

    /// <summary>The code of the custom number format of the date column.</summary>
    public const string DateFormatCode = "yyyy-mm-dd";

    /// <summary>The title of the workbook's summary information.</summary>
    public const string Title = "Sample workbook";

    /// <summary>The index of the custom number format of the date column.</summary>
    private const ushort DateFormatIndex = 164;

    /// <summary>The index of the extended format of the text cells, the general format.</summary>
    private const ushort GeneralXf = 0;

    /// <summary>The index of the extended format of the date column, <see cref="DateFormatCode" />.</summary>
    private const ushort DateXf = 1;

    /// <summary>The index of the extended format of the value columns, the built-in format <c>0.00</c>.</summary>
    private const ushort ValueXf = 2;

    /// <summary>Writes records through a writer passed by reference.</summary>
    /// <param name="writer">The writer.</param>
    private delegate void WriteAction(ref BiffWriter writer);

    /// <summary>
    /// Gets the values of the first data row in columns B to E, none of which an RK value represents.
    /// </summary>
    /// <value>Four values the sample writes as <c>NUMBER</c> records.</value>
    public static IReadOnlyList<double> FirstRowValues { get; } = [1.2345678, 2.3456789, 3.4567891, 4.5678912];

    /// <summary>
    /// Gets the values of the last data row in columns B to E, which the sample writes in one <c>MULRK</c> run.
    /// </summary>
    /// <value>Four values with exact RK forms.</value>
    public static IReadOnlyList<double> LastRowValues { get; } = [0.75, 1.5, 2.25, 112.71];

    /// <summary>
    /// Opens the sample workbook as a seekable, read-only stream.
    /// </summary>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the sample.</returns>
    public static MemoryStream OpenSample() =>
        new(BuildSample(), writable: false);

    /// <summary>
    /// Builds the sample workbook.
    /// </summary>
    /// <returns>The bytes of the <c>.xls</c> compound file.</returns>
    public static byte[] BuildSample()
    {
        string[] strings = ["Title", "Alpha", "Beta", "Gamma", "Delta", "Units", "Series ID", "SER-A", "SER-B", "SER-C", "SER-D", "Source"];
        byte[] globals = Emit((ref BiffWriter w) =>
        {
            w.WriteCodePage(1200);
            w.WriteDateMode(false);
            w.WriteFormat(DateFormatIndex, DateFormatCode);
            w.WriteXf(new BiffXfRecord(0, 0, 0));
            w.WriteXf(new BiffXfRecord(0, DateFormatIndex, 0));
            w.WriteXf(new BiffXfRecord(0, 2, 0));
            w.WriteSst(strings);
        });

        byte[] workbook = Biff8TestWorkbook.BuildWorkbookStream(
            [globals],
            new Biff8TestWorkbook.SheetSpec("Data", 0, 0, [Emit(WriteDataSheet)]),
            new Biff8TestWorkbook.SheetSpec("Notes", 0, 0, [Emit(WriteNotesSheet)]));

        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Workbook", workbook);
        _ = root.AddStream(SummaryInformation.StreamName, new SummaryInformationBuilder { Title = Title, Author = "Bodu", ApplicationName = "Bodu tests" }.ToArray());
        _ = root.AddStream(DocumentSummaryInformation.StreamName, new DocumentSummaryInformationBuilder { Company = "Bodu" }.ToArray());
        return root.ToArray();
    }

    /// <summary>
    /// Writes the records of the <c>Data</c> sheet between its <c>BOF</c> and <c>EOF</c>.
    /// </summary>
    /// <param name="w">The writer.</param>
    private static void WriteDataSheet(ref BiffWriter w)
    {
        // Cells are written row by row, each row's from left to right, as Excel writes them.
        w.WriteDimensions(new BiffDimensionsRecord(0, LastDataRow + 1, 0, 5));
        w.WriteLabel(0, 0, GeneralXf, "Sample daily values");
        w.WriteLabelSst(1, 0, GeneralXf, 0);
        for (int column = 1; column <= 4; column++)
            w.WriteLabelSst(1, column, GeneralXf, (uint)column);

        w.WriteLabelSst(5, 0, GeneralXf, 5);
        for (int column = 1; column <= 4; column++)
            w.WriteLabel(5, column, GeneralXf, "Points");

        w.WriteLabelSst(SeriesIdRow, 0, GeneralXf, 6);
        for (int column = 1; column <= 4; column++)
            w.WriteLabelSst(SeriesIdRow, column, GeneralXf, (uint)(6 + column));

        for (int i = 0; i < DataRowCount; i++)
        {
            int row = FirstDataRow + i;
            if (row == LastDataRow)
            {
                w.WriteMulRk(row, 0, [new BiffRkCell(DateXf, Rk(FirstSerialDate + i)), .. LastRowValues.Select(static value => new BiffRkCell(ValueXf, Rk(value)))]);
                continue;
            }

            w.WriteRk(row, 0, DateXf, Rk(FirstSerialDate + i));
            for (int column = 1; column <= 4; column++)
            {
                if (row == FirstDataRow)
                    w.WriteNumber(row, column, ValueXf, FirstRowValues[column - 1]);
                else
                    w.WriteRk(row, column, ValueXf, Rk(column + (i * 0.25)));
            }
        }
    }

    /// <summary>
    /// Writes the records of the <c>Notes</c> sheet between its <c>BOF</c> and <c>EOF</c>.
    /// </summary>
    /// <param name="w">The writer.</param>
    private static void WriteNotesSheet(ref BiffWriter w)
    {
        w.WriteDimensions(new BiffDimensionsRecord(0, 3, 0, 2));
        w.WriteLabel(0, 0, GeneralXf, "Notes");
        w.WriteLabelSst(1, 0, GeneralXf, 11);
        w.WriteLabel(1, 1, GeneralXf, "Built by the tests");
        w.WriteBoolean(2, 0, GeneralXf, true);
        w.WriteError(2, 1, GeneralXf, 0x2A);
    }

    /// <summary>
    /// Returns the RK value of a number that has one.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns>The RK value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="value" /> has no exact RK form.</exception>
    private static uint Rk(double value) =>
        BiffRk.TryEncode(value, out uint rk) ? rk : throw new InvalidOperationException($"{value} has no RK form.");

    /// <summary>
    /// Runs a writer callback against a fresh BIFF8 writer and returns the records it wrote.
    /// </summary>
    /// <param name="write">The callback.</param>
    /// <returns>The records.</returns>
    private static byte[] Emit(WriteAction write)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);
        write(ref writer);
        return output.WrittenSpan.ToArray();
    }
}
