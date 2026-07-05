// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsKey" /> returns <see langword="true" />
    /// for a present key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsPresent_ShouldReturnTrue()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.ContainsKey(20));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsKey" /> returns <see langword="false" />
    /// for an absent key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsAbsent_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.ContainsKey(25));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsKey" /> resolves membership through the
    /// active comparer.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsComparerEqual_ShouldReturnTrue()
    {
        var sut = new NavigableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha", 1);

        Assert.IsTrue(sut.ContainsKey("ALPHA"));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsKey" /> returns <see langword="false" />
    /// on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.ContainsKey(1));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsKey" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.ContainsKey(null!);
        }, "key");
    }
}
