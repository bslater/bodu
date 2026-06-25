// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedDictionaryTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class LinkedDictionaryTests
{
    /// <summary>
    /// Verifies that the non-generic <see cref="IDictionary" /> enumerator yields entries in iteration order.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenEnumerated_ShouldYieldEntriesInOrder()
    {
        IDictionary dictionary = CreatePopulated();

        var keys = new List<object>();
        IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
            keys.Add(enumerator.Key);

        CollectionAssert.AreEqual(new object[] { "a", "b", "c" }, keys);
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Add(object, object)" /> appends a new entry.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenAddCalled_ShouldAppendEntry()
    {
        IDictionary dictionary = new LinkedDictionary<string, int>();

        dictionary.Add("a", 1);

        Assert.AreEqual(1, dictionary["a"]);
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Add(object, object)" /> throws when the key is of the wrong type.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenKeyOfWrongType_ShouldThrowExactly()
    {
        IDictionary dictionary = new LinkedDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add(42, 1);
        });
    }

    /// <summary>
    /// Verifies that the read-only dictionary view exposes the same keys in iteration order.
    /// </summary>
    [TestMethod]
    public void IReadOnlyDictionary_WhenKeysRead_ShouldYieldKeysInOrder()
    {
        IReadOnlyDictionary<string, int> dictionary = CreatePopulated();

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, dictionary.Values.ToArray());
    }
}
