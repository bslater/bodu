// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSectionTests.IsMatch.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationSectionTests
{
    /// <summary>
    /// Verifies that a slashless pattern matches at any directory depth.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternHasNoSlash_ShouldMatchAtAnyDepth()
    {
        BoduConfigurationSection section = new("*.cs");

        Assert.IsTrue(section.IsMatch("Foo.cs"));
        Assert.IsTrue(section.IsMatch("a/b/Foo.cs"));
    }

    /// <summary>
    /// Verifies that an anchored pattern only matches paths relative to the document root.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternIsAnchored_ShouldRequireMatchFromRoot()
    {
        BoduConfigurationSection section = new("src/**/*.cs");

        Assert.IsTrue(section.IsMatch("src/Foo.cs"));
        Assert.IsTrue(section.IsMatch("src/a/Foo.cs"));
        Assert.IsFalse(section.IsMatch("other/Foo.cs"));
    }

    /// <summary>
    /// Verifies that a regular section returns the same result as compiling and matching the pattern
    /// directly.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenSectionIsRegular_ShouldDelegateToCompiledPattern()
    {
        BoduConfigurationSection section = new("*.{cs,csproj}");

        Assert.AreEqual(BoduConfigurationPattern.Compile("*.{cs,csproj}").IsMatch("Foo.cs"), section.IsMatch("Foo.cs"));
    }
}
