// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.Nulls.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.GetOrAddSection(string)" /> rejects a
    /// <see langword="null" /> pattern.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_WhenPatternIsNull_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = doc.GetOrAddSection(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.GetOrAddSection(string)" /> rejects a blank pattern.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_WhenPatternIsBlank_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = doc.GetOrAddSection("   ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.AddSection(BoduConfigurationSection)" /> rejects a
    /// <see langword="null" /> section.
    /// </summary>
    [TestMethod]
    public void AddSection_WhenSectionIsNull_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            doc.AddSection(null!);
        });
    }
}
