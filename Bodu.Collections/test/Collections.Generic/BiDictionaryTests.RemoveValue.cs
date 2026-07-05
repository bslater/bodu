// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.RemoveValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that removing an existing value removes the pair bound to it from both directions.
    /// </summary>
    [TestMethod]
    public void RemoveValue_WhenValueExists_ShouldRemoveBoundPair()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.RemoveValue(2);

        Assert.IsTrue(removed);
        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("b"));
        Assert.IsFalse(dictionary.ContainsValue(2));
    }

    /// <summary>
    /// Verifies that removing a missing value returns <see langword="false" /> without modifying state.
    /// </summary>
    [TestMethod]
    public void RemoveValue_WhenValueMissing_ShouldReturnFalse()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.RemoveValue(99);

        Assert.IsFalse(removed);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.RemoveValue(TValue)" /> throws when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveValue_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.RemoveValue(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
