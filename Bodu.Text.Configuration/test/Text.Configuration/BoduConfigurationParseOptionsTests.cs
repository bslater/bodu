// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseOptionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationParseOptions" /> defaults and per-profile presets.
/// </summary>
[TestClass]
public partial class BoduConfigurationParseOptionsTests
{
    /// <summary>
    /// Verifies that the default-constructed parse options exposes the documented defaults.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenAccessed_ShouldExposeDocumentedValues()
    {
        BoduConfigurationParseOptions options = new();

        Assert.AreEqual(BoduConfigurationProfile.Bodu, options.Profile);
        Assert.AreEqual(BoduConfigurationInlineCommentMode.WhitespaceIntroduced, options.InlineCommentMode);
        Assert.AreEqual(IniDuplicateKeyBehavior.LastWins, options.DuplicateKeyMode);
        Assert.AreEqual(BoduConfigurationDiagnosticMode.Throw, options.DiagnosticMode);
        Assert.IsTrue(options.TrimKeysAndValues);
        Assert.IsFalse(options.AllowKeyOnlyProperties);
    }
}
