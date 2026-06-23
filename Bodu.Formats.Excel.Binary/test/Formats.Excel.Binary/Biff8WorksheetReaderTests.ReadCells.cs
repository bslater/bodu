// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorksheetReaderTests.ReadCells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Excel.Binary;

public partial class Biff8WorksheetReaderTests
{
    /// <summary>
    /// Verifies that a formula cell with a numeric cached result is surfaced as a <see cref="ExcelCellKind.Number" />
    /// cell.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenFormulaHasNumericResult_ShouldSurfaceNumberCell()
    {
        byte[] result = new byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(result, 1234.5);

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Formula(2, 3, result));

        ExcelCell cell = grid[(2, 3)];
        Assert.AreEqual(ExcelCellKind.Number, cell.Kind);
        Assert.AreEqual(1234.5, cell.NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that a formula cell with a boolean cached result is surfaced as a <see cref="ExcelCellKind.Boolean" />
    /// cell.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenFormulaHasBooleanResult_ShouldSurfaceBooleanCell()
    {
        byte[] result = [0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Formula(0, 0, result));

        ExcelCell cell = grid[(0, 0)];
        Assert.AreEqual(ExcelCellKind.Boolean, cell.Kind);
        Assert.IsTrue(cell.BooleanValue!.Value);
    }

    /// <summary>
    /// Verifies that a formula cell with an error cached result is surfaced as an <see cref="ExcelCellKind.Error" />
    /// cell carrying the error code.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenFormulaHasErrorResult_ShouldSurfaceErrorCellWithCode()
    {
        byte[] result = [0x02, 0x00, 0x2A, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Formula(1, 1, result));

        ExcelCell cell = grid[(1, 1)];
        Assert.AreEqual(ExcelCellKind.Error, cell.Kind);
        Assert.AreEqual(ExcelErrorCode.NotAvailable, cell.ErrorValue!.Value);
    }

    /// <summary>
    /// Verifies that a formula cell with a string cached result decodes its trailing <c>STRING</c> record into a text
    /// cell.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenFormulaHasStringResult_ShouldSurfaceTextCellFromStringRecord()
    {
        byte[] result = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid =
            ReadGrid(Formula(4, 5, result), CompressedString("Hello"));

        ExcelCell cell = grid[(4, 5)];
        Assert.AreEqual(ExcelCellKind.String, cell.Kind);
        Assert.AreEqual("Hello", cell.StringValue);
    }

    /// <summary>
    /// Verifies that a formula cell whose string result is not followed by a <c>STRING</c> record yields an empty text
    /// cell rather than throwing or consuming a later record.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenFormulaStringResultHasNoStringRecord_ShouldSurfaceEmptyTextCell()
    {
        byte[] result = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Formula(0, 0, result));

        ExcelCell cell = grid[(0, 0)];
        Assert.AreEqual(ExcelCellKind.String, cell.Kind);
        Assert.AreEqual(string.Empty, cell.StringValue);
    }

    /// <summary>
    /// Verifies that a <c>BOOLERR</c> error cell exposes the mapped <see cref="ExcelErrorCode" />.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenBoolErrIsError_ShouldExposeErrorCode()
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(BoolErr(0, 0, 0x07, isError: true));

        ExcelCell cell = grid[(0, 0)];
        Assert.AreEqual(ExcelCellKind.Error, cell.Kind);
        Assert.AreEqual(ExcelErrorCode.DivideByZero, cell.ErrorValue!.Value);
    }

    /// <summary>
    /// Verifies that a <c>BOOLERR</c> boolean cell is surfaced as a boolean rather than an error.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenBoolErrIsBoolean_ShouldSurfaceBooleanCell()
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(BoolErr(0, 0, 0x01, isError: false));

        ExcelCell cell = grid[(0, 0)];
        Assert.AreEqual(ExcelCellKind.Boolean, cell.Kind);
        Assert.IsTrue(cell.BooleanValue!.Value);
        Assert.IsNull(cell.ErrorValue);
    }

    /// <summary>
    /// Verifies that a malformed (truncated) formula record throws <see cref="Biff8FormatException" />.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenFormulaRecordTruncated_ShouldThrowBiff8FormatException()
    {
        Biff8Record truncated = Record(Biff8RecordType.Formula, new byte[8]);

        _ = Assert.ThrowsExactly<Biff8FormatException>(() =>
        {
            _ = Biff8WorksheetReader.ReadCells([truncated], 0, 1, []).ToList();
        });
    }
}
