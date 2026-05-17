// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.GetBoolean.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationViewTests
{
    /// <summary>
    /// Verifies that <c>true</c> and <c>false</c> string values are parsed case-insensitively.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenValueIsTrueOrFalse_ShouldReturnParsedValue()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\nflag = TRUE\nother = False\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.IsTrue(view.GetBoolean("flag"));
        Assert.IsFalse(view.GetBoolean("other"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetBoolean(string)" /> throws on a malformed value.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenValueIsMalformed_ShouldThrowExactly()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\nflag = sometimes\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetBoolean("flag"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetBoolean(string, bool)" /> returns the fallback only
    /// for missing keys.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenKeyMissingAndFallbackProvided_ShouldReturnFallback()
    {
        IniDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.IsTrue(view.GetBoolean("missing", true));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.TryGetBoolean(string, out bool)" /> reports failure
    /// without throwing for malformed or missing values.
    /// </summary>
    [TestMethod]
    public void TryGetBoolean_WhenKeyMissingOrMalformed_ShouldReturnFalse()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\nbad = sometimes\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.IsFalse(view.TryGetBoolean("missing", out _));
        Assert.IsFalse(view.TryGetBoolean("bad", out _));
    }
}
