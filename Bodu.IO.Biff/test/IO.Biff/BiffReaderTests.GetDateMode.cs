// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetDateMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a DATEMODE record decodes the date system selector.
    /// </summary>
    /// <param name="is1904">The value to encode and expect.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetDateMode_WhenWellFormed_ShouldDecode(bool is1904)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.DateMode(is1904), BiffRecordType.DateMode);

        Assert.AreEqual(is1904, reader.GetDateMode().Is1904);
    }

    /// <summary>
    /// Verifies that any non-zero value selects the 1904 system.
    /// </summary>
    /// <param name="raw">The raw record value.</param>
    /// <param name="expected">The expected flag.</param>
    [TestMethod]
    [DataRow((ushort)0, false)]
    [DataRow((ushort)1, true)]
    [DataRow((ushort)0x0100, true)]
    public void GetDateMode_WhenRawValue_ShouldTreatNonZeroAs1904(ushort raw, bool expected)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.DateMode, BiffTestRecords.UInt16(raw)), BiffRecordType.DateMode);

        Assert.AreEqual(expected, reader.GetDateMode().Is1904);
    }
}
