// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.PreambleLayering.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

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
        ConfigurationDocument doc = ConfigurationDocument.Parse(fixture);

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
        ConfigurationDocument doc = ConfigurationDocument.Parse(fixture);

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
        ConfigurationDocument doc = ConfigurationDocument.Parse(fixture);

        ConfigurationView view = doc.Resolve("Foo.cs", ConfigurationResolveOptions.EditorConfigCompatible);

        Assert.IsNull(view["application:name"]);
    }
}
