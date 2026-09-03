// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the behavior of <see cref="PstFileOptions" />.
/// </summary>
[TestClass]
public partial class PstFileOptionsTests
{
    /// <summary>
    /// Verifies that a new instance defaults to compatible validation.
    /// </summary>
    [TestMethod]
    public void ValidationLevel_WhenDefaulted_ShouldBeCompatible()
    {
        var options = new PstFileOptions();

        Assert.AreEqual(PstValidationLevel.Compatible, options.ValidationLevel);
    }

    /// <summary>
    /// Verifies that the validation level initializes to the requested value.
    /// </summary>
    [TestMethod]
    public void ValidationLevel_WhenInitialized_ShouldRetainValue()
    {
        var options = new PstFileOptions { ValidationLevel = PstValidationLevel.Strict };

        Assert.AreEqual(PstValidationLevel.Strict, options.ValidationLevel);
    }

    /// <summary>
    /// Verifies that a new instance defaults to the documented decoded-block cache budget.
    /// </summary>
    [TestMethod]
    public void BlockCacheSize_WhenDefaulted_ShouldBe256()
    {
        var options = new PstFileOptions();

        Assert.AreEqual(256, options.BlockCacheSize);
    }

    /// <summary>
    /// Verifies that the cache budget initializes to the requested value, including zero (caching disabled).
    /// </summary>
    [TestMethod]
    public void BlockCacheSize_WhenInitialized_ShouldRetainValue()
    {
        Assert.AreEqual(0, new PstFileOptions { BlockCacheSize = 0 }.BlockCacheSize);
        Assert.AreEqual(4, new PstFileOptions { BlockCacheSize = 4 }.BlockCacheSize);
    }

    /// <summary>
    /// Verifies that a negative cache budget throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void BlockCacheSize_WhenNegative_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new PstFileOptions { BlockCacheSize = -1 };
        });
    }
}
