// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.GetInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetInt64(string)" /> returns the parsed value for a well-formed
    /// integer.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenKeyExistsWithValidInteger_ShouldReturnParsedValue()
    {
        var doc = ConfigurationDocument.Parse("[*]\nsize = 9000000000\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(9_000_000_000L, view.GetInt64("size"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetInt64(string, long)" /> returns the fallback only when the key is
    /// absent; a malformed present value still throws.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenKeyMissingAndFallbackProvided_ShouldReturnFallback()
    {
        IniDocument doc = new();
        ConfigurationView view = doc.Resolve();

        Assert.AreEqual(7L, view.GetInt64("missing", 7L));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetInt64(string, long)" /> still throws
    /// <see cref="FormatException" /> when the key is present but malformed, even with a fallback.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenPresentButMalformedAndFallbackProvided_ShouldThrowExactly()
    {
        var doc = ConfigurationDocument.Parse("[*]\nsize = notanumber\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetInt64("size", 0L));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetInt64(string, out long)" /> reports failure for missing or
    /// malformed values without throwing, and success for a well-formed value.
    /// </summary>
    [TestMethod]
    public void TryGetInt64_WhenMissingOrMalformed_ShouldReturnFalseOtherwiseTrue()
    {
        var doc = ConfigurationDocument.Parse("[*]\nsize = 42\nbad = abc\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsTrue(view.TryGetInt64("size", out long value));
        Assert.AreEqual(42L, value);
        Assert.IsFalse(view.TryGetInt64("missing", out _));
        Assert.IsFalse(view.TryGetInt64("bad", out _));
    }
}
