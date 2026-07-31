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
public class PstFileOptionsTests
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
}
