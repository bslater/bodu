// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationWriteOptionsTests.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationWriteOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationWriteOptions.Bodu" /> enables inline comment emission.
    /// </summary>
    [TestMethod]
    public void Bodu_WhenAccessed_ShouldEnableInlineComments()
    {
        Assert.IsTrue(ConfigurationWriteOptions.Bodu.WriteInlineComments);
        Assert.AreEqual(ConfigurationProfile.Bodu, ConfigurationWriteOptions.Bodu.Profile);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationWriteOptions.EditorConfigCompatible" /> disables inline
    /// comment emission to match EditorConfig 0.17.2 semantics.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldDisableInlineComments()
    {
        Assert.IsFalse(ConfigurationWriteOptions.EditorConfigCompatible.WriteInlineComments);
        Assert.AreEqual(ConfigurationProfile.EditorConfigCompatible, ConfigurationWriteOptions.EditorConfigCompatible.Profile);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationWriteOptions.For(ConfigurationProfile)" /> rejects
    /// undefined enum values.
    /// </summary>
    [TestMethod]
    public void For_WhenProfileIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ConfigurationWriteOptions.For((ConfigurationProfile)42);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationWriteOptions.For(ConfigurationProfile)" /> maps the <c>Relaxed</c> profile
    /// to inline comments enabled and comment preservation enabled.
    /// </summary>
    [TestMethod]
    public void For_WhenRelaxedProfile_ShouldEnableInlineCommentsAndPreserveComments()
    {
        ConfigurationWriteOptions options = ConfigurationWriteOptions.For(ConfigurationProfile.Relaxed);

        Assert.AreEqual(
            (ConfigurationProfile.Relaxed, true, true),
            (options.Profile, options.WriteInlineComments, options.PreserveComments));
    }
}
