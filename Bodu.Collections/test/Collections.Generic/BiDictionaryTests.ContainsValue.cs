// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.ContainsValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.ContainsValue(TValue)" /> returns <see langword="true" />
    /// for a bound value.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueExists_ShouldReturnTrue()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.ContainsValue(3));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.ContainsValue(TValue)" /> returns <see langword="false" />
    /// for an unbound value.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueMissing_ShouldReturnFalse()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.ContainsValue(99));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.ContainsValue(TValue)" /> reflects removal of the pair
    /// that held the value.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenBoundPairRemoved_ShouldReturnFalse()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Remove("c");

        Assert.IsFalse(dictionary.ContainsValue(3));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.ContainsValue(TValue)" /> throws when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.ContainsValue(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
