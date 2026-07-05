// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="ICollection{T}.IsReadOnly" /> returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenRead_ShouldReturnFalse()
    {
        ICollection<KeyValuePair<string, int>> dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the generic <see cref="ICollection{T}" /> pair surface adds, finds, and removes matching pairs.
    /// </summary>
    [TestMethod]
    public void ICollection_WhenPairAddedAndRemoved_ShouldMutateDictionary()
    {
        var dictionary = new BiDictionary<string, int>();
        ICollection<KeyValuePair<string, int>> pairs = dictionary;

        pairs.Add(new KeyValuePair<string, int>("a", 1));

        Assert.IsTrue(pairs.Contains(new KeyValuePair<string, int>("a", 1)));
        Assert.IsFalse(pairs.Contains(new KeyValuePair<string, int>("a", 2)));
        Assert.IsTrue(pairs.Remove(new KeyValuePair<string, int>("a", 1)));
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that removing a pair whose value does not match leaves the dictionary unchanged.
    /// </summary>
    [TestMethod]
    public void ICollection_WhenPairValueMismatch_ShouldNotRemove()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();
        ICollection<KeyValuePair<string, int>> pairs = dictionary;

        bool removed = pairs.Remove(new KeyValuePair<string, int>("a", 99));

        Assert.IsFalse(removed);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.CopyTo(KeyValuePair{TKey, TValue}[], int)" /> copies every
    /// pair starting at the given index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationLargeEnough_ShouldCopyAllPairs()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[5];

        dictionary.CopyTo(array, 2);

        var copied = array.Skip(2).OrderBy(kvp => kvp.Key).ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3),
            },
            copied);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.CopyTo(KeyValuePair{TKey, TValue}[], int)" /> throws when
    /// the destination array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.CopyTo(KeyValuePair{TKey, TValue}[], int)" /> throws when
    /// the index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            dictionary.CopyTo(new KeyValuePair<string, int>[3], -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.CopyTo(KeyValuePair{TKey, TValue}[], int)" /> throws when
    /// the destination is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.CopyTo(new KeyValuePair<string, int>[2], 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Keys" /> and <see cref="BiDictionary{TKey,
    /// TValue}.Values" /> expose the current entries.
    /// </summary>
    [TestMethod]
    public void Keys_WhenRead_ShouldExposeCurrentEntries()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, dictionary.Values.ToArray());
    }

    /// <summary>
    /// Verifies that the read-only dictionary view exposes the same keys and values as the mutable surface.
    /// </summary>
    [TestMethod]
    public void IReadOnlyDictionary_WhenKeysAndValuesRead_ShouldMatchMutableSurface()
    {
        IReadOnlyDictionary<string, int> dictionary = CreatePopulated();

        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, dictionary.Values.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Add(object, object)" /> adds an entry through the non-generic surface.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenAddCalled_ShouldAddEntry()
    {
        IDictionary dictionary = new BiDictionary<string, int>();

        dictionary.Add("a", 1);

        Assert.AreEqual(1, dictionary["a"]);
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Add(object, object)" /> throws when the key is of the wrong type.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenKeyOfWrongType_ShouldThrowArgumentException()
    {
        IDictionary dictionary = new BiDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add(42, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Add(object, object)" /> throws when the value is of the wrong type.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenValueOfWrongType_ShouldThrowArgumentException()
    {
        IDictionary dictionary = new BiDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("a", "not an int");
        });
    }

    /// <summary>
    /// Verifies that the non-generic indexer getter returns the value for an existing key and
    /// <see langword="null" /> for a missing or mistyped key.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenIndexerGet_ShouldReturnValueOrNull()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.AreEqual(2, dictionary["b"]);
        Assert.IsNull(dictionary["missing"]);
        Assert.IsNull(dictionary[42]);
    }

    /// <summary>
    /// Verifies that the non-generic indexer setter assigns a value through the policy-aware generic indexer.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenIndexerSet_ShouldAssignValue()
    {
        IDictionary dictionary = new BiDictionary<string, int>();

        dictionary["a"] = 1;

        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Contains(object)" /> matches only correctly typed, present keys.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenContainsCalled_ShouldMatchTypedKeysOnly()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.Contains("a"));
        Assert.IsFalse(dictionary.Contains("missing"));
        Assert.IsFalse(dictionary.Contains(42));
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Remove(object)" /> removes an existing entry from both directions.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenRemoveCalled_ShouldRemoveEntry()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();
        IDictionary nonGeneric = dictionary;

        nonGeneric.Remove("a");

        Assert.IsFalse(dictionary.ContainsKey("a"));
        Assert.IsFalse(dictionary.ContainsValue(1));
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IDictionary" /> enumerator yields every entry.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenEnumerated_ShouldYieldAllEntries()
    {
        IDictionary dictionary = CreatePopulated();

        var keys = new List<object>();
        IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
            keys.Add(enumerator.Key);

        CollectionAssert.AreEquivalent(new object[] { "a", "b", "c" }, keys);
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.IsFixedSize" /> and <see cref="IDictionary.IsReadOnly" /> report a
    /// mutable, growable dictionary.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenFlagsRead_ShouldReportMutableGrowable()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.IsFixedSize);
        Assert.IsFalse(dictionary.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IDictionary.Keys" /> and <see cref="IDictionary.Values" />
    /// collections expose the current entries.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenKeysAndValuesRead_ShouldExposeCurrentEntries()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.AreEqual(3, dictionary.Keys.Count);
        Assert.AreEqual(3, dictionary.Values.Count);
    }
}
