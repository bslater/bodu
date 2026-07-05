// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="NavigableDictionary{TKey, TValue}" />.
/// </summary>
[TestClass]
public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies the happy-path smoke check: adding unordered entries yields key-sorted enumeration and answers a
    /// floor query.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void NavigableDictionary_AddAndQuery_ShouldSortByKeyAndAnswerFloor()
    {
        var sut = new NavigableDictionary<int, string>();

        sut.Add(30, "thirty");
        sut.Add(10, "ten");
        sut.Add(20, "twenty");

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.Select(e => e.Key).ToList());
        Assert.IsTrue(sut.TryGetFloorEntry(25, out KeyValuePair<int, string> floor));
        Assert.AreEqual(20, floor.Key);
        Assert.AreEqual("twenty", floor.Value);
    }

    /// <summary>
    /// Creates a <see cref="NavigableDictionary{TKey, TValue}" /> of <see cref="int" /> keys seeded with the
    /// specified keys via <see cref="NavigableDictionary{TKey, TValue}.Add" />, each mapped to
    /// <c>key * 100</c>.
    /// </summary>
    /// <param name="keys">The keys to add, in the given order.</param>
    /// <returns>The populated dictionary.</returns>
    private static NavigableDictionary<int, int> CreateDictionary(params int[] keys)
    {
        var dictionary = new NavigableDictionary<int, int>();
        foreach (int key in keys)
            dictionary.Add(key, key * 100);

        return dictionary;
    }

    /// <summary>
    /// Materialises the entries of <paramref name="dictionary" /> using the public struct enumerator.
    /// </summary>
    /// <typeparam name="TKey">The key type of the dictionary.</typeparam>
    /// <typeparam name="TValue">The value type of the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to snapshot.</param>
    /// <returns>A list containing the entries in enumeration order.</returns>
    private static List<KeyValuePair<TKey, TValue>> SnapshotByEnumerator<TKey, TValue>(NavigableDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        var result = new List<KeyValuePair<TKey, TValue>>(dictionary.Count);
        foreach (KeyValuePair<TKey, TValue> entry in dictionary)
            result.Add(entry);

        return result;
    }
}
