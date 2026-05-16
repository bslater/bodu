// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseOptionsTests.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationParseOptionsTests
{
    /// <summary>
    /// Verifies that the EditorConfig-compatible preset disables inline comments.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldDisableInlineComments()
    {
        BoduConfigurationParseOptions options = BoduConfigurationParseOptions.EditorConfigCompatible;

        Assert.AreEqual(BoduConfigurationProfile.EditorConfigCompatible, options.Profile);
        Assert.AreEqual(BoduConfigurationInlineCommentMode.Disabled, options.InlineCommentMode);
    }

    /// <summary>
    /// Verifies that the Strict preset rejects duplicate keys and duplicate section headers.
    /// </summary>
    [TestMethod]
    public void Strict_WhenAccessed_ShouldRejectDuplicates()
    {
        BoduConfigurationParseOptions options = BoduConfigurationParseOptions.Strict;

        Assert.AreEqual(BoduConfigurationDuplicateKeyMode.Reject, options.DuplicateKeyMode);
        Assert.AreEqual(BoduConfigurationDuplicateSectionMode.Reject, options.DuplicateSectionMode);
    }

    /// <summary>
    /// Verifies that the Relaxed preset collects diagnostics rather than throwing.
    /// </summary>
    [TestMethod]
    public void Relaxed_WhenAccessed_ShouldCollectDiagnostics()
    {
        BoduConfigurationParseOptions options = BoduConfigurationParseOptions.Relaxed;

        Assert.AreEqual(BoduConfigurationDiagnosticMode.Collect, options.DiagnosticMode);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationParseOptions.For(BoduConfigurationProfile)" /> rejects
    /// undefined enum values.
    /// </summary>
    [TestMethod]
    public void For_WhenProfileIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BoduConfigurationParseOptions.For((BoduConfigurationProfile)42);
        });
    }
}
