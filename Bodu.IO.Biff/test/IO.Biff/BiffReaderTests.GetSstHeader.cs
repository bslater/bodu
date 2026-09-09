// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetSstHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that the SST header counts decode without reading the strings.
    /// </summary>
    [TestMethod]
    public void GetSstHeader_WhenWellFormed_ShouldDecodeCounts()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Sst(12, 3, BiffTestRecords.UnicodeString("a")), BiffRecordType.Sst);

        BiffSstHeader header = reader.GetSstHeader();

        Assert.AreEqual(12u, header.TotalCount);
        Assert.AreEqual(3u, header.UniqueCount);
    }
}
