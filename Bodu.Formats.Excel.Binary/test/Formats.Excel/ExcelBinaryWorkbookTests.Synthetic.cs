// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Synthetic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a workbook whose globals contain a file-pass record is reported as encrypted.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFilePassPresent_ShouldThrowEncryptedWorkbookException()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.FilePass()],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]));

        _ = Assert.ThrowsExactly<ExcelBinaryEncryptedWorkbookException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }

    /// <summary>
    /// Verifies that the unencrypted sample workbook opens without being reported as encrypted.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenSampleWorkbook_ShouldNotThrowEncryptedWorkbookException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Assert.IsNotNull(workbook);
    }

    /// <summary>
    /// Verifies that the declared visibility and type of every sheet are surfaced, distinguishing hidden from very
    /// hidden and worksheets from chart and macro sheets.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenSheetsVaryInVisibilityAndType_ShouldReportEach()
    {
        byte[][] body = [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)];
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Visible", 0x00, 0x00, body),
            new Biff8TestWorkbook.SheetSpec("Hidden", 0x00, 0x01, body),
            new Biff8TestWorkbook.SheetSpec("VeryHidden", 0x00, 0x02, body),
            new Biff8TestWorkbook.SheetSpec("Chart", 0x02, 0x00, body),
            new Biff8TestWorkbook.SheetSpec("Macro", 0x01, 0x00, body));

        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(ExcelSheetVisibility.Visible, workbook.Worksheets[0].Visibility);
        Assert.AreEqual(ExcelSheetVisibility.Hidden, workbook.Worksheets[1].Visibility);
        Assert.AreEqual(ExcelSheetVisibility.VeryHidden, workbook.Worksheets[2].Visibility);
        Assert.AreEqual(ExcelSheetType.Worksheet, workbook.Worksheets[0].Type);
        Assert.AreEqual(ExcelSheetType.Chart, workbook.Worksheets[3].Type);
        Assert.AreEqual(ExcelSheetType.MacroSheet, workbook.Worksheets[4].Type);
    }

    /// <summary>
    /// Verifies that a workbook declaring the 1904 date system reports it.
    /// </summary>
    [TestMethod]
    public void DateSystem_When1904Declared_ShouldReport1904()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.DateMode(is1904: true)],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]));

        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(ExcelDateSystem.Excel1904, workbook.DateSystem);
    }

    /// <summary>
    /// Verifies that a shared-string cell and an inline-label cell decode to their text values.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenSharedAndInlineStrings_ShouldDecodeBoth()
    {
        byte[][] body =
        [
            Biff8TestWorkbook.LabelSst(0, 0, 1),
            Biff8TestWorkbook.Label(0, 1, "Inline"),
            Biff8TestWorkbook.Number(1, 0, 3.5),
        ];
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Sst("Alpha", "Beta")],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, body));

        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(xls);
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Sheet1");

        Assert.AreEqual("Beta", grid[(0, 0)].StringValue);
        Assert.AreEqual("Inline", grid[(0, 1)].StringValue);
        Assert.AreEqual(3.5, grid[(1, 0)].NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that a compatibility-save workbook carrying both <c>Book</c> and <c>Workbook</c> streams reads the
    /// BIFF8 <c>Workbook</c> stream.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenBothBookAndWorkbookStreams_ShouldReadWorkbookStream()
    {
        byte[] workbookBytes = Biff8TestWorkbook.BuildWorkbookStream(
            [Biff8TestWorkbook.Sst("Only")],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1), Biff8TestWorkbook.LabelSst(0, 0, 0)]));

        MemoryStream container = new();
        using (CompoundFile file = CompoundFile.Create(container, leaveOpen: true))
        {
            file.RootStorage.CreateStream("Book", new byte[] { 0x09, 0x08, 0x00, 0x00 });
            file.RootStorage.CreateStream("Workbook", workbookBytes);
            file.Commit();
        }

        container.Position = 0;
        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(container);
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Sheet1");

        Assert.AreEqual("Only", grid[(0, 0)].StringValue);
    }

    /// <summary>
    /// Verifies that a compound file without a workbook stream throws
    /// <see cref="ExcelBinaryWorkbookStreamNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenNoWorkbookStream_ShouldThrowWorkbookStreamNotFoundException()
    {
        MemoryStream container = new();
        using (CompoundFile file = CompoundFile.Create(container, leaveOpen: true))
        {
            file.RootStorage.CreateStream("NotAWorkbook", new byte[] { 1, 2, 3, 4 });
            file.Commit();
        }

        container.Position = 0;

        _ = Assert.ThrowsExactly<ExcelBinaryWorkbookStreamNotFoundException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(container);
        });
    }

    /// <summary>
    /// Verifies that a workbook whose globals declare a non-BIFF8 version throws
    /// <see cref="ExcelBinaryUnsupportedException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenVersionNotBiff8_ShouldThrowUnsupportedException()
    {
        // Globals BOF declaring BIFF5 (0x0500), followed by EOF.
        byte[] bofPayload = new byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(bofPayload, 0x0500);
        BinaryPrimitives.WriteUInt16LittleEndian(bofPayload.AsSpan(2), Biff8TestWorkbook.BofGlobals);

        byte[] globals = [.. Biff8TestWorkbook.Record(0x0809, bofPayload), .. Biff8TestWorkbook.Eof()];
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(globals, "Workbook");

        _ = Assert.ThrowsExactly<ExcelBinaryUnsupportedException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }
}
