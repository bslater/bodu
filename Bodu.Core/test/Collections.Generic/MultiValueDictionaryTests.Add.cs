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
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add(TKey,TValue)"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            mvd.Add(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add(TKey,TValue)"/> creates a new key entry when the key is new.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNew_ShouldCreateEntryAndIncrementKeyCount()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.Add("a", 1);

        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(1, mvd.Count);
        Assert.IsTrue(mvd.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add(TKey,TValue)"/> appends to an existing key without duplicating the key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyExists_ShouldAppendValueWithoutDuplicatingKey()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("a", 3);

        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual(3, mvd["a"].Count);
    }

    /// <summary>
    /// Verifies that values are stored in insertion order for each key.
    /// </summary>
    [TestMethod]
    public void Add_WhenValuesAddedForSameKey_ShouldPreserveInsertionOrder()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.Add("k", 30);
        mvd.Add("k", 10);
        mvd.Add("k", 20);

        IReadOnlyList<int> values = mvd["k"];

        Assert.AreEqual(30, values[0]);
        Assert.AreEqual(10, values[1]);
        Assert.AreEqual(20, values[2]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add"/> correctly maintains <see cref="MultiValueDictionary{TKey,TValue}.Count"/> across multiple keys.
    /// </summary>
    [TestMethod]
    public void Add_WhenMultipleKeysUsed_ShouldMaintainTotalCount()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.Add("a", 1);
        mvd.Add("b", 2);
        mvd.Add("c", 3);
        mvd.Add("a", 4);

        Assert.AreEqual(4, mvd.Count);
        Assert.AreEqual(3, mvd.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add"/> correctly accumulates any number
    /// of values under a single key, across a range of value counts.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(10)]
    [DataRow(100)]
    public void Add_WhenValuesAccumulatedForOneKey_ShouldTrackCountExactly(int valueCount)
    {
        var mvd = new MultiValueDictionary<string, int>();

        for (var i = 0; i < valueCount; i++)
            mvd.Add("k", i);

        Assert.AreEqual(valueCount, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(valueCount, mvd["k"].Count);
    }

    /// <summary>
    /// Verifies that a struct key with the same field values is treated as the same key regardless of
    /// which instance is used, confirming value-type equality semantics for dictionary lookup.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsValueTypeStruct_ShouldMatchByValue()
    {
        var mvd = new MultiValueDictionary<Coord, string>();
        var key = new Coord(1, 2);

        mvd.Add(key, "alpha");
        mvd.Add(new Coord(1, 2), "beta");

        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(2, mvd[new Coord(1, 2)].Count);
    }

    /// <summary>
    /// Verifies that two struct keys with different field values are treated as distinct keys.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsValueTypeStructWithDifferentFields_ShouldStoreSeparately()
    {
        var mvd = new MultiValueDictionary<Coord, string>();

        mvd.Add(new Coord(1, 2), "a");
        mvd.Add(new Coord(3, 4), "b");

        Assert.AreEqual(2, mvd.KeyCount);
        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual("a", mvd[new Coord(1, 2)][0]);
        Assert.AreEqual("b", mvd[new Coord(3, 4)][0]);
    }

    /// <summary>
    /// Verifies that struct values are stored and retrieved by value, and that insertion order is preserved.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsValueTypeStruct_ShouldStoreAndRetrieveByValue()
    {
        var mvd = new MultiValueDictionary<string, Coord>();
        mvd.Add("grid", new Coord(1, 2));
        mvd.Add("grid", new Coord(3, 4));

        IReadOnlyList<Coord> values = mvd["grid"];

        Assert.AreEqual(2, values.Count);
        Assert.AreEqual(new Coord(1, 2), values[0]);
        Assert.AreEqual(new Coord(3, 4), values[1]);
    }

    /// <summary>
    /// Verifies that two distinct <see cref="Label"/> instances with the same text are stored as separate
    /// values, because <see cref="Label"/> uses reference identity.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsReferenceType_ShouldStoreBothInstances()
    {
        var l1 = new Label("hello");
        var l2 = new Label("hello");

        var mvd = new MultiValueDictionary<string, Label>();
        mvd.Add("k", l1);
        mvd.Add("k", l2);

        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(2, mvd["k"].Count);
    }

    /// <summary>
    /// Verifies that a custom key comparer causes case-insensitively equal string keys to be merged into a
    /// single entry, accumulating their values together.
    /// </summary>
    [TestMethod]
    public void Add_WhenCustomComparerUsed_ShouldMergeEquivalentKeys()
    {
        var mvd =
            new MultiValueDictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        mvd.Add("Alpha", 1);
        mvd.Add("ALPHA", 2);
        mvd.Add("alpha", 3);

        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual(3, mvd["Alpha"].Count);
    }

    /// <summary>
    /// Verifies that a <see langword="null"/> value can be stored and retrieved under a key.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldStoreAndRetrieveNull()
    {
        var mvd = new MultiValueDictionary<string, string?>();

        mvd.Add("k", null);

        Assert.AreEqual(1, mvd.Count);
        Assert.IsNull(mvd["k"][0]);
    }

    /// <summary>
    /// Verifies that a large number of keys and values are stored and retrieved correctly.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Add_WhenManyKeysAndValuesAdded_ShouldRetrieveAllCorrectly()
    {
        var mvd = new MultiValueDictionary<int, int>();
        int keyCount = 200;
        int valuesPerKey = 10;

        for (int k = 0; k < keyCount; k++)
        {
            for (int v = 0; v < valuesPerKey; v++)
                mvd.Add(k, v);
        }

        Assert.AreEqual(keyCount * valuesPerKey, mvd.Count);
        Assert.AreEqual(keyCount, mvd.KeyCount);

        for (int k = 0; k < keyCount; k++)
        {
            IReadOnlyList<int> values = mvd[k];
            Assert.AreEqual(valuesPerKey, values.Count);
            for (int v = 0; v < valuesPerKey; v++)
                Assert.AreEqual(v, values[v]);
        }
    }
}
