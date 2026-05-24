// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationSourceLocationTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the <see cref="ConfigurationSourceLocation" /> readonly struct.
/// </summary>
[TestClass]
public class ConfigurationSourceLocationTests
{
    /// <summary>
    /// Verifies that two locations with the same fields compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAllFieldsMatch_ShouldBeTrue()
    {
        ConfigurationSourceLocation a = new(1, 1, 5, "file.cs");
        ConfigurationSourceLocation b = new(1, 1, 5, "file.cs");

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
        ConfigurationSourceLocation a = new(1, 1, 5);
        ConfigurationSourceLocation b = new(2, 1, 5);

        Assert.IsFalse(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationSourceLocation.None" /> reports zero fields.
    /// </summary>
    [TestMethod]
    public void None_WhenAccessed_ShouldReportZeroFields()
    {
        ConfigurationSourceLocation location = ConfigurationSourceLocation.None;

        Assert.AreEqual(0, location.LineNumber);
        Assert.AreEqual(0, location.LinePosition);
        Assert.AreEqual(0, location.Length);
        Assert.IsNull(location.Path);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationSourceLocation.ToString" /> renders the line and column.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLocationIsKnown_ShouldIncludeLineAndColumn()
    {
        ConfigurationSourceLocation location = new(7, 13, 1);

        var text = location.ToString();

        StringAssert.Contains(text, "7");
        StringAssert.Contains(text, "13");
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationSourceLocation.ToString" /> includes the path prefix when
    /// one is supplied.
    /// </summary>
    [TestMethod]
    public void ToString_WhenPathProvided_ShouldIncludePathPrefix()
    {
        ConfigurationSourceLocation location = new(1, 1, 1, "file.boduconfig");

        Assert.IsTrue(location.ToString().StartsWith("file.boduconfig", StringComparison.Ordinal));
    }
}
