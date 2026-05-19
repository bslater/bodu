// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.GetString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetString(string)" /> returns the value when the key
    /// is present.
    /// </summary>
    [TestMethod]
    public void GetString_WhenKeyExists_ShouldReturnValue()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\na = hello\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual("hello", view.GetString("a"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetString(string)" /> throws
    /// <see cref="KeyNotFoundException" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetString_WhenKeyIsMissing_ShouldThrowExactly()
    {
        IniDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = view.GetString("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetString(string, string?)" /> returns the fallback
    /// when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetString_WhenKeyIsMissingAndFallbackProvided_ShouldReturnFallback()
    {
        IniDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.AreEqual("default", view.GetString("missing", "default"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.TryGetString(string, out string?)" /> returns
    /// <see langword="false" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void TryGetString_WhenKeyIsMissing_ShouldReturnFalse()
    {
        IniDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.IsFalse(view.TryGetString("missing", out var value));
        Assert.IsNull(value);
    }
}
