// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class LinkedDictionaryTests
{
    /// <summary>
    /// Creates a populated insertion-order dictionary with the keys <c>"a"</c>, <c>"b"</c>, <c>"c"</c> mapped to
    /// <c>1</c>, <c>2</c>, <c>3</c>.
    /// </summary>
    /// <returns>A populated <see cref="LinkedDictionary{TKey, TValue}" />.</returns>
    private static LinkedDictionary<string, int> CreatePopulated() =>
        new() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };

    /// <summary>
    /// Verifies that a freshly constructed dictionary preserves insertion order when enumerated and exposes the added
    /// entries.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void LinkedDictionary_WhenEntriesAdded_ShouldEnumerateInInsertionOrder()
    {
        var dictionary = new LinkedDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
        Assert.AreEqual(2, dictionary["b"]);
        Assert.AreEqual(3, dictionary.Count);
    }
}
