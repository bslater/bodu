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

    /// <summary>
    /// Verifies that enumerating an empty dictionary yields no elements.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEmpty_ShouldYieldNoElements()
    {
        var dictionary = new SequencedDictionary<string, int>();

        using IEnumerator<KeyValuePair<string, int>> enumerator = dictionary.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the non-generic dictionary enumerator yields no elements over an empty dictionary.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEmptyNonGeneric_ShouldYieldNoElements()
    {
        System.Collections.IDictionary dictionary = new SequencedDictionary<string, int>();

        System.Collections.IDictionaryEnumerator enumerator = dictionary.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the dictionary enumerator reports no further elements once iteration is exhausted.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenExhausted_ShouldReturnFalseFromMoveNext()
    {
        var dictionary = CreatePopulated();

        using IEnumerator<KeyValuePair<string, int>> enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
        {
            // Drain the enumerator.
        }

        Assert.IsFalse(enumerator.MoveNext());
    }
}
