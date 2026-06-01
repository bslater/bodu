// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.Remove" /> uses the configured key comparer.
    /// </summary>
    [TestMethod]
    public void Remove_WhenCustomComparerUsed_ShouldRemoveFromEquivalentKey()
    {
        var mvd = new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        mvd.Add("Alpha", 1);
        mvd.Add("Alpha", 2);

        var removed = mvd.Remove("alpha", 1);

        Assert.IsTrue(removed);
        Assert.AreEqual(1, mvd.Count);
        CollectionAssert.AreEqual(new[] { 2 }, mvd["ALPHA"].ToList());
    }

    /// <summary>
    /// Verifies that removing a missing key is a no-op and does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldNotInvalidateActiveEnumerator()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        Assert.IsFalse(mvd.Remove("missing", 1));

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> returns <see langword="false"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, int>();

        var result = mvd.Remove("missing", 1);

        Assert.IsFalse(result);
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsNull_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd.Remove(null!, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes the key entry when its last value is removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastValueRemoved_ShouldRemoveKeyEntry()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 42);

        mvd.Remove("k", 42);

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that removing a missing value from an existing key is a no-op and does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueAbsentForExistingKey_ShouldNotInvalidateActiveEnumerator()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        Assert.IsFalse(mvd.Remove("a", 99));

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> returns <see langword="false"/> when the value is absent for an existing key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueAbsentForExistingKey_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);

        var result = mvd.Remove("k", 99);

        Assert.IsFalse(result);
        Assert.AreEqual(1, mvd.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes only one occurrence when the same value appears multiple times under a key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueAppearsMultipleTimes_ShouldRemoveOnlyOneOccurrence()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 5);
        mvd.Add("k", 5);
        mvd.Add("k", 5);

        var result = mvd.Remove("k", 5);

        Assert.IsTrue(result);
        Assert.AreEqual(2, mvd.Count);
        Assert.HasCount(2, mvd["k"]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> can remove the
    /// value at any position within a key's list, for a range of list sizes.
    /// </summary>
    [TestMethod]
    [DataRow(3, 0)]
    [DataRow(3, 1)]
    [DataRow(3, 2)]
    [DataRow(5, 0)]
    [DataRow(5, 4)]
    public void Remove_WhenValueIsAtVariousPositions_ShouldRemoveExactlyOne(int listSize, int positionToRemove)
    {
        var mvd = new MultiValueDictionary<string, int>();
        for (var i = 0; i < listSize; i++)
            mvd.Add("k", i * 10);

        var valueToRemove = positionToRemove * 10;
        var result = mvd.Remove("k", valueToRemove);

        Assert.IsTrue(result);
        Assert.AreEqual(listSize - 1, mvd.Count);
        Assert.DoesNotContain(valueToRemove, mvd["k"]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes the first occurrence and preserves remaining values when the value appears at the first position.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsFirstInList_ShouldPreserveRemaining()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 10);
        mvd.Add("k", 20);
        mvd.Add("k", 30);

        var result = mvd.Remove("k", 10);

        Assert.IsTrue(result);
        Assert.AreEqual(2, mvd.Count);
        CollectionAssert.AreEqual(new[] { 20, 30 }, mvd["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes the value and preserves remaining when the value appears at the last position.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsLastInList_ShouldPreserveRemaining()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 10);
        mvd.Add("k", 20);
        mvd.Add("k", 30);

        var result = mvd.Remove("k", 30);

        Assert.IsTrue(result);
        Assert.AreEqual(2, mvd.Count);
        CollectionAssert.AreEqual(new[] { 10, 20 }, mvd["k"].ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes one value and returns <see langword="true"/>.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValuePresent_ShouldReturnTrueAndDecrementCount()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);
        mvd.Add("k", 2);
        mvd.Add("k", 3);

        var result = mvd.Remove("k", 2);

        Assert.IsTrue(result);
        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        CollectionAssert.AreEqual(new[] { 1, 3 }, mvd["k"].ToList());
    }

}
