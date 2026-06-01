// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.Unset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationResolverTests
{
    /// <summary>
    /// Verifies that under <see cref="ConfigurationUnsetValueMode.TreatAsLiteral" /> (the Bodu default)
    /// the literal string <c>unset</c> is preserved in the resolved view.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenUnsetModeIsTreatAsLiteral_ShouldPreserveUnsetLiteral()
    {
        IniDocument doc = ConfigurationDocument.Parse(ConfigurationFixtures.UnsetSentinel);

        ConfigurationView view = doc.Resolve("generated/Foo.cs");

        Assert.AreEqual("unset", view.GetString("format:indent:size"));
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationUnsetValueMode.RemoveEffectiveValue" /> the resolved
    /// view omits keys whose value is <c>unset</c>.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenUnsetModeIsRemoveEffectiveValue_ShouldOmitUnsetKey()
    {
        IniDocument doc = ConfigurationDocument.Parse(ConfigurationFixtures.UnsetSentinel);

        ConfigurationResolveOptions options = new()
        {
            UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
        };

        ConfigurationView view = doc.Resolve("generated/Foo.cs", options);

        Assert.IsNull(view["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that under EditorConfig-compatible resolution the <c>unset</c> sentinel removes the
    /// effective value.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenEditorConfigCompatibleAndUnsetSentinel_ShouldRemoveEffectiveValue()
    {
        const string fixture = """
[*.cs]
format.indent.size = 4

[generated/**]
format.indent.size = unset
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new()
        {
            PathRoot = "/project",
            UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
        };

        ConfigurationView view = doc.Resolve("/project/generated/Foo.cs", options);

        Assert.IsNull(view["format:indent:size"]);
    }
}
