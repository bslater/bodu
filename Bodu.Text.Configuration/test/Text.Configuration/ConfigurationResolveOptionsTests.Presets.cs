// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolveOptionsTests.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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

    /// <summary>
    /// Verifies that the EditorConfig-compatible preset maps keys with <see cref="ConfigurationKeyMapping.Identity" />,
    /// so an EditorConfig key keeps its dots, both through the cached preset and through
    /// <see cref="ConfigurationResolveOptions.For(ConfigurationProfile)" />.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldUseIdentityKeyMapping()
    {
        Assert.AreEqual(ConfigurationKeyMapping.Identity, ConfigurationResolveOptions.EditorConfigCompatible.KeyOptions.Mapping);
        Assert.AreEqual(
            ConfigurationKeyMapping.Identity,
            ConfigurationResolveOptions.For(ConfigurationProfile.EditorConfigCompatible).KeyOptions.Mapping);
    }

    /// <summary>
    /// Verifies that every preset other than the EditorConfig-compatible one keeps the dotted-to-colon key mapping.
    /// </summary>
    /// <param name="profile">The profile whose preset is checked.</param>
    [TestMethod]
    [DataRow(ConfigurationProfile.Bodu)]
    [DataRow(ConfigurationProfile.Strict)]
    [DataRow(ConfigurationProfile.Relaxed)]
    public void For_WhenProfileIsNotEditorConfigCompatible_ShouldMapDotsToColons(ConfigurationProfile profile)
    {
        Assert.AreEqual(ConfigurationKeyMapping.DotToColon, ConfigurationResolveOptions.For(profile).KeyOptions.Mapping);
    }

    /// <summary>
    /// Verifies that the EditorConfig-compatible preset lowercases keys, as EditorConfig does after parsing, and that
    /// the other presets keep keys as written.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldLowercaseKeys()
    {
        Assert.IsTrue(ConfigurationResolveOptions.EditorConfigCompatible.KeyOptions.LowercaseKeys);
        Assert.IsFalse(ConfigurationResolveOptions.Bodu.KeyOptions.LowercaseKeys);
        Assert.IsFalse(ConfigurationResolveOptions.For(ConfigurationProfile.Strict).KeyOptions.LowercaseKeys);
        Assert.IsFalse(ConfigurationResolveOptions.For(ConfigurationProfile.Relaxed).KeyOptions.LowercaseKeys);
    }
}
