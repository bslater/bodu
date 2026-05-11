// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.AddRange.cs" company="PlaceholderCompany">
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
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            mvd.AddRange(null!, [1, 2]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> throws <see cref="ArgumentNullException"/> for a null values sequence.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenValuesIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            mvd.AddRange("a", null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> appends all values to an existing key.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCalled_ShouldAppendAllValuesInOrder()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);

        mvd.AddRange("k", [2, 3, 4]);

        Assert.AreEqual(4, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, mvd["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> correctly counts items from a new key.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenKeyIsNew_ShouldCreateKeyAndSetCount()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.AddRange("new", [10, 20, 30]);

        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> does not create a key entry when the values sequence is empty and the key is new.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenEmptySequenceForNewKey_ShouldNotCreateKey()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.AddRange("k", []);

        Assert.AreEqual(0, mvd.KeyCount);
        Assert.AreEqual(0, mvd.Count);
        Assert.IsFalse(mvd.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> does not modify the count when the values sequence is empty and the key already exists.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenEmptySequenceForExistingKey_ShouldNotModifyCount()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);
        var countBefore = mvd.Count;

        mvd.AddRange("k", []);

        Assert.AreEqual(countBefore, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> appends all elements in the
    /// supplied sequence in insertion order, for a range of sequence lengths.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(10)]
    [DataRow(50)]
    public void AddRange_WhenSequenceHasVaryingLength_ShouldAppendAllInOrder(int length)
    {
        var mvd = new MultiValueDictionary<string, int>();
        var values = Enumerable.Range(0, length).ToArray();

        mvd.AddRange("k", values);

        Assert.AreEqual(length, mvd.Count);
        CollectionAssert.AreEqual(values, mvd["k"].ToList());
    }

    /// <summary>
    /// Verifies that adding an empty range to an existing key is a no-op and does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenEmptySourceForExistingKey_ShouldNotInvalidateActiveEnumerator()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        mvd.AddRange("a", Array.Empty<int>());

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that adding a range from an existing value view appends a snapshot of that view.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenSourceIsExistingValueView_ShouldAppendSnapshot()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.AddRange("k", [1, 2]);

        IReadOnlyList<int> source = mvd["k"];

        mvd.AddRange("k", source);

        CollectionAssert.AreEqual(new[] { 1, 2, 1, 2 }, mvd["k"].ToList());
        Assert.AreEqual(4, mvd.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.AddRange" /> preserves null values and insertion order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenValuesContainNull_ShouldPreserveNullEntries()
    {
        var mvd = new MultiValueDictionary<string, string?>();

        mvd.AddRange("k", ["a", null, "b"]);

        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual("a", mvd["k"][0]);
        Assert.IsNull(mvd["k"][1]);
        Assert.AreEqual("b", mvd["k"][2]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.AddRange" /> enumerates a deferred source once.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenSourceIsDeferred_ShouldEnumerateSourceOnce()
    {
        var mvd = new MultiValueDictionary<string, int>();
        var enumerationCount = 0;

        IEnumerable<int> Source()
        {
            enumerationCount++;
            yield return 1;
            yield return 2;
        }

        mvd.AddRange("k", Source());

        Assert.AreEqual(1, enumerationCount);
        CollectionAssert.AreEqual(new[] { 1, 2 }, mvd["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.AddRange" /> preserves dictionary state when a deferred source throws.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenDeferredSourceThrows_ShouldPreserveExistingValuesForSameKey()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.AddRange("k", [10, 20]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            mvd.AddRange("k", EnumerateOneThenThrowForAddRangeEdgeCases());
        });

        CollectionAssert.AreEqual(new[] { 10, 20 }, mvd["k"].ToList());
        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
    }

    private static IEnumerable<int> EnumerateOneThenThrowForAddRangeEdgeCases()
    {
        yield return 99;

        throw new InvalidOperationException("Simulated source enumeration failure.");
    }
}
