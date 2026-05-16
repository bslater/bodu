// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.GetString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

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
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*]\na = hello\n");
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
        BoduConfigurationDocument doc = new();
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
        BoduConfigurationDocument doc = new();
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
        BoduConfigurationDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.IsFalse(view.TryGetString("missing", out string? value));
        Assert.IsNull(value);
    }
}
