// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.GetInt32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetInt32(string)" /> returns the parsed value for a
    /// well-formed integer.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenKeyExistsWithValidInteger_ShouldReturnParsedValue()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*]\nsize = 42\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(42, view.GetInt32("size"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetInt32(string)" /> throws
    /// <see cref="KeyNotFoundException" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenKeyIsMissing_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = view.GetInt32("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetInt32(string)" /> throws
    /// <see cref="FormatException" /> when the value is present but malformed.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenValueIsMalformed_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*]\nsize = notanumber\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetInt32("size"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetInt32(string, int)" /> returns the fallback only
    /// when the key is absent; a malformed present value still throws.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenKeyMissingAndFallbackProvided_ShouldReturnFallback()
    {
        BoduConfigurationDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.AreEqual(7, view.GetInt32("missing", 7));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetInt32(string, int)" /> still throws
    /// <see cref="FormatException" /> when the key is present but malformed, even with a fallback.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenPresentButMalformedAndFallbackProvided_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*]\nsize = notanumber\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetInt32("size", 0));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.TryGetInt32(string, out int)" /> reports failure for
    /// missing or malformed values without throwing.
    /// </summary>
    [TestMethod]
    public void TryGetInt32_WhenKeyMissingOrMalformed_ShouldReturnFalse()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*]\nbad = abc\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.IsFalse(view.TryGetInt32("missing", out int _));
        Assert.IsFalse(view.TryGetInt32("bad", out int _));
    }
}
