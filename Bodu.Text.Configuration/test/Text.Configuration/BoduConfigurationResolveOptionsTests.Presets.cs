// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolveOptionsTests.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationResolveOptionsTests
{
    /// <summary>
    /// Verifies that the Bodu preset applies preamble properties and uses the empty-root fallback.
    /// </summary>
    [TestMethod]
    public void Bodu_WhenAccessed_ShouldApplyPreambleAndUseEmptyRootFallback()
    {
        BoduConfigurationResolveOptions options = BoduConfigurationResolveOptions.Bodu;

        Assert.AreEqual(BoduConfigurationProfile.Bodu, options.Profile);
        Assert.IsTrue(options.ApplyPreambleProperties);
        Assert.AreEqual(BoduConfigurationMissingPathRootMode.UseEmptyRoot, options.MissingPathRootMode);
        Assert.AreEqual(BoduConfigurationUnsetValueMode.TreatAsLiteral, options.UnsetValueMode);
    }

    /// <summary>
    /// Verifies that the EditorConfig-compatible preset skips non-root preamble values, requires a path
    /// root, and honours the <c>unset</c> sentinel.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldFollowEditorConfigSemantics()
    {
        BoduConfigurationResolveOptions options = BoduConfigurationResolveOptions.EditorConfigCompatible;

        Assert.AreEqual(BoduConfigurationProfile.EditorConfigCompatible, options.Profile);
        Assert.IsFalse(options.ApplyPreambleProperties);
        Assert.AreEqual(BoduConfigurationMissingPathRootMode.Throw, options.MissingPathRootMode);
        Assert.AreEqual(BoduConfigurationUnsetValueMode.RemoveEffectiveValue, options.UnsetValueMode);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationResolveOptions.For(BoduConfigurationProfile)" /> rejects
    /// undefined enum values.
    /// </summary>
    [TestMethod]
    public void For_WhenProfileIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BoduConfigurationResolveOptions.For((BoduConfigurationProfile)42);
        });
    }
}
