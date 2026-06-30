// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.KeyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that the keys view reports the same count as the dictionary.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenQueried_ShouldReportDictionaryCount()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreEqual(3, dictionary.Keys.Count);
    }

    /// <summary>
    /// Verifies that the keys view reports membership through <see cref="ICollection{T}.Contains" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenContainsCalled_ShouldReportMembership()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.Keys.Contains("b"));
        Assert.IsFalse(dictionary.Keys.Contains("missing"));
    }

    /// <summary>
    /// Verifies that the keys view reports itself as read-only.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenQueried_ShouldBeReadOnly()
    {
        ICollection<string> keys = CreatePopulated().Keys;

        Assert.IsTrue(keys.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the generic <see cref="ICollection{T}.CopyTo" /> copies keys in iteration order from the specified index.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenCopyToWithOffset_ShouldCopyKeysInOrder()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        string[] array = new string[4];

        dictionary.Keys.CopyTo(array, 1);

        Assert.IsNull(array[0]);
        CollectionAssert.AreEqual(new[] { null, "a", "b", "c" }, array);
    }

    /// <summary>
    /// Verifies that the generic <see cref="ICollection{T}.CopyTo" /> throws when the destination array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenCopyToNullArray_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.Keys.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> copies keys into an object array in iteration order.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenNonGenericCopyTo_ShouldCopyKeysInOrder()
    {
        var keys = (ICollection)CreatePopulated().Keys;
        object[] array = new object[3];

        keys.CopyTo(array, 0);

        CollectionAssert.AreEqual(new object[] { "a", "b", "c" }, array);
    }

    /// <summary>
    /// Verifies that the keys view shares the dictionary's sync root.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenSyncRootAccessed_ShouldMatchDictionarySyncRoot()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreSame(((ICollection)dictionary).SyncRoot, ((ICollection)dictionary.Keys).SyncRoot);
    }

    /// <summary>
    /// Verifies that mutating the dictionary through the keys view's <see cref="ICollection{T}.Clear" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenClearCalled_ShouldThrowExactly()
    {
        ICollection<string> keys = CreatePopulated().Keys;

        Assert.ThrowsExactly<NotSupportedException>(keys.Clear);
    }

    /// <summary>
    /// Verifies that mutating the dictionary through the keys view's <see cref="ICollection{T}.Remove" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenRemoveCalled_ShouldThrowExactly()
    {
        ICollection<string> keys = CreatePopulated().Keys;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            keys.Remove("a");
        });
    }

    /// <summary>
    /// Verifies that mutating the dictionary while enumerating the keys view invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void KeyCollection_WhenDictionaryMutatedDuringEnumeration_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (string _ in dictionary.Keys)
                dictionary.Add("d", 4);
        });
    }

    /// <summary>
    /// Verifies that the keys view enumerator does not support <see cref="IEnumerator.Reset" />.
    /// </summary>
    /// <remarks>
    /// The keys view is exposed through a compiler-generated iterator, whose <see cref="IEnumerator.Reset" />
    /// implementation throws <see cref="NotSupportedException" /> — unlike the BCL <c>Dictionary.KeyCollection</c>.
    /// </remarks>
    [TestMethod]
    public void KeyCollection_WhenResetCalled_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        IEnumerator<string> enumerator = dictionary.Keys.GetEnumerator();

        Assert.ThrowsExactly<NotSupportedException>(enumerator.Reset);
    }
}
