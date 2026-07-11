// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the indexer getter returns the stored value for an existing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReturnValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 42);

        Assert.AreEqual(42, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="KeyNotFoundException" /> for a missing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissing_ShouldThrowKeyNotFoundException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = dictionary["missing"];
        });

        Assert.IsTrue(ex.Message.Contains("missing", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the indexer setter adds a new entry when the key is absent.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetForNewKey_ShouldAddEntry()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        dictionary["a"] = 1;

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the indexer setter replaces the value for an existing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetForExistingKey_ShouldReplaceValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary["a"] = 1;

        dictionary["a"] = 2;

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(2, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the indexer getter counts as a policy access, repositioning the key under the LeastRecentlyUsed
    /// policy.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenReadForLeastRecentlyUsed_ShouldRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        _ = dictionary["a"];

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "Reading 'a' through the indexer should have made 'b' the eviction candidate.");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key on either accessor throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary[null!];
        });

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary[null!] = 1;
        });
    }
}
