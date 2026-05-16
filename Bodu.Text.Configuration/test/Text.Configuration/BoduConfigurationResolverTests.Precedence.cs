// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolverTests.Precedence.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationResolverTests
{
    /// <summary>
    /// Verifies that a later matching section overrides an earlier matching section's value.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenLaterSectionAlsoMatches_ShouldUseLaterSectionValue()
    {
        const string fixture = """
[*.cs]
format.indent.size = 4

[*.cs]
format.indent.size = 2
""";
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(fixture);
        BoduConfigurationView view = doc.Resolve("Foo.cs");

        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that an earlier matching section's values are kept for keys not overridden in a later
    /// matching section.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenLaterSectionOmitsKey_ShouldRetainEarlierSectionValue()
    {
        const string fixture = """
[*.cs]
format.indent.style = space
format.indent.size = 4

[src/**/*.cs]
format.indent.size = 2
""";
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(fixture);
        BoduConfigurationView view = doc.Resolve("src/Foo.cs");

        Assert.AreEqual("space", view.GetString("format:indent:style"));
        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that a section that does not match the target path contributes no values to the resolved
    /// view.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSectionDoesNotMatch_ShouldNotContributeValues()
    {
        const string fixture = """
[*.cs]
format.indent.size = 4

[*.txt]
format.indent.size = 1
""";
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(fixture);
        BoduConfigurationView view = doc.Resolve("Foo.cs");

        Assert.AreEqual(4, view.GetInt32("format:indent:size"));
    }
}
