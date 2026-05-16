// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolverTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for resolution behaviour exercised via
/// <see cref="BoduConfigurationDocument.Resolve(string?, BoduConfigurationResolveOptions?)" />.
/// </summary>
[TestClass]
public partial class BoduConfigurationResolverTests
{
    /// <summary>
    /// Verifies that resolving against a target with no matching sections still applies preamble properties
    /// under the Bodu profile.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNoSectionMatches_ShouldStillApplyPreamble()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("root = true\napplication.name = Bodu\n");
        BoduConfigurationView view = doc.Resolve("README");

        Assert.AreEqual("Bodu", view.GetString("application:name"));
    }

    /// <summary>
    /// Verifies that under <see cref="BoduConfigurationProfile.EditorConfigCompatible" /> preamble pairs
    /// other than <c>root</c> do not contribute to resolution.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatible_ShouldIgnoreNonRootPreamblePairs()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("application.name = Bodu\n");
        BoduConfigurationView view = doc.Resolve("Foo.cs", BoduConfigurationResolveOptions.EditorConfigCompatible);

        Assert.IsNull(view["application:name"]);
    }

    /// <summary>
    /// Verifies that the resolved view of a freshly created empty document is empty.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDocumentIsEmpty_ShouldProduceEmptyView()
    {
        IniDocument doc = new();
        BoduConfigurationView view = doc.Resolve("anything");

        Assert.AreEqual(0, view.Count);
    }

    /// <summary>
    /// Verifies that the resolver is a snapshot — mutating the document after resolution does not change a
    /// previously returned view.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDocumentMutatedAfterwards_ShouldNotAffectExistingView()
    {
        IniDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Minimal);
        BoduConfigurationView view = doc.Resolve("Foo.cs");

        doc.Sections[0].SetEntry("format.indent.size", "999");

        Assert.AreEqual("4", view.GetString("format:indent:size"));
    }
}
