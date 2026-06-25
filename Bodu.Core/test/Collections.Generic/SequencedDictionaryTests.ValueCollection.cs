// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.ValueCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that the values view reports the same count as the dictionary.
    /// </summary>
    [TestMethod]
    public void ValueCollection_WhenQueried_ShouldReportDictionaryCount()
    {
        var dictionary = CreatePopulated();

        Assert.AreEqual(3, dictionary.Values.Count);
    }

    /// <summary>
    /// Verifies that the values view reports itself as read-only.
    /// </summary>
    [TestMethod]
    public void ValueCollection_WhenQueried_ShouldBeReadOnly()
    {
        ICollection<int> values = CreatePopulated().Values;

        Assert.IsTrue(values.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the generic <see cref="ICollection{T}.CopyTo" /> copies values in iteration order from the specified index.
    /// </summary>
    [TestMethod]
    public void ValueCollection_WhenCopyToWithOffset_ShouldCopyValuesInOrder()
    {
        var dictionary = CreatePopulated();
        var array = new int[4];

        dictionary.Values.CopyTo(array, 1);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> copies values into an object array in iteration order.
    /// </summary>
    [TestMethod]
    public void ValueCollection_WhenNonGenericCopyTo_ShouldCopyValuesInOrder()
    {
        ICollection values = (ICollection)CreatePopulated().Values;
        var array = new object[3];

        values.CopyTo(array, 0);

        CollectionAssert.AreEqual(new object[] { 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that the values view shares the dictionary's sync root.
    /// </summary>
    [TestMethod]
    public void ValueCollection_WhenSyncRootAccessed_ShouldMatchDictionarySyncRoot()
    {
        var dictionary = CreatePopulated();

        Assert.AreSame(((ICollection)dictionary).SyncRoot, ((ICollection)dictionary.Values).SyncRoot);
    }

    /// <summary>
    /// Verifies that mutating the dictionary through the values view's <see cref="ICollection{T}.Add" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_WhenAddCalled_ShouldThrowExactly()
    {
        ICollection<int> values = CreatePopulated().Values;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            values.Add(99);
        });
    }

    /// <summary>
    /// Verifies that mutating the dictionary through the values view's <see cref="ICollection{T}.Clear" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_WhenClearCalled_ShouldThrowExactly()
    {
        ICollection<int> values = CreatePopulated().Values;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            values.Clear();
        });
    }
}
