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
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> returns <see langword="true"/> when the key is present.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyPresent_ShouldReturnTrue()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 1);

        Assert.IsTrue(sut.ContainsKey("k"));
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
    // Count — edge cases
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Count"/> decrements when the last value is removed from a key, eliminating the key entry.
    /// </summary>
    [TestMethod]
    public void Count_WhenLastValueRemovedFromKey_ShouldDecrementByOne()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 42);

        sut.Remove("k", 42);

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.KeyCount"/> decrements to zero after the last value is removed from the only key.
    /// </summary>
    [TestMethod]
    public void KeyCount_WhenLastValueRemovedFromKey_ShouldDecrementToZero()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 42);

        sut.Remove("k", 42);

        Assert.AreEqual(0, sut.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Count"/> is correctly decremented after <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/>.
    /// </summary>
    [TestMethod]
    public void Count_AfterRemoveAll_ShouldReflectRemovedValues()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("a", 2);
        sut.Add("a", 3);
        sut.Add("b", 4);

        sut.RemoveAll("a");

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
    }

    // --------------------------------------------------------
    // Keys — edge cases
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Keys"/> is empty when the dictionary contains no entries.
    /// </summary>
    [TestMethod]
    public void Keys_WhenEmpty_ShouldBeEmpty()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.AreEqual(0, sut.Keys.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Keys"/> no longer contains a key after all its values are removed via <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/>.
    /// </summary>
    [TestMethod]
    public void Keys_AfterRemoveAll_ShouldNotContainRemovedKey()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("x", 1);
        sut.Add("y", 2);

        sut.RemoveAll("x");

        CollectionAssert.DoesNotContain(sut.Keys.ToList(), "x");
        CollectionAssert.Contains(sut.Keys.ToList(), "y");
    }

    // --------------------------------------------------------
    // Clear — edge cases
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

    /// <summary>
    /// Verifies that calling <see cref="MultiValueDictionary{TKey,TValue}.Clear"/> on an already-empty dictionary leaves it in a valid empty state.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.KeyCount);
    }

    /// <summary>
    /// Verifies that entries added after <see cref="MultiValueDictionary{TKey,TValue}.Clear"/> are tracked correctly.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledThenItemsReAdded_ShouldTrackNewItems()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("old", 1);
        sut.Clear();

        sut.Add("new", 10);
        sut.Add("new", 20);

        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
        Assert.IsFalse(sut.ContainsKey("old"));
        Assert.IsTrue(sut.ContainsKey("new"));
    }
}
