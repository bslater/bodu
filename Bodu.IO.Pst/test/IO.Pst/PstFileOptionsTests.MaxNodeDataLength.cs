// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileOptionsTests.MaxNodeDataLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstFileOptionsTests
{
    /// <summary>
    /// Verifies that a new instance defaults to the documented 256 MiB materialization limit.
    /// </summary>
    [TestMethod]
    public void MaxNodeDataLength_WhenDefaulted_ShouldBe256MiB()
    {
        Assert.AreEqual(256L * 1024 * 1024, new PstFileOptions().MaxNodeDataLength);
    }

    /// <summary>
    /// Verifies that the limit initializes to the requested value.
    /// </summary>
    [TestMethod]
    public void MaxNodeDataLength_WhenInitialized_ShouldRetainValue()
    {
        Assert.AreEqual(4096L, new PstFileOptions { MaxNodeDataLength = 4096 }.MaxNodeDataLength);
    }

    /// <summary>
    /// Verifies that a zero or negative limit throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="value">The rejected value.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    public void MaxNodeDataLength_WhenNotPositive_ShouldThrowArgumentOutOfRangeException(long value)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new PstFileOptions { MaxNodeDataLength = value };
        });
    }
}
