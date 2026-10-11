// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.ReadWorksheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a materialized worksheet supports random access by position.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenByName_ShouldSupportRandomAccess()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet("Data");

        Assert.IsTrue(sheet.TryGetCell(10, 1, out ExcelCell seriesId));
        Assert.AreEqual("SER-A", seriesId.StringValue);
        Assert.IsFalse(sheet.TryGetCell(100000, 100000, out _));
    }

    /// <summary>
    /// Verifies that a materialized worksheet exposes its name, index, and used range.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenByIndex_ShouldExposeSheetMetadata()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet(0);

        Assert.AreEqual("Data", sheet.Name);
        Assert.AreEqual(0, sheet.Index);
        Assert.AreEqual(111, sheet.Dimensions.RowCount);
    }

    /// <summary>
    /// Verifies that a materialized worksheet groups cells into rows in ascending row and column order.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenGroupingRows_ShouldOrderRowsAndCells()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet("Data");
        var rowIndexes = sheet.Rows.Select(r => r.RowIndex).ToList();

        CollectionAssert.AreEqual(rowIndexes.OrderBy(r => r).ToList(), rowIndexes);
        foreach (ExcelRow row in sheet.Rows)
        {
            var columns = row.Cells.Select(c => c.ColumnIndex).ToList();
            CollectionAssert.AreEqual(columns.OrderBy(c => c).ToList(), columns);
            Assert.IsTrue(row.Cells.All(c => c.RowIndex == row.RowIndex));
        }
    }

    /// <summary>
    /// Verifies that requesting an unknown worksheet name throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenNameMissing_ShouldThrowKeyNotFoundException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = workbook.ReadWorksheet("NoSuchSheet");
        });
    }

    /// <summary>
    /// Verifies that requesting a worksheet index outside the range throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = workbook.ReadWorksheet(99);
        });
    }

    /// <summary>
    /// Verifies that reading a sheet whose BOF declares BIFF4 (version <c>0x0400</c>) in a BIFF8 workbook throws
    /// <see cref="ExcelBinaryUnsupportedException" />, with the codec's exception as its inner exception, as the open does
    /// for the workbook globals.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenSheetBofDeclaresUnsupportedVersion_ShouldThrowExcelBinaryUnsupportedException()
    {
        byte[] sheet = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Record(0x0809, [0x00, 0x04, 0x10, 0x00, 0xBB, 0x0D, 0xCC, 0x07]),
            Biff8TestWorkbook.Number(0, 0, 9),
            Biff8TestWorkbook.Eof());
        byte[] globals = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.BoundSheet(0, 0, 0, "Odd"), Biff8TestWorkbook.Eof());
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(globals.AsSpan(20 + 4), (uint)globals.Length);
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile([.. globals, .. sheet], "Workbook");

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        var ex = Assert.ThrowsExactly<ExcelBinaryUnsupportedException>(() =>
        {
            _ = workbook.ReadWorksheet(0);
        });

        Assert.IsInstanceOfType<BiffUnsupportedVersionException>(ex.InnerException);
    }
}
