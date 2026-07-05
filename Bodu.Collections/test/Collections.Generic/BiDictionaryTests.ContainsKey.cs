// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.ContainsKey(TKey)" /> returns <see langword="true" /> for
    /// an existing key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyExists_ShouldReturnTrue()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.ContainsKey(TKey)" /> returns <see langword="false" /> for
    /// a missing key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyMissing_ShouldReturnFalse()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.ContainsKey(TKey)" /> throws when the key is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.ContainsKey(null!);
        });

        Assert.AreEqual("key", ex.ParamName);
    }
}
