// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the resolved <see cref="ConfigurationView" /> snapshot.
/// </summary>
[TestClass]
public partial class ConfigurationViewTests
{
    /// <summary>
    /// Verifies that the indexer returns the resolved value for present keys.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReturnValue()
    {
        var doc = ConfigurationDocument.Parse("[*]\na = 1\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual("1", view["a"]);
    }

    /// <summary>
    /// Verifies that the indexer returns <see langword="null" /> for absent keys.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsMissing_ShouldReturnNull()
    {
        var doc = ConfigurationDocument.Parse("[*]\na = 1\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsNull(view["missing"]);
    }

    /// <summary>
    /// Verifies that the indexer rejects a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowExactly()
    {
        IniDocument doc = new();
        ConfigurationView view = doc.Resolve();

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = view[null!]);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.Count" /> reports the number of resolved keys.
    /// </summary>
    [TestMethod]
    public void Count_WhenAccessed_ShouldReturnResolvedKeyCount()
    {
        var doc = ConfigurationDocument.Parse("[*]\na = 1\nb = 2\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(2, view.Count);
    }
}
