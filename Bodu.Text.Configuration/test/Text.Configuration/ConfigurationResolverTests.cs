// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for resolution behaviour exercised via
/// <see cref="ConfigurationDocument.Resolve(string?, ConfigurationResolveOptions?)" />.
/// </summary>
[TestClass]
public partial class ConfigurationResolverTests
{
    /// <summary>
    /// Verifies that resolving against a target with no matching sections still applies preamble properties
    /// under the Bodu profile.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNoSectionMatches_ShouldStillApplyPreamble()
    {
        var doc = ConfigurationDocument.Parse("root = true\napplication.name = Bodu\n");
        ConfigurationView view = doc.Resolve("README");

        Assert.AreEqual("Bodu", view.GetString("application:name"));
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationProfile.EditorConfigCompatible" /> preamble pairs
    /// other than <c>root</c> do not contribute to resolution.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatible_ShouldIgnoreNonRootPreamblePairs()
    {
        var doc = ConfigurationDocument.Parse("application.name = Bodu\n");
        ConfigurationView view = doc.Resolve("Foo.cs", ConfigurationResolveOptions.EditorConfigCompatible);

        Assert.IsNull(view["application:name"]);
    }

    /// <summary>
    /// Verifies that the resolved view of a freshly created empty document is empty.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDocumentIsEmpty_ShouldProduceEmptyView()
    {
        IniDocument doc = new();
        ConfigurationView view = doc.Resolve("anything");

        Assert.AreEqual(0, view.Count);
    }

    /// <summary>
    /// Verifies that the resolver is a snapshot — mutating the document after resolution does not change a
    /// previously returned view.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDocumentMutatedAfterwards_ShouldNotAffectExistingView()
    {
        var doc = ConfigurationDocument.Parse(ConfigurationFixtures.Minimal);
        ConfigurationView view = doc.Resolve("Foo.cs");

        doc.Sections[0].SetEntry("format.indent.size", "999");

        Assert.AreEqual("4", view.GetString("format:indent:size"));
    }
}
