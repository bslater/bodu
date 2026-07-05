// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" /> returns the value for an
    /// existing key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExists_ShouldReturnTrueAndValue()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool found = dictionary.TryGetValue("b", out int value);

        Assert.IsTrue(found);
        Assert.AreEqual(2, value);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" /> returns
    /// <see langword="false" /> and the default value for a missing key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissing_ShouldReturnFalseAndDefault()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool found = dictionary.TryGetValue("missing", out int value);

        Assert.IsFalse(found);
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" /> throws when the key is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryGetValue(null!, out _);
        });

        Assert.AreEqual("key", ex.ParamName);
    }
}
