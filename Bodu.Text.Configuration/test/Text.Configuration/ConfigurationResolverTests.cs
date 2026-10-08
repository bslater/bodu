// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Text.Configuration.Infrastructure;


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
    /// Verifies that the resolver is a snapshot - mutating the document after resolution does not change a
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

    /// <summary>
    /// Verifies that under <see cref="ConfigurationProfile.EditorConfigCompatible" /> a dotted key is reported as
    /// written, keeping its dots, and is found under that name.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatibleAndKeyIsDotted_ShouldKeepTheDots()
    {
        var doc = ConfigurationDocument.Parse(
            "[*]\ndotnet_diagnostic.ide0005.severity = warning\n",
            ConfigurationParseOptions.EditorConfigCompatible);

        ConfigurationView view = doc.Resolve("Program.cs", ConfigurationResolveOptions.EditorConfigCompatible);

        CollectionAssert.AreEqual(new[] { "dotnet_diagnostic.ide0005.severity" }, view.Keys.ToArray());
        Assert.AreEqual("warning", view["dotnet_diagnostic.ide0005.severity"]);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationProfile.EditorConfigCompatible" /> no preamble pair reaches the
    /// resolved view, <c>root</c> included, while the document keeps the pairs in its global section.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatible_ShouldLeaveTheWholePreambleOutOfTheView()
    {
        var doc = ConfigurationDocument.Parse(
            "root = true\nindent_style = tab\n\n[*]\nindent_size = 4\n",
            ConfigurationParseOptions.EditorConfigCompatible);

        ConfigurationView view = doc.Resolve("Program.cs", ConfigurationResolveOptions.EditorConfigCompatible);

        CollectionAssert.AreEqual(new[] { "indent_size" }, view.Keys.ToArray());
        CollectionAssert.AreEqual(
            new[] { "root", "indent_style" },
            doc.GlobalSection.Entries.Select(entry => entry.Key).ToArray());
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationProfile.EditorConfigCompatible" /> a key written with capitals is
    /// reported lowercased, as EditorConfig lowercases every key after parsing (core-test <c>lowercase_names</c>), and is
    /// still found under the case it was written in.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatibleAndKeyHasCapitals_ShouldLowercaseTheKey()
    {
        var doc = ConfigurationDocument.Parse(
            "root = true\n\n[test.c]\nTestProperty = testvalue\ndotnet_diagnostic.CA1000.severity = warning\n",
            ConfigurationParseOptions.EditorConfigCompatible);

        ConfigurationView view = doc.Resolve("test.c", ConfigurationResolveOptions.EditorConfigCompatible);

        CollectionAssert.AreEqual(new[] { "testproperty", "dotnet_diagnostic.ca1000.severity" }, view.Keys.ToArray());
        Assert.AreEqual("testvalue", view["TestProperty"]);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationProfile.EditorConfigCompatible" /> keys are lowercased with the
    /// invariant culture, so a capital I becomes i even while the current culture is Turkish.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatibleUnderTurkishCulture_ShouldLowercaseKeysInvariantly()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var doc = ConfigurationDocument.Parse("[*]\nINDENT_SIZE = 4\n", ConfigurationParseOptions.EditorConfigCompatible);

            ConfigurationView view = doc.Resolve("a.txt", ConfigurationResolveOptions.EditorConfigCompatible);

            CollectionAssert.AreEqual(new[] { "indent_size" }, view.Keys.ToArray());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Verifies that the profiles other than <see cref="ConfigurationProfile.EditorConfigCompatible" /> report a key in
    /// the case it was written in.
    /// </summary>
    /// <param name="profile">The profile whose parse and resolve options apply.</param>
    [TestMethod]
    [DataRow(ConfigurationProfile.Bodu)]
    [DataRow(ConfigurationProfile.Strict)]
    [DataRow(ConfigurationProfile.Relaxed)]
    public void Resolve_WhenProfileIsNotEditorConfigCompatible_ShouldKeepTheCaseOfKeys(ConfigurationProfile profile)
    {
        var doc = ConfigurationDocument.Parse("[*]\nTestProperty = testvalue\n", ConfigurationParseOptions.For(profile));

        ConfigurationView view = doc.Resolve("a.txt", ConfigurationResolveOptions.For(profile));

        CollectionAssert.AreEqual(new[] { "TestProperty" }, view.Keys.ToArray());
    }
}
