// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryGetValue" /> returns the value and <see langword="true" /> for an existing key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExists_ShouldReturnTrueAndValue()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        bool found = dictionary.TryGetValue("b", out int value);

        Assert.IsTrue(found);
        Assert.AreEqual(2, value);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryGetValue" /> returns <see langword="false" /> and the default value for a missing key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissing_ShouldReturnFalseAndDefault()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        bool found = dictionary.TryGetValue("missing", out int value);

        Assert.IsFalse(found);
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that a successful lookup in insertion-order mode does not change the iteration order.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenInsertionOrder_ShouldNotChangeOrder()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        _ = dictionary.TryGetValue("a", out _);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }
}
