// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseOptionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationParseOptions" /> defaults and per-profile presets.
/// </summary>
[TestClass]
public partial class ConfigurationParseOptionsTests
{
    /// <summary>
    /// Verifies that the default-constructed parse options exposes the documented defaults.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenAccessed_ShouldExposeDocumentedValues()
    {
        ConfigurationParseOptions options = new();

        Assert.AreEqual(ConfigurationProfile.Bodu, options.Profile);
        Assert.AreEqual(ConfigurationInlineCommentMode.WhitespaceIntroduced, options.InlineCommentMode);
        Assert.AreEqual(IniDuplicateKeyBehavior.LastWins, options.DuplicateKeyMode);
        Assert.AreEqual(ConfigurationDiagnosticMode.Throw, options.DiagnosticMode);
        Assert.IsTrue(options.TrimKeysAndValues);
        Assert.IsFalse(options.AllowKeyOnlyProperties);
    }
}
