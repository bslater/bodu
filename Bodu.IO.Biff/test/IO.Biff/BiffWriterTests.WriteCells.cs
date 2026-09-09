// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteCells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that a NUMBER record is written in the fourteen-byte layout and decodes back.
    /// </summary>
    [TestMethod]
    public void WriteNumber_WhenWritten_ShouldDecodeBack()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteNumber(3, 4, 15, -2.5));

        BiffReader reader = Single(bytes);
        Assert.AreEqual(new BiffNumberRecord(3, 4, 15, -2.5), reader.GetNumber());
        CollectionAssert.AreEqual(BiffTestRecords.Number(3, 4, -2.5, 15), bytes);
    }

    /// <summary>
    /// Verifies that an RK record decodes back.
    /// </summary>
    [TestMethod]
    public void WriteRk_WhenWritten_ShouldDecodeBack()
    {
        Assert.IsTrue(BiffRk.TryEncode(12.34, out uint rk));
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteRk(1, 2, 3, rk));

        Assert.AreEqual(12.34, Single(bytes, BiffVersion.Biff5).GetRk().Value);
    }

    /// <summary>
    /// Verifies that a MULRK run decodes back with the declared last column.
    /// </summary>
    [TestMethod]
    public void WriteMulRk_WhenWritten_ShouldDecodeBack()
    {
        BiffRkCell[] cells = [new(1, 0x02), new(2, 0x06), new(3, 0x0A)];
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteMulRk(7, 4, cells));

        BiffMulRkRecord run = Single(bytes).GetMulRk();
        Assert.AreEqual(7, run.Row);
        Assert.AreEqual(4, run.FirstColumn);
        Assert.AreEqual(6, run.LastColumn);
        Assert.AreEqual(3, run.Count);
        Assert.AreEqual(2.0, run[2].Value);
        CollectionAssert.AreEqual(BiffTestRecords.MulRk(7, 4, (1, 0x02), (2, 0x06), (3, 0x0A)), bytes);
    }

    /// <summary>
    /// Verifies that an empty MULRK run is rejected.
    /// </summary>
    [TestMethod]
    public void WriteMulRk_WhenEmpty_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => Emit8((ref BiffWriter w) => w.WriteMulRk(0, 0, default)));
    }

    /// <summary>
    /// Verifies that BLANK and MULBLANK records decode back.
    /// </summary>
    [TestMethod]
    public void WriteBlank_WhenWritten_ShouldDecodeBackWithMulBlank()
    {
        byte[] bytes = Emit8((ref BiffWriter w) =>
        {
            w.WriteBlank(1, 1, 9);
            w.WriteMulBlank(2, 5, [10, 11]);
        });

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(new BiffBlankRecord(1, 1, 9), reader.GetBlank());
        Assert.IsTrue(reader.Read());
        BiffMulBlankRecord run = reader.GetMulBlank();
        Assert.AreEqual(2, run.Count);
        Assert.AreEqual(6, run.LastColumn);
        Assert.AreEqual(11, run[1]);
    }

    /// <summary>
    /// Verifies that boolean and error cells decode back through the BOOLERR record.
    /// </summary>
    [TestMethod]
    public void WriteBoolean_WhenWritten_ShouldDecodeBackWithError()
    {
        byte[] bytes = Emit8((ref BiffWriter w) =>
        {
            w.WriteBoolean(0, 0, 0, true);
            w.WriteError(0, 1, 0, 0x2A);
        });

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        BiffBoolErrRecord boolean = reader.GetBoolErr();
        Assert.IsFalse(boolean.IsError);
        Assert.IsTrue(boolean.BooleanValue);
        Assert.IsTrue(reader.Read());
        BiffBoolErrRecord error = reader.GetBoolErr();
        Assert.IsTrue(error.IsError);
        Assert.AreEqual(0x2A, error.ErrorCode);
    }

    /// <summary>
    /// Verifies that a row or column outside the 16-bit range is rejected by every cell writer.
    /// </summary>
    [TestMethod]
    public void WriteNumber_WhenRowOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteNumber(65536, 0, 0, 1)),
            "row");
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteBlank(0, -1, 0)),
            "column");
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteMulBlank(0, 65535, [1, 2])),
            "xfIndices");
    }

    /// <summary>
    /// Verifies that a ROW record decodes back.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenWritten_ShouldDecodeBack()
    {
        var row = new BiffRowRecord(5, 1, 4, 0x00FF, 0x00C0, 0x0015);
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteRow(row));

        Assert.AreEqual(row, Single(bytes).GetRow());
    }

    /// <summary>
    /// Verifies that a DIMENSIONS record is written in the version's layout and decodes back.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="expectedLength">The expected payload length.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5, 10)]
    [DataRow(BiffVersion.Biff8, 14)]
    public void WriteDimensions_WhenWritten_ShouldUseVersionLayout(BiffVersion version, int expectedLength)
    {
        var dimensions = new BiffDimensionsRecord(1, 100, 2, 8);
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteDimensions(dimensions));

        BiffReader reader = Single(bytes, version);
        Assert.AreEqual(expectedLength, reader.RecordLength);
        Assert.AreEqual(dimensions, reader.GetDimensions());
    }

    /// <summary>
    /// Verifies that a row beyond the 16-bit range is accepted under BIFF8 and rejected under BIFF5.
    /// </summary>
    [TestMethod]
    public void WriteDimensions_WhenRowExceeds16Bits_ShouldDependOnVersion()
    {
        var dimensions = new BiffDimensionsRecord(0, 70000, 0, 1);

        Assert.AreEqual(70000, Single(Emit8((ref BiffWriter w) => w.WriteDimensions(dimensions))).GetDimensions().LastRowExclusive);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Emit5((ref BiffWriter w) => w.WriteDimensions(dimensions)));
    }

    /// <summary>
    /// Verifies that an XF record is written at the version's length and decodes back.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="expectedLength">The expected payload length.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5, 16)]
    [DataRow(BiffVersion.Biff8, 20)]
    public void WriteXf_WhenWritten_ShouldUseVersionLength(BiffVersion version, int expectedLength)
    {
        var xf = new BiffXfRecord(2, 14, 0xFFF5);
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteXf(xf));

        BiffReader reader = Single(bytes, version);
        Assert.AreEqual(expectedLength, reader.RecordLength);
        Assert.AreEqual(xf, reader.GetXf());
    }
}
