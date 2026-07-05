// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.TryGetKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryGetKey(TValue, out TKey)" /> returns the key bound to
    /// an existing value.
    /// </summary>
    [TestMethod]
    public void TryGetKey_WhenValueExists_ShouldReturnTrueAndKey()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool found = dictionary.TryGetKey(3, out string? key);

        Assert.IsTrue(found);
        Assert.AreEqual("c", key);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryGetKey(TValue, out TKey)" /> returns
    /// <see langword="false" /> and the default key for a missing value.
    /// </summary>
    [TestMethod]
    public void TryGetKey_WhenValueMissing_ShouldReturnFalseAndDefault()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool found = dictionary.TryGetKey(99, out string? key);

        Assert.IsFalse(found);
        Assert.IsNull(key);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryGetKey(TValue, out TKey)" /> reflects a re-binding made
    /// through the indexer.
    /// </summary>
    [TestMethod]
    public void TryGetKey_WhenKeyRebound_ShouldReflectNewBinding()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary["a"] = 10;

        Assert.IsFalse(dictionary.TryGetKey(1, out _));
        Assert.IsTrue(dictionary.TryGetKey(10, out string? key));
        Assert.AreEqual("a", key);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryGetKey(TValue, out TKey)" /> throws when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetKey_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryGetKey(null!, out _);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
