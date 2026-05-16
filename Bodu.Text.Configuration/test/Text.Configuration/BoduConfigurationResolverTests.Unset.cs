// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolverTests.Unset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationResolverTests
{
    /// <summary>
    /// Verifies that under <see cref="BoduConfigurationUnsetValueMode.TreatAsLiteral" /> (the Bodu default)
    /// the literal string <c>unset</c> is preserved in the resolved view.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenUnsetModeIsTreatAsLiteral_ShouldPreserveUnsetLiteral()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.UnsetSentinel);

        BoduConfigurationView view = doc.Resolve("generated/Foo.cs");

        Assert.AreEqual("unset", view.GetString("format:indent:size"));
    }

    /// <summary>
    /// Verifies that under <see cref="BoduConfigurationUnsetValueMode.RemoveEffectiveValue" /> the resolved
    /// view omits keys whose value is <c>unset</c>.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenUnsetModeIsRemoveEffectiveValue_ShouldOmitUnsetKey()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.UnsetSentinel);

        BoduConfigurationResolveOptions options = new()
        {
            UnsetValueMode = BoduConfigurationUnsetValueMode.RemoveEffectiveValue,
        };

        BoduConfigurationView view = doc.Resolve("generated/Foo.cs", options);

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
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(fixture);

        BoduConfigurationResolveOptions options = new()
        {
            PathRoot = "/project",
            UnsetValueMode = BoduConfigurationUnsetValueMode.RemoveEffectiveValue,
        };

        BoduConfigurationView view = doc.Resolve("/project/generated/Foo.cs", options);

        Assert.IsNull(view["format:indent:size"]);
    }
}
