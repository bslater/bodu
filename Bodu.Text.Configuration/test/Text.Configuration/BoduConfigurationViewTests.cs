// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the resolved <see cref="BoduConfigurationView" /> snapshot.
/// </summary>
[TestClass]
public partial class BoduConfigurationViewTests
{
    /// <summary>
    /// Verifies that the indexer returns the resolved value for present keys.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReturnValue()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\na = 1\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual("1", view["a"]);
    }

    /// <summary>
    /// Verifies that the indexer returns <see langword="null" /> for absent keys.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsMissing_ShouldReturnNull()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\na = 1\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.IsNull(view["missing"]);
    }

    /// <summary>
    /// Verifies that the indexer rejects a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowExactly()
    {
        IniDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = view[null!]);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.Count" /> reports the number of resolved keys.
    /// </summary>
    [TestMethod]
    public void Count_WhenAccessed_ShouldReturnResolvedKeyCount()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\na = 1\nb = 2\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(2, view.Count);
    }
}
