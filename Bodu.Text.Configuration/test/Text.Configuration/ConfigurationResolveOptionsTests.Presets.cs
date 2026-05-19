// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolveOptionsTests.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationResolveOptionsTests
{
    /// <summary>
    /// Verifies that the Bodu preset applies preamble properties and uses the empty-root fallback.
    /// </summary>
    [TestMethod]
    public void Bodu_WhenAccessed_ShouldApplyPreambleAndUseEmptyRootFallback()
    {
        ConfigurationResolveOptions options = ConfigurationResolveOptions.Bodu;

        Assert.AreEqual(ConfigurationProfile.Bodu, options.Profile);
        Assert.IsTrue(options.ApplyPreambleProperties);
        Assert.AreEqual(ConfigurationMissingPathRootMode.UseEmptyRoot, options.MissingPathRootMode);
        Assert.AreEqual(ConfigurationUnsetValueMode.TreatAsLiteral, options.UnsetValueMode);
    }

    /// <summary>
    /// Verifies that the EditorConfig-compatible preset skips non-root preamble values, requires a path
    /// root, and honours the <c>unset</c> sentinel.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldFollowEditorConfigSemantics()
    {
        ConfigurationResolveOptions options = ConfigurationResolveOptions.EditorConfigCompatible;

        Assert.AreEqual(ConfigurationProfile.EditorConfigCompatible, options.Profile);
        Assert.IsFalse(options.ApplyPreambleProperties);
        Assert.AreEqual(ConfigurationMissingPathRootMode.Throw, options.MissingPathRootMode);
        Assert.AreEqual(ConfigurationUnsetValueMode.RemoveEffectiveValue, options.UnsetValueMode);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationResolveOptions.For(ConfigurationProfile)" /> rejects
    /// undefined enum values.
    /// </summary>
    [TestMethod]
    public void For_WhenProfileIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ConfigurationResolveOptions.For((ConfigurationProfile)42);
        });
    }
}
