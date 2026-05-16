// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriteOptionsTests.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationWriteOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationWriteOptions.Bodu" /> enables inline comment emission.
    /// </summary>
    [TestMethod]
    public void Bodu_WhenAccessed_ShouldEnableInlineComments()
    {
        Assert.IsTrue(BoduConfigurationWriteOptions.Bodu.WriteInlineComments);
        Assert.AreEqual(BoduConfigurationProfile.Bodu, BoduConfigurationWriteOptions.Bodu.Profile);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationWriteOptions.EditorConfigCompatible" /> disables inline
    /// comment emission to match EditorConfig 0.17.2 semantics.
    /// </summary>
    [TestMethod]
    public void EditorConfigCompatible_WhenAccessed_ShouldDisableInlineComments()
    {
        Assert.IsFalse(BoduConfigurationWriteOptions.EditorConfigCompatible.WriteInlineComments);
        Assert.AreEqual(BoduConfigurationProfile.EditorConfigCompatible, BoduConfigurationWriteOptions.EditorConfigCompatible.Profile);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationWriteOptions.For(BoduConfigurationProfile)" /> rejects
    /// undefined enum values.
    /// </summary>
    [TestMethod]
    public void For_WhenProfileIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BoduConfigurationWriteOptions.For((BoduConfigurationProfile)42);
        });
    }
}
