// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.GetBoolean.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    /// <summary>
    /// Verifies that <c>true</c> and <c>false</c> string values are parsed case-insensitively.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenValueIsTrueOrFalse_ShouldReturnParsedValue()
    {
        var doc = ConfigurationDocument.Parse("[*]\nflag = TRUE\nother = False\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsTrue(view.GetBoolean("flag"));
        Assert.IsFalse(view.GetBoolean("other"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetBoolean(string)" /> throws on a malformed value.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenValueIsMalformed_ShouldThrowExactly()
    {
        var doc = ConfigurationDocument.Parse("[*]\nflag = sometimes\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetBoolean("flag"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetBoolean(string, bool)" /> returns the fallback only
    /// for missing keys.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenKeyMissingAndFallbackProvided_ShouldReturnFallback()
    {
        IniDocument doc = new();
        ConfigurationView view = doc.Resolve();

        Assert.IsTrue(view.GetBoolean("missing", true));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetBoolean(string, out bool)" /> reports failure
    /// without throwing for malformed or missing values.
    /// </summary>
    [TestMethod]
    public void TryGetBoolean_WhenKeyMissingOrMalformed_ShouldReturnFalse()
    {
        var doc = ConfigurationDocument.Parse("[*]\nbad = sometimes\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsFalse(view.TryGetBoolean("missing", out _));
        Assert.IsFalse(view.TryGetBoolean("bad", out _));
    }
}
