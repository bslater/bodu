// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SampleWorkbook.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using Bodu.IO.Biff;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.Formats.Excel.Binary.Samples.ExcelReading;

/// <summary>
/// Builds the workbook the scenarios read: a BIFF8 <c>.xls</c> file whose records are written by
/// <see cref="BiffWriter" /> and whose container is written by <see cref="CompoundStorageBuilder" />, so the sample
/// carries no data file and every value in it is synthetic.
/// </summary>
/// <remarks>
/// The <c>Data</c> sheet is a table of daily readings from four stations: a title in A1, the station names (row 2),
/// their units (row 3) and series identifiers (row 4), then one row a day from 2024-01-01 for 400 days, each holding
/// the date in column A, formatted as a date, a reading per station in columns B to E, and a remark in column F. The
/// <c>Notes</c> sheet holds text, a Boolean, a spreadsheet error, and a formula with its cached result. The globals
/// carry a few records Excel writes that the codec does not name, and the remarks make the shared string table long
/// enough to continue across several records.
/// </remarks>
internal static class SampleWorkbook
{
    /// <summary>The number of daily rows of the <c>Data</c> sheet.</summary>
    private const int Days = 400;

    /// <summary>The zero-based row of the <c>Data</c> sheet that holds the first day.</summary>
    private const int FirstDayRow = 4;

    /// <summary>The serial date of the first day, 2024-01-01 in the 1900 date system.</summary>
    private const int FirstSerialDate = 45292;

    /// <summary>The index of the custom number format of the date column.</summary>
    private const ushort DateFormatIndex = 164;

    /// <summary>The extended format of text cells: the general format.</summary>
    private const ushort GeneralXf = 0;

    /// <summary>The extended format of the date column: the custom format <c>yyyy-mm-dd</c>.</summary>
    private const ushort DateXf = 1;

    /// <summary>The extended format of the readings: the built-in format <c>0.00</c>.</summary>
    private const ushort ReadingXf = 2;

    /// <summary>The names of the four stations.</summary>
    private static readonly string[] s_stations = ["North", "South", "East", "West"];

    /// <summary>Writes records through a writer passed by reference.</summary>
    /// <param name="writer">The writer.</param>
    private delegate void WriteAction(ref BiffWriter writer);

    /// <summary>
    /// Writes the workbook to a file.
    /// </summary>
    /// <param name="path">The path of the file to write.</param>
    public static void Write(string path) =>
        File.WriteAllBytes(path, Build());

