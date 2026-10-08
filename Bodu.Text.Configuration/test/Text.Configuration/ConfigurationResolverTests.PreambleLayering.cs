// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.PreambleLayering.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationResolverTests
{
    /// <summary>
    /// Verifies that preamble properties seed the resolved view and matching sections override them.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPreambleAndSectionDefineSameKey_ShouldUseSectionValue()
    {
        const string fixture = """
format.indent.size = 4

[*.cs]
format.indent.size = 2
""";
        var doc = ConfigurationDocument.Parse(fixture);

        ConfigurationView view = doc.Resolve("Foo.cs");
        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that when <see cref="ConfigurationResolveOptions.ApplyPreambleProperties" /> is
    /// disabled, preamble pairs do not contribute to the resolved view.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenApplyPreamblePropertiesIsFalse_ShouldOmitPreambleValues()
    {
        const string fixture = """
application.name = Bodu

[*.cs]
format.indent.size = 4
""";
        var doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new() { ApplyPreambleProperties = false };
        ConfigurationView view = doc.Resolve("Foo.cs", options);

        Assert.IsNull(view["application:name"]);
        Assert.AreEqual("4", view.GetString("format:indent:size"));
    }

    /// <summary>
    /// Verifies that the EditorConfig-compatible preset disables preamble layering.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatiblePreset_ShouldNotApplyPreamble()
    {
        const string fixture = """
application.name = Bodu

[*.cs]
format.indent.size = 4
""";
        var doc = ConfigurationDocument.Parse(fixture);

        ConfigurationView view = doc.Resolve("Foo.cs", ConfigurationResolveOptions.EditorConfigCompatible);

        Assert.IsNull(view["application:name"]);
    }

    /// <summary>
    /// Verifies that under the default <see cref="ConfigurationProfile.Bodu" /> profile the preamble's <c>root</c> pair
    /// is consumed rather than resolved as a property, while the other preamble pairs still contribute.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenBoduPreambleHasRoot_ShouldNotResolveRootAsAProperty()
    {
        var doc = ConfigurationDocument.Parse("root = true\nfoo = bar\n[*]\nk = v\n");

        ConfigurationView view = doc.Resolve("a.txt");

        CollectionAssert.AreEqual(new[] { "foo", "k" }, view.Keys.ToArray());
        Assert.IsNull(view["root"]);
    }

    /// <summary>
    /// Verifies that every profile that layers the preamble leaves its <c>root</c> pair out of the view whatever the
    /// case it is written in, as EditorConfig keys are case-insensitive, while the other preamble pairs contribute.
    /// </summary>
    /// <param name="profile">The profile whose parse and resolve options apply.</param>
    [TestMethod]
    [DataRow(ConfigurationProfile.Bodu)]
    [DataRow(ConfigurationProfile.Strict)]
    [DataRow(ConfigurationProfile.Relaxed)]
    public void Resolve_WhenPreambleHasRootInAnyCase_ShouldLeaveItOutOfTheView(ConfigurationProfile profile)
    {
        var doc = ConfigurationDocument.Parse("ROOT = true\nfoo = bar\n", ConfigurationParseOptions.For(profile));

        ConfigurationView view = doc.Resolve("a.txt", ConfigurationResolveOptions.For(profile));

        CollectionAssert.AreEqual(new[] { "foo" }, view.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that a <c>root</c> pair inside a section is an ordinary property, since only the preamble's
    /// <c>root</c> has a meaning of its own.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRootIsInsideASection_ShouldResolveItAsAProperty()
    {
        var doc = ConfigurationDocument.Parse("[*]\nroot = true\n");

        ConfigurationView view = doc.Resolve("a.txt");

        Assert.AreEqual("true", view["root"]);
    }

    /// <summary>
    /// Verifies that resolving leaves the preamble's <c>root</c> pair in the document's global section, where a
    /// caller that collects files up the directory tree reads it.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPreambleHasRoot_ShouldKeepThePairInTheGlobalSection()
    {
        var doc = ConfigurationDocument.Parse("root = true\nfoo = bar\n");

        _ = doc.Resolve("a.txt");

        Assert.AreEqual("true", doc.GlobalSection["root"]);
    }
}
