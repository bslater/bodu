// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedDictionaryTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LinkedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="LinkedDictionary{TKey, TValue}.ContainsKey" /> returns <see langword="true" /> for an existing key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyExists_ShouldReturnTrue()
    {
        var dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.ContainsKey("b"));
    }

    /// <summary>
    /// Verifies that <see cref="LinkedDictionary{TKey, TValue}.ContainsKey" /> returns <see langword="false" /> for a missing key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyMissing_ShouldReturnFalse()
    {
        var dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="LinkedDictionary{TKey, TValue}.ContainsKey" /> does not reposition the entry even in access-order mode.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenAccessOrder_ShouldNotChangeOrder()
    {
        var dictionary = new LinkedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        _ = dictionary.ContainsKey("a");

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }
}
