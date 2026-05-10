// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    // --------------------------------------------------------
    // Add(TKey, TValue)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add(TKey,TValue)"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add(TKey,TValue)"/> creates a new key entry when the key is new.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNew_ShouldCreateEntryAndIncrementKeyCount()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        sut.Add("a", 1);

        Assert.AreEqual(1, sut.KeyCount);
        Assert.AreEqual(1, sut.Count);
        Assert.IsTrue(sut.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add(TKey,TValue)"/> appends to an existing key without duplicating the key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyExists_ShouldAppendValueWithoutDuplicatingKey()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        sut.Add("a", 1);
        sut.Add("a", 2);
        sut.Add("a", 3);

        Assert.AreEqual(1, sut.KeyCount);
        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(3, sut["a"].Count);
    }

    /// <summary>
    /// Verifies that values are stored in insertion order for each key.
    /// </summary>
    [TestMethod]
    public void Add_WhenValuesAddedForSameKey_ShouldPreserveInsertionOrder()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        sut.Add("k", 30);
        sut.Add("k", 10);
        sut.Add("k", 20);

        IReadOnlyList<int> values = sut["k"];

        Assert.AreEqual(30, values[0]);
        Assert.AreEqual(10, values[1]);
        Assert.AreEqual(20, values[2]);
    }

    // --------------------------------------------------------
    // AddRange(TKey, IEnumerable<TValue>)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.AddRange(null!, [1, 2]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> throws <see cref="ArgumentNullException"/> for a null values sequence.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenValuesIsNull_ShouldThrowArgumentNullException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.AddRange("a", null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> appends all values to an existing key.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCalled_ShouldAppendAllValuesInOrder()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 1);

        sut.AddRange("k", [2, 3, 4]);

        Assert.AreEqual(4, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, sut["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> correctly counts items from a new key.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenKeyIsNew_ShouldCreateKeyAndSetCount()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        sut.AddRange("new", [10, 20, 30]);

        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> does not create a key entry when the values sequence is empty and the key is new.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenEmptySequenceForNewKey_ShouldNotCreateKey()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        sut.AddRange("k", []);

        Assert.AreEqual(0, sut.KeyCount);
        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> does not modify the count when the values sequence is empty and the key already exists.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenEmptySequenceForExistingKey_ShouldNotModifyCount()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("k", 1);
        var countBefore = sut.Count;

        sut.AddRange("k", []);

        Assert.AreEqual(countBefore, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add"/> correctly maintains <see cref="MultiValueDictionary{TKey,TValue}.Count"/> across multiple keys.
    /// </summary>
    [TestMethod]
    public void Add_WhenMultipleKeysUsed_ShouldMaintainTotalCount()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        sut.Add("a", 1);
        sut.Add("b", 2);
        sut.Add("c", 3);
        sut.Add("a", 4);

        Assert.AreEqual(4, sut.Count);
        Assert.AreEqual(3, sut.KeyCount);
    }
}
