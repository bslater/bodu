// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that the indexer getter returns the value associated with an existing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReturnValue()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreEqual(2, dictionary["b"]);
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="KeyNotFoundException" /> for a missing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissing_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = dictionary["missing"];
        });
    }

    /// <summary>
    /// Verifies that assigning a new key through the indexer appends the entry to the end of the iteration order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssigningNewKey_ShouldAppendToEnd()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        dictionary["d"] = 4;

        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that assigning an existing key through the indexer updates the value without changing insertion order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssigningExistingKey_ForInsertionOrder_ShouldUpdateValueInPlace()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        dictionary["a"] = 99;

        Assert.AreEqual(99, dictionary["a"]);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that the indexer setter throws when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssigningNullKey_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary[null!] = 1;
        });

        Assert.AreEqual("key", ex.ParamName);
    }
}
