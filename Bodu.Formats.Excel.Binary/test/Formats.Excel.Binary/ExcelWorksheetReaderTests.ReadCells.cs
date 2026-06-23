// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.ReadCells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Linq;

namespace Bodu.Formats.Excel.Binary;

public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Verifies that a formula cell with a numeric cached result is surfaced as a <see cref="ExcelCellKind.Number" />
    /// cell.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenFormulaHasNumericResult_ShouldSurfaceNumberCell()
    {
        byte[] result = new byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(result, 1234.5);

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Biff8TestWorkbook.Formula(2, 3, result));

        ExcelCell cell = grid[(2, 3)];
        Assert.AreEqual(ExcelCellKind.Number, cell.Kind);
        Assert.AreEqual(1234.5, cell.NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that a formula cell with a boolean cached result is surfaced as a <see cref="ExcelCellKind.Boolean" />
    /// cell.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenFormulaHasBooleanResult_ShouldSurfaceBooleanCell()
    {
        byte[] result = [0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Biff8TestWorkbook.Formula(0, 0, result));

        ExcelCell cell = grid[(0, 0)];
        Assert.AreEqual(ExcelCellKind.Boolean, cell.Kind);
        Assert.IsTrue(cell.BooleanValue!.Value);
    }

    /// <summary>
    /// Verifies that a formula cell with an error cached result is surfaced as an <see cref="ExcelCellKind.Error" />
    /// cell carrying the error code.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenFormulaHasErrorResult_ShouldSurfaceErrorCellWithCode()
    {
        byte[] result = [0x02, 0x00, 0x2A, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Biff8TestWorkbook.Formula(1, 1, result));

        ExcelCell cell = grid[(1, 1)];
        Assert.AreEqual(ExcelCellKind.Error, cell.Kind);
        Assert.AreEqual(ExcelErrorCode.NotAvailable, cell.ErrorValue!.Value);
    }

    /// <summary>
    /// Verifies that a formula cell with a string cached result decodes its trailing <c>STRING</c> record into a text
    /// cell.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenFormulaHasStringResult_ShouldSurfaceTextCellFromStringRecord()
    {
        byte[] result = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid =
            ReadGrid(Biff8TestWorkbook.Formula(4, 5, result), Biff8TestWorkbook.CompressedString("Hello"));

        ExcelCell cell = grid[(4, 5)];
        Assert.AreEqual(ExcelCellKind.String, cell.Kind);
        Assert.AreEqual("Hello", cell.StringValue);
    }

    /// <summary>
    /// Verifies that a formula cell whose string result is not followed by a <c>STRING</c> record yields an empty text
    /// cell rather than throwing or consuming a later record.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenFormulaStringResultHasNoStringRecord_ShouldSurfaceEmptyTextCellAndKeepNextRecord()
    {
        byte[] result = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF];

        Dictionary<(int Row, int Column), ExcelCell> grid =
            ReadGrid(Biff8TestWorkbook.Formula(0, 0, result), Biff8TestWorkbook.Number(1, 0, 9.0));

        Assert.AreEqual(string.Empty, grid[(0, 0)].StringValue);
        Assert.AreEqual(9.0, grid[(1, 0)].NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that a <c>BOOLERR</c> error cell exposes the mapped <see cref="ExcelErrorCode" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenBoolErrIsError_ShouldExposeErrorCode()
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Biff8TestWorkbook.BoolErr(0, 0, 0x07, isError: true));

        ExcelCell cell = grid[(0, 0)];
        Assert.AreEqual(ExcelCellKind.Error, cell.Kind);
        Assert.AreEqual(ExcelErrorCode.DivideByZero, cell.ErrorValue!.Value);
    }

    /// <summary>
    /// Verifies that a <c>BOOLERR</c> boolean cell is surfaced as a boolean rather than an error.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenBoolErrIsBoolean_ShouldSurfaceBooleanCell()
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Biff8TestWorkbook.BoolErr(0, 0, 0x01, isError: false));

        ExcelCell cell = grid[(0, 0)];
        Assert.AreEqual(ExcelCellKind.Boolean, cell.Kind);
        Assert.IsTrue(cell.BooleanValue!.Value);
        Assert.IsNull(cell.ErrorValue);
    }

    /// <summary>
    /// Verifies that a <c>MULRK</c> record expands into one numeric cell per contained RK value, in column order.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenMulRk_ShouldExpandToOneCellPerValue()
    {
        // RK encoding 0x...: integer flag (bit 1) set, value 5 and 9 in the high 30 bits.
        uint rk5 = (5u << 2) | 0x02;
        uint rk9 = (9u << 2) | 0x02;

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadGrid(Biff8TestWorkbook.MulRk(3, 1, (0, rk5), (0, rk9)));

        Assert.AreEqual(5.0, grid[(3, 1)].NumberValue!.Value, 0.0);
        Assert.AreEqual(9.0, grid[(3, 2)].NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that <see cref="ExcelWorksheetReader.ReadRows" /> groups cells by row in the order they appear.
    /// </summary>
    [TestMethod]
    public void ReadRows_WhenCellsSpanRows_ShouldGroupByRow()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Number(0, 1, 2.0),
            Biff8TestWorkbook.Number(1, 0, 3.0));

        List<ExcelRow> rows = reader.ReadRows().ToList();

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(0, rows[0].RowIndex);
        Assert.AreEqual(2, rows[0].Cells.Count);
        Assert.AreEqual(1, rows[1].RowIndex);
        Assert.AreEqual(1, rows[1].Cells.Count);
    }

    /// <summary>
    /// Verifies that reading from a disposed worksheet reader throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenReaderDisposed_ShouldThrowObjectDisposedException()
    {
        ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(Biff8TestWorkbook.Number(0, 0, 1.0));
        reader.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });
    }
}
