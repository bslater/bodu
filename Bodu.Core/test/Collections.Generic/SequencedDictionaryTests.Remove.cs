// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that removing an existing key removes it from both the store and the iteration order, and returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldRemoveAndReturnTrue()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.Remove("b");

        Assert.IsTrue(removed);
        Assert.IsFalse(dictionary.ContainsKey("b"));
        CollectionAssert.AreEqual(new[] { "a", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that removing a missing key leaves the dictionary unchanged and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyMissing_ShouldReturnFalse()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.Remove("missing");

        Assert.IsFalse(removed);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that removing a key/value pair only succeeds when both key and value match.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPairValueDiffers_ShouldReturnFalse()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.Remove(new KeyValuePair<string, int>("a", 999));

        Assert.IsFalse(removed);
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that re-adding a removed key appends it to the end of the iteration order.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyReadded_ShouldAppendAtEnd()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Remove("a");
        dictionary.Add("a", 1);

        CollectionAssert.AreEqual(new[] { "b", "c", "a" }, dictionary.Keys.ToArray());
    }
}
