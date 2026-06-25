// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Add" /> over key/value pairs appends the entry.
    /// </summary>
    [TestMethod]
    public void ICollectionOfT_WhenAddCalled_ShouldAppendEntry()
    {
        ICollection<KeyValuePair<string, int>> dictionary = new SequencedDictionary<string, int>();

        dictionary.Add(new KeyValuePair<string, int>("a", 1));

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.Contains(new KeyValuePair<string, int>("a", 1)));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Remove" /> removes only when both key and value match.
    /// </summary>
    [TestMethod]
    public void ICollectionOfT_WhenRemoveValueMatches_ShouldRemoveAndReturnTrue()
    {
        ICollection<KeyValuePair<string, int>> dictionary = CreatePopulated();

        bool removed = dictionary.Remove(new KeyValuePair<string, int>("b", 2));

        Assert.IsTrue(removed);
        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Remove" /> returns <see langword="false" /> when the value does not match.
    /// </summary>
    [TestMethod]
    public void ICollectionOfT_WhenRemoveValueDiffers_ShouldReturnFalse()
    {
        ICollection<KeyValuePair<string, int>> dictionary = CreatePopulated();

        bool removed = dictionary.Remove(new KeyValuePair<string, int>("b", 999));

        Assert.IsFalse(removed);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.SyncRoot" /> returns the same instance on repeated reads.
    /// </summary>
    [TestMethod]
    public void ICollection_WhenSyncRootAccessedRepeatedly_ShouldReturnSameInstance()
    {
        ICollection dictionary = CreatePopulated();

        Assert.AreSame(dictionary.SyncRoot, dictionary.SyncRoot);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.IsSynchronized" /> reports the dictionary as not synchronized.
    /// </summary>
    [TestMethod]
    public void ICollection_WhenQueried_ShouldNotBeSynchronized()
    {
        ICollection dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.IsSynchronized);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> copies entries as <see cref="DictionaryEntry" /> values in iteration order.
    /// </summary>
    [TestMethod]
    public void ICollection_WhenCopyToArray_ShouldCopyEntriesInOrder()
    {
        ICollection dictionary = CreatePopulated();
        var array = new DictionaryEntry[3];

        dictionary.CopyTo(array, 0);

        Assert.AreEqual("a", array[0].Key);
        Assert.AreEqual(1, array[0].Value);
        Assert.AreEqual("c", array[2].Key);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws when the destination array is too small.
    /// </summary>
    [TestMethod]
    public void ICollection_WhenCopyToArrayTooSmall_ShouldThrowExactly()
    {
        ICollection dictionary = CreatePopulated();
        var array = new DictionaryEntry[2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.CopyTo(array, 0);
        });
    }
}
