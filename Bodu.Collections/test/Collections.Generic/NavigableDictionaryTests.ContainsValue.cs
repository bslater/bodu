// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.ContainsValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> returns <see langword="true" />
    /// for a stored value.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsPresent_ShouldReturnTrue()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.ContainsValue(2000));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> returns <see langword="false" />
    /// for a value no entry stores.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsAbsent_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.ContainsValue(999));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> finds a stored
    /// <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenNullValueIsStored_ShouldReturnTrue()
    {
        var sut = new NavigableDictionary<int, string?>();
        sut.Add(1, "one");
        sut.Add(2, null);

        Assert.IsTrue(sut.ContainsValue(null));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> returns <see langword="false" />
    /// for <see langword="null" /> when no entry stores a <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenNullValueIsNotStored_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, string?>();
        sut.Add(1, "one");

        Assert.IsFalse(sut.ContainsValue(null));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> matches duplicated values stored
    /// under different keys.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueStoredUnderMultipleKeys_ShouldReturnTrue()
    {
        var sut = new NavigableDictionary<int, string>();
        sut.Add(1, "shared");
        sut.Add(2, "shared");

        Assert.IsTrue(sut.ContainsValue("shared"));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> returns <see langword="false" />
    /// on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, string>();

        Assert.IsFalse(sut.ContainsValue("anything"));
    }
}
