// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.RemoveAll.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.RemoveAll" /> uses the configured key comparer.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenCustomComparerUsed_ShouldRemoveEquivalentKey()
    {
        var mvd = new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        mvd.Add("Alpha", 1);
        mvd.Add("Beta", 2);

        bool removed = mvd.RemoveAll("alpha");

        Assert.IsTrue(removed);
        Assert.IsFalse(mvd.ContainsKey("ALPHA"));
        Assert.IsTrue(mvd.ContainsKey("beta"));
        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
    }

    /// <summary>
    /// Verifies that removing all values for a missing key is a no-op and does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyAbsent_ShouldNotInvalidateActiveEnumerator()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        Assert.IsFalse(mvd.RemoveAll("missing"));

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> returns <see langword="false"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyAbsent_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, int>();

        bool result = mvd.RemoveAll("missing");

        Assert.IsFalse(result);
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> removes all values for a
    /// key regardless of how many values were stored, across a range of value counts.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(10)]
    public void RemoveAll_WhenKeyHasVariousValueCounts_ShouldRemoveAllAndUpdateCount(int valueCount)
    {
        var mvd = new MultiValueDictionary<string, int>();
        for (int i = 0; i < valueCount; i++)
            mvd.Add("k", i);
        mvd.Add("other", 99);

        bool result = mvd.RemoveAll("k");

        Assert.IsTrue(result);
        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyIsNull_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd.RemoveAll(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> removes the key and all its values.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyPresent_ShouldReturnTrueAndRemoveAllValues()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("x", 1);
        mvd.Add("x", 2);
        mvd.Add("x", 3);
        mvd.Add("y", 4);

        bool result = mvd.RemoveAll("x");

        Assert.IsTrue(result);
        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey("x"));
        Assert.IsTrue(mvd.ContainsKey("y"));
    }

}
