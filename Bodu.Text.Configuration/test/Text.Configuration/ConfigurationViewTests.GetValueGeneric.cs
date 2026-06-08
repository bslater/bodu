// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.GetValueGeneric.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetValue{T}(string)" /> parses any
    /// <see cref="System.ISpanParsable{TSelf}" /> type using invariant culture, mirroring the
    /// <c>IniSection.GetValue&lt;T&gt;</c> API from <c>Bodu.Text.Ini</c>.
    /// </summary>
    [TestMethod]
    public void GetValueGeneric_WhenKeyExistsForIntegerType_ShouldReturnParsedValue()
    {
        var doc = ConfigurationDocument.Parse("[*]\nsize = 42\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(42, view.GetValue<int>("size"));
        Assert.AreEqual(42L, view.GetValue<long>("size"));
        Assert.AreEqual(42.0, view.GetValue<double>("size"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetValue{T}(string)" /> throws
    /// <see cref="KeyNotFoundException" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetValueGeneric_WhenKeyIsMissing_ShouldThrowExactly()
    {
        IniDocument doc = new();
        ConfigurationView view = doc.Resolve();

        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = view.GetValue<int>("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetValue{T}(string)" /> throws
    /// <see cref="FormatException" /> when the value cannot be parsed.
    /// </summary>
    [TestMethod]
    public void GetValueGeneric_WhenValueIsMalformed_ShouldThrowExactly()
    {
        var doc = ConfigurationDocument.Parse("[*]\nsize = notanumber\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetValue<int>("size"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetValue{T}(string, out T)" /> reports failure
    /// without throwing for missing or malformed values.
    /// </summary>
    [TestMethod]
    public void TryGetValueGeneric_WhenKeyMissingOrMalformed_ShouldReturnFalse()
    {
        var doc = ConfigurationDocument.Parse("[*]\nbad = abc\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsFalse(view.TryGetValue("missing", out int _));
        Assert.IsFalse(view.TryGetValue("bad", out int _));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetValue{T}(string)" /> normalizes dotted keys to
    /// colon-delimited form, matching the indexer's lookup behaviour.
    /// </summary>
    [TestMethod]
    public void GetValueGeneric_WhenKeyUsesDottedForm_ShouldResolveToSameValue()
    {
        var doc = ConfigurationDocument.Parse("[*]\nlogging.level.default = 7\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(7, view.GetValue<int>("logging.level.default"));
        Assert.AreEqual(7, view.GetValue<int>("logging:level:default"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetValue{T}(string, T)" /> returns the fallback only when the key is
    /// absent; a malformed present value still throws.
    /// </summary>
    [TestMethod]
    public void GetValueGeneric_WhenKeyMissingAndFallbackProvided_ShouldReturnFallback()
    {
        var doc = ConfigurationDocument.Parse("[*]\nsize = 42\nbad = abc\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(99, view.GetValue("missing", 99));
        Assert.AreEqual(42, view.GetValue("size", 99));
        Assert.ThrowsExactly<FormatException>(() => _ = view.GetValue("bad", 0));
    }
}
