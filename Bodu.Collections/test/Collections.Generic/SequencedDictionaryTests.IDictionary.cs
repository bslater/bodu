// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
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
        IDictionary dictionary = new SequencedDictionary<string, int>();

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
        IDictionary dictionary = new SequencedDictionary<string, int>();

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

    /// <summary>
    /// Verifies that the non-generic indexer getter returns the value for an existing key.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenIndexerGetExistingKey_ShouldReturnValue()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.AreEqual(2, dictionary["b"]);
    }

    /// <summary>
    /// Verifies that the non-generic indexer getter returns <see langword="null" /> for a missing key.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenIndexerGetMissingKey_ShouldReturnNull()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.IsNull(dictionary["missing"]);
    }

    /// <summary>
    /// Verifies that the non-generic indexer getter returns <see langword="null" /> when the key is of the wrong type.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenIndexerGetWrongKeyType_ShouldReturnNull()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.IsNull(dictionary[42]);
    }

    /// <summary>
    /// Verifies that the non-generic indexer setter adds or updates an entry.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenIndexerSet_ShouldAssignValue()
    {
        IDictionary dictionary = new SequencedDictionary<string, int>();

        dictionary["a"] = 1;

        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the non-generic indexer setter throws when the value is of the wrong type.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenIndexerSetWrongValueType_ShouldThrowExactly()
    {
        IDictionary dictionary = new SequencedDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary["a"] = "not-an-int";
        });
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Contains(object)" /> reports membership for an existing key.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenContainsExistingKey_ShouldReturnTrue()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.Contains("a"));
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Contains(object)" /> returns <see langword="false" /> for a wrong-type key.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenContainsWrongKeyType_ShouldReturnFalse()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.IsFalse(dictionary.Contains(42));
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Remove(object)" /> removes an existing key.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenRemoveExistingKey_ShouldRemoveEntry()
    {
        IDictionary dictionary = CreatePopulated();

        dictionary.Remove("b");

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.Contains("b"));
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary.Remove(object)" /> throws when the key is of the wrong type.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenRemoveWrongKeyType_ShouldThrowExactly()
    {
        IDictionary dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Remove(42);
        });
    }

    /// <summary>
    /// Verifies that the non-generic enumerator exposes each entry's key, value, and <see cref="DictionaryEntry" />.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenEnumeratorRead_ShouldExposeEntryKeyAndValue()
    {
        IDictionary dictionary = CreatePopulated();
        IDictionaryEnumerator enumerator = dictionary.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Key);
        Assert.AreEqual(1, enumerator.Value);
        Assert.AreEqual(new DictionaryEntry("a", 1), enumerator.Entry);
    }

    /// <summary>
    /// Verifies that resetting the non-generic enumerator restarts iteration from the first entry.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenEnumeratorReset_ShouldRestartFromFirstEntry()
    {
        IDictionary dictionary = CreatePopulated();
        IDictionaryEnumerator enumerator = dictionary.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Reset();
        Assert.IsTrue(enumerator.MoveNext());

        Assert.AreEqual("a", enumerator.Key);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IDictionary.Keys" /> and <see cref="IDictionary.Values" /> expose entries in iteration order.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenKeysAndValuesRead_ShouldExposeInOrder()
    {
        IDictionary dictionary = CreatePopulated();

        CollectionAssert.AreEqual(new object[] { "a", "b", "c" }, dictionary.Keys.Cast<object>().ToArray());
        CollectionAssert.AreEqual(new object[] { 1, 2, 3 }, dictionary.Values.Cast<object>().ToArray());
    }
}
