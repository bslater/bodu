// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.Enumeration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView" /> enumerates every resolved entry.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenResolved_ShouldYieldEveryResolvedKey()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\na = 1\nb = 2\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        var entries = view.ToList();

        Assert.HasCount(2, entries);
        Assert.Contains(e => e.Key == "a" && e.Value == "1", entries);
        Assert.Contains(e => e.Key == "b" && e.Value == "2", entries);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.Keys" /> exposes every resolved key.
    /// </summary>
    [TestMethod]
    public void Keys_WhenAccessed_ShouldExposeEveryKey()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\na = 1\nb = 2\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        var keys = view.Keys.ToList();

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, keys);
    }
}
