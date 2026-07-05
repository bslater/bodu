// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that the indexer getter returns the value bound to an existing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReturnValue()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreEqual(2, dictionary["b"]);
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="KeyNotFoundException" /> for a missing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissing_ShouldThrowKeyNotFoundException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = dictionary["missing"];
        });
    }

    /// <summary>
    /// Verifies that assigning a new key through the indexer adds the pair in both directions.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNew_ShouldAddPair()
    {
        var dictionary = new BiDictionary<string, int>();

        dictionary["a"] = 1;

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.TryGetKey(1, out string? key));
        Assert.AreEqual("a", key);
    }

    /// <summary>
    /// Verifies that re-binding an existing key through the indexer unbinds the key's previous value from the
    /// inverse index.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyRebound_ShouldCleanInverseEntry()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary["a"] = 10;

        Assert.AreEqual(10, dictionary["a"]);
        Assert.IsFalse(dictionary.ContainsValue(1));
        Assert.IsTrue(dictionary.TryGetKey(10, out string? key));
        Assert.AreEqual("a", key);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that assigning a value already bound to a different key throws <see cref="ArgumentException" />
    /// under the <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> policy.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenValueBoundToDifferentKey_ForThrowPolicy_ShouldThrowArgumentException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary["a"] = 2;
        });

        Assert.AreEqual("value", ex.ParamName);
        Assert.AreEqual(1, dictionary["a"]);
        Assert.AreEqual(2, dictionary["b"]);
    }

    /// <summary>
    /// Verifies that assigning a value already bound to a different key evicts the previous binding under the
    /// <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> policy — the old key is removed.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenValueBoundToDifferentKey_ForReplacePolicy_ShouldEvictPreviousBinding()
    {
        var dictionary = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary["a"] = 2;

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("b"));
        Assert.IsFalse(dictionary.ContainsValue(1));
        Assert.AreEqual(2, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that re-assigning a key its own current value is not treated as a conflict under either policy.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenValueReassignedToSameKey_ShouldNotThrow()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary["b"] = 2;

        Assert.AreEqual(3, dictionary.Count);
        Assert.AreEqual(2, dictionary["b"]);
        Assert.IsTrue(dictionary.TryGetKey(2, out string? key));
        Assert.AreEqual("b", key);
    }

    /// <summary>
    /// Verifies that the indexer setter throws when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary[null!] = "x";
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the indexer setter throws when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary["x"] = null!;
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
