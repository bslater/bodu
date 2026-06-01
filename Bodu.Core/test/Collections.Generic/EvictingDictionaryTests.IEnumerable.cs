// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.IEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that mutating the dictionary via Clear during enumeration invalidates the active enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryIsClearedDuringEnumeration_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<int, int>(10);
        for (var i = 0; i < 3; i++)
            dictionary.Add(i, i);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> _ in dictionary)
                dictionary.Clear();
        });
    }

    /// <summary>
    /// Verifies that mutating the dictionary via Add during enumeration invalidates the active enumerator and
    /// causes the next <c>MoveNext</c> to throw <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryIsMutatedByAddDuringEnumeration_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<int, int>(10);
        for (var i = 0; i < 5; i++)
            dictionary.Add(i, i);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> kvp in dictionary)
                dictionary.Add(kvp.Key + 100, kvp.Value);
        });
    }

    /// <summary>
    /// Verifies that mutating the dictionary via Remove during enumeration invalidates the active enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryIsMutatedByRemoveDuringEnumeration_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<int, int>(10);
        for (var i = 0; i < 5; i++)
            dictionary.Add(i, i);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> kvp in dictionary)
                dictionary.Remove(kvp.Key);
        });
    }

    /// <summary>
    /// Verifies that GetEnumerator returns an empty enumerator when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void IEnumerable_GenericGetEnumerator_WhenDictionaryIsEmpty_ShouldReturnNoElements()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        IEnumerator<KeyValuePair<string, int>> enumerator = dictionary.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }
    /// <summary>
    /// Verifies that GetEnumerator supports foreach iteration over all entries.
    /// </summary>
    [TestMethod]
    public void IEnumerable_GenericGetEnumerator_WhenUsedInForeach_ShouldEnumerateAllEntries()
    {
        var dictionary = new EvictingDictionary<int, string>(3);
        dictionary.Add(1, "A");
        dictionary.Add(2, "B");
        dictionary.Add(3, "C");

        var actual = new List<KeyValuePair<int, string>>();
        foreach (KeyValuePair<int, string> kvp in dictionary)
        {
            actual.Add(kvp);
        }

        CollectionAssert.AreEqual(dictionary.ToArray(), actual);
    }

    /// <summary>
    /// Verifies that the non-generic GetEnumerator returns the same entries as the generic one.
    /// </summary>
    [TestMethod]
    public void IEnumerable_NonGenericGetEnumerator_WhenUsed_ShouldReturnSameEntriesAsGenericEnumerator()
    {
        var dictionary = new EvictingDictionary<string, string>(3);
        dictionary.Add("x", "1");
        dictionary.Add("y", "2");

        var actual = new List<KeyValuePair<string, string>>();
        IEnumerator enumerator = ((System.Collections.IEnumerable)dictionary).GetEnumerator();

        while (enumerator.MoveNext())
        {
            Assert.IsInstanceOfType(enumerator.Current, typeof(KeyValuePair<string, string>));
            actual.Add((KeyValuePair<string, string>)enumerator.Current);
        }

        CollectionAssert.AreEqual(dictionary.ToArray(), actual);
    }

}
