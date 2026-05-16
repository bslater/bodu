// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Text.Configuration;

namespace Bodu.Smoke;

/// <summary>
/// Smoke tests for the Bodu Text Configuration library — one happy-path test per primary public type.
/// </summary>
[TestClass]
public class SmokeTests
{
    private const string Sample = """
# Bodu configuration smoke fixture
root = true

[*.cs]
format.indent.style = space
format.indent.size = 4
logging.level.default = Information

[src/**/*.{cs,csproj}]
format.indent.size = 2
logging.level.default = Warning
""";

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Parse(string)" /> populates the preamble and
    /// sections from a representative input.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationDocument_Parse_ShouldPopulateSections()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(Sample);

        Assert.AreEqual(true, doc.Root);
        Assert.AreEqual(2, doc.Sections.Count);
        Assert.AreEqual("*.cs", doc.Sections[0].Pattern);
        Assert.AreEqual("src/**/*.{cs,csproj}", doc.Sections[1].Pattern);
        Assert.AreEqual(0, doc.Diagnostics.Length);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Resolve(string?, BoduConfigurationResolveOptions?)" />
    /// layers preamble and matching sections so that later sections override earlier ones.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationDocument_Resolve_ShouldLayerSectionsInOrder()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(Sample);
        BoduConfigurationView view = doc.Resolve("src/Bodu.Text.Configuration/src/Foo.cs");

        Assert.AreEqual("space", view.GetString("format:indent:style"));
        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
        Assert.AreEqual("Warning", view.GetString("logging:level:default"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Save(System.IO.TextWriter, BoduConfigurationWriteOptions?)" />
    /// emits text that re-parses to an equivalent document (round-trip).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationDocument_RoundTrip_ShouldPreserveSemantics()
    {
        BoduConfigurationDocument original = BoduConfigurationDocument.Parse(Sample);
        string text = original.ToString();
        BoduConfigurationDocument reparsed = BoduConfigurationDocument.Parse(text);

        Assert.AreEqual(original.Sections.Count, reparsed.Sections.Count);
        Assert.AreEqual(original.Root, reparsed.Root);
        Assert.AreEqual(original.Preamble.Properties.Count, reparsed.Preamble.Properties.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationPattern" /> compiles a representative EditorConfig glob and
    /// matches expected paths.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationPattern_IsMatch_ShouldMatchExpectedPaths()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("src/**/*.{cs,csproj}");

        Assert.IsTrue(pattern.IsMatch("src/Foo.cs"));
        Assert.IsTrue(pattern.IsMatch("src/Bodu.Text.Configuration/src/Foo.cs"));
        Assert.IsFalse(pattern.IsMatch("src/Foo.txt"));
        Assert.IsFalse(pattern.IsMatch("Foo.cs"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationKey.Parse(string)" /> splits a dotted key into segments and
    /// produces the colon-delimited configuration key.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationKey_Parse_ShouldMapDotToColon()
    {
        BoduConfigurationKey key = BoduConfigurationKey.Parse("logging.level.default");

        Assert.AreEqual("logging:level:default", key.ConfigurationKey);
        Assert.AreEqual(3, key.Segments.Length);
    }
}