    /// <summary>
    /// Builds the workbook.
    /// </summary>
    /// <returns>The bytes of the <c>.xls</c> compound file.</returns>
    public static byte[] Build()
    {
        // The shared strings: the header labels, the station names and identifiers, then one remark a day.
        List<string> strings = ["Station", "Units", "Series ID", "Remark", .. s_stations, .. s_stations.Select(station => "RD-" + station[0])];
        int firstRemark = strings.Count;
        for (int day = 0; day < Days; day++)
        {
            string date = new DateOnly(2024, 1, 1).AddDays(day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            strings.Add($"Reading {day + 1:D3} of {Days}, taken {date} at all four stations");
        }

        byte[] data = Emit((ref BiffWriter w) =>
        {
            w.WriteBof(BiffSubstreamType.Worksheet);
            WriteDataSheet(ref w, firstRemark);
            w.WriteEof();
        });
        byte[] notes = Emit(static (ref BiffWriter w) =>
        {
            w.WriteBof(BiffSubstreamType.Worksheet);
            WriteNotesSheet(ref w);
            w.WriteEof();
        });

        // The sheet directory in the globals records where each sheet's BOF is, after the globals, so the globals are
        // written once to measure them and again with the offsets; a BOUNDSHEET's length does not depend on its offset.
        uint[] offsets = new uint[2];
        byte[] globals = Emit((ref BiffWriter w) => WriteGlobals(ref w, strings, offsets));
        offsets[0] = (uint)globals.Length;
        offsets[1] = offsets[0] + (uint)data.Length;
        globals = Emit((ref BiffWriter w) => WriteGlobals(ref w, strings, offsets));

        byte[] workbook = [.. globals, .. data, .. notes];
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Workbook", workbook);
        _ = root.AddStream(SummaryInformation.StreamName, new SummaryInformationBuilder { Title = "Daily readings", Author = "Bodu samples", ApplicationName = "Bodu sample generator" }.ToArray());
        _ = root.AddStream(DocumentSummaryInformation.StreamName, new DocumentSummaryInformationBuilder { Company = "Bodu" }.ToArray());
        return root.ToArray();
    }

    /// <summary>
    /// Writes the workbook globals substream.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="strings">The shared strings.</param>
    /// <param name="offsets">The offsets of the <c>Data</c> and <c>Notes</c> sheets' <c>BOF</c> records.</param>
    private static void WriteGlobals(ref BiffWriter w, List<string> strings, uint[] offsets)
    {
        w.WriteBof(BiffSubstreamType.WorkbookGlobals);
        w.WriteCodePage(1200);

        // Records Excel writes that the codec has no name for: the sheet tab identifiers, the built-in function group
        // count, and the double-stream flag.
        w.WriteRecord(0x013D, [0x01, 0x00, 0x02, 0x00]);
        w.WriteRecord(0x009C, [0x0E, 0x00]);
        w.WriteRecord(0x0161, [0x00, 0x00]);

        w.WriteDateMode(false);
        w.WriteFont(200, 0, 0x7FFF, 400, 0, 0, 0, 0, "Arial");
        w.WriteFormat(DateFormatIndex, "yyyy-mm-dd");
        w.WriteXf(new BiffXfRecord(0, 0, 0));
        w.WriteXf(new BiffXfRecord(0, DateFormatIndex, 0));
        w.WriteXf(new BiffXfRecord(0, 2, 0));
        w.WriteSst([.. strings]);
        w.WriteBoundSheet(offsets[0], BiffSheetState.Visible, BiffSheetType.Worksheet, "Data");
        w.WriteBoundSheet(offsets[1], BiffSheetState.Visible, BiffSheetType.Worksheet, "Notes");
        w.WriteEof();
    }

    /// <summary>
    /// Writes the cells of the <c>Data</c> sheet.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="firstRemark">The shared string index of the first day's remark.</param>
    private static void WriteDataSheet(ref BiffWriter w, int firstRemark)
    {
        // Cells are written row by row, each row's from left to right, as Excel writes them.
        w.WriteDimensions(new BiffDimensionsRecord(0, FirstDayRow + Days, 0, 6));
        w.WriteLabel(0, 0, GeneralXf, "Daily readings, four stations");
        w.WriteLabelSst(1, 0, GeneralXf, 0);
        for (int station = 0; station < s_stations.Length; station++)
            w.WriteLabelSst(1, station + 1, GeneralXf, (uint)(4 + station));

        w.WriteLabelSst(2, 0, GeneralXf, 1);
        for (int station = 0; station < s_stations.Length; station++)
            w.WriteLabel(2, station + 1, GeneralXf, "mm");

        w.WriteLabelSst(3, 0, GeneralXf, 2);
        for (int station = 0; station < s_stations.Length; station++)
            w.WriteLabelSst(3, station + 1, GeneralXf, (uint)(8 + station));

        w.WriteLabelSst(3, 5, GeneralXf, 3);

        Span<BiffRkCell> readings = stackalloc BiffRkCell[4];
        for (int day = 0; day < Days; day++)
        {
            int row = FirstDayRow + day;
            _ = BiffRk.TryEncode(FirstSerialDate + day, out uint date);
            w.WriteRk(row, 0, DateXf, date);

            // A reading in hundredths of a millimetre has an RK form, so each day's four readings are one MULRK run.
            for (int station = 0; station < readings.Length; station++)
            {
                _ = BiffRk.TryEncode(Reading(day, station), out uint rk);
                readings[station] = new BiffRkCell(ReadingXf, rk);
            }

            w.WriteMulRk(row, 1, readings);
            w.WriteLabelSst(row, 5, GeneralXf, (uint)(firstRemark + day));
        }
    }

    /// <summary>
    /// Writes the cells of the <c>Notes</c> sheet.
    /// </summary>
    /// <param name="w">The writer.</param>
    private static void WriteNotesSheet(ref BiffWriter w)
    {
        w.WriteDimensions(new BiffDimensionsRecord(0, 5, 0, 2));
        w.WriteLabel(0, 0, GeneralXf, "Notes");
        w.WriteLabel(1, 0, GeneralXf, "The readings are synthetic: the sample generates them.");
        w.WriteLabel(2, 0, GeneralXf, "Complete");
        w.WriteBoolean(2, 1, GeneralXf, true);
        w.WriteLabel(3, 0, GeneralXf, "Ratio");
        w.WriteError(3, 1, GeneralXf, 0x07);
        w.WriteLabel(4, 0, GeneralXf, "Days");

        // =400: one ptgInt token, whose result Excel would cache as the number 400.
        w.WriteFormula(4, 1, GeneralXf, Days, [0x1E, Days & 0xFF, Days >> 8]);
    }

    /// <summary>
    /// Returns a station's reading for a day, in millimeters to two decimal places.
    /// </summary>
    /// <param name="day">The zero-based day.</param>
    /// <param name="station">The zero-based station.</param>
    /// <returns>The reading.</returns>
    private static double Reading(int day, int station) =>
        ((1000 * (station + 1)) + (((day * 37) + (station * 11)) % 200)) / 100.0;

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
