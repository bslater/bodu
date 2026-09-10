// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetBoolErr.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a boolean BOOLERR record decodes its value.
    /// </summary>
    [TestMethod]
    public void GetBoolErr_WhenBoolean_ShouldDecodeBooleanValue()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.BoolErr(1, 1, 1, isError: false), BiffRecordType.BoolErr);

        BiffBoolErrRecord cell = reader.GetBoolErr();

        Assert.IsFalse(cell.IsError);
        Assert.IsTrue(cell.BooleanValue);
        Assert.AreEqual(1, cell.RawValue);
    }

    /// <summary>
    /// Verifies that an error BOOLERR record decodes its error code.
    /// </summary>
    [TestMethod]
    public void GetBoolErr_WhenError_ShouldDecodeErrorCode()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.BoolErr(2, 3, 0x2A, isError: true, xf: 4), BiffRecordType.BoolErr);

        BiffBoolErrRecord cell = reader.GetBoolErr();

        Assert.IsTrue(cell.IsError);
        Assert.AreEqual(0x2A, cell.ErrorCode);
        Assert.AreEqual(2, cell.Row);
        Assert.AreEqual(3, cell.Column);
        Assert.AreEqual(4, cell.XfIndex);
    }

    /// <summary>
    /// Verifies that a boolean cell whose value byte is neither zero nor one is reported as true.
    /// </summary>
    [TestMethod]
    public void GetBoolErr_WhenBooleanByteIsNotOne_ShouldReportTrue()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.BoolErr(0, 0, 0xFF, isError: false), BiffRecordType.BoolErr);

        BiffBoolErrRecord cell = reader.GetBoolErr();

        Assert.IsFalse(cell.IsError);
        Assert.IsTrue(cell.BooleanValue);
        Assert.AreEqual(0xFF, cell.RawValue);
    }

    /// <summary>
    /// Verifies that a kind byte other than zero or one is treated as an error marker.
    /// </summary>
    [TestMethod]
    public void GetBoolErr_WhenKindByteIsNotZeroOrOne_ShouldReportError()
    {
        byte[] record = BiffTestRecords.BoolErr(0, 0, 0x2A, isError: true);
        record[4 + 7] = 0x07;
        BiffReader reader = ReadTo8(record, BiffRecordType.BoolErr);

        Assert.IsTrue(reader.GetBoolErr().IsError);
        Assert.AreEqual(0x2A, reader.GetBoolErr().ErrorCode);
    }

    /// <summary>
    /// Verifies that every documented BIFF error code round-trips through the record.
    /// </summary>
    /// <param name="code">The error code.</param>
    [TestMethod]
    [DataRow((byte)0x00)]
    [DataRow((byte)0x07)]
    [DataRow((byte)0x0F)]
    [DataRow((byte)0x17)]
    [DataRow((byte)0x1D)]
    [DataRow((byte)0x24)]
    [DataRow((byte)0x2A)]
    public void GetBoolErr_WhenErrorCode_ShouldExposeCode(byte code)
    {
        BiffReader reader = ReadTo5(BiffTestRecords.BoolErr(1, 1, code, isError: true), BiffRecordType.BoolErr);

        Assert.AreEqual(code, reader.GetBoolErr().ErrorCode);
    }
}
