// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Enumerator.VersionCheck.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that mutating the dictionary during enumeration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDictionaryMutatedDuringEnumeration_ShouldThrowExactly()
    {
        var dictionary = CreatePopulated();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in dictionary)
                dictionary.Add("d", 4);
        });
    }

    /// <summary>
    /// Verifies that, in access-order mode, reading an entry during enumeration invalidates the enumerator because the read repositions the entry.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAccessOrderReadDuringEnumeration_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> entry in dictionary)
                _ = dictionary[entry.Key];
        });
    }

    /// <summary>
    /// Verifies that enumerating without mutation completes and yields every entry in iteration order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenNotMutated_ShouldYieldAllEntries()
    {
        var dictionary = CreatePopulated();

        var seen = new List<string>();
        foreach (KeyValuePair<string, int> kvp in dictionary)
            seen.Add(kvp.Key);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, seen);
    }
}
