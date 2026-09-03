// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileOptionsTests.MaxDataTreeLeaves.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstFileOptionsTests
{
    /// <summary>
    /// Verifies that a new instance defaults to the documented fan-out limit.
    /// </summary>
    [TestMethod]
    public void MaxDataTreeLeaves_WhenDefaulted_ShouldBe65536()
    {
        Assert.AreEqual(65_536, new PstFileOptions().MaxDataTreeLeaves);
    }

    /// <summary>
    /// Verifies that the limit initializes to the requested value.
    /// </summary>
    [TestMethod]
    public void MaxDataTreeLeaves_WhenInitialized_ShouldRetainValue()
    {
        Assert.AreEqual(8, new PstFileOptions { MaxDataTreeLeaves = 8 }.MaxDataTreeLeaves);
    }

    /// <summary>
    /// Verifies that a zero or negative limit throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="value">The rejected value.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void MaxDataTreeLeaves_WhenNotPositive_ShouldThrowArgumentOutOfRangeException(int value)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new PstFileOptions { MaxDataTreeLeaves = value };
        });
    }
}
