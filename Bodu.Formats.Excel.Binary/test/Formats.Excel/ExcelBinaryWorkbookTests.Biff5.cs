// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Biff5.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a BIFF5 workbook stored under the <c>Book</c> stream opens and yields every cell kind, decoding
    /// its code-page text through the CODEPAGE record.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenBiff5Workbook_ShouldReadEveryCellKind()
    {
        Assert.IsTrue(BiffRk.TryEncode(12.5, out uint rk));
        using MemoryStream xls = Biff5TestWorkbook.BuildWorkbook(
            1252,
            null,
            ("Feuille", (ref BiffWriter w) =>
            {
                w.WriteDimensions(new BiffDimensionsRecord(0, 3, 0, 4));
                w.WriteLabel(0, 0, 0, "café");
                w.WriteNumber(0, 1, 0, 3.25);
                w.WriteRk(0, 2, 0, rk);
                w.WriteBoolean(1, 0, 0, true);
                w.WriteError(1, 1, 0, 0x2A);
                w.WriteFormula(2, 0, 0, BiffCachedResultKind.String, 0, default);
                w.WriteString("été");
                w.WriteFormula(2, 1, 0, 7.0, default);
            }));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Feuille");

        Assert.AreEqual("café", grid[(0, 0)].StringValue);
        Assert.AreEqual(3.25, grid[(0, 1)].NumberValue);
        Assert.AreEqual(12.5, grid[(0, 2)].NumberValue);
        Assert.IsTrue(grid[(1, 0)].BooleanValue);
        Assert.AreEqual(ExcelErrorCode.NotAvailable, grid[(1, 1)].ErrorValue);
        Assert.AreEqual("été", grid[(2, 0)].StringValue);
        Assert.AreEqual(7.0, grid[(2, 1)].NumberValue);
        Assert.AreEqual(7, grid.Count);
    }

    /// <summary>
    /// Verifies that a BIFF5 workbook's sheet directory and used range decode through the version's layouts.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenBiff5Workbook_ShouldDescribeWorksheets()
    {
        using MemoryStream xls = Biff5TestWorkbook.BuildWorkbook(
            1252,
            null,
            ("Résumé", (ref BiffWriter w) => w.WriteDimensions(new BiffDimensionsRecord(2, 12, 1, 5))),
            ("Second", (ref BiffWriter w) => { }));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(2, workbook.Worksheets.Count);
        Assert.AreEqual("Résumé", workbook.Worksheets[0].Name);
        Assert.AreEqual(2, workbook.Worksheets[0].Dimensions.FirstRowIndex);
        Assert.AreEqual(10, workbook.Worksheets[0].Dimensions.RowCount);
        Assert.AreEqual(1, workbook.Worksheets[0].Dimensions.FirstColumnIndex);
        Assert.AreEqual(4, workbook.Worksheets[0].Dimensions.ColumnCount);
        Assert.AreEqual("Second", workbook.Worksheets[1].Name);
    }

    /// <summary>
    /// Verifies that BIFF5 text is decoded with the code page the CODEPAGE record declares rather than the default.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenBiff5WorkbookDeclaresCodePage_ShouldDecodeTextWithIt()
    {
        using MemoryStream xls = Biff5TestWorkbook.BuildWorkbook(
            437,
            null,
            ("Sheet1", (ref BiffWriter w) => w.WriteLabel(0, 0, 0, "AΘ")));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual("AΘ", ReadCellGrid(workbook, "Sheet1")[(0, 0)].StringValue);
    }

    /// <summary>
    /// Verifies that BIFF5 FORMAT records (8-bit-length codes) and 16-byte XF records feed number-format resolution.
    /// </summary>
    [TestMethod]
    public void GetNumberFormatCode_WhenBiff5Workbook_ShouldResolveCustomFormat()
    {
        using MemoryStream xls = Biff5TestWorkbook.BuildWorkbook(
            1252,
            (ref BiffWriter w) =>
            {
                w.WriteFormat(164, "0.00 €");
                w.WriteXf(new BiffXfRecord(0, 164, 0));
            },
            ("Sheet1", (ref BiffWriter w) => w.WriteNumber(0, 0, 0, 1.5)));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        ExcelCell cell = ReadCellGrid(workbook, "Sheet1")[(0, 0)];

        Assert.AreEqual(164, cell.FormatIndex);
        Assert.AreEqual("0.00 €", workbook.GetNumberFormatCode(cell.FormatIndex));
    }

    /// <summary>
    /// Verifies that a BIFF5 RSTRING (rich-text label) cell surfaces as a text cell.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenBiff5WorkbookHasRString_ShouldReadAsText()
    {
        byte[] rstring = Biff5TestWorkbook.RString(0, 0, "riche", 1252, runCount: 2);
        using MemoryStream xls = Biff5TestWorkbook.BuildWorkbook(
            1252,
            null,
            ("Sheet1", (ref BiffWriter w) => w.WriteRecord(BiffRecordType.RString, rstring.AsSpan(4))));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual("riche", ReadCellGrid(workbook, "Sheet1")[(0, 0)].StringValue);
    }

    /// <summary>
    /// Verifies that a workbook in a BIFF version before BIFF5 is rejected as unsupported, with the version in the
    /// message.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenVersionBeforeBiff5_ShouldThrowUnsupportedException()
    {
        byte[] bofPayload = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bofPayload, 0x0400);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bofPayload.AsSpan(2), Biff8TestWorkbook.BofGlobals);
        byte[] globals = [.. Biff8TestWorkbook.Record(0x0809, bofPayload), .. Biff8TestWorkbook.Eof()];
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(globals, "Book");

        var ex = Assert.ThrowsExactly<ExcelBinaryUnsupportedException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });

        Assert.IsTrue(ex.Message.Contains("0400", StringComparison.OrdinalIgnoreCase));
    }
}
