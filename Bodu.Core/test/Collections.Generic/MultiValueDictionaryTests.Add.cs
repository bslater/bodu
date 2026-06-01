// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

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
        Assert.HasCount(3, mvd["Alpha"]);
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
        Assert.HasCount(3, mvd["a"]);
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
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add(TKey,TValue)"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            mvd.Add(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that a large number of keys and values are stored and retrieved correctly.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyKeysAndValuesAdded_ShouldRetrieveAllCorrectly()
    {
        var mvd = new MultiValueDictionary<int, int>();
        var keyCount = 200;
        var valuesPerKey = 10;

        for (var k = 0; k < keyCount; k++)
        {
            for (var v = 0; v < valuesPerKey; v++)
                mvd.Add(k, v);
        }

        Assert.AreEqual(keyCount * valuesPerKey, mvd.Count);
        Assert.AreEqual(keyCount, mvd.KeyCount);

        for (var k = 0; k < keyCount; k++)
        {
            IReadOnlyList<int> values = mvd[k];
            Assert.HasCount(valuesPerKey, values);
            for (var v = 0; v < valuesPerKey; v++)
                Assert.AreEqual(v, values[v]);
        }
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
        Assert.HasCount(valueCount, mvd["k"]);
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

}
