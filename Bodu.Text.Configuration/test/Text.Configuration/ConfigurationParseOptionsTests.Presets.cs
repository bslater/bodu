// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseOptionsTests.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationParseOptionsTests
{
    /// <summary>
    /// Verifies that the EditorConfig-compatible preset disables inline comments.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldDisableInlineComments()
    {
        ConfigurationParseOptions options = ConfigurationParseOptions.EditorConfigCompatible;

        Assert.AreEqual(ConfigurationProfile.EditorConfigCompatible, options.Profile);
        Assert.AreEqual(ConfigurationInlineCommentMode.Disabled, options.InlineCommentMode);
    }

    /// <summary>
    /// Verifies that the Strict preset rejects duplicate keys and duplicate section headers.
    /// </summary>
    [TestMethod]
    public void Strict_WhenAccessed_ShouldRejectDuplicates()
    {
        ConfigurationParseOptions options = ConfigurationParseOptions.Strict;

        Assert.AreEqual(IniDuplicateKeyBehavior.Disallowed, options.DuplicateKeyMode);
        Assert.AreEqual(IniDuplicateSectionBehavior.Disallowed, options.DuplicateSectionMode);
    }

    /// <summary>
    /// Verifies that the Relaxed preset collects diagnostics rather than throwing.
    /// </summary>
    [TestMethod]
    public void Relaxed_WhenAccessed_ShouldCollectDiagnostics()
    {
        ConfigurationParseOptions options = ConfigurationParseOptions.Relaxed;

        Assert.AreEqual(ConfigurationDiagnosticMode.Collect, options.DiagnosticMode);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationParseOptions.For(ConfigurationProfile)" /> rejects
    /// undefined enum values.
    /// </summary>
    [TestMethod]
    public void For_WhenProfileIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ConfigurationParseOptions.For((ConfigurationProfile)42);
        });
    }
}
