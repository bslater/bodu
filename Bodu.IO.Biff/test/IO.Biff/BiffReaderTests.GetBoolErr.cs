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
}
