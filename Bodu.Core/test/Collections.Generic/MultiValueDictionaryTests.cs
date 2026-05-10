// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="MultiValueDictionary{TKey, TValue}"/>.
/// </summary>
[TestClass]
public partial class MultiValueDictionaryTests
{
    // --------------------------------------------------------
    // Constructor — default
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the default constructor produces an empty dictionary with zero counts.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Ctor_WhenDefault_ShouldBeEmpty()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.KeyCount);
    }

    /// <summary>
    /// Verifies that the default constructor uses the default equality comparer for keys.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldUseDefaultComparer()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.AreEqual(EqualityComparer<string>.Default, sut.Comparer);
    }

    // --------------------------------------------------------
    // Constructor — with comparer
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that passing a null comparer to the comparer constructor defaults to the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        MultiValueDictionary<string, int> sut =
            new MultiValueDictionary<string, int>((IEqualityComparer<string>?)null);

        Assert.AreEqual(EqualityComparer<string>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that a custom comparer is used for key equality.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        MultiValueDictionary<string, int> sut =
            new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        sut.Add("KEY", 1);
        sut.Add("key", 2);

        Assert.AreEqual(1, sut.KeyCount);
        Assert.AreEqual(2, sut.Count);
    }

    // --------------------------------------------------------
    // Count and KeyCount
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Count"/> reflects the total value entries.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAdded_ShouldReflectTotalValueEntries()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("a", 2);
        sut.Add("b", 3);

        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.KeyCount"/> reflects only the number of distinct keys.
    /// </summary>
    [TestMethod]
    public void KeyCount_WhenItemsAdded_ShouldReflectDistinctKeyCount()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("a", 2);
        sut.Add("b", 3);

        Assert.AreEqual(2, sut.KeyCount);
    }

    // --------------------------------------------------------
    // Keys
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Keys"/> contains all distinct keys.
    /// </summary>
    [TestMethod]
    public void Keys_WhenItemsAdded_ShouldContainAllDistinctKeys()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("x", 1);
        sut.Add("y", 2);
        sut.Add("x", 3);

        CollectionAssert.Contains(sut.Keys.ToList(), "x");
        CollectionAssert.Contains(sut.Keys.ToList(), "y");
        Assert.AreEqual(2, sut.Keys.Count);
    }

    // --------------------------------------------------------
    // Indexer
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the indexer returns an empty list for an absent key rather than throwing.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyAbsent_ShouldReturnEmptyList()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        IReadOnlyList<int> result = sut["missing"];

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that the indexer returns the values for an existing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyPresent_ShouldReturnAssociatedValues()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 10);
        sut.Add("a", 20);

        IReadOnlyList<int> result = sut["a"];

        Assert.AreEqual(2, result.Count);
        CollectionAssert.AreEqual(new[] { 10, 20 }, result.ToList());
    }

    // --------------------------------------------------------
    // ContainsKey
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> returns <see langword="false"/> for an absent key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyAbsent_ShouldReturnFalse()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.IsFalse(sut.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.ContainsKey(null!);
        });
    }

    // --------------------------------------------------------
    // Clear
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Clear"/> removes all entries.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllKeysAndValues()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.KeyCount);
        Assert.IsFalse(sut.ContainsKey("a"));
    }
}
