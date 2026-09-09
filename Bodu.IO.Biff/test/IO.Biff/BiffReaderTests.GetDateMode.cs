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
}
