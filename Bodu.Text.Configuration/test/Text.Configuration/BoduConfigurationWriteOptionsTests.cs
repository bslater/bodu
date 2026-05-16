// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriteOptionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationWriteOptions" /> defaults and per-profile presets.
/// </summary>
[TestClass]
public partial class BoduConfigurationWriteOptionsTests
{
    /// <summary>
    /// Verifies that the default-constructed write options exposes the documented defaults.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenAccessed_ShouldExposeDocumentedValues()
    {
        BoduConfigurationWriteOptions options = new();

        Assert.AreEqual("\n", options.NewLine);
        Assert.AreEqual(" = ", options.KeyValueSeparator);
        Assert.AreEqual('#', options.CommentPrefix);
        Assert.IsTrue(options.PreserveComments);
        Assert.IsTrue(options.WriteInlineComments);
        Assert.IsTrue(options.InsertBlankLineBetweenSections);
    }
}
