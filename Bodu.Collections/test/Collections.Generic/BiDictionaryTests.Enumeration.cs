// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that enumeration yields every key/value pair currently in the dictionary.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenPopulated_ShouldYieldAllPairs()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        var pairs = dictionary.OrderBy(kvp => kvp.Key).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3),
            },
            pairs);
    }

    /// <summary>
    /// Verifies that enumeration of an empty dictionary yields no pairs.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenEmpty_ShouldYieldNothing()
    {
        var dictionary = new BiDictionary<string, int>();

        KeyValuePair<string, int>[] pairs = [.. dictionary];

        Assert.AreEqual(0, pairs.Length);
    }

    /// <summary>
    /// Verifies that adding an entry during enumeration invalidates the enumerator, which then throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenAddedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in dictionary)
                dictionary.Add("d", 4);
        });
    }

    /// <summary>
    /// Verifies that adding through the <see cref="BiDictionary{TKey, TValue}.Inverse" /> view during enumeration of
    /// the original invalidates the enumerator, which then throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenAddedThroughInverseDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in dictionary)
                dictionary.Inverse.Add(4, "d");
        });
    }
}
