// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileOptionsTests.OmitUnresolvableValues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstFileOptions.OmitUnresolvableValues" />.
/// </summary>
public partial class PstFileOptionsTests
{
    /// <summary>
    /// Verifies that a new instance does not omit unresolvable values.
    /// </summary>
    [TestMethod]
    public void OmitUnresolvableValues_WhenDefaulted_ShouldBeFalse()
    {
        var options = new PstFileOptions();

        Assert.IsFalse(options.OmitUnresolvableValues);
    }

    /// <summary>
    /// Verifies that the option initializes to the requested value.
    /// </summary>
    [TestMethod]
    public void OmitUnresolvableValues_WhenInitialized_ShouldRetainValue()
    {
        var options = new PstFileOptions { OmitUnresolvableValues = true };

        Assert.IsTrue(options.OmitUnresolvableValues);
    }
}
