// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSourceLocationTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the <see cref="BoduConfigurationSourceLocation" /> readonly struct.
/// </summary>
[TestClass]
public class BoduConfigurationSourceLocationTests
{
    /// <summary>
    /// Verifies that two locations with the same fields compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAllFieldsMatch_ShouldBeTrue()
    {
        BoduConfigurationSourceLocation a = new(1, 1, 5, "file.cs");
        BoduConfigurationSourceLocation b = new(1, 1, 5, "file.cs");

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that locations differing in any field compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenLineNumberDiffers_ShouldBeFalse()
    {
        BoduConfigurationSourceLocation a = new(1, 1, 5);
        BoduConfigurationSourceLocation b = new(2, 1, 5);

        Assert.IsFalse(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSourceLocation.None" /> reports zero fields.
    /// </summary>
    [TestMethod]
    public void None_WhenAccessed_ShouldReportZeroFields()
    {
        BoduConfigurationSourceLocation location = BoduConfigurationSourceLocation.None;

        Assert.AreEqual(0, location.LineNumber);
        Assert.AreEqual(0, location.LinePosition);
        Assert.AreEqual(0, location.Length);
        Assert.IsNull(location.Path);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSourceLocation.ToString" /> renders the line and column.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLocationIsKnown_ShouldIncludeLineAndColumn()
    {
        BoduConfigurationSourceLocation location = new(7, 13, 1);

        var text = location.ToString();

        StringAssert.Contains(text, "7");
        StringAssert.Contains(text, "13");
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSourceLocation.ToString" /> includes the path prefix when
    /// one is supplied.
    /// </summary>
    [TestMethod]
    public void ToString_WhenPathProvided_ShouldIncludePathPrefix()
    {
        BoduConfigurationSourceLocation location = new(1, 1, 1, "file.boduconfig");

        Assert.IsTrue(location.ToString().StartsWith("file.boduconfig", StringComparison.Ordinal));
    }
}
