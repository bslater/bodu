// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationWriteOptionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationWriteOptions" /> defaults and per-profile presets.
/// </summary>
[TestClass]
public partial class ConfigurationWriteOptionsTests
{
    /// <summary>
    /// Verifies that the default-constructed write options exposes the documented defaults.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenAccessed_ShouldExposeDocumentedValues()
    {
        ConfigurationWriteOptions options = new();

        Assert.AreEqual("\n", options.NewLine);
        Assert.AreEqual(" = ", options.KeyValueSeparator);
        Assert.AreEqual('#', options.CommentPrefix);
        Assert.IsTrue(options.PreserveComments);
        Assert.IsTrue(options.WriteInlineComments);
        Assert.IsTrue(options.InsertBlankLineBetweenSections);
    }
}
