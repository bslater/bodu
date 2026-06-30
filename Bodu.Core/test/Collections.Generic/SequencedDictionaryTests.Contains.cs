// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Contains" /> returns <see langword="true" /> when both key and value match.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyAndValueMatch_ShouldReturnTrue()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.Contains(new KeyValuePair<string, int>("b", 2)));
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Contains" /> returns <see langword="false" /> when the key matches but the value differs.
    /// </summary>
    [TestMethod]
    public void Contains_WhenValueDiffers_ShouldReturnFalse()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.Contains(new KeyValuePair<string, int>("b", 999)));
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Contains" /> returns <see langword="false" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyMissing_ShouldReturnFalse()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.Contains(new KeyValuePair<string, int>("missing", 1)));
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Contains" /> does not change the iteration order in access-order mode.
    /// </summary>
    [TestMethod]
    public void Contains_WhenAccessOrder_ShouldNotChangeOrder()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        _ = dictionary.Contains(new KeyValuePair<string, int>("a", 1));

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }
}
