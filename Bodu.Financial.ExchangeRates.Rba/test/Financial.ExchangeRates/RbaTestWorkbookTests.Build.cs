// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestWorkbookTests.Build.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Formats.Excel;
using Bodu.IO.Biff;
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaTestWorkbookTests
{
    /// <summary>
    /// Verifies that an era's workbook is a version 3 compound file holding the workbook stream and both property-set
    /// streams, whose BIFF8 globals declare code page 1200 and the 1900 date system and name a data sheet and a notes
    /// sheet, the notes sheet with the trailing space the 2007-2009 workbook gives it.
    /// </summary>
    /// <param name="label">The era label.</param>
    [TestMethod]
    [DynamicData(nameof(Eras))]
    public void Build_WhenEra_ShouldWriteF11Container(string label)
    {
        byte[] file = RbaTestWorkbook.Build(GetEra(label));

        using CompoundFile compound = CompoundFile.Open(new MemoryStream(file, writable: false));
        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(new MemoryStream(file, writable: false));

        Assert.AreEqual(3, BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(0x1A)), "compound file major version");
        CollectionAssert.AreEquivalent(
            new[] { RbaTestWorkbook.WorkbookStreamName, SummaryInformation.StreamName, DocumentSummaryInformation.StreamName },
            compound.RootStorage.EnumerateStreams().Select(static entry => entry.Name).ToArray());
        Assert.AreEqual(1200, ReadCodePage(ReadWorkbookStream(file)));
        Assert.AreEqual(BiffVersion.Biff8, workbook.BiffVersion);
        Assert.AreEqual(ExcelDateSystem.Excel1900, workbook.DateSystem);
        CollectionAssert.AreEqual(
            new[] { "Data", label == "2007-2009" ? "Notes " : "Notes" },
            workbook.Worksheets.Select(static sheet => sheet.Name).ToArray());
    }

    /// <summary>
    /// Verifies that an era's data sheet encodes its cells as Excel encodes the RBA's: text through the shared string
    /// table, numbers as <c>NUMBER</c>, <c>RK</c> and <c>MULRK</c> records with dates both alone and leading a run from
    /// column 0, blanks alone and in runs, title and description formulas in 2010-2013 alone, and a
    /// <c>DIMENSIONS</c> range wider than the cells it holds.
    /// </summary>
    /// <param name="label">The era label.</param>
    [TestMethod]
    [DynamicData(nameof(Eras))]
    public void Build_WhenEra_ShouldEncodeCellsAsExcelDoes(string label)
    {
        RbaTestSheet sheet = RbaTestWorkbook.CreateSheet(GetEra(label));
        byte[] file = RbaTestWorkbook.Build(sheet);
        List<(BiffRecordType Type, int Row, int Column)> records = ReadRecords(ReadWorkbookStream(file), DataSubstream);

        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(new MemoryStream(file, writable: false));
        ExcelWorksheetDimensions dimensions = workbook.Worksheets[0].Dimensions;

        Assert.Contains(static record => record.Type == BiffRecordType.LabelSst, records);
        Assert.Contains(static record => record.Type == BiffRecordType.Number && record.Row >= RbaTestSheet.FirstDataRow, records);
        Assert.Contains(static record => record.Type == BiffRecordType.Rk && record.Column == 0, records);
        Assert.Contains(static record => record.Type == BiffRecordType.MulRk && record.Column == 0, records);
        Assert.Contains(static record => record.Type == BiffRecordType.MulRk && record.Column > 0 && record.Row >= RbaTestSheet.FirstDataRow, records);
        Assert.Contains(static record => record.Type == BiffRecordType.Blank && record.Row >= RbaTestSheet.FirstDataRow, records);
        Assert.Contains(static record => record.Type == BiffRecordType.MulBlank, records);
        Assert.AreEqual(label == "2010-2013" ? 2 * sheet.Columns.Count : 0, records.Count(static record => record.Type == BiffRecordType.Formula));
        Assert.AreEqual(records.Count(static record => record.Type == BiffRecordType.Formula), records.Count(static record => record.Type == BiffRecordType.String));
        Assert.IsGreaterThan(RbaTestSheet.FirstDataRow + sheet.Dates.Count, dimensions.RowCount);
        Assert.IsGreaterThan(sheet.Columns.Count + 1, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that the generator is deterministic: building every era's workbook twice writes the same bytes.
    /// </summary>
    [TestMethod]
    public void Build_WhenCalledTwice_ShouldWriteIdenticalBytes()
    {
        foreach (RbaEraWorkbook era in RbaEraWorkbook.Default)
            CollectionAssert.AreEqual(RbaTestWorkbook.Build(era), RbaTestWorkbook.Build(era), era.Label);
    }
}
