// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.Enumeration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView" /> enumerates every resolved entry.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenResolved_ShouldYieldEveryResolvedKey()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*]\na = 1\nb = 2\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        List<KeyValuePair<string, string?>> entries = view.ToList();

        Assert.AreEqual(2, entries.Count);
        Assert.IsTrue(entries.Any(e => e.Key == "a" && e.Value == "1"));
        Assert.IsTrue(entries.Any(e => e.Key == "b" && e.Value == "2"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.Keys" /> exposes every resolved key.
    /// </summary>
    [TestMethod]
    public void Keys_WhenAccessed_ShouldExposeEveryKey()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*]\na = 1\nb = 2\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        List<string> keys = view.Keys.ToList();

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, keys);
    }
}
