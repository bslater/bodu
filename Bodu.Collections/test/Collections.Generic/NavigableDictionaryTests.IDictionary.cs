// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.IsReadOnly" /> reports <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenQueried_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.IsReadOnly);
        Assert.IsFalse(((ICollection<KeyValuePair<int, int>>)sut).IsReadOnly);
    }

    /// <summary>
    /// Verifies that the pair-based <see cref="ICollection{T}.Add" /> delegates to the key/value overload,
    /// including its duplicate-key throw.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenPairSupplied_ShouldDelegateToAdd()
    {
        var sut = new NavigableDictionary<int, string>();
        ICollection<KeyValuePair<int, string>> collection = sut;

        collection.Add(new KeyValuePair<int, string>(1, "one"));

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("one", sut[1]);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.Add(new KeyValuePair<int, string>(1, "again"));
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the pair-based <see cref="ICollection{T}.Contains" /> matches only when both the key and the
    /// value agree.
    /// </summary>
    [TestMethod]
    public void ICollectionContains_WhenPairQueried_ShouldMatchKeyAndValue()
    {
        var sut = CreateDictionary(10, 20);
        ICollection<KeyValuePair<int, int>> collection = sut;

        Assert.IsTrue(collection.Contains(new KeyValuePair<int, int>(10, 1000)));
        Assert.IsFalse(collection.Contains(new KeyValuePair<int, int>(10, 999)));
        Assert.IsFalse(collection.Contains(new KeyValuePair<int, int>(99, 1000)));
    }

    /// <summary>
    /// Verifies that the pair-based <see cref="ICollection{T}.Remove" /> removes only an exact key/value match.
    /// </summary>
    [TestMethod]
    public void ICollectionRemove_WhenPairSupplied_ShouldRemoveOnlyExactMatch()
    {
        var sut = CreateDictionary(10, 20);
        ICollection<KeyValuePair<int, int>> collection = sut;

        Assert.IsFalse(collection.Remove(new KeyValuePair<int, int>(10, 999)));
        Assert.AreEqual(2, sut.Count);

        Assert.IsTrue(collection.Remove(new KeyValuePair<int, int>(10, 1000)));
        Assert.AreEqual(1, sut.Count);
        Assert.IsFalse(sut.ContainsKey(10));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CopyTo" /> copies the entries in ascending key
    /// order starting at the requested index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayHasSpace_ShouldCopyEntriesAscendingFromIndex()
    {
        var sut = CreateDictionary(30, 10, 20);
        var destination = new KeyValuePair<int, int>[4];

        sut.CopyTo(destination, 1);

        Assert.AreEqual(default, destination[0]);
        Assert.AreEqual(new KeyValuePair<int, int>(10, 1000), destination[1]);
        Assert.AreEqual(new KeyValuePair<int, int>(20, 2000), destination[2]);
        Assert.AreEqual(new KeyValuePair<int, int>(30, 3000), destination[3]);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CopyTo" /> validates the destination array and
    /// index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArgumentsInvalid_ShouldThrow()
    {
        var sut = CreateDictionary(1, 2, 3);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.CopyTo(null!, 0);
        });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.CopyTo(new KeyValuePair<int, int>[3], -1);
        });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new KeyValuePair<int, int>[3], 1);
        });
    }

    /// <summary>
    /// Verifies that the dictionary behaves correctly when accessed through
    /// <see cref="IDictionary{TKey, TValue}" /> — indexer, lookup, and removal round-trip.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenAccessedThroughInterface_ShouldRoundTripEntries()
    {
        IDictionary<int, string> sut = new NavigableDictionary<int, string>();

        sut.Add(2, "two");
        sut[1] = "one";

        Assert.AreEqual(2, sut.Count);
        Assert.IsTrue(sut.ContainsKey(1));
        Assert.IsTrue(sut.TryGetValue(2, out string? value));
        Assert.AreEqual("two", value);
        Assert.IsTrue(sut.Remove(1));
        Assert.AreEqual(1, sut.Count);
    }
}
