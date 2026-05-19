// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Text.Configuration;
using Bodu.Text.Ini;

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
    /// Verifies that <see cref="BoduConfigurationDocument.Parse(string)" /> populates the global section and
    /// the named sections from a representative input.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationDocument_Parse_ShouldPopulateSections()
    {
        BoduConfigurationParseResult result = BoduConfigurationDocument.ParseWithDiagnostics(Sample);
        IniDocument doc = result.Document;

        Assert.AreEqual("true", doc.GlobalSection["root"]);
        Assert.HasCount(2, doc.Sections);
        Assert.AreEqual("*.cs", doc.Sections[0].Name);
        Assert.AreEqual("src/**/*.{cs,csproj}", doc.Sections[1].Name);
        Assert.IsEmpty(result.Diagnostics);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationExtensions.Resolve(IniDocument, string?)" /> layers the
    /// global section and matching named sections so that later sections override earlier ones.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationDocument_Resolve_ShouldLayerSectionsInOrder()
    {
        IniDocument doc = BoduConfigurationDocument.Parse(Sample);
        BoduConfigurationView view = doc.Resolve("src/Bodu.Text.Configuration/src/Foo.cs");

        Assert.AreEqual("space", view.GetString("format:indent:style"));
        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
        Assert.AreEqual("Warning", view.GetString("logging:level:default"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Save(IniDocument, System.IO.TextWriter, BoduConfigurationWriteOptions?)" />
    /// emits text that re-parses to an equivalent document (round-trip).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationDocument_RoundTrip_ShouldPreserveSemantics()
    {
        IniDocument original = BoduConfigurationDocument.Parse(Sample);
        using StringWriter sw = new();
        BoduConfigurationDocument.Save(original, sw);
        IniDocument reparsed = BoduConfigurationDocument.Parse(sw.ToString());

        Assert.HasCount(original.Sections.Count, reparsed.Sections);
        Assert.AreEqual(original.GlobalSection["root"], reparsed.GlobalSection["root"]);
        Assert.HasCount(original.GlobalSection.Entries.Count, reparsed.GlobalSection.Entries);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationPattern" /> compiles a representative EditorConfig glob and
    /// matches expected paths.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void BoduConfigurationPattern_IsMatch_ShouldMatchExpectedPaths()
    {
        var pattern = BoduConfigurationPattern.Compile("src/**/*.{cs,csproj}");

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
        var key = BoduConfigurationKey.Parse("logging.level.default");

        Assert.AreEqual("logging:level:default", key.ConfigurationKey);
        Assert.HasCount(3, key.Segments);
    }
}
